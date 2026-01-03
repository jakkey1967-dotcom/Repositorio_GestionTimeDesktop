param(
    [string]$Configuration = "Release",
    [string]$OutputPath = "bin\Release\Installer",
    [switch]$Clean
)

Write-Host ""
Write-Host "🏗️  PREPARANDO BUILD PARA INSTALADOR MSI" -ForegroundColor Green -BackgroundColor DarkGreen
Write-Host "===============================================" -ForegroundColor Green
Write-Host ""

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

try {
    if ($Clean) {
        Write-Host "🧹 Limpiando proyecto..." -ForegroundColor Cyan
        dotnet clean --configuration $Configuration | Out-Host
    }

    # Crear directorio de salida para instalador
    if (!(Test-Path $OutputPath)) {
        New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
        Write-Host "📁 Directorio de instalador creado: $OutputPath" -ForegroundColor Yellow
    }

    Write-Host "📋 Publicando aplicación para instalador..." -ForegroundColor Cyan
    Write-Host "   • Configuración: $Configuration" -ForegroundColor White
    Write-Host "   • Runtime: win-x64" -ForegroundColor White
    Write-Host "   • Self-contained: true" -ForegroundColor White
    Write-Host "   • WindowsAppSDK: Self-contained" -ForegroundColor White
    Write-Host ""

    # Publicar con configuración optimizada para instalador
    $publishPath = "$OutputPath\App"
    dotnet publish -c $Configuration -r win-x64 --self-contained true -p:Platform=x64 -p:WindowsAppSDKSelfContained=true -o $publishPath

    if (Test-Path "$publishPath\GestionTime.Desktop.exe") {
        $stopwatch.Stop()
        
        Write-Host ""
        Write-Host "✅ BUILD PARA INSTALADOR COMPLETADO" -ForegroundColor Green -BackgroundColor DarkGreen
        Write-Host "===============================================" -ForegroundColor Green
        Write-Host ""
        
        # Estadísticas
        $files = Get-ChildItem $publishPath -File -Recurse
        $totalSize = ($files | Measure-Object -Property Length -Sum).Sum
        $exeFile = $files | Where-Object { $_.Name -eq 'GestionTime.Desktop.exe' }
        
        Write-Host "📊 ESTADÍSTICAS DEL BUILD:" -ForegroundColor Magenta
        Write-Host "   • Archivos totales: $($files.Count)" -ForegroundColor White
        Write-Host "   • Tamaño total: $([math]::Round($totalSize/1MB, 2)) MB" -ForegroundColor White
        Write-Host "   • Ejecutable: $($exeFile.Name) ($([math]::Round($exeFile.Length/1KB, 2)) KB)" -ForegroundColor White
        Write-Host "   • Tiempo de build: $($stopwatch.Elapsed.TotalSeconds.ToString('F1')) segundos" -ForegroundColor White
        Write-Host "   • Directorio: $publishPath" -ForegroundColor White
        Write-Host ""
        
        # Verificar archivos clave
        $requiredFiles = @(
            "GestionTime.Desktop.exe",
            "GestionTime.Desktop.dll",
            "Microsoft.WindowsAppRuntime.dll",
            "appsettings.json"
        )
        
        Write-Host "🔍 VERIFICACIÓN DE ARCHIVOS CLAVE:" -ForegroundColor Yellow
        foreach ($file in $requiredFiles) {
            if (Test-Path (Join-Path $publishPath $file)) {
                Write-Host "   ✅ $file" -ForegroundColor Green
            } else {
                Write-Host "   ❌ $file (FALTANTE)" -ForegroundColor Red
            }
        }
        Write-Host ""
        
        # Crear estructura para instalador
        Write-Host "📦 PREPARANDO ESTRUCTURA PARA INSTALADOR MSI:" -ForegroundColor Cyan
        
        # Copiar assets importantes a nivel superior
        $assetsPath = Join-Path $OutputPath "Assets"
        if (!(Test-Path $assetsPath)) {
            New-Item -ItemType Directory -Path $assetsPath -Force | Out-Null
        }
        
        # Copiar icono para el instalador
        if (Test-Path "Assets\app_logo.ico") {
            Copy-Item "Assets\app_logo.ico" $assetsPath -Force
            Write-Host "   ✅ Icono copiado para instalador" -ForegroundColor Green
        }
        
        # Crear archivo de información del producto
        $productInfo = @"
[Product Information]
ProductName=GestionTime Desktop
ProductVersion=1.1.0.0
Company=GestionTime Solutions
Description=Aplicación de gestión de tiempo para partes de trabajo
Copyright=© 2025 GestionTime Solutions
ExecutableName=GestionTime.Desktop.exe
InstallPath=GestionTime Desktop
ProgramFilesFolder=GestionTime Desktop
StartMenuFolder=GestionTime Desktop

[System Requirements]
MinWindowsVersion=10.0.17763.0
TargetFramework=net8.0-windows10.0.19041.0
Architecture=x64
RequiredMemoryMB=512
RequiredDiskSpaceMB=300

[Installation]
CreateDesktopShortcut=true
CreateStartMenuShortcut=true
AddToPath=false
RunAfterInstall=true

[Files]
MainExecutable=$publishPath\GestionTime.Desktop.exe
ApplicationFiles=$publishPath\*
TotalSizeMB=$([math]::Round($totalSize/1MB, 2))
FileCount=$($files.Count)
"@
        
        $productInfoPath = Join-Path $OutputPath "ProductInfo.ini"
        $productInfo | Out-File -FilePath $productInfoPath -Encoding UTF8
        Write-Host "   ✅ ProductInfo.ini creado" -ForegroundColor Green
        
        Write-Host ""
        Write-Host "📋 SIGUIENTES PASOS PARA CREAR MSI:" -ForegroundColor Cyan
        Write-Host "   1. Instalar 'Microsoft Visual Studio Installer Projects' en VS" -ForegroundColor White
        Write-Host "   2. Agregar nuevo proyecto 'Setup Project' a la solución" -ForegroundColor White
        Write-Host "   3. Configurar con los archivos en: $publishPath" -ForegroundColor White
        Write-Host "   4. Usar ProductInfo.ini para configuración" -ForegroundColor White
        Write-Host ""
        
        Write-Host "🛠️  ALTERNATIVA - WiX TOOLSET:" -ForegroundColor Blue
        Write-Host "   Para instalador más avanzado, usar:" -ForegroundColor White
        Write-Host "   winget install WiX.Toolset" -ForegroundColor Gray
        Write-Host ""
        
        Write-Host "📂 Archivos listos en: $OutputPath" -ForegroundColor Green
        Write-Host ""
        
    } else {
        Write-Host "❌ ERROR: No se encontró el ejecutable principal" -ForegroundColor Red
        exit 1
    }
    
} catch {
    Write-Host "❌ ERROR DURANTE EL BUILD:" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor White
    exit 1
}

Write-Host "🎉 ¡BUILD PARA INSTALADOR COMPLETADO!" -ForegroundColor Green -BackgroundColor DarkGreen
Write-Host "===============================================" -ForegroundColor Green