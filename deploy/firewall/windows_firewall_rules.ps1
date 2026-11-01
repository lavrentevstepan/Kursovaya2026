Set-NetFirewallProfile -Profile Domain,Public,Private -Enabled True
Set-NetFirewallProfile -Profile Domain,Public,Private -DefaultInboundAction Block
Set-NetFirewallProfile -Profile Domain,Public,Private -DefaultOutboundAction Allow

New-NetFirewallRule -Name "SKB-Web-HTTP" -DisplayName "SKB Web HTTP" -Direction Inbound -Protocol TCP -LocalPort 80 -Action Allow
New-NetFirewallRule -Name "SKB-Web-HTTPS" -DisplayName "SKB Web HTTPS" -Direction Inbound -Protocol TCP -LocalPort 443 -Action Allow
New-NetFirewallRule -Name "SKB-Web-Kestrel" -DisplayName "SKB Kestrel Internal" -Direction Inbound -Protocol TCP -LocalPort 5000,5001 -RemoteAddress 127.0.0.1,10.20.0.0/24 -Action Allow

New-NetFirewallRule -Name "SKB-Block-Sql-External" -DisplayName "SKB Block External SQL" -Direction Inbound -Protocol TCP -LocalPort 1433 -RemoteAddress "Internet" -Action Block
New-NetFirewallRule -Name "SKB-Block-Ollama-External" -DisplayName "SKB Block External Ollama" -Direction Inbound -Protocol TCP -LocalPort 11434 -RemoteAddress "Internet" -Action Block
New-NetFirewallRule -Name "SKB-Block-Search-External" -DisplayName "SKB Block External Elasticsearch" -Direction Inbound -Protocol TCP -LocalPort 9200 -RemoteAddress "Internet" -Action Block

New-NetFirewallRule -Name "SKB-Allow-Admin-RDP" -DisplayName "SKB Admin RDP Management" -Direction Inbound -Protocol TCP -LocalPort 3389 -RemoteAddress 10.20.0.50,10.20.0.51 -Action Allow
