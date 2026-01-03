param(
    [switch]$Rebuild,
    [switch]$OpenOutput,
    [switch]$CreateElevated
)

Write-Host ""
Write-Host "🔧 CREANDO INSTALADOR AUTO-EXTRAÍBLE MEJORADO" -ForegroundColor Green -BackgroundColor DarkGreen
Write-Host "===============================================" -ForegroundColor Green
Write-Host ""

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

try {
    # Asegurar que el build para instalador esté actualizado
    if ($Rebuild) {
        Write-Host "🔄 Rebuilding aplicación..." -ForegroundColor Cyan
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

    # Crear script de instalación embebido MEJORADO con mejor manejo de errores
    $installerScript = @'
@echo off
setlocal enabledelayedexpansion
title GestionTime Desktop Installer v1.1.0

:: Configurar colores si es posible
for /f "delims=" %%A in ('echo prompt $E^| cmd') do set "ESC=%%A"

echo.
echo ========================================================
echo   INSTALADOR GESTIONTIME DESKTOP v1.1.0 MEJORADO
echo   GestionTime Solutions (c) 2025
echo ========================================================
echo.

:: Verificar permisos de administrador MEJORADO
echo [INFO] Verificando permisos de administrador...
net session >nul 2>&1
if errorlevel 1 (
    echo.
    echo [ERROR] Este instalador requiere permisos de administrador.
    echo.
    echo SOLUCION:
    echo 1. Cerrar esta ventana
    echo 2. Click derecho en el archivo del instalador
    echo 3. Seleccionar "Ejecutar como administrador"
    echo.
    echo O desde PowerShell como administrador:
    echo    Start-Process cmd -ArgumentList "/c \"%~f0\"" -Verb RunAs
    echo.
    pause
    exit /b 1
)

echo [INFO] Permisos de administrador: OK
echo.

:: Configurar variables mejoradas - UBICACION CAMBIADA A C:\App
set "INSTALL_DIR=C:\App\GestionTime Desktop"
set "TEMP_DIR=%TEMP%\GestionTimeInstaller_%RANDOM%"
set "SHORTCUT_DIR=%ALLUSERSPROFILE%\Microsoft\Windows\Start Menu\Programs"
set "LOG_FILE=%TEMP%\GestionTimeInstaller.log"

:: Inicializar log
echo === GestionTime Desktop Installer Log === > "%LOG_FILE%"
echo Start Time: %DATE% %TIME% >> "%LOG_FILE%"
echo. >> "%LOG_FILE%"

echo [INFO] Instalando en: %INSTALL_DIR%
echo [INFO] Directorio temporal: %TEMP_DIR%
echo [INFO] Log: %LOG_FILE%
echo.

:: Cerrar aplicación si está ejecutándose (MEJORADO)
echo [PASO 0] Verificando aplicacion en ejecucion...
echo Cerrando GestionTime Desktop... >> "%LOG_FILE%"

taskkill /f /im GestionTime.Desktop.exe /t >nul 2>&1
if !errorlevel! equ 0 (
    echo [INFO] Aplicacion cerrada
) else (
    echo [INFO] Aplicacion no estaba ejecutandose
)

timeout /t 2 /nobreak >nul

:: Verificar espacio disponible CORREGIDO
echo [PASO 0.5] Verificando espacio en disco...
powershell -NoProfile -ExecutionPolicy Bypass -Command "try { $drive = Get-WmiObject -Class Win32_LogicalDisk | Where-Object { $_.DeviceID -eq 'C:' }; $freeSpaceGB = [math]::Round($drive.FreeSpace / 1GB, 2); $requiredSpaceGB = 0.5; Write-Host '[INFO] Espacio libre en C:' $freeSpaceGB 'GB'; Write-Host '[INFO] Espacio requerido:' $requiredSpaceGB 'GB'; if ($freeSpaceGB -lt $requiredSpaceGB) { Write-Host '[ERROR] Espacio insuficiente en disco'; exit 1 } else { Write-Host '[INFO] Espacio disponible: OK' } } catch { Write-Host '[WARNING] No se pudo verificar espacio, continuando...' }"

if errorlevel 1 (
    echo [ERROR] Espacio insuficiente en disco
    echo Se requieren al menos 500 MB libres >> "%LOG_FILE%"
    pause
    exit /b 1
)

:: Crear directorio temporal con verificacion de errores
echo [PASO 1] Creando directorio temporal...
if exist "%TEMP_DIR%" (
    echo Removiendo directorio temporal anterior... >> "%LOG_FILE%"
    rmdir /s /q "%TEMP_DIR%" 2>>"%LOG_FILE%"
)

mkdir "%TEMP_DIR%" 2>>"%LOG_FILE%"
if errorlevel 1 (
    echo [ERROR] No se pudo crear directorio temporal
    echo Error creando directorio temporal >> "%LOG_FILE%"
    pause
    exit /b 1
)

echo Directorio temporal creado: %TEMP_DIR% >> "%LOG_FILE%"

:: Extraer archivos comprimidos CORREGIDO COMPLETAMENTE
echo [PASO 2] Extrayendo archivos...
echo Iniciando extraccion... >> "%LOG_FILE%"

:: Metodo alternativo usando PowerShell para extraer ZIP embebido
powershell -NoProfile -ExecutionPolicy Bypass -Command "& {$ErrorActionPreference = 'Stop'; try { Write-Host '   [DEBUG] Iniciando extraccion de instalador...'; $installerPath = '%~f0'; Write-Host '   [DEBUG] Ruta del instalador:' $installerPath; if (-not (Test-Path $installerPath)) { throw 'No se puede acceder al archivo del instalador' }; $installerBytes = [System.IO.File]::ReadAllBytes($installerPath); Write-Host '   [DEBUG] Tamano del instalador:' $installerBytes.Length 'bytes'; $marker = [System.Text.Encoding]::UTF8.GetBytes('__ZIP_DATA__'); Write-Host '   [DEBUG] Buscando marcador ZIP...'; $markerIndex = -1; for ($i = 0; $i -le ($installerBytes.Length - $marker.Length); $i++) { $found = $true; for ($j = 0; $j -lt $marker.Length; $j++) { if ($installerBytes[$i + $j] -ne $marker[$j]) { $found = $false; break; } }; if ($found) { $markerIndex = $i + $marker.Length; break; } }; if ($markerIndex -eq -1) { throw 'Marcador ZIP no encontrado en el instalador' }; Write-Host '   [DEBUG] Marcador encontrado en posicion:' $markerIndex; $zipDataSize = $installerBytes.Length - $markerIndex; Write-Host '   [DEBUG] Tamano de datos ZIP:' $zipDataSize 'bytes'; if ($zipDataSize -lt 1000) { throw 'Datos ZIP demasiado pequenos: ' + $zipDataSize + ' bytes' }; $zipData = New-Object byte[] $zipDataSize; [Array]::Copy($installerBytes, $markerIndex, $zipData, 0, $zipDataSize); $tempZipPath = '%TEMP_DIR%\app_data.zip'; [System.IO.File]::WriteAllBytes($tempZipPath, $zipData); Write-Host '   [DEBUG] ZIP temporal creado:' $tempZipPath; $zipInfo = Get-Item $tempZipPath; Write-Host '   [DEBUG] Tamano del archivo ZIP:' $zipInfo.Length 'bytes'; Add-Type -AssemblyName System.IO.Compression.FileSystem; Write-Host '   [DEBUG] Validando integridad del ZIP...'; try { $zipArchive = [System.IO.Compression.ZipFile]::OpenRead($tempZipPath); $entryCount = $zipArchive.Entries.Count; Write-Host '   [DEBUG] ZIP valido - Entradas:' $entryCount; $zipArchive.Dispose(); } catch { throw 'ZIP corrupto o invalido: ' + $_.Exception.Message }; $extractPath = '%TEMP_DIR%\app'; Write-Host '   [DEBUG] Extrayendo a:' $extractPath; if (Test-Path $extractPath) { Remove-Item $extractPath -Recurse -Force }; [System.IO.Compression.ZipFile]::ExtractToDirectory($tempZipPath, $extractPath); Write-Host '   [SUCCESS] Extraccion completada exitosamente'; $extractedFiles = Get-ChildItem $extractPath -Recurse -File; Write-Host '   [DEBUG] Archivos extraidos:' $extractedFiles.Count; } catch { Write-Host '   [ERROR CRITICO] Fallo en extraccion:' $_.Exception.Message; Write-Host '   [ERROR DETALLE]' $_; exit 1; }}"

if errorlevel 1 (
    echo [ERROR] Fallo critico en la extraccion de archivos.
    echo Error critico en extraccion >> "%LOG_FILE%"
    echo.
    echo DIAGNOSTICO AVANZADO:
    echo - Verificar que el instalador no este corrupto
    echo - Comprobar que tiene permisos de escritura en TEMP
    echo - Revisar el log para mas detalles: %LOG_FILE%
    echo.
    echo INTENTAR:
    echo 1. Ejecutar desde una ubicacion diferente
    echo 2. Desactivar temporalmente el antivirus
    echo 3. Re-generar el instalador
    echo.
    pause
    exit /b 1
)

echo [INFO] Archivos extraidos correctamente.
echo Extraccion exitosa >> "%LOG_FILE%"

:: Verificar archivos extraidos
echo [PASO 2.5] Verificando archivos extraidos...
if not exist "%TEMP_DIR%\app\GestionTime.Desktop.exe" (
    echo [ERROR] Archivo principal no encontrado en extraccion.
    echo Archivo principal faltante >> "%LOG_FILE%"
    echo.
    echo DIAGNOSTICO - Contenido extraido:
    if exist "%TEMP_DIR%\app" (
        dir "%TEMP_DIR%\app" /b
        echo.
        echo Subdirectorios:
        dir "%TEMP_DIR%\app" /ad /b
    ) else (
        echo No se creo el directorio de extraccion
    )
    echo.
    pause
    exit /b 1
)

:: Contar archivos extraidos
for /f %%i in ('dir /b /s "%TEMP_DIR%\app\*" 2^>nul ^| find /c /v ""') do set "EXTRACTED_COUNT=%%i"
echo [INFO] Archivos extraidos: !EXTRACTED_COUNT!
echo Archivos extraidos: !EXTRACTED_COUNT! >> "%LOG_FILE%"

:: Crear directorio raiz App si no existe
echo [PASO 2.7] Preparando directorio raiz...
if not exist "C:\App" (
    mkdir "C:\App" 2>>"%LOG_FILE%"
    if errorlevel 1 (
        echo [ERROR] No se pudo crear directorio C:\App
        echo Error creando directorio raiz >> "%LOG_FILE%"
        pause
        exit /b 1
    )
    echo [INFO] Directorio C:\App creado
    echo Directorio C:\App creado >> "%LOG_FILE%"
)

:: Crear directorio de instalacion con backup
echo [PASO 3] Preparando directorio de instalacion...
if exist "%INSTALL_DIR%" (
    echo [INFO] Haciendo backup de instalacion anterior...
    set "BACKUP_DIR=%INSTALL_DIR%.backup.%DATE:/=-%_%TIME::=-%"
    set "BACKUP_DIR=!BACKUP_DIR: =!"
    
    move "%INSTALL_DIR%" "!BACKUP_DIR!" >nul 2>&1
    if errorlevel 1 (
        echo [WARNING] No se pudo hacer backup, eliminando instalacion anterior...
        rmdir /s /q "%INSTALL_DIR%" 2>>"%LOG_FILE%"
    ) else (
        echo Backup creado en !BACKUP_DIR! >> "%LOG_FILE%"
    )
    
    timeout /t 1 /nobreak >nul
)

mkdir "%INSTALL_DIR%" 2>>"%LOG_FILE%"
if errorlevel 1 (
    echo [ERROR] No se pudo crear directorio de instalacion.
    echo Error creando directorio instalacion >> "%LOG_FILE%"
    pause
    exit /b 1
)

:: Copiar archivos con progreso y verificacion
echo [PASO 4] Copiando archivos de aplicacion...
echo Iniciando copia de archivos... >> "%LOG_FILE%"

xcopy "%TEMP_DIR%\app\*" "%INSTALL_DIR%\" /E /I /H /Y /Q 2>>"%LOG_FILE%"

if errorlevel 1 (
    echo [ERROR] Fallo la copia de archivos.
    echo Error en copia de archivos >> "%LOG_FILE%"
    pause
    exit /b 1
)

echo [INFO] Archivos copiados correctamente.
echo Copia de archivos exitosa >> "%LOG_FILE%"

:: Verificar instalacion completa
echo [PASO 4.5] Verificando instalacion...
if not exist "%INSTALL_DIR%\GestionTime.Desktop.exe" (
    echo [ERROR] Verificacion fallo: archivo principal no encontrado.
    echo Verificacion fallo >> "%LOG_FILE%"
    pause
    exit /b 1
)

:: Contar archivos instalados
for /f %%i in ('dir /b /s "%INSTALL_DIR%\*" 2^>nul ^| find /c /v ""') do set "INSTALLED_COUNT=%%i"
echo [INFO] Archivos instalados: !INSTALLED_COUNT!
echo Archivos instalados: !INSTALLED_COUNT! >> "%LOG_FILE%"

if !INSTALLED_COUNT! LSS 100 (
    echo [WARNING] Pocos archivos instalados, verificar instalacion.
    echo Warning: Pocos archivos instalados >> "%LOG_FILE%"
)

:: Crear accesos directos con verificacion
echo [PASO 5] Creando accesos directos...
powershell -NoProfile -ExecutionPolicy Bypass -Command "try { $ws = New-Object -ComObject WScript.Shell; $shortcut = $ws.CreateShortcut('%SHORTCUT_DIR%\GestionTime Desktop.lnk'); $shortcut.TargetPath = '%INSTALL_DIR%\GestionTime.Desktop.exe'; $shortcut.WorkingDirectory = '%INSTALL_DIR%'; $shortcut.Description = 'Aplicacion de gestion de tiempo'; $shortcut.Save(); Write-Host 'Acceso directo menu inicio creado' } catch { Write-Host 'Error creando acceso directo menu:' $_ }"

powershell -NoProfile -ExecutionPolicy Bypass -Command "try { $ws = New-Object -ComObject WScript.Shell; $shortcut = $ws.CreateShortcut('%PUBLIC%\Desktop\GestionTime Desktop.lnk'); $shortcut.TargetPath = '%INSTALL_DIR%\GestionTime.Desktop.exe'; $shortcut.WorkingDirectory = '%INSTALL_DIR%'; $shortcut.Description = 'Aplicacion de gestion de tiempo'; $shortcut.Save(); Write-Host 'Acceso directo escritorio creado' } catch { Write-Host 'Error creando acceso directo escritorio:' $_ }"

echo [INFO] Accesos directos creados.
echo Accesos directos creados >> "%LOG_FILE%"

:: Registro en Panel de Control MEJORADO
echo [PASO 6] Registrando en Panel de Control...
reg add "HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop" /v "DisplayName" /t REG_SZ /d "GestionTime Desktop" /f >nul 2>>"%LOG_FILE%"
reg add "HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop" /v "DisplayVersion" /t REG_SZ /d "1.1.0.0" /f >nul 2>>"%LOG_FILE%"
reg add "HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop" /v "Publisher" /t REG_SZ /d "GestionTime Solutions" /f >nul 2>>"%LOG_FILE%"
reg add "HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop" /v "InstallLocation" /t REG_SZ /d "%INSTALL_DIR%" /f >nul 2>>"%LOG_FILE%"
reg add "HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop" /v "UninstallString" /t REG_SZ /d "%INSTALL_DIR%\Uninstall.bat" /f >nul 2>>"%LOG_FILE%"
reg add "HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop" /v "DisplayIcon" /t REG_SZ /d "%INSTALL_DIR%\GestionTime.Desktop.exe" /f >nul 2>>"%LOG_FILE%"
reg add "HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop" /v "InstallDate" /t REG_SZ /d "%DATE:/=%" /f >nul 2>>"%LOG_FILE%"

:: Crear desinstalador mejorado
echo [PASO 7] Creando desinstalador...
(
echo @echo off
echo title Desinstalador GestionTime Desktop
echo echo Desinstalando GestionTime Desktop...
echo taskkill /f /im GestionTime.Desktop.exe /t ^>nul 2^>^&1
echo timeout /t 2 /nobreak ^>nul
echo echo Eliminando archivos...
echo rmdir /s /q "%INSTALL_DIR%" 2^>nul
echo echo Eliminando accesos directos...
echo del "%PUBLIC%\Desktop\GestionTime Desktop.lnk" ^>nul 2^>^&1
echo del "%SHORTCUT_DIR%\GestionTime Desktop.lnk" ^>nul 2^>^&1
echo echo Eliminando registro...
echo reg delete "HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop" /f ^>nul 2^>^&1
echo echo Desinstalacion completada.
echo echo GestionTime Desktop ha sido desinstalado completamente.
echo pause
) > "%INSTALL_DIR%\Uninstall.bat"

echo [INFO] Desinstalador creado.

:: Limpiar archivos temporales
echo [PASO 8] Limpiando archivos temporales...
if exist "%TEMP_DIR%" (
    rmdir /s /q "%TEMP_DIR%" 2>>"%LOG_FILE%"
    echo Archivos temporales eliminados >> "%LOG_FILE%"
)

:: Log final
echo End Time: %DATE% %TIME% >> "%LOG_FILE%"
echo Installation completed successfully >> "%LOG_FILE%"

echo [INFO] Instalacion completada exitosamente.
echo.
echo ========================================================
echo   INSTALACION COMPLETADA CON EXITO
echo ========================================================
echo.
echo Aplicacion instalada en: %INSTALL_DIR%
echo Archivos instalados: !INSTALLED_COUNT!
echo Accesos directos creados en Escritorio y Menu Inicio
echo Log de instalacion: %LOG_FILE%
echo.

:: Verificacion final mejorada
if exist "%INSTALL_DIR%\GestionTime.Desktop.exe" (
    echo [INFO] Verificacion final: EXITOSA
    echo.
    set /p "launch=Desea ejecutar GestionTime Desktop ahora? (S/N): "
    if /i "!launch!"=="S" (
        echo [INFO] Iniciando GestionTime Desktop...
        start "" "%INSTALL_DIR%\GestionTime.Desktop.exe"
    )
) else (
    echo [ERROR] Verificacion final: FALLO
    echo La instalacion no se pudo verificar correctamente.
    echo Revisar log: %LOG_FILE%
)

echo.
echo Gracias por instalar GestionTime Desktop!
echo Para soporte: support@gestiontime.com
pause
exit /b 0

__ZIP_DATA__
'@

    # Crear el instalador auto-extraíble MEJORADO
    $installerPath = Join-Path $outputDir "GestionTimeDesktopInstaller.bat"
    $installerScript | Out-File -FilePath $installerPath -Encoding ASCII -NoNewline

    # Agregar los datos del ZIP al final del archivo (sin base64, binario directo)
    $zipBytes = [System.IO.File]::ReadAllBytes($zipPath)
    $installerBytes = [System.IO.File]::ReadAllBytes($installerPath)
    
    # Combinar script + marcador + datos ZIP
    $combinedBytes = New-Object byte[] ($installerBytes.Length + $zipBytes.Length)
    [Array]::Copy($installerBytes, 0, $combinedBytes, 0, $installerBytes.Length)
    [Array]::Copy($zipBytes, 0, $combinedBytes, $installerBytes.Length, $zipBytes.Length)
    
    [System.IO.File]::WriteAllBytes($installerPath, $combinedBytes)

    $stopwatch.Stop()

    # Verificar el resultado
    $installerFile = Get-Item $installerPath
    
    Write-Host ""
    Write-Host "✅ INSTALADOR AUTO-EXTRAÍBLE MEJORADO CREADO" -ForegroundColor Green -BackgroundColor DarkGreen
    Write-Host "===============================================" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "📊 INFORMACIÓN DEL INSTALADOR MEJORADO:" -ForegroundColor Magenta
    Write-Host "   • Archivo: $($installerFile.Name)" -ForegroundColor White
    Write-Host "   • Tamaño: $([math]::Round($installerFile.Length/1MB, 2)) MB" -ForegroundColor White
    Write-Host "   • Ubicación: $($installerFile.FullName)" -ForegroundColor White
    Write-Host "   • Tiempo de creación: $($stopwatch.Elapsed.TotalSeconds.ToString('F1')) segundos" -ForegroundColor White
    Write-Host ""
    
    Write-Host "🎯 MEJORAS IMPLEMENTADAS:" -ForegroundColor Cyan
    Write-Host "   ✅ Verificación mejorada de permisos de administrador" -ForegroundColor Green
    Write-Host "   ✅ Manejo de errores más robusto" -ForegroundColor Green
    Write-Host "   ✅ Log detallado de instalación" -ForegroundColor Green
    Write-Host "   ✅ Verificación de espacio en disco" -ForegroundColor Green
    Write-Host "   ✅ Backup automático de instalación anterior" -ForegroundColor Green
    Write-Host "   ✅ Conteo y verificación de archivos instalados" -ForegroundColor Green
    Write-Host "   ✅ Mejor diagnóstico de problemas" -ForegroundColor Green
    Write-Host "   ✅ Extracción binaria directa (sin base64)" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "📋 INSTRUCCIONES PARA USAR EL INSTALADOR MEJORADO:" -ForegroundColor Yellow
    Write-Host "===============================================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "🔴 OPCIÓN 1: Ejecutar con permisos de administrador" -ForegroundColor Red
    Write-Host "   1. Click derecho en: $($installerFile.Name)" -ForegroundColor White
    Write-Host "   2. Seleccionar: 'Ejecutar como administrador'" -ForegroundColor White
    Write-Host "   3. Seguir las instrucciones del instalador" -ForegroundColor White
    Write-Host ""
    Write-Host "🔵 OPCIÓN 2: Desde PowerShell como administrador" -ForegroundColor Blue
    Write-Host "   1. Abrir PowerShell como administrador" -ForegroundColor White
    Write-Host "   2. Navegar al directorio del proyecto" -ForegroundColor White
    Write-Host "   3. Ejecutar: .\$($installerFile.Name)" -ForegroundColor White
    Write-Host ""

    # Crear script para ejecutar con elevación automática
    if ($CreateElevated) {
        Write-Host "🚀 CREANDO SCRIPT DE ELEVACIÓN AUTOMÁTICA:" -ForegroundColor Blue
        
        $elevatedScript = @"
# Script para ejecutar instalador con elevación automática
if (!([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "Solicitando permisos de administrador..." -ForegroundColor Yellow
    Start-Process PowerShell -Verb RunAs -ArgumentList "-ExecutionPolicy Bypass -Command `"cd '$((Get-Location).Path)'; cmd /c '$(Split-Path $installerFile.FullName -Leaf)'`""
    exit
}

Write-Host "Ejecutando instalador con permisos de administrador..." -ForegroundColor Green
cmd /c "$($installerFile.FullName)"
"@
        
        $elevatedScriptPath = Join-Path $outputDir "InstallAsAdmin.ps1"
        $elevatedScript | Out-File -FilePath $elevatedScriptPath -Encoding UTF8
        
        Write-Host "   ✅ Script de elevación creado: InstallAsAdmin.ps1" -ForegroundColor Green
        Write-Host "   📋 Usar: .\InstallAsAdmin.ps1" -ForegroundColor White
    }
    
    if ($OpenOutput) {
        Write-Host "📂 Abriendo directorio de salida..." -ForegroundColor Green
        Start-Process "explorer.exe" -ArgumentList $outputDir
    }
    
    Write-Host ""
    Write-Host "🎉 ¡INSTALADOR MEJORADO LISTO!" -ForegroundColor Green -BackgroundColor DarkGreen
    Write-Host "===============================================" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "💡 PRÓXIMOS PASOS:" -ForegroundColor Blue
    Write-Host "   1. Ejecutar como administrador para instalar" -ForegroundColor White
    Write-Host "   2. El instalador creará log en %TEMP%\GestionTimeInstaller.log" -ForegroundColor White
    Write-Host "   3. Si hay errores, revisar el log para diagnóstico" -ForegroundColor White

} catch {
    Write-Host "❌ ERROR DURANTE LA CREACIÓN:" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor White
    exit 1
}

Write-Host ""
Write-Host "🔧 INSTALADOR MEJORADO COMPLETADO" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green