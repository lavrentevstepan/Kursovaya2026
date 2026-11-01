param(
    [string]$Action = ""
)

$OutputEncoding = [System.Text.Encoding]::UTF8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$dotnetExe = "dotnet"
$localDotnet = "$env:LOCALAPPDATA\Microsoft\dotnet\dotnet.exe"
if (Test-Path $localDotnet) {
    $dotnetExe = $localDotnet
    $env:PATH = "$env:LOCALAPPDATA\Microsoft\dotnet;$env:PATH"
}

function Show-Header {
    Clear-Host
    Write-Host "================================================================================" -ForegroundColor Cyan
    Write-Host "         SecureKnowledgeBase (SKB) - Единый Центр Управления DevSecOps          " -ForegroundColor Cyan
    Write-Host "                   Курсовой проект (Вариант 25, .NET 8)                        " -ForegroundColor DarkCyan
    Write-Host "================================================================================" -ForegroundColor Cyan
}

function Pause-Menu {
    Write-Host ""
    Write-Host "Нажмите Enter для возврата в главное меню..." -ForegroundColor DarkGray
    if ([Console]::IsInputRedirected) {
        $null = [Console]::ReadLine()
    } else {
        $null = [Console]::ReadKey($true)
    }
}

function Invoke-SelectedAction([string]$choice) {
    switch ($choice.Trim()) {
        "1" {
            Show-Header
            Write-Host "Запуск веб-приложения SecureKnowledgeBase..." -ForegroundColor Cyan
            Write-Host "URL: http://localhost:5000 (Для остановки нажмите Ctrl+C)`n" -ForegroundColor Yellow
            Start-Process "http://localhost:5000"
            & $dotnetExe run --project src/SecureKnowledgeBase.Web --urls "http://localhost:5000"
            if ([string]::IsNullOrWhiteSpace($Action)) { Pause-Menu }
        }
        "2" {
            Show-Header
            Write-Host "Запуск автоматизированных тестов xUnit..." -ForegroundColor Cyan
            & $dotnetExe test SecureKnowledgeBase.sln --configuration Release --verbosity normal
            if ([string]::IsNullOrWhiteSpace($Action)) { Pause-Menu }
        }
        "3" {
            Show-Header
            Write-Host "Выберите профиль выполнения пайплайна:" -ForegroundColor Cyan
            Write-Host " 1. Production (Чистая релизная сборка, 100% Quality Gates)" -ForegroundColor Green
            Write-Host " 2. Audit (Демонстрационный аудит уязвимостей и дедупликации)" -ForegroundColor Yellow
            $pChoice = if ([string]::IsNullOrWhiteSpace($Action)) { Read-Host "Профиль (1/2)" } else { "1" }
            $prof = if ($pChoice -eq "2") { "Audit" } else { "Production" }
            & powershell -ExecutionPolicy Bypass -File scripts/run_pipeline_locally.ps1 -Profile $prof
            if ([string]::IsNullOrWhiteSpace($Action)) { Pause-Menu }
        }
        "4" {
            Show-Header
            Write-Host "Запуск статического анализа кода (SAST)..." -ForegroundColor Cyan
            & powershell -ExecutionPolicy Bypass -File scripts/run_lightweight_sast.ps1 -SourcePath src
            if ([string]::IsNullOrWhiteSpace($Action)) { Pause-Menu }
        }
        "5" {
            Show-Header
            Write-Host "Выберите цель для анализа сторонних компонентов (SCA):" -ForegroundColor Cyan
            Write-Host " 1. Основное решение SecureKnowledgeBase (чистые зависимости)" -ForegroundColor Green
            Write-Host " 2. Демонстрационный стенд (уязвимый пакет CVE-2024-21907)" -ForegroundColor Yellow
            $sChoice = if ([string]::IsNullOrWhiteSpace($Action)) { Read-Host "Цель (1/2)" } else { "1" }
            if ($sChoice -eq "2") {
                & powershell -ExecutionPolicy Bypass -File scripts/run_dependency_check.ps1 -Target sample_data/vulnerable_dependency_demo -OutputDir reports/dependency-check-demo
            } else {
                & powershell -ExecutionPolicy Bypass -File scripts/run_dependency_check.ps1 -Target src
            }
            if ([string]::IsNullOrWhiteSpace($Action)) { Pause-Menu }
        }
        "6" {
            Show-Header
            Write-Host "Выберите цель аудита инфраструктурного кода Terraform (tfsec):" -ForegroundColor Cyan
            Write-Host " 1. Целевая инфраструктура 4 узлов (deploy/terraform, защищенная)" -ForegroundColor Green
            Write-Host " 2. Демонстрационный стенд с дефектами (bad_infra.tf, открытый SSH)" -ForegroundColor Yellow
            $tChoice = if ([string]::IsNullOrWhiteSpace($Action)) { Read-Host "Цель (1/2)" } else { "1" }
            if ($tChoice -eq "2") {
                & powershell -ExecutionPolicy Bypass -File scripts/run_tfsec.ps1 -Target sample_data/insecure_terraform_demo -OutputDir reports/tfsec-demo
            } else {
                & powershell -ExecutionPolicy Bypass -File scripts/run_tfsec.ps1 -Target deploy/terraform
            }
            if ([string]::IsNullOrWhiteSpace($Action)) { Pause-Menu }
        }
        "7" {
            Show-Header
            Write-Host "Запуск анализа контейнерной безопасности (Docker Scout)..." -ForegroundColor Cyan
            & powershell -ExecutionPolicy Bypass -File scripts/run_docker_scout.ps1
            if ([string]::IsNullOrWhiteSpace($Action)) { Pause-Menu }
        }
        "8" {
            Show-Header
            Write-Host "Генерация спецификаций цепочки поставок (SBOM) и подпись Cosign..." -ForegroundColor Cyan
            & powershell -ExecutionPolicy Bypass -File scripts/generate_and_sign_artifacts.ps1
            if ([string]::IsNullOrWhiteSpace($Action)) { Pause-Menu }
        }
        "9" {
            Show-Header
            Write-Host "Запуск симулятора событий безопасности среды выполнения (Falco)..." -ForegroundColor Cyan
            & powershell -ExecutionPolicy Bypass -File scripts/simulate_falco_security_events.ps1
            if ([string]::IsNullOrWhiteSpace($Action)) { Pause-Menu }
        }
        "10" {
            Show-Header
            Write-Host "Запуск агрегатора уязвимостей и дедупликатора DefectDojo..." -ForegroundColor Cyan
            & powershell -ExecutionPolicy Bypass -File scripts/import_reports_to_defectdojo.ps1 -TargetProfile Production
            if (Test-Path "reports/defectdojo/defectdojo_dashboard.html") {
                Start-Process (Resolve-Path "reports/defectdojo/defectdojo_dashboard.html").Path
            }
            if ([string]::IsNullOrWhiteSpace($Action)) { Pause-Menu }
        }
        "11" {
            Show-Header
            Write-Host "Проверка окружения и статуса локальных компонентов..." -ForegroundColor Cyan
            Write-Host ""
            Write-Host (" .NET SDK:       " + (& $dotnetExe --version)) -ForegroundColor Green
            $dbMsg = if (Test-Path "app_data/secure_kb.db") { "Инициализирована (app_data/secure_kb.db)" } else { "Будет создана при первом запуске" }
            Write-Host (" База данных:    " + $dbMsg) -ForegroundColor Green
            $storageMsg = if (Test-Path "app_data/storage") { "Готово (app_data/storage)" } else { "Создано" }
            Write-Host (" Хранилище:      " + $storageMsg) -ForegroundColor Green
            Write-Host (" Тестовые файлы: " + (Get-ChildItem "sample_data" -File -Recurse).Count + " файлов в sample_data/") -ForegroundColor Green

            $apiResponding = $false
            try {
                $h = Invoke-RestMethod -Uri "http://localhost:5000/health" -TimeoutSec 1 -ErrorAction SilentlyContinue
                if ($h.status -eq "Healthy") { $apiResponding = $true }
            } catch {}

            $webMsg = if ($apiResponding) { "ONLINE (http://localhost:5000)" } else { "OFFLINE (Запустите пункт 1)" }
            $webColor = if ($apiResponding) { "Green" } else { "Yellow" }
            Write-Host (" Веб-сервер:     " + $webMsg) -ForegroundColor $webColor
            if ([string]::IsNullOrWhiteSpace($Action)) { Pause-Menu }
        }
        "12" {
            Show-Header
            Write-Host "Открытие каталога отчетов безопасности (reports/)..." -ForegroundColor Cyan
            if (Test-Path "reports") {
                Invoke-Item (Resolve-Path "reports").Path
            } else {
                Write-Host "Каталог reports еще не сформирован. Запустите тесты или пайплайн." -ForegroundColor Yellow
            }
            if ([string]::IsNullOrWhiteSpace($Action)) { Pause-Menu }
        }
        "0" {
            Write-Host "`nЗавершение работы лаунчера SecureKnowledgeBase. До свидания!" -ForegroundColor Green
            exit 0
        }
        default {
            Write-Host "`nНекорректный выбор. Пожалуйста, введите число от 0 до 12." -ForegroundColor Red
            if ([string]::IsNullOrWhiteSpace($Action)) { Start-Sleep -Seconds 1 }
        }
    }
}

