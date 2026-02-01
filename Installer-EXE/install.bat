@echo off
setlocal enabledelayedexpansion

:: ═══════════════════════════════════════════════════════════════
:: INSTALADOR GESTIONTIME DESKTOP v1.2.0-beta
:: Auto-extractor creado con IExpress
:: ═══════════════════════════════════════════════════════════════

echo.
echo ╔═══════════════════════════════════════════════════════════════╗
echo ║   GESTIONTIME DESKTOP v1.2.0-beta - INSTALADOR                ║
echo ╚═══════════════════════════════════════════════════════════════╝
echo.

:: Verificar privilegios de administrador
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo [ERROR] Este instalador requiere privilegios de Administrador.
    echo.
    echo Por favor, ejecuta el instalador como Administrador:
    echo   1. Clic derecho sobre el instalador
    echo   2. "Ejecutar como administrador"
    echo.
    pause
    exit /b 1
)

echo [1/5] Preparando instalacion...

:: Directorio de instalación
set "INSTALL_DIR=%ProgramFiles%\GestionTime Solutions\GestionTime Desktop"
set "TEMP_DIR=%TEMP%\GestionTime-Install"

:: Crear directorio de instalación
echo [2/5] Creando directorios...
if not exist "%INSTALL_DIR%" (
    mkdir "%INSTALL_DIR%" 2>nul
    if !errorLevel! neq 0 (
        echo [ERROR] No se pudo crear el directorio de instalacion.
        pause
        exit /b 1
    )
)

:: Copiar archivos desde el directorio temporal
echo [3/5] Copiando archivos...
xcopy /E /I /Y "%TEMP_DIR%\*" "%INSTALL_DIR%\" >nul 2>&1
if !errorLevel! neq 0 (
    echo [ERROR] Error al copiar archivos.
    pause
    exit /b 1
)

:: Crear acceso directo en el Escritorio
echo [4/5] Creando accesos directos...

:: Escritorio
set "DESKTOP=%USERPROFILE%\Desktop"
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "$WshShell = New-Object -ComObject WScript.Shell; ^
     $Shortcut = $WshShell.CreateShortcut('%DESKTOP%\GestionTime Desktop.lnk'); ^
     $Shortcut.TargetPath = '%INSTALL_DIR%\GestionTime.Desktop.exe'; ^
     $Shortcut.WorkingDirectory = '%INSTALL_DIR%'; ^
     $Shortcut.IconLocation = '%INSTALL_DIR%\Assets\app_logo.ico'; ^
     $Shortcut.Description = 'Aplicacion de gestion de tiempo'; ^
     $Shortcut.Save()" >nul 2>&1

:: Menú Inicio
set "START_MENU=%ProgramData%\Microsoft\Windows\Start Menu\Programs\GestionTime Desktop"
if not exist "%START_MENU%" mkdir "%START_MENU%" >nul 2>&1

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "$WshShell = New-Object -ComObject WScript.Shell; ^
     $Shortcut = $WshShell.CreateShortcut('%START_MENU%\GestionTime Desktop.lnk'); ^
     $Shortcut.TargetPath = '%INSTALL_DIR%\GestionTime.Desktop.exe'; ^
     $Shortcut.WorkingDirectory = '%INSTALL_DIR%'; ^
     $Shortcut.IconLocation = '%INSTALL_DIR%\Assets\app_logo.ico'; ^
     $Shortcut.Description = 'Aplicacion de gestion de tiempo'; ^
     $Shortcut.Save()" >nul 2>&1

:: Registrar en Panel de Control
echo [5/5] Registrando aplicacion...
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop" /v "DisplayName" /t REG_SZ /d "GestionTime Desktop" /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop" /v "DisplayVersion" /t REG_SZ /d "1.2.0.0" /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop" /v "Publisher" /t REG_SZ /d "GestionTime Solutions" /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop" /v "DisplayIcon" /t REG_SZ /d "%INSTALL_DIR%\Assets\app_logo.ico" /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop" /v "InstallLocation" /t REG_SZ /d "%INSTALL_DIR%" /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop" /v "UninstallString" /t REG_SZ /d "%INSTALL_DIR%\uninstall.bat" /f >nul 2>&1

:: Crear desinstalador
(
echo @echo off
echo echo Desinstalando GestionTime Desktop...
echo rd /s /q "%INSTALL_DIR%" 2^>nul
echo del "%DESKTOP%\GestionTime Desktop.lnk" 2^>nul
echo rd /s /q "%START_MENU%" 2^>nul
echo reg delete "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop" /f 2^>nul
echo echo Desinstalacion completada.
echo pause
) > "%INSTALL_DIR%\uninstall.bat"

:: Limpiar archivos temporales
rd /s /q "%TEMP_DIR%" >nul 2>&1

echo.
echo ╔═══════════════════════════════════════════════════════════════╗
echo ║   ✅ INSTALACION COMPLETADA EXITOSAMENTE                      ║
echo ╚═══════════════════════════════════════════════════════════════╝
echo.
echo GestionTime Desktop se ha instalado en:
echo   %INSTALL_DIR%
echo.
echo Accesos directos creados en:
echo   - Escritorio
echo   - Menu Inicio
echo.
echo Para desinstalar, busca "GestionTime Desktop" en:
echo   Configuracion ^> Aplicaciones ^> Aplicaciones instaladas
echo.
pause
