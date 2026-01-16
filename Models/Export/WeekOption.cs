using System;

namespace GestionTime.Desktop.Models.Export;

/// <summary>Representa una semana disponible para exportación con formato ISO.</summary>
public sealed class WeekOption
{
    /// <summary>Año de la semana.</summary>
    public int Year { get; set; }

    /// <summary>Número de semana ISO (1-53).</summary>
    public int WeekNumber { get; set; }

    /// <summary>Fecha de inicio de la semana (lunes).</summary>
    public DateTime StartDate { get; set; }

    /// <summary>Fecha de fin de la semana (domingo).</summary>
    public DateTime EndDate { get; set; }

    /// <summary>Texto descriptivo para mostrar en ComboBox: "Semana 03 (15/01/2026 - 21/01/2026)".</summary>
    public string DisplayText { get; set; } = string.Empty;

    /// <summary>Constructor para crear una opción de semana.</summary>
    public WeekOption(int year, int weekNumber, DateTime startDate, DateTime endDate)
    {
        Year = year;
        WeekNumber = weekNumber;
        StartDate = startDate;
        EndDate = endDate;
        DisplayText = $"Semana {weekNumber:D2} ({startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy})";
    }

    /// <summary>Constructor por defecto para serialización.</summary>
    public WeekOption() { }
}
