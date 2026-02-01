# Script para Cambiar Roles de Usuario - GestionTime
# Uso: .\Change-UserRole.ps1 -UserId 5 -NewRole "ADMIN"

param(
    [Parameter(Mandatory=$false)]
    [int]$UserId,
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("ADMIN", "EDITOR", "USER")]
    [string]$NewRole,
    
    [Parameter(Mandatory=$false)]
    [string]$AdminEmail = "admin@empresa.com",
    
    [Parameter(Mandatory=$false)]
    [string]$AdminPassword,
    
    [Parameter(Mandatory=$false)]
    [string]$ApiUrl = "https://gestiontimeapi.onrender.com"
)

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🔐 GestionTime - Cambiar Rol de Usuario" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════`n" -ForegroundColor Cyan

# Función para hacer login
function Get-AuthToken {
    param([string]$Email, [string]$Password)
    
    Write-Host "🔑 Iniciando sesión como $Email..." -ForegroundColor Yellow
    
    $loginBody = @{
        email = $Email
        password = $Password
    } | ConvertTo-Json
    
    try {
        $response = Invoke-RestMethod -Uri "$ApiUrl/api/v1/auth/login-desktop" `
            -Method Post `
            -Body $loginBody `
            -ContentType "application/json"
        
        Write-Host "✅ Login exitoso!" -ForegroundColor Green
        return $response.AccessToken
    }
    catch {
        Write-Host "❌ Error en login: $_" -ForegroundColor Red
        return $null
    }
}

# Función para listar usuarios
function Get-Users {
    param([string]$Token)
    
    $headers = @{ Authorization = "Bearer $Token" }
    
    try {
        $users = Invoke-RestMethod -Uri "$ApiUrl/api/v1/admin/users" `
            -Headers $headers `
            -Method Get
        
        return $users
    }
    catch {
        Write-Host "❌ Error obteniendo usuarios: $_" -ForegroundColor Red
        return @()
    }
}

# Función para cambiar rol
function Update-UserRole {
    param([string]$Token, [int]$UserId, [string]$Role)
    
    $headers = @{ Authorization = "Bearer $Token" }
    $body = @{ role = $Role } | ConvertTo-Json
    
    try {
        $result = Invoke-RestMethod -Uri "$ApiUrl/api/v1/admin/users/$UserId/roles" `
            -Headers $headers `
            -Method Put `
            -Body $body `
            -ContentType "application/json"
        
        return $result
    }
    catch {
        Write-Host "❌ Error actualizando rol: $_" -ForegroundColor Red
        return $null
    }
}

# MAIN SCRIPT

# Si no se proporcionó contraseña, pedirla
if ([string]::IsNullOrEmpty($AdminPassword)) {
    $securePassword = Read-Host "🔒 Contraseña de $AdminEmail" -AsSecureString
    $AdminPassword = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    )
}

# Hacer login
$token = Get-AuthToken -Email $AdminEmail -Password $AdminPassword

if ([string]::IsNullOrEmpty($token)) {
    Write-Host "`n❌ No se pudo obtener el token. Verifica las credenciales." -ForegroundColor Red
    exit 1
}

# Obtener lista de usuarios
Write-Host "`n📋 Obteniendo lista de usuarios..." -ForegroundColor Yellow
$users = Get-Users -Token $token

if ($users.Count -eq 0) {
    Write-Host "⚠️ No se encontraron usuarios." -ForegroundColor Yellow
    exit 0
}

# Mostrar tabla de usuarios
Write-Host "`n═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "📊 USUARIOS DISPONIBLES" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════`n" -ForegroundColor Cyan

$users | Format-Table -Property `
    @{Name="ID"; Expression={$_.id}; Width=5},
    @{Name="Email"; Expression={$_.email}; Width=30},
    @{Name="Nombre"; Expression={if($_.first_name){$_.first_name + " " + $_.last_name}else{"N/A"}}; Width=25},
    @{Name="Rol"; Expression={$_.role}; Width=10},
    @{Name="Activo"; Expression={if($_.is_active){"✓"}else{"✗"}}; Width=7}

# Si no se proporcionó ID de usuario, preguntar
if ($UserId -eq 0) {
    $UserId = Read-Host "`n👤 ID del usuario a modificar"
}

# Verificar que el usuario existe
$selectedUser = $users | Where-Object { $_.id -eq $UserId }

if ($null -eq $selectedUser) {
    Write-Host "❌ Usuario con ID $UserId no encontrado." -ForegroundColor Red
    exit 1
}

Write-Host "`n✅ Usuario seleccionado:" -ForegroundColor Green
Write-Host "   Email: $($selectedUser.email)"
Write-Host "   Rol actual: $($selectedUser.role)" -ForegroundColor Yellow

# Si no se proporcionó nuevo rol, preguntar
if ([string]::IsNullOrEmpty($NewRole)) {
    Write-Host "`n🎯 Roles disponibles:"
    Write-Host "   1) ADMIN   - Control total del sistema"
    Write-Host "   2) EDITOR  - Puede editar y gestionar contenido"
    Write-Host "   3) USER    - Solo lectura"
    
    $roleChoice = Read-Host "`nSelecciona el nuevo rol (1-3)"
    
    $NewRole = switch ($roleChoice) {
        "1" { "ADMIN" }
        "2" { "EDITOR" }
        "3" { "USER" }
        default { 
            Write-Host "❌ Opción inválida." -ForegroundColor Red
            exit 1
        }
    }
}

# Confirmar cambio
Write-Host "`n⚠️ CONFIRMACIÓN:" -ForegroundColor Yellow
Write-Host "   Usuario: $($selectedUser.email)"
Write-Host "   Rol actual: $($selectedUser.role)"
Write-Host "   Nuevo rol: $NewRole" -ForegroundColor Cyan

$confirm = Read-Host "`n¿Continuar con el cambio? (S/N)"

if ($confirm -ne "S" -and $confirm -ne "s") {
    Write-Host "`n❌ Operación cancelada." -ForegroundColor Red
    exit 0
}

# Cambiar rol
Write-Host "`n🔄 Actualizando rol..." -ForegroundColor Yellow
$result = Update-UserRole -Token $token -UserId $UserId -Role $NewRole

if ($null -ne $result -and $result.success) {
    Write-Host "`n✅ ¡ROL ACTUALIZADO EXITOSAMENTE!" -ForegroundColor Green
    Write-Host "   $($result.message)" -ForegroundColor Cyan
    
    # Verificar cambio
    Write-Host "`n📊 Verificando cambio..." -ForegroundColor Yellow
    $updatedUsers = Get-Users -Token $token
    $updatedUser = $updatedUsers | Where-Object { $_.id -eq $UserId }
    
    if ($updatedUser.role -eq $NewRole) {
        Write-Host "✅ Verificación exitosa. Nuevo rol: $($updatedUser.role)" -ForegroundColor Green
        
        Write-Host "`n⚠️ IMPORTANTE:" -ForegroundColor Yellow
        Write-Host "   El usuario debe CERRAR SESIÓN y VOLVER A ENTRAR" -ForegroundColor Yellow
        Write-Host "   para que el nuevo rol se aplique correctamente." -ForegroundColor Yellow
    }
    else {
        Write-Host "⚠️ El rol no se refleja aún. Puede tardar unos segundos." -ForegroundColor Yellow
    }
}
else {
    Write-Host "`n❌ No se pudo actualizar el rol." -ForegroundColor Red
}

Write-Host "`n═══════════════════════════════════════════════════════" -ForegroundColor Cyan
