resource "local_file" "compute_nodes_manifest" {
  filename = "${path.module}/compute_nodes.json"
  content = jsonencode({
    nodes = [
      {
        name                       = "skb-app-node-01"
        role                       = "web_application"
        flavor                     = var.vm_instance_types["app"]
        ip_address                 = "10.20.0.10"
        associate_public_ip        = true
        disk_encryption_enabled    = var.enable_disk_encryption
        password_auth_disabled     = true
        permit_root_login          = false
        security_group             = "sg-app-web"
      },
      {
        name                       = "skb-db-node-01"
        role                       = "database_server"
        flavor                     = var.vm_instance_types["db"]
        ip_address                 = "10.20.10.20"
        associate_public_ip        = false
        disk_encryption_enabled    = var.enable_disk_encryption
        password_auth_disabled     = true
        permit_root_login          = false
        security_group             = "sg-db-storage"
      },
      {
        name                       = "skb-ai-node-01"
        role                       = "ai_inference_search"
        flavor                     = var.vm_instance_types["ai"]
        ip_address                 = "10.20.20.30"
        associate_public_ip        = false
        disk_encryption_enabled    = var.enable_disk_encryption
        password_auth_disabled     = true
        permit_root_login          = false
        security_group             = "sg-ai-engine"
      },
      {
        name                       = "skb-bastion-host"
        role                       = "management_bastion"
        flavor                     = var.vm_instance_types["bastion"]
        ip_address                 = "10.20.50.50"
        associate_public_ip        = true
        disk_encryption_enabled    = var.enable_disk_encryption
        password_auth_disabled     = true
        permit_root_login          = false
        security_group             = "sg-bastion-ssh"
      }
    ]
  })
}
