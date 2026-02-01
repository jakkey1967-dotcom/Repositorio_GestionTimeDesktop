# 🌐 Sistema de Usuarios Online y Modificaciones de API

## 📋 Resumen Ejecutivo

Este documento detalla el **sistema completo de usuarios online** implementado en GestionTime Desktop, incluyendo las modificaciones realizadas en la API del backend y la integración en el frontend.

---

## 🎯 Objetivos del Sistema

1. ✅ **Monitoreo en tiempo real**: Ver qué usuarios están conectados/activos
2. ✅ **Gestión de roles**: Cambiar roles de usuarios (ADMIN, EDITOR, USER)
3. ✅ **Sistema de presencia**: Tracking automático de última actividad
4. ✅ **Ventana flotante**: Interfaz dedicada para visualización de usuarios

---

## 🏗️ Arquitectura General

```
┌─────────────────────────────────────────────────────────────────┐
│                         FRONTEND                                │
│                    (GestionTime Desktop)                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌──────────────────┐         ┌───────────────────┐           │
│  │  DiarioPage      │────────▶│ UsersOnlineWindow │           │
│  │  (Botón Abrir)   │         │  (Ventana Modal)  │           │
│  └──────────────────┘         └─────────┬─────────┘           │
│                                          │                      │
│                                          ▼                      │
│                              ┌──────────────────────┐          │
│                              │ UsersOnlineViewModel │          │
│                              │ (Lógica de negocio)  │          │
│                              └──────────┬───────────┘          │
│                                          │                      │
│                                          ▼                      │
│                    ┌─────────────────────────────────┐         │
│                    │      PresenceService             │         │
│                    │  (Polling cada 15s + Caché)     │         │
│                    └────────────┬────────────────────┘         │
│                                 │                               │
└─────────────────────────────────┼───────────────────────────────┘
                                  │
                                  │ HTTPS
                                  │
┌─────────────────────────────────▼───────────────────────────────┐
│                         BACKEND API                             │
│                 (GestionTimeApi - Render)                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  GET /api/v1/admin/users                                       │
│  └─▶ Lista de usuarios con last_seen_at                       │
│                                                                 │
│  GET /api/v1/admin/ping                                        │
│  └─▶ Actualiza last_seen_at del usuario actual                │
│                                                                 │
│  PUT /api/v1/admin/users/{userId}/roles                        │
│  └─▶ Cambia el rol de un usuario                              │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📡 Endpoints de la API

### 1. **GET /api/v1/admin/users**

**Descripción:** Obtiene la lista completa de usuarios del sistema con información de presencia.

**Headers:**
```http
Authorization: Bearer <token_jwt>
```

**Respuesta Exitosa (200 OK):**
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "email": "pedro.santos@empresa.com",
    "fullName": "Pedro Santos",
    "enabled": true,
    "roles": ["ADMIN"],
    "lastSeenAt": "2024-01-15T10:30:00Z"
  },
  {
    "id": "660e8400-e29b-41d4-a716-446655440001",
    "email": "maria.lopez@empresa.com",
    "fullName": "María López",
    "enabled": true,
    "roles": ["EDITOR"],
    "lastSeenAt": "2024-01-15T10:28:45Z"
  }
]
```

**Campos Importantes:**
- `id` (Guid): Identificador único del usuario
- `lastSeenAt` (DateTime?): Última vez que el usuario estuvo activo (UTC)
- `roles` (string[]): Lista de roles del usuario
- `enabled` (bool): Usuario activo/inactivo

**Lógica de "Online":**
```csharp
// Un usuario está ONLINE si:
// 1. enabled = true
// 2. lastSeenAt != null
// 3. lastSeenAt >= DateTime.UtcNow.AddMinutes(-2)
```

---

### 2. **GET /api/v1/admin/ping**

**Descripción:** Actualiza el campo `last_seen_at` del usuario actual en la base de datos.

**Headers:**
```http
Authorization: Bearer <token_jwt>
```

**Respuesta Exitosa (200 OK):**
```json
{
  "message": "Ping recibido",
  "timestamp": "2024-01-15T10:30:15Z"
}
```

**Uso:**
- El frontend envía un ping cada 60 segundos
- Mantiene actualizado el estado de presencia del usuario

---

### 3. **PUT /api/v1/admin/users/{userId}/roles**

**Descripción:** Cambia el rol de un usuario específico.

**Headers:**
```http
Authorization: Bearer <token_jwt>
Content-Type: application/json
```

**Body:**
```json
{
  "role": "EDITOR"
}
```

**Roles Válidos:**
- `ADMIN`: Acceso completo (gestión de usuarios, configuración)
- `EDITOR`: Puede crear/editar partes
- `USER`: Solo lectura

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "message": "Rol actualizado correctamente",
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "newRole": "EDITOR"
}
```

**Errores Comunes:**
- `401 Unauthorized`: Token inválido o expirado
- `403 Forbidden`: Usuario sin permisos de ADMIN
- `404 Not Found`: Usuario no existe
- `400 Bad Request`: Rol inválido

---

## 🖥️ Implementación en Desktop

### **Archivo 1: Services/Presence/PresenceService.cs**

**Responsabilidades:**
- ✅ Obtiene lista de usuarios desde la API
- ✅ Implementa caché de 15 segundos
- ✅ Envía ping cada 60 segundos
- ✅ Maneja errores de conexión

**Código Clave:**
```csharp
public sealed class PresenceService
{
    private static PresenceService? _instance;
    public static PresenceService Instance => _instance ??= new PresenceService();

    private List<UserListItemDto> _cachedUsers = new();
    private DateTime _lastFetch = DateTime.MinValue;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(15);

    public async Task<List<UserListItemDto>> GetUsersAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var elapsed = now - _lastFetch;

        // Retornar caché si es válido
        if (_cachedUsers.Any() && elapsed < _cacheDuration)
        {
            return _cachedUsers;
        }

        // Petición a la API
        var response = await App.Api.GetAsync<List<UserListItemDto>>(
            "/api/v1/admin/users", ct);

        if (response != null && response.Any())
        {
            _cachedUsers = response;
            _lastFetch = now;
        }

        return _cachedUsers;
    }

    public async Task<bool> PingAsync(CancellationToken ct = default)
    {
        try
        {
            await App.Api.GetAsync<object>("/api/v1/admin/ping", ct);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void ClearCache()
    {
        _cachedUsers.Clear();
        _lastFetch = DateTime.MinValue;
    }
}
```

---

### **Archivo 2: ViewModels/UsersOnlineViewModel.cs**

**Responsabilidades:**
- ✅ Transforma datos de la API a objetos de UI
- ✅ Gestiona timer de refresco (15 segundos)
- ✅ Ordena usuarios por estado (online primero) y rol
- ✅ Notifica cambios a la UI (INotifyPropertyChanged)

**Propiedades Principales:**
```csharp
public ObservableCollection<UserCardItem> Users { get; } = new();
public int OnlineCount { get; private set; }
public int OfflineCount { get; private set; }
public bool IsLoading { get; private set; }
public string? ErrorMessage { get; private set; }
```

**Lógica de Ordenamiento:**
```csharp
private List<UserCardItem> SortUsers(List<UserCardItem> users)
{
    return users
        .OrderByDescending(u => u.IsOnline)        // Online primero
        .ThenBy(u => u.RolePriority)               // Luego por rol (ADMIN, EDITOR, USER)
        .ThenBy(u => u.FullName)                   // Finalmente por nombre
        .ToList();
}
```

---

### **Archivo 3: Views/UsersOnlineWindow.xaml**

**Estructura:**
```xaml
<Window Title="Usuarios Online" Width="400" Height="600">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>  <!-- Header -->
            <RowDefinition Height="*"/>     <!-- Contenido -->
        </Grid.RowDefinitions>

        <!-- Header con título y subtítulo -->
        <Border Grid.Row="0" Background="#0FA7B6" Padding="20,16">
            <StackPanel>
                <TextBlock Text="Usuarios Online" FontSize="20"/>
                <TextBlock x:Name="TxtSubtitle" Text="X online, Y offline"/>
            </StackPanel>
        </Border>

        <!-- Contenido: Loading / Error / Lista -->
        <Grid Grid.Row="1" Padding="12">
            <!-- Loading Panel -->
            <StackPanel x:Name="LoadingPanel" Visibility="Collapsed">
                <ProgressRing IsActive="True"/>
            </StackPanel>

            <!-- Error Panel -->
            <StackPanel x:Name="ErrorPanel" Visibility="Collapsed">
                <TextBlock x:Name="TxtError" Text="Error..."/>
            </StackPanel>

            <!-- Lista de Usuarios -->
            <ScrollViewer x:Name="UsersScrollViewer">
                <ItemsControl x:Name="UsersListView"/>
            </ScrollViewer>
        </Grid>
    </Grid>
</Window>
```

**Tarjeta de Usuario (DataTemplate):**
```xaml
<Border Background="#F5F5F5" CornerRadius="8" Padding="12" Margin="0,0,0,8">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto"/>  <!-- Avatar -->
            <ColumnDefinition Width="*"/>     <!-- Info -->
            <ColumnDefinition Width="Auto"/>  <!-- Status -->
        </Grid.ColumnDefinitions>

        <!-- Avatar con iniciales -->
        <Border Grid.Column="0" Width="48" Height="48" 
                Background="#0FA7B6" CornerRadius="24">
            <TextBlock Text="{x:Bind Initials}" 
                       Foreground="White" FontSize="20"/>
        </Border>

        <!-- Información del usuario -->
        <StackPanel Grid.Column="1" Margin="12,0,0,0">
            <TextBlock Text="{x:Bind FullName}" FontWeight="SemiBold"/>
            <TextBlock Text="{x:Bind Email}" Foreground="#666" FontSize="12"/>
            <TextBlock Text="{x:Bind RoleBadge}" FontSize="11" Foreground="#0FA7B6"/>
        </StackPanel>

        <!-- Indicador Online/Offline -->
        <Border Grid.Column="2" Width="12" Height="12" 
                Background="{x:Bind StatusColor}" CornerRadius="6"/>
    </Grid>
</Border>
```

---

### **Archivo 4: Services/Admin/AdminUsersService.cs**

**Responsabilidad:** Gestionar operaciones administrativas de usuarios (cambiar roles).

**Método Principal:**
```csharp
public async Task<bool> UpdateUserRoleAsync(Guid userId, string newRole, CancellationToken ct = default)
{
    var request = new UpdateUserRoleRequest { Role = newRole };
    
    var response = await App.Api.PutAsync<UpdateUserRoleRequest, UpdateUserRoleResponse>(
        $"/api/v1/admin/users/{userId}/roles",
        request,
        ct
    );

    if (response?.Success == true)
    {
        // Limpiar caché para forzar recarga
        PresenceService.Instance.ClearCache();
        return true;
    }

    return false;
}
```

---

## 🔄 Flujo de Funcionamiento

### **Apertura de la Ventana**

```
Usuario hace clic en "Usuarios Online" (DiarioPage)
    ↓
Se abre UsersOnlineWindow
    ↓
UsersOnlineViewModel.LoadAsync() se ejecuta
    ↓
PresenceService.GetUsersAsync() consulta la API
    ↓
Respuesta parseada a List<UserCardItem>
    ↓
Usuarios ordenados (online → offline, rol, nombre)
    ↓
ObservableCollection actualizada → UI se refresca
    ↓
Timer inicia (refresco cada 15 segundos)
```

### **Actualización Periódica**

```
Timer tick (cada 15 segundos)
    ↓
UsersOnlineViewModel.LoadAsync() se ejecuta
    ↓
PresenceService verifica caché
    ├─ Caché válido (<15s) → Retorna caché (sin petición HTTP)
    └─ Caché expirado (>15s) → Consulta API
    ↓
Usuarios actualizados en UI
```

### **Cambio de Rol**

```
Usuario ADMIN hace clic en "Cambiar Rol"
    ↓
Dialog muestra roles disponibles (ADMIN, EDITOR, USER)
    ↓
Usuario selecciona nuevo rol y confirma
    ↓
AdminUsersService.UpdateUserRoleAsync(userId, newRole)
    ↓
PUT /api/v1/admin/users/{userId}/roles
    ↓
Backend actualiza rol en BD
    ↓
Respuesta exitosa
    ↓
PresenceService.ClearCache() invalida caché
    ↓
Próxima recarga muestra rol actualizado
```

---

## 🎨 Estados Visuales

### **Indicador de Presencia**

| Estado | Color | Condición |
|--------|-------|-----------|
| 🟢 Online | Verde (#4CAF50) | `lastSeenAt >= now - 2min` |
| 🔴 Offline | Rojo (#F44336) | `lastSeenAt < now - 2min` |
| ⚪ Desconocido | Gris (#BDBDBD) | `lastSeenAt == null` |

### **Badges de Rol**

| Rol | Color | Icono |
|-----|-------|-------|
| ADMIN | Rojo (#F44336) | 👑 |
| EDITOR | Azul (#2196F3) | ✏️ |
| USER | Gris (#757575) | 👤 |

---

## 🔒 Seguridad y Permisos

### **Restricciones de Acceso**

| Endpoint | Rol Mínimo | Descripción |
|----------|-----------|-------------|
| GET /api/v1/admin/users | ADMIN | Solo admins pueden ver usuarios |
| GET /api/v1/admin/ping | Cualquiera | Todos pueden hacer ping |
| PUT /api/v1/admin/users/{id}/roles | ADMIN | Solo admins cambian roles |

### **Validación en Frontend**

```csharp
// Ejemplo: Ocultar botón "Cambiar Rol" si no es ADMIN
var currentUserRole = App.CurrentUserProfile?.Role;
BtnChangeRole.Visibility = currentUserRole == "ADMIN" 
    ? Visibility.Visible 
    : Visibility.Collapsed;
```

---

## 🐛 Manejo de Errores

### **Errores Comunes**

| Error | Causa | Solución |
|-------|-------|----------|
| 401 Unauthorized | Token expirado | Refrescar token o logout |
| 403 Forbidden | Usuario sin permisos | Mostrar mensaje: "Requiere rol ADMIN" |
| 404 Not Found | Endpoint no implementado | Verificar versión del backend |
| 500 Internal Server Error | Error del servidor | Mostrar mensaje amigable y reintentar |

### **Implementación Robusta**

```csharp
try
{
    var users = await PresenceService.Instance.GetUsersAsync(ct);
    // Procesar usuarios...
}
catch (HttpRequestException httpEx) when (httpEx.StatusCode == HttpStatusCode.Unauthorized)
{
    ErrorMessage = "Tu sesión ha expirado. Por favor, inicia sesión nuevamente.";
    ShowError();
}
catch (HttpRequestException httpEx) when (httpEx.StatusCode == HttpStatusCode.Forbidden)
{
    ErrorMessage = "No tienes permisos para ver esta información. Contacta al administrador.";
    ShowError();
}
catch (HttpRequestException httpEx)
{
    ErrorMessage = $"Error de conexión: {httpEx.Message}";
    ShowError();
}
catch (Exception ex)
{
    _log?.LogError(ex, "Error inesperado cargando usuarios");
    ErrorMessage = "Error inesperado. Revisa los logs para más detalles.";
    ShowError();
}
```

---

## 📊 Métricas y Logging

### **Logs Importantes**

```
📥 Cargando usuarios desde API GET /api/v1/admin/users...
✅ Usuarios cargados: 12 usuarios
📦 Usuarios desde caché (12 usuarios, caché válido por 8.3s)
📡 Enviando ping a GET /api/v1/admin/ping...
✅ Ping enviado correctamente
🔄 Actualizando rol del usuario 550e8400-e29b-41d4-a716-446655440000 a EDITOR...
✅ Rol actualizado correctamente
```

### **Estadísticas en Subtitle**

```csharp
private void UpdateSubtitle()
{
    TxtSubtitle.Text = $"{OnlineCount} online, {OfflineCount} offline • Actualizado a las {DateTime.Now:HH:mm:ss}";
}
```

---

## ✅ Checklist de Implementación

### **Backend (API)**

- [x] Agregar campo `last_seen_at` a tabla `users`
- [x] Implementar endpoint `GET /api/v1/admin/users`
- [x] Implementar endpoint `GET /api/v1/admin/ping`
- [x] Implementar endpoint `PUT /api/v1/admin/users/{id}/roles`
- [x] Agregar middleware de autenticación JWT
- [x] Agregar validación de permisos (solo ADMIN)

### **Frontend (Desktop)**

- [x] Crear `PresenceService` con caché
- [x] Crear `AdminUsersService` para gestión de roles
- [x] Crear `UsersOnlineViewModel` con timer
- [x] Crear `UsersOnlineWindow` con XAML
- [x] Integrar botón en `DiarioPage`
- [x] Manejar errores de conexión
- [x] Implementar logging completo

---

## 🚀 Próximas Mejoras (Futuro)

1. **SignalR para Real-Time**: Reemplazar polling por WebSocket
2. **Notificaciones Push**: Avisar cuando un usuario se conecta/desconecta
3. **Chat Interno**: Comunicación entre usuarios online
4. **Historial de Actividad**: Ver últimas acciones de cada usuario
5. **Exportar Lista**: Generar reporte de usuarios activos

---

## 📝 Referencias

- **Código Frontend**: `GestionTime.Desktop/Services/Presence/PresenceService.cs`
- **Código Backend**: `GestionTimeApi/Controllers/AdminUsersController.cs`
- **Documentación API**: `../GestionTimeApi/docs/RESUMEN-SISTEMA-PRESENCIA-PENDIENTE.md`
- **Scripts PowerShell**: `Scripts/Change-UserRole.ps1`

---

**Última Actualización:** 2024  
**Versión del Sistema:** v1.5.0-beta  
**Autor:** GestionTime Development Team
