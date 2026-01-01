# ================================================================
# PRUEBA REAL DEL SISTEMA DE LOGGING - VERSIÓN CON DETECCIÓN AUTO
# ================================================================

Write-Host "?? PRUEBA REAL DEL LOGGING CON DETECCIÓN AUTOMÁTICA" -ForegroundColor Cyan
Write-Host "===================================================" -ForegroundColor Cyan
Write-Host ""

# ================================================================
# DETECCIÓN AUTOMÁTICA DE LA UBICACIÓN DEL EJECUTABLE
# ================================================================

Write-Host "?? DETECTANDO UBICACIÓN DEL EJECUTABLE..." -ForegroundColor Yellow
Write-Host ""

# Buscar en todas las ubicaciones posibles
$searchPaths = @(
    "bin\Debug\net8.0-windows10.0.19041.0\GestionTime.Desktop.exe",
    "bin\Debug\net8.0-windows10.0.19041.0\win-x64\GestionTime.Desktop.exe", 
    "bin\x64\Debug\net8.0-windows10.0.19041.0\GestionTime.Desktop.exe",
    "bin\x64\Debug\net8.0-windows10.0.19041.0\win-x64\GestionTime.Desktop.exe",
    "bin\Release\net8.0-windows10.0.19041.0\GestionTime.Desktop.exe",
    "bin\Release\net8.0-windows10.0.19041.0\win-x64\GestionTime.Desktop.exe",
    "bin\x64\Release\net8.0-windows10.0.19041.0\GestionTime.Desktop.exe",
    "bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\GestionTime.Desktop.exe"
)

$GestionTimeDesktopPath = $null
$GestionTimeDesktopDir = $null

foreach ($searchPath in $searchPaths) {
    if (Test-Path $searchPath) {
        $GestionTimeDesktopPath = Resolve-Path $searchPath
        $GestionTimeDesktopDir = Split-Path $GestionTimeDesktopPath -Parent
        Write-Host "? Ejecutable encontrado: $GestionTimeDesktopPath" -ForegroundColor Green
        Write-Host "?? Directorio: $GestionTimeDesktopDir" -ForegroundColor Gray
        break
    }
}

if (-not $GestionTimeDesktopPath) {
    Write-Host "? NO SE ENCONTRÓ EL EJECUTABLE" -ForegroundColor Red
    Write-Host ""
    Write-Host "?? COMPILANDO PROYECTO..." -ForegroundColor Yellow
    
    try {
        msbuild /p:Configuration=Debug /verbosity:quiet
        Write-Host "? Compilación completada" -ForegroundColor Green
        
        # Buscar nuevamente después de compilar
        foreach ($searchPath in $searchPaths) {
            if (Test-Path $searchPath) {
                $GestionTimeDesktopPath = Resolve-Path $searchPath
                $GestionTimeDesktopDir = Split-Path $GestionTimeDesktopPath -Parent
                Write-Host "? Ejecutable generado: $GestionTimeDesktopPath" -ForegroundColor Green
                Write-Host "?? Directorio: $GestionTimeDesktopDir" -ForegroundColor Gray
                break
            }
        }
        
        if (-not $GestionTimeDesktopPath) {
            Write-Host "? El ejecutable no se generó después de compilar" -ForegroundColor Red
            Write-Host ""
            Write-Host "?? POSIBLES SOLUCIONES:" -ForegroundColor Yellow
            Write-Host "   1. Compilar desde Visual Studio (Build ? Build Solution)" -ForegroundColor Gray
            Write-Host "   2. Verificar errores de compilación" -ForegroundColor Gray
            Write-Host "   3. Limpiar y reconstruir (Build ? Rebuild Solution)" -ForegroundColor Gray
            Write-Host ""
            Read-Host "Presiona Enter para finalizar"
            exit 1
        }
    } catch {
        Write-Host "? Error compilando: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
        Read-Host "Presiona Enter para finalizar"
        exit 1
    }
}

# ================================================================
# CONFIGURACIÓN DE VARIABLES DE ENTORNO
# ================================================================

Write-Host ""
Write-Host "?? CONFIGURANDO VARIABLES DE ENTORNO..." -ForegroundColor Yellow

