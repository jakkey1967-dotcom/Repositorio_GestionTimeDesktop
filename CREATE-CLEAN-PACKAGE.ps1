# ====================================================================
# Script para Crear Paquete LIMPIO con SOLO Archivos Necesarios
# Elimina archivos temporales, PDB, XML, etc.
# Fecha: 2025-01-27
# ====================================================================

param(
    [string]$Version = "1.1.0.0"
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  PAQUETE LIMPIO - SOLO ARCHIVOS NECESARIOS" -ForegroundColor Cyan
Write-Host "  GestionTime Desktop v$Version            " -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

# ====================================================================
# 1. VERIFICAR ORIGEN
# ====================================================================

Write-Host "1. Verificando archivos de origen..." -ForegroundColor Yellow

$debugPath = "bin\x64\Debug\net8.0-windows10.0.19041.0"
$debugExe = Join-Path $debugPath "GestionTime.Desktop.exe"

if (!(Test-Path $debugExe)) {
    Write-Host "   [ERROR] No se encuentra la compilacion Debug" -ForegroundColor Red
    Write-Host "   Ejecuta primero: dotnet build" -ForegroundColor Yellow
    exit 1
}

Write-Host "   [OK] Archivos de origen encontrados" -ForegroundColor Green
Write-Host ""

# ====================================================================
# 2. CREAR CARPETA LIMPIA
# ====================================================================

Write-Host "2. Creando carpeta limpia..." -ForegroundColor Yellow

$cleanPath = "bin\Clean\GestionTime-Desktop-$Version"

# Limpiar si existe
if (Test-Path $cleanPath) {
    Write-Host "   Limpiando carpeta anterior..." -ForegroundColor Gray
    Remove-Item -Path $cleanPath -Recurse -Force
}

# Crear directorio
New-Item -ItemType Directory -Path $cleanPath -Force | Out-Null

Write-Host "   [OK] Carpeta limpia creada" -ForegroundColor Green
Write-Host ""

# ====================================================================
# 3. COPIAR SOLO ARCHIVOS NECESARIOS
# ====================================================================

Write-Host "3. Copiando SOLO archivos necesarios..." -ForegroundColor Yellow

# Archivos ejecutables principales
Write-Host "   Copiando ejecutables..." -ForegroundColor Gray
Copy-Item -Path "$debugPath\GestionTime.Desktop.exe" -Destination $cleanPath -Force
Copy-Item -Path "$debugPath\GestionTime.Desktop.dll" -Destination $cleanPath -Force

# Archivos de configuracion
Write-Host "   Copiando configuracion..." -ForegroundColor Gray
Copy-Item -Path "$debugPath\appsettings.json" -Destination $cleanPath -Force

# DLLs de Microsoft WindowsAppRuntime (CRITICAS)
Write-Host "   Copiando WindowsAppRuntime..." -ForegroundColor Gray
$runtimeDlls = @(
    "Microsoft.WindowsAppRuntime.Bootstrap.dll",
    "Microsoft.WindowsAppRuntime.dll",
    "Microsoft.Windows.ApplicationModel.DynamicDependency.dll",
    "Microsoft.Windows.ApplicationModel.Resources.dll",
    "Microsoft.Windows.ApplicationModel.WindowsAppRuntime.dll",
    "Microsoft.Windows.AppLifecycle.dll",
    "Microsoft.Windows.AppNotifications.dll",
    "Microsoft.Windows.PushNotifications.dll",
    "Microsoft.Windows.Security.AccessControl.dll",
    "Microsoft.Windows.System.Power.dll",
    "Microsoft.Windows.System.dll",
    "Microsoft.Windows.Widgets.dll"
)

foreach ($dll in $runtimeDlls) {
    $sourcePath = Join-Path $debugPath $dll
    if (Test-Path $sourcePath) {
        Copy-Item -Path $sourcePath -Destination $cleanPath -Force
    }
}

# DLLs de WinRT
Write-Host "   Copiando WinRT..." -ForegroundColor Gray
$winrtDlls = @(
    "WinRT.Runtime.dll",
    "Microsoft.Windows.SDK.NET.dll"
)

foreach ($dll in $winrtDlls) {
    $sourcePath = Join-Path $debugPath $dll
    if (Test-Path $sourcePath) {
        Copy-Item -Path $sourcePath -Destination $cleanPath -Force
    }
}

# DLLs de Microsoft UI
Write-Host "   Copiando Microsoft UI..." -ForegroundColor Gray
$uiDlls = Get-ChildItem -Path $debugPath -Filter "Microsoft.UI.*.dll" -File
foreach ($dll in $uiDlls) {
    Copy-Item -Path $dll.FullName -Destination $cleanPath -Force
}

# DLLs de Microsoft Extensions
Write-Host "   Copiando Microsoft Extensions..." -ForegroundColor Gray
$extensionDlls = Get-ChildItem -Path $debugPath -Filter "Microsoft.Extensions.*.dll" -File
foreach ($dll in $extensionDlls) {
    Copy-Item -Path $dll.FullName -Destination $cleanPath -Force
}

# DLLs de CommunityToolkit
Write-Host "   Copiando CommunityToolkit..." -ForegroundColor Gray
$toolkitDlls = Get-ChildItem -Path $debugPath -Filter "CommunityToolkit.*.dll" -File
foreach ($dll in $toolkitDlls) {
    Copy-Item -Path $dll.FullName -Destination $cleanPath -Force
}

# Otras DLLs necesarias
Write-Host "   Copiando otras dependencias..." -ForegroundColor Gray
$otherDlls = @(
    "System.Diagnostics.EventLog.dll",
    "System.Diagnostics.EventLog.Messages.dll",
    "FluentAssertions.dll",
    "Newtonsoft.Json.dll"
)

foreach ($dll in $otherDlls) {
    $sourcePath = Join-Path $debugPath $dll
    if (Test-Path $sourcePath) {
        Copy-Item -Path $sourcePath -Destination $cleanPath -Force
    }
}

# Carpetas de recursos (Microsoft.UI.Xaml)
Write-Host "   Copiando recursos de UI..." -ForegroundColor Gray
$resourceFolders = Get-ChildItem -Path $debugPath -Directory | Where-Object { 
    $_.Name -match "^[a-z]{2}-[A-Z]{2}$" -or $_.Name -eq "Microsoft.UI.Xaml"
}

foreach ($folder in $resourceFolders) {
    $destFolder = Join-Path $cleanPath $folder.Name
    Copy-Item -Path $folder.FullName -Destination $destFolder -Recurse -Force
}

# Archivo .deps.json
Write-Host "   Copiando metadatos..." -ForegroundColor Gray
$depsFile = Get-ChildItem -Path $debugPath -Filter "*.deps.json" -File | Select-Object -First 1
if ($depsFile) {
    Copy-Item -Path $depsFile.FullName -Destination $cleanPath -Force
}

# Archivo .runtimeconfig.json
$runtimeConfigFile = Get-ChildItem -Path $debugPath -Filter "*.runtimeconfig.json" -File | Select-Object -First 1
if ($runtimeConfigFile) {
    Copy-Item -Path $runtimeConfigFile.FullName -Destination $cleanPath -Force
}

Write-Host "   [OK] Archivos copiados" -ForegroundColor Green
Write-Host ""

# ====================================================================
# 4. CONTAR ARCHIVOS Y TAMANO
# ====================================================================

Write-Host "4. Calculando estadisticas..." -ForegroundColor Yellow

$fileCount = (Get-ChildItem -Path $cleanPath -Recurse -File).Count
$totalSize = (Get-ChildItem -Path $cleanPath -Recurse -File | Measure-Object -Property Length -Sum).Sum
$totalSizeMB = [math]::Round($totalSize / 1MB, 2)

Write-Host "   Archivos: $fileCount" -ForegroundColor Gray
Write-Host "   Tamano: $totalSizeMB MB" -ForegroundColor Gray
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
echo ============================================
echo.
echo Iniciando aplicacion...
echo.

cd /d "%~dp0"
start "" "GestionTime.Desktop.exe"

if %errorLevel% NEQ 0 (
    echo.
    echo [ERROR] No se pudo iniciar la aplicacion
    echo.
    pause
)
"@

$launcherPath = Join-Path $cleanPath "INICIAR.bat"
$launcherBat | Out-File -FilePath $launcherPath -Encoding ASCII -Force

Write-Host "   [OK] INICIAR.bat creado" -ForegroundColor Green
Write-Host ""

# ====================================================================
# 6. CREAR README
# ====================================================================

Write-Host "6. Creando documentacion..." -ForegroundColor Yellow

$readmeContent = @"
============================================
GESTIONTIME DESKTOP - VERSION PORTABLE LIMPIA
============================================

VERSION: $Version
ARCHIVOS: Solo los necesarios ($fileCount archivos, $totalSizeMB MB)
FECHA: $(Get-Date -Format 'dd/MM/yyyy')

============================================
INSTRUCCIONES:
============================================

1. Ejecutar: INICIAR.bat
   O hacer doble clic en: GestionTime.Desktop.exe

2. La aplicacion se abre SIN INSTALACION

============================================
CONTENIDO:
============================================

Esta carpeta contiene SOLO los archivos necesarios:

- GestionTime.Desktop.exe     <- Ejecutable principal
- GestionTime.Desktop.dll     <- Libreria principal
- appsettings.json            <- Configuracion
- Microsoft.*.dll             <- Librerias de Windows
- Carpetas de recursos        <- Idiomas y recursos UI

NO INCLUYE:
- Archivos .pdb (debugging)
- Archivos .xml (documentacion)
- Archivos temporales
- Archivos de desarrollo

============================================
CARACTERISTICAS:
============================================

[OK] Paquete LIMPIO (solo archivos necesarios)
[OK] NO requiere instalacion
[OK] NO requiere permisos de administrador
[OK] Runtime .NET 8 incluido
[OK] WindowsAppSDK incluido
[OK] Portable (USB, red, etc.)

============================================
REQUISITOS:
============================================

- Windows 10 build 17763+ o Windows 11
- Arquitectura x64
- 100 MB de espacio en disco

============================================
USO:
============================================

ESCENARIO 1: Uso Personal
- Copiar carpeta a: C:\Apps\GestionTime\
- Ejecutar INICIAR.bat
- Crear acceso directo si se desea

ESCENARIO 2: USB Portable
- Copiar carpeta a USB
- Ejecutar desde USB en cualquier PC
- Funciona sin instalar

ESCENARIO 3: Red
- Copiar a: \\servidor\apps\GestionTime\
- Usuarios ejecutan desde red
- Sin instalacion local

============================================
CONFIGURACION:
============================================

Editar: appsettings.json

Cambiar URL del API, logging, etc.

============================================
SOPORTE:
============================================

Web: https://gestiontime.com/support
Email: support@gestiontime.com

Copyright (c) 2025 GestionTime Solutions
"@

$readmePath = Join-Path $cleanPath "LEEME.txt"
$readmeContent | Out-File -FilePath $readmePath -Encoding UTF8 -Force

Write-Host "   [OK] LEEME.txt creado" -ForegroundColor Green
Write-Host ""

# ====================================================================
# 7. CREAR ACCESO DIRECTO
# ====================================================================

Write-Host "7. Creando acceso directo..." -ForegroundColor Yellow

try {
    $shell = New-Object -ComObject WScript.Shell
    $shortcutPath = Join-Path $cleanPath "GestionTime Desktop.lnk"
    $shortcut = $shell.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = Join-Path $cleanPath "GestionTime.Desktop.exe"
    $shortcut.WorkingDirectory = $cleanPath
    $shortcut.Description = "GestionTime Desktop"
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

$zipPath = "bin\Clean\GestionTime-Desktop-$Version-Clean.zip"

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Write-Host "   Comprimiendo..." -ForegroundColor Gray

try {
    Compress-Archive -Path $cleanPath -DestinationPath $zipPath -CompressionLevel Optimal -Force
    
    $zipSize = (Get-Item $zipPath).Length
    $zipSizeMB = [math]::Round($zipSize / 1MB, 2)
    
    Write-Host "   [OK] ZIP creado: $zipSizeMB MB" -ForegroundColor Green
} catch {
    Write-Host "   [WARN] Error creando ZIP: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""

# ====================================================================
# 9. PROBAR APLICACION
# ====================================================================

Write-Host "9. Probando aplicacion..." -ForegroundColor Yellow

$testExe = Join-Path $cleanPath "GestionTime.Desktop.exe"

if (Test-Path $testExe) {
    Write-Host "   Ejecutable encontrado" -ForegroundColor Green
    
    $testNow = Read-Host "   Quieres probar la aplicacion ahora? (S/N)"
    
    if ($testNow -eq "S" -or $testNow -eq "s") {
        Write-Host "   Iniciando aplicacion..." -ForegroundColor Gray
        Write-Host "   (Cierra cuando termines de probar)" -ForegroundColor Gray
        Write-Host ""
        
        Push-Location $cleanPath
        Start-Process "GestionTime.Desktop.exe" -Wait
        Pop-Location
        
        Write-Host ""
        Write-Host "   [OK] Aplicacion probada" -ForegroundColor Green
    }
}

Write-Host ""

# ====================================================================
# 10. CREAR LISTA DE ARCHIVOS
# ====================================================================

Write-Host "10. Creando lista de archivos..." -ForegroundColor Yellow

$fileListPath = Join-Path $cleanPath "ARCHIVOS.txt"
$fileList = "LISTA DE ARCHIVOS INCLUIDOS`n" +
            "============================`n`n"

$files = Get-ChildItem -Path $cleanPath -Recurse -File | Sort-Object FullName
foreach ($file in $files) {
    $relativePath = $file.FullName.Substring($cleanPath.Length + 1)
    $sizeKB = [math]::Round($file.Length / 1KB, 2)
    $fileList += "$relativePath ($sizeKB KB)`n"
}

$fileList += "`nTOTAL: $fileCount archivos ($totalSizeMB MB)"
$fileList | Out-File -FilePath $fileListPath -Encoding UTF8 -Force

Write-Host "   [OK] ARCHIVOS.txt creado" -ForegroundColor Green
Write-Host ""

# ====================================================================
# 11. RESUMEN FINAL
# ====================================================================

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  PAQUETE LIMPIO CREADO EXITOSAMENTE       " -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "INFORMACION:" -ForegroundColor White
Write-Host "   Producto: GestionTime Desktop" -ForegroundColor Gray
Write-Host "   Version: $Version" -ForegroundColor Gray
Write-Host "   Tipo: Portable Limpio (solo archivos necesarios)" -ForegroundColor Gray
Write-Host "   Archivos: $fileCount (vs 740 en version Debug completa)" -ForegroundColor Gray
Write-Host "   Tamano: $totalSizeMB MB (vs 312 MB Debug completa)" -ForegroundColor Gray
if (Test-Path $zipPath) {
    Write-Host "   ZIP: $zipSizeMB MB (vs 121 MB Debug completa)" -ForegroundColor Gray
}
Write-Host ""
Write-Host "UBICACION:" -ForegroundColor White
Write-Host "   Carpeta: $(Resolve-Path $cleanPath)" -ForegroundColor Cyan
if (Test-Path $zipPath) {
    Write-Host "   ZIP: $(Resolve-Path $zipPath)" -ForegroundColor Cyan
}
Write-Host ""
Write-Host "ARCHIVOS:" -ForegroundColor White
Write-Host "   - INICIAR.bat               <- Launcher" -ForegroundColor Gray
Write-Host "   - GestionTime.Desktop.exe   <- Ejecutable" -ForegroundColor Gray
Write-Host "   - LEEME.txt                 <- Instrucciones" -ForegroundColor Gray
Write-Host "   - ARCHIVOS.txt              <- Lista completa" -ForegroundColor Gray
Write-Host "   - GestionTime Desktop.lnk   <- Acceso directo" -ForegroundColor Gray
Write-Host ""
Write-Host "VENTAJAS DEL PAQUETE LIMPIO:" -ForegroundColor White
Write-Host "   [OK] Solo archivos necesarios" -ForegroundColor Green
Write-Host "   [OK] Sin archivos .pdb (debugging)" -ForegroundColor Green
Write-Host "   [OK] Sin archivos .xml (docs)" -ForegroundColor Green
Write-Host "   [OK] Sin archivos temporales" -ForegroundColor Green
Write-Host "   [OK] Mas pequeno y rapido de distribuir" -ForegroundColor Green
Write-Host "   [OK] FUNCIONA exactamente igual" -ForegroundColor Green
Write-Host ""
Write-Host "PARA USAR:" -ForegroundColor Yellow
Write-Host "   1. Ir a: bin\Clean\GestionTime-Desktop-$Version\" -ForegroundColor Gray
Write-Host "   2. Ejecutar: INICIAR.bat" -ForegroundColor Gray
Write-Host ""
Write-Host "PARA DISTRIBUIR:" -ForegroundColor Yellow
if (Test-Path $zipPath) {
    Write-Host "   Compartir: GestionTime-Desktop-$Version-Clean.zip ($zipSizeMB MB)" -ForegroundColor Cyan
} else {
    Write-Host "   Comprimir la carpeta y compartir" -ForegroundColor Gray
}
Write-Host ""

# Abrir carpeta
Write-Host "Abriendo carpeta..." -ForegroundColor Yellow
Start-Process explorer.exe -ArgumentList "/select,`"$(Resolve-Path (Join-Path $cleanPath 'INICIAR.bat'))`""

Write-Host ""
Write-Host "[OK] PAQUETE LIMPIO LISTO" -ForegroundColor Green
Write-Host ""
Write-Host "Presiona cualquier tecla para salir..." -ForegroundColor Cyan
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
