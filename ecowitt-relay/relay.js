/**
 * Ecowitt HTTP → HTTPS Relay
 *
 * The GW1100 gateway only supports plain HTTP for custom server uploads.
 * This lightweight relay listens on HTTP locally and forwards the form data
 * to the HTTPS API endpoint.
 *
 * Usage:
 *   node relay.js
 *
 * Gateway config:
 *   Server:   <this PC's local IP, e.g. 192.168.1.100>
 *   Path:     /api/weather/ecowitt
 *   Port:     8080
 *   Protocol: Ecowitt
 */

const http = require("http");
const https = require("https");

const LOCAL_PORT = 8080;
const TARGET_HOST = "lawncare-7fa77.web.app";
const TARGET_PATH = "/api/weather/ecowitt";

const server = http.createServer((req, res) => {
  if (req.method !== "POST") {
    res.writeHead(405);
    res.end("Method Not Allowed");
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
