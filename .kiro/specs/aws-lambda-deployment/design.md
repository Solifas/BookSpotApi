# Design Document - AWS Lambda Deployment for BookSpot API

## Overview

This design document describes the architecture and implementation approach for deploying the BookSpot ASP.NET Core API to AWS Lambda with DynamoDB backend using Terraform. The solution provides a serverless, scalable infrastructure that automatically handles traffic spikes while minimizing operational overhead.

## Architecture

### High-Level Architecture

```
┌─────────────┐
│   Client    │
│ (Web/Mobile)│
└──────┬──────┘
       │ HTTPS
       ▼
┌─────────────────┐
│  API Gateway    │
│  (REST API)     │
└────────┬────────┘
         │ Invoke
         ▼
┌─────────────────┐      ┌──────────────┐
│  Lambda         │─────▶│  CloudWatch  │
│  (.NET 8 API)   │      │  Logs        │
└────────┬────────┘      └──────────────┘
         │
         │ Read/Write
         ▼
┌─────────────────┐
│   DynamoDB      │
│   (6 Tables)    │
└─────────────────┘
```

### Component Responsibilities

**API Gateway:**
- Receives HTTPS requests from clients
- Routes requests to Lambda function
- Handles CORS preflight requests
- Provides public endpoint URL

**Lambda Function:**
- Runs BookSpot ASP.NET Core API
- Processes HTTP requests via API Gateway proxy integration
- Connects to DynamoDB for data operations
- Writes logs to CloudWatch

**DynamoDB:**
- Stores application data in 6 tables
- Provides fast, scalable NoSQL storage
- Handles concurrent read/write operations

**IAM Role:**
- Grants Lambda permissions to access DynamoDB
- Allows Lambda to write logs to CloudWatch
- Follows least-privilege principle

## Components and Interfaces

### 1. Terraform Project Structure

```
terraform/
├── main.tf                 # Root module configuration
├── variables.tf            # Input variables
├── outputs.tf              # Output values
├── providers.tf            # AWS provider configuration
├── modules/
│   ├── lambda/
│   │   ├── main.tf        # Lambda function resource
│   │   ├── variables.tf   # Lambda-specific variables
│   │   └── outputs.tf     # Lambda outputs (ARN, name)
│   ├── api-gateway/
│   │   ├── main.tf        # API Gateway resources
│   │   ├── variables.tf   # API Gateway variables
│   │   └── outputs.tf     # API endpoint URL
│   ├── dynamodb/
│   │   ├── main.tf        # DynamoDB table definitions
│   │   ├── variables.tf   # Table configuration variables
│   │   └── outputs.tf     # Table names and ARNs
│   └── iam/
│       ├── main.tf        # IAM roles and policies
│       ├── variables.tf   # IAM configuration
│       └── outputs.tf     # Role ARN
└── environments/
    ├── dev.tfvars         # Development environment variables
    └── prod.tfvars        # Production environment variables
```

### 2. Lambda Module Design

**Purpose:** Creates and configures the AWS Lambda function for the BookSpot API.

**Key Resources:**
- `aws_lambda_function` - Main Lambda function
- `aws_lambda_permission` - Allows API Gateway to invoke Lambda
- `aws_cloudwatch_log_group` - Log storage

**Configuration:**
```hcl
resource "aws_lambda_function" "bookspot_api" {
  function_name = "${var.environment}-bookspot-api"
  role          = var.lambda_role_arn
  handler       = "BookSpot.API"
  runtime       = "dotnet8"
  
  filename         = var.lambda_package_path
  source_code_hash = filebase64sha256(var.lambda_package_path)
  
  memory_size = 512
  timeout     = 30
  
  environment {
    variables = {
      ASPNETCORE_ENVIRONMENT = var.environment
      Jwt__SecretKey        = var.jwt_secret_key
      Jwt__Issuer           = var.jwt_issuer
      Jwt__Audience         = var.jwt_audience
    }
  }
}
```

**Inputs:**
- `environment` - Environment name (dev, prod)
- `lambda_package_path` - Path to deployment ZIP file
- `lambda_role_arn` - IAM role ARN
- `jwt_secret_key` - JWT signing key
- `jwt_issuer` - JWT issuer
- `jwt_audience` - JWT audience

**Outputs:**
- `function_arn` - Lambda function ARN
- `function_name` - Lambda function name
- `invoke_arn` - ARN for API Gateway integration

### 3. API Gateway Module Design

**Purpose:** Creates REST API with Lambda proxy integration.

**Key Resources:**
- `aws_api_gateway_rest_api` - REST API definition
- `aws_api_gateway_resource` - Proxy resource ({proxy+})
- `aws_api_gateway_method` - ANY method for all HTTP verbs
- `aws_api_gateway_integration` - Lambda proxy integration
- `aws_api_gateway_deployment` - API deployment
- `aws_api_gateway_stage` - Deployment stage

