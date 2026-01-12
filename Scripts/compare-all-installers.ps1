Write-Host ""
Write-Host "📊 COMPARACIÓN COMPLETA DE INSTALADORES GESTIONTIME DESKTOP" -ForegroundColor Green -BackgroundColor DarkGreen
Write-Host "===============================================" -ForegroundColor Green
Write-Host ""

# Función para formatear tamaño
function Format-Size($bytes) {
    if ($bytes -gt 1MB) {
        return "$([math]::Round($bytes/1MB, 2)) MB"
    } elseif ($bytes -gt 1KB) {
        return "$([math]::Round($bytes/1KB, 1)) KB"
    } else {
        return "$bytes bytes"
    }
}

# Verificar todos los instaladores disponibles
$installers = @(
    @{
        Name = "MSI Debug Completo"
        Path = "bin\Debug\MSI\GestionTimeDesktop-Debug-Complete-1.1.0.msi"
        Description = "MSI con 59 archivos críticos + debugging"
        Type = "MSI"
        Config = "Debug"
        Recommended = "Desarrollo/Testing"
    },
    @{
        Name = "MSI Release Mejorado"
        Path = "bin\Release\MSI\GestionTimeDesktop-Improved-1.1.0.msi"
        Description = "MSI con 10 archivos críticos optimizado"
        Type = "MSI"
        Config = "Release"
        Recommended = "Corporativo/Empresarial"
    },
    @{
        Name = "MSI Release Básico"
        Path = "bin\Release\MSI\GestionTimeDesktop-1.1.0.msi"
        Description = "MSI básico con archivos mínimos"
        Type = "MSI"
        Config = "Release"
        Recommended = "❌ NO USAR"
    },
    @{
        Name = "Auto-extraíble Completo"
        Path = "bin\Release\SelfExtractingInstaller\GestionTimeDesktopInstaller.bat"
        Description = "Instalador con TODOS los 520+ archivos"
        Type = "Auto-extraíble"
        Config = "Release"
        Recommended = "Usuarios Finales"
    }
)

Write-Host "🔍 ANALIZANDO INSTALADORES DISPONIBLES..." -ForegroundColor Cyan
Write-Host ""

$results = @()
foreach ($installer in $installers) {
    $result = [PSCustomObject]@{
        Nombre = $installer.Name
        Existe = Test-Path $installer.Path
        Tamaño = if (Test-Path $installer.Path) { Format-Size (Get-Item $installer.Path).Length } else { "N/A" }
        Descripción = $installer.Description
        Tipo = $installer.Type
        Configuración = $installer.Config
        Recomendado = $installer.Recommended
        Ruta = $installer.Path
    }
    $results += $result
}

# Mostrar tabla comparativa
Write-Host "📋 TABLA COMPARATIVA DE INSTALADORES:" -ForegroundColor Magenta
Write-Host "===============================================" -ForegroundColor Magenta

$index = 1
foreach ($result in $results) {
    $status = if ($result.Existe) { "✅" } else { "❌" }
    $color = if ($result.Existe) { "Green" } else { "Red" }
    
    Write-Host ""
    Write-Host "[$index] $($result.Nombre) $status" -ForegroundColor $color
    Write-Host "    📏 Tamaño: $($result.Tamaño)" -ForegroundColor White
    Write-Host "    📝 Descripción: $($result.Descripción)" -ForegroundColor White
    Write-Host "    🎯 Tipo: $($result.Tipo)" -ForegroundColor White
    Write-Host "    ⚙️  Configuración: $($result.Configuración)" -ForegroundColor White
    Write-Host "    💡 Recomendado para: $($result.Recomendado)" -ForegroundColor White
    Write-Host "    📂 Ubicación: $($result.Ruta)" -ForegroundColor Gray
    
    $index++
}

Write-Host ""
Write-Host "🎯 RECOMENDACIONES POR ESCENARIO:" -ForegroundColor Blue
Write-Host "===============================================" -ForegroundColor Blue

