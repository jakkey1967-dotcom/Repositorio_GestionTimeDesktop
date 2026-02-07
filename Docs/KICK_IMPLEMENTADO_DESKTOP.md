# ✅ BOTÓN KICK IMPLEMENTADO EN DESKTOP

**Fecha**: 2026-02-02  
**Componente**: SettingsWindow → Gestión de Usuarios  
**Endpoint**: `POST /api/v1/admin/presence/users/{userId}/kick`  
**Estado**: ✅ **COMPLETADO**  

---

## 📋 **IMPLEMENTACIÓN COMPLETADA**

### ✅ **1. AdminUsersService.cs**

**Añadido método `KickUserAsync`**:

```csharp
/// <summary>Echa a un usuario online revocando todas sus sesiones activas.</summary>
public async Task<bool> KickUserAsync(Guid userId, CancellationToken ct = default)
{
    try
    {
        var response = await App.Api.PostAsync<object, KickUserResponse>(
            $"/api/v1/admin/presence/users/{userId}/kick",
            null,
            ct
        );

        if (response?.Ok == true)
        {
            _log?.LogInformation("✅ Usuario echado: {sessionsRevoked} sesiones revocadas", 
                response.SessionsRevoked);
            
            // Limpiar caché de presencia
            Services.Presence.PresenceService.Instance.ClearCache();
            
            return true;
        }
        
        return false;
    }
    catch (Exception ex)
    {
        _log?.LogError(ex, "❌ Error echando usuario {userId}", userId);
        return false;
    }
}
```

---

### ✅ **2. KickUserResponse.cs (NUEVO)**

**DTO de respuesta creado**:

```csharp
public sealed class KickUserResponse
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("sessionsRevoked")]
    public int SessionsRevoked { get; set; }
}
```

---

### ✅ **3. UserViewModel.cs**

**Añadida propiedad `CanKick`**:

```csharp
/// <summary>Indica si se puede echar al usuario (solo si está online).</summary>
public bool CanKick => IsOnline;
```

---

### ✅ **4. SettingsWindow.xaml.cs**

#### **A) Botón Kick añadido al flyout**:

```csharp
// Botón Kick (solo si está online)
if (user.CanKick)
{
    var btnKick = new Button
    {
        Content = "🚪 Echar usuario",
        Background = new SolidColorBrush(ColorHelper.FromArgb(255, 220, 38, 38)), // Rojo
        Foreground = new SolidColorBrush(Colors.White),
        HorizontalAlignment = HorizontalAlignment.Stretch,
        Padding = new Thickness(12, 8, 12, 8),
        CornerRadius = new CornerRadius(6),
        FontWeight = FontWeights.SemiBold
    };
    
    btnKick.Click += async (s, e) =>
    {
        flyout.Hide();
        await KickUserAsync(user);
    };
    
    stack.Children.Add(btnKick);
}
```

#### **B) Método `KickUserAsync` implementado**:

```csharp
private async Task KickUserAsync(Models.UserViewModel user)
{
    // 1. Confirmación
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
    
    // 2. Llamar al servicio
    var success = await AdminUsersService.Instance.KickUserAsync(user.Id);
    
    if (success)
    {
        // 3. Actualizar estado local
        user.IsOnline = false;
        
        // 4. Limpiar caché y refrescar lista
        App.Api.ClearGetCache();
        await LoadUsersInlineAsync(permissionsContent);
        
        txtStatus.Text = $"✅ {user.FullName} ha sido echado correctamente";
    }
    else
    {
        txtStatus.Text = $"❌ Error al echar a {user.FullName}";
    }
}
```

---

## 🎨 **DISEÑO VISUAL**

### **Flyout CON usuario ONLINE**:

```
┌─────────────────────────────────┐
│ 👤 Francisco Santos             │
│                                  │
│ Rol: [ADMIN ▼]                  │
│                                  │
│ ✅ Usuario habilitado           │
│                                  │
│ [🚪 Echar usuario]              │ ← NUEVO (botón rojo)
│                                  │
│ [💾 Guardar cambios]            │
└─────────────────────────────────┘
```

### **Flyout CON usuario OFFLINE** (botón NO visible):

```
┌─────────────────────────────────┐
│ 👤 Wilson Sánchez                │
│                                  │
│ Rol: [USER ▼]                   │
│                                  │
│ ✅ Usuario habilitado           │
│                                  │
│ [💾 Guardar cambios]            │
└─────────────────────────────────┘
```

---

## 📊 **FLUJO COMPLETO**

```
1. Usuario ADMIN abre Settings → Permisos y roles
2. Ve lista de usuarios con indicador Online/Offline
3. Pulsa [⋯] en tarjeta de usuario ONLINE
4. Aparece flyout con botón "🚪 Echar usuario" (rojo)
5. Pulsa botón Kick
6. Aparece confirmación: "¿Seguro que quieres echar a ...?"
7. Confirma con "🚪 Echar"
8. Desktop llama POST /api/v1/admin/presence/users/{userId}/kick
9. Backend revoca todas las sesiones del usuario
10. Desktop actualiza estado local (IsOnline = false)
11. Desktop limpia caché y refresca lista
12. Usuario aparece como OFFLINE inmediatamente
13. Toast: "✅ Francisco Santos ha sido echado correctamente"
```

---

## ✅ **VERIFICACIÓN**

