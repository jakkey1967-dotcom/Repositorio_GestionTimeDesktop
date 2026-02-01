# ===============================================
# Diagnóstico de Token - Desktop
# ===============================================
# Verifica que el token se esté generando y usando correctamente
# ===============================================

Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🔍 DIAGNÓSTICO DE TOKEN - Desktop" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Configuración
$EMAIL = "psantos@global-retail.com"
$PASSWORD = "12345678"
$BASE_URL = "https://localhost:2502/api/v1"

# Ignorar certificados SSL
if (-not ([System.Management.Automation.PSTypeName]'ServerCertificateValidationCallback').Type) {
    $certCallback = @"
    using System;
    using System.Net;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;
    public class ServerCertificateValidationCallback {
        public static void Ignore() {
            if(ServicePointManager.ServerCertificateValidationCallback == null) {
                ServicePointManager.ServerCertificateValidationCallback += 
                    delegate(Object obj, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors) {
                        return true;
                    };
            }
        }
    }
"@
    Add-Type $certCallback
}
[ServerCertificateValidationCallback]::Ignore()

Write-Host "🔐 PASO 1: Hacer login..." -ForegroundColor Yellow
$loginBody = @{
    Email = $EMAIL
    Password = $PASSWORD
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod `
        -Uri "$BASE_URL/auth/login-desktop" `
        -Method POST `
        -ContentType "application/json" `
        -Body $loginBody
    
    Write-Host "   ✅ Login exitoso" -ForegroundColor Green
    Write-Host ""
    
    # Analizar el token
    $token = $loginResponse.accessToken
    
    if ([string]::IsNullOrEmpty($token)) {
        Write-Host "   ❌ NO SE RECIBIÓ TOKEN" -ForegroundColor Red
        Write-Host ""
        Write-Host "   Respuesta completa:" -ForegroundColor Yellow
        Write-Host ($loginResponse | ConvertTo-Json -Depth 3) -ForegroundColor Gray
        exit 1
    }
    
    Write-Host "📊 PASO 2: Analizar el token JWT..." -ForegroundColor Yellow
    Write-Host "   • Longitud: $($token.Length) caracteres" -ForegroundColor White
    
    # Decodificar el JWT (solo para ver el payload)
    $parts = $token.Split('.')
    if ($parts.Length -eq 3) {
        Write-Host "   ✅ Formato JWT válido (3 partes)" -ForegroundColor Green
        
        # Decodificar payload (parte 2)
        $payload = $parts[1]
        
        # Agregar padding si es necesario
        switch ($payload.Length % 4) {
            2 { $payload += "==" }
            3 { $payload += "=" }
        }
        
        try {
            $payloadBytes = [Convert]::FromBase64String($payload)
            $payloadJson = [System.Text.Encoding]::UTF8.GetString($payloadBytes)
            $payloadObj = $payloadJson | ConvertFrom-Json
            
            Write-Host "   📄 PAYLOAD DECODIFICADO:" -ForegroundColor Cyan
            Write-Host ($payloadObj | Format-List | Out-String) -ForegroundColor White
            
            # Verificar expiración
            if ($payloadObj.exp) {
                $expDate = [DateTimeOffset]::FromUnixTimeSeconds($payloadObj.exp).LocalDateTime
                $now = Get-Date
                $diff = ($expDate - $now).TotalMinutes
                
                Write-Host "   ⏰ EXPIRACIÓN:" -ForegroundColor Cyan
                Write-Host "      • Expira el: $($expDate.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor White
                Write-Host "      • Ahora son: $($now.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor White
                Write-Host "      • Tiempo restante: $([math]::Round($diff, 2)) minutos" -ForegroundColor White
                
                if ($diff -le 0) {
                    Write-Host "      ❌ TOKEN YA EXPIRÓ" -ForegroundColor Red
                } elseif ($diff -lt 5) {
                    Write-Host "      ⚠️  TOKEN EXPIRARÁ PRONTO" -ForegroundColor Yellow
                } else {
                    Write-Host "      ✅ TOKEN VÁLIDO" -ForegroundColor Green
                }
            }
        } catch {
            Write-Host "   ⚠️  No se pudo decodificar el payload" -ForegroundColor Yellow
        }
    } else {
        Write-Host "   ❌ Formato JWT inválido (esperaba 3 partes, recibió $($parts.Length))" -ForegroundColor Red
    }
    
    Write-Host ""
    Write-Host "🧪 PASO 3: Probar el token con diferentes endpoints..." -ForegroundColor Yellow
    
    $headers = @{
        "Authorization" = "Bearer $token"
    }
    
    # Test 1: /health (no requiere auth)
    Write-Host "   [1/5] GET /health (sin auth)..." -ForegroundColor Gray
    try {
        $health = Invoke-RestMethod -Uri "$BASE_URL/health" -Method GET
        Write-Host "      ✅ 200 OK" -ForegroundColor Green
    } catch {
        Write-Host "      ❌ Error: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    }
    
    # Test 2: /profiles/me
    Write-Host "   [2/5] GET /profiles/me (requiere auth)..." -ForegroundColor Gray
    try {
        $profile = Invoke-RestMethod -Uri "$BASE_URL/profiles/me" -Method GET -Headers $headers
        Write-Host "      ✅ 200 OK - Usuario: $($profile.firstName) $($profile.lastName)" -ForegroundColor Green
    } catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-Host "      ❌ $statusCode - $($_.Exception.Message)" -ForegroundColor Red
        
        if ($statusCode -eq 401) {
            Write-Host "         🔍 TOKEN RECHAZADO POR EL BACKEND" -ForegroundColor Red
        }
    }
    
    # Test 3: /partes
    Write-Host "   [3/5] GET /partes?limit=1 (requiere auth)..." -ForegroundColor Gray
    try {
        $partes = Invoke-RestMethod -Uri "$BASE_URL/partes?limit=1" -Method GET -Headers $headers
        Write-Host "      ✅ 200 OK - $($partes.Count) partes" -ForegroundColor Green
    } catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-Host "      ❌ $statusCode - $($_.Exception.Message)" -ForegroundColor Red
    }
    
    # Test 4: /clientes
    Write-Host "   [4/5] GET /clientes?page=1&pageSize=1 (requiere auth)..." -ForegroundColor Gray
    try {
        $clientes = Invoke-RestMethod -Uri "$BASE_URL/clientes?page=1&pageSize=1" -Method GET -Headers $headers
        Write-Host "      ✅ 200 OK" -ForegroundColor Green
    } catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-Host "      ❌ $statusCode - $($_.Exception.Message)" -ForegroundColor Red
    }
    
    # Test 5: /admin/ping (requiere auth)
    Write-Host "   [5/5] GET /admin/ping (requiere auth)..." -ForegroundColor Gray
    try {
        $ping = Invoke-RestMethod -Uri "$BASE_URL/admin/ping" -Method GET -Headers $headers
        Write-Host "      ✅ 200 OK" -ForegroundColor Green
    } catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-Host "      ❌ $statusCode - $($_.Exception.Message)" -ForegroundColor Red
    }
    
} catch {
    Write-Host "   ❌ Login falló: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "📝 CONCLUSIÓN" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Si TODOS los endpoints devuelven 401:" -ForegroundColor White
Write-Host "   → El backend NO está aceptando tokens del header Authorization" -ForegroundColor Red
Write-Host "   → Necesitas aplicar el fix: Fix-JWT-Authentication.ps1" -ForegroundColor Yellow
Write-Host ""
Write-Host "Si ALGUNOS endpoints funcionan y otros no:" -ForegroundColor White
Write-Host "   → Problema de permisos/roles en el backend" -ForegroundColor Yellow
Write-Host ""
Write-Host "Si el token YA EXPIRÓ:" -ForegroundColor White
Write-Host "   → El backend está generando tokens con tiempo de vida muy corto" -ForegroundColor Yellow
Write-Host ""
