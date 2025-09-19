#!/bin/bash

echo "Creating DynamoDB tables for BookSpot..."

# Set AWS CLI to use LocalStack
export AWS_ACCESS_KEY_ID=test
export AWS_SECRET_ACCESS_KEY=test
export AWS_DEFAULT_REGION=us-east-1

# Check if awslocal is available, fallback to aws cli with endpoint
if command -v awslocal &> /dev/null; then
    echo "Using awslocal command"
    AWS_CMD="awslocal"
elif command -v aws &> /dev/null; then
    echo "Using aws cli with LocalStack endpoint"
    AWS_CMD="aws --endpoint-url=http://localhost:4566"
else
    echo "❌ Neither awslocal nor aws cli found. Please install one of them:"
    echo "For awslocal: pip install awscli-local"
    echo "For aws cli: https://docs.aws.amazon.com/cli/latest/userguide/getting-started-install.html"
    exit 1
fi

# Function to create table with error handling
create_table() {
    local table_name=$1
    echo "Creating table: $table_name"
    
    $AWS_CMD dynamodb create-table \
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
$AWS_CMD dynamodb list-tables --no-cli-pager

echo ""
echo "🎉 Setup complete! Your DynamoDB tables are ready for BookSpot."