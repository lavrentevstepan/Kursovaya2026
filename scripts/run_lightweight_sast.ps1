param(
    [string]$SourcePath = "src",
    [string]$OutputDir = "reports/sast"
)

$sourceFullPath = Resolve-Path $SourcePath
$reportsDir = Join-Path $PWD $OutputDir

if (-not (Test-Path $reportsDir)) {
    New-Item -ItemType Directory -Path $reportsDir -Force | Out-Null
}

$sastReportPath = Join-Path $reportsDir "sast-report.json"

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " Lightweight Auxiliary SAST Scanner - Kursovaya2026" -ForegroundColor Cyan
Write-Host " Source Directory: $sourceFullPath" -ForegroundColor Cyan
Write-Host " Note: Auxiliary educational scanner pursuant to M3=5" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

$csFiles = Get-ChildItem -Path $sourceFullPath -Filter "*.cs" -Recurse | Where-Object { $_.FullName -notmatch "(\\|/)obj(\\|/)" -and $_.FullName -notmatch "(\\|/)bin(\\|/)" }

$rules = @(
    @{
        Id = "SKB-SAST-001"
        Name = "Hardcoded Secret Detection"
        Cwe = "CWE-798"
        Severity = "CRITICAL"
        Pattern = '(?i)(api[_-]?key|secret[_-]?token|password)\s*=\s*["''][A-Za-z0-9+/=]{16,}["'']'
        Message = "Potential hardcoded plaintext secret or credential detected."
    },
    @{
        Id = "SKB-SAST-002"
        Name = "Weak Cryptographic Hash"
        Cwe = "CWE-327"
        Severity = "HIGH"
        Pattern = '\b(MD5|SHA1)\.Create\s*\('
        Message = "Use of broken cryptographic hash function (MD5/SHA1). Use SHA-256 or higher."
    },
    @{
        Id = "SKB-SAST-003"
        Name = "Unsafe Deserialization"
        Cwe = "CWE-502"
        Severity = "CRITICAL"
        Pattern = '\bBinaryFormatter\b'
        Message = "Insecure BinaryFormatter deserialization detected. Use System.Text.Json."
    },
    @{
        Id = "SKB-SAST-004"
        Name = "Raw SQL Concatenation"
        Cwe = "CWE-89"
        Severity = "HIGH"
        Pattern = '(?i)\b(ExecuteSqlRaw|FromSqlRaw)\s*\(\s*["''\$].*\{.*\}.*["'']'
        Message = "Possible unparameterized raw SQL string interpolation. Use parameterized queries."
    }
)

$findings = @()

foreach ($file in $csFiles) {
    $lines = Get-Content $file.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $lineText = $lines[$i]
        foreach ($rule in $rules) {
            if ($lineText -match $rule.Pattern) {
                $findings += [PSCustomObject]@{
                    ruleId = $rule.Id
                    ruleName = $rule.Name
                    cwe = $rule.Cwe
                    severity = $rule.Severity
                    filePath = $file.FullName.Substring($sourceFullPath.ToString().Length).TrimStart("\", "/")
                    lineNumber = $i + 1
                    lineContent = $lineText.Trim()
                    message = $rule.Message
                }
            }
        }
    }
}

$sarifReport = [PSCustomObject]@{
    schema = "https://raw.githubusercontent.com/oasis-tcs/sarif-spec/master/Schemata/sarif-schema-2.1.0.json"
    version = "2.1.0"
    runs = @(
        [PSCustomObject]@{
            tool = [PSCustomObject]@{
                driver = [PSCustomObject]@{
                    name = "SKB-Lightweight-SAST"
                    version = "1.0.0"
                    informationUri = "https://github.com/lavrentevstepan/Kursovaya2026"
                    rules = $rules | ForEach-Object {
                        [PSCustomObject]@{
                            id = $_.Id
                            name = $_.Name
                            shortDescription = [PSCustomObject]@{ text = $_.Message }
                            defaultConfiguration = [PSCustomObject]@{ level = if ($_.Severity -eq "CRITICAL" -or $_.Severity -eq "HIGH") { "error" } else { "warning" } }
                        }
                    }
                }
            }
            results = $findings | ForEach-Object {
                [PSCustomObject]@{
                    ruleId = $_.ruleId
                    level = if ($_.severity -eq "CRITICAL" -or $_.severity -eq "HIGH") { "error" } else { "warning" }
                    message = [PSCustomObject]@{ text = $_.message }
                    locations = @(
                        [PSCustomObject]@{
                            physicalLocation = [PSCustomObject]@{
                                artifactLocation = [PSCustomObject]@{ uri = $_.filePath }
                                region = [PSCustomObject]@{ startLine = $_.lineNumber }
                            }
                        }
                    )
                }
            }
        }
    )
}

$sarifJson = $sarifReport | ConvertTo-Json -Depth 10
Set-Content -Path $sastReportPath -Value $sarifJson -Encoding UTF8

Write-Host "Files Scanned: $($csFiles.Count)" -ForegroundColor Green
Write-Host "Security Rules Checked: $($rules.Count)" -ForegroundColor Green
Write-Host "Vulnerabilities Found: $($findings.Count)" -ForegroundColor $(if ($findings.Count -gt 0) { "Red" } else { "Green" })
Write-Host "Report Path: $sastReportPath" -ForegroundColor Cyan

if ($findings.Count -eq 0) {
    Write-Host "[SUCCESS] SAST Analysis Clean! No security flaws detected in source code." -ForegroundColor Green
    exit 0
} else {
    Write-Host "[WARNING] SAST Analysis detected $($findings.Count) issue(s)." -ForegroundColor Yellow
    exit 0
}

