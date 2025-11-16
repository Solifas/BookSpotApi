# Simple script to seed services data directly to DynamoDB
Write-Host "Seeding services data..." -ForegroundColor Green

# Set environment variables
$env:AWS_ACCESS_KEY_ID = "test"
$env:AWS_SECRET_ACCESS_KEY = "test" 
$env:AWS_DEFAULT_REGION = "eu-west-1"

$endpoint = "http://localhost:4566"

Write-Host "Adding services to DynamoDB..." -ForegroundColor Yellow

# Service 1
$item1 = '{"Id":{"S":"service-001"},"BusinessId":{"S":"business-001"},"Name":{"S":"Haircut and Style"},"Price":{"N":"45.99"},"DurationMinutes":{"N":"60"},"CreatedAt":{"S":"2025-09-01T18:00:00.0000000Z"}}'
aws dynamodb put-item --table-name services --item $item1 --endpoint-url $endpoint

# Service 2  
$item2 = '{"Id":{"S":"service-002"},"BusinessId":{"S":"business-001"},"Name":{"S":"Hair Color"},"Price":{"N":"89.99"},"DurationMinutes":{"N":"120"},"CreatedAt":{"S":"2025-09-01T18:00:00.0000000Z"}}'
aws dynamodb put-item --table-name services --item $item2 --endpoint-url $endpoint

# Service 3
$item3 = '{"Id":{"S":"service-003"},"BusinessId":{"S":"business-001"},"Name":{"S":"Beard Trim"},"Price":{"N":"25.00"},"DurationMinutes":{"N":"30"},"CreatedAt":{"S":"2025-09-01T18:00:00.0000000Z"}}'
aws dynamodb put-item --table-name services --item $item3 --endpoint-url $endpoint

# Service 4
$item4 = '{"Id":{"S":"service-004"},"BusinessId":{"S":"business-002"},"Name":{"S":"Deep Tissue Massage"},"Price":{"N":"120.00"},"DurationMinutes":{"N":"90"},"CreatedAt":{"S":"2025-09-01T18:00:00.0000000Z"}}'
aws dynamodb put-item --table-name services --item $item4 --endpoint-url $endpoint

# Service 5
$item5 = '{"Id":{"S":"service-005"},"BusinessId":{"S":"business-002"},"Name":{"S":"Facial Treatment"},"Price":{"N":"75.00"},"DurationMinutes":{"N":"60"},"CreatedAt":{"S":"2025-09-01T18:00:00.0000000Z"}}'
aws dynamodb put-item --table-name services --item $item5 --endpoint-url $endpoint

# Service 6
$item6 = '{"Id":{"S":"service-006"},"BusinessId":{"S":"business-002"},"Name":{"S":"Manicure and Pedicure"},"Price":{"N":"55.00"},"DurationMinutes":{"N":"75"},"CreatedAt":{"S":"2025-09-01T18:00:00.0000000Z"}}'
aws dynamodb put-item --table-name services --item $item6 --endpoint-url $endpoint

Write-Host "Services added successfully!" -ForegroundColor Green
Write-Host "Test GetAll endpoint: GET http://localhost:5000/services" -ForegroundColor Cyan