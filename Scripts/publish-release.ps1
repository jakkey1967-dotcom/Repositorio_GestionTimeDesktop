param(
    [ValidateSet("SelfContained", "FrameworkDependent", "MSIX")]
    [string]$Type = "SelfContained",
    
    [switch]$OpenOutput
)

Write-Host ""
Write-Host "🚀 PUBLICANDO GESTIONTIME DESKTOP" -ForegroundColor Green -BackgroundColor DarkGreen
Write-Host "===============================================" -ForegroundColor Green
Write-Host ""

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

try {
    switch ($Type) {
        "SelfContained" {
            Write-Host "📦 MODO: Self-Contained" -ForegroundColor Yellow
            Write-Host "   • Tamaño: ~254 MB" -ForegroundColor White
            Write-Host "   • Dependencias: Ninguna" -ForegroundColor Green
            Write-Host "   • Compatibilidad: Máxima" -ForegroundColor Green
            Write-Host ""
            
            Write-Host "🧹 Limpiando proyecto..." -ForegroundColor Cyan
            dotnet clean --configuration Release | Out-Host
            
            Write-Host "📋 Publicando..." -ForegroundColor Cyan
            dotnet publish -c Release -r win-x64 --self-contained true -p:Platform=x64 -p:WindowsAppSDKSelfContained=true
            
            $outputDir = "bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\publish"
        }
        
        "FrameworkDependent" {
            Write-Host "📦 MODO: Framework-Dependent" -ForegroundColor Yellow
            Write-Host "   • Tamaño: ~5 MB" -ForegroundColor Green  
            Write-Host "   • Dependencias: Windows App Runtime requerido" -ForegroundColor Red
            Write-Host "   • Compatibilidad: Requiere instalación previa" -ForegroundColor Yellow
            Write-Host ""
            
            Write-Host "🧹 Limpiando proyecto..." -ForegroundColor Cyan
            dotnet clean --configuration Release | Out-Host
            
            Write-Host "📋 Publicando..." -ForegroundColor Cyan
            dotnet publish -c Release -r win-x64 --self-contained false -p:Platform=x64
            
            $outputDir = "bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\publish"
        }
        
        "MSIX" {
            Write-Host "📦 MODO: MSIX Package" -ForegroundColor Yellow
            Write-Host "   • Tamaño: ~50 MB" -ForegroundColor Yellow
            Write-Host "   • Dependencias: Auto-instaladas" -ForegroundColor Green
            Write-Host "   • Compatibilidad: Professional" -ForegroundColor Green
            Write-Host ""
            Write-Host "ℹ️  Para crear MSIX:" -ForegroundColor Blue
            Write-Host "   1. Abrir Visual Studio" -ForegroundColor White
            Write-Host "   2. Click derecho en proyecto → Publish → Create App Packages" -ForegroundColor White
            Write-Host "   3. Seleccionar 'Sideloading'" -ForegroundColor White
            Write-Host "   4. Marcar x64 y Create" -ForegroundColor White
            Write-Host ""
            return
        }
    }
    
    if (Test-Path $outputDir) {
        $stopwatch.Stop()
        
        Write-Host ""
        Write-Host "✅ PUBLICACIÓN COMPLETADA" -ForegroundColor Green -BackgroundColor DarkGreen
        Write-Host "===============================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "📁 Directorio de salida:" -ForegroundColor Cyan
        Write-Host "   $outputDir" -ForegroundColor White
        Write-Host ""
        
        # Estadísticas de archivos
        $files = Get-ChildItem $outputDir -File
        $totalSize = ($files | Measure-Object -Property Length -Sum).Sum
        $exeFile = $files | Where-Object { $_.Extension -eq '.exe' -and $_.Name -like 'GestionTime*' }
        
        Write-Host "📊 ESTADÍSTICAS:" -ForegroundColor Magenta
        Write-Host "   • Archivos totales: $($files.Count)" -ForegroundColor White
        Write-Host "   • Tamaño total: $([math]::Round($totalSize/1MB, 2)) MB" -ForegroundColor White
        Write-Host "   • Ejecutable principal: $($exeFile.Name) ($([math]::Round($exeFile.Length/1KB, 2)) KB)" -ForegroundColor White
        Write-Host "   • Tiempo de compilación: $($stopwatch.Elapsed.TotalSeconds.ToString('F1')) segundos" -ForegroundColor White
        Write-Host ""
        
        # Verificación de archivos clave
        $requiredFiles = @("GestionTime.Desktop.exe", "GestionTime.Desktop.dll")
        Write-Host "🔍 VERIFICACIÓN:" -ForegroundColor Yellow
        foreach ($file in $requiredFiles) {
            if (Test-Path (Join-Path $outputDir $file)) {
                Write-Host "   ✅ $file" -ForegroundColor Green
            } else {
                Write-Host "   ❌ $file (FALTANTE)" -ForegroundColor Red
            }
        }
        Write-Host ""
        
        # Instrucciones de distribución
        if ($Type -eq "SelfContained") {
            Write-Host "📦 INSTRUCCIONES DE DISTRIBUCIÓN:" -ForegroundColor Cyan
            Write-Host "   1. Comprime toda la carpeta $outputDir" -ForegroundColor White
            Write-Host "   2. Envía el ZIP al usuario final" -ForegroundColor White
            Write-Host "   3. Usuario extrae y ejecuta GestionTime.Desktop.exe" -ForegroundColor White
            Write-Host "   4. No requiere instalaciones adicionales ✅" -ForegroundColor Green
        } else {
            Write-Host "⚠️  REQUISITOS PARA EL USUARIO FINAL:" -ForegroundColor Red
            Write-Host "   1. Windows 10 versión 1809 o superior" -ForegroundColor White
            Write-Host "   2. Windows App Runtime instalado" -ForegroundColor White
            Write-Host "   3. .NET 8 Desktop Runtime instalado" -ForegroundColor White
            Write-Host ""
            Write-Host "💡 Descarga Windows App Runtime en:" -ForegroundColor Blue
            Write-Host "   https://aka.ms/windowsappsdk/1.8/stable-vsix-2024-10-24-c1" -ForegroundColor White
        }
        
        Write-Host ""
        
        if ($OpenOutput) {
            Write-Host "📂 Abriendo directorio de salida..." -ForegroundColor Green
            Start-Process "explorer.exe" -ArgumentList $outputDir
        } else {
            Write-Host "💡 Para abrir la carpeta de salida, ejecuta:" -ForegroundColor Blue
            Write-Host "   .\publish-release.ps1 -Type $Type -OpenOutput" -ForegroundColor White
        }
        
    } else {
        Write-Host "❌ ERROR: No se encontró el directorio de salida" -ForegroundColor Red
        Write-Host "   Esperado: $outputDir" -ForegroundColor White
        exit 1
    }
    
} catch {
    Write-Host "❌ ERROR DURANTE LA PUBLICACIÓN:" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor White
    exit 1
}

Write-Host ""
Write-Host "🎉 ¡LISTO PARA DISTRIBUIR!" -ForegroundColor Green -BackgroundColor DarkGreen
Write-Host "===============================================" -ForegroundColor Green