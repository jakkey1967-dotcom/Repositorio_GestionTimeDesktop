using System.Collections.Generic;

namespace GestionTime.Desktop.Services.Notifications;

/// <summary>Opciones configurables para una notificación.</summary>
public sealed class NotificationOptions
{
    /// <summary>Duración personalizada en ms (null = usar default de config).</summary>
    public int? DurationMs { get; set; }
    
    /// <summary>ID de correlación para actualizar notificaciones existentes.</summary>
    public string? CorrelationId { get; set; }
    
    /// <summary>Lista de acciones personalizadas.</summary>
    public List<NotificationAction>? Actions { get; set; }
    
    /// <summary>Si true, envía la notificación al endpoint configurado.</summary>
    public bool Persist { get; set; }
    
    /// <summary>Si false (default), usa throttling para evitar duplicados.</summary>
    public bool AllowDuplicates { get; set; }
    
    /// <summary>Icono personalizado (Segoe UI Symbol).</summary>
    public string? CustomIcon { get; set; }
}
