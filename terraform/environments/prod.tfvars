environment    = "prod"
region         = "eu-west-1"
aws_account_id = "2350-8571-3467"

# Lambda Configuration
lambda_package_path = "../bookspot-api.zip"
lambda_memory_size  = 512
lambda_timeout      = 30

# JWT Configuration (CHANGE THESE VALUES!)
jwt_secret_key = ""
jwt_issuer     = "HireAPro"
jwt_audience   = "HireAPro"

# CORS Configuration
cors_allowed_origins = [
  "http://hire-pros-prod.s3-website-eu-west-1.amazonaws.com"
]