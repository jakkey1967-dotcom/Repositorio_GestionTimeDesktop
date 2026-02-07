# 🔐 Gestión de Usuarios Inline en Settings

**Fecha**: 2025-02-01  
**Estado**: Backend actualizado, DTOs creados ✅

---

## ✅ COMPLETADO

### 1. DTOs actualizados
- ✅ `UpdateUserRolesRequest` (con `roles: string[]`)
- ✅ `UpdateUserEnabledRequest` (con `enabled: bool`)
- ✅ Legacy `UpdateUserRoleRequest` marcado como `[Obsolete]`

### 2. AdminUsersService ampliado
- ✅ `GetRolesAsync()` → GET /api/v1/roles
- ✅ `UpdateUserRolesAsync(userId, roles[])` → PUT /api/v1/users/{id}/roles
- ✅ `UpdateUserEnabledAsync(userId, enabled)` → PUT /api/v1/users/{id}/enabled

---

## ⏳ PENDIENTE: Actualizar UI en SettingsWindow

### Diseño propuesto (sin romper layout actual)

En `CreatePermissionsContent()`, reemplazar el botón "Abrir Gestión de Usuarios" por:

**ListView de usuarios con cards**:

```
┌─────────────────────────────────────────────────────────────┐
│  Gestión de roles de usuarios                              │
│  ⚠️ Solo ADMIN: Asignar roles y habilitar/deshabilitar     │
│                                                             │
│  [🔄 Actualizar]  [🔍 Buscar: ____________]                 │
│                                                             │
│  ┌─ ADMIN (2) ────────────────────────────────────────────┐│
│  │  ┌─────────────────────────────────────────────────┐   ││
│  │  │ 👤 Pedro Santos                      [⋯]       │   ││
│  │  │ 📧 psantos@example.com                         │   ││
│  │  │ 🟢 Online                                       │   ││
│  │  └─────────────────────────────────────────────────┘   ││
│  │  ┌─────────────────────────────────────────────────┐   ││
│  │  │ 👤 Admin User                         [⋯]       │   ││
│  │  │ 📧 admin@example.com                           │   ││
│  │  │ ⚪ Offline                                      │   ││
│  │  └─────────────────────────────────────────────────┘   ││
│  └─────────────────────────────────────────────────────────┘│
│                                                             │
│  ┌─ USER (5) ────────────────────────────────────────────┐│
│  │  ...                                                   ││
│  └─────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────┘
```

### Flyout al pulsar [⋯]:

```
┌─────────────────────────────────┐
│ 👤 Editar usuario              │
│                                 │
│ Rol actual: [ADMIN ▼]          │
│                                 │
│ ✅ Usuario habilitado          │
│                                 │
│ [💾 Guardar]  [❌ Cancelar]    │
│                                 │
│ (Si Online) [🚪 Kick]          │
└─────────────────────────────────┘
```

### Lógica de carga

1. **Cargar usuarios**: `GET /api/v1/users?page=1&pageSize=50`
2. **Cargar presencia**: Reutilizar `PresenceService.GetAllUsersAsync()`
3. **Combinar datos** por `userId`:
   - Enabled/Roles viene de `/users`
   - Online/Offline viene de `/presence`
4. **Agrupar por rol** (como cards actuales)

### Código ejemplo (pseudocódigo)

```csharp
private async Task<List<UserViewModel>> LoadUsersWithPresenceAsync()
{
    // 1. Cargar usuarios del sistema
    var usersResponse = await App.Api.GetAsync<UsersPagedResult>("/api/v1/users?pageSize=50");
    
    // 2. Cargar presencia
    var presenceUsers = await PresenceService.Instance.GetAllUsersAsync();
    
    // 3. Combinar
    var combined = usersResponse.Users.Select(u => new UserViewModel
    {
        Id = u.Id,
        FullName = u.FullName,
        Email = u.Email,
        Roles = u.Roles,
        Enabled = u.Enabled,
        IsOnline = presenceUsers.Any(p => p.UserId == u.Id.ToString() && p.IsOnline)
    }).ToList();
    
    return combined;
}
```

### Handler de guardar cambios

```csharp
private async void OnSaveUserChanges(Guid userId, string selectedRole, bool enabled)
{
    try
    {
        ShowLoading(userId, true);
        
        // 1. Actualizar rol si cambió
        if (roleChanged)
        {
            await AdminUsersService.Instance.UpdateUserRolesAsync(userId, new[] { selectedRole });
        }
        
        // 2. Actualizar enabled si cambió
        if (enabledChanged)
        {
            await AdminUsersService.Instance.UpdateUserEnabledAsync(userId, enabled);
        }
        
        // 3. Refrescar lista
        await RefreshUsersList();
        
        ShowStatusMessage("✅ Usuario actualizado correctamente", isSuccess: true);
    }
    catch (Exception ex)
    {
        ShowStatusMessage($"❌ Error: {ex.Message}", isSuccess: false);
    }
    finally
    {
        ShowLoading(userId, false);
    }
}
```

---

## 🎨 UI Components necesarios

### UserViewModel (para binding)

```csharp
public class UserViewModel : INotifyPropertyChanged
{
    public Guid Id { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string[] Roles { get; set; }
    public bool Enabled { get; set; }
    public bool IsOnline { get; set; }
    public bool IsBusy { get; set; } // Para mostrar loading por usuario
    
    public string RolePrincipal => Roles?.FirstOrDefault() ?? "USER";
    public string StatusIcon => IsOnline ? "🟢" : "⚪";
    public string StatusText => IsOnline ? "Online" : "Offline";
}
```

### Grouping (como lo actual)

```csharp
var grouped = users.GroupBy(u => u.RolePrincipal)
                   .OrderBy(g => g.Key == "ADMIN" ? 0 : g.Key == "EDITOR" ? 1 : 2);
```

---

## 🔒 Validaciones

1. **Auto-deshabilitación**: Confirmar si el usuario intenta deshabilitarse a sí mismo
2. **Auto-degradación de rol**: Confirmar si ADMIN intenta quitarse el rol ADMIN
3. **Último ADMIN**: No permitir deshabilitar o degradar al último ADMIN del sistema

---

## 📝 Próximos pasos

1. [ ] Crear `UserViewModel` para binding
2. [ ] Modificar `CreatePermissionsContent()` para mostrar ListView con cards
3. [ ] Implementar `LoadUsersWithPresenceAsync()`
4. [ ] Añadir Flyout con ComboBox (roles) + ToggleSwitch (enabled)
5. [ ] Implementar handlers `OnSaveUserChanges` y validaciones
6. [ ] Testing completo

---

**Archivos a modificar**:
- `Views/SettingsWindow.xaml.cs` → `CreatePermissionsContent()`
- (Opcional) Crear `ViewModels/UserViewModel.cs` si se necesita MVVM completo

**NO tocar**:
- Backend
- DiarioPage
- Ventanas eliminadas (UsersOnlineWindow)
