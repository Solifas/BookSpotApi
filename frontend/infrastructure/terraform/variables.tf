variable "project_slug" {
  type    = string
  default = "hire-pros"
}

variable "environment" {
  type    = string
  default = "prod"
}

variable "aws_region" {
  type    = string
  default = "eu-west-1"
}

variable "bucket_name" {
  type        = string
  default     = null
  description = "Optional override for the S3 bucket name. Must be globally unique."
}

variable "index_document" {
  type    = string
  default = "index.html"
}

variable "error_document" {
  type        = string
  default     = "index.html"
  description = "Use index.html for SPA routing fallback."
}

variable "cacheable_paths" {
  type        = list(string)
  default     = ["assets/*"]
  description = "Globs that can use long-lived caching (Vite emits hashed assets under assets/)."
}
