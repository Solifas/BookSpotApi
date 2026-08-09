output "api_endpoint" {
  description = "API Gateway endpoint URL"
  value       = module.api_gateway.api_endpoint
}

output "lambda_function_arn" {
  description = "Lambda function ARN"
  value       = module.lambda.function_arn
}

output "lambda_function_name" {
  description = "Lambda function name"
  value       = module.lambda.function_name
}

output "dynamodb_table_names" {
  description = "Map of DynamoDB table names"
  value       = module.dynamodb.table_names
}

output "dynamodb_table_arns" {
  description = "Map of DynamoDB table ARNs"
  value       = module.dynamodb.table_arns
}

output "iam_role_arn" {
  description = "Lambda execution role ARN"
  value       = module.iam.lambda_role_arn
}

output "cors_allowed_origins" {
  description = "Configured CORS allowed origins"
  value       = var.cors_allowed_origins
}
