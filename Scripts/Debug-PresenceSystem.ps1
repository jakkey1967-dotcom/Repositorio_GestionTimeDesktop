# ====================================================
# Debug-PresenceSystem.ps1
# Diagnostico completo del sistema de presencia
# ====================================================

param(
    [Parameter(Mandatory=$false)]
    [string]$BaseUrl = "https://localhost:2502",
    
    [Parameter(Mandatory=$false)]
    [string]$AdminEmail = "psantos@global-retail.com",
    
    [Parameter(Mandatory=$false)]
    [string]$AdminPassword = "12345678"
)

Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host "DIAGNOSTICO DEL SISTEMA DE PRESENCIA" -ForegroundColor Cyan
Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host ""

# Configurar SSL
if ($BaseUrl -like "*localhost*" -or $BaseUrl -like "*127.0.0.1*") {
    $code = @"
using System.Net;
using System.Security.Cryptography.X509Certificates;
public class TrustAllCertsPolicy : ICertificatePolicy {
    public bool CheckValidationResult(
        ServicePoint srvPoint, X509Certificate certificate,
        WebRequest request, int certificateProblem) {
        return true;
    }
}
"@
    if (-not ([System.Management.Automation.PSTypeName]'TrustAllCertsPolicy').Type) {
        Add-Type -TypeDefinition $code
    }
    [System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
}

# ===== PASO 1: Login con ADMIN =====
Write-Host "PASO 1: Login como ADMIN ($AdminEmail)..." -ForegroundColor Yellow

$loginBody = @{
    email = $AdminEmail
    password = $AdminPassword
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod `
        -Uri "$BaseUrl/api/v1/auth/login-desktop" `
        -Method POST `
        -Body $loginBody `
        -ContentType "application/json"
    
    $adminToken = $loginResponse.accessToken
    Write-Host "   Login ADMIN: OK" -ForegroundColor Green
    Write-Host ""
}
catch {
    Write-Host "   ERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# ===== PASO 2: Verificar usuarios online ANTES =====
Write-Host "PASO 2: Verificando usuarios online ANTES..." -ForegroundColor Yellow

$headers = @{
    "Authorization" = "Bearer $adminToken"
}

try {
    $presenceBefore = Invoke-RestMethod `
        -Uri "$BaseUrl/api/v1/presence/users" `
        -Method GET `
        -Headers $headers
    
    Write-Host "   Total usuarios: $($presenceBefore.Count)" -ForegroundColor Gray
    Write-Host "   Usuarios ONLINE:" -ForegroundColor Gray
    
    foreach ($user in $presenceBefore | Where-Object { $_.isOnline }) {
        $lastSeen = [DateTime]::Parse($user.lastSeenAt)
        $seconds = ([DateTime]::UtcNow - $lastSeen).TotalSeconds
        Write-Host "     - $($user.fullName): $($user.role) (hace $([int]$seconds)s)" -ForegroundColor Cyan
    }
    
    Write-Host ""
}
catch {
    Write-Host "   ERROR: $($_.Exception.Message)" -ForegroundColor Red
}

# ===== PASO 3: Login como USER (wsanchez) =====
Write-Host "PASO 3: Simulando login de wsanchez (USER)..." -ForegroundColor Yellow

$loginBody2 = @{
    email = "wsanchez@global-retail.com"
    password = "12345678"
} | ConvertTo-Json

try {
    $loginResponse2 = Invoke-RestMethod `
        -Uri "$BaseUrl/api/v1/auth/login-desktop" `
        -Method POST `
        -Body $loginBody2 `
        -ContentType "application/json"
    
    Write-Host "   Login USER: OK" -ForegroundColor Green
    Write-Host "   Usuario: $($loginResponse2.user.fullName)" -ForegroundColor Gray
    Write-Host ""
}
catch {
    Write-Host "   ERROR: $($_.Exception.Message)" -ForegroundColor Red
}

# ===== PASO 4: Esperar 3 segundos =====
Write-Host "PASO 4: Esperando 3 segundos..." -ForegroundColor Yellow
Start-Sleep -Seconds 3
Write-Host ""

# ===== PASO 5: Verificar usuarios online DESPUES =====
Write-Host "PASO 5: Verificando usuarios online DESPUES del login..." -ForegroundColor Yellow

try {
    $presenceAfter = Invoke-RestMethod `
        -Uri "$BaseUrl/api/v1/presence/users" `
        -Method GET `
        -Headers $headers
    
    Write-Host "   Total usuarios: $($presenceAfter.Count)" -ForegroundColor Gray
    Write-Host "   Usuarios ONLINE:" -ForegroundColor Gray
    
    $onlineUsers = @()
    foreach ($user in $presenceAfter | Where-Object { $_.isOnline }) {
        $lastSeen = [DateTime]::Parse($user.lastSeenAt)
        $seconds = ([DateTime]::UtcNow - $lastSeen).TotalSeconds
        Write-Host "     - $($user.fullName): $($user.role) (hace $([int]$seconds)s)" -ForegroundColor Cyan
        $onlineUsers += $user
    }
    
    Write-Host ""
    
    # Verificar si wsanchez aparece como online
    $wsanchezOnline = $onlineUsers | Where-Object { $_.email -eq "wsanchez@global-retail.com" }
    
    if ($wsanchezOnline) {
        Write-Host "   PROBLEMA DETECTADO: wsanchez aparece ONLINE solo con login" -ForegroundColor Red
        Write-Host "   Sin haber enviado ningun ping de presencia!" -ForegroundColor Red
        Write-Host ""
    }
}
catch {
    Write-Host "   ERROR: $($_.Exception.Message)" -ForegroundColor Red
}

# ===== PASO 6: Esperar 35 segundos para ver timeout =====
Write-Host "PASO 6: Esperando 35 segundos para ver si se marca offline..." -ForegroundColor Yellow
Write-Host "   (El timeout deberia ser ~30 segundos)" -ForegroundColor Gray
Write-Host ""

for ($i = 35; $i -gt 0; $i--) {
    Write-Host "`r   Esperando: $i segundos..." -NoNewline -ForegroundColor Gray
    Start-Sleep -Seconds 1
}

Write-Host ""
Write-Host ""

# ===== PASO 7: Verificar usuarios online DESPUES del timeout =====
Write-Host "PASO 7: Verificando usuarios online DESPUES de 35s..." -ForegroundColor Yellow

try {
    $presenceTimeout = Invoke-RestMethod `
        -Uri "$BaseUrl/api/v1/presence/users" `
        -Method GET `
        -Headers $headers
    
    Write-Host "   Total usuarios: $($presenceTimeout.Count)" -ForegroundColor Gray
    Write-Host "   Usuarios ONLINE:" -ForegroundColor Gray
    
    $onlineAfterTimeout = @()
    foreach ($user in $presenceTimeout | Where-Object { $_.isOnline }) {
        $lastSeen = [DateTime]::Parse($user.lastSeenAt)
        $seconds = ([DateTime]::UtcNow - $lastSeen).TotalSeconds
        Write-Host "     - $($user.fullName): $($user.role) (hace $([int]$seconds)s)" -ForegroundColor Cyan
        $onlineAfterTimeout += $user
    }
    
    Write-Host ""
    
    # Verificar si wsanchez TODAVIA aparece como online
    $wsanchezStillOnline = $onlineAfterTimeout | Where-Object { $_.email -eq "wsanchez@global-retail.com" }
    
    if ($wsanchezStillOnline) {
        $lastSeen = [DateTime]::Parse($wsanchezStillOnline.lastSeenAt)
        $seconds = ([DateTime]::UtcNow - $lastSeen).TotalSeconds
        
        Write-Host "===================================================================" -ForegroundColor Red
        Write-Host "PROBLEMA CONFIRMADO" -ForegroundColor Red
        Write-Host "===================================================================" -ForegroundColor Red
        Write-Host ""
        Write-Host "wsanchez SIGUE ONLINE despues de $([int]$seconds) segundos" -ForegroundColor Red
        Write-Host "sin enviar ningun ping de presencia." -ForegroundColor Red
        Write-Host ""
        Write-Host "Causas posibles:" -ForegroundColor Yellow
        Write-Host "  1. El timeout del backend es > 35 segundos" -ForegroundColor Gray
        Write-Host "  2. El endpoint /presence/users no calcula isOnline correctamente" -ForegroundColor Gray
        Write-Host "  3. El login crea un registro de presencia sin expirar" -ForegroundColor Gray
        Write-Host ""
    }
    else {
        Write-Host "===================================================================" -ForegroundColor Green
        Write-Host "SISTEMA FUNCIONANDO CORRECTAMENTE" -ForegroundColor Green
        Write-Host "===================================================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "wsanchez se marco como OFFLINE correctamente" -ForegroundColor Green
        Write-Host "despues del timeout." -ForegroundColor Green
        Write-Host ""
    }
}
catch {
    Write-Host "   ERROR: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "Diagnostico completo." -ForegroundColor Cyan
Write-Host ""
