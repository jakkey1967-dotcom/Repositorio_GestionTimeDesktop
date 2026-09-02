using Microsoft.UI.Xaml.Media;

namespace GestionTime.Desktop.Models;

/// <summary>Segmento visual de un parte existente en la fecha del editor.</summary>
public sealed class PartTimeSegment
{
    public int ParteId { get; init; }
    public TimeSpan Start { get; init; }
    public TimeSpan End { get; init; }
    public int DurationMinutes { get; init; }
    public string TimeText { get; init; } = string.Empty;
    public string DurationText { get; init; } = string.Empty;
    public string Cliente { get; init; } = string.Empty;
    public string Tienda { get; init; } = string.Empty;
    public string Ticket { get; init; } = string.Empty;
    public string Estado { get; init; } = string.Empty;
    public bool IsOverlapping { get; init; }
    public bool IsEditing { get; init; }
    public string StatusLabel => IsEditing ? "Editado" : string.Empty;
    public double DisplayWidth { get; set; }
    public Brush? Background { get; set; }
    public Brush? Foreground { get; set; }
    public string TooltipText { get; init; } = string.Empty;
    public string AutomationName { get; init; } = string.Empty;
    public string OverlapGlyph => IsOverlapping ? "⚠" : string.Empty;
}
