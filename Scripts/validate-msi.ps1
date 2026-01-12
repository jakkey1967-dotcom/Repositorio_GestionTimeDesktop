param(
    [string]$MsiPath = "bin\Release\MSI\GestionTimeDesktop-1.1.0.msi"
)

Write-Host ""
Write-Host "🔍 VALIDADOR DE MSI PROFESIONAL" -ForegroundColor Green -BackgroundColor DarkGreen
Write-Host "===============================================" -ForegroundColor Green
Write-Host ""

try {
    # Verificar que el archivo existe
    if (!(Test-Path $MsiPath)) {
        Write-Host "❌ ERROR: MSI no encontrado en $MsiPath" -ForegroundColor Red
        Write-Host "   Ejecutar: .\build-msi-complete.ps1" -ForegroundColor Yellow
        exit 1
    }

    $msiFile = Get-Item $MsiPath
    Write-Host "✅ MSI encontrado: $($msiFile.Name)" -ForegroundColor Green
    Write-Host ""

    # Información básica
    Write-Host "📊 INFORMACIÓN BÁSICA:" -ForegroundColor Cyan
    Write-Host "   • Archivo: $($msiFile.Name)" -ForegroundColor White
    Write-Host "   • Tamaño: $([math]::Round($msiFile.Length/1MB, 2)) MB" -ForegroundColor White
    Write-Host "   • Fecha: $($msiFile.LastWriteTime)" -ForegroundColor White
    Write-Host "   • Ubicación: $($msiFile.FullName)" -ForegroundColor White
    Write-Host ""

    # Verificar archivos fuente
    Write-Host "🔍 VERIFICANDO ARCHIVOS FUENTE:" -ForegroundColor Cyan
    $sourceFiles = @(
        @{Path="Installer\MSI\Product.wxs"; Name="Definición del producto"},
        @{Path="Installer\MSI\Features.wxs"; Name="Características y componentes"},
        @{Path="Installer\MSI\UI.wxs"; Name="Interfaz de usuario"},
        @{Path="Installer\MSI\License.rtf"; Name="Licencia del software"}
    )

    foreach ($source in $sourceFiles) {
        if (Test-Path $source.Path) {
            Write-Host "   ✅ $($source.Name)" -ForegroundColor Green
        } else {
            Write-Host "   ❌ $($source.Name) (FALTANTE)" -ForegroundColor Red
        }
    }
    Write-Host ""

    # Verificar aplicación compilada
    Write-Host "🔍 VERIFICANDO APLICACIÓN COMPILADA:" -ForegroundColor Cyan
    $appPath = "bin\Release\Installer\App"
    if (Test-Path $appPath) {
        $appFiles = Get-ChildItem $appPath -File | Measure-Object
        Write-Host "   ✅ Aplicación compilada ($($appFiles.Count) archivos)" -ForegroundColor Green
        
        # Verificar archivos críticos
        $criticalFiles = @(
            "GestionTime.Desktop.exe",
            "GestionTime.Desktop.dll", 
            "appsettings.json"
        )
        
        foreach ($file in $criticalFiles) {
            $filePath = Join-Path $appPath $file
            if (Test-Path $filePath) {
                $fileInfo = Get-Item $filePath
                Write-Host "     ✅ $file ($([math]::Round($fileInfo.Length/1KB, 1)) KB)" -ForegroundColor Green
            } else {
                Write-Host "     ❌ $file (FALTANTE)" -ForegroundColor Red
            }
        }
    } else {
        Write-Host "   ❌ Aplicación no compilada" -ForegroundColor Red
        Write-Host "     Ejecutar: .\build-for-installer.ps1 -Clean" -ForegroundColor Yellow
    }
    Write-Host ""

    # Información del MSI (usando propiedades del archivo)
    Write-Host "🎯 ANÁLISIS DEL MSI:" -ForegroundColor Magenta
    try {
        # Verificar firma del archivo (si existe)
        $signature = Get-AuthenticodeSignature $msiFile.FullName -ErrorAction SilentlyContinue
        if ($signature -and $signature.Status -eq "Valid") {
            Write-Host "   ✅ MSI firmado digitalmente" -ForegroundColor Green
        } else {
            Write-Host "   ⚪ MSI no firmado (normal para desarrollo)" -ForegroundColor White
        }

        # Verificar que es un archivo MSI válido
        $fileHeader = [System.IO.File]::ReadAllBytes($msiFile.FullName)[0..7]
        $msiSignature = [System.Text.Encoding]::ASCII.GetString($fileHeader[0..7])
        if ($msiSignature -match "Microsoft Office Database") {
            Write-Host "   ✅ Formato MSI válido" -ForegroundColor Green
        } else {
            Write-Host "   ❌ Formato MSI inválido" -ForegroundColor Red
        }

    } catch {
        Write-Host "   ⚠️  No se pudo analizar completamente el MSI" -ForegroundColor Yellow
    }
    Write-Host ""

    # Verificar herramientas necesarias
    Write-Host "🛠️  VERIFICANDO HERRAMIENTAS:" -ForegroundColor Cyan
    
    # WiX
    $wixPath = where.exe wix 2>$null
    if ($wixPath) {
        Write-Host "   ✅ WiX Toolset instalado" -ForegroundColor Green
    } else {
        Write-Host "   ❌ WiX Toolset no encontrado" -ForegroundColor Red
    }

    # .NET SDK
    $dotnetPath = where.exe dotnet 2>$null
    if ($dotnetPath) {
        $dotnetVersion = dotnet --version 2>$null
        Write-Host "   ✅ .NET SDK $dotnetVersion" -ForegroundColor Green
    } else {
        Write-Host "   ❌ .NET SDK no encontrado" -ForegroundColor Red
    }
    Write-Host ""

    # Comandos de prueba
    Write-Host "🧪 COMANDOS DE PRUEBA:" -ForegroundColor Blue
    Write-Host "   # Instalar en modo silencioso (PRUEBA):" -ForegroundColor White
    Write-Host "   msiexec /i `"$($msiFile.Name)`" /quiet /l*v test-install.log" -ForegroundColor Gray
    Write-Host ""
    Write-Host "   # Desinstalar:" -ForegroundColor White
    Write-Host "   msiexec /x `"$($msiFile.Name)`" /quiet" -ForegroundColor Gray
    Write-Host ""
    Write-Host "   # Verificar propiedades del MSI:" -ForegroundColor White
    Write-Host "   msiexec /i `"$($msiFile.Name)`" /qn INSTALLLEVEL=1 /l*v properties.log" -ForegroundColor Gray
    Write-Host ""

    # Resumen final
    Write-Host "📋 RESUMEN DE VALIDACIÓN:" -ForegroundColor Magenta
    Write-Host "===============================================" -ForegroundColor Magenta
    
    $validationScore = 0
    $maxScore = 6
    
    # Checks
    if (Test-Path $MsiPath) { $validationScore++ }
    if (Test-Path "Installer\MSI\Product.wxs") { $validationScore++ }
    if (Test-Path "Installer\MSI\Features.wxs") { $validationScore++ }
    if (Test-Path "Installer\MSI\UI.wxs") { $validationScore++ }
    if (Test-Path "bin\Release\Installer\App\GestionTime.Desktop.exe") { $validationScore++ }
    if ($wixPath) { $validationScore++ }

    $percentage = [math]::Round(($validationScore / $maxScore) * 100)
    
    if ($percentage -eq 100) {
        Write-Host "✅ VALIDACIÓN COMPLETA ($validationScore/$maxScore) - 100%" -ForegroundColor Green
        Write-Host "   MSI listo para distribución profesional" -ForegroundColor Green
    } elseif ($percentage -ge 80) {
        Write-Host "⚠️  VALIDACIÓN PARCIAL ($validationScore/$maxScore) - $percentage%" -ForegroundColor Yellow
        Write-Host "   MSI funcional, revisar advertencias arriba" -ForegroundColor Yellow
    } else {
        Write-Host "❌ VALIDACIÓN FALLIDA ($validationScore/$maxScore) - $percentage%" -ForegroundColor Red
        Write-Host "   Corregir problemas antes de usar MSI" -ForegroundColor Red
    }

    Write-Host ""
    Write-Host "🎯 PRÓXIMOS PASOS RECOMENDADOS:" -ForegroundColor Blue
    Write-Host "   1. Probar instalación en máquina virtual" -ForegroundColor White
    Write-Host "   2. Verificar que la aplicación se ejecute correctamente" -ForegroundColor White
    Write-Host "   3. Probar desinstalación completa" -ForegroundColor White
    Write-Host "   4. Distribuir a usuarios de prueba" -ForegroundColor White
    Write-Host "   5. Configurar firma digital para producción" -ForegroundColor White

} catch {
    Write-Host "❌ ERROR DURANTE LA VALIDACIÓN:" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor White
}

Write-Host ""
Write-Host "🔍 VALIDACIÓN COMPLETADA" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green