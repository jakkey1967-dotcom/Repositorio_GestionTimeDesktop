@echo off
REM ========================================================================
REM GENERADOR DE MSIX CON VISUAL STUDIO - GestionTime Desktop
REM VERSION: 1.0
REM USO: Doble clic para generar instalador MSIX con Visual Studio
REM ========================================================================

title Generar Instalador MSIX - GestionTime Desktop
color 0B
cls

echo.
echo ================================================================
echo   GENERADOR DE INSTALADOR MSIX
echo   GestionTime Desktop v1.2.0
echo ================================================================
echo.
echo Este script abre Visual Studio con instrucciones paso a paso
echo para generar un instalador MSIX (MSI moderno de Windows 10/11)
echo.
echo Ventajas del MSIX:
echo   - Instalacion limpia y profesional
echo   - Integracion con Windows 10/11
echo   - Instalador/desinstalador automatico
echo   - Actualizaciones automaticas
echo.
echo Requisitos:
echo   - Visual Studio 2022 instalado
echo   - .NET Desktop Development workload
echo.
echo Presiona cualquier tecla para continuar...
pause >nul

cls
echo.
echo ================================================================
echo   ABRIENDO VISUAL STUDIO...
echo ================================================================
echo.

cd /d "%~dp0"

PowerShell.exe -NoProfile -ExecutionPolicy Bypass -Command "& '%~dp0GENERAR-MSIX-VISUAL-STUDIO.ps1'"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ================================================================
    echo   VISUAL STUDIO NO ENCONTRADO
    echo ================================================================
    echo.
    echo Visual Studio 2022 no esta instalado o no se encontro.
    echo.
    echo Opciones:
    echo   1. Instalar Visual Studio 2022 Community (gratis):
    echo      https://visualstudio.microsoft.com/downloads/
    echo.
    echo   2. O usa el metodo PORTABLE (mas simple):
    echo      Ejecuta: GENERAR-INSTALADOR-PORTABLE.bat
    echo.
    pause
    exit /b 1
)

echo.
echo ================================================================
echo   VISUAL STUDIO ABIERTO
echo ================================================================
echo.
echo Sigue las instrucciones en pantalla de PowerShell
echo para crear el paquete MSIX.
echo.
echo Cuando termine, el instalador estara en:
echo   AppPackages\GestionTime.Desktop_1.2.0.0_x64_Test\
echo.
pause
exit /b 0
