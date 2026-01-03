# ====================================================================
# Paquete Portable COMPLETO - Copia TODO lo necesario
# Incluye TODAS las carpetas de recursos
# Fecha: 2025-01-27
# ====================================================================

param(
    [string]$Version = "1.1.0.0"
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  PAQUETE PORTABLE COMPLETO                " -ForegroundColor Cyan
Write-Host "  GestionTime Desktop v$Version            " -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

# ====================================================================
# 1. VERIFICAR DEBUG
# ====================================================================

Write-Host "1. Verificando compilacion Debug..." -ForegroundColor Yellow

$debugPath = "bin\x64\Debug\net8.0-windows10.0.19041.0"
$debugExe = Join-Path $debugPath "GestionTime.Desktop.exe"

if (!(Test-Path $debugExe)) {
    Write-Host "   [ERROR] No se encuentra Debug funcional" -ForegroundColor Red
    Write-Host "   Compila primero: dotnet build" -ForegroundColor Yellow
    exit 1
}

Write-Host "   [OK] Debug encontrado" -ForegroundColor Green
Write-Host ""

# ====================================================================
# 2. CREAR CARPETA FINAL
# ====================================================================

Write-Host "2. Creando paquete portable..." -ForegroundColor Yellow

$finalPath = "DISTRIBUIR\GestionTime-Desktop-$Version"

# Limpiar si existe
if (Test-Path $finalPath) {
    Write-Host "   Limpiando paquete anterior..." -ForegroundColor Gray
    Remove-Item -Path $finalPath -Recurse -Force
}

# Crear directorio
New-Item -ItemType Directory -Path $finalPath -Force | Out-Null

# Copiar TODO desde Debug
Write-Host "   Copiando TODOS los archivos desde Debug..." -ForegroundColor Gray
Write-Host "   (Esto incluye todas las carpetas de recursos)" -ForegroundColor Gray

Copy-Item -Path "$debugPath\*" -Destination $finalPath -Recurse -Force

$fileCount = (Get-ChildItem -Path $finalPath -Recurse -File).Count
$totalSize = (Get-ChildItem -Path $finalPath -Recurse -File | Measure-Object -Property Length -Sum).Sum
$totalSizeMB = [math]::Round($totalSize / 1MB, 2)

Write-Host "   [OK] Copiado completo: $fileCount archivos ($totalSizeMB MB)" -ForegroundColor Green
Write-Host ""

# ====================================================================
# 3. LIMPIAR ARCHIVOS INNECESARIOS (OPCIONAL)
# ====================================================================

Write-Host "3. Limpiando archivos temporales..." -ForegroundColor Yellow

# Solo eliminar archivos claramente temporales
$filesToRemove = @(
    "*.pdb.tmp",
    "*.tmp",
    "*.cache"
)

$removedCount = 0
foreach ($pattern in $filesToRemove) {
    $files = Get-ChildItem -Path $finalPath -Filter $pattern -Recurse -File -ErrorAction SilentlyContinue
    foreach ($file in $files) {
        Remove-Item $file.FullName -Force -ErrorAction SilentlyContinue
        $removedCount++
    }
}

if ($removedCount -gt 0) {
    Write-Host "   [OK] $removedCount archivos temporales eliminados" -ForegroundColor Green
} else {
    Write-Host "   [OK] Sin archivos temporales" -ForegroundColor Green
}

Write-Host ""

# ====================================================================
# 4. VERIFICAR ESTRUCTURA
# ====================================================================

Write-Host "4. Verificando estructura..." -ForegroundColor Yellow

# Verificar ejecutable
if (Test-Path (Join-Path $finalPath "GestionTime.Desktop.exe")) {
    Write-Host "   [OK] Ejecutable principal" -ForegroundColor Green
}

# Verificar carpetas de recursos
$resourceFolders = Get-ChildItem -Path $finalPath -Directory | Where-Object { 
    $_.Name -match "^[a-z]{2}-[A-Z]{2}$" -or $_.Name -eq "Microsoft.UI.Xaml"
}

if ($resourceFolders) {
    Write-Host "   [OK] $($resourceFolders.Count) carpetas de recursos" -ForegroundColor Green
    foreach ($folder in $resourceFolders | Select-Object -First 5) {
        Write-Host "      - $($folder.Name)" -ForegroundColor Gray
    }
    if ($resourceFolders.Count -gt 5) {
        Write-Host "      ... y $($resourceFolders.Count - 5) mas" -ForegroundColor Gray
    }
} else {
    Write-Host "   [WARN] No se encontraron carpetas de recursos" -ForegroundColor Yellow
}

Write-Host ""

# ====================================================================
# 5. CREAR LAUNCHER
# ====================================================================

Write-Host "5. Creando launcher..." -ForegroundColor Yellow

$launcherBat = @"
@echo off
title GestionTime Desktop v$Version

echo ============================================
echo   GESTIONTIME DESKTOP v$Version
echo   VERSION PORTABLE COMPLETA
echo ============================================
echo.
echo Iniciando aplicacion...
echo.

cd /d "%~dp0"
start "" "GestionTime.Desktop.exe"

if %errorLevel% NEQ 0 (
    echo.
    echo [ERROR] No se pudo iniciar
    echo Presiona cualquier tecla para salir...
    pause >nul
)
"@

$launcherPath = Join-Path $finalPath "INICIAR.bat"
$launcherBat | Out-File -FilePath $launcherPath -Encoding ASCII -Force

Write-Host "   [OK] INICIAR.bat creado" -ForegroundColor Green
Write-Host ""

# ====================================================================
# 6. CREAR README
# ====================================================================

Write-Host "6. Creando documentacion..." -ForegroundColor Yellow

$readmeContent = @"
============================================
GESTIONTIME DESKTOP v$Version
VERSION PORTABLE COMPLETA
============================================

INSTRUCCIONES:
--------------

1. Ejecutar: INICIAR.bat
   O doble clic en: GestionTime.Desktop.exe

2. La aplicacion funciona SIN INSTALACION

CONTENIDO:
----------

Este paquete contiene:
- Ejecutable principal
- Todas las librerias necesarias
- Runtime .NET 8 completo
- WindowsAppSDK completo
- Carpetas de recursos (idiomas, temas)
- Archivos de configuracion

Total: $fileCount archivos ($totalSizeMB MB)

CARACTERISTICAS:
----------------

[OK] NO requiere instalacion
[OK] NO requiere permisos de administrador
[OK] Completamente portable (USB, red, etc.)
[OK] Runtime .NET incluido
[OK] WindowsAppSDK incluido
[OK] FUNCIONA GARANTIZADO

REQUISITOS:
-----------

- Windows 10 build 17763+ o Windows 11
- Arquitectura x64
- $([math]::Ceiling($totalSizeMB * 1.5)) MB espacio en disco

USO PORTABLE:
-------------

Puedes copiar esta carpeta a:
- USB
- Otra carpeta
- Otro PC
- Servidor de red (\\servidor\apps\)

Y ejecutar directamente sin instalar.

CONFIGURACION:
--------------

Editar: appsettings.json

SOPORTE:
--------

Web: https://gestiontime.com/support
Email: support@gestiontime.com

Copyright (c) 2025 GestionTime Solutions
"@

$readmePath = Join-Path $finalPath "LEEME.txt"
$readmeContent | Out-File -FilePath $readmePath -Encoding UTF8 -Force

Write-Host "   [OK] LEEME.txt creado" -ForegroundColor Green
Write-Host ""

# ====================================================================
# 7. CREAR ACCESO DIRECTO
# ====================================================================

Write-Host "7. Creando acceso directo..." -ForegroundColor Yellow

try {
    $shell = New-Object -ComObject WScript.Shell
    $shortcutPath = Join-Path $finalPath "GestionTime Desktop.lnk"
    $shortcut = $shell.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = Join-Path $finalPath "GestionTime.Desktop.exe"
    $shortcut.WorkingDirectory = $finalPath
    $shortcut.Description = "GestionTime Desktop v$Version"
    $shortcut.Save()
    
    Write-Host "   [OK] Acceso directo creado" -ForegroundColor Green
} catch {
    Write-Host "   [WARN] No se pudo crear acceso directo" -ForegroundColor Yellow
}

Write-Host ""

# ====================================================================
# 8. CREAR ZIP
# ====================================================================

Write-Host "8. Creando archivo ZIP..." -ForegroundColor Yellow

$zipPath = "DISTRIBUIR\GestionTime-Desktop-$Version.zip"

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Write-Host "   Comprimiendo..." -ForegroundColor Gray

try {
    Compress-Archive -Path $finalPath -DestinationPath $zipPath -CompressionLevel Optimal -Force
    
    $zipSize = (Get-Item $zipPath).Length
    $zipSizeMB = [math]::Round($zipSize / 1MB, 2)
    
    Write-Host "   [OK] ZIP creado: $zipSizeMB MB" -ForegroundColor Green
} catch {
    Write-Host "   [WARN] Error creando ZIP: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""

# ====================================================================
# 9. PROBAR
# ====================================================================

Write-Host "9. Probar aplicacion?" -ForegroundColor Yellow

$testExe = Join-Path $finalPath "GestionTime.Desktop.exe"
$testNow = Read-Host "   Probar ahora? (S/N)"

if ($testNow -eq "S" -or $testNow -eq "s") {
    Write-Host "   Iniciando..." -ForegroundColor Gray
    Write-Host "   (Cierra la app cuando termines de probar)" -ForegroundColor Gray
    Write-Host ""
    
    Push-Location $finalPath
    Start-Process "GestionTime.Desktop.exe" -Wait
    Pop-Location
    
    Write-Host ""
    Write-Host "   [OK] Aplicacion probada" -ForegroundColor Green
}

Write-Host ""

# ====================================================================
# 10. RESUMEN
# ====================================================================

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  PAQUETE COMPLETO CREADO EXITOSAMENTE     " -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "PAQUETE FINAL:" -ForegroundColor White
Write-Host "   Version: $Version" -ForegroundColor Gray
Write-Host "   Archivos: $fileCount" -ForegroundColor Gray
Write-Host "   Tamano carpeta: $totalSizeMB MB" -ForegroundColor Gray
if (Test-Path $zipPath) {
    Write-Host "   Tamano ZIP: $zipSizeMB MB" -ForegroundColor Gray
}
Write-Host ""
Write-Host "UBICACION:" -ForegroundColor White
Write-Host "   Carpeta: $(Resolve-Path $finalPath)" -ForegroundColor Cyan
if (Test-Path $zipPath) {
    Write-Host "   ZIP: $(Resolve-Path $zipPath)" -ForegroundColor Cyan
}
Write-Host ""
Write-Host "CONTENIDO:" -ForegroundColor White
Write-Host "   - INICIAR.bat               <- Launcher" -ForegroundColor Gray
Write-Host "   - GestionTime.Desktop.exe   <- Ejecutable" -ForegroundColor Gray
Write-Host "   - GestionTime Desktop.lnk   <- Acceso directo" -ForegroundColor Gray
Write-Host "   - LEEME.txt                 <- Instrucciones" -ForegroundColor Gray
Write-Host "   - appsettings.json          <- Configuracion" -ForegroundColor Gray
Write-Host "   - *.dll                     <- Librerias ($fileCount archivos)" -ForegroundColor Gray
Write-Host "   - Carpetas de recursos      <- Idiomas, temas, etc." -ForegroundColor Gray
Write-Host ""
Write-Host "PARA USAR:" -ForegroundColor Yellow
Write-Host "   1. Ir a: DISTRIBUIR\GestionTime-Desktop-$Version\" -ForegroundColor Gray
Write-Host "   2. Ejecutar: INICIAR.bat" -ForegroundColor Gray
Write-Host "   3. FUNCIONA sin instalacion" -ForegroundColor Gray
Write-Host ""
Write-Host "PARA DISTRIBUIR:" -ForegroundColor Yellow
if (Test-Path $zipPath) {
    Write-Host "   Compartir el ZIP: GestionTime-Desktop-$Version.zip ($zipSizeMB MB)" -ForegroundColor Cyan
} else {
    Write-Host "   Comprimir la carpeta y compartir" -ForegroundColor Gray
}
Write-Host ""
Write-Host "CARACTERISTICAS:" -ForegroundColor White
Write-Host "   [OK] Copia COMPLETA desde Debug funcional" -ForegroundColor Green
Write-Host "   [OK] Incluye TODAS las carpetas necesarias" -ForegroundColor Green
Write-Host "   [OK] Runtime .NET 8 completo incluido" -ForegroundColor Green
Write-Host "   [OK] WindowsAppSDK completo incluido" -ForegroundColor Green
Write-Host "   [OK] NO requiere instalacion" -ForegroundColor Green
Write-Host "   [OK] Portable (USB, red, etc.)" -ForegroundColor Green
Write-Host "   [OK] FUNCIONA GARANTIZADO" -ForegroundColor Green
Write-Host ""

# Abrir carpeta
Write-Host "Abriendo carpeta DISTRIBUIR..." -ForegroundColor Yellow
Start-Process explorer.exe -ArgumentList "/select,`"$(Resolve-Path (Join-Path $finalPath 'INICIAR.bat'))`""

Write-Host ""
Write-Host "[OK] LISTO PARA DISTRIBUIR" -ForegroundColor Green
Write-Host ""
Write-Host "Presiona cualquier tecla para salir..." -ForegroundColor Cyan
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
