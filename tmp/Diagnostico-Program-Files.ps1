# ═══════════════════════════════════════════════════════════════
# 🔍 DIAGNÓSTICO ESPECÍFICO PARA INSTALACIÓN EN PROGRAM FILES
# GestionTime Desktop - Troubleshooting
# ═══════════════════════════════════════════════════════════════

$ErrorActionPreference = "Continue"
$installPath = "C:\Program Files\GestionTime"

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🔍 DIAGNÓSTICO - Instalación en Program Files" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "📁 Ruta de instalación: $installPath" -ForegroundColor Yellow
Write-Host "⏰ Fecha: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host ""

# ═══════════════════════════════════════════════════════════════
# VERIFICAR SI SE EJECUTA COMO ADMINISTRADOR
# ═══════════════════════════════════════════════════════════════
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if ($isAdmin) {
    Write-Host "✅ Ejecutando como Administrador" -ForegroundColor Green
} else {
    Write-Host "⚠️  NO ejecutando como Administrador" -ForegroundColor Yellow
    Write-Host "   Algunas operaciones pueden fallar" -ForegroundColor Gray
    Write-Host ""
    Write-Host "💡 Para mejores resultados, ejecuta este script como Administrador:" -ForegroundColor Yellow
    Write-Host "   1. Click derecho en PowerShell" -ForegroundColor White
    Write-Host "   2. Seleccionar 'Ejecutar como administrador'" -ForegroundColor White
    Write-Host "   3. Navegar a: cd 'C:\Program Files\GestionTime'" -ForegroundColor White
    Write-Host "   4. Ejecutar: .\Diagnostico-Program-Files.ps1" -ForegroundColor White
    Write-Host ""
}

# ═══════════════════════════════════════════════════════════════
# 1. VERIFICAR QUE LA CARPETA EXISTE
# ═══════════════════════════════════════════════════════════════
Write-Host "1️⃣  Verificando existencia de carpeta..." -ForegroundColor Yellow

if (Test-Path $installPath) {
    Write-Host "   ✅ Carpeta existe: $installPath" -ForegroundColor Green
    
    # Listar contenido
    $fileCount = (Get-ChildItem -Path $installPath -File).Count
    $folderCount = (Get-ChildItem -Path $installPath -Directory).Count
    Write-Host "   📊 Archivos: $fileCount, Carpetas: $folderCount" -ForegroundColor Cyan
} else {
    Write-Host "   ❌ Carpeta NO existe: $installPath" -ForegroundColor Red
    Write-Host "   La aplicación no está instalada en esta ubicación" -ForegroundColor Gray
    Write-Host ""
    Pause
    exit 1
}
Write-Host ""

# ═══════════════════════════════════════════════════════════════
# 2. VERIFICAR PERMISOS DE LECTURA/ESCRITURA
# ═══════════════════════════════════════════════════════════════
Write-Host "2️⃣  Verificando permisos en Program Files..." -ForegroundColor Yellow

# Verificar lectura
try {
    $testRead = Get-ChildItem -Path $installPath -ErrorAction Stop | Out-Null
    Write-Host "   ✅ Permisos de LECTURA: OK" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Sin permisos de LECTURA" -ForegroundColor Red
    Write-Host "      Error: $($_.Exception.Message)" -ForegroundColor Gray
}

