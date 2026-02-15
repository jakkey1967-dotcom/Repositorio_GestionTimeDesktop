# Script automatizado para diagnosticar el endpoint de informes
# Fecha: 2026-02-09
# Versión: v1.9.5-alpha

param(
    [Parameter(Mandatory=$false)]
    [string]$Email = "psantos@global-retail.com",

    [Parameter(Mandatory=$false)]
    [string]$Password = "12345678",

    [Parameter(Mandatory=$false)]
    [string]$BaseUrl = "https://gestiontimeapi.onrender.com",

    [Parameter(Mandatory=$false)]
    [string]$Date = "2026-02-09"
)

$ErrorActionPreference = "Stop"

Write-Host "Diagnostico AUTOMATICO del endpoint /api/v2/informes/resumen" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Configuracion:" -ForegroundColor Yellow
Write-Host "  Email: $Email" -ForegroundColor White
Write-Host "  BaseUrl: $BaseUrl" -ForegroundColor White
Write-Host "  Fecha: $Date" -ForegroundColor White
Write-Host ""

try {
    # ═══════════════════════════════════════════════════════════════
    # PASO 1: LOGIN
    # ═══════════════════════════════════════════════════════════════
    Write-Host ""
    Write-Host "PASO 1: Autenticacion..." -ForegroundColor Cyan
    Write-Host "============================" -ForegroundColor Cyan
    Write-Host ""

    $loginBody = @{
        email = $Email
        password = $Password
    } | ConvertTo-Json
    
    $loginEndpoint = "$BaseUrl/api/v1/auth/login-desktop"
    Write-Host "Endpoint: $loginEndpoint" -ForegroundColor Gray
    
    $loginResponse = Invoke-RestMethod -Uri $loginEndpoint `
        -Method Post `
        -Body $loginBody `
        -ContentType "application/json"

    $token = $loginResponse.accessToken

    if (-not $token) {
        Write-Host "DEBUG: Respuesta de login:" -ForegroundColor Magenta
        $loginResponse | ConvertTo-Json -Depth 5 | Write-Host -ForegroundColor Gray
        throw "No se recibio token en la respuesta de login"
    }
    
    Write-Host "[OK] Login exitoso" -ForegroundColor Green
    Write-Host "  Token recibido: $($token.Substring(0, 30))..." -ForegroundColor Gray
    Write-Host "  Usuario: $($loginResponse.user.fullName)" -ForegroundColor Gray
    Write-Host "  Email: $($loginResponse.user.email)" -ForegroundColor Gray
    Write-Host "  Rol: $($loginResponse.user.role)" -ForegroundColor Gray
    Write-Host "  SessionId: $($loginResponse.sessionId)" -ForegroundColor Gray
    Write-Host ""

    # ═══════════════════════════════════════════════════════════════
    # PASO 2: CONSULTAR INFORMES
    # ═══════════════════════════════════════════════════════════════
    Write-Host "PASO 2: Consultar Informes..." -ForegroundColor Cyan
    Write-Host "================================" -ForegroundColor Cyan
    Write-Host ""
    
    $headers = @{
        "Authorization" = "Bearer $token"
        "Accept" = "application/json"
    }
    
    $informesEndpoint = "$BaseUrl/api/v2/informes/resumen?scope=day&date=$Date"
    Write-Host "Endpoint: $informesEndpoint" -ForegroundColor Gray
    Write-Host ""
    
    $response = Invoke-RestMethod -Uri $informesEndpoint -Method Get -Headers $headers
    
    # ═══════════════════════════════════════════════════════════════
    # PASO 3: ANALIZAR RESPUESTA
    # ═══════════════════════════════════════════════════════════════
    Write-Host "✅ RESPUESTA RECIBIDA:" -ForegroundColor Green
    Write-Host "════════════════════════════════════════════════════" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "RESUMEN GENERAL:" -ForegroundColor Yellow
    Write-Host "  Partes: $($response.partsCount)" -ForegroundColor White

    $recordedHours = [Math]::Floor($response.recordedMinutes / 60)
    $recordedMins = $response.recordedMinutes % 60
    Write-Host "  Tiempo Registrado: $($response.recordedMinutes) min ($recordedHours h $recordedMins m)" -ForegroundColor White

    $coveredHours = [Math]::Floor($response.coveredMinutes / 60)
    $coveredMins = $response.coveredMinutes % 60
    Write-Host "  Tiempo Real (sin solape): $($response.coveredMinutes) min ($coveredHours h $coveredMins m)" -ForegroundColor White

    $overlapHours = [Math]::Floor($response.overlapMinutes / 60)
    $overlapMins = $response.overlapMinutes % 60
    Write-Host "  Solape: $($response.overlapMinutes) min ($overlapHours h $overlapMins m)" -ForegroundColor White

    Write-Host "  Inicio Global: $($response.firstStart)" -ForegroundColor White
    Write-Host "  Fin Global: $($response.lastEnd)" -ForegroundColor White
    Write-Host ""
    
    # ═══════════════════════════════════════════════════════════════
    # PASO 4: MOSTRAR INTERVALOS
    # ═══════════════════════════════════════════════════════════════
    if ($response.mergedIntervals -and $response.mergedIntervals.Count -gt 0) {
        Write-Host "INTERVALOS CUBIERTOS (unidos, sin solape):" -ForegroundColor Yellow
        $totalCovered = 0
        foreach ($interval in $response.mergedIntervals) {
            Write-Host "  $($interval.start) - $($interval.end) ($($interval.minutes) min)" -ForegroundColor Cyan
            $totalCovered += $interval.minutes
        }
        Write-Host "  ________________________________" -ForegroundColor Gray
        $totalHours = [Math]::Floor($totalCovered / 60)
        $totalMins = $totalCovered % 60
        Write-Host "  Total cubierto: $totalCovered min ($totalHours h $totalMins m)" -ForegroundColor Green
        Write-Host ""
    }
    
    # ═══════════════════════════════════════════════════════════════
    # PASO 5: MOSTRAR HUECOS
    # ═══════════════════════════════════════════════════════════════
    if ($response.gaps -and $response.gaps.Count -gt 0) {
        Write-Host "HUECOS DETECTADOS:" -ForegroundColor Yellow
        foreach ($gap in $response.gaps) {
            Write-Host "  $($gap.start) - $($gap.end) ($($gap.minutes) min)" -ForegroundColor Red
        }
        Write-Host ""
    }
    
    Write-Host "════════════════════════════════════════════════════" -ForegroundColor Green
    Write-Host ""
    
    # ═══════════════════════════════════════════════════════════════
    # PASO 6: VERIFICAR DISCREPANCIA
    # ═══════════════════════════════════════════════════════════════
    Write-Host "ANALISIS DE DISCREPANCIA:" -ForegroundColor Cyan
    Write-Host "====================================================" -ForegroundColor Cyan
    Write-Host ""
    
    $expectedParts = 5
    $expectedCovered = 510  # 8h 30m
    
    Write-Host "Comparacion con datos esperados:" -ForegroundColor Yellow
    Write-Host ""

    # Comparar Partes
    if ($response.partsCount -eq $expectedParts) {
        Write-Host "  [OK] Partes: $($response.partsCount) (esperado: $expectedParts)" -ForegroundColor Green
    } else {
        $diff = $response.partsCount - $expectedParts
        $percent = [Math]::Round(($diff / $expectedParts) * 100, 1)
        Write-Host "  [ERROR] Partes: $($response.partsCount) (esperado: $expectedParts)" -ForegroundColor Red
        Write-Host "     Diferencia: +$diff partes (+$percent %)" -ForegroundColor Red
    }

    # Comparar Tiempo Cubierto
    if ($response.coveredMinutes -eq $expectedCovered) {
        Write-Host "  [OK] Tiempo Cubierto: $($response.coveredMinutes) min (esperado: $expectedCovered min)" -ForegroundColor Green
    } else {
        $diff = $response.coveredMinutes - $expectedCovered
        $diffHours = [Math]::Floor([Math]::Abs($diff) / 60)
        $diffMins = [Math]::Abs($diff) % 60
        Write-Host "  [ERROR] Tiempo Cubierto: $($response.coveredMinutes) min (esperado: $expectedCovered min)" -ForegroundColor Red
        Write-Host "     Diferencia: +$diff min (+$diffHours h $diffMins m)" -ForegroundColor Red
    }
    
    Write-Host ""
    
    # ═══════════════════════════════════════════════════════════════
    # CONCLUSIÓN
    # ═══════════════════════════════════════════════════════════════
    if ($response.partsCount -ne $expectedParts -or $response.coveredMinutes -ne $expectedCovered) {
        Write-Host "[ALERTA] DISCREPANCIA CONFIRMADA:" -ForegroundColor Red
        Write-Host "====================================================" -ForegroundColor Red
        Write-Host ""
        Write-Host "Posibles causas:" -ForegroundColor Cyan
        Write-Host "  1. Duplicacion de partes en el backend (JOIN incorrecto)" -ForegroundColor White
        Write-Host "  2. Filtro de fecha no se aplica correctamente" -ForegroundColor White
        Write-Host "  3. Partes de otros dias incluidos en la consulta" -ForegroundColor White
        Write-Host "  4. Problema con timezone en el backend" -ForegroundColor White
        Write-Host ""
        Write-Host "Accion requerida:" -ForegroundColor Yellow
        Write-Host "  - Revisar backend: GestionTimeApi/Controllers/InformesController.cs" -ForegroundColor White
        Write-Host "  - Verificar query SQL para duplicados" -ForegroundColor White
        Write-Host "  - Comparar con /api/v2/partes/intervalos-cubiertos (que funciona)" -ForegroundColor White
        Write-Host ""
    } else {
        Write-Host "[OK] LOS DATOS COINCIDEN CON LO ESPERADO" -ForegroundColor Green
        Write-Host "====================================================" -ForegroundColor Green
        Write-Host ""
    }
    
    # ═══════════════════════════════════════════════════════════════
    # PASO 7: MOSTRAR JSON COMPLETO
    # ═══════════════════════════════════════════════════════════════
    Write-Host "RESPUESTA JSON COMPLETA:" -ForegroundColor Yellow
    Write-Host "====================================================" -ForegroundColor Gray
    $response | ConvertTo-Json -Depth 10 | Write-Host -ForegroundColor Gray
    Write-Host "====================================================" -ForegroundColor Gray
    Write-Host ""

    Write-Host "[OK] Diagnostico completado exitosamente" -ForegroundColor Green
    Write-Host ""
    
} catch {
    Write-Host ""
    Write-Host "❌ ERROR:" -ForegroundColor Red
    Write-Host "════════════════════════════════════════════════════" -ForegroundColor Red
    Write-Host ""
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    
    if ($_.Exception.Response) {
        try {
            $reader = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
            $responseBody = $reader.ReadToEnd()
            Write-Host "Detalles de la respuesta:" -ForegroundColor Yellow
            Write-Host $responseBody -ForegroundColor Gray
        } catch {
            Write-Host "No se pudo leer el cuerpo de la respuesta" -ForegroundColor Gray
        }
    }
    
    Write-Host ""
    Write-Host "Stack trace:" -ForegroundColor Gray
    Write-Host $_.ScriptStackTrace -ForegroundColor DarkGray
    Write-Host ""
    
    exit 1
}
