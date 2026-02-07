# 🚪 AÑADIR BOTÓN "ECHAR USUARIO" (KICK) EN SETTINGS

**Fecha**: 2026-02-02  
**Endpoint**: `POST /api/v1/admin/presence/users/{userId}/kick`  
**Componente**: SettingsWindow → Gestión de Usuarios  
**Versión**: v1.1.0  

---

## 📋 **REQUISITO**

Añadir un botón "Echar" (Kick) en el flyout de edición de usuarios que:
1. Solo aparezca si el usuario está **ONLINE**
2. Llame al endpoint `/api/v1/admin/presence/users/{userId}/kick`
3. Revoque todas las sesiones del usuario (lo marca OFFLINE inmediatamente)
4. Actualice la UI para mostrar el usuario como OFFLINE

---

## 🎯 **IMPLEMENTACIÓN**

### **1. Añadir método en AdminUsersService.cs**

```csharp
/// <summary>Echa a un usuario online revocando todas sus sesiones activas.</summary>
/// <param name="userId">ID del usuario a echar (Guid).</param>
public async Task<bool> KickUserAsync(Guid userId, CancellationToken ct = default)
{
    try
    {
        var response = await _api.PostAsync($"/api/v1/admin/presence/users/{userId}/kick", null, ct);
        return response?.IsSuccessStatusCode == true;
    }
    catch (Exception ex)
    {
        _log?.LogError(ex, "Error echando usuario {UserId}", userId);
        return false;
    }
}
```

**Ubicación**: `Services/Admin/AdminUsersService.cs`

---

### **2. Actualizar UserViewModel con propiedad KickCommand**

```csharp
public class UserViewModel : INotifyPropertyChanged
{
    // ... propiedades existentes ...
    
    public ICommand KickCommand { get; set; }
    
    public bool CanKick => IsOnline; // Solo si está online
}
```

**Ubicación**: `Models/UserViewModel.cs`

---

### **3. Flyout con botón Kick (XAML)**

**Actualizar el Flyout de edición de usuario** en `SettingsWindow.xaml.cs` → `CreatePermissionsContent()`:

```xaml
<!-- Flyout al pulsar [⋯] en tarjeta de usuario -->
<Flyout x:Name="EditUserFlyout" Placement="Bottom">
    <StackPanel Width="280" Spacing="12">
        <TextBlock Text="Editar usuario" 
                   FontSize="16" 
                   FontWeight="SemiBold"/>
        
        <TextBlock Text="{Binding FullName}" 
                   FontWeight="SemiBold"/>
        <TextBlock Text="{Binding Email}" 
                   FontSize="12" 
                   Foreground="{ThemeResource TextFillColorSecondaryBrush}"/>
        
        <StackPanel Spacing="8">
            <TextBlock Text="Rol:" FontSize="12"/>
            <ComboBox x:Name="RoleComboBox"
                      SelectedItem="{Binding RolePrincipal, Mode=TwoWay}"
                      ItemsSource="{Binding AvailableRoles}"
                      HorizontalAlignment="Stretch"/>
        </StackPanel>
        
        <ToggleSwitch Header="Usuario habilitado"
                      IsOn="{Binding Enabled, Mode=TwoWay}"
                      OnContent="Sí"
                      OffContent="No"/>
        
        <!-- NUEVO: Botón Kick (solo si está online) -->
        <Button x:Name="BtnKick"
                Content="🚪 Echar usuario"
                Command="{Binding KickCommand}"
                Visibility="{Binding CanKick, Converter={StaticResource BoolToVisibilityConverter}}"
                HorizontalAlignment="Stretch"
                Background="#FF6B4C"
                Foreground="White"
                ToolTipService.ToolTip="Revoca todas las sesiones activas del usuario"/>
        
        <Grid ColumnDefinitions="*,Auto">
            <Button Grid.Column="0"
                    Content="💾 Guardar"
                    Click="OnSaveUserChanges"
                    HorizontalAlignment="Stretch"/>
            <Button Grid.Column="1"
                    Content="❌ Cancelar"
                    Click="OnCancelEditUser"
                    Margin="8,0,0,0"/>
        </Grid>
    </StackPanel>
</Flyout>
```

---

### **4. Handler del botón Kick (Code-behind)**

