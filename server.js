const { spawn, spawnSync } = require('child_process');
const path = require('path');
const fs = require('fs');

// Path to published .NET app
const publishDir = path.join(__dirname, 'publish');
const dllPath = path.join(publishDir, 'ThesisRepository.dll');

function ensurePublished(callback) {
  if (fs.existsSync(dllPath)) return callback();

  // If dotnet SDK is not available on the host, trying to spawn it will fail with ENOENT.
  // Check synchronously and fail with a clear message rather than an unhandled exception.
  const check = spawnSync('dotnet', ['--version'], { encoding: 'utf8' });
  if (check.error || check.status !== 0) {
    console.error('Published DLL not found at', dllPath);
    if (check.error && check.error.code === 'ENOENT') {
      console.error('dotnet CLI was not found in PATH (spawn dotnet ENOENT).');
    } else if (check.stderr) {
      console.error('dotnet --version failed:', check.stderr.toString());
    }
    console.error('Options: install the .NET SDK on the host, or publish the app before deployment so the published DLL is present.');
    process.exit(1);
  }

  console.log('Published DLL not found, running dotnet publish...');
  const pub = spawn('dotnet', ['publish', 'ThesisRepository.csproj', '-c', 'Release', '-o', 'publish'], { stdio: 'inherit' });

  pub.on('error', (err) => {
    console.error('Failed to run dotnet publish:', err && err.message ? err.message : err);
    process.exit(1);
  });

  pub.on('close', (code) => {
    if (code !== 0) {
      console.error('dotnet publish failed with code', code);
      process.exit(code);
    } else {
      callback();
    }
  });
}

ensurePublished(() => {
  if (!fs.existsSync(dllPath)) {
    console.error('Published DLL still not found at', dllPath);
    process.exit(1);
  }

  const port = process.env.PORT || '5000';
  // Ensure Kestrel binds to the provided port
  const env = Object.assign({}, process.env);
  env.ASPNETCORE_URLS = `http://0.0.0.0:${port}`;

  console.log(`Starting dotnet app: ${dllPath} on port ${port}`);
  const child = spawn('dotnet', [dllPath], { env, stdio: 'inherit' });

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
