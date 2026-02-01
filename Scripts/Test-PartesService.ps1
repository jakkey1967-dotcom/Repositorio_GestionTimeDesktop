# ===============================================
# TEST PARTES SERVICE - Verificar filtros avanzados
# ===============================================

Write-Host "╔══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "║ TEST PARTES SERVICE - FILTROS AVANZADOS" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════" -ForegroundColor Cyan

$baseUrl = "https://localhost:2502/api/v1"
$EMAIL = "psantos@global-retail.com"
$PASSWORD = "12345678"

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

# 1. LOGIN
Write-Host "`n🔐 [1/10] Login..." -ForegroundColor Cyan
try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/auth/login-desktop" -Method POST -ContentType "application/json" -Body (@{Email=$EMAIL;Password=$PASSWORD} | ConvertTo-Json)
    $headers = @{"Authorization"="Bearer $($loginResponse.accessToken)"}
    Write-Host "✅ Token obtenido" -ForegroundColor Green
} catch {
    Write-Host "❌ Login falló: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 2. LISTAR TODOS (sin filtros)
Write-Host "`n📋 [2/10] GET /partes (sin filtros)..." -ForegroundColor Cyan
try {
    $partes = Invoke-RestMethod -Uri "$baseUrl/partes" -Method GET -Headers $headers
    Write-Host "✅ Total: $($partes.Count) partes" -ForegroundColor Green
    if ($partes.Count -gt 0) {
        $primer = $partes[0]
        Write-Host "   • Primer parte: ID=$($primer.id), Cliente=$($primer.cliente), Fecha=$($primer.fecha)" -ForegroundColor Gray
    }
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}

# 3. FILTRO POR FECHA ESPECÍFICA (hoy)
$hoy = (Get-Date).ToString("yyyy-MM-dd")
Write-Host "`n📅 [3/10] GET /partes?fecha=$hoy..." -ForegroundColor Cyan
try {
    $partes = Invoke-RestMethod -Uri "$baseUrl/partes?fecha=$hoy" -Method GET -Headers $headers
    Write-Host "✅ Partes de hoy: $($partes.Count)" -ForegroundColor Green
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}

# 4. FILTRO POR RANGO DE FECHAS (últimos 7 días)
$hace7dias = (Get-Date).AddDays(-7).ToString("yyyy-MM-dd")
Write-Host "`n📆 [4/10] GET /partes?fechaInicio=$hace7dias&fechaFin=$hoy..." -ForegroundColor Cyan
try {
    $partes = Invoke-RestMethod -Uri "$baseUrl/partes?fechaInicio=$hace7dias&fechaFin=$hoy" -Method GET -Headers $headers
    Write-Host "✅ Partes últimos 7 días: $($partes.Count)" -ForegroundColor Green
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}

# 5. BÚSQUEDA POR TEXTO
Write-Host "`n🔍 [5/10] GET /partes?q=test..." -ForegroundColor Cyan
try {
    $partes = Invoke-RestMethod -Uri "$baseUrl/partes?q=test" -Method GET -Headers $headers
    Write-Host "✅ Búsqueda 'test': $($partes.Count) resultados" -ForegroundColor Green
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}

# 6. FILTRO POR ESTADO (Cerrado = 2)
Write-Host "`n✅ [6/10] GET /partes?estado=2..." -ForegroundColor Cyan
try {
    $partes = Invoke-RestMethod -Uri "$baseUrl/partes?estado=2" -Method GET -Headers $headers
    Write-Host "✅ Partes cerrados: $($partes.Count)" -ForegroundColor Green
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}

# 7. CREAR PARTE
Write-Host "`n📝 [7/10] POST /partes - Crear parte..." -ForegroundColor Cyan
$newParte = @{
    fecha_trabajo = $hoy
    hora_inicio = "09:00"
    hora_fin = "10:00"
    id_cliente = 1
    tienda = "Test"
    id_grupo = 1
    id_tipo = 1
    accion = "Test desde script PowerShell"
    ticket = "TICKET-$(Get-Random -Maximum 9999)"
}

try {
    $creado = Invoke-RestMethod -Uri "$baseUrl/partes" -Method POST -Headers $headers -Body ($newParte | ConvertTo-Json) -ContentType "application/json"
    Write-Host "✅ Parte creado: ID=$($creado.id)" -ForegroundColor Green
    $parteId = $creado.id
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    $parteId = 0
}

if ($parteId -gt 0) {
    # 8. OBTENER POR ID
    Write-Host "`n🔍 [8/10] GET /partes/$parteId..." -ForegroundColor Cyan
    try {
        $parte = Invoke-RestMethod -Uri "$baseUrl/partes/$parteId" -Method GET -Headers $headers
        Write-Host "✅ Parte obtenido: Cliente=$($parte.cliente)" -ForegroundColor Green
    } catch {
        Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    }

    # 9. CERRAR PARTE
    Write-Host "`n✅ [9/10] POST /partes/$parteId/cerrar..." -ForegroundColor Cyan
    try {
        Invoke-RestMethod -Uri "$baseUrl/partes/$parteId/cerrar" -Method POST -Headers $headers
        Write-Host "✅ Parte cerrado" -ForegroundColor Green
    } catch {
        Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    }

    # 10. ELIMINAR PARTE
    Write-Host "`n🗑️  [10/10] DELETE /partes/$parteId..." -ForegroundColor Cyan
    try {
        Invoke-RestMethod -Uri "$baseUrl/partes/$parteId" -Method DELETE -Headers $headers
        Write-Host "✅ Parte eliminado" -ForegroundColor Green
    } catch {
        Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`n╔══════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "║ ✅ TESTS COMPLETADOS" -ForegroundColor Green
Write-Host "╚══════════════════════════════════════════════════════════════" -ForegroundColor Green

Write-Host "`n💡 RESUMEN:" -ForegroundColor Yellow
Write-Host "   • PartesService creado con soporte para todos los filtros" -ForegroundColor White
Write-Host "   • Filtros por fecha: fecha, fechaInicio, fechaFin" -ForegroundColor White
Write-Host "   • Filtros por IDs: idCliente, idTipo, idGrupo" -ForegroundColor White
Write-Host "   • Búsqueda por texto: search (parámetro 'q' en API)" -ForegroundColor White
Write-Host "   • Filtro por estado: 0=Abierto, 1=Pausado, 2=Cerrado, 3=Enviado, 9=Anulado" -ForegroundColor White
Write-Host "   • Acciones: Crear, Actualizar, Cerrar, Enviar, Anular, Eliminar" -ForegroundColor White
Write-Host ""