Write-Host ""
Write-Host "🧪 PARA DESARROLLO Y TESTING:" -ForegroundColor Yellow
$debugMSI = $results | Where-Object { $_.Nombre -eq "MSI Debug Completo" }
if ($debugMSI.Existe) {
    Write-Host "   ✅ USAR: MSI Debug Completo ($($debugMSI.Tamaño))" -ForegroundColor Green
    Write-Host "      • 59 archivos críticos incluidos" -ForegroundColor White
    Write-Host "      • Símbolos de debugging" -ForegroundColor White
    Write-Host "      • Instalación independiente" -ForegroundColor White
    Write-Host "      • Comando: msiexec /i `"$($debugMSI.Ruta.Split('\')[-1])`"" -ForegroundColor Gray
} else {
    Write-Host "   ⚠️  MSI Debug no disponible - Ejecutar: .\create-msi-debug-complete.ps1" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "🏢 PARA ENTORNOS CORPORATIVOS:" -ForegroundColor Cyan
$releaseMSI = $results | Where-Object { $_.Nombre -eq "MSI Release Mejorado" }
if ($releaseMSI.Existe) {
    Write-Host "   ✅ USAR: MSI Release Mejorado ($($releaseMSI.Tamaño))" -ForegroundColor Green
    Write-Host "      • Compatible con Group Policy" -ForegroundColor White
    Write-Host "      • Instalación silenciosa nativa" -ForegroundColor White
    Write-Host "      • Actualizaciones automáticas" -ForegroundColor White
    Write-Host "      • Comando: msiexec /i `"$($releaseMSI.Ruta.Split('\')[-1])`"" -ForegroundColor Gray
} else {
    Write-Host "   ⚠️  MSI Release Mejorado no disponible - Ejecutar: .\create-improved-msi.ps1" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "👥 PARA USUARIOS FINALES:" -ForegroundColor Green
$autoInstaller = $results | Where-Object { $_.Nombre -eq "Auto-extraíble Completo" }
if ($autoInstaller.Existe) {
    Write-Host "   ✅ USAR: Auto-extraíble Completo ($($autoInstaller.Tamaño))" -ForegroundColor Green
    Write-Host "      • TODOS los archivos incluidos (520+)" -ForegroundColor White
    Write-Host "      • Funcionamiento 100% garantizado" -ForegroundColor White
    Write-Host "      • Sin dependencias externas" -ForegroundColor White
    Write-Host "      • Comando: .\$($autoInstaller.Ruta.Replace('\', '\'))" -ForegroundColor Gray
} else {
    Write-Host "   ⚠️  Auto-extraíble no disponible - Ejecutar: .\create-selfextracting-installer.ps1" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "❌ NO RECOMENDADO:" -ForegroundColor Red
$basicMSI = $results | Where-Object { $_.Nombre -eq "MSI Release Básico" }
if ($basicMSI.Existe) {
    Write-Host "   ❌ MSI Release Básico ($($basicMSI.Tamaño))" -ForegroundColor Red
    Write-Host "      • Solo 3 archivos - FALTARÁN DEPENDENCIAS" -ForegroundColor Red
    Write-Host "      • Aplicación no funcionará correctamente" -ForegroundColor Red
}

Write-Host ""
Write-Host "🚀 COMANDOS RÁPIDOS PARA CREAR INSTALADORES:" -ForegroundColor Blue
Write-Host "===============================================" -ForegroundColor Blue
Write-Host ""

Write-Host "# Crear MSI Debug completo (RECOMENDADO para testing):" -ForegroundColor Green
Write-Host ".\create-msi-debug-complete.ps1 -OpenOutput" -ForegroundColor Gray
Write-Host ""

Write-Host "# Crear MSI Release mejorado:" -ForegroundColor Cyan
Write-Host ".\create-improved-msi.ps1 -OpenOutput" -ForegroundColor Gray
Write-Host ""

Write-Host "# Crear instalador auto-extraíble completo:" -ForegroundColor Yellow
Write-Host ".\create-selfextracting-installer.ps1 -Rebuild -OpenOutput" -ForegroundColor Gray
Write-Host ""

Write-Host "# Validar cualquier MSI:" -ForegroundColor Blue
Write-Host ".\validate-msi.ps1 -MsiPath `"ruta\al\archivo.msi`"" -ForegroundColor Gray

Write-Host ""
Write-Host "📊 ANÁLISIS COMPLETADO" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green

# Mostrar estadísticas finales
$existingInstallers = $results | Where-Object { $_.Existe }
$totalSize = 0
foreach ($installer in $existingInstallers) {
    if ($installer.Tamaño -ne "N/A") {
        $path = $installer.Ruta
        if (Test-Path $path) {
            $totalSize += (Get-Item $path).Length
        }
    }
}

Write-Host ""
Write-Host "📈 ESTADÍSTICAS FINALES:" -ForegroundColor Magenta
Write-Host "   • Instaladores disponibles: $($existingInstallers.Count)/4" -ForegroundColor White
Write-Host "   • Tamaño total ocupado: $(Format-Size $totalSize)" -ForegroundColor White
Write-Host "   • Opciones recomendadas: 3 (según escenario)" -ForegroundColor White