param(
    [switch]$Rebuild,
    [switch]$OpenOutput
)

Write-Host ""
Write-Host "🏗️  CREANDO INSTALADOR CON INNO SETUP" -ForegroundColor Green -BackgroundColor DarkGreen
Write-Host "===============================================" -ForegroundColor Green
Write-Host ""

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
$ErrorActionPreference = "Stop"

try {
    # Verificar que Inno Setup esté instalado
    $innoPath = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
    if (!(Test-Path $innoPath)) {
        Write-Host "❌ ERROR: Inno Setup no está instalado" -ForegroundColor Red
        Write-Host "   Instalar con: winget install JRSoftware.InnoSetup" -ForegroundColor Yellow
        exit 1
    }
    
    Write-Host "✅ Inno Setup encontrado: $innoPath" -ForegroundColor Green

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

    # Verificar archivos de configuración de Inno Setup
    $issFile = "Installer\GestionTimeSetup.iss"
    if (!(Test-Path $issFile)) {
        Write-Host "❌ ERROR: No se encontró el archivo de configuración: $issFile" -ForegroundColor Red
        exit 1
    }

    Write-Host "✅ Configuración encontrada: $issFile" -ForegroundColor Green

    # Crear directorio de salida
    $outputDir = "bin\Release\Installer"
    if (!(Test-Path $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    }

    Write-Host ""
    Write-Host "📦 Compilando instalador con Inno Setup..." -ForegroundColor Cyan
    Write-Host "   • Configuración: $issFile" -ForegroundColor White
    Write-Host "   • Directorio de salida: $outputDir" -ForegroundColor White
    Write-Host ""

    # Compilar con Inno Setup
    Write-Host "🔨 Compilando..." -ForegroundColor Yellow
    & "$innoPath" "$issFile"

    if ($LASTEXITCODE -eq 0) {
        $stopwatch.Stop()
        
        Write-Host ""
        Write-Host "✅ INSTALADOR CREADO EXITOSAMENTE" -ForegroundColor Green -BackgroundColor DarkGreen
        Write-Host "===============================================" -ForegroundColor Green
        Write-Host ""

        # Buscar el archivo de salida
        $installerFile = Get-ChildItem $outputDir -Filter "GestionTimeDesktopSetup-*.exe" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        
        if ($installerFile) {
            Write-Host "📊 INFORMACIÓN DEL INSTALADOR:" -ForegroundColor Magenta
            Write-Host "   • Archivo: $($installerFile.Name)" -ForegroundColor White
            Write-Host "   • Tamaño: $([math]::Round($installerFile.Length/1MB, 2)) MB" -ForegroundColor White
            Write-Host "   • Ubicación: $($installerFile.FullName)" -ForegroundColor White
            Write-Host "   • Tiempo de compilación: $($stopwatch.Elapsed.TotalSeconds.ToString('F1')) segundos" -ForegroundColor White
            Write-Host ""

            # Información del contenido
            $appFiles = Get-ChildItem "bin\Release\Installer\App" -File -Recurse
            $totalSize = ($appFiles | Measure-Object -Property Length -Sum).Sum
            
            Write-Host "📦 CONTENIDO DEL INSTALADOR:" -ForegroundColor Cyan
            Write-Host "   ✅ Aplicación completa self-contained" -ForegroundColor Green
            Write-Host "   ✅ $($appFiles.Count) archivos incluidos" -ForegroundColor Green
            Write-Host "   ✅ Tamaño total descomprimido: $([math]::Round($totalSize/1MB, 2)) MB" -ForegroundColor Green
            Write-Host "   ✅ Runtime .NET 8 incluido" -ForegroundColor Green
            Write-Host "   ✅ WindowsAppSDK incluido" -ForegroundColor Green
            Write-Host "   ✅ Interfaz de instalación moderna" -ForegroundColor Green
            Write-Host "   ✅ Accesos directos automáticos" -ForegroundColor Green
            Write-Host "   ✅ Desinstalación desde Panel de Control" -ForegroundColor Green
            Write-Host ""

            Write-Host "🎯 CARACTERÍSTICAS DE LA INSTALACIÓN:" -ForegroundColor Yellow
            Write-Host "   • Instalación en Program Files" -ForegroundColor White
            Write-Host "   • Creación automática de accesos directos" -ForegroundColor White
            Write-Host "   • Registro en Panel de Control" -ForegroundColor White
            Write-Host "   • Soporte para actualizaciones" -ForegroundColor White
            Write-Host "   • Instalación silenciosa disponible" -ForegroundColor White
            Write-Host "   • Desinstalación limpia" -ForegroundColor White
            Write-Host ""

            Write-Host "📋 INSTRUCCIONES PARA DISTRIBUIR:" -ForegroundColor Blue
            Write-Host "   1. Distribuir el archivo: $($installerFile.Name)" -ForegroundColor White
            Write-Host "   2. Usuario ejecuta como administrador" -ForegroundColor White
            Write-Host "   3. Seguir asistente de instalación" -ForegroundColor White
            Write-Host "   4. Aplicación se ejecuta automáticamente" -ForegroundColor White
            Write-Host ""

            Write-Host "🔧 INSTALACIÓN SILENCIOSA:" -ForegroundColor Gray
            Write-Host "   `"$($installerFile.Name)`" /SILENT" -ForegroundColor DarkGray
            Write-Host ""

            if ($OpenOutput) {
                Write-Host "📂 Abriendo directorio de salida..." -ForegroundColor Green
                Start-Process "explorer.exe" -ArgumentList $outputDir
            } else {
                Write-Host "💡 Para abrir la carpeta del instalador:" -ForegroundColor Blue
                Write-Host "   .\create-inno-installer.ps1 -OpenOutput" -ForegroundColor White
            }

            Write-Host ""
            Write-Host "🎉 ¡INSTALADOR PROFESIONAL LISTO!" -ForegroundColor Green -BackgroundColor DarkGreen
            Write-Host "===============================================" -ForegroundColor Green

        } else {
            Write-Host "❌ ERROR: No se encontró el archivo de instalación generado" -ForegroundColor Red
            exit 1
        }

    } else {
        Write-Host "❌ ERROR: Falló la compilación con Inno Setup" -ForegroundColor Red
        exit 1
    }

} catch {
    Write-Host "❌ ERROR DURANTE LA CREACIÓN DEL INSTALADOR:" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor White
    exit 1
}