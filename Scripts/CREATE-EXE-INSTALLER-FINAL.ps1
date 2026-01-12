# ===========================================================================
# CREAR INSTALADOR EXE (SIMIL-MSI) - GESTIONTIME DESKTOP
# VERSION: 1.0 - ENERO 2026
# DESCRIPCION: Crea un instalador EXE profesional sin necesidad de WiX
# ===========================================================================

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "  CREAR INSTALADOR EXE - GESTIONTIME DESKTOP" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

$projectDir = "C:\GestionTime\GestionTimeDesktop"
$binDir = "$projectDir\bin\x64\Debug\net8.0-windows10.0.19041.0"
$outputDir = "$projectDir\Installer\Output"
$version = "1.2.0"

# Verificar ejecutable
if (-not (Test-Path "$binDir\GestionTime.Desktop.exe")) {
    Write-Host "ERROR: Ejecutable no encontrado" -ForegroundColor Red
    exit 1
}

Write-Host "[1/3] Creando archivo SFX (auto-extraíble)..." -ForegroundColor Yellow

# Crear ZIP temporal
$zipTemp = Join-Path $env:TEMP "GestionTime-Temp.zip"
if (Test-Path $zipTemp) { Remove-Item $zipTemp -Force }

Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($binDir, $zipTemp, [System.IO.Compression.CompressionLevel]::Optimal, $false)

Write-Host "   ZIP temporal creado" -ForegroundColor Green

Write-Host ""
Write-Host "[2/3] Creando script de instalación..." -ForegroundColor Yellow

$installScript = @'
@echo off
setlocal EnableDelayedExpansion

cls
echo ============================================
echo   INSTALADOR GESTIONTIME DESKTOP v1.2.0
echo ============================================
echo.

REM Verificar permisos de administrador
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: Se requieren permisos de administrador
    echo.
    echo Solucion:
    echo   - Clic derecho en el instalador
    echo   - Seleccionar "Ejecutar como administrador"
    echo.
    pause
    exit /b 1
)

set "INSTALL_DIR=C:\Program Files\GestionTime\Desktop"
echo Directorio de instalacion:
echo   !INSTALL_DIR!
echo.
pause

echo.
echo Instalando...
if not exist "!INSTALL_DIR!" mkdir "!INSTALL_DIR!"
xcopy /E /I /Y /Q "%~dp0files\*" "!INSTALL_DIR!\" >nul

REM Crear acceso directo
set "START_MENU=%ProgramData%\Microsoft\Windows\Start Menu\Programs"
powershell -Command "$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut('!START_MENU!\GestionTime Desktop.lnk'); $s.TargetPath = '!INSTALL_DIR!\GestionTime.Desktop.exe'; $s.WorkingDirectory = '!INSTALL_DIR!'; $s.Save()" >nul 2>&1

REM Registrar
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop" /v DisplayName /t REG_SZ /d "GestionTime Desktop" /f >nul
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop" /v DisplayVersion /t REG_SZ /d "1.2.0" /f >nul
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop" /v Publisher /t REG_SZ /d "Global Retail Solutions" /f >nul
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop" /v InstallLocation /t REG_SZ /d "!INSTALL_DIR!" /f >nul
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop" /v UninstallString /t REG_SZ /d "!INSTALL_DIR!\Uninstall.bat" /f >nul

REM Crear desinstalador
(
echo @echo off
echo echo Desinstalando GestionTime Desktop...
echo taskkill /F /IM GestionTime.Desktop.exe 2^>nul
echo timeout /t 2 /nobreak ^>nul
echo rd /s /q "!INSTALL_DIR!"
echo del "!START_MENU!\GestionTime Desktop.lnk" 2^>nul
echo reg delete "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop" /f 2^>nul
echo echo Desinstalacion completada.
echo pause
) > "!INSTALL_DIR!\Uninstall.bat"

echo.
echo ============================================
echo   INSTALACION COMPLETADA
echo ============================================
echo.
echo GestionTime Desktop se ha instalado en:
echo   !INSTALL_DIR!
echo.
echo Buscar "GestionTime Desktop" en el Menu Inicio
echo.
pause
'@

