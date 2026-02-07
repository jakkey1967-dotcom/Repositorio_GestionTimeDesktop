# ⚡ VERIFICACIÓN RÁPIDA: Fix Invalidación de Caché

Write-Host ""
Write-Host "========================================" -ForegroundColor Red
Write-Host "  ⚠️  APLICACIÓN EN DEBUGGING ACTIVO" -ForegroundColor Red
Write-Host "========================================" -ForegroundColor Red
Write-Host ""

Write-Host "❌ LOS CAMBIOS EN ApiClient.cs NO SE HAN APLICADO AÚN" -ForegroundColor Yellow
Write-Host ""
Write-Host "Razón: La aplicación está ejecutándose en modo debugging." -ForegroundColor Gray
Write-Host "El código antiguo (con bug) sigue en memoria." -ForegroundColor Gray
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  PASOS OBLIGATORIOS ANTES DE PROBAR" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "1️⃣  DETENER DEBUGGING" -ForegroundColor Green
Write-Host "   - En Visual Studio: Pulsar 'Stop' (Shift+F5)" -ForegroundColor Gray
Write-Host "   - O cerrar la aplicación GestionTime Desktop" -ForegroundColor Gray
Write-Host ""

Write-Host "2️⃣  RECOMPILAR PROYECTO" -ForegroundColor Green
Write-Host "   - En Visual Studio: Build → Rebuild Solution (Ctrl+Shift+B)" -ForegroundColor Gray
Write-Host "   - Verificar que la compilación sea exitosa" -ForegroundColor Gray
Write-Host ""

Write-Host "3️⃣  INICIAR DE NUEVO" -ForegroundColor Green
Write-Host "   - En Visual Studio: Debug → Start Debugging (F5)" -ForegroundColor Gray
Write-Host "   - O ejecutar GestionTime.Desktop.exe manualmente" -ForegroundColor Gray
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  VERIFICACIÓN DEL FIX" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "4️⃣  PROBAR EDICIÓN DE CLIENTE" -ForegroundColor Green
Write-Host "   - Ir a Settings → Clientes" -ForegroundColor Gray
Write-Host "   - Editar cualquier cliente (cambiar nombre)" -ForegroundColor Gray
Write-Host "   - Pulsar 'Guardar'" -ForegroundColor Gray
Write-Host "   - Pulsar 'Aceptar' en el diálogo de éxito" -ForegroundColor Gray
Write-Host ""

Write-Host "5️⃣  VERIFICAR LOGS INMEDIATAMENTE" -ForegroundColor Green
Write-Host "   Buscar en los últimos logs:" -ForegroundColor Gray
Write-Host ""
Write-Host '   $logFile = Get-ChildItem "Data\logs\GestionTime-*.log" | Sort-Object LastWriteTime | Select-Object -Last 1' -ForegroundColor Cyan
Write-Host '   Select-String -Path $logFile.FullName -Pattern "entrada.*caché invalidadas" | Select-Object -Last 5' -ForegroundColor Cyan
Write-Host ""

Write-Host "6️⃣  LOGS ESPERADOS (DESPUÉS DEL FIX)" -ForegroundColor Green
Write-Host ""
Write-Host "   ✅ CORRECTO (con el nuevo código):" -ForegroundColor Yellow
Write-Host '      "✅ 1 entrada(s) de caché invalidadas para: /api/v1/clientes"' -ForegroundColor Green
Write-Host '      "🗑️ Entrada de caché invalidada: /api/v1/clientes?page=1&size=50"' -ForegroundColor Green
Write-Host '      "HTTP GET /api/v1/clientes?page=1&size=50 -> 200 en XXms"' -ForegroundColor Green
Write-Host '      (SIN el mensaje "💾 Usando CACHÉ")' -ForegroundColor Green
Write-Host ""
Write-Host "   ❌ INCORRECTO (todavía usa código antiguo):" -ForegroundColor Yellow
Write-Host '      "CLIENTES_UI OnSaveClienteClick | invalidating cache for /api/v1/clientes..."' -ForegroundColor Red
Write-Host '      "💾 GET /api/v1/clientes?page=1&size=50 - Usando CACHÉ (edad: XX.Xs)"' -ForegroundColor Red
Write-Host '      (NO aparece mensaje "entrada(s) de caché invalidadas")' -ForegroundColor Red
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  COMPORTAMIENTO ESPERADO" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "✅ Los cambios en el nombre del cliente aparecen INMEDIATAMENTE en la lista" -ForegroundColor Green
Write-Host "✅ NO es necesario cerrar/reabrir SettingsWindow" -ForegroundColor Green
Write-Host "✅ NO es necesario recargar manualmente la lista" -ForegroundColor Green
Write-Host "✅ Los logs muestran 'N entrada(s) de caché invalidadas'" -ForegroundColor Green
Write-Host "✅ Los logs NO muestran 'Usando CACHÉ' después de guardar" -ForegroundColor Green
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  SI EL FIX NO FUNCIONA" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "❌ Verificar que se recompiló correctamente:" -ForegroundColor Yellow
Write-Host '   - Buscar en Output de Visual Studio: "Build succeeded"' -ForegroundColor Gray
Write-Host '   - Verificar que no hay errores de compilación' -ForegroundColor Gray
Write-Host ""

Write-Host "❌ Verificar que la app se reinició:" -ForegroundColor Yellow
Write-Host '   - Cerrar TODAS las instancias de GestionTime.Desktop.exe' -ForegroundColor Gray
Write-Host '   - Verificar en Task Manager (Administrador de tareas)' -ForegroundColor Gray
Write-Host '   - Iniciar de nuevo desde Visual Studio o el exe' -ForegroundColor Gray
Write-Host ""

Write-Host "❌ Recopilar logs completos:" -ForegroundColor Yellow
Write-Host '   $logFile = Get-ChildItem "Data\logs\GestionTime-*.log" | Sort-Object LastWriteTime | Select-Object -Last 1' -ForegroundColor Cyan
Write-Host '   Select-String -Path $logFile.FullName -Pattern "CLIENTES_UI" | Select-Object -Last 100 | Out-File "debug_logs.txt"' -ForegroundColor Cyan
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "📝 RESUMEN:" -ForegroundColor Yellow
Write-Host "  1. DETENER app" -ForegroundColor Gray
Write-Host "  2. RECOMPILAR (Ctrl+Shift+B)" -ForegroundColor Gray
Write-Host "  3. INICIAR de nuevo (F5)" -ForegroundColor Gray
Write-Host "  4. PROBAR edición de cliente" -ForegroundColor Gray
Write-Host "  5. VERIFICAR logs (debe aparecer 'entrada(s) de caché invalidadas')" -ForegroundColor Gray
Write-Host ""

Write-Host "⏰ Tiempo estimado: 2-3 minutos" -ForegroundColor Cyan
Write-Host ""
