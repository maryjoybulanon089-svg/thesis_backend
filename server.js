const { spawn, spawnSync } = require('child_process');
const path = require('path');
const fs = require('fs');

// Path candidates to published .NET app
const publishCandidates = [
  path.join(__dirname, 'publish', 'ThesisRepository.dll'),
  path.join(__dirname, 'ThesisRepository.dll'),
  path.join(__dirname, '..', 'publish', 'ThesisRepository.dll'),
  path.join(__dirname, '..', 'ThesisRepository', 'publish', 'ThesisRepository.dll'),
  // honor environment override if provided
  process.env.PUBLISHED_DLL_PATH,
].filter(Boolean);

function findPublishedDll() {
  for (const p of publishCandidates) {
    try {
      if (fs.existsSync(p)) return p;
    } catch (e) {
      // ignore
    }
  }
  return null;
}

// Common locations for dotnet runtime on Linux
const commonDotnetPaths = [
  'dotnet', // rely on PATH first
  '/usr/bin/dotnet',
  '/usr/local/bin/dotnet',
  '/snap/bin/dotnet',
  '/opt/dotnet/dotnet',
  '/usr/share/dotnet/dotnet',
];

function findDotnetExecutable() {
  for (const cmd of commonDotnetPaths) {
    try {
      const r = spawnSync(cmd, ['--version'], { encoding: 'utf8' });
      if (!r.error && r.status === 0) return cmd;
    } catch (e) {
      // ignore and continue
    }
  }
  return null;
}

function ensurePublished(callback) {
  const foundDll = findPublishedDll();
  if (foundDll) return callback(foundDll);

  // Look for a dotnet executable in PATH or common locations
  const dotnetCmd = findDotnetExecutable();
  if (!dotnetCmd) {
    console.error('Published DLL not found at any of:', publishCandidates.join(', '));
    console.error('dotnet CLI was not found in PATH or common locations (spawn dotnet ENOENT).');
    console.error('Options:');
    console.error('- Deploy using your Dockerfile so the app is built in the image (recommended).');
    console.error('- Install the .NET SDK/runtime on the host so the process can publish/run.');
    console.error('- Pre-publish the app locally and include the publish/ folder in the deployed artifact.');
    process.exit(1);
  }

  console.log('Published DLL not found, running dotnet publish using', dotnetCmd);
  const pub = spawn(dotnetCmd, ['publish', 'ThesisRepository.csproj', '-c', 'Release', '-o', 'publish'], { stdio: 'inherit' });

  pub.on('error', (err) => {
    console.error('Failed to run dotnet publish:', err && err.message ? err.message : err);
    process.exit(1);
  });

  pub.on('close', (code) => {
    if (code !== 0) {
      console.error('dotnet publish failed with code', code);
      process.exit(code);
    } else {
      const newDll = findPublishedDll();
      if (!newDll) {
        console.error('dotnet publish completed but published DLL still not found. Looked at:', publishCandidates.join(', '));
        process.exit(1);
      }
      callback(newDll);
    }
  });
}

ensurePublished(() => {
  // allow callback to supply the discovered DLL path
  const usedDll = typeof dllPath === 'string' ? dllPath : null;
  const port = process.env.PORT || '5000';
  // Ensure Kestrel binds to the provided port
  const env = Object.assign({}, process.env);
  env.ASPNETCORE_URLS = `http://0.0.0.0:${port}`;

  const actualDll = findPublishedDll() || (usedDll && fs.existsSync(usedDll) ? usedDll : null);
  if (!actualDll) {
    console.error('Published DLL not available to start. Looked at:', publishCandidates.join(', '));
    process.exit(1);
  }

  const dotnetCmd = findDotnetExecutable();
  if (!dotnetCmd) {
    console.error('dotnet runtime not found to start the published DLL. Ensure the runtime is installed or use a runtime image.');
    process.exit(1);
  }

  console.log(`Starting dotnet app: ${actualDll} on port ${port} using ${dotnetCmd}`);
  const child = spawn(dotnetCmd, [actualDll], { env, stdio: 'inherit' });

  child.on('error', (err) => {
    console.error('Failed to start dotnet process:', err && err.message ? err.message : err);
    if (err && err.code === 'ENOENT') {
      console.error('dotnet CLI not found in PATH. Ensure the runtime image includes the .NET runtime or start the published DLL on a host with dotnet installed.');
    }
    process.exit(1);
  });

  const shutdown = () => {
    console.log('Shutting down dotnet process...');
    child.kill('SIGTERM');
    setTimeout(() => process.exit(0), 2000);
  };

  process.on('SIGINT', shutdown);
  process.on('SIGTERM', shutdown);

  child.on('exit', (code, signal) => {
    console.log('dotnet process exited', { code, signal });
    process.exit(code !== null ? code : 0);
  });
});
