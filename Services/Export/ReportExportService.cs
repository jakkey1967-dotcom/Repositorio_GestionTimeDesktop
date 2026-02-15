using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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

            // Desglose semanal (si hay datos)
            if (data.DayBreakdown.Count > 0)
            {
                row += 2;
                ws.Cell(row, 1).Value = "Desglose Semanal";
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 1).Style.Font.FontSize = 14;
                ws.Range(row, 1, row, 4).Merge();

                if (!string.IsNullOrEmpty(data.WeekTotalHours))
                {
                    row++;
                    ws.Cell(row, 1).Value = $"Total semana: {data.WeekTotalHours}";
                    ws.Cell(row, 1).Style.Font.FontSize = 12;
                    ws.Range(row, 1, row, 4).Merge();
                }

                row++;
                string[] dayHeaders = { "Día", "Horas", "% Obj. 8h", "% Semanal" };
                for (int i = 0; i < dayHeaders.Length; i++)
                {
                    var cell = ws.Cell(row, i + 1);
                    cell.Value = dayHeaders[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0F766E");
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                int dayStartRow = row;
                foreach (var day in data.DayBreakdown)
                {
                    row++;
                    ws.Cell(row, 1).Value = day.Day;
                    ws.Cell(row, 2).Value = day.Hours;
                    ws.Cell(row, 3).Value = $"{day.Percent8h}%";
                    ws.Cell(row, 4).Value = $"{day.WeeklyPercent}%";

                    ws.Cell(row, 3).Style.Font.FontColor = day.Percent8h >= 100
                        ? XLColor.FromHtml("#10B981")
                        : XLColor.FromHtml("#F59E0B");
                }

                var dayRange = ws.Range(dayStartRow, 1, row, 4);
                dayRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dayRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
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

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    // Header
                    page.Header().Column(col =>
                    {
                        col.Item().Text(data.Title).FontSize(20).SemiBold().FontColor("#0F766E");
                        col.Item().PaddingTop(4).Text(data.ScopeText).FontSize(13);
                        col.Item().Text(data.AgentText).FontSize(12);
                        col.Item().Text($"Generado: {data.GeneratedAt}").FontSize(9).FontColor("#888888");
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

                        // Desglose semanal
                        if (data.DayBreakdown.Count > 0)
                        {
                            col.Item().PaddingTop(20).Text("Desglose Semanal").FontSize(14).SemiBold();

                            if (!string.IsNullOrEmpty(data.WeekTotalHours))
                            {
                                col.Item().PaddingTop(4).Text($"Total semana: {data.WeekTotalHours}")
                                    .FontSize(12).FontColor("#0F766E").SemiBold();
                            }

                            col.Item().PaddingTop(8).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background("#0F766E").Padding(6).Text("Día").FontColor("#FFFFFF").SemiBold();
                                    header.Cell().Background("#0F766E").Padding(6).Text("Horas").FontColor("#FFFFFF").SemiBold();
                                    header.Cell().Background("#0F766E").Padding(6).Text("% Obj. 8h").FontColor("#FFFFFF").SemiBold();
                                    header.Cell().Background("#0F766E").Padding(6).Text("% Semanal").FontColor("#FFFFFF").SemiBold();
                                });

                                foreach (var day in data.DayBreakdown)
                                {
                                    table.Cell().BorderBottom(1).BorderColor("#EEEEEE").Padding(5).Text(day.Day);
                                    table.Cell().BorderBottom(1).BorderColor("#EEEEEE").Padding(5).Text(day.Hours);

                                    var pctColor = day.Percent8h >= 100 ? "#10B981" : "#F59E0B";
                                    table.Cell().BorderBottom(1).BorderColor("#EEEEEE").Padding(5)
                                        .Text($"{day.Percent8h}%").FontColor(pctColor).SemiBold();

                                    table.Cell().BorderBottom(1).BorderColor("#EEEEEE").Padding(5)
                                        .Text($"{day.WeeklyPercent}%");
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
}
