terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0" # Specify a recent AWS provider version
    }
  }
}

provider "aws" {
  region = var.aws_region
  # Credentials will be sourced from AWS CLI configuration, environment variables, or IAM role.
}

#------------------------------------------------------------------------------
# Networking (VPC, Subnets, Gateways, Route Tables)
#------------------------------------------------------------------------------
module "vpc" {
  source  = "terraform-aws-modules/vpc/aws"
  version = "~> 5.0" # Use a recent version of the VPC module

  name = "${var.project_name}-vpc"
  cidr = var.vpc_cidr

  azs             = var.vpc_availability_zones
  private_subnets = var.vpc_private_subnets
  public_subnets  = var.vpc_public_subnets

  enable_nat_gateway     = true # Set to true if your private subnets need outbound internet
  single_nat_gateway     = true # For cost savings in dev/test
  one_nat_gateway_per_az = false # For cost savings in dev/test

  enable_dns_hostnames = true
  enable_dns_support   = true

  tags = {
    Terraform   = "true"
    Environment = var.environment
    Project     = var.project_name
  }
}

#------------------------------------------------------------------------------
# ECR Repository
#------------------------------------------------------------------------------
resource "aws_ecr_repository" "api_repo" {
  name                 = "${var.project_name}-api-repo" // This will evaluate to "stocktrader-api-repo"
  image_tag_mutability = "MUTABLE"

  image_scanning_configuration {
    scan_on_push = true
  }

  tags = {
    Environment = var.environment
    Project     = var.project_name
  }
}

#------------------------------------------------------------------------------
# RDS MySQL Database
#------------------------------------------------------------------------------
resource "aws_db_subnet_group" "rds_subnet_group" {
  name       = "${var.project_name}-rds-subnet-group"
  subnet_ids = module.vpc.private_subnets # RDS should be in private subnets

  tags = {
    Name        = "${var.project_name}-rds-subnet-group"
    Environment = var.environment
    Project     = var.project_name
  }
}

resource "aws_security_group" "rds_sg" {
  name        = "${var.project_name}-rds-sg"
  description = "Allow MySQL traffic from ECS tasks"
  vpc_id      = module.vpc.vpc_id

  # Ingress rule will be added later to allow from ECS task SG
  # Egress: Allow all outbound by default (usually fine)
  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name        = "${var.project_name}-rds-sg"
    Environment = var.environment
    Project     = var.project_name
  }
}

resource "aws_db_instance" "mysql_db" {
  identifier             = "${var.project_name}-db-instance"
  allocated_storage      = var.db_allocated_storage
  storage_type           = "gp3"
  engine                 = "mysql"
  engine_version         = var.db_engine_version
  instance_class         = var.db_instance_class
  db_name                = var.db_name
  username               = var.db_username
  password               = var.db_password
  db_subnet_group_name   = aws_db_subnet_group.rds_subnet_group.name
  vpc_security_group_ids = [aws_security_group.rds_sg.id]
  parameter_group_name   = "default.mysql8.0" // <--- MODIFIED HERE

  publicly_accessible    = false
  skip_final_snapshot    = true
  multi_az               = false

  tags = {
    Name        = "${var.project_name}-db-instance"
    Environment = var.environment
    Project     = var.project_name
  }
}

#------------------------------------------------------------------------------
# ECS Cluster
#------------------------------------------------------------------------------
resource "aws_ecs_cluster" "main_cluster" {
  name = "${var.project_name}-ecs-cluster" # e.g., stocktrader-ecs-cluster

  setting {
    name  = "containerInsights"
    value = "enabled" # Or "disabled"
  }

  tags = {
    Name        = "${var.project_name}-ecs-cluster"
    Environment = var.environment
    Project     = var.project_name
  }
}

#------------------------------------------------------------------------------
# Application Load Balancer (ALB)
#------------------------------------------------------------------------------
resource "aws_security_group" "alb_sg" {
  name        = "${var.project_name}-alb-sg"
  description = "Allow HTTP/HTTPS traffic to ALB"
  vpc_id      = module.vpc.vpc_id

  ingress {
    description      = "HTTP from Internet"
    from_port        = 80
    to_port          = 80
    protocol         = "tcp"
    cidr_blocks      = ["0.0.0.0/0"]
    ipv6_cidr_blocks = ["::/0"]
  }
  # Add ingress for HTTPS (port 443) when you configure an HTTPS listener
  # ingress {
  #   description      = "HTTPS from Internet"
  #   from_port        = 443
  #   to_port          = 443
  #   protocol         = "tcp"
  #   cidr_blocks      = ["0.0.0.0/0"]
  #   ipv6_cidr_blocks = ["::/0"]
  # }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name        = "${var.project_name}-alb-sg"
    Environment = var.environment
    Project     = var.project_name
  }
}

