# ═══════════════════════════════════════════════════════════════
# 🔍 VERIFICADOR DE INSTALACIÓN - GestionTime Desktop
# ═══════════════════════════════════════════════════════════════
# Este script verifica que todos los requisitos estén cumplidos
# para ejecutar GestionTime Desktop correctamente.
# ═══════════════════════════════════════════════════════════════

$ErrorActionPreference = "Continue"

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🔍 VERIFICADOR DE INSTALACIÓN - GestionTime Desktop" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Fecha: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host "Ruta: $(Get-Location)" -ForegroundColor Gray
Write-Host ""

$allOk = $true

# ═══════════════════════════════════════════════════════════════
# 1. VERIFICAR WINDOWS APP RUNTIME
# ═══════════════════════════════════════════════════════════════
Write-Host "1️⃣  Verificando Windows App Runtime..." -ForegroundColor Yellow
try {
    $runtimeCheck = winget list --id Microsoft.WindowsAppRuntime.1.8 2>&1 | Out-String
    
    if ($runtimeCheck -match "Microsoft.WindowsAppRuntime.1.8" -or $runtimeCheck -match "WindowsAppRuntime") {
        Write-Host "    ✅ Windows App Runtime 1.8 instalado" -ForegroundColor Green
    } else {
        Write-Host "    ❌ Windows App Runtime 1.8 NO encontrado" -ForegroundColor Red
        Write-Host "" -ForegroundColor Yellow
        Write-Host "    📥 Para instalar, ejecuta:" -ForegroundColor Yellow
        Write-Host "       winget install Microsoft.WindowsAppRuntime.1.8" -ForegroundColor White
        Write-Host "" -ForegroundColor Yellow
        Write-Host "    🔗 O descarga manualmente desde:" -ForegroundColor Yellow
        Write-Host "       https://aka.ms/windowsappsdk/1.8/latest/windowsappruntimeinstall-x64.exe" -ForegroundColor White
        $allOk = $false
    }
} catch {
    Write-Host "    ⚠️  No se pudo verificar (winget no disponible)" -ForegroundColor Yellow
    Write-Host "       Verifica manualmente en Configuración → Aplicaciones" -ForegroundColor Gray
}
Write-Host ""

# ═══════════════════════════════════════════════════════════════
# 2. VERIFICAR ARCHIVOS CRÍTICOS
# ═══════════════════════════════════════════════════════════════
Write-Host "2️⃣  Verificando archivos críticos..." -ForegroundColor Yellow

$criticalFiles = @{
    "GestionTime.Desktop.exe" = "Ejecutable principal"
    "appsettings.json" = "Archivo de configuración"
    "Microsoft.UI.Xaml.dll" = "WinUI 3 core library"
    "WinRT.Runtime.dll" = "Windows Runtime interop"
    "Microsoft.WindowsAppRuntime.Bootstrap.dll" = "App Runtime bootstrap"
}

foreach ($file in $criticalFiles.Keys) {
    if (Test-Path $file) {
        $size = (Get-Item $file).Length
        $sizeKB = [math]::Round($size / 1KB, 2)
        Write-Host "    ✅ $file" -ForegroundColor Green -NoNewline
        Write-Host " ($sizeKB KB)" -ForegroundColor Gray
    } else {
        Write-Host "    ❌ $file FALTANTE - $($criticalFiles[$file])" -ForegroundColor Red
        $allOk = $false
    }
}
Write-Host ""

# ═══════════════════════════════════════════════════════════════
# 3. VERIFICAR ARCHIVOS BLOQUEADOS
# ═══════════════════════════════════════════════════════════════
Write-Host "3️⃣  Verificando archivos bloqueados (Zone.Identifier)..." -ForegroundColor Yellow
try {
    $blockedFiles = @()
    Get-ChildItem -Recurse -File -ErrorAction SilentlyContinue | ForEach-Object {
        try {
            $stream = Get-Item $_.FullName -Stream Zone.Identifier -ErrorAction SilentlyContinue
            if ($stream) {
                $blockedFiles += $_.Name
            }
        } catch {
            # Ignorar errores de acceso a streams
        }
    }
    
    if ($blockedFiles.Count -gt 0) {
        Write-Host "    ⚠️  $($blockedFiles.Count) archivos bloqueados encontrados" -ForegroundColor Yellow
        Write-Host "       Intentando desbloquear..." -ForegroundColor Yellow
        
        try {
            Get-ChildItem -Recurse -ErrorAction SilentlyContinue | Unblock-File -ErrorAction SilentlyContinue
            Write-Host "    ✅ Archivos desbloqueados correctamente" -ForegroundColor Green
        } catch {
            Write-Host "    ⚠️  Algunos archivos no se pudieron desbloquear" -ForegroundColor Yellow
            Write-Host "       Ejecuta este script como Administrador" -ForegroundColor Gray
        }
    } else {
        Write-Host "    ✅ Sin archivos bloqueados" -ForegroundColor Green
    }
} catch {
    Write-Host "    ⚠️  No se pudo verificar archivos bloqueados" -ForegroundColor Yellow
}
Write-Host ""

