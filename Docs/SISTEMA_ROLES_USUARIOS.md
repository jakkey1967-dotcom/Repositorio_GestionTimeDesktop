# 🔐 Sistema de Roles de Usuario - GestionTime Desktop

## 📋 Descripción

Este documento describe cómo implementar un sistema de roles de usuario en GestionTime Desktop, permitiendo control de acceso basado en permisos (RBAC - Role-Based Access Control).

---

## 🎯 Objetivo

Crear un sistema que:

- ✅ Distinga entre diferentes tipos de usuarios (Admin, Técnico, Supervisor, Usuario)
- ✅ Controle el acceso a funcionalidades según el rol
- ✅ Muestre/oculte elementos de UI según permisos
- ✅ Valide acciones críticas en el backend y frontend

---

## 🏗️ Arquitectura del Sistema

### **Flujo General**

```
┌─────────────┐
│   Login     │ → Backend valida credenciales
└──────┬──────┘
       ↓
┌─────────────┐
│  JWT Token  │ → Incluye claims: roles, permisos
└──────┬──────┘
       ↓
┌─────────────┐
│  Frontend   │ → Lee roles y aplica restricciones
└─────────────┘
       ↓
   ┌─────────────────────────┐
   │  UI se adapta al rol    │
   └─────────────────────────┘
```

---

## 👥 Roles Propuestos

| Rol | Permisos | Descripción |
|-----|----------|-------------|
| **Admin** | Todos | Control total del sistema |
| **Supervisor** | Ver, Editar, Exportar | Gestión de partes de su equipo |
| **Técnico** | Ver, Crear, Editar (propios) | Gestión de sus propios partes |
| **Usuario** | Solo lectura | Consulta de información |

---

## 📂 Estructura de Archivos

```
GestionTime.Desktop/
├── Models/
│   ├── Auth/
│   │   ├── UserRole.cs              ← Enum de roles
│   │   ├── Permission.cs            ← Enum de permisos
│   │   └── UserInfo.cs              ← Info del usuario con roles
│   └── Dtos/
│       └── LoginResponse.cs         ← Respuesta con rol incluido
├── Services/
│   ├── AuthService.cs               ← Servicio de autenticación
│   └── PermissionService.cs         ← Validación de permisos
├── Helpers/
│   ├── RoleHelper.cs                ← Helper para verificar roles
│   └── PermissionVisibilityConverter.cs ← Converter XAML
└── Views/
    └── DiarioPage.xaml              ← UI adaptada a roles
```

---

## 🔧 Implementación Paso a Paso

### **PASO 1: Crear Enum de Roles**

**Archivo:** `Models/Auth/UserRole.cs`

```csharp
namespace GestionTime.Desktop.Models.Auth;

/// <summary>Roles de usuario en el sistema.</summary>
public enum UserRole
{
    /// <summary>Usuario sin rol asignado.</summary>
    None = 0,
    
    /// <summary>Usuario estándar (solo lectura).</summary>
    Usuario = 1,
    
    /// <summary>Técnico (gestiona sus propios partes).</summary>
    Tecnico = 2,
    
    /// <summary>Supervisor (gestiona partes de su equipo).</summary>
    Supervisor = 3,
    
    /// <summary>Administrador (control total).</summary>
    Admin = 4
}
```

---

### **PASO 2: Crear Enum de Permisos**

**Archivo:** `Models/Auth/Permission.cs`

```csharp
namespace GestionTime.Desktop.Models.Auth;

/// <summary>Permisos específicos en el sistema.</summary>
[Flags]
public enum Permission
{
    None = 0,
    
    // Partes
    PartesVer = 1 << 0,           // 1
    PartesCrear = 1 << 1,         // 2
    PartesEditar = 1 << 2,        // 4
    PartesEliminar = 1 << 3,      // 8
    PartesExportar = 1 << 4,      // 16
    PartesImportar = 1 << 5,      // 32
    
    // Configuración
    ConfigVer = 1 << 6,           // 64
    ConfigEditar = 1 << 7,        // 128
    
    // Usuarios
    UsuariosVer = 1 << 8,         // 256
    UsuariosGestionar = 1 << 9,   // 512
    
    // Reportes
    ReportesVer = 1 << 10,        // 1024
    ReportesExportar = 1 << 11,   // 2048
    
    // Todos los permisos
    All = int.MaxValue
}
```

