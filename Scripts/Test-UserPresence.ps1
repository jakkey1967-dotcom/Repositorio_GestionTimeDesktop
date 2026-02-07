# ====================================================
# Test-UserPresence.ps1
# Simula otro usuario conectandose para probar presencia
# ====================================================

param(
    [Parameter(Mandatory=$false)]
    [string]$Email = "wsanchez@global-retail.com",
    
    [Parameter(Mandatory=$false)]
    [string]$Password = "12345678",
    
    [Parameter(Mandatory=$false)]
    [string]$BaseUrl = "https://localhost:2502"
)

Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host "SIMULADOR DE PRESENCIA DE USUARIO" -ForegroundColor Cyan
Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Usuario: $Email" -ForegroundColor Yellow
Write-Host "API Base: $BaseUrl" -ForegroundColor Yellow
Write-Host ""

# Ignorar certificados SSL en desarrollo
if ($BaseUrl -like "*localhost*" -or $BaseUrl -like "*127.0.0.1*") {
    Write-Host "MODO DESARROLLO: Ignorando validacion SSL" -ForegroundColor Yellow
    
    # Crear clase para ignorar certificados SSL
    $code = @"
using System.Net;
using System.Security.Cryptography.X509Certificates;
public class TrustAllCertsPolicy : ICertificatePolicy {
    public bool CheckValidationResult(
        ServicePoint srvPoint, X509Certificate certificate,
        WebRequest request, int certificateProblem) {
        return true;
    }
}
"@
    
    # Solo añadir si no existe ya
    if (-not ([System.Management.Automation.PSTypeName]'TrustAllCertsPolicy').Type) {
        Add-Type -TypeDefinition $code
    }
    
    [System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
}

# ===== PASO 1: LOGIN =====
Write-Host ""
Write-Host "PASO 1: Iniciando sesion..." -ForegroundColor Cyan

$loginBody = @{
    email = $Email
    password = $Password
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod `
        -Uri "$BaseUrl/api/v1/auth/login-desktop" `
        -Method POST `
        -Body $loginBody `
        -ContentType "application/json"
    
    $token = $loginResponse.accessToken
    
    if (-not $token) {
        Write-Host "ERROR: No se obtuvo token de acceso" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "Login exitoso" -ForegroundColor Green
    Write-Host "   Token: $($token.Substring(0, 20))..." -ForegroundColor Gray
    Write-Host "   Usuario: $($loginResponse.user.fullName)" -ForegroundColor Gray
    Write-Host "   ID: $($loginResponse.user.id)" -ForegroundColor Gray
}
catch {
    Write-Host "ERROR en login: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host "   Detalles: $($_.ErrorDetails.Message)" -ForegroundColor Gray
    }
    exit 1
}

# ===== PASO 2: ENVIAR PINGS DE PRESENCIA =====
Write-Host ""
Write-Host "PASO 2: Enviando pings de presencia..." -ForegroundColor Cyan
Write-Host "   (Presiona Ctrl+C para detener)" -ForegroundColor Yellow
Write-Host ""

$pingCount = 0
$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Determinar endpoint según el rol del usuario
$userRole = "USER"  # Por defecto

if ($loginResponse.user.roles -and $loginResponse.user.roles.Count -gt 0) {
    $userRole = $loginResponse.user.roles[0]
    Write-Host "Rol del usuario: $userRole" -ForegroundColor Gray
}
else {
    Write-Host "Rol del usuario: No especificado (usando USER por defecto)" -ForegroundColor Yellow
}

if ($userRole -eq "ADMIN") {
    $pingEndpoint = "$BaseUrl/api/v1/admin/ping"
    Write-Host "Usando endpoint ADMIN: $pingEndpoint" -ForegroundColor Gray
}
else {
    $pingEndpoint = "$BaseUrl/api/v1/admin/ping"
    Write-Host "Usando endpoint: $pingEndpoint" -ForegroundColor Yellow
    Write-Host "NOTA: Este usuario NO es ADMIN, puede fallar con 403." -ForegroundColor Yellow
}

Write-Host ""

try {
    while ($true) {
        $pingCount++
        
        $timestamp = Get-Date -Format "HH:mm:ss"
        Write-Host "[$timestamp] Ping #$pingCount enviando..." -NoNewline
        
        try {
            $pingResponse = Invoke-RestMethod `
                -Uri $pingEndpoint `
                -Method GET `
                -Headers $headers
            
            if ($pingResponse.ok) {
                Write-Host " OK (Role: $($pingResponse.role))" -ForegroundColor Green
            }
            else {
                Write-Host " Respuesta inesperada" -ForegroundColor Yellow
            }
        }
        catch {
            Write-Host " ERROR: $($_.Exception.Message)" -ForegroundColor Red
            
            # Si es 401, el token expiró
            if ($_.Exception.Response.StatusCode -eq 401) {
                Write-Host "   Token expirado, reintentando login..." -ForegroundColor Yellow
                
                $loginResponse = Invoke-RestMethod `
                    -Uri "$BaseUrl/api/v1/auth/login-desktop" `
                    -Method POST `
                    -Body $loginBody `
                    -ContentType "application/json"
                
                $token = $loginResponse.accessToken
                $headers["Authorization"] = "Bearer $token"
                
                Write-Host "   Token renovado" -ForegroundColor Green
            }
            elseif ($_.Exception.Response.StatusCode -eq 403) {
                Write-Host "   (403: Sin permisos ADMIN, pero continua activo)" -ForegroundColor Yellow
                # NO hacer break, continuar enviando pings
            }
        }
        
        # Esperar 5 segundos (igual que el heartbeat real)
        Start-Sleep -Seconds 5
    }
}
catch {
    Write-Host ""
    Write-Host "Simulacion detenida" -ForegroundColor Yellow
}
finally {
    Write-Host ""
    Write-Host "===================================================================" -ForegroundColor Cyan
    Write-Host "RESUMEN" -ForegroundColor Cyan
    Write-Host "===================================================================" -ForegroundColor Cyan
    Write-Host "   Total de pings enviados: $pingCount" -ForegroundColor Gray
    Write-Host "   Usuario: $Email" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Cerrando sesion de $Email..." -ForegroundColor Yellow
    Write-Host "(El backend cerrara la sesion automaticamente al no recibir pings)" -ForegroundColor Gray
    Write-Host ""
}