### **Prueba 1: Usuario ONLINE**

1. Ejecutar script de test:
```powershell
.\Scripts\Test-UserPresence.ps1
```

2. Abrir Settings → Permisos y roles
3. Verificar que Wilson Sánchez aparece ONLINE (🟢)
4. Pulsar [⋯] en su tarjeta
5. **Verificar**: Botón "🚪 Echar usuario" visible (rojo)
6. Pulsar botón Kick
7. Confirmar acción
8. **Resultado esperado**:
   - Usuario marcado como OFFLINE (⚪) inmediatamente
   - Script de test muestra error 401 (sesión cerrada)
   - Estado: "✅ Wilson Sánchez ha sido echado correctamente"

### **Prueba 2: Usuario OFFLINE**

1. Detener script de test (`Ctrl+C`)
2. Esperar 35 segundos (timeout)
3. Refrescar lista (botón 🔄)
4. Pulsar [⋯] en tarjeta de Wilson Sánchez
5. **Verificar**: Botón "Echar usuario" NO visible

### **Prueba 3: Endpoint del backend**

Verificar que el endpoint existe y funciona:

```powershell
curl https://localhost:2502/api/v1/admin/presence/users/3e90c352-f0aa-48ac-a611-b197dccaf49e/kick `
     -Method POST `
     -Headers @{"Authorization"="Bearer ADMIN_TOKEN"}
```

**Respuesta esperada**:
```json
{
  "ok": true,
  "message": "Usuario echado correctamente",
  "sessionsRevoked": 1
}
```

---

## 📝 **ARCHIVOS MODIFICADOS**

```
Services/Admin/
└── AdminUsersService.cs
    └── Añadido método KickUserAsync(userId)

Models/Dtos/
└── KickUserResponse.cs  ← NUEVO
    └── DTO de respuesta del endpoint /kick

Models/
└── UserViewModel.cs
    └── Añadida propiedad CanKick (calculada)

Views/
└── SettingsWindow.xaml.cs
    ├── Botón Kick añadido en ShowUserActionsFlyout()
    └── Método KickUserAsync() implementado
```

---

## 🔗 **ARCHIVOS RELACIONADOS**

### **Frontend (Desktop)**:
- `Services/Admin/AdminUsersService.cs` - Servicio de administración
- `Models/Dtos/KickUserResponse.cs` - DTO de respuesta (NUEVO)
- `Models/UserViewModel.cs` - ViewModel de usuario
- `Views/SettingsWindow.xaml.cs` - Vista de Settings

### **Backend (GestionTimeApi)**:
- `Controllers/AdminPresenceController.cs` - Endpoint `/kick` (ya existente ✅)

---

## 📚 **DOCUMENTACIÓN RELACIONADA**

- [BOTON_KICK_USUARIO_SETTINGS.md](BOTON_KICK_USUARIO_SETTINGS.md) - Documentación de diseño
- [GESTION_USUARIOS_INLINE_SETTINGS.md](GESTION_USUARIOS_INLINE_SETTINGS.md) - Gestión inline
- [FIX_LOGOUT_PRESENCIA_BACKEND.md](FIX_LOGOUT_PRESENCIA_BACKEND.md) - Lógica de revocación
- [SISTEMA_ROLES_USUARIOS.md](SISTEMA_ROLES_USUARIOS.md) - Sistema de roles

---

## 🎯 **RESULTADO FINAL**

| Característica | Estado |
|----------------|--------|
| **Botón Kick visible si ONLINE** | ✅ Implementado |
| **Botón oculto si OFFLINE** | ✅ Implementado |
| **Confirmación antes de echar** | ✅ Implementado |
| **Llamada al endpoint backend** | ✅ Implementado |
| **Actualización UI automática** | ✅ Implementado |
| **Toast con feedback** | ✅ Implementado |
| **Logging completo** | ✅ Implementado |
| **Compilación exitosa** | ✅ Verificado |

---

## 💡 **CARACTERÍSTICAS ADICIONALES**

### **1. Botón solo visible para usuarios ONLINE**:
```csharp
if (user.CanKick) // Solo si IsOnline == true
{
    // Mostrar botón Kick
}
```

### **2. Confirmación antes de ejecutar**:
```csharp
var dialog = new ContentDialog
{
    Title = "¿Echar usuario?",
    Content = "¿Seguro que quieres echar a ...?"
};
```

### **3. Feedback claro al usuario**:
```csharp
txtStatus.Text = "✅ Usuario echado correctamente";
// o
txtStatus.Text = "❌ Error al echar usuario";
```

### **4. Actualización automática de la UI**:
```csharp
user.IsOnline = false;  // Actualizar estado local
App.Api.ClearGetCache();  // Limpiar caché
await LoadUsersInlineAsync();  // Refrescar lista
```

---

## 🚀 **PRÓXIMOS PASOS**

1. ✅ **Implementación completada**
2. ⏳ **Testing**: Probar con usuarios reales
3. ⏳ **Validación**: Verificar comportamiento en escenarios edge
4. ⏳ **Documentación**: Actualizar manual de usuario si es necesario

---

**Autor**: GitHub Copilot  
**Ticket**: Implementar botón Kick en gestión de usuarios  
**Estado**: ✅ **COMPLETADO**  
**Prioridad**: 🟡 MEDIA (funcionalidad ADMIN)