if (-not [string]::IsNullOrWhiteSpace($Action)) {
    Invoke-SelectedAction $Action
    exit 0
}

while ($true) {
    Show-Header

    Write-Host " [ ОСНОВНЫЕ ДЕЙСТВИЯ С ПРИЛОЖЕНИЕМ ]" -ForegroundColor Yellow
    Write-Host "  1. Запустить веб-приложение (Blazor Server + REST API, http://localhost:5000)" -ForegroundColor White
    Write-Host "  2. Запустить автоматизированные модульные тесты (55 тестов xUnit)" -ForegroundColor White
    Write-Host "  3. Запустить полный сквозной DevSecOps пайплайн (M1 - M11)" -ForegroundColor Green
    Write-Host ""
    Write-Host " [ ИНСТРУМЕНТЫ БЕЗОПАСНОСТИ DEVSECOPS ]" -ForegroundColor Yellow
    Write-Host "  4. Статический анализ исходного кода (SAST, SARIF отчет)" -ForegroundColor White
    Write-Host "  5. Анализ сторонних компонентов (SCA, OWASP Dependency-Check)" -ForegroundColor White
    Write-Host "  6. Аудит инфраструктурного кода Terraform (IaC, tfsec)" -ForegroundColor White
    Write-Host "  7. Анализ безопасности базовых контейнеров (Docker Scout)" -ForegroundColor White
    Write-Host "  8. Цепочка поставок: генерация SBOM и цифровая подпись Cosign" -ForegroundColor White
    Write-Host "  9. Мониторинг безопасности среды выполнения (Falco Runtime Security)" -ForegroundColor White
    Write-Host " 10. Агрегация уязвимостей и запуск дашборда (DefectDojo)" -ForegroundColor White
    Write-Host ""
    Write-Host " [ ДИАГНОСТИКА И ОТЧЕТНОСТЬ ]" -ForegroundColor Yellow
    Write-Host " 11. Проверить здоровье и статус локальных компонентов (Health Check)" -ForegroundColor White
    Write-Host " 12. Открыть каталог сформированных отчетов безопасности (HTML/JSON)" -ForegroundColor White
    Write-Host ""
    Write-Host "  0. Выход из программы" -ForegroundColor DarkRed
    Write-Host "--------------------------------------------------------------------------------" -ForegroundColor DarkGray

    $userChoice = Read-Host "Выберите действие (0-12)"
    Invoke-SelectedAction $userChoice
}