# Verificar escritura (crear archivo de test en logs)
$testLogPath = Join-Path $installPath "logs"
if (-not (Test-Path $testLogPath)) {
    try {
        New-Item -Path $testLogPath -ItemType Directory -Force -ErrorAction Stop | Out-Null
        Write-Host "   ✅ Permisos de ESCRITURA: OK (carpeta logs creada)" -ForegroundColor Green
    } catch {
        Write-Host "   ❌ Sin permisos de ESCRITURA para crear carpeta logs" -ForegroundColor Red
        Write-Host "      Error: $($_.Exception.Message)" -ForegroundColor Gray
        Write-Host ""
        Write-Host "      ⚠️  PROBLEMA CRÍTICO: La aplicación necesita escribir logs" -ForegroundColor Yellow
        Write-Host "         Solución: Ejecutar la aplicación como Administrador (primera vez)" -ForegroundColor White
    }
} else {
    Write-Host "   ✅ Carpeta 'logs' ya existe" -ForegroundColor Green
    
    # Test de escritura en logs
    $testFile = Join-Path $testLogPath "test_$(Get-Date -Format 'yyyyMMddHHmmss').tmp"
    try {
        "Test" | Out-File -FilePath $testFile -ErrorAction Stop
        Remove-Item $testFile -ErrorAction SilentlyContinue
        Write-Host "   ✅ Permisos de ESCRITURA en logs: OK" -ForegroundColor Green
    } catch {
        Write-Host "   ❌ Sin permisos de ESCRITURA en logs" -ForegroundColor Red
        Write-Host "      Error: $($_.Exception.Message)" -ForegroundColor Gray
        Write-Host ""
        Write-Host "      ⚠️  PROBLEMA: La aplicación no puede escribir logs" -ForegroundColor Yellow
        Write-Host "         Solución 1: Ejecutar como Administrador" -ForegroundColor White
        Write-Host "         Solución 2: Mover logs a LocalAppData (modificar appsettings.json)" -ForegroundColor White
    }
}
Write-Host ""

# ═══════════════════════════════════════════════════════════════
# 3. VERIFICAR ARCHIVOS CRÍTICOS
# ═══════════════════════════════════════════════════════════════
Write-Host "3️⃣  Verificando archivos críticos..." -ForegroundColor Yellow

$criticalFiles = @{
    "GestionTime.Desktop.exe" = "Ejecutable principal"
    "appsettings.json" = "Configuración"
    "Microsoft.UI.Xaml.dll" = "WinUI 3"
    "WinRT.Runtime.dll" = "Windows Runtime"
    "Microsoft.WindowsAppRuntime.Bootstrap.dll" = "App Runtime"
}

$allFilesPresent = $true
foreach ($file in $criticalFiles.Keys) {
    $filePath = Join-Path $installPath $file
    if (Test-Path $filePath) {
        $size = (Get-Item $filePath).Length
        $sizeKB = [math]::Round($size / 1KB, 2)
        Write-Host "   ✅ $file" -ForegroundColor Green -NoNewline
        Write-Host " ($sizeKB KB)" -ForegroundColor Gray
    } else {
        Write-Host "   ❌ $file FALTANTE - $($criticalFiles[$file])" -ForegroundColor Red
        $allFilesPresent = $false
    }
}

if (-not $allFilesPresent) {
    Write-Host ""
    Write-Host "   ⚠️  Archivos críticos faltantes - Reinstalar la aplicación" -ForegroundColor Yellow
}
Write-Host ""

# ═══════════════════════════════════════════════════════════════
# 4. VERIFICAR Windows App Runtime
# ═══════════════════════════════════════════════════════════════
Write-Host "4️⃣  Verificando Windows App Runtime..." -ForegroundColor Yellow

try {
    $runtimeCheck = winget list --id Microsoft.WindowsAppRuntime.1.8 2>&1 | Out-String
    
    if ($runtimeCheck -match "Microsoft.WindowsAppRuntime.1.8" -or $runtimeCheck -match "1\.8") {
        Write-Host "   ✅ Windows App Runtime 1.8 instalado" -ForegroundColor Green
    } else {
        Write-Host "   ❌ Windows App Runtime 1.8 NO encontrado" -ForegroundColor Red
        Write-Host ""
        Write-Host "      📥 INSTALAR AHORA:" -ForegroundColor Yellow
        
        if ($isAdmin) {
            $install = Read-Host "      ¿Instalar Windows App Runtime automáticamente? (S/N)"
            if ($install -eq "S" -or $install -eq "s") {
                Write-Host ""
                Write-Host "      Instalando..." -ForegroundColor Cyan
                winget install Microsoft.WindowsAppRuntime.1.8 --silent --accept-package-agreements --accept-source-agreements
                
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "      ✅ Instalación completada" -ForegroundColor Green
                } else {
                    Write-Host "      ❌ Error en instalación" -ForegroundColor Red
                }
            }
        } else {
            Write-Host "      Ejecuta como Administrador:" -ForegroundColor White
            Write-Host "      winget install Microsoft.WindowsAppRuntime.1.8" -ForegroundColor Cyan
        }
        
        Write-Host ""
        Write-Host "      🔗 O descarga manual:" -ForegroundColor Yellow
        Write-Host "      https://aka.ms/windowsappsdk/1.8/latest/windowsappruntimeinstall-x64.exe" -ForegroundColor Cyan
    }
} catch {
    Write-Host "   ⚠️  No se pudo verificar (winget no disponible)" -ForegroundColor Yellow
}
Write-Host ""