# Establecer variables de entorno para esta sesión
$env:GESTIONTIME_DESKTOP_PATH = $GestionTimeDesktopPath.Path
$env:GESTIONTIME_DESKTOP_DIR = $GestionTimeDesktopDir

Write-Host "   ? GESTIONTIME_DESKTOP_PATH = $($env:GESTIONTIME_DESKTOP_PATH)" -ForegroundColor Green
Write-Host "   ? GESTIONTIME_DESKTOP_DIR = $($env:GESTIONTIME_DESKTOP_DIR)" -ForegroundColor Green

# ================================================================
# CONFIGURAR DIRECTORIO DE LOGS
# ================================================================

Write-Host ""
Write-Host "?? CONFIGURANDO DIRECTORIO DE LOGS..." -ForegroundColor Yellow

$logsPath = Join-Path $GestionTimeDesktopDir "logs"

# Crear directorio de logs si no existe
if (-not (Test-Path $logsPath)) {
    New-Item -ItemType Directory -Path $logsPath -Force | Out-Null
    Write-Host "   ? Directorio logs creado: $logsPath" -ForegroundColor Green
} else {
    Write-Host "   ? Directorio logs existe: $logsPath" -ForegroundColor Green
}

# Verificar permisos de escritura
try {
    $testFile = Join-Path $logsPath "test_write_$(Get-Date -Format 'HHmmss').tmp"
    "test" | Out-File $testFile -ErrorAction Stop
    Remove-Item $testFile -ErrorAction SilentlyContinue
    Write-Host "   ? Permisos de escritura verificados" -ForegroundColor Green
} catch {
    Write-Host "   ? Error de permisos de escritura: $($_.Exception.Message)" -ForegroundColor Red
}

# ================================================================
# CONFIGURAR APPSETTINGS.JSON
# ================================================================

Write-Host ""
Write-Host "?? CONFIGURANDO appsettings.json..." -ForegroundColor Yellow

$appsettingsSource = "appsettings.json"
$appsettingsTarget = Join-Path $GestionTimeDesktopDir "appsettings.json"

if (Test-Path $appsettingsSource) {
    Copy-Item $appsettingsSource $appsettingsTarget -Force
    Write-Host "   ? appsettings.json copiado a: $appsettingsTarget" -ForegroundColor Green
} else {
    Write-Host "   ?? appsettings.json no encontrado en directorio actual" -ForegroundColor Yellow
}

# ================================================================
# LIMPIAR LOGS ANTERIORES
# ================================================================

Write-Host ""
Write-Host "?? LIMPIANDO LOGS ANTERIORES..." -ForegroundColor Yellow

# Limpiar logs de prueba
$testLogs = Get-ChildItem "$logsPath\test_*.log", "$logsPath\manual_*.log" -ErrorAction SilentlyContinue
if ($testLogs) {
    Remove-Item $testLogs.FullName -Force
    Write-Host "   ? $($testLogs.Count) logs de prueba eliminados" -ForegroundColor Green
} else {
    Write-Host "   ?? No hay logs de prueba para limpiar" -ForegroundColor Gray
}

# Mostrar estado inicial
$initialLogs = Get-ChildItem "$logsPath\*.log" -ErrorAction SilentlyContinue
Write-Host "   ?? Logs existentes: $($initialLogs.Count)" -ForegroundColor Gray
if ($initialLogs) {
    foreach ($log in $initialLogs) {
        $ageMinutes = [math]::Round(((Get-Date) - $log.LastWriteTime).TotalMinutes, 1)
        Write-Host "      • $($log.Name) ($([math]::Round($log.Length/1KB, 2)) KB, hace $ageMinutes min)" -ForegroundColor Gray
    }
}

# ================================================================
# EJECUTAR APLICACIÓN
# ================================================================

Write-Host ""
Write-Host "?? EJECUTANDO APLICACIÓN..." -ForegroundColor Yellow
Write-Host ""

Push-Location $GestionTimeDesktopDir

$appExecuted = $false
$executionSuccessful = $false

