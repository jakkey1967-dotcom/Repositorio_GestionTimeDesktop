param(
    [string]$Configuration = "Release",
    [switch]$Rebuild,
    [switch]$OpenOutput
)

Write-Host ""
Write-Host "🏗️  CREANDO INSTALADOR MSI CON WiX" -ForegroundColor Green -BackgroundColor DarkGreen
Write-Host "===============================================" -ForegroundColor Green
Write-Host ""

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
$ErrorActionPreference = "Stop"

try {
    # Verificar que WiX esté instalado
    $wixPath = where.exe wix 2>$null
    if (-not $wixPath) {
        Write-Host "❌ ERROR: WiX Toolset no está instalado" -ForegroundColor Red
        Write-Host "   Instalar con: winget install WiXToolset.WiXCLI" -ForegroundColor Yellow
        exit 1
    }
    
    Write-Host "✅ WiX encontrado en: $wixPath" -ForegroundColor Green

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
    $appPath = "bin\Release\Installer\App\GestionTime.Desktop.exe"
    if (!(Test-Path $appPath)) {
        Write-Host "❌ ERROR: No se encontró la aplicación compilada" -ForegroundColor Red
        Write-Host "   Ejecutar primero: .\build-for-installer.ps1 -Clean" -ForegroundColor Yellow
        exit 1
    }

    Write-Host "✅ Aplicación encontrada: $appPath" -ForegroundColor Green

    # Crear directorio de salida para MSI
    $msiOutputPath = "bin\Release\MSI"
    if (!(Test-Path $msiOutputPath)) {
        New-Item -ItemType Directory -Path $msiOutputPath -Force | Out-Null
    }

    # Limpiar archivos anteriores
    Remove-Item "$msiOutputPath\*" -Force -ErrorAction SilentlyContinue

    Write-Host "📦 Compilando instalador MSI..." -ForegroundColor Cyan
    Write-Host "   • Archivo fuente: Installer\Product.wxs" -ForegroundColor White
    Write-Host "   • Directorio de salida: $msiOutputPath" -ForegroundColor White
    Write-Host ""

    # Cambiar al directorio base para paths relativos
    $originalLocation = Get-Location
    
    try {
        # Compilar el archivo WiX
        Write-Host "🔨 Paso 1: Compilando archivos WiX..." -ForegroundColor Yellow
        wix build "Installer\Product.wxs" -out "$msiOutputPath\GestionTimeDesktop-1.1.0.msi" -arch x64

        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ ERROR: Falló la compilación de WiX" -ForegroundColor Red
            exit 1
        }

        $stopwatch.Stop()
        
        Write-Host ""
        Write-Host "✅ INSTALADOR MSI CREADO EXITOSAMENTE" -ForegroundColor Green -BackgroundColor DarkGreen
        Write-Host "===============================================" -ForegroundColor Green
        Write-Host ""

        # Verificar el archivo MSI generado
        $msiFile = Get-Item "$msiOutputPath\GestionTimeDesktop-1.1.0.msi" -ErrorAction SilentlyContinue
        
        if ($msiFile) {
            Write-Host "📊 INFORMACIÓN DEL INSTALADOR:" -ForegroundColor Magenta
            Write-Host "   • Archivo MSI: $($msiFile.Name)" -ForegroundColor White
            Write-Host "   • Tamaño: $([math]::Round($msiFile.Length/1MB, 2)) MB" -ForegroundColor White
            Write-Host "   • Ubicación: $($msiFile.FullName)" -ForegroundColor White
            Write-Host "   • Tiempo de compilación: $($stopwatch.Elapsed.TotalSeconds.ToString('F1')) segundos" -ForegroundColor White
            Write-Host ""

            # Información del producto
            Write-Host "🎯 CARACTERÍSTICAS DEL INSTALADOR:" -ForegroundColor Cyan
            Write-Host "   ✅ Instalación en Program Files" -ForegroundColor Green
            Write-Host "   ✅ Accesos directos en Escritorio y Menú Inicio" -ForegroundColor Green
            Write-Host "   ✅ Registro en Panel de Control (Agregar/Quitar programas)" -ForegroundColor Green
            Write-Host "   ✅ Soporte para actualizaciones automáticas" -ForegroundColor Green
            Write-Host "   ✅ Desinstalación limpia" -ForegroundColor Green
            Write-Host "   ✅ Runtime .NET 8 incluido (self-contained)" -ForegroundColor Green
            Write-Host "   ✅ WindowsAppSDK incluido" -ForegroundColor Green
            Write-Host ""

            Write-Host "📋 INSTRUCCIONES DE DISTRIBUCIÓN:" -ForegroundColor Yellow
            Write-Host "   1. Distribuir el archivo: $($msiFile.Name)" -ForegroundColor White
            Write-Host "   2. Usuario hace doble-click para instalar" -ForegroundColor White
            Write-Host "   3. Seguir el asistente de instalación" -ForegroundColor White
            Write-Host "   4. La aplicación se ejecutará automáticamente al finalizar" -ForegroundColor White
            Write-Host ""

            Write-Host "🔧 INSTALACIÓN SILENCIOSA (OPCIONAL):" -ForegroundColor Blue
            Write-Host "   msiexec /i `"$($msiFile.Name)`" /quiet" -ForegroundColor Gray
            Write-Host ""

            Write-Host "❌ DESINSTALACIÓN:" -ForegroundColor Red
            Write-Host "   • Panel de Control → Agregar/Quitar programas → GestionTime Desktop" -ForegroundColor White
            Write-Host "   • O: msiexec /x `"$($msiFile.Name)`" /quiet" -ForegroundColor Gray
            Write-Host ""

            if ($OpenOutput) {
                Write-Host "📂 Abriendo directorio de salida..." -ForegroundColor Green
                Start-Process "explorer.exe" -ArgumentList $msiOutputPath
            } else {
                Write-Host "💡 Para abrir la carpeta del MSI, ejecuta:" -ForegroundColor Blue
                Write-Host "   .\create-msi-installer.ps1 -OpenOutput" -ForegroundColor White
            }

            Write-Host ""
            Write-Host "🎉 ¡INSTALADOR MSI LISTO PARA DISTRIBUCIÓN!" -ForegroundColor Green -BackgroundColor DarkGreen
            Write-Host "===============================================" -ForegroundColor Green

        } else {
            Write-Host "❌ ERROR: No se pudo crear el archivo MSI" -ForegroundColor Red
            exit 1
        }

    } finally {
        Set-Location $originalLocation
    }

} catch {
    Write-Host "❌ ERROR DURANTE LA CREACIÓN DEL MSI:" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor White
    Write-Host ""
    
    if ($_.Exception.Message -like "*wix*") {
        Write-Host "💡 SUGERENCIAS PARA RESOLVER ERRORES WiX:" -ForegroundColor Yellow
        Write-Host "   1. Verificar que todos los archivos fuente existan" -ForegroundColor White
        Write-Host "   2. Ejecutar: .\build-for-installer.ps1 -Clean" -ForegroundColor White
        Write-Host "   3. Verificar la sintaxis en Product.wxs" -ForegroundColor White
        Write-Host "   4. Reinstalar WiX: winget install WiXToolset.WiXCLI" -ForegroundColor White
    }
    
    exit 1
}