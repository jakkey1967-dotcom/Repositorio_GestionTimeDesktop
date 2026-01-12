@echo off
REM ============================================================================
REM GENERAR MSI SIMPLE - SIN HEAT
REM ============================================================================

echo.
echo ========================================
echo   GENERAR MSI SIMPLE
echo ========================================
echo.

cd /d C:\GestionTime\GestionTimeDesktop

set WIXBIN=C:\Program Files\WiX Toolset v6.0\bin
set BINDIR=bin\x64\Debug\net8.0-windows10.0.19041.0
set OUTDIR=Installer\Output
set MSIFILE=%OUTDIR%\GestionTime-Desktop-Setup.msi

REM Paso 1: Copiar window-config.ini
echo [1/2] Preparando archivos...
if exist "Installer\window-config.ini" (
    copy /Y "Installer\window-config.ini" "%BINDIR%\window-config.ini" >nul
    echo    [OK] window-config.ini copiado
)

REM Paso 2: Compilar usando Product_Simple.wxs si existe
echo.
echo [2/2] Compilando MSI...

if exist "Installer\MSI\Product_Simple.wxs" (
    "%WIXBIN%\wix.exe" build ^
        -arch x64 ^
        -ext WixToolset.UI.wixext ^
        "Installer\MSI\Product_Simple.wxs" ^
        -out "%MSIFILE%" ^
        -bindpath "%BINDIR%"
) else if exist "Installer\MSI\Product.wxs" (
    "%WIXBIN%\wix.exe" build ^
        -arch x64 ^
        -ext WixToolset.UI.wixext ^
        "Installer\MSI\Product.wxs" ^
        -out "%MSIFILE%" ^
        -bindpath "%BINDIR%"
) else (
    echo ERROR: No se encontro archivo WXS
    pause
    exit /b 1
)

if errorlevel 1 (
    echo ERROR: Compilacion fallo
    pause
    exit /b 1
)

REM Verificar resultado
if not exist "%OUTDIR%" mkdir "%OUTDIR%"

if exist "%MSIFILE%" (
    echo.
    echo ========================================
    echo   MSI CREADO
    echo ========================================
    echo.
    for %%F in ("%MSIFILE%") do echo Tamano: %%~zF bytes
    echo.
    explorer /select,"%MSIFILE%"
    echo [OK] Completado!
) else (
    echo ERROR: MSI no se creo
)

pause
