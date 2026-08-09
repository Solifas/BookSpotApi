# CloudWatch Log Group
resource "aws_cloudwatch_log_group" "lambda_logs" {
  name              = "/aws/lambda/${var.environment}-bookspot-api"
  retention_in_days = 7

  tags = {
    Name = "${var.environment}-bookspot-api-logs"
  }
}

# Lambda Function
resource "aws_lambda_function" "bookspot_api" {
  function_name = "${var.environment}-bookspot-api"
  role          = var.lambda_role_arn
  handler       = "BookSpot.API"
  runtime       = "dotnet8"

  filename         = var.lambda_package_path
  source_code_hash = fileexists(var.lambda_package_path) ? filebase64sha256(var.lambda_package_path) : null

  memory_size = var.memory_size
  timeout     = var.timeout

  environment {
    variables = {
      ASPNETCORE_ENVIRONMENT = var.environment
      Jwt__SecretKey         = var.jwt_secret_key
      Jwt__Issuer            = var.jwt_issuer
      Jwt__Audience          = var.jwt_audience
      CORS__AllowedOrigins   = join(",", var.cors_allowed_origins)
    }
  }

  depends_on = [
    aws_cloudwatch_log_group.lambda_logs
  ]

  tags = {
    Name = "${var.environment}-bookspot-api"
  }
}
