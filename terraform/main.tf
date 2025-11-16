# DynamoDB Tables Module
module "dynamodb" {
  source = "./modules/dynamodb"

  environment = var.environment
}

# IAM Roles and Policies Module
module "iam" {
  source = "./modules/iam"

  environment         = var.environment
  dynamodb_table_arns = values(module.dynamodb.table_arns)
}

# Lambda Function Module
module "lambda" {
  source = "./modules/lambda"

  environment          = var.environment
  lambda_package_path  = var.lambda_package_path
  lambda_role_arn      = module.iam.lambda_role_arn
  memory_size          = var.lambda_memory_size
  timeout              = var.lambda_timeout
  jwt_secret_key       = var.jwt_secret_key
  jwt_issuer           = var.jwt_issuer
  jwt_audience         = var.jwt_audience
  cors_allowed_origins = var.cors_allowed_origins
}

# API Gateway Module
module "api_gateway" {
  source = "./modules/api-gateway"

  environment          = var.environment
  lambda_invoke_arn    = module.lambda.invoke_arn
  lambda_function_name = module.lambda.function_name
  cors_allowed_origins = var.cors_allowed_origins
}
