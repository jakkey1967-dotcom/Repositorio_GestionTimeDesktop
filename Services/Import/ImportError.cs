namespace GestionTime.Desktop.Services.Import;

/// <summary>Representa un error durante la importación de una fila.</summary>
public sealed class ImportError
{
    public int RowIndex { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? RawData { get; set; } // JSON de la fila problemática
}
