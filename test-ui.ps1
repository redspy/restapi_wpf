# UI Automation + API Integration Test
param(
    [string]$ExePath = "$PSScriptRoot\httpserver\bin\Debug\httpserver.exe",
    [string]$BaseUrl = "http://localhost:8080"
)

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$pass = 0; $fail = 0

function Write-Pass($msg) { Write-Host "  [PASS] $msg" -ForegroundColor Green; $script:pass++ }
function Write-Fail($msg) { Write-Host "  [FAIL] $msg" -ForegroundColor Red;  $script:fail++ }

# -- 1. Launch App -------------------------------------------------------
Write-Host ""
Write-Host "[1/6] Launching app..." -ForegroundColor Cyan

if (-not (Test-Path $ExePath)) {
    Write-Host "  EXE not found: $ExePath" -ForegroundColor Red
    Write-Host "  Please build first (Ctrl+Shift+B in Visual Studio)." -ForegroundColor Yellow
    exit 1
}

Get-Process -Name "httpserver" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

$proc = Start-Process -FilePath $ExePath -PassThru
Start-Sleep -Seconds 2
Write-Host "  PID: $($proc.Id)"

# -- 2. Find Window ------------------------------------------------------
Write-Host "[2/6] Finding WPF window..." -ForegroundColor Cyan

$root = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
$window = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)

if (-not $window) {
    Write-Host "  ERROR: Window not found." -ForegroundColor Red
    $proc | Stop-Process -Force; exit 1
}
Write-Host "  Found: $($window.Current.Name)"

# -- 3. Click Start Button -----------------------------------------------
Write-Host "[3/6] Clicking Start button..." -ForegroundColor Cyan

$btnCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::NameProperty, "Start")
$btn = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $btnCond)

if (-not $btn) {
    Write-Host "  ERROR: Start button not found." -ForegroundColor Red
    $proc | Stop-Process -Force; exit 1
}

$btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
Start-Sleep -Seconds 1

$statusCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::AutomationIdProperty, "StatusLabel")
$statusEl = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $statusCond)
$statusText = if ($statusEl) { $statusEl.Current.Name } else { "(unknown)" }

if ($statusText -eq "Running") {
    Write-Pass "Server status: $statusText"
} else {
    Write-Fail "Server status: $statusText (expected: Running)"
}

# -- 4. API Tests --------------------------------------------------------
Write-Host ""
Write-Host "[4/6] Running API tests..." -ForegroundColor Cyan

function Test-Api($method, $path, $body, $expectedStatus) {
    try {
        $params = @{ Uri = "$BaseUrl$path"; Method = $method; UseBasicParsing = $true; ErrorAction = "Stop" }
        if ($body) {
            $params.Body = ($body | ConvertTo-Json -Compress)
            $params.Headers = @{ "Content-Type" = "application/json" }
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

$r1 = Test-Api "GET"  "/api/hello"  $null                     200
if ($r1 -and $r1.message -eq "Hello, World!") { Write-Pass "  message = Hello, World!" } else { Write-Fail "  message field error" }

$r2 = Test-Api "GET"  "/api/status" $null                     200
if ($r2 -and $r2.status -eq "running") { Write-Pass "  uptime = $($r2.uptime_seconds)s" } else { Write-Fail "  status field error" }

$r3 = Test-Api "POST" "/api/echo"   @{name="test";value=42}   200
if ($r3 -and $r3.echoed -eq $true)  { Write-Pass "  echoed = true" } else { Write-Fail "  echoed field error" }

Test-Api "POST" "/api/echo"      $null  400 | Out-Null
Test-Api "GET"  "/api/not-exist" $null  404 | Out-Null

# -- 5. Verify UI Log ----------------------------------------------------
Write-Host ""
Write-Host "[5/6] Verifying UI log..." -ForegroundColor Cyan
Start-Sleep -Milliseconds 500

$listCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::AutomationIdProperty, "LogListBox")
$listBox = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $listCond)

if ($listBox) {
    $items = $listBox.FindAll([System.Windows.Automation.TreeScope]::Children,
        [System.Windows.Automation.Condition]::TrueCondition)
    Write-Host "  Log entries: $($items.Count)"
    if ($items.Count -gt 0) {
        Write-Pass "Requests recorded in UI log"
        foreach ($item in $items) { Write-Host "    $($item.Current.Name)" }
    } else {
        Write-Fail "UI log is empty"
    }
} else {
    Write-Host "  LogListBox not found, skipping log verification." -ForegroundColor Yellow
}

# -- 6. Result & Cleanup -------------------------------------------------
Write-Host ""
Write-Host "======================================" -ForegroundColor White
$total = $pass + $fail
$color = if ($fail -eq 0) { "Green" } else { "Red" }
Write-Host "  Result: $pass / $total passed" -ForegroundColor $color
if ($fail -gt 0) { Write-Host "  Failed: $fail" -ForegroundColor Red }
Write-Host "======================================" -ForegroundColor White

Write-Host ""
Write-Host "[6/6] Closing app..." -ForegroundColor Cyan
$proc | Stop-Process -Force
Write-Host "  Done"
Write-Host ""

if ($fail -gt 0) { exit 1 } else { exit 0 }
