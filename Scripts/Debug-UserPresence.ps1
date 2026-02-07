# ====================================================
# Debug-UserPresence.ps1
# Diagnóstico detallado del script de presencia
# ====================================================

param(
    [Parameter(Mandatory=$false)]
    [string]$Email = "wsanchez@global-retail.com",
    
    [Parameter(Mandatory=$false)]
    [string]$Password = "12345678",
    
    [Parameter(Mandatory=$false)]
    [string]$BaseUrl = "https://localhost:2502"
)

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🔍 DIAGNÓSTICO DE PRESENCIA DE USUARIO" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# ===== PASO 1: VERIFICAR PARÁMETROS =====
Write-Host "📋 PASO 1: Verificando parámetros..." -ForegroundColor Yellow
Write-Host "   Email: $Email"
Write-Host "   Password: $('*' * $Password.Length) ($($Password.Length) caracteres)"
Write-Host "   BaseUrl: $BaseUrl"
Write-Host ""

# ===== PASO 2: VERIFICAR CONECTIVIDAD =====
Write-Host "🌐 PASO 2: Verificando conectividad con el backend..." -ForegroundColor Yellow

try {
    $healthUrl = "$BaseUrl/api/v1/health"
    Write-Host "   Probando: $healthUrl" -NoNewline
    
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
    
    $healthResponse = Invoke-RestMethod -Uri $healthUrl -Method GET -TimeoutSec 5
    Write-Host " ✅" -ForegroundColor Green
    Write-Host "   Estado: $($healthResponse.status)"
    Write-Host ""
}
catch {
    Write-Host " ❌" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "⚠️ El backend no está accesible. Verifica:" -ForegroundColor Yellow
    Write-Host "   1. El backend está corriendo" -ForegroundColor Gray
    Write-Host "   2. La URL es correcta: $BaseUrl" -ForegroundColor Gray
    Write-Host "   3. No hay firewall bloqueando" -ForegroundColor Gray
    Write-Host ""
    exit 1
}

# ===== PASO 3: INTENTAR LOGIN =====
Write-Host "🔐 PASO 3: Intentando login..." -ForegroundColor Yellow

$loginUrl = "$BaseUrl/api/v1/auth/login-desktop"
Write-Host "   URL: $loginUrl"

$loginBody = @{
    email = $Email
    password = $Password
} | ConvertTo-Json

Write-Host "   Body JSON:"
Write-Host "   $loginBody" -ForegroundColor Gray
Write-Host ""

