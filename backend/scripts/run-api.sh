#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
if BACKEND_DIR="$(cd "$SCRIPT_DIR/.." && pwd -W 2>/dev/null)"; then
  : # Git Bash: keep a native Windows path for Docker and dotnet.
else
  BACKEND_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
fi
export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"
export ASPNETCORE_URLS="${ASPNETCORE_URLS:-http://localhost:5000}"
export AWS__Region="${AWS__Region:-us-east-1}"
export AWS__ServiceURL="${AWS__ServiceURL:-http://localhost:4566}"
export AWS__AccessKey="${AWS__AccessKey:-test}"
export AWS__SecretKey="${AWS__SecretKey:-test}"

bash "$BACKEND_DIR/scripts/local-env.sh" init
exec dotnet run --no-launch-profile --project "$BACKEND_DIR/BookSpot.API/BookSpot.API.csproj"
