output "vpc_id" {
  description = "The ID of the VPC"
  value       = module.vpc.vpc_id
}

output "public_subnet_ids" {
  description = "List of IDs of public subnets"
  value       = module.vpc.public_subnets
}

output "private_subnet_ids" {
  description = "List of IDs of private subnets"
  value       = module.vpc.private_subnets
}

output "ecr_repository_url" {
  description = "The URL of the ECR repository"
  value       = aws_ecr_repository.api_repo.repository_url
}

output "rds_instance_address" {
  description = "The address of the RDS instance"
  value       = aws_db_instance.mysql_db.address
}

output "rds_instance_port" {
  description = "The port of the RDS instance"
  value       = aws_db_instance.mysql_db.port
}

output "alb_dns_name" {
  description = "The DNS name of the Application Load Balancer"
  value       = aws_lb.main_alb.dns_name
}

output "ecs_cluster_name" {
  description = "The name of the ECS cluster"
  value       = aws_ecs_cluster.main_cluster.name
}

output "ecs_service_name" {
  description = "The name of the ECS service"
  value       = aws_ecs_service.api_service.name
}

output "api_endpoint_custom_domain" {
  description = "Custom domain API endpoint if Route 53 is configured"
  value = var.domain_name != "" ? "http://${var.api_subdomain}.${trim(data.aws_route53_zone.selected_zone[0].name, ".")}/" : "Route 53 domain_name variable not set."
}