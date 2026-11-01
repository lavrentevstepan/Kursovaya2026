param(
    [string]$DefectDojoUrl = "http://localhost:8080",
    [string]$ApiKey = "",
    [string]$ProductName = "SecureKnowledgeBase",
    [string]$EngagementName = "DevSecOps Audit Cycle 1",
    [string]$OutputDir = "reports/defectdojo",
    [string]$TargetProfile = "Audit",
    [switch]$DemonstrateDeduplication = $true
)

$outputDirPath = Join-Path $PWD $OutputDir
if (-not (Test-Path $outputDirPath)) {
    New-Item -ItemType Directory -Path $outputDirPath -Force | Out-Null
}

$unifiedJsonPath = Join-Path $outputDirPath "unified_findings.json"
$htmlDashboardPath = Join-Path $outputDirPath "defectdojo_dashboard.html"

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " DefectDojo Vulnerability Management Aggregator (M8)" -ForegroundColor Cyan
Write-Host " Product:    $ProductName" -ForegroundColor Cyan
Write-Host " Engagement: $EngagementName (Profile: $TargetProfile)" -ForegroundColor Cyan
Write-Host " Output:     $outputDirPath" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

$allRawFindings = @()

$depPath = if ($TargetProfile -eq "Production") { Join-Path $PWD "reports/dependency-check/dependency-check-report.json" } else { Join-Path $PWD "reports/dependency-check-demo/dependency-check-report.json" }
if (Test-Path $depPath) {
    $depData = Get-Content $depPath -Raw | ConvertFrom-Json
    if ($depData.vulnerabilities) {
        foreach ($v in $depData.vulnerabilities) {
            $allRawFindings += [PSCustomObject]@{
                scanner = "OWASP Dependency-Check (SCA)"
                ruleId = $v.cve
                title = "$($v.cve) in $($v.dependency)"
                severity = $v.severity.ToUpper()
                cvss = [double]$v.cvssScore
                cwe = $v.cwe
                filePath = $v.dependency
                lineNumber = 1
                description = $v.description
                mitigation = $v.remediation
                dateDetected = $depData.scanDate
            }
        }
    }
}

$tfPath = if ($TargetProfile -eq "Production") { Join-Path $PWD "reports/tfsec/tfsec-report.json" } else { Join-Path $PWD "reports/tfsec-demo/tfsec-report.json" }
if (Test-Path $tfPath) {
    $tfData = Get-Content $tfPath -Raw | ConvertFrom-Json
    if ($tfData.results) {
        foreach ($iss in $tfData.results) {
            $allRawFindings += [PSCustomObject]@{
                scanner = "tfsec (IaC Security)"
                ruleId = $iss.ruleCode
                title = "$($iss.ruleName) ($($iss.ruleCode))"
                severity = $iss.severity.ToUpper()
                cvss = if ($iss.severity -eq "CRITICAL") { 9.1 } else { 7.5 }
                cwe = $iss.cwe
                filePath = $iss.file
                lineNumber = $iss.line
                description = $iss.description
                mitigation = $iss.resolution
                dateDetected = $tfData.scanDate
            }
        }
    }
}

$scoutPath = Join-Path $PWD "reports/docker-scout/scout-summary.json"
if (Test-Path $scoutPath) {
    $scoutData = Get-Content $scoutPath -Raw | ConvertFrom-Json
    if ($scoutData.vulnerabilitiesSummary.critical -gt 0 -or $scoutData.vulnerabilitiesSummary.high -gt 0) {
        $allRawFindings += [PSCustomObject]@{
            scanner = "Docker Scout (Container)"
            ruleId = "DOCKER-SCOUT-001"
            title = "Container Base Image High/Critical Vulnerabilities"
            severity = "HIGH"
            cvss = 7.8
            cwe = "CWE-1395"
            filePath = "Dockerfile"
            lineNumber = 1
            description = "Unpatched vulnerabilities in container OS layer packages."
            mitigation = "Migrate to Alpine 8.0 or Chiseled runtime."
            dateDetected = $scoutData.scanDate
        }
    }
}

