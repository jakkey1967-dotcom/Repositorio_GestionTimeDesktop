@echo off
REM ========================================================================
REM MENU PRINCIPAL - GENERADOR DE INSTALADORES
REM GestionTime Desktop v1.2.0
REM ========================================================================

title Generar Instalador - GestionTime Desktop
color 0B

:MENU
cls
echo.
echo ================================================================
echo   GENERADOR DE INSTALADORES
echo   GestionTime Desktop v1.2.0
echo ================================================================
echo.
echo Selecciona el tipo de instalador que quieres generar:
echo.
echo   [1] Portable (ZIP)          - MAS RAPIDO Y SIMPLE
echo       - No requiere instalacion
echo       - Solo .NET SDK 8 necesario
echo       - Tiempo: 1-2 minutos
echo       - Tamanio: ~45 MB
echo.
echo   [2] MSIX (con Visual Studio) - MAS PROFESIONAL
echo       - Instalador moderno Windows 10/11
echo       - Requiere Visual Studio 2022
echo       - Tiempo: 2-3 minutos
echo       - Tamanio: ~42 MB
echo.
echo   [3] Ver documentacion completa
echo.
echo   [4] Salir
echo.
echo ================================================================
echo.
set /p opcion="Elige una opcion (1-4): "

if "%opcion%"=="1" goto PORTABLE
if "%opcion%"=="2" goto MSIX
if "%opcion%"=="3" goto DOCS
if "%opcion%"=="4" goto SALIR

echo.
echo Opcion invalida. Presiona cualquier tecla para intentar de nuevo...
pause >nul
goto MENU

:PORTABLE
cls
echo.
echo ================================================================
echo   GENERAR INSTALADOR PORTABLE (ZIP)
echo ================================================================
echo.
echo Iniciando proceso...
echo.
call "%~dp0GENERAR-INSTALADOR-PORTABLE.bat"
echo.
echo Presiona cualquier tecla para volver al menu...
pause >nul
goto MENU

:MSIX
cls
echo.
echo ================================================================
echo   GENERAR INSTALADOR MSIX
echo ================================================================
echo.
echo Iniciando proceso...
echo.
call "%~dp0GENERAR-INSTALADOR-MSIX.bat"
echo.
echo Presiona cualquier tecla para volver al menu...
pause >nul
goto MENU

:DOCS
cls
echo.
echo ================================================================
echo   DOCUMENTACION DISPONIBLE
echo ================================================================
echo.
echo Los siguientes archivos contienen informacion detallada:
echo.
echo   COMO-GENERAR-INSTALADOR-SIMPLE.md
echo   - Guia paso a paso para cada metodo
echo   - Requisitos y troubleshooting
echo   - Comparacion de metodos
echo.
echo   Installer\README-CREAR-MSI-MSIX-DEFINITIVO.md
echo   - Documentacion tecnica completa
echo   - Metodos avanzados (MSI con WiX, EXE con Inno)
echo.
echo Presiona cualquier tecla para abrir la guia simple...
pause >nul

start "" "%~dp0COMO-GENERAR-INSTALADOR-SIMPLE.md"

echo.
echo Presiona cualquier tecla para volver al menu...
pause >nul
goto MENU

:SALIR
cls
echo.
echo ================================================================
echo   GRACIAS POR USAR GESTIONTIME DESKTOP
echo ================================================================
echo.
echo Para soporte: soporte@gestiontime.com
echo.
exit /b 0
