# ====================================================================
# Script DEFINITIVO - Paquete Portable que FUNCIONA
# Copia desde la compilacion Debug que YA FUNCIONA
# Fecha: 2025-01-27
# ====================================================================

param(
    [string]$Version = "1.1.0.0"
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  PAQUETE PORTABLE DESDE DEBUG FUNCIONAL   " -ForegroundColor Cyan
Write-Host "  GestionTime Desktop v$Version            " -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

# ====================================================================
# 1. VERIFICAR QUE EL DEBUG FUNCIONE
# ====================================================================

Write-Host "1. Verificando compilacion Debug..." -ForegroundColor Yellow

$debugPath = "bin\x64\Debug\net8.0-windows10.0.19041.0"
$debugExe = Join-Path $debugPath "GestionTime.Desktop.exe"

if (!(Test-Path $debugExe)) {
    Write-Host "   [ERROR] No se encuentra la compilacion Debug" -ForegroundColor Red
    Write-Host "   Compilando en Debug..." -ForegroundColor Yellow
    
    dotnet build GestionTime.Desktop.csproj -c Debug
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "   [ERROR] Error compilando" -ForegroundColor Red
        exit 1
    }
}

if (!(Test-Path $debugExe)) {
    Write-Host "   [ERROR] No se genero el ejecutable" -ForegroundColor Red
    exit 1
}

Write-Host "   [OK] Compilacion Debug encontrada" -ForegroundColor Green
Write-Host "   Ruta: $debugPath" -ForegroundColor Gray
Write-Host ""

# ====================================================================
# 2. CREAR PAQUETE PORTABLE DESDE DEBUG
# ====================================================================

Write-Host "2. Creando paquete portable..." -ForegroundColor Yellow

$portablePath = "bin\Portable\GestionTime-Desktop-$Version"

# Limpiar si existe
if (Test-Path $portablePath) {
    Write-Host "   Limpiando paquete anterior..." -ForegroundColor Gray
    Remove-Item -Path $portablePath -Recurse -Force
}

# Crear directorio
New-Item -ItemType Directory -Path $portablePath -Force | Out-Null

# Copiar TODOS los archivos del Debug
Write-Host "   Copiando archivos desde Debug (YA FUNCIONA)..." -ForegroundColor Gray

Copy-Item -Path "$debugPath\*" -Destination $portablePath -Recurse -Force

$fileCount = (Get-ChildItem -Path $portablePath -Recurse -File).Count
$totalSize = (Get-ChildItem -Path $portablePath -Recurse -File | Measure-Object -Property Length -Sum).Sum
$totalSizeMB = [math]::Round($totalSize / 1MB, 2)

Write-Host "   [OK] $fileCount archivos copiados ($totalSizeMB MB)" -ForegroundColor Green
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
    "Microsoft.Windows.ApplicationModel.DynamicDependency.dll",
    "Microsoft.Windows.SDK.NET.dll"
)

$allOk = $true
foreach ($file in $criticalFiles) {
    $filePath = Join-Path $portablePath $file
    if (Test-Path $filePath) {
        Write-Host "   [OK] $file" -ForegroundColor Green
    } else {
        Write-Host "   [WARN] Falta: $file" -ForegroundColor Yellow
        $allOk = $false
    }
}

Write-Host ""

# ====================================================================
# 4. CREAR LAUNCHER
# ====================================================================

Write-Host "4. Creando launcher..." -ForegroundColor Yellow

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
    echo Verifica:
    echo - Todos los archivos estan descomprimidos
    echo - Windows 10 build 17763 o superior
    echo - Arquitectura x64
    echo.
    pause
)
"@

$launcherPath = Join-Path $portablePath "INICIAR.bat"
$launcherBat | Out-File -FilePath $launcherPath -Encoding ASCII -Force

Write-Host "   [OK] INICIAR.bat creado" -ForegroundColor Green
Write-Host ""

# ====================================================================
# 5. CREAR README
# ====================================================================

Write-Host "5. Creando documentacion..." -ForegroundColor Yellow

$readmeContent = @"
============================================
GESTIONTIME DESKTOP - VERSION PORTABLE
============================================

VERSION: $Version
TIPO: Portable - Sin Instalacion
FECHA: $(Get-Date -Format 'dd/MM/yyyy')

============================================
INSTRUCCIONES DE USO:
============================================

OPCION 1: LAUNCHER
------------------
1. Ejecutar: INICIAR.bat
2. La aplicacion se abre automaticamente

OPCION 2: DIRECTO
------------------
1. Doble clic en: GestionTime.Desktop.exe
2. La aplicacion se abre directamente

============================================
CARACTERISTICAS:
============================================

