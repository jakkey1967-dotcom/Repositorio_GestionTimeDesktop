# ================================================================
# PRUEBA FINAL - URLs HARDCODED
# ================================================================

Write-Host "?? PRUEBA FINAL CON URLs HARDCODED" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""

# 1. Despertar API
Write-Host "? DESPERTANDO API..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "https://gestiontimeapi.onrender.com/health" -TimeoutSec 20 -UseBasicParsing
    Write-Host "   ? API despierta (Status: $($response.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   ?? API no responde: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""

# 2. Verificar archivos
Write-Host "?? VERIFICANDO ARCHIVOS..." -ForegroundColor Yellow
$binPath = "bin\Debug\net8.0-windows10.0.19041.0"

$files = @(
    @{ Path = "$binPath\GestionTime.Desktop.exe"; Name = "Ejecutable" },
    @{ Path = "$binPath\appsettings.json"; Name = "Configuración" }
)

foreach ($file in $files) {
    if (Test-Path $file.Path) {
        Write-Host "   ? $($file.Name): OK" -ForegroundColor Green
    } else {
        Write-Host "   ? $($file.Name): NO ENCONTRADO" -ForegroundColor Red
    }
}

Write-Host ""

# 3. Verificar configuración
if (Test-Path "$binPath\appsettings.json") {
    $config = Get-Content "$binPath\appsettings.json" | ConvertFrom-Json
    Write-Host "?? CONFIGURACIÓN ACTUAL:" -ForegroundColor Yellow
    Write-Host "   • BaseUrl: $($config.Api.BaseUrl)" -ForegroundColor Gray
    Write-Host "   • LoginPath: $($config.Api.LoginPath)" -ForegroundColor Gray
}

Write-Host ""

# 4. Ejecutar aplicación con captura de errores
Write-Host "?? EJECUTANDO APLICACIÓN..." -ForegroundColor Yellow
Write-Host "   (Con URLs hardcoded a gestiontimeapi.onrender.com)" -ForegroundColor Gray

Push-Location $binPath

try {
    # Intentar ejecutar con diferentes métodos
    Write-Host "   ?? Método 1: Start-Process..." -ForegroundColor Gray
    
    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = ".\GestionTime.Desktop.exe"
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.CreateNoWindow = $false
    
    $process = [System.Diagnostics.Process]::Start($startInfo)
    
    if ($process) {
        Write-Host "   ? Proceso iniciado (PID: $($process.Id))" -ForegroundColor Green
        
        # Esperar un poco
        Start-Sleep 3
        
        # Leer salidas
        $stdout = ""
        $stderr = ""
        
        try {
            if (!$process.StandardOutput.EndOfStream) {
                $stdout = $process.StandardOutput.ReadToEnd()
            }
            if (!$process.StandardError.EndOfStream) {
                $stderr = $process.StandardError.ReadToEnd()
            }
        } catch {
            # Ignorar errores de lectura
        }
        
        # Verificar si sigue corriendo
        if (!$process.HasExited) {
            Write-Host "   ? Aplicación ejecutándose" -ForegroundColor Green
            
            # Esperar unos segundos más
            Start-Sleep 5
            
            # Verificar una última vez
            if (!$process.HasExited) {
                Write-Host "   ?? ¡APLICACIÓN ESTABLE!" -ForegroundColor Green
                Write-Host "   ?? Ahora prueba hacer login - NO debería mostrar error de localhost:2501" -ForegroundColor Cyan
                
                $response = Read-Host "¿Cerrar la aplicación? (S/N)"
                if ($response -eq "S" -or $response -eq "s") {
                    try {
                        $process.CloseMainWindow()
                        Start-Sleep 2
                        if (!$process.HasExited) {
                            $process.Kill()
                        }
                        Write-Host "   ? Aplicación cerrada" -ForegroundColor Green
                    } catch {
                        Write-Host "   ?? Error cerrando: $($_.Exception.Message)" -ForegroundColor Yellow
                    }
                }
            } else {
                Write-Host "   ?? Aplicación se cerró después de 8 segundos" -ForegroundColor Yellow
                Write-Host "   ?? Código de salida: $($process.ExitCode)" -ForegroundColor Gray
            }
        } else {
            Write-Host "   ? Aplicación se cerró inmediatamente" -ForegroundColor Red
            Write-Host "   ?? Código de salida: $($process.ExitCode)" -ForegroundColor Gray
            
            if ($stdout) {
                Write-Host "   ?? STDOUT:" -ForegroundColor Cyan
                Write-Host "$stdout" -ForegroundColor Gray
            }
            
            if ($stderr) {
                Write-Host "   ?? STDERR:" -ForegroundColor Red
                Write-Host "$stderr" -ForegroundColor Gray
            }
        }
        
        $process.Dispose()
    }
    
} catch {
    Write-Host "   ? Error iniciando proceso: $($_.Exception.Message)" -ForegroundColor Red
} finally {
    Pop-Location
}

Write-Host ""

# 5. Verificar Event Log
Write-Host "?? VERIFICANDO EVENT LOG..." -ForegroundColor Yellow
try {
    $events = Get-EventLog -LogName Application -After (Get-Date).AddMinutes(-2) -Source "*NET*" -ErrorAction SilentlyContinue | Select-Object -First 3
    
    if ($events) {
        Write-Host "   ?? Eventos .NET recientes encontrados:" -ForegroundColor Yellow
        foreach ($event in $events) {
            if ($event.Message -match "GestionTime") {
                Write-Host "   ?? $($event.TimeGenerated): $($event.EntryType)" -ForegroundColor Gray
                $shortMsg = ($event.Message -split "`n")[0]
                Write-Host "       $shortMsg" -ForegroundColor Gray
            }
        }
    } else {
        Write-Host "   ? No hay eventos de error recientes" -ForegroundColor Green
    }
} catch {
    Write-Host "   ?? No se pudo leer Event Log" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=================================" -ForegroundColor Cyan
Write-Host "   ?? RESULTADO DE LA PRUEBA" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "? CAMBIOS APLICADOS:" -ForegroundColor Green
Write-Host "   • URLs hardcoded a gestiontimeapi.onrender.com" -ForegroundColor Gray
Write-Host "   • Eliminada cualquier referencia a localhost:2501" -ForegroundColor Gray
Write-Host "   • Recompilación limpia realizada" -ForegroundColor Gray
Write-Host ""

Write-Host "?? SI LA APLICACIÓN SE EJECUTA:" -ForegroundColor Cyan
Write-Host "   • Ya NO debería aparecer error de localhost:2501" -ForegroundColor Gray
Write-Host "   • Debería intentar conectarse a gestiontimeapi.onrender.com" -ForegroundColor Gray
Write-Host "   • Cualquier error ahora sería de credenciales, no de URL" -ForegroundColor Gray
Write-Host ""

if (Get-Process "GestionTime.Desktop" -ErrorAction SilentlyContinue) {
    Write-Host "?? ¡APLICACIÓN EJECUTÁNDOSE!" -ForegroundColor Green
    Write-Host "Prueba hacer login ahora." -ForegroundColor White
} else {
    Write-Host "?? La aplicación no está ejecutándose actualmente." -ForegroundColor Yellow
    Write-Host "Intenta ejecutarla manualmente desde Visual Studio (F5)." -ForegroundColor Gray
}

Write-Host ""
Read-Host "Presiona Enter para finalizar"