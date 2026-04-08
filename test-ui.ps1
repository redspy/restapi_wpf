# UI 자동화 + API 통합 테스트 스크립트
param(
    [string]$ExePath = "$PSScriptRoot\httpserver\bin\Debug\httpserver.exe",
    [string]$BaseUrl = "http://localhost:8080"
)

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$pass = 0; $fail = 0

function Write-Pass($msg) { Write-Host "  [PASS] $msg" -ForegroundColor Green; $script:pass++ }
function Write-Fail($msg) { Write-Host "  [FAIL] $msg" -ForegroundColor Red;  $script:fail++ }

# ── 1. 앱 실행 ──────────────────────────────────────────────
Write-Host ""
Write-Host "[1/6] 앱 실행 중..." -ForegroundColor Cyan

if (-not (Test-Path $ExePath)) {
    Write-Host "  EXE를 찾을 수 없습니다: $ExePath" -ForegroundColor Red
    Write-Host "  Visual Studio에서 먼저 빌드(Ctrl+Shift+B)하세요." -ForegroundColor Yellow
    exit 1
}

Get-Process -Name 'httpserver' -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

$proc = Start-Process -FilePath $ExePath -PassThru
Start-Sleep -Seconds 2
Write-Host "  PID: $($proc.Id)"

# ── 2. 창 탐색 ──────────────────────────────────────────────
Write-Host "[2/6] WPF 창 탐색 중..." -ForegroundColor Cyan

$root = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
$window = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)

if (-not $window) {
    Write-Host "  ERROR: 창을 찾을 수 없습니다." -ForegroundColor Red
    $proc | Stop-Process -Force; exit 1
}
Write-Host "  창 발견: $($window.Current.Name)"

# ── 3. Start 버튼 클릭 ──────────────────────────────────────
Write-Host "[3/6] Start 버튼 클릭..." -ForegroundColor Cyan

$btnCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::NameProperty, 'Start')
$btn = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $btnCond)

if (-not $btn) {
    Write-Host "  ERROR: Start 버튼을 찾을 수 없습니다." -ForegroundColor Red
    $proc | Stop-Process -Force; exit 1
}

$btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
Start-Sleep -Seconds 1

$statusCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'StatusLabel')
$statusEl = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $statusCond)
$statusText = if ($statusEl) { $statusEl.Current.Name } else { "(알 수 없음)" }

if ($statusText -eq "Running") {
    Write-Pass "서버 상태: $statusText"
} else {
    Write-Fail "서버 상태: $statusText (expected: Running)"
}

# ── 4. API 테스트 ────────────────────────────────────────────
Write-Host ""
Write-Host "[4/6] API 테스트 실행..." -ForegroundColor Cyan

function Test-Api($method, $path, $body, $expectedStatus) {
    try {
        $params = @{ Uri = "$BaseUrl$path"; Method = $method; UseBasicParsing = $true; ErrorAction = 'Stop' }
        if ($body) {
            $params.Body = ($body | ConvertTo-Json -Compress)
            $params.Headers = @{ 'Content-Type' = 'application/json' }
        }
        $r = Invoke-WebRequest @params
        if ($r.StatusCode -eq $expectedStatus) {
            Write-Pass "$method $path -> $($r.StatusCode)"
            return $r.Content | ConvertFrom-Json
        } else {
            Write-Fail "$method $path -> $($r.StatusCode) (expected $expectedStatus)"
        }
    } catch [System.Net.WebException] {
        $code = [int]$_.Exception.Response.StatusCode
        if ($code -eq $expectedStatus) { Write-Pass "$method $path -> $code" }
        else                           { Write-Fail "$method $path -> $code (expected $expectedStatus)" }
    } catch {
        Write-Fail "$method $path -> $($_.Exception.Message)"
    }
    return $null
}

$r1 = Test-Api 'GET'  '/api/hello'  $null                      200
if ($r1 -and $r1.message -eq 'Hello, World!') { Write-Pass "  message = Hello, World!" } else { Write-Fail "  message 필드 오류" }

$r2 = Test-Api 'GET'  '/api/status' $null                      200
if ($r2 -and $r2.status -eq 'running') { Write-Pass "  uptime = $($r2.uptime_seconds)s" } else { Write-Fail "  status 필드 오류" }

$r3 = Test-Api 'POST' '/api/echo'   @{name='테스트';value=42}  200
if ($r3 -and $r3.echoed -eq $true)  { Write-Pass "  echoed = true" } else { Write-Fail "  echoed 필드 오류" }

Test-Api 'POST' '/api/echo'      $null  400 | Out-Null
Test-Api 'GET'  '/api/not-exist' $null  404 | Out-Null

# ── 5. UI 로그 검증 ──────────────────────────────────────────
Write-Host ""
Write-Host "[5/6] UI 로그 검증..." -ForegroundColor Cyan
Start-Sleep -Milliseconds 500

$listCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'LogListBox')
$listBox = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $listCond)

if ($listBox) {
    $items = $listBox.FindAll([System.Windows.Automation.TreeScope]::Children,
        [System.Windows.Automation.Condition]::TrueCondition)
    Write-Host "  로그 항목 수: $($items.Count)"
    if ($items.Count -gt 0) {
        Write-Pass "UI 로그에 요청이 기록됨"
        foreach ($item in $items) { Write-Host "    $($item.Current.Name)" }
    } else {
        Write-Fail "UI 로그가 비어 있음"
    }
} else {
    Write-Host "  LogListBox를 찾을 수 없어 건너뜀" -ForegroundColor Yellow
}

# ── 6. 결과 및 종료 ──────────────────────────────────────────
Write-Host ""
Write-Host "======================================" -ForegroundColor White
$total = $pass + $fail
$color = if ($fail -eq 0) { 'Green' } else { 'Red' }
Write-Host "  결과: $pass / $total 통과" -ForegroundColor $color
if ($fail -gt 0) { Write-Host "  실패: $fail 건" -ForegroundColor Red }
Write-Host "======================================" -ForegroundColor White

Write-Host ""
Write-Host "[6/6] 앱 종료..." -ForegroundColor Cyan
$proc | Stop-Process -Force
Write-Host "  완료"
Write-Host ""

if ($fail -gt 0) { exit 1 } else { exit 0 }
