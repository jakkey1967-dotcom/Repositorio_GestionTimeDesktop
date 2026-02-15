# Script para diagnosticar el endpoint de informes
# Fecha: 2026-02-09

$ErrorActionPreference = "Stop"

# Configuración
$BaseUrl = "https://gestiontime-api.onrender.com"
$Date = "2026-02-09"

Write-Host "🔍 Diagnóstico del endpoint /api/v2/informes/resumen" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""

# Solicitar token
Write-Host "📝 Introduce tu token JWT:" -ForegroundColor Yellow
$Token = Read-Host -AsSecureString
$TokenPlain = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($Token)
)

# Headers
$Headers = @{
    "Authorization" = "Bearer $TokenPlain"
    "Accept" = "application/json"
}

try {
    Write-Host ""
    Write-Host "📊 Consultando resumen para el día $Date..." -ForegroundColor Cyan
    Write-Host ""
    
    $Endpoint = "$BaseUrl/api/v2/informes/resumen?scope=day&date=$Date"
    Write-Host "Endpoint: $Endpoint" -ForegroundColor Gray
    Write-Host ""
    
    $Response = Invoke-RestMethod -Uri $Endpoint -Method Get -Headers $Headers
    
    Write-Host "✅ RESPUESTA RECIBIDA:" -ForegroundColor Green
    Write-Host "=================================================" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "📋 RESUMEN GENERAL:" -ForegroundColor Yellow
    Write-Host "  • Partes: $($Response.partsCount)" -ForegroundColor White
    Write-Host "  • Tiempo Registrado: $($Response.recordedMinutes) min ($([Math]::Floor($Response.recordedMinutes / 60))h $($Response.recordedMinutes % 60)m)" -ForegroundColor White
    Write-Host "  • Tiempo Real (sin solape): $($Response.coveredMinutes) min ($([Math]::Floor($Response.coveredMinutes / 60))h $($Response.coveredMinutes % 60)m)" -ForegroundColor White
    Write-Host "  • Solape: $($Response.overlapMinutes) min ($([Math]::Floor($Response.overlapMinutes / 60))h $($Response.overlapMinutes % 60)m)" -ForegroundColor White
    Write-Host "  • Inicio Global: $($Response.firstStart)" -ForegroundColor White
    Write-Host "  • Fin Global: $($Response.lastEnd)" -ForegroundColor White
    Write-Host ""
    
    if ($Response.mergedIntervals -and $Response.mergedIntervals.Count -gt 0) {
        Write-Host "⏱️ INTERVALOS CUBIERTOS (unidos, sin solape):" -ForegroundColor Yellow
        $TotalCovered = 0
        foreach ($interval in $Response.mergedIntervals) {
            Write-Host "  • $($interval.start) - $($interval.end) ($($interval.minutes) min)" -ForegroundColor Cyan
            $TotalCovered += $interval.minutes
        }
        Write-Host "  ─────────────────────────────────" -ForegroundColor Gray
        Write-Host "  Total cubierto: $TotalCovered min ($([Math]::Floor($TotalCovered / 60))h $($TotalCovered % 60)m)" -ForegroundColor Green
        Write-Host ""
    }
    
    if ($Response.gaps -and $Response.gaps.Count -gt 0) {
        Write-Host "❗ HUECOS DETECTADOS:" -ForegroundColor Yellow
        foreach ($gap in $Response.gaps) {
            Write-Host "  • $($gap.start) - $($gap.end) ($($gap.minutes) min)" -ForegroundColor Red
        }
        Write-Host ""
    }
    
    Write-Host "=================================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "✅ Diagnóstico completado" -ForegroundColor Green
    Write-Host ""
    
    # Verificar discrepancia
    if ($Response.partsCount -ne 5 -or $Response.coveredMinutes -ne 510) {
        Write-Host "⚠️ DISCREPANCIA DETECTADA:" -ForegroundColor Red
        Write-Host "  Esperado: 5 partes, 510 min (8h 30m) cubierto" -ForegroundColor Yellow
        Write-Host "  Recibido: $($Response.partsCount) partes, $($Response.coveredMinutes) min ($([Math]::Floor($Response.coveredMinutes / 60))h $($Response.coveredMinutes % 60)m) cubierto" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "🔍 Posibles causas:" -ForegroundColor Cyan
        Write-Host "  1. El backend está devolviendo partes duplicados" -ForegroundColor White
        Write-Host "  2. Hay partes de otros días incluidos en la consulta" -ForegroundColor White
        Write-Host "  3. El parámetro 'date' no se está filtrando correctamente" -ForegroundColor White
        Write-Host "  4. Problema con timezone en el backend" -ForegroundColor White
        Write-Host ""
    } else {
        Write-Host "✅ Los datos coinciden con lo esperado" -ForegroundColor Green
        Write-Host ""
    }
    
    # Mostrar respuesta JSON completa
    Write-Host "📄 RESPUESTA JSON COMPLETA:" -ForegroundColor Yellow
    Write-Host "=================================================" -ForegroundColor Gray
    $Response | ConvertTo-Json -Depth 10 | Write-Host -ForegroundColor Gray
    Write-Host "=================================================" -ForegroundColor Gray
    
} catch {
    Write-Host ""
    Write-Host "❌ ERROR:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $Reader = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
        $ResponseBody = $Reader.ReadToEnd()
        Write-Host ""
        Write-Host "Detalles:" -ForegroundColor Yellow
        Write-Host $ResponseBody -ForegroundColor Gray
    }
}