resource "aws_lb" "main_alb" {
  name               = "${var.project_name}-alb" # e.g., stocktrader-alb
  internal           = false
  load_balancer_type = "application"
  security_groups    = [aws_security_group.alb_sg.id]
  subnets            = module.vpc.public_subnets # ALB needs to be in public subnets

  enable_deletion_protection = false # For dev/test; true for production

  tags = {
    Name        = "${var.project_name}-alb"
    Environment = var.environment
    Project     = var.project_name
  }
}

resource "aws_lb_target_group" "api_tg" {
  name        = "${var.project_name}-api-tg" # e.g., stocktrader-api-tg
  port        = var.container_port
  protocol    = "HTTP"
  vpc_id      = module.vpc.vpc_id
  target_type = "ip" # For Fargate

  health_check {
    enabled             = true
    path                = var.health_check_path
    port                = "traffic-port" # Uses the port defined for the target group
    protocol            = "HTTP"
    matcher             = "200" # Expect HTTP 200 for healthy
    interval            = 30
    timeout             = 5
    healthy_threshold   = 2
    unhealthy_threshold = 2
  }

  tags = {
    Name        = "${var.project_name}-api-tg"
    Environment = var.environment
    Project     = var.project_name
  }
}

resource "aws_lb_listener" "http_listener" {
  load_balancer_arn = aws_lb.main_alb.arn
  port              = "80"
  protocol          = "HTTP"

  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.api_tg.arn
  }
}

#------------------------------------------------------------------------------
# ECS Task Definition and Service
#------------------------------------------------------------------------------
resource "aws_security_group" "ecs_tasks_sg" {
  name        = "${var.project_name}-ecs-tasks-sg"
  description = "Allow traffic to ECS tasks from ALB"
  vpc_id      = module.vpc.vpc_id

  ingress {
    description     = "Allow traffic from ALB on container port"
    from_port       = var.container_port
    to_port         = var.container_port
    protocol        = "tcp"
    security_groups = [aws_security_group.alb_sg.id] # Source is the ALB's security group
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"] # Allows tasks to pull images, connect to RDS (via its SG), etc.
  }

  tags = {
    Name        = "${var.project_name}-ecs-tasks-sg"
    Environment = var.environment
    Project     = var.project_name
  }
}

# Allow RDS SG to accept traffic from ECS Tasks SG
resource "aws_security_group_rule" "rds_ingress_from_ecs" {
  type                     = "ingress"
  from_port                = 3306 # MySQL port
  to_port                  = 3306
  protocol                 = "tcp"
  security_group_id        = aws_security_group.rds_sg.id
  source_security_group_id = aws_security_group.ecs_tasks_sg.id
  description              = "Allow MySQL traffic from ECS tasks"
}


