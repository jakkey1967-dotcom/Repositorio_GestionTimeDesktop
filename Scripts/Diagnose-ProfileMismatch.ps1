# ═══════════════════════════════════════════════════════════════
# DIAGNÓSTICO: API devuelve perfil incorrecto (/profiles/me)
# ═══════════════════════════════════════════════════════════════
# PROBLEMA:
#   • Login: wsanchez@global-retail.com → Wilson Sánchez
#   • API GET /profiles/me devuelve: Francisco Santos (INCORRECTO)
# ═══════════════════════════════════════════════════════════════

param(
    [string]$BaseUrl = "https://localhost:2502",
    [string]$Email = "wsanchez@global-retail.com",
    [string]$Password = "12345678"
)

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🔍 DIAGNÓSTICO: Perfil devuelto por /profiles/me" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Ignorar errores de certificado SSL para desarrollo
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }

# ═══════════════════════════════════════════════════════════════
# 🔐 PASO 1: Login y obtener token
# ═══════════════════════════════════════════════════════════════

Write-Host "📧 PASO 1: Login con $Email..." -ForegroundColor Yellow

$loginBody = @{
    email = $Email
    password = $Password
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod `
        -Uri "$BaseUrl/api/v1/auth/login-desktop" `
        -Method Post `
        -Body $loginBody `
        -ContentType "application/json" `
        -ErrorAction Stop

    $token = $loginResponse.token
    
    Write-Host "✅ Login exitoso" -ForegroundColor Green
    Write-Host "   • Token length: $($token.Length)" -ForegroundColor Gray
    
    # Extraer información del usuario del login response
    Write-Host ""
    Write-Host "📋 Información del LOGIN:" -ForegroundColor Cyan
    Write-Host "   • Email: $($loginResponse.user.email)" -ForegroundColor White
    Write-Host "   • Name: $($loginResponse.user.name)" -ForegroundColor White
    Write-Host "   • FullName: $($loginResponse.user.fullName)" -ForegroundColor White
    Write-Host "   • Role: $($loginResponse.user.role)" -ForegroundColor White
    
} catch {
    Write-Host "❌ Error en login: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# ═══════════════════════════════════════════════════════════════
# 🔍 PASO 2: Decodificar el token JWT (sin verificar firma)
# ═══════════════════════════════════════════════════════════════

Write-Host ""
Write-Host "🔑 PASO 2: Decodificando token JWT..." -ForegroundColor Yellow

try {
    # Extraer el payload (segunda parte del JWT)
    $parts = $token.Split('.')
    if ($parts.Count -ge 2) {
        $payload = $parts[1]
        
        # Añadir padding si es necesario
        $padding = 4 - ($payload.Length % 4)
        if ($padding -ne 4) {
            $payload += "=" * $padding
        }
        
        # Decodificar Base64URL → JSON
        $payloadJson = [System.Text.Encoding]::UTF8.GetString(
            [System.Convert]::FromBase64String($payload)
        )
        
        $jwtPayload = $payloadJson | ConvertFrom-Json
        
        Write-Host "✅ Token decodificado:" -ForegroundColor Green
        Write-Host ""
        Write-Host "📋 PAYLOAD DEL TOKEN JWT:" -ForegroundColor Cyan
        Write-Host "   • sub (user_id): $($jwtPayload.sub)" -ForegroundColor White
        Write-Host "   • email: $($jwtPayload.email)" -ForegroundColor White
        Write-Host "   • name: $($jwtPayload.name)" -ForegroundColor White
        Write-Host "   • role: $($jwtPayload.role)" -ForegroundColor White
        Write-Host "   • exp (expira): $(Get-Date -UnixTimeSeconds $jwtPayload.exp -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
        
        $userId = $jwtPayload.sub
        
    } else {
        Write-Host "⚠️ No se pudo decodificar el token JWT" -ForegroundColor Yellow
    }
} catch {
    Write-Host "⚠️ Error decodificando token: $($_.Exception.Message)" -ForegroundColor Yellow
}

# ═══════════════════════════════════════════════════════════════
# 🔍 PASO 3: Llamar al endpoint /profiles/me
# ═══════════════════════════════════════════════════════════════

Write-Host ""
Write-Host "👤 PASO 3: Obteniendo perfil desde /profiles/me..." -ForegroundColor Yellow

$headers = @{
    "Authorization" = "Bearer $token"
}

try {
    $profileResponse = Invoke-RestMethod `
        -Uri "$BaseUrl/api/v1/profiles/me" `
        -Method Get `
        -Headers $headers `
        -ErrorAction Stop

    Write-Host "✅ Perfil obtenido:" -ForegroundColor Green
    Write-Host ""
    Write-Host "📋 PERFIL DEVUELTO POR /profiles/me:" -ForegroundColor Cyan
    Write-Host "   • ID: $($profileResponse.id)" -ForegroundColor White
    Write-Host "   • first_name: $($profileResponse.first_name)" -ForegroundColor White
    Write-Host "   • last_name: $($profileResponse.last_name)" -ForegroundColor White
    Write-Host "   • full_name: $($profileResponse.full_name)" -ForegroundColor White
    Write-Host "   • phone: $($profileResponse.phone)" -ForegroundColor White
    Write-Host "   • mobile: $($profileResponse.mobile)" -ForegroundColor White
    
    # Verificar si hay un campo user_id en el perfil
    if ($profileResponse.PSObject.Properties.Name -contains "user_id") {
        Write-Host "   • user_id: $($profileResponse.user_id)" -ForegroundColor White
    } else {
        Write-Host "   ⚠️ NO HAY CAMPO user_id en el perfil" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "❌ Error obteniendo perfil: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   StatusCode: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    exit 1
}

# ═══════════════════════════════════════════════════════════════
# 📊 PASO 4: Comparar datos
# ═══════════════════════════════════════════════════════════════

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "📊 COMPARACIÓN DE DATOS" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

$loginName = $loginResponse.user.name
$loginFullName = $loginResponse.user.fullName
$profileFullName = "$($profileResponse.first_name) $($profileResponse.last_name)"

Write-Host "┌────────────────────────────────────────────────────────┐" -ForegroundColor Gray
Write-Host "│ Campo                │ Login Response │ /profiles/me  │" -ForegroundColor Gray
Write-Host "├────────────────────────────────────────────────────────┤" -ForegroundColor Gray

# Comparar nombres
if ($loginName -eq $profileFullName) {
    Write-Host "│ Nombre               │ ✅ $loginName │ ✅ $profileFullName │" -ForegroundColor Green
} else {
    Write-Host "│ Nombre               │ $loginName │ ❌ $profileFullName │" -ForegroundColor Red
}

# Comparar emails (si existe en el perfil)
if ($profileResponse.PSObject.Properties.Name -contains "email") {
    if ($loginResponse.user.email -eq $profileResponse.email) {
        Write-Host "│ Email                │ ✅ Coincide                                │" -ForegroundColor Green
    } else {
        Write-Host "│ Email                │ ❌ NO coincide                             │" -ForegroundColor Red
    }
}

Write-Host "└────────────────────────────────────────────────────────┘" -ForegroundColor Gray

# ═══════════════════════════════════════════════════════════════
# 🎯 PASO 5: Diagnóstico final
# ═══════════════════════════════════════════════════════════════

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🎯 DIAGNÓSTICO FINAL" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

if ($loginName -ne $profileFullName) {
    Write-Host "❌ PROBLEMA CONFIRMADO: El perfil NO coincide con el usuario logueado" -ForegroundColor Red
    Write-Host ""
    Write-Host "POSIBLES CAUSAS:" -ForegroundColor Yellow
    Write-Host ""
    
    if (-not ($profileResponse.PSObject.Properties.Name -contains "user_id")) {
        Write-Host "1. ⚠️ La tabla user_profiles NO tiene columna user_id" -ForegroundColor Yellow
        Write-Host "   → El endpoint /profiles/me devuelve SIEMPRE el primer perfil" -ForegroundColor Gray
        Write-Host "   → SOLUCIÓN: Añadir columna user_id a user_profiles" -ForegroundColor Cyan
        Write-Host "   → Ejecutar: ..\GestionTimeApi\scripts\Add-UserIdToProfiles.sql" -ForegroundColor Cyan
    } else {
        Write-Host "2. ⚠️ La relación user_id en user_profiles es INCORRECTA" -ForegroundColor Yellow
        Write-Host "   → El user_id en el perfil NO coincide con el token JWT" -ForegroundColor Gray
        Write-Host "   → Token JWT user_id: $userId" -ForegroundColor Gray
        Write-Host "   → Profile user_id: $($profileResponse.user_id)" -ForegroundColor Gray
        Write-Host "   → SOLUCIÓN: Actualizar la FK en la base de datos" -ForegroundColor Cyan
    }
    
    Write-Host ""
    Write-Host "3. ⚠️ El endpoint /profiles/me NO filtra por user_id del token" -ForegroundColor Yellow
    Write-Host "   → Verificar el código del controlador ProfilesController" -ForegroundColor Gray
    Write-Host "   → Debe usar: var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)" -ForegroundColor Cyan
    Write-Host "   → Y filtrar: WHERE user_id = userId" -ForegroundColor Cyan
    
} else {
    Write-Host "✅ PERFIL CORRECTO: El perfil coincide con el usuario logueado" -ForegroundColor Green
    Write-Host ""
    Write-Host "El problema reportado NO se reproduce en este momento." -ForegroundColor Yellow
    Write-Host "Posible causa: El error era temporal o ya fue corregido." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "📝 PRÓXIMOS PASOS" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Ejecutar el script SQL de diagnóstico:" -ForegroundColor White
Write-Host "   ..\GestionTimeApi\scripts\Diagnose-ProfileMismatch.sql" -ForegroundColor Cyan
Write-Host ""
Write-Host "2. Verificar el código del endpoint /profiles/me:" -ForegroundColor White
Write-Host "   ProfilesController.cs" -ForegroundColor Cyan
Write-Host ""
Write-Host "3. Si falta user_id, ejecutar migración:" -ForegroundColor White
Write-Host "   ..\GestionTimeApi\scripts\Add-UserIdToProfiles.sql" -ForegroundColor Cyan
Write-Host ""
