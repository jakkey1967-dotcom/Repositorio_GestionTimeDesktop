# ================================================================
# DIAGNÓSTICO Y CREACIÓN DEL DIRECTORIO DE LOGS
# ================================================================

Write-Host "?? DIAGNÓSTICO DEL DIRECTORIO DE LOGS" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Rutas a verificar
$projectRoot = Get-Location
$expectedLocations = @(
    @{ 
        Name = "Proyecto Root"
        Path = Join-Path $projectRoot "logs"
        Type = "Development"
    },
    @{ 
        Name = "Bin Debug"
        Path = Join-Path $projectRoot "bin\Debug\net8.0-windows10.0.19041.0\logs"
        Type = "Runtime"
    },
    @{ 
        Name = "AppData Local"
        Path = Join-Path $env:LOCALAPPDATA "Packages\*GestionTime*\LocalState\logs"
        Type = "Packaged"
    },
    @{ 
        Name = "Temp"
        Path = Join-Path $env:TEMP "GestionTime\logs"
        Type = "Fallback"
    }
)

Write-Host "?? VERIFICANDO UBICACIONES DE LOGS:" -ForegroundColor Yellow
Write-Host ""

foreach ($location in $expectedLocations) {
    Write-Host "?? $($location.Name) ($($location.Type)):" -ForegroundColor Cyan
    Write-Host "   ?? Ruta: $($location.Path)" -ForegroundColor Gray
    
    if ($location.Path -like "*\*GestionTime*\*") {
        # Buscar con wildcard
        $expandedPaths = Get-ChildItem (Split-Path $location.Path -Parent) -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name -like "*GestionTime*" }
        
        if ($expandedPaths) {
            foreach ($expandedPath in $expandedPaths) {
                $fullLogsPath = Join-Path $expandedPath.FullName "logs"
                if (Test-Path $fullLogsPath) {
                    Write-Host "   ? Existe en: $fullLogsPath" -ForegroundColor Green
                    $logFiles = Get-ChildItem $fullLogsPath -Filter "*.log" -ErrorAction SilentlyContinue
                    Write-Host "   ?? Archivos: $($logFiles.Count) logs encontrados" -ForegroundColor Gray
                } else {
                    Write-Host "   ? No existe en: $fullLogsPath" -ForegroundColor Red
                }
            }
        } else {
            Write-Host "   ? Directorio padre no encontrado" -ForegroundColor Red
        }
    } else {
        if (Test-Path $location.Path) {
            Write-Host "   ? Existe" -ForegroundColor Green
            $logFiles = Get-ChildItem $location.Path -Filter "*.log" -ErrorAction SilentlyContinue
            Write-Host "   ?? Archivos: $($logFiles.Count) logs encontrados" -ForegroundColor Gray
        } else {
            Write-Host "   ? No existe" -ForegroundColor Red
        }
    }
    Write-Host ""
}

# Verificar configuración actual
Write-Host "?? VERIFICANDO CONFIGURACIÓN ACTUAL:" -ForegroundColor Yellow
Write-Host ""

if (Test-Path "appsettings.json") {
    $config = Get-Content "appsettings.json" | ConvertFrom-Json
    $configuredLogPath = $config.Logging.LogPath
    Write-Host "?? appsettings.json LogPath: '$configuredLogPath'" -ForegroundColor Green
    
    # Resolver la ruta basándose en la lógica de la aplicación
    if ([System.IO.Path]::IsPathRooted($configuredLogPath)) {
        $resolvedPath = $configuredLogPath
    } else {
        # Ruta relativa - simular AppContext.BaseDirectory
        $simulatedBaseDir = Join-Path $projectRoot "bin\Debug\net8.0-windows10.0.19041.0"
        $resolvedPath = Join-Path $simulatedBaseDir $configuredLogPath
    }
    
    Write-Host "?? Ruta resuelta: '$resolvedPath'" -ForegroundColor Gray
    $expectedLogDir = Split-Path $resolvedPath -Parent
    Write-Host "?? Directorio esperado: '$expectedLogDir'" -ForegroundColor Gray
    
    if (Test-Path $expectedLogDir) {
        Write-Host "? El directorio esperado existe" -ForegroundColor Green
    } else {
        Write-Host "? El directorio esperado NO existe" -ForegroundColor Red
    }
} else {
    Write-Host "? appsettings.json no encontrado" -ForegroundColor Red
}

Write-Host ""

# Crear directorios faltantes
Write-Host "?? CREANDO DIRECTORIOS FALTANTES:" -ForegroundColor Yellow
Write-Host ""

$dirsToCreate = @(
    Join-Path $projectRoot "logs",
    Join-Path $projectRoot "bin\Debug\net8.0-windows10.0.19041.0\logs"
)

foreach ($dir in $dirsToCreate) {
    Write-Host "?? Creando: $dir" -ForegroundColor Gray
    
    try {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
        
        # Test de escritura
        $testFile = Join-Path $dir "test_creation.log"
        "Test de creación - $(Get-Date)" | Out-File $testFile -Encoding UTF8
        
        if (Test-Path $testFile) {
            Write-Host "   ? Creado exitosamente y con permisos de escritura" -ForegroundColor Green
            Remove-Item $testFile -Force
        } else {
            Write-Host "   ?? Creado pero sin permisos de escritura" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "   ? Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""

# Verificar que el sistema esté configurado correctamente
Write-Host "?? PROBANDO SISTEMA DE LOGGING:" -ForegroundColor Yellow
Write-Host ""

# Simular la lógica de creación de la aplicación
$binPath = "bin\Debug\net8.0-windows10.0.19041.0"

if (Test-Path "$binPath\appsettings.json") {
    Write-Host "? appsettings.json existe en bin" -ForegroundColor Green
} else {
    Write-Host "?? appsettings.json NO existe en bin - copiando..." -ForegroundColor Yellow
    Copy-Item "appsettings.json" "$binPath\appsettings.json" -Force
    Write-Host "? appsettings.json copiado" -ForegroundColor Green
}

# Verificar directorio final
$finalLogDir = "$binPath\logs"
if (Test-Path $finalLogDir) {
    Write-Host "? Directorio logs final existe: $finalLogDir" -ForegroundColor Green
    
    # Crear log de prueba
    $testLogFile = Join-Path $finalLogDir "manual_test_$(Get-Date -Format 'yyyyMMdd_HHmmss').log"
    $testContent = @"
=== LOG DE PRUEBA MANUAL ===
Fecha: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
Directorio: $finalLogDir
Configuración: OK
Estado: Directorio creado exitosamente
================================
"@
    
    try {
        $testContent | Out-File $testLogFile -Encoding UTF8
        Write-Host "? Log de prueba creado: $($testLogFile | Split-Path -Leaf)" -ForegroundColor Green
        
        $fileSize = (Get-Item $testLogFile).Length
        Write-Host "   ?? Tamaño: $([math]::Round($fileSize / 1KB, 2)) KB" -ForegroundColor Gray
    } catch {
        Write-Host "? Error creando log de prueba: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "? Directorio logs final NO existe después de creación" -ForegroundColor Red
}

Write-Host ""

# Mostrar estado final
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "   ?? ESTADO FINAL DEL SISTEMA" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Verificar todos los directorios
$allLogsCreated = $true
foreach ($location in $expectedLocations[0..1]) { # Solo verificar Development y Runtime
    if (Test-Path $location.Path) {
        Write-Host "? $($location.Name): Disponible" -ForegroundColor Green
    } else {
        Write-Host "? $($location.Name): Faltante" -ForegroundColor Red
        $allLogsCreated = $false
    }
}

Write-Host ""

if ($allLogsCreated) {
    Write-Host "?? ¡TODOS LOS DIRECTORIOS DE LOGS ESTÁN LISTOS!" -ForegroundColor Green
    Write-Host ""
    Write-Host "?? PRÓXIMOS PASOS:" -ForegroundColor Cyan
    Write-Host "   1. Ejecutar tu aplicación" -ForegroundColor Gray
    Write-Host "   2. Los logs se crearán automáticamente en:" -ForegroundColor Gray
    Write-Host "      • $binPath\logs\" -ForegroundColor Gray
    Write-Host "   3. Usar .\verificar-ubicacion-logs.ps1 para monitorear" -ForegroundColor Gray
} else {
    Write-Host "?? ALGUNOS DIRECTORIOS FALTAN" -ForegroundColor Yellow
    Write-Host "   Ejecuta este script nuevamente o créalos manualmente" -ForegroundColor Gray
}

Write-Host ""

# Mostrar archivos actuales en logs
$currentLogFiles = Get-ChildItem "$binPath\logs\*.log" -ErrorAction SilentlyContinue
if ($currentLogFiles) {
    Write-Host "?? LOGS ACTUALES ENCONTRADOS:" -ForegroundColor Cyan
    foreach ($logFile in $currentLogFiles) {
        $ageMinutes = [math]::Round(((Get-Date) - $logFile.LastWriteTime).TotalMinutes, 1)
        $sizeKB = [math]::Round($logFile.Length / 1KB, 2)
        Write-Host "   • $($logFile.Name) - $sizeKB KB (modificado hace $ageMinutes min)" -ForegroundColor Gray
    }
} else {
    Write-Host "?? No hay logs actuales - se crearán al ejecutar la aplicación" -ForegroundColor Gray
}

Write-Host ""
Read-Host "Presiona Enter para finalizar"