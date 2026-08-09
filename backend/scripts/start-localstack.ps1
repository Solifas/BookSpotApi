# Start LocalStack for BookSpot Development
Write-Host "Starting LocalStack for BookSpot..." -ForegroundColor Green

# Check if Docker is running
try {
    docker info | Out-Null
    Write-Host "✓ Docker is running" -ForegroundColor Green
}
catch {
    Write-Host "✗ Docker is not running. Please start Docker Desktop first." -ForegroundColor Red
    exit 1
}

# Start LocalStack
Write-Host "Starting LocalStack containers..." -ForegroundColor Yellow
docker-compose up -d

# Wait for LocalStack to be ready
Write-Host "Waiting for LocalStack to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Check if LocalStack is healthy
$maxAttempts = 30
$attempt = 0

while ($attempt -lt $maxAttempts) {
    try {
        $response = Invoke-RestMethod -Uri "http://localhost:4566/_localstack/health" -Method Get -TimeoutSec 5
        if ($response.services.dynamodb -eq "available") {
            Write-Host "✓ LocalStack is ready!" -ForegroundColor Green
            break
        }
    }
    catch {
        # Continue waiting
    }
    
    $attempt++
    Write-Host "Waiting for LocalStack... ($attempt/$maxAttempts)" -ForegroundColor Yellow
    Start-Sleep -Seconds 2
}

if ($attempt -eq $maxAttempts) {
    Write-Host "✗ LocalStack failed to start properly" -ForegroundColor Red
    exit 1
}

# List DynamoDB tables to verify setup
Write-Host "Verifying DynamoDB tables..." -ForegroundColor Yellow
try {
    $env:AWS_ACCESS_KEY_ID = "test"
    $env:AWS_SECRET_ACCESS_KEY = "test"
    $env:AWS_DEFAULT_REGION = "eu-west-1"
    
    $tables = aws dynamodb list-tables --endpoint-url http://localhost:4566 --output json | ConvertFrom-Json
    
    Write-Host "Available DynamoDB tables:" -ForegroundColor Green
    foreach ($table in $tables.TableNames) {
        Write-Host "  - $table" -ForegroundColor Cyan
    }
}
catch {
    Write-Host "Warning: Could not verify DynamoDB tables. Make sure AWS CLI is installed." -ForegroundColor Yellow
}

Write-Host "`nLocalStack is ready for development!" -ForegroundColor Green
Write-Host "DynamoDB endpoint: http://localhost:4566" -ForegroundColor Cyan
Write-Host "LocalStack dashboard: http://localhost:4566/_localstack/health" -ForegroundColor Cyan
Write-Host "`nTo stop LocalStack, run: docker-compose down" -ForegroundColor Gray