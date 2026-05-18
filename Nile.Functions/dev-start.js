#!/usr/bin/env node
// Launches the Azure Functions host and the Angular dev server together.
// The frontend's `npm start` already calls `ng serve --open`, so the browser
// auto-opens once the Angular dev server finishes its initial compile.

const { spawn } = require('child_process');
const net = require('net');
const path = require('path');

const repoRoot = path.resolve(__dirname, '..');
const backendDir = path.resolve(__dirname, 'Clients/Nile.Client.Functions');
const frontendDir = path.resolve(repoRoot, 'FrontEnd');

const BACKEND_PORT = 7071;
const READINESS_TIMEOUT_MS = 60_000;
const POLL_INTERVAL_MS = 500;

let backend = null;
let frontend = null;
let shuttingDown = false;

function shutdown(code) {
  if (shuttingDown) return;
  shuttingDown = true;
  if (frontend && frontend.exitCode === null) frontend.kill();
  if (backend && backend.exitCode === null) backend.kill();
  process.exit(code ?? 0);
}

function prefix(stream, tag) {
  let buffer = '';
  stream.on('data', (chunk) => {
    buffer += chunk.toString();
    let nl;
    while ((nl = buffer.indexOf('\n')) !== -1) {
      process.stdout.write(`[${tag}] ${buffer.slice(0, nl + 1)}`);
      buffer = buffer.slice(nl + 1);
    }
  });
  stream.on('end', () => {
    if (buffer.length) process.stdout.write(`[${tag}] ${buffer}\n`);
  });
}

function probePort(port) {
  return new Promise((resolve) => {
    const socket = net.connect({ host: '127.0.0.1', port });
    socket.once('connect', () => { socket.end(); resolve(true); });
    socket.once('error', () => { resolve(false); });
    socket.setTimeout(POLL_INTERVAL_MS, () => { socket.destroy(); resolve(false); });
  });
}

async function waitForBackend() {
  const deadline = Date.now() + READINESS_TIMEOUT_MS;
  while (Date.now() < deadline) {
    if (await probePort(BACKEND_PORT)) return true;
    await new Promise((r) => setTimeout(r, POLL_INTERVAL_MS));
  }
  return false;
}

console.log(`Starting Azure Functions host in ${backendDir}...`);
backend = spawn('func', ['start'], {
  cwd: backendDir,
  stdio: ['ignore', 'pipe', 'pipe'],
});
prefix(backend.stdout, 'backend');
prefix(backend.stderr, 'backend');
backend.on('exit', (code) => {
  console.log(`[backend] exited with code ${code}`);
  shutdown(code ?? 0);
});

(async () => {
  console.log(`Waiting for backend on tcp://127.0.0.1:${BACKEND_PORT} ...`);
  const ready = await waitForBackend();
  if (shuttingDown) return;
  if (!ready) {
    console.warn(`[dev-start] backend did not bind port ${BACKEND_PORT} within ${READINESS_TIMEOUT_MS / 1000}s; launching frontend anyway.`);
  } else {
    console.log('[dev-start] backend is listening; launching frontend (browser will open automatically).');
  }

  const npm = process.platform === 'win32' ? 'npm.cmd' : 'npm';
  frontend = spawn(npm, ['start'], { cwd: frontendDir, stdio: 'inherit' });
  frontend.on('exit', (code) => {
    console.log(`[frontend] exited with code ${code}`);
    shutdown(code ?? 0);
  });
})();

process.on('SIGINT', () => shutdown(130));
process.on('SIGTERM', () => shutdown(143));
