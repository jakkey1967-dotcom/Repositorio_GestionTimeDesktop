using GestionTime.Desktop.Helpers;
using System;

namespace GestionTime.Desktop.Helpers;

/// <summary>Métodos helper estáticos para DiarioPage.</summary>
public static class DiarioPageHelpers
{
    /// <summary>Verifica si una cadena contiene otra (case-insensitive).</summary>
    public static bool Has(string? s, string q)
        => !string.IsNullOrWhiteSpace(s) && s.Contains(q, StringComparison.OrdinalIgnoreCase);

    /// <summary>Parsea una cadena HH:mm a TimeSpan.</summary>
    public static TimeSpan ParseTime(string? hhmm)
        => TimeSpan.TryParse(hhmm, out var ts) ? ts : TimeSpan.Zero;

    /// <summary>Recorta una cadena para logging.</summary>
    public static string TrimForLog(string s, int max)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (s.Length <= max) return s;
        return s.Substring(0, max) + "…";
    }

    /// <summary>Construye el texto del tooltip de cobertura de tiempo.</summary>
    public static string BuildCoverageTooltipText(IntervalMerger.CoverageResult coverage)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("⏱️ TIEMPO REAL OCUPADO (SIN SOLAPAMIENTO)");
        sb.AppendLine();
        sb.AppendLine($"📊 Cubierto: {IntervalMerger.FormatDuration(coverage.TotalCovered)}");
        
        if (coverage.TotalOverlap.TotalMinutes > 0)
            sb.AppendLine($"⚠️ Solapado: {IntervalMerger.FormatDuration(coverage.TotalOverlap)}");
        
        sb.AppendLine();
        sb.AppendLine($"🕐 Intervalos cubiertos ({coverage.MergedIntervals.Count}):");
        
        foreach (var interval in coverage.MergedIntervals)
        {
            var formatted = IntervalMerger.FormatInterval(interval);
            var duration = IntervalMerger.FormatDuration(interval.Duration);
            sb.AppendLine($"   • {formatted} ({duration})");
        }
        
        return sb.ToString().TrimEnd();
    }
}
