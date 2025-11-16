# Implementation Plan

- [x] 1. Set up Terraform project structure


  - Create terraform directory with modules subdirectory
  - Create main.tf, variables.tf, outputs.tf, and providers.tf in root
  - Create environments directory with dev.tfvars and prod.tfvars
  - _Requirements: 1.1, 2.1, 3.1, 4.1_





- [ ] 2. Implement IAM module
- [x] 2.1 Create IAM module directory structure


  - Create terraform/modules/iam directory
  - Create main.tf, variables.tf, and outputs.tf files
  - _Requirements: 4.1, 4.2_


- [ ] 2.2 Implement Lambda execution role
  - Create aws_iam_role resource with Lambda assume role policy
  - Add trust relationship for lambda.amazonaws.com service

  - _Requirements: 4.1, 4.5_

- [ ] 2.3 Implement CloudWatch Logs policy
  - Create aws_iam_role_policy for CloudWatch Logs access
  - Grant permissions for CreateLogGroup, CreateLogStream, PutLogEvents

  - _Requirements: 4.2_

- [x] 2.4 Implement DynamoDB access policy




  - Create aws_iam_role_policy for DynamoDB operations
  - Grant permissions for GetItem, PutItem, UpdateItem, DeleteItem, Query, Scan
  - Use variable for table ARNs to scope permissions

  - _Requirements: 4.3, 4.4_

- [ ] 2.5 Define IAM module outputs
  - Output lambda_role_arn for use in Lambda module
  - Output lambda_role_name for reference
  - _Requirements: 4.1_

- [x] 3. Implement DynamoDB module

- [ ] 3.1 Create DynamoDB module directory structure
  - Create terraform/modules/dynamodb directory
  - Create main.tf, variables.tf, and outputs.tf files
  - _Requirements: 3.1_

- [ ] 3.2 Implement profiles table
  - Create aws_dynamodb_table resource for profiles

  - Set hash_key to "Id" (string type)
  - Configure on-demand billing mode
  - Enable server-side encryption
  - Enable point-in-time recovery
  - Add environment and application tags
  - _Requirements: 3.1, 3.2, 3.4, 3.5_


- [ ] 3.3 Implement businesses table
  - Create aws_dynamodb_table resource for businesses
  - Set hash_key to "Id" (string type)
  - Add ProviderId attribute for GSI
  - Create ProviderId-index global secondary index
  - Configure on-demand billing, encryption, and recovery
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_


- [ ] 3.4 Implement services table
  - Create aws_dynamodb_table resource for services
  - Set hash_key to "Id" (string type)
  - Add BusinessId and ProviderId attributes for GSIs
  - Create BusinessId-index and ProviderId-index global secondary indexes
  - Configure on-demand billing, encryption, and recovery

  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

- [ ] 3.5 Implement bookings table
  - Create aws_dynamodb_table resource for bookings
  - Set hash_key to "Id" (string type)
  - Add ProviderId and ClientId attributes for GSIs
  - Create ProviderId-index and ClientId-index global secondary indexes

  - Configure on-demand billing, encryption, and recovery
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_





- [ ] 3.6 Implement business_hours table
  - Create aws_dynamodb_table resource for business_hours
  - Set hash_key to "Id" (string type)

  - Add BusinessId attribute for GSI
  - Create BusinessId-index global secondary index
  - Configure on-demand billing, encryption, and recovery
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

- [ ] 3.7 Implement reviews table
  - Create aws_dynamodb_table resource for reviews
  - Set hash_key to "Id" (string type)

  - Add BusinessId attribute for GSI
  - Create BusinessId-index global secondary index
  - Configure on-demand billing, encryption, and recovery
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

- [ ] 3.8 Define DynamoDB module outputs
  - Output map of table names (profiles, businesses, services, bookings, business_hours, reviews)

  - Output map of table ARNs for IAM policy
  - _Requirements: 3.1_

- [ ] 4. Implement Lambda module
- [x] 4.1 Create Lambda module directory structure

  - Create terraform/modules/lambda directory
  - Create main.tf, variables.tf, and outputs.tf files
  - _Requirements: 1.1_

