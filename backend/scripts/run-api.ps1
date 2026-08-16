# Run the BookSpot API at http://localhost:5000 using local-only configuration.
$ErrorActionPreference = "Stop"
$backend = Split-Path -Parent $PSScriptRoot
Push-Location $backend
try {
    bash ./scripts/local-env.sh init
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $env:ASPNETCORE_URLS = "http://localhost:5000"
    $env:AWS__Region = "us-east-1"
    $env:AWS__ServiceURL = "http://localhost:4566"
    $env:AWS__AccessKey = "test"
    $env:AWS__SecretKey = "test"
    dotnet run --no-launch-profile --project ./BookSpot.API/BookSpot.API.csproj
}
finally {
    Pop-Location
}