---

### **PASO 3: Actualizar LoginResponse**

**Archivo:** `Models/Dtos/LoginResponse.cs`

```csharp
using System.Text.Json.Serialization;

namespace GestionTime.Desktop.Models.Dtos;

public sealed class LoginResponse
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = "";

    [JsonPropertyName("refreshToken")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("userName")]
    public string UserName { get; set; } = "";

    [JsonPropertyName("email")]
    public string? Email { get; set; }
    
    // 🆕 NUEVO: Rol del usuario
    [JsonPropertyName("role")]
    public string Role { get; set; } = "Usuario";
    
    // 🆕 NUEVO: Nombre completo
    [JsonPropertyName("fullName")]
    public string? FullName { get; set; }
}
```

---

### **PASO 4: Crear Servicio de Permisos**

**Archivo:** `Services/PermissionService.cs`

```csharp
using GestionTime.Desktop.Models.Auth;
using System;
using System.Collections.Generic;

namespace GestionTime.Desktop.Services;

/// <summary>Servicio para validar permisos de usuario.</summary>
public sealed class PermissionService
{
    private static PermissionService? _instance;
    public static PermissionService Instance => _instance ??= new PermissionService();

    private UserRole _currentRole = UserRole.None;
    private string _currentUserName = "";

    /// <summary>Establece el rol actual del usuario.</summary>
    public void SetUserRole(string role, string userName)
    {
        _currentRole = ParseRole(role);
        _currentUserName = userName;
    }

    /// <summary>Obtiene el rol actual.</summary>
    public UserRole CurrentRole => _currentRole;

    /// <summary>Obtiene el nombre del rol actual.</summary>
    public string CurrentRoleName => _currentRole.ToString();

    /// <summary>Verifica si el usuario tiene un permiso específico.</summary>
    public bool HasPermission(Permission permission)
    {
        var rolePermissions = GetRolePermissions(_currentRole);
        return (rolePermissions & permission) == permission;
    }

    /// <summary>Verifica si el usuario tiene al menos uno de varios permisos.</summary>
    public bool HasAnyPermission(params Permission[] permissions)
    {
        foreach (var permission in permissions)
        {
            if (HasPermission(permission))
                return true;
        }
        return false;
    }

    /// <summary>Verifica si el usuario tiene todos los permisos especificados.</summary>
    public bool HasAllPermissions(params Permission[] permissions)
    {
        foreach (var permission in permissions)
        {
            if (!HasPermission(permission))
                return false;
        }
        return true;
    }

    /// <summary>Verifica si el usuario puede editar un parte específico.</summary>
    public bool CanEditParte(string parteOwner)
    {
        // Admin y Supervisor pueden editar cualquier parte
        if (_currentRole == UserRole.Admin || _currentRole == UserRole.Supervisor)
            return true;

        // Técnicos solo pueden editar sus propios partes
        if (_currentRole == UserRole.Tecnico)
            return string.Equals(_currentUserName, parteOwner, StringComparison.OrdinalIgnoreCase);

        return false;
    }

    /// <summary>Obtiene los permisos asociados a un rol.</summary>
    private Permission GetRolePermissions(UserRole role)
    {
        return role switch
        {
            UserRole.Admin => Permission.All,
            
            UserRole.Supervisor => 
                Permission.PartesVer | Permission.PartesCrear | Permission.PartesEditar | 
                Permission.PartesEliminar | Permission.PartesExportar | Permission.PartesImportar |
                Permission.ReportesVer | Permission.ReportesExportar,
            
            UserRole.Tecnico => 
                Permission.PartesVer | Permission.PartesCrear | Permission.PartesEditar |
                Permission.PartesExportar,
            
            UserRole.Usuario => 
                Permission.PartesVer | Permission.ReportesVer,
            
            _ => Permission.None
        };
    }

    /// <summary>Convierte string a enum UserRole.</summary>
    private UserRole ParseRole(string role)
    {
        return role?.ToLowerInvariant() switch
        {
            "admin" or "administrador" => UserRole.Admin,
            "supervisor" => UserRole.Supervisor,
            "tecnico" or "técnico" => UserRole.Tecnico,
            "usuario" => UserRole.Usuario,
            _ => UserRole.None
        };
    }

    /// <summary>Limpia el rol actual (logout).</summary>
    public void Clear()
    {
        _currentRole = UserRole.None;
        _currentUserName = "";
    }
}
```

