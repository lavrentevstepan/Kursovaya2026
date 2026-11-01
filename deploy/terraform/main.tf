terraform {
  required_version = ">= 1.0.0"

  required_providers {
    local = {
      source  = "hashicorp/local"
      version = "~> 2.4.0"
    }
  }
}

locals {
  project_name = "secure-knowledge-base"
  environment  = var.environment
  app_subnet   = "10.20.0.0/24"
}
