$appPath = "C:\app\GestionTimeDesktop"

Write-Host ""
Write-Host "🔧 DIAGNÓSTICO DIRECTO DE LA APP INSTALADA" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""

if (!(Test-Path $appPath)) {
    Write-Host "❌ Directorio no encontrado: $appPath" -ForegroundColor Red
    exit
}

Write-Host "📁 Directorio encontrado: $appPath" -ForegroundColor Green
Write-Host ""

# Verificar archivos críticos
Write-Host "🔍 VERIFICANDO ARCHIVOS CRÍTICOS:" -ForegroundColor Cyan
$criticalFiles = @(
    "GestionTime.Desktop.exe",
    "GestionTime.Desktop.dll",
    "Microsoft.WindowsAppRuntime.dll",
    "appsettings.json"
)

foreach ($file in $criticalFiles) {
    $filePath = Join-Path $appPath $file
    if (Test-Path $filePath) {
        $fileInfo = Get-Item $filePath
        Write-Host "   ✅ $file ($([math]::Round($fileInfo.Length/1KB, 1)) KB)" -ForegroundColor Green
    } else {
        Write-Host "   ❌ $file (FALTANTE)" -ForegroundColor Red
    }
}

Write-Host ""

# Verificar appsettings.json
Write-Host "📋 VERIFICANDO CONFIGURACIÓN:" -ForegroundColor Cyan
$appSettingsPath = Join-Path $appPath "appsettings.json"
if (Test-Path $appSettingsPath) {
    try {
        $config = Get-Content $appSettingsPath | ConvertFrom-Json
        Write-Host "   ✅ appsettings.json válido" -ForegroundColor Green
        Write-Host "   📡 API URL: $($config.Api.BaseUrl)" -ForegroundColor White
    } catch {
        Write-Host "   ❌ appsettings.json corrupto: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "   ❌ appsettings.json no encontrado" -ForegroundColor Red
}

Write-Host ""

# Verificar recursos WinUI
Write-Host "🎨 VERIFICANDO RECURSOS WINUI:" -ForegroundColor Cyan
$priFiles = Get-ChildItem $appPath -Filter "*.pri" -ErrorAction SilentlyContinue
Write-Host "   📦 Archivos .pri: $($priFiles.Count)" -ForegroundColor White
foreach ($pri in $priFiles) {
    Write-Host "      - $($pri.Name)" -ForegroundColor Gray
}

Write-Host ""

# Intentar ejecutar y capturar errores
Write-Host "🚀 INTENTANDO EJECUTAR APLICACIÓN:" -ForegroundColor Cyan
$exePath = Join-Path $appPath "GestionTime.Desktop.exe"

try {
    Write-Host "   📍 Cambiando al directorio de la app..." -ForegroundColor Yellow
    Set-Location $appPath
    
    Write-Host "   🔄 Iniciando proceso..." -ForegroundColor Yellow
    
    # Intentar ejecutar con diferentes métodos
    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = $exePath
    $startInfo.WorkingDirectory = $appPath
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.CreateNoWindow = $true
    
    $process = [System.Diagnostics.Process]::Start($startInfo)
    
    # Esperar un poco para ver si arranca
    Start-Sleep -Seconds 5
    
    if ($process.HasExited) {
        $exitCode = $process.ExitCode
        $stdout = $process.StandardOutput.ReadToEnd()
        $stderr = $process.StandardError.ReadToEnd()
        
        Write-Host "   ❌ La aplicación terminó inmediatamente" -ForegroundColor Red
        Write-Host "   🔢 Código de salida: $exitCode" -ForegroundColor Red
        
        if ($stdout) {
            Write-Host "   📤 Output:" -ForegroundColor Yellow
            Write-Host "      $stdout" -ForegroundColor White
        }
        
        if ($stderr) {
            Write-Host "   📤 Error:" -ForegroundColor Yellow
            Write-Host "      $stderr" -ForegroundColor White
        }
        
        # Verificar logs de eventos de Windows
        Write-Host ""
        Write-Host "   🔍 Verificando eventos de Windows..." -ForegroundColor Yellow
        try {
            $events = Get-WinEvent -FilterHashtable @{LogName='Application'; StartTime=(Get-Date).AddMinutes(-5)} -MaxEvents 10 -ErrorAction SilentlyContinue | 
                      Where-Object { $_.Message -like "*GestionTime*" -or $_.Message -like "*WindowsAppRuntime*" }
            
            if ($events) {
                Write-Host "   📋 Eventos relacionados encontrados:" -ForegroundColor Yellow
                foreach ($event in $events) {
                    Write-Host "      ⚠️  $($event.TimeCreated): $($event.LevelDisplayName) - $($event.Message)" -ForegroundColor Red
                }
            } else {
                Write-Host "   ℹ️  Sin eventos relacionados en los últimos 5 minutos" -ForegroundColor Gray
            }
        } catch {
            Write-Host "   ⚠️  No se pudieron leer eventos: $($_.Exception.Message)" -ForegroundColor Yellow
        }
        
    } else {
        Write-Host "   ✅ La aplicación está ejecutándose (PID: $($process.Id))" -ForegroundColor Green
        Write-Host "   🔄 Esperando 10 segundos más..." -ForegroundColor Yellow
        Start-Sleep -Seconds 10
        
        if (-not $process.HasExited) {
            Write-Host "   ✅ Aplicación funcionando correctamente!" -ForegroundColor Green
            $process.CloseMainWindow()
        } else {
            Write-Host "   ❌ La aplicación se cerró después de ejecutarse" -ForegroundColor Red
        }
    }
    
    $process.Dispose()
    
} catch {
    Write-Host "   ❌ ERROR AL EJECUTAR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   📋 Tipo de error: $($_.Exception.GetType().Name)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "💡 POSIBLES SOLUCIONES:" -ForegroundColor Blue
Write-Host "=====================" -ForegroundColor Blue
Write-Host ""
Write-Host "1️⃣ Ejecutar manualmente desde la línea de comandos:" -ForegroundColor Yellow
Write-Host "   cd `"$appPath`"" -ForegroundColor White
Write-Host "   .\GestionTime.Desktop.exe" -ForegroundColor White
Write-Host ""
Write-Host "2️⃣ Verificar si falta Visual C++ Redistributable:" -ForegroundColor Yellow
Write-Host "   Instalar: https://aka.ms/vs/17/release/vc_redist.x64.exe" -ForegroundColor White
Write-Host ""
Write-Host "3️⃣ Ejecutar como administrador:" -ForegroundColor Yellow
Write-Host "   Click derecho → Ejecutar como administrador" -ForegroundColor White
Write-Host ""
Write-Host "4️⃣ Verificar Windows Update:" -ForegroundColor Yellow
Write-Host "   Actualizar Windows a la última versión" -ForegroundColor White

Write-Host ""
Write-Host "🔧 DIAGNÓSTICO COMPLETADO" -ForegroundColor Green
Write-Host "========================" -ForegroundColor Green