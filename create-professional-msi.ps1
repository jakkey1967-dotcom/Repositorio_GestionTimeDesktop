param(
    [switch]$Rebuild,
    [switch]$OpenOutput,
    [switch]$InstallAfterBuild
)

Write-Host ""
Write-Host "🏗️  CREANDO INSTALADOR MSI PROFESIONAL CON WiX" -ForegroundColor Green -BackgroundColor DarkGreen
Write-Host "===============================================" -ForegroundColor Green
Write-Host ""

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
$ErrorActionPreference = "Stop"

try {
    # Verificar WiX
    $wixPath = where.exe wix 2>$null
    if (-not $wixPath) {
        Write-Host "❌ ERROR: WiX Toolset no está instalado" -ForegroundColor Red
        Write-Host "   Instalar con: winget install WiXToolset.WiXCLI" -ForegroundColor Yellow
        exit 1
    }
    
    Write-Host "✅ WiX v6.0.2 encontrado: $wixPath" -ForegroundColor Green

    # Rebuild de la aplicación si es necesario
    if ($Rebuild) {
        Write-Host "🔄 Rebuilding aplicación para MSI..." -ForegroundColor Cyan
        & powershell -ExecutionPolicy Bypass -File "build-for-installer.ps1" -Clean
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ ERROR: Falló el build de la aplicación" -ForegroundColor Red
            exit 1
        }
    }

    # Verificar que la aplicación esté compilada
    $appPath = "bin\Release\Installer\App\GestionTime.Desktop.exe"
    if (!(Test-Path $appPath)) {
        Write-Host "❌ ERROR: Aplicación no encontrada en $appPath" -ForegroundColor Red
        Write-Host "   Ejecutar: .\build-for-installer.ps1 -Clean" -ForegroundColor Yellow
        exit 1
    }

    Write-Host "✅ Aplicación encontrada: $appPath" -ForegroundColor Green

    # Verificar archivos del proyecto MSI
    $msiProject = "Installer\MSI\GestionTimeDesktop.wixproj"
    $productWxs = "Installer\MSI\Product.wxs"
    $featuresWxs = "Installer\MSI\Features.wxs"
    $uiWxs = "Installer\MSI\UI.wxs"

    $requiredFiles = @($msiProject, $productWxs, $featuresWxs, $uiWxs)
    foreach ($file in $requiredFiles) {
        if (!(Test-Path $file)) {
            Write-Host "❌ ERROR: Archivo del proyecto MSI faltante: $file" -ForegroundColor Red
            exit 1
        }
    }

    Write-Host "✅ Archivos del proyecto MSI verificados" -ForegroundColor Green

    # Crear directorio de salida
    $outputDir = "bin\Release\MSI"
    if (!(Test-Path $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    }

    # Limpiar build anterior
    Remove-Item "$outputDir\*" -Force -ErrorAction SilentlyContinue

    Write-Host ""
    Write-Host "📦 Compilando proyecto MSI..." -ForegroundColor Cyan
    Write-Host "   • Proyecto: $msiProject" -ForegroundColor White
    Write-Host "   • Plataforma: x64" -ForegroundColor White
    Write-Host "   • Configuración: Release" -ForegroundColor White
    Write-Host "   • Salida: $outputDir" -ForegroundColor White
    Write-Host ""

    # Cambiar al directorio del proyecto MSI
    Push-Location "Installer\MSI"
    
    try {
        Write-Host "🔨 Ejecutando build de WiX..." -ForegroundColor Yellow
        
        # Build del proyecto WiX
        wix build GestionTimeDesktop.wixproj -arch x64 -out "..\..\$outputDir\GestionTimeDesktop-1.1.0.msi"
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ ERROR: Falló la compilación del proyecto MSI" -ForegroundColor Red
            exit 1
        }

        $stopwatch.Stop()

        Write-Host ""
        Write-Host "✅ INSTALADOR MSI PROFESIONAL CREADO" -ForegroundColor Green -BackgroundColor DarkGreen
        Write-Host "===============================================" -ForegroundColor Green
        Write-Host ""

        # Verificar resultado
        $msiPath = "..\..\$outputDir\GestionTimeDesktop-1.1.0.msi"
        if (Test-Path $msiPath) {
            $msiFile = Get-Item $msiPath
            
            Write-Host "📊 INFORMACIÓN DEL MSI PROFESIONAL:" -ForegroundColor Magenta
            Write-Host "   • Archivo: $($msiFile.Name)" -ForegroundColor White
            Write-Host "   • Tamaño: $([math]::Round($msiFile.Length/1MB, 2)) MB" -ForegroundColor White
            Write-Host "   • Ubicación: $($msiFile.FullName)" -ForegroundColor White
            Write-Host "   • Tiempo de build: $($stopwatch.Elapsed.TotalSeconds.ToString('F1')) segundos" -ForegroundColor White
            Write-Host ""

            Write-Host "🎯 CARACTERÍSTICAS DEL MSI PROFESIONAL:" -ForegroundColor Cyan
            Write-Host "   ✅ Interfaz de instalación avanzada (WixUI_FeatureTree)" -ForegroundColor Green
            Write-Host "   ✅ Selección de características personalizable" -ForegroundColor Green
            Write-Host "   ✅ Accesos directos opcionales (Escritorio/Menú)" -ForegroundColor Green
            Write-Host "   ✅ Actualizaciones automáticas soportadas" -ForegroundColor Green
            Write-Host "   ✅ Registro en Panel de Control completo" -ForegroundColor Green
            Write-Host "   ✅ Desinstalación limpia y completa" -ForegroundColor Green
            Write-Host "   ✅ Soporte para Group Policy" -ForegroundColor Green
            Write-Host "   ✅ Instalación silenciosa disponible" -ForegroundColor Green
            Write-Host ""

            Write-Host "📋 INSTALACIÓN PARA USUARIOS:" -ForegroundColor Yellow
            Write-Host "   • Doble-click en: $($msiFile.Name)" -ForegroundColor White
            Write-Host "   • Seguir asistente de instalación" -ForegroundColor White
            Write-Host "   • Seleccionar características deseadas" -ForegroundColor White
            Write-Host "   • Confirmar ubicación de instalación" -ForegroundColor White
            Write-Host ""

            Write-Host "🔧 INSTALACIÓN SILENCIOSA:" -ForegroundColor Blue
            Write-Host "   msiexec /i `"$($msiFile.Name)`" /quiet ADDLOCAL=ALL" -ForegroundColor Gray
            Write-Host ""

            Write-Host "🔧 INSTALACIÓN CON LOG:" -ForegroundColor Blue
            Write-Host "   msiexec /i `"$($msiFile.Name)`" /l*v install.log" -ForegroundColor Gray
            Write-Host ""

            Write-Host "❌ DESINSTALACIÓN:" -ForegroundColor Red
            Write-Host "   • Panel de Control → Programas → GestionTime Desktop" -ForegroundColor White
            Write-Host "   • O: msiexec /x `"$($msiFile.Name)`" /quiet" -ForegroundColor Gray
            Write-Host ""

            if ($InstallAfterBuild) {
                Write-Host "🚀 Instalando MSI automáticamente..." -ForegroundColor Green
                Start-Process "msiexec.exe" -ArgumentList "/i", "`"$($msiFile.FullName)`"", "/qb" -Wait
            }

            if ($OpenOutput) {
                Write-Host "📂 Abriendo directorio de salida..." -ForegroundColor Green
                Start-Process "explorer.exe" -ArgumentList (Split-Path $msiFile.FullName)
            }

            Write-Host "🎉 ¡MSI PROFESIONAL LISTO PARA DISTRIBUCIÓN!" -ForegroundColor Green -BackgroundColor DarkGreen
            Write-Host "===============================================" -ForegroundColor Green

        } else {
            Write-Host "❌ ERROR: No se pudo encontrar el archivo MSI generado" -ForegroundColor Red
            exit 1
        }

    } finally {
        Pop-Location
    }

} catch {
    Write-Host "❌ ERROR DURANTE LA CREACIÓN DEL MSI:" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor White
    
    # Información adicional para debugging
    Write-Host ""
    Write-Host "💡 INFORMACIÓN DE DEBUGGING:" -ForegroundColor Yellow
    Write-Host "   • Verificar que todos los archivos .wxs estén correctos" -ForegroundColor White
    Write-Host "   • Verificar rutas de archivos en Features.wxs" -ForegroundColor White
    Write-Host "   • Ejecutar: wix build -v para más detalles" -ForegroundColor White
    Write-Host ""
    
    exit 1
}