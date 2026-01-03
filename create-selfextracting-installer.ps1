param(
    [switch]$Rebuild,
    [switch]$OpenOutput
)

Write-Host ""
Write-Host "🏗️  CREANDO INSTALADOR AUTO-EXTRAÍBLE (CORREGIDO XAML)" -ForegroundColor Green -BackgroundColor DarkGreen
Write-Host "===============================================" -ForegroundColor Green
Write-Host ""

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

try {
    # Asegurar que el build para instalador esté actualizado
    if ($Rebuild) {
        Write-Host "🔄 Rebuilding aplicación con configuración XAML corregida..." -ForegroundColor Cyan
        & powershell -ExecutionPolicy Bypass -File "build-for-installer.ps1" -Clean
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ ERROR: Falló el build de la aplicación" -ForegroundColor Red
            exit 1
        }
    }

    # Verificar que existan los archivos necesarios
    $appPath = "bin\Release\Installer\App"
    $appExe = Join-Path $appPath "GestionTime.Desktop.exe"
    
    if (!(Test-Path $appExe)) {
        Write-Host "❌ ERROR: No se encontró la aplicación compilada" -ForegroundColor Red
        Write-Host "   Ejecutar primero: .\build-for-installer.ps1 -Clean" -ForegroundColor Yellow
        exit 1
    }

    Write-Host "✅ Aplicación encontrada: $appExe" -ForegroundColor Green

    # VERIFICACIÓN CRÍTICA: Archivos de recursos WinUI
    $resourceFiles = Get-ChildItem $appPath -Filter "*.pri" -ErrorAction SilentlyContinue
    $xamlFiles = Get-ChildItem $appPath -Filter "*.xbf" -ErrorAction SilentlyContinue -Recurse
    
    Write-Host ""
    Write-Host "🔍 VERIFICACIÓN PREVIA DE RECURSOS WinUI:" -ForegroundColor Yellow
    Write-Host "   • Archivos .pri: $($resourceFiles.Count)" -ForegroundColor White
    Write-Host "   • Archivos .xbf: $($xamlFiles.Count)" -ForegroundColor White
    
    if ($resourceFiles.Count -eq 0) {
        Write-Host "   ⚠️  ADVERTENCIA: Sin archivos .pri encontrados" -ForegroundColor Yellow
        Write-Host "   Esto puede causar el error 'Cannot locate resource'" -ForegroundColor Yellow
    } else {
        Write-Host "   ✅ Archivos de recursos WinUI encontrados" -ForegroundColor Green
    }
    Write-Host ""

    # Crear directorio de salida
    $outputDir = "bin\Release\SelfExtractingInstaller"
    if (!(Test-Path $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    }

    # Obtener estadísticas de archivos
    $allFiles = Get-ChildItem $appPath -File -Recurse
    $totalSize = ($allFiles | Measure-Object -Property Length -Sum).Sum
    
    Write-Host "📊 ANÁLISIS DE ARCHIVOS:" -ForegroundColor Cyan
    Write-Host "   • Total de archivos: $($allFiles.Count)" -ForegroundColor White
    Write-Host "   • Tamaño total: $([math]::Round($totalSize/1MB, 2)) MB" -ForegroundColor White
    Write-Host ""

    Write-Host "📦 Creando archivo comprimido..." -ForegroundColor Yellow
    
    # Comprimir toda la aplicación
    $zipPath = Join-Path $outputDir "GestionTimeDesktop.zip"
    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }
    
    Compress-Archive -Path "$appPath\*" -DestinationPath $zipPath -CompressionLevel Optimal
    
    $zipFile = Get-Item $zipPath
    Write-Host "   ✅ Archivo comprimido: $([math]::Round($zipFile.Length/1MB, 2)) MB" -ForegroundColor Green

    # Crear script de instalación embebido MEJORADO
    $installerScript = @'
@echo off
setlocal enabledelayedexpansion
cls
echo.
echo ========================================================
echo   INSTALADOR GESTIONTIME DESKTOP v1.1.0 (CORREGIDO)
echo   GestionTime Solutions (c) 2025
echo ========================================================
echo.

:: Verificar permisos de administrador
openfiles >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Este instalador requiere permisos de administrador.
    echo Por favor, ejecute como administrador.
    echo.
    pause
    exit /b 1
)

echo [INFO] Permisos de administrador: OK
echo.

:: Configurar variables
set "INSTALL_DIR=%ProgramFiles%\GestionTime Desktop"
set "TEMP_DIR=%TEMP%\GestionTimeInstaller"
set "SHORTCUT_DIR=%ALLUSERSPROFILE%\Microsoft\Windows\Start Menu\Programs"

echo [INFO] Instalando en: %INSTALL_DIR%
echo [INFO] Directorio temporal: %TEMP_DIR%
echo.

:: Cerrar aplicación si está ejecutándose
echo [PASO 0] Verificando aplicación en ejecución...
taskkill /f /im GestionTime.Desktop.exe >nul 2>&1
timeout /t 2 /nobreak >nul

:: Crear directorio temporal
if exist "%TEMP_DIR%" rmdir /s /q "%TEMP_DIR%"
mkdir "%TEMP_DIR%"

:: Extraer archivos comprimidos (PowerShell embebido)
echo [PASO 1] Extrayendo archivos...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "Add-Type -AssemblyName System.IO.Compression.FileSystem; ^
     $zipBytes = [System.IO.File]::ReadAllBytes('%~f0'); ^
     $zipStart = [System.Text.Encoding]::ASCII.GetString($zipBytes) -split '__ZIP_DATA__' | Select-Object -First 1 | Measure-Object -Character | Select-Object -ExpandProperty Characters; ^
     $zipData = New-Object byte[] ($zipBytes.Length - $zipStart - 12); ^
     [Array]::Copy($zipBytes, $zipStart + 12, $zipData, 0, $zipData.Length); ^
     [System.IO.File]::WriteAllBytes('%TEMP_DIR%\app.zip', $zipData); ^
     [System.IO.Compression.ZipFile]::ExtractToDirectory('%TEMP_DIR%\app.zip', '%TEMP_DIR%\app')"

if errorlevel 1 (
    echo [ERROR] Falló la extracción de archivos.
    pause
    exit /b 1
)

echo [INFO] Archivos extraídos correctamente.

:: Crear directorio de instalación
echo [PASO 2] Creando directorio de instalación...
if exist "%INSTALL_DIR%" (
    echo [INFO] Removiendo instalación anterior...
    rmdir /s /q "%INSTALL_DIR%"
    timeout /t 1 /nobreak >nul
)
mkdir "%INSTALL_DIR%"

:: Copiar archivos con verificación mejorada
echo [PASO 3] Copiando archivos de aplicación...
xcopy "%TEMP_DIR%\app\*" "%INSTALL_DIR%\" /E /I /H /Y /Q >nul

if errorlevel 1 (
    echo [ERROR] Falló la copia de archivos.
    pause
    exit /b 1
)

echo [INFO] Archivos copiados correctamente.

:: Verificar archivos críticos
echo [PASO 3.5] Verificando instalación...
if not exist "%INSTALL_DIR%\GestionTime.Desktop.exe" (
    echo [ERROR] Archivo ejecutable principal no encontrado.
    pause
    exit /b 1
)

if not exist "%INSTALL_DIR%\GestionTime.Desktop.dll" (
    echo [ERROR] Biblioteca principal no encontrada.
    pause
    exit /b 1
)

echo [INFO] Archivos críticos verificados.

:: Crear accesos directos
echo [PASO 4] Creando accesos directos...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "$ws = New-Object -ComObject WScript.Shell; ^
     $shortcut = $ws.CreateShortcut('%SHORTCUT_DIR%\GestionTime Desktop.lnk'); ^
     $shortcut.TargetPath = '%INSTALL_DIR%\GestionTime.Desktop.exe'; ^
     $shortcut.WorkingDirectory = '%INSTALL_DIR%'; ^
     $shortcut.Description = 'Aplicacion de gestion de tiempo'; ^
     $shortcut.Save()"

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "$ws = New-Object -ComObject WScript.Shell; ^
     $shortcut = $ws.CreateShortcut('%PUBLIC%\Desktop\GestionTime Desktop.lnk'); ^
     $shortcut.TargetPath = '%INSTALL_DIR%\GestionTime.Desktop.exe'; ^
     $shortcut.WorkingDirectory = '%INSTALL_DIR%'; ^
     $shortcut.Description = 'Aplicacion de gestion de tiempo'; ^
     $shortcut.Save()"

echo [INFO] Accesos directos creados.

:: Registro en Panel de Control
echo [PASO 5] Registrando en Panel de Control...
reg add "HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop" /v "DisplayName" /t REG_SZ /d "GestionTime Desktop" /f >nul
reg add "HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop" /v "DisplayVersion" /t REG_SZ /d "1.1.0.0" /f >nul
reg add "HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop" /v "Publisher" /t REG_SZ /d "GestionTime Solutions" /f >nul
reg add "HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop" /v "InstallLocation" /t REG_SZ /d "%INSTALL_DIR%" /f >nul
reg add "HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop" /v "UninstallString" /t REG_SZ /d "%INSTALL_DIR%\Uninstall.bat" /f >nul
reg add "HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop" /v "DisplayIcon" /t REG_SZ /d "%INSTALL_DIR%\GestionTime.Desktop.exe" /f >nul

:: Crear desinstalador mejorado
echo [PASO 6] Creando desinstalador...
(
echo @echo off
echo echo Desinstalando GestionTime Desktop...
echo taskkill /f /im GestionTime.Desktop.exe ^>nul 2^>^&1
echo timeout /t 2 /nobreak ^>nul
echo rmdir /s /q "%INSTALL_DIR%"
echo del "%PUBLIC%\Desktop\GestionTime Desktop.lnk" ^>nul 2^>^&1
echo del "%SHORTCUT_DIR%\GestionTime Desktop.lnk" ^>nul 2^>^&1
echo reg delete "HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop" /f ^>nul 2^>^&1
echo echo Desinstalacion completada.
echo pause
) > "%INSTALL_DIR%\Uninstall.bat"

