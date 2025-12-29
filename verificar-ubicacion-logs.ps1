# ================================================================
# VERIFICADOR DE UBICACIÓN DE LOGS - GESTIONTIME DESKTOP  
# Fecha: 29/12/2025
# Propósito: Encontrar y verificar archivos de log actuales
# ================================================================

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "   ?? VERIFICADOR DE UBICACIÓN DE LOGS" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

# Rutas de búsqueda comunes
$searchPaths = @(
    "C:\Logs\GestionTime",
    "C:\GestionTime\GestionTime.Desktop\bin\Debug\net8.0-windows10.0.19041.0\logs",
    "C:\GestionTime\GestionTime.Desktop\bin\Release\net8.0-windows10.0.19041.0\logs", 
    "$env:LOCALAPPDATA\Packages\*GestionTime*\LocalState\logs",
    "$env:TEMP\GestionTime\logs",
    "C:\GestionTime\GestionTime.Desktop\logs"
)

Write-Host "?? BUSCANDO ARCHIVOS DE LOG EN RUTAS COMUNES..." -ForegroundColor Yellow
Write-Host ""

$logsEncontrados = @()
$totalSize = 0

foreach ($searchPath in $searchPaths) {
    Write-Host "?? Buscando en: $searchPath" -ForegroundColor Gray
    
    try {
        # Expandir wildcards en la ruta si existen
        $expandedPaths = Get-ChildItem $searchPath -ErrorAction SilentlyContinue | Where-Object { $_.PSIsContainer }
        
        if ($expandedPaths) {
            foreach ($path in $expandedPaths) {
                $logFiles = Get-ChildItem -Path $path.FullName -Filter "*.log" -ErrorAction SilentlyContinue
                
                if ($logFiles) {
                    foreach ($file in $logFiles) {
                        $logsEncontrados += $file
                        $totalSize += $file.Length
                        
                        $sizeKB = [math]::Round($file.Length / 1KB, 2)
                        $sizeMB = [math]::Round($file.Length / 1MB, 2)
                        
                        Write-Host "   ? ENCONTRADO: $($file.Name)" -ForegroundColor Green
                        Write-Host "      ?? Path: $($file.DirectoryName)" -ForegroundColor Gray
                        Write-Host "      ?? Tamaño: $sizeKB KB ($sizeMB MB)" -ForegroundColor Gray
                        Write-Host "      ?? Modificado: $($file.LastWriteTime)" -ForegroundColor Gray
                        Write-Host ""
                    }
                }
            }
        } else {
            # Búsqueda directa en la ruta
            $logFiles = Get-ChildItem -Path $searchPath -Filter "*.log" -ErrorAction SilentlyContinue
            
            if ($logFiles) {
                foreach ($file in $logFiles) {
                    $logsEncontrados += $file
                    $totalSize += $file.Length
                    
                    $sizeKB = [math]::Round($file.Length / 1KB, 2)
                    $sizeMB = [math]::Round($file.Length / 1MB, 2)
                    
                    Write-Host "   ? ENCONTRADO: $($file.Name)" -ForegroundColor Green
                    Write-Host "      ?? Path: $($file.DirectoryName)" -ForegroundColor Gray
                    Write-Host "      ?? Tamaño: $sizeKB KB ($sizeMB MB)" -ForegroundColor Gray
                    Write-Host "      ?? Modificado: $($file.LastWriteTime)" -ForegroundColor Gray
                    Write-Host ""
                }
            }
        }
    }
    catch {
        # Ignorar errores de acceso
    }
}

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "   ?? RESUMEN DE ARCHIVOS ENCONTRADOS" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

