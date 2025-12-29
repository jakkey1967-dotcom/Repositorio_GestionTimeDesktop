@echo off
echo ================================================================
echo   CREAR INSTALADOR MSIX PARA GESTIONTIME DESKTOP
echo ================================================================
echo.
echo Este script creara un instalador MSIX profesional que incluye
echo automaticamente Windows App Runtime y todas las dependencias.
echo.
echo IMPORTANTE: Debe ejecutarse desde Visual Studio Developer Command Prompt
echo o tener MSBuild disponible en el PATH.
echo.

pause

echo.
echo Ejecutando script PowerShell para crear el instalador...
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0crear-instalador-msix.ps1"

pause