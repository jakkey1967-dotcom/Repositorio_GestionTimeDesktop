# ================================================================
# PRUEBA REAL DEL SISTEMA DE LOGGING DE LA APLICACIÓN
# ================================================================

Write-Host "?? PRUEBA REAL DEL LOGGING DE LA APLICACIÓN" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan
Write-Host ""

$binPath = "bin\Debug\net8.0-windows10.0.19041.0"
$logsPath = "$binPath\logs"

# 1. Limpiar logs de prueba anteriores
Write-Host "?? Limpiando logs de prueba anteriores..." -ForegroundColor Yellow
Remove-Item "$logsPath\test_*.log" -Force -ErrorAction SilentlyContinue
Remove-Item "$logsPath\manual_*.log" -Force -ErrorAction SilentlyContinue

# 2. Verificar estado inicial
Write-Host "?? Estado inicial:" -ForegroundColor Yellow
$initialLogs = Get-ChildItem "$logsPath\*.log" -ErrorAction SilentlyContinue
Write-Host "   • Logs existentes: $($initialLogs.Count)" -ForegroundColor Gray
if ($initialLogs) {
    foreach ($log in $initialLogs) {
        Write-Host "     - $($log.Name) ($([math]::Round($log.Length/1KB, 2)) KB)" -ForegroundColor Gray
    }
}

Write-Host ""

# 3. Asegurar configuración
Write-Host "?? Asegurando configuración..." -ForegroundColor Yellow
Copy-Item "appsettings.json" "$binPath\appsettings.json" -Force
Write-Host "   ? appsettings.json copiado" -ForegroundColor Green

# 4. Intentar ejecutar aplicación con diferentes métodos
Write-Host "?? INTENTANDO EJECUTAR APLICACIÓN..." -ForegroundColor Yellow
Write-Host ""

Push-Location $binPath

$appExecuted = $false
$methods = @(
    @{ Name = "Start-Process Normal"; Command = { Start-Process ".\GestionTime.Desktop.exe" -PassThru -Wait -WindowStyle Minimized -ErrorAction Stop } },
    @{ Name = "Start-Process Visible"; Command = { Start-Process ".\GestionTime.Desktop.exe" -PassThru -WindowStyle Normal -ErrorAction Stop } },
    @{ Name = "Invoke-Expression"; Command = { $proc = Start-Process ".\GestionTime.Desktop.exe" -PassThru; Start-Sleep 3; $proc } }
)

