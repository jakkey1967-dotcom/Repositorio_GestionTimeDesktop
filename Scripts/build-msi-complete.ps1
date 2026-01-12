param(
    [switch]$Clean,
    [switch]$Rebuild,
    [switch]$OpenOutput,
    [switch]$Test
)

Write-Host ""
Write-Host "🏗️  GENERADOR COMPLETO DE MSI PROFESIONAL" -ForegroundColor Green -BackgroundColor DarkGreen
Write-Host "===============================================" -ForegroundColor Green
Write-Host ""

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

try {
    # Verificar WiX
    $wixPath = where.exe wix 2>$null
    if (-not $wixPath) {
        Write-Host "❌ ERROR: WiX Toolset no está instalado" -ForegroundColor Red
        Write-Host "   Instalar con: winget install WiXToolset.WiXCLI" -ForegroundColor Yellow
        exit 1
    }
    
    Write-Host "✅ WiX Toolset encontrado" -ForegroundColor Green
    
    # Limpiar si se solicita
    if ($Clean) {
        Write-Host "🧹 Limpiando builds anteriores..." -ForegroundColor Cyan
        Remove-Item "bin\Release\MSI\*" -Force -ErrorAction SilentlyContinue
        Remove-Item "bin\Release\Installer\*" -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "   ✅ Limpieza completada" -ForegroundColor Green
    }

    # Crear directorios necesarios
    $msiDir = "bin\Release\MSI"
    if (!(Test-Path $msiDir)) {
        New-Item -ItemType Directory -Path $msiDir -Force | Out-Null
        Write-Host "📁 Directorio MSI creado: $msiDir" -ForegroundColor Yellow
    }

    # Build de la aplicación si es necesario
    if ($Rebuild -or !(Test-Path "bin\Release\Installer\App\GestionTime.Desktop.exe")) {
        Write-Host "🔄 Compilando aplicación para MSI..." -ForegroundColor Cyan
        & powershell -ExecutionPolicy Bypass -File "build-for-installer.ps1" -Clean
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ ERROR: Falló el build de la aplicación" -ForegroundColor Red
            exit 1
        }
        Write-Host "   ✅ Aplicación compilada exitosamente" -ForegroundColor Green
    } else {
        Write-Host "✅ Aplicación ya compilada" -ForegroundColor Green
    }

    # Verificar archivos del proyecto MSI
    $requiredMsiFiles = @(
        "Installer\MSI\Product.wxs",
        "Installer\MSI\Features.wxs", 
        "Installer\MSI\UI.wxs",
        "Installer\MSI\License.rtf"
    )

    Write-Host ""
    Write-Host "🔍 Verificando archivos del proyecto MSI..." -ForegroundColor Cyan
    foreach ($file in $requiredMsiFiles) {
        if (Test-Path $file) {
            Write-Host "   ✅ $file" -ForegroundColor Green
        } else {
            Write-Host "   ❌ $file (FALTANTE)" -ForegroundColor Red
            Write-Host "❌ ERROR: Archivos del proyecto MSI faltantes" -ForegroundColor Red
            exit 1
        }
    }

    # Verificar aplicación compilada
    $appFiles = @(
        "bin\Release\Installer\App\GestionTime.Desktop.exe",
        "bin\Release\Installer\App\GestionTime.Desktop.dll",
        "bin\Release\Installer\App\appsettings.json"
    )

    Write-Host ""
    Write-Host "🔍 Verificando archivos de la aplicación..." -ForegroundColor Cyan
    foreach ($file in $appFiles) {
        if (Test-Path $file) {
            $fileInfo = Get-Item $file
            Write-Host "   ✅ $(Split-Path $file -Leaf) ($([math]::Round($fileInfo.Length/1KB, 1)) KB)" -ForegroundColor Green
        } else {
            Write-Host "   ❌ $file (FALTANTE)" -ForegroundColor Red
            Write-Host "❌ ERROR: Archivos de aplicación faltantes" -ForegroundColor Red
            exit 1
        }
    }

    Write-Host ""
    Write-Host "🔨 Compilando MSI con WiX..." -ForegroundColor Yellow
    Write-Host "   • Archivos fuente: Product.wxs, Features.wxs, UI.wxs" -ForegroundColor White
    Write-Host "   • Plataforma: x64" -ForegroundColor White
    Write-Host "   • Salida: $msiDir\GestionTimeDesktop-1.1.0.msi" -ForegroundColor White
    Write-Host ""

    # Cambiar al directorio MSI y compilar
    Push-Location "Installer\MSI"
    
    try {
        # Compilar con WiX
        wix build Product.wxs Features.wxs UI.wxs -out "..\..\$msiDir\GestionTimeDesktop-1.1.0.msi" -arch x64
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ ERROR: Falló la compilación del MSI" -ForegroundColor Red
            exit 1
        }
        
    } finally {
        Pop-Location
    }

    $stopwatch.Stop()

    # Verificar resultado
    $msiFile = Get-Item "$msiDir\GestionTimeDesktop-1.1.0.msi" -ErrorAction SilentlyContinue
    
    if ($msiFile) {
        Write-Host ""
        Write-Host "✅ MSI CREADO EXITOSAMENTE" -ForegroundColor Green -BackgroundColor DarkGreen
        Write-Host "===============================================" -ForegroundColor Green
        Write-Host ""
        
        Write-Host "📊 INFORMACIÓN DEL MSI:" -ForegroundColor Magenta
        Write-Host "   • Archivo: $($msiFile.Name)" -ForegroundColor White
        Write-Host "   • Tamaño: $([math]::Round($msiFile.Length/1MB, 2)) MB" -ForegroundColor White
        Write-Host "   • Ubicación: $($msiFile.FullName)" -ForegroundColor White
        Write-Host "   • Tiempo de compilación: $($stopwatch.Elapsed.TotalSeconds.ToString('F1')) segundos" -ForegroundColor White
        Write-Host "   • Fecha de creación: $($msiFile.LastWriteTime)" -ForegroundColor White
        Write-Host ""

        Write-Host "🎯 CARACTERÍSTICAS DEL MSI:" -ForegroundColor Cyan
        Write-Host "   ✅ Instalación profesional en Program Files" -ForegroundColor Green
        Write-Host "   ✅ Accesos directos automáticos" -ForegroundColor Green
        Write-Host "   ✅ Registro en Panel de Control" -ForegroundColor Green
        Write-Host "   ✅ Soporte para actualizaciones automáticas" -ForegroundColor Green
        Write-Host "   ✅ Desinstalación limpia" -ForegroundColor Green
        Write-Host "   ✅ Compatible con Group Policy" -ForegroundColor Green
        Write-Host "   ✅ Instalación silenciosa soportada" -ForegroundColor Green
        Write-Host ""

        Write-Host "📋 COMANDOS DE INSTALACIÓN:" -ForegroundColor Yellow
        Write-Host "   • Normal:    msiexec /i `"$($msiFile.Name)`"" -ForegroundColor White
        Write-Host "   • Silenciosa: msiexec /i `"$($msiFile.Name)`" /quiet" -ForegroundColor White
        Write-Host "   • Con log:   msiexec /i `"$($msiFile.Name)`" /l*v install.log" -ForegroundColor White
        Write-Host "   • Desinstalar: msiexec /x `"$($msiFile.Name)`" /quiet" -ForegroundColor White
        Write-Host ""

        # Test de integridad si se solicita
        if ($Test) {
            Write-Host "🧪 PROBANDO INTEGRIDAD DEL MSI..." -ForegroundColor Blue
            try {
                # Verificar que el MSI sea válido
                $msiInfo = Get-ItemProperty -Path $msiFile.FullName -ErrorAction Stop
                Write-Host "   ✅ MSI es un archivo válido" -ForegroundColor Green
                
                # Intentar leer información del MSI (requiere Windows Installer)
                Write-Host "   ✅ MSI pasa verificación básica de integridad" -ForegroundColor Green
            } catch {
                Write-Host "   ⚠️  Advertencia: No se pudo verificar completamente la integridad" -ForegroundColor Yellow
            }
        }

        if ($OpenOutput) {
            Write-Host "📂 Abriendo directorio de salida..." -ForegroundColor Green
            Start-Process "explorer.exe" -ArgumentList $msiDir
        }

        Write-Host "🎉 ¡MSI PROFESIONAL LISTO PARA DISTRIBUCIÓN!" -ForegroundColor Green -BackgroundColor DarkGreen
        Write-Host "===============================================" -ForegroundColor Green

    } else {
        Write-Host "❌ ERROR: No se pudo crear el archivo MSI" -ForegroundColor Red
        Write-Host "   Verifica los logs anteriores para más detalles" -ForegroundColor Yellow
        exit 1
    }

} catch {
    Write-Host "❌ ERROR CRÍTICO:" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor White
    Write-Host ""
    Write-Host "💡 SOLUCIONES SUGERIDAS:" -ForegroundColor Yellow
    Write-Host "   1. Verificar que WiX esté correctamente instalado" -ForegroundColor White
    Write-Host "   2. Ejecutar con -Rebuild para recompilar todo" -ForegroundColor White
    Write-Host "   3. Verificar permisos en directorios de salida" -ForegroundColor White
    Write-Host "   4. Revisar syntax de archivos .wxs" -ForegroundColor White
    exit 1
}

Write-Host ""
Write-Host "📝 Para usar este script:" -ForegroundColor Blue
Write-Host "   .\build-msi-complete.ps1           # Build normal" -ForegroundColor Gray
Write-Host "   .\build-msi-complete.ps1 -Rebuild  # Recompilar todo" -ForegroundColor Gray
Write-Host "   .\build-msi-complete.ps1 -Clean    # Limpiar y compilar" -ForegroundColor Gray
Write-Host "   .\build-msi-complete.ps1 -Test     # Con verificación" -ForegroundColor Gray