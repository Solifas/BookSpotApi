# Stop LocalStack for BookSpot Development
Write-Host "Stopping LocalStack..." -ForegroundColor Yellow

# Stop and remove containers
docker-compose down

Write-Host "✓ LocalStack stopped successfully!" -ForegroundColor Green