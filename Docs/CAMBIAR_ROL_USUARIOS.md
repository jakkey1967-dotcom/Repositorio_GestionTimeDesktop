# 🔐 Cambiar Rol de Usuarios en GestionTime Desktop

## Método 1: Desde PowerShell (Directamente al Backend)

```powershell
# 1. Login como ADMIN para obtener el token
$loginBody = @{
    email = "admin@empresa.com"
    password = "tu_contraseña_admin"
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri "https://gestiontimeapi.onrender.com/api/v1/auth/login-desktop" `
    -Method Post `
    -Body $loginBody `
    -ContentType "application/json"

$token = $loginResponse.AccessToken
Write-Host "✅ Token obtenido: $($token.Substring(0,20))..." -ForegroundColor Green

# 2. Obtener lista de usuarios para ver los IDs
$headers = @{ Authorization = "Bearer $token" }
$users = Invoke-RestMethod -Uri "https://gestiontimeapi.onrender.com/api/v1/admin/users" `
    -Headers $headers `
    -Method Get

Write-Host "`n📋 Usuarios disponibles:" -ForegroundColor Cyan
$users | Format-Table Id, Email, Role, @{Name="Active";Expression={$_.is_active}}

# 3. Cambiar el rol de un usuario
$userId = 5  # ⚠️ CAMBIAR por el ID real del usuario
$newRole = "EDITOR"  # Opciones: ADMIN, EDITOR, USER

$roleBody = @{
    role = $newRole
} | ConvertTo-Json

Write-Host "`n🔄 Cambiando rol del usuario $userId a $newRole..." -ForegroundColor Yellow

try {
    $result = Invoke-RestMethod -Uri "https://gestiontimeapi.onrender.com/api/v1/admin/users/$userId/roles" `
        -Headers $headers `
        -Method Put `
        -Body $roleBody `
        -ContentType "application/json"
    
    Write-Host "✅ Rol actualizado exitosamente!" -ForegroundColor Green
    Write-Host "Mensaje: $($result.message)" -ForegroundColor Cyan
    
    # Verificar cambio
    $updatedUser = Invoke-RestMethod -Uri "https://gestiontimeapi.onrender.com/api/v1/admin/users" `
        -Headers $headers `
        -Method Get | Where-Object { $_.id -eq $userId }
    
    Write-Host "`n📊 Usuario actualizado:" -ForegroundColor Green
    Write-Host "  ID: $($updatedUser.id)"
    Write-Host "  Email: $($updatedUser.email)"
    Write-Host "  Rol: $($updatedUser.role)" -ForegroundColor Yellow
}
catch {
    Write-Host "❌ Error: $_" -ForegroundColor Red
}
```

---

## Método 2: Desde el Código C# (Desktop App)

### Ejemplo básico en un botón de contexto:

```csharp
// En UsersOnlineWindow.xaml.cs o en un nuevo AdminUsersPage.xaml.cs

private async void OnChangeRoleClick(object sender, RoutedEventArgs e)
{
    // Obtener el usuario seleccionado
    var button = sender as Button;
    var user = button?.DataContext as UserCardItem;
    
    if (user == null) return;

    // Mostrar diálogo de selección de rol
    var dialog = new ContentDialog
    {
        Title = $"Cambiar rol de {user.FullName}",
        Content = CreateRoleSelector(user.Role),
        PrimaryButtonText = "Guardar",
        CloseButtonText = "Cancelar",
        XamlRoot = this.Content.XamlRoot
    };

    var result = await dialog.ShowAsync();
    
    if (result == ContentDialogResult.Primary)
    {
        var selectedRole = GetSelectedRole(); // Implementar según tu UI
        
        // Llamar al servicio
        var success = await AdminUsersService.Instance.UpdateUserRoleAsync(
            user.Id, 
            selectedRole
        );

        if (success)
        {
            App.Notifications?.ShowSuccess(
                $"Rol de {user.FullName} actualizado a {selectedRole}",
                title: "✅ Rol Actualizado"
            );
            
            // Refrescar lista de usuarios
            await RefreshUsersAsync();
        }
        else
        {
            App.Notifications?.ShowError(
                "No se pudo actualizar el rol. Verifica los permisos.",
                title: "❌ Error"
            );
        }
    }
}

private StackPanel CreateRoleSelector(string currentRole)
{
    var stack = new StackPanel { Spacing = 12 };
    
    var roles = AdminUsersService.Instance.GetAvailableRoles();
    
    foreach (var role in roles)
    {
        var radio = new RadioButton
        {
            Content = role,
            GroupName = "RoleGroup",
            IsChecked = role == currentRole,
            Tag = role
        };
        stack.Children.Add(radio);
    }
    
    return stack;
}
```

---

## Método 3: Script SQL Directo (Más Rápido)

Si tienes acceso directo a la base de datos:

```sql
-- Ver usuarios actuales
SELECT id, email, role, is_active FROM users;

-- Cambiar rol (reemplaza ID y rol según necesites)
UPDATE users 
SET role = 'ADMIN'  -- Opciones: ADMIN, EDITOR, USER
WHERE id = 5;

-- Verificar cambio
SELECT id, email, role FROM users WHERE id = 5;
```

---

## 🎯 Roles Disponibles

| Rol | Descripción | Permisos |
|-----|-------------|----------|
| **ADMIN** | Administrador del sistema | Acceso completo, puede ver todos los usuarios |
| **EDITOR** | Supervisor/Editor | Puede editar contenido y ver usuarios |
| **USER** | Usuario estándar | Solo lectura |

---

## ✅ Verificar que Funcionó

1. **Cerrar sesión** en la aplicación Desktop
2. **Volver a hacer login** (para obtener nuevo token con rol actualizado)
3. La ventana de usuarios debería mostrar el rol actualizado
4. El usuario debería tener los permisos correspondientes al nuevo rol

---

## 🚨 Importante

- Solo usuarios con rol **ADMIN** pueden cambiar roles
- Después de cambiar el rol, el usuario debe **cerrar sesión y volver a entrar**
- El token JWT contiene el rol, por lo que hasta que no se renueve, el cambio no se refleja en el cliente

---

## 🔧 Testing Rápido

```powershell
# Cambiar un usuario a ADMIN (para testing)
curl -X PUT "https://gestiontimeapi.onrender.com/api/v1/admin/users/5/roles" `
  -H "Authorization: Bearer TU_TOKEN_AQUI" `
  -H "Content-Type: application/json" `
  -d '{\"role\": \"ADMIN\"}'
```
