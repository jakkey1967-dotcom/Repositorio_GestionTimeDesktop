# Script de verificación: Bug Clientes Post-Guardado
# Este script ayuda a verificar que el bug está corregido

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✅ VERIFICACIÓN: Bug Clientes Post-Guardado" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "📋 Pasos de verificación manual:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. Abrir GestionTime Desktop" -ForegroundColor White
Write-Host "2. Login con usuario admin" -ForegroundColor White
Write-Host "3. Ir a Configuración > Clientes" -ForegroundColor White
Write-Host ""
Write-Host "4. ✏️ EDITAR un cliente:" -ForegroundColor Cyan
Write-Host "   - Click en cualquier tarjeta de cliente" -ForegroundColor Gray
Write-Host "   - Modificar la nota (añadir '1' al final)" -ForegroundColor Gray
Write-Host "   - Pulsar 'Guardar'" -ForegroundColor Gray
Write-Host "   - Verificar que se cierra el panel" -ForegroundColor Gray
Write-Host ""
Write-Host "5. ✅ VERIFICAR que funciona después:" -ForegroundColor Green
Write-Host "   a) Click en OTRA tarjeta de cliente" -ForegroundColor Gray
Write-Host "      → Debe abrir el panel de edición" -ForegroundColor Green
Write-Host "   b) Cerrar el panel (X)" -ForegroundColor Gray
Write-Host "   c) Click en el botón 'Nuevo'" -ForegroundColor Gray
Write-Host "      → Debe abrir el panel para crear nuevo" -ForegroundColor Green
Write-Host ""
Write-Host "6. 🔍 REVISAR logs en Output Window:" -ForegroundColor Yellow
Write-Host "   Buscar estas líneas:" -ForegroundColor Gray
Write-Host ""
Write-Host "   ✅ Después de guardar:" -ForegroundColor Gray
Write-Host "      CLIENTES_UI OnSaveClienteClick | END | button.IsEnabled=true" -ForegroundColor DarkGray
Write-Host ""
Write-Host "   ✅ Al editar otro cliente:" -ForegroundColor Gray
Write-Host "      CLIENTES_UI OnClienteCardClick | clienteId=X" -ForegroundColor DarkGray
Write-Host "      CLIENTES_UI ShowClienteEditPanel | END | editPanel.IsVisible=True" -ForegroundColor DarkGray
Write-Host ""
Write-Host "   ✅ Al crear nuevo:" -ForegroundColor Gray
Write-Host "      CLIENTES_UI btnNewCliente_Click | START" -ForegroundColor DarkGray
Write-Host "      CLIENTES_UI ShowClienteEditPanel | END | editPanel.IsVisible=True" -ForegroundColor DarkGray
Write-Host ""
Write-Host "   ❌ NO debe aparecer:" -ForegroundColor Gray
Write-Host "      CLIENTES_UI ShowClienteEditPanel | editPanel NOT FOUND" -ForegroundColor Red
Write-Host ""

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🔧 Solución Aplicada" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. ✅ editPanel.Tag NO se limpia al cerrar" -ForegroundColor Green
Write-Host "2. ✅ editPanel.Name = 'ClienteEditPanel' (fallback)" -ForegroundColor Green
Write-Host "3. ✅ Búsqueda multi-método del editPanel" -ForegroundColor Green
Write-Host "4. ✅ Logging exhaustivo (40+ logs)" -ForegroundColor Green
Write-Host ""

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "📄 Documentación" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "- Docs\FIX_CLIENTES_BUG_POST_GUARDADO_LOGGING.md" -ForegroundColor Gray
Write-Host "- Docs\SOLUCION_BUG_CLIENTES_POST_GUARDADO.md" -ForegroundColor Gray
Write-Host ""

# Preguntar al usuario si quiere ver los logs
Write-Host "¿Deseas buscar logs de diagnóstico en el archivo actual? (S/N): " -ForegroundColor Yellow -NoNewline
$response = Read-Host

if ($response -eq 'S' -or $response -eq 's') {
    Write-Host ""
    Write-Host "Buscando logs en archivos .log más recientes..." -ForegroundColor Cyan
    
    $logFiles = Get-ChildItem -Path "." -Filter "*.log" -Recurse -ErrorAction SilentlyContinue | 
                Sort-Object LastWriteTime -Descending | 
                Select-Object -First 3
    
    if ($logFiles.Count -gt 0) {
        Write-Host ""
        Write-Host "Archivos de log encontrados:" -ForegroundColor Green
        $logFiles | ForEach-Object { Write-Host "  - $($_.FullName)" -ForegroundColor Gray }
        
        Write-Host ""
        Write-Host "Buscando 'CLIENTES_UI ShowClienteEditPanel | editPanel NOT FOUND'..." -ForegroundColor Yellow
        
        $foundErrors = $false
        foreach ($logFile in $logFiles) {
            $errors = Select-String -Path $logFile.FullName -Pattern "editPanel NOT FOUND" -Context 2,2
            if ($errors) {
                $foundErrors = $true
                Write-Host ""
                Write-Host "❌ Encontrado en: $($logFile.Name)" -ForegroundColor Red
                $errors | ForEach-Object { Write-Host $_.Line -ForegroundColor Red }
            }
        }
        
        if (-not $foundErrors) {
            Write-Host ""
            Write-Host "✅ NO se encontró 'editPanel NOT FOUND' - Bug corregido" -ForegroundColor Green
        }
    }
    else {
        Write-Host ""
        Write-Host "⚠️ No se encontraron archivos de log recientes" -ForegroundColor Yellow
        Write-Host "   Ejecuta la aplicación y vuelve a intentarlo" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✅ Verificación completada" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