if ($logsEncontrados.Count -eq 0) {
    Write-Host "? NO SE ENCONTRARON ARCHIVOS DE LOG" -ForegroundColor Red
    Write-Host ""
    Write-Host "?? POSIBLES CAUSAS:" -ForegroundColor Yellow
    Write-Host "   • La aplicación no se ha ejecutado aún" -ForegroundColor Gray
    Write-Host "   • Los logs están en una ubicación no incluida en la búsqueda" -ForegroundColor Gray
    Write-Host "   • Error de permisos para acceder a los directorios" -ForegroundColor Gray
    Write-Host ""
    Write-Host "?? RECOMENDACIONES:" -ForegroundColor Yellow
    Write-Host "   1. Ejecutar la aplicación GestionTime Desktop" -ForegroundColor Gray
    Write-Host "   2. Buscar 'LOG PATH =' en Visual Studio Output" -ForegroundColor Gray
    Write-Host "   3. Verificar permisos de escritura en directorios" -ForegroundColor Gray
} else {
    Write-Host "?? Total archivos encontrados: $($logsEncontrados.Count)" -ForegroundColor Green
    
    $totalSizeMB = [math]::Round($totalSize / 1MB, 2)
    Write-Host "?? Tamaño total: $totalSizeMB MB" -ForegroundColor Green
    Write-Host ""
    
    # Agrupar por directorio
    $byDirectory = $logsEncontrados | Group-Object DirectoryName
    
    Write-Host "?? ARCHIVOS POR DIRECTORIO:" -ForegroundColor Cyan
    Write-Host ""
    
    foreach ($group in $byDirectory) {
        Write-Host "?? $($group.Name)" -ForegroundColor Yellow
        
        foreach ($file in $group.Group | Sort-Object LastWriteTime -Descending) {
            $sizeKB = [math]::Round($file.Length / 1KB, 2)
            Write-Host "   ?? $($file.Name) - $sizeKB KB - $($file.LastWriteTime)" -ForegroundColor Gray
        }
        Write-Host ""
    }
    
    # Identificar tipos de logs
    Write-Host "?? ANÁLISIS DE TIPOS DE LOG:" -ForegroundColor Cyan
    Write-Host ""
    
    $debugLogs = $logsEncontrados | Where-Object { $_.Name -match "^app\.log$" }
    $rotatingLogs = $logsEncontrados | Where-Object { $_.Name -match "_rotating\.log$" }
    $datedLogs = $logsEncontrados | Where-Object { $_.Name -match "\d{8}" }
    
    if ($debugLogs) {
        Write-Host "???  Logs de Debug (DebugFileLoggerProvider): $($debugLogs.Count)" -ForegroundColor Blue
        foreach ($log in $debugLogs) {
            Write-Host "   ?? $($log.FullName)" -ForegroundColor Gray
        }
        Write-Host ""
    }
    
    if ($rotatingLogs) {
        Write-Host "?? Logs con Rotación (RotatingFileLoggerProvider): $($rotatingLogs.Count)" -ForegroundColor Blue
        foreach ($log in $rotatingLogs) {
            Write-Host "   ?? $($log.FullName)" -ForegroundColor Gray
        }
        Write-Host ""
    }
    
    if ($datedLogs) {
        Write-Host "?? Logs con Fecha: $($datedLogs.Count)" -ForegroundColor Blue
        foreach ($log in $datedLogs) {
            Write-Host "   ?? $($log.FullName)" -ForegroundColor Gray
        }
        Write-Host ""
    }
    
    # Mostrar el log más reciente
    $mostRecent = $logsEncontrados | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    
    Write-Host "?? ARCHIVO MÁS RECIENTE:" -ForegroundColor Green
    Write-Host "   ?? $($mostRecent.Name)" -ForegroundColor White
    Write-Host "   ?? $($mostRecent.DirectoryName)" -ForegroundColor Gray
    Write-Host "   ?? $($mostRecent.LastWriteTime)" -ForegroundColor Gray
    Write-Host ""
    
    # Opción para abrir directorio
    Write-Host "?? ACCIONES DISPONIBLES:" -ForegroundColor Yellow
    $response = Read-Host "¿Deseas abrir el directorio del log más reciente? (S/N)"
    
    if ($response -eq "S" -or $response -eq "s") {
        try {
            Start-Process "explorer.exe" -ArgumentList $mostRecent.DirectoryName
            Write-Host "? Directorio abierto en Windows Explorer" -ForegroundColor Green
        } catch {
            Write-Host "? Error abriendo directorio: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

Write-Host ""
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "   ?? INFORMACIÓN ADICIONAL" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "?? CONFIGURACIÓN ACTUAL DETECTADA:" -ForegroundColor Yellow
Write-Host ""

# Detectar configuración basada en archivos encontrados
if ($logsEncontrados | Where-Object { $_.Name -eq "app.log" }) {
    Write-Host "   ???  DebugFileLoggerProvider: ? ACTIVO" -ForegroundColor Green
    Write-Host "      ?? Genera: app.log" -ForegroundColor Gray
}

if ($logsEncontrados | Where-Object { $_.Name -match "_rotating" }) {
    Write-Host "   ?? RotatingFileLoggerProvider: ? ACTIVO" -ForegroundColor Green
    Write-Host "      ?? Genera: app_YYYYMMDD_rotating.log" -ForegroundColor Gray
    Write-Host "      ?? Rotación: 10MB máximo, 5 archivos" -ForegroundColor Gray
}

if (($logsEncontrados | Where-Object { $_.Name -eq "app.log" }) -and ($logsEncontrados | Where-Object { $_.Name -match "_rotating" })) {
    Write-Host ""
    Write-Host "??  DETECTADO: DOBLE LOGGING ACTIVO" -ForegroundColor Yellow
    Write-Host "   • Se están generando logs duplicados" -ForegroundColor Gray
    Write-Host "   • Recomendado: unificar en solo RotatingFileLoggerProvider" -ForegroundColor Gray
}

Write-Host ""
Write-Host "?? DOCUMENTACIÓN RELACIONADA:" -ForegroundColor Cyan
Write-Host "   • Helpers/DIAGNOSTICO_CONFIGURACION_LOGS.md" -ForegroundColor Gray
Write-Host "   • Helpers/OPTIMIZACION_LOGGING_INMEDIATA.md" -ForegroundColor Gray
Write-Host ""

Read-Host "Presiona Enter para finalizar"