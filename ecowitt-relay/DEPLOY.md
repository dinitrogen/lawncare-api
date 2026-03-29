# Ecowitt Relay

## Why this exists

The Ecowitt GW1100 gateway only supports **plain HTTP** for custom server uploads.
Our API lives behind Firebase Hosting which only serves **HTTPS**.
This relay bridges the gap: it listens on HTTP and forwards the data to the HTTPS API.

There are two ways to run the relay:

| Mode | When to use | Always-on? |
|------|------------|------------|
| **Local** (your PC) | Quick testing, development | No — stops when PC is off |
| **Cloud** (GCE VM) | Production, 24/7 collection | Yes — free-tier `e2-micro` |

---

## Option A: Local relay (your PC)

Run this when you want to test quickly or your PC is on. No cloud setup needed.

### 1. Start the relay

Open a terminal in the `ecowitt-relay` folder and run:

```bash
node relay.js
```

You should see:

```
Ecowitt relay listening on http://0.0.0.0:8080
Forwarding to https://lawncare-7fa77.web.app/api/weather/ecowitt
```

### 2. Find your PC's local IP

**Windows:** Open a terminal and run `ipconfig`. Look for your Wi-Fi or Ethernet adapter's
**IPv4 Address** (something like `192.168.1.143`).

### 3. Configure the gateway

In the Ecowitt app → your device → Custom Server:

| Setting         | Value                          |
|-----------------|--------------------------------|
| Enable          | On                             |
| Protocol        | Ecowitt                        |
| Server          | Your PC's local IP (e.g. `192.168.1.143`) |
| Path            | `/api/weather/ecowitt`         |
| Port            | `8080`                         |
| Upload Interval | 60 seconds                     |

### 4. Stop the relay

Press `Ctrl+C` in the terminal.

---

## Option B: Cloud relay (GCE VM — always-on, free)

This runs 24/7 on Google's free-tier `e2-micro` VM. One-time setup.

### Prerequisites

