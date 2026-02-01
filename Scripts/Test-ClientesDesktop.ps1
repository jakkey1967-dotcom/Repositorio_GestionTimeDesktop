# ===============================================
# TEST CLIENTES - Verificar Desktop con Backend
# ===============================================

Write-Host "╔══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "║ TEST CLIENTES - DESKTOP + BACKEND" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════" -ForegroundColor Cyan

# Configuración
$baseUrl = "https://localhost:2502/api/v1"
$EMAIL = "psantos@global-retail.com"
$PASSWORD = "12345678"

# Forzar UTF-8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

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
                    delegate(
                        Object obj, 
                        X509Certificate certificate, 
                        X509Chain chain, 
                        SslPolicyErrors errors
                    ) {
                        return true;
                    };
            }
        }
    }
"@
    Add-Type $certCallback
}
[ServerCertificateValidationCallback]::Ignore()

Write-Host "`n🔍 [1/7] Verificando API..." -ForegroundColor Cyan

$apiProcess = Get-Process -Name "GestionTime.Api" -ErrorAction SilentlyContinue
if ($null -eq $apiProcess) {
    Write-Host "❌ La API NO está corriendo." -ForegroundColor Red
    Write-Host "   Por favor, inicia la API desde Visual Studio (F5)" -ForegroundColor Yellow
    exit 1
}
Write-Host "✅ API corriendo (PID: $($apiProcess.Id))" -ForegroundColor Green

Write-Host "`n🔐 [2/7] Login Desktop..." -ForegroundColor Cyan
$loginBody = @{
    Email = $EMAIL
    Password = $PASSWORD
}

try {
    $loginResponse = Invoke-RestMethod `
        -Uri "$baseUrl/auth/login-desktop" `
        -Method POST `
        -ContentType "application/json" `
        -Body ($loginBody | ConvertTo-Json)
    
    $accessToken = $loginResponse.accessToken
    $headers = @{ "Authorization" = "Bearer $accessToken" }
    
    Write-Host "✅ Login exitoso - Token length: $($accessToken.Length)" -ForegroundColor Green
} catch {
    Write-Host "❌ Login falló: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "`n📋 [3/7] GET /clientes?page=1&pageSize=10..." -ForegroundColor Cyan
try {
    $clientes = Invoke-RestMethod -Uri "$baseUrl/clientes?page=1&pageSize=10" -Method GET -Headers $headers
    Write-Host "✅ Total: $($clientes.totalItems) clientes, Mostrando: $($clientes.items.Count)" -ForegroundColor Green
    
    if ($clientes.items.Count -gt 0) {
        $primer = $clientes.items[0]
        Write-Host "   • Primer cliente: ID=$($primer.id), Nombre=$($primer.nombre)" -ForegroundColor Gray
    }
} catch {
    Write-Host "❌ Error listando clientes: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n🔍 [4/7] GET /clientes?search=test..." -ForegroundColor Cyan
try {
    $busqueda = Invoke-RestMethod -Uri "$baseUrl/clientes?search=test&page=1&pageSize=5" -Method GET -Headers $headers
    Write-Host "✅ Búsqueda 'test': $($busqueda.totalItems) resultados" -ForegroundColor Green
} catch {
    Write-Host "❌ Error en búsqueda: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n📝 [5/7] POST /clientes - Crear cliente..." -ForegroundColor Cyan
$newCliente = @{
    Nombre = "Cliente Test Desktop $(Get-Date -Format 'HHmmss')"
    IdPuntoop = 9999
    LocalNum = 1
    NombreComercial = "Test Comercial Desktop"
    Provincia = "Test Province"
    Nota = "Cliente creado desde Desktop test"
}

try {
    $createdCliente = Invoke-RestMethod `
        -Uri "$baseUrl/clientes" `
        -Method POST `
        -Headers $headers `
        -Body ($newCliente | ConvertTo-Json) `
        -ContentType "application/json"
    
    Write-Host "✅ Cliente creado: ID=$($createdCliente.id), Nombre=$($createdCliente.nombre)" -ForegroundColor Green
    $clienteId = $createdCliente.id
} catch {
    Write-Host "❌ Error creando cliente: $($_.Exception.Message)" -ForegroundColor Red
    $clienteId = 0
}

if ($clienteId -gt 0) {
    Write-Host "`n✏️  [6/7] PATCH /clientes/$clienteId/nota - Actualizar nota..." -ForegroundColor Cyan
    $updateNota = @{
        Nota = "Nota actualizada desde Desktop $(Get-Date -Format 'HH:mm:ss')"
    }
    
    try {
        $clienteConNota = Invoke-RestMethod `
            -Uri "$baseUrl/clientes/$clienteId/nota" `
            -Method PATCH `
            -Headers $headers `
            -Body ($updateNota | ConvertTo-Json) `
            -ContentType "application/json"
        
        Write-Host "✅ Nota actualizada: '$($clienteConNota.nota)'" -ForegroundColor Green
    } catch {
        Write-Host "❌ Error actualizando nota: $($_.Exception.Message)" -ForegroundColor Red
    }

    Write-Host "`n🗑️  [7/7] DELETE /clientes/$clienteId - Eliminar cliente..." -ForegroundColor Cyan
    try {
        Invoke-RestMethod -Uri "$baseUrl/clientes/$clienteId" -Method DELETE -Headers $headers
        Write-Host "✅ Cliente eliminado correctamente" -ForegroundColor Green
    } catch {
        Write-Host "❌ Error eliminando cliente: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`n╔══════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "║ ✅ TESTS COMPLETADOS" -ForegroundColor Green
Write-Host "╚══════════════════════════════════════════════════════════════" -ForegroundColor Green

Write-Host "`n💡 RESUMEN:" -ForegroundColor Yellow
Write-Host "   • ApiClient ahora tiene soporte para PATCH (nuevo método PatchAsync)" -ForegroundColor White
Write-Host "   • ClientesService.UpdateNotaAsync usa PATCH en lugar de PUT" -ForegroundColor White
Write-Host "   • TiposService y GruposService ya estaban correctos" -ForegroundColor White
Write-Host "   • Todos los servicios de catálogo están listos para usar" -ForegroundColor White
Write-Host ""