$falcoPath = Join-Path $PWD "reports/falco/falco_alerts.json"
if ($TargetProfile -ne "Production" -and (Test-Path $falcoPath)) {
    $falcoData = Get-Content $falcoPath -Raw | ConvertFrom-Json
    if ($falcoData.events) {
        foreach ($evt in $falcoData.events) {
            $normSev = switch ($evt.priority.ToUpper()) {
                "CRITICAL" { "CRITICAL" }
                "WARNING"  { "MEDIUM" }
                "NOTICE"   { "LOW" }
                default    { "INFO" }
            }
            $targetProc = "runtime-kernel"
            if ($evt.outputFields -and $evt.outputFields."proc.name") {
                $targetProc = $evt.outputFields."proc.name"
            }
            $allRawFindings += [PSCustomObject]@{
                scanner = "Sysdig Falco (Runtime Security)"
                ruleId = $evt.id
                title = $evt.rule
                severity = $normSev
                cvss = switch ($normSev) { "CRITICAL" { 9.8 } "HIGH" { 7.5 } "MEDIUM" { 5.5 } "LOW" { 3.1 } default { 1.0 } }
                cwe = if ($evt.tags -contains "rce") { "CWE-94" } elseif ($evt.tags -contains "data_leak") { "CWE-200" } else { "CWE-693" }
                filePath = $targetProc
                lineNumber = 1
                description = $evt.output
                mitigation = $evt.remediation
                dateDetected = $evt.time
            }
        }
    }
}

$sastPath = Join-Path $PWD "reports/sast/sast-report.json"
if (Test-Path $sastPath) {
    $sastData = Get-Content $sastPath -Raw | ConvertFrom-Json
    if ($sastData.runs -and $sastData.runs[0].results -and ($sastData.runs[0].results -is [array])) {
        foreach ($res in $sastData.runs[0].results) {
            $filePath = "source-code"
            $lineNum = 1
            if ($res.locations -and $res.locations.Count -gt 0) {
                $filePath = $res.locations[0].physicalLocation.artifactLocation.uri
                $lineNum = $res.locations[0].physicalLocation.region.startLine
            }
            $allRawFindings += [PSCustomObject]@{
                scanner = "Auxiliary SAST (Roslyn/SARIF)"
                ruleId = $res.ruleId
                title = $res.message.text
                severity = "LOW"
                cvss = 3.5
                cwe = "CWE-20"
                filePath = $filePath
                lineNumber = $lineNum
                description = $res.message.text
                mitigation = "Ensure proper input sanitization."
                dateDetected = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssZ")
            }
        }
    }
}

if ($DemonstrateDeduplication) {
    $firstScanCount = $allRawFindings.Count
    for ($i = 0; $i -lt $firstScanCount; $i++) {
        $original = $allRawFindings[$i]
        $allRawFindings += [PSCustomObject]@{
            scanner = $original.scanner
            ruleId = $original.ruleId
            title = $original.title
            severity = $original.severity
            cvss = $original.cvss
            cwe = $original.cwe
            filePath = $original.filePath
            lineNumber = $original.lineNumber
            description = $original.description
            mitigation = $original.mitigation
            dateDetected = (Get-Date).AddMinutes(15).ToString("yyyy-MM-ddTHH:mm:ssZ")
        }
    }
}

$processedFindings = @()
$seenHashes = @{}
$duplicateCount = 0

$idCounter = 1
foreach ($f in $allRawFindings) {
    $hashString = "$($f.scanner)|$($f.ruleId)|$($f.title)|$($f.filePath)|$($f.lineNumber)"
    $hashBytes = [System.Security.Cryptography.SHA256]::Create().ComputeHash([System.Text.Encoding]::UTF8.GetBytes($hashString))
    $hashCode = [System.BitConverter]::ToString($hashBytes).Replace("-", "").ToLower()

    $isDuplicate = $false
    if ($seenHashes.ContainsKey($hashCode)) {
        $isDuplicate = $true
        $duplicateCount++
    } else {
        $seenHashes[$hashCode] = $true
    }

    $findingObj = [PSCustomObject]@{
        id = $idCounter++
        title = $f.title
        scanner = $f.scanner
        severity = $f.severity
        cvss = $f.cvss
        cwe = $f.cwe
        filePath = $f.filePath
        lineNumber = $f.lineNumber
        description = $f.description
        mitigation = $f.mitigation
        dateDetected = $f.dateDetected
        active = (-not $isDuplicate)
        verified = $true
        duplicate = $isDuplicate
        hashCode = $hashCode
    }
    $processedFindings += $findingObj
}

$critCount = @($processedFindings | Where-Object { $_.active -and $_.severity -eq "CRITICAL" }).Count
$highCount = @($processedFindings | Where-Object { $_.active -and $_.severity -eq "HIGH" }).Count
$medCount  = @($processedFindings | Where-Object { $_.active -and $_.severity -eq "MEDIUM" }).Count
$lowCount  = @($processedFindings | Where-Object { $_.active -and $_.severity -eq "LOW" }).Count
$activeCount = @($processedFindings | Where-Object { $_.active }).Count

