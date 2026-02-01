# ═══════════════════════════════════════════════════════════════════════════════
# Script: Test-PresenceEndpoint.ps1
# Descripción: Prueba y compara endpoints de presencia de usuarios
# ═══════════════════════════════════════════════════════════════════════════════

param(
    [Parameter(Mandatory=$false)]
    [string]$Email = "psantos@global-retail.com",
    
    [Parameter(Mandatory=$false)]
    [string]$BaseUrl = "https://gestiontimeapi.onrender.com"
)

$ErrorActionPreference = "Stop"

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  TEST: Endpoints de Presencia - GestionTime API" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# ═══════════════════════════════════════════════════════════════════════════════
# 1. LOGIN
# ═══════════════════════════════════════════════════════════════════════════════

Write-Host "📡 PASO 1: LOGIN" -ForegroundColor Yellow
Write-Host "Endpoint: $BaseUrl/api/v1/auth/login-desktop" -ForegroundColor Gray

$password = Read-Host "Ingresa la contraseña para $Email" -AsSecureString
$plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($password)
)

try {
    $loginBody = @{
        email = $Email
        password = $plainPassword
    } | ConvertTo-Json

    $login = Invoke-RestMethod -Uri "$BaseUrl/api/v1/auth/login-desktop" `
        -Method Post `
        -Body $loginBody `
        -ContentType "application/json"

    if ($login.accessToken) {
        $token = $login.accessToken
        Write-Host "✅ Token obtenido exitosamente" -ForegroundColor Green
        Write-Host "   Token (primeros 50 chars): $($token.Substring(0, [Math]::Min(50, $token.Length)))..." -ForegroundColor Gray
        Write-Host ""
    } else {
        Write-Host "❌ Error: Login no devolvió access_token" -ForegroundColor Red
        Write-Host "   Respuesta: $($login | ConvertTo-Json)" -ForegroundColor Gray
        exit 1
    }
} catch {
    Write-Host "❌ Error en Login:" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor Gray
    exit 1
}

# ═══════════════════════════════════════════════════════════════════════════════
# 2. TEST ENDPOINT ACTUAL: /api/v1/admin/users
# ═══════════════════════════════════════════════════════════════════════════════

Write-Host "📡 PASO 2: TEST /api/v1/admin/users (ACTUAL)" -ForegroundColor Yellow
Write-Host "Endpoint: $BaseUrl/api/v1/admin/users" -ForegroundColor Gray

try {
    $usersAdmin = Invoke-RestMethod -Uri "$BaseUrl/api/v1/admin/users" `
        -Headers @{Authorization="Bearer $token"} `
        -Method Get

    Write-Host "✅ Endpoint responde correctamente" -ForegroundColor Green
    Write-Host "   Total usuarios: $($usersAdmin.Count)" -ForegroundColor Gray
    Write-Host "   Autenticación: Bearer Token (JWT)" -ForegroundColor Gray
    Write-Host ""

    # Guardar respuesta
    $usersAdmin | ConvertTo-Json -Depth 3 | Out-File "users_admin_response.json"
    Write-Host "   Respuesta guardada en: users_admin_response.json" -ForegroundColor Gray
    Write-Host ""

    # Mostrar primer usuario (ejemplo)
    if ($usersAdmin.Count -gt 0) {
        Write-Host "   📄 Ejemplo de usuario (primero):" -ForegroundColor Gray
        $firstUser = $usersAdmin[0]
        Write-Host "      • ID: $($firstUser.id)" -ForegroundColor Gray
        Write-Host "      • Email: $($firstUser.email)" -ForegroundColor Gray
        Write-Host "      • FullName: $($firstUser.fullName)" -ForegroundColor Gray
        Write-Host "      • Enabled: $($firstUser.enabled)" -ForegroundColor Gray
        Write-Host "      • Roles: $($firstUser.roles -join ', ')" -ForegroundColor Gray
        Write-Host "      • LastSeenAt: $($firstUser.lastSeenAt)" -ForegroundColor Gray
        
        # Verificar IsOnline
        if ($firstUser.lastSeenAt) {
            $lastSeen = [DateTime]::Parse($firstUser.lastSeenAt)
            $minutesAgo = ([DateTime]::UtcNow - $lastSeen).TotalMinutes
            $isOnline = $minutesAgo -le 2
            
            $status = if ($isOnline) { "🟢 ONLINE" } else { "🔴 OFFLINE" }
            Write-Host "      • Estado: $status (hace $([Math]::Round($minutesAgo, 1)) minutos)" -ForegroundColor $(if ($isOnline) { "Green" } else { "Red" })
        } else {
            Write-Host "      • Estado: 🔴 OFFLINE (sin lastSeenAt)" -ForegroundColor Red
        }
    }
    Write-Host ""

} catch {
    Write-Host "❌ Error en /api/v1/admin/users:" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor Gray
    Write-Host ""
    
    # Verificar si es 401/403
    if ($_.Exception.Response) {
        $statusCode = [int]$_.Exception.Response.StatusCode
        Write-Host "   HTTP Status: $statusCode" -ForegroundColor Gray
        
        if ($statusCode -eq 401) {
            Write-Host "   ⚠️ Token inválido o expirado" -ForegroundColor Yellow
        } elseif ($statusCode -eq 403) {
            Write-Host "   ⚠️ Usuario $Email no tiene permisos de ADMIN" -ForegroundColor Yellow
        }
    }
    Write-Host ""
}

# ═══════════════════════════════════════════════════════════════════════════════
# 3. TEST ENDPOINT ALTERNATIVO: /v1/presence/users
# ═══════════════════════════════════════════════════════════════════════════════

Write-Host "📡 PASO 3: TEST /v1/presence/users (ALTERNATIVO)" -ForegroundColor Yellow
Write-Host "Endpoint: $BaseUrl/v1/presence/users" -ForegroundColor Gray

try {
    $usersPresence = Invoke-RestMethod -Uri "$BaseUrl/v1/presence/users" `
        -Headers @{Authorization="Bearer $token"} `
        -Method Get

    Write-Host "✅ Endpoint responde correctamente" -ForegroundColor Green
    Write-Host "   Total usuarios: $($usersPresence.Count)" -ForegroundColor Gray
    Write-Host "   Autenticación: Bearer Token (JWT)" -ForegroundColor Gray
    Write-Host ""

    # Guardar respuesta
    $usersPresence | ConvertTo-Json -Depth 3 | Out-File "users_presence_response.json"
    Write-Host "   Respuesta guardada en: users_presence_response.json" -ForegroundColor Gray
    Write-Host ""

    # Mostrar primer usuario (ejemplo)
    if ($usersPresence.Count -gt 0) {
        Write-Host "   📄 Ejemplo de usuario (primero):" -ForegroundColor Gray
        $firstUser = $usersPresence[0]
        Write-Host "      • ID: $($firstUser.id)" -ForegroundColor Gray
        Write-Host "      • Email: $($firstUser.email)" -ForegroundColor Gray
        Write-Host "      • FullName: $($firstUser.fullName)" -ForegroundColor Gray
        Write-Host "      • Enabled: $($firstUser.enabled)" -ForegroundColor Gray
        Write-Host "      • Roles: $($firstUser.roles -join ', ')" -ForegroundColor Gray
        Write-Host "      • LastSeenAt: $($firstUser.lastSeenAt)" -ForegroundColor Gray
    }
    Write-Host ""

} catch {
    Write-Host "❌ Endpoint /v1/presence/users NO EXISTE o requiere autenticación diferente" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Gray
    Write-Host ""
    
    # Verificar si es 404 (no existe)
    if ($_.Exception.Response) {
        $statusCode = [int]$_.Exception.Response.StatusCode
        Write-Host "   HTTP Status: $statusCode" -ForegroundColor Gray
        
        if ($statusCode -eq 404) {
            Write-Host "   ℹ️ Este endpoint NO ESTÁ IMPLEMENTADO en el backend actual" -ForegroundColor Cyan
        } elseif ($statusCode -eq 401) {
            Write-Host "   ⚠️ Requiere autenticación diferente (posiblemente cookies)" -ForegroundColor Yellow
        }
    }
    Write-Host ""
}

# ═══════════════════════════════════════════════════════════════════════════════
# 4. TEST ENDPOINT PING: /api/v1/admin/ping
# ═══════════════════════════════════════════════════════════════════════════════

Write-Host "📡 PASO 4: TEST /api/v1/admin/ping" -ForegroundColor Yellow
Write-Host "Endpoint: $BaseUrl/api/v1/admin/ping" -ForegroundColor Gray

try {
    $ping = Invoke-RestMethod -Uri "$BaseUrl/api/v1/admin/ping" `
        -Headers @{Authorization="Bearer $token"} `
        -Method Get

    Write-Host "✅ Ping enviado exitosamente" -ForegroundColor Green
    Write-Host "   Respuesta: $($ping | ConvertTo-Json -Compress)" -ForegroundColor Gray
    Write-Host ""

} catch {
    Write-Host "[ERROR] Error en /api/v1/admin/ping:" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor Gray
    Write-Host "   [AVISO] Endpoint posiblemente NO IMPLEMENTADO" -ForegroundColor Yellow
    Write-Host ""
}

# ═══════════════════════════════════════════════════════════════════════════════
# 5. COMPARACIÓN Y RECOMENDACIONES
# ═══════════════════════════════════════════════════════════════════════════════

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  RESUMEN Y RECOMENDACIONES" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

if ($usersAdmin -and $usersPresence) {
    Write-Host "✅ AMBOS ENDPOINTS FUNCIONAN" -ForegroundColor Green
    Write-Host ""
    Write-Host "   Comparación:" -ForegroundColor Yellow
    Write-Host "   • /api/v1/admin/users:    $($usersAdmin.Count) usuarios" -ForegroundColor Gray
    Write-Host "   • /v1/presence/users:     $($usersPresence.Count) usuarios" -ForegroundColor Gray
    Write-Host ""
    Write-Host "   📋 Revisa los archivos JSON para comparar estructura:" -ForegroundColor Yellow
    Write-Host "      • users_admin_response.json" -ForegroundColor Gray
    Write-Host "      • users_presence_response.json" -ForegroundColor Gray
    Write-Host ""
    Write-Host "   💡 RECOMENDACIÓN:" -ForegroundColor Yellow
    Write-Host "      Usa el endpoint que tenga TODOS los campos necesarios," -ForegroundColor Gray
    Write-Host "      especialmente 'lastSeenAt' para detectar online/offline." -ForegroundColor Gray

} elseif ($usersAdmin -and -not $usersPresence) {
    Write-Host "✅ SOLO /api/v1/admin/users FUNCIONA" -ForegroundColor Green
    Write-Host ""
    Write-Host "   💡 RECOMENDACIÓN:" -ForegroundColor Yellow
    Write-Host "      Mantén el sistema actual usando /api/v1/admin/users." -ForegroundColor Gray
    Write-Host "      Este endpoint ya tiene todo lo necesario:" -ForegroundColor Gray
    Write-Host "         • lastSeenAt (para detectar online/offline)" -ForegroundColor Gray
    Write-Host "         • roles (para agrupar usuarios)" -ForegroundColor Gray
    Write-Host "         • enabled (para filtrar activos)" -ForegroundColor Gray
    Write-Host "         • Autenticación Bearer Token (JWT)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "      ❌ /v1/presence/users NO EXISTE en tu backend actual." -ForegroundColor Red

} elseif ($usersPresence -and -not $usersAdmin) {
    Write-Host "⚠️ SOLO /v1/presence/users FUNCIONA" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "   💡 RECOMENDACIÓN:" -ForegroundColor Yellow
    Write-Host "      Cambia PresenceService.cs para usar /v1/presence/users" -ForegroundColor Gray
    Write-Host ""
    Write-Host "      📝 Cambio necesario:" -ForegroundColor Yellow
    Write-Host "         Archivo: Services\Presence\PresenceService.cs" -ForegroundColor Gray
    Write-Host "         Línea 47:" -ForegroundColor Gray
    Write-Host "            Antes: await App.Api.GetAsync<List<UserListItemDto>>(\"/api/v1/admin/users\", ct);" -ForegroundColor Red
    Write-Host "            Después: await App.Api.GetAsync<List<UserListItemDto>>(\"/v1/presence/users\", ct);" -ForegroundColor Green

} else {
    Write-Host "❌ NINGÚN ENDPOINT FUNCIONA" -ForegroundColor Red
    Write-Host ""
    Write-Host "   Posibles causas:" -ForegroundColor Yellow
    Write-Host "      • Backend no está corriendo" -ForegroundColor Gray
    Write-Host "      • Usuario no tiene permisos de ADMIN" -ForegroundColor Gray
    Write-Host "      • Token inválido o expirado" -ForegroundColor Gray
    Write-Host "      • Endpoints no implementados en el backend" -ForegroundColor Gray
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
