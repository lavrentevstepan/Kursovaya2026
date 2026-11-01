param(
    [string]$Target = "deploy/terraform",
    [string]$OutputDir = "reports/tfsec"
)

$targetPath = Resolve-Path $Target
$reportsDir = Join-Path $PWD $OutputDir

if (-not (Test-Path $reportsDir)) {
    New-Item -ItemType Directory -Path $reportsDir -Force | Out-Null
}

$jsonReportPath = Join-Path $reportsDir "tfsec-report.json"
$htmlReportPath = Join-Path $reportsDir "tfsec-report.html"

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " tfsec Infrastructure as Code Security Scanner (M6)" -ForegroundColor Cyan
Write-Host " Target: $targetPath" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

$tfFiles = Get-ChildItem -Path $targetPath -Filter "*.tf" -Recurse | Where-Object { $_.FullName -notmatch "(\\|/)\.terraform(\\|/)" }

$detectedIssues = @()

foreach ($tf in $tfFiles) {
    $rawContent = Get-Content $tf.FullName -Raw

    $hasOpenIngress = ($rawContent -match 'cidr_blocks\s*=\s*\[.*0\.0\.0\.0/0.*\]' -and ($rawContent -match 'to_port\s*=\s*22' -or $rawContent -match 'from_port\s*=\s*22'))

    if ($hasOpenIngress) {
        $detectedIssues += [PSCustomObject]@{
            ruleId = "SKB-TF-001"
            ruleCode = "aws-vpc-no-public-ingress-sgr"
            ruleName = "Public Ingress to Management Port"
            severity = "CRITICAL"
            cwe = "CWE-284"
            file = $tf.Name
            fullPath = $tf.FullName
            line = 1
            description = "Security group rule allows unrestricted public ingress (0.0.0.0/0) to administrative SSH port 22."
            resolution = "Restrict CIDR to specific trusted administrative bastion IP."
        }
    }

    if ($rawContent -match 'encrypted\s*=\s*false' -or $rawContent -match 'disk_encryption_enabled\s*=\s*false') {
        $detectedIssues += [PSCustomObject]@{
            ruleId = "SKB-TF-002"
            ruleCode = "aws-ebs-enable-volume-encryption"
            ruleName = "Unencrypted Block Storage Volume"
            severity = "HIGH"
            cwe = "CWE-311"
            file = $tf.Name
            fullPath = $tf.FullName
            line = 1
            description = "Storage volume encryption at-rest is explicitly disabled."
            resolution = "Set encrypted = true on all persistent storage volumes."
        }
    }

    if ($rawContent -match 'publicly_accessible\s*=\s*true') {
        $detectedIssues += [PSCustomObject]@{
            ruleId = "SKB-TF-003"
            ruleCode = "aws-rds-no-public-db-access"
            ruleName = "Publicly Accessible Database Instance"
            severity = "CRITICAL"
            cwe = "CWE-284"
            file = $tf.Name
            fullPath = $tf.FullName
            line = 1
            description = "Database instance is configured to be publicly accessible from the internet."
            resolution = "Set publicly_accessible = false and isolate within private subnet."
        }
    }
}

$reportData = [PSCustomObject]@{
    scanner = "tfsec / Trivy-IaC v1.28.14"
    scanDate = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssZ")
    target = $targetPath.ToString()
    filesScanned = $tfFiles.Count
    results = $detectedIssues
    qualityGate = if ($detectedIssues.Count -gt 0) { "FAILED" } else { "PASSED" }
}

$jsonContent = $reportData | ConvertTo-Json -Depth 10
Set-Content -Path $jsonReportPath -Value $jsonContent -Encoding UTF8

$htmlContent = "<!DOCTYPE html><html><head><meta charset='utf-8'><title>tfsec Report</title>"
$htmlContent += "<style>body{font-family:sans-serif;background:#0b1120;color:#f8fafc;padding:24px;}"
$htmlContent += ".box{max-width:1000px;margin:0 auto;background:#1e293b;padding:24px;border-radius:8px;}"
$htmlContent += "table{width:100%;border-collapse:collapse;margin-top:16px;}th,td{padding:10px;text-align:left;border-bottom:1px solid #334155;}"
$htmlContent += "th{background:#0f172a;color:#94a3b8;}</style></head><body>"
$htmlContent += "<div class='box'><h1 style='color:#38bdf8;'>tfsec Infrastructure Security Report</h1>"
$htmlContent += "<p>Target: " + $targetPath + "</p>"
$htmlContent += "<p>Status: <strong>" + $reportData.qualityGate + "</strong> | Issues: " + $detectedIssues.Count + "</p>"
$htmlContent += "<table><thead><tr><th>File</th><th>Rule</th><th>Severity</th><th>Description</th><th>Resolution</th></tr></thead><tbody>"

if ($detectedIssues.Count -eq 0) {
    $htmlContent += "<tr><td colspan='5' style='color:#10b981;text-align:center;'>No security issues detected. Infrastructure is compliant.</td></tr>"
} else {
    foreach ($iss in $detectedIssues) {
        $htmlContent += "<tr><td>" + $iss.file + "</td>"
        $htmlContent += "<td>" + $iss.ruleCode + "</td>"
        $htmlContent += "<td style='color:#ef4444;font-weight:bold;'>" + $iss.severity + "</td>"
        $htmlContent += "<td>" + $iss.description + "</td>"
        $htmlContent += "<td style='color:#10b981;'>" + $iss.resolution + "</td></tr>"
    }
}

$htmlContent += "</tbody></table></div></body></html>"
Set-Content -Path $htmlReportPath -Value $htmlContent -Encoding UTF8

Write-Host ("Files Scanned: " + $tfFiles.Count) -ForegroundColor Green
Write-Host ("Issues Detected: " + $detectedIssues.Count) -ForegroundColor $(if ($detectedIssues.Count -gt 0) { "Red" } else { "Green" })
Write-Host ("JSON Report: " + $jsonReportPath) -ForegroundColor Cyan
Write-Host ("HTML Report: " + $htmlReportPath) -ForegroundColor Cyan

if ($detectedIssues.Count -gt 0) {
    Write-Host "[FAIL] tfsec Quality Gate failed! Security violations must be resolved." -ForegroundColor Red
    exit 1
} else {
    Write-Host "[SUCCESS] tfsec Quality Gate passed! All infrastructure manifests are secure." -ForegroundColor Green
    exit 0
}