# ═══════════════════════════════════════════════════════════════
# 5. VERIFICAR ARCHIVOS BLOQUEADOS
# ═══════════════════════════════════════════════════════════════
Write-Host "5️⃣  Verificando archivos bloqueados (Zone.Identifier)..." -ForegroundColor Yellow

try {
    Push-Location $installPath
    
    $blockedFiles = @()
    Get-ChildItem -Recurse -File -ErrorAction SilentlyContinue | ForEach-Object {
        try {
            $stream = Get-Item $_.FullName -Stream Zone.Identifier -ErrorAction SilentlyContinue
            if ($stream) {
                $blockedFiles += $_.Name
            }
        } catch {
            # Ignorar
        }
    }
    
    if ($blockedFiles.Count -gt 0) {
        Write-Host "   ⚠️  $($blockedFiles.Count) archivos bloqueados encontrados" -ForegroundColor Yellow
        
        if ($isAdmin) {
            Write-Host "      Intentando desbloquear..." -ForegroundColor Yellow
            try {
                Get-ChildItem -Recurse -ErrorAction SilentlyContinue | Unblock-File -ErrorAction SilentlyContinue
                Write-Host "   ✅ Archivos desbloqueados" -ForegroundColor Green
            } catch {
                Write-Host "   ⚠️  Algunos archivos no se pudieron desbloquear" -ForegroundColor Yellow
            }
        } else {
            Write-Host "      Ejecuta como Administrador para desbloquear" -ForegroundColor Gray
        }
    } else {
        Write-Host "   ✅ Sin archivos bloqueados" -ForegroundColor Green
    }
    
    Pop-Location
} catch {
    Write-Host "   ⚠️  No se pudo verificar archivos bloqueados" -ForegroundColor Yellow
    Write-Host "      Error: $($_.Exception.Message)" -ForegroundColor Gray
}
Write-Host ""

# ═══════════════════════════════════════════════════════════════
# 6. REVISAR EMERGENCY LOG
# ═══════════════════════════════════════════════════════════════
Write-Host "6️⃣  Buscando logs de emergencia..." -ForegroundColor Yellow

$emergencyLogPath = Join-Path $env:LOCALAPPDATA "GestionTime\emergency.log"

if (Test-Path $emergencyLogPath) {
    Write-Host "   📝 Emergency log encontrado: $emergencyLogPath" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "   Últimas 10 líneas:" -ForegroundColor Yellow
    Get-Content $emergencyLogPath -Tail 10 | ForEach-Object {
        Write-Host "      $_" -ForegroundColor Gray
    }
} else {
    Write-Host "   ℹ️  No hay emergency log (la app nunca intentó iniciar)" -ForegroundColor Cyan
}
Write-Host ""

# ═══════════════════════════════════════════════════════════════
# 7. REVISAR EVENT VIEWER
# ═══════════════════════════════════════════════════════════════
Write-Host "7️⃣  Revisando Event Viewer (errores recientes)..." -ForegroundColor Yellow

