# Seed test data for BookSpot API
Write-Host "🌱 Seeding test data..." -ForegroundColor Green

$baseUrl = "http://localhost:5000"

# Function to make API calls
function Invoke-ApiCall {
    param(
        [string]$Method,
        [string]$Endpoint,
        [object]$Body = $null
    )
    
    $headers = @{ "Content-Type" = "application/json" }
    $uri = "$baseUrl$Endpoint"
    
    try {
        if ($Body) {
            $jsonBody = $Body | ConvertTo-Json -Depth 10
            $response = Invoke-RestMethod -Uri $uri -Method $Method -Headers $headers -Body $jsonBody
        } else {
            $response = Invoke-RestMethod -Uri $uri -Method $Method -Headers $headers
        }
        return $response
    } catch {
        Write-Host "Error calling $Method $Endpoint : $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

# Check if API is running
Write-Host "Checking if API is running..." -ForegroundColor Yellow
try {
    $healthCheck = Invoke-RestMethod -Uri "$baseUrl/swagger/index.html" -Method Get -TimeoutSec 5
    Write-Host "✓ API is running" -ForegroundColor Green
} catch {
    Write-Host "✗ API is not running. Please start it with: .\scripts\run-api.ps1" -ForegroundColor Red
    exit 1
}

Write-Host "Creating test data..." -ForegroundColor Yellow

# Create test profiles
Write-Host "Creating profiles..." -ForegroundColor Cyan
$provider1 = Invoke-ApiCall -Method "POST" -Endpoint "/profiles" -Body @{
    email = "provider1@salon.com"
    userType = "provider"
}

$provider2 = Invoke-ApiCall -Method "POST" -Endpoint "/profiles" -Body @{
    email = "provider2@spa.com"
    userType = "provider"
}

$client1 = Invoke-ApiCall -Method "POST" -Endpoint "/profiles" -Body @{
    email = "client1@example.com"
    userType = "client"
}

if ($provider1 -and $provider2 -and $client1) {
    Write-Host "✓ Profiles created" -ForegroundColor Green
    
    # Create test businesses
    Write-Host "Creating businesses..." -ForegroundColor Cyan
    $business1 = Invoke-ApiCall -Method "POST" -Endpoint "/businesses" -Body @{
        providerId = $provider1.id
        businessName = "Elite Hair Salon"
        city = "New York"
    }
    
    $business2 = Invoke-ApiCall -Method "POST" -Endpoint "/businesses" -Body @{
        providerId = $provider2.id
        businessName = "Luxury Day Spa"
        city = "Los Angeles"
    }
    
    if ($business1 -and $business2) {
        Write-Host "✓ Businesses created" -ForegroundColor Green
        
        # Create test services
        Write-Host "Creating services..." -ForegroundColor Cyan
        $services = @(
            @{ businessId = $business1.id; name = "Haircut and Style"; price = 45.99; durationMinutes = 60 },
            @{ businessId = $business1.id; name = "Hair Color"; price = 89.99; durationMinutes = 120 },
            @{ businessId = $business1.id; name = "Beard Trim"; price = 25.00; durationMinutes = 30 },
            @{ businessId = $business2.id; name = "Deep Tissue Massage"; price = 120.00; durationMinutes = 90 },
            @{ businessId = $business2.id; name = "Facial Treatment"; price = 75.00; durationMinutes = 60 },
            @{ businessId = $business2.id; name = "Manicure and Pedicure"; price = 55.00; durationMinutes = 75 }
        )
        
        $createdServices = @()
        foreach ($service in $services) {
            $created = Invoke-ApiCall -Method "POST" -Endpoint "/services" -Body $service
            if ($created) {
                $createdServices += $created
                Write-Host "  ✓ Created: $($service.name)" -ForegroundColor Green
            }
        }
        
        Write-Host "✓ $($createdServices.Count) services created" -ForegroundColor Green
    }
}

Write-Host "`n🎉 Test data seeded successfully!" -ForegroundColor Green
Write-Host "`n📋 What was created:" -ForegroundColor Cyan
Write-Host "- 3 Profiles (2 providers, 1 client)" -ForegroundColor Gray
Write-Host "- 2 Businesses (Hair Salon, Day Spa)" -ForegroundColor Gray
Write-Host "- 6 Services (3 per business)" -ForegroundColor Gray

Write-Host "`n🧪 Test the GetAll endpoint:" -ForegroundColor Green
Write-Host "GET $baseUrl/services" -ForegroundColor Cyan
Write-Host "Or visit: $baseUrl/swagger" -ForegroundColor Cyan