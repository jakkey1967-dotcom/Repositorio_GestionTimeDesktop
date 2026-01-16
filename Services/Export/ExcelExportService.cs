using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using GestionTime.Desktop.Models.Dtos;
using Microsoft.Extensions.Logging;

namespace GestionTime.Desktop.Services.Export;

/// <summary>Implementación del servicio de exportación a Excel usando ClosedXML.</summary>
public sealed class ExcelExportService : IExcelExportService
{
    public ExcelExportService()
    {
    }

    /// <summary>Exporta partes a Excel con formato profesional y columnas específicas.</summary>
    public async Task ExportAsync(IEnumerable<ParteDto> partes, string filePath, CancellationToken cancellationToken = default)
    {
        if (partes == null)
            throw new ArgumentNullException(nameof(partes));

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));

        var listaPartes = partes.ToList();

        App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
        App.Log?.LogInformation("📊 EXPORTACIÓN A EXCEL - Iniciando");
        App.Log?.LogInformation("   • Archivo destino: {file}", filePath);
        App.Log?.LogInformation("   • Registros a exportar: {count}", listaPartes.Count);
        App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");

        try
        {
            await Task.Run(() =>
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Partes");

                cancellationToken.ThrowIfCancellationRequested();

                // ✅ ENCABEZADOS (según requisitos)
                var headers = new[]
                {
                    "PROYECTO",      // Cliente
                    "FECHA",         // Fecha trabajo
                    "HORA INICIO",   // Hora inicio
                    "HORA FIN",      // Hora fin
                    "DURACION",      // Duración formateada
                    "TAREA",         // Acción/Descripción
                    "GRUPO",         // Grupo
                    "TIPO"           // Tipo
                };

                // Escribir encabezados
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = worksheet.Cell(1, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0F766E"); // Teal 700
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }

                App.Log?.LogDebug("✅ Encabezados escritos (columnas: {count})", headers.Length);

                cancellationToken.ThrowIfCancellationRequested();

                // ✅ DATOS (fila por fila)
                int row = 2;
                foreach (var parte in listaPartes)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // PROYECTO = Cliente
                    worksheet.Cell(row, 1).Value = parte.Cliente ?? string.Empty;

                    // FECHA (formato dd/MM/yyyy)
                    worksheet.Cell(row, 2).Value = parte.Fecha != default
                        ? parte.Fecha.ToString("dd/MM/yyyy")
                        : string.Empty;

                    // HORA INICIO (formato HH:mm)
                    worksheet.Cell(row, 3).Value = FormatHora(parte.HoraInicio);

                    // HORA FIN (formato HH:mm)
                    worksheet.Cell(row, 4).Value = FormatHora(parte.HoraFin);

                    // DURACION (convertir minutos a HH:mm)
                    worksheet.Cell(row, 5).Value = FormatDuracion(parte.DuracionMin);

                    // TAREA = Acción
                    worksheet.Cell(row, 6).Value = parte.Accion ?? string.Empty;

                    // GRUPO
                    worksheet.Cell(row, 7).Value = parte.Grupo ?? string.Empty;

                    // TIPO
                    worksheet.Cell(row, 8).Value = parte.Tipo ?? string.Empty;

                    row++;
                }

                App.Log?.LogDebug("✅ Datos escritos ({count} filas)", listaPartes.Count);

                cancellationToken.ThrowIfCancellationRequested();

                // ✅ FORMATO PROFESIONAL
                
                // Autofiltro en encabezados
                worksheet.RangeUsed()?.SetAutoFilter();
                App.Log?.LogDebug("✅ Autofiltro aplicado");

                // Ajustar ancho de columnas automáticamente
                worksheet.Columns().AdjustToContents();
                App.Log?.LogDebug("✅ Columnas autoajustadas");

                // Congelar primera fila (encabezados)
                worksheet.SheetView.FreezeRows(1);
                App.Log?.LogDebug("✅ Primera fila congelada");

                // Bordes en toda la tabla
                var dataRange = worksheet.Range(1, 1, row - 1, headers.Length);
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                App.Log?.LogDebug("✅ Bordes aplicados");

                cancellationToken.ThrowIfCancellationRequested();

                // ✅ GUARDAR ARCHIVO
                workbook.SaveAs(filePath);
                App.Log?.LogInformation("✅ Archivo Excel guardado exitosamente: {file}", filePath);

            }, cancellationToken);

            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
            App.Log?.LogInformation("✅ EXPORTACIÓN COMPLETADA EXITOSAMENTE");
            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
        }
        catch (OperationCanceledException)
        {
            App.Log?.LogWarning("⚠️ Exportación cancelada por el usuario");
            throw;
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error durante la exportación a Excel");
            throw new InvalidOperationException($"Error exportando a Excel: {ex.Message}", ex);
        }
    }

    /// <summary>Formatea una hora en formato HH:mm.</summary>
    private static string FormatHora(string? hora)
    {
        if (string.IsNullOrWhiteSpace(hora))
            return string.Empty;

        // Si ya tiene formato HH:mm, retornar tal cual
        if (TimeSpan.TryParse(hora, out var time))
            return time.ToString(@"hh\:mm");

        return hora.Trim();
    }

    /// <summary>Convierte minutos a formato HH:mm.</summary>
    private static string FormatDuracion(int minutos)
    {
        if (minutos <= 0)
            return string.Empty;

        var horas = minutos / 60;
        var mins = minutos % 60;
        return $"{horas:D2}:{mins:D2}";
    }
}
