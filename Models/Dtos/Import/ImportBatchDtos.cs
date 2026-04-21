using System.Collections.Generic;

namespace GestionTime.Desktop.Models.Dtos.Import;

/// <summary>Respuesta del upload de batch (POST /api/v2/admin/import/batches).</summary>
public sealed class ImportBatchCreateResponse
{
    public long BatchId { get; set; }
    public string Status { get; set; } = "";
    public int RowsLoaded { get; set; }
    public int DupsInFile { get; set; }
    public string TargetUserEmail { get; set; } = "";
    public string SourceFileName { get; set; } = "";
}

/// <summary>Respuesta de validación (POST /api/v2/admin/import/batches/{id}/validate).</summary>
public sealed class ImportBatchValidateResponse
{
    public long BatchId { get; set; }
    public string Status { get; set; } = "";
    public int TotalRows { get; set; }
    public int RowsOk { get; set; }
    public int DupsInFile { get; set; }
    public int DupsInDb { get; set; }
    public int Invalid { get; set; }
    public List<ImportRowPreviewDto> Rows { get; set; } = new();
}

/// <summary>Vista previa de una fila del batch.</summary>
public sealed class ImportRowPreviewDto
{
    public long Id { get; set; }
    public int RowNumber { get; set; }
    public string ValidationStatus { get; set; } = "";
    public string? ValidationError { get; set; }
    public string FechaTrabajo { get; set; } = "";
    public string HoraInicio { get; set; } = "";
    public string HoraFin { get; set; } = "";
    public string Accion { get; set; } = "";
    public string? Ticket { get; set; }
    public int IdCliente { get; set; }
    public string? Tienda { get; set; }
    public int? IdGrupo { get; set; }
    public int? IdTipo { get; set; }
}

/// <summary>Respuesta de aplicación (POST /api/v2/admin/import/batches/{id}/apply).</summary>
public sealed class ImportBatchApplyResponse
{
    public long BatchId { get; set; }
    public string Status { get; set; } = "";
    public int Inserted { get; set; }
    public int SkippedDups { get; set; }
}
