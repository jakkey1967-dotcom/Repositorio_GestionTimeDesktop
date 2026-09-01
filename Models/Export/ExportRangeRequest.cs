using System;
using System.Collections.Generic;

namespace GestionTime.Desktop.Models.Export;

/// <summary>Modo de generación de archivos Excel para un rango de semanas.</summary>
public enum ExportMode
{
    Unified = 0,
    OneFilePerWeek = 1
}

/// <summary>Solicitud de exportación por rango de semanas ISO completas.</summary>
public sealed class ExportRangeRequest
{
    /// <summary>Lunes efectivo del rango (incluido).</summary>
    public DateTime EffectiveMonday { get; init; }

    /// <summary>Domingo efectivo del rango (incluido).</summary>
    public DateTime EffectiveSunday { get; init; }

    /// <summary>Modo de exportación seleccionado.</summary>
    public ExportMode Mode { get; init; }

    /// <summary>Semanas ISO completas incluidas en el rango.</summary>
    public IReadOnlyList<WeekOption> Weeks { get; init; } = Array.Empty<WeekOption>();
}