```csharp
private async void OnKickUser(object sender, RoutedEventArgs e)
{
    var button = sender as Button;
    var user = button?.DataContext as UserViewModel;
    if (user == null) return;
    
    // Confirmación
    var dialog = new ContentDialog
    {
        Title = "¿Echar usuario?",
        Content = $"¿Seguro que quieres echar a {user.FullName}?\n\n" +
                  "Se cerrarán todas sus sesiones activas y será marcado como offline.",
        PrimaryButtonText = "🚪 Echar",
        CloseButtonText = "Cancelar",
        DefaultButton = ContentDialogButton.Close,
        XamlRoot = this.Content.XamlRoot
    };
    
    if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        return;
    
    try
    {
        ShowLoading(user.Id, true);
        ShowStatusMessage($"⏳ Echando a {user.FullName}...", isSuccess: true);
        
        var success = await AdminUsersService.Instance.KickUserAsync(user.Id);
        
        if (success)
        {
            // Actualizar estado local
            user.IsOnline = false;
            
            // Refrescar lista completa
            await RefreshUsersList();
            
            ShowStatusMessage($"✅ {user.FullName} ha sido echado correctamente", isSuccess: true);
            EditUserFlyout.Hide();
        }
        else
        {
            ShowStatusMessage($"❌ Error al echar a {user.FullName}", isSuccess: false);
        }
    }
    catch (Exception ex)
    {
        _log?.LogError(ex, "Error en OnKickUser");
        ShowStatusMessage($"❌ Error: {ex.Message}", isSuccess: false);
    }
    finally
    {
        ShowLoading(user.Id, false);
    }
}
```

---

## 📊 **RESPUESTA DEL ENDPOINT**

### **Request**:
```http
POST /api/v1/admin/presence/users/{userId}/kick
Authorization: Bearer <token>
```

### **Response exitosa (200 OK)**:
```json
{
  "ok": true,
  "message": "Usuario echado correctamente",
  "sessionsRevoked": 2
}
```

### **Response error (404 Not Found)**:
```json
{
  "ok": false,
  "message": "Usuario no encontrado o sin sesiones activas"
}
```

### **Response error (403 Forbidden)**:
```json
{
  "message": "Solo usuarios ADMIN pueden echar usuarios"
}
```

---

## 🎨 **DISEÑO VISUAL**

### **Flyout CON usuario ONLINE**:

```
┌─────────────────────────────────┐
│ Editar usuario                  │
│                                 │
│ Francisco Santos                │
│ psantos@global-retail.com       │
│                                 │
│ Rol: [ADMIN ▼]                 │
│                                 │
│ ✅ Usuario habilitado          │
│                                 │
│ [🚪 Echar usuario]             │ ← NUEVO (rojo)
│                                 │
│ [💾 Guardar]  [❌ Cancelar]    │
└─────────────────────────────────┘
```

### **Flyout CON usuario OFFLINE** (botón NO visible):

```
┌─────────────────────────────────┐
│ Editar usuario                  │
│                                 │
│ Wilson Sánchez                  │
│ wsanchez@global-retail.com      │
│                                 │
│ Rol: [USER ▼]                  │
│                                 │
│ ✅ Usuario habilitado          │
│                                 │
│ [💾 Guardar]  [❌ Cancelar]    │
└─────────────────────────────────┘
```

---

## ✅ **VERIFICACIÓN**

### **Prueba 1: Usuario ONLINE**

1. Iniciar script de test: `.\Scripts\Test-UserPresence.ps1`
2. Abrir Settings → Permisos y roles
3. Ver que Wilson Sánchez aparece ONLINE
4. Pulsar [⋯] en su tarjeta
5. **Verificar**: Botón "🚪 Echar usuario" visible (rojo)
6. Pulsar botón Kick
7. Confirmar acción
8. **Resultado esperado**:
   - Usuario marcado como OFFLINE inmediatamente
   - Script de test muestra error 401 (sesión cerrada)
   - Toast: "✅ Wilson Sánchez ha sido echado correctamente"

### **Prueba 2: Usuario OFFLINE**

1. Detener script de test
2. Esperar 35 segundos (timeout)
3. Refrescar lista
4. Pulsar [⋯] en tarjeta de Wilson Sánchez
5. **Verificar**: Botón "Echar usuario" NO visible

### **Prueba 3: Usuario sin sesiones**

1. Usuario nunca ha iniciado sesión
2. Pulsar [⋯]
3. **Verificar**: Botón NO visible (IsOnline = false)

---

## 🔒 **VALIDACIONES**

### **Backend (endpoint)**:

1. ✅ **Solo ADMIN**: Verificar que el usuario autenticado tiene rol ADMIN
2. ✅ **Usuario existe**: Verificar que `{userId}` existe en la base de datos
3. ✅ **Sesiones activas**: Buscar sesiones con `RevokedAt IS NULL`
4. ✅ **Revocar sesiones**: Marcar `RevokedAt = DateTime.UtcNow`
5. ✅ **Logging**: Registrar quién echó a quién y cuándo

### **Frontend (Desktop)**:

