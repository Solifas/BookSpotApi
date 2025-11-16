#!/bin/bash

# Deploy BookSpot API infrastructure to AWS using Terraform
# Usage: ./scripts/deploy.sh [dev|prod]

set -e

# Check if environment parameter is provided
if [ -z "$1" ]; then
    echo "❌ Error: Environment parameter required"
    echo "Usage: ./scripts/deploy.sh [dev|prod]"
    exit 1
fi

ENVIRONMENT=$1
TFVARS_FILE="environments/${ENVIRONMENT}.tfvars"

# Validate environment
if [ "$ENVIRONMENT" != "dev" ] && [ "$ENVIRONMENT" != "prod" ]; then
    echo "❌ Error: Invalid environment. Use 'dev' or 'prod'"
    exit 1
fi

# Check if tfvars file exists
if [ ! -f "../terraform/$TFVARS_FILE" ]; then
    echo "❌ Error: Configuration file not found: terraform/$TFVARS_FILE"
    exit 1
fi

echo "🚀 Deploying BookSpot API to $ENVIRONMENT environment..."

# Navigate to terraform directory
cd ../terraform

# Initialize Terraform
echo "📦 Initializing Terraform..."
terraform init

# Validate configuration
echo "✅ Validating Terraform configuration..."
terraform validate

# Format Terraform files
echo "🎨 Formatting Terraform files..."
terraform fmt -recursive

# Create plan
echo "📋 Creating deployment plan..."
terraform plan -var-file="$TFVARS_FILE" -out=tfplan

# Show plan summary
echo ""
echo "📊 Deployment Plan Summary:"
echo "Environment: $ENVIRONMENT"
echo "Configuration: $TFVARS_FILE"
echo ""

# Ask for confirmation
read -p "Do you want to apply this plan? (yes/no): " CONFIRM

if [ "$CONFIRM" != "yes" ]; then
    echo "❌ Deployment cancelled"
    rm -f tfplan
    exit 0
fi

# Apply plan
echo "🚀 Applying Terraform changes..."
terraform apply tfplan

# Clean up plan file
rm -f tfplan

# Show outputs
echo ""
echo "✅ Deployment complete!"
echo ""
echo "📋 Outputs:"
terraform output

echo ""
echo "🎉 BookSpot API deployed successfully to $ENVIRONMENT!"