$qualityGateStatus = if ($critCount -eq 0 -and $highCount -eq 0) { "PASSED" } else { "FAILED" }

$defectDojoPayload = [PSCustomObject]@{
    product = [PSCustomObject]@{
        name = $ProductName
        description = "DevSecOps Secure Knowledge Base Coursework Solution"
        prodType = "DevSecOps Education"
    }
    engagement = [PSCustomObject]@{
        name = $EngagementName
        targetStart = (Get-Date).ToString("yyyy-MM-dd")
        targetEnd = (Get-Date).AddDays(14).ToString("yyyy-MM-dd")
        status = "In Progress"
        environment = "Local / Development"
    }
    aggregationMetrics = [PSCustomObject]@{
        totalFindings = $processedFindings.Count
        activeFindings = $activeCount
        duplicateFindings = $duplicateCount
        critical = $critCount
        high = $highCount
        medium = $medCount
        low = $lowCount
        qualityGate = $qualityGateStatus
    }
    findings = $processedFindings
}

$jsonPayload = $defectDojoPayload | ConvertTo-Json -Depth 10
Set-Content -Path $unifiedJsonPath -Value $jsonPayload -Encoding UTF8

$qgClass = if ($qualityGateStatus -eq "PASSED") { "qg-passed" } else { "qg-failed" }

$htmlDashboard = "<!DOCTYPE html><html><head><meta charset='utf-8'><title>DefectDojo Unified Security Dashboard</title>"
$htmlDashboard += "<style>body{font-family:Segoe UI,sans-serif;background:#0b101b;color:#f1f5f9;margin:0;padding:24px;}"
$htmlDashboard += ".container{max-width:1200px;margin:0 auto;}"
$htmlDashboard += ".header-bar{display:flex;justify-content:space-between;align-items:center;background:#131c2e;padding:20px 24px;border-radius:12px;border:1px solid #1e293b;margin-bottom:24px;}"
$htmlDashboard += ".grid-stats{display:grid;grid-template-columns:repeat(6,1fr);gap:14px;margin-bottom:24px;}"
$htmlDashboard += ".card-stat{background:#131c2e;border:1px solid #1e293b;border-radius:10px;padding:16px;text-align:center;}"
$htmlDashboard += ".num{font-size:26px;font-weight:bold;margin-bottom:4px;}"
$htmlDashboard += ".lbl{font-size:11px;color:#94a3b8;text-transform:uppercase;letter-spacing:0.05em;}"
$htmlDashboard += ".badge{padding:3px 8px;border-radius:4px;font-size:11px;font-weight:bold;display:inline-block;}"
$htmlDashboard += ".badge-critical{background:#7f1d1d;color:#fca5a5;border:1px solid #dc2626;}"
$htmlDashboard += ".badge-high{background:#78350f;color:#fde68a;border:1px solid #d97706;}"
$htmlDashboard += ".badge-medium{background:#1e3a8a;color:#93c5fd;border:1px solid #2563eb;}"
$htmlDashboard += ".badge-low{background:#064e3b;color:#a7f3d0;border:1px solid #059669;}"
$htmlDashboard += ".badge-dup{background:#3b0764;color:#d8b4fe;border:1px solid #9333ea;}"
$htmlDashboard += ".table-box{background:#131c2e;border:1px solid #1e293b;border-radius:12px;padding:20px;}"
$htmlDashboard += "table{width:100%;border-collapse:collapse;margin-top:14px;font-size:13px;}"
$htmlDashboard += "th,td{padding:12px 14px;text-align:left;border-bottom:1px solid #1e293b;}"
$htmlDashboard += "th{background:#0d1524;color:#94a3b8;text-transform:uppercase;font-size:11px;}"
$htmlDashboard += "tr:hover{background:#1e293b;}"
$htmlDashboard += ".qg-badge{padding:6px 14px;border-radius:20px;font-weight:bold;font-size:13px;}"
$htmlDashboard += ".qg-failed{background:#7f1d1d;color:#fca5a5;border:1px solid #ef4444;}"
$htmlDashboard += ".qg-passed{background:#064e3b;color:#a7f3d0;border:1px solid #10b981;}"
$htmlDashboard += "</style></head><body>"
$htmlDashboard += "<div class='container'>"
$htmlDashboard += "<div class='header-bar'><div><h1 style='margin:0;color:#38bdf8;'>DefectDojo Vulnerability Management</h1>"
$htmlDashboard += "<p style='margin:4px 0 0 0;color:#94a3b8;'>Product: $ProductName | Engagement: $EngagementName | Standard: DefectDojo API v2 Normalization</p></div>"
$htmlDashboard += "<div>Quality Gate: <span class='qg-badge $qgClass'>$qualityGateStatus</span></div></div>"

