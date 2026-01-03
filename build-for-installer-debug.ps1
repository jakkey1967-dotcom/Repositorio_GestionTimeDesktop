param(
    [string]$Configuration = "Debug",
    [string]$OutputPath = "bin\Debug\Installer",
    [switch]$Clean,
    [switch]$Force
)

Write-Host ""
Write-Host "🏗️  BUILD PARA INSTALADOR - CONFIGURACIÓN DEBUG" -ForegroundColor Green -BackgroundColor DarkGreen
Write-Host "===============================================" -ForegroundColor Green
Write-Host ""

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

try {
    if ($Clean) {
        Write-Host "🧹 Limpiando proyecto..." -ForegroundColor Cyan
        dotnet clean --configuration $Configuration | Out-Host
        Remove-Item "bin\Debug" -Recurse -Force -ErrorAction SilentlyContinue
    }

    # Crear directorio de salida para instalador
    if (!(Test-Path $OutputPath)) {
        New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
        Write-Host "📁 Directorio de instalador creado: $OutputPath" -ForegroundColor Yellow
    }

    Write-Host "📋 Publicando aplicación para instalador - DEBUG..." -ForegroundColor Cyan
    Write-Host "   • Configuración: $Configuration" -ForegroundColor White
    Write-Host "   • Runtime: win-x64" -ForegroundColor White
    Write-Host "   • Self-contained: true" -ForegroundColor White
    Write-Host "   • WindowsAppSDK: Self-contained" -ForegroundColor White
    Write-Host "   • Debug completo: true" -ForegroundColor White
    Write-Host ""

    # Publicar con configuración DEBUG completa
    $publishPath = "$OutputPath\App"
    
    # CONFIGURACIÓN DEBUG COMPLETA
    dotnet publish -c $Configuration -r win-x64 --self-contained true `
        -p:Platform=x64 `
        -p:WindowsAppSDKSelfContained=true `
        -p:UseWinUI=true `
        -p:EnableMsixTooling=false `
        -p:GeneratePackageOnBuild=false `
        -p:AppxPackage=false `
        -p:PublishSingleFile=false `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:IncludeAllContentForSelfExtract=true `
        -p:DebugType=full `
        -p:DebugSymbols=true `
        -p:Optimize=false `
        -o $publishPath

    if (Test-Path "$publishPath\GestionTime.Desktop.exe") {
        $stopwatch.Stop()
        
        Write-Host ""
        Write-Host "✅ BUILD DEBUG PARA INSTALADOR COMPLETADO" -ForegroundColor Green -BackgroundColor DarkGreen
        Write-Host "===============================================" -ForegroundColor Green
        Write-Host ""
        
        # Estadísticas
        $files = Get-ChildItem $publishPath -File -Recurse
        $totalSize = ($files | Measure-Object -Property Length -Sum).Sum
        $exeFile = $files | Where-Object { $_.Name -eq 'GestionTime.Desktop.exe' }
        
        Write-Host "📊 ESTADÍSTICAS DEL BUILD DEBUG:" -ForegroundColor Magenta
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
            "GestionTime.Desktop.pdb",
            "Microsoft.WindowsAppRuntime.dll",
            "appsettings.json"
        )
        
        Write-Host "🔍 VERIFICACIÓN DE ARCHIVOS CLAVE (DEBUG):" -ForegroundColor Yellow
        foreach ($file in $requiredFiles) {
            if (Test-Path (Join-Path $publishPath $file)) {
                $fileInfo = Get-Item (Join-Path $publishPath $file)
                Write-Host "   ✅ $file ($([math]::Round($fileInfo.Length/1KB, 1)) KB)" -ForegroundColor Green
            } else {
                Write-Host "   ❌ $file (FALTANTE)" -ForegroundColor Red
            }
        }
        
        # Verificar archivos de debugging
        $debugFiles = Get-ChildItem $publishPath -Filter "*.pdb" -ErrorAction SilentlyContinue
        Write-Host ""
        Write-Host "🔧 ARCHIVOS DE DEBUG:" -ForegroundColor Cyan
        Write-Host "   • Archivos .pdb encontrados: $($debugFiles.Count)" -ForegroundColor White
        if ($debugFiles.Count -gt 0) {
            Write-Host "   ✅ Símbolos de debugging incluidos" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️  Sin archivos de debugging" -ForegroundColor Yellow
        }
        
        # Verificar archivos de recursos WinUI
        $resourceFiles = Get-ChildItem $publishPath -Filter "*.pri" -ErrorAction SilentlyContinue
        $xamlFiles = Get-ChildItem $publishPath -Filter "*.xbf" -ErrorAction SilentlyContinue -Recurse
        
        Write-Host ""
        Write-Host "🎨 VERIFICACIÓN DE RECURSOS WinUI:" -ForegroundColor Cyan
        Write-Host "   • Archivos .pri (Package Resource Index): $($resourceFiles.Count)" -ForegroundColor White
        Write-Host "   • Archivos .xbf (XAML Binary Format): $($xamlFiles.Count)" -ForegroundColor White
        
        if ($resourceFiles.Count -gt 0) {
            Write-Host "   ✅ Recursos WinUI encontrados" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️  Sin archivos .pri - posible problema XAML" -ForegroundColor Yellow
        }
        
        # Crear archivo de información del producto DEBUG
        $productInfo = @"
[Product Information - DEBUG]
ProductName=GestionTime Desktop (Debug)
ProductVersion=1.1.0.0
Configuration=Debug
Company=GestionTime Solutions
Description=Aplicación de gestión de tiempo para partes de trabajo (DEBUG)
Copyright=© 2025 GestionTime Solutions
ExecutableName=GestionTime.Desktop.exe
InstallPath=GestionTime Desktop Debug
ProgramFilesFolder=GestionTime Desktop Debug
StartMenuFolder=GestionTime Desktop Debug

[System Requirements]
MinWindowsVersion=10.0.17763.0
TargetFramework=net8.0-windows10.0.19041.0
Architecture=x64
RequiredMemoryMB=512
RequiredDiskSpaceMB=400

[Installation]
CreateDesktopShortcut=true
CreateStartMenuShortcut=true
AddToPath=false
RunAfterInstall=true

[Files - DEBUG]
MainExecutable=$publishPath\GestionTime.Desktop.exe
ApplicationFiles=$publishPath\*
TotalSizeMB=$([math]::Round($totalSize/1MB, 2))
FileCount=$($files.Count)
ResourceFiles=$($resourceFiles.Count)
XamlFiles=$($xamlFiles.Count)
DebugFiles=$($debugFiles.Count)

[Build Configuration - DEBUG]
UseWinUI=true
EnableMsixTooling=false
GeneratePackageOnBuild=false
AppxPackage=false
PublishSingleFile=false
DebugType=full
DebugSymbols=true
Optimize=false
Configuration=Debug
"@
        
        $productInfoPath = Join-Path $OutputPath "ProductInfo_Debug.ini"
        $productInfo | Out-File -FilePath $productInfoPath -Encoding UTF8
        Write-Host "   ✅ ProductInfo_Debug.ini creado" -ForegroundColor Green
        
        Write-Host ""
        Write-Host "📂 Archivos listos en: $OutputPath" -ForegroundColor Green
        Write-Host ""
        
        # Comparación con Release
        $releaseAppPath = "bin\Release\Installer\App"
        if (Test-Path $releaseAppPath) {
            $releaseFiles = Get-ChildItem $releaseAppPath -File -Recurse
            Write-Host "📊 COMPARACIÓN DEBUG vs RELEASE:" -ForegroundColor Blue
            Write-Host "   • DEBUG:   $($files.Count) archivos, $([math]::Round($totalSize/1MB, 2)) MB" -ForegroundColor White
            Write-Host "   • RELEASE: $($releaseFiles.Count) archivos" -ForegroundColor White
            
            $sizeDiff = $totalSize - ($releaseFiles | Measure-Object -Property Length -Sum).Sum
            if ($sizeDiff -gt 0) {
                Write-Host "   • DEBUG tiene $([math]::Round($sizeDiff/1MB, 2)) MB más (símbolos de debug)" -ForegroundColor Yellow
            }
        }
        
    } else {
        Write-Host "❌ ERROR: No se encontró el ejecutable principal" -ForegroundColor Red
        exit 1
    }
    
} catch {
    Write-Host "❌ ERROR DURANTE EL BUILD:" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor White
    exit 1
}

Write-Host "🎉 ¡BUILD DEBUG PARA INSTALADOR COMPLETADO!" -ForegroundColor Green -BackgroundColor DarkGreen
Write-Host "===============================================" -ForegroundColor Green