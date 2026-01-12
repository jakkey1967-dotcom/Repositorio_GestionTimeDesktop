param(
    [string]$InstallPath = "$env:ProgramFiles\GestionTime Desktop",
    [switch]$Verbose
)

Write-Host ""
Write-Host "🔍 DIAGNÓSTICO AVANZADO RUNTIME - GESTIONTIME DESKTOP" -ForegroundColor Green -BackgroundColor DarkGreen
Write-Host "===============================================" -ForegroundColor Green
Write-Host ""

$ErrorActionPreference = "Continue"

try {
    # Verificar instalación
    Write-Host "📂 VERIFICANDO INSTALACIÓN:" -ForegroundColor Cyan
    Write-Host "   • Directorio: $InstallPath" -ForegroundColor White
    
    if (!(Test-Path $InstallPath)) {
        Write-Host "   ❌ Directorio de instalación no encontrado" -ForegroundColor Red
        Write-Host "     La aplicación no está instalada" -ForegroundColor Red
        return
    }
    
    Write-Host "   ✅ Directorio de instalación encontrado" -ForegroundColor Green
    
    # Obtener archivos instalados
    $installedFiles = Get-ChildItem $InstallPath -File -ErrorAction SilentlyContinue
    Write-Host "   • Archivos instalados: $($installedFiles.Count)" -ForegroundColor White
    
    # Verificar ejecutable principal
    $exePath = Join-Path $InstallPath "GestionTime.Desktop.exe"
    if (!(Test-Path $exePath)) {
        Write-Host "   ❌ Ejecutable principal no encontrado" -ForegroundColor Red
        return
    }
    
    Write-Host "   ✅ Ejecutable principal encontrado" -ForegroundColor Green
    
    # Información del ejecutable
    try {
        $exeInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($exePath)
        Write-Host "   • Versión: $($exeInfo.FileVersion)" -ForegroundColor White
        Write-Host "   • Descripción: $($exeInfo.FileDescription)" -ForegroundColor White
    } catch {
        Write-Host "   ⚠️  No se pudo leer información del ejecutable" -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "🔍 ANÁLISIS DETALLADO DE ARCHIVOS:" -ForegroundColor Cyan
    
    # Archivos críticos
    $criticalFiles = @{
        "GestionTime.Desktop.exe" = "Ejecutable principal"
        "GestionTime.Desktop.dll" = "Biblioteca principal"
        "GestionTime.Desktop.pdb" = "Símbolos de debugging"
        "appsettings.json" = "Configuración"
        "System.Private.CoreLib.dll" = ".NET Core Runtime"
        "Microsoft.WindowsAppRuntime.dll" = "Windows App SDK"
        "WinRT.Runtime.dll" = "WinRT Runtime"
        "Microsoft.UI.Xaml.dll" = "WinUI 3 Framework"
    }
    
    $missingCritical = @()
    foreach ($file in $criticalFiles.Keys) {
        $filePath = Join-Path $InstallPath $file
        if (Test-Path $filePath) {
            $fileInfo = Get-Item $filePath
            Write-Host "   ✅ $file ($([math]::Round($fileInfo.Length/1KB, 1)) KB)" -ForegroundColor Green
        } else {
            Write-Host "   ❌ $file (FALTANTE) - $($criticalFiles[$file])" -ForegroundColor Red
            $missingCritical += $file
        }
    }
    
    # Archivos de recursos WinUI
    Write-Host ""
    Write-Host "🎨 RECURSOS WinUI:" -ForegroundColor Cyan
    $resourceFiles = @(
        "Microsoft.UI.pri",
        "Microsoft.UI.Xaml.Controls.pri",
        "Microsoft.WindowsAppRuntime.pri"
    )
    
    $missingResources = @()
    foreach ($resource in $resourceFiles) {
        $resourcePath = Join-Path $InstallPath $resource
        if (Test-Path $resourcePath) {
            $resourceInfo = Get-Item $resourcePath
            Write-Host "   ✅ $resource ($([math]::Round($resourceInfo.Length/1KB, 1)) KB)" -ForegroundColor Green
        } else {
            Write-Host "   ❌ $resource (FALTANTE)" -ForegroundColor Red
            $missingResources += $resource
        }
    }
    
    # Verificar runtime .NET en el sistema
    Write-Host ""
    Write-Host "⚙️ VERIFICANDO .NET RUNTIME EN SISTEMA:" -ForegroundColor Cyan
    
    try {
        $dotnetRuntimes = dotnet --list-runtimes 2>$null
        if ($dotnetRuntimes) {
            $net8Runtime = $dotnetRuntimes | Where-Object { $_ -like "*Microsoft.NETCore.App 8.*" }
            $windowsDesktopRuntime = $dotnetRuntimes | Where-Object { $_ -like "*Microsoft.WindowsDesktop.App 8.*" }
            
            if ($net8Runtime) {
                Write-Host "   ✅ .NET 8 Runtime encontrado" -ForegroundColor Green
                if ($Verbose) {
                    $net8Runtime | ForEach-Object { Write-Host "     $($_)" -ForegroundColor White }
                }
            } else {
                Write-Host "   ❌ .NET 8 Runtime NO encontrado" -ForegroundColor Red
            }
            
            if ($windowsDesktopRuntime) {
                Write-Host "   ✅ Windows Desktop Runtime encontrado" -ForegroundColor Green
                if ($Verbose) {
                    $windowsDesktopRuntime | ForEach-Object { Write-Host "     $($_)" -ForegroundColor White }
                }
            } else {
                Write-Host "   ❌ Windows Desktop Runtime NO encontrado" -ForegroundColor Red
            }
        } else {
            Write-Host "   ❌ .NET CLI no disponible" -ForegroundColor Red
        }
    } catch {
        Write-Host "   ⚠️  No se pudo verificar runtime .NET: $($_.Exception.Message)" -ForegroundColor Yellow
    }
    
    # Verificar WindowsAppSDK
    Write-Host ""
    Write-Host "🪟 VERIFICANDO WINDOWS APP SDK:" -ForegroundColor Cyan
    
    $appSDKPath = Join-Path $env:ProgramFiles "WindowsApps"
    $appSDKInstalled = $false
    if (Test-Path $appSDKPath) {
        try {
            $appSDKDirs = Get-ChildItem $appSDKPath -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name -like "*Microsoft.WindowsAppRuntime*" }
            if ($appSDKDirs) {
                Write-Host "   ✅ Windows App SDK instalado" -ForegroundColor Green
                $appSDKInstalled = $true
                if ($Verbose) {
                    $appSDKDirs | ForEach-Object { Write-Host "     $($_.Name)" -ForegroundColor White }
                }
            } else {
                Write-Host "   ⚠️  Windows App SDK no encontrado en WindowsApps" -ForegroundColor Yellow
            }
        } catch {
            Write-Host "   ⚠️  No se pudo acceder a WindowsApps (normal)" -ForegroundColor Yellow
        }
    }
    
    # Verificar logs de errores
    Write-Host ""
    Write-Host "📋 VERIFICANDO LOGS DE ERROR:" -ForegroundColor Cyan
    
    $logsPath = Join-Path $InstallPath "logs"
    if (Test-Path $logsPath) {
        $logFiles = Get-ChildItem $logsPath -Filter "*.log" -ErrorAction SilentlyContinue
        if ($logFiles.Count -gt 0) {
            $latestLog = $logFiles | Sort-Object LastWriteTime -Descending | Select-Object -First 1
            Write-Host "   ✅ Logs encontrados: $($logFiles.Count)" -ForegroundColor Green
            Write-Host "   • Log más reciente: $($latestLog.Name)" -ForegroundColor White
            
            # Leer errores del log
            $logContent = Get-Content $latestLog.FullName -Tail 20 -ErrorAction SilentlyContinue
            $errors = $logContent | Where-Object { $_ -match "ERROR|CRITICAL|Exception" }
            
            if ($errors) {
                Write-Host ""
                Write-Host "🔴 ERRORES ENCONTRADOS EN LOG:" -ForegroundColor Red
                foreach ($error in $errors | Select-Object -Last 5) {
                    Write-Host "     $error" -ForegroundColor Red
                }
            }
        } else {
            Write-Host "   ⚪ Sin archivos de log" -ForegroundColor White
        }
    } else {
        Write-Host "   ⚪ Sin directorio de logs" -ForegroundColor White
    }
    
    # Verificar Event Viewer para errores de aplicación
    Write-Host ""
    Write-Host "🔍 VERIFICANDO EVENT VIEWER:" -ForegroundColor Cyan
    
    try {
        $appErrors = Get-EventLog -LogName Application -Source "*GestionTime*" -Newest 5 -ErrorAction SilentlyContinue
        if ($appErrors) {
            Write-Host "   ⚠️  Errores de aplicación encontrados en Event Viewer:" -ForegroundColor Yellow
            foreach ($error in $appErrors) {
                Write-Host "     [$($error.TimeGenerated)] $($error.Message.Substring(0, [Math]::Min(100, $error.Message.Length)))..." -ForegroundColor Yellow
            }
        } else {
            Write-Host "   ✅ Sin errores específicos en Event Viewer" -ForegroundColor Green
        }
    } catch {
        Write-Host "   ⚪ No se pudo acceder a Event Viewer" -ForegroundColor White
    }
    
    # Probar ejecución con más detalle
    Write-Host ""
    Write-Host "🚀 PRUEBA DE EJECUCIÓN DIAGNÓSTICA:" -ForegroundColor Cyan
    
    try {
        # Verificar dependencias DLL
        Write-Host "   • Verificando dependencias DLL..." -ForegroundColor White
        
        $startInfo = New-Object System.Diagnostics.ProcessStartInfo
        $startInfo.FileName = $exePath
        $startInfo.WorkingDirectory = $InstallPath
        $startInfo.UseShellExecute = $false
        $startInfo.RedirectStandardOutput = $true
        $startInfo.RedirectStandardError = $true
        $startInfo.CreateNoWindow = $true
        
        $process = New-Object System.Diagnostics.Process
        $process.StartInfo = $startInfo
        
        Write-Host "   • Intentando iniciar proceso..." -ForegroundColor White
        $started = $process.Start()
        
        if ($started) {
            # Esperar un poco para ver si inicia correctamente
            Start-Sleep -Seconds 3
            
            if (!$process.HasExited) {
                Write-Host "   ✅ Proceso iniciado correctamente" -ForegroundColor Green
                $process.Kill()
            } else {
                Write-Host "   ❌ Proceso terminó inmediatamente (Exit Code: $($process.ExitCode))" -ForegroundColor Red
                
                $stdOut = $process.StandardOutput.ReadToEnd()
                $stdErr = $process.StandardError.ReadToEnd()
                
                if ($stdErr) {
                    Write-Host "   🔴 Error de salida:" -ForegroundColor Red
                    Write-Host "     $stdErr" -ForegroundColor Red
                }
                
                if ($stdOut) {
                    Write-Host "   📄 Salida estándar:" -ForegroundColor White
                    Write-Host "     $stdOut" -ForegroundColor White
                }
            }
        } else {
            Write-Host "   ❌ No se pudo iniciar el proceso" -ForegroundColor Red
        }
        
    } catch {
        Write-Host "   ❌ Error al intentar ejecutar: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    # Resumen del diagnóstico
    Write-Host ""
    Write-Host "📊 RESUMEN DEL DIAGNÓSTICO:" -ForegroundColor Magenta
    Write-Host "===============================================" -ForegroundColor Magenta
    
    $issues = @()
    
    if ($missingCritical.Count -gt 0) {
        $issues += "Archivos críticos faltantes: $($missingCritical.Count)"
    }
    
    if ($missingResources.Count -gt 0) {
        $issues += "Recursos WinUI faltantes: $($missingResources.Count)"
    }
    
    # Diagnóstico final
    if ($issues.Count -eq 0) {
        Write-Host "✅ DIAGNÓSTICO: INSTALACIÓN PARECE COMPLETA" -ForegroundColor Green
        Write-Host "   El problema puede ser de configuración o runtime" -ForegroundColor Green
    } else {
        Write-Host "❌ PROBLEMAS IDENTIFICADOS:" -ForegroundColor Red
        foreach ($issue in $issues) {
            Write-Host "   • $issue" -ForegroundColor Red
        }
    }
    
    Write-Host ""
    Write-Host "💡 RECOMENDACIONES:" -ForegroundColor Blue
    
    if ($missingCritical.Count -gt 0) {
        Write-Host "   1. ❌ MSI actual incompleto - Usar instalador completo:" -ForegroundColor Red
        Write-Host "      .\create-selfextracting-installer.ps1 -Rebuild" -ForegroundColor Gray
        Write-Host ""
        Write-Host "   2. ⚠️  O instalar MSI Debug con más archivos:" -ForegroundColor Yellow
        Write-Host "      .\create-msi-debug-complete.ps1 -OpenOutput" -ForegroundColor Gray
    } else {
        Write-Host "   1. ✅ Archivos principales presentes" -ForegroundColor Green
        Write-Host "   2. 🔧 Verificar configuración de appsettings.json" -ForegroundColor Yellow
        Write-Host "   3. 🔄 Intentar ejecutar como administrador" -ForegroundColor Yellow
        Write-Host "   4. 📋 Revisar logs detallados de la aplicación" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "❌ ERROR DURANTE EL DIAGNÓSTICO:" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor White
}

Write-Host ""
Write-Host "🔍 DIAGNÓSTICO COMPLETADO" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green