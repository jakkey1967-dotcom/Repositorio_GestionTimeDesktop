# Scripts\Test-ExcelDurationSummable.ps1
# Test para verificar que la columna DURACION es sumable en Excel

Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host "TEST: Columna DURACION Sumable en Excel" -ForegroundColor Cyan
Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host ""

# 1. Verificar que ParseTimeToExcelValue existe
Write-Host "1. Verificando funcion ParseTimeToExcelValue()..." -ForegroundColor Yellow
$excelServiceFile = "Services\Export\ExcelExportService.cs"

if (!(Test-Path $excelServiceFile)) {
    Write-Host "   ERROR: Archivo no encontrado: $excelServiceFile" -ForegroundColor Red
    exit 1
}

$content = Get-Content $excelServiceFile -Raw

if ($content -match "ParseTimeToExcelValue") {
    Write-Host "   OK: Funcion ParseTimeToExcelValue encontrada" -ForegroundColor Green
} else {
    Write-Host "   ERROR: Funcion ParseTimeToExcelValue no encontrada" -ForegroundColor Red
    exit 1
}

# 2. Verificar que usa fórmulas para duración
Write-Host ""
Write-Host "2. Verificando formula IF para duracion..." -ForegroundColor Yellow

if ($content -match 'FormulaA1 = \$"=IF\(D\{row\}<C\{row\}') {
    Write-Host "   OK: Formula IF para cruce de medianoche encontrada" -ForegroundColor Green
} else {
    Write-Host "   ERROR: Formula de duracion no encontrada" -ForegroundColor Red
    exit 1
}

# 3. Verificar formato [h]:mm:ss
Write-Host ""
Write-Host "3. Verificando formato [h]:mm:ss..." -ForegroundColor Yellow

if ($content -match '\[h\]:mm:ss') {
    Write-Host "   OK: Formato [h]:mm:ss aplicado (permite >24h)" -ForegroundColor Green
} else {
    Write-Host "   ERROR: Formato [h]:mm:ss no encontrado" -ForegroundColor Red
    exit 1
}

# 4. Verificar fila TOTAL
Write-Host ""
Write-Host "4. Verificando fila TOTAL..." -ForegroundColor Yellow

if ($content -match '=SUM\(E\{firstDataRow\}:E\{lastDataRow\}\)') {
    Write-Host "   OK: Fila TOTAL con SUM encontrada" -ForegroundColor Green
} else {
    Write-Host "   ERROR: Fila TOTAL no encontrada" -ForegroundColor Red
    exit 1
}

# 5. Verificar auto-cálculo
Write-Host ""
Write-Host "5. Verificando auto-calculo..." -ForegroundColor Yellow

if ($content -match 'CalculateMode = XLCalculateMode.Auto') {
    Write-Host "   OK: Auto-calculo configurado" -ForegroundColor Green
} else {
    Write-Host "   ERROR: Auto-calculo no configurado" -ForegroundColor Red
    exit 1
}

# 6. Verificar que Hora Inicio/Fin usan valores numéricos
Write-Host ""
Write-Host "6. Verificando valores numericos para horas..." -ForegroundColor Yellow

if ($content -match 'worksheet\.Cell\(row, 3\)\.Value = horaInicio\.Value') {
    Write-Host "   OK: Hora Inicio como valor numerico" -ForegroundColor Green
} else {
    Write-Host "   ERROR: Hora Inicio no es valor numerico" -ForegroundColor Red
    exit 1
}

if ($content -match 'worksheet\.Cell\(row, 4\)\.Value = horaFin\.Value') {
    Write-Host "   OK: Hora Fin como valor numerico" -ForegroundColor Green
} else {
    Write-Host "   ERROR: Hora Fin no es valor numerico" -ForegroundColor Red
    exit 1
}

# 7. Verificar formato HH:mm para horas
Write-Host ""
Write-Host "7. Verificando formato HH:mm para horas..." -ForegroundColor Yellow

if ($content -match 'NumberFormat\.Format = "HH:mm"') {
    Write-Host "   OK: Formato HH:mm aplicado a horas" -ForegroundColor Green
} else {
    Write-Host "   ERROR: Formato HH:mm no encontrado" -ForegroundColor Red
    exit 1
}

# Resumen
Write-Host ""
Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host "RESUMEN DEL TEST" -ForegroundColor Cyan
Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "OK: ParseTimeToExcelValue() implementada" -ForegroundColor Green
Write-Host "OK: Formula IF para cruce de medianoche" -ForegroundColor Green
Write-Host "OK: Formato [h]:mm:ss en DURACION" -ForegroundColor Green
Write-Host "OK: Fila TOTAL con SUM()" -ForegroundColor Green
Write-Host "OK: Auto-calculo configurado" -ForegroundColor Green
Write-Host "OK: Hora Inicio/Fin como valores numericos" -ForegroundColor Green
Write-Host "OK: Formato HH:mm en horas" -ForegroundColor Green
Write-Host ""
Write-Host "RESULTADO ESPERADO:" -ForegroundColor Cyan
Write-Host "   - Hora Inicio/Fin: valores de tiempo reales, no texto" -ForegroundColor White
Write-Host "   - Duracion: formula Excel =IF(Fin<Inicio,Fin+1-Inicio,Fin-Inicio)" -ForegroundColor White
Write-Host "   - Formato [h]:mm:ss permite mostrar >24 horas" -ForegroundColor White
Write-Host "   - Fila TOTAL suma automaticamente todas las duraciones" -ForegroundColor White
Write-Host "   - Excel recalcula al abrir (sin intervencion manual)" -ForegroundColor White
Write-Host ""
Write-Host "PRUEBA MANUAL:" -ForegroundColor Cyan
Write-Host "   1. Exporta una semana con varios registros" -ForegroundColor White
Write-Host "   2. Incluye un turno nocturno (23:00 - 01:00)" -ForegroundColor White
Write-Host "   3. Abre el Excel (sin hacer nada manual)" -ForegroundColor White
Write-Host "   4. Verifica que DURACION muestra valores correctos" -ForegroundColor White
Write-Host "   5. Verifica que turno nocturno = 2:00:00 (no -22:00:00)" -ForegroundColor White
Write-Host "   6. Verifica que TOTAL suma correctamente" -ForegroundColor White
Write-Host "   7. Anade una fila manualmente y verifica suma automatica" -ForegroundColor White
Write-Host ""
Write-Host "CASOS DE PRUEBA:" -ForegroundColor Cyan
Write-Host "   - Normal: 08:30 - 10:00 = 1:30:00" -ForegroundColor White
Write-Host "   - Medianoche: 23:00 - 01:00 = 2:00:00" -ForegroundColor White
Write-Host "   - Jornada larga: 08:00 - 20:00 = 12:00:00" -ForegroundColor White
Write-Host "   - Total >24h: 5 jornadas x 8h = 40:00:00 (no 16:00:00)" -ForegroundColor White
Write-Host ""
Write-Host "TEST COMPLETADO EXITOSAMENTE" -ForegroundColor Green
Write-Host "===============================================================" -ForegroundColor Cyan