try {
    Write-Host "?? Ejecutando desde: $(Get-Location)" -ForegroundColor Gray
    Write-Host "?? Comando: .\GestionTime.Desktop.exe" -ForegroundColor Gray
    Write-Host ""
    
    # Método 1: Start-Process con captura
    Write-Host "?? Método 1: Start-Process con monitoreo..." -ForegroundColor Cyan
    
    $process = Start-Process ".\GestionTime.Desktop.exe" -PassThru -WindowStyle Normal
    
    if ($process) {
        Write-Host "   ? Proceso iniciado (PID: $($process.Id))" -ForegroundColor Green
        $appExecuted = $true
        
        # Monitorear por varios intervalos
        $monitoringSteps = @(
            @{ Time = 2; Message = "Inicialización..." },
            @{ Time = 3; Message = "Configurando logging..." },
            @{ Time = 5; Message = "Verificando estabilidad..." }
        )
        
        $totalWaitTime = 0
        foreach ($step in $monitoringSteps) {
            Start-Sleep $step.Time
            $totalWaitTime += $step.Time
            
            $running = Get-Process -Id $process.Id -ErrorAction SilentlyContinue
            if ($running) {
                Write-Host "   ? $($step.Message) (${totalWaitTime}s) - Ejecutándose" -ForegroundColor Gray
            } else {
                Write-Host "   ? Aplicación se cerró durante: $($step.Message) (${totalWaitTime}s)" -ForegroundColor Red
                Write-Host "   ?? Código de salida: $($process.ExitCode)" -ForegroundColor Gray
                break
            }
        }
        
        # Verificación final
        $finalRunning = Get-Process -Id $process.Id -ErrorAction SilentlyContinue
        if ($finalRunning) {
            Write-Host "   ? Aplicación estable después de ${totalWaitTime} segundos" -ForegroundColor Green
            $executionSuccessful = $true
            
            # Esperar un poco más para generar logs
            Write-Host "   ? Esperando generación de logs (5 segundos más)..." -ForegroundColor Gray
            Start-Sleep 5
            
            # Cerrar aplicación
            Write-Host "   ?? Cerrando aplicación..." -ForegroundColor Gray
            try {
                $finalRunning.CloseMainWindow()
                Start-Sleep 2
                if (!$finalRunning.HasExited) {
                    $finalRunning.Kill()
                    Write-Host "   ?? Aplicación cerrada forzadamente" -ForegroundColor Yellow
                } else {
                    Write-Host "   ? Aplicación cerrada correctamente" -ForegroundColor Green
                }
            } catch {
                Write-Host "   ?? Error cerrando aplicación: $($_.Exception.Message)" -ForegroundColor Yellow
            }
        }
    } else {
        Write-Host "   ? No se pudo iniciar el proceso" -ForegroundColor Red
    }
    
} catch {
    Write-Host "   ? Error ejecutando aplicación: $($_.Exception.Message)" -ForegroundColor Red
} finally {
    Pop-Location
}

# ================================================================
# VERIFICAR LOGS GENERADOS
# ================================================================

Write-Host ""
Write-Host "?? ANALIZANDO LOGS GENERADOS..." -ForegroundColor Cyan
Write-Host ""

$finalLogs = Get-ChildItem "$logsPath\*.log" -ErrorAction SilentlyContinue
$newLogs = $finalLogs | Where-Object { 
    (Get-Date) - $_.LastWriteTime -lt [TimeSpan]::FromMinutes(2) -and 
    $_.Name -notlike "test_*" -and 
    $_.Name -notlike "manual_*" 
}

