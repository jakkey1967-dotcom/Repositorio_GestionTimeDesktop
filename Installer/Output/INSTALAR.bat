@echo off
REM ===========================================================================
REM INSTALADOR AUTOMATICO - GESTIONTIME DESKTOP
REM VERSION: 1.2.0
REM FECHA: 08/01/2026
REM ===========================================================================

cls
echo.
echo ================================================================
echo   INSTALADOR GESTIONTIME DESKTOP v1.2.0
echo ================================================================
echo.
echo Este instalador copiara GestionTime Desktop a:
echo   C:\Program Files\GestionTime\Desktop
echo.
echo Presiona CTRL+C para cancelar o cualquier tecla para continuar...
pause >nul

REM Verificar permisos de administrador
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo.
    echo ERROR: Se requieren permisos de administrador
    echo.
    echo Solucion:
    echo   1. Clic derecho en este archivo
    echo   2. Seleccionar "Ejecutar como administrador"
    echo.
    pause
    exit /b 1
)

echo.
echo [1/5] Verificando requisitos...

REM Verificar que existe el ZIP
if not exist "GestionTime-Desktop-1.2.0-Portable.zip" (
    echo ERROR: No se encuentra el archivo ZIP
    echo Asegurate de que este archivo BAT esta en la misma carpeta que:
    echo   GestionTime-Desktop-1.2.0-Portable.zip
    echo.
    pause
    exit /b 1
)

echo   OK - Archivo ZIP encontrado

echo.
echo [2/5] Creando directorio de instalacion...

set "INSTALL_DIR=C:\Program Files\GestionTime\Desktop"

if not exist "%INSTALL_DIR%" (
    mkdir "%INSTALL_DIR%"
    echo   OK - Directorio creado
) else (
    echo   OK - Directorio ya existe
)

echo.
echo [3/5] Extrayendo archivos...

REM Usar PowerShell para extraer ZIP
powershell -Command "Expand-Archive -Path 'GestionTime-Desktop-1.2.0-Portable.zip' -DestinationPath '%INSTALL_DIR%' -Force"

if %errorLevel% neq 0 (
    echo ERROR: Fallo al extraer archivos
    pause
    exit /b 1
)

echo   OK - Archivos extraidos

echo.
echo [4/5] Creando accesos directos...

REM Crear acceso directo en Menu Inicio
set "START_MENU=%ProgramData%\Microsoft\Windows\Start Menu\Programs"
powershell -Command "$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut('%START_MENU%\GestionTime Desktop.lnk'); $s.TargetPath = '%INSTALL_DIR%\GestionTime.Desktop.exe'; $s.WorkingDirectory = '%INSTALL_DIR%'; $s.Description = 'Sistema de gestion de partes de trabajo'; $s.Save()"

echo   OK - Acceso directo en Menu Inicio creado

REM Preguntar si crear acceso en Escritorio
echo.
set /p DESKTOP_SHORTCUT="Crear acceso directo en Escritorio? (S/N): "
if /i "%DESKTOP_SHORTCUT%"=="S" (
    set "DESKTOP=%PUBLIC%\Desktop"
    powershell -Command "$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut('%DESKTOP%\GestionTime Desktop.lnk'); $s.TargetPath = '%INSTALL_DIR%\GestionTime.Desktop.exe'; $s.WorkingDirectory = '%INSTALL_DIR%'; $s.Description = 'Sistema de gestion de partes de trabajo'; $s.Save()"
    echo   OK - Acceso directo en Escritorio creado
)

echo.
echo [5/5] Registrando en Programas y caracteristicas...

REM Agregar entrada en registro para "Programas y caracteristicas"
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop" /v DisplayName /t REG_SZ /d "GestionTime Desktop" /f >nul
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop" /v DisplayVersion /t REG_SZ /d "1.2.0" /f >nul
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop" /v Publisher /t REG_SZ /d "Global Retail Solutions" /f >nul
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop" /v InstallLocation /t REG_SZ /d "%INSTALL_DIR%" /f >nul
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop" /v DisplayIcon /t REG_SZ /d "%INSTALL_DIR%\GestionTime.Desktop.exe" /f >nul
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop" /v UninstallString /t REG_SZ /d "%INSTALL_DIR%\Uninstall.bat" /f >nul

echo   OK - Registrado

REM Crear desinstalador
echo @echo off > "%INSTALL_DIR%\Uninstall.bat"
echo echo Desinstalando GestionTime Desktop... >> "%INSTALL_DIR%\Uninstall.bat"
echo taskkill /F /IM GestionTime.Desktop.exe 2^>nul >> "%INSTALL_DIR%\Uninstall.bat"
echo timeout /t 2 /nobreak ^>nul >> "%INSTALL_DIR%\Uninstall.bat"
echo rd /s /q "%INSTALL_DIR%" >> "%INSTALL_DIR%\Uninstall.bat"
echo del "%START_MENU%\GestionTime Desktop.lnk" 2^>nul >> "%INSTALL_DIR%\Uninstall.bat"
echo del "%PUBLIC%\Desktop\GestionTime Desktop.lnk" 2^>nul >> "%INSTALL_DIR%\Uninstall.bat"
echo reg delete "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop" /f 2^>nul >> "%INSTALL_DIR%\Uninstall.bat"
echo echo Desinstalacion completada. >> "%INSTALL_DIR%\Uninstall.bat"
echo pause >> "%INSTALL_DIR%\Uninstall.bat"

echo.
echo ================================================================
echo   INSTALACION COMPLETADA EXITOSAMENTE
echo ================================================================
echo.
echo GestionTime Desktop ha sido instalado en:
echo   %INSTALL_DIR%
echo.
echo Para iniciar la aplicacion:
echo   - Buscar "GestionTime Desktop" en el Menu Inicio
echo   - O ejecutar: %INSTALL_DIR%\GestionTime.Desktop.exe
echo.
echo Para desinstalar:
echo   - Panel de Control ^> Programas y caracteristicas ^> GestionTime Desktop
echo   - O ejecutar: %INSTALL_DIR%\Uninstall.bat
echo.
set /p LAUNCH="Deseas iniciar GestionTime Desktop ahora? (S/N): "
if /i "%LAUNCH%"=="S" (
    start "" "%INSTALL_DIR%\GestionTime.Desktop.exe"
)

echo.
echo Presiona cualquier tecla para salir...
pause >nul
