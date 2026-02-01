# ===============================================
# Comparar Token Desktop vs Swagger
# ===============================================

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "COMPARAR TOKEN DESKTOP vs SWAGGER" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

$EMAIL = "psantos@global-retail.com"
$PASSWORD = "12345678"
$BASE_URL = "https://localhost:2502/api/v1"

# Ignorar SSL
Add-Type @"
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    public class TrustAllCertsPolicy : ICertificatePolicy {
        public bool CheckValidationResult(ServicePoint srvPoint, X509Certificate certificate, WebRequest request, int certificateProblem) {
            return true;
        }
    }
"@

[System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12

Write-Host "Obteniendo token desde /auth/login-desktop..." -ForegroundColor Yellow

$loginBody = @{
    Email = $EMAIL
    Password = $PASSWORD
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod `
        -Uri "$BASE_URL/auth/login-desktop" `
        -Method POST `
        -ContentType "application/json" `
        -Body $loginBody
    
    $token = $response.accessToken
    
    if ([string]::IsNullOrEmpty($token)) {
        Write-Host "   [ERROR] NO SE RECIBIO TOKEN" -ForegroundColor Red
        Write-Host "   Respuesta:" -ForegroundColor Yellow
        Write-Host ($response | ConvertTo-Json -Depth 3) -ForegroundColor Gray
        exit 1
    }
    
    Write-Host "   [OK] Token recibido" -ForegroundColor Green
    Write-Host "   Longitud: $($token.Length) caracteres" -ForegroundColor White
    Write-Host ""
    
    # Decodificar payload
    $parts = $token.Split('.')
    if ($parts.Length -eq 3) {
        $payload = $parts[1]
        
        switch ($payload.Length % 4) {
            2 { $payload += "==" }
            3 { $payload += "=" }
        }
        
        $payloadBytes = [Convert]::FromBase64String($payload)
        $payloadJson = [System.Text.Encoding]::UTF8.GetString($payloadBytes)
        $payloadObj = $payloadJson | ConvertFrom-Json
        
        Write-Host "CONTENIDO DEL TOKEN:" -ForegroundColor Cyan
        Write-Host "   Usuario: $($payloadObj.email)" -ForegroundColor White
        Write-Host "   Rol: $($payloadObj.role)" -ForegroundColor White
        Write-Host "   Issuer: $($payloadObj.iss)" -ForegroundColor White
        Write-Host "   Audience: $($payloadObj.aud)" -ForegroundColor White
        
        if ($payloadObj.exp) {
            $expDate = [DateTimeOffset]::FromUnixTimeSeconds($payloadObj.exp).LocalDateTime
            $now = Get-Date
            $diff = ($expDate - $now).TotalMinutes
            
            Write-Host "   Expira: $($expDate.ToString('yyyy-MM-dd HH:mm:ss')) ($([math]::Round($diff, 1)) min)" -ForegroundColor White
        }
        
        Write-Host ""
        Write-Host "TOKEN (primeros 100 caracteres):" -ForegroundColor Cyan
        Write-Host "   $($token.Substring(0, [Math]::Min(100, $token.Length)))..." -ForegroundColor Gray
    }
    
    Write-Host ""
    Write-Host "PROBANDO TOKEN CON ENDPOINTS:" -ForegroundColor Yellow
    
    $headers = @{
        "Authorization" = "Bearer $token"
    }
    
    # Test 1: /partes
    Write-Host "   [1/3] GET /partes?limit=1" -ForegroundColor Gray
    try {
        $partes = Invoke-RestMethod -Uri "$BASE_URL/partes?limit=1" -Method GET -Headers $headers
        Write-Host "      [OK] 200 OK - Partes: $($partes.Count)" -ForegroundColor Green
    } catch {
        $code = $_.Exception.Response.StatusCode.value__
        Write-Host "      [ERROR] $code" -ForegroundColor Red
        
        if ($code -eq 401) {
            Write-Host "      [!!!] EL TOKEN ES RECHAZADO POR EL BACKEND" -ForegroundColor Red
        }
    }
    
    # Test 2: /profiles/me
    Write-Host "   [2/3] GET /profiles/me" -ForegroundColor Gray
    try {
        $profile = Invoke-RestMethod -Uri "$BASE_URL/profiles/me" -Method GET -Headers $headers
        Write-Host "      [OK] 200 OK" -ForegroundColor Green
    } catch {
        Write-Host "      [ERROR] $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    }
    
    # Test 3: /clientes
    Write-Host "   [3/3] GET /clientes?page=1&pageSize=1" -ForegroundColor Gray
    try {
        $clientes = Invoke-RestMethod -Uri "$BASE_URL/clientes?page=1&pageSize=1" -Method GET -Headers $headers
        Write-Host "      [OK] 200 OK" -ForegroundColor Green
    } catch {
        Write-Host "      [ERROR] $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    }
    
} catch {
    Write-Host "   [ERROR] Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "CONCLUSION:" -ForegroundColor Yellow
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Si TODOS los endpoints devuelven 401:" -ForegroundColor White
Write-Host "   -> El token de /auth/login-desktop es invalido" -ForegroundColor Yellow
Write-Host "   -> O el backend no esta leyendo el header Authorization" -ForegroundColor Yellow
Write-Host ""
Write-Host "Si ALGUNOS funcionan:" -ForegroundColor White
Write-Host "   -> Problema de permisos/roles" -ForegroundColor Yellow
Write-Host ""