- GCP project `lawncare-7fa77` with billing enabled
- A browser — all commands run in [**Google Cloud Shell**](https://shell.cloud.google.com/?project=lawncare-7fa77)
  (a free Linux terminal in your browser, no installs needed)

### Step 1: Open Cloud Shell

Go to https://shell.cloud.google.com/?project=lawncare-7fa77 and wait for the terminal to load.

### Step 2: Create a firewall rule

This tells Google Cloud to allow incoming web traffic (port 80) to reach the relay VM.
**Copy-paste this entire block** into Cloud Shell and press Enter:

```bash
gcloud compute firewall-rules create allow-ecowitt-relay \
  --direction=INGRESS \
  --priority=1000 \
  --network=default \
  --action=ALLOW \
  --rules=tcp:80 \
  --target-tags=ecowitt-relay \
  --source-ranges=0.0.0.0/0
```

### Step 3: Reserve a static IP address

This gives the VM a permanent public IP that won't change if the VM restarts.
**Copy-paste and run:**

```bash
gcloud compute addresses create ecowitt-relay-ip \
  --region=us-central1
```

### Step 4: Get the IP address

This prints the IP you'll configure the gateway with. **Copy-paste and run:**

```bash
gcloud compute addresses describe ecowitt-relay-ip \
  --region=us-central1 --format="get(address)"
```

**Write down the IP address** that gets printed (e.g. `136.119.134.190`). You'll need it in Step 6.

### Step 5: Create the VM

This is a big command that does several things at once:
1. Creates a file called `startup.sh` that tells the VM what software to install and run
2. Creates the VM itself and attaches the static IP from Step 3

**Copy-paste this entire block** (from `cat` all the way down to the last line) into
Cloud Shell and press Enter. It's one single command even though it's many lines:

```bash
cat > /tmp/startup.sh << 'STARTUP'
#!/bin/bash
set -e
curl -fsSL https://deb.nodesource.com/setup_20.x | bash -
apt-get install -y nodejs
mkdir -p /opt/ecowitt-relay

cat > /opt/ecowitt-relay/relay.js << 'EOF'
const http = require("http");
const https = require("https");
const server = http.createServer((req, res) => {
  if (req.method !== "POST") {
    res.writeHead(200);
    res.end("Ecowitt relay running");
    return;
  }
  const chunks = [];
  req.on("data", (chunk) => chunks.push(chunk));
  req.on("end", () => {
    const body = Buffer.concat(chunks);
    const now = new Date().toISOString();
    console.log(`[${now}] Received ${body.length} bytes from ${req.socket.remoteAddress}`);
    const proxy = https.request({
      hostname: "lawncare-7fa77.web.app",
      port: 443,
      path: "/api/weather/ecowitt",
      method: "POST",
      headers: {
        "Content-Type": "application/x-www-form-urlencoded",
        "Content-Length": body.length,
      },
    }, (r) => {
      console.log(`[${now}] Forwarded -> ${r.statusCode}`);
      res.writeHead(r.statusCode);
      res.end();
    });
    proxy.on("error", (e) => {
      console.error(`[${now}] Forward error: ${e.message}`);
      res.writeHead(502);
      res.end();
    });
    proxy.write(body);
    proxy.end();
  });
});
server.listen(80, "0.0.0.0", () => console.log("Relay listening on :80"));
EOF

cat > /etc/systemd/system/ecowitt-relay.service << 'EOF'
[Unit]
Description=Ecowitt Relay
After=network.target

[Service]
ExecStart=/usr/bin/node /opt/ecowitt-relay/relay.js
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable --now ecowitt-relay
STARTUP

gcloud compute instances create ecowitt-relay \
  --zone=us-central1-a \
  --machine-type=e2-micro \
  --image-family=debian-12 \
  --image-project=debian-cloud \
  --tags=ecowitt-relay \
  --address=ecowitt-relay-ip \
  --metadata-from-file=startup-script=/tmp/startup.sh
```

Wait for it to finish. It takes about 30 seconds. You'll see a table with the VM details.

### Step 6: Verify it's working

Wait about 60 seconds for the VM to start up and install Node.js, then run this test.
**Copy-paste and run** (it uses the IP from Step 4 automatically):

```bash
RELAY_IP=$(gcloud compute addresses describe ecowitt-relay-ip \
  --region=us-central1 --format="get(address)")

curl -X POST "http://$RELAY_IP/api/weather/ecowitt" \
  -d "PASSKEY=test&stationtype=GW1100A&dateutc=2025-01-01+12:00:00&tempf=70&humidity=52"
```

If it returns nothing and no error, that's a success (HTTP 200). If you get
"Connection refused", wait another 30 seconds and try again.

### Step 7: Configure the gateway

In the Ecowitt app → your device → Custom Server:

| Setting         | Value                          |
|-----------------|--------------------------------|
| Enable          | On                             |
| Protocol        | Ecowitt                        |
| Server          | The IP from Step 4 (e.g. `136.119.134.190`) |
| Path            | `api/weather/ecowitt`         |
| Port            | `80`                           |
| Upload Interval | 60 seconds                     |

---

## Switching between Local and Cloud

Only one relay should be active at a time. To switch, just change the **Server** and **Port**
in the Ecowitt app's Custom Server settings:

| Switch to | Server | Port |
|-----------|--------|------|
| Local PC  | Your PC's local IP (e.g. `192.168.1.143`) | `8080` |
| Cloud VM  | The static IP from Step 4 (e.g. `136.119.134.190`) | `80` |

Everything else (Path, Protocol, Upload Interval) stays the same.

---

## Cloud VM management

All commands below are run in [Cloud Shell](https://shell.cloud.google.com/?project=lawncare-7fa77).

### View relay logs (live)

See what the relay is receiving in real time. Press `Ctrl+C` to stop watching.

```bash
gcloud compute ssh ecowitt-relay --zone=us-central1-a \
  --command="journalctl -u ecowitt-relay -f"
```

### Restart the relay

If the relay seems stuck or you need to refresh it:

```bash
gcloud compute ssh ecowitt-relay --zone=us-central1-a \
  --command="sudo systemctl restart ecowitt-relay"
```

### Stop the VM (saves resources, stops data collection)

```bash
gcloud compute instances stop ecowitt-relay --zone=us-central1-a
```

### Start the VM back up

```bash
gcloud compute instances start ecowitt-relay --zone=us-central1-a
```

### Delete everything (if you no longer need the cloud relay)

```bash
gcloud compute instances delete ecowitt-relay --zone=us-central1-a
gcloud compute addresses delete ecowitt-relay-ip --region=us-central1
gcloud compute firewall-rules delete allow-ecowitt-relay
```
