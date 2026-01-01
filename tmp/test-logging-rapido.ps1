# ================================================================
# PRUEBA RÁPIDA DEL SISTEMA DE LOGGING
# ================================================================

Write-Host "?? PRUEBA RÁPIDA DEL SISTEMA DE LOGGING" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host ""

# Compilar proyecto
Write-Host "?? Compilando proyecto..." -ForegroundColor Yellow
dotnet build --configuration Debug 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "? Error compilando proyecto" -ForegroundColor Red
    exit 1
}
Write-Host "? Proyecto compilado" -ForegroundColor Green

# Copiar appsettings.json
$binPath = "bin\Debug\net8.0-windows10.0.19041.0"
Copy-Item "appsettings.json" "$binPath\appsettings.json" -Force

# Limpiar logs anteriores
$logDir = "$binPath\logs"
if (Test-Path $logDir) {
    Remove-Item "$logDir\*" -Force -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "?? Ejecutando aplicación (modo DEBUG con pruebas automáticas)..." -ForegroundColor Yellow

# Ejecutar aplicación
Push-Location $binPath
$process = Start-Process ".\GestionTime.Desktop.exe" -PassThru -WindowStyle Minimized

# Esperar inicialización
Start-Sleep 8

# Verificar logs
Pop-Location
if (Test-Path "$logDir\*.log") {
    $logFiles = Get-ChildItem "$logDir\*.log"
    Write-Host ""
    Write-Host "? LOGS GENERADOS:" -ForegroundColor Green
    foreach ($file in $logFiles) {
        Write-Host "   ?? $($file.Name) - $([math]::Round($file.Length/1KB, 2)) KB" -ForegroundColor Gray
    }
    
    # Mostrar últimas líneas
    $latestLog = $logFiles | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    Write-Host ""
    Write-Host "?? ÚLTIMAS LÍNEAS DEL LOG:" -ForegroundColor Cyan
    Get-Content $latestLog.FullName -Tail 10 | ForEach-Object { Write-Host "   $_" -ForegroundColor Gray }
    
    Write-Host ""
    Write-Host "?? SISTEMA DE LOGGING FUNCIONANDO CORRECTAMENTE" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "? NO SE GENERARON LOGS" -ForegroundColor Red
    Write-Host "   Ejecuta .\test-sistema-logging.ps1 para diagnóstico completo" -ForegroundColor Yellow
}

# Cerrar aplicación
if ($process -and !$process.HasExited) {
    $process.CloseMainWindow()
    Start-Sleep 2
    if (!$process.HasExited) {
        $process.Kill()
    }
}

Write-Host ""