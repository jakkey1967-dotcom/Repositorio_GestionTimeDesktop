param(
    [string]$Configuration = "Release",
    [string]$OutputPath = "bin\Release\Installer",
    [switch]$Clean
)

Write-Host ""
Write-Host "🏗️ PREPARANDO BUILD FINAL CORREGIDO PARA WINUI 3" -ForegroundColor Green -BackgroundColor DarkGreen
Write-Host "================================================" -ForegroundColor Green
Write-Host ""

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

try {
    if ($Clean) {
        Write-Host "🧹 Limpiando proyecto..." -ForegroundColor Cyan
        dotnet clean --configuration $Configuration | Out-Host
    }

    # Crear directorio de salida
    if (!(Test-Path $OutputPath)) {
        New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
    }

    Write-Host "📋 CONFIGURACIÓN FINAL CORREGIDA PARA WINUI 3..." -ForegroundColor Cyan
    Write-Host "   • Configuración: $Configuration" -ForegroundColor White
    Write-Host "   • Runtime: win-x64" -ForegroundColor White  
    Write-Host "   • Self-contained: true" -ForegroundColor White
    Write-Host "   • CORRECCIÓN: Sin trimming, con manifest correcto" -ForegroundColor Yellow
    Write-Host ""

    # Build FINAL CORREGIDO
    $publishPath = "$OutputPath\App"
    
    Write-Host "🔨 Ejecutando build final corregido..." -ForegroundColor Yellow
    
    dotnet publish -c $Configuration -r win-x64 --self-contained true `
        -p:Platform=x64 `
        -p:WindowsAppSDKSelfContained=true `
        -p:UseWinUI=true `
        -p:EnableMsixTooling=false `
        -p:GeneratePackageOnBuild=false `
        -p:AppxPackage=false `
        -p:PublishSingleFile=false `
        -p:PublishTrimmed=false `
        -p:TrimMode=partial `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:IncludeAllContentForSelfExtract=true `
        -p:PublishReadyToRun=false `
        -p:RuntimeIdentifier=win-x64 `
        -p:DebugType=portable `
        -p:DebugSymbols=true `
        -o $publishPath

    if (Test-Path "$publishPath\GestionTime.Desktop.exe") {
        Write-Host ""
        Write-Host "✅ BUILD CORREGIDO COMPLETADO" -ForegroundColor Green
        
        # Copiar manifest corregido si existe
        if (Test-Path "app-fixed.manifest") {
            Copy-Item "app-fixed.manifest" "$publishPath\GestionTime.Desktop.exe.manifest" -Force
            Write-Host "   ✅ Manifest corregido aplicado" -ForegroundColor Green
        }
        
        # Verificar archivos críticos
        $files = Get-ChildItem $publishPath -File -Recurse
        $criticalFiles = @("GestionTime.Desktop.exe", "GestionTime.Desktop.dll", "Microsoft.WindowsAppRuntime.dll", "appsettings.json")
        
        Write-Host ""
        Write-Host "🔍 VERIFICACIÓN FINAL:" -ForegroundColor Cyan
        foreach ($file in $criticalFiles) {
            if (Test-Path (Join-Path $publishPath $file)) {
                Write-Host "   ✅ $file" -ForegroundColor Green
            } else {
                Write-Host "   ❌ $file (FALTANTE)" -ForegroundColor Red
            }
        }
        
        $stopwatch.Stop()
        Write-Host ""
        Write-Host "📊 ESTADÍSTICAS:" -ForegroundColor Magenta
        Write-Host "   • Archivos: $($files.Count)" -ForegroundColor White
        Write-Host "   • Tiempo: $($stopwatch.Elapsed.TotalSeconds.ToString('F1'))s" -ForegroundColor White
        Write-Host "   • Tamaño: $([math]::Round(($files | Measure-Object Length -Sum).Sum/1MB, 2)) MB" -ForegroundColor White
        
        Write-Host ""
        Write-Host "🎯 BUILD CORREGIDO LISTO - DEBERÍA FUNCIONAR AHORA" -ForegroundColor Green -BackgroundColor DarkGreen
        
    } else {
        Write-Host "❌ ERROR: Build falló" -ForegroundColor Red
        exit 1
    }
    
} catch {
    Write-Host "❌ ERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "🎉 BUILD FINAL CORREGIDO COMPLETADO" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green