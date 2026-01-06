using ExcelDataReader;
using GestionTime.Desktop.Models.Dtos;
using GestionTime.Desktop.Helpers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GestionTime.Desktop.Services.Import;

/// <summary>Servicio para leer e importar partes desde archivos Excel (.xls/.xlsx).</summary>
public sealed class ExcelPartesImportService
{
    private readonly CatalogManager _catalogManager;

    public ExcelPartesImportService()
    {
        _catalogManager = new CatalogManager();
    }

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

            // 🆕 NUEVO: Cargar catálogos para búsqueda de cliente, grupo y tipo
            logger?.LogInformation("📚 Cargando catálogos...");
            await _catalogManager.LoadGruposAsync();
            await _catalogManager.LoadTiposAsync();
            
            // Cargar clientes desde API
            await LoadClientesAsync(logger);
            logger?.LogInformation("✅ Catálogos cargados correctamente");

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

    // 🆕 NUEVO: Lista de clientes cargada desde API
    private List<ClienteResponse>? _clientesCache = null;

    /// <summary>Carga catálogo de clientes desde API.</summary>
    private async Task LoadClientesAsync(ILogger? logger)
    {
        try
        {
            var path = "/api/v1/catalog/clientes?limit=500&offset=0";
            logger?.LogDebug("🔄 Cargando clientes desde {path}", path);
            
            var response = await App.Api.GetAsync<ClienteResponse[]>(path, CancellationToken.None);
            
            if (response != null)
            {
                _clientesCache = response.ToList();
                logger?.LogInformation("✅ {count} clientes cargados", _clientesCache.Count);
            }
            else
            {
                logger?.LogWarning("⚠️ API devolvió null para clientes");
                _clientesCache = new List<ClienteResponse>();
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "❌ Error cargando catálogo de clientes");
            _clientesCache = new List<ClienteResponse>();
        }
    }

