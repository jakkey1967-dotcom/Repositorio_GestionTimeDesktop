# Verify-BannerUpdate.ps1
# Verifica que el banner de DiarioPage se actualice correctamente al cambiar de usuario

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🔍 VERIFICACIÓN: Banner de DiarioPage - Actualización de Nombre" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "📋 PROBLEMA SOLUCIONADO:" -ForegroundColor Yellow
Write-Host "   • Al cambiar de usuario, el EMAIL se actualizaba correctamente" -ForegroundColor Green
Write-Host "   • Pero el NOMBRE se quedaba con el del usuario anterior" -ForegroundColor Red
Write-Host ""

Write-Host "✅ SOLUCIÓN APLICADA:" -ForegroundColor Green
Write-Host "   • Eliminado check 'if (App.CurrentUserProfile == null)'" -ForegroundColor White
Write-Host "   • SIEMPRE se recarga el perfil desde API al abrir DiarioPage" -ForegroundColor White
Write-Host "   • Logging mejorado para diagnóstico" -ForegroundColor White
Write-Host ""

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🧪 PLAN DE PRUEBAS" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "PASO 1: Preparar dos usuarios de prueba" -ForegroundColor Yellow
Write-Host "   • Usuario 1: pedro.santos@empresa.com / Pedro Santos" -ForegroundColor Gray
Write-Host "   • Usuario 2: maria.lopez@empresa.com / María López" -ForegroundColor Gray
Write-Host ""

Write-Host "PASO 2: Login con Usuario 1" -ForegroundColor Yellow
Write-Host "   1. Ejecutar GestionTime Desktop" -ForegroundColor White
Write-Host "   2. Iniciar sesión con pedro.santos@empresa.com" -ForegroundColor White
Write-Host "   3. Verificar en DiarioPage:" -ForegroundColor White
Write-Host "      └─ Nombre: 'Pedro Santos'" -ForegroundColor Green
Write-Host "      └─ Email: 'pedro.santos@empresa.com'" -ForegroundColor Green
Write-Host "   4. Verificar en Output > Debug:" -ForegroundColor White
Write-Host "      └─ '📥 Cargando perfil del usuario actual desde API...'" -ForegroundColor Gray
Write-Host "      └─ '✅ Perfil cargado: Pedro | Santos | ...' " -ForegroundColor Gray
Write-Host "      └─ '🎨 Banner actualizado:'" -ForegroundColor Gray
Write-Host "      └─ '   • DisplayName: Pedro Santos'" -ForegroundColor Gray
Write-Host ""

Write-Host "PASO 3: Logout" -ForegroundColor Yellow
Write-Host "   1. Click en botón 'Cerrar Sesión' (DiarioPage)" -ForegroundColor White
Write-Host "   2. Confirmar cierre de sesión" -ForegroundColor White
Write-Host "   3. Verificar en Output > Debug:" -ForegroundColor White
Write-Host "      └─ '✅ CurrentUserProfile limpiado (null)'" -ForegroundColor Gray
Write-Host ""

Write-Host "PASO 4: Login con Usuario 2 (CRÍTICO)" -ForegroundColor Yellow
Write-Host "   1. Iniciar sesión con maria.lopez@empresa.com" -ForegroundColor White
Write-Host "   2. ⚠️  VERIFICAR EN BANNER:" -ForegroundColor Red
Write-Host "      └─ Nombre: 'María López' (NO 'Pedro Santos')" -ForegroundColor Green
Write-Host "      └─ Email: 'maria.lopez@empresa.com'" -ForegroundColor Green
Write-Host "   3. Verificar en Output > Debug:" -ForegroundColor White
Write-Host "      └─ '📥 Cargando perfil del usuario actual desde API...'" -ForegroundColor Gray
Write-Host "      └─ '   • CurrentLoginEmail: maria.lopez@empresa.com'" -ForegroundColor Gray
Write-Host "      └─ '   • CurrentUserProfile (antes): Pedro Santos' ← (si existía)" -ForegroundColor Yellow
Write-Host "      └─ '✅ Perfil cargado: María | López | ...' " -ForegroundColor Gray
Write-Host "      └─ '🎨 Banner actualizado:'" -ForegroundColor Gray
Write-Host "      └─ '   • DisplayName: María López' ← ✅ ACTUALIZADO" -ForegroundColor Green
Write-Host ""

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "📊 COMPARACIÓN ANTES/DESPUÉS DEL FIX" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "ANTES DEL FIX:" -ForegroundColor Red
Write-Host "┌─────────────────┬───────────────────┬──────────────────────┐" -ForegroundColor Gray
Write-Host "│ Campo           │ Login Usuario 1   │ Logout → Login U2    │" -ForegroundColor Gray
Write-Host "├─────────────────┼───────────────────┼──────────────────────┤" -ForegroundColor Gray
Write-Host "│ Nombre          │ Pedro Santos      │ ❌ Pedro Santos       │" -ForegroundColor Red
Write-Host "│ Email           │ pedro@...         │ ✅ maria@...          │" -ForegroundColor Green
Write-Host "└─────────────────┴───────────────────┴──────────────────────┘" -ForegroundColor Gray
Write-Host ""

