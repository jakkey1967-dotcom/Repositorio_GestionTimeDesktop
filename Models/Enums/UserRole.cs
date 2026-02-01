namespace GestionTime.Desktop.Models.Enums;

/// <summary>Roles de usuario en el sistema.</summary>
public enum UserRole
{
    /// <summary>Usuario básico - Acceso limitado.</summary>
    USER = 0,
    
    /// <summary>Editor - Puede editar catálogos.</summary>
    EDITOR = 1,
    
    /// <summary>Administrador - Acceso completo.</summary>
    ADMIN = 2
}
