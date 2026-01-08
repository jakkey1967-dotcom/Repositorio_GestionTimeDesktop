@echo off
REM ============================================================================
REM GENERAR MSI - METODO DIRECTO CON WIX HEAT
REM ============================================================================

echo.
echo ========================================
echo   GENERAR MSI - METODO DIRECTO
echo ========================================
echo.

cd /d C:\GestionTime\GestionTimeDesktop

set WIXBIN=C:\Program Files\WiX Toolset v6.0\bin
set BINDIR=bin\x64\Debug\net8.0-windows10.0.19041.0
set OUTDIR=Installer\Output
set MSIFILE=%OUTDIR%\GestionTime-Desktop-Setup.msi

REM Verificar WiX
if not exist "%WIXBIN%\wix.exe" (
    echo ERROR: WiX Toolset v6.0 no encontrado
    pause
    exit /b 1
)

REM Verificar ejecutable
if not exist "%BINDIR%\GestionTime.Desktop.exe" (
    echo ERROR: Ejecutable no encontrado
    echo Compilar con: dotnet build -c Debug
    pause
    exit /b 1
)

REM Crear directorio de salida
if not exist "%OUTDIR%" mkdir "%OUTDIR%"

REM Paso 1: Copiar window-config.ini personalizado
echo [1/3] Copiando configuracion personalizada...
if exist "Installer\window-config.ini" (
    copy /Y "Installer\window-config.ini" "%BINDIR%\window-config.ini" >nul
    echo    [OK] window-config.ini copiado
)

REM Paso 2: Generar componentes con heat
echo.
echo [2/3] Generando componentes con heat...
"%WIXBIN%\wix.exe" build ^
    -ext WixToolset.Heat.Extension ^
    -heat dir "%BINDIR%" ^
    -cg AppFiles ^
    -dr INSTALLFOLDER ^
    -sfrag ^
    -srd ^
    -sreg ^
    -ag ^
    -out "%TEMP%\AppFiles.wxs"

if errorlevel 1 (
    echo ERROR: Heat fallo
    pause
    exit /b 1
)

echo    [OK] Componentes generados

REM Paso 3: Compilar MSI
echo.
echo [3/3] Compilando MSI...
"%WIXBIN%\wix.exe" build ^
    -arch x64 ^
    -ext WixToolset.UI.wixext ^
    "%TEMP%\AppFiles.wxs" ^
    -out "%MSIFILE%" ^
    -bindpath "%BINDIR%"

if errorlevel 1 (
    echo ERROR: Compilacion fallo
    del "%TEMP%\AppFiles.wxs"
    pause
    exit /b 1
)

del "%TEMP%\AppFiles.wxs"

REM Verificar resultado
if exist "%MSIFILE%" (
    echo.
    echo ========================================
    echo   MSI CREADO EXITOSAMENTE
    echo ========================================
    echo.
    echo Archivo: %MSIFILE%
    echo.
    explorer /select,"%MSIFILE%"
) else (
    echo ERROR: MSI no se creo
    pause
    exit /b 1
)

echo.
echo [OK] Completado!
pause
