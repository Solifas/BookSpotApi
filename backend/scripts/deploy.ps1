# Deploy BookSpot API infrastructure to AWS using Terraform
# Usage: .\scripts\deploy.ps1 -Environment dev|prod

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("dev", "prod")]
    [string]$Environment
)

$ErrorActionPreference = "Stop"

$tfvarsFile = "environments\$Environment.tfvars"

Write-Host "🚀 Deploying BookSpot API to $Environment environment..." -ForegroundColor Cyan

# Check if tfvars file exists
if (-not (Test-Path "terraform\$tfvarsFile")) {
    Write-Host "❌ Error: Configuration file not found: terraform\$tfvarsFile" -ForegroundColor Red
    exit 1
}

# Navigate to terraform directory
Set-Location terraform

# Initialize Terraform
Write-Host "📦 Initializing Terraform..." -ForegroundColor Yellow
terraform init

# Validate configuration
Write-Host "✅ Validating Terraform configuration..." -ForegroundColor Yellow
terraform validate

# Format Terraform files
Write-Host "🎨 Formatting Terraform files..." -ForegroundColor Yellow
terraform fmt -recursive

# Create plan
Write-Host "📋 Creating deployment plan..." -ForegroundColor Yellow
terraform plan -var-file="$tfvarsFile" -out=tfplan

# Show plan summary
Write-Host ""
Write-Host "📊 Deployment Plan Summary:" -ForegroundColor Cyan
Write-Host "Environment: $Environment" -ForegroundColor White
Write-Host "Configuration: $tfvarsFile" -ForegroundColor White
Write-Host ""

# Ask for confirmation
$confirm = Read-Host "Do you want to apply this plan? (yes/no)"

if ($confirm -ne "yes") {
    Write-Host "❌ Deployment cancelled" -ForegroundColor Red
    Remove-Item tfplan -ErrorAction SilentlyContinue
    exit 0
}

# Apply plan
Write-Host "🚀 Applying Terraform changes..." -ForegroundColor Yellow
terraform apply tfplan

# Clean up plan file
Remove-Item tfplan -ErrorAction SilentlyContinue

# Show outputs
Write-Host ""
Write-Host "✅ Deployment complete!" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Outputs:" -ForegroundColor Cyan
terraform output

Write-Host ""
Write-Host "🎉 BookSpot API deployed successfully to $Environment!" -ForegroundColor Green
