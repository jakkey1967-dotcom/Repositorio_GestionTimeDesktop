# ================================================================
# DIAGNÓSTICO EN TIEMPO REAL - LOGS GESTIONTIME DESKTOP
# Fecha: 29/12/2025
# Propósito: Detectar dónde se están escribiendo realmente los logs
# ================================================================

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "   ?? DIAGNÓSTICO EN TIEMPO REAL DE LOGS" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

# Función para mostrar rutas de manera segura
function Show-PathSafely($path) {
    try {
        if (Test-Path $path) {
            return (Resolve-Path $path).Path
        } else {
            return "$path (NO EXISTE)"
        }
    } catch {
        return "$path (ERROR)"
    }
}

Write-Host "?? VERIFICANDO CONFIGURACIÓN ACTUAL..." -ForegroundColor Yellow
Write-Host ""

# 1. Verificar AppContext.BaseDirectory
Write-Host "?? CONTEXTO DE EJECUCIÓN:" -ForegroundColor Cyan

# Detectar si estamos en bin\Debug o bin\Release
$currentDir = Get-Location
Write-Host "   • Directorio actual: $currentDir" -ForegroundColor Gray

# Buscar el ejecutable
$exePaths = @(
    "bin\Debug\net8.0-windows10.0.19041.0\GestionTime.Desktop.exe",
    "bin\Release\net8.0-windows10.0.19041.0\GestionTime.Desktop.exe",
    "publish\msix-package\GestionTime.Desktop.exe"
)

$foundExe = $null
foreach ($exePath in $exePaths) {
    $fullExePath = Join-Path $currentDir $exePath
    if (Test-Path $fullExePath) {
        $foundExe = $fullExePath
        $exeDir = Split-Path $foundExe -Parent
        Write-Host "   • Ejecutable encontrado: $foundExe" -ForegroundColor Green
        Write-Host "   • BaseDirectory esperado: $exeDir" -ForegroundColor Gray
        
        # Verificar logs en esa ubicación
        $expectedLogDir = Join-Path $exeDir "logs"
        Write-Host "   • Directorio logs esperado: $(Show-PathSafely $expectedLogDir)" -ForegroundColor Gray
        
        if (Test-Path $expectedLogDir) {
            $logFiles = Get-ChildItem $expectedLogDir -Filter "*.log" -ErrorAction SilentlyContinue
            if ($logFiles) {
                Write-Host "   • ? Logs encontrados en BaseDirectory:" -ForegroundColor Green
                foreach ($log in $logFiles) {
                    Write-Host "     ?? $($log.Name) ($([math]::Round($log.Length/1KB, 2)) KB) - $($log.LastWriteTime)" -ForegroundColor Gray
                }
            } else {
                Write-Host "   • ??  Directorio logs existe pero sin archivos .log" -ForegroundColor Yellow
            }
        } else {
            Write-Host "   • ? Directorio logs NO existe en BaseDirectory" -ForegroundColor Red
        }
        break
    }
}

if (-not $foundExe) {
    Write-Host "   • ? No se encontró ejecutable compilado" -ForegroundColor Red
    Write-Host "   • ?? Ejecuta: dotnet build primero" -ForegroundColor Yellow
}

Write-Host ""

# 2. Verificar appsettings.json
Write-Host "?? CONFIGURACIÓN appsettings.json:" -ForegroundColor Cyan
$appsettingsPath = "appsettings.json"

