using ExcelDataReader;
using GestionTime.Desktop.Models.Dtos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionTime.Desktop.Services.Import;

/// <summary>Servicio para leer e importar partes desde archivos Excel (.xls/.xlsx).</summary>
public sealed class ExcelPartesImportService
{
    /// <summary>Lee un archivo Excel y retorna partes válidos + errores.</summary>
    public async Task<ImportResult> ReadExcelAsync(string filePath, ILogger? logger = null)
    {
        var result = new ImportResult
        {
            FileName = Path.GetFileName(filePath)
        };

        try
        {
            // Registrar codificación para archivos antiguos .xls
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            logger?.LogInformation("═══════════════════════════════════════════════════════════════");
            logger?.LogInformation("📊 IMPORTACIÓN EXCEL - Iniciando");
            logger?.LogInformation("   Archivo: {file}", result.FileName);

            await Task.Run(() =>
            {
                using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var reader = ExcelReaderFactory.CreateReader(stream);
                var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
                {
                    ConfigureDataTable = _ => new ExcelDataTableConfiguration
                    {
                        UseHeaderRow = true // Primera fila = headers
                    }
                });

                if (dataSet.Tables.Count == 0)
                {
                    logger?.LogWarning("Excel sin hojas/tablas");
                    return;
                }

                var table = dataSet.Tables[0]!;
                result.TotalRows = table.Rows.Count;

                logger?.LogInformation("   Total filas: {total}", result.TotalRows);
                logger?.LogInformation("   Columnas detectadas: {cols}", string.Join(", ", GetColumnNames(table)));

                for (int i = 0; i < table.Rows.Count; i++)
                {
                    var row = table.Rows[i];
                    var rowIndex = i + 2; // +2 porque Excel empieza en 1 y header=1

                    try
                    {
                        var parte = MapRowToParte(row, table, rowIndex, logger);
                        result.ValidItems.Add(parte);
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add(new ImportError
                        {
                            RowIndex = rowIndex,
                            Reason = ex.Message,
                            RawData = GetRowDataAsString(row, table)
                        });
                        logger?.LogWarning("Fila {row}: {error}", rowIndex, ex.Message);
                    }
                }
            });

            logger?.LogInformation("✅ Lectura completada:");
            logger?.LogInformation("   • Válidos: {valid}", result.ValidItems.Count);
            logger?.LogInformation("   • Errores: {errors}", result.Errors.Count);
            logger?.LogInformation("═══════════════════════════════════════════════════════════════");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error leyendo Excel");
            throw;
        }

