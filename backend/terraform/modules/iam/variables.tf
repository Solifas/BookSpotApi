variable "environment" {
  description = "Environment name (dev, staging, prod)"
  type        = string
  default     = "staging"
}

variable "dynamodb_table_arns" {
  description = "List of DynamoDB table ARNs to grant access"
  type        = list(string)
}