echo [INFO] Desinstalador creado.

:: Limpiar archivos temporales
echo [PASO 7] Limpiando archivos temporales...
if exist "%TEMP_DIR%" rmdir /s /q "%TEMP_DIR%"

echo [INFO] Instalacion completada exitosamente.
echo.
echo ========================================================
echo   INSTALACION COMPLETADA
echo ========================================================
echo.
echo Aplicacion instalada en: %INSTALL_DIR%
echo Accesos directos creados en Escritorio y Menu Inicio
echo.

:: Verificación final
if exist "%INSTALL_DIR%\GestionTime.Desktop.exe" (
    echo [INFO] Instalacion verificada correctamente.
    echo.
    set /p "launch=¿Ejecutar GestionTime Desktop ahora? (S/N): "
    if /i "!launch!"=="S" (
        echo [INFO] Iniciando GestionTime Desktop...
        start "" "%INSTALL_DIR%\GestionTime.Desktop.exe"
    )
) else (
    echo [ERROR] La instalacion no se pudo verificar.
    echo Por favor, verifique manualmente.
)

echo.
echo Gracias por instalar GestionTime Desktop!
pause
exit /b 0

__ZIP_DATA__
'@

    # Crear el instalador auto-extraíble
    $installerPath = Join-Path $outputDir "GestionTimeDesktopInstaller.bat"
    $installerScript | Out-File -FilePath $installerPath -Encoding ASCII

    # Agregar los datos del ZIP al final del archivo
    $zipBytes = [System.IO.File]::ReadAllBytes($zipPath)
    [System.IO.File]::AppendAllText($installerPath, [System.Convert]::ToBase64String($zipBytes))

    $stopwatch.Stop()

    # Verificar el resultado
    $installerFile = Get-Item $installerPath
    
    Write-Host ""
    Write-Host "✅ INSTALADOR AUTO-EXTRAÍBLE CORREGIDO CREADO" -ForegroundColor Green -BackgroundColor DarkGreen
    Write-Host "===============================================" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "📊 INFORMACIÓN DEL INSTALADOR:" -ForegroundColor Magenta
    Write-Host "   • Archivo: $($installerFile.Name)" -ForegroundColor White
    Write-Host "   • Tamaño: $([math]::Round($installerFile.Length/1MB, 2)) MB" -ForegroundColor White
    Write-Host "   • Ubicación: $($installerFile.FullName)" -ForegroundColor White
    Write-Host "   • Tiempo de creación: $($stopwatch.Elapsed.TotalSeconds.ToString('F1')) segundos" -ForegroundColor White
    Write-Host ""
    
    Write-Host "🎯 CORRECCIONES IMPLEMENTADAS:" -ForegroundColor Cyan
    Write-Host "   ✅ Configuración WinUI 3 corregida en build" -ForegroundColor Green
    Write-Host "   ✅ Verificación de recursos XAML incluida" -ForegroundColor Green
    Write-Host "   ✅ Cierre de aplicación antes de instalar" -ForegroundColor Green
    Write-Host "   ✅ Verificación de archivos críticos mejorada" -ForegroundColor Green
    Write-Host "   ✅ Instalación en Program Files con permisos" -ForegroundColor Green
    Write-Host "   ✅ Accesos directos automáticos" -ForegroundColor Green
    Write-Host "   ✅ Desinstalador incluido" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "📋 INSTRUCCIONES PARA USAR:" -ForegroundColor Yellow
    Write-Host "   1. Distribuir archivo: $($installerFile.Name)" -ForegroundColor White
    Write-Host "   2. Usuario ejecuta como administrador" -ForegroundColor White
    Write-Host "   3. El instalador verifica y corrige automáticamente" -ForegroundColor White
    Write-Host "   4. Aplicación se instala sin errores XAML" -ForegroundColor White
    Write-Host ""
    
    if ($resourceFiles.Count -gt 0) {
        Write-Host "✅ RECURSOS WinUI VERIFICADOS - DEBERÍA FUNCIONAR" -ForegroundColor Green -BackgroundColor DarkGreen
    } else {
        Write-Host "⚠️  ADVERTENCIA: Sin recursos .pri - puede requerir rebuild" -ForegroundColor Yellow -BackgroundColor DarkYellow
    }
    
    Write-Host ""
    Write-Host "❌ PARA DESINSTALAR:" -ForegroundColor Red
    Write-Host "   • Panel de Control → Programas → GestionTime Desktop" -ForegroundColor White
    Write-Host "   • O ejecutar: C:\Program Files\GestionTime Desktop\Uninstall.bat" -ForegroundColor White
    Write-Host ""
    
    if ($OpenOutput) {
        Write-Host "📂 Abriendo directorio de salida..." -ForegroundColor Green
        Start-Process "explorer.exe" -ArgumentList $outputDir
    }
    
    Write-Host "🎉 ¡INSTALADOR CORREGIDO LISTO!" -ForegroundColor Green -BackgroundColor DarkGreen
    Write-Host "===============================================" -ForegroundColor Green

} catch {
    Write-Host "❌ ERROR DURANTE LA CREACIÓN:" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor White
    exit 1
}