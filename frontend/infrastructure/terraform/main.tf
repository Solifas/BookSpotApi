terraform {
  required_version = ">= 1.9.0, < 2.0.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.65"
    }
  }
}

provider "aws" {
  region = var.aws_region
}

locals {
  bucket_name = coalesce(var.bucket_name, "${var.project_slug}-${var.environment}")
  dist_path   = "${path.module}/../../dist"
  files       = fileset(local.dist_path, "**")

  mime_types = {
    css         = "text/css"
    html        = "text/html"
    ico         = "image/x-icon"
    js          = "text/javascript"
    json        = "application/json"
    map         = "application/json"
    png         = "image/png"
    svg         = "image/svg+xml"
    txt         = "text/plain"
    webmanifest = "application/manifest+json"
    webp        = "image/webp"
    woff2       = "font/woff2"
  }
}

resource "aws_s3_bucket" "website" {
  bucket        = local.bucket_name
  force_destroy = true

  tags = {
    Project     = var.project_slug
    Environment = var.environment
  }
}

resource "aws_s3_bucket_public_access_block" "website" {
  bucket                  = aws_s3_bucket.website.id
  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_versioning" "website" {
  bucket = aws_s3_bucket.website.id

  versioning_configuration {
    status = "Enabled"
  }
}

resource "aws_s3_bucket_server_side_encryption_configuration" "website" {
  bucket = aws_s3_bucket.website.id

  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

resource "aws_cloudfront_origin_access_control" "oac" {
  name                              = "${local.bucket_name}-oac"
  description                       = "OAC for ${local.bucket_name} static site"
  origin_access_control_origin_type = "s3"
  signing_behavior                  = "always"
  signing_protocol                  = "sigv4"
}

resource "aws_s3_object" "assets" {
  for_each = { for file in local.files : file => file }

  bucket = aws_s3_bucket.website.id
  key    = each.key
  source = "${local.dist_path}/${each.value}"
  etag   = filemd5("${local.dist_path}/${each.value}")

  content_type = lookup(
    local.mime_types,
    regex("([^.]+)$", each.value)[0],
    "application/octet-stream"
  )

  cache_control = contains(var.cacheable_paths, each.key) ? "public, max-age=604800, immutable" : "no-cache"
}

resource "aws_cloudfront_cache_policy" "no_cache" {
  name        = "${local.bucket_name}-no-cache"
  comment     = "Disable caching for HTML/SPA shell"
  default_ttl = 0
  max_ttl     = 0
  min_ttl     = 0

  parameters_in_cache_key_and_forwarded_to_origin {
    cookies_config {
      cookie_behavior = "none"
    }

    headers_config {
      header_behavior = "none"
    }

    query_strings_config {
      query_string_behavior = "none"
    }
  }
}

resource "aws_cloudfront_cache_policy" "long_cache" {
  name        = "${local.bucket_name}-long-cache"
  comment     = "Long cache for versioned assets"
  default_ttl = 31536000
  max_ttl     = 31536000
  min_ttl     = 86400

  parameters_in_cache_key_and_forwarded_to_origin {
    cookies_config {
      cookie_behavior = "none"
    }

    headers_config {
      header_behavior = "none"
    }

    query_strings_config {
      query_string_behavior = "none"
    }
  }
}

resource "aws_cloudfront_distribution" "cdn" {
  enabled             = true
  comment             = "Static site for ${local.bucket_name}"
  default_root_object = var.index_document

  origin {
    domain_name = aws_s3_bucket.website.bucket_regional_domain_name
    origin_id   = "s3-origin"

    s3_origin_config {
      origin_access_identity = ""
    }

    origin_access_control_id = aws_cloudfront_origin_access_control.oac.id
  }

  default_cache_behavior {
    target_origin_id       = "s3-origin"
    viewer_protocol_policy = "redirect-to-https"
    allowed_methods        = ["GET", "HEAD", "OPTIONS"]
    cached_methods         = ["GET", "HEAD"]
    compress               = true
    cache_policy_id        = aws_cloudfront_cache_policy.no_cache.id
  }

  ordered_cache_behavior {
    path_pattern           = "assets/*"
    target_origin_id       = "s3-origin"
    viewer_protocol_policy = "redirect-to-https"
    allowed_methods        = ["GET", "HEAD", "OPTIONS"]
    cached_methods         = ["GET", "HEAD"]
    compress               = true
    cache_policy_id        = aws_cloudfront_cache_policy.long_cache.id
  }

  custom_error_response {
    error_code            = 403
    response_code         = 200
    response_page_path    = "/${var.index_document}"
    error_caching_min_ttl = 0
  }

  custom_error_response {
    error_code            = 404
    response_code         = 200
    response_page_path    = "/${var.index_document}"
    error_caching_min_ttl = 0
  }

  restrictions {
    geo_restriction {
      restriction_type = "none"
    }
  }

  viewer_certificate {
    cloudfront_default_certificate = true
  }

  price_class = "PriceClass_All"
}

data "aws_iam_policy_document" "oac_access" {
  statement {
    sid     = "AllowCloudFrontOAC"
    effect  = "Allow"
    actions = ["s3:GetObject"]

    resources = ["${aws_s3_bucket.website.arn}/*"]

    principals {
      type        = "Service"
      identifiers = ["cloudfront.amazonaws.com"]
    }

    condition {
      test     = "StringEquals"
      variable = "AWS:SourceArn"
      values   = [aws_cloudfront_distribution.cdn.arn]
    }
  }
}

resource "aws_s3_bucket_policy" "oac_read" {
  bucket = aws_s3_bucket.website.id
  policy = data.aws_iam_policy_document.oac_access.json
}