$htmlDashboard += "<div class='grid-stats'>"
$htmlDashboard += "<div class='card-stat'><div class='num' style='color:#38bdf8;'>$($processedFindings.Count)</div><div class='lbl'>Total Ingested</div></div>"
$htmlDashboard += "<div class='card-stat'><div class='num' style='color:#ef4444;'>$critCount</div><div class='lbl'>Critical Active</div></div>"
$htmlDashboard += "<div class='card-stat'><div class='num' style='color:#f59e0b;'>$highCount</div><div class='lbl'>High Active</div></div>"
$htmlDashboard += "<div class='card-stat'><div class='num' style='color:#3b82f6;'>$medCount</div><div class='lbl'>Medium Active</div></div>"
$htmlDashboard += "<div class='card-stat'><div class='num' style='color:#10b981;'>$lowCount</div><div class='lbl'>Low Active</div></div>"
$htmlDashboard += "<div class='card-stat'><div class='num' style='color:#a855f7;'>$duplicateCount</div><div class='lbl'>Deduplicated (Suppressed)</div></div>"
$htmlDashboard += "</div>"

$htmlDashboard += "<div class='table-box'><h3 style='margin:0 0 8px 0;color:#f8fafc;'>Normalized Findings Ledger (DefectDojo Hash-Deduped)</h3>"
$htmlDashboard += "<table><thead><tr><th>ID</th><th>Status</th><th>Severity</th><th>Title & CWE</th><th>Scanner</th><th>Target</th><th>CVSS</th><th>Remediation Action</th></tr></thead><tbody>"

foreach ($f in $processedFindings) {
    $sevClass = "badge-low"
    if ($f.severity -eq "CRITICAL") { $sevClass = "badge-critical" }
    elseif ($f.severity -eq "HIGH") { $sevClass = "badge-high" }
    elseif ($f.severity -eq "MEDIUM") { $sevClass = "badge-medium" }

    $statusHtml = if ($f.duplicate) { "<span class='badge badge-dup'>DUPLICATE</span>" } else { "<span class='badge badge-low'>ACTIVE</span>" }

    $htmlDashboard += "<tr><td>#" + $f.id + "</td>"
    $htmlDashboard += "<td>" + $statusHtml + "</td>"
    $htmlDashboard += "<td><span class='badge " + $sevClass + "'>" + $f.severity + "</span></td>"
    $htmlDashboard += "<td><strong>" + $f.title + "</strong><br><small style='color:#94a3b8;'>" + $f.cwe + " | Hash: " + $f.hashCode.Substring(0,8) + "...</small></td>"
    $htmlDashboard += "<td><span style='color:#38bdf8;font-size:11px;'>" + $f.scanner + "</span></td>"
    $htmlDashboard += "<td><code>" + $f.filePath + "</code></td>"
    $htmlDashboard += "<td style='font-weight:bold;'>" + $f.cvss + "</td>"
    $htmlDashboard += "<td style='color:#cbd5e1;font-size:12px;'>" + $f.mitigation + "</td></tr>"
}

$htmlDashboard += "</tbody></table></div></div></body></html>"
Set-Content -Path $htmlDashboardPath -Value $htmlDashboard -Encoding UTF8

Write-Host "Total Ingested:    $($processedFindings.Count)" -ForegroundColor Green
Write-Host "Active Findings:   $activeCount" -ForegroundColor Yellow
Write-Host "Deduplicated:      $duplicateCount" -ForegroundColor Magenta
Write-Host "Critical Active:   $critCount" -ForegroundColor $(if ($critCount -gt 0) { "Red" } else { "Green" })
Write-Host "High Active:       $highCount" -ForegroundColor $(if ($highCount -gt 0) { "Yellow" } else { "Green" })
Write-Host "Medium Active:     $medCount" -ForegroundColor Cyan
Write-Host "Low Active:        $lowCount" -ForegroundColor DarkCyan
Write-Host "Quality Gate:      $qualityGateStatus" -ForegroundColor $(if ($qualityGateStatus -eq "PASSED") { "Green" } else { "Red" })
Write-Host "Unified JSON:      $unifiedJsonPath" -ForegroundColor Cyan
Write-Host "Dashboard HTML:    $htmlDashboardPath" -ForegroundColor Cyan
Write-Host "[SUCCESS] DefectDojo Aggregator completed successfully." -ForegroundColor Green
