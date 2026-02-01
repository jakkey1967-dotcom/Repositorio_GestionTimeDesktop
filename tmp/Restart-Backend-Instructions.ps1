# ===============================================
# Instrucciones para Reiniciar Backend
# ===============================================

Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🔄 REINICIO DEL BACKEND REQUERIDO" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "✅ El backend ha sido actualizado para aceptar tokens desde:" -ForegroundColor Green
Write-Host "   • Header Authorization: Bearer {token} (Desktop/Mobile)" -ForegroundColor White
Write-Host "   • Cookie 'access_token' (Navegadores web)" -ForegroundColor White
Write-Host ""

Write-Host "📋 PASOS A SEGUIR:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1️⃣  Detén el backend actual (Ctrl+C en la terminal donde corre)" -ForegroundColor Cyan
Write-Host ""
Write-Host "2️⃣  Reinicia el backend:" -ForegroundColor Cyan
Write-Host "   cd C:\GestionTime\GestionTimeApi" -ForegroundColor White
Write-Host "   dotnet run" -ForegroundColor White
Write-Host ""
Write-Host "3️⃣  Espera a ver estos mensajes:" -ForegroundColor Cyan
Write-Host '   Now listening on: http://localhost:2501' -ForegroundColor Gray
Write-Host '   Now listening on: https://localhost:2502' -ForegroundColor Gray
Write-Host ""
Write-Host "4️⃣  Ejecuta el Desktop:" -ForegroundColor Cyan
Write-Host "   cd C:\GestionTime\GestionTimeDesktop" -ForegroundColor White
Write-Host "   dotnet run" -ForegroundColor White
Write-Host ""
Write-Host "5️⃣  Haz login y verifica que los datos se carguen correctamente" -ForegroundColor Cyan
Write-Host ""

Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🔍 VERIFICACIÓN:" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Después del login, en los logs del Desktop deberías ver:" -ForegroundColor White
Write-Host '   HTTP GET /api/v1/partes | Token: True | Header: True' -ForegroundColor Gray
Write-Host '   HTTP GET /api/v1/partes -> 200 en Xms' -ForegroundColor Green
Write-Host '   ✅ Petición exitosa - 25 partes cargados' -ForegroundColor Green
Write-Host ""

Write-Host "Si ves 401 Unauthorized, el backend no se reinició correctamente." -ForegroundColor Yellow
Write-Host ""
