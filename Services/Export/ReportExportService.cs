using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;

namespace GestionTime.Desktop.Services.Export;

/// <summary>Datos del informe para exportación.</summary>
public class ReportExportData
{
    public string Title { get; set; } = "Informe GestionTime";
    public string ScopeText { get; set; } = string.Empty;
    public string AgentText { get; set; } = string.Empty;
    public string GeneratedAt { get; set; } = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
    public int PartsCount { get; set; }
    public string RecordedTime { get; set; } = string.Empty;
    public string CoveredTime { get; set; } = string.Empty;
    public string OverlapTime { get; set; } = string.Empty;
    public string FirstStart { get; set; } = "-";
    public string LastEnd { get; set; } = "-";
    public string StatusMessage { get; set; } = string.Empty;
    public string WeekTotalHours { get; set; } = string.Empty;
    public string TotalHeaderText { get; set; } = string.Empty;
    public List<DayExportRow> DayBreakdown { get; set; } = new();
}

/// <summary>Fila de desglose diario para exportación.</summary>
public class DayExportRow
{
    public string Day { get; set; } = string.Empty;
    public string Hours { get; set; } = string.Empty;
    public int Percent8h { get; set; }
    public int WeeklyPercent { get; set; }
}

/// <summary>Servicio de exportación de informes (Excel + PDF + Email).</summary>
public sealed class ReportExportService
{
    // GT-BEGIN: Excel export
    /// <summary>Exporta resumen de informe a Excel (.xlsx).</summary>
    public async Task ExportToExcelAsync(ReportExportData data, string filePath, CancellationToken ct = default)
    {
        await Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Resumen");

            // Título
            ws.Cell(1, 1).Value = data.Title;
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 16;
            ws.Range(1, 1, 1, 4).Merge();

            ws.Cell(2, 1).Value = data.ScopeText;
            ws.Cell(2, 1).Style.Font.FontSize = 12;
            ws.Range(2, 1, 2, 4).Merge();

            ws.Cell(3, 1).Value = data.AgentText;
            ws.Cell(3, 1).Style.Font.FontSize = 12;
            ws.Range(3, 1, 3, 4).Merge();

            ws.Cell(4, 1).Value = $"Generado: {data.GeneratedAt}";
            ws.Cell(4, 1).Style.Font.FontSize = 10;
            ws.Cell(4, 1).Style.Font.FontColor = XLColor.Gray;
            ws.Range(4, 1, 4, 4).Merge();

            // Tabla de resumen
            int row = 6;
            string[] headers = { "Métrica", "Valor" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(row, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0F766E");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            var metrics = new (string Label, string Value)[]
            {
                ("Partes", data.PartsCount.ToString()),
                ("Registrado", data.RecordedTime),
                ("Real (sin solape)", data.CoveredTime),
                ("Solape", data.OverlapTime),
                ("Inicio", data.FirstStart),
                ("Fin", data.LastEnd),
                ("Estado", data.StatusMessage)
            };

            foreach (var (label, value) in metrics)
            {
                row++;
                ws.Cell(row, 1).Value = label;
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 2).Value = value;
            }

            var summaryRange = ws.Range(6, 1, row, 2);
            summaryRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            summaryRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Desglose semanal con gráfica de barras (si hay datos)
            if (data.DayBreakdown.Count > 0)
            {
                row += 2;
                ws.Cell(row, 1).Value = "📊 Horas por día (Lun-Sáb)";
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 1).Style.Font.FontSize = 14;
                ws.Range(row, 1, row, 3).Merge();

                if (!string.IsNullOrEmpty(data.WeekTotalHours))
                {
                    ws.Cell(row, 4).Value = $"{data.TotalHeaderText} {data.WeekTotalHours}";
                    ws.Cell(row, 4).Style.Font.Bold = true;
                    ws.Cell(row, 4).Style.Font.FontColor = XLColor.FromHtml("#0F766E");
                    ws.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    ws.Range(row, 4, row, 6).Merge();
                }

                row++;
                ws.Cell(row, 1).Value = data.AgentText;
                ws.Cell(row, 1).Style.Font.FontSize = 10;
                ws.Range(row, 1, row, 2).Merge();
                ws.Cell(row, 3).Value = $"Inicio: {data.FirstStart}";
                ws.Cell(row, 3).Style.Font.FontSize = 10;
                ws.Cell(row, 5).Value = $"Fin: {data.LastEnd}";
                ws.Cell(row, 5).Style.Font.FontSize = 10;

                // Gráfica de barras visual
                row++;
                string[] chartHeaders = { "Día", "Barra", "", "% 8h", "Horas", "Semanal" };
                for (int i = 0; i < chartHeaders.Length; i++)
                {
                    var cell = ws.Cell(row, i + 1);
                    cell.Value = chartHeaders[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E293B");
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                int chartStartRow = row;
                ws.Column(2).Width = 30;
                ws.Column(3).Width = 10;

                foreach (var day in data.DayBreakdown)
                {
                    row++;
                    var barColor = day.Percent8h >= 100
                        ? XLColor.FromHtml("#10B981")
                        : XLColor.FromHtml("#F59E0B");

                    ws.Cell(row, 1).Value = day.Day;
                    ws.Cell(row, 1).Style.Font.Bold = true;
                    ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Barra visual: relleno proporcional al % (columnas 2-3 mergeadas)
                    var barCell = ws.Cell(row, 2);
                    var pctCapped = Math.Min(day.Percent8h, 120);
                    barCell.Value = new string('█', Math.Max(1, pctCapped / 4));
                    barCell.Style.Font.FontColor = barColor;
                    barCell.Style.Font.FontSize = 14;
                    barCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                    ws.Cell(row, 3).Value = $"{day.Percent8h}%";
                    ws.Cell(row, 3).Style.Font.Bold = true;
                    ws.Cell(row, 3).Style.Font.FontColor = barColor;
                    ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Cell(row, 4).Value = $"{day.Percent8h}%";
                    ws.Cell(row, 4).Style.Font.FontColor = barColor;
                    ws.Cell(row, 4).Style.Font.Bold = true;
                    ws.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Cell(row, 5).Value = day.Hours;
                    ws.Cell(row, 5).Style.Font.Bold = true;
                    ws.Cell(row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                    ws.Cell(row, 6).Value = $"{day.WeeklyPercent}%";
                    ws.Cell(row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                    // Fondo oscuro para simular tema dark
                    var rowRange = ws.Range(row, 1, row, 6);
                    rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E293B");
                    rowRange.Style.Font.FontColor = rowRange.Style.Font.FontColor == XLColor.Black
                        ? XLColor.White : rowRange.Style.Font.FontColor;
                    // Restaurar colores específicos que ya se setearon
                    ws.Cell(row, 1).Style.Font.FontColor = XLColor.FromHtml("#94A3B8");
                    ws.Cell(row, 5).Style.Font.FontColor = XLColor.White;
                    ws.Cell(row, 6).Style.Font.FontColor = XLColor.FromHtml("#94A3B8");
                }

                var chartRange = ws.Range(chartStartRow, 1, row, 6);
                chartRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                chartRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                chartRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#334155");
                chartRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#334155");

                ws.Column(1).AdjustToContents();
                ws.Column(4).AdjustToContents();
                ws.Column(5).AdjustToContents();
                ws.Column(6).AdjustToContents();
            }

            ws.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        }, ct);
    }
    // GT-END

    // GT-BEGIN: PDF export
    /// <summary>Exporta resumen de informe a PDF.</summary>
    public async Task ExportToPdfAsync(ReportExportData data, string filePath, CancellationToken ct = default)
    {
        await Task.Run(() =>
        {
            QuestPDF.Settings.License = LicenseType.Community;

            // GT-BEGIN: Cargar logo de la app para el header del PDF
            byte[]? logoBytes = null;
            try
            {
                var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "LogoOscuro.png");
                if (File.Exists(logoPath))
                {
                    var rawBytes = File.ReadAllBytes(logoPath);
                    logoBytes = ApplyRoundedCorners(rawBytes);
                }
            }
            catch { /* Logo opcional, no bloquea exportación */ }
            // GT-END

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    // Header con logo + info
                    page.Header().Column(col =>
                    {
                        col.Item().Row(headerRow =>
                        {
                            if (logoBytes != null)
                            {
                                headerRow.AutoItem().AlignMiddle().Width(90).Image(logoBytes);
                                headerRow.AutoItem().PaddingLeft(12);
                            }
                            headerRow.RelativeItem().Column(titleCol =>
                            {
                                titleCol.Item().Text(data.Title).FontSize(20).SemiBold().FontColor("#0F766E");
                                titleCol.Item().PaddingTop(4).Text(data.ScopeText).FontSize(13);
                                titleCol.Item().Text(data.AgentText).FontSize(12);
                                titleCol.Item().Text($"Generado: {data.GeneratedAt}").FontSize(9).FontColor("#888888");
                            });
                        });
                        col.Item().PaddingTop(8).LineHorizontal(1).LineColor("#CCCCCC");
                    });

                    // Content
                    page.Content().PaddingTop(10).Column(col =>
                    {
                        col.Item().Text("Resumen").FontSize(14).SemiBold();

                        // Tabla de métricas
                        col.Item().PaddingTop(8).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(3);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background("#0F766E").Padding(6)
                                    .Text("Métrica").FontColor("#FFFFFF").SemiBold();
                                header.Cell().Background("#0F766E").Padding(6)
                                    .Text("Valor").FontColor("#FFFFFF").SemiBold();
                            });

                            void AddRow(string label, string value)
                            {
                                table.Cell().BorderBottom(1).BorderColor("#EEEEEE").Padding(5)
                                    .Text(label).SemiBold();
                                table.Cell().BorderBottom(1).BorderColor("#EEEEEE").Padding(5)
                                    .Text(value);
                            }

                            AddRow("Partes", data.PartsCount.ToString());
                            AddRow("Registrado", data.RecordedTime);
                            AddRow("Real (sin solape)", data.CoveredTime);
                            AddRow("Solape", data.OverlapTime);
                            AddRow("Inicio", data.FirstStart);
                            AddRow("Fin", data.LastEnd);
                            AddRow("Estado", data.StatusMessage);
                        });

                        // Gráfica semanal visual (barras horizontales)
                        if (data.DayBreakdown.Count > 0)
                        {
                            col.Item().PaddingTop(20).Background("#1E293B").Padding(16).Column(chart =>
                            {
                                // Título + Total
                                chart.Item().Row(titleRow =>
                                {
                                    titleRow.RelativeItem().Text("📊 Horas por día (Lun-Sáb)")
                                        .FontSize(14).SemiBold().FontColor("#F1F5F9");
                                    titleRow.AutoItem().Text(text =>
                                    {
                                        text.Span(data.TotalHeaderText + " ").FontSize(10).FontColor("#94A3B8");
                                        text.Span(data.WeekTotalHours).FontSize(12).Bold().FontColor("#0F766E");
                                    });
                                });

                                // Agente + Inicio/Fin
                                chart.Item().PaddingTop(6).Row(infoRow =>
                                {
                                    infoRow.AutoItem().Text($"👤 {data.AgentText}").FontSize(9).FontColor("#CBD5E1");
                                    infoRow.AutoItem().PaddingLeft(20).Text($"🕒 Inicio: {data.FirstStart}")
                                        .FontSize(9).FontColor("#94A3B8");
                                    infoRow.AutoItem().PaddingLeft(20).Text($"🏁 Fin: {data.LastEnd}")
                                        .FontSize(9).FontColor("#94A3B8");
                                });

                                chart.Item().PaddingTop(12);

                                // Barras por día
                                foreach (var day in data.DayBreakdown)
                                {
                                    var barColor = day.Percent8h >= 100 ? "#10B981" : "#F59E0B";
                                    var barWidth = Math.Min(day.Percent8h, 120);

                                    chart.Item().PaddingBottom(6).Row(barRow =>
                                    {
                                        // Etiqueta día
                                        barRow.ConstantItem(35).AlignMiddle()
                                            .Text(day.Day).FontSize(10).SemiBold().FontColor("#94A3B8");

                                        // Barra con % superpuesto
                                        barRow.RelativeItem().Height(24).Layers(layers =>
                                        {
                                            // Fondo barra
                                            layers.Layer().Background("#334155");

                                            // Barra coloreada proporcional
                                            layers.Layer().Row(inner =>
                                            {
                                                inner.RelativeItem(barWidth).Background(barColor);
                                                inner.RelativeItem(Math.Max(1, 120 - barWidth));
                                            });

                                            // Texto % centrado en la barra
                                            layers.PrimaryLayer().AlignCenter().AlignMiddle()
                                                .Text($"{day.Percent8h}%").FontSize(10).Bold().FontColor("#FFFFFF");
                                        });

                                        // Horas + (%) 
                                        barRow.ConstantItem(120).AlignMiddle().AlignRight()
                                            .Text(text =>
                                            {
                                                text.Span(day.Hours).FontSize(10).SemiBold().FontColor("#F1F5F9");
                                                text.Span($" ({day.Percent8h}%)").FontSize(9).FontColor("#94A3B8");
                                            });

                                        // % semanal
                                        barRow.ConstantItem(40).AlignMiddle().AlignRight()
                                            .Text($"{day.WeeklyPercent}%").FontSize(10).Bold().FontColor("#64748B");
                                    });
                                }
                            });
                        }
                    });

                    // Footer
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("GestionTime — ").FontSize(9).FontColor("#999999");
                        text.Span("Página ").FontSize(9).FontColor("#999999");
                        text.CurrentPageNumber().FontSize(9).FontColor("#999999");
                    });
                });
            }).GeneratePdf(filePath);
        }, ct);
    }
    // GT-END

    // GT-BEGIN: Email body builder
    /// <summary>Genera cuerpo de texto plano para email.</summary>
    public string BuildEmailBody(ReportExportData data)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(data.Title);
        sb.AppendLine(new string('=', 40));
        sb.AppendLine();
        sb.AppendLine(data.ScopeText);
        sb.AppendLine(data.AgentText);
        sb.AppendLine($"Generado: {data.GeneratedAt}");
        sb.AppendLine();
        sb.AppendLine("--- Resumen ---");
        sb.AppendLine($"Partes: {data.PartsCount}");
        sb.AppendLine($"Registrado: {data.RecordedTime}");
        sb.AppendLine($"Real (sin solape): {data.CoveredTime}");
        sb.AppendLine($"Solape: {data.OverlapTime}");
        sb.AppendLine($"Inicio: {data.FirstStart}");
        sb.AppendLine($"Fin: {data.LastEnd}");
        sb.AppendLine($"Estado: {data.StatusMessage}");

        if (data.DayBreakdown.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("--- Desglose Semanal ---");
            if (!string.IsNullOrEmpty(data.WeekTotalHours))
                sb.AppendLine($"Total semana: {data.WeekTotalHours}");
            sb.AppendLine();

            foreach (var day in data.DayBreakdown)
            {
                sb.AppendLine($"{day.Day}: {day.Hours} ({day.Percent8h}% obj 8h, {day.WeeklyPercent}% semanal)");
            }
        }

        return sb.ToString();
    }
    // GT-END

    // GT-BEGIN: Logo con esquinas redondeadas para PDF
    /// <summary>Aplica esquinas redondeadas a una imagen usando SkiaSharp.</summary>
    private static byte[] ApplyRoundedCorners(byte[] imageBytes)
    {
        using var original = SKBitmap.Decode(imageBytes);
        var w = original.Width;
        var h = original.Height;
        var radius = Math.Min(w, h) * 0.12f;

        var info = new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var rect = new SKRect(0, 0, w, h);
        var rrect = new SKRoundRect(rect, radius, radius);
        canvas.ClipRoundRect(rrect, SKClipOperation.Intersect, true);
        canvas.DrawBitmap(original, 0, 0);

        using var snap = surface.Snapshot();
        using var data = snap.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
    // GT-END
}
