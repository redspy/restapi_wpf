# HTTP Server 자동화 테스트 스크립트
param(
    [string]$BaseUrl = "http://localhost:8080"
)

$pass = 0
$fail = 0

function Write-Pass($msg) {
    Write-Host "  [PASS] $msg" -ForegroundColor Green
    $script:pass++
}

function Write-Fail($msg) {
    Write-Host "  [FAIL] $msg" -ForegroundColor Red
    $script:fail++
}

function Invoke-Test {
    param(
        [string]$Name,
        [string]$Method = "GET",
        [string]$Path,
        [object]$Body = $null,
        [int]$ExpectedStatus = 200,
        [hashtable]$ExpectedFields = @{}
    )

    Write-Host ""
    Write-Host "[$Method $Path]" -ForegroundColor Cyan

    try {
        $params = @{
            Uri     = "$BaseUrl$Path"
            Method  = $Method
            Headers = @{ "Content-Type" = "application/json" }
            UseBasicParsing = $true
            ErrorAction = "Stop"
        }

        if ($Body) {
            $params.Body = ($Body | ConvertTo-Json -Compress)
        }

        $response = Invoke-WebRequest @params
        $json = $response.Content | ConvertFrom-Json

        # 상태 코드 검사
        if ($response.StatusCode -eq $ExpectedStatus) {
            Write-Pass "Status $($response.StatusCode)"
        } else {
            Write-Fail "Status $($response.StatusCode) (expected $ExpectedStatus)"
        }

        # 필드 검사
        foreach ($key in $ExpectedFields.Keys) {
            $expected = $ExpectedFields[$key]
            $actual = $json.$key

            if ($null -eq $actual) {
                Write-Fail "Field '$key' missing in response"
            } elseif ($expected -ne "*" -and $actual -ne $expected) {
                Write-Fail "Field '$key' = '$actual' (expected '$expected')"
            } else {
                Write-Pass "Field '$key' present$(if ($expected -ne '*') { " = '$actual'" })"
            }
        }

        return $json

    } catch [System.Net.WebException] {
        $statusCode = [int]$_.Exception.Response.StatusCode
        if ($statusCode -eq $ExpectedStatus) {
            Write-Pass "Status $statusCode (expected error)"
        } else {
            Write-Fail "HTTP $statusCode - $($_.Exception.Message)"
        }
    } catch {
        Write-Fail "요청 실패 - $($_.Exception.Message)"
        Write-Host "    서버가 실행 중인지 확인하세요: $BaseUrl" -ForegroundColor Yellow
    }

    return $null
}

# ─────────────────────────────────────────
Write-Host ""
Write-Host "======================================" -ForegroundColor White
Write-Host "  HTTP Server 자동화 테스트" -ForegroundColor White
Write-Host "  대상: $BaseUrl" -ForegroundColor White
Write-Host "======================================" -ForegroundColor White

# 서버 연결 가능 여부 사전 확인
Write-Host ""
Write-Host "[서버 연결 확인]" -ForegroundColor Cyan
try {
    $ping = Invoke-WebRequest -Uri "$BaseUrl/api/hello" -UseBasicParsing -ErrorAction Stop
    Write-Host "  서버 응답 확인됨" -ForegroundColor Green
} catch {
    Write-Host "  서버에 연결할 수 없습니다. 앱을 실행하고 Start를 누른 후 다시 시도하세요." -ForegroundColor Red
    exit 1
}

# ─────────────────────────────────────────
Write-Host ""
Write-Host "--- GET /api/hello ---" -ForegroundColor White

Invoke-Test -Method GET -Path "/api/hello" -ExpectedStatus 200 -ExpectedFields @{
    message   = "Hello, World!"
    timestamp = "*"
}

# ─────────────────────────────────────────
Write-Host ""
Write-Host "--- GET /api/status ---" -ForegroundColor White

$statusResult = Invoke-Test -Method GET -Path "/api/status" -ExpectedStatus 200 -ExpectedFields @{
    status         = "running"
    uptime_seconds = "*"
}

if ($statusResult -and $statusResult.uptime_seconds -ge 0) {
    Write-Pass "Uptime $($statusResult.uptime_seconds)초 (유효한 값)"
}

# ─────────────────────────────────────────
Write-Host ""
Write-Host "--- POST /api/echo ---" -ForegroundColor White

$echoResult = Invoke-Test -Method POST -Path "/api/echo" `
    -Body @{ name = "테스트"; value = 42 } `
    -ExpectedStatus 200 `
    -ExpectedFields @{
        name      = "테스트"
        value     = "*"
        echoed    = "*"
        timestamp = "*"
    }

if ($echoResult -and $echoResult.echoed -eq $true) {
    Write-Pass "echoed = true 확인"
}

# ─────────────────────────────────────────
Write-Host ""
Write-Host "--- POST /api/echo (빈 바디) ---" -ForegroundColor White

try {
    $emptyBody = Invoke-WebRequest -Uri "$BaseUrl/api/echo" -Method POST `
        -Headers @{ "Content-Type" = "application/json" } `
        -Body "" -UseBasicParsing -ErrorAction Stop
    $json = $emptyBody.Content | ConvertFrom-Json
    if ($emptyBody.StatusCode -eq 400 -or $json.error) {
        Write-Pass "빈 바디 → 400 또는 error 필드 반환"
        $script:pass++
    } else {
        Write-Fail "빈 바디에서 오류를 반환해야 함"
        $script:fail++
    }
} catch [System.Net.WebException] {
    $code = [int]$_.Exception.Response.StatusCode
    if ($code -eq 400) {
        Write-Pass "빈 바디 → 400 Bad Request"
        $script:pass++
    } else {
        Write-Fail "예상치 못한 상태 코드: $code"
        $script:fail++
    }
} catch {
    Write-Fail "요청 실패: $($_.Exception.Message)"
    $script:fail++
}

# ─────────────────────────────────────────
Write-Host ""
Write-Host "--- GET /api/not-exist (404) ---" -ForegroundColor White

try {
    Invoke-WebRequest -Uri "$BaseUrl/api/not-exist" -UseBasicParsing -ErrorAction Stop | Out-Null
    Write-Fail "404가 반환되어야 하는데 성공 응답이 왔습니다"
    $script:fail++
} catch [System.Net.WebException] {
    $code = [int]$_.Exception.Response.StatusCode
    if ($code -eq 404) {
        Write-Pass "존재하지 않는 경로 → 404 Not Found"
        $script:pass++
    } else {
        Write-Fail "예상치 못한 상태 코드: $code"
        $script:fail++
    }
} catch {
    Write-Fail "요청 실패: $($_.Exception.Message)"
    $script:fail++
}

# ─────────────────────────────────────────
Write-Host ""
Write-Host "======================================" -ForegroundColor White
$total = $pass + $fail
$color = if ($fail -eq 0) { "Green" } else { "Red" }
Write-Host "  결과: $pass / $total 통과" -ForegroundColor $color
if ($fail -gt 0) {
    Write-Host "  실패: $fail 건" -ForegroundColor Red
}
Write-Host "======================================" -ForegroundColor White
Write-Host ""

if ($fail -gt 0) { exit 1 } else { exit 0 }
