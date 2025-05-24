variable "aws_region" {
  description = "The AWS region to deploy resources in."
  type        = string
  default     = "us-east-1"
}

variable "project_name" {
  description = "A name prefix for all resources."
  type        = string
  default     = "stocktrader"
}

variable "environment" {
  description = "Deployment environment (e.g., dev, staging, prod)."
  type        = string
  default     = "dev"
}

# Networking




# RDS
variable "db_allocated_storage" {
  description = "The allocated storage for the RDS instance (in GB)."
  type        = number
  default     = 20
}

variable "db_engine_version" {
  description = "The MySQL engine version for RDS."
  type        = string
  default     = "8.0" # Check latest supported 8.0.x version for RDS MySQL
}

variable "db_instance_class" {
  description = "The instance class for the RDS instance."
  type        = string
  default     = "db.t3.micro" # Or db.t2.micro for free tier eligibility
}

variable "db_name" {
  description = "The name of the initial database to create in RDS."
  type        = string
  default     = "stocktraderdb" # Ensure this matches what your app expects
}

variable "db_username" {
  description = "The master username for the RDS database."
  type        = string
  # No default - should be provided
}

variable "db_password" {
  description = "The master password for the RDS database."
  type        = string
  sensitive   = true # Marks this variable as sensitive
  # No default - should be provided
}

# ECS & Container
variable "fargate_cpu" {
  description = "Fargate task CPU units (e.g., 256 (.25 vCPU), 512 (.5 vCPU))."
  type        = number
  default     = 512 # .5 vCPU
}

variable "fargate_memory" {
  description = "Fargate task memory in MiB (e.g., 512 (0.5GB), 1024 (1GB))."
  type        = number
  default     = 1024 # 1GB
}

variable "container_name" {
  description = "The name of the container in the task definition."
  type        = string
  default     = "stocktrader-api-container"
}

variable "image_uri" {
  description = "The full URI of the Docker image in ECR (account.dkr.ecr.region.amazonaws.com/repo:tag)."
  type        = string
  # No default - this will be supplied by your CI/CD pipeline or set manually for initial deployment.
  # Example: "123456789012.dkr.ecr.us-east-1.amazonaws.com/stocktrader-api-repo:latest"
}

variable "container_port" {
  description = "The port the container listens on."
  type        = number
  default     = 8080
}

variable "container_cpu" {
  description = "CPU units to reserve for the container (can be same as fargate_cpu for single container task)."
  type        = number
  default     = 512
}

variable "container_memory" {
  description = "Memory in MiB to reserve for the container (can be same as fargate_memory)."
  type        = number
  default     = 1024
}

variable "desired_task_count" {
  description = "The desired number of tasks for the ECS service."
  type        = number
  default     = 1
}

variable "health_check_path" {
  description = "The path for the ALB health check."
  type        = string
  default     = "/health"
}

variable "log_retention_days" {
  description = "Number of days to retain CloudWatch logs."
  type        = number
  default     = 7
}

# JWT Settings
variable "jwt_secret_key" {
  description = "Secret key for signing JWT tokens."
  type        = string
  sensitive   = true
  # No default
}

variable "jwt_issuer" {
  description = "Issuer for JWT tokens (e.g., your API's public URL)."
  type        = string
  # No default - will depend on ALB DNS or custom domain
}

variable "jwt_audience" {
  description = "Audience for JWT tokens (e.g., your API's public URL)."
  type        = string
  # No default
}

variable "jwt_duration_minutes" {
  description = "Duration in minutes for JWT token validity."
  type        = number
  default     = 60
}

variable "domain_name" {
  description = "The Route 53 hosted zone name (e.g., stocktraderyaredmekonnen.click)"
  type        = string
  default     = "" // Set this in your .tfvars or as an env var
}

variable "api_subdomain" {
  description = "The subdomain for the API (e.g., api)"
  type        = string
  default     = "api"
}

# Add variables for Stripe and Finnhub API keys if you want to manage them via Terraform
# (though AWS Secrets Manager is better for these)
# variable "stripe_secret_key" {
#   description = "Stripe Secret Key (Test Key)"
#   type        = string
#   sensitive   = true
# }
# variable "finnhub_api_key" {
#   description = "Finnhub API Key"
#   type        = string
#   sensitive   = true
# }
