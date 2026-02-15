using System;
using System.Collections.Generic;

namespace GestionTime.Desktop.Models.Dtos.Reports;

/// <summary>Respuesta del endpoint GET /api/v2/informes/resumen.</summary>
public class InformeResumenDto
{
    public int PartsCount { get; set; }
    public int RecordedMinutes { get; set; }
    public int CoveredMinutes { get; set; }
    public int OverlapMinutes { get; set; }
    public string? FirstStart { get; set; }
    public string? LastEnd { get; set; }
    public List<IntervalDto> MergedIntervals { get; set; } = new();
    public List<GapDto> Gaps { get; set; } = new();

    // ⚠️ CAMBIO: Backend devuelve array, no dictionary
    public List<DayStatsDto>? ByDay { get; set; }
}

/// <summary>Intervalo cubierto (sin solapamientos).</summary>
public class IntervalDto
{
    public string Start { get; set; } = string.Empty;
    public string End { get; set; } = string.Empty;
    public int Minutes { get; set; }
}

/// <summary>Hueco (gap) detectado entre intervalos.</summary>
public class GapDto
{
    public string Start { get; set; } = string.Empty;
    public string End { get; set; } = string.Empty;
    public int Minutes { get; set; }
}

/// <summary>Estadísticas por día (solo en week/range).</summary>
public class DayStatsDto
{
    public DateTime Date { get; set; }  // ← Backend devuelve "2026-01-26T00:00:00Z"
    public int PartsCount { get; set; }
    public int RecordedMinutes { get; set; }
    public int CoveredMinutes { get; set; }
    public int OverlapMinutes { get; set; }
    public string? FirstStart { get; set; }
    public string? LastEnd { get; set; }
}
