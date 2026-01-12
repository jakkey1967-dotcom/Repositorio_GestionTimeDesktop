param(
    [string]$InstallPath = "C:\Program Files\GestionTime Solutions\GestionTime Desktop"
)

Write-Host ""
Write-Host "🔍 ANÁLISIS COMPARATIVO: MSI vs APLICACIÓN COMPLETA" -ForegroundColor Green -BackgroundColor DarkGreen
Write-Host "===============================================" -ForegroundColor Green
Write-Host ""

try {
    # Verificar instalación actual
    if (!(Test-Path $InstallPath)) {
        Write-Host "❌ Aplicación no instalada en $InstallPath" -ForegroundColor Red
        return
    }

    # Obtener archivos instalados
    $installedFiles = Get-ChildItem $InstallPath -File
    Write-Host "📊 ESTADO ACTUAL DE LA INSTALACIÓN:" -ForegroundColor Cyan
    Write-Host "   • Directorio: $InstallPath" -ForegroundColor White
    Write-Host "   • Archivos instalados: $($installedFiles.Count)" -ForegroundColor White
    Write-Host ""

    # Mostrar archivos instalados
    Write-Host "📁 ARCHIVOS ACTUALMENTE INSTALADOS:" -ForegroundColor Cyan
    foreach ($file in $installedFiles | Sort-Object Name) {
        $size = [math]::Round($file.Length/1KB, 1)
        Write-Host "   ✅ $($file.Name) ($size KB)" -ForegroundColor Green
    }

    # Comparar con aplicación completa
    $completeAppPath = "bin\Release\Installer\App"
    if (Test-Path $completeAppPath) {
        $completeFiles = Get-ChildItem $completeAppPath -File
        
        Write-Host ""
        Write-Host "📊 COMPARACIÓN CON APLICACIÓN COMPLETA:" -ForegroundColor Magenta
        Write-Host "   • Archivos en MSI instalado: $($installedFiles.Count)" -ForegroundColor White
        Write-Host "   • Archivos en aplicación completa: $($completeFiles.Count)" -ForegroundColor White
        Write-Host "   • Diferencia: $($completeFiles.Count - $installedFiles.Count) archivos faltantes" -ForegroundColor Yellow
        Write-Host ""

        # Archivos importantes que faltan
        $installedNames = $installedFiles | ForEach-Object { $_.Name }
        $missingFiles = $completeFiles | Where-Object { $_.Name -notin $installedNames }
        
        if ($missingFiles.Count -gt 0) {
            Write-Host "❌ ARCHIVOS IMPORTANTES FALTANTES:" -ForegroundColor Red
            
            # Agrupar por tipo
            $missingByType = $missingFiles | Group-Object { $_.Extension }
            
            foreach ($group in $missingByType | Sort-Object Name) {
                $ext = if ($group.Name) { $group.Name } else { "(sin extensión)" }
                Write-Host "   📂 $ext ($($group.Count) archivos):" -ForegroundColor Yellow
                
                $importantFiles = $group.Group | Where-Object { 
                    $_.Name -like "*System*" -or 
                    $_.Name -like "*Microsoft*" -or 
                    $_.Name -like "*Windows*" -or 
                    $_.Name -like "*WinRT*" -or
                    $_.Name -like "*GestionTime*" -or
                    $_.Extension -eq ".exe" -or
                    $_.Extension -eq ".pdb"
                }
                
                if ($importantFiles) {
                    foreach ($file in $importantFiles | Sort-Object Length -Descending | Select-Object -First 10) {
                        $size = [math]::Round($file.Length/1KB, 1)
                        Write-Host "     ⚠️  $($file.Name) ($size KB)" -ForegroundColor Red
                    }
                }
                
                if ($group.Count -gt 10) {
                    Write-Host "     ... y $($group.Count - 10) archivos más" -ForegroundColor Gray
                }
            }
        }

        # Archivos críticos específicos que pueden estar faltando
        $criticalMissing = @(
            "hostfxr.dll",
            "hostpolicy.dll", 
            "Microsoft.Win32.Primitives.dll",
            "System.Runtime.dll",
            "System.Threading.dll",
            "System.IO.FileSystem.dll",
            "Microsoft.UI.Xaml.Controls.dll"
        )

        Write-Host ""
        Write-Host "🔍 VERIFICANDO ARCHIVOS CRÍTICOS ESPECÍFICOS:" -ForegroundColor Cyan
        
        foreach ($critical in $criticalMissing) {
            $installedPath = Join-Path $InstallPath $critical
            $completePath = Join-Path $completeAppPath $critical
            
            if (Test-Path $installedPath) {
                Write-Host "   ✅ $critical (PRESENTE)" -ForegroundColor Green
            } elseif (Test-Path $completePath) {
                $fileInfo = Get-Item $completePath
                Write-Host "   ❌ $critical (FALTANTE - $([math]::Round($fileInfo.Length/1KB, 1)) KB)" -ForegroundColor Red
            } else {
                Write-Host "   ⚪ $critical (NO APLICABLE)" -ForegroundColor Gray
            }
        }
    }

    # Verificar problemas conocidos
    Write-Host ""
    Write-Host "🔧 DIAGNÓSTICO DE PROBLEMAS CONOCIDOS:" -ForegroundColor Blue
    
    # 1. Verificar configuración
    $configPath = Join-Path $InstallPath "appsettings.json"
    if (Test-Path $configPath) {
        try {
            $config = Get-Content $configPath | ConvertFrom-Json
            Write-Host "   ✅ Configuración leída correctamente" -ForegroundColor Green
            
            # Verificar configuraciones críticas
            if ($config.ApiClient -and $config.ApiClient.BaseUrl) {
                Write-Host "     • API Base URL: $($config.ApiClient.BaseUrl)" -ForegroundColor White
            }
            
            if ($config.Logging) {
                Write-Host "     • Logging configurado: ✅" -ForegroundColor Green
            }
            
        } catch {
            Write-Host "   ❌ Error al leer configuración: $($_.Exception.Message)" -ForegroundColor Red
        }
    }

    # 2. Verificar permisos de directorio
    try {
        $acl = Get-Acl $InstallPath
        Write-Host "   ✅ Permisos de directorio: OK" -ForegroundColor Green
    } catch {
        Write-Host "   ⚠️  Problema de permisos en directorio" -ForegroundColor Yellow
    }

    # 3. Test de ejecución con parámetros de debugging
    Write-Host ""
    Write-Host "🚀 PRUEBA DE EJECUCIÓN AVANZADA:" -ForegroundColor Blue
    
    try {
        $exePath = Join-Path $InstallPath "GestionTime.Desktop.exe"
        
        # Crear proceso con redirección de salida
        $psi = New-Object System.Diagnostics.ProcessStartInfo
        $psi.FileName = $exePath
        $psi.WorkingDirectory = $InstallPath
        $psi.UseShellExecute = $false
        $psi.RedirectStandardOutput = $true
        $psi.RedirectStandardError = $true
        $psi.CreateNoWindow = $true
        
        $process = [System.Diagnostics.Process]::Start($psi)
        
        # Esperar un poco para capturar salida inicial
        Start-Sleep -Seconds 2
        
        if (!$process.HasExited) {
            Write-Host "   ✅ Aplicación iniciada correctamente" -ForegroundColor Green
            Write-Host "     • PID: $($process.Id)" -ForegroundColor White
            Write-Host "     • Estado: Running" -ForegroundColor White
            
            # Terminar el proceso de prueba
            $process.Kill()
            $process.WaitForExit(5000)
            Write-Host "     • Proceso de prueba terminado" -ForegroundColor Gray
            
        } else {
            Write-Host "   ❌ Aplicación terminó inmediatamente" -ForegroundColor Red
            Write-Host "     • Exit Code: $($process.ExitCode)" -ForegroundColor Red
            
            $stdout = $process.StandardOutput.ReadToEnd()
            $stderr = $process.StandardError.ReadToEnd()
            
            if ($stderr) {
                Write-Host "     • Error: $stderr" -ForegroundColor Red
            }
            
            if ($stdout) {
                Write-Host "     • Output: $stdout" -ForegroundColor Yellow
            }
        }
        
    } catch {
        Write-Host "   ❌ Error al ejecutar: $($_.Exception.Message)" -ForegroundColor Red
    }

    # Recomendaciones finales
    Write-Host ""
    Write-Host "💡 RECOMENDACIONES BASADAS EN EL ANÁLISIS:" -ForegroundColor Blue
    Write-Host "===============================================" -ForegroundColor Blue
    
    if ($missingFiles -and $missingFiles.Count -gt 100) {
        Write-Host ""
        Write-Host "🔴 PROBLEMA CRÍTICO IDENTIFICADO:" -ForegroundColor Red
        Write-Host "   • Faltan $($missingFiles.Count) archivos de los $($completeFiles.Count) necesarios" -ForegroundColor Red
        Write-Host "   • El MSI actual solo instala archivos básicos" -ForegroundColor Red
        Write-Host ""
        Write-Host "✅ SOLUCIÓN RECOMENDADA:" -ForegroundColor Green
        Write-Host "   1. Usar instalador auto-extraíble completo:" -ForegroundColor Green
        Write-Host "      .\create-selfextracting-installer.ps1 -Rebuild" -ForegroundColor Gray
        Write-Host ""
        Write-Host "   2. O crear MSI Debug completo:" -ForegroundColor Yellow
        Write-Host "      .\create-msi-debug-complete.ps1 -Rebuild -OpenOutput" -ForegroundColor Gray
        
    } elseif ($missingFiles -and $missingFiles.Count -gt 10) {
        Write-Host ""
        Write-Host "🟡 PROBLEMA MODERADO:" -ForegroundColor Yellow
        Write-Host "   • Faltan algunos archivos importantes ($($missingFiles.Count))" -ForegroundColor Yellow
        Write-Host "   • La aplicación puede funcionar parcialmente" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "✅ SOLUCIÓN:" -ForegroundColor Green
        Write-Host "   Usar MSI mejorado con más archivos:" -ForegroundColor Green
        Write-Host "   .\create-improved-msi.ps1 -OpenOutput" -ForegroundColor Gray
        
    } else {
        Write-Host ""
        Write-Host "🟢 INSTALACIÓN PARECE COMPLETA:" -ForegroundColor Green
        Write-Host "   • La mayoría de archivos están presentes" -ForegroundColor Green
        Write-Host "   • El problema puede ser de configuración o temporal" -ForegroundColor Green
        Write-Host ""
        Write-Host "🔧 SOLUCIONES A PROBAR:" -ForegroundColor Blue
        Write-Host "   1. Ejecutar como administrador" -ForegroundColor White
        Write-Host "   2. Verificar configuración de red/API" -ForegroundColor White
        Write-Host "   3. Reiniciar el sistema" -ForegroundColor White
        Write-Host "   4. Revisar logs de la aplicación" -ForegroundColor White
    }

} catch {
    Write-Host "❌ ERROR DURANTE EL ANÁLISIS:" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor White
}

Write-Host ""
Write-Host "🔍 ANÁLISIS COMPARATIVO COMPLETADO" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green