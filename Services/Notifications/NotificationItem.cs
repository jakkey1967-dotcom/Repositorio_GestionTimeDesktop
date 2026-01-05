using System;
using System.Collections.Generic;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace GestionTime.Desktop.Services.Notifications;

/// <summary>Representa una notificación individual en el sistema.</summary>
public sealed class NotificationItem
{
    /// <summary>ID único de la notificación (GUID).</summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>Tipo de notificación (Info, Success, Warning, Error).</summary>
    public NotificationType Type { get; init; }
    
    /// <summary>Título opcional de la notificación.</summary>
    public string? Title { get; init; }
    
    /// <summary>Mensaje principal de la notificación.</summary>
    public string Message { get; init; } = string.Empty;
    
    /// <summary>Momento en que se creó la notificación.</summary>
    public DateTime Timestamp { get; init; } = DateTime.Now;
    
    /// <summary>Duración en ms antes de auto-cerrar (0 = no auto-cerrar).</summary>
    public int DurationMs { get; init; }
    
    /// <summary>Lista de acciones disponibles en la notificación.</summary>
    public List<NotificationAction> Actions { get; init; } = new();
    
    /// <summary>ID de correlación para actualizar notificaciones existentes.</summary>
    public string? CorrelationId { get; init; }
    
    /// <summary>Indica si la notificación ha sido leída/vista.</summary>
    public bool IsRead { get; set; }
    
    /// <summary>Icono de la notificación (Segoe UI Symbol).</summary>
    public string Icon { get; init; } = "\uE946"; // Info por defecto
    
    /// <summary>Brush de acento según el tipo.</summary>
    public SolidColorBrush AccentBrush => Type switch
    {
        NotificationType.Success => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 40, 167, 69)),
        NotificationType.Warning => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 193, 7)),
        NotificationType.Error => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 220, 53, 69)),
        _ => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 123, 255))
    };
}
