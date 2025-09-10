# Seed services data directly to DynamoDB for testing GetAll endpoint
Write-Host "🌱 Seeding services data..." -ForegroundColor Green

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

# Function to add service to DynamoDB
function Add-Service {
    param(
        [string]$Id,
        [string]$BusinessId,
        [string]$Name,
        [decimal]$Price,
        [int]$DurationMinutes
    )
    
    $createdAt = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")
    
    $item = @{
        "Id" = @{ "S" = $Id }
        "BusinessId" = @{ "S" = $BusinessId }
        "Name" = @{ "S" = $Name }
        "Price" = @{ "N" = $Price.ToString() }
        "DurationMinutes" = @{ "N" = $DurationMinutes.ToString() }
        "CreatedAt" = @{ "S" = $createdAt }
    } | ConvertTo-Json -Depth 10
    
    try {
        aws dynamodb put-item --table-name services --item $item --endpoint-url $endpoint | Out-Null
        Write-Host "✓ Added service: $Name" -ForegroundColor Green
    } catch {
        Write-Host "✗ Failed to add service: $Name - $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Create sample services
Write-Host "Adding sample services..." -ForegroundColor Yellow

Add-Service -Id "service-001" -BusinessId "business-001" -Name "Haircut and Style" -Price 45.99 -DurationMinutes 60
Add-Service -Id "service-002" -BusinessId "business-001" -Name "Hair Color" -Price 89.99 -DurationMinutes 120
Add-Service -Id "service-003" -BusinessId "business-001" -Name "Beard Trim" -Price 25.00 -DurationMinutes 30
Add-Service -Id "service-004" -BusinessId "business-002" -Name "Deep Tissue Massage" -Price 120.00 -DurationMinutes 90
Add-Service -Id "service-005" -BusinessId "business-002" -Name "Facial Treatment" -Price 75.00 -DurationMinutes 60
Add-Service -Id "service-006" -BusinessId "business-002" -Name "Manicure and Pedicure" -Price 55.00 -DurationMinutes 75

# Verify services were added
Write-Host "`nVerifying services..." -ForegroundColor Yellow
try {
    $scanResult = aws dynamodb scan --table-name services --endpoint-url $endpoint --output json | ConvertFrom-Json
    $serviceCount = $scanResult.Items.Count
    
    Write-Host "✓ Found $serviceCount services in the table:" -ForegroundColor Green
    foreach ($item in $scanResult.Items) {
        $name = $item.Name.S
        $price = $item.Price.N
        Write-Host "  - $name ($price)" -ForegroundColor Cyan
    }
} catch {
    Write-Host "✗ Failed to verify services" -ForegroundColor Red
}

Write-Host "`n🎉 Services seeded successfully!" -ForegroundColor Green
Write-Host "`n🧪 Test the GetAll endpoint:" -ForegroundColor Green
Write-Host "GET http://localhost:5000/services" -ForegroundColor Cyan
Write-Host "Or visit: http://localhost:5000/swagger" -ForegroundColor Cyan