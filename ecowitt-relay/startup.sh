#!/bin/bash
# GCE startup script — installs Node.js and runs the Ecowitt relay as a systemd service.

set -e

# Install Node.js 20 LTS
curl -fsSL https://deb.nodesource.com/setup_20.x | bash -
apt-get install -y nodejs

# Create relay directory and script
mkdir -p /opt/ecowitt-relay
cat > /opt/ecowitt-relay/relay.js << 'EOF'
const http = require("http");
const https = require("https");

const LOCAL_PORT = 80;
const TARGET_HOST = "lawncare-7fa77.web.app";
const TARGET_PATH = "/api/weather/ecowitt";

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

    const options = {
      hostname: TARGET_HOST,
      port: 443,
      path: TARGET_PATH,
      method: "POST",
      headers: {
        "Content-Type": "application/x-www-form-urlencoded",
        "Content-Length": body.length,
      },
    };

    const proxy = https.request(options, (proxyRes) => {
      console.log(`[${now}] Forwarded → ${proxyRes.statusCode}`);
      res.writeHead(proxyRes.statusCode);
      res.end();
    });

    proxy.on("error", (err) => {
      console.error(`[${now}] Forward error: ${err.message}`);
      res.writeHead(502);
      res.end("Bad Gateway");
    });

    proxy.write(body);
    proxy.end();
  });
});

server.listen(LOCAL_PORT, "0.0.0.0", () => {
  console.log(`Ecowitt relay listening on http://0.0.0.0:${LOCAL_PORT}`);
  console.log(`Forwarding to https://${TARGET_HOST}${TARGET_PATH}`);
});
EOF

# Create systemd service
cat > /etc/systemd/system/ecowitt-relay.service << 'EOF'
[Unit]
Description=Ecowitt HTTP to HTTPS Relay
After=network.target

[Service]
ExecStart=/usr/bin/node /opt/ecowitt-relay/relay.js
Restart=always
RestartSec=5
StandardOutput=journal
StandardError=journal

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable ecowitt-relay
systemctl start ecowitt-relay
