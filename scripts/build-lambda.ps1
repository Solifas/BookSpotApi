# Build Lambda deployment package for BookSpot API
# This script publishes the .NET project and creates a ZIP file for Lambda deployment

$ErrorActionPreference = "Stop"

Write-Host "Building BookSpot API for AWS Lambda..." -ForegroundColor Cyan

# Navigate to API project directory
Set-Location BookSpot.API

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean -c Release

# Publish for Lambda (linux-x64 runtime)
Write-Host "Publishing .NET project for linux-x64..." -ForegroundColor Yellow
dotnet publish -c Release -r linux-x64 --self-contained false -o ./publish

# Navigate to publish directory
Set-Location ./publish

# Create ZIP deployment package
Write-Host "Creating deployment package..." -ForegroundColor Yellow
Compress-Archive -Path * -DestinationPath ../../../bookspot-api.zip -Force

# Return to root directory
Set-Location ../../..

Write-Host "✅ Lambda deployment package created: bookspot-api.zip" -ForegroundColor Green

# Check package size
$packageSize = (Get-Item bookspot-api.zip).Length
$packageSizeMB = [math]::Round($packageSize / 1MB, 2)
Write-Host "📦 Package size: $packageSizeMB MB" -ForegroundColor Cyan

# Check if package is too large
$maxSizeMB = 50
if ($packageSizeMB -gt $maxSizeMB) {
    Write-Host "⚠️  Warning: Package size exceeds 50MB. Consider using S3 for deployment." -ForegroundColor Yellow
}

Write-Host "✅ Build complete!" -ForegroundColor Green