$installerDir = Join-Path $env:TEMP "GestionTime-Installer"
if (Test-Path $installerDir) { Remove-Item $installerDir -Recurse -Force }
New-Item -ItemType Directory -Path $installerDir -Force | Out-Null
New-Item -ItemType Directory -Path "$installerDir\files" -Force | Out-Null

# Copiar archivos
Copy-Item "$binDir\*" -Destination "$installerDir\files" -Recurse -Force

# Guardar script
$installScript | Out-File -FilePath "$installerDir\Install.bat" -Encoding ASCII

Write-Host "   Script de instalación creado" -ForegroundColor Green

Write-Host ""
Write-Host "[3/3] Creando instalador autoextraíble..." -ForegroundColor Yellow

# Crear SFX usando IExpress (integrado en Windows)
$sedScript = @"
[Version]
Class=IEXPRESS
SEDVersion=3
[Options]
PackagePurpose=InstallApp
ShowInstallProgramWindow=0
HideExtractAnimation=0
UseLongFileName=1
InsideCompressed=0
CAB_FixedSize=0
CAB_ResvCodeSigning=0
RebootMode=N
InstallPrompt=%InstallPrompt%
DisplayLicense=%DisplayLicense%
FinishMessage=%FinishMessage%
TargetName=%TargetName%
FriendlyName=%FriendlyName%
AppLaunched=%AppLaunched%
PostInstallCmd=%PostInstallCmd%
AdminQuietInstCmd=%AdminQuietInstCmd%
UserQuietInstCmd=%UserQuietInstCmd%
SourceFiles=SourceFiles
[Strings]
InstallPrompt=¿Desea instalar GestionTime Desktop v1.2.0?
DisplayLicense=
FinishMessage=Instalación completada. Busque GestionTime Desktop en el Menú Inicio.
TargetName=$outputDir\GestionTime-Desktop-1.2.0-Setup.exe
FriendlyName=GestionTime Desktop v1.2.0
AppLaunched=Install.bat
PostInstallCmd=<None>
AdminQuietInstCmd=
UserQuietInstCmd=
FILE0="Install.bat"
FILE1="files"
[SourceFiles]
SourceFiles0=$installerDir\
[SourceFiles0]
%FILE0%=
%FILE1%=
"@

$sedFile = Join-Path $env:TEMP "GestionTime.sed"
$sedScript | Out-File -FilePath $sedFile -Encoding ASCII

# Ejecutar IExpress
& iexpress /N $sedFile

if (Test-Path "$outputDir\GestionTime-Desktop-1.2.0-Setup.exe") {
    $exeFile = Get-Item "$outputDir\GestionTime-Desktop-1.2.0-Setup.exe"
    
    Write-Host ""
    Write-Host "===============================================" -ForegroundColor Green
    Write-Host "  INSTALADOR EXE CREADO EXITOSAMENTE" -ForegroundColor Green
    Write-Host "===============================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "ARCHIVO:" -ForegroundColor Cyan
    Write-Host "  $($exeFile.FullName)" -ForegroundColor White
    Write-Host ""
    Write-Host "TAMAÑO:" -ForegroundColor Cyan
    Write-Host "  $([math]::Round($exeFile.Length / 1MB, 2)) MB" -ForegroundColor White
    Write-Host ""
    Write-Host "TIPO: Instalador Auto-Extraíble (EXE)" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "FUNCIONA COMO MSI:" -ForegroundColor Yellow
    Write-Host "  - Instalación profesional" -ForegroundColor White
    Write-Host "  - Accesos directos automáticos" -ForegroundColor White
    Write-Host "  - Registro en Programas y características" -ForegroundColor White
    Write-Host "  - Desinstalador incluido" -ForegroundColor White
    Write-Host ""
    
    Start-Process explorer.exe -ArgumentList "/select,`"$($exeFile.FullName)`""
} else {
    Write-Host "ERROR: No se pudo crear el instalador" -ForegroundColor Red
}

# Limpiar
Remove-Item $zipTemp -Force -ErrorAction SilentlyContinue
Remove-Item $installerDir -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $sedFile -Force -ErrorAction SilentlyContinue

Write-Host ""