- [x] 4.2 Implement Lambda function resource

  - Create aws_lambda_function resource
  - Set function_name with environment prefix
  - Configure dotnet8 runtime
  - Set handler to "BookSpot.API"




  - Configure filename and source_code_hash from deployment package
  - Set memory_size to 512MB and timeout to 30 seconds
  - _Requirements: 1.1, 1.2, 1.3_


- [ ] 4.3 Configure Lambda environment variables
  - Add ASPNETCORE_ENVIRONMENT variable
  - Add Jwt__SecretKey variable
  - Add Jwt__Issuer variable

  - Add Jwt__Audience variable
  - Use Terraform variables for values
  - _Requirements: 1.4_

- [x] 4.4 Create CloudWatch Log Group

  - Create aws_cloudwatch_log_group resource
  - Set name to /aws/lambda/${function_name}
  - Configure retention period (7 days default)
  - _Requirements: 1.1_

- [x] 4.5 Create Lambda permission for API Gateway

  - Create aws_lambda_permission resource
  - Allow apigateway.amazonaws.com to invoke Lambda
  - Set source_arn to API Gateway execution ARN
  - _Requirements: 1.1_

- [ ] 4.6 Define Lambda module outputs
  - Output function_arn for reference

  - Output function_name for API Gateway integration
  - Output invoke_arn for API Gateway integration
  - _Requirements: 1.1_

- [ ] 5. Implement API Gateway module
- [x] 5.1 Create API Gateway module directory structure

  - Create terraform/modules/api-gateway directory
  - Create main.tf, variables.tf, and outputs.tf files
  - _Requirements: 2.1_


- [x] 5.2 Create REST API resource

  - Create aws_api_gateway_rest_api resource
  - Set name with environment prefix
  - Configure REGIONAL endpoint type
  - _Requirements: 2.1_


- [ ] 5.3 Create proxy resource and method
  - Create aws_api_gateway_resource for {proxy+} path
  - Create aws_api_gateway_method with ANY HTTP method
  - Set authorization to NONE (JWT handled by application)
  - _Requirements: 2.2_


- [ ] 5.4 Configure Lambda proxy integration
  - Create aws_api_gateway_integration resource
  - Set type to AWS_PROXY
  - Set integration_http_method to POST
  - Configure uri with Lambda invoke ARN

  - _Requirements: 2.2_

- [ ] 5.5 Implement CORS support
  - Create aws_api_gateway_method for OPTIONS

  - Create aws_api_gateway_integration with MOCK type
  - Create aws_api_gateway_method_response with CORS headers
  - Create aws_api_gateway_integration_response with CORS values
  - Configure Access-Control-Allow-Origin, Methods, and Headers
  - _Requirements: 2.3_


- [ ] 5.6 Create API deployment and stage
  - Create aws_api_gateway_deployment resource
  - Create aws_api_gateway_stage resource (e.g., "prod", "dev")
  - Configure stage with deployment reference

  - Add depends_on for all methods and integrations
  - _Requirements: 2.4_

