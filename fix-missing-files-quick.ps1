param(
    [string]$InstallPath = "C:\Program Files\GestionTime Solutions\GestionTime Desktop",
    [switch]$Force
)

Write-Host ""
Write-Host "🔧 SOLUCIÓN RÁPIDA: COPIAR ARCHIVOS FALTANTES" -ForegroundColor Green -BackgroundColor DarkGreen
Write-Host "===============================================" -ForegroundColor Green
Write-Host ""

$ErrorActionPreference = "Stop"

try {
    # Verificar si tenemos la aplicación completa
    $sourceAppPath = "bin\Release\Installer\App"
    if (!(Test-Path $sourceAppPath)) {
        Write-Host "❌ ERROR: Aplicación completa no encontrada en $sourceAppPath" -ForegroundColor Red
        Write-Host "   Ejecutar primero: .\build-for-installer.ps1 -Clean" -ForegroundColor Yellow
        return
    }

    # Verificar instalación actual
    if (!(Test-Path $InstallPath)) {
        Write-Host "❌ ERROR: Instalación actual no encontrada en $InstallPath" -ForegroundColor Red
        return
    }

    Write-Host "📊 ANALIZANDO ARCHIVOS FALTANTES:" -ForegroundColor Cyan

    # Obtener archivos actuales y completos
    $currentFiles = Get-ChildItem $InstallPath -File
    $completeFiles = Get-ChildItem $sourceAppPath -File
    
    Write-Host "   • Archivos actuales: $($currentFiles.Count)" -ForegroundColor White
    Write-Host "   • Archivos completos: $($completeFiles.Count)" -ForegroundColor White

    # Identificar archivos faltantes
    $currentNames = $currentFiles | ForEach-Object { $_.Name }
    $missingFiles = $completeFiles | Where-Object { $_.Name -notin $currentNames }
    
    Write-Host "   • Archivos faltantes: $($missingFiles.Count)" -ForegroundColor Yellow

    if ($missingFiles.Count -eq 0) {
        Write-Host "✅ No hay archivos faltantes" -ForegroundColor Green
        return
    }

    # Seleccionar archivos críticos para copiar
    $criticalFiles = $missingFiles | Where-Object {
        $_.Name -like "hostfxr.dll" -or
        $_.Name -like "hostpolicy.dll" -or
        $_.Name -like "System.*.dll" -or
        $_.Name -like "Microsoft.Win32.*.dll" -or
        $_.Name -like "*deps.json" -or
        $_.Name -like "*runtimeconfig.json" -or
        $_.Name -like "GestionTime.Desktop.pdb" -or
        ($_.Extension -eq ".dll" -and $_.Length -gt 500KB)
    }

    Write-Host ""
    Write-Host "🔧 COPIANDO ARCHIVOS CRÍTICOS:" -ForegroundColor Yellow
    Write-Host "   • Archivos críticos identificados: $($criticalFiles.Count)" -ForegroundColor White

    if (!$Force) {
        Write-Host ""
        Write-Host "⚠️  ADVERTENCIA: Se van a copiar archivos al directorio de Program Files" -ForegroundColor Yellow
        Write-Host "   Esto requiere permisos de administrador" -ForegroundColor Yellow
        $response = Read-Host "¿Continuar? (S/N)"
        if ($response -ne "S" -and $response -ne "s") {
            Write-Host "Operación cancelada" -ForegroundColor Yellow
            return
        }
    }

    $copied = 0
    $errors = 0

    foreach ($file in $criticalFiles) {
        try {
            $sourcePath = $file.FullName
            $destPath = Join-Path $InstallPath $file.Name
            
            Copy-Item $sourcePath $destPath -Force
            Write-Host "   ✅ $($file.Name) ($([math]::Round($file.Length/1KB, 1)) KB)" -ForegroundColor Green
            $copied++
            
        } catch {
            Write-Host "   ❌ Error copiando $($file.Name): $($_.Exception.Message)" -ForegroundColor Red
            $errors++
        }
    }

    Write-Host ""
    Write-Host "📊 RESULTADO DE LA COPIA:" -ForegroundColor Magenta
    Write-Host "   • Archivos copiados exitosamente: $copied" -ForegroundColor Green
    Write-Host "   • Errores: $errors" -ForegroundColor Red

    if ($copied -gt 0) {
        Write-Host ""
        Write-Host "✅ ARCHIVOS CRÍTICOS COPIADOS" -ForegroundColor Green
        Write-Host "   La aplicación debería funcionar mejor ahora" -ForegroundColor Green
        
        Write-Host ""
        Write-Host "🚀 PROBANDO LA APLICACIÓN:" -ForegroundColor Blue
        
        try {
            $exePath = Join-Path $InstallPath "GestionTime.Desktop.exe"
            Write-Host "   • Iniciando: $exePath" -ForegroundColor White
            
            Start-Process $exePath -WorkingDirectory $InstallPath
            Write-Host "   ✅ Aplicación iniciada" -ForegroundColor Green
            
        } catch {
            Write-Host "   ❌ Error al iniciar: $($_.Exception.Message)" -ForegroundColor Red
        }
        
    } else {
        Write-Host ""
        Write-Host "❌ NO SE PUDIERON COPIAR ARCHIVOS" -ForegroundColor Red
        Write-Host "   Usar instalador completo como alternativa:" -ForegroundColor Yellow
        Write-Host "   .\create-selfextracting-installer.ps1 -Rebuild" -ForegroundColor Gray
    }

} catch {
    Write-Host "❌ ERROR CRÍTICO:" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor White
    Write-Host ""
    Write-Host "💡 SOLUCIÓN ALTERNATIVA:" -ForegroundColor Blue
    Write-Host "   .\create-selfextracting-installer.ps1 -Rebuild" -ForegroundColor Gray
}

Write-Host ""
Write-Host "🔧 SOLUCIÓN RÁPIDA COMPLETADA" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green