# ===============================================
# Comparar Token Desktop vs Swagger
# ===============================================

Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🔍 COMPARAR TOKEN DESKTOP vs SWAGGER" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

$EMAIL = "psantos@global-retail.com"
$PASSWORD = "12345678"
$BASE_URL = "https://localhost:2502/api/v1"

# Ignorar SSL
if (-not ([System.Management.Automation.PSTypeName]'ServerCertificateValidationCallback').Type) {
    Add-Type @"
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    public class ServerCertificateValidationCallback {
        public static void Ignore() {
            if(ServicePointManager.ServerCertificateValidationCallback == null) {
                ServicePointManager.ServerCertificateValidationCallback += 
                    delegate(object obj, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors) {
                        return true;
                    };
            }
        }
    }
"@
}
[ServerCertificateValidationCallback]::Ignore()

Write-Host "🔐 Obteniendo token desde /auth/login-desktop..." -ForegroundColor Yellow

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
        Write-Host "   ❌ NO SE RECIBIÓ TOKEN" -ForegroundColor Red
        Write-Host "   Respuesta:" -ForegroundColor Yellow
        Write-Host ($response | ConvertTo-Json -Depth 3) -ForegroundColor Gray
        exit 1
    }
    
    Write-Host "   ✅ Token recibido" -ForegroundColor Green
    Write-Host "   📏 Longitud: $($token.Length) caracteres" -ForegroundColor White
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
        
        Write-Host "📄 CONTENIDO DEL TOKEN:" -ForegroundColor Cyan
        Write-Host "   • Usuario: $($payloadObj.email -or $payloadObj.sub)" -ForegroundColor White
        Write-Host "   • Rol: $($payloadObj.role)" -ForegroundColor White
        Write-Host "   • Issuer: $($payloadObj.iss)" -ForegroundColor White
        Write-Host "   • Audience: $($payloadObj.aud)" -ForegroundColor White
        
        if ($payloadObj.exp) {
            $expDate = [DateTimeOffset]::FromUnixTimeSeconds($payloadObj.exp).LocalDateTime
            $now = Get-Date
            $diff = ($expDate - $now).TotalMinutes
            
            Write-Host "   • Expira: $($expDate.ToString('yyyy-MM-dd HH:mm:ss')) ($([math]::Round($diff, 1)) min)" -ForegroundColor White
        }
        
        Write-Host ""
        Write-Host "🔑 TOKEN COMPLETO (primeros 100 caracteres):" -ForegroundColor Cyan
        Write-Host "   $($token.Substring(0, [Math]::Min(100, $token.Length)))..." -ForegroundColor Gray
    }
    
    Write-Host ""
    Write-Host "🧪 PROBANDO TOKEN CON ENDPOINTS:" -ForegroundColor Yellow
    
    $headers = @{
        "Authorization" = "Bearer $token"
    }
    
    # Test 1: /partes
    Write-Host "   [1/3] GET /partes?limit=1" -ForegroundColor Gray
    try {
        $partes = Invoke-RestMethod -Uri "$BASE_URL/partes?limit=1" -Method GET -Headers $headers
        Write-Host "      ✅ 200 OK - Partes: $($partes.Count)" -ForegroundColor Green
    } catch {
        $code = $_.Exception.Response.StatusCode.value__
        Write-Host "      ❌ $code" -ForegroundColor Red
        
        if ($code -eq 401) {
            Write-Host "      ⚠️  EL TOKEN ES RECHAZADO POR EL BACKEND" -ForegroundColor Red
            Write-Host ""
            Write-Host "🔍 POSIBLES CAUSAS:" -ForegroundColor Yellow
            Write-Host "   1. El endpoint /auth/login-desktop genera un token DIFERENTE" -ForegroundColor White
            Write-Host "   2. La clave JWT (JWT_SECRET_KEY) es diferente entre endpoints" -ForegroundColor White
            Write-Host "   3. El backend requiere claims adicionales que no están en el token" -ForegroundColor White
        }
    }
    
    # Test 2: /profiles/me
    Write-Host "   [2/3] GET /profiles/me" -ForegroundColor Gray
    try {
        $profile = Invoke-RestMethod -Uri "$BASE_URL/profiles/me" -Method GET -Headers $headers
        Write-Host "      ✅ 200 OK" -ForegroundColor Green
    } catch {
        Write-Host "      ❌ $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    }
    
    # Test 3: /clientes
    Write-Host "   [3/3] GET /clientes?page=1&pageSize=1" -ForegroundColor Gray
    try {
        $clientes = Invoke-RestMethod -Uri "$BASE_URL/clientes?page=1&pageSize=1" -Method GET -Headers $headers
        Write-Host "      ✅ 200 OK" -ForegroundColor Green
    } catch {
        Write-Host "      ❌ $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    }
    
} catch {
    Write-Host "   ❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "💡 SIGUIENTE PASO:" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Si TODOS los endpoints devuelven 401 con este token:" -ForegroundColor White
Write-Host "   → Comparar este token con el de Swagger" -ForegroundColor Yellow
Write-Host "   → Verificar que /auth/login-desktop use la misma clave JWT" -ForegroundColor Yellow
Write-Host ""
Write-Host "Si el token funciona en Swagger pero no aquí:" -ForegroundColor White
Write-Host "   → Hay diferencia entre cómo Swagger y PowerShell envían el header" -ForegroundColor Yellow
Write-Host ""