try {
    Write-Host "   Enviando petición..." -NoNewline
    
    $response = Invoke-WebRequest `
        -Uri $loginUrl `
        -Method POST `
        -Body $loginBody `
        -ContentType "application/json" `
        -TimeoutSec 10
    
    Write-Host " ✅" -ForegroundColor Green
    Write-Host "   Status Code: $($response.StatusCode)"
    Write-Host ""
    
    # Parsear respuesta
    $loginResponse = $response.Content | ConvertFrom-Json
    
    Write-Host "   📦 RESPUESTA COMPLETA:" -ForegroundColor Cyan
    Write-Host "   $($response.Content)" -ForegroundColor Gray
    Write-Host ""
    
    if ($loginResponse.accessToken) {
        Write-Host "   ✅ Token obtenido correctamente" -ForegroundColor Green
        Write-Host "   Token (primeros 30 chars): $($loginResponse.accessToken.Substring(0, 30))..."
        Write-Host "   Usuario: $($loginResponse.user.fullName)"
        Write-Host "   ID: $($loginResponse.user.id)"
        Write-Host "   Email: $($loginResponse.user.email)"
        Write-Host ""
        
        $token = $loginResponse.accessToken
        
        # ===== PASO 4: PROBAR PING =====
        Write-Host "💓 PASO 4: Probando ping de presencia..." -ForegroundColor Yellow
        
        $pingUrl = "$BaseUrl/api/v1/admin/ping"
        Write-Host "   URL: $pingUrl"
        
        $headers = @{
            "Authorization" = "Bearer $token"
            "Content-Type" = "application/json"
        }
        
        Write-Host "   Headers:"
        Write-Host "     Authorization: Bearer $($token.Substring(0, 30))..." -ForegroundColor Gray
        Write-Host ""
        
        try {
            Write-Host "   Enviando ping..." -NoNewline
            
            $pingResponse = Invoke-WebRequest `
                -Uri $pingUrl `
                -Method GET `
                -Headers $headers `
                -TimeoutSec 10
            
            Write-Host " ✅" -ForegroundColor Green
            Write-Host "   Status Code: $($pingResponse.StatusCode)"
            Write-Host ""
            
            $pingData = $pingResponse.Content | ConvertFrom-Json
            
            Write-Host "   📦 RESPUESTA PING:" -ForegroundColor Cyan
            Write-Host "   $($pingResponse.Content)" -ForegroundColor Gray
            Write-Host ""
            
            if ($pingData.ok) {
                Write-Host "   ✅ Ping exitoso!" -ForegroundColor Green
                Write-Host "   Role: $($pingData.role)"
                Write-Host ""
                
                Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Green
                Write-Host "✅ DIAGNÓSTICO COMPLETO: TODO FUNCIONANDO CORRECTAMENTE" -ForegroundColor Green
                Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Green
                Write-Host ""
                Write-Host "🎯 El script Test-UserPresence.ps1 debería funcionar correctamente." -ForegroundColor Cyan
                Write-Host "   Ejecuta: .\Scripts\Test-UserPresence.ps1" -ForegroundColor Gray
                Write-Host ""
            }
            else {
                Write-Host "   ⚠️ Ping respondió pero con ok=false" -ForegroundColor Yellow
            }
        }
        catch {
            Write-Host " ❌" -ForegroundColor Red
            Write-Host "   Error en ping: $($_.Exception.Message)" -ForegroundColor Red
            
            if ($_.Exception.Response) {
                Write-Host "   Status Code: $($_.Exception.Response.StatusCode.value__)"
                
                try {
                    $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                    $responseBody = $reader.ReadToEnd()
                    Write-Host "   Response Body:"
                    Write-Host "   $responseBody" -ForegroundColor Gray
                }
                catch {}
            }
            
            Write-Host ""
            Write-Host "⚠️ Posibles causas:" -ForegroundColor Yellow
            Write-Host "   1. El endpoint /api/v1/admin/ping no existe" -ForegroundColor Gray
            Write-Host "   2. El token no tiene permisos ADMIN" -ForegroundColor Gray
            Write-Host "   3. El backend no implementa este endpoint" -ForegroundColor Gray
            Write-Host ""
        }
    }
    else {
        Write-Host "   ❌ No se obtuvo token en la respuesta" -ForegroundColor Red
        Write-Host ""
    }
}
catch {
    Write-Host " ❌" -ForegroundColor Red
    Write-Host ""
    Write-Host "   ❌ Error en login: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        Write-Host "   Status Code: $($_.Exception.Response.StatusCode.value__)"
        
        # Intentar leer el body del error
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $responseBody = $reader.ReadToEnd()
            Write-Host "   Response Body:"
            Write-Host "   $responseBody" -ForegroundColor Gray
        }
        catch {}
    }
    
    Write-Host ""
    Write-Host "⚠️ Posibles causas:" -ForegroundColor Yellow
    Write-Host "   1. Credenciales incorrectas (email/password)" -ForegroundColor Gray
    Write-Host "   2. Usuario no existe en la base de datos" -ForegroundColor Gray
    Write-Host "   3. Usuario deshabilitado" -ForegroundColor Gray
    Write-Host "   4. Endpoint incorrecto" -ForegroundColor Gray
    Write-Host ""
    
    # Sugerencias
    Write-Host "💡 SUGERENCIAS:" -ForegroundColor Cyan
    Write-Host "   Prueba con otro usuario:" -ForegroundColor Gray
    Write-Host "   .\Scripts\Debug-UserPresence.ps1 -Email 'psantos@global-retail.com' -Password 'TU_PASSWORD'" -ForegroundColor Yellow
    Write-Host ""
}