Write-Host "DESPUÉS DEL FIX:" -ForegroundColor Green
Write-Host "┌─────────────────┬───────────────────┬──────────────────────┐" -ForegroundColor Gray
Write-Host "│ Campo           │ Login Usuario 1   │ Logout → Login U2    │" -ForegroundColor Gray
Write-Host "├─────────────────┼───────────────────┼──────────────────────┤" -ForegroundColor Gray
Write-Host "│ Nombre          │ Pedro Santos      │ ✅ María López        │" -ForegroundColor Green
Write-Host "│ Email           │ pedro@...         │ ✅ maria@...          │" -ForegroundColor Green
Write-Host "└─────────────────┴───────────────────┴──────────────────────┘" -ForegroundColor Gray
Write-Host ""

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🔍 LOGGING DE DIAGNÓSTICO" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "Al abrir DiarioPage, busca en Output > Debug:" -ForegroundColor White
Write-Host ""
Write-Host "📥 Cargando perfil del usuario actual desde API..." -ForegroundColor Cyan
Write-Host "   • CurrentLoginEmail: maria.lopez@empresa.com" -ForegroundColor Gray
Write-Host "   • CurrentUserProfile (antes): Pedro Santos" -ForegroundColor Yellow
Write-Host "✅ Perfil cargado: María | López | María López | +34 611 222 333" -ForegroundColor Green
Write-Host "🎨 Banner actualizado:" -ForegroundColor Cyan
Write-Host "   • DisplayName: María López" -ForegroundColor Green
Write-Host "   • DisplayEmail: maria.lopez@empresa.com" -ForegroundColor Green
Write-Host "   • DisplayPhone: +34 611 222 333" -ForegroundColor Green
Write-Host ""

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "⚠️ SI EL NOMBRE NO SE ACTUALIZA:" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "1. Verificar en Output > Debug:" -ForegroundColor White
Write-Host "   └─ ¿Aparece '📥 Cargando perfil del usuario actual desde API...'?" -ForegroundColor Gray
Write-Host "   └─ ¿Aparece '✅ Perfil cargado: [Nombre] [Apellido] | ...'?" -ForegroundColor Gray
Write-Host ""

Write-Host "2. Si NO aparecen los logs:" -ForegroundColor Yellow
Write-Host "   └─ El OnPageLoaded() de DiarioPage NO se está ejecutando" -ForegroundColor Red
Write-Host "   └─ Verificar navegación desde LoginPage" -ForegroundColor Red
Write-Host ""

Write-Host "3. Si los logs muestran 'Pedro Santos' en lugar de 'María López':" -ForegroundColor Yellow
Write-Host "   └─ El API está devolviendo el perfil incorrecto" -ForegroundColor Red
Write-Host "   └─ Verificar token JWT (debe ser del usuario correcto)" -ForegroundColor Red
Write-Host "   └─ Ejecutar: curl -H 'Authorization: Bearer [TOKEN]' https://api.../profiles/me" -ForegroundColor Gray
Write-Host ""

Write-Host "4. Si el perfil se carga correctamente pero el banner no cambia:" -ForegroundColor Yellow
Write-Host "   └─ Problema en el binding XAML" -ForegroundColor Red
Write-Host "   └─ Verificar que ViewModel.DisplayName dispara PropertyChanged" -ForegroundColor Red
Write-Host ""

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✅ CHECKLIST FINAL" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

$checklist = @(
    @{ Item = "Login Usuario 1 → Nombre correcto"; Status = $false }
    @{ Item = "Email correcto en Usuario 1"; Status = $false }
    @{ Item = "Logs de carga de perfil visibles"; Status = $false }
    @{ Item = "Logout realizado correctamente"; Status = $false }
    @{ Item = "Login Usuario 2 → Nombre ACTUALIZADO"; Status = $false }
    @{ Item = "Email actualizado en Usuario 2"; Status = $false }
    @{ Item = "Logs muestran perfil de Usuario 2"; Status = $false }
)

foreach ($item in $checklist) {
    $icon = if ($item.Status) { "✅" } else { "⬜" }
    Write-Host "$icon $($item.Item)" -ForegroundColor $(if ($item.Status) { "Green" } else { "Gray" })
}
Write-Host ""

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🚀 COMENZAR VERIFICACIÓN" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Ejecuta GestionTime Desktop" -ForegroundColor White
Write-Host "2. Abre Output > Debug en Visual Studio" -ForegroundColor White
Write-Host "3. Sigue el PLAN DE PRUEBAS de arriba" -ForegroundColor White
Write-Host "4. Marca cada item del checklist" -ForegroundColor White
Write-Host ""
Write-Host "✅ Si todos los items están marcados → FIX VALIDADO" -ForegroundColor Green
Write-Host ""
