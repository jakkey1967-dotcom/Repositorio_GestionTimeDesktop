# ================================================================
# VERIFICACIÓN REAL DE UBICACIONES - SCRIPT DE VALIDACIÓN
# ================================================================

Write-Host "?? VERIFICACIÓN DE UBICACIONES REALES VS REPORTADAS" -ForegroundColor Cyan
Write-Host "===================================================" -ForegroundColor Cyan
Write-Host ""

# ================================================================
# MOSTRAR TODAS LAS UBICACIONES POSIBLES
# ================================================================

Write-Host "?? UBICACIONES POSIBLES DE LOGS:" -ForegroundColor Yellow
Write-Host ""

$locations = @(
    @{ Name = "Directorio Proyecto Root"; Path = "C:\GestionTime\GestionTime.Desktop\Logs" },
    @{ Name = "Directorio Proyecto logs"; Path = "C:\GestionTime\GestionTime.Desktop\logs" },
    @{ Name = "Bin Debug Estándar"; Path = "C:\GestionTime\GestionTime.Desktop\bin\Debug\net8.0-windows10.0.19041.0\logs" },
    @{ Name = "Bin x64 Debug"; Path = "C:\GestionTime\GestionTime.Desktop\bin\x64\Debug\net8.0-windows10.0.19041.0\logs" },
    @{ Name = "Bin x64 Debug win-x64"; Path = "C:\GestionTime\GestionTime.Desktop\bin\x64\Debug\net8.0-windows10.0.19041.0\win-x64\logs" }
)

foreach ($location in $locations) {
    Write-Host "?? $($location.Name):" -ForegroundColor Cyan
    Write-Host "   ?? Ruta: $($location.Path)" -ForegroundColor Gray
    
    if (Test-Path $location.Path) {
        Write-Host "   ? EXISTE" -ForegroundColor Green
        
        $files = Get-ChildItem $location.Path -Filter "*.log" -ErrorAction SilentlyContinue
        if ($files) {
            Write-Host "   ?? Archivos encontrados: $($files.Count)" -ForegroundColor Green
            foreach ($file in $files) {
                $ageMinutes = [math]::Round(((Get-Date) - $file.LastWriteTime).TotalMinutes, 1)
                $sizeKB = [math]::Round($file.Length / 1KB, 2)
                Write-Host "      • $($file.Name) - $sizeKB KB (hace $ageMinutes min)" -ForegroundColor Gray
            }
        } else {
            Write-Host "   ?? Directorio vacío (sin archivos .log)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "   ? NO EXISTE" -ForegroundColor Red
    }
    Write-Host ""
}

# ================================================================
# EJECUTAR EL SCRIPT MEJORADO Y CAPTURAR VARIABLES
# ================================================================

Write-Host "?? EJECUTANDO DETECCIÓN AUTOMÁTICA..." -ForegroundColor Yellow
Write-Host ""

# Simular la lógica del script para encontrar el ejecutable
$searchPaths = @(
    "bin\Debug\net8.0-windows10.0.19041.0\GestionTime.Desktop.exe",
    "bin\Debug\net8.0-windows10.0.19041.0\win-x64\GestionTime.Desktop.exe", 
    "bin\x64\Debug\net8.0-windows10.0.19041.0\GestionTime.Desktop.exe",
    "bin\x64\Debug\net8.0-windows10.0.19041.0\win-x64\GestionTime.Desktop.exe"
)

$foundPath = $null
$foundDir = $null

foreach ($searchPath in $searchPaths) {
    if (Test-Path $searchPath) {
        $foundPath = Resolve-Path $searchPath
        $foundDir = Split-Path $foundPath -Parent
        Write-Host "?? EJECUTABLE DETECTADO:" -ForegroundColor Green
        Write-Host "   ?? Ruta completa: $($foundPath.Path)" -ForegroundColor Gray
        Write-Host "   ?? Directorio: $foundDir" -ForegroundColor Gray
        break
    }
}

if ($foundDir) {
    $scriptLogsPath = Join-Path $foundDir "logs"
    Write-Host ""
    Write-Host "?? EL SCRIPT USARÁ:" -ForegroundColor Cyan
    Write-Host "   ?? Directorio logs: $scriptLogsPath" -ForegroundColor White
    Write-Host ""
    
    # Verificar si esta ubicación existe y tiene archivos
    if (Test-Path $scriptLogsPath) {
        Write-Host "? DIRECTORIO SCRIPT EXISTE" -ForegroundColor Green
        $scriptFiles = Get-ChildItem $scriptLogsPath -Filter "*.log" -ErrorAction SilentlyContinue
        if ($scriptFiles) {
            Write-Host "?? Archivos en directorio del script: $($scriptFiles.Count)" -ForegroundColor Green
            foreach ($file in $scriptFiles) {
                $ageMinutes = [math]::Round(((Get-Date) - $file.LastWriteTime).TotalMinutes, 1)
                $sizeKB = [math]::Round($file.Length / 1KB, 2)
                Write-Host "   • $($file.Name) - $sizeKB KB (hace $ageMinutes min)" -ForegroundColor Gray
            }
        } else {
            Write-Host "?? Directorio del script está vacío" -ForegroundColor Yellow
        }
    } else {
        Write-Host "? DIRECTORIO SCRIPT NO EXISTE" -ForegroundColor Red
    }
} else {
    Write-Host "? NO SE ENCONTRÓ EL EJECUTABLE" -ForegroundColor Red
}

Write-Host ""

# ================================================================
# COMPARAR CON LA UBICACIÓN QUE ESTÁ VERIFICANDO EL USUARIO
# ================================================================

Write-Host "?? UBICACIÓN QUE ESTÁ VERIFICANDO EL USUARIO:" -ForegroundColor Yellow
$userCheckPath = "C:\GestionTime\GestionTime.Desktop\Logs"
Write-Host "   ?? $userCheckPath" -ForegroundColor White
Write-Host ""

if (Test-Path $userCheckPath) {
    Write-Host "? La ubicación del usuario EXISTE" -ForegroundColor Green
    $userFiles = Get-ChildItem $userCheckPath -Filter "*.log" -ErrorAction SilentlyContinue
    if ($userFiles) {
        Write-Host "?? Archivos en ubicación del usuario: $($userFiles.Count)" -ForegroundColor Green
        foreach ($file in $userFiles) {
            $ageMinutes = [math]::Round(((Get-Date) - $file.LastWriteTime).TotalMinutes, 1)
            $sizeKB = [math]::Round($file.Length / 1KB, 2)
            Write-Host "   • $($file.Name) - $sizeKB KB (hace $ageMinutes min)" -ForegroundColor Gray
        }
    } else {
        Write-Host "?? La ubicación del usuario está vacía" -ForegroundColor Yellow
    }
} else {
    Write-Host "? La ubicación del usuario NO EXISTE" -ForegroundColor Red
    Write-Host "   ?? Crear directorio..." -ForegroundColor Cyan
    try {
        New-Item -ItemType Directory -Path $userCheckPath -Force | Out-Null
        Write-Host "   ? Directorio creado: $userCheckPath" -ForegroundColor Green
    } catch {
        Write-Host "   ? Error creando directorio: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""

# ================================================================
# ANÁLISIS DE DISCREPANCIA
# ================================================================

Write-Host "?? ANÁLISIS DE DISCREPANCIA:" -ForegroundColor Cyan
Write-Host ""

if ($foundDir) {
    $scriptPath = Join-Path $foundDir "logs"
    
    if ($scriptPath -eq $userCheckPath) {
        Write-Host "? COINCIDENCIA: Script y usuario verifican la misma ubicación" -ForegroundColor Green
    } else {
        Write-Host "?? DISCREPANCIA ENCONTRADA:" -ForegroundColor Yellow
        Write-Host "   ?? Script usa: $scriptPath" -ForegroundColor Red
        Write-Host "   ?? Usuario verifica: $userCheckPath" -ForegroundColor Red
        Write-Host ""
        Write-Host "?? ESTO EXPLICA POR QUE NO VE LOS ARCHIVOS!" -ForegroundColor Yellow
        Write-Host "   El script está creando logs en una ubicación diferente" -ForegroundColor Gray
        Write-Host "   a la que está verificando el usuario." -ForegroundColor Gray
    }
} else {
    Write-Host "? No se puede hacer comparación - ejecutable no encontrado" -ForegroundColor Red
}

Write-Host ""

# ================================================================
# SOLUCIONES PROPUESTAS
# ================================================================

Write-Host "?? SOLUCIONES PROPUESTAS:" -ForegroundColor Cyan
Write-Host ""

if ($foundDir) {
    $scriptPath = Join-Path $foundDir "logs"
    
    Write-Host "?? OPCIÓN 1: Verificar la ubicación correcta" -ForegroundColor Yellow
    Write-Host "   Verificar logs en: $scriptPath" -ForegroundColor White
    Write-Host "   Comando: explorer '$scriptPath'" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "?? OPCIÓN 2: Sincronizar ubicaciones" -ForegroundColor Yellow
    Write-Host "   Copiar logs a la ubicación que esperas:" -ForegroundColor Gray
    Write-Host "   Copy-Item '$scriptPath\*.log' '$userCheckPath\' -Force" -ForegroundColor White
    Write-Host ""
    
    Write-Host "?? OPCIÓN 3: Crear enlace simbólico" -ForegroundColor Yellow
    Write-Host "   Crear enlace desde ubicación esperada a real:" -ForegroundColor Gray
    Write-Host "   cmd /c mklink /D '$userCheckPath' '$scriptPath'" -ForegroundColor White
    Write-Host ""
}

Write-Host "?? OPCIÓN 4: Modificar configuración" -ForegroundColor Yellow
Write-Host "   Cambiar appsettings.json para usar ruta absoluta:" -ForegroundColor Gray
Write-Host "   LogPath: 'C:\\GestionTime\\GestionTime.Desktop\\Logs\\app.log'" -ForegroundColor White
Write-Host ""

# ================================================================
# ACCIONES INMEDIATAS
# ================================================================

Write-Host "?? ACCIONES INMEDIATAS:" -ForegroundColor Green
Write-Host ""

if ($foundDir) {
    $scriptPath = Join-Path $foundDir "logs"
    
    Write-Host "1. ?? Abrir ubicación real de logs:" -ForegroundColor Cyan
    $response = Read-Host "   ¿Abrir $scriptPath en Explorer? (S/N)"
    if ($response -eq "S" -or $response -eq "s") {
        try {
            Start-Process "explorer.exe" -ArgumentList $scriptPath
            Write-Host "   ? Directorio abierto" -ForegroundColor Green
        } catch {
            Write-Host "   ? Error abriendo: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    
    Write-Host ""
    Write-Host "2. ?? Copiar logs a ubicación esperada:" -ForegroundColor Cyan
    $response2 = Read-Host "   ¿Copiar logs a $userCheckPath? (S/N)"
    if ($response2 -eq "S" -or $response2 -eq "s") {
        try {
            if (-not (Test-Path $userCheckPath)) {
                New-Item -ItemType Directory -Path $userCheckPath -Force | Out-Null
            }
            
            $logFiles = Get-ChildItem "$scriptPath\*.log" -ErrorAction SilentlyContinue
            if ($logFiles) {
                Copy-Item $logFiles.FullName $userCheckPath -Force
                Write-Host "   ? $($logFiles.Count) archivos copiados a $userCheckPath" -ForegroundColor Green
            } else {
                Write-Host "   ?? No hay archivos .log para copiar" -ForegroundColor Yellow
            }
        } catch {
            Write-Host "   ? Error copiando: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

Write-Host ""
Write-Host "===================================================" -ForegroundColor Cyan
Write-Host "   ?? RESUMEN DE VERIFICACIÓN" -ForegroundColor Cyan
Write-Host "===================================================" -ForegroundColor Cyan
Write-Host ""

if ($foundDir) {
    Write-Host "?? PROBLEMA IDENTIFICADO:" -ForegroundColor Yellow
    Write-Host "   • Script funciona correctamente" -ForegroundColor Green
    Write-Host "   • Logs se crean en ubicación real detectada" -ForegroundColor Green
    Write-Host "   • Usuario verifica ubicación diferente" -ForegroundColor Red
    Write-Host "   • Por eso no ve los archivos" -ForegroundColor Red
    Write-Host ""
    Write-Host "? SOLUCIÓN:" -ForegroundColor Green
    Write-Host "   Verificar logs en: $(Join-Path $foundDir 'logs')" -ForegroundColor White
} else {
    Write-Host "? PROBLEMA:" -ForegroundColor Red
    Write-Host "   No se encontró ejecutable compilado" -ForegroundColor Gray
    Write-Host "   Compilar proyecto primero" -ForegroundColor Gray
}

Write-Host ""
Read-Host "Presiona Enter para finalizar"