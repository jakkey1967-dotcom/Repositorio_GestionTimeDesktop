# ===============================================
# Reiniciar Backend - FIX APLICADO
# ===============================================

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "REINICIAR BACKEND CON FIX APLICADO" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "[INFO] El fix YA ESTA APLICADO en Program.cs" -ForegroundColor Green
Write-Host "[INFO] Solo falta COMPILAR y EJECUTAR el backend" -ForegroundColor Yellow
Write-Host ""

cd C:\GestionTime\GestionTimeApi

Write-Host "[1/2] Compilando backend..." -ForegroundColor Yellow

# Buscar el archivo .csproj
$csprojFiles = Get-ChildItem -Filter "*.csproj"

if ($csprojFiles.Count -eq 0) {
    Write-Host "   [ERROR] No se encontro archivo .csproj" -ForegroundColor Red
    exit 1
}

$csproj = $csprojFiles[0].Name
Write-Host "   [INFO] Compilando: $csproj" -ForegroundColor Cyan

dotnet build $csproj --configuration Debug

if ($LASTEXITCODE -eq 0) {
    Write-Host "   [OK] Compilacion exitosa" -ForegroundColor Green
} else {
    Write-Host "   [ERROR] Error de compilacion" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "===============================================" -ForegroundColor Green
Write-Host "BACKEND COMPILADO - LISTO PARA EJECUTAR" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green
Write-Host ""
Write-Host "AHORA EJECUTA:" -ForegroundColor Cyan
Write-Host ""
Write-Host "   Terminal 1 (Backend):" -ForegroundColor Yellow
Write-Host "   cd C:\GestionTime\GestionTimeApi" -ForegroundColor White
Write-Host "   dotnet run --project $csproj" -ForegroundColor White
Write-Host ""
Write-Host "   Espera a ver:" -ForegroundColor Yellow
Write-Host '   Now listening on: http://localhost:2501' -ForegroundColor Gray
Write-Host ""
Write-Host "   Terminal 2 (Desktop):" -ForegroundColor Yellow
Write-Host "   cd C:\GestionTime\GestionTimeDesktop" -ForegroundColor White
Write-Host "   dotnet run" -ForegroundColor White
Write-Host ""
Write-Host "Entonces el problema estara RESUELTO." -ForegroundColor Green
Write-Host ""
