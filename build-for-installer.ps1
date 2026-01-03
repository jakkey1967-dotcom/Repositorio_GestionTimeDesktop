param(
    [string]$Configuration = "Release",
    [string]$OutputPath = "bin\Release\Installer",
    [switch]$Clean
)

Write-Host ""
Write-Host "🏗️  PREPARANDO BUILD PARA INSTALADOR (CORREGIDO WINUI 3)" -ForegroundColor Green -BackgroundColor DarkGreen
Write-Host "=======================================================" -ForegroundColor Green
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

    Write-Host "📋 Publicando aplicación WinUI 3 (CONFIGURACIÓN CORREGIDA)..." -ForegroundColor Cyan
    Write-Host "   • Configuración: $Configuration" -ForegroundColor White
    Write-Host "   • Runtime: win-x64" -ForegroundColor White
    Write-Host "   • Self-contained: true" -ForegroundColor White
    Write-Host "   • WindowsAppSDK: Self-contained" -ForegroundColor White
    Write-Host "   • Trimming: DESACTIVADO (requerido para WinUI 3)" -ForegroundColor White
    Write-Host ""

    # Publicar con configuración ESPECÍFICA para WinUI 3
    $publishPath = "$OutputPath\App"
    
    # CONFIGURACIÓN CRÍTICA para WinUI 3 - NO TRIMMING
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
        -p:PublishReadyToRun=true `
        -p:RuntimeIdentifier=win-x64 `
        -o $publishPath

    if (Test-Path "$publishPath\GestionTime.Desktop.exe") {
        $stopwatch.Stop()
        
        Write-Host ""
        Write-Host "✅ BUILD CORREGIDO PARA WINUI 3 COMPLETADO" -ForegroundColor Green -BackgroundColor DarkGreen
        Write-Host "=========================================" -ForegroundColor Green
        Write-Host ""
        
        # Verificar recursos críticos
        $files = Get-ChildItem $publishPath -File -Recurse
        $totalSize = ($files | Measure-Object -Property Length -Sum).Sum
        $exeFile = $files | Where-Object { $_.Name -eq 'GestionTime.Desktop.exe' }
        $runtimeFiles = $files | Where-Object { $_.Name -like '*WindowsApp*' }
        $resourceFiles = $files | Where-Object { $_.Extension -eq '.pri' }
        
        Write-Host "📊 ESTADÍSTICAS DEL BUILD CORREGIDO:" -ForegroundColor Magenta
        Write-Host "   • Archivos totales: $($files.Count)" -ForegroundColor White
        Write-Host "   • Tamaño total: $([math]::Round($totalSize/1MB, 2)) MB" -ForegroundColor White
        Write-Host "   • Ejecutable: $($exeFile.Name) ($([math]::Round($exeFile.Length/1KB, 2)) KB)" -ForegroundColor White
        Write-Host "   • WindowsAppSDK files: $($runtimeFiles.Count)" -ForegroundColor White
        Write-Host "   • Recursos WinUI (.pri): $($resourceFiles.Count)" -ForegroundColor White
        Write-Host "   • Tiempo de build: $($stopwatch.Elapsed.TotalSeconds.ToString('F1')) segundos" -ForegroundColor White
        Write-Host ""
        
        # Verificar archivos críticos específicos
        $criticalFiles = @(
            "GestionTime.Desktop.exe",
            "GestionTime.Desktop.dll", 
            "Microsoft.WindowsAppRuntime.dll",
            "appsettings.json"
        )
        
        Write-Host "🔍 VERIFICACIÓN DE ARCHIVOS CRÍTICOS:" -ForegroundColor Yellow
        foreach ($file in $criticalFiles) {
            if (Test-Path (Join-Path $publishPath $file)) {
                Write-Host "   ✅ $file" -ForegroundColor Green
            } else {
                Write-Host "   ❌ $file (CRÍTICO FALTANTE)" -ForegroundColor Red
            }
        }
        
        if ($resourceFiles.Count -gt 0) {
            Write-Host ""
            Write-Host "✅ BUILD CORREGIDO - WINUI 3 CONFIGURADO CORRECTAMENTE" -ForegroundColor Green -BackgroundColor DarkGreen
        } else {
            Write-Host ""
            Write-Host "⚠️  ADVERTENCIA - RECURSOS WINUI PUEDEN FALTAR" -ForegroundColor Yellow -BackgroundColor DarkYellow
        }
        
    } else {
        Write-Host "❌ ERROR: No se encontró el ejecutable principal después del build" -ForegroundColor Red
        exit 1
    }
    
} catch {
    Write-Host "❌ ERROR DURANTE EL BUILD CORREGIDO:" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor White
    exit 1
}

Write-Host ""
Write-Host "🎉 BUILD CORREGIDO PARA WINUI 3 COMPLETADO!" -ForegroundColor Green -BackgroundColor DarkGreen
Write-Host "============================================" -ForegroundColor Green
