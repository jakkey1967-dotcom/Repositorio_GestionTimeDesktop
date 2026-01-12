@echo off
REM ========================================================================
REM VERIFICAR REQUISITOS DEL SISTEMA
REM ========================================================================

title Verificar Requisitos - GestionTime Desktop
color 0B

echo.
echo ================================================================
echo   VERIFICADOR DE REQUISITOS
echo   GestionTime Desktop v1.2.0
echo ================================================================
echo.
echo Este script verifica que tu sistema tenga todo lo necesario
echo para generar instaladores.
echo.
echo Presiona cualquier tecla para continuar...
pause >nul

cls
echo.
echo Verificando requisitos...
echo.

PowerShell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0VERIFICAR-REQUISITOS.ps1"

echo.
pause