---

### **PASO 5: Actualizar LoginPage para Guardar Rol**

**Archivo:** `Views/LoginPage.xaml.cs`

Modificar el método `OnLoginClick`:

```csharp
private async void OnLoginClick(object sender, RoutedEventArgs e)
{
    // ... código existente ...

    var response = await App.Api.LoginAsync(request);

    if (response != null)
    {
        // 🆕 NUEVO: Guardar rol en PermissionService
        PermissionService.Instance.SetUserRole(response.Role, response.UserName);
        
        App.Log?.LogInformation("✅ Login exitoso - Usuario: {user}, Rol: {role}", 
            response.UserName, response.Role);

        // ... resto del código ...
    }
}
```

---

### **PASO 6: Crear Converter para XAML**

**Archivo:** `Helpers/PermissionVisibilityConverter.cs`

```csharp
using GestionTime.Desktop.Models.Auth;
using GestionTime.Desktop.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace GestionTime.Desktop.Helpers;

/// <summary>Converter para mostrar/ocultar elementos según permisos.</summary>
public sealed class PermissionVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (parameter is not string permissionName)
            return Visibility.Collapsed;

        if (!Enum.TryParse<Permission>(permissionName, out var permission))
            return Visibility.Collapsed;

        return PermissionService.Instance.HasPermission(permission) 
            ? Visibility.Visible 
            : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
```

---

### **PASO 7: Usar en XAML**

**Archivo:** `Views/DiarioPage.xaml`

```xaml
<Page.Resources>
    <!-- Converter de permisos -->
    <helpers:PermissionVisibilityConverter x:Key="PermissionVisibilityConverter"/>
</Page.Resources>

<!-- Botón visible solo para Admin y Supervisor -->
<Button x:Name="BtnImportar"
        Content="Importar Excel"
        Click="OnImportarExcel"
        Visibility="{Binding Mode=OneWay, 
                     Converter={StaticResource PermissionVisibilityConverter}, 
                     ConverterParameter=PartesImportar}"/>

<!-- Botón visible solo para Admin -->
<Button x:Name="BtnConfiguracion"
        Content="Configuración"
        Click="OnConfiguracion"
        Visibility="{Binding Mode=OneWay, 
                     Converter={StaticResource PermissionVisibilityConverter}, 
                     ConverterParameter=ConfigEditar}"/>
```

---

### **PASO 8: Validar en Code-Behind**

**Archivo:** `Views/DiarioPage.xaml.cs`

```csharp
private async void OnBorrar(object sender, RoutedEventArgs e)
{
    // Verificar permiso
    if (!PermissionService.Instance.HasPermission(Permission.PartesEliminar))
    {
        await ShowInfoAsync("No tienes permisos para eliminar partes.");
        return;
    }

    // ... resto del código ...
}

private async void OnEditar(object sender, RoutedEventArgs e)
{
    var parte = LvPartes.SelectedItem as ParteDto;
    if (parte == null) return;

    // Verificar si puede editar este parte específico
    if (!PermissionService.Instance.CanEditParte(parte.Tecnico))
    {
        await ShowInfoAsync("Solo puedes editar tus propios partes.");
        return;
    }

    // ... resto del código ...
}
```

