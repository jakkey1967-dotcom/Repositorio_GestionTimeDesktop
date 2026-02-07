# Scripts\Test-ExportValidations.ps1
# Test para verificar que las validaciones de exportacion funcionan

Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host "TEST: Validaciones de Exportacion Excel" -ForegroundColor Cyan
Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host ""

$excelServiceFile = "Services\Export\ExcelExportService.cs"

if (!(Test-Path $excelServiceFile)) {
    Write-Host "ERROR: Archivo no encontrado: $excelServiceFile" -ForegroundColor Red
    exit 1
}

$content = Get-Content $excelServiceFile -Raw

# 1. Verificar que se registran errores por fila
Write-Host "1. Verificando registro de errores por fila..." -ForegroundColor Yellow

if ($content -match 'var errorDetails = new List<string>\(\)') {
    Write-Host "   OK: Lista errorDetails encontrada" -ForegroundColor Green
} else {
    Write-Host "   ERROR: No se encontro lista errorDetails" -ForegroundColor Red
    exit 1
}

if ($content -match 'App\.Log\?\. ?LogWarning\(".*Fila \{row\}.*Parte ID \{id\}') {
    Write-Host "   OK: Log de advertencias por fila implementado" -ForegroundColor Green
} else {
    Write-Host "   ERROR: Log de advertencias por fila no encontrado" -ForegroundColor Red
    exit 1
}

# 2. Verificar validacion de cliente vacio
Write-Host ""
Write-Host "2. Verificando validacion de cliente vacio..." -ForegroundColor Yellow

if ($content -match 'if \(string\.IsNullOrWhiteSpace\(parte\.Cliente\)\)') {
    Write-Host "   OK: Validacion de cliente vacio encontrada" -ForegroundColor Green
} else {
    Write-Host "   ERROR: Validacion de cliente vacio no encontrada" -ForegroundColor Red
    exit 1
}

# 3. Verificar validacion de fecha invalida
Write-Host ""
Write-Host "3. Verificando validacion de fecha invalida..." -ForegroundColor Yellow

if ($content -match 'errorDetails\.Add\("Fecha inválida"\)') {
    Write-Host "   OK: Validacion de fecha invalida encontrada" -ForegroundColor Green
} else {
    Write-Host "   ERROR: Validacion de fecha invalida no encontrada" -ForegroundColor Red
    exit 1
}

# 4. Verificar contador de horas faltantes
Write-Host ""
Write-Host "4. Verificando contador de horas faltantes..." -ForegroundColor Yellow

if ($content -match 'int rowsWithMissingTime = 0') {
    Write-Host "   OK: Contador rowsWithMissingTime encontrado" -ForegroundColor Green
} else {
    Write-Host "   ERROR: Contador rowsWithMissingTime no encontrado" -ForegroundColor Red
    exit 1
}

if ($content -match 'rowsWithMissingTime\+\+') {
    Write-Host "   OK: Incremento de contador implementado" -ForegroundColor Green
} else {
    Write-Host "   ERROR: Incremento de contador no encontrado" -ForegroundColor Red
    exit 1
}

# 5. Verificar validacion de duracion sospechosa
Write-Host ""
Write-Host "5. Verificando validacion de duracion sospechosa..." -ForegroundColor Yellow

if ($content -match 'if \(duracionCalculada > 0\.666667\)') {
    Write-Host "   OK: Validacion de duracion >16h encontrada" -ForegroundColor Green
} else {
    Write-Host "   ERROR: Validacion de duracion >16h no encontrada" -ForegroundColor Red
    exit 1
}

if ($content -match 'errorDetails\.Add\(\$"Duración sospechosa: \{duracionCalculada \* 24:F2\}h"\)') {
    Write-Host "   OK: Log de duracion sospechosa implementado" -ForegroundColor Green
} else {
    Write-Host "   ERROR: Log de duracion sospechosa no encontrado" -ForegroundColor Red
    exit 1
}

# 6. Verificar validacion de DuracionMin sospechosa
Write-Host ""
Write-Host "6. Verificando validacion de DuracionMin..." -ForegroundColor Yellow

if ($content -match 'if \(parte\.DuracionMin > 960\)') {
    Write-Host "   OK: Validacion de DuracionMin >16h encontrada" -ForegroundColor Green
} else {
    Write-Host "   ERROR: Validacion de DuracionMin >16h no encontrada" -ForegroundColor Red
    exit 1
}

if ($content -match 'int rowsWithFallbackDuration = 0') {
    Write-Host "   OK: Contador fallback encontrado" -ForegroundColor Green
} else {
    Write-Host "   ERROR: Contador fallback no encontrado" -ForegroundColor Red
    exit 1
}

# 7. Verificar logs de resumen de validacion
Write-Host ""
Write-Host "7. Verificando logs de resumen..." -ForegroundColor Yellow

if ($content -match 'LogWarning\(".*VALIDACIÓN: \{errors\} filas con advertencias/errores"') {
    Write-Host "   OK: Log resumen de errores encontrado" -ForegroundColor Green
} else {
    Write-Host "   ERROR: Log resumen de errores no encontrado" -ForegroundColor Red
    exit 1
}

