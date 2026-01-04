@echo off
title GestionTime Desktop v1.1.0.0

echo ============================================
echo   GESTIONTIME DESKTOP v1.1.0.0
echo   VERSION PORTABLE COMPLETA
echo ============================================
echo.
echo Iniciando aplicacion...
echo.

cd /d "%~dp0"
start "" "GestionTime.Desktop.exe"

if %errorLevel% NEQ 0 (
    echo.
    echo [ERROR] No se pudo iniciar
    echo Presiona cualquier tecla para salir...
    pause >nul
)