try {
    $recentErrors = Get-EventLog -LogName Application -Source ".NET Runtime" -Newest 5 -After (Get-Date).AddHours(-1) -ErrorAction SilentlyContinue |
        Where-Object { $_.EntryType -eq "Error" }
    
    if ($recentErrors) {
        Write-Host "   ⚠️  Errores de .NET Runtime encontrados:" -ForegroundColor Yellow
        foreach ($error in $recentErrors) {
            Write-Host "      [$($error.TimeGenerated)] $($error.Message.Substring(0, [Math]::Min(100, $error.Message.Length)))..." -ForegroundColor Gray
        }
        Write-Host ""
        Write-Host "      💡 Para ver detalles completos:" -ForegroundColor Cyan
        Write-Host "         eventvwr.msc → Windows Logs → Application → .NET Runtime" -ForegroundColor White
    } else {
        Write-Host "   ✅ Sin errores recientes de .NET Runtime" -ForegroundColor Green
    }
} catch {
    Write-Host "   ℹ️  No se pudo acceder a Event Viewer" -ForegroundColor Cyan
}
Write-Host ""

# ═══════════════════════════════════════════════════════════════
# 8. VERIFICAR CONFIGURACIÓN (appsettings.json)
# ═══════════════════════════════════════════════════════════════
Write-Host "8️⃣  Verificando appsettings.json..." -ForegroundColor Yellow

$appsettingsPath = Join-Path $installPath "appsettings.json"

if (Test-Path $appsettingsPath) {
    try {
        $config = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
        Write-Host "   ✅ Archivo válido" -ForegroundColor Green
        
        if ($config.Api) {
            Write-Host "      • API URL: $($config.Api.BaseUrl)" -ForegroundColor Gray
            Write-Host "      • Login Path: $($config.Api.LoginPath)" -ForegroundColor Gray
            
            if ($config.Logging -and $config.Logging.LogPath) {
                Write-Host "      • Log Path: $($config.Logging.LogPath)" -ForegroundColor Gray
            }
        }
    } catch {
        Write-Host "   ❌ Error leyendo JSON: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "   ❌ appsettings.json no encontrado" -ForegroundColor Red
}
Write-Host ""

# ═══════════════════════════════════════════════════════════════
# RESUMEN Y RECOMENDACIONES
# ═══════════════════════════════════════════════════════════════
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "📊 RESUMEN Y RECOMENDACIONES" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

if (-not $isAdmin) {
    Write-Host "⚠️  RECOMENDACIÓN PRINCIPAL:" -ForegroundColor Yellow
    Write-Host "   La aplicación está en Program Files y NO ejecutaste este script" -ForegroundColor White
    Write-Host "   como Administrador. Esto puede causar problemas de permisos." -ForegroundColor White
    Write-Host ""
    Write-Host "   🎯 SOLUCIÓN:" -ForegroundColor Green
    Write-Host "   1. Click derecho en GestionTime.Desktop.exe" -ForegroundColor White
    Write-Host "   2. Seleccionar 'Ejecutar como administrador'" -ForegroundColor White
    Write-Host "   3. Permitir UAC cuando aparezca" -ForegroundColor White
    Write-Host "   4. La app creará carpetas/archivos necesarios" -ForegroundColor White
    Write-Host "   5. Después de la primera ejecución, ya no será necesario Admin" -ForegroundColor White
    Write-Host ""
}

Write-Host "💡 ALTERNATIVA RECOMENDADA:" -ForegroundColor Cyan
Write-Host "   Para evitar problemas de permisos en Program Files," -ForegroundColor White
Write-Host "   considera instalar en una ubicación de usuario:" -ForegroundColor White
Write-Host ""
Write-Host "   📁 Opción 1: C:\Users\$env:USERNAME\AppData\Local\GestionTime" -ForegroundColor Gray
Write-Host "   📁 Opción 2: C:\Users\$env:USERNAME\GestionTime" -ForegroundColor Gray
Write-Host "   📁 Opción 3: D:\GestionTime (si tienes otro disco)" -ForegroundColor Gray
Write-Host ""

# Ofrecer abrir Event Viewer
$openEventViewer = Read-Host "¿Abrir Event Viewer para ver errores? (S/N)"
if ($openEventViewer -eq "S" -or $openEventViewer -eq "s") {
    Write-Host ""
    Write-Host "Abriendo Event Viewer..." -ForegroundColor Cyan
    eventvwr.msc
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "Presiona cualquier tecla para salir..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
