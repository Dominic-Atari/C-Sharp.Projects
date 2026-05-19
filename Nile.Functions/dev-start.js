#!/usr/bin/env node
// Launches both Azure Functions hosts and the Angular dev server together.
// - Clients host (auth + V1 user/post endpoints) on 7071
// - Functions host (schools/topics/courses/users/posts/health) on 7072
// The frontend's `npm start` already calls `ng serve --open`, so the browser
// auto-opens once the Angular dev server finishes its initial compile.
//
// Usage:
//   node dev-start.js              # launches the default frontend (App)
//   node dev-start.js App          # launches App/
//   node dev-start.js FrontEnd     # launches FrontEnd/

const fs = require('fs');
const { spawn } = require('child_process');
const net = require('net');
const path = require('path');

const repoRoot = path.resolve(__dirname, '..');
const clientsDir = path.resolve(__dirname, 'Clients/Nile.Client.Functions');
const functionsDir = path.resolve(__dirname, 'Functions');

const FRONTEND_CHOICES = ['App', 'FrontEnd'];
const requestedFrontend = process.argv[2] ?? 'App';
if (!FRONTEND_CHOICES.includes(requestedFrontend)) {
  console.error(`[dev-start] Unknown frontend "${requestedFrontend}". Choose one of: ${FRONTEND_CHOICES.join(', ')}`);
  process.exit(1);
}
const frontendDir = path.resolve(repoRoot, requestedFrontend);
if (!fs.existsSync(path.join(frontendDir, 'package.json'))) {
  console.error(`[dev-start] No package.json found at ${frontendDir} — expected ${requestedFrontend}/package.json.`);
  process.exit(1);
}
console.log(`[dev-start] Frontend selected: ${requestedFrontend} (${frontendDir})`);

const CLIENTS_PORT = 7071;
const FUNCTIONS_PORT = 7072;
const READINESS_TIMEOUT_MS = 90_000;
const POLL_INTERVAL_MS = 500;

const children = [];
let frontend = null;
let shuttingDown = false;

function shutdown(code) {
  if (shuttingDown) return;
  shuttingDown = true;
  if (frontend && frontend.exitCode === null) frontend.kill();
  for (const c of children) {
    if (c.proc && c.proc.exitCode === null) c.proc.kill();
  }
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

async function waitForPort(port, label) {
  const deadline = Date.now() + READINESS_TIMEOUT_MS;
  while (Date.now() < deadline) {
    if (await probePort(port)) return true;
    await new Promise((r) => setTimeout(r, POLL_INTERVAL_MS));
  }
  console.warn(`[dev-start] ${label} did not bind port ${port} within ${READINESS_TIMEOUT_MS / 1000}s.`);
  return false;
}

function startHost({ label, cwd, port }) {
  console.log(`Starting ${label} Functions host in ${cwd} (port ${port})...`);
  // --cors "*" emits Access-Control-Allow-Origin for every dev origin.
  // --port pins the host so two hosts run side by side.
  // (We let func do its own incremental build; --no-build skips the worker
  // metadata-generation step and yields "No job functions found".)
  const proc = spawn('func', ['start', '--cors', '*', '--port', String(port)], {
    cwd,
    stdio: ['ignore', 'pipe', 'pipe'],
  });
  prefix(proc.stdout, label);
  prefix(proc.stderr, label);
  proc.on('exit', (code) => {
    console.log(`[${label}] exited with code ${code}`);
    shutdown(code ?? 0);
  });
  children.push({ label, proc, port });
}

console.log('[dev-start] Prerequisite: SQL Server (docker-compose up -d) and DbUp migrations (cd Data/Nile.DbUp && dotnet run) must be applied.');

// Serialize host startup. Functions/ has a ProjectReference to Clients/, so
// running both `func start` processes in parallel races on the shared build
// outputs and one host fails with file-lock errors. Start clients, wait for
// it to bind, then start functions.
startHost({ label: 'clients', cwd: clientsDir, port: CLIENTS_PORT });

(async () => {
  console.log(`Waiting for clients host on tcp://127.0.0.1:${CLIENTS_PORT} ...`);
  const clientsReady = await waitForPort(CLIENTS_PORT, 'clients host');
  if (shuttingDown) return;
  if (!clientsReady) {
    console.warn('[dev-start] clients host never bound its port; aborting.');
    shutdown(1);
    return;
  }

  startHost({ label: 'functions', cwd: functionsDir, port: FUNCTIONS_PORT });
  console.log(`Waiting for functions host on tcp://127.0.0.1:${FUNCTIONS_PORT} ...`);
  const functionsReady = await waitForPort(FUNCTIONS_PORT, 'functions host');
  if (shuttingDown) return;
  if (functionsReady) {
    console.log('[dev-start] both backends are listening; launching frontend (browser will open automatically).');
  } else {
    console.warn('[dev-start] functions host never bound its port; launching frontend anyway.');
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
