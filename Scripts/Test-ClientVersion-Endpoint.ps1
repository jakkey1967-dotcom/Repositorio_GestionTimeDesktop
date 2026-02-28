<#
.SYNOPSIS
    Test del endpoint POST /api/v2/client-version
.DESCRIPTION
    1. Login -> obtiene token
    2. POST /api/v2/client-version -> registra version
    3. GET  /api/v2/client-version/all -> verifica registro (requiere ADMIN)
    4. Test negativo con version larga
#>

$ErrorActionPreference = "Stop"

# -- Config -----------------------------------------------------------
$BaseUrl  = "https://gestiontimeapi.onrender.com"
$Email    = "psantos@global-retail.com"
$Password = "12345678"

$AppVersion  = "2.0.1-beta"
$Platform    = "Desktop"
$OsVersion   = "Microsoft Windows NT 10.0.26200.0"
$MachineName = $env:COMPUTERNAME

Write-Host ""
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "  TEST: POST /api/v2/client-version" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

# -- Step 1: Login ----------------------------------------------------
Write-Host "[Step 1] Login..." -ForegroundColor Yellow
$loginBody = @{
    email      = $Email
    password   = $Password
    appVersion = $AppVersion
    platform   = $Platform
} | ConvertTo-Json -Compress

try {
    $loginResponse = Invoke-RestMethod `
        -Uri "$BaseUrl/api/v1/auth/login-desktop" `
        -Method POST `
        -ContentType "application/json; charset=utf-8" `
        -Body $loginBody

    $token = $loginResponse.accessToken
    if (-not $token) {
        Write-Host "  [FAIL] Login OK pero no se recibio token" -ForegroundColor Red
        Write-Host "  Respuesta: $($loginResponse | ConvertTo-Json -Depth 3)" -ForegroundColor Gray
        exit 1
    }
    $shortToken = $token.Substring(0, [Math]::Min(30, $token.Length))
    Write-Host "  [OK] Login exitoso - Token: $shortToken..." -ForegroundColor Green
}
catch {
    Write-Host "  [FAIL] Error en login: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails) { Write-Host "  Body: $($_.ErrorDetails.Message)" -ForegroundColor Gray }
    exit 1
}

$headers = @{
    "Authorization"  = "Bearer $token"
    "X-App-Version"  = $AppVersion
    "X-App-Platform" = $Platform
}

# -- Step 2: POST /api/v2/client-version ------------------------------
Write-Host ""
Write-Host "[Step 2] POST /api/v2/client-version" -ForegroundColor Yellow

$versionBody = @{
    appVersion  = $AppVersion
    platform    = $Platform
    osVersion   = $OsVersion
    machineName = $MachineName
} | ConvertTo-Json -Compress

Write-Host "  Body: $versionBody" -ForegroundColor Gray