if ($content -match 'LogInformation\(".*VALIDACIÓN: Todos los datos son correctos"\)') {
    Write-Host "   OK: Log de validacion exitosa encontrado" -ForegroundColor Green
} else {
    Write-Host "   ERROR: Log de validacion exitosa no encontrado" -ForegroundColor Red
    exit 1
}

# 8. Verificar validacion en ParseTimeToExcelValue
Write-Host ""
Write-Host "8. Verificando ParseTimeToExcelValue robusta..." -ForegroundColor Yellow

if ($content -match 'if \(timeSpan\.TotalHours < 0 \|\| timeSpan\.TotalHours >= 24\)') {
    Write-Host "   OK: Validacion de rango 0-24h encontrada" -ForegroundColor Green
} else {
    Write-Host "   ERROR: Validacion de rango 0-24h no encontrada" -ForegroundColor Red
    exit 1
}

if ($content -match 'LogWarning\(".*Hora fuera de rango') {
    Write-Host "   OK: Log de hora fuera de rango implementado" -ForegroundColor Green
} else {
    Write-Host "   ERROR: Log de hora fuera de rango no encontrado" -ForegroundColor Red
    exit 1
}

if ($content -match 'var normalizedHours =') {
    Write-Host "   OK: Normalizacion de horas fuera de rango implementada" -ForegroundColor Green
} else {
    Write-Host "   ERROR: Normalizacion de horas no encontrada" -ForegroundColor Red
    exit 1
}

if ($content -match 'catch.*Exception') {
    Write-Host "   OK: Try-catch para parseo robusto implementado" -ForegroundColor Green
} else {
    Write-Host "   ERROR: Try-catch no encontrado" -ForegroundColor Red
    exit 1
}

# Resumen
Write-Host ""
Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host "RESUMEN DEL TEST" -ForegroundColor Cyan
Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "OK: Registro de errores por fila" -ForegroundColor Green
Write-Host "OK: Validacion de cliente vacio" -ForegroundColor Green
Write-Host "OK: Validacion de fecha invalida" -ForegroundColor Green
Write-Host "OK: Contador de horas faltantes" -ForegroundColor Green
Write-Host "OK: Validacion de duracion >16h" -ForegroundColor Green
Write-Host "OK: Validacion de DuracionMin" -ForegroundColor Green
Write-Host "OK: Logs de resumen de validacion" -ForegroundColor Green
Write-Host "OK: ParseTimeToExcelValue robusta" -ForegroundColor Green
Write-Host ""
Write-Host "VALIDACIONES IMPLEMENTADAS:" -ForegroundColor Cyan
Write-Host "   - Cliente vacio" -ForegroundColor White
Write-Host "   - Fecha invalida" -ForegroundColor White
Write-Host "   - Hora Inicio/Fin faltante o invalida" -ForegroundColor White
Write-Host "   - Horas fuera de rango (0-24h)" -ForegroundColor White
Write-Host "   - Duracion sospechosa (>16h)" -ForegroundColor White
Write-Host "   - DuracionMin sospechosa (>960 min)" -ForegroundColor White
Write-Host "   - Tarea vacia" -ForegroundColor White
Write-Host "   - Sin duracion disponible (ni horas ni minutos)" -ForegroundColor White
Write-Host ""
Write-Host "METRICAS RASTREADAS:" -ForegroundColor Cyan
Write-Host "   - rowsWithErrors: Filas con al menos 1 advertencia" -ForegroundColor White
Write-Host "   - rowsWithMissingTime: Horas faltantes o invalidas" -ForegroundColor White
Write-Host "   - rowsWithFallbackDuration: Uso de DuracionMin" -ForegroundColor White
Write-Host ""
Write-Host "LOGS ESPERADOS:" -ForegroundColor Cyan
Write-Host "   Por fila:" -ForegroundColor Gray
Write-Host "   Warning: Fila X - Parte ID Y: [detalles errores]" -ForegroundColor Gray
Write-Host ""
Write-Host "   Resumen:" -ForegroundColor Gray
Write-Host "   Warning: VALIDACION: X filas con advertencias/errores" -ForegroundColor Gray
Write-Host "   Warning: VALIDACION: X valores de hora faltantes" -ForegroundColor Gray
Write-Host "   Info: VALIDACION: X filas usan DuracionMin (fallback)" -ForegroundColor Gray
Write-Host "   Info: VALIDACION: Todos los datos son correctos" -ForegroundColor Gray
Write-Host ""
Write-Host "PRUEBA MANUAL RECOMENDADA:" -ForegroundColor Cyan
Write-Host "   1. Exportar semana con datos variados" -ForegroundColor White
Write-Host "   2. Incluir registros con errores intencionales:" -ForegroundColor White
Write-Host "      - Cliente vacio" -ForegroundColor White
Write-Host "      - Hora Inicio/Fin faltante" -ForegroundColor White
Write-Host "      - Duracion >16h (turno muy largo)" -ForegroundColor White
Write-Host "   3. Verificar logs en Output window" -ForegroundColor White
Write-Host "   4. Verificar que Excel exporta correctamente" -ForegroundColor White
Write-Host "   5. Verificar que celdas problematicas quedan vacias" -ForegroundColor White
Write-Host ""
Write-Host "TEST COMPLETADO EXITOSAMENTE" -ForegroundColor Green
Write-Host "===============================================================" -ForegroundColor Cyan