**Configuration:**
```hcl
resource "aws_api_gateway_rest_api" "bookspot" {
  name        = "${var.environment}-bookspot-api"
  description = "BookSpot API Gateway"
  
  endpoint_configuration {
    types = ["REGIONAL"]
  }
}

resource "aws_api_gateway_resource" "proxy" {
  rest_api_id = aws_api_gateway_rest_api.bookspot.id
  parent_id   = aws_api_gateway_rest_api.bookspot.root_resource_id
  path_part   = "{proxy+}"
}

resource "aws_api_gateway_method" "proxy" {
  rest_api_id   = aws_api_gateway_rest_api.bookspot.id
  resource_id   = aws_api_gateway_resource.proxy.id
  http_method   = "ANY"
  authorization = "NONE"
}

resource "aws_api_gateway_integration" "lambda" {
  rest_api_id = aws_api_gateway_rest_api.bookspot.id
  resource_id = aws_api_gateway_method.proxy.resource_id
  http_method = aws_api_gateway_method.proxy.http_method
  
  integration_http_method = "POST"
  type                    = "AWS_PROXY"
  uri                     = var.lambda_invoke_arn
}
```

**CORS Configuration:**
```hcl
resource "aws_api_gateway_method" "options" {
  rest_api_id   = aws_api_gateway_rest_api.bookspot.id
  resource_id   = aws_api_gateway_resource.proxy.id
  http_method   = "OPTIONS"
  authorization = "NONE"
}

resource "aws_api_gateway_integration" "options" {
  rest_api_id = aws_api_gateway_rest_api.bookspot.id
  resource_id = aws_api_gateway_resource.proxy.id
  http_method = aws_api_gateway_method.options.http_method
  type        = "MOCK"
  
  request_templates = {
    "application/json" = "{\"statusCode\": 200}"
  }
}

resource "aws_api_gateway_method_response" "options" {
  rest_api_id = aws_api_gateway_rest_api.bookspot.id
  resource_id = aws_api_gateway_resource.proxy.id
  http_method = aws_api_gateway_method.options.http_method
  status_code = "200"
  
  response_parameters = {
    "method.response.header.Access-Control-Allow-Headers" = true
    "method.response.header.Access-Control-Allow-Methods" = true
    "method.response.header.Access-Control-Allow-Origin"  = true
  }
}
```

**Inputs:**
- `environment` - Environment name
- `lambda_invoke_arn` - Lambda function invoke ARN
- `lambda_function_name` - Lambda function name
- `cors_allowed_origins` - List of allowed CORS origins

**Outputs:**
- `api_endpoint` - Full API Gateway endpoint URL
- `api_id` - API Gateway REST API ID
- `stage_name` - Deployment stage name

### 4. DynamoDB Module Design

**Purpose:** Creates all required DynamoDB tables for BookSpot application.

**Table Definitions:**

**Profiles Table:**
```hcl
resource "aws_dynamodb_table" "profiles" {
  name           = "${var.environment}-bookspot-profiles"
  billing_mode   = "PAY_PER_REQUEST"
  hash_key       = "Id"
  
  attribute {
    name = "Id"
    type = "S"
  }
  
  server_side_encryption {
    enabled = true
  }
  
  point_in_time_recovery {
    enabled = true
  }
  
  tags = {
    Environment = var.environment
    Application = "BookSpot"
    Table       = "profiles"
  }
}
```

**Businesses Table:**
```hcl
resource "aws_dynamodb_table" "businesses" {
  name           = "${var.environment}-bookspot-businesses"
  billing_mode   = "PAY_PER_REQUEST"
  hash_key       = "Id"
  
  attribute {
    name = "Id"
    type = "S"
  }
  
  attribute {
    name = "ProviderId"
    type = "S"
  }
  
  global_secondary_index {
    name            = "ProviderId-index"
    hash_key        = "ProviderId"
    projection_type = "ALL"
  }
  
  server_side_encryption {
    enabled = true
  }
  
  point_in_time_recovery {
    enabled = true
  }
  
  tags = {
    Environment = var.environment
    Application = "BookSpot"
    Table       = "businesses"
  }
}
```

**Services Table:**
```hcl
resource "aws_dynamodb_table" "services" {
  name           = "${var.environment}-bookspot-services"
  billing_mode   = "PAY_PER_REQUEST"
  hash_key       = "Id"
  
  attribute {
    name = "Id"
    type = "S"
  }
  
  attribute {
    name = "BusinessId"
    type = "S"
  }
  
  attribute {
    name = "ProviderId"
    type = "S"
  }
  
  global_secondary_index {
    name            = "BusinessId-index"
    hash_key        = "BusinessId"
    projection_type = "ALL"
  }
  
  global_secondary_index {
    name            = "ProviderId-index"
    hash_key        = "ProviderId"
    projection_type = "ALL"
  }
  
  server_side_encryption {
    enabled = true
  }
  
  point_in_time_recovery {
    enabled = true
  }
  
  tags = {
    Environment = var.environment
    Application = "BookSpot"
    Table       = "services"
  }
}
```

