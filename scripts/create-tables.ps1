# Create DynamoDB Tables for BookSpot in LocalStack
Write-Host "🗄️  Creating DynamoDB tables in LocalStack..." -ForegroundColor Green

# Set AWS environment variables for LocalStack
$env:AWS_ACCESS_KEY_ID = "test"
$env:AWS_SECRET_ACCESS_KEY = "test"
$env:AWS_DEFAULT_REGION = "us-east-1"

$endpoint = "http://localhost:4566"

# Check if LocalStack is running
try {
    $response = Invoke-RestMethod -Uri "$endpoint/_localstack/health" -Method Get -TimeoutSec 5
    if ($response.services.dynamodb -ne "available") {
        Write-Host "✗ LocalStack DynamoDB not available. Please start LocalStack first." -ForegroundColor Red
        exit 1
    }
    Write-Host "✓ LocalStack is running" -ForegroundColor Green
} catch {
    Write-Host "✗ LocalStack not running. Please start it with: .\scripts\start-localstack.ps1" -ForegroundColor Red
    exit 1
}

# Function to create a table
function Create-DynamoTable {
    param(
        [string]$TableName,
        [string]$KeyName = "Id"
    )
    
    Write-Host "Creating table: $TableName" -ForegroundColor Yellow
    
    try {
        aws dynamodb create-table `
            --table-name $TableName `
            --attribute-definitions AttributeName=$KeyName,AttributeType=S `
            --key-schema AttributeName=$KeyName,KeyType=HASH `
            --billing-mode PAY_PER_REQUEST `
            --endpoint-url $endpoint `
            --output json | Out-Null
        
        Write-Host "✓ Table '$TableName' created successfully" -ForegroundColor Green
    } catch {
        if ($_.Exception.Message -like "*ResourceInUseException*") {
            Write-Host "ℹ️  Table '$TableName' already exists" -ForegroundColor Cyan
        } else {
            Write-Host "✗ Failed to create table '$TableName': $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

# Create all tables based on the entity definitions
$tables = @(
    "profiles",
    "businesses", 
    "services",
    "bookings",
    "business_hours",
    "reviews"
)

Write-Host "Creating tables..." -ForegroundColor Yellow
foreach ($table in $tables) {
    Create-DynamoTable -TableName $table
}

# Wait for tables to be active
Write-Host "`nWaiting for tables to be active..." -ForegroundColor Yellow
Start-Sleep -Seconds 3

# Verify tables were created
Write-Host "`nVerifying tables..." -ForegroundColor Yellow
try {
    $tableList = aws dynamodb list-tables --endpoint-url $endpoint --output json | ConvertFrom-Json
    
    Write-Host "✓ Available tables:" -ForegroundColor Green
    foreach ($table in $tableList.TableNames) {
        Write-Host "  - $table" -ForegroundColor Cyan
    }
    
    Write-Host "`n🎉 All tables created successfully!" -ForegroundColor Green
    Write-Host "You can now run the API and test endpoints." -ForegroundColor Cyan
    
} catch {
    Write-Host "✗ Failed to verify tables" -ForegroundColor Red
}

Write-Host "`n📝 Next steps:" -ForegroundColor Green
Write-Host "1. Run the API: .\scripts\run-api.ps1" -ForegroundColor Cyan
Write-Host "2. Test endpoints using BookSpot.http or Swagger UI" -ForegroundColor Cyan