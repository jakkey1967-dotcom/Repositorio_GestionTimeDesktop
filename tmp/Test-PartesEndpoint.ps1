# ===============================================
# Test rápido - Verificar token y endpoint
# ===============================================

Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🔍 DIAGNÓSTICO - Endpoint /api/v1/partes" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# 1. Verificar que la API responda
Write-Host "1️⃣ Probando API sin autenticación..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "http://localhost:2501/api/v1/partes?limit=5" -Method Get -ErrorAction Stop
    Write-Host "   ✅ API respondió SIN autenticación" -ForegroundColor Green
    Write-Host "   📊 Partes devueltos: $($response.Count)" -ForegroundColor White
} catch {
    if ($_.Exception.Response.StatusCode.value__ -eq 401) {
        Write-Host "   ⚠️  401 Unauthorized - El endpoint REQUIERE autenticación" -ForegroundColor Yellow
        Write-Host "   Esto es NORMAL - Necesitas hacer login primero en el Desktop" -ForegroundColor Gray
    } else {
        Write-Host "   ❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}
Write-Host ""

# 2. Probar con token (si existe)
Write-Host "2️⃣ Verificando si hay token guardado..." -ForegroundColor Yellow
$settings = [Windows.Storage.ApplicationData]::Current.LocalSettings
$token = $settings.Values["UserToken"]

if ($token) {
    Write-Host "   ✅ Token encontrado: $($token.Substring(0, 20))..." -ForegroundColor Green
    Write-Host ""
    Write-Host "3️⃣ Probando endpoint CON token..." -ForegroundColor Yellow
    
    try {
        $headers = @{
            "Authorization" = "Bearer $token"
        }
        $response = Invoke-RestMethod -Uri "http://localhost:2501/api/v1/partes?limit=5" -Method Get -Headers $headers -ErrorAction Stop
        Write-Host "   ✅ API respondió CON autenticación" -ForegroundColor Green
        Write-Host "   📊 Partes devueltos: $($response.Count)" -ForegroundColor White
        
        if ($response.Count -gt 0) {
            Write-Host ""
            Write-Host "   📄 Primer parte:" -ForegroundColor Cyan
            $primer = $response[0]
            Write-Host "      ID: $($primer.id)" -ForegroundColor Gray
            Write-Host "      Fecha: $($primer.fecha)" -ForegroundColor Gray
            Write-Host "      Cliente: $($primer.cliente)" -ForegroundColor Gray
        }
    } catch {
        Write-Host "   ❌ Error con token: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "   StatusCode: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Gray
    }
} else {
    Write-Host "   ❌ NO hay token guardado" -ForegroundColor Red
    Write-Host "   Necesitas hacer LOGIN en el Desktop primero" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "💡 CONCLUSIÓN:" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
if ($token) {
    Write-Host "✅ Tienes token guardado" -ForegroundColor Green
    Write-Host "🔄 Si el Desktop no muestra datos, el problema está en:" -ForegroundColor Yellow
    Write-Host "   1. El token expiró" -ForegroundColor Gray
    Write-Host "   2. El ApiClient no está enviando el token" -ForegroundColor Gray
    Write-Host "   3. Hay un error silencioso (revisa logs del Desktop)" -ForegroundColor Gray
} else {
    Write-Host "❌ NO tienes token guardado" -ForegroundColor Red
    Write-Host "🔑 SOLUCIÓN:" -ForegroundColor Yellow
    Write-Host "   1. Ejecuta el Desktop: dotnet run" -ForegroundColor White
    Write-Host "   2. Haz LOGIN con usuario/contraseña" -ForegroundColor White
    Write-Host "   3. DESPUÉS navega a DiarioPage" -ForegroundColor White
}
Write-Host ""