**Bookings Table:**
```hcl
resource "aws_dynamodb_table" "bookings" {
  name           = "${var.environment}-bookspot-bookings"
  billing_mode   = "PAY_PER_REQUEST"
  hash_key       = "Id"
  
  attribute {
    name = "Id"
    type = "S"
  }
  
  attribute {
    name = "ProviderId"
    type = "S"
  }
  
  attribute {
    name = "ClientId"
    type = "S"
  }
  
  global_secondary_index {
    name            = "ProviderId-index"
    hash_key        = "ProviderId"
    projection_type = "ALL"
  }
  
  global_secondary_index {
    name            = "ClientId-index"
    hash_key        = "ClientId"
    projection_type = "ALL"
  }
  
  server_side_encryption {
    enabled = true
  }
  
  point_in_time_recovery {
    enabled = true
  }
  
  tags = {
    Environment = var.environment
    Application = "BookSpot"
    Table       = "bookings"
  }
}
```

**Business Hours Table:**
```hcl
resource "aws_dynamodb_table" "business_hours" {
  name           = "${var.environment}-bookspot-business-hours"
  billing_mode   = "PAY_PER_REQUEST"
  hash_key       = "Id"
  
  attribute {
    name = "Id"
    type = "S"
  }
  
  attribute {
    name = "BusinessId"
    type = "S"
  }
  
  global_secondary_index {
    name            = "BusinessId-index"
    hash_key        = "BusinessId"
    projection_type = "ALL"
  }
  
  server_side_encryption {
    enabled = true
  }
  
  point_in_time_recovery {
    enabled = true
  }
  
  tags = {
    Environment = var.environment
    Application = "BookSpot"
    Table       = "business_hours"
  }
}
```

**Reviews Table:**
```hcl
resource "aws_dynamodb_table" "reviews" {
  name           = "${var.environment}-bookspot-reviews"
  billing_mode   = "PAY_PER_REQUEST"
  hash_key       = "Id"
  
  attribute {
    name = "Id"
    type = "S"
  }
  
  attribute {
    name = "BusinessId"
    type = "S"
  }
  
  global_secondary_index {
    name            = "BusinessId-index"
    hash_key        = "BusinessId"
    projection_type = "ALL"
  }
  
  server_side_encryption {
    enabled = true
  }
  
  point_in_time_recovery {
    enabled = true
  }
  
  tags = {
    Environment = var.environment
    Application = "BookSpot"
    Table       = "reviews"
  }
}
```

**Inputs:**
- `environment` - Environment name for table naming

**Outputs:**
- `table_names` - Map of all table names
- `table_arns` - Map of all table ARNs

### 5. IAM Module Design

**Purpose:** Creates IAM role and policies for Lambda execution.

**Lambda Execution Role:**
```hcl
resource "aws_iam_role" "lambda_execution" {
  name = "${var.environment}-bookspot-lambda-role"
  
  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action = "sts:AssumeRole"
      Effect = "Allow"
      Principal = {
        Service = "lambda.amazonaws.com"
      }
    }]
  })
}
```

**CloudWatch Logs Policy:**
```hcl
resource "aws_iam_role_policy" "lambda_logs" {
  name = "${var.environment}-bookspot-lambda-logs"
  role = aws_iam_role.lambda_execution.id
  
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect = "Allow"
      Action = [
        "logs:CreateLogGroup",
        "logs:CreateLogStream",
        "logs:PutLogEvents"
      ]
      Resource = "arn:aws:logs:*:*:*"
    }]
  })
}
```

**DynamoDB Access Policy:**
```hcl
resource "aws_iam_role_policy" "lambda_dynamodb" {
  name = "${var.environment}-bookspot-lambda-dynamodb"
  role = aws_iam_role.lambda_execution.id
  
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect = "Allow"
      Action = [
        "dynamodb:GetItem",
        "dynamodb:PutItem",
        "dynamodb:UpdateItem",
        "dynamodb:DeleteItem",
        "dynamodb:Query",
        "dynamodb:Scan"
      ]
      Resource = var.dynamodb_table_arns
    }]
  })
}
```

**Inputs:**
- `environment` - Environment name
- `dynamodb_table_arns` - List of DynamoDB table ARNs to grant access

**Outputs:**
- `lambda_role_arn` - IAM role ARN for Lambda
- `lambda_role_name` - IAM role name

## Data Models

### DynamoDB Table Schema

All tables use string-based partition keys (Id) for flexibility and follow the existing application schema:

