using GestionTime.Desktop.Models.Dtos;
using System.Collections.Generic;

namespace GestionTime.Desktop.Services.Import;

/// <summary>Estado de una fila del Excel en el proceso de importación ADMIN.</summary>
public enum ImportRowStatus
{
    OK,
    DUP_IN_FILE,
    DUP_EXISTS,
    INVALID
}

/// <summary>Fila del preview de importación con estado y mensaje.</summary>
public sealed class ImportPreviewRow
{
    public int RowNumber { get; set; }
    public string? Fecha { get; set; }
    public string? Inicio { get; set; }
    public string? Fin { get; set; }
    public string? Cliente { get; set; }
    public string? Ticket { get; set; }
    public string? Accion { get; set; }
    public string? Grupo { get; set; }
    public string? Tipo { get; set; }
    public ImportRowStatus Status { get; set; }
    public string Mensaje { get; set; } = string.Empty;

    /// <summary>Parte mapeado (null si INVALID).</summary>
    public ParteCreateRequest? Parte { get; set; }

    /// <summary>Clave de dedupe calculada para esta fila.</summary>
    public string DedupeKey { get; set; } = string.Empty;
}

/// <summary>Resumen de resultados de la importación.</summary>
public sealed class ImportSummary
{
    /// <summary>ID del batch en el backend (asignado tras el upload).</summary>
    public long BatchId { get; set; }
    public int Total { get; set; }
    public int Ok { get; set; }
    public int DupInFile { get; set; }
    public int DupExists { get; set; }
    public int Invalid { get; set; }
    public List<ImportPreviewRow> Rows { get; set; } = new();
}
