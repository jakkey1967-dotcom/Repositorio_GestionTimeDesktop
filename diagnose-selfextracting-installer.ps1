param(
    [string]$InstallerPath = "bin\Release\SelfExtractingInstaller\GestionTimeDesktopInstaller.bat",
    [switch]$TestOnly,
    [switch]$Verbose
)

Write-Host ""
Write-Host "🔍 DIAGNÓSTICO DEL INSTALADOR AUTO-EXTRAÍBLE" -ForegroundColor Green -BackgroundColor DarkGreen
Write-Host "===============================================" -ForegroundColor Green
Write-Host ""

$ErrorActionPreference = "Continue"

try {
    # Verificar que el instalador existe
    if (!(Test-Path $InstallerPath)) {
        Write-Host "❌ ERROR: Instalador no encontrado en $InstallerPath" -ForegroundColor Red
        Write-Host "   Crear instalador: .\create-selfextracting-installer.ps1 -Rebuild" -ForegroundColor Yellow
        return
    }

    $installerFile = Get-Item $InstallerPath
    Write-Host "📊 INFORMACIÓN DEL INSTALADOR:" -ForegroundColor Cyan
    Write-Host "   • Archivo: $($installerFile.Name)" -ForegroundColor White
    Write-Host "   • Tamaño: $([math]::Round($installerFile.Length/1MB, 2)) MB" -ForegroundColor White
    Write-Host "   • Fecha: $($installerFile.LastWriteTime)" -ForegroundColor White
    Write-Host "   • Ubicación: $($installerFile.FullName)" -ForegroundColor White
    Write-Host ""

    # Verificar estructura del instalador
    Write-Host "🔍 ANALIZANDO ESTRUCTURA DEL INSTALADOR:" -ForegroundColor Cyan
    
    try {
        # Leer las primeras líneas para verificar el script
        $firstLines = Get-Content $InstallerPath -Head 50
        $scriptLines = $firstLines | Where-Object { $_ -notlike "__ZIP_DATA__*" -and $_ -notlike "*base64*" }
        
        Write-Host "   ✅ Script legible: $($scriptLines.Count) líneas de código" -ForegroundColor Green
        
        # Buscar marcador de datos
        $zipMarker = $firstLines | Where-Object { $_ -like "*__ZIP_DATA__*" }
        if ($zipMarker) {
            Write-Host "   ✅ Marcador de datos ZIP encontrado" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️  Marcador ZIP no encontrado en primeras líneas" -ForegroundColor Yellow
        }
        
    } catch {
        Write-Host "   ❌ Error al leer estructura: $($_.Exception.Message)" -ForegroundColor Red
    }

    # Verificar aplicación fuente
    Write-Host ""
    Write-Host "🔍 VERIFICANDO APLICACIÓN FUENTE:" -ForegroundColor Cyan
    
    $sourceAppPath = "bin\Release\Installer\App"
    if (Test-Path $sourceAppPath) {
        $sourceFiles = Get-ChildItem $sourceAppPath -File -Recurse
        Write-Host "   ✅ Aplicación fuente: $($sourceFiles.Count) archivos" -ForegroundColor Green
        
        $criticalFiles = @(
            "GestionTime.Desktop.exe",
            "GestionTime.Desktop.dll",
            "appsettings.json"
        )
        
        foreach ($file in $criticalFiles) {
            $filePath = Join-Path $sourceAppPath $file
            if (Test-Path $filePath) {
                Write-Host "     ✅ $file" -ForegroundColor Green
            } else {
                Write-Host "     ❌ $file (FALTANTE)" -ForegroundColor Red
            }
        }
    } else {
        Write-Host "   ❌ Aplicación fuente no encontrada" -ForegroundColor Red
        Write-Host "     Ejecutar: .\build-for-installer.ps1 -Clean" -ForegroundColor Yellow
    }

    # Verificar permisos
    Write-Host ""
    Write-Host "🔐 VERIFICANDO PERMISOS:" -ForegroundColor Cyan
    
    # Verificar si se está ejecutando como administrador
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $isAdmin = ([Security.Principal.WindowsPrincipal] $currentUser).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    
    if ($isAdmin) {
        Write-Host "   ✅ Ejecutándose como administrador" -ForegroundColor Green
    } else {
        Write-Host "   ❌ NO se está ejecutando como administrador" -ForegroundColor Red
        Write-Host "     El instalador requiere permisos de administrador" -ForegroundColor Yellow
    }

    # Verificar acceso de escritura a Program Files
    $programFilesTest = "$env:ProgramFiles\test_write_access.tmp"
    try {
        "test" | Out-File $programFilesTest -ErrorAction Stop
        Remove-Item $programFilesTest -ErrorAction SilentlyContinue
        Write-Host "   ✅ Acceso de escritura a Program Files" -ForegroundColor Green
    } catch {
        Write-Host "   ❌ Sin acceso de escritura a Program Files" -ForegroundColor Red
    }

    # Verificar herramientas necesarias
    Write-Host ""
    Write-Host "🛠️  VERIFICANDO HERRAMIENTAS:" -ForegroundColor Cyan
    
    # PowerShell
    if ($PSVersionTable.PSVersion.Major -ge 5) {
        Write-Host "   ✅ PowerShell $($PSVersionTable.PSVersion)" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️  PowerShell versión antigua: $($PSVersionTable.PSVersion)" -ForegroundColor Yellow
    }
    
    # Comandos del sistema
    $systemCommands = @("xcopy", "timeout", "taskkill", "mkdir", "rmdir")
    foreach ($cmd in $systemCommands) {
        try {
            $null = Get-Command $cmd -ErrorAction Stop
            Write-Host "   ✅ $cmd disponible" -ForegroundColor Green
        } catch {
            Write-Host "   ❌ $cmd NO disponible" -ForegroundColor Red
        }
    }

    if ($TestOnly) {
        Write-Host ""
        Write-Host "⚠️  MODO TEST-ONLY - No se ejecutará el instalador" -ForegroundColor Yellow
        return
    }

    # Prueba de extracción (simulada)
    Write-Host ""
    Write-Host "🧪 PRUEBA DE EXTRACCIÓN SIMULADA:" -ForegroundColor Blue
    
    try {
        # Crear directorio temporal
        $tempTestDir = "$env:TEMP\GestionTimeInstallerTest"
        if (Test-Path $tempTestDir) {
            Remove-Item $tempTestDir -Recurse -Force
        }
        New-Item -ItemType Directory -Path $tempTestDir -Force | Out-Null
        
        Write-Host "   ✅ Directorio temporal creado: $tempTestDir" -ForegroundColor Green
        
        # Intentar ejecutar la parte de extracción del instalador
        # (esto es una simulación, no la instalación completa)
        
        Write-Host "   🔍 Intentando validar estructura interna..." -ForegroundColor White
        
        # Leer el final del archivo donde deberían estar los datos ZIP
        $fileBytes = [System.IO.File]::ReadAllBytes($installerFile.FullName)
        $fileSize = $fileBytes.Length
        
        # Buscar marcador de inicio de datos ZIP
        $zipDataStart = -1
        $searchString = [System.Text.Encoding]::ASCII.GetBytes("__ZIP_DATA__")
        
        for ($i = 0; $i -lt $fileSize - $searchString.Length; $i++) {
            $match = $true
            for ($j = 0; $j -lt $searchString.Length; $j++) {
                if ($fileBytes[$i + $j] -ne $searchString[$j]) {
                    $match = $false
                    break
                }
            }
            if ($match) {
                $zipDataStart = $i + $searchString.Length + 1
                break
            }
        }
        
        if ($zipDataStart -gt 0 -and $zipDataStart -lt $fileSize) {
            $dataSize = $fileSize - $zipDataStart
            Write-Host "   ✅ Datos ZIP encontrados: $([math]::Round($dataSize/1MB, 2)) MB" -ForegroundColor Green
            
            # Verificar que no son solo caracteres base64 vacíos
            if ($dataSize -gt 1MB) {
                Write-Host "   ✅ Tamaño de datos parece válido" -ForegroundColor Green
            } else {
                Write-Host "   ⚠️  Datos muy pequeños, posible problema" -ForegroundColor Yellow
            }
            
        } else {
            Write-Host "   ❌ Datos ZIP no encontrados o corruptos" -ForegroundColor Red
        }
        
        # Limpiar
        Remove-Item $tempTestDir -Recurse -Force -ErrorAction SilentlyContinue
        
    } catch {
        Write-Host "   ❌ Error en prueba de extracción: $($_.Exception.Message)" -ForegroundColor Red
    }

    # Ejecutar instalador con captura de errores
    Write-Host ""
    Write-Host "🚀 EJECUTANDO INSTALADOR CON DIAGNÓSTICO:" -ForegroundColor Blue
    
    if (!$isAdmin) {
        Write-Host "   ⚠️  ADVERTENCIA: Instalador requiere permisos de administrador" -ForegroundColor Yellow
        Write-Host "   Para ejecutar correctamente:" -ForegroundColor Yellow
        Write-Host "   1. Abrir PowerShell como Administrador" -ForegroundColor White
        Write-Host "   2. Navegar a: $((Get-Location).Path)" -ForegroundColor White
        Write-Host "   3. Ejecutar: .\$InstallerPath" -ForegroundColor White
        Write-Host ""
        $continuar = Read-Host "¿Intentar ejecutar de todas formas? (S/N)"
        if ($continuar -ne "S" -and $continuar -ne "s") {
            return
        }
    }

    try {
        Write-Host "   🔄 Iniciando instalador..." -ForegroundColor White
        
        # Crear proceso para capturar salida
        $psi = New-Object System.Diagnostics.ProcessStartInfo
        $psi.FileName = "cmd.exe"
        $psi.Arguments = "/c `"$($installerFile.FullName)`""
        $psi.UseShellExecute = $false
        $psi.RedirectStandardOutput = $true
        $psi.RedirectStandardError = $true
        $psi.CreateNoWindow = $true
        
        $process = [System.Diagnostics.Process]::Start($psi)
        
        # Esperar a que termine o un tiempo máximo
        $timeout = 300000 # 5 minutos
        if ($process.WaitForExit($timeout)) {
            $exitCode = $process.ExitCode
            $stdout = $process.StandardOutput.ReadToEnd()
            $stderr = $process.StandardError.ReadToEnd()
            
            Write-Host ""
            Write-Host "📋 RESULTADO DE LA INSTALACIÓN:" -ForegroundColor Magenta
            Write-Host "   • Código de salida: $exitCode" -ForegroundColor White
            
            if ($exitCode -eq 0) {
                Write-Host "   ✅ Instalación aparentemente exitosa" -ForegroundColor Green
            } else {
                Write-Host "   ❌ Instalación falló (código $exitCode)" -ForegroundColor Red
            }
            
            if ($stdout -and $Verbose) {
                Write-Host ""
                Write-Host "📄 SALIDA ESTÁNDAR:" -ForegroundColor Cyan
                Write-Host $stdout -ForegroundColor White
            }
            
            if ($stderr) {
                Write-Host ""
                Write-Host "🔴 ERRORES:" -ForegroundColor Red
                Write-Host $stderr -ForegroundColor Red
            }
            
        } else {
            Write-Host "   ⏱️  Instalación tomó más de 5 minutos - terminando proceso" -ForegroundColor Yellow
            $process.Kill()
        }
        
    } catch {
        Write-Host "   ❌ Error al ejecutar instalador: $($_.Exception.Message)" -ForegroundColor Red
    }

    # Verificar resultado de la instalación
    Write-Host ""
    Write-Host "🔍 VERIFICANDO RESULTADO:" -ForegroundColor Cyan
    
    $installPaths = @(
        "$env:ProgramFiles\GestionTime Desktop",
        "$env:ProgramFiles\GestionTime Solutions\GestionTime Desktop"
    )
    
    $installed = $false
    foreach ($installPath in $installPaths) {
        if (Test-Path "$installPath\GestionTime.Desktop.exe") {
            Write-Host "   ✅ Aplicación instalada en: $installPath" -ForegroundColor Green
            
            $installedFiles = Get-ChildItem $installPath -File -ErrorAction SilentlyContinue
            Write-Host "     • Archivos instalados: $($installedFiles.Count)" -ForegroundColor White
            
            $installed = $true
            break
        }
    }
    
    if (!$installed) {
        Write-Host "   ❌ Aplicación NO encontrada en ubicaciones esperadas" -ForegroundColor Red
    }

} catch {
    Write-Host "❌ ERROR CRÍTICO DURANTE DIAGNÓSTICO:" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor White
}

Write-Host ""
Write-Host "💡 RECOMENDACIONES:" -ForegroundColor Blue
Write-Host "===============================================" -ForegroundColor Blue

if (!$isAdmin) {
    Write-Host "🔴 PROBLEMA PRINCIPAL: Falta de permisos de administrador" -ForegroundColor Red
    Write-Host ""
    Write-Host "✅ SOLUCIÓN:" -ForegroundColor Green
    Write-Host "   1. Cerrar PowerShell actual" -ForegroundColor White
    Write-Host "   2. Abrir PowerShell como Administrador:" -ForegroundColor White
    Write-Host "      - Click derecho en PowerShell → 'Ejecutar como administrador'" -ForegroundColor Gray
    Write-Host "   3. Navegar al directorio del proyecto" -ForegroundColor White
    Write-Host "   4. Ejecutar: .\$InstallerPath" -ForegroundColor White
} else {
    Write-Host "🟡 OTRAS POSIBLES SOLUCIONES:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "   1. Recrear instalador:" -ForegroundColor White
    Write-Host "      .\create-selfextracting-installer.ps1 -Rebuild" -ForegroundColor Gray
    Write-Host ""
    Write-Host "   2. Usar MSI en su lugar:" -ForegroundColor White
    Write-Host "      .\create-msi-debug-complete.ps1 -Rebuild -OpenOutput" -ForegroundColor Gray
    Write-Host ""
    Write-Host "   3. Desactivar antivirus temporalmente" -ForegroundColor White
    Write-Host "   4. Verificar espacio en disco disponible" -ForegroundColor White
}

Write-Host ""
Write-Host "🔍 DIAGNÓSTICO COMPLETADO" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green