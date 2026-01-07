# 🎯 Implementación Banner Dinámico con Perfil de Usuario

**Objetivo:** Mostrar dinámicamente nombre completo + email + teléfono del usuario en el banner de DiarioPage.

---

## ✅ PASO 1: Session Store Global (YA HECHO)

**Archivo:** `App.xaml.cs`  
**Cambios:** Ya implementados en el commit anterior

```csharp
// 🆕 NUEVO: Session Store Global
public static UserProfileResponse? CurrentUserProfile { get; set; }
public static string? CurrentLoginEmail { get; set; }
```

---

## 📝 PASO 2: Guardar Email del Login

**Archivo:** `Views/LoginPage.xaml.cs`  
**Ubicación:** Método `OnLoginClick`, DESPUÉS del login exitoso

### **Buscar esta línea:**
```csharp
// Navega a DiarioPage después del login exitoso
App.MainWindowInstance?.Navigator?.Navigate(typeof(Views.DiarioPage));
```

### **AGREGAR JUSTO ANTES:**
```csharp
// 🆕 NUEVO: Guardar email del login en sesión global
App.CurrentLoginEmail = EmailTextBox.Text?.Trim();
App.Log?.LogInformation("📧 Email del login guardado: {email}", App.CurrentLoginEmail);
```

---

## 🔧 PASO 3: Cargar Perfil en DiarioPage

**Archivo:** `Views/DiarioPage.xaml.cs`  
**Ubicación:** Método `OnPageLoaded`, DESPUÉS de cargar la información de LocalSettings

### **Buscar este bloque:**
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
    
    ViewModel.SetUserInfo(userName, userEmail, userRole);
}
catch (Exception ex)
{
    App.Log?.LogWarning(ex, "Error cargando información del usuario");
    ViewModel.SetUserInfo("Usuario", "usuario@empresa.com", "Usuario");
}
```

### **REEMPLAZAR COMPLETAMENTE CON:**
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

## 🎨 PASO 4: Propiedades en DiarioViewModel

**Archivo:** `ViewModels/DiarioViewModel.cs`  
**Ubicación:** Junto a las propiedades existentes `UserName`, `UserEmail`, `UserRole`

### **AGREGAR ESTAS 3 PROPIEDADES:**
```csharp
// 🆕 NUEVO: Propiedades para mostrar en el banner (dinámicas desde perfil)
[ObservableProperty]
private string displayName = "Usuario";

[ObservableProperty]
private string displayEmail = "usuario@empresa.com";

[ObservableProperty]
private string displayPhone = "";
```

---

## 🖼️ PASO 5: Actualizar UI en DiarioPage.xaml

**Archivo:** `Views/DiarioPage.xaml`  
**Ubicación:** Sección del banner (donde dice "Francisco • Admin")

### **Buscar este bloque:**
```xml
<!-- Usuario: Nombre + Rol + Email -->
<StackPanel Orientation="Horizontal" Spacing="8">
    <FontIcon Glyph="&#xE77B;" FontSize="14" Foreground="White" Opacity="0.9"/>
    <StackPanel Spacing="2">
        <!-- Línea 1: Nombre • Rol -->
        <StackPanel Orientation="Horizontal" Spacing="8">
            <TextBlock Text="{x:Bind ViewModel.UserName}" 
                       FontSize="14" FontWeight="SemiBold" Foreground="White"/>
            <TextBlock Text="•" 
                       FontSize="14" Foreground="White" Opacity="0.6"/>
            <TextBlock Text="{x:Bind ViewModel.UserRole}" 
                       FontSize="14" FontWeight="SemiBold" Foreground="White" Opacity="0.9"/>
        </StackPanel>
        <!-- Línea 2: Email -->
        <TextBlock Text="{x:Bind ViewModel.UserEmail}" 
                   FontSize="12" Foreground="White" Opacity="0.8"/>
    </StackPanel>
</StackPanel>
```

### **REEMPLAZAR COMPLETAMENTE CON:**
```xml
<!-- 🆕 NUEVO: Usuario dinámico con nombre completo + email + teléfono -->
<StackPanel Orientation="Horizontal" Spacing="8">
    <FontIcon Glyph="&#xE77B;" FontSize="14" Foreground="White" Opacity="0.9"/>
    <StackPanel Spacing="2">
        <!-- Línea 1: Nombre completo (negrita) -->
        <TextBlock Text="{x:Bind ViewModel.DisplayName, Mode=OneWay}" 
                   FontSize="14" 
                   FontWeight="SemiBold" 
                   Foreground="White"/>
        
        <!-- Línea 2: Email -->
        <TextBlock Text="{x:Bind ViewModel.DisplayEmail, Mode=OneWay}" 
                   FontSize="12" 
                   Foreground="White" 
                   Opacity="0.8"/>
        
        <!-- Línea 3: Teléfono (ocultar si está vacío) -->
        <TextBlock Text="{x:Bind ViewModel.DisplayPhone, Mode=OneWay}" 
                   FontSize="12" 
                   Foreground="White" 
                   Opacity="0.7"
                   Visibility="{x:Bind ViewModel.DisplayPhone, Mode=OneWay, Converter={StaticResource StringNotEmptyToVisibilityConverter}}"/>
    </StackPanel>
