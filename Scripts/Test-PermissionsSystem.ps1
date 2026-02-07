# 🧪 Script de Testing: Sistema de Permisos en Settings

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  TESTING: Sistema de Permisos Settings" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "📋 ROLES DISPONIBLES:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1️⃣  USER (Acceso Limitado)" -ForegroundColor Green
Write-Host "   ✅ Perfil y cuenta" -ForegroundColor Gray
Write-Host "   ✅ Usuarios online / Presencia" -ForegroundColor Gray
Write-Host "   ✅ Salir" -ForegroundColor Gray
Write-Host "   ❌ Clientes, Grupos/Tipos, etc." -ForegroundColor Gray
Write-Host ""

Write-Host "2️⃣  EDITOR (Acceso Medio)" -ForegroundColor Green
Write-Host "   ✅ Perfil y cuenta" -ForegroundColor Gray
Write-Host "   ✅ Clientes" -ForegroundColor Gray
Write-Host "   ✅ Grupos y Tipos" -ForegroundColor Gray
Write-Host "   ✅ Usuarios online / Presencia" -ForegroundColor Gray
Write-Host "   ✅ Salir" -ForegroundColor Gray
Write-Host "   ❌ Permisos, Integraciones, etc." -ForegroundColor Gray
Write-Host ""

Write-Host "3️⃣  ADMIN (Acceso Total)" -ForegroundColor Green
Write-Host "   ✅ TODO sin restricciones" -ForegroundColor Gray
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "🔧 CÓMO CAMBIAR EL ROL PARA PROBAR:" -ForegroundColor Yellow
Write-Host ""
Write-Host "Editar archivo:" -ForegroundColor Gray
Write-Host "   ViewModels/SettingsViewModel.cs" -ForegroundColor Cyan
Write-Host ""
Write-Host "Buscar línea ~59:" -ForegroundColor Gray
Write-Host '   _permissionService.SetCurrentUserRole(UserRole.ADMIN);' -ForegroundColor Cyan
Write-Host ""
Write-Host "Cambiar a uno de estos:" -ForegroundColor Gray
Write-Host ""
Write-Host "   USER:" -ForegroundColor Green
Write-Host '   _permissionService.SetCurrentUserRole(UserRole.USER);' -ForegroundColor Cyan
Write-Host ""
Write-Host "   EDITOR:" -ForegroundColor Green
Write-Host '   _permissionService.SetCurrentUserRole(UserRole.EDITOR);' -ForegroundColor Cyan
Write-Host ""
Write-Host "   ADMIN:" -ForegroundColor Green
Write-Host '   _permissionService.SetCurrentUserRole(UserRole.ADMIN);' -ForegroundColor Cyan
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "🧪 PASOS PARA TESTING:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1️⃣  PROBAR COMO USER" -ForegroundColor Green
Write-Host "   - Cambiar rol a USER en SettingsViewModel.cs" -ForegroundColor Gray
Write-Host "   - Recompilar (Ctrl+Shift+B)" -ForegroundColor Gray
Write-Host "   - Iniciar app (F5)" -ForegroundColor Gray
Write-Host "   - Ir a Settings" -ForegroundColor Gray
Write-Host "   - Verificar candados:" -ForegroundColor Gray
Write-Host "     • Perfil y cuenta: 🔓 (verde)" -ForegroundColor Gray
Write-Host "     • Clientes: 🔒 (amarillo)" -ForegroundColor Gray
Write-Host "     • Usuarios online: 🔓 (verde)" -ForegroundColor Gray
Write-Host "   - Intentar hacer click en 'Clientes':" -ForegroundColor Gray
Write-Host "     ✅ ESPERADO: InfoBar 'Acceso denegado'" -ForegroundColor Gray
Write-Host "     ✅ ESPERADO: NO carga contenido" -ForegroundColor Gray
Write-Host ""

