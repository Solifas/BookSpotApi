variable "environment" {
  description = "Environment name (dev, staging, prod)"
  type        = string
}

variable "region" {
  description = "AWS region for resources"
  type        = string
  default     = "eu-west-1"
}

variable "aws_account_id" {
  description = "AWS account ID"
  type        = string
}

variable "lambda_package_path" {
  description = "Path to Lambda deployment package (ZIP file)"
  type        = string
  default     = "../bookspot-api.zip"
}

variable "lambda_memory_size" {
  description = "Lambda function memory size in MB"
  type        = number
  default     = 512
}

variable "lambda_timeout" {
  description = "Lambda function timeout in seconds"
  type        = number
  default     = 30
}

variable "jwt_secret_key" {
  description = "JWT secret key for token signing"
  type        = string
  sensitive   = true
}

variable "jwt_issuer" {
  description = "JWT token issuer"
  type        = string
  default     = "BookSpot"
}

variable "jwt_audience" {
  description = "JWT token audience"
  type        = string
  default     = "BookSpot"
}

variable "cors_allowed_origins" {
  description = "List of allowed CORS origins"
  type        = list(string)
  default     = ["*"]
}
