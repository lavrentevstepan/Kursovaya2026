output "infrastructure_summary" {
  value = {
    app_endpoint       = "http://10.20.0.10:80"
    db_internal_ip     = "10.20.10.20"
    ai_internal_ip     = "10.20.20.30"
    bastion_gateway_ip = "10.20.50.50"
    disk_encryption    = var.enable_disk_encryption
  }
}