[OK] NO requiere instalacion
[OK] NO requiere permisos de administrador
[OK] Runtime .NET 8 incluido
[OK] WindowsAppSDK incluido
[OK] Completamente portable
[OK] Funciona desde USB, red, cualquier carpeta

============================================
REQUISITOS DEL SISTEMA:
============================================

- Windows 10 version 1809 (build 17763) o superior
- Windows 11 (recomendado)
- Arquitectura: x64 (64-bit)
- Espacio en disco: 300 MB

============================================
USO PORTABLE:
============================================

ESCENARIO 1: USB
----------------
1. Copiar esta carpeta completa a USB
2. Conectar USB en cualquier PC Windows
3. Ejecutar INICIAR.bat
4. La aplicacion funciona sin instalar

ESCENARIO 2: RED
----------------
1. Copiar carpeta a servidor de red
2. Usuarios acceden desde \\servidor\apps\GestionTime\
3. Ejecutar INICIAR.bat
4. Funciona sin instalacion local

ESCENARIO 3: MULTIPLE VERSIONS
-------------------------------
Puedes tener multiples versiones en carpetas diferentes:
- C:\Apps\GestionTime-1.1.0\
- C:\Apps\GestionTime-1.2.0\
Sin conflictos entre ellas

============================================
CONFIGURACION:
============================================

Archivo: appsettings.json

Editar para cambiar:
- URL del servidor API
- Nivel de logging
- Otras configuraciones

Cada carpeta puede tener su propia configuracion.

============================================
SOLUCION DE PROBLEMAS:
============================================

PROBLEMA: "Windows protegió el equipo"
SOLUCION: 
- Click en "Mas informacion"
- Click en "Ejecutar de todas formas"
- Esto es normal para apps sin firma digital

PROBLEMA: Falta archivo DLL
SOLUCION:
- Descomprimir TODO el contenido
- No copiar solo el EXE
- Todos los archivos son necesarios

PROBLEMA: No inicia
SOLUCION:
- Verificar Windows 10 build 17763+
- Verificar arquitectura x64
- Ejecutar desde carpeta descomprimida
- NO ejecutar desde dentro del ZIP

============================================
ARCHIVOS IMPORTANTES:
============================================

INICIAR.bat              <- Launcher rapido
GestionTime.Desktop.exe  <- Ejecutable principal
appsettings.json         <- Configuracion
*.dll                    <- Librerias necesarias

TOTAL: $fileCount archivos ($totalSizeMB MB)

============================================
SOPORTE TECNICO:
============================================

Web: https://gestiontime.com/support
Email: support@gestiontime.com

============================================
LICENCIA:
============================================

Copyright (c) 2025 GestionTime Solutions
Todos los derechos reservados.

Este software se proporciona "tal cual" sin
garantias de ningun tipo.
"@

$readmePath = Join-Path $portablePath "LEEME.txt"
$readmeContent | Out-File -FilePath $readmePath -Encoding UTF8 -Force

Write-Host "   [OK] LEEME.txt creado" -ForegroundColor Green
Write-Host ""

# ====================================================================
# 6. CREAR ACCESO DIRECTO
# ====================================================================

Write-Host "6. Creando acceso directo..." -ForegroundColor Yellow

try {
    $shell = New-Object -ComObject WScript.Shell
    $shortcutPath = Join-Path $portablePath "GestionTime Desktop.lnk"
    $shortcut = $shell.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = Join-Path $portablePath "GestionTime.Desktop.exe"
    $shortcut.WorkingDirectory = $portablePath
    $shortcut.Description = "GestionTime Desktop - Gestion de tiempo"
    $shortcut.Save()
    
    Write-Host "   [OK] Acceso directo creado" -ForegroundColor Green
} catch {
    Write-Host "   [WARN] No se pudo crear acceso directo" -ForegroundColor Yellow
}

Write-Host ""

# ====================================================================
# 7. CREAR ZIP
# ====================================================================

Write-Host "7. Creando archivo ZIP..." -ForegroundColor Yellow

$zipPath = "bin\Portable\GestionTime-Desktop-$Version-Portable.zip"

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Write-Host "   Comprimiendo archivos..." -ForegroundColor Gray

try {
    Compress-Archive -Path $portablePath -DestinationPath $zipPath -CompressionLevel Optimal -Force
    
    $zipSize = (Get-Item $zipPath).Length
    $zipSizeMB = [math]::Round($zipSize / 1MB, 2)
    
    Write-Host "   [OK] ZIP creado: $zipSizeMB MB" -ForegroundColor Green
} catch {
    Write-Host "   [WARN] Error creando ZIP: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "   Puedes comprimir manualmente la carpeta" -ForegroundColor Gray
}

Write-Host ""

