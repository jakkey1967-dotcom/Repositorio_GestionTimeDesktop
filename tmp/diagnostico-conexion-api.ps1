# ================================================================
# DIAGNÓSTICO RÁPIDO DE CONEXIÓN A LA API
# ================================================================

Write-Host "?? DIAGNÓSTICO DE CONEXIÓN A LA API" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""

# 1. Verificar compilación
Write-Host "?? Verificando compilación..." -ForegroundColor Yellow
$binPath = "bin\Debug\net8.0-windows10.0.19041.0"
$exePath = "$binPath\GestionTime.Desktop.exe"

if (-not (Test-Path $exePath)) {
    Write-Host "? Ejecutable no encontrado. Compilando..." -ForegroundColor Red
    msbuild /p:Configuration=Debug /verbosity:quiet
    if (-not (Test-Path $exePath)) {
        Write-Host "? Error de compilación" -ForegroundColor Red
        exit 1
    }
}
Write-Host "? Ejecutable encontrado" -ForegroundColor Green

# 2. Copiar appsettings.json
Write-Host "?? Copiando configuración..." -ForegroundColor Yellow
Copy-Item "appsettings.json" "$binPath\appsettings.json" -Force
Write-Host "? Configuración copiada" -ForegroundColor Green

# 3. Verificar configuración API
Write-Host "?? Verificando configuración API..." -ForegroundColor Yellow
try {
    $config = Get-Content "appsettings.json" | ConvertFrom-Json
    $baseUrl = $config.Api.BaseUrl
    Write-Host "   • BaseUrl: $baseUrl" -ForegroundColor Gray
    
    # Test de conectividad básica
    Write-Host "   • Probando conectividad..." -ForegroundColor Gray
    try {
        $response = Invoke-WebRequest -Uri $baseUrl -Method HEAD -TimeoutSec 10 -ErrorAction Stop
        Write-Host "? API accesible (Status: $($response.StatusCode))" -ForegroundColor Green
    } catch {
        Write-Host "?? API no accesible: $($_.Exception.Message)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "? Error leyendo configuración: $($_.Exception.Message)" -ForegroundColor Red
}

# 4. Verificar logs para errores
Write-Host ""
Write-Host "?? Verificando logs de errores..." -ForegroundColor Yellow
$logDirs = @("$binPath\logs", "logs", "C:\Logs\GestionTime")

$errorsFound = $false
foreach ($logDir in $logDirs) {
    if (Test-Path $logDir) {
        $logFiles = Get-ChildItem $logDir -Filter "*.log" -ErrorAction SilentlyContinue
        foreach ($logFile in $logFiles) {
            $content = Get-Content $logFile.FullName -ErrorAction SilentlyContinue
            if ($content) {
                $errors = $content | Where-Object { $_ -match "(ERROR|EXCEPTION|FAILED|Error|Exception)" }
                if ($errors) {
                    Write-Host "?? Errores encontrados en $($logFile.Name):" -ForegroundColor Yellow
                    $errors | Select-Object -Last 3 | ForEach-Object {
                        Write-Host "   $_" -ForegroundColor Red
                    }
                    $errorsFound = $true
                }
            }
        }
    }
}

if (-not $errorsFound) {
    Write-Host "? No se encontraron errores en logs" -ForegroundColor Green
}

# 5. Ejecutar aplicación con timeout
Write-Host ""
Write-Host "?? Ejecutando aplicación para test..." -ForegroundColor Yellow

Push-Location $binPath
try {
    $process = Start-Process ".\GestionTime.Desktop.exe" -PassThru -WindowStyle Minimized
    Write-Host "   • Aplicación iniciada (PID: $($process.Id))" -ForegroundColor Gray
    
    # Esperar 5 segundos
    Start-Sleep 5
    
    # Verificar si sigue ejecutándose
    $running = Get-Process -Id $process.Id -ErrorAction SilentlyContinue
    if ($running) {
        Write-Host "? Aplicación ejecutándose correctamente" -ForegroundColor Green
        $running.CloseMainWindow()
        Start-Sleep 2
        if (!$running.HasExited) {
            $running.Kill()
        }
    } else {
        Write-Host "? Aplicación se cerró durante inicialización" -ForegroundColor Red
    }
} catch {
    Write-Host "? Error ejecutando aplicación: $($_.Exception.Message)" -ForegroundColor Red
} finally {
    Pop-Location
}

# 6. Verificar logs generados
Write-Host ""
Write-Host "?? Verificando logs generados..." -ForegroundColor Yellow
$newLogs = $false
foreach ($logDir in $logDirs) {
    if (Test-Path $logDir) {
        $logFiles = Get-ChildItem $logDir -Filter "*.log" -ErrorAction SilentlyContinue | Where-Object { 
            (Get-Date) - $_.LastWriteTime -lt [TimeSpan]::FromMinutes(2) 
        }
        if ($logFiles) {
            Write-Host "? Logs nuevos encontrados en $logDir:" -ForegroundColor Green
            foreach ($file in $logFiles) {
                Write-Host "   • $($file.Name) ($(([math]::Round($file.Length/1KB, 2))) KB)" -ForegroundColor Gray
            }
            $newLogs = $true
            
            # Mostrar últimas líneas relevantes
            $latestLog = $logFiles | Sort-Object LastWriteTime -Descending | Select-Object -First 1
            $lastLines = Get-Content $latestLog.FullName -Tail 5 -ErrorAction SilentlyContinue
            if ($lastLines) {
                Write-Host "   ?? Últimas líneas:" -ForegroundColor Cyan
                foreach ($line in $lastLines) {
                    Write-Host "      $line" -ForegroundColor Gray
                }
            }
        }
    }
}

if (-not $newLogs) {
    Write-Host "? No se generaron logs nuevos" -ForegroundColor Red
}

Write-Host ""
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "   ?? RESUMEN DEL DIAGNÓSTICO" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan

if ($newLogs) {
    Write-Host "? APLICACIÓN INICIANDO CORRECTAMENTE" -ForegroundColor Green
    Write-Host "   • El problema puede estar en la lógica de la aplicación" -ForegroundColor Gray
    Write-Host "   • Revisa los logs para errores específicos de conexión" -ForegroundColor Gray
} else {
    Write-Host "? PROBLEMA DE INICIALIZACIÓN" -ForegroundColor Red
    Write-Host "   • La aplicación no está llegando al sistema de logging" -ForegroundColor Gray
    Write-Host "   • Problema anterior a la conexión con la API" -ForegroundColor Gray
}

Write-Host ""
Read-Host "Presiona Enter para continuar"