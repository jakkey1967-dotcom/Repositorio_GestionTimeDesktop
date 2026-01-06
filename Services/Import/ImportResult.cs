using GestionTime.Desktop.Models.Dtos;
using System.Collections.Generic;

namespace GestionTime.Desktop.Services.Import;

/// <summary>Resultado del proceso de lectura/validación de Excel.</summary>
public sealed class ImportResult
{
    public List<ParteCreateRequest> ValidItems { get; set; } = new();
    public List<ImportError> Errors { get; set; } = new();
    public int TotalRows { get; set; }
    public string FileName { get; set; } = string.Empty;
}