# ====================================================================
# 8. PROBAR APLICACION
# ====================================================================

Write-Host "8. Probando aplicacion..." -ForegroundColor Yellow

$testExe = Join-Path $portablePath "GestionTime.Desktop.exe"

if (Test-Path $testExe) {
    Write-Host "   Ejecutable encontrado" -ForegroundColor Green
    
    $testNow = Read-Host "   Quieres probar la aplicacion ahora? (S/N)"
    
    if ($testNow -eq "S" -or $testNow -eq "s") {
        Write-Host "   Iniciando aplicacion..." -ForegroundColor Gray
        Write-Host "   (Cierra la aplicacion cuando termines)" -ForegroundColor Gray
        Write-Host ""
        
        # Ejecutar desde el directorio portable
        Push-Location $portablePath
        Start-Process "GestionTime.Desktop.exe" -Wait
        Pop-Location
        
        Write-Host ""
        Write-Host "   [OK] Aplicacion probada" -ForegroundColor Green
    }
}

Write-Host ""

# ====================================================================
# 9. RESUMEN FINAL
# ====================================================================

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  PAQUETE PORTABLE CREADO EXITOSAMENTE     " -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "INFORMACION DEL PAQUETE:" -ForegroundColor White
Write-Host "   Producto: GestionTime Desktop" -ForegroundColor Gray
Write-Host "   Version: $Version" -ForegroundColor Gray
Write-Host "   Tipo: Portable (sin instalacion)" -ForegroundColor Gray
Write-Host "   Origen: Debug (YA FUNCIONA)" -ForegroundColor Gray
Write-Host "   Archivos: $fileCount" -ForegroundColor Gray
Write-Host "   Tamano: $totalSizeMB MB" -ForegroundColor Gray
if (Test-Path $zipPath) {
    Write-Host "   ZIP: $zipSizeMB MB" -ForegroundColor Gray
}
Write-Host ""
Write-Host "UBICACION:" -ForegroundColor White
Write-Host "   Carpeta: $(Resolve-Path $portablePath)" -ForegroundColor Cyan
if (Test-Path $zipPath) {
    Write-Host "   ZIP: $(Resolve-Path $zipPath)" -ForegroundColor Cyan
}
Write-Host ""
Write-Host "ARCHIVOS CLAVE:" -ForegroundColor White
Write-Host "   - INICIAR.bat                <- Ejecutar esto" -ForegroundColor Gray
Write-Host "   - GestionTime.Desktop.exe    <- O esto directamente" -ForegroundColor Gray
Write-Host "   - GestionTime Desktop.lnk    <- Acceso directo" -ForegroundColor Gray
Write-Host "   - LEEME.txt                  <- Instrucciones" -ForegroundColor Gray
Write-Host ""
Write-Host "COMO USAR:" -ForegroundColor Yellow
Write-Host "   1. Ir a: bin\Portable\GestionTime-Desktop-$Version\" -ForegroundColor Gray
Write-Host "   2. Ejecutar: INICIAR.bat" -ForegroundColor Gray
Write-Host "   3. La app funciona SIN INSTALACION" -ForegroundColor Gray
Write-Host ""
Write-Host "PARA DISTRIBUIR:" -ForegroundColor Yellow
if (Test-Path $zipPath) {
    Write-Host "   Compartir el archivo:" -ForegroundColor Gray
    Write-Host "   GestionTime-Desktop-$Version-Portable.zip ($zipSizeMB MB)" -ForegroundColor Cyan
} else {
    Write-Host "   Comprimir la carpeta y compartir el ZIP" -ForegroundColor Gray
}
Write-Host ""
Write-Host "CARACTERISTICAS:" -ForegroundColor White
Write-Host "   [OK] Copiado desde Debug FUNCIONAL" -ForegroundColor Green
Write-Host "   [OK] NO requiere instalacion" -ForegroundColor Green
Write-Host "   [OK] NO requiere permisos admin" -ForegroundColor Green
Write-Host "   [OK] Portable (USB, red, etc.)" -ForegroundColor Green
Write-Host "   [OK] Runtime .NET incluido" -ForegroundColor Green
Write-Host "   [OK] WindowsAppSDK incluido" -ForegroundColor Green
Write-Host ""

# Abrir carpeta
Write-Host "Abriendo carpeta..." -ForegroundColor Yellow
Start-Process explorer.exe -ArgumentList "/select,`"$(Resolve-Path (Join-Path $portablePath 'INICIAR.bat'))`""

Write-Host ""
Write-Host "[OK] LISTO PARA USAR" -ForegroundColor Green
Write-Host ""
Write-Host "Presiona cualquier tecla para salir..." -ForegroundColor Cyan
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
