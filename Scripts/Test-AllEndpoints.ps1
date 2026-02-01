# ═══════════════════════════════════════════════════════════════════════════════
# Script: Test-AllEndpoints.ps1
# Descripcion: Prueba TODOS los endpoints de autenticacion y presencia
# ═══════════════════════════════════════════════════════════════════════════════

param(
    [Parameter(Mandatory=$false)]
    [string]$Email = "psantos@global-retail.com",
    
    [Parameter(Mandatory=$false)]
    [string]$BaseUrl = "https://gestiontimeapi.onrender.com"
)

$ErrorActionPreference = "Stop"

Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host "  TEST COMPLETO: Endpoints de Auth y Presencia - GestionTime API" -ForegroundColor Cyan
Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host ""

# Pedir contrasena
$password = Read-Host "Ingresa la contrasena para $Email" -AsSecureString
$plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($password)
)

$tokens = @{}

# ═══════════════════════════════════════════════════════════════════════════════
# FUNCION: Probar Login
# ═══════════════════════════════════════════════════════════════════════════════

function Test-LoginEndpoint {
    param(
        [string]$EndpointPath,
        [string]$Name
    )
    
    Write-Host "[TEST] $Name" -ForegroundColor Yellow
    Write-Host "Endpoint: $BaseUrl$EndpointPath" -ForegroundColor Gray
    
    try {
        $loginBody = @{
            email = $Email
            password = $plainPassword
        } | ConvertTo-Json

        $login = Invoke-RestMethod -Uri "$BaseUrl$EndpointPath" `
            -Method Post `
            -Body $loginBody `
            -ContentType "application/json"

        if ($login.accessToken) {
            $token = $login.accessToken
            Write-Host "[OK] Token obtenido exitosamente" -ForegroundColor Green
            Write-Host "   Token (50 chars): $($token.Substring(0, [Math]::Min(50, $token.Length)))..." -ForegroundColor Gray
            Write-Host ""
            return $token
        } else {
            Write-Host "[ERROR] Login no devolvio access_token" -ForegroundColor Red
            Write-Host "   Respuesta: $($login | ConvertTo-Json -Compress)" -ForegroundColor Gray
            Write-Host ""
            return $null
        }
    } catch {
        Write-Host "[ERROR] $($_.Exception.Message)" -ForegroundColor Red
        
        try {
            if ($_.Exception.Response) {
                $statusCode = [int]$_.Exception.Response.StatusCode
                Write-Host "   HTTP Status: $statusCode" -ForegroundColor Gray
                
                if ($statusCode -eq 404) {
                    Write-Host "   [INFO] Este endpoint NO EXISTE" -ForegroundColor Cyan
                } elseif ($statusCode -eq 401) {
                    Write-Host "   [AVISO] Credenciales invalidas" -ForegroundColor Yellow
                }
            }
        } catch { }
        
        Write-Host ""
        return $null
    }
}

# ═══════════════════════════════════════════════════════════════════════════════
# FUNCION: Probar Endpoint de Usuarios
# ═══════════════════════════════════════════════════════════════════════════════

function Test-UsersEndpoint {
    param(
        [string]$EndpointPath,
        [string]$Name,
        [string]$Token
    )
    
    Write-Host "[TEST] $Name" -ForegroundColor Yellow
    Write-Host "Endpoint: $BaseUrl$EndpointPath" -ForegroundColor Gray
    
    if ([string]::IsNullOrEmpty($Token)) {
        Write-Host "[SKIP] No hay token disponible" -ForegroundColor Yellow
        Write-Host ""
        return $null
    }
    
    try {
        $users = Invoke-RestMethod -Uri "$BaseUrl$EndpointPath" `
            -Headers @{Authorization="Bearer $Token"} `
            -Method Get

        Write-Host "[OK] Endpoint responde correctamente" -ForegroundColor Green
        Write-Host "   Total usuarios: $($users.Count)" -ForegroundColor Gray
        Write-Host ""

        # Guardar respuesta
        $filename = "users_" + $Name.Replace(" ", "_").Replace("/", "_") + ".json"
        $users | ConvertTo-Json -Depth 3 | Out-File $filename
        Write-Host "   Respuesta guardada en: $filename" -ForegroundColor Gray
        Write-Host ""

        # Mostrar primer usuario
        if ($users.Count -gt 0) {
            $firstUser = $users[0]
            Write-Host "   [EJEMPLO] Primer usuario:" -ForegroundColor Gray
            Write-Host "      - ID: $($firstUser.id)" -ForegroundColor Gray
            Write-Host "      - Email: $($firstUser.email)" -ForegroundColor Gray
            Write-Host "      - FullName: $($firstUser.fullName)" -ForegroundColor Gray
            Write-Host "      - Enabled: $($firstUser.enabled)" -ForegroundColor Gray
            Write-Host "      - Roles: $($firstUser.roles -join ', ')" -ForegroundColor Gray
            Write-Host "      - LastSeenAt: $($firstUser.lastSeenAt)" -ForegroundColor Gray
            
            # Verificar IsOnline
            if ($firstUser.lastSeenAt) {
                $lastSeen = [DateTime]::Parse($firstUser.lastSeenAt)
                $minutesAgo = ([DateTime]::UtcNow - $lastSeen).TotalMinutes
                $isOnline = $minutesAgo -le 2
                
                $status = if ($isOnline) { "[ONLINE]" } else { "[OFFLINE]" }
                Write-Host "      - Estado: $status (hace $([Math]::Round($minutesAgo, 1)) minutos)" -ForegroundColor $(if ($isOnline) { "Green" } else { "Red" })
            } else {
                Write-Host "      - Estado: [OFFLINE] (sin lastSeenAt)" -ForegroundColor Red
            }
        }
        Write-Host ""

        return $users
        
    } catch {
        Write-Host "[ERROR] $($_.Exception.Message)" -ForegroundColor Red
        
        try {
            if ($_.Exception.Response) {
                $statusCode = [int]$_.Exception.Response.StatusCode
                Write-Host "   HTTP Status: $statusCode" -ForegroundColor Gray
                
                if ($statusCode -eq 404) {
                    Write-Host "   [INFO] Este endpoint NO EXISTE" -ForegroundColor Cyan
                } elseif ($statusCode -eq 401) {
                    Write-Host "   [AVISO] Token invalido o expirado" -ForegroundColor Yellow
                } elseif ($statusCode -eq 403) {
                    Write-Host "   [AVISO] Usuario no tiene permisos" -ForegroundColor Yellow
                }
            }
        } catch { }
        
        Write-Host ""
        return $null
    }
}

# ═══════════════════════════════════════════════════════════════════════════════
# 1. PROBAR ENDPOINTS DE LOGIN
# ═══════════════════════════════════════════════════════════════════════════════

Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host "  PASO 1: Probar Endpoints de Login" -ForegroundColor Cyan
Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host ""

$loginEndpoints = @(
    @{ Path = "/api/v1/auth/login-desktop"; Name = "login-desktop" }
    @{ Path = "/api/v1/auth/login"; Name = "login" }
    @{ Path = "/v1/auth/login"; Name = "v1-login" }
)

foreach ($endpoint in $loginEndpoints) {
    $token = Test-LoginEndpoint -EndpointPath $endpoint.Path -Name $endpoint.Name
    if ($token) {
        $tokens[$endpoint.Name] = $token
    }
}

# ═══════════════════════════════════════════════════════════════════════════════
# 2. PROBAR ENDPOINTS DE USUARIOS
# ═══════════════════════════════════════════════════════════════════════════════

Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host "  PASO 2: Probar Endpoints de Usuarios" -ForegroundColor Cyan
Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host ""

$usersEndpoints = @(
    @{ Path = "/api/v1/admin/users"; Name = "admin-users"; RequiresLogin = "login-desktop" }
    @{ Path = "/v1/admin/users"; Name = "v1-admin-users"; RequiresLogin = "login" }
    @{ Path = "/v1/presence/users"; Name = "presence-users"; RequiresLogin = "login" }
    @{ Path = "/api/v1/presence/users"; Name = "api-presence-users"; RequiresLogin = "login-desktop" }
)

$results = @{}

foreach ($endpoint in $usersEndpoints) {
    # Intentar con el token del login requerido primero
    $token = $tokens[$endpoint.RequiresLogin]
    
    # Si no hay token, intentar con cualquier token disponible
    if (-not $token) {
        $token = $tokens.Values | Select-Object -First 1
    }
    
    $users = Test-UsersEndpoint -EndpointPath $endpoint.Path -Name $endpoint.Name -Token $token
    if ($users) {
        $results[$endpoint.Name] = $users
    }
}

# ═══════════════════════════════════════════════════════════════════════════════
# 3. RESUMEN Y RECOMENDACIONES
# ═══════════════════════════════════════════════════════════════════════════════

Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host "  RESUMEN Y RECOMENDACIONES" -ForegroundColor Cyan
Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "[LOGINS EXITOSOS]" -ForegroundColor Yellow
if ($tokens.Count -eq 0) {
    Write-Host "   [ERROR] Ningun endpoint de login funciono" -ForegroundColor Red
} else {
    foreach ($key in $tokens.Keys) {
        Write-Host "   [OK] $key" -ForegroundColor Green
    }
}
Write-Host ""

Write-Host "[ENDPOINTS DE USUARIOS EXITOSOS]" -ForegroundColor Yellow
if ($results.Count -eq 0) {
    Write-Host "   [ERROR] Ningun endpoint de usuarios funciono" -ForegroundColor Red
} else {
    foreach ($key in $results.Keys) {
        Write-Host "   [OK] $key ($($results[$key].Count) usuarios)" -ForegroundColor Green
    }
}
Write-Host ""

# ═══════════════════════════════════════════════════════════════════════════════
# 4. RECOMENDACION ESPECIFICA
# ═══════════════════════════════════════════════════════════════════════════════

Write-Host "[RECOMENDACION]" -ForegroundColor Yellow

if ($results.ContainsKey("presence-users")) {
    Write-Host ""
    Write-Host "   [ENCONTRADO] /v1/presence/users funciona!" -ForegroundColor Green
    Write-Host ""
    Write-Host "   [CAMBIO REQUERIDO] Actualizar PresenceService.cs:" -ForegroundColor Cyan
    Write-Host "      Archivo: Services\Presence\PresenceService.cs" -ForegroundColor Gray
    Write-Host "      Linea 47:" -ForegroundColor Gray
    Write-Host "         Antes: await App.Api.GetAsync<List<UserListItemDto>>(`"/api/v1/admin/users`", ct);" -ForegroundColor Red
    Write-Host "         Despues: await App.Api.GetAsync<List<UserListItemDto>>(`"/v1/presence/users`", ct);" -ForegroundColor Green
    Write-Host ""
    Write-Host "   [CAMBIO LOGIN] Actualizar App.xaml.cs:" -ForegroundColor Cyan
    Write-Host "      Archivo: App.xaml.cs" -ForegroundColor Gray
    Write-Host "      Linea ~165:" -ForegroundColor Gray
    Write-Host "         Antes: var loginPath = `"/api/v1/auth/login-desktop`";" -ForegroundColor Red
    Write-Host "         Despues: var loginPath = `"/v1/auth/login`";" -ForegroundColor Green
    Write-Host ""
    
} elseif ($results.ContainsKey("admin-users")) {
    Write-Host ""
    Write-Host "   [INFO] Sistema actual funciona correctamente" -ForegroundColor Green
    Write-Host "      - Endpoint: /api/v1/admin/users" -ForegroundColor Gray
    Write-Host "      - Login: /api/v1/auth/login-desktop" -ForegroundColor Gray
    Write-Host "      - Autenticacion: Bearer Token (JWT)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "   [NO CAMBIAR] /v1/presence/users NO existe en tu backend" -ForegroundColor Yellow
    Write-Host ""
    
} else {
    Write-Host ""
    Write-Host "   [ERROR] Ningun endpoint de usuarios funciono" -ForegroundColor Red
    Write-Host "      - Verifica que el usuario tenga permisos de ADMIN" -ForegroundColor Gray
    Write-Host "      - Revisa los tokens obtenidos" -ForegroundColor Gray
    Write-Host "      - Confirma que el backend este corriendo" -ForegroundColor Gray
    Write-Host ""
}

Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host ""
