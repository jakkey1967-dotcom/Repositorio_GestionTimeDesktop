using GestionTime.Desktop.Models.Enums;
using Microsoft.UI.Xaml.Media;

namespace GestionTime.Desktop.Models;

/// <summary>Representa una sección del menú de Settings.</summary>
public sealed class SettingsSectionItem
{
    /// <summary>ID único de la sección.</summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>Nombre visible en el menú.</summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>Descripción breve.</summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>Icono Segoe MDL2 Assets.</summary>
    public string Icon { get; set; } = "\uE713"; // Default: Settings
    
    /// <summary>Roles permitidos para acceder a esta sección.</summary>
    public UserRole[] AllowedRoles { get; set; } = Array.Empty<UserRole>();
    
    /// <summary>Indica si la sección está visible en el menú (según permisos).</summary>
    public bool IsVisible { get; set; } = true;
    
    /// <summary>Indica si el usuario actual tiene permiso para acceder a esta sección.</summary>
    public bool IsAllowed { get; set; } = true;
    
    /// <summary>Icono de candado: 🔓 (permitido) o 🔒 (bloqueado).</summary>
    public string LockIcon { get; set; } = "\uE785"; // LockOpen
    
    /// <summary>Color del candado: verde/teal (permitido) o amarillo (bloqueado).</summary>
    public Brush LockBrush { get; set; } = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
}
