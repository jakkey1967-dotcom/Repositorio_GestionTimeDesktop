# ===============================================
# Script de diagnóstico - ListView vacía
# ===============================================

Write-Host "════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🔍 DIAGNÓSTICO - ListView vacía en DiarioPage" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# 1. Verificar configuración
Write-Host "1️⃣ Verificando configuración..." -ForegroundColor Yellow
$config = Get-Content "appsettings.json" -Raw | ConvertFrom-Json
Write-Host "   BaseUrl: $($config.Api.BaseUrl)" -ForegroundColor White
Write-Host "   PartesPath: $($config.Api.PartesPath)" -ForegroundColor White
Write-Host ""

# 2. Verificar que la API responde
Write-Host "2️⃣ Verificando conexión con API..." -ForegroundColor Yellow
try {
    $healthCheck = Invoke-RestMethod -Uri "http://localhost:2501/api/v1/health" -Method Get -TimeoutSec 3 -ErrorAction Stop
    Write-Host "   ✅ API responde correctamente" -ForegroundColor Green
} catch {
    Write-Host "   ❌ API NO responde en http://localhost:2501" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Gray
    exit 1
}
Write-Host ""

# 3. Verificar endpoint de partes (SIN autenticación)
Write-Host "3️⃣ Probando endpoint /api/v1/partes (sin auth)..." -ForegroundColor Yellow
try {
    $partesResponse = Invoke-RestMethod -Uri "http://localhost:2501/api/v1/partes?limit=5" -Method Get -TimeoutSec 5 -ErrorAction Stop
    Write-Host "   ✅ Endpoint responde" -ForegroundColor Green
    Write-Host "   Partes devueltos: $($partesResponse.Count)" -ForegroundColor White
    
    if ($partesResponse.Count -gt 0) {
        $primer = $partesResponse[0]
        Write-Host ""
        Write-Host "   📄 Primer parte:" -ForegroundColor Cyan
        Write-Host "      ID: $($primer.id)" -ForegroundColor Gray
        Write-Host "      Fecha: $($primer.fecha)" -ForegroundColor Gray
        Write-Host "      Cliente: $($primer.cliente)" -ForegroundColor Gray
        Write-Host "      Acción: $($primer.accion.Substring(0, [Math]::Min(50, $primer.accion.Length)))..." -ForegroundColor Gray
    }
} catch {
    if ($_.Exception.Response.StatusCode -eq 401) {
        Write-Host "   ⚠️  Endpoint requiere autenticación (401)" -ForegroundColor Yellow
        Write-Host "   Esto es NORMAL - El Desktop debe hacer login primero" -ForegroundColor Gray
    } else {
        Write-Host "   ❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}
Write-Host ""

# 4. Verificar logs de la aplicación
Write-Host "4️⃣ Buscando logs de la aplicación..." -ForegroundColor Yellow
$logFiles = Get-ChildItem "logs" -File -ErrorAction SilentlyContinue | 
            Sort-Object LastWriteTime -Descending | 
            Select-Object -First 1

if ($logFiles) {
    Write-Host "   ✅ Log encontrado: $($logFiles.Name)" -ForegroundColor Green
    Write-Host "   Última modificación: $($logFiles.LastWriteTime)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "   📋 Últimas líneas (errores):" -ForegroundColor Cyan
    Get-Content $logFiles.FullName -Tail 30 | Select-String -Pattern "error|fail|exception" -Context 0 | Select-Object -First 5
} else {
    Write-Host "   ⚠️  No se encontraron logs" -ForegroundColor Yellow
    Write-Host "   La aplicación puede no haberse ejecutado aún" -ForegroundColor Gray
}
Write-Host ""

Write-Host "════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✅ DIAGNÓSTICO COMPLETADO" -ForegroundColor Green
Write-Host "════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "🔎 POSIBLES CAUSAS:" -ForegroundColor Yellow
Write-Host "   1. No has hecho login → La ListView se carga DESPUÉS del login" -ForegroundColor Gray
Write-Host "   2. Token expirado → Cierra y vuelve a abrir la app" -ForegroundColor Gray
Write-Host "   3. Error en la respuesta del backend → Verifica logs" -ForegroundColor Gray
Write-Host "   4. Problema de binding → Verifica que LvPartes.ItemsSource esté bindeado" -ForegroundColor Gray
Write-Host ""
Write-Host "💡 SIGUIENTE PASO:" -ForegroundColor Cyan
Write-Host "   1. Ejecuta la app: dotnet run" -ForegroundColor White
Write-Host "   2. Haz login" -ForegroundColor White
Write-Host "   3. Ve a DiarioPage" -ForegroundColor White
Write-Host "   4. Verifica los logs en logs\app.log" -ForegroundColor White
Write-Host ""
