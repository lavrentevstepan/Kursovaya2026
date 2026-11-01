variable "environment" {
  type        = string
  default     = "production"
  description = "Target deployment environment"
}

variable "vpc_cidr" {
  type        = string
  default     = "10.20.0.0/16"
  description = "CIDR block for internal management network"
}

variable "admin_trusted_ip" {
  type        = string
  default     = "10.20.0.50/32"
  description = "Trusted administrative IP for Bastion host access"
}

variable "enable_disk_encryption" {
  type        = bool
  default     = true
  description = "Enforce at-rest disk encryption on all VMs"
}

variable "vm_instance_types" {
  type = map(string)
  default = {
    app     = "vm-standard-4cpu-8gb"
    db      = "vm-standard-4cpu-16gb"
    ai      = "vm-standard-8cpu-16gb"
    bastion = "vm-micro-1cpu-2gb"
  }
  description = "Hardware specifications for multi-node topology"
}
