# Simple seed script for BookSpot API
Write-Host "🌱 Seeding test data..." -ForegroundColor Green

$baseUrl = "http://localhost:5000"
$headers = @{ "Content-Type" = "application/json" }

# Check if API is running
Write-Host "Checking if API is running..." -ForegroundColor Yellow
try {
    Invoke-RestMethod -Uri "$baseUrl/swagger/index.html" -Method Get -TimeoutSec 5 | Out-Null
    Write-Host "✓ API is running" -ForegroundColor Green
} catch {
    Write-Host "✗ API is not running. Please start it first." -ForegroundColor Red
    exit 1
}

# Create profiles
Write-Host "Creating profiles..." -ForegroundColor Cyan

$provider1Body = @{
    email = "provider1@salon.com"
    userType = "provider"
} | ConvertTo-Json

$provider1 = Invoke-RestMethod -Uri "$baseUrl/profiles" -Method POST -Headers $headers -Body $provider1Body
Write-Host "✓ Created provider profile: $($provider1.id)" -ForegroundColor Green

# Create business
Write-Host "Creating business..." -ForegroundColor Cyan

$businessBody = @{
    providerId = $provider1.id
    businessName = "Elite Hair Salon"
    city = "New York"
} | ConvertTo-Json

$business1 = Invoke-RestMethod -Uri "$baseUrl/businesses" -Method POST -Headers $headers -Body $businessBody
Write-Host "✓ Created business: $($business1.id)" -ForegroundColor Green

# Create services
Write-Host "Creating services..." -ForegroundColor Cyan

$service1Body = @{
    businessId = $business1.id
    name = "Haircut and Style"
    price = 45.99
    durationMinutes = 60
} | ConvertTo-Json

$service1 = Invoke-RestMethod -Uri "$baseUrl/services" -Method POST -Headers $headers -Body $service1Body
Write-Host "✓ Created service: $($service1.name)" -ForegroundColor Green

$service2Body = @{
    businessId = $business1.id
    name = "Hair Color"
    price = 89.99
    durationMinutes = 120
} | ConvertTo-Json

$service2 = Invoke-RestMethod -Uri "$baseUrl/services" -Method POST -Headers $headers -Body $service2Body
Write-Host "✓ Created service: $($service2.name)" -ForegroundColor Green

$service3Body = @{
    businessId = $business1.id
    name = "Beard Trim"
    price = 25.00
    durationMinutes = 30
} | ConvertTo-Json

$service3 = Invoke-RestMethod -Uri "$baseUrl/services" -Method POST -Headers $headers -Body $service3Body
Write-Host "✓ Created service: $($service3.name)" -ForegroundColor Green

Write-Host "`n🎉 Test data seeded successfully!" -ForegroundColor Green
Write-Host "Created 3 services for testing GetAll endpoint" -ForegroundColor Cyan
Write-Host "`nTest the GetAll endpoint: GET $baseUrl/services" -ForegroundColor Yellow