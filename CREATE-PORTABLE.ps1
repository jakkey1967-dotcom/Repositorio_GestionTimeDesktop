# ====================================================================
# Crear Paquete Portable de GestionTime Desktop
# NO requiere instalacion - Ejecutar directamente
# Fecha: 2025-01-27
# ====================================================================

param(
    [string]$Version = "1.1.0.0"
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  CREAR PAQUETE PORTABLE (SIN INSTALACION) " -ForegroundColor Cyan
Write-Host "  GestionTime Desktop v$Version            " -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

# ====================================================================
# 1. LIMPIAR Y PUBLICAR
# ====================================================================

Write-Host "1. Publicando aplicacion..." -ForegroundColor Yellow

$publishPath = "bin\Release\Portable\GestionTime-Desktop-$Version"

# Limpiar
if (Test-Path $publishPath) {
    Write-Host "   Limpiando compilacion anterior..." -ForegroundColor Gray
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
    "-p:IncludeNativeLibrariesForSelfExtract=true"
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
# 2. VERIFICAR ARCHIVOS CRITICOS
# ====================================================================

Write-Host "2. Verificando archivos criticos..." -ForegroundColor Yellow

$criticalFiles = @(
    "GestionTime.Desktop.exe",
    "GestionTime.Desktop.dll",
    "appsettings.json",
    "Microsoft.WindowsAppRuntime.Bootstrap.dll",
    "Microsoft.WindowsAppRuntime.dll"
)

$allOk = $true
foreach ($file in $criticalFiles) {
    $filePath = Join-Path $publishPath $file
    if (Test-Path $filePath) {
        Write-Host "   [OK] $file" -ForegroundColor Green
    } else {
        Write-Host "   [ERROR] Falta: $file" -ForegroundColor Red
        $allOk = $false
    }
}

if (!$allOk) {
    Write-Host ""
    Write-Host "[ERROR] Faltan archivos criticos" -ForegroundColor Red
    exit 1
}

Write-Host ""

# ====================================================================
# 3. CREAR LAUNCHER SIMPLE
# ====================================================================

Write-Host "3. Creando launcher..." -ForegroundColor Yellow

$launcherBat = @"
@echo off
title GestionTime Desktop

echo ============================================
echo   GESTIONTIME DESKTOP v$Version
echo ============================================
echo.
echo Iniciando aplicacion...
echo.

start "" "%~dp0GestionTime.Desktop.exe"

if %errorLevel% NEQ 0 (
    echo.
    echo [ERROR] No se pudo iniciar la aplicacion
    echo.
    pause
)
"@

$launcherPath = Join-Path $publishPath "INICIAR.bat"
$launcherBat | Out-File -FilePath $launcherPath -Encoding ASCII -Force

Write-Host "   [OK] Launcher creado: INICIAR.bat" -ForegroundColor Green
Write-Host ""

# ====================================================================
# 4. CREAR README
# ====================================================================

Write-Host "4. Creando documentacion..." -ForegroundColor Yellow

$readmeContent = @"
============================================
GESTIONTIME DESKTOP - VERSION PORTABLE
============================================

VERSION: $Version
FECHA: $(Get-Date -Format 'dd/MM/yyyy')

COMO USAR:
----------

1. Descomprimir la carpeta completa

2. Ejecutar INICIAR.bat
   O hacer doble clic en: GestionTime.Desktop.exe

3. La aplicacion se ejecuta SIN INSTALACION

IMPORTANTE:
-----------

* NO requiere instalacion
* NO requiere permisos de administrador
* Todos los archivos necesarios estan incluidos
* Runtime .NET 8 incluido (self-contained)
* WindowsAppSDK incluido

PORTABLE:
---------

Puedes copiar esta carpeta completa a:
- USB
- Otra carpeta
- Otro PC (Windows 10/11 x64)

Y ejecutar directamente sin instalar.

REQUISITOS:
-----------

- Windows 10 version 1809 (build 17763) o superior
- Windows 11 (recomendado)
- Arquitectura x64

ARCHIVOS:
---------

- INICIAR.bat           <- Launcher (opcional)
- GestionTime.Desktop.exe <- Ejecutable principal
- appsettings.json      <- Configuracion
- *.dll                 <- Librerias necesarias

CONFIGURACION:
--------------

Editar appsettings.json para cambiar:
- URL del servidor API
- Nivel de logging
- Otras configuraciones

SOPORTE:
--------

Para soporte tecnico:
https://gestiontime.com/support

LICENCIA:
---------

Copyright (c) 2025 GestionTime Solutions
Todos los derechos reservados.
"@

$readmePath = Join-Path $publishPath "LEEME.txt"
$readmeContent | Out-File -FilePath $readmePath -Encoding UTF8 -Force

Write-Host "   [OK] Documentacion creada: LEEME.txt" -ForegroundColor Green
Write-Host ""

# ====================================================================
# 5. CREAR ACCESO DIRECTO (OPCIONAL)
# ====================================================================

Write-Host "5. Creando acceso directo..." -ForegroundColor Yellow

$shell = New-Object -ComObject WScript.Shell
$shortcutPath = Join-Path $publishPath "GestionTime Desktop.lnk"
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = Join-Path $publishPath "GestionTime.Desktop.exe"
$shortcut.WorkingDirectory = $publishPath
$shortcut.Description = "GestionTime Desktop - Gestion de tiempo y partes de trabajo"
$shortcut.IconLocation = Join-Path $publishPath "GestionTime.Desktop.exe"
$shortcut.Save()

Write-Host "   [OK] Acceso directo creado" -ForegroundColor Green
Write-Host ""

# ====================================================================
# 6. CREAR ZIP PARA DISTRIBUCION
# ====================================================================

Write-Host "6. Creando archivo ZIP para distribucion..." -ForegroundColor Yellow

$zipPath = "bin\Release\Portable\GestionTime-Desktop-$Version-Portable.zip"

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Write-Host "   Comprimiendo archivos..." -ForegroundColor Gray

try {
    Compress-Archive -Path $publishPath -DestinationPath $zipPath -CompressionLevel Optimal
    
    $zipSize = (Get-Item $zipPath).Length
    $zipSizeMB = [math]::Round($zipSize / 1MB, 2)
    
    Write-Host "   [OK] ZIP creado: $zipSizeMB MB" -ForegroundColor Green
} catch {
    Write-Host "   [WARN] No se pudo crear el ZIP automaticamente" -ForegroundColor Yellow
    Write-Host "   Puedes comprimir manualmente la carpeta:" -ForegroundColor Gray
    Write-Host "   $publishPath" -ForegroundColor Gray
}

Write-Host ""

# ====================================================================
# 7. PROBAR APLICACION
# ====================================================================

Write-Host "7. Probando aplicacion..." -ForegroundColor Yellow

$exePath = Join-Path $publishPath "GestionTime.Desktop.exe"

if (Test-Path $exePath) {
    Write-Host "   Ejecutable encontrado: GestionTime.Desktop.exe" -ForegroundColor Green
    
    $testLaunch = Read-Host "   Quieres probar la aplicacion ahora? (S/N)"
    
    if ($testLaunch -eq "S" -or $testLaunch -eq "s") {
        Write-Host "   Iniciando aplicacion de prueba..." -ForegroundColor Gray
        Write-Host "   (Cierra la aplicacion cuando termines de probar)" -ForegroundColor Gray
        
        Start-Process $exePath -WorkingDirectory $publishPath -Wait
        
        Write-Host "   [OK] Aplicacion probada" -ForegroundColor Green
    }
} else {
    Write-Host "   [ERROR] No se encontro el ejecutable" -ForegroundColor Red
}

Write-Host ""

# ====================================================================
# 8. RESUMEN FINAL
# ====================================================================

$stopwatch.Stop()
$elapsed = $stopwatch.Elapsed.TotalSeconds

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  PAQUETE PORTABLE CREADO EXITOSAMENTE     " -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Informacion del paquete:" -ForegroundColor White
Write-Host "   * Producto: GestionTime Desktop" -ForegroundColor Gray
Write-Host "   * Version: $Version" -ForegroundColor Gray
Write-Host "   * Tipo: Portable (sin instalacion)" -ForegroundColor Gray
Write-Host "   * Plataforma: Windows 10/11 x64" -ForegroundColor Gray
Write-Host "   * Archivos: $fileCount" -ForegroundColor Gray
Write-Host "   * Tamano carpeta: $totalSizeMB MB" -ForegroundColor Gray
if (Test-Path $zipPath) {
    Write-Host "   * Tamano ZIP: $zipSizeMB MB" -ForegroundColor Gray
}
Write-Host "   * Tiempo: $([math]::Round($elapsed, 1)) segundos" -ForegroundColor Gray
Write-Host ""
Write-Host "Ubicacion:" -ForegroundColor White
Write-Host "   Carpeta: $(Resolve-Path $publishPath)" -ForegroundColor Cyan
if (Test-Path $zipPath) {
    Write-Host "   ZIP: $(Resolve-Path $zipPath)" -ForegroundColor Cyan
}
Write-Host ""
Write-Host "Caracteristicas:" -ForegroundColor White
Write-Host "   [OK] NO requiere instalacion" -ForegroundColor Green
Write-Host "   [OK] NO requiere permisos de administrador" -ForegroundColor Green
Write-Host "   [OK] Runtime .NET 8 incluido (self-contained)" -ForegroundColor Green
Write-Host "   [OK] WindowsAppSDK incluido" -ForegroundColor Green
Write-Host "   [OK] Completamente portable" -ForegroundColor Green
Write-Host "   [OK] Ejecutar desde USB, carpeta, red, etc." -ForegroundColor Green
Write-Host ""
Write-Host "Archivos importantes:" -ForegroundColor White
Write-Host "   - INICIAR.bat                <- Launcher rapido" -ForegroundColor Gray
Write-Host "   - GestionTime.Desktop.exe    <- Ejecutable principal" -ForegroundColor Gray
Write-Host "   - GestionTime Desktop.lnk    <- Acceso directo" -ForegroundColor Gray
Write-Host "   - LEEME.txt                  <- Instrucciones" -ForegroundColor Gray
Write-Host "   - appsettings.json           <- Configuracion" -ForegroundColor Gray
Write-Host ""
Write-Host "Para usar:" -ForegroundColor Yellow
Write-Host "   1. Descomprimir la carpeta (si es ZIP)" -ForegroundColor Gray
Write-Host "   2. Ejecutar INICIAR.bat" -ForegroundColor Gray
Write-Host "   O hacer doble clic en: GestionTime.Desktop.exe" -ForegroundColor Gray
Write-Host ""
Write-Host "Para distribuir:" -ForegroundColor Yellow
if (Test-Path $zipPath) {
    Write-Host "   Distribuir el archivo ZIP:" -ForegroundColor Gray
    Write-Host "   GestionTime-Desktop-$Version-Portable.zip" -ForegroundColor Cyan
} else {
    Write-Host "   1. Comprimir la carpeta manualmente" -ForegroundColor Gray
    Write-Host "   2. Distribuir el ZIP" -ForegroundColor Gray
}
Write-Host ""

# Abrir carpeta
Write-Host "Abriendo carpeta del paquete portable..." -ForegroundColor Yellow
Start-Process explorer.exe -ArgumentList "/select,`"$(Resolve-Path (Join-Path $publishPath 'INICIAR.bat'))`""

Write-Host ""
Write-Host "[OK] Proceso completado exitosamente" -ForegroundColor Green
Write-Host ""
Write-Host "Presiona cualquier tecla para salir..." -ForegroundColor Cyan
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
