# ========================================
# 🔍 Script de Validación de Advertencias
# GestionTime.Desktop
# ========================================

param(
    [switch]$Detailed,
    [switch]$CriticalOnly,
    [string]$OutputFile = ""
)

$ErrorActionPreference = "Continue"

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🔍 VALIDANDO ADVERTENCIAS DEL PROYECTO" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Función para compilar y analizar advertencias
function Test-ProjectWarnings {
    param([switch]$Detailed, [switch]$CriticalOnly)
    
    Write-Host "📊 Compilando proyecto..." -ForegroundColor Yellow
    
    $buildOutput = dotnet build --verbosity minimal 2>&1
    $buildExitCode = $LASTEXITCODE
    
    if ($buildExitCode -ne 0) {
        Write-Host "❌ ERRORES DE COMPILACIÓN DETECTADOS" -ForegroundColor Red
        Write-Host "El proyecto no compila correctamente. Corregir errores primero." -ForegroundColor Yellow
        Write-Host ""
        $buildOutput | Select-String "error CS|error NU|error MVVM|error NETSDK" | ForEach-Object {
            Write-Host "   🚨 $_" -ForegroundColor Red
        }
        return $null
    }
    
    # Extraer todas las advertencias
    $allWarnings = $buildOutput | Select-String "warning CS|warning NU|warning MVVM|warning NETSDK"
    $warningCount = $allWarnings.Count
    
    # Clasificar advertencias por tipo
    $warningsByType = @{
        "CS8618" = @() # Nullable fields not initialized
        "CS8625" = @() # NULL to non-nullable conversion
        "CS8622" = @() # Nullability mismatch in delegates
        "CS0169" = @() # Field never used
        "CS0414" = @() # Field assigned but never used
        "MVVMTK0045" = @() # AOT compatibility issues
        "NU1603" = @() # NuGet version conflicts
        "NETSDK1198" = @() # Missing publish profile
        "Other" = @()
    }
    
    foreach ($warning in $allWarnings) {
        $warningText = $warning.ToString()
        
        foreach ($type in $warningsByType.Keys) {
            if ($warningText -match $type) {
                $warningsByType[$type] += $warningText
                break
            }
        }
        
        # Si no coincidió con ningún tipo conocido, agregarlo a "Other"
        $found = $false
        foreach ($type in $warningsByType.Keys) {
            if ($type -ne "Other" -and $warningText -match $type) {
                $found = $true
                break
            }
        }
        if (-not $found) {
            $warningsByType["Other"] += $warningText
        }
    }
    
    # Calcular advertencias críticas
    $criticalTypes = @("CS8618", "CS8625", "CS8622", "MVVMTK0045")
    $criticalCount = 0
    foreach ($type in $criticalTypes) {
        $criticalCount += $warningsByType[$type].Count
    }
    
    return @{
        TotalWarnings = $warningCount
        CriticalWarnings = $criticalCount
        WarningsByType = $warningsByType
        BuildSuccess = $true
    }
}

# Ejecutar análisis
$result = Test-ProjectWarnings -Detailed:$Detailed -CriticalOnly:$CriticalOnly

if ($result -eq $null) {
    Write-Host "⚠️ No se pudo completar el análisis debido a errores de compilación" -ForegroundColor Yellow
    exit 1
}

# Mostrar resultados
Write-Host "📊 RESUMEN DE RESULTADOS:" -ForegroundColor Green
Write-Host "   • Total de advertencias: $($result.TotalWarnings)" -ForegroundColor $(if($result.TotalWarnings -lt 20) {"Green"} else {"Red"})
Write-Host "   • Advertencias críticas: $($result.CriticalWarnings)" -ForegroundColor $(if($result.CriticalWarnings -eq 0) {"Green"} else {"Red"})
Write-Host ""

# Estado general
if ($result.CriticalWarnings -eq 0 -and $result.TotalWarnings -lt 10) {
    Write-Host "✅ ¡EXCELENTE! Proyecto con muy pocas advertencias" -ForegroundColor Green
} elseif ($result.CriticalWarnings -eq 0) {
    Write-Host "🟡 BUENO: Sin advertencias críticas, solo menores" -ForegroundColor Yellow
} else {
    Write-Host "🔴 MEJORABLE: Hay advertencias críticas que corregir" -ForegroundColor Red
}

