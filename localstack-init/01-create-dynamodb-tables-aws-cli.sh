#!/bin/bash

echo "Creating DynamoDB tables for BookSpot using AWS CLI..."

# Set AWS CLI to use LocalStack
export AWS_ACCESS_KEY_ID=test
export AWS_SECRET_ACCESS_KEY=test
export AWS_DEFAULT_REGION=us-east-1
export AWS_ENDPOINT_URL=http://localhost:4566

# Function to create table with error handling
create_table() {
    local table_name=$1
    echo "Creating table: $table_name"
    
    aws dynamodb create-table \
        --endpoint-url http://localhost:4566 \
        --table-name $table_name \
        --attribute-definitions \
            AttributeName=Id,AttributeType=S \
        --key-schema \
            AttributeName=Id,KeyType=HASH \
        --provisioned-throughput \
            ReadCapacityUnits=5,WriteCapacityUnits=5 \
        --no-cli-pager
    
    if [ $? -eq 0 ]; then
        echo "✅ Table $table_name created successfully"
    else
        echo "❌ Failed to create table $table_name"
    fi
    echo ""
}

# Check if LocalStack is running
echo "Checking if LocalStack is running..."
if curl -s http://localhost:4566/_localstack/health > /dev/null; then
    echo "✅ LocalStack is running"
else
    echo "❌ LocalStack is not running. Please start LocalStack first:"
    echo "docker run --rm -it -p 4566:4566 -p 4571:4571 localstack/localstack"
    exit 1
fi

# Create all tables
create_table "profiles"
create_table "businesses"
create_table "services"
create_table "business_hours"
create_table "bookings"
create_table "reviews"

echo "All DynamoDB tables creation completed!"

# List all tables to verify
echo "Listing all tables:"
aws dynamodb list-tables \
    --endpoint-url http://localhost:4566 \
    --no-cli-pager

echo ""
echo "🎉 Setup complete! Your DynamoDB tables are ready for BookSpot."