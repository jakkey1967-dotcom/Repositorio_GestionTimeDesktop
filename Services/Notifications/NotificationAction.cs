using System;
using System.Threading.Tasks;

namespace GestionTime.Desktop.Services.Notifications;

/// <summary>Acción que puede ejecutarse desde una notificación.</summary>
public sealed class NotificationAction
{
    /// <summary>Texto del botón de acción.</summary>
    public string Label { get; init; } = string.Empty;
    
    /// <summary>Acción asíncrona a ejecutar al hacer clic.</summary>
    public Func<Task>? OnClick { get; init; }
    
    /// <summary>Si true, cierra la notificación automáticamente tras ejecutar la acción.</summary>
    public bool CloseOnClick { get; init; } = true;
    
    /// <summary>Icono opcional (Segoe UI Symbol).</summary>
    public string? Icon { get; init; }
}
