@echo off
REM ========================================================================
REM SCRIPT: Generar Instalador MSIX/MSI - GestionTime Desktop
REM VERSION: 1.0
REM USO: Doble clic en este archivo para generar el instalador
REM ========================================================================

title Generando Instalador GestionTime Desktop...
color 0B

echo.
echo ================================================================
echo   GENERADOR DE INSTALADOR - GESTIONTIME DESKTOP
echo ================================================================
echo.
echo Este script genera automaticamente el instalador MSIX
echo (formato moderno de Windows 10/11, equivalente a MSI)
echo.
echo Presiona cualquier tecla para continuar...
pause >nul

cls
echo.
echo ================================================================
echo   INICIANDO GENERACION...
echo ================================================================
echo.

REM Cambiar al directorio del proyecto
cd /d "%~dp0"

REM Ejecutar script PowerShell
PowerShell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0CREATE-MSIX-INSTALLER.ps1"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ================================================================
    echo   ERROR AL GENERAR INSTALADOR
    echo ================================================================
    echo.
    echo Si el error persiste, prueba:
    echo   1. Abre Visual Studio 2022
    echo   2. Click derecho en proyecto ^> Publish ^> Create App Packages
    echo   3. Selecciona Sideloading
    echo   4. Click Create
    echo.
    pause
    exit /b 1
)

echo.
echo ================================================================
echo   PROCESO COMPLETADO
echo ================================================================
echo.
echo Revisa el explorador de archivos que se abrio
echo con la ubicacion del instalador generado.
echo.
pause
exit /b 0
