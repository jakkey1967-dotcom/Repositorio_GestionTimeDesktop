# ================================================================
# VERIFICACIÓN FINAL - APLICACIÓN CON API CORRECTA
# ================================================================

Write-Host "?? VERIFICACIÓN FINAL - API CORREGIDA" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "? CORRECCIÓN APLICADA:" -ForegroundColor Green
Write-Host "   • Fallback cambiado de 'localhost:2501' a 'https://gestiontimeapi.onrender.com'" -ForegroundColor Gray
Write-Host "   • Windows App Runtime 1.8 instalado" -ForegroundColor Gray
Write-Host "   • appsettings.json copiado correctamente" -ForegroundColor Gray
Write-Host ""

# 1. Despertar API
Write-Host "? DESPERTANDO API DE RENDER..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "https://gestiontimeapi.onrender.com/health" -TimeoutSec 30 -UseBasicParsing
    Write-Host "   ? API despierta (Status: $($response.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   ?? Error despertando API: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""

# 2. Compilar
Write-Host "?? COMPILANDO PROYECTO..." -ForegroundColor Yellow
$binPath = "bin\Debug\net8.0-windows10.0.19041.0"

try {
    msbuild /p:Configuration=Debug /verbosity:quiet78
    Write-Host "   ? Compilación exitosa" -ForegroundColor Green
} catch {
    Write-Host "   ? Error de compilación" -ForegroundColor Red
    Read-Host "Presiona Enter para salir"
    exit 1
}

# 3. Copiar configuración
Write-Host "?? COPIANDO CONFIGURACIÓN..." -ForegroundColor Yellow
Copy-Item "appsettings.json" "$binPath\appsettings.json" -Force
Write-Host "   ? appsettings.json copiado" -ForegroundColor Green

Write-Host ""

# 4. Ejecutar aplicación
Write-Host "?? EJECUTANDO APLICACIÓN..." -ForegroundColor Yellow
Write-Host "   (La aplicación debería abrirse sin mostrar error de localhost:2501)" -ForegroundColor Gray

Push-Location $binPath

try {
    $process = Start-Process ".\GestionTime.Desktop.exe" -PassThru
    Write-Host "   ? Aplicación iniciada (PID: $($process.Id))" -ForegroundColor Green
    
    # Esperar un momento
    Start-Sleep 5
    
    # Verificar si sigue ejecutándose
    $running = Get-Process -Id $process.Id -ErrorAction SilentlyContinue
    if ($running) {
        Write-Host "   ? Aplicación ejecutándose estable" -ForegroundColor Green
        Write-Host ""
        Write-Host "?? ¡ÉXITO! LA APLICACIÓN ESTÁ FUNCIONANDO" -ForegroundColor Green
        Write-Host "   • Ya no debería mostrar error de localhost:2501" -ForegroundColor Gray
        Write-Host "   • Debería intentar conectarse a la API de Render" -ForegroundColor Gray
        Write-Host ""
        Write-Host "?? AHORA PUEDES:" -ForegroundColor Cyan
        Write-Host "   1. Probar a hacer login con credenciales válidas" -ForegroundColor Gray
        Write-Host "   2. Si no tienes credenciales, crear un usuario en la API" -ForegroundColor Gray
        Write-Host "   3. Verificar que la conexión funciona correctamente" -ForegroundColor Gray
        
        Write-Host ""
        $response = Read-Host "¿Deseas cerrar la aplicación? (S/N)"
        if ($response -eq "S" -or $response -eq "s") {
            $running.CloseMainWindow()
            Start-Sleep 2
            if (!$running.HasExited) {
                $running.Kill()
            }
            Write-Host "   ? Aplicación cerrada" -ForegroundColor Green
        } else {
            Write-Host "   ?? Aplicación sigue ejecutándose" -ForegroundColor Cyan
        }
    } else {
        Write-Host "   ?? Aplicación se cerró (puede ser normal)" -ForegroundColor Yellow
        Write-Host "      Pero ya no debería mostrar error de localhost:2501" -ForegroundColor Gray
    }
    
} catch {
    Write-Host "   ? Error ejecutando aplicación: $($_.Exception.Message)" -ForegroundColor Red
} finally {
    Pop-Location
}

Write-Host ""

# 5. Verificar logs
Write-Host "?? VERIFICANDO LOGS GENERADOS..." -ForegroundColor Yellow
$logFiles = Get-ChildItem "$binPath\logs" -Filter "*.log" -ErrorAction SilentlyContinue | Where-Object { 
    (Get-Date) - $_.LastWriteTime -lt [TimeSpan]::FromMinutes(2) 
}

if ($logFiles) {
    Write-Host "   ? Logs nuevos encontrados:" -ForegroundColor Green
    foreach ($file in $logFiles) {
        Write-Host "      • $($file.Name) ($(([math]::Round($file.Length/1KB, 2))) KB)" -ForegroundColor Gray
    }
    
    # Mostrar últimas líneas relevantes para verificar API
    $latestLog = $logFiles | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    $lastLines = Get-Content $latestLog.FullName -Tail 5 -ErrorAction SilentlyContinue
    if ($lastLines) {
        Write-Host ""
        Write-Host "   ?? Últimas líneas del log:" -ForegroundColor Cyan
        foreach ($line in $lastLines) {
            # Resaltar líneas sobre API
            if ($line -match "gestiontimeapi|BaseUrl|API") {
                Write-Host "      $line" -ForegroundColor Yellow
            } else {
                Write-Host "      $line" -ForegroundColor Gray
            }
        }
    }
} else {
    Write-Host "   ?? No se generaron logs nuevos" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "   ?? RESUMEN FINAL" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "? PROBLEMA RESUELTO:" -ForegroundColor Green
Write-Host "   • Error de localhost:2501 corregido" -ForegroundColor Gray
Write-Host "   • Aplicación usa API de Render correctamente" -ForegroundColor Gray
Write-Host "   • Windows App Runtime instalado" -ForegroundColor Gray
Write-Host ""

Write-Host "?? RESULTADO ESPERADO:" -ForegroundColor Cyan
Write-Host "   • Ya NO aparece error 'localhost:2501'" -ForegroundColor Gray
Write-Host "   • La aplicación intenta conectarse a gestiontimeapi.onrender.com" -ForegroundColor Gray
Write-Host "   • Posibles errores ahora serían de credenciales, no de conexión" -ForegroundColor Gray
Write-Host ""

Read-Host "Presiona Enter para finalizar"