# Profiles Table
resource "aws_dynamodb_table" "profiles" {
  name         = "profiles"
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "Id"

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
    Name        = "profiles"
    Table       = "profiles"
    Environment = var.environment
  }
}

# Businesses Table
resource "aws_dynamodb_table" "businesses" {
  name         = "businesses"
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "Id"

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
    Name        = "businesses"
    Table       = "businesses"
    Environment = var.environment
  }
}

# Services Table
resource "aws_dynamodb_table" "services" {
  name         = "services"
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "Id"

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
    Name        = "services"
    Table       = "services"
    Environment = var.environment
  }
}

# Bookings Table
resource "aws_dynamodb_table" "bookings" {
  name         = "bookings"
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "Id"

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
    Name        = "bookings"
    Table       = "bookings"
    Environment = var.environment
  }
}

# Business Hours Table
resource "aws_dynamodb_table" "business_hours" {
  name         = "business_hours"
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "Id"

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
    Name        = "business_hours"
    Table       = "business_hours"
    Environment = var.environment
  }
}

# Reviews Table
resource "aws_dynamodb_table" "reviews" {
  name         = "reviews"
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "Id"

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
    Name        = "reviews"
    Table       = "reviews"
    Environment = var.environment
  }
}
# Password Reset Tokens Table
resource "aws_dynamodb_table" "password_reset_tokens" {
  name         = "password_reset_tokens"
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "Token"

  attribute {
    name = "Token"
    type = "S"
  }

  attribute {
    name = "Email"
    type = "S"
  }

  global_secondary_index {
    name            = "Email-index"
    hash_key        = "Email"
    projection_type = "ALL"
  }

  # TTL for automatic cleanup of expired tokens
  ttl {
    attribute_name = "ExpiresAt"
    enabled        = true
  }

  server_side_encryption {
    enabled = true
  }

  point_in_time_recovery {
    enabled = true
  }

  tags = {
    Name        = "password_reset_tokens"
    Table       = "password_reset_tokens"
    Environment = var.environment
  }
}
