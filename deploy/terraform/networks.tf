resource "local_file" "network_topology_manifest" {
  filename = "${path.module}/network_topology.json"
  content = jsonencode({
    vpc_cidr = var.vpc_cidr
    subnets = [
      { name = "app-dmz-subnet", cidr = "10.20.0.0/24", tier = "public_dmz" },
      { name = "db-isolated-subnet", cidr = "10.20.10.0/24", tier = "isolated_data" },
      { name = "ai-search-subnet", cidr = "10.20.20.0/24", tier = "isolated_ai" },
      { name = "bastion-mgmt-subnet", cidr = "10.20.50.0/24", tier = "management" }
    ]
    security_groups = [
      {
        id          = "sg-app-web"
        description = "Public HTTP/HTTPS traffic to Reverse Proxy"
        inbound = [
          { protocol = "tcp", port = 80, source = "0.0.0.0/0", action = "allow" },
          { protocol = "tcp", port = 443, source = "0.0.0.0/0", action = "allow" }
        ]
        outbound = [
          { protocol = "tcp", port = 1433, destination = "10.20.10.20/32", action = "allow" },
          { protocol = "tcp", port = 11434, destination = "10.20.20.30/32", action = "allow" }
        ]
      },
      {
        id          = "sg-db-storage"
        description = "Isolated database storage access"
        inbound = [
          { protocol = "tcp", port = 1433, source = "10.20.0.10/32", action = "allow" },
          { protocol = "tcp", port = 0, source = "0.0.0.0/0", action = "deny" }
        ]
      },
      {
        id          = "sg-ai-engine"
        description = "Isolated LLM and search engine access"
        inbound = [
          { protocol = "tcp", port = 11434, source = "10.20.0.10/32", action = "allow" },
          { protocol = "tcp", port = 9200, source = "10.20.0.10/32", action = "allow" },
          { protocol = "tcp", port = 0, source = "0.0.0.0/0", action = "deny" }
        ]
      },
      {
        id          = "sg-bastion-ssh"
        description = "Hardened SSH management gateway"
        inbound = [
          { protocol = "tcp", port = 22, source = var.admin_trusted_ip, action = "allow" },
          { protocol = "tcp", port = 22, source = "0.0.0.0/0", action = "deny" }
        ]
      }
    ]
  })
}