# ═══════════════════════════════════════════════════════════════
# 4. VERIFICAR Y CREAR CARPETA LOGS
# ═══════════════════════════════════════════════════════════════
Write-Host "4️⃣  Verificando carpeta de logs..." -ForegroundColor Yellow
if (-not (Test-Path "logs")) {
    try {
        New-Item -Path "logs" -ItemType Directory -Force | Out-Null
        Write-Host "    ✅ Carpeta 'logs' creada" -ForegroundColor Green
    } catch {
        Write-Host "    ❌ No se pudo crear carpeta 'logs'" -ForegroundColor Red
        Write-Host "       Error: $($_.Exception.Message)" -ForegroundColor Gray
        $allOk = $false
    }
} else {
    Write-Host "    ✅ Carpeta 'logs' existe" -ForegroundColor Green
}
Write-Host ""

# ═══════════════════════════════════════════════════════════════
# 5. VERIFICAR PERMISOS DE ESCRITURA
# ═══════════════════════════════════════════════════════════════
Write-Host "5️⃣  Verificando permisos de escritura..." -ForegroundColor Yellow
try {
    $testFile = "logs\test_$(Get-Date -Format 'yyyyMMddHHmmss').txt"
    "Test de escritura - $(Get-Date)" | Out-File $testFile -ErrorAction Stop
    
    if (Test-Path $testFile) {
        Remove-Item $testFile -ErrorAction SilentlyContinue
        Write-Host "    ✅ Permisos de escritura OK" -ForegroundColor Green
    } else {
        Write-Host "    ❌ No se pudo escribir en 'logs'" -ForegroundColor Red
        $allOk = $false
    }
} catch {
    Write-Host "    ❌ Sin permisos de escritura en 'logs'" -ForegroundColor Red
    Write-Host "       Error: $($_.Exception.Message)" -ForegroundColor Gray
    Write-Host "" -ForegroundColor Yellow
    Write-Host "       💡 Soluciones:" -ForegroundColor Yellow
    Write-Host "          • Ejecutar como Administrador" -ForegroundColor White
    Write-Host "          • Mover a carpeta de usuario (C:\Users\<username>\GestionTime)" -ForegroundColor White
    Write-Host "          • Dar permisos de escritura a esta carpeta" -ForegroundColor White
    $allOk = $false
}
Write-Host ""

# ═══════════════════════════════════════════════════════════════
# 6. VERIFICAR VERSIÓN DE WINDOWS
# ═══════════════════════════════════════════════════════════════
Write-Host "6️⃣  Verificando versión de Windows..." -ForegroundColor Yellow
try {
    $osVersion = [System.Environment]::OSVersion.Version
    $build = $osVersion.Build
    
    Write-Host "    📊 Windows versión: $($osVersion.Major).$($osVersion.Minor) (Build $build)" -ForegroundColor Cyan
    
    # Windows 10 1809 = Build 17763
    # Windows 10 21H2 = Build 19044
    # Windows 11 = Build 22000+
    
    if ($build -ge 17763) {
        Write-Host "    ✅ Versión compatible (requiere Build 17763+)" -ForegroundColor Green
    } else {
        Write-Host "    ❌ Versión no compatible (requiere Windows 10 1809 o superior)" -ForegroundColor Red
        Write-Host "       Tu build: $build, Requerido: 17763+" -ForegroundColor Gray
        $allOk = $false
    }
} catch {
    Write-Host "    ⚠️  No se pudo verificar versión de Windows" -ForegroundColor Yellow
}
Write-Host ""

