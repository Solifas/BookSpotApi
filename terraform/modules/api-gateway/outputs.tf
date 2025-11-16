output "api_endpoint" {
  description = "Full API Gateway endpoint URL"
  value       = "https://${aws_api_gateway_rest_api.bookspot.id}.execute-api.${data.aws_region.current.name}.amazonaws.com/${aws_api_gateway_stage.bookspot.stage_name}"
}

output "api_id" {
  description = "API Gateway REST API ID"
  value       = aws_api_gateway_rest_api.bookspot.id
}

output "stage_name" {
  description = "API Gateway deployment stage name"
  value       = aws_api_gateway_stage.bookspot.stage_name
}
