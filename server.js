// Simple shim for platforms that expect /src/server.js or /server.js at repo root.
try {
  require('./src/server.js');
} catch (err) {
  console.error('Failed to load ./src/server.js:', err && err.message ? err.message : err);
  process.exit(1);
}
