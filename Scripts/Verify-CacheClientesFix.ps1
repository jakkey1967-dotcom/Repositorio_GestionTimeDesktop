# ✅ Script de Verificación: Fix Caché Clientes

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  VERIFICACIÓN: Fix Caché Clientes" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "📋 PASOS PARA VERIFICAR EL FIX:" -ForegroundColor Yellow
Write-Host ""

Write-Host "1️⃣  ABRIR SETTINGS → CLIENTES" -ForegroundColor Green
Write-Host "   - Abrir GestionTime Desktop"
Write-Host "   - Ir a Settings (⚙️) → Sección 'Clientes'"
Write-Host ""

Write-Host "2️⃣  EDITAR UN CLIENTE EXISTENTE" -ForegroundColor Green
Write-Host "   - Click en cualquier card de cliente"
Write-Host "   - Cambiar el 'Nombre' (ej: agregar ' EDITADO' al final)"
Write-Host "   - Pulsar 'Guardar'"
Write-Host ""

Write-Host "3️⃣  VERIFICAR QUE SE VE EL CAMBIO INMEDIATAMENTE" -ForegroundColor Green
Write-Host "   ✅ ESPERADO: La lista se recarga y muestra el nuevo nombre"
Write-Host "   ❌ BUG: La lista muestra el nombre viejo (del caché)"
Write-Host ""

Write-Host "4️⃣  REVISAR LOGS EN BUSCA DE INVALIDACIÓN" -ForegroundColor Green
Write-Host "   Buscar en logs (Data/logs/GestionTime-[fecha].log):"
Write-Host ""
Write-Host "   ✅ LÍNEA ESPERADA (nueva):" -ForegroundColor Yellow
Write-Host '      "CLIENTES_UI OnSaveClienteClick | invalidating cache for /api/v1/clientes..."' -ForegroundColor Cyan
Write-Host ""
Write-Host "   ✅ LÍNEA ESPERADA (ApiClient):" -ForegroundColor Yellow
Write-Host '      "🗑️ Entrada de caché invalidada: /api/v1/clientes?page=1&size=50"' -ForegroundColor Cyan
Write-Host ""
Write-Host "   ✅ LÍNEA ESPERADA (GET sin caché):" -ForegroundColor Yellow
Write-Host '      "HTTP GET /api/v1/clientes?page=1&size=50 -> 200 en XXms"' -ForegroundColor Cyan
Write-Host '      (SIN el mensaje "💾 Usando CACHÉ")' -ForegroundColor Cyan
Write-Host ""

Write-Host "5️⃣  PROBAR OTRAS OPERACIONES" -ForegroundColor Green
Write-Host "   A) CREAR NUEVO CLIENTE:"
Write-Host "      - Pulsar 'Nuevo Cliente'"
Write-Host "      - Rellenar datos y guardar"
Write-Host "      - ✅ Verificar que aparece INMEDIATAMENTE en la lista"
Write-Host ""
Write-Host "   B) ACTUALIZAR SOLO NOTA:"
Write-Host "      - Editar cliente existente"
Write-Host "      - Cambiar solo el campo 'Nota'"
Write-Host "      - Pulsar 'Guardar Solo Nota'"
Write-Host "      - ✅ Verificar que el cambio aparece INMEDIATAMENTE"
Write-Host ""
Write-Host "   C) ELIMINAR CLIENTE:"
Write-Host "      - Editar cliente existente"
Write-Host "      - Pulsar '🗑️ Eliminar'"
Write-Host "      - Confirmar"
Write-Host "      - ✅ Verificar que desaparece INMEDIATAMENTE de la lista"
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "🔍 CÓMO BUSCAR EN LOS LOGS:" -ForegroundColor Yellow
Write-Host ""
Write-Host "# Buscar líneas de invalidación de caché:" -ForegroundColor Gray
Write-Host 'Select-String -Path "Data\logs\GestionTime-*.log" -Pattern "invalidating cache" | Select-Object -Last 10' -ForegroundColor Cyan
Write-Host ""

Write-Host "# Buscar todas las operaciones de clientes del día:" -ForegroundColor Gray
Write-Host 'Select-String -Path "Data\logs\GestionTime-*.log" -Pattern "CLIENTES_UI" | Select-Object -Last 50' -ForegroundColor Cyan
Write-Host ""

Write-Host "# Buscar entradas de caché invalidadas:" -ForegroundColor Gray
Write-Host 'Select-String -Path "Data\logs\GestionTime-*.log" -Pattern "Entrada de caché invalidada" | Select-Object -Last 10' -ForegroundColor Cyan
Write-Host ""

Write-Host "# Verificar que NO hay mensajes de caché después de guardar:" -ForegroundColor Gray
Write-Host 'Select-String -Path "Data\logs\GestionTime-*.log" -Pattern "Usando CACHÉ" -Context 2,2 | Select-Object -Last 10' -ForegroundColor Cyan
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "❌ ERRORES POSIBLES:" -ForegroundColor Red
Write-Host ""
Write-Host "  1. SI LA LISTA NO SE REFRESCA:" -ForegroundColor Yellow
Write-Host "     - Buscar en logs: 'CLIENTES_UI OnSaveClienteClick | invalidating cache'"
Write-Host "     - Si NO aparece → El fix NO se aplicó correctamente"
Write-Host ""
Write-Host "  2. SI SIGUE USANDO CACHÉ:" -ForegroundColor Yellow
Write-Host "     - Buscar en logs: '💾 Usando CACHÉ' después de guardar"
Write-Host "     - Si aparece → La invalidación NO funciona (verificar ApiClient.cs)"
Write-Host ""
Write-Host "  3. SI NO SE ENCUENTRA editPanel:" -ForegroundColor Yellow
Write-Host "     - Buscar en logs: 'CLIENTES_UI ShowClienteEditPanel | editPanel NOT FOUND'"
Write-Host "     - Este es Bug #1 (ya debería estar resuelto)"
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "✅ RESULTADO ESPERADO:" -ForegroundColor Green
Write-Host "  - Guardar → Cambios aparecen INMEDIATAMENTE"
Write-Host "  - Crear → Nuevo cliente aparece INMEDIATAMENTE"
Write-Host "  - Actualizar nota → Cambio aparece INMEDIATAMENTE"
Write-Host "  - Eliminar → Cliente desaparece INMEDIATAMENTE"
Write-Host "  - Logs muestran 'invalidating cache' en cada operación"
Write-Host "  - Logs NO muestran '💾 Usando CACHÉ' después de guardar"
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "📝 PARA REPORTAR RESULTADOS:" -ForegroundColor Yellow
Write-Host ""
Write-Host "Si TODO funciona correctamente:" -ForegroundColor Green
Write-Host '  Responde: "✅ Fix verificado - Los cambios se reflejan inmediatamente"' -ForegroundColor Cyan
Write-Host ""
Write-Host "Si ALGO falla:" -ForegroundColor Red
Write-Host "  1. Copia las últimas 100 líneas de logs de CLIENTES_UI:"
Write-Host '     Select-String -Path "Data\logs\GestionTime-*.log" -Pattern "CLIENTES_UI" | Select-Object -Last 100 | Out-File -FilePath "clientes_logs.txt"'
Write-Host "  2. Adjunta el archivo 'clientes_logs.txt' en tu reporte"
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