- **profiles**: User profiles (clients and providers)
- **businesses**: Business entities owned by providers
- **services**: Services offered by businesses
- **bookings**: Appointment bookings between clients and providers
- **business_hours**: Operating hours for businesses
- **reviews**: Customer reviews for businesses

Global Secondary Indexes (GSI) are created for common query patterns:
- Services by BusinessId
- Services by ProviderId
- Bookings by ProviderId
- Bookings by ClientId
- Business Hours by BusinessId
- Reviews by BusinessId

## Error Handling

### Lambda Function Errors
- API Gateway returns 502 Bad Gateway for Lambda execution errors
- CloudWatch Logs capture detailed error information
- Application exceptions are handled by the ASP.NET Core global exception handler

### DynamoDB Errors
- Throttling: Handled by AWS SDK automatic retries
- Item not found: Application returns 404 Not Found
- Validation errors: Application returns 400 Bad Request

### API Gateway Errors
- 400: Bad Request (malformed input)
- 401: Unauthorized (missing/invalid JWT)
- 403: Forbidden (insufficient permissions)
- 404: Not Found (resource doesn't exist)
- 500: Internal Server Error (unhandled exceptions)

## Testing Strategy

### Infrastructure Testing
1. **Terraform Validation**
   - Run `terraform validate` to check syntax
   - Run `terraform plan` to preview changes
   - Review plan output before applying

2. **Module Testing**
   - Test each module independently
   - Verify outputs match expected values
   - Check resource creation in AWS Console

3. **Integration Testing**
   - Deploy to dev environment first
   - Test API Gateway endpoint with sample requests
   - Verify Lambda function executes successfully
   - Confirm DynamoDB read/write operations work

### Application Testing
1. **Local Testing**
   - Test API locally with LocalStack DynamoDB
   - Verify all endpoints work as expected
   - Run unit and integration tests

2. **Lambda Package Testing**
   - Build Lambda deployment package
   - Test package size (< 250MB unzipped)
   - Verify all dependencies are included

3. **End-to-End Testing**
   - Deploy to dev environment
   - Run API tests against deployed endpoint
   - Test authentication and authorization
   - Verify CORS configuration
   - Test all CRUD operations

## Deployment Process

### Prerequisites
1. AWS CLI configured with appropriate credentials
2. Terraform installed (version 1.5+)
3. .NET 8 SDK installed
4. AWS account with necessary permissions

### Build Lambda Package
```bash
# Navigate to API project
cd BookSpot.API

# Publish for Lambda
dotnet publish -c Release -r linux-x64 --self-contained false

# Create deployment package
cd bin/Release/net8.0/linux-x64/publish
zip -r ../../../../../bookspot-api.zip .
```

### Deploy Infrastructure
```bash
# Navigate to terraform directory
cd terraform

# Initialize Terraform
terraform init

# Plan deployment
terraform plan -var-file="environments/dev.tfvars" -out=tfplan

# Apply changes
terraform apply tfplan
```

### Update Application
```bash
# Rebuild Lambda package
./scripts/build-lambda.sh

# Update Lambda function
terraform apply -var-file="environments/dev.tfvars" -target=module.lambda
```

## Design Decisions

### Why Lambda over EC2/ECS?
- **Cost**: Pay only for actual compute time
- **Scalability**: Automatic scaling without configuration
- **Maintenance**: No server management required
- **Integration**: Native integration with API Gateway

### Why API Gateway Proxy Integration?
- **Simplicity**: Forward all requests to Lambda without transformation
- **Flexibility**: ASP.NET Core handles all routing
- **Compatibility**: Works with existing API structure

### Why On-Demand Billing for DynamoDB?
- **Cost-effective**: Pay only for actual read/write operations
- **No capacity planning**: Automatically scales with demand
- **Suitable for variable workloads**: Good for applications with unpredictable traffic

### Why Modular Terraform Structure?
- **Reusability**: Modules can be used across environments
- **Maintainability**: Easier to update specific components
- **Testing**: Can test modules independently
- **Organization**: Clear separation of concerns

## Security Considerations

1. **IAM Least Privilege**: Lambda role has minimal required permissions
2. **Encryption**: DynamoDB tables encrypted at rest
3. **HTTPS Only**: API Gateway enforces HTTPS
4. **JWT Authentication**: API validates JWT tokens
5. **CORS**: Configured to allow only specific origins
6. **Secrets Management**: Sensitive values passed as environment variables (future: AWS Secrets Manager)

## Performance Considerations

1. **Lambda Memory**: 512MB provides good balance of cost and performance
2. **Lambda Timeout**: 30 seconds allows for complex operations
3. **DynamoDB On-Demand**: Handles traffic spikes automatically
4. **API Gateway Caching**: Can be enabled for frequently accessed endpoints (future enhancement)
5. **Cold Start Mitigation**: Consider provisioned concurrency for production (future enhancement)