try {
    $versionResponse = Invoke-RestMethod `
        -Uri "$BaseUrl/api/v2/client-version" `
        -Method POST `
        -ContentType "application/json; charset=utf-8" `
        -Headers $headers `
        -Body $versionBody

    Write-Host "  [OK] Registro exitoso (200 OK)" -ForegroundColor Green
    Write-Host ""
    Write-Host "  Respuesta:" -ForegroundColor Cyan

    $ok = $versionResponse.ok
    $updateReq = $versionResponse.updateRequired
    $updateAvail = $versionResponse.updateAvailable
    $minReq = $versionResponse.minRequiredVersion
    $latest = $versionResponse.latestVersion
    $url = $versionResponse.updateUrl
    $msg = $versionResponse.message

    Write-Host "    ok              = $ok" -ForegroundColor White
    if ($updateReq) {
        Write-Host "    updateRequired  = $updateReq" -ForegroundColor Red
    } else {
        Write-Host "    updateRequired  = $updateReq" -ForegroundColor Green
    }
    if ($updateAvail) {
        Write-Host "    updateAvailable = $updateAvail" -ForegroundColor Yellow
    } else {
        Write-Host "    updateAvailable = $updateAvail" -ForegroundColor Green
    }
    Write-Host "    minRequired     = $minReq" -ForegroundColor White
    Write-Host "    latestVersion   = $latest" -ForegroundColor White
    Write-Host "    updateUrl       = $url" -ForegroundColor White
    Write-Host "    message         = $msg" -ForegroundColor White
}
catch {
    $statusCode = 0
    if ($_.Exception.Response) {
        $statusCode = [int]$_.Exception.Response.StatusCode
    }
    Write-Host "  [FAIL] Error ($statusCode): $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails) {
        Write-Host "  Body: $($_.ErrorDetails.Message)" -ForegroundColor Gray
    }

    if ($statusCode -eq 400) {
        Write-Host ""
        Write-Host "  [!] 400 Bad Request - Posible causa:" -ForegroundColor Yellow
        Write-Host "    - appVersion excede MaxLength(50)?" -ForegroundColor Yellow
        Write-Host "    - Longitud enviada: $($AppVersion.Length) chars" -ForegroundColor Yellow
    }
    elseif ($statusCode -eq 401) {
        Write-Host "  [!] 401 Unauthorized - Token expirado o invalido" -ForegroundColor Yellow
    }
    elseif ($statusCode -eq 404) {
        Write-Host "  [!] 404 Not Found - Endpoint no desplegado? Verificar ruta v2" -ForegroundColor Yellow
    }
}

# -- Step 3: GET /api/v2/client-version/all (ADMIN) -------------------
Write-Host ""
Write-Host "[Step 3] GET /api/v2/client-version/all (verificar registro en BD)" -ForegroundColor Yellow

try {
    $allVersions = Invoke-RestMethod `
        -Uri "$BaseUrl/api/v2/client-version/all" `
        -Method GET `
        -Headers $headers

    if ($allVersions -is [array]) {
        Write-Host "  [OK] $($allVersions.Count) registro(s) encontrados" -ForegroundColor Green
        Write-Host ""

        foreach ($v in $allVersions) {
            $isCurrentUser = ($v.email -eq $Email)
            if ($isCurrentUser) {
                $color = "Cyan"
                $marker = " << TU"
            } else {
                $color = "Gray"
                $marker = ""
            }

            Write-Host "    -----------------------------------------" -ForegroundColor DarkGray
            Write-Host "    Usuario:  $($v.fullName) ($($v.email))$marker" -ForegroundColor $color
            Write-Host "    Version:  $($v.appVersionRaw)" -ForegroundColor $color
            Write-Host "    Platform: $($v.platform)" -ForegroundColor $color
            Write-Host "    OS:       $($v.osVersion)" -ForegroundColor $color
            Write-Host "    Machine:  $($v.machineName)" -ForegroundColor $color
            Write-Host "    Ultimo:   $($v.loggedAt)" -ForegroundColor $color
        }
    }
    else {
        Write-Host "  [INFO] Respuesta:" -ForegroundColor Yellow
        Write-Host "  $($allVersions | ConvertTo-Json -Depth 3)" -ForegroundColor Gray
    }
}
catch {
    $statusCode = 0
    if ($_.Exception.Response) {
        $statusCode = [int]$_.Exception.Response.StatusCode
    }
    Write-Host "  [FAIL] Error ($statusCode): $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails) { Write-Host "  Body: $($_.ErrorDetails.Message)" -ForegroundColor Gray }
}

# -- Step 4: Test negativo (version con +commitHash) ------------------
Write-Host ""
Write-Host "[Step 4] Test negativo - version con +commitHash (debe dar 400)" -ForegroundColor Yellow

$longVersion = "2.0.0-beta+6aeedc008c7a5c3457a8d994b98e851ce5c132cd"
Write-Host "  Version: $longVersion ($($longVersion.Length) chars, MaxLength=50)" -ForegroundColor Gray

$badBody = @{
    appVersion  = $longVersion
    platform    = $Platform
    osVersion   = $OsVersion
    machineName = $MachineName
} | ConvertTo-Json -Compress

try {
    $badResponse = Invoke-RestMethod `
        -Uri "$BaseUrl/api/v2/client-version" `
        -Method POST `
        -ContentType "application/json; charset=utf-8" `
        -Headers $headers `
        -Body $badBody

    Write-Host "  [WARN] Inesperado: 200 OK con version larga (backend acepto $($longVersion.Length) chars)" -ForegroundColor Yellow
}
catch {
    $statusCode = 0
    if ($_.Exception.Response) {
        $statusCode = [int]$_.Exception.Response.StatusCode
    }
    if ($statusCode -eq 400) {
        Write-Host "  [OK] Correcto: 400 Bad Request (version excede MaxLength)" -ForegroundColor Green
    }
    else {
        Write-Host "  [FAIL] Error inesperado ($statusCode): $($_.Exception.Message)" -ForegroundColor Red
    }
}

# -- Resumen -----------------------------------------------------------
Write-Host ""
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "  TEST COMPLETADO" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""
