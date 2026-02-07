# ═══════════════════════════════════════════════════════════════
# VALIDACIÓN SIMPLE: Nombre del banner VS Login
# ═══════════════════════════════════════════════════════════════

param(
    [string]$Email = "wsanchez@global-retail.com",
    [string]$Password = "tu_password"
)

# Ignorar errores SSL
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }

$baseUrl = "https://localhost:2502"

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🔍 VALIDACIÓN: ¿El banner muestra el nombre correcto?" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# ═══════════════════════════════════════════════════════════════
# 1. LOGIN
# ═══════════════════════════════════════════════════════════════

Write-Host "1️⃣ LOGIN con: $Email" -ForegroundColor Yellow

$loginBody = @{
    email = $Email
    password = $Password
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/auth/login-desktop" `
        -Method Post `
        -Body $loginBody `
        -ContentType "application/json"

    Write-Host "   ✅ Login exitoso" -ForegroundColor Green
    Write-Host ""
    Write-Host "   📋 Datos del LOGIN:" -ForegroundColor White
    Write-Host "      • Nombre: $($loginResponse.user.name)" -ForegroundColor Cyan
    Write-Host "      • Email:  $($loginResponse.user.email)" -ForegroundColor Gray
    
    $expectedName = $loginResponse.user.name
    $token = $loginResponse.token

} catch {
    Write-Host "   ❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# ═══════════════════════════════════════════════════════════════
# 2. LLAMAR A /profiles/me (lo que usa el banner)
# ═══════════════════════════════════════════════════════════════

Write-Host ""
Write-Host "2️⃣ OBTENIENDO PERFIL (lo que muestra el banner)..." -ForegroundColor Yellow

$headers = @{
    "Authorization" = "Bearer $token"
}

try {
    $profileResponse = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/profiles/me" `
        -Method Get `
        -Headers $headers

    Write-Host "   ✅ Perfil obtenido" -ForegroundColor Green
    Write-Host ""
    Write-Host "   📋 Datos del BANNER:" -ForegroundColor White
    Write-Host "      • Nombre: $($profileResponse.first_name) $($profileResponse.last_name)" -ForegroundColor Cyan
    Write-Host "      • Teléfono: $($profileResponse.phone)" -ForegroundColor Gray
    
    $bannerName = "$($profileResponse.first_name) $($profileResponse.last_name)"

} catch {
    Write-Host "   ❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# ═══════════════════════════════════════════════════════════════
# 3. COMPARAR
# ═══════════════════════════════════════════════════════════════

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "📊 RESULTADO DE LA VALIDACIÓN" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "Login dice:        " -NoNewline -ForegroundColor White
Write-Host "$expectedName" -ForegroundColor Green

Write-Host "Banner muestra:    " -NoNewline -ForegroundColor White

if ($expectedName -eq $bannerName) {
    Write-Host "$bannerName" -ForegroundColor Green
    Write-Host ""
    Write-Host "✅ ¡CORRECTO! El banner muestra el nombre esperado" -ForegroundColor Green
} else {
    Write-Host "$bannerName" -ForegroundColor Red
    Write-Host ""
    Write-Host "❌ ¡ERROR! El banner NO muestra el nombre correcto" -ForegroundColor Red
    Write-Host ""
    Write-Host "PROBLEMA:" -ForegroundColor Yellow
    Write-Host "   • Login devuelve:  $expectedName" -ForegroundColor Gray
    Write-Host "   • API devuelve:    $bannerName" -ForegroundColor Red
    Write-Host ""
    Write-Host "SOLUCIÓN:" -ForegroundColor Cyan
    Write-Host "   → La API /profiles/me está devolviendo el perfil INCORRECTO" -ForegroundColor White
    Write-Host "   → Verifica en el backend que el endpoint filtre por user_id del token" -ForegroundColor White
}

Write-Host ""
