@echo off
REM ===========================================================================
REM GENERAR MSI COMPLETO - VERSION QUE FUNCIONA CON WIX v3.14
REM Usa heat.exe de WiX para generar componentes automaticamente
REM ===========================================================================

setlocal enabledelayedexpansion

echo.
echo ================================================
echo   GENERAR MSI COMPLETO - GESTIONTIME DESKTOP
echo ================================================
echo.

cd /d C:\GestionTime\GestionTimeDesktop

set WIXDIR=C:\Program Files (x86)\WiX Toolset v3.14
set BINDIR=bin\x64\Debug\net8.0-windows10.0.19041.0
set OUTDIR=Installer\Output
set MSIFILE=%OUTDIR%\GestionTime-Desktop-1.2.0-Setup.msi

REM Verificar WiX v3.14
if not exist "%WIXDIR%\bin\heat.exe" (
    echo ERROR: WiX Toolset v3.14 no encontrado en Program Files (x86)
    echo.
    pause
    exit /b 1
)

echo Usando WiX Toolset v3.14
set HEAT=%WIXDIR%\bin\heat.exe
set CANDLE=%WIXDIR%\bin\candle.exe
set LIGHT=%WIXDIR%\bin\light.exe

REM Verificar ejecutable
if not exist "%BINDIR%\GestionTime.Desktop.exe" (
    echo ERROR: Ejecutable no encontrado
    echo Ejecutar: dotnet build -c Debug
    pause
    exit /b 1
)

REM Crear directorio de salida
if not exist "%OUTDIR%" mkdir "%OUTDIR%"

echo [1/4] Copiando window-config.ini...
if exist "Installer\window-config.ini" (
    copy /Y "Installer\window-config.ini" "%BINDIR%\window-config.ini" >nul 2>&1
    echo    OK - Copiado
)

echo.
echo [2/4] Generando componentes con heat.exe...

"%HEAT%" dir "%BINDIR%" ^
    -cg AppFiles ^
    -dr INSTALLFOLDER ^
    -gg ^
    -sfrag ^
    -srd ^
    -sreg ^
    -var var.SourceDir ^
    -out "%TEMP%\AppFiles.wxs"

if errorlevel 1 (
    echo ERROR: heat.exe fallo
    pause
    exit /b 1
)

echo    OK - Componentes generados con heat.exe

echo.
echo [3/4] Compilando con candle.exe...
"%CANDLE%" -nologo "%TEMP%\AppFiles.wxs" "Installer\MSI\Product.wxs" -dSourceDir="%BINDIR%" -out "%TEMP%\"

if errorlevel 1 (
    echo ERROR: candle.exe fallo
    del "%TEMP%\AppFiles.wxs" >nul 2>&1
    pause
    exit /b 1
)

echo    OK - Compilado

echo.
echo [4/4] Enlazando con light.exe...
"%LIGHT%" -nologo -ext WixUIExtension "%TEMP%\AppFiles.wixobj" "%TEMP%\Product.wixobj" -out "%MSIFILE%"

if errorlevel 1 (
    echo ERROR: light.exe fallo
    del "%TEMP%\AppFiles.wxs" >nul 2>&1
    del "%TEMP%\*.wixobj" >nul 2>&1
    pause
    exit /b 1
)

echo    OK - MSI enlazado

REM Limpiar archivos temporales
del "%TEMP%\AppFiles.wxs" >nul 2>&1
del "%TEMP%\*.wixobj" >nul 2>&1
del "%TEMP%\*.wixpdb" >nul 2>&1

if exist "%MSIFILE%" (
    echo.
    echo ================================================
    echo   MSI CREADO EXITOSAMENTE
    echo ================================================
    echo.
    for %%F in ("%MSIFILE%") do (
        set size=%%~zF
        set /a sizeMB=!size!/1048576
        echo Archivo: %%~nxF
        echo Tamano: !sizeMB! MB
    )
    echo.
    echo Ubicacion: %OUTDIR%
    echo.
    explorer /select,"%MSIFILE%"
    echo OK - Completado
) else (
    echo ERROR: MSI no se creo
)

echo.
pause
