// Compatibility shim: some deployment platforms expect entry file at src/server.js
// This file simply delegates to the top-level server.js in the project root.
try {
  require('../server.js');
} catch (err) {
  // Provide a clearer error message to help debugging deployments
  console.error('Failed to start server.js from src/server.js:', err);
  process.exit(1);
}
