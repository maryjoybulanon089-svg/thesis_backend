#!/bin/sh
# If Render supplies PORT, set ASPNETCORE_URLS so Kestrel binds to it
if [ ! -z "$PORT" ]; then
  export ASPNETCORE_URLS="http://0.0.0.0:$PORT"
fi

exec dotnet ThesisRepository.dll