Write-Host ""

# Mostrar detalle por tipo
Write-Host "📋 DETALLE POR TIPO DE ADVERTENCIA:" -ForegroundColor Cyan

foreach ($type in $result.WarningsByType.Keys | Sort-Object) {
    $warnings = $result.WarningsByType[$type]
    if ($warnings.Count -eq 0) { continue }
    
    $isCritical = $type -in @("CS8618", "CS8625", "CS8622", "MVVMTK0045")
    $color = if ($isCritical) { "Red" } else { "Yellow" }
    $priority = if ($isCritical) { "🔴 ALTA" } else { "🟡 MEDIA" }
    
    $description = switch ($type) {
        "CS8618" { "Campos no inicializados (nullable)" }
        "CS8625" { "Conversión NULL a non-nullable" }
        "CS8622" { "Desajuste de nulabilidad en delegados" }
        "CS0169" { "Campo declarado pero no usado" }
        "CS0414" { "Campo asignado pero no usado" }
        "MVVMTK0045" { "Incompatibilidad AOT en ViewModels" }
        "NU1603" { "Conflicto de versiones NuGet" }
        "NETSDK1198" { "Perfil de publicación faltante" }
        default { "Otras advertencias" }
    }
    
    Write-Host ""
    Write-Host "   $type ($($warnings.Count) advertencias) - $priority" -ForegroundColor $color
    Write-Host "   └─ $description" -ForegroundColor Gray
    
    if ($Detailed -or ($CriticalOnly -and $isCritical)) {
        foreach ($warning in $warnings | Select-Object -First 3) {
            $shortWarning = $warning -replace ".*?warning $type[^:]*:", "..."
            if ($shortWarning.Length -gt 80) {
                $shortWarning = $shortWarning.Substring(0, 80) + "..."
            }
            Write-Host "      • $shortWarning" -ForegroundColor Gray
        }
        if ($warnings.Count -gt 3) {
            Write-Host "      ... y $($warnings.Count - 3) más" -ForegroundColor DarkGray
        }
    }
}

# Generar archivo de salida si se especifica
if ($OutputFile) {
    $reportContent = @"
# 🔍 Reporte de Advertencias - GestionTime.Desktop
Generado: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

## 📊 Resumen
- **Total de advertencias**: $($result.TotalWarnings)
- **Advertencias críticas**: $($result.CriticalWarnings)
- **Estado de compilación**: ✅ Exitosa

## 📋 Detalle por Tipo
"@

    foreach ($type in $result.WarningsByType.Keys | Sort-Object) {
        $warnings = $result.WarningsByType[$type]
        if ($warnings.Count -eq 0) { continue }
        
        $reportContent += "`n`n### $type ($($warnings.Count) advertencias)`n"
        foreach ($warning in $warnings) {
            $reportContent += "- ``$warning```n"
        }
    }
    
    $reportContent | Out-File -FilePath $OutputFile -Encoding UTF8
    Write-Host ""
    Write-Host "📄 Reporte guardado en: $OutputFile" -ForegroundColor Green
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan

# Sugerencias
Write-Host "💡 PRÓXIMOS PASOS RECOMENDADOS:" -ForegroundColor Yellow
if ($result.CriticalWarnings -gt 0) {
    Write-Host "   1. Corregir advertencias críticas (CS8618, CS8625, MVVMTK0045)" -ForegroundColor White
    Write-Host "   2. Implementar correcciones de nullable reference types" -ForegroundColor White
    Write-Host "   3. Migrar ViewModels a partial properties si es posible" -ForegroundColor White
}

if ($result.WarningsByType["CS0169"].Count -gt 0 -or $result.WarningsByType["CS0414"].Count -gt 0) {
    Write-Host "   4. Limpiar campos no utilizados para mejor mantenibilidad" -ForegroundColor White
}

Write-Host "   5. Configurar warnings como errores en desarrollo futuro" -ForegroundColor White
Write-Host ""

exit 0