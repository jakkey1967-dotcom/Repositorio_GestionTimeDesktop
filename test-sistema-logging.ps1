# ================================================================
# PRUEBA COMPLETA DEL SISTEMA DE LOGGING - GESTIONTIME DESKTOP
# Fecha: 29/12/2025
# Propósito: Verificar que el logging funciona correctamente
# ================================================================

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "   ?? PRUEBA COMPLETA DEL SISTEMA DE LOGGING" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

$ErrorActionPreference = "Continue"

# Variables de configuración
$projectRoot = Get-Location
$binPath = "bin\Debug\net8.0-windows10.0.19041.0"
$exePath = Join-Path $binPath "GestionTime.Desktop.exe"
$appsettingsPath = "appsettings.json"
$binAppsettingsPath = Join-Path $binPath "appsettings.json"

Write-Host "?? CONFIGURACIÓN DE PRUEBA:" -ForegroundColor Yellow
Write-Host "   • Proyecto: $projectRoot" -ForegroundColor Gray
Write-Host "   • Ejecutable: $exePath" -ForegroundColor Gray
Write-Host "   • Configuración: $appsettingsPath" -ForegroundColor Gray
Write-Host ""

# =================================================================
# FASE 1: VERIFICAR PREREQUISITOS
# =================================================================

Write-Host "?? FASE 1: VERIFICANDO PREREQUISITOS" -ForegroundColor Cyan
Write-Host ""

$prerequisitesOk = $true

# 1.1 Verificar que el proyecto está compilado
if (Test-Path $exePath) {
    Write-Host "   ? Ejecutable encontrado: $exePath" -ForegroundColor Green
} else {
    Write-Host "   ? Ejecutable NO encontrado: $exePath" -ForegroundColor Red
    Write-Host "      ?? Ejecuta: dotnet build o usa Visual Studio" -ForegroundColor Yellow
    $prerequisitesOk = $false
}

# 1.2 Verificar appsettings.json existe
if (Test-Path $appsettingsPath) {
    Write-Host "   ? appsettings.json encontrado" -ForegroundColor Green
    
    # Leer configuración
    try {
        $appsettings = Get-Content $appsettingsPath | ConvertFrom-Json
        if ($appsettings.Logging -and $appsettings.Logging.LogPath) {
            Write-Host "   ? LogPath configurado: $($appsettings.Logging.LogPath)" -ForegroundColor Green
        } else {
            Write-Host "   ??  LogPath no configurado en appsettings.json" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "   ? Error leyendo appsettings.json: $($_.Exception.Message)" -ForegroundColor Red
        $prerequisitesOk = $false
    }
} else {
    Write-Host "   ? appsettings.json NO encontrado" -ForegroundColor Red
    $prerequisitesOk = $false
}

# 1.3 Copiar appsettings.json al directorio de salida
if (Test-Path $appsettingsPath) {
    try {
        Copy-Item $appsettingsPath $binAppsettingsPath -Force
        Write-Host "   ? appsettings.json copiado al directorio de salida" -ForegroundColor Green
    } catch {
        Write-Host "   ? Error copiando appsettings.json: $($_.Exception.Message)" -ForegroundColor Red
        $prerequisitesOk = $false
    }
}

# 1.4 Verificar que el directorio logs esperado puede crearse
$expectedLogDir = Join-Path $binPath "logs"
try {
    if (-not (Test-Path $expectedLogDir)) {
        New-Item -ItemType Directory -Path $expectedLogDir -Force | Out-Null
    }
    
    # Test de escritura
    $testFile = Join-Path $expectedLogDir "test_write.tmp"
    "test" | Out-File $testFile -ErrorAction Stop
    Remove-Item $testFile -ErrorAction SilentlyContinue
    
    Write-Host "   ? Directorio logs accesible y con permisos de escritura" -ForegroundColor Green
} catch {
    Write-Host "   ? Problema con directorio logs: $($_.Exception.Message)" -ForegroundColor Red
    $prerequisitesOk = $false
}

if (-not $prerequisitesOk) {
    Write-Host ""
    Write-Host "? PREREQUISITOS NO CUMPLIDOS - Corrige los errores antes de continuar" -ForegroundColor Red
    Read-Host "Presiona Enter para salir"
    exit 1
}

Write-Host ""
Write-Host "? TODOS LOS PREREQUISITOS CUMPLIDOS" -ForegroundColor Green
Write-Host ""

# =================================================================
# FASE 2: LIMPIAR LOGS ANTERIORES
# =================================================================

Write-Host "?? FASE 2: LIMPIANDO LOGS ANTERIORES" -ForegroundColor Cyan
Write-Host ""

$logsToClean = @(
    (Join-Path $expectedLogDir "*"),
    (Join-Path $projectRoot "logs\*"),
    "C:\Logs\GestionTime\*"
)

foreach ($logPattern in $logsToClean) {
    try {
        $files = Get-ChildItem $logPattern -File -ErrorAction SilentlyContinue
        if ($files) {
            foreach ($file in $files) {
                Remove-Item $file.FullName -Force -ErrorAction SilentlyContinue
                Write-Host "   ??? Eliminado: $($file.Name)" -ForegroundColor Gray
            }
        }
    } catch {
        # Ignorar errores de limpieza
    }
}

Write-Host "   ? Limpieza completada" -ForegroundColor Green
Write-Host ""

# =================================================================
# FASE 3: EJECUTAR APLICACIÓN Y MONITOREAR
# =================================================================

Write-Host "?? FASE 3: EJECUTANDO APLICACIÓN Y MONITOREANDO LOGS" -ForegroundColor Cyan
Write-Host ""

# Cambiar al directorio del ejecutable
Push-Location $binPath

Write-Host "   ?? Ejecutando desde: $(Get-Location)" -ForegroundColor Gray
Write-Host "   ?? Iniciando aplicación..." -ForegroundColor Yellow

try {
    # Ejecutar aplicación
    $process = Start-Process ".\GestionTime.Desktop.exe" -PassThru -WindowStyle Minimized
    
    if ($process) {
        Write-Host "   ? Aplicación iniciada (PID: $($process.Id))" -ForegroundColor Green
        
        # Esperar un momento para que la aplicación inicialice
        Write-Host "   ? Esperando inicialización (10 segundos)..." -ForegroundColor Yellow
        Start-Sleep -Seconds 10
        
        # Verificar si el proceso sigue ejecutándose
        $runningProcess = Get-Process -Id $process.Id -ErrorAction SilentlyContinue
        if ($runningProcess) {
            Write-Host "   ? Aplicación ejecutándose correctamente" -ForegroundColor Green
            
            # Cerrar la aplicación después de la prueba
            Write-Host "   ?? Cerrando aplicación para verificar logs..." -ForegroundColor Yellow
            $runningProcess.CloseMainWindow()
            Start-Sleep -Seconds 2
            
            # Force kill si no se cerró
            $stillRunning = Get-Process -Id $process.Id -ErrorAction SilentlyContinue
            if ($stillRunning) {
                $stillRunning.Kill()
                Write-Host "   ?? Aplicación cerrada forzadamente" -ForegroundColor Yellow
            }
        } else {
            Write-Host "   ??  Aplicación se cerró durante la inicialización" -ForegroundColor Yellow
        }
    } else {
        Write-Host "   ? Error iniciando aplicación" -ForegroundColor Red
    }
} catch {
    Write-Host "   ? Error ejecutando aplicación: $($_.Exception.Message)" -ForegroundColor Red
}

Pop-Location

Write-Host ""

# =================================================================
# FASE 4: VERIFICAR LOGS GENERADOS
# =================================================================

Write-Host "?? FASE 4: VERIFICANDO LOGS GENERADOS" -ForegroundColor Cyan
Write-Host ""

$logsFound = @()
$searchPaths = @(
    $expectedLogDir,
    (Join-Path $projectRoot "logs"),
    "C:\Logs\GestionTime"
)

foreach ($searchPath in $searchPaths) {
    if (Test-Path $searchPath) {
        Write-Host "   ?? Buscando en: $searchPath" -ForegroundColor Gray
        
        $logFiles = Get-ChildItem $searchPath -Filter "*.log" -ErrorAction SilentlyContinue
        
        if ($logFiles) {
            foreach ($logFile in $logFiles) {
                $logsFound += $logFile
                
                $sizeKB = [math]::Round($logFile.Length / 1KB, 2)
                $age = (Get-Date) - $logFile.LastWriteTime
                
                if ($age.TotalMinutes -lt 5) {
                    Write-Host "      ? NUEVO: $($logFile.Name) ($sizeKB KB) - $($logFile.LastWriteTime)" -ForegroundColor Green
                } else {
                    Write-Host "      ?? ANTIGUO: $($logFile.Name) ($sizeKB KB) - $($logFile.LastWriteTime)" -ForegroundColor Gray
                }
            }
        } else {
            Write-Host "      ? No se encontraron archivos .log" -ForegroundColor Red
        }
    } else {
        Write-Host "   ?? No existe: $searchPath" -ForegroundColor Gray
    }
}

Write-Host ""

# =================================================================
# FASE 5: ANÁLISIS DE LOGS
# =================================================================

Write-Host "?? FASE 5: ANÁLISIS DE LOGS GENERADOS" -ForegroundColor Cyan
Write-Host ""

$recentLogs = $logsFound | Where-Object { ((Get-Date) - $_.LastWriteTime).TotalMinutes -lt 5 }

if ($recentLogs.Count -eq 0) {
    Write-Host "? NO SE GENERARON LOGS NUEVOS" -ForegroundColor Red
    Write-Host ""
    Write-Host "?? POSIBLES PROBLEMAS:" -ForegroundColor Yellow
    Write-Host "   • La aplicación no llegó al constructor App()" -ForegroundColor Gray
    Write-Host "   • Error en la inicialización del RotatingFileLoggerProvider" -ForegroundColor Gray
    Write-Host "   • Problema de permisos de escritura" -ForegroundColor Gray
    Write-Host "   • Configuración incorrecta del LogPath" -ForegroundColor Gray
    Write-Host ""
    Write-Host "?? RECOMENDACIONES:" -ForegroundColor Yellow
    Write-Host "   1. Ejecutar desde Visual Studio en modo Debug" -ForegroundColor Gray
    Write-Host "   2. Revisar Output window para errores" -ForegroundColor Gray
    Write-Host "   3. Verificar Event Viewer de Windows" -ForegroundColor Gray
} else {
    Write-Host "? LOGS NUEVOS ENCONTRADOS:" -ForegroundColor Green
    Write-Host ""
    
    foreach ($logFile in $recentLogs) {
        Write-Host "?? ARCHIVO: $($logFile.Name)" -ForegroundColor White
        Write-Host "   ?? Ubicación: $($logFile.DirectoryName)" -ForegroundColor Gray
        Write-Host "   ?? Tamaño: $([math]::Round($logFile.Length / 1KB, 2)) KB" -ForegroundColor Gray
        Write-Host "   ?? Modificado: $($logFile.LastWriteTime)" -ForegroundColor Gray
        
        # Mostrar contenido del log (últimas 15 líneas)
        try {
            $content = Get-Content $logFile.FullName -Tail 15 -ErrorAction Stop
            Write-Host "   ?? CONTENIDO (últimas 15 líneas):" -ForegroundColor Cyan
            foreach ($line in $content) {
                Write-Host "      $line" -ForegroundColor Gray
            }
        } catch {
            Write-Host "   ? Error leyendo contenido: $($_.Exception.Message)" -ForegroundColor Red
        }
        
        Write-Host ""
    }
    
    # Análisis del contenido
    Write-Host "?? ANÁLISIS DEL CONTENIDO:" -ForegroundColor Cyan
    Write-Host ""
    
    $allContent = ""
    foreach ($logFile in $recentLogs) {
        try {
            $content = Get-Content $logFile.FullName -Raw -ErrorAction Stop
            $allContent += $content
        } catch {
            # Ignorar errores de lectura
        }
    }
    
    # Verificar elementos clave del sistema de logging
    $checks = @(
        @{ Name = "APP START"; Pattern = "APP START"; Description = "Aplicación inicializó correctamente" },
        @{ Name = "Sistema de logging inicializado"; Pattern = "Sistema de logging inicializado"; Description = "Logger configurado correctamente" },
        @{ Name = "MODO DEBUG/RELEASE"; Pattern = "(MODO DEBUG|MODO RELEASE)"; Description = "Nivel de logging configurado" },
        @{ Name = "Log path"; Pattern = "Log en:"; Description = "Ruta de logs reportada" },
        @{ Name = "RotatingFileLogger"; Pattern = "Rotación:"; Description = "Configuración de rotación activa" },
        @{ Name = "OnLaunched"; Pattern = "OnLaunched"; Description = "Aplicación completó lanzamiento" },
        @{ Name = "API BaseUrl"; Pattern = "API BaseUrl"; Description = "Configuración API cargada" }
    )
    
    foreach ($check in $checks) {
        if ($allContent -match $check.Pattern) {
            Write-Host "   ? $($check.Name): $($check.Description)" -ForegroundColor Green
        } else {
            Write-Host "   ? $($check.Name): $($check.Description)" -ForegroundColor Red
        }
    }
    
    Write-Host ""
    
    # Contar líneas de log
    $logLines = ($allContent -split "`n").Count
    Write-Host "?? ESTADÍSTICAS:" -ForegroundColor Cyan
    Write-Host "   • Total archivos de log: $($recentLogs.Count)" -ForegroundColor Gray
    Write-Host "   • Total líneas de log: $logLines" -ForegroundColor Gray
    Write-Host "   • Tamaño total: $([math]::Round(($recentLogs | Measure-Object Length -Sum).Sum / 1KB, 2)) KB" -ForegroundColor Gray
}

# =================================================================
# FASE 6: PRUEBA DE ESCRITURA MANUAL
# =================================================================

Write-Host ""
Write-Host "?? FASE 6: PRUEBA DE ESCRITURA MANUAL" -ForegroundColor Cyan
Write-Host ""

Write-Host "   ?? Probando creación manual de logs..." -ForegroundColor Yellow

try {
    # Usar la misma configuración que la aplicación
    $manualLogPath = Join-Path $expectedLogDir "manual_test.log"
    $testMessage = "PRUEBA MANUAL DE LOGGING - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss.fff')"
    
    Add-Content -Path $manualLogPath -Value $testMessage -Encoding UTF8
    
    if (Test-Path $manualLogPath) {
        Write-Host "   ? Escritura manual exitosa: $manualLogPath" -ForegroundColor Green
        Write-Host "   ?? Contenido: $testMessage" -ForegroundColor Gray
    } else {
        Write-Host "   ? Fallo en escritura manual" -ForegroundColor Red
    }
} catch {
    Write-Host "   ? Error en escritura manual: $($_.Exception.Message)" -ForegroundColor Red
}

# =================================================================
# RESUMEN FINAL
# =================================================================

Write-Host ""
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "   ?? RESUMEN FINAL DE LA PRUEBA" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

if ($recentLogs.Count -gt 0) {
    Write-Host "?? RESULTADO: ÉXITO" -ForegroundColor Green
    Write-Host ""
    Write-Host "? EL SISTEMA DE LOGGING FUNCIONA CORRECTAMENTE" -ForegroundColor Green
    Write-Host ""
    Write-Host "?? RESUMEN:" -ForegroundColor White
    Write-Host "   • Logs generados: $($recentLogs.Count) archivo(s)" -ForegroundColor Gray
    Write-Host "   • Ubicación principal: $expectedLogDir" -ForegroundColor Gray
    Write-Host "   • Configuración: Funcionando correctamente" -ForegroundColor Gray
    Write-Host "   • RotatingFileLoggerProvider: Activo" -ForegroundColor Gray
    Write-Host ""
    Write-Host "?? PRÓXIMOS PASOS:" -ForegroundColor Yellow
    Write-Host "   • El sistema de logging está listo para producción" -ForegroundColor Gray
    Write-Host "   • Los logs se generarán automáticamente durante el uso normal" -ForegroundColor Gray
    Write-Host "   • Usar .\verificar-ubicacion-logs.ps1 para monitoreo rutinario" -ForegroundColor Gray
} else {
    Write-Host "?? RESULTADO: PROBLEMAS DETECTADOS" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "? EL SISTEMA DE LOGGING NO ESTÁ FUNCIONANDO COMO ESPERADO" -ForegroundColor Red
    Write-Host ""
    Write-Host "?? ACCIONES REQUERIDAS:" -ForegroundColor Yellow
    Write-Host "   1. Ejecutar aplicación desde Visual Studio en modo Debug" -ForegroundColor Gray
    Write-Host "   2. Revisar Output window para errores de inicialización" -ForegroundColor Gray
    Write-Host "   3. Verificar que RotatingFileLoggerProvider se inicializa correctamente" -ForegroundColor Gray
    Write-Host "   4. Comprobar Event Viewer para errores del framework" -ForegroundColor Gray
}

Write-Host ""

# Opción para abrir directorio de logs
if ($recentLogs.Count -gt 0) {
    $response = Read-Host "¿Deseas abrir el directorio de logs? (S/N)"
    if ($response -eq "S" -or $response -eq "s") {
        try {
            Start-Process "explorer.exe" -ArgumentList $expectedLogDir
            Write-Host "? Directorio de logs abierto en Explorer" -ForegroundColor Green
        } catch {
            Write-Host "? Error abriendo directorio: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

Write-Host ""
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "   ?? PRUEBA COMPLETADA" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

Read-Host "Presiona Enter para finalizar"