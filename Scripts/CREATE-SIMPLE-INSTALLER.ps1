# ====================================================================
# Instalador PowerShell Autocontenido para GestionTime Desktop
# No requiere herramientas externas
# Fecha: 2025-01-27
# ====================================================================

param(
    [string]$Version = "1.1.0.0"
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  CREAR INSTALADOR AUTOCONTENIDO           " -ForegroundColor Cyan
Write-Host "  GestionTime Desktop v$Version            " -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

# ====================================================================
# 1. PUBLICAR APLICACION
# ====================================================================

Write-Host "1. Publicando aplicacion en Release..." -ForegroundColor Yellow

$publishPath = "bin\Release\Installer\App"

# Limpiar
if (Test-Path $publishPath) {
    Remove-Item -Path $publishPath -Recurse -Force
}

# Publicar
Write-Host "   Compilando aplicacion (esto puede tardar)..." -ForegroundColor Gray

$publishArgs = @(
    "publish"
    "GestionTime.Desktop.csproj"
    "-c", "Release"
    "-r", "win-x64"
    "--self-contained", "true"
    "-p:PublishSingleFile=false"
    "-p:PublishReadyToRun=true"
    "-o", $publishPath
    "-v", "quiet"
)

& dotnet @publishArgs | Out-Null

if ($LASTEXITCODE -ne 0) {
    Write-Host "   [ERROR] Error publicando" -ForegroundColor Red
    exit 1
}

$fileCount = (Get-ChildItem -Path $publishPath -Recurse -File).Count
$totalSize = (Get-ChildItem -Path $publishPath -Recurse -File | Measure-Object -Property Length -Sum).Sum
$totalSizeMB = [math]::Round($totalSize / 1MB, 2)

Write-Host "   [OK] Aplicacion publicada: $fileCount archivos ($totalSizeMB MB)" -ForegroundColor Green
Write-Host ""

# ====================================================================
# 2. CREAR SCRIPT INSTALADOR
# ====================================================================

Write-Host "2. Creando script instalador..." -ForegroundColor Yellow

$installerScript = @'
# INSTALADOR DE GESTIONTIME DESKTOP
# Ejecutar como Administrador

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  INSTALADOR - GESTIONTIME DESKTOP         " -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

# Verificar permisos de administrador
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (!$isAdmin) {
    Write-Host "[ERROR] Este instalador requiere permisos de administrador" -ForegroundColor Red
    Write-Host "Ejecutar como administrador (click derecho > Ejecutar como administrador)" -ForegroundColor Yellow
    Read-Host "Presiona Enter para salir"
    exit 1
}

Write-Host "1. Bienvenido al instalador de GestionTime Desktop" -ForegroundColor Green
Write-Host ""

# Directorio de instalacion predeterminado
$defaultInstallDir = "$env:ProgramFiles\GestionTime Desktop"
Write-Host "Directorio de instalacion predeterminado:" -ForegroundColor Yellow
Write-Host "   $defaultInstallDir" -ForegroundColor White
Write-Host ""

$response = Read-Host "Presiona Enter para continuar o escribe una ruta diferente"

if ($response) {
    $installDir = $response
} else {
    $installDir = $defaultInstallDir
}

Write-Host ""
Write-Host "2. Instalando en: $installDir" -ForegroundColor Green
Write-Host ""

# Crear directorio de instalacion
if (!(Test-Path $installDir)) {
    New-Item -ItemType Directory -Path $installDir -Force | Out-Null
}

# Copiar archivos
Write-Host "3. Copiando archivos..." -ForegroundColor Yellow

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$appSource = Join-Path $scriptDir "App"

if (!(Test-Path $appSource)) {
    Write-Host "[ERROR] No se encontraron los archivos de la aplicacion" -ForegroundColor Red
    Read-Host "Presiona Enter para salir"
    exit 1
}

$files = Get-ChildItem -Path $appSource -Recurse -File
$current = 0

foreach ($file in $files) {
    $current++
    $percent = [math]::Round(($current / $files.Count) * 100)
    Write-Progress -Activity "Instalando archivos" -Status "$percent% completado" -PercentComplete $percent
    
    $relativePath = $file.FullName.Substring($appSource.Length + 1)
    $destPath = Join-Path $installDir $relativePath
    $destDir = Split-Path -Parent $destPath
    
    if (!(Test-Path $destDir)) {
        New-Item -ItemType Directory -Path $destDir -Force | Out-Null
    }
    
    Copy-Item -Path $file.FullName -Destination $destPath -Force
}

Write-Progress -Activity "Instalando archivos" -Completed

Write-Host "   [OK] $($files.Count) archivos instalados" -ForegroundColor Green
Write-Host ""

# Crear acceso directo en Menu Inicio
Write-Host "4. Creando accesos directos..." -ForegroundColor Yellow

$exePath = Join-Path $installDir "GestionTime.Desktop.exe"

# Acceso directo en Menu Inicio
$startMenuPath = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs"
$shortcutPath = Join-Path $startMenuPath "GestionTime Desktop.lnk"

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $exePath
$shortcut.WorkingDirectory = $installDir
$shortcut.Description = "Aplicacion de gestion de tiempo"
$shortcut.Save()

Write-Host "   [OK] Acceso directo creado en Menu Inicio" -ForegroundColor Green

# Preguntar por acceso directo en Escritorio
$createDesktop = Read-Host "Crear acceso directo en Escritorio? (S/N)"

if ($createDesktop -eq "S" -or $createDesktop -eq "s") {
    $desktopPath = [Environment]::GetFolderPath("CommonDesktopDirectory")
    $desktopShortcutPath = Join-Path $desktopPath "GestionTime Desktop.lnk"
    
    $desktopShortcut = $shell.CreateShortcut($desktopShortcutPath)
    $desktopShortcut.TargetPath = $exePath
    $desktopShortcut.WorkingDirectory = $installDir
    $desktopShortcut.Description = "Aplicacion de gestion de tiempo"
    $desktopShortcut.Save()
    
    Write-Host "   [OK] Acceso directo creado en Escritorio" -ForegroundColor Green
}

Write-Host ""

# Registrar en Panel de Control
Write-Host "5. Registrando aplicacion en Panel de Control..." -ForegroundColor Yellow

$uninstallPath = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop"

if (!(Test-Path $uninstallPath)) {
    New-Item -Path $uninstallPath -Force | Out-Null
}

Set-ItemProperty -Path $uninstallPath -Name "DisplayName" -Value "GestionTime Desktop"
Set-ItemProperty -Path $uninstallPath -Name "DisplayVersion" -Value "1.1.0.0"
Set-ItemProperty -Path $uninstallPath -Name "Publisher" -Value "GestionTime Solutions"
Set-ItemProperty -Path $uninstallPath -Name "InstallLocation" -Value $installDir
Set-ItemProperty -Path $uninstallPath -Name "DisplayIcon" -Value "$exePath,0"
Set-ItemProperty -Path $uninstallPath -Name "UninstallString" -Value "powershell.exe -ExecutionPolicy Bypass -File `"$installDir\Uninstall.ps1`""

# Crear script de desinstalacion
$uninstallScript = @"
# Desinstalador de GestionTime Desktop

`$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "DESINSTALANDO GESTIONTIME DESKTOP" -ForegroundColor Yellow
Write-Host ""

`$installDir = "$installDir"

if (Test-Path `$installDir) {
    Remove-Item -Path `$installDir -Recurse -Force
    Write-Host "[OK] Archivos eliminados" -ForegroundColor Green
}

# Eliminar accesos directos
`$startMenuPath = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\GestionTime Desktop.lnk"
if (Test-Path `$startMenuPath) {
    Remove-Item -Path `$startMenuPath -Force
}

`$desktopPath = "[Environment]::GetFolderPath('CommonDesktopDirectory')"
`$desktopShortcut = Join-Path `$desktopPath "GestionTime Desktop.lnk"
if (Test-Path `$desktopShortcut) {
    Remove-Item -Path `$desktopShortcut -Force
}

# Eliminar registro
Remove-Item -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop" -Force -ErrorAction SilentlyContinue

Write-Host "[OK] Desinstalacion completada" -ForegroundColor Green
Write-Host ""
Read-Host "Presiona Enter para salir"
"@

$uninstallScript | Out-File -FilePath (Join-Path $installDir "Uninstall.ps1") -Encoding UTF8 -Force

Write-Host "   [OK] Aplicacion registrada" -ForegroundColor Green
Write-Host ""

# Finalizar
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  INSTALACION COMPLETADA EXITOSAMENTE      " -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "GestionTime Desktop se ha instalado en:" -ForegroundColor White
Write-Host "   $installDir" -ForegroundColor Cyan
Write-Host ""
Write-Host "Puedes iniciar la aplicacion desde:" -ForegroundColor White
Write-Host "   - Menu Inicio > GestionTime Desktop" -ForegroundColor Gray
if ($createDesktop -eq "S" -or $createDesktop -eq "s") {
    Write-Host "   - Escritorio > GestionTime Desktop" -ForegroundColor Gray
}
Write-Host ""

$launch = Read-Host "Iniciar GestionTime Desktop ahora? (S/N)"

if ($launch -eq "S" -or $launch -eq "s") {
    Start-Process $exePath
}

Write-Host ""
Write-Host "Gracias por instalar GestionTime Desktop!" -ForegroundColor Green
Write-Host ""
Read-Host "Presiona Enter para salir"
'@

$installerScriptPath = Join-Path $publishPath "..\Install.ps1"
$installerScript | Out-File -FilePath $installerScriptPath -Encoding UTF8 -Force

Write-Host "   [OK] Script instalador creado" -ForegroundColor Green
Write-Host ""

# ====================================================================
# 3. CREAR ARCHIVO BAT LAUNCHER
# ====================================================================

Write-Host "3. Creando launcher..." -ForegroundColor Yellow

$launcherBat = @'
@echo off
title Instalador GestionTime Desktop

echo =============================================
echo   INSTALADOR - GESTIONTIME DESKTOP
echo =============================================
echo.
echo Verificando permisos de administrador...

net session >nul 2>&1
if %errorLevel% == 0 (
    echo [OK] Ejecutando como administrador
    echo.
    powershell.exe -ExecutionPolicy Bypass -File "%~dp0Install.ps1"
) else (
    echo [ERROR] Este instalador requiere permisos de administrador
    echo.
    echo Solicitar permisos de administrador...
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit
)

pause
'@

$launcherPath = Join-Path $publishPath "..\INSTALAR.bat"
$launcherBat | Out-File -FilePath $launcherPath -Encoding ASCII -Force

Write-Host "   [OK] Launcher creado" -ForegroundColor Green
Write-Host ""

# ====================================================================
# 4. CREAR README
# ====================================================================

Write-Host "4. Creando documentacion..." -ForegroundColor Yellow

$readmeContent = @"
============================================
GESTIONTIME DESKTOP - INSTALADOR v$Version
============================================

INSTRUCCIONES DE INSTALACION:
------------------------------

1. Ejecutar INSTALAR.bat como Administrador

2. Seguir las instrucciones en pantalla

3. La aplicacion se instalara en:
   C:\Program Files\GestionTime Desktop\

4. Accesos directos se crearan en:
   - Menu Inicio
   - Escritorio (opcional)


REQUISITOS DEL SISTEMA:
-----------------------

- Windows 10 version 1809 o superior
- Windows 11 (recomendado)
- Arquitectura x64
- 500 MB de espacio en disco


DESINSTALACION:
---------------

Panel de Control > Programas y caracteristicas > GestionTime Desktop > Desinstalar


SOPORTE TECNICO:
----------------

Para soporte, visite:
https://gestiontime.com/support


Copyright (c) 2025 GestionTime Solutions
"@

$readmePath = Join-Path $publishPath "..\LEEME.txt"
$readmeContent | Out-File -FilePath $readmePath -Encoding UTF8 -Force

Write-Host "   [OK] Documentacion creada" -ForegroundColor Green
Write-Host ""

# ====================================================================
# 5. RESUMEN
# ====================================================================

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  PAQUETE INSTALADOR CREADO EXITOSAMENTE   " -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Ubicacion del paquete:" -ForegroundColor White
Write-Host "   $(Resolve-Path 'bin\Release\Installer')" -ForegroundColor Cyan
Write-Host ""
Write-Host "Contenido:" -ForegroundColor White
Write-Host "   - INSTALAR.bat       (Ejecutar esto)" -ForegroundColor Gray
Write-Host "   - Install.ps1        (Script de instalacion)" -ForegroundColor Gray
Write-Host "   - LEEME.txt          (Instrucciones)" -ForegroundColor Gray
Write-Host "   - App\               (Archivos de la aplicacion)" -ForegroundColor Gray
Write-Host ""
Write-Host "Archivos incluidos: $fileCount" -ForegroundColor White
Write-Host "Tamano total: $totalSizeMB MB" -ForegroundColor White
Write-Host ""
Write-Host "Para distribuir:" -ForegroundColor Yellow
Write-Host "   1. Comprimir la carpeta 'Installer' en ZIP" -ForegroundColor Gray
Write-Host "   2. Distribuir el archivo ZIP" -ForegroundColor Gray
Write-Host "   3. Usuario descomprime y ejecuta INSTALAR.bat" -ForegroundColor Gray
Write-Host ""

# Abrir carpeta
Write-Host "Abriendo carpeta del instalador..." -ForegroundColor Yellow
Start-Process explorer.exe -ArgumentList "/select,`"$(Resolve-Path 'bin\Release\Installer\INSTALAR.bat')`""

Write-Host ""
Write-Host "[OK] Proceso completado" -ForegroundColor Green
Write-Host ""
Write-Host "Presiona cualquier tecla para salir..." -ForegroundColor Cyan
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
