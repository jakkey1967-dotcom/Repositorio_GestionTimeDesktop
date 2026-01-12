@echo off
REM ========================================================================
REM GENERADOR DE INSTALADOR PORTABLE - GestionTime Desktop
REM VERSION: 1.0
REM USO: Doble clic para generar instalador portable (ZIP)
REM ========================================================================

title Generar Instalador Portable - GestionTime Desktop
color 0B
cls

echo.
echo ================================================================
echo   GENERADOR DE INSTALADOR PORTABLE
echo   GestionTime Desktop v1.2.0
echo ================================================================
echo.
echo Este script genera un instalador portable (ZIP) que:
echo   - NO requiere instalacion
echo   - Funciona en cualquier carpeta
echo   - Incluye todas las dependencias
echo   - Tamanio: ~45 MB
echo.
echo Metodo: Compilacion + Empaquetado ZIP
echo Tiempo estimado: 1-2 minutos
echo.
echo Presiona cualquier tecla para iniciar...
pause >nul

cls
echo.
echo ================================================================
echo   INICIANDO GENERACION...
echo ================================================================
echo.

cd /d "%~dp0"

PowerShell.exe -NoProfile -ExecutionPolicy Bypass -Command "& '%~dp0GENERAR-INSTALADOR-PORTABLE.ps1'"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ================================================================
    echo   ERROR AL GENERAR INSTALADOR
    echo ================================================================
    echo.
    echo Posibles causas:
    echo   - .NET SDK 8 no instalado
    echo   - Error de compilacion en el proyecto
    echo   - Falta de permisos
    echo.
    echo Revisa los mensajes de error arriba.
    echo.
    pause
    exit /b 1
)

echo.
echo ================================================================
echo   INSTALADOR GENERADO EXITOSAMENTE
echo ================================================================
echo.
echo El explorador de archivos se abrio con la ubicacion
echo del instalador ZIP generado.
echo.
echo Para distribuir: Envia el archivo ZIP a los usuarios.
echo.
pause
exit /b 0