1. ✅ **Botón solo visible si IsOnline**: Binding a `CanKick`
2. ✅ **Confirmación**: ContentDialog antes de ejecutar
3. ✅ **Actualización UI**: Refrescar lista después del kick
4. ✅ **Mensajes claros**: Toast con éxito/error

---

## 📝 **ARCHIVOS A MODIFICAR**

```
Services/Admin/
└── AdminUsersService.cs
    └── Añadir método KickUserAsync(userId)

Models/
└── UserViewModel.cs
    └── Añadir propiedad CanKick (calculada)
    └── Añadir KickCommand (ICommand)

Views/
└── SettingsWindow.xaml.cs
    └── CreatePermissionsContent()
        ├── Añadir botón Kick en Flyout
        └── Implementar OnKickUser handler
```

---

## 🔗 **ARCHIVOS RELACIONADOS**

### **Frontend**:
- `Services/Admin/AdminUsersService.cs` - Servicio de gestión de usuarios
- `Models/UserViewModel.cs` - ViewModel de usuario
- `Views/SettingsWindow.xaml.cs` - Vista de Settings
- `Docs/GESTION_USUARIOS_INLINE_SETTINGS.md` - Documentación existente

### **Backend** (GestionTimeApi):
- `Controllers/AdminPresenceController.cs` - Endpoint `/kick` (nuevo)
- `Controllers/AuthController.cs` - Lógica de revocación de sesiones (ya existe)
- `Models/UserSession.cs` - Tabla de sesiones

---

## 📚 **DOCUMENTACIÓN RELACIONADA**

- [FIX_LOGOUT_PRESENCIA_BACKEND.md](FIX_LOGOUT_PRESENCIA_BACKEND.md) - Cómo funciona la revocación de sesiones
- [GESTION_USUARIOS_INLINE_SETTINGS.md](GESTION_USUARIOS_INLINE_SETTINGS.md) - Gestión de usuarios en Settings
- [SISTEMA_ROLES_USUARIOS.md](SISTEMA_ROLES_USUARIOS.md) - Sistema de roles ADMIN/EDITOR/USER

---

## 💡 **IMPLEMENTACIÓN DEL BACKEND (si no existe)**

Si el endpoint `/kick` aún no existe en el backend, este sería el código:

```csharp
// Controllers/AdminPresenceController.cs

[HttpPost("users/{userId}/kick")]
[Authorize(Roles = "ADMIN")]
public async Task<IActionResult> KickUser(Guid userId)
{
    var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
    _logger.LogInformation("ADMIN {CurrentUserId} intenta echar a {TargetUserId}", 
        currentUserId, userId);
    
    // Buscar sesiones activas
    var sessions = await _db.UserSessions
        .Where(s => s.UserId == userId && s.RevokedAt == null)
        .ToListAsync();
    
    if (!sessions.Any())
    {
        return NotFound(new { ok = false, message = "Usuario no tiene sesiones activas" });
    }
    
    // Revocar todas las sesiones
    foreach (var session in sessions)
    {
        session.RevokedAt = DateTime.UtcNow;
    }
    
    await _db.SaveChangesAsync();
    
    _logger.LogInformation("✅ Usuario {UserId} echado: {Count} sesiones revocadas", 
        userId, sessions.Count);
    
    return Ok(new 
    { 
        ok = true, 
        message = "Usuario echado correctamente",
        sessionsRevoked = sessions.Count
    });
}
```

---

## 🎯 **RESULTADO FINAL**

| Característica | Estado |
|----------------|--------|
| **Botón Kick visible si ONLINE** | ✅ Implementar |
| **Botón oculto si OFFLINE** | ✅ Implementar |
| **Confirmación antes de echar** | ✅ Implementar |
| **Endpoint funcional** | ⚠️ **Verificar si existe** |
| **Actualización UI automática** | ✅ Implementar |
| **Toast con feedback** | ✅ Implementar |

---

## 🚀 **SIGUIENTE PASO**

**1. Verificar si el endpoint existe**:
```powershell
curl https://localhost:2502/api/v1/admin/presence/users/00000000-0000-0000-0000-000000000000/kick `
     -Method POST `
     -Headers @{"Authorization"="Bearer TOKEN"}
```

**Respuesta esperada**:
- **404**: Usuario no encontrado (endpoint existe ✅)
- **405/404 Method Not Allowed**: Endpoint NO existe (crear en backend)

**2. Si no existe, crear en backend** con el código de arriba.

**3. Implementar en Desktop** con los pasos 1-4 de esta guía.

---

**Autor**: GitHub Copilot  
**Ticket**: Añadir botón "Echar usuario" en gestión de usuarios  
**Prioridad**: 🟡 MEDIA (funcionalidad ADMIN)