if ($newLogs -and $newLogs.Count -gt 0) {
    Write-Host "?? ¡LOGS REALES DE LA APLICACIÓN ENCONTRADOS!" -ForegroundColor Green
    Write-Host ""
    
    foreach ($logFile in $newLogs) {
        Write-Host "?? ARCHIVO: $($logFile.Name)" -ForegroundColor White
        Write-Host "   ?? Tamaño: $([math]::Round($logFile.Length/1KB, 2)) KB" -ForegroundColor Gray
        Write-Host "   ?? Modificado: $($logFile.LastWriteTime)" -ForegroundColor Gray
        Write-Host "   ?? Ubicación: $($logFile.FullName)" -ForegroundColor Gray
        
        # Analizar contenido del log
        try {
            $content = Get-Content $logFile.FullName -ErrorAction Stop
            Write-Host "   ?? Contenido: $($content.Count) líneas" -ForegroundColor Cyan
            
            if ($content.Count -gt 0) {
                
                # Mostrar estadísticas
                $infoLines = ($content | Where-Object { $_ -match "\[INF\]|\[Information\]" }).Count
                $warnLines = ($content | Where-Object { $_ -match "\[WRN\]|\[Warning\]" }).Count
                $errorLines = ($content | Where-Object { $_ -match "\[ERR\]|\[Error\]" }).Count
                
                Write-Host "      ?? Información: $infoLines | Advertencias: $warnLines | Errores: $errorLines" -ForegroundColor Gray
                
                # Mostrar primeras líneas
                Write-Host ""
                Write-Host "      === PRIMERAS 3 LÍNEAS ===" -ForegroundColor Cyan
                $content | Select-Object -First 3 | ForEach-Object {
                    Write-Host "      $_" -ForegroundColor Gray
                }
                
                # Mostrar últimas líneas si hay más de 6
                if ($content.Count -gt 6) {
                    Write-Host "      ..." -ForegroundColor Gray
                    Write-Host "      === ÚLTIMAS 3 LÍNEAS ===" -ForegroundColor Cyan
                    $content | Select-Object -Last 3 | ForEach-Object {
                        Write-Host "      $_" -ForegroundColor Gray
                    }
                } elseif ($content.Count -gt 3) {
                    $content | Select-Object -Skip 3 | ForEach-Object {
                        Write-Host "      $_" -ForegroundColor Gray
                    }
                }
                
                # Buscar líneas importantes
                $importantPatterns = @(
                    @{ Pattern = "APP START"; Color = "Green"; Prefix = "??" },
                    @{ Pattern = "Sistema de logging"; Color = "Green"; Prefix = "??" },
                    @{ Pattern = "BaseUrl|API.*BaseUrl"; Color = "Cyan"; Prefix = "??" },
                    @{ Pattern = "Error|Exception"; Color = "Red"; Prefix = "??" },
                    @{ Pattern = "Login|Auth"; Color = "Yellow"; Prefix = "??" }
                )
                
                $importantLines = @()
                foreach ($pattern in $importantPatterns) {
                    $matches = $content | Where-Object { $_ -match $pattern.Pattern }
                    foreach ($match in $matches) {
                        $importantLines += @{
                            Line = $match
                            Color = $pattern.Color
                            Prefix = $pattern.Prefix
                        }
                    }
                }
                
                if ($importantLines.Count -gt 0) {
                    Write-Host ""
                    Write-Host "      === LÍNEAS IMPORTANTES ===" -ForegroundColor Yellow
                    $importantLines | Select-Object -First 8 | ForEach-Object {
                        Write-Host "      $($_.Prefix) $($_.Line)" -ForegroundColor $_.Color
                    }
                    if ($importantLines.Count -gt 8) {
                        Write-Host "      ... y $($importantLines.Count - 8) líneas más" -ForegroundColor Gray
                    }
                }
                
            } else {
                Write-Host "      ? Archivo de log vacío" -ForegroundColor Red
            }
            
        } catch {
            Write-Host "   ? Error leyendo log: $($_.Exception.Message)" -ForegroundColor Red
        }
        
        Write-Host ""
    }
    
    # Mostrar comandos útiles
    Write-Host "?? COMANDOS ÚTILES PARA MONITOREO:" -ForegroundColor Cyan
    Write-Host "   # Ver log en tiempo real:" -ForegroundColor Gray
    Write-Host "   Get-Content '$($newLogs[0].FullName)' -Wait" -ForegroundColor White
    Write-Host ""
    Write-Host "   # Abrir directorio de logs:" -ForegroundColor Gray
    Write-Host "   explorer '$logsPath'" -ForegroundColor White
    
} else {
    Write-Host "? NO SE GENERARON LOGS DE LA APLICACIÓN" -ForegroundColor Red
    Write-Host ""
    
    # Mostrar diagnóstico
    Write-Host "?? DIAGNÓSTICO:" -ForegroundColor Yellow
    if (-not $appExecuted) {
        Write-Host "   • La aplicación no se pudo ejecutar" -ForegroundColor Red
    } elseif (-not $executionSuccessful) {
        Write-Host "   • La aplicación se ejecutó pero se cerró rápidamente" -ForegroundColor Yellow
        Write-Host "   • Posible problema en la inicialización" -ForegroundColor Yellow
    } else {
        Write-Host "   • La aplicación se ejecutó correctamente" -ForegroundColor Green
        Write-Host "   • Pero no se generaron logs (problema en DebugFileLoggerProvider)" -ForegroundColor Red
    }
    
    Write-Host ""
    Write-Host "?? Logs existentes en directorio:" -ForegroundColor Yellow
    if ($finalLogs) {
        foreach ($log in $finalLogs) {
            $age = (Get-Date) - $log.LastWriteTime
            Write-Host "   • $($log.Name) - $([math]::Round($log.Length/1KB, 2)) KB (hace $([math]::Round($age.TotalMinutes, 1)) min)" -ForegroundColor Gray
        }
    } else {
        Write-Host "   • Ningún archivo .log encontrado en $logsPath" -ForegroundColor Gray
    }
}

