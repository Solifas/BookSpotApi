output "table_names" {
  description = "Map of DynamoDB table names"
  value = {
    profiles              = aws_dynamodb_table.profiles.name
    businesses            = aws_dynamodb_table.businesses.name
    services              = aws_dynamodb_table.services.name
    bookings              = aws_dynamodb_table.bookings.name
    business_hours        = aws_dynamodb_table.business_hours.name
    reviews               = aws_dynamodb_table.reviews.name
    password_reset_tokens = aws_dynamodb_table.password_reset_tokens.name
  }
}

output "table_arns" {
  description = "Map of DynamoDB table ARNs"
  value = {
    profiles              = aws_dynamodb_table.profiles.arn
    businesses            = aws_dynamodb_table.businesses.arn
    services              = aws_dynamodb_table.services.arn
    bookings              = aws_dynamodb_table.bookings.arn
    business_hours        = aws_dynamodb_table.business_hours.arn
    reviews               = aws_dynamodb_table.reviews.arn
    password_reset_tokens = aws_dynamodb_table.password_reset_tokens.arn
  }
}