    /// <summary>Mapea una fila de Excel a ParteCreateRequest.</summary>
    private ParteCreateRequest MapRowToParte(DataRow row, DataTable table, int rowIndex, ILogger? logger)
    {
        // Mapeo de columnas (case-insensitive) con ALIAS ALTERNATIVOS
        var fecha = GetCellValue(row, table, "Fecha", "FECHA");
        var cliente = GetCellValue(row, table, "Cliente", "PROYECTO", "cliente");  // ✅ NUEVO: Alias "PROYECTO"
        var tienda = GetCellValue(row, table, "Tienda", "tienda");
        var accion = GetCellValue(row, table, "Accion", "Acción", "TAREA", "Tarea");  // ✅ NUEVO: Alias "TAREA"
        var horaInicio = GetCellValue(row, table, "HoraInicio", "Hora Inicio", "Inicio", "HORA INICIO", "HORA_INICIO");
        var horaFin = GetCellValue(row, table, "HoraFin", "Hora Fin", "Fin", "HORA FIN", "HORA_FIN");
        var duracionMin = GetCellValue(row, table, "Duracion_min", "Duracion", "Duración", "DURACION");
        var ticket = GetCellValue(row, table, "Ticket", "ticket");
        var grupo = GetCellValue(row, table, "Grupo", "GRUPO", "grupo");
        var tipo = GetCellValue(row, table, "Tipo", "TIPO", "tipo");
        var tecnico = GetCellValue(row, table, "Tecnico", "Técnico", "tecnico");
        // 🔒 NOTA: No se lee el campo "Estado" del Excel, siempre será Cerrado (2)

        // 🆕 NUEVO: Log detallado de valores leídos para debug
        logger?.LogDebug("═══ Fila {row} - Valores leídos ═══", rowIndex);
        logger?.LogDebug("  Fecha: '{value}'", fecha ?? "(null)");
        logger?.LogDebug("  Cliente/Proyecto: '{value}'", cliente ?? "(null)");
        logger?.LogDebug("  Tienda: '{value}'", tienda ?? "(null)");
        logger?.LogDebug("  Accion/Tarea: '{value}'", accion ?? "(null)");
        logger?.LogDebug("  HoraInicio: '{value}'", horaInicio ?? "(null)");
        logger?.LogDebug("  HoraFin: '{value}'", horaFin ?? "(null)");
        logger?.LogDebug("  Ticket: '{value}'", ticket ?? "(null)");
        logger?.LogDebug("  Grupo: '{value}'", grupo ?? "(null)");
        logger?.LogDebug("  Tipo: '{value}'", tipo ?? "(null)");
        logger?.LogDebug("  Estado: FORZADO → Cerrado (2)");

        // Validar campos requeridos
        if (string.IsNullOrWhiteSpace(fecha))
            throw new Exception("Fecha vacía");
        if (string.IsNullOrWhiteSpace(cliente))
            throw new Exception("Cliente/Proyecto vacío");
        if (string.IsNullOrWhiteSpace(accion))
            throw new Exception("Acción/Tarea vacía");
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

        // ✅ MEJORADO: Calcular duración SIEMPRE desde horas
        int? duracionMinutos = CalcularDuracion(horaInicioStr, horaFinStr, logger);
        
        // Si viene duración en Excel, validar contra calculado
        if (!string.IsNullOrWhiteSpace(duracionMin) && int.TryParse(duracionMin, out var durExcel))
        {
            logger?.LogDebug("Fila {row}: Duración Excel={excel}min vs Calculada={calc}min", 
                rowIndex, durExcel, duracionMinutos);
            
            // Usar calculada (más confiable)
            duracionMinutos = duracionMinutos ?? durExcel;
        }

        // Estado: mapear texto a int (1=Abierto, 2=Cerrado, 3=Pausado)
        // 🔒 FORZADO: SIEMPRE Estado = 2 (Cerrado) para importación Excel
        int estadoInt = 2; // FIJO: Todos los partes importados son CERRADOS

        // ✅ MEJORADO: Buscar cliente por nombre en catálogo
        int clienteId = BuscarClienteId(cliente, logger);
        if (clienteId == 0)
        {
            throw new Exception($"Cliente '{cliente}' no encontrado en catálogo");
        }

        return new ParteCreateRequest
        {
            FechaTrabajo = fechaDate.ToString("yyyy-MM-dd"),
            HoraInicio = horaInicioStr,
            HoraFin = horaFinStr,
            DuracionMin = duracionMinutos,
            IdCliente = clienteId, // ✅ CORREGIDO: ID real desde catálogo
            Tienda = tienda,
            IdGrupo = BuscarGrupoId(grupo, logger),
            IdTipo = BuscarTipoId(tipo, logger),
            Accion = accion?.Trim() ?? "",
            Ticket = ticket?.Trim(),
            Tecnico = tecnico?.Trim(),
            Estado = 2  // 🔒 FORZADO: SIEMPRE Cerrado (2)
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
                
                // ✅ CORREGIDO: Solo formatear como fecha si es el campo "Fecha"
                if (val is DateTime dt)
                {
                    // Si es una columna de fecha, devolver solo la fecha
                    if (name.Equals("Fecha", StringComparison.OrdinalIgnoreCase) || 
                        name.Equals("FECHA", StringComparison.OrdinalIgnoreCase))
                    {
                        return dt.ToString("yyyy-MM-dd");
                    }
                    
                    // Si es una columna de hora, devolver el DateTime completo como string
                    // para que TryParseTime lo procese correctamente
                    return dt.ToString("yyyy-MM-dd HH:mm:ss");
                }
                
                return val?.ToString()?.Trim();
            }
        }
        return null;
    }

    /// <summary>Intenta parsear hora en formato HH:mm, TimeSpan o DateTime completo.</summary>
    private bool TryParseTime(string input, out string result)
    {
        result = string.Empty;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        // 🆕 NUEVO: Intentar parsear como DateTime completo (ej: "31/12/1899 8:30:00")
        if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
        {
            // Extraer solo la parte de hora
            result = $"{dateTime.Hour:D2}:{dateTime.Minute:D2}";
            return true;
        }

        // Intentar parsear como TimeSpan
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
                // Validar rangos
                if (h >= 0 && h <= 23 && m >= 0 && m <= 59)
                {
                    result = $"{h:D2}:{m:D2}";
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>🆕 NUEVO: Calcula duración en minutos entre dos horas HH:mm.</summary>
    private int? CalcularDuracion(string horaInicio, string? horaFin, ILogger? logger)
    {
        if (string.IsNullOrWhiteSpace(horaFin))
            return null;

        if (!TimeSpan.TryParse(horaInicio, out var inicio))
        {
            logger?.LogWarning("⚠️ No se pudo parsear hora inicio: {hora}", horaInicio);
            return null;
        }

        if (!TimeSpan.TryParse(horaFin, out var fin))
        {
            logger?.LogWarning("⚠️ No se pudo parsear hora fin: {hora}", horaFin);
            return null;
        }

        var duracion = (fin - inicio).TotalMinutes;
        
        // Si duración negativa, probablemente cruzó medianoche
        if (duracion < 0)
        {
            duracion += 24 * 60; // Añadir 24 horas
        }

        return (int)Math.Round(duracion);
    }

    /// <summary>✅ MEJORADO: Busca cliente por nombre en catálogo cargado.</summary>
    private int BuscarClienteId(string? cliente, ILogger? logger)
    {
        if (string.IsNullOrWhiteSpace(cliente))
            return 0;

        if (_clientesCache == null || !_clientesCache.Any())
        {
            logger?.LogWarning("⚠️ Catálogo de clientes no cargado");
            return 0;
        }

        var clienteNormalizado = NormalizarTexto(cliente.Trim());
        logger?.LogDebug("🔍 Buscando cliente normalizado: '{original}' → '{normalizado}'", cliente, clienteNormalizado);

        // Buscar por nombre exacto (normalizado, sin acentos ni mayúsculas)
        var clienteEncontrado = _clientesCache.FirstOrDefault(c => 
            string.Equals(NormalizarTexto(c.Nombre), clienteNormalizado, StringComparison.Ordinal));

        if (clienteEncontrado != null)
        {
            logger?.LogDebug("✅ Cliente '{nombre}' → ID={id}", cliente, clienteEncontrado.Id);
            return clienteEncontrado.Id;
        }

        // Buscar por coincidencia parcial (normalizado)
        clienteEncontrado = _clientesCache.FirstOrDefault(c => 
            NormalizarTexto(c.Nombre).Contains(clienteNormalizado, StringComparison.Ordinal));

        if (clienteEncontrado != null)
        {
            logger?.LogDebug("✅ Cliente '{nombre}' (parcial) → ID={id}", cliente, clienteEncontrado.Id);
            return clienteEncontrado.Id;
        }

        logger?.LogWarning("⚠️ Cliente '{nombre}' NO encontrado en catálogo", cliente);
        return 0;
    }
    
    /// <summary>Normaliza texto: elimina acentos, convierte a mayúsculas y elimina espacios duplicados.</summary>
    private static string NormalizarTexto(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return string.Empty;
        
        // 1. Convertir a mayúsculas
        var textoNormalizado = texto.ToUpperInvariant();
        
        // 2. Eliminar acentos
        textoNormalizado = RemoverAcentos(textoNormalizado);
        
        // 3. Eliminar espacios múltiples
        textoNormalizado = System.Text.RegularExpressions.Regex.Replace(textoNormalizado, @"\s+", " ");
        
        // 4. Trim final
        return textoNormalizado.Trim();
    }
    
    /// <summary>Elimina acentos y diacríticos de un texto.</summary>
    private static string RemoverAcentos(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return texto;
        
        // Normalizar a FormD (descomponer caracteres con acentos)
        var normalizedString = texto.Normalize(System.Text.NormalizationForm.FormD);
        var stringBuilder = new System.Text.StringBuilder();
        
        foreach (var c in normalizedString)
        {
            // Solo agregar caracteres que NO sean marcas de acento
            var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }
        
        // Re-normalizar a FormC (composición)
        return stringBuilder.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }

    /// <summary>✅ MEJORADO: Busca grupo por nombre en catálogo.</summary>
    private int? BuscarGrupoId(string? grupo, ILogger? logger)
    {
        if (string.IsNullOrWhiteSpace(grupo))
        {
            logger?.LogDebug("⚠️ Grupo vacío o null");
            return null;
        }

        logger?.LogDebug("🔍 Buscando grupo: '{nombre}'", grupo);
        
        var gruposTrimmed = grupo.Trim();
        logger?.LogDebug("   Trimmed: '{trimmed}'", gruposTrimmed);
        
        var grupoId = _catalogManager.GetGrupoId(gruposTrimmed);
        
        if (grupoId.HasValue)
        {
            logger?.LogInformation("✅ Grupo '{nombre}' → ID={id}", grupo, grupoId.Value);
        }
        else
        {
            // 🆕 NUEVO: Log más detallado cuando no se encuentra
            logger?.LogWarning("⚠️ Grupo '{nombre}' NO encontrado en catálogo", grupo);
            
            var todosGrupos = _catalogManager.GetAllGrupos();
            logger?.LogDebug("📋 Grupos disponibles en catálogo ({count}):", todosGrupos.Count);
            foreach (var g in todosGrupos.Take(10))
            {
                logger?.LogDebug("   - [{id}] '{nombre}'", g.Id_grupo, g.Nombre);
            }
        }

        return grupoId;
    }

    /// <summary>✅ MEJORADO: Busca tipo por nombre en catálogo.</summary>
    private int? BuscarTipoId(string? tipo, ILogger? logger)
    {
        if (string.IsNullOrWhiteSpace(tipo))
            return null;

        var tipoId = _catalogManager.GetTipoId(tipo.Trim());
        
        if (tipoId.HasValue)
        {
            logger?.LogDebug("✅ Tipo '{nombre}' → ID={id}", tipo, tipoId.Value);
        }
        else
        {
            logger?.LogDebug("⚠️ Tipo '{nombre}' no encontrado", tipo);
        }

        return tipoId;
    }

    /// <summary>Parsea cliente (DEPRECADO - usar BuscarClienteId).</summary>
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

    /// <summary>Parsea grupo (DEPRECADO - usar BuscarGrupoId).</summary>
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

    /// <summary>Parsea tipo (DEPRECADO - usar BuscarTipoId).</summary>
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