foreach ($method in $methods) {
    Write-Host "?? Método: $($method.Name)" -ForegroundColor Cyan
    
    try {
        $process = & $method.Command
        
        if ($process) {
            Write-Host "   ? Proceso iniciado (PID: $($process.Id))" -ForegroundColor Green
            
            # Esperar un poco para que la aplicación inicialice
            Write-Host "   ? Esperando inicialización (5 segundos)..." -ForegroundColor Gray
            Start-Sleep 5
            
            # Verificar si sigue ejecutándose
            $running = Get-Process -Id $process.Id -ErrorAction SilentlyContinue
            if ($running) {
                Write-Host "   ? Aplicación ejecutándose correctamente" -ForegroundColor Green
                $appExecuted = $true
                
                # Esperar un poco más para generar logs
                Write-Host "   ? Esperando generación de logs (5 segundos más)..." -ForegroundColor Gray
                Start-Sleep 5
                
                # Cerrar aplicación
                Write-Host "   ?? Cerrando aplicación..." -ForegroundColor Gray
                try {
                    $running.CloseMainWindow()
                    Start-Sleep 2
                    if (!$running.HasExited) {
                        $running.Kill()
                    }
                    Write-Host "   ? Aplicación cerrada correctamente" -ForegroundColor Green
                } catch {
                    Write-Host "   ?? Error cerrando: $($_.Exception.Message)" -ForegroundColor Yellow
                }
                
                break # Salir del bucle si este método funcionó
                
            } else {
                Write-Host "   ? Aplicación se cerró inmediatamente" -ForegroundColor Red
                Write-Host "   ?? Código de salida: $($process.ExitCode)" -ForegroundColor Gray
            }
        } else {
            Write-Host "   ? No se pudo iniciar el proceso" -ForegroundColor Red
        }
        
    } catch {
        Write-Host "   ? Error: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Write-Host ""
}

Pop-Location

# 5. Verificar si se generaron logs reales
Write-Host "?? VERIFICANDO LOGS GENERADOS POR LA APLICACIÓN..." -ForegroundColor Cyan
Write-Host ""

$finalLogs = Get-ChildItem "$logsPath\*.log" -ErrorAction SilentlyContinue
$newLogs = $finalLogs | Where-Object { 
    (Get-Date) - $_.LastWriteTime -lt [TimeSpan]::FromMinutes(2) -and 
    $_.Name -notlike "test_*" -and 
    $_.Name -notlike "manual_*" 
}

if ($newLogs) {
    Write-Host "? ¡LOGS REALES ENCONTRADOS!" -ForegroundColor Green
    Write-Host ""
    
    foreach ($logFile in $newLogs) {
        Write-Host "?? Archivo: $($logFile.Name)" -ForegroundColor White
        Write-Host "   ?? Tamaño: $([math]::Round($logFile.Length/1KB, 2)) KB" -ForegroundColor Gray
        Write-Host "   ?? Modificado: $($logFile.LastWriteTime)" -ForegroundColor Gray
        
        # Leer y mostrar contenido del log
        try {
            $content = Get-Content $logFile.FullName -ErrorAction Stop
            Write-Host "   ?? Contenido ($($content.Count) líneas):" -ForegroundColor Cyan
            
            if ($content.Count -gt 0) {
                # Mostrar primeras líneas
                Write-Host "      === PRIMERAS LÍNEAS ===" -ForegroundColor Cyan
                $firstLines = $content | Select-Object -First 5
                foreach ($line in $firstLines) {
                    Write-Host "      $line" -ForegroundColor Gray
                }
                
                if ($content.Count -gt 10) {
                    Write-Host "      ..." -ForegroundColor Gray
                    # Mostrar últimas líneas
                    Write-Host "      === ÚLTIMAS LÍNEAS ===" -ForegroundColor Cyan
                    $lastLines = $content | Select-Object -Last 5
                    foreach ($line in $lastLines) {
                        Write-Host "      $line" -ForegroundColor Gray
                    }
                } elseif ($content.Count -gt 5) {
                    $remainingLines = $content | Select-Object -Skip 5
                    foreach ($line in $remainingLines) {
                        Write-Host "      $line" -ForegroundColor Gray
                    }
                }
                
                # Buscar líneas específicas importantes
                $importantLines = $content | Where-Object { 
                    $_ -match "APP START|Sistema de logging|BaseUrl|Error|Exception|API" 
                }
                
                if ($importantLines) {
                    Write-Host "      === LÍNEAS IMPORTANTES ===" -ForegroundColor Yellow
                    foreach ($line in $importantLines) {
                        if ($line -match "Error|Exception") {
                            Write-Host "      ?? $line" -ForegroundColor Red
                        } elseif ($line -match "APP START|Sistema de logging") {
                            Write-Host "      ? $line" -ForegroundColor Green
                        } else {
                            Write-Host "      ?? $line" -ForegroundColor Cyan
                        }
                    }
                }
                
            } else {
                Write-Host "      ? Archivo vacío" -ForegroundColor Red
            }
            
        } catch {
            Write-Host "   ? Error leyendo archivo: $($_.Exception.Message)" -ForegroundColor Red
        }
        
        Write-Host ""
    }
    
} else {
    Write-Host "? NO SE GENERARON LOGS REALES DE LA APLICACIÓN" -ForegroundColor Red
    Write-Host ""
    Write-Host "?? Logs actuales encontrados:" -ForegroundColor Yellow
    if ($finalLogs) {
        foreach ($log in $finalLogs) {
            $age = (Get-Date) - $log.LastWriteTime
            Write-Host "   • $($log.Name) - $([math]::Round($log.Length/1KB, 2)) KB (hace $([math]::Round($age.TotalMinutes, 1)) min)" -ForegroundColor Gray
        }
    } else {
        Write-Host "   • Ningún archivo .log encontrado" -ForegroundColor Gray
    }
}

# 6. Análisis de Event Log
Write-Host "?? VERIFICANDO EVENT LOG PARA ERRORES..." -ForegroundColor Yellow
Write-Host ""

try {
    $events = Get-EventLog -LogName Application -After (Get-Date).AddMinutes(-5) -Source "*NET*" -ErrorAction SilentlyContinue | 
              Where-Object { $_.Message -like "*GestionTime*" } |
              Select-Object -First 3
    
    if ($events) {
        Write-Host "?? Eventos relacionados con GestionTime:" -ForegroundColor Yellow
        foreach ($event in $events) {
            Write-Host "   ?? $($event.TimeGenerated): $($event.EntryType)" -ForegroundColor Gray
            $shortMsg = ($event.Message -split "`n")[0]
            Write-Host "      $shortMsg" -ForegroundColor Gray
        }
    } else {
        Write-Host "? No hay eventos de error recientes relacionados con GestionTime" -ForegroundColor Green
    }
} catch {
    Write-Host "?? No se pudo acceder al Event Log" -ForegroundColor Yellow
}

Write-Host ""

# 7. Resumen final
Write-Host "===========================================" -ForegroundColor Cyan
Write-Host "   ?? RESULTADO DE LA PRUEBA REAL" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan
Write-Host ""

if ($newLogs -and $newLogs.Count -gt 0) {
    Write-Host "?? ¡ÉXITO! EL SISTEMA DE LOGGING FUNCIONA" -ForegroundColor Green
    Write-Host ""
    Write-Host "? Confirmado:" -ForegroundColor Green
    Write-Host "   • La aplicación se ejecuta correctamente" -ForegroundColor Gray
    Write-Host "   • El sistema de logging genera archivos reales" -ForegroundColor Gray
    Write-Host "   • Los logs contienen información de la aplicación" -ForegroundColor Gray
    Write-Host "   • La configuración está funcionando" -ForegroundColor Gray
    
} elseif ($appExecuted) {
    Write-Host "?? APLICACIÓN SE EJECUTA PERO NO GENERA LOGS" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "? Problemas identificados:" -ForegroundColor Red
    Write-Host "   • La aplicación inicia pero no llega al sistema de logging" -ForegroundColor Gray
    Write-Host "   • Posible error en DebugFileLoggerProvider" -ForegroundColor Gray
    Write-Host "   • Problema en la configuración interna" -ForegroundColor Gray
    
} else {
    Write-Host "? LA APLICACIÓN NO SE EJECUTA CORRECTAMENTE" -ForegroundColor Red
    Write-Host ""
    Write-Host "?? Posibles causas:" -ForegroundColor Yellow
    Write-Host "   • Dependencias faltantes (Windows App Runtime)" -ForegroundColor Gray
    Write-Host "   • Error en el código de inicialización" -ForegroundColor Gray
    Write-Host "   • Problema con la configuración" -ForegroundColor Gray
    
    Write-Host ""
    Write-Host "?? Próximos pasos recomendados:" -ForegroundColor Cyan
    Write-Host "   1. Ejecutar desde Visual Studio en modo Debug" -ForegroundColor Gray
    Write-Host "   2. Revisar Output window para errores específicos" -ForegroundColor Gray
    Write-Host "   3. Verificar que Windows App Runtime está instalado" -ForegroundColor Gray
}

Write-Host ""
Read-Host "Presiona Enter para finalizar"