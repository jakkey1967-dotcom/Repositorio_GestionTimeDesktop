# ===========================================================================
# DESCARGAR E INSTALAR WIX v3.14 AUTOMATICAMENTE
# ===========================================================================

Write-Host "Descargando WiX Toolset v3.14..." -ForegroundColor Yellow

$url = "https://github.com/wixtoolset/wix3/releases/download/wix3141rtm/wix314.exe"
$installer = "$env:TEMP\wix314.exe"

try {
    Invoke-WebRequest -Uri $url -OutFile $installer
    Write-Host "Instalando WiX v3.14..." -ForegroundColor Yellow
    Start-Process $installer -ArgumentList "/install", "/quiet", "/norestart" -Wait
    Write-Host "WiX v3.14 instalado" -ForegroundColor Green
    Write-Host ""
    Write-Host "Ahora ejecutar: GENERAR-MSI-CON-HEAT.bat" -ForegroundColor Cyan
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
}
