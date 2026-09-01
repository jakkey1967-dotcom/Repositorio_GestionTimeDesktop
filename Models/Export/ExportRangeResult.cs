using System;
using System.Collections.Generic;

namespace GestionTime.Desktop.Models.Export;

/// <summary>Resultado de una exportación Excel por rango de semanas.</summary>
public sealed class ExportRangeResult
{
    /// <summary>Indica si todos los archivos previstos se generaron correctamente.</summary>
    public bool IsSuccess { get; init; }

    /// <summary>Total de registros exportados.</summary>
    public int TotalRecords { get; init; }

    /// <summary>Lunes efectivo del rango.</summary>
    public DateTime EffectiveMonday { get; init; }

    /// <summary>Domingo efectivo del rango.</summary>
    public DateTime EffectiveSunday { get; init; }

    /// <summary>Semanas con archivo generado.</summary>
    public int WeeksProcessed { get; init; }

    /// <summary>Semanas del rango sin registros.</summary>
    public int WeeksWithoutRecords { get; init; }

    /// <summary>Rutas de los archivos creados.</summary>
    public IReadOnlyList<string> GeneratedFiles { get; init; } = Array.Empty<string>();

    /// <summary>Archivo o carpeta de destino.</summary>
    public string? Destination { get; init; }

    /// <summary>Ruta del archivo que falló, si aplica.</summary>
    public string? FailedFile { get; init; }

    /// <summary>Mensaje de error de un fallo parcial o total.</summary>
    public string? ErrorMessage { get; init; }
}
