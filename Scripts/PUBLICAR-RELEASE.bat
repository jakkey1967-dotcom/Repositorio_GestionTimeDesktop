@echo off
REM ===========================================================================
REM PUBLICAR Y CREAR INSTALADOR - METODO DOTNET
REM ===========================================================================

echo.
echo ================================================
echo   PUBLICAR APLICACION Y CREAR INSTALADOR
echo ================================================
echo.

cd /d C:\GestionTime\GestionTimeDesktop

echo [1/2] Publicando aplicacion con dotnet publish...
echo.

dotnet publish GestionTime.Desktop.csproj ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=false ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:PublishReadyToRun=false ^
    -o "publish\win-x64"

if errorlevel 1 (
    echo ERROR: Publicacion fallo
    pause
    exit /b 1
)

echo.
echo [2/2] Copiando window-config.ini...
if exist "Installer\window-config.ini" (
    copy /Y "Installer\window-config.ini" "publish\win-x64\window-config.ini" >nul 2>&1
    echo    OK
)

echo.
echo ================================================
echo   PUBLICACION COMPLETA
echo ================================================
echo.
echo Carpeta: publish\win-x64\
echo.
echo Contenido listo para distribuir:
dir /s /b "publish\win-x64\*.exe" | find "GestionTime.Desktop.exe"
echo.
echo Para crear ZIP de distribucion:
echo Comprimir carpeta: publish\win-x64\
echo.
explorer "publish\win-x64"
pause
