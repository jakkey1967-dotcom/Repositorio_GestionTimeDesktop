# ===============================================
# Verificación rápida del formato de respuesta
# ===============================================

Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🔍 VERIFICANDO FORMATO DE RESPUESTA DEL BACKEND" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

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

$BASE_URL = "https://localhost:2502/api/v1"

# Login
Write-Host "🔐 Haciendo login..." -ForegroundColor Yellow
$loginBody = @{
    Email = "psantos@global-retail.com"
    Password = "12345678"
} | ConvertTo-Json

try {
    $login = Invoke-RestMethod -Uri "$BASE_URL/auth/login-desktop" -Method POST -ContentType "application/json" -Body $loginBody
    $headers = @{ "Authorization" = "Bearer $($login.accessToken)" }
    Write-Host "   ✅ Login OK" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Login falló" -ForegroundColor Red
    exit
}

Write-Host ""

# Probar Clientes
Write-Host "📋 GET /clientes?page=1&pageSize=2" -ForegroundColor Yellow
try {
    $clientes = Invoke-RestMethod -Uri "$BASE_URL/clientes?page=1&pageSize=2" -Method GET -Headers $headers
    
    Write-Host ""
    Write-Host "   PROPIEDADES DE LA RESPUESTA:" -ForegroundColor Cyan
    $clientes.PSObject.Properties | ForEach-Object {
        Write-Host "      • $($_.Name): $($_.Value)" -ForegroundColor White
    }
    
    Write-Host ""
    Write-Host "   JSON COMPLETO:" -ForegroundColor Cyan
    Write-Host ($clientes | ConvertTo-Json -Depth 3) -ForegroundColor Gray
    
} catch {
    Write-Host "   ❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