if (Test-Path $appsettingsPath) {
    try {
        $appsettings = Get-Content $appsettingsPath | ConvertFrom-Json
        
        if ($appsettings.Logging -and $appsettings.Logging.LogPath) {
            Write-Host "   • LogPath configurado: $($appsettings.Logging.LogPath)" -ForegroundColor Green
            
            # Resolver la ruta
            $logPath = $appsettings.Logging.LogPath
            if ([System.IO.Path]::IsPathRooted($logPath)) {
                $resolvedLogPath = $logPath
            } else {
                # Ruta relativa - combinar con BaseDirectory
                if ($foundExe) {
                    $baseDir = Split-Path $foundExe -Parent
                    $resolvedLogPath = Join-Path $baseDir $logPath
                } else {
                    $resolvedLogPath = Join-Path $currentDir $logPath
                }
            }
            
            Write-Host "   • Ruta resuelta: $(Show-PathSafely $resolvedLogPath)" -ForegroundColor Gray
            
            # Verificar el directorio
            $logDir = Split-Path $resolvedLogPath -Parent
            if (Test-Path $logDir) {
                Write-Host "   • ? Directorio existe" -ForegroundColor Green
                
                # Verificar permisos de escritura
                try {
                    $testFile = Join-Path $logDir "test_write_$(Get-Date -Format 'HHmmss').tmp"
                    "test" | Out-File $testFile -ErrorAction Stop
                    Remove-Item $testFile -ErrorAction SilentlyContinue
                    Write-Host "   • ? Permisos de escritura: OK" -ForegroundColor Green
                } catch {
                    Write-Host "   • ? Sin permisos de escritura: $($_.Exception.Message)" -ForegroundColor Red
                }
            } else {
                Write-Host "   • ? Directorio NO existe" -ForegroundColor Red
            }
        } else {
            Write-Host "   • ??  No hay LogPath configurado en appsettings.json" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "   • ? Error leyendo appsettings.json: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "   • ? appsettings.json no encontrado" -ForegroundColor Red
}

Write-Host ""

# 3. Verificar todas las ubicaciones posibles donde podrían estar los logs
Write-Host "?? BÚSQUEDA EXHAUSTIVA DE LOGS RECIENTES:" -ForegroundColor Cyan
Write-Host ""

$searchPaths = @(
    "C:\GestionTime\GestionTime.Desktop\logs",
    "C:\GestionTime\GestionTime.Desktop\bin\Debug\net8.0-windows10.0.19041.0\logs",
    "C:\GestionTime\GestionTime.Desktop\bin\Release\net8.0-windows10.0.19041.0\logs",
    "$env:LOCALAPPDATA\Packages\*GestionTime*\LocalState\logs",
    "$env:TEMP\GestionTime\logs",
    "logs"
)

$recentLogs = @()
$cutoffTime = (Get-Date).AddHours(-1) # Logs de la última hora

foreach ($searchPath in $searchPaths) {
    Write-Host "?? Buscando en: $searchPath" -ForegroundColor Gray
    
    try {
        if ($searchPath -like "*GestionTime*") {
            # Expandir wildcards
            $expandedPaths = Get-ChildItem $searchPath -Directory -ErrorAction SilentlyContinue
            
            foreach ($expandedPath in $expandedPaths) {
                $logFiles = Get-ChildItem $expandedPath.FullName -Filter "*.log" -ErrorAction SilentlyContinue | Where-Object { $_.LastWriteTime -gt $cutoffTime }
                if ($logFiles) {
                    $recentLogs += $logFiles
                    foreach ($file in $logFiles) {
                        Write-Host "   ? RECIENTE: $($file.Name) - $($file.LastWriteTime)" -ForegroundColor Yellow
                        Write-Host "      ?? $($file.DirectoryName)" -ForegroundColor Gray
                    }
                }
            }
        } else {
            $logFiles = Get-ChildItem $searchPath -Filter "*.log" -ErrorAction SilentlyContinue | Where-Object { $_.LastWriteTime -gt $cutoffTime }
            if ($logFiles) {
                $recentLogs += $logFiles
                foreach ($file in $logFiles) {
                    Write-Host "   ? RECIENTE: $($file.Name) - $($file.LastWriteTime)" -ForegroundColor Yellow
                    Write-Host "      ?? $($file.DirectoryName)" -ForegroundColor Gray
                }
            }
        }
    } catch {
        # Ignorar errores de acceso
    }
}

Write-Host ""

# 4. Análisis de logs recientes
if ($recentLogs.Count -eq 0) {
    Write-Host "? NO SE ENCONTRARON LOGS RECIENTES (última hora)" -ForegroundColor Red
    Write-Host ""
    Write-Host "?? POSIBLES CAUSAS:" -ForegroundColor Yellow
    Write-Host "   • La aplicación no se ha ejecutado recientemente" -ForegroundColor Gray
    Write-Host "   • Hay un error en la configuración de logging" -ForegroundColor Gray
    Write-Host "   • Los logs se están escribiendo en una ubicación no buscada" -ForegroundColor Gray
    Write-Host "   • Problema de permisos de escritura" -ForegroundColor Gray
    Write-Host ""
    Write-Host "?? PRÓXIMOS PASOS RECOMENDADOS:" -ForegroundColor Yellow
    Write-Host "   1. Ejecutar la aplicación desde Visual Studio (Debug)" -ForegroundColor Gray
    Write-Host "   2. Buscar 'LOG PATH =' en la ventana Output" -ForegroundColor Gray
    Write-Host "   3. Verificar que no hay excepciones en el inicio" -ForegroundColor Gray
    Write-Host "   4. Revisar el Event Viewer de Windows por errores" -ForegroundColor Gray
} else {
    Write-Host "? LOGS RECIENTES ENCONTRADOS:" -ForegroundColor Green
    Write-Host ""
    
    $mostRecent = $recentLogs | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    Write-Host "?? LOG MÁS RECIENTE:" -ForegroundColor Green
    Write-Host "   • Archivo: $($mostRecent.Name)" -ForegroundColor White
    Write-Host "   • Ubicación: $($mostRecent.DirectoryName)" -ForegroundColor Gray
    Write-Host "   • Modificado: $($mostRecent.LastWriteTime)" -ForegroundColor Gray
    Write-Host "   • Tamaño: $([math]::Round($mostRecent.Length/1KB, 2)) KB" -ForegroundColor Gray
    Write-Host ""
    
    # Mostrar últimas líneas del log
    Write-Host "?? ÚLTIMAS 10 LÍNEAS DEL LOG:" -ForegroundColor Cyan
    try {
        $lastLines = Get-Content $mostRecent.FullName -Tail 10 -ErrorAction Stop
        foreach ($line in $lastLines) {
            Write-Host "   $line" -ForegroundColor Gray
        }
    } catch {
        Write-Host "   ? Error leyendo archivo: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "   ??? COMANDOS DE VERIFICACIÓN" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "?? PARA EJECUTAR APLICACIÓN Y MONITOREAR LOGS:" -ForegroundColor Yellow
Write-Host ""

if ($foundExe) {
    Write-Host "# Ejecutar aplicación:" -ForegroundColor Green
    Write-Host "Start-Process '$foundExe'" -ForegroundColor Gray
    Write-Host ""
    
    $expectedLogDir = Join-Path (Split-Path $foundExe -Parent) "logs"
    Write-Host "# Monitorear logs en tiempo real:" -ForegroundColor Green
    Write-Host "Get-ChildItem '$expectedLogDir\*.log' | ForEach-Object { Get-Content `$_.FullName -Wait }" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "# Verificar logs después de ejecutar:" -ForegroundColor Green
    Write-Host "Get-ChildItem '$expectedLogDir\*.log' | Select-Object Name, Length, LastWriteTime" -ForegroundColor Gray
}

Write-Host ""
Read-Host "Presiona Enter para finalizar"