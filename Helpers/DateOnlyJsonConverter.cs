using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GestionTime.Desktop.Helpers;

/// <summary>
/// Converter personalizado para manejar fechas como "solo fecha" (sin hora) desde la API
/// Previene problemas de zona horaria cuando la API devuelve "2026-01-02T00:00:00Z" o "2026-01-02T00:00:00"
/// </summary>
public class DateOnlyJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dateString = reader.GetString();
        
        if (string.IsNullOrWhiteSpace(dateString))
        {
            return default;
        }

        // ✅ SOLUCIÓN: Extraer solo la parte de fecha (yyyy-MM-dd) directamente del string
        // Esto evita CUALQUIER conversión de zona horaria
        if (dateString.Length >= 10)
        {
            var datePart = dateString.Substring(0, 10); // "2026-01-01"
            
            // Parsear solo la fecha sin información de hora/zona
            if (DateTime.TryParseExact(datePart, "yyyy-MM-dd", CultureInfo.InvariantCulture, 
                DateTimeStyles.None, out var parsedDate))
            {
                // Retornar con DateTimeKind.Unspecified para evitar conversiones
                return DateTime.SpecifyKind(parsedDate, DateTimeKind.Unspecified);
            }
        }

        // Fallback: intentar parsear normalmente
        if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
        {
            return DateTime.SpecifyKind(dateTime.Date, DateTimeKind.Unspecified);
        }

        // Último fallback
        return default;
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // Escribir solo la fecha en formato ISO 8601 (yyyy-MM-dd)
        writer.WriteStringValue(value.ToString("yyyy-MM-dd"));
    }
}
