# ====================================================================
# Script Completo para Crear Instalador EXE de GestionTime Desktop
# Fecha: 2025-01-27
# Version: 1.1.0.0
# ====================================================================

param(
    [string]$Version = "1.1.0.0",
    [switch]$SkipBuild = $false,
    [switch]$TestApp = $false
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  INSTALADOR EXE - GESTIONTIME DESKTOP     " -ForegroundColor Cyan
Write-Host "  Version: $Version                        " -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

# ====================================================================
# 1. VERIFICAR HERRAMIENTAS
# ====================================================================

Write-Host "1. Verificando herramientas necesarias..." -ForegroundColor Yellow

# Verificar dotnet
if (!(Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "   [ERROR] .NET SDK no encontrado" -ForegroundColor Red
    Write-Host "   Instalar desde: https://dot.net" -ForegroundColor Yellow
    exit 1
}
Write-Host "   [OK] .NET SDK encontrado" -ForegroundColor Green

# Verificar Inno Setup
$innoPath = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
if (!(Test-Path $innoPath)) {
    Write-Host "   [WARN] Inno Setup no encontrado" -ForegroundColor Yellow
    Write-Host "   Instalando Inno Setup..." -ForegroundColor Gray
    
    try {
        winget install JRSoftware.InnoSetup --accept-package-agreements --accept-source-agreements
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   [OK] Inno Setup instalado correctamente" -ForegroundColor Green
        } else {
            Write-Host "   [ERROR] No se pudo instalar Inno Setup" -ForegroundColor Red
            Write-Host "   Instalar manualmente desde: https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
            exit 1
        }
    } catch {
        Write-Host "   [ERROR] Error instalando Inno Setup: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "   [OK] Inno Setup encontrado" -ForegroundColor Green
}

Write-Host ""

# ====================================================================
# 2. LIMPIAR Y PUBLICAR APLICACION
# ====================================================================

if (!$SkipBuild) {
    Write-Host "2. Publicando aplicacion en Release..." -ForegroundColor Yellow
    
    # Limpiar compilaciones anteriores
    Write-Host "   Limpiando compilaciones anteriores..." -ForegroundColor Gray
    
    $foldersToClean = @(
        "bin\Release\net8.0-windows10.0.19041.0",
        "bin\Release\Installer",
        "obj\Release"
    )
    
    foreach ($folder in $foldersToClean) {
        if (Test-Path $folder) {
            Remove-Item -Path $folder -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
    
    # Publicar aplicacion
    Write-Host "   Publicando aplicacion (esto puede tardar unos minutos)..." -ForegroundColor Gray
    
    $publishPath = "bin\Release\Installer\App"
    
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
    
    $publishOutput = & dotnet @publishArgs 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "   [ERROR] Error publicando aplicacion" -ForegroundColor Red
        Write-Host "   $publishOutput" -ForegroundColor Gray
        exit 1
    }
    
    Write-Host "   [OK] Aplicacion publicada exitosamente" -ForegroundColor Green
    
    # Contar archivos
    $fileCount = (Get-ChildItem -Path $publishPath -Recurse -File).Count
    $totalSize = (Get-ChildItem -Path $publishPath -Recurse -File | Measure-Object -Property Length -Sum).Sum
    $totalSizeMB = [math]::Round($totalSize / 1MB, 2)
    
    Write-Host "   Archivos generados: $fileCount ($totalSizeMB MB)" -ForegroundColor Gray
    
} else {
    Write-Host "2. Saltando compilacion (usando archivos existentes)..." -ForegroundColor Yellow
    
    $publishPath = "bin\Release\Installer\App"
    
    if (!(Test-Path $publishPath)) {
        Write-Host "   [ERROR] No se encontraron archivos publicados" -ForegroundColor Red
        Write-Host "   Ejecuta sin -SkipBuild" -ForegroundColor Yellow
        exit 1
    }
}

Write-Host ""

# ====================================================================
# 3. VERIFICAR ARCHIVOS CRITICOS
# ====================================================================

Write-Host "3. Verificando archivos criticos..." -ForegroundColor Yellow

$criticalFiles = @(
    "GestionTime.Desktop.exe",
    "GestionTime.Desktop.dll",
    "appsettings.json",
    "Microsoft.WindowsAppRuntime.Bootstrap.dll",
    "Microsoft.WindowsAppRuntime.dll",
    "Microsoft.Windows.ApplicationModel.DynamicDependency.dll"
)

$missingFiles = @()
foreach ($file in $criticalFiles) {
    $filePath = Join-Path $publishPath $file
    if (!(Test-Path $filePath)) {
        $missingFiles += $file
    }
}

if ($missingFiles.Count -gt 0) {
    Write-Host "   [ERROR] Archivos criticos faltantes:" -ForegroundColor Red
    foreach ($file in $missingFiles) {
        Write-Host "      - $file" -ForegroundColor Gray
    }
    exit 1
}

Write-Host "   [OK] Todos los archivos criticos presentes" -ForegroundColor Green
Write-Host ""

# ====================================================================
# 4. PROBAR APLICACION (OPCIONAL)
# ====================================================================

if ($TestApp) {
    Write-Host "4. Probando aplicacion antes de crear instalador..." -ForegroundColor Yellow
    
    $exePath = Join-Path $publishPath "GestionTime.Desktop.exe"
    
    Write-Host "   Iniciando aplicacion de prueba..." -ForegroundColor Gray
    Write-Host "   (Cierra la aplicacion cuando verifiques que funciona)" -ForegroundColor Gray
    
    Start-Process $exePath -Wait
    
    Write-Host "   [OK] Aplicacion probada" -ForegroundColor Green
    Write-Host ""
}

# ====================================================================
# 5. VERIFICAR/CREAR ARCHIVOS DE INNO SETUP
# ====================================================================

Write-Host "5. Preparando configuracion de Inno Setup..." -ForegroundColor Yellow

# Crear archivos necesarios si no existen
if (!(Test-Path "Installer")) {
    New-Item -ItemType Directory -Path "Installer" -Force | Out-Null
}

# Crear License.rtf si no existe
if (!(Test-Path "Installer\License.rtf")) {
    $licenseContent = @"
{\rtf1\ansi\deff0
{\fonttbl{\f0\fnil\fcharset0 Arial;}}
\viewkind4\uc1\pard\lang1034\f0\fs20
LICENCIA DE USO - GESTIONTIME DESKTOP\par
\par
Copyright (c) 2025 GestionTime Solutions\par
\par
Por la presente se concede permiso para usar esta aplicacion.\par
\par
EL SOFTWARE SE PROPORCIONA "TAL CUAL", SIN GARANTIA DE NINGUN TIPO.\par
}
"@
    $licenseContent | Out-File -FilePath "Installer\License.rtf" -Encoding ASCII -Force
}

# Crear Readme.txt si no existe
if (!(Test-Path "Installer\Readme.txt")) {
    $readmeContent = @"
GESTIONTIME DESKTOP - VERSION $Version

Aplicacion de gestion de tiempo y partes de trabajo.

REQUISITOS DEL SISTEMA:
- Windows 10 version 1809 (build 17763) o superior
- Windows 11 (recomendado)
- Arquitectura x64

INSTALACION:
1. Ejecutar el instalador como administrador
2. Seguir las instrucciones del asistente
3. La aplicacion se iniciara automaticamente

SOPORTE:
Para soporte tecnico, visite:
https://gestiontime.com/support
"@
    $readmeContent | Out-File -FilePath "Installer\Readme.txt" -Encoding UTF8 -Force
}

Write-Host "   [OK] Archivos de configuracion listos" -ForegroundColor Green
Write-Host ""

# ====================================================================
# 6. COMPILAR INSTALADOR CON INNO SETUP
# ====================================================================

Write-Host "6. Compilando instalador EXE con Inno Setup..." -ForegroundColor Yellow

$issFile = "Installer\GestionTimeSetup.iss"

if (!(Test-Path $issFile)) {
    Write-Host "   [ERROR] No se encontro el archivo: $issFile" -ForegroundColor Red
    exit 1
}

Write-Host "   Ejecutando Inno Setup Compiler..." -ForegroundColor Gray

# Compilar con Inno Setup
$compileOutput = & "$innoPath" "$issFile" 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "   [ERROR] Error compilando instalador" -ForegroundColor Red
    Write-Host "   $compileOutput" -ForegroundColor Gray
    exit 1
}

Write-Host "   [OK] Instalador compilado exitosamente" -ForegroundColor Green
Write-Host ""

# ====================================================================
# 7. VERIFICAR INSTALADOR GENERADO
# ====================================================================

Write-Host "7. Verificando instalador generado..." -ForegroundColor Yellow

$outputDir = "bin\Release\Installer"
$installerFiles = Get-ChildItem -Path $outputDir -Filter "GestionTimeDesktopSetup-*.exe" -ErrorAction SilentlyContinue

if (!$installerFiles) {
    Write-Host "   [ERROR] No se encontro el instalador generado" -ForegroundColor Red
    exit 1
}

$installerFile = $installerFiles | Sort-Object LastWriteTime -Descending | Select-Object -First 1
$installerSizeMB = [math]::Round($installerFile.Length / 1MB, 2)

Write-Host "   [OK] Instalador encontrado:" -ForegroundColor Green
Write-Host "      Archivo: $($installerFile.Name)" -ForegroundColor Gray
Write-Host "      Tamano: $installerSizeMB MB" -ForegroundColor Gray
Write-Host "      Ruta: $($installerFile.FullName)" -ForegroundColor Gray

Write-Host ""

# ====================================================================
# 8. RESUMEN FINAL
# ====================================================================

$stopwatch.Stop()
$elapsed = $stopwatch.Elapsed.TotalSeconds

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  [OK] INSTALADOR EXE CREADO EXITOSAMENTE  " -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Informacion del instalador:" -ForegroundColor White
Write-Host "   * Producto: GestionTime Desktop" -ForegroundColor Gray
Write-Host "   * Version: $Version" -ForegroundColor Gray
Write-Host "   * Plataforma: x64 (Windows 10/11)" -ForegroundColor Gray
Write-Host "   * Tamano comprimido: $installerSizeMB MB" -ForegroundColor Gray
Write-Host "   * Tamano descomprimido: $totalSizeMB MB" -ForegroundColor Gray
Write-Host "   * Archivos incluidos: $fileCount" -ForegroundColor Gray
Write-Host "   * Tiempo de creacion: $([math]::Round($elapsed, 1)) segundos" -ForegroundColor Gray
Write-Host ""
Write-Host "Caracteristicas:" -ForegroundColor White
Write-Host "   [OK] Runtime .NET 8 incluido (self-contained)" -ForegroundColor Green
Write-Host "   [OK] WindowsAppSDK incluido" -ForegroundColor Green
Write-Host "   [OK] Interfaz de instalacion moderna" -ForegroundColor Green
Write-Host "   [OK] Accesos directos automaticos" -ForegroundColor Green
Write-Host "   [OK] Desinstalacion desde Panel de Control" -ForegroundColor Green
Write-Host "   [OK] Soporte para actualizaciones" -ForegroundColor Green
Write-Host ""
Write-Host "Ubicacion del instalador:" -ForegroundColor White
Write-Host "   $($installerFile.FullName)" -ForegroundColor Cyan
Write-Host ""
Write-Host "Para instalar:" -ForegroundColor White
Write-Host "   1. Ejecutar como administrador:" -ForegroundColor Gray
Write-Host "      $($installerFile.Name)" -ForegroundColor Cyan
Write-Host ""
Write-Host "   2. Instalacion silenciosa:" -ForegroundColor Gray
Write-Host "      $($installerFile.Name) /SILENT /NORESTART" -ForegroundColor Cyan
Write-Host ""
Write-Host "Para desinstalar:" -ForegroundColor White
Write-Host "   Panel de Control > Programas > GestionTime Desktop" -ForegroundColor Gray
Write-Host ""

# Abrir carpeta del instalador
Write-Host "Abriendo carpeta del instalador..." -ForegroundColor Yellow
Start-Process explorer.exe -ArgumentList "/select,`"$($installerFile.FullName)`""

Write-Host ""
Write-Host "[OK] Proceso completado exitosamente" -ForegroundColor Green
Write-Host ""
Write-Host "Presiona cualquier tecla para salir..." -ForegroundColor Cyan
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
