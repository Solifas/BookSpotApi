# BookSpot API - AWS Lambda Deployment

This directory contains Terraform configuration for deploying the BookSpot ASP.NET Core API to AWS Lambda with DynamoDB backend.

## Prerequisites

- **AWS CLI** configured with appropriate credentials
- **Terraform** 1.5.0 or later
- **.NET 8 SDK** installed
- **AWS Account** with necessary permissions

## Project Structure

```
terraform/
├── main.tf                 # Root module configuration
├── variables.tf            # Input variables
├── outputs.tf              # Output values
├── providers.tf            # AWS provider configuration
├── modules/
│   ├── lambda/            # Lambda function module
│   ├── api-gateway/       # API Gateway module
│   ├── dynamodb/          # DynamoDB tables module
│   └── iam/               # IAM roles and policies module
└── environments/
    ├── dev.tfvars         # Development environment config
    └── prod.tfvars        # Production environment config
```

## Quick Start

### 1. Configure Environment Variables

Edit the appropriate environment file:

**For Development:**
```bash
# Edit terraform/environments/dev.tfvars
environment      = "dev"
region           = "us-east-1"
aws_account_id   = "YOUR_AWS_ACCOUNT_ID"  # ⚠️ CHANGE THIS
jwt_secret_key   = "your-secure-secret-key"  # ⚠️ CHANGE THIS
```

**For Production:**
```bash
# Edit terraform/environments/prod.tfvars
environment      = "prod"
region           = "us-east-1"
aws_account_id   = "YOUR_AWS_ACCOUNT_ID"  # ⚠️ CHANGE THIS
jwt_secret_key   = "CHANGE-THIS-TO-SECURE-KEY"  # ⚠️ CHANGE THIS
```

### 2. Build Lambda Deployment Package

**On Windows (PowerShell):**
```powershell
.\scripts\build-lambda.ps1
```

**On Linux/Mac:**
```bash
bash scripts/build-lambda.sh
```

This will create `bookspot-api.zip` in the root directory.

### 3. Deploy Infrastructure

**On Windows (PowerShell):**
```powershell
.\scripts\deploy.ps1 -Environment dev
```

**On Linux/Mac:**
```bash
bash scripts/deploy.sh dev
```

### 4. Get API Endpoint

After deployment, get your API endpoint:

```bash
cd terraform
terraform output api_endpoint
```

## Manual Deployment Steps

If you prefer to run Terraform commands manually:

```bash
# Navigate to terraform directory
cd terraform

# Initialize Terraform
terraform init

# Validate configuration
terraform validate

# Plan deployment
terraform plan -var-file="environments/dev.tfvars" -out=tfplan

# Apply changes
terraform apply tfplan

# View outputs
terraform output
```

## Infrastructure Components

### DynamoDB Tables

The following tables are created with on-demand billing:

- **profiles** - User profiles (clients and providers)
- **businesses** - Business entities
- **services** - Services offered by businesses
- **bookings** - Appointment bookings
- **business_hours** - Business operating hours
- **reviews** - Customer reviews

All tables include:
- Server-side encryption
- Point-in-time recovery
- Global Secondary Indexes for efficient queries

### Lambda Function

- **Runtime**: .NET 8
- **Memory**: 512MB (configurable)
- **Timeout**: 30 seconds (configurable)
- **Environment Variables**: JWT configuration, environment name

### API Gateway

- **Type**: REST API with Lambda proxy integration
- **CORS**: Enabled with configurable origins
- **Stage**: Environment-specific (dev/prod)

### IAM Roles

- Lambda execution role with:
  - CloudWatch Logs permissions
  - DynamoDB read/write permissions (scoped to BookSpot tables)

## Configuration Variables

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `environment` | Environment name | - | Yes |
| `region` | AWS region | us-east-1 | No |
| `aws_account_id` | AWS account ID | - | Yes |
| `lambda_package_path` | Path to Lambda ZIP | ../bookspot-api.zip | No |
| `lambda_memory_size` | Lambda memory in MB | 512 | No |
| `lambda_timeout` | Lambda timeout in seconds | 30 | No |
| `jwt_secret_key` | JWT signing key | - | Yes |
| `jwt_issuer` | JWT issuer | BookSpot | No |
| `jwt_audience` | JWT audience | BookSpot | No |
| `cors_allowed_origins` | CORS allowed origins | ["*"] | No |

## Outputs

After deployment, Terraform provides:

- `api_endpoint` - Full API Gateway endpoint URL
- `lambda_function_arn` - Lambda function ARN
- `lambda_function_name` - Lambda function name
- `dynamodb_table_names` - Map of all DynamoDB table names
- `dynamodb_table_arns` - Map of all DynamoDB table ARNs
- `iam_role_arn` - Lambda execution role ARN

## Testing the Deployment

Test your API endpoint:

```bash
# Get the API endpoint
API_ENDPOINT=$(cd terraform && terraform output -raw api_endpoint)

# Test health check (if you have one)
curl $API_ENDPOINT/health

# Test with authentication
curl -X POST $API_ENDPOINT/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"password"}'
```

## Updating the Application

To deploy code changes:

1. Rebuild the Lambda package:
   ```powershell
   .\scripts\build-lambda.ps1
   ```

2. Update Lambda function:
   ```bash
   cd terraform
   terraform apply -var-file="environments/dev.tfvars" -target=module.lambda
   ```

## Destroying Infrastructure

To remove all resources:

```bash
cd terraform
terraform destroy -var-file="environments/dev.tfvars"
```

⚠️ **Warning**: This will delete all data in DynamoDB tables!

## Troubleshooting

### Lambda Package Too Large

If your package exceeds 50MB:
1. Review dependencies and remove unused packages
2. Consider using Lambda Layers for shared dependencies
3. Use S3 for deployment (requires Terraform configuration update)

### Permission Errors

Ensure your AWS credentials have permissions for:
- Lambda (create, update, invoke)
- API Gateway (create, deploy)
- DynamoDB (create tables, manage)
- IAM (create roles, attach policies)
- CloudWatch Logs (create log groups)

### API Gateway 502 Errors

Check Lambda logs:
```bash
aws logs tail /aws/lambda/dev-bookspot-api --follow
```

### DynamoDB Access Issues

Verify IAM role has correct permissions:
```bash
cd terraform
terraform output iam_role_arn
```

## Cost Estimation

Approximate monthly costs for low-traffic development:

- **Lambda**: ~$0-5 (first 1M requests free)
- **API Gateway**: ~$0-5 (first 1M requests free)
- **DynamoDB**: ~$0-5 (on-demand, low usage)
- **CloudWatch Logs**: ~$0-2 (7-day retention)

**Total**: ~$0-20/month for development

Production costs will vary based on traffic.

## Security Best Practices

1. **Never commit secrets** to version control
2. **Use AWS Secrets Manager** for production JWT keys
3. **Enable API Gateway API keys** for additional security
4. **Review IAM policies** regularly
5. **Enable CloudWatch alarms** for monitoring
6. **Use HTTPS only** (enforced by API Gateway)
7. **Implement rate limiting** in API Gateway

## Support

For issues or questions:
1. Check CloudWatch Logs for Lambda errors
2. Review Terraform plan output before applying
3. Verify AWS credentials and permissions
4. Check DynamoDB table configurations in AWS Console

## License

MIT License - See LICENSE file for details
