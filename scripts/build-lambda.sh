#!/bin/bash

# Build Lambda deployment package for BookSpot API
# This script publishes the .NET project and creates a ZIP file for Lambda deployment

set -e

echo "Building BookSpot API for AWS Lambda..."

# Navigate to API project directory
cd BookSpot.API

# Clean previous builds
echo "Cleaning previous builds..."
dotnet clean -c Release

# Publish for Lambda (linux-x64 runtime)
echo "Publishing .NET project for linux-x64..."
dotnet publish -c Release -r linux-x64 --self-contained false -o ./publish

# Navigate to publish directory
cd ./publish

# Create ZIP deployment package
echo "Creating deployment package..."
zip -r ../../../bookspot-api.zip .

# Return to root directory
cd ../../..

echo "✅ Lambda deployment package created: bookspot-api.zip"
echo "📦 Package size: $(du -h bookspot-api.zip | cut -f1)"

# Check if package is too large
PACKAGE_SIZE=$(stat -f%z bookspot-api.zip 2>/dev/null || stat -c%s bookspot-api.zip 2>/dev/null)
MAX_SIZE=$((50 * 1024 * 1024))  # 50MB

if [ "$PACKAGE_SIZE" -gt "$MAX_SIZE" ]; then
    echo "⚠️  Warning: Package size exceeds 50MB. Consider using S3 for deployment."
fi

echo "✅ Build complete!"