Write-Host "2️⃣  PROBAR COMO EDITOR" -ForegroundColor Green
Write-Host "   - Cambiar rol a EDITOR en SettingsViewModel.cs" -ForegroundColor Gray
Write-Host "   - Recompilar (Ctrl+Shift+B)" -ForegroundColor Gray
Write-Host "   - Iniciar app (F5)" -ForegroundColor Gray
Write-Host "   - Ir a Settings" -ForegroundColor Gray
Write-Host "   - Verificar candados:" -ForegroundColor Gray
Write-Host "     • Perfil y cuenta: 🔓 (verde)" -ForegroundColor Gray
Write-Host "     • Clientes: 🔓 (verde)" -ForegroundColor Gray
Write-Host "     • Grupos y Tipos: 🔓 (verde)" -ForegroundColor Gray
Write-Host "     • Integraciones: 🔒 (amarillo)" -ForegroundColor Gray
Write-Host "   - Hacer click en 'Clientes':" -ForegroundColor Gray
Write-Host "     ✅ ESPERADO: Carga contenido normalmente" -ForegroundColor Gray
Write-Host "   - Intentar hacer click en 'Integraciones':" -ForegroundColor Gray
Write-Host "     ✅ ESPERADO: InfoBar 'Acceso denegado'" -ForegroundColor Gray
Write-Host ""

Write-Host "3️⃣  PROBAR COMO ADMIN" -ForegroundColor Green
Write-Host "   - Cambiar rol a ADMIN en SettingsViewModel.cs" -ForegroundColor Gray
Write-Host "   - Recompilar (Ctrl+Shift+B)" -ForegroundColor Gray
Write-Host "   - Iniciar app (F5)" -ForegroundColor Gray
Write-Host "   - Ir a Settings" -ForegroundColor Gray
Write-Host "   - Verificar candados:" -ForegroundColor Gray
Write-Host "     • TODOS: 🔓 (verde)" -ForegroundColor Gray
Write-Host "   - Hacer click en cualquier sección:" -ForegroundColor Gray
Write-Host "     ✅ ESPERADO: Carga contenido sin restricciones" -ForegroundColor Gray
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "📊 VERIFICACIÓN DE LOGS:" -ForegroundColor Yellow
Write-Host ""
Write-Host "Al iniciar Settings con cada rol:" -ForegroundColor Gray
Write-Host ""
Write-Host '   Select-String -Path "Data\logs\GestionTime-*.log" -Pattern "Settings iniciado con rol" | Select-Object -Last 5' -ForegroundColor Cyan
Write-Host ""
Write-Host "Al intentar acceder a sección bloqueada:" -ForegroundColor Gray
Write-Host ""
Write-Host '   Select-String -Path "Data\logs\GestionTime-*.log" -Pattern "Intento de acceso bloqueado" | Select-Object -Last 10' -ForegroundColor Cyan
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "✅ RESULTADO ESPERADO:" -ForegroundColor Green
Write-Host ""
Write-Host "  - USER: Solo 3 secciones accesibles (Perfil, Presencia, Salir)" -ForegroundColor Gray
Write-Host "  - EDITOR: 5 secciones accesibles (+ Clientes, Grupos/Tipos)" -ForegroundColor Gray
Write-Host "  - ADMIN: Todas las 9 secciones accesibles" -ForegroundColor Gray
Write-Host "  - Candados visuales correctos (🔓 verde vs 🔒 amarillo)" -ForegroundColor Gray
Write-Host "  - InfoBar aparece al intentar acceder a sección bloqueada" -ForegroundColor Gray
Write-Host "  - NO se carga contenido ni se ejecutan APIs en secciones bloqueadas" -ForegroundColor Gray
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "⚠️  NOTAS IMPORTANTES:" -ForegroundColor Red
Write-Host ""
Write-Host "1. Cada cambio de rol requiere RECOMPILAR la app" -ForegroundColor Yellow
Write-Host "2. El rol se establece al iniciar SettingsWindow" -ForegroundColor Yellow
Write-Host "3. En producción, el rol debe venir del backend (/api/v1/users/me)" -ForegroundColor Yellow
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "📝 PARA REPORTAR PROBLEMAS:" -ForegroundColor Yellow
Write-Host ""
Write-Host "Si algo NO funciona como esperado:" -ForegroundColor Gray
Write-Host ""
Write-Host "1. Capturar screenshot del menú con candados" -ForegroundColor Cyan
Write-Host "2. Capturar logs de la sesión:" -ForegroundColor Cyan
Write-Host '   $logFile = Get-ChildItem "Data\logs\GestionTime-*.log" | Sort-Object LastWriteTime | Select-Object -Last 1' -ForegroundColor Cyan
Write-Host '   Select-String -Path $logFile.FullName -Pattern "Settings|Intento de acceso" | Out-File "settings_test_logs.txt"' -ForegroundColor Cyan
Write-Host "3. Reportar con rol probado y comportamiento inesperado" -ForegroundColor Cyan
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "🚀 ¡LISTO PARA PROBAR!" -ForegroundColor Green
Write-Host ""
