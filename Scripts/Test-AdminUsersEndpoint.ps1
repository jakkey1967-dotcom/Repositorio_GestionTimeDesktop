# Script de Prueba - Endpoint GET /api/v1/admin/users
# Este script verifica que el endpoint devuelva usuarios correctamente

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🔍 Verificando Endpoint GET /api/v1/admin/users" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════`n" -ForegroundColor Cyan

# Credenciales (REEMPLAZA CON TUS DATOS)
$email = Read-Host "📧 Email de usuario ADMIN"
$securePassword = Read-Host "🔒 Contraseña" -AsSecureString
$password = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword))

# 1. Login
Write-Host "`n🔑 Iniciando sesión..." -ForegroundColor Yellow
$loginBody = @{
    email = $email
    password = $password
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "https://gestiontimeapi.onrender.com/api/v1/auth/login-desktop" `
        -Method Post `
        -Body $loginBody `
        -ContentType "application/json"
    
    $token = $loginResponse.AccessToken
    Write-Host "✅ Login exitoso!" -ForegroundColor Green
    Write-Host "   Token: $($token.Substring(0,20))..." -ForegroundColor Gray
    Write-Host "   Usuario: $($loginResponse.UserName)" -ForegroundColor Gray
    Write-Host "   Rol: $($loginResponse.UserRole)" -ForegroundColor Yellow
}
catch {
    Write-Host "❌ Error en login: $_" -ForegroundColor Red
    exit 1
}

# 2. Obtener usuarios
Write-Host "`n📋 Obteniendo lista de usuarios..." -ForegroundColor Yellow
$headers = @{ Authorization = "Bearer $token" }

try {
    $users = Invoke-RestMethod -Uri "https://gestiontimeapi.onrender.com/api/v1/admin/users" `
        -Headers $headers `
        -Method Get
    
    Write-Host "✅ Usuarios obtenidos exitosamente!" -ForegroundColor Green
    Write-Host "   Total: $($users.Count) usuarios" -ForegroundColor Cyan
    
    # Mostrar estructura del primer usuario
    if ($users.Count -gt 0) {
        Write-Host "`n📊 Estructura del primer usuario:" -ForegroundColor Cyan
        $firstUser = $users[0]
        $firstUser | ConvertTo-Json -Depth 3 | Write-Host -ForegroundColor Gray
    }
    
    # Mostrar tabla de usuarios
    Write-Host "`n═══════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "👥 LISTA DE USUARIOS" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════`n" -ForegroundColor Cyan
    
    $users | Format-Table -Property `
        @{Name="ID"; Expression={$_.id.ToString().Substring(0,8)+"..."}; Width=12},
        @{Name="Email"; Expression={$_.email}; Width=30},
        @{Name="Nombre"; Expression={$_.fullName}; Width=25},
        @{Name="Roles"; Expression={$_.roles -join ", "}; Width=20},
        @{Name="Activo"; Expression={if($_.enabled){"✓"}else{"✗"}}; Width=7}
    
    # Verificar campos requeridos
    Write-Host "`n🔍 Verificando campos del DTO..." -ForegroundColor Yellow
    
    $requiredFields = @("id", "email", "fullName", "enabled", "roles")
    $firstUser = $users[0]
    $missingFields = @()
    
    foreach ($field in $requiredFields) {
        if ($null -eq $firstUser.$field) {
            $missingFields += $field
            Write-Host "   ❌ Campo faltante: $field" -ForegroundColor Red
        }
        else {
            Write-Host "   ✅ Campo presente: $field" -ForegroundColor Green
        }
    }
    
    if ($missingFields.Count -eq 0) {
        Write-Host "`n✅ TODOS LOS CAMPOS REQUERIDOS ESTÁN PRESENTES" -ForegroundColor Green
        Write-Host "   La ventana de usuarios debería funcionar correctamente" -ForegroundColor Cyan
    }
    else {
        Write-Host "`n❌ FALTAN CAMPOS EN LA RESPUESTA DEL BACKEND" -ForegroundColor Red
        Write-Host "   Campos faltantes: $($missingFields -join ', ')" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "❌ Error obteniendo usuarios: $_" -ForegroundColor Red
    Write-Host "`nDetalles del error:" -ForegroundColor Yellow
    Write-Host $_.Exception.Message -ForegroundColor Gray
    
    if ($_.Exception.Response) {
        Write-Host "`nCódigo de estado: $($_.Exception.Response.StatusCode)" -ForegroundColor Yellow
    }
}

Write-Host "`n═══════════════════════════════════════════════════════" -ForegroundColor Cyan
