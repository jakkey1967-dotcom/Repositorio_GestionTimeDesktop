# 🎯 PASO 3: Cargar Perfil Dinámicamente en DiarioPage

## 📍 **Ubicación del Código a Reemplazar**

**Archivo:** `Views/DiarioPage.xaml.cs`  
**Método:** `OnPageLoaded`  
**Líneas:** ~Buscar el bloque de "Cargar información del usuario desde LocalSettings"

---

## ✂️ **CÓDIGO A ELIMINAR:**

```csharp
// Cargar información del usuario desde LocalSettings
try
{
    var settings = Windows.Storage.ApplicationData.Current.LocalSettings.Values;

    var userName = settings.TryGetValue("UserName", out var nameObj) && nameObj is string name
        ? name
        : "Usuario";

    var userEmail = settings.TryGetValue("UserEmail", out var emailObj) && emailObj is string email
        ? email
        : "usuario@empresa.com";

    var userRole = settings.TryGetValue("UserRole", out var roleObj) && roleObj is string role
        ? role
        : "Usuario";

    App.Log?.LogInformation("📋 Cargando información de usuario desde LocalSettings:");
    App.Log?.LogInformation("   • UserName: {name} (default: {isDefault})", userName, nameObj == null);
    App.Log?.LogInformation("   • UserEmail: {email} (default: {isDefault})", userEmail, emailObj == null);
    App.Log?.LogInformation("   • UserRole: {role} (default: {isDefault})", userRole, roleObj == null);

    ViewModel.SetUserInfo(userName, userEmail, userRole);
}
catch (Exception ex)
{
    App.Log?.LogWarning(ex, "Error cargando información del usuario");
    ViewModel.SetUserInfo("Usuario", "usuario@empresa.com", "Usuario");
}
```

---

## ➕ **CÓDIGO NUEVO A INSERTAR:**

```csharp
// 🆕 NUEVO: Cargar perfil dinámicamente desde API (solo si no está cacheado)
try
{
    // Intentar cargar perfil desde cache global primero
    if (App.CurrentUserProfile == null)
    {
        App.Log?.LogInformation("📥 Cargando perfil del usuario desde API...");
        
        try
        {
            App.CurrentUserProfile = await App.ProfileService.GetCurrentUserProfileAsync();
            
            if (App.CurrentUserProfile != null)
            {
                App.Log?.LogInformation("✅ Perfil cargado: {firstName} {lastName} | {phone}", 
                    App.CurrentUserProfile.FirstName, 
                    App.CurrentUserProfile.LastName,
                    App.CurrentUserProfile.Phone);
            }
            else
            {
                App.Log?.LogWarning("⚠️ Perfil no encontrado en backend, usando datos del login");
            }
        }
        catch (Exception profileEx)
        {
            App.Log?.LogWarning(profileEx, "⚠️ Error cargando perfil, usando fallback");
        }
    }
    
    // Construir información para mostrar en el banner
    string displayName;
    string displayEmail;
    string displayPhone;
    
    if (App.CurrentUserProfile != null)
    {
        // 📊 Usar datos del perfil completo
        displayName = $"{App.CurrentUserProfile.FirstName} {App.CurrentUserProfile.LastName}".Trim();
        displayEmail = App.CurrentLoginEmail ?? App.CurrentUserProfile.FullName ?? "usuario@empresa.com";
        displayPhone = App.CurrentUserProfile.Phone ?? "";
        
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = displayEmail.Split('@')[0]; // Fallback: parte local del email
        }
    }
    else
    {
        // 📧 Fallback: Usar email del login
        var settings = Windows.Storage.ApplicationData.Current.LocalSettings.Values;
        
        var userName = settings.TryGetValue("UserName", out var nameObj) && nameObj is string name 
            ? name 
            : "Usuario";
            
        displayName = userName;
        displayEmail = App.CurrentLoginEmail ?? "usuario@empresa.com";
        displayPhone = ""; // Sin perfil, no hay teléfono
    }
    
    // Actualizar ViewModel con los datos dinámicos
    ViewModel.DisplayName = displayName;
    ViewModel.DisplayEmail = displayEmail;
    ViewModel.DisplayPhone = displayPhone;
    
    App.Log?.LogInformation("🎨 Banner actualizado: {name} | {email} | {phone}", 
        displayName, displayEmail, 
        string.IsNullOrEmpty(displayPhone) ? "(sin teléfono)" : displayPhone);
}
catch (Exception ex)
{
    App.Log?.LogWarning(ex, "Error cargando perfil del usuario");
    
    // Fallback seguro
    ViewModel.DisplayName = "Usuario";
    ViewModel.DisplayEmail = App.CurrentLoginEmail ?? "usuario@empresa.com";
    ViewModel.DisplayPhone = "";
}
```

---

## 📝 **INSTRUCCIONES:**

1. Abrir `Views/DiarioPage.xaml.cs`
2. Buscar el método `OnPageLoaded`
3. Localizar el bloque "Cargar información del usuario desde LocalSettings"
4. **REEMPLAZAR COMPLETAMENTE** ese bloque con el código nuevo
5. Guardar y compilar

---

**Nota:** Este código carga dinámicamente el perfil del usuario desde la API y actualiza 3 nuevas propiedades del ViewModel: `DisplayName`, `DisplayEmail`, `DisplayPhone`.