        return result;
    }

    /// <summary>Mapea una fila de Excel a ParteCreateRequest.</summary>
    private ParteCreateRequest MapRowToParte(DataRow row, DataTable table, int rowIndex, ILogger? logger)
    {
        // Mapeo de columnas (case-insensitive)
        var fecha = GetCellValue(row, table, "Fecha");
        var cliente = GetCellValue(row, table, "Cliente");
        var tienda = GetCellValue(row, table, "Tienda");
        var accion = GetCellValue(row, table, "Accion", "Acción");
        var horaInicio = GetCellValue(row, table, "HoraInicio", "Hora Inicio", "Inicio");
        var horaFin = GetCellValue(row, table, "HoraFin", "Hora Fin", "Fin");
        var duracionMin = GetCellValue(row, table, "Duracion_min", "Duracion", "Duración");
        var ticket = GetCellValue(row, table, "Ticket");
        var grupo = GetCellValue(row, table, "Grupo");
        var tipo = GetCellValue(row, table, "Tipo");
        var tecnico = GetCellValue(row, table, "Tecnico", "Técnico");
        var estado = GetCellValue(row, table, "Estado");

        // Validar campos requeridos
        if (string.IsNullOrWhiteSpace(fecha))
            throw new Exception("Fecha vacía");
        if (string.IsNullOrWhiteSpace(cliente))
            throw new Exception("Cliente vacío");
        if (string.IsNullOrWhiteSpace(accion))
            throw new Exception("Acción vacía");
        if (string.IsNullOrWhiteSpace(horaInicio))
            throw new Exception("Hora Inicio vacía");

        // Parsear fecha
        DateTime fechaDate;
        if (!DateTime.TryParse(fecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out fechaDate))
        {
            // Fallback: intentar parseDate dd/MM/yyyy
            if (!DateTime.TryParseExact(fecha, new[] { "dd/MM/yyyy", "yyyy-MM-dd", "d/M/yyyy" }, 
                CultureInfo.InvariantCulture, DateTimeStyles.None, out fechaDate))
            {
                throw new Exception($"Fecha inválida: {fecha}");
            }
        }

        // Parsear hora inicio
        if (!TryParseTime(horaInicio, out var horaInicioStr))
            throw new Exception($"Hora Inicio inválida: {horaInicio}");

        // Parsear hora fin (opcional)
        string? horaFinStr = null;
        if (!string.IsNullOrWhiteSpace(horaFin))
        {
            if (TryParseTime(horaFin, out var parsed))
                horaFinStr = parsed;
        }
        else
        {
            // Si HoraFin vacía: usar hora actual si es hoy, sino 18:00
            horaFinStr = fechaDate.Date == DateTime.Today
                ? DateTime.Now.ToString("HH:mm")
                : "18:00";
        }

        // Calcular duración si no viene
        int? duracionMinutos = null;
        if (!string.IsNullOrWhiteSpace(duracionMin) && int.TryParse(duracionMin, out var dur))
        {
            duracionMinutos = dur;
        }
        else if (horaFinStr != null)
        {
            // Calcular desde horas
            if (TimeSpan.TryParse(horaInicioStr, out var inicio) && TimeSpan.TryParse(horaFinStr, out var fin))
            {
                duracionMinutos = (int)(fin - inicio).TotalMinutes;
                if (duracionMinutos < 0) duracionMinutos = 0;
            }
        }

        // Estado: mapear texto a int (1=Abierto, 2=Cerrado, 3=Pausado)
        int estadoInt = 2; // Por defecto cerrado
        if (!string.IsNullOrWhiteSpace(estado))
        {
            if (estado.Contains("abierto", StringComparison.OrdinalIgnoreCase)) estadoInt = 1;
            else if (estado.Contains("pausado", StringComparison.OrdinalIgnoreCase)) estadoInt = 3;
            else if (int.TryParse(estado, out var est)) estadoInt = est;
        }

        return new ParteCreateRequest
        {
            FechaTrabajo = fechaDate.ToString("yyyy-MM-dd"),
            HoraInicio = horaInicioStr,
            HoraFin = horaFinStr,
            DuracionMin = duracionMinutos,
            IdCliente = ParseClienteId(cliente), // Simplificado: ID=1 si no hay
            Tienda = tienda,
            IdGrupo = ParseGrupoId(grupo),
            IdTipo = ParseTipoId(tipo),
            Accion = accion?.Trim() ?? "",
            Ticket = ticket?.Trim(),
            Tecnico = tecnico?.Trim(),
            Estado = estadoInt
        };
    }

    /// <summary>Obtiene el valor de una celda por nombre de columna (case-insensitive).</summary>
    private string? GetCellValue(DataRow row, DataTable table, params string[] possibleNames)
    {
        foreach (var name in possibleNames)
        {
            var col = table.Columns.Cast<DataColumn>()
                .FirstOrDefault(c => c.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (col != null && !row.IsNull(col))
            {
                var val = row[col];
                if (val is DateTime dt)
                    return dt.ToString("yyyy-MM-dd");
                return val?.ToString()?.Trim();
            }
        }
        return null;
    }

    /// <summary>Intenta parsear hora en formato HH:mm o TimeSpan.</summary>
    private bool TryParseTime(string input, out string result)
    {
        result = string.Empty;

        if (TimeSpan.TryParse(input, out var ts))
        {
            result = $"{ts.Hours:D2}:{ts.Minutes:D2}";
            return true;
        }

        // Intentar parseo directo HH:mm
        if (input.Contains(':') && input.Length >= 4)
        {
            var parts = input.Split(':');
            if (parts.Length >= 2 && int.TryParse(parts[0], out var h) && int.TryParse(parts[1], out var m))
            {
                result = $"{h:D2}:{m:D2}";
                return true;
            }
        }

        return false;
    }

    /// <summary>Parsea cliente (simplificado: retorna 1 siempre o implementa lookup).</summary>
    private int ParseClienteId(string? cliente)
    {
        // TODO: Implementar lookup en catálogo si existe
        // Por ahora, asumimos ID=1 o extraer número del texto
        if (string.IsNullOrWhiteSpace(cliente))
            return 1;

        // Si cliente viene como "123 - Nombre", extraer ID
        if (cliente.Contains("-"))
        {
            var parts = cliente.Split('-', 2);
            if (int.TryParse(parts[0].Trim(), out var id))
                return id;
        }

        return 1; // Fallback
    }

    /// <summary>Parsea grupo (simplificado).</summary>
    private int? ParseGrupoId(string? grupo)
    {
        if (string.IsNullOrWhiteSpace(grupo))
            return null;

        // Si grupo viene como "123 - Nombre", extraer ID
        if (grupo.Contains("-"))
        {
            var parts = grupo.Split('-', 2);
            if (int.TryParse(parts[0].Trim(), out var id))
                return id;
        }

        return null;
    }

    /// <summary>Parsea tipo (simplificado).</summary>
    private int? ParseTipoId(string? tipo)
    {
        if (string.IsNullOrWhiteSpace(tipo))
            return null;

        // Si tipo viene como "123 - Nombre", extraer ID
        if (tipo.Contains("-"))
        {
            var parts = tipo.Split('-', 2);
            if (int.TryParse(parts[0].Trim(), out var id))
                return id;
        }

        return null;
    }

    /// <summary>Obtiene nombres de columnas.</summary>
    private string[] GetColumnNames(DataTable table)
    {
        return table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();
    }

    /// <summary>Serializa una fila a string para debug.</summary>
    private string GetRowDataAsString(DataRow row, DataTable table)
    {
        var values = row.ItemArray.Select(v => v?.ToString() ?? "NULL");
        return string.Join(" | ", values);
    }
}
