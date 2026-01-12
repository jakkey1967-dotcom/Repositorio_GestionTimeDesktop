param(
    [switch]$FixInstaller,
    [switch]$TestInstalled,
    [switch]$DebugMode
)

Write-Host ""
Write-Host "🔧 DIAGNOSTICANDO PROBLEMAS DE APLICACIÓN WINUI 3" -ForegroundColor Green -BackgroundColor DarkGreen
Write-Host "=================================================" -ForegroundColor Green
Write-Host ""

$ErrorActionPreference = "Continue"

function Test-InstalledApp {
    Write-Host "🔍 VERIFICANDO APLICACIÓN INSTALADA:" -ForegroundColor Cyan
    
    $possiblePaths = @(
        "C:\App\GestionTime Desktop\GestionTime.Desktop.exe",
        "C:\Program Files\GestionTime Desktop\GestionTime.Desktop.exe",
        "C:\Program Files (x86)\GestionTime Desktop\GestionTime.Desktop.exe"
    )
    
    $installedPath = $null
    foreach ($path in $possiblePaths) {
        if (Test-Path $path) {
            $installedPath = $path
            Write-Host "   ✅ Aplicación encontrada en: $path" -ForegroundColor Green
            break
        }
    }
    
    if (-not $installedPath) {
        Write-Host "   ❌ Aplicación no encontrada en ubicaciones esperadas" -ForegroundColor Red
        return $false
    }
    
    # Verificar archivos críticos
    $appDir = Split-Path $installedPath
    $criticalFiles = @(
        "GestionTime.Desktop.exe",
        "GestionTime.Desktop.dll",
        "Microsoft.WindowsAppRuntime.dll",
        "appsettings.json"
    )
    
    Write-Host "   📋 Verificando archivos críticos:" -ForegroundColor Yellow
    $allCriticalFound = $true
    foreach ($file in $criticalFiles) {
        $filePath = Join-Path $appDir $file
        if (Test-Path $filePath) {
            Write-Host "      ✅ $file" -ForegroundColor Green
        } else {
            Write-Host "      ❌ $file (FALTANTE)" -ForegroundColor Red
            $allCriticalFound = $false
        }
    }
    
    # Verificar recursos WinUI
    $resourceFiles = Get-ChildItem $appDir -Filter "*.pri" -ErrorAction SilentlyContinue
    Write-Host "   🎨 Recursos WinUI (.pri): $($resourceFiles.Count)" -ForegroundColor Yellow
    
    if ($resourceFiles.Count -eq 0) {
        Write-Host "      ⚠️  Sin recursos .pri - problema con WinUI" -ForegroundColor Yellow
    }
    
    # Intentar ejecutar y ver qué pasa
    if ($allCriticalFound) {
        Write-Host ""
        Write-Host "   🚀 Intentando ejecutar aplicación..." -ForegroundColor Cyan
        
        try {
            $process = Start-Process $installedPath -WindowStyle Hidden -PassThru -ErrorAction Stop
            Start-Sleep 3
            
            if (-not $process.HasExited) {
                Write-Host "      ✅ Aplicación arrancó correctamente" -ForegroundColor Green
                $process.CloseMainWindow()
                return $true
            } else {
                Write-Host "      ❌ Aplicación terminó inmediatamente (código: $($process.ExitCode))" -ForegroundColor Red
                return $false
            }
        } catch {
            Write-Host "      ❌ Error al ejecutar: $($_.Exception.Message)" -ForegroundColor Red
            return $false
        }
    }
    
    return $false
}

function Get-WindowsAppSDKStatus {
    Write-Host ""
    Write-Host "🔍 VERIFICANDO WINDOWS APP SDK:" -ForegroundColor Cyan
    
    # Verificar WindowsAppRuntime instalado
    $runtimePaths = @(
        "${env:ProgramFiles}\WindowsApps\*Microsoft.WindowsAppRuntime*",
        "${env:LOCALAPPDATA}\Microsoft\WindowsApps\*Microsoft.WindowsAppRuntime*"
    )
    
    $found = $false
    foreach ($pattern in $runtimePaths) {
        $items = Get-ChildItem $pattern -ErrorAction SilentlyContinue
        if ($items) {
            Write-Host "   ✅ WindowsAppRuntime encontrado: $($items.Count) versiones" -ForegroundColor Green
            $found = $true
            foreach ($item in $items | Select-Object -First 3) {
                Write-Host "      - $($item.Name)" -ForegroundColor Gray
            }
        }
    }
    
    if (-not $found) {
        Write-Host "   ❌ WindowsAppRuntime no encontrado en el sistema" -ForegroundColor Red
        Write-Host "   💡 La aplicación debe ser self-contained o instalar el runtime" -ForegroundColor Yellow
    }
}

function Fix-BuildConfiguration {
    Write-Host ""
    Write-Host "🔧 CORRIGIENDO CONFIGURACIÓN DE BUILD:" -ForegroundColor Cyan
    
    # Actualizar build-for-installer.ps1
    $buildScript = "build-for-installer.ps1"
    if (Test-Path $buildScript) {
        $content = Get-Content $buildScript -Raw
        
        # Verificar parámetros críticos
        $requiredParams = @(
            "--self-contained true",
            "-p:WindowsAppSDKSelfContained=true",
            "-p:UseWinUI=true",
            "-p:PublishTrimmed=false"
        )
        
        $needsUpdate = $false
        foreach ($param in $requiredParams) {
            if ($content -notlike "*$param*") {
                Write-Host "   ⚠️  Parámetro faltante: $param" -ForegroundColor Yellow
                $needsUpdate = $true
            }
        }
        
        if ($needsUpdate) {
            Write-Host "   🔨 Actualizando configuración de build..." -ForegroundColor Yellow
            
            # Crear versión corregida del script
            $fixedContent = @'
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
'@
            
            $fixedContent | Out-File $buildScript -Encoding UTF8
            Write-Host "   ✅ Script de build actualizado" -ForegroundColor Green
        } else {
            Write-Host "   ✅ Configuración de build es correcta" -ForegroundColor Green
        }
    }
}

function Show-Solutions {
    Write-Host ""
    Write-Host "💡 SOLUCIONES RECOMENDADAS:" -ForegroundColor Blue
    Write-Host "=========================" -ForegroundColor Blue
    Write-Host ""
    
    Write-Host "1️⃣ RECONSTRUIR CON CONFIGURACIÓN CORREGIDA:" -ForegroundColor Yellow
    Write-Host "   .\diagnose-winui-app.ps1 -FixInstaller" -ForegroundColor White
    Write-Host "   .\build-for-installer.ps1 -Clean" -ForegroundColor White
    Write-Host "   .\create-improved-selfextracting-installer.ps1 -Rebuild" -ForegroundColor White
    Write-Host ""
    
    Write-Host "2️⃣ VERIFICAR INSTALACIÓN:" -ForegroundColor Yellow
    Write-Host "   .\diagnose-winui-app.ps1 -TestInstalled" -ForegroundColor White
    Write-Host ""
    
    Write-Host "3️⃣ PROBLEMAS COMUNES WINUI 3:" -ForegroundColor Yellow
    Write-Host "   • Trimming debe estar DESACTIVADO" -ForegroundColor White
    Write-Host "   • WindowsAppSDK debe ser self-contained" -ForegroundColor White
    Write-Host "   • Recursos .pri deben incluirse" -ForegroundColor White
    Write-Host "   • No usar PublishSingleFile con WinUI 3" -ForegroundColor White
    Write-Host ""
    
    Write-Host "4️⃣ INSTALACIÓN MANUAL PARA PRUEBAS:" -ForegroundColor Yellow
    Write-Host "   1. Copiar manualmente bin\Release\Installer\App\*" -ForegroundColor White
    Write-Host "   2. A C:\App\GestionTime Desktop\" -ForegroundColor White
    Write-Host "   3. Ejecutar GestionTime.Desktop.exe directamente" -ForegroundColor White
}

# Ejecutar diagnósticos
if ($TestInstalled) {
    Test-InstalledApp
    exit
}

if ($FixInstaller) {
    Fix-BuildConfiguration
    Write-Host ""
    Write-Host "✅ CONFIGURACIÓN CORREGIDA" -ForegroundColor Green
    Write-Host "Ejecutar ahora: .\build-for-installer.ps1 -Clean" -ForegroundColor Yellow
    exit
}

# Diagnóstico completo
Write-Host "🎯 EJECUTANDO DIAGNÓSTICO COMPLETO..." -ForegroundColor Magenta
Write-Host ""

Test-InstalledApp
Get-WindowsAppSDKStatus
Show-Solutions

Write-Host ""
Write-Host "🔧 DIAGNÓSTICO COMPLETADO" -ForegroundColor Green -BackgroundColor DarkGreen
Write-Host "========================" -ForegroundColor Green