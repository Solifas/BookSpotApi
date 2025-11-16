# Run BookSpot API with Swagger
Write-Host "🚀 Starting BookSpot API..." -ForegroundColor Green

# Check if LocalStack is running
Write-Host "Checking LocalStack..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "http://localhost:4566/_localstack/health" -Method Get -TimeoutSec 5
    if ($response.services.dynamodb -eq "available") {
        Write-Host "✓ LocalStack is running" -ForegroundColor Green
        
        # Check if tables exist, create them if not
        Write-Host "Checking DynamoDB tables..." -ForegroundColor Yellow
        $env:AWS_ACCESS_KEY_ID = "test"
        $env:AWS_SECRET_ACCESS_KEY = "test"
        $env:AWS_DEFAULT_REGION = "eu-west-1"
        
        try {
            $tables = aws dynamodb list-tables --endpoint-url http://localhost:4566 --output json | ConvertFrom-Json
            if ($tables.TableNames.Count -eq 0) {
                Write-Host "No tables found. Creating tables..." -ForegroundColor Yellow
                & ".\scripts\create-tables.ps1"
            } else {
                Write-Host "✓ DynamoDB tables found" -ForegroundColor Green
            }
        } catch {
            Write-Host "Creating DynamoDB tables..." -ForegroundColor Yellow
            & ".\scripts\create-tables.ps1"
        }
    } else {
        Write-Host "⚠️  LocalStack DynamoDB not available" -ForegroundColor Yellow
    }
} catch {
    Write-Host "⚠️  LocalStack not running. Start it with: .\scripts\start-localstack.ps1" -ForegroundColor Yellow
}

# Navigate to API project
Set-Location "BookSpot.API"

# Set environment variables for development
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://localhost:5000"

Write-Host "Starting API server..." -ForegroundColor Yellow
Write-Host "API will be available at: http://localhost:5000" -ForegroundColor Cyan
Write-Host "Swagger UI will be available at: http://localhost:5000/swagger" -ForegroundColor Cyan
Write-Host "Press Ctrl+C to stop the server" -ForegroundColor Gray
Write-Host ""

# Run the API
dotnet run