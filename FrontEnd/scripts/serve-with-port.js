#!/usr/bin/env node
const { spawn } = require('child_process');
const net = require('net');
const path = require('path');

const preferred = [8104, 8105, 8106];

function isPortFree(port) {
  return new Promise((resolve) => {
    const srv = net.createServer()
      .once('error', () => { resolve(false); })
      .once('listening', () => { srv.close(() => resolve(true)); })
      // bind to 0.0.0.0 to detect listeners on any interface (matches ng serve behaviour)
      .listen(port, '0.0.0.0');
  });
}

(async () => {
  let chosen = null;
  for (const p of preferred) {
    // eslint-disable-next-line no-await-in-loop
    if (await isPortFree(p)) { chosen = p; break; }
  }
  if (!chosen) {
    // fallback: ask OS for any free port by binding to 0
    // find one quickly by creating server on port 0
    chosen = await new Promise((resolve) => {
      const s = net.createServer().listen(0, '127.0.0.1', () => {
        const port = s.address().port;
        s.close(() => resolve(port));
      });
    });
  }

  const args = ['serve', 'nile-app', '--proxy-config', 'proxy.conf.json', '--port', String(chosen), '--host', 'localhost', '--open'];
  const opts = { cwd: path.resolve(__dirname, '..'), stdio: 'inherit' };
  const ng = process.platform === 'win32' ? 'ng.cmd' : 'ng';
  console.log(`Starting frontend on port ${chosen}...`);
  const p = spawn(ng, args, opts);
  p.on('exit', (code) => process.exit(code));
})();
