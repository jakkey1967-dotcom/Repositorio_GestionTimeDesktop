# ===============================================
# Verificación rápida del token después del login
# ===============================================

Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🔐 VERIFICACIÓN DE TOKEN - GestionTime Desktop" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "📋 INSTRUCCIONES:" -ForegroundColor Yellow
Write-Host "   1. Ejecuta la aplicación Desktop: dotnet run" -ForegroundColor White
Write-Host "   2. Haz LOGIN con usuario/contraseña" -ForegroundColor White
Write-Host "   3. Observa los logs de la API (donde ves este archivo)" -ForegroundColor White
Write-Host ""

Write-Host "🔍 QUÉ BUSCAR EN LOS LOGS:" -ForegroundColor Yellow
Write-Host ""
Write-Host "✅ SI FUNCIONA, verás:" -ForegroundColor Green
Write-Host "   [HH:MM:SS] POST /api/v1/auth/login-desktop → 200 OK" -ForegroundColor White
Write-Host "   [HH:MM:SS] GET /api/v1/partes → 200 OK" -ForegroundColor White
Write-Host "   [HH:MM:SS] GET /api/v1/presence/users → 200 OK" -ForegroundColor White
Write-Host ""

Write-Host "❌ SI NO FUNCIONA, verás:" -ForegroundColor Red
Write-Host "   [HH:MM:SS] POST /api/v1/auth/login-desktop → 200 OK" -ForegroundColor White
Write-Host "   [HH:MM:SS] GET /api/v1/partes → 401 Unauthorized" -ForegroundColor Red
Write-Host "   [HH:MM:SS] GET /api/v1/presence/users → 401 Unauthorized" -ForegroundColor Red
Write-Host ""

Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "💾 SOLUCIÓN APLICADA:" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ DiarioPage ahora espera 500ms a que el token esté disponible" -ForegroundColor Green
Write-Host "✅ Si el token no está disponible, muestra error" -ForegroundColor Green
Write-Host "✅ Esto soluciona la condición de carrera" -ForegroundColor Green
Write-Host ""

Write-Host "════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🚀 SIGUIENTE PASO:" -ForegroundColor Yellow
Write-Host "════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Ejecuta: dotnet run" -ForegroundColor White
Write-Host "2. Haz login" -ForegroundColor White
Write-Host "3. Observa si ahora SÍ aparecen datos en la ListView" -ForegroundColor White
Write-Host ""
