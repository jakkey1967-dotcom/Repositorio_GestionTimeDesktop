# ================================================================
# DEBUG EXTENSIVO - CAPTURA SALIDA DE DEBUG
# ================================================================

Write-Host "?? DEBUG EXTENSIVO - CAPTURA DE CONFIGURACIÓN" -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host ""

$binPath = "bin\Debug\net8.0-windows10.0.19041.0"

# 1. Verificar archivos
Write-Host "?? VERIFICANDO ARCHIVOS..." -ForegroundColor Yellow
$exePath = "$binPath\GestionTime.Desktop.exe"
$configPath = "$binPath\appsettings.json"

Write-Host "   • Ejecutable: $(if (Test-Path $exePath) {'? OK'} else {'? NO ENCONTRADO'})" -ForegroundColor Gray
Write-Host "   • Configuración: $(if (Test-Path $configPath) {'? OK'} else {'? NO ENCONTRADO'})" -ForegroundColor Gray

if (Test-Path $configPath) {
    $configContent = Get-Content $configPath | ConvertFrom-Json
    Write-Host "   • BaseUrl en config: $($configContent.Api.BaseUrl)" -ForegroundColor Gray
}

Write-Host ""

# 2. Ejecutar desde Visual Studio para capturar debug output
Write-Host "?? EJECUTANDO CON VISUAL STUDIO..." -ForegroundColor Yellow
Write-Host "   (Esto capturará la salida de Debug.WriteLine)" -ForegroundColor Gray

# Buscar devenv.exe
$vsPath = Get-ChildItem "C:\Program Files*\Microsoft Visual Studio\*\*\Common7\IDE\devenv.exe" -ErrorAction SilentlyContinue | Select-Object -First 1

if ($vsPath) {
    Write-Host "   • Visual Studio encontrado: $($vsPath.FullName)" -ForegroundColor Green
    Write-Host "   • Abriendo proyecto..." -ForegroundColor Gray
    
    try {
        # Abrir el proyecto en VS
        Start-Process $vsPath.FullName -ArgumentList "GestionTime.Desktop.csproj" -PassThru
        Write-Host "   ? Visual Studio iniciado con el proyecto" -ForegroundColor Green
        Write-Host ""
        Write-Host "?? INSTRUCCIONES:" -ForegroundColor Cyan
        Write-Host "   1. Presiona F5 para ejecutar en modo Debug" -ForegroundColor Gray
        Write-Host "   2. Mira la ventana 'Output' ? 'Debug'" -ForegroundColor Gray
        Write-Host "   3. Busca las líneas que empiecen con '===' para ver la configuración" -ForegroundColor Gray
        Write-Host "   4. Especialmente busca '=== URLs FINALES ===' para ver qué URL se está usando" -ForegroundColor Gray
        Write-Host ""
        Write-Host "??  Después de ejecutar en VS, presiona Enter para continuar con debug alternativo..." -ForegroundColor Yellow
        Read-Host
    } catch {
        Write-Host "   ? Error abriendo Visual Studio: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "   ? Visual Studio no encontrado" -ForegroundColor Red
}

# 3. Método alternativo: usar DebugView para capturar debug output
Write-Host "?? MÉTODO ALTERNATIVO: EJECUCIÓN DIRECTA CON LOGGING" -ForegroundColor Yellow

Push-Location $binPath

try {
    # Limpiar logs anteriores
    Remove-Item "logs\*.log" -Force -ErrorAction SilentlyContinue
    
    Write-Host "   • Ejecutando aplicación..." -ForegroundColor Gray
    
    # Ejecutar la aplicación
    $process = Start-Process ".\GestionTime.Desktop.exe" -PassThru -WindowStyle Normal
    
    if ($process) {
        Write-Host "   • Aplicación iniciada (PID: $($process.Id))" -ForegroundColor Green
        Write-Host "   • Esperando 8 segundos para ver el comportamiento..." -ForegroundColor Gray
        
        Start-Sleep 8
        
        # Verificar si sigue corriendo
        $running = Get-Process -Id $process.Id -ErrorAction SilentlyContinue
        if ($running) {
            Write-Host "   ? Aplicación ejecutándose" -ForegroundColor Green
        } else {
            Write-Host "   ?? Aplicación se cerró" -ForegroundColor Yellow
        }
        
        # Verificar logs
        Write-Host ""
        Write-Host "?? VERIFICANDO LOGS GENERADOS..." -ForegroundColor Cyan
        
        $logFiles = Get-ChildItem "logs\*.log" -ErrorAction SilentlyContinue | Where-Object { 
            (Get-Date) - $_.LastWriteTime -lt [TimeSpan]::FromMinutes(1) 
        }
        
        if ($logFiles) {
            Write-Host "   ? Logs encontrados:" -ForegroundColor Green
            foreach ($file in $logFiles) {
                Write-Host "      • $($file.Name)" -ForegroundColor Gray
                
                # Leer el log y buscar información de API
                $content = Get-Content $file.FullName -ErrorAction SilentlyContinue
                if ($content) {
                    $apiLines = $content | Where-Object { $_ -match "BaseUrl|API|gestiontime|localhost" }
                    if ($apiLines) {
                        Write-Host "      ?? Líneas relevantes encontradas:" -ForegroundColor Cyan
                        foreach ($line in $apiLines) {
                            if ($line -match "localhost") {
                                Write-Host "        ?? $line" -ForegroundColor Red
                            } else {
                                Write-Host "        ? $line" -ForegroundColor Green
                            }
                        }
                    }
                }
            }
        } else {
            Write-Host "   ? No se generaron logs" -ForegroundColor Red
        }
        
        # Cerrar la aplicación si sigue corriendo
        if ($running) {
            Write-Host ""
            $closeResponse = Read-Host "¿Cerrar la aplicación? (S/N)"
            if ($closeResponse -eq "S" -or $closeResponse -eq "s") {
                $running.CloseMainWindow()
                Start-Sleep 2
                if (!$running.HasExited) {
                    $running.Kill()
                }
                Write-Host "   ? Aplicación cerrada" -ForegroundColor Green
            }
        }
    }
    
} catch {
    Write-Host "   ? Error ejecutando aplicación: $($_.Exception.Message)" -ForegroundColor Red
} finally {
    Pop-Location
}

Write-Host ""
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "   ?? PRÓXIMOS PASOS PARA DEBUG" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "?? SI SIGUE APARECIENDO localhost:2501:" -ForegroundColor Yellow
Write-Host "   1. Ejecuta desde Visual Studio (F5) y mira Output ? Debug" -ForegroundColor Gray
Write-Host "   2. Busca las líneas '=== CONFIGURACIÓN DEBUG ===' y '=== URLs FINALES ==='" -ForegroundColor Gray
Write-Host "   3. Verifica qué URL se está cargando realmente" -ForegroundColor Gray
Write-Host ""

Write-Host "?? TAMBIÉN PUEDES:" -ForegroundColor Cyan
Write-Host "   • Usar DebugView de Microsoft para capturar Debug.WriteLine" -ForegroundColor Gray
Write-Host "   • Verificar que no hay versiones cached de la aplicación" -ForegroundColor Gray
Write-Host "   • Comprobar si el error viene de otro lugar (no de App.xaml.cs)" -ForegroundColor Gray

Write-Host ""
Read-Host "Presiona Enter para finalizar"