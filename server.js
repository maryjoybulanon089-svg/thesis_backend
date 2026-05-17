const { spawn } = require('child_process');
const path = require('path');
const fs = require('fs');

// Path to published .NET app
const publishDir = path.join(__dirname, 'publish');
const dllPath = path.join(publishDir, 'ThesisRepository.dll');

function ensurePublished(callback) {
  if (fs.existsSync(dllPath)) return callback();

  console.log('Published DLL not found, running dotnet publish...');
  const pub = spawn('dotnet', ['publish', 'ThesisRepository.csproj', '-c', 'Release', '-o', 'publish'], { stdio: 'inherit' });
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
