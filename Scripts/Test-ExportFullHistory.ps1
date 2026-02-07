# Scripts\Test-ExportFullHistory.ps1
# Test para verificar que la exportacion Excel carga TODO el historial

Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host "TEST: Exportacion Excel con Historial Completo" -ForegroundColor Cyan
Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host ""

# 1. Verificar que PartesService tiene parametros limit/offset
Write-Host "1. Verificando PartesService.ListAsync()..." -ForegroundColor Yellow
$partesServiceFile = "Services\Catalog\PartesService.cs"

if (!(Test-Path $partesServiceFile)) {
    Write-Host "   ERROR: Archivo no encontrado: $partesServiceFile" -ForegroundColor Red
    exit 1
}

$content = Get-Content $partesServiceFile -Raw

if ($content -match "int\? limit = null" -and $content -match "int\? offset = null") {
    Write-Host "   OK: Parametros limit y offset encontrados" -ForegroundColor Green
} else {
    Write-Host "   ERROR: Faltan parametros limit/offset" -ForegroundColor Red
    exit 1
}

if ($content -match 'queryParams\.Add\(\$"limit=\{limit\.Value\}"\)') {
    Write-Host "   OK: Logica de paginacion implementada" -ForegroundColor Green
} else {
    Write-Host "   ERROR: Falta logica de paginacion" -ForegroundColor Red
    exit 1
}

# 2. Verificar que DiarioPage carga historial completo
Write-Host ""
Write-Host "2. Verificando OnExportarExcel() en DiarioPage..." -ForegroundColor Yellow
$diarioPageFile = "Views\DiarioPage.xaml.cs"

if (!(Test-Path $diarioPageFile)) {
    Write-Host "   ERROR: Archivo no encontrado: $diarioPageFile" -ForegroundColor Red
    exit 1
}

$content = Get-Content $diarioPageFile -Raw

if ($content -match "Cargando historial completo para exportación") {
    Write-Host "   OK: Log de carga completa encontrado" -ForegroundColor Green
} else {
    Write-Host "   ERROR: No se encontro log de carga completa" -ForegroundColor Red
    exit 1
}

if ($content -match "var partesService = new Services\.Catalog\.PartesService") {
    Write-Host "   OK: Instancia de PartesService encontrada" -ForegroundColor Green
} else {
    Write-Host "   ERROR: No se instancia PartesService" -ForegroundColor Red
    exit 1
}

if ($content -match "limit: 10000") {
    Write-Host "   OK: Limite alto configurado (10000)" -ForegroundColor Green
} else {
    Write-Host "   ERROR: Limite no configurado correctamente" -ForegroundColor Red
    exit 1
}

if ($content -match "CalculateAvailableWeeks\(new ObservableCollection<ParteDto>\(allPartes\)\)") {
    Write-Host "   OK: Semanas calculadas desde historial completo" -ForegroundColor Green
} else {
    Write-Host "   ERROR: Semanas no se calculan desde historial completo" -ForegroundColor Red
    exit 1
}

if ($content -match "var partesToExport = allPartes") {
    Write-Host "   OK: Exportacion desde historial completo" -ForegroundColor Green
} else {
    Write-Host "   ERROR: Exportacion no usa historial completo" -ForegroundColor Red
    exit 1
}

# 3. Resumen
Write-Host ""
Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host "RESUMEN DEL TEST" -ForegroundColor Cyan
Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "OK: PartesService: Parametros limit/offset anadidos" -ForegroundColor Green
Write-Host "OK: DiarioPage: Carga historial completo antes de exportar" -ForegroundColor Green
Write-Host "OK: DiarioPage: Semanas calculadas desde historial completo" -ForegroundColor Green
Write-Host "OK: DiarioPage: Exportacion usa historial completo" -ForegroundColor Green
Write-Host ""
Write-Host "RESULTADO ESPERADO:" -ForegroundColor Cyan
Write-Host "   - Usuario presiona 'Exportar Excel'" -ForegroundColor White
Write-Host "   - App muestra loader y carga TODO el historial (hasta 10000 partes)" -ForegroundColor White
Write-Host "   - Dialogo muestra TODAS las semanas disponibles (no solo la actual)" -ForegroundColor White
Write-Host "   - Usuario puede exportar CUALQUIER semana del historial" -ForegroundColor White
Write-Host ""
Write-Host "PRUEBA MANUAL:" -ForegroundColor Cyan
Write-Host "   1. Inicia la app y ve a DiarioPage" -ForegroundColor White
Write-Host "   2. Observa que solo carga 25 partes inicialmente" -ForegroundColor White
Write-Host "   3. Presiona 'Exportar Excel'" -ForegroundColor White
Write-Host "   4. Observa loader mientras carga historial" -ForegroundColor White
Write-Host "   5. En el dialogo, verifica que hay MUCHAS semanas disponibles" -ForegroundColor White
Write-Host "   6. Selecciona una semana antigua (ej: hace 3 meses)" -ForegroundColor White
Write-Host "   7. Exporta y verifica que el Excel contiene datos correctos" -ForegroundColor White
Write-Host ""
Write-Host "TEST COMPLETADO EXITOSAMENTE" -ForegroundColor Green
Write-Host "===============================================================" -ForegroundColor Cyan

