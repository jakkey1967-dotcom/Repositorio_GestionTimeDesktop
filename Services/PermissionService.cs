using GestionTime.Desktop.Models.Enums;
using Microsoft.Extensions.Logging;

namespace GestionTime.Desktop.Services;

/// <summary>Servicio de gestión de permisos y roles.</summary>
public sealed class PermissionService
{
    private readonly ILogger<PermissionService>? _log;
    private UserRole _currentUserRole = UserRole.USER;
    
    public PermissionService()
    {
        _log = App.LogFactory?.CreateLogger<PermissionService>();
    }
    
    /// <summary>Establece el rol del usuario actual.</summary>
    public void SetCurrentUserRole(UserRole role)
    {
        _currentUserRole = role;
        _log?.LogInformation("✅ Rol de usuario establecido: {role}", role);
    }
    
    /// <summary>Establece el rol del usuario actual desde un string.</summary>
    public void SetCurrentUserRole(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            _currentUserRole = UserRole.USER;
            return;
        }
        
        // Mapeo de nombres de rol del backend
        _currentUserRole = roleName.ToUpperInvariant() switch
        {
            "ADMIN" => UserRole.ADMIN,
            "EDITOR" => UserRole.EDITOR,
            "USER" => UserRole.USER,
            _ => UserRole.USER
        };
        
        _log?.LogInformation("✅ Rol establecido desde string '{roleName}': {role}", roleName, _currentUserRole);
    }
    
    /// <summary>Obtiene el rol del usuario actual.</summary>
    public UserRole GetCurrentUserRole() => _currentUserRole;
    
    /// <summary>Alias de GetCurrentUserRole() para consistencia.</summary>
    public UserRole GetCurrentRole() => _currentUserRole;
    
    /// <summary>Verifica si el usuario actual puede acceder a una sección.</summary>
    public bool CanAccessSection(string sectionId, UserRole[] allowedRoles)
    {
        if (allowedRoles == null || allowedRoles.Length == 0)
        {
            // Sin restricciones específicas
            return true;
        }
        
        var canAccess = allowedRoles.Contains(_currentUserRole);
        
        if (!canAccess)
        {
            _log?.LogWarning("❌ Acceso denegado a sección '{section}'. Rol actual: {currentRole}, Roles permitidos: {allowedRoles}", 
                sectionId, _currentUserRole, string.Join(", ", allowedRoles));
        }
        
        return canAccess;
    }
    
    /// <summary>Verifica si el usuario actual tiene un rol específico o superior.</summary>
    public bool HasRole(UserRole minimumRole)
    {
        return _currentUserRole >= minimumRole;
    }
    
    /// <summary>Verifica si el usuario es administrador.</summary>
    public bool IsAdmin() => _currentUserRole == UserRole.ADMIN;
    
    /// <summary>Verifica si el usuario es editor o superior.</summary>
    public bool IsEditorOrAbove() => _currentUserRole >= UserRole.EDITOR;
}