data "aws_iam_policy_document" "ecs_task_execution_role_policy" {
  statement {
    actions = ["sts:AssumeRole"]
    principals {
      type        = "Service"
      identifiers = ["ecs-tasks.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "ecs_task_execution_role" {
  name               = "${var.project_name}-ecs-execution-role"
  assume_role_policy = data.aws_iam_policy_document.ecs_task_execution_role_policy.json
}

resource "aws_iam_role_policy_attachment" "ecs_task_execution_role_policy_attachment" {
  role       = aws_iam_role.ecs_task_execution_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"
  # This policy allows ECS to pull images from ECR and send logs to CloudWatch.
}

resource "aws_cloudwatch_log_group" "api_logs" {
  name              = "/ecs/${var.project_name}-api-task"
  retention_in_days = var.log_retention_days

  tags = {
    Environment = var.environment
    Project     = var.project_name
  }
}

resource "aws_ecs_task_definition" "api_task_def" {
  family                   = "${var.project_name}-api-task" # e.g., stocktrader-api-task
  requires_compatibilities = ["FARGATE"]
  network_mode             = "awsvpc"
  cpu                      = var.fargate_cpu
  memory                   = var.fargate_memory
  execution_role_arn       = aws_iam_role.ecs_task_execution_role.arn
  # task_role_arn          = (optional) If your application code needs to call AWS APIs

  container_definitions = jsonencode([
    {
      name      = var.container_name # e.g., stocktrader-api-container
      image     = var.image_uri      # e.g., YOUR_ACCOUNT_ID.dkr.ecr.REGION.amazonaws.com/stocktrader-api-repo:latest
      cpu       = var.container_cpu
      memory    = var.container_memory
      essential = true
      portMappings = [
        {
          containerPort = var.container_port
          hostPort      = var.container_port # For Fargate awsvpc mode, hostPort is typically same as containerPort
          protocol      = "tcp"
        }
      ]
      environment = [
        { name = "ASPNETCORE_ENVIRONMENT", value = var.environment },
        { name = "ASPNETCORE_URLS", value = "http://+:${var.container_port}" },
        { name = "ConnectionStrings__DefaultConnection", value = "Server=${aws_db_instance.mysql_db.address};Port=${aws_db_instance.mysql_db.port};Database=${var.db_name};Uid=${var.db_username};Pwd=${var.db_password};SslMode=Preferred;" },
        { name = "JwtSettings__Key", value = var.jwt_secret_key },
        { name = "JwtSettings__Issuer", value = var.jwt_issuer }, # e.g., http://YOUR_ALB_DNS or custom domain
        { name = "JwtSettings__Audience", value = var.jwt_audience },
        { name = "JwtSettings__DurationInMinutes", value = tostring(var.jwt_duration_minutes) },
        # Add other environment variables for Stripe, Finnhub API keys etc.
        # These should ideally come from a secure source like AWS Secrets Manager or Parameter Store,
        # or be passed as sensitive variables.
        # Example:
        # { name = "StripeSettings__SecretKey", value = var.stripe_secret_key },
        # { name = "MarketDataProviders__Finnhub__ApiKey", value = var.finnhub_api_key }
      ]
      logConfiguration = {
        logDriver = "awslogs"
        options = {
          "awslogs-group"         = aws_cloudwatch_log_group.api_logs.name
          "awslogs-region"        = var.aws_region
          "awslogs-stream-prefix" = "ecs"
        }
      }
    }
  ])

  tags = {
    Name        = "${var.project_name}-api-task"
    Environment = var.environment
    Project     = var.project_name
  }
}

resource "aws_ecs_service" "api_service" {
  name            = "${var.project_name}-api-service" # e.g., stocktrader-api-service
  cluster         = aws_ecs_cluster.main_cluster.id
  task_definition = aws_ecs_task_definition.api_task_def.arn
  desired_count   = var.desired_task_count
  launch_type     = "FARGATE"

  network_configuration {
    subnets          = module.vpc.private_subnets # Run tasks in private subnets for security
    security_groups  = [aws_security_group.ecs_tasks_sg.id]
    assign_public_ip = false # Tasks in private subnets don't need public IPs if behind ALB in public subnets
  }

  load_balancer {
    target_group_arn = aws_lb_target_group.api_tg.arn
    container_name   = var.container_name
    container_port   = var.container_port
  }

  # Ensure service waits for ALB to be ready if there's an explicit dependency
  depends_on = [aws_lb_listener.http_listener]

  # Optional: deployment controller settings
  deployment_controller {
    type = "ECS" # Or CODE_DEPLOY
  }

  # Optional: health check grace period
  health_check_grace_period_seconds = 60 # Give tasks time to start before health checks fail them

  tags = {
    Name        = "${var.project_name}-api-service"
    Environment = var.environment
    Project     = var.project_name
  }
}
data "aws_route53_zone" "selected_zone" {
  count        = var.domain_name != "" ? 1 : 0 # Only create if domain_name is provided
  name         = var.domain_name
  private_zone = false
}

resource "aws_route53_record" "api_dns" {
  count   = var.domain_name != "" ? 1 : 0 # Only create if domain_name is provided
  zone_id = data.aws_route53_zone.selected_zone[0].zone_id
  name    = "${var.api_subdomain}.${data.aws_route53_zone.selected_zone[0].name}" // e.g., api.stocktraderyaredmekonnen.click
  type    = "A"

  alias {
    name                   = aws_lb.main_alb.dns_name
    zone_id                = aws_lb.main_alb.zone_id
    evaluate_target_health = true
  }
}