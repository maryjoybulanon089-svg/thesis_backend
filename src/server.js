const { spawnSync, spawn } = require('child_process');
const http = require('http');
const fs = require('fs');
const path = require('path');

const port = process.env.PORT ? parseInt(process.env.PORT, 10) : 5000;

function startFallbackServer() {
  const server = http.createServer((req, res) => {
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({
      status: 'ok',
      message: 'This repository is a Docker-based .NET app. Configure Render to use the Dockerfile (env: docker) or deploy a published DLL. See README.'
    }));
  });

  server.listen(port, () => {
    console.log(`Fallback Node server listening on port ${port}.`);
    console.log('The real backend should be run from the Docker image or by running the published DLL with dotnet.');
  });
}

function findPublishedDll() {
  const candidates = [
    path.join(process.cwd(), 'publish', 'ThesisRepository.dll'),
    path.join(process.cwd(), 'ThesisRepository.dll'),
    process.env.PUBLISHED_DLL_PATH
  ].filter(Boolean);

  for (const p of candidates) {
    try {
      if (fs.existsSync(p)) return p;
    } catch (e) {
      // ignore
    }
  }
  return null;
}

// If dotnet exists and a published DLL is present, spawn it and keep process alive.
try {
  const check = spawnSync('dotnet', ['--version'], { encoding: 'utf8' });
  const dll = findPublishedDll();
  if (!check.error && check.status === 0 && dll) {
    console.log('dotnet CLI found and published DLL located at', dll);
    const env = Object.assign({}, process.env);
    if (env.PORT) env.ASPNETCORE_URLS = `http://0.0.0.0:${env.PORT}`;

    const child = spawn('dotnet', [dll], { env, stdio: 'inherit' });

    child.on('error', (err) => {
      console.error('Failed to start dotnet child process:', err);
      console.error('Falling back to the Node informational server.');
      startFallbackServer();
    });

    child.on('exit', (code, signal) => {
      console.log('dotnet process exited', { code, signal });
      // if the child exits, fall back to the informational Node server so the service stays up
      startFallbackServer();
    });
    // keep the Node process alive while the child runs
  } else {
    if (check.error && check.error.code === 'ENOENT') {
      console.warn('dotnet CLI not found (spawn dotnet ENOENT). Starting informational Node server.');
    } else if (!dll) {
      console.warn('Published DLL not found. Starting informational Node server.');
    }
    startFallbackServer();
  }
} catch (ex) {
  console.error('Unexpected error in src/server.js', ex);
  startFallbackServer();
}
