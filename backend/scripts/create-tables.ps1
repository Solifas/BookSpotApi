# Idempotently provision BookSpot DynamoDB tables from PowerShell.
$ErrorActionPreference = "Stop"
$backend = Split-Path -Parent $PSScriptRoot
Push-Location $backend
try {
    bash ./scripts/local-env.sh init
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
    Pop-Location
}
