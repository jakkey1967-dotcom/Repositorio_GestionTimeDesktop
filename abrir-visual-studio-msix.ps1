# Script para abrir Visual Studio y crear MSIX manualmente

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "   ABRIR VISUAL STUDIO PARA CREAR MSIX" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

$projectPath = "C:\GestionTime\GestionTime.Desktop\GestionTime.Desktop.csproj"

Write-Host "?? PASOS A SEGUIR EN VISUAL STUDIO:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. ? Visual Studio se abrirá automáticamente con el proyecto" -ForegroundColor White
Write-Host "2. ?? Verificar que compile: Build ? Build Solution" -ForegroundColor White
Write-Host "3. ?? Click derecho en proyecto 'GestionTime.Desktop'" -ForegroundColor White
Write-Host "4. ?? Publish ? Create App Packages..." -ForegroundColor White
Write-Host "5. ?? Seleccionar 'Sideloading'" -ForegroundColor White
Write-Host "6. ?? Arquitectura: Solo x64 marcado" -ForegroundColor White
Write-Host "7. ?? Version: 1.1.0.0" -ForegroundColor White
Write-Host "8. ?? Create new certificate o usar existente" -ForegroundColor White
Write-Host "9. ? Click 'Create' y esperar" -ForegroundColor White
Write-Host ""

Write-Host "?? El instalador se creará en:" -ForegroundColor Green
Write-Host "   C:\GestionTime\GestionTime.Desktop\AppPackages\" -ForegroundColor White
Write-Host ""

Write-Host "?? Archivo final esperado:" -ForegroundColor Green
Write-Host "   GestionTime.Desktop_1.1.0.0_x64.msix" -ForegroundColor White
Write-Host ""

Read-Host "Presiona Enter para abrir Visual Studio"

Write-Host "?? Abriendo Visual Studio..." -ForegroundColor Cyan

try {
    Start-Process -FilePath "devenv.exe" -ArgumentList $projectPath
    Write-Host "? Visual Studio abierto correctamente" -ForegroundColor Green
} catch {
    Write-Host "?? No se pudo abrir con 'devenv.exe', intentando alternativas..." -ForegroundColor Yellow
    
    try {
        # Buscar Visual Studio 2022
        $vsPath = Get-ChildItem "C:\Program Files\Microsoft Visual Studio\2022" -Recurse -Filter "devenv.exe" | Select-Object -First 1
        
        if ($vsPath) {
            Start-Process -FilePath $vsPath.FullName -ArgumentList $projectPath
            Write-Host "? Visual Studio abierto desde: $($vsPath.FullName)" -ForegroundColor Green
        } else {
            throw "No se encontró Visual Studio"
        }
    } catch {
        Write-Host "? Error abriendo Visual Studio: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
        Write-Host "?? SOLUCIÓN MANUAL:" -ForegroundColor Yellow
        Write-Host "1. Abrir Visual Studio 2022 manualmente" -ForegroundColor White
        Write-Host "2. Abrir proyecto: $projectPath" -ForegroundColor White
        Write-Host "3. Seguir los pasos listados arriba" -ForegroundColor White
    }
}

Write-Host ""
Write-Host "? Sigue los pasos en Visual Studio para crear el instalador MSIX" -ForegroundColor Green
Write-Host ""
Read-Host "Presiona Enter cuando hayas terminado en Visual Studio"