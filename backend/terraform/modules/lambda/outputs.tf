output "function_arn" {
  description = "ARN of the Lambda function"
  value       = aws_lambda_function.bookspot_api.arn
}

output "function_name" {
  description = "Name of the Lambda function"
  value       = aws_lambda_function.bookspot_api.function_name
}

output "invoke_arn" {
  description = "Invoke ARN of the Lambda function for API Gateway"
  value       = aws_lambda_function.bookspot_api.invoke_arn
}
