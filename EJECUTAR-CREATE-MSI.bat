@echo off
echo.
echo ========================================
echo   EJECUTANDO CREATE-MSI-COMPLETE.ps1
echo ========================================
echo.

cd /d C:\GestionTime\GestionTimeDesktop

REM Ejecutar el script usando Invoke-Expression para evitar parsing
powershell -NoProfile -ExecutionPolicy Bypass -Command "Invoke-Expression (Get-Content '.\CREATE-MSI-COMPLETE.ps1' -Raw)"

echo.
echo Proceso finalizado
pause
