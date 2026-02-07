# ========================================================================
# Test-HoraInicioInteligente.ps1
# ========================================================================
# Script para validar que la hora de inicio de un nuevo parte
# se calcula correctamente desde la hora FIN del último parte del día.
# ========================================================================

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🧪 TEST: HORA DE INICIO INTELIGENTE - Nuevo Parte" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# ========================================================================
# PASO 1: Verificar que la aplicación esté funcionando
# ========================================================================

Write-Host "📋 PASO 1: Instrucciones para el test manual" -ForegroundColor Yellow
Write-Host ""
Write-Host "Este test requiere interacción manual con la aplicación." -ForegroundColor White
Write-Host ""

# ========================================================================
# PASO 2: Escenario de prueba
# ========================================================================

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "📝 ESCENARIO DE PRUEBA" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "1. Abre GestionTime Desktop" -ForegroundColor Green
Write-Host ""

Write-Host "2. Crea un parte HOY con estos datos:" -ForegroundColor Green
Write-Host "   • Cliente: 'Cliente Test'" -ForegroundColor White
Write-Host "   • Hora inicio: '08:30'" -ForegroundColor White
Write-Host "   • Hora fin: '10:00'" -ForegroundColor White
Write-Host "   • Guarda el parte" -ForegroundColor White
Write-Host ""

Write-Host "3. Presiona el botón 'Nuevo Parte'" -ForegroundColor Green
Write-Host ""

Write-Host "4. Verifica la hora de inicio del nuevo parte:" -ForegroundColor Green
Write-Host "   ✅ CORRECTO: Hora inicio = '10:00' (hora FIN del parte anterior)" -ForegroundColor Green
Write-Host "   ❌ INCORRECTO: Cualquier otra hora" -ForegroundColor Red
Write-Host ""

# ========================================================================
# PASO 3: Verificación de logs
# ========================================================================

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🔍 PASO 3: Verificar logs (opcional)" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

$logPath = Join-Path $env:USERPROFILE "AppData\Local\GestionTime\logs\app.log"

if (Test-Path $logPath) {
    Write-Host "📂 Archivo de logs encontrado: $logPath" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "Buscando logs relacionados con 'Nuevo parte'..." -ForegroundColor Yellow
    Write-Host ""
    
    # Obtener las últimas 50 líneas del log
    $logLines = Get-Content $logPath -Tail 100 | Where-Object { 
        $_ -match "📌 Nuevo parte" -or 
        $_ -match "📍 Hora heredada" -or
        $_ -match "PARTE_CREATE_ABIERTO"
    }
    
    if ($logLines) {
        Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
        Write-Host "📄 LOGS RELEVANTES (últimas 100 líneas)" -ForegroundColor Cyan
        Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
        Write-Host ""
        
        foreach ($line in $logLines) {
            if ($line -match "Usando hora FIN") {
                Write-Host $line -ForegroundColor Green
            }
            elseif ($line -match "SIN hora fin") {
                Write-Host $line -ForegroundColor Yellow
            }
            elseif ($line -match "hora actual") {
                Write-Host $line -ForegroundColor Cyan
            }
            else {
                Write-Host $line -ForegroundColor White
            }
        }
        
        Write-Host ""
        Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    }
    else {
        Write-Host "⚠️  No se encontraron logs relevantes en las últimas 100 líneas" -ForegroundColor Yellow
        Write-Host "    Intenta crear un nuevo parte y ejecuta este script nuevamente" -ForegroundColor White
    }
}
else {
    Write-Host "⚠️  Archivo de logs no encontrado: $logPath" -ForegroundColor Yellow
    Write-Host "    La aplicación debe ejecutarse al menos una vez para generar logs" -ForegroundColor White
}

Write-Host ""

# ========================================================================
# PASO 4: Casos de prueba adicionales
# ========================================================================

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🧪 CASOS DE PRUEBA ADICIONALES" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "Test 1: Parte cerrado (con hora fin)" -ForegroundColor Yellow
Write-Host "   1. Crea un parte: 08:00 - 10:00 (cerrado)" -ForegroundColor White
Write-Host "   2. Nuevo Parte → Esperado: hora inicio = 10:00" -ForegroundColor Green
Write-Host ""

Write-Host "Test 2: Parte abierto (sin hora fin)" -ForegroundColor Yellow
Write-Host "   1. Crea un parte: 10:30 - (vacío, sin cerrar)" -ForegroundColor White
Write-Host "   2. Nuevo Parte → Esperado: hora inicio = 10:30 (fallback)" -ForegroundColor Green
Write-Host ""

Write-Host "Test 3: Primer parte del día" -ForegroundColor Yellow
Write-Host "   1. Elimina todos los partes de hoy" -ForegroundColor White
Write-Host "   2. Nuevo Parte → Esperado: hora inicio = hora actual" -ForegroundColor Green
Write-Host ""

Write-Host "Test 4: Múltiples partes" -ForegroundColor Yellow
Write-Host "   1. Crea 3 partes: 08:00-10:00, 10:30-12:00, 14:00-16:00" -ForegroundColor White
Write-Host "   2. Nuevo Parte → Esperado: hora inicio = 16:00 (último)" -ForegroundColor Green
Write-Host ""

# ========================================================================
# RESUMEN
# ========================================================================

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✅ CRITERIOS DE ÉXITO" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "El fix es CORRECTO si:" -ForegroundColor Green
Write-Host "  ✅ Nuevo parte usa hora FIN del último parte cerrado" -ForegroundColor Green
Write-Host "  ✅ Si el último parte está abierto, usa su hora INICIO" -ForegroundColor Green
Write-Host "  ✅ Si no hay partes hoy, usa hora actual del sistema" -ForegroundColor Green
Write-Host "  ✅ Los logs muestran claramente cuál hora se está usando" -ForegroundColor Green
Write-Host ""

Write-Host "El fix es INCORRECTO si:" -ForegroundColor Red
Write-Host "  ❌ Nuevo parte usa hora de INICIO del último parte (en lugar de FIN)" -ForegroundColor Red
Write-Host "  ❌ Siempre usa la hora actual (ignorando partes existentes)" -ForegroundColor Red
Write-Host "  ❌ La hora calculada es incorrecta o inconsistente" -ForegroundColor Red
Write-Host ""

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🏁 FIN DEL TEST" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "Presiona cualquier tecla para abrir los logs completos..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

if (Test-Path $logPath) {
    notepad $logPath
}
else {
    Write-Host "⚠️  No se puede abrir el archivo de logs (no existe)" -ForegroundColor Yellow
}
