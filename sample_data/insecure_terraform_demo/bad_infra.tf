resource "aws_security_group_rule" "insecure_ssh_ingress" {
  type        = "ingress"
  from_port   = 22
  to_port     = 22
  protocol    = "tcp"
  cidr_blocks = ["0.0.0.0/0"]
}

resource "aws_ebs_volume" "unencrypted_storage" {
  availability_zone = "us-east-1a"
  size              = 100
  encrypted         = false
}

resource "aws_db_instance" "public_database" {
  allocated_storage   = 50
  engine              = "sqlserver-ex"
  instance_class      = "db.t3.medium"
  publicly_accessible = true
}
