# ====================================================
# Fix-PresenceTimeout.ps1
# Reduce el timeout de presencia de 2 minutos a 30 segundos
# ====================================================

$backendPath = "C:\GestionTime\GestionTimeApi\Controllers\PresenceController.cs"

Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host "FIX: REDUCIR TIMEOUT DE PRESENCIA A 30 SEGUNDOS" -ForegroundColor Cyan
Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host ""

# Backup del archivo original
$backupPath = "$backendPath.backup"
Copy-Item $backendPath $backupPath -Force
Write-Host "Backup creado: $backupPath" -ForegroundColor Gray
Write-Host ""

# Leer contenido
$content = Get-Content $backendPath -Raw

# Reemplazar threshold
$content = $content -replace 'private const int ONLINE_THRESHOLD_MINUTES = 2;', 'private const int ONLINE_THRESHOLD_SECONDS = 30;'
$content = $content -replace 'AddMinutes\(-ONLINE_THRESHOLD_MINUTES\)', 'AddSeconds(-ONLINE_THRESHOLD_SECONDS)'
$content = $content -replace 'Considerar usuario online si lastSeenAt < 2 minutos', 'Considerar usuario online si lastSeenAt < 30 segundos'

# Guardar cambios
Set-Content $backendPath -Value $content -NoNewline

Write-Host "Cambios aplicados:" -ForegroundColor Green
Write-Host "  - ONLINE_THRESHOLD_MINUTES = 2 -> ONLINE_THRESHOLD_SECONDS = 30" -ForegroundColor Gray
Write-Host "  - AddMinutes(-2) -> AddSeconds(-30)" -ForegroundColor Gray
Write-Host ""

Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host "SIGUIENTE PASO:" -ForegroundColor Cyan
Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Reinicia el backend (GestionTimeApi)" -ForegroundColor Yellow
Write-Host "2. Ejecuta el diagnostico de nuevo:" -ForegroundColor Yellow
Write-Host "   .\Scripts\Debug-PresenceSystem.ps1" -ForegroundColor Gray
Write-Host ""
Write-Host "Ahora deberia marcar usuarios offline despues de 30 segundos." -ForegroundColor Green
Write-Host ""