---

## 📊 Tabla de Permisos por Rol

| Funcionalidad | Usuario | Técnico | Supervisor | Admin |
|---------------|---------|---------|------------|-------|
| Ver partes | ✅ | ✅ | ✅ | ✅ |
| Crear parte | ❌ | ✅ | ✅ | ✅ |
| Editar parte propio | ❌ | ✅ | ✅ | ✅ |
| Editar parte ajeno | ❌ | ❌ | ✅ | ✅ |
| Eliminar parte | ❌ | ❌ | ✅ | ✅ |
| Importar Excel | ❌ | ❌ | ✅ | ✅ |
| Exportar Excel | ❌ | ✅ | ✅ | ✅ |
| Configuración | ❌ | ❌ | ❌ | ✅ |
| Gestión usuarios | ❌ | ❌ | ❌ | ✅ |

---

## 🔒 Seguridad

### ✅ **Validación en Frontend**

```csharp
// Verificar permiso antes de ejecutar acción
if (!PermissionService.Instance.HasPermission(Permission.PartesEliminar))
{
    await ShowInfoAsync("Sin permisos.");
    return;
}
```

### ✅ **Validación en Backend (CRÍTICO)**

El backend **SIEMPRE** debe validar permisos en cada endpoint:

```csharp
[Authorize(Roles = "Admin,Supervisor")]
[HttpDelete("api/v1/partes/{id}")]
public async Task<IActionResult> DeleteParte(int id)
{
    // Backend valida rol desde JWT token
    // ...
}
```

---

## 🧪 Testing

### **Test de Permisos**

```csharp
[Test]
public void Admin_TienePermiso_Eliminar()
{
    PermissionService.Instance.SetUserRole("Admin", "admin@test.com");
    Assert.IsTrue(PermissionService.Instance.HasPermission(Permission.PartesEliminar));
}

[Test]
public void Tecnico_NoTienePermiso_Eliminar()
{
    PermissionService.Instance.SetUserRole("Tecnico", "tecnico@test.com");
    Assert.IsFalse(PermissionService.Instance.HasPermission(Permission.PartesEliminar));
}

[Test]
public void Tecnico_PuedeEditar_PartePropio()
{
    PermissionService.Instance.SetUserRole("Tecnico", "juan.perez");
    Assert.IsTrue(PermissionService.Instance.CanEditParte("juan.perez"));
    Assert.IsFalse(PermissionService.Instance.CanEditParte("maria.lopez"));
}
```

---

## 📝 Checklist de Implementación

- [ ] Crear `UserRole.cs` enum
- [ ] Crear `Permission.cs` enum
- [ ] Actualizar `LoginResponse.cs` con campo `Role`
- [ ] Crear `PermissionService.cs`
- [ ] Crear `PermissionVisibilityConverter.cs`
- [ ] Actualizar `LoginPage.xaml.cs` para guardar rol
- [ ] Actualizar `DiarioPage.xaml` con Converter
- [ ] Validar permisos en `DiarioPage.xaml.cs`
- [ ] Agregar `[Authorize]` en endpoints del backend
- [ ] Crear tests unitarios
- [ ] Documentar en `MANUAL_USUARIO.md`

---

## 🚀 Mejoras Futuras

1. **Permisos Dinámicos**: Cargar permisos desde base de datos
2. **Roles Personalizados**: Permitir crear roles custom
3. **Auditoría**: Registrar todas las acciones por usuario
4. **Roles Múltiples**: Un usuario puede tener varios roles
5. **Caché de Permisos**: Optimizar consultas

---

## 🔗 Referencias

- [ASP.NET Core Authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/)
- [JWT Claims](https://jwt.io/introduction)
- [RBAC Pattern](https://en.wikipedia.org/wiki/Role-based_access_control)

---

**¿Necesitas ayuda implementando esto?** Consulta el equipo de desarrollo.
