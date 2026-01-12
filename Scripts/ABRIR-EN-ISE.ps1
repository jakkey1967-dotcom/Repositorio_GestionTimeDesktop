# Script para abrir CREATE-MSI-COMPLETE.ps1 en PowerShell ISE
$scriptPath = Join-Path $PSScriptRoot "CREATE-MSI-COMPLETE.ps1"

if (Test-Path $scriptPath) {
    Write-Host "Abriendo PowerShell ISE con el script..." -ForegroundColor Green
    & powershell_ise.exe -File $scriptPath
} else {
    Write-Host "ERROR: No se encuentra CREATE-MSI-COMPLETE.ps1" -ForegroundColor Red
    Read-Host "Presiona Enter para salir"
}