# ═══════════════════════════════════════════════════════════════
# 7. VERIFICAR CONFIGURACIÓN (appsettings.json)
# ═══════════════════════════════════════════════════════════════
Write-Host "7️⃣  Verificando configuración (appsettings.json)..." -ForegroundColor Yellow
if (Test-Path "appsettings.json") {
    try {
        $config = Get-Content "appsettings.json" -Raw | ConvertFrom-Json
        
        if ($config.Api) {
            Write-Host "    ✅ Archivo de configuración válido" -ForegroundColor Green
            Write-Host "       • API URL: $($config.Api.BaseUrl)" -ForegroundColor Gray
            Write-Host "       • Login Path: $($config.Api.LoginPath)" -ForegroundColor Gray
            
            # Verificar que no apunte a localhost (error común)
            if ($config.Api.BaseUrl -match "localhost|127\.0\.0\.1") {
                Write-Host "    ⚠️  ADVERTENCIA: API apunta a localhost" -ForegroundColor Yellow
                Write-Host "       Esto solo funcionará si tienes el backend corriendo localmente" -ForegroundColor Gray
            }
        } else {
            Write-Host "    ⚠️  Configuración incompleta (falta sección Api)" -ForegroundColor Yellow
            $allOk = $false
        }
    } catch {
        Write-Host "    ❌ Error leyendo appsettings.json" -ForegroundColor Red
        Write-Host "       Error: $($_.Exception.Message)" -ForegroundColor Gray
        $allOk = $false
    }
} else {
    Write-Host "    ❌ appsettings.json no encontrado" -ForegroundColor Red
    $allOk = $false
}
Write-Host ""

# ═══════════════════════════════════════════════════════════════
# 8. VERIFICAR .NET RUNTIME (SI ES FRAMEWORK-DEPENDENT)
# ═══════════════════════════════════════════════════════════════
Write-Host "8️⃣  Verificando .NET Runtime..." -ForegroundColor Yellow
try {
    $dotnetCheck = dotnet --list-runtimes 2>&1 | Out-String
    
    if ($dotnetCheck -match "Microsoft.WindowsDesktop.App 8\.0") {
        Write-Host "    ✅ .NET 8 Desktop Runtime instalado" -ForegroundColor Green
    } elseif ($dotnetCheck -match "Microsoft.NETCore.App") {
        Write-Host "    ℹ️  .NET encontrado (verificar versión)" -ForegroundColor Cyan
        Write-Host "       Si es self-contained, este check no es necesario" -ForegroundColor Gray
    } else {
        Write-Host "    ⚠️  .NET Runtime no detectado" -ForegroundColor Yellow
        Write-Host "       Si tu build es self-contained, esto es normal" -ForegroundColor Gray
    }
} catch {
    Write-Host "    ℹ️  dotnet CLI no disponible (normal si es self-contained)" -ForegroundColor Cyan
}
Write-Host ""

# ═══════════════════════════════════════════════════════════════
# RESUMEN FINAL
# ═══════════════════════════════════════════════════════════════
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
if ($allOk) {
    Write-Host "✅ VERIFICACIÓN COMPLETADA - TODO OK" -ForegroundColor Green
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "🚀 La aplicación debería iniciar correctamente." -ForegroundColor Green
    Write-Host ""
    Write-Host "Para ejecutar:" -ForegroundColor Yellow
    Write-Host "   .\GestionTime.Desktop.exe" -ForegroundColor White
    Write-Host ""
    Write-Host "Si aún no inicia, revisa Event Viewer de Windows:" -ForegroundColor Yellow
    Write-Host "   Win+R → eventvwr.msc → Windows Logs → Application" -ForegroundColor White
} else {
    Write-Host "⚠️  VERIFICACIÓN COMPLETADA - PROBLEMAS DETECTADOS" -ForegroundColor Yellow
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "❌ Revisa los errores marcados arriba y corrígelos." -ForegroundColor Red
    Write-Host ""
    Write-Host "📝 Pasos recomendados:" -ForegroundColor Yellow
    Write-Host "   1. Instalar Windows App Runtime (si falta)" -ForegroundColor White
    Write-Host "   2. Desbloquear archivos (si están bloqueados)" -ForegroundColor White
    Write-Host "   3. Verificar permisos de escritura" -ForegroundColor White
    Write-Host "   4. Ejecutar como Administrador (primera vez)" -ForegroundColor White
    Write-Host ""
    Write-Host "💡 Si necesitas más ayuda, consulta:" -ForegroundColor Yellow
    Write-Host "   DIAGNOSTICO_NO_ARRANCA.md" -ForegroundColor White
}
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Ofrecer ejecutar la app
if ($allOk) {
    $execute = Read-Host "¿Deseas ejecutar la aplicación ahora? (S/N)"
    if ($execute -eq "S" -or $execute -eq "s") {
        Write-Host ""
        Write-Host "🚀 Iniciando GestionTime Desktop..." -ForegroundColor Cyan
        Start-Process ".\GestionTime.Desktop.exe"
    }
}

Write-Host "Presiona cualquier tecla para salir..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
