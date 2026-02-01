@echo off
REM ============================================
REM INSTALADOR PORTABLE - GestionTime Desktop
REM ============================================

echo.
echo ===============================================================
echo    INSTALADOR PORTABLE - GESTIONTIME DESKTOP v1.1.0
echo ===============================================================
echo.

REM Verificar si PowerShell está disponible
where powershell >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: PowerShell no esta disponible en este sistema.
    echo Por favor, instala PowerShell o ejecuta manualmente:
    echo   InstallPortable.ps1
    pause
    exit /b 1
)

echo Iniciando instalador...
echo.
echo NOTA: Este instalador requiere permisos de Administrador.
echo       Si aparece un aviso de UAC, acepta para continuar.
echo.

REM Ejecutar el script de PowerShell con permisos elevados
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "& { Start-Process powershell.exe -ArgumentList '-NoProfile -ExecutionPolicy Bypass -File \"%~dp0InstallPortable.ps1\"' -Verb RunAs }"

echo.
echo Si la ventana de PowerShell no aparecio, ejecuta manualmente:
echo   1. Clic derecho en InstallPortable.ps1
echo   2. Ejecutar con PowerShell
echo.

pause