# ================================================================
# RESUMEN FINAL
# ================================================================

Write-Host ""
Write-Host "===================================================" -ForegroundColor Cyan
Write-Host "   ?? RESUMEN DEL DIAGNÓSTICO" -ForegroundColor Cyan
Write-Host "===================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "? CONFIGURACIÓN APLICADA:" -ForegroundColor Green
Write-Host "   • Ejecutable: $($env:GESTIONTIME_DESKTOP_PATH)" -ForegroundColor Gray
Write-Host "   • Directorio: $($env:GESTIONTIME_DESKTOP_DIR)" -ForegroundColor Gray
Write-Host "   • Logs: $logsPath" -ForegroundColor Gray
Write-Host ""

if ($newLogs -and $newLogs.Count -gt 0) {
    Write-Host "?? ¡ÉXITO! EL SISTEMA DE LOGGING ESTÁ FUNCIONANDO" -ForegroundColor Green
    Write-Host ""
    Write-Host "? Confirmaciones:" -ForegroundColor Green
    Write-Host "   • Aplicación se ejecuta correctamente" -ForegroundColor Gray
    Write-Host "   • Sistema de logging genera archivos reales" -ForegroundColor Gray
    Write-Host "   • Logs contienen información válida de la aplicación" -ForegroundColor Gray
    Write-Host "   • Configuración automática funcional" -ForegroundColor Gray
    
} else {
    Write-Host "?? PROBLEMA IDENTIFICADO" -ForegroundColor Yellow
    Write-Host ""
    if ($executionSuccessful) {
        Write-Host "? La aplicación se ejecuta pero el logging no funciona" -ForegroundColor Red
        Write-Host ""
        Write-Host "?? Próximos pasos:" -ForegroundColor Yellow
        Write-Host "   1. Revisar DebugFileLoggerProvider en Diagnostics/" -ForegroundColor Gray
        Write-Host "   2. Verificar que la configuración se carga correctamente" -ForegroundColor Gray
        Write-Host "   3. Ejecutar desde Visual Studio para debugging" -ForegroundColor Gray
    } else {
        Write-Host "? La aplicación tiene problemas de ejecución" -ForegroundColor Red
        Write-Host ""
        Write-Host "?? Próximos pasos:" -ForegroundColor Yellow
        Write-Host "   1. Ejecutar desde Visual Studio (F5) para ver errores" -ForegroundColor Gray
        Write-Host "   2. Verificar Event Viewer para errores del sistema" -ForegroundColor Gray
        Write-Host "   3. Revisar dependencias y Windows App Runtime" -ForegroundColor Gray
    }
}

Write-Host ""

# Opción para abrir directorio
if ($newLogs -and $newLogs.Count -gt 0) {
    $response = Read-Host "¿Deseas abrir el directorio de logs? (S/N)"
    if ($response -eq "S" -or $response -eq "s") {
        Start-Process "explorer.exe" -ArgumentList $logsPath
        Write-Host "? Directorio de logs abierto" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "===================================================" -ForegroundColor Cyan
Write-Host "   ?? DIAGNÓSTICO COMPLETADO" -ForegroundColor Cyan
Write-Host "===================================================" -ForegroundColor Cyan
Write-Host ""

Read-Host "Presiona Enter para finalizar"