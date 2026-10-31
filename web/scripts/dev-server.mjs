// Starts the Angular dev server, or reuses one that is already running on the port.
// The API's SPA proxy can call "pnpm start" twice when Angular comes up between two of its
// readiness checks; without this, the second copy stops at a "port already in use" prompt.
import { spawn } from 'node:child_process';
import { connect } from 'node:net';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const port = 4200;
const webRoot = join(dirname(fileURLToPath(import.meta.url)), '..');

if (await isListening(port)) {
  console.log(`The Angular dev server is already running at http://localhost:${port}.`);
  console.log('Nothing to start. This window closes in a few seconds.');
  setTimeout(() => process.exit(0), 5000);
} else {
  const ng = join(webRoot, 'node_modules', '@angular', 'cli', 'bin', 'ng.js');
  const child = spawn(process.execPath, [ng, 'serve', '--port', String(port), ...process.argv.slice(2)], {
    cwd: webRoot,
    stdio: 'inherit',
  });
  child.on('exit', (code) => process.exit(code ?? 0));
}

/** True when something accepts connections on the port, over IPv6 or IPv4. */
async function isListening(port) {
  const results = await Promise.all(['::1', '127.0.0.1'].map((host) => canConnect(host, port)));
  return results.some(Boolean);
}

function canConnect(host, port) {
  return new Promise((resolve) => {
    const socket = connect({ host, port, timeout: 1000 });
    const done = (result) => {
      socket.destroy();
      resolve(result);
    };
    socket.once('connect', () => done(true));
    socket.once('timeout', () => done(false));
    socket.once('error', () => done(false));
  });
}