- [x] 5.7 Define API Gateway module outputs

  - Output api_endpoint with full URL (https://{api_id}.execute-api.{region}.amazonaws.com/{stage})

  - Output api_id for reference
  - Output stage_name for reference
  - _Requirements: 2.5_

- [ ] 6. Implement root Terraform configuration
- [ ] 6.1 Configure AWS provider
  - Create providers.tf with AWS provider configuration

  - Set region from variable
  - Configure default tags for all resources
  - _Requirements: 1.1, 2.1, 3.1, 4.1_

- [ ] 6.2 Define root module variables
  - Create variables.tf with environment, region, aws_account_id


  - Add Lambda configuration variables (memory, timeout, package_path)


  - Add JWT configuration variables (secret_key, issuer, audience)
  - Add CORS configuration variable (allowed_origins)
  - _Requirements: 1.3, 1.4, 2.3_

- [x] 6.3 Instantiate IAM module


  - Call IAM module in main.tf
  - Pass environment variable
  - Pass DynamoDB table ARNs from DynamoDB module output
  - _Requirements: 4.1_


- [ ] 6.4 Instantiate DynamoDB module
  - Call DynamoDB module in main.tf
  - Pass environment variable
  - _Requirements: 3.1_

- [ ] 6.5 Instantiate Lambda module
  - Call Lambda module in main.tf
  - Pass environment, lambda_package_path, lambda_role_arn
  - Pass JWT configuration variables
  - _Requirements: 1.1, 1.4_

- [ ] 6.6 Instantiate API Gateway module
  - Call API Gateway module in main.tf
  - Pass environment, lambda_invoke_arn, lambda_function_name
  - Pass CORS allowed origins
  - _Requirements: 2.1, 2.3_

- [ ] 6.7 Define root module outputs
  - Output API endpoint URL from API Gateway module
  - Output Lambda function ARN
  - Output DynamoDB table names
  - _Requirements: 2.5_

- [ ] 7. Create environment configuration files
- [ ] 7.1 Create dev.tfvars
  - Set environment = "dev"
  - Set region (e.g., "eu-west-1")
  - Set JWT configuration for development
  - Set CORS allowed origins for development
  - Set Lambda package path
  - _Requirements: 1.3, 1.4, 2.3_

- [ ] 7.2 Create prod.tfvars
  - Set environment = "prod"
  - Set region (e.g., "eu-west-1")
  - Set JWT configuration for production
  - Set CORS allowed origins for production
  - Set Lambda package path
  - _Requirements: 1.3, 1.4, 2.3_

- [ ] 8. Create deployment scripts
- [ ] 8.1 Create Lambda build script
  - Create scripts/build-lambda.sh
  - Add commands to publish .NET project for linux-x64
  - Add commands to create ZIP deployment package
  - Make script executable
  - _Requirements: 1.2, 1.5_

- [ ] 8.2 Create Terraform deployment script
  - Create scripts/deploy.sh
  - Add commands for terraform init, plan, and apply
  - Accept environment parameter (dev/prod)
  - Include validation steps
  - _Requirements: 1.1_

- [ ] 9. Create documentation
- [ ]* 9.1 Create README.md
  - Document prerequisites (AWS CLI, Terraform, .NET SDK)
  - Document project structure
  - Document deployment process
  - Include example commands
  - Add troubleshooting section
  - _Requirements: 1.1, 2.1, 3.1, 4.1_

- [ ]* 9.2 Create .gitignore
  - Ignore Terraform state files (.tfstate, .tfstate.backup)
  - Ignore .terraform directory
  - Ignore Lambda deployment packages (*.zip)
  - Ignore environment variable files with secrets
  - _Requirements: 1.1_

- [ ] 10. Test and validate deployment
- [ ]* 10.1 Validate Terraform configuration
  - Run terraform validate
  - Run terraform fmt to format files
  - Fix any syntax errors
  - _Requirements: 1.1, 2.1, 3.1, 4.1_

- [ ] 10.2 Build Lambda deployment package
  - Run build-lambda.sh script
  - Verify ZIP file is created
  - Check package size (should be < 50MB)
  - _Requirements: 1.2, 1.5_

- [ ] 10.3 Deploy to dev environment
  - Run terraform init
  - Run terraform plan with dev.tfvars
  - Review plan output
  - Run terraform apply with dev.tfvars
  - _Requirements: 1.1, 2.1, 3.1, 4.1_

- [ ]* 10.4 Test deployed API
  - Get API endpoint URL from Terraform output
  - Test health check endpoint
  - Test authentication with JWT token
  - Test CRUD operations for each entity
  - Verify CORS headers in responses
  - _Requirements: 1.1, 2.2, 2.3_

- [ ]* 10.5 Verify DynamoDB tables
  - Check all 6 tables exist in AWS Console
  - Verify table configurations (billing mode, encryption, recovery)
  - Verify Global Secondary Indexes are created
  - Test read/write operations through API
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

- [ ]* 10.6 Verify IAM permissions
  - Check Lambda execution role exists
  - Verify CloudWatch Logs permissions
  - Verify DynamoDB permissions
  - Check CloudWatch Logs for Lambda execution logs
  - _Requirements: 4.1, 4.2, 4.3, 4.4_
