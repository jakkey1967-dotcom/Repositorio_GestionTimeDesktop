# 🎨 Banner Mejorado con Estado del Servicio - DiarioPage

**Fecha:** 2025-01-XX  
**Estado:** ✅ Implementado y Compilado  
**Objetivo:** Mejorar el banner superior con información de usuario, rol y estado del servicio

---

## 📋 **Características Implementadas**

### 1️⃣ **Banner con Diseño de 3 Columnas**

```
┌─────────────────────────────────────────────────────────────┐
│ LOGO  │  Gestor de Tareas               │  [⚙️ Tema]       │
│       │  👤 Francisco • Admin           │  🟢 Servicio:    │
│       │     francisco@empresa.com       │     Online       │
└─────────────────────────────────────────────────────────────┘
```

**Distribución:**
- **Columna 1 (Izquierda):** Logo de la aplicación
- **Columna 2 (Centro):** Título + Usuario (nombre + rol) + Email
- **Columna 3 (Derecha):** Botón de tema + Estado del servicio

---

### 2️⃣ **Información del Usuario con Rol**

**Ubicación:** Centro del banner, junto al logo

**Elementos:**
- **Título Principal:** "Gestor de Tareas" (22px, SemiBold, blanco)
- **Icono Usuario:** 👤 (FontIcon E77B)
- **Línea de Usuario:** "Francisco • Admin" (14px, SemiBold, blanco)
  - Nombre del usuario
  - Separador "•"
  - Rol del usuario
- **Correo:** "francisco@empresa.com" (12px, 80% opacity, blanco)

**Datos obtenidos del login:**
- `UserName`: Nombre del usuario (desde LoginResponse o email si no está disponible)
- `UserRole`: Rol del usuario (Admin, Usuario, etc.)
- `UserEmail`: Email del usuario

---

### 3️⃣ **Estado del Servicio (Reubicado a la Derecha)**

**Ubicación:** Columna derecha del banner, debajo del botón de tema

**Componentes:**
- **LED Circular:** 12px de diámetro
  - Verde (#10B981) cuando Online
  - Rojo (#EF4444) cuando Offline
  - Efecto glow sutil (círculo exterior difuminado 16px con opacity 0.4)
  
- **Texto del Estado:**
  - "Servicio: Online" (12px, blanco)
  - "Servicio: Offline" (12px, blanco)
  
- **Icono de Advertencia:** ⚠️
  - Solo visible cuando está Offline
  - Color amarillo (#FBBF24)
  - Tamaño 11px
  - Tooltip: "Sin conexión con el servidor"

---

## 🎨 **Paleta de Colores**

| Elemento | Estado | Color | Hex |
|----------|--------|-------|-----|
| LED | Online | Verde | #10B981 |
| LED | Offline | Rojo | #EF4444 |
| LED | Desconocido | Gris | #9CA3AF |
| Advertencia | - | Amarillo | #FBBF24 |
| Banner BG | - | Turquesa | #0B8C99 |
| Separador Usuario • | - | Blanco | #FFFFFF (60% opacity) |

---

## 💻 **Archivos Modificados/Creados**

### 1. **ViewModels/DiarioViewModel.cs** (MODIFICADO)

**Propiedades Agregadas:**

```csharp
[ObservableProperty]
private string title = "Gestor de Tareas";

[ObservableProperty]
private string userName = "Usuario";

[ObservableProperty]
private string userEmail = "usuario@empresa.com";

[ObservableProperty]
private string userRole = "Usuario";  // 🆕 NUEVO

[ObservableProperty]
private bool isServiceOnline = false;

[ObservableProperty]
private bool isCheckingService = false;

[ObservableProperty]
private string lastCheckTime = "--:--";
```

**Métodos Modificados:**

```csharp
/// <summary>
/// Actualiza la información del usuario logueado
/// </summary>
public void SetUserInfo(string name, string email, string role)  // 🆕 Agregado parámetro role
{
    UserName = name;
    UserEmail = email;
    UserRole = role;  // 🆕 NUEVO
    App.Log?.LogInformation("👤 Usuario actualizado: {name} ({email}) - Rol: {role}", name, email, role);
}
```

---

### 2. **Services/ApiClient.cs** (MODIFICADO)

**LoginResponse Actualizado:**
```csharp
public sealed class LoginResponse
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? Message { get; set; }
    public string? UserName { get; set; }    // 🆕 NUEVO
    public string? UserEmail { get; set; }   // 🆕 NUEVO
    public string? UserRole { get; set; }    // 🆕 NUEVO
}
```

**Endpoint de Health Check:**
```csharp
/// <summary>
/// Verifica si el servidor está disponible (ping)
/// </summary>
public async Task<bool> PingAsync(CancellationToken ct = default)
{
    try
    {
        var pingPath = "/api/v1/health";  // 🆕 Actualizado endpoint
        
        var sw = Stopwatch.StartNew();
        using var resp = await _http.GetAsync(pingPath, ct);
        sw.Stop();

        var isOnline = resp.IsSuccessStatusCode;
        
        if (isOnline)
        {
            _log.LogDebug("PING {url} -> OK ({ms}ms)", pingPath, sw.ElapsedMilliseconds);
        }
        else
        {
            _log.LogDebug("PING {url} -> {code} ({ms}ms)", pingPath, (int)resp.StatusCode, sw.ElapsedMilliseconds);
        }

        return isOnline;
    }
    catch (HttpRequestException ex)
    {
        _log.LogDebug("PING failed: {msg}", ex.Message);
        return false;
    }
    catch (TaskCanceledException)
    {
        _log.LogDebug("PING timeout");
        return false;
    }
    catch (Exception ex)
    {
        _log.LogDebug(ex, "PING exception");
        return false;
    }
}
```

---

### 3. **Views/LoginPage.xaml.cs** (MODIFICADO)

**Guardado de Información del Usuario:**
```csharp
// Después del login exitoso...

// Guardar información del usuario
try
{
    var settings = ApplicationData.Current.LocalSettings.Values;
    settings["UserName"] = res.UserName ?? email; // Usar email si no viene UserName
    settings["UserEmail"] = res.UserEmail ?? email;
    settings["UserRole"] = res.UserRole ?? "Usuario";
    
    App.Log?.LogInformation("Información de usuario guardada: {name} - {role}", 
        res.UserName ?? email, res.UserRole ?? "Usuario");
}
catch (Exception ex)
{
    App.Log?.LogWarning(ex, "Error guardando información de usuario");
}
```

**Datos guardados en LocalSettings:**
- `UserName`: Nombre del usuario (o email si no está disponible)
- `UserEmail`: Email del usuario
- `UserRole`: Rol del usuario (por defecto "Usuario")

---

### 4. **Views/DiarioPage.xaml.cs** (MODIFICADO)

**Carga de Información del Usuario:**
```csharp
private async void OnPageLoaded(object sender, RoutedEventArgs e)
{
    App.Log?.LogInformation("DiarioPage Loaded ✅");
    
    // Inicializar tema y assets
    UpdateThemeAssets(this.RequestedTheme);
    
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
    
    // ...existing code...
}
```

---

### 5. **Views/DiarioPage.xaml** (MODIFICADO)

**Estructura del Banner (3 Columnas):**
```xaml
<Border Grid.Row="0" Background="{ThemeResource BannerBg}" CornerRadius="10" Padding="16">
    <Grid ColumnSpacing="16">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto"/>    <!-- Logo -->
            <ColumnDefinition Width="*"/>       <!-- Usuario -->
            <ColumnDefinition Width="Auto"/>    <!-- Tema + Servicio -->
        </Grid.ColumnDefinitions>
        
        <!-- COLUMNA 1: Logo -->
        <Border Grid.Column="0" VerticalAlignment="Center">
            <Image x:Name="LogoImageBanner" MaxHeight="60"/>
        </Border>
        
        <!-- COLUMNA 2: Título + Usuario -->
        <StackPanel Grid.Column="1" VerticalAlignment="Center" Spacing="4">
            <!-- Título -->
            <TextBlock Text="{x:Bind ViewModel.Title}" FontSize="22" FontWeight="SemiBold" Foreground="White"/>
            
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
        
        <!-- COLUMNA 3: Botón Tema + Estado Servicio -->
        <StackPanel Grid.Column="2" VerticalAlignment="Center" Spacing="12">
            <!-- Botón Tema -->
            <Button x:Name="BtnTheme" Background="Transparent" 
                    BorderBrush="White" BorderThickness="1" CornerRadius="8" 
                    Padding="10,8" HorizontalAlignment="Center">
                <FontIcon Glyph="&#xE700;" Foreground="White"/>
                <Button.Flyout>
                    <MenuFlyout>
                        <ToggleMenuFlyoutItem x:Name="ThemeSystemItem" Text="Tema del sistema" Click="OnThemeSystem"/>
                        <ToggleMenuFlyoutItem x:Name="ThemeLightItem" Text="Tema claro" Click="OnThemeLight"/>
                        <ToggleMenuFlyoutItem x:Name="ThemeDarkItem" Text="Tema oscuro" Click="OnThemeDark"/>
                    </MenuFlyout>
                </Button.Flyout>
            </Button>
            
            <!-- Estado del Servicio -->
            <Grid HorizontalAlignment="Center" ColumnSpacing="8">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>
                
                <!-- LED con Glow -->
                <Grid Grid.Column="0" Width="12" Height="12">
                    <Ellipse Fill="{...Converter}" Opacity="0.4" Width="16" Height="16"/>
                    <Ellipse Fill="{...Converter}" Width="12" Height="12"/>
                </Grid>
                
                <!-- Texto -->
                <StackPanel Grid.Column="1" Orientation="Horizontal" Spacing="6">
                    <TextBlock Text="Servicio:" FontSize="12" Foreground="White" Opacity="0.8"/>
                    <TextBlock Text="{...Converter}" FontSize="12" FontWeight="SemiBold" Foreground="White"/>
                    <FontIcon Glyph="&#xE7BA;" FontSize="11" Foreground="#FBBF24" 
                              Visibility="{...Converter}"/>
                </StackPanel>
            </Grid>
        </StackPanel>
    </Grid>
</Border>
```

---

## 📊 **Tipografía y Jerarquía Visual**

| Elemento | Tamaño | Peso | Opacidad | Color |
|----------|--------|------|----------|-------|
| Título ("Gestor de Tareas") | 22px | SemiBold | 100% | Blanco |
| Nombre Usuario | 14px | SemiBold | 100% | Blanco |
| Separador "•" | 14px | Normal | 60% | Blanco |
| Rol Usuario | 14px | SemiBold | 90% | Blanco |
| Correo Usuario | 12px | Normal | 80% | Blanco |
| "Servicio:" | 12px | Normal | 80% | Blanco |
| Estado ("Online/Offline") | 12px | SemiBold | 100% | Blanco |
| Icono Usuario | 14px | - | 90% | Blanco |
| Icono Advertencia | 11px | - | 100% | Amarillo |

---

## 🔄 **Flujo de Datos del Usuario**

```
Login (LoginPage)
    ↓
LoginAsync() → LoginResponse
    ↓
Extraer: UserName, UserEmail, UserRole
    ↓
Guardar en LocalSettings:
    - UserName
    - UserEmail  
    - UserRole
    ↓
Navegar a DiarioPage
    ↓
OnPageLoaded()
    ↓
Leer LocalSettings
    ↓
ViewModel.SetUserInfo(name, email, role)
    ↓
Actualizar UI (Binding)
    ↓
Banner muestra: "Francisco • Admin"
                  "francisco@empresa.com"
```

---

## 🔧 **Configuración del Backend**

### Endpoint de Login

El backend debe devolver en la respuesta JSON:

```json
{
  "accessToken": "eyJhbGc...",
  "userName": "Francisco",
  "userEmail": "francisco@empresa.com",
  "userRole": "Admin",
  "message": "ok"
}
```

**Campos opcionales:**
- Si `userName` no viene, se usa el email del login
- Si `userRole` no viene, se usa "Usuario" por defecto

### Endpoint de Health Check

```
GET /api/v1/health
```

Debe devolver status 200 si el servidor está disponible.

**Ejemplo de implementación (backend):**

```csharp
app.MapGet("/api/v1/health", () => Results.Ok(new { status = "healthy" }));
```

---

## ✅ **Checklist de Implementación**

- [x] Agregar propiedad `UserRole` en DiarioViewModel
- [x] Actualizar método `SetUserInfo` con parámetro `role`
- [x] Agregar propiedades `UserName`, `UserEmail`, `UserRole` en LoginResponse
- [x] Guardar datos del usuario en LocalSettings después del login
- [x] Cargar datos del usuario desde LocalSettings en DiarioPage
- [x] Modificar XAML del banner a diseño de 3 columnas
- [x] Mostrar rol del usuario con separador "•"
- [x] Reubicar estado del servicio a columna derecha (debajo del botón de tema)
- [x] Actualizar endpoint de ping a `/api/v1/health`
- [x] Ajustar tamaños de fuente (Servicio: 12px en vez de 13px)
- [x] Compilar sin errores
- [x] Actualizar documentación

---

## 🧪 **Testing**

### Test 1: Login y Datos de Usuario
1. ✅ Hacer login con credenciales válidas
2. ✅ Verificar que LoginResponse incluya `userName`, `userEmail`, `userRole`
3. ✅ Verificar en logs: "Información de usuario guardada"
4. ✅ Navegar a DiarioPage
5. ✅ Verificar banner muestra: "Francisco • Admin"
6. ✅ Verificar email se muestra debajo

### Test 2: Fallback de Datos
1. ✅ Login con respuesta que NO incluye `userName`
2. ✅ Verificar que se usa el email como nombre
3. ✅ Verificar que rol por defecto es "Usuario"

### Test 3: Posición de Elementos
1. ✅ Logo a la izquierda
2. ✅ Usuario en el centro
3. ✅ Botón de tema arriba-derecha
4. ✅ Estado del servicio abajo-derecha (debajo del botón)

### Test 4: Estado del Servicio
1. ✅ LED verde cuando servidor online
2. ✅ LED rojo + icono ⚠️ cuando servidor offline
3. ✅ Texto "Servicio: Online/Offline" actualizado

---

## 📝 **Logs Generados**

### Login:
```
[INFO] LoginAsync iniciado para francisco@empresa.com
[INFO] Información de usuario guardada: Francisco - Admin
[INFO] Navegación a DiarioPage completada ✅
```

### Carga de DiarioPage:
```
[INFO] DiarioPage Loaded ✅
[INFO] 👤 Usuario actualizado: Francisco (francisco@empresa.com) - Rol: Admin
[INFO] 🔄 Monitoreo de servicio iniciado (cada 10 segundos)
```

### Health Check:
```
[DEBUG] PING /api/v1/health -> OK (45ms)
[DEBUG] 🌐 Estado del servicio: ONLINE ✅ a las 14:32:15
```

---

## 🚀 **Mejoras Futuras (Opcional)**

1. **Foto de perfil** del usuario (avatar circular)
2. **Menú desplegable** al hacer clic en el nombre del usuario
3. **Diferentes iconos** según el rol (👤 Usuario, 👑 Admin, ⚙️ Técnico)
4. **Tooltip** con información adicional al pasar el mouse sobre el nombre
5. **Indicador de permisos** según el rol
6. **Badge de notificaciones** junto al nombre del usuario

---

## 📚 **Archivos Relacionados**

- `ViewModels/DiarioViewModel.cs` - ViewModel con UserRole
- `Services/ApiClient.cs` - LoginResponse actualizado + PingAsync
- `Views/LoginPage.xaml.cs` - Guardado de datos de usuario
- `Views/DiarioPage.xaml` - UI del banner (3 columnas)
- `Views/DiarioPage.xaml.cs` - Carga de datos de usuario
- `Helpers/ServiceStatusConverter.cs` - Converters del estado del servicio

---

**✅ Estado Final:** Banner completo con información de usuario (nombre + rol + email) y estado del servicio reubicado a la derecha.

**🎉 Implementación completada y compilada sin errores!**
