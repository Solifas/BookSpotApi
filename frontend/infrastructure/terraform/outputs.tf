output "bucket_name" {
  description = "S3 bucket hosting the static site."
  value       = aws_s3_bucket.website.id
}

output "cloudfront_distribution_id" {
  description = "ID of the CloudFront distribution serving the site."
  value       = aws_cloudfront_distribution.cdn.id
}

output "cloudfront_domain_name" {
  description = "CloudFront domain for HTTPS delivery."
  value       = aws_cloudfront_distribution.cdn.domain_name
}

output "cloudfront_url" {
  description = "HTTPS URL via CloudFront."
  value       = "https://${aws_cloudfront_distribution.cdn.domain_name}"
}
