using System;
using System.Collections.Generic;
using System.Globalization;
using GestionTime.Desktop.Models.Export;

namespace GestionTime.Desktop.Helpers;

/// <summary>Normaliza y enumera semanas ISO (lunes a domingo) para exportación.</summary>
public static class IsoWeekRangeHelper
{
    /// <summary>Devuelve el lunes de la semana ISO que contiene la fecha.</summary>
    public static DateTime GetMonday(DateTime date)
    {
        var value = date.Date;
        var year = ISOWeek.GetYear(value);
        var week = ISOWeek.GetWeekOfYear(value);
        return ISOWeek.ToDateTime(year, week, DayOfWeek.Monday).Date;
    }

    /// <summary>Devuelve el domingo de la semana ISO que contiene la fecha.</summary>
    public static DateTime GetSunday(DateTime date) => GetMonday(date).AddDays(6);

    /// <summary>Obtiene la clave ISO (año, semana) de una fecha.</summary>
    public static (int Year, int Week) GetKey(DateTime date)
    {
        var value = date.Date;
        return (ISOWeek.GetYear(value), ISOWeek.GetWeekOfYear(value));
    }

    /// <summary>Texto compacto de la semana ISO: "S36 · 31/08–06/09".</summary>
    public static string GetWeekLabel(DateTime date)
    {
        var key = GetKey(date);
        var monday = GetMonday(date);
        var sunday = GetSunday(date);
        return $"S{key.Week} · {monday:dd/MM}–{sunday:dd/MM}";
    }

    /// <summary>Tooltip de la semana ISO: "Semana ISO 36 de 2026 · 31/08/2026–06/09/2026".</summary>
    public static string GetWeekTooltip(DateTime date)
    {
        var key = GetKey(date);
        var monday = GetMonday(date);
        var sunday = GetSunday(date);
        return $"Semana ISO {key.Week} de {key.Year} · {monday:dd/MM/yyyy}–{sunday:dd/MM/yyyy}";
    }

    /// <summary>Indica si la fecha pertenece a la semana ISO indicada.</summary>
    public static bool MatchesWeek(DateTime date, WeekOption week)
    {
        if (week == null)
            return false;

        var key = GetKey(date);
        return key.Year == week.Year && key.Week == week.WeekNumber;
    }

    /// <summary>Enumera las semanas ISO completas inclusivas entre lunes y domingo.</summary>
    public static IReadOnlyList<WeekOption> EnumerateWeeks(DateTime monday, DateTime sunday)
    {
        var start = monday.Date;
        var end = sunday.Date;
        var weeks = new List<WeekOption>();

        if (end < start)
            return weeks;

        var cursor = start;
        while (cursor <= end)
        {
            var key = GetKey(cursor);
            var weekStart = ISOWeek.ToDateTime(key.Year, key.Week, DayOfWeek.Monday).Date;
            var weekEnd = weekStart.AddDays(6);
            weeks.Add(new WeekOption(key.Year, key.Week, weekStart, weekEnd));
            cursor = weekStart.AddDays(7);
        }

        return weeks;
    }
}