</StackPanel>
```

### **⚠️ NOTA SOBRE EL CONVERTER:**

Si el converter `StringNotEmptyToVisibilityConverter` NO EXISTE, usa este enfoque alternativo más simple:

```xml
<!-- Línea 3: Teléfono (ocultar si está vacío) - VERSIÓN SIN CONVERTER -->
<TextBlock FontSize="12" 
           Foreground="White" 
           Opacity="0.7">
    <Run Text="📞"/>
    <Run Text="{x:Bind ViewModel.DisplayPhone, Mode=OneWay}"/>
</TextBlock>
```

O simplemente OMITE el atributo `Visibility` y deja que muestre vacío (menos elegante pero funcional).

---

## ✅ RESULTADO ESPERADO

### **Antes:**
```
[Logo] Gestor de Tareas
       👤 Francisco • Admin
          msn@tdkportal.com
```

### **Después (con perfil cargado):**
```
[Logo] Gestor de Tareas
       👤 Francisco Santos García
          msn@tdkportal.com
          📞 +34 666 123 456
```

### **Después (sin teléfono):**
```
[Logo] Gestor de Tareas
       👤 Francisco Santos García
          msn@tdkportal.com
```

### **Después (sin perfil - fallback):**
```
[Logo] Gestor de Tareas
       👤 msn
          msn@tdkportal.com
```

---

## 📊 LOGS ESPERADOS

```log
[INFO] OnPageLoaded() - Iniciando carga de DiarioPage
[INFO] 📥 Cargando perfil del usuario desde API...
[INFO] ✅ Perfil cargado: Francisco Santos | +34 666 123 456
[INFO] 🎨 Banner actualizado: Francisco Santos García | msn@tdkportal.com | +34 666 123 456
```

O si no hay perfil:

```log
[INFO] OnPageLoaded() - Iniciando carga de DiarioPage
[INFO] 📥 Cargando perfil del usuario desde API...
[WARN] ⚠️ Perfil no encontrado en backend, usando datos del login
[INFO] 🎨 Banner actualizado: msn | msn@tdkportal.com | (sin teléfono)
```

---

## 🧪 TESTING

1. ✅ **Compilar** el proyecto
2. ✅ **Ejecutar** la aplicación
3. ✅ **Hacer login** con credenciales válidas
4. ✅ **Verificar banner** muestra:
   - Nombre completo (si hay perfil)
   - Email del login
   - Teléfono (si existe en perfil)
5. ✅ **Revisar logs** en `app.log`

---

## 🔧 TROUBLESHOOTING

### **Problema: "No compila por `Mode=OneWay`"**
**Solución:** WinUI 3 requiere `Mode=OneWay` en `x:Bind` para propiedades que cambian dinámicamente.

### **Problema: "El teléfono siempre se muestra vacío"**
**Solución:** Verificar que `DisplayPhone` tenga valor en logs. Si siempre es vacío, el perfil no está cargándose correctamente.

### **Problema: "Sale 'Usuario' en lugar del nombre"**
**Solución:** 
1. Verificar que `App.CurrentLoginEmail` se esté guardando en LoginPage
2. Verificar que `App.ProfileService.GetCurrentUserProfileAsync()` NO esté devolviendo null
3. Revisar logs para ver si hay errores

---

## ✅ CHECKLIST FINAL

- [ ] **App.xaml.cs:** Propiedades `CurrentUserProfile` y `CurrentLoginEmail` agregadas
- [ ] **LoginPage.xaml.cs:** Email guardado en `App.CurrentLoginEmail` después del login
- [ ] **DiarioPage.xaml.cs:** Perfil cargado en `OnPageLoaded` y propiedades actualizadas
- [ ] **DiarioViewModel.cs:** Propiedades `DisplayName`, `DisplayEmail`, `DisplayPhone` agregadas
- [ ] **DiarioPage.xaml:** UI actualizada con bindings `{x:Bind ViewModel.DisplayX, Mode=OneWay}`
- [ ] **Compilación:** Sin errores
- [ ] **Testing:** Banner muestra datos dinámicos correctamente

---

**Autor:** GitHub Copilot  
**Fecha:** 2025-01-28  
**Versión:** 1.0  
**Estado:** ✅ Listo para implementar  
