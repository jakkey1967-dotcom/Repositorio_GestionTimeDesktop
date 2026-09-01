using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestionTime.Desktop.Helpers;
using GestionTime.Desktop.Models.Dtos;
using GestionTime.Desktop.Models.Export;
using Microsoft.Extensions.Logging;

namespace GestionTime.Desktop.Services.Export;

/// <summary>Orquesta la exportación Excel unificada o por semanas ISO.</summary>
public sealed class ExcelRangeExportService
{
    private readonly IExcelExportService _excelExportService;

    public ExcelRangeExportService(IExcelExportService excelExportService)
    {
        _excelExportService = excelExportService ?? throw new ArgumentNullException(nameof(excelExportService));
    }

    /// <summary>Exporta el rango indicado reutilizando el formato Excel existente.</summary>
    public async Task<ExportRangeResult> ExportRangeAsync(
        IReadOnlyList<ParteDto> partes,
        ExportRangeRequest request,
        string destinationPathOrFolder,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(partes);
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(destinationPathOrFolder))
            throw new ArgumentNullException(nameof(destinationPathOrFolder));

        var monday = request.EffectiveMonday.Date;
        var sunday = request.EffectiveSunday.Date;
        var weeks = request.Weeks.Count > 0
            ? request.Weeks
            : IsoWeekRangeHelper.EnumerateWeeks(monday, sunday);

        var inRange = partes
            .Where(p => p.Fecha.Date >= monday && p.Fecha.Date <= sunday)
            .GroupBy(p => p.Id)
            .Select(g => g.First())
            .OrderBy(p => p.Fecha)
            .ThenBy(p => p.HoraInicio)
            .ToList();

        App.Log?.LogInformation("📊 ExportRange: {count} registros, {weeks} semanas, modo {mode}",
            inRange.Count, weeks.Count, request.Mode);

        if (request.Mode == ExportMode.Unified)
        {
            await _excelExportService.ExportAsync(inRange, destinationPathOrFolder, cancellationToken);
            var emptyWeeks = weeks.Count(week => !inRange.Any(p => IsoWeekRangeHelper.MatchesWeek(p.Fecha, week)));
            return new ExportRangeResult
            {
                IsSuccess = true,
                TotalRecords = inRange.Count,
                EffectiveMonday = monday,
                EffectiveSunday = sunday,
                WeeksProcessed = weeks.Count - emptyWeeks,
                WeeksWithoutRecords = emptyWeeks,
                GeneratedFiles = new[] { destinationPathOrFolder },
                Destination = destinationPathOrFolder
            };
        }

        var generated = new List<string>();
        var empty = 0;
        var exportedRecords = 0;
        string? failedFile = null;
        string? errorMessage = null;

        foreach (var week in weeks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var weekPartes = inRange
                .Where(p => IsoWeekRangeHelper.MatchesWeek(p.Fecha, week)
                            && p.Fecha.Date >= week.StartDate.Date
                            && p.Fecha.Date <= week.EndDate.Date)
                .ToList();

            if (weekPartes.Count == 0)
            {
                empty++;
                continue;
            }

            var fileName = $"GestionTime_Semana_{week.Year}_{week.WeekNumber:D2}_{week.StartDate:yyyy-MM-dd}_a_{week.EndDate:yyyy-MM-dd}.xlsx";
            var filePath = GetUniqueFilePath(destinationPathOrFolder, fileName);

            try
            {
                await _excelExportService.ExportAsync(weekPartes, filePath, cancellationToken);
                generated.Add(filePath);
                exportedRecords += weekPartes.Count;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                App.Log?.LogError(ex, "❌ Error exportando semana {year}-{week}", week.Year, week.WeekNumber);
                failedFile = filePath;
                errorMessage = ex.Message;
                break;
            }
        }

        return new ExportRangeResult
        {
            IsSuccess = failedFile == null && generated.Count > 0,
            TotalRecords = exportedRecords,
            EffectiveMonday = monday,
            EffectiveSunday = sunday,
            WeeksProcessed = generated.Count,
            WeeksWithoutRecords = empty,
            GeneratedFiles = generated,
            Destination = destinationPathOrFolder,
            FailedFile = failedFile,
            ErrorMessage = errorMessage
        };
    }

    /// <summary>Genera una ruta única sin sobrescribir archivos existentes.</summary>
    private static string GetUniqueFilePath(string directory, string fileName)
    {
        var path = Path.Combine(directory, fileName);
        if (!File.Exists(path))
            return path;

        var name = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        var index = 2;
        do
        {
            path = Path.Combine(directory, $"{name} ({index}){extension}");
            index++;
        } while (File.Exists(path));

        return path;
    }
}
