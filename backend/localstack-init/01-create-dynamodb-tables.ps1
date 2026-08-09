# PowerShell script to create DynamoDB tables for BookSpot

Write-Host "Creating DynamoDB tables for BookSpot..." -ForegroundColor Green

# Set AWS CLI environment variables for LocalStack
$env:AWS_ACCESS_KEY_ID = "test"
$env:AWS_SECRET_ACCESS_KEY = "test"
$env:AWS_DEFAULT_REGION = "eu-west-1"
$env:AWS_ENDPOINT_URL = "http://localhost:4566"

# Function to create table with error handling
function Create-DynamoDBTable {
    param(
        [string]$TableName
    )
    
    Write-Host "Creating table: $TableName" -ForegroundColor Yellow
    
    try {
        aws dynamodb create-table `
            --endpoint-url http://localhost:4566 `
            --table-name $TableName `
            --attribute-definitions AttributeName=Id,AttributeType=S `
            --key-schema AttributeName=Id,KeyType=HASH `
            --provisioned-throughput ReadCapacityUnits=5,WriteCapacityUnits=5 `
            --no-cli-pager
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Table $TableName created successfully" -ForegroundColor Green
        } else {
            Write-Host "❌ Failed to create table $TableName" -ForegroundColor Red
        }
    }
    catch {
        Write-Host "❌ Error creating table $TableName : $_" -ForegroundColor Red
    }
    
    Write-Host ""
}

# Check if LocalStack is running
Write-Host "Checking if LocalStack is running..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:4566/_localstack/health" -UseBasicParsing -TimeoutSec 5
    Write-Host "✅ LocalStack is running" -ForegroundColor Green
}
catch {
    Write-Host "❌ LocalStack is not running. Please start LocalStack first:" -ForegroundColor Red
    Write-Host "docker run --rm -it -p 4566:4566 -p 4571:4571 localstack/localstack" -ForegroundColor Cyan
    exit 1
}

# Create all tables
$tables = @("profiles", "businesses", "services", "business_hours", "bookings", "reviews")

foreach ($table in $tables) {
    Create-DynamoDBTable -TableName $table
}

Write-Host "All DynamoDB tables creation completed!" -ForegroundColor Green

# List all tables to verify
Write-Host "Listing all tables:" -ForegroundColor Yellow
aws dynamodb list-tables --endpoint-url http://localhost:4566 --no-cli-pager

Write-Host ""
Write-Host "🎉 Setup complete! Your DynamoDB tables are ready for BookSpot." -ForegroundColor Green