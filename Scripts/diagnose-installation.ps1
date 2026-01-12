param(
    [string]$InstallPath = "$env:ProgramFiles\GestionTime Desktop"
)

Write-Host ""
Write-Host "🔍 DIAGNÓSTICO GESTIONTIME DESKTOP" -ForegroundColor Green -BackgroundColor DarkGreen
Write-Host "===============================================" -ForegroundColor Green
Write-Host ""

try {
    Write-Host "📂 VERIFICANDO INSTALACIÓN:" -ForegroundColor Cyan
    Write-Host "   • Directorio de instalación: $InstallPath" -ForegroundColor White
    
    if (!(Test-Path $InstallPath)) {
        Write-Host "   ❌ Directorio de instalación no encontrado" -ForegroundColor Red
        Write-Host "   La aplicación no está instalada correctamente" -ForegroundColor Red
        return
    }
    
    Write-Host "   ✅ Directorio de instalación encontrado" -ForegroundColor Green
    
    # Verificar archivos principales
    $requiredFiles = @(
        "GestionTime.Desktop.exe",
        "GestionTime.Desktop.dll",
        "appsettings.json"
    )
    
    Write-Host ""
    Write-Host "📄 VERIFICANDO ARCHIVOS PRINCIPALES:" -ForegroundColor Cyan
    foreach ($file in $requiredFiles) {
        $filePath = Join-Path $InstallPath $file
        if (Test-Path $filePath) {
            $fileInfo = Get-Item $filePath
            Write-Host "   ✅ $file ($([math]::Round($fileInfo.Length/1KB, 1)) KB)" -ForegroundColor Green
        } else {
            Write-Host "   ❌ $file (FALTANTE)" -ForegroundColor Red
        }
    }
    
    # Verificar archivos de recursos WinUI
    Write-Host ""
    Write-Host "🎨 VERIFICANDO RECURSOS WinUI:" -ForegroundColor Cyan
    
    $priFiles = Get-ChildItem $InstallPath -Filter "*.pri" -ErrorAction SilentlyContinue
    $xbfFiles = Get-ChildItem $InstallPath -Filter "*.xbf" -ErrorAction SilentlyContinue -Recurse
    
    Write-Host "   • Archivos .pri (Package Resource Index): $($priFiles.Count)" -ForegroundColor White
    Write-Host "   • Archivos .xbf (XAML Binary Format): $($xbfFiles.Count)" -ForegroundColor White
    
    if ($priFiles.Count -gt 0) {
        Write-Host "   ✅ Archivos de recursos encontrados" -ForegroundColor Green
        foreach ($pri in $priFiles) {
            Write-Host "     - $($pri.Name) ($([math]::Round($pri.Length/1KB, 1)) KB)" -ForegroundColor White
        }
    } else {
        Write-Host "   ❌ Sin archivos .pri - CAUSA PROBABLE DEL ERROR XAML" -ForegroundColor Red
        Write-Host "     Esto causa: 'Cannot locate resource from ms-appx:///MainWindow.xaml'" -ForegroundColor Red
    }
    
    # Verificar runtime .NET
    Write-Host ""
    Write-Host "⚙️ VERIFICANDO RUNTIME .NET:" -ForegroundColor Cyan
    
    $dotnetFiles = Get-ChildItem $InstallPath -Filter "*.dll" | Where-Object { 
        $_.Name -like "*System*" -or $_.Name -like "*Microsoft*" 
    }
    
    Write-Host "   • Bibliotecas .NET encontradas: $($dotnetFiles.Count)" -ForegroundColor White
    
    $coreLibs = @("System.Private.CoreLib.dll", "Microsoft.WindowsAppRuntime.dll")
    foreach ($lib in $coreLibs) {
        $libPath = Join-Path $InstallPath $lib
        if (Test-Path $libPath) {
            Write-Host "   ✅ $lib" -ForegroundColor Green
        } else {
            Write-Host "   ❌ $lib (FALTANTE)" -ForegroundColor Red
        }
    }
    
    # Verificar logs de errores
    Write-Host ""
    Write-Host "📋 VERIFICANDO LOGS DE ERROR:" -ForegroundColor Cyan
    
    $logsPath = Join-Path $InstallPath "logs"
    if (Test-Path $logsPath) {
        $logFiles = Get-ChildItem $logsPath -Filter "*.log" -ErrorAction SilentlyContinue
        Write-Host "   • Archivos de log encontrados: $($logFiles.Count)" -ForegroundColor White
        
        if ($logFiles.Count -gt 0) {
            $latestLog = $logFiles | Sort-Object LastWriteTime -Descending | Select-Object -First 1
            Write-Host "   • Log más reciente: $($latestLog.Name)" -ForegroundColor White
            
            # Leer últimas líneas del log
            $logContent = Get-Content $latestLog.FullName -Tail 10 -ErrorAction SilentlyContinue
            if ($logContent) {
                Write-Host ""
                Write-Host "📜 ÚLTIMAS LÍNEAS DEL LOG:" -ForegroundColor Yellow
                foreach ($line in $logContent) {
                    if ($line -match "ERROR|CRITICAL") {
                        Write-Host "   🔴 $line" -ForegroundColor Red
                    } elseif ($line -match "WARNING") {
                        Write-Host "   🟡 $line" -ForegroundColor Yellow
                    } else {
                        Write-Host "   ⚪ $line" -ForegroundColor White
                    }
                }
            }
        }
    } else {
        Write-Host "   ⚪ Sin directorio de logs" -ForegroundColor White
    }
    
    # Verificar registro de Windows
    Write-Host ""
    Write-Host "🔧 VERIFICANDO REGISTRO DE WINDOWS:" -ForegroundColor Cyan
    
    $regPath = "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop"
    if (Test-Path $regPath) {
        Write-Host "   ✅ Registro de desinstalación encontrado" -ForegroundColor Green
        $regInfo = Get-ItemProperty $regPath -ErrorAction SilentlyContinue
        if ($regInfo) {
            Write-Host "     - Nombre: $($regInfo.DisplayName)" -ForegroundColor White
            Write-Host "     - Versión: $($regInfo.DisplayVersion)" -ForegroundColor White
        }
    } else {
        Write-Host "   ❌ Registro de desinstalación no encontrado" -ForegroundColor Red
    }
    
    # Probar ejecución
    Write-Host ""
    Write-Host "🚀 DIAGNÓSTICO DE EJECUCIÓN:" -ForegroundColor Cyan
    
    $exePath = Join-Path $InstallPath "GestionTime.Desktop.exe"
    if (Test-Path $exePath) {
        Write-Host "   • Intentando obtener información del ejecutable..." -ForegroundColor White
        
        try {
            $fileVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($exePath)
            Write-Host "   ✅ Ejecutable válido:" -ForegroundColor Green
            Write-Host "     - Versión: $($fileVersion.FileVersion)" -ForegroundColor White
            Write-Host "     - Descripción: $($fileVersion.FileDescription)" -ForegroundColor White
            Write-Host "     - Compañía: $($fileVersion.CompanyName)" -ForegroundColor White
        } catch {
            Write-Host "   ❌ Error al leer información del ejecutable: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    
    # Resumen de diagnóstico
    Write-Host ""
    Write-Host "📊 RESUMEN DEL DIAGNÓSTICO:" -ForegroundColor Magenta
    Write-Host "===============================================" -ForegroundColor Magenta
    
    $issues = @()
    
    if ($priFiles.Count -eq 0) {
        $issues += "Sin archivos .pri - Error XAML probable"
    }
    
    foreach ($file in $requiredFiles) {
        $filePath = Join-Path $InstallPath $file
        if (!(Test-Path $filePath)) {
            $issues += "Falta archivo: $file"
        }
    }
    
    if ($issues.Count -eq 0) {
        Write-Host "✅ DIAGNÓSTICO: INSTALACIÓN COMPLETA" -ForegroundColor Green
        Write-Host "   No se encontraron problemas críticos" -ForegroundColor Green
    } else {
        Write-Host "❌ PROBLEMAS ENCONTRADOS:" -ForegroundColor Red
        foreach ($issue in $issues) {
            Write-Host "   • $issue" -ForegroundColor Red
        }
        
        Write-Host ""
        Write-Host "💡 SOLUCIONES RECOMENDADAS:" -ForegroundColor Blue
        if ($priFiles.Count -eq 0) {
            Write-Host "   1. Reinstalar con el instalador corregido más reciente" -ForegroundColor White
            Write-Host "   2. Asegurar que el build incluye recursos WinUI 3" -ForegroundColor White
        }
        foreach ($file in $requiredFiles) {
            $filePath = Join-Path $InstallPath $file
            if (!(Test-Path $filePath)) {
                Write-Host "   3. Verificar que $file se copió correctamente" -ForegroundColor White
            }
        }
    }
    
} catch {
    Write-Host "❌ ERROR DURANTE EL DIAGNÓSTICO:" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor White
}

Write-Host ""
Write-Host "🔍 DIAGNÓSTICO COMPLETADO" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green