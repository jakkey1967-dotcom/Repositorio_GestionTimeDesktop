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
                int firstDataRow = 2;
                int rowsWithErrors = 0;
                int rowsWithMissingTime = 0;
                int rowsWithFallbackDuration = 0;
                
                foreach (var parte in listaPartes)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    bool hasError = false;
                    var errorDetails = new List<string>();

                    // PROYECTO = Cliente
                    worksheet.Cell(row, 1).Value = parte.Cliente ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(parte.Cliente))
                    {
                        errorDetails.Add("Cliente vacío");
                    }

                    // FECHA (como valor de fecha, no texto)
                    if (parte.Fecha != default)
                    {
                        worksheet.Cell(row, 2).Value = parte.Fecha;
                        worksheet.Cell(row, 2).Style.DateFormat.Format = "dd/MM/yyyy";
                    }
                    else
                    {
                        errorDetails.Add("Fecha inválida");
                        hasError = true;
                    }

                    // HORA INICIO (como valor de tiempo, no texto)
                    var horaInicio = ParseTimeToExcelValue(parte.HoraInicio);
                    if (horaInicio.HasValue)
                    {
                        worksheet.Cell(row, 3).Value = horaInicio.Value;
                        worksheet.Cell(row, 3).Style.NumberFormat.Format = "HH:mm";
                    }
                    else
                    {
                        errorDetails.Add($"Hora Inicio inválida o vacía: '{parte.HoraInicio}'");
                        rowsWithMissingTime++;
                    }

                    // HORA FIN (como valor de tiempo, no texto)
                    var horaFin = ParseTimeToExcelValue(parte.HoraFin);
                    if (horaFin.HasValue)
                    {
                        worksheet.Cell(row, 4).Value = horaFin.Value;
                        worksheet.Cell(row, 4).Style.NumberFormat.Format = "HH:mm";
                    }
                    else
                    {
                        errorDetails.Add($"Hora Fin inválida o vacía: '{parte.HoraFin}'");
                        rowsWithMissingTime++;
                    }

                    // DURACION (fórmula Excel: HoraFin - HoraInicio, con manejo de medianoche)
                    if (horaInicio.HasValue && horaFin.HasValue)
                    {
                        // ✅ Validar que la duración calculada sea razonable (<24h normalmente)
                        var duracionCalculada = horaFin.Value - horaInicio.Value;
                        if (duracionCalculada < 0)
                        {
                            // Cruce de medianoche
                            duracionCalculada += 1.0; // Sumar 1 día
                        }
                        
                        // Advertir si duración >16 horas (jornada muy larga, posible error)
                        if (duracionCalculada > 0.666667) // 16/24 = 0.666667
                        {
                            errorDetails.Add($"Duración sospechosa: {duracionCalculada * 24:F2}h");
                            hasError = true;
                        }
                        
                        // Fórmula: Si HoraFin < HoraInicio, suma 1 día (cruce de medianoche)
                        worksheet.Cell(row, 5).FormulaA1 = $"=IF(D{row}<C{row},D{row}+1-C{row},D{row}-C{row})";
                        worksheet.Cell(row, 5).Style.NumberFormat.Format = "[h]:mm:ss";
                    }
                    else
                    {
                        // Fallback: Si no hay horas, usar duración en minutos
                        if (parte.DuracionMin > 0)
                        {
                            // Advertir si duración >16 horas
                            if (parte.DuracionMin > 960) // 16 * 60
                            {
                                errorDetails.Add($"DuracionMin sospechosa: {parte.DuracionMin} min ({parte.DuracionMin/60.0:F2}h)");
                                hasError = true;
                            }
                            
                            // Convertir minutos a fracción de día (1 día = 1440 minutos)
                            worksheet.Cell(row, 5).Value = parte.DuracionMin / 1440.0;
                            worksheet.Cell(row, 5).Style.NumberFormat.Format = "[h]:mm:ss";
                            rowsWithFallbackDuration++;
                        }
                        else
                        {
                            // Sin duración disponible - dejar celda vacía
                            errorDetails.Add("Sin duración disponible (horas y minutos faltantes)");
                            hasError = true;
                        }
                    }

                    // TAREA = Acción
                    worksheet.Cell(row, 6).Value = parte.Accion ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(parte.Accion))
                    {
                        errorDetails.Add("Tarea vacía");
                    }

                    // GRUPO
                    worksheet.Cell(row, 7).Value = parte.Grupo ?? string.Empty;

                    // TIPO
                    worksheet.Cell(row, 8).Value = parte.Tipo ?? string.Empty;
                    
                    // ✅ LOG: Registrar advertencias para esta fila
                    if (hasError || errorDetails.Any())
                    {
                        rowsWithErrors++;
                        App.Log?.LogWarning("⚠️ Fila {row} - Parte ID {id}: {errors}", 
                            row, parte.Id, string.Join("; ", errorDetails));
                    }

                    row++;
                }

                int lastDataRow = row - 1;
                
                App.Log?.LogDebug("✅ Datos escritos ({count} filas)", listaPartes.Count);
                
                // ✅ LOG: Resumen de validación
                if (rowsWithErrors > 0)
                {
                    App.Log?.LogWarning("⚠️ VALIDACIÓN: {errors} filas con advertencias/errores", rowsWithErrors);
                }
                if (rowsWithMissingTime > 0)
                {
                    App.Log?.LogWarning("⚠️ VALIDACIÓN: {count} valores de hora faltantes o inválidos", rowsWithMissingTime);
                }
                if (rowsWithFallbackDuration > 0)
                {
                    App.Log?.LogInformation("ℹ️ VALIDACIÓN: {count} filas usan DuracionMin (fallback)", rowsWithFallbackDuration);
                }
                if (rowsWithErrors == 0 && rowsWithMissingTime == 0)
                {
                    App.Log?.LogInformation("✅ VALIDACIÓN: Todos los datos son correctos");
                }

                cancellationToken.ThrowIfCancellationRequested();

                // ✅ FILA DE TOTAL
                if (listaPartes.Any())
                {
                    int totalRow = row;
                    
                    // Etiqueta "TOTAL"
                    worksheet.Cell(totalRow, 1).Value = "TOTAL";
                    worksheet.Cell(totalRow, 1).Style.Font.Bold = true;
                    worksheet.Cell(totalRow, 1).Style.Font.FontSize = 12;
                    
                    // Fórmula SUM en columna DURACION
                    worksheet.Cell(totalRow, 5).FormulaA1 = $"=SUM(E{firstDataRow}:E{lastDataRow})";
                    worksheet.Cell(totalRow, 5).Style.NumberFormat.Format = "[h]:mm:ss";
                    worksheet.Cell(totalRow, 5).Style.Font.Bold = true;
                    worksheet.Cell(totalRow, 5).Style.Font.FontSize = 12;
                    worksheet.Cell(totalRow, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#E0F2F1"); // Teal 50
                    
                    App.Log?.LogDebug("✅ Fila TOTAL añadida (fila {row})", totalRow);
                    
                    row++; // Incrementar para incluir fila total en rangos
                }

                // ✅ FORMATO PROFESIONAL
                
                // Autofiltro en encabezados (solo hasta la última fila de datos, sin incluir TOTAL)
                var autoFilterRange = worksheet.Range(1, 1, lastDataRow, headers.Length);
                autoFilterRange.SetAutoFilter();
                App.Log?.LogDebug("✅ Autofiltro aplicado");

                // Ajustar ancho de columnas automáticamente
                worksheet.Columns().AdjustToContents();
                App.Log?.LogDebug("✅ Columnas autoajustadas");

                // Congelar primera fila (encabezados)
                worksheet.SheetView.FreezeRows(1);
                App.Log?.LogDebug("✅ Primera fila congelada");

                // Bordes en toda la tabla (incluyendo fila TOTAL)
                var dataRange = worksheet.Range(1, 1, row - 1, headers.Length);
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                App.Log?.LogDebug("✅ Bordes aplicados");
                
                // ✅ CONFIGURAR WORKBOOK PARA AUTO-CÁLCULO
                workbook.CalculateMode = XLCalculateMode.Auto;
                workbook.RecalculateAllFormulas();
                App.Log?.LogDebug("✅ Workbook configurado para auto-cálculo");

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

    /// <summary>Convierte un string de hora (HH:mm o HH:mm:ss) a un valor numérico de Excel (fracción de día).</summary>
    private static double? ParseTimeToExcelValue(string? horaStr)
    {
        if (string.IsNullOrWhiteSpace(horaStr))
            return null;

        try
        {
            // Intentar parsear como TimeSpan
            if (TimeSpan.TryParse(horaStr, out var timeSpan))
            {
                // Validar que esté en rango válido (0-24 horas)
                if (timeSpan.TotalHours < 0 || timeSpan.TotalHours >= 24)
                {
                    App.Log?.LogWarning("⚠️ Hora fuera de rango (0-24h): '{hora}' = {hours}h", 
                        horaStr, timeSpan.TotalHours);
                    
                    // Normalizar al rango 0-24h
                    var normalizedHours = ((timeSpan.TotalHours % 24) + 24) % 24;
                    return normalizedHours / 24.0;
                }
                
                // Convertir TimeSpan a fracción de día (1 día = 24 horas)
                // Excel representa tiempos como fracciones: 0.5 = 12:00:00
                return timeSpan.TotalDays;
            }
            else
            {
                App.Log?.LogWarning("⚠️ Formato de hora inválido: '{hora}'", horaStr);
            }
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error parseando hora: '{hora}'", horaStr);
        }

        return null;
    }
}


