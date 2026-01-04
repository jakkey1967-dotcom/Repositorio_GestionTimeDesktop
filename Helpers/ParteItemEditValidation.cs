using System;
using System.Linq;

namespace GestionTime.Desktop.Helpers;

/// <summary>
/// Helper para validación de campos de ParteItemEdit
/// Centraliza toda la lógica de validación y formateo
/// </summary>
public static class ParteItemEditValidation
{
    /// <summary>
    /// Normaliza y valida una hora en formato HH:mm
    /// </summary>
    /// <param name="value">Hora ingresada por el usuario</param>
    /// <returns>
    /// - string.Empty si está vacío
    /// - null si es inválido
    /// - "HH:mm" formateado si es válido
    /// </returns>
    public static string? NormalizeHora(string? value)
    {
        var txt = (value ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(txt))
            return string.Empty;

        // Mantener solo dígitos y limitar a 4
        var digits = new string(txt.Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
            return string.Empty;
        if (digits.Length < 4)
            return null; // incompleto
        if (digits.Length > 4)
            digits = digits[..4];

        var hh = digits[..2];
        var mm = digits[2..];
        
        if (!int.TryParse(hh, out var h) || h < 0 || h > 23)
            return null;
        if (!int.TryParse(mm, out var m) || m < 0 || m > 59)
            return null;

        return $"{h:00}:{m:00}";
    }
    
    /// <summary>
    /// Formatea automáticamente una hora mientras se escribe
    /// Inserta ":" automáticamente después de 2 dígitos
    /// </summary>
    public static (string formatted, int cursorPosition) FormatHoraWhileTyping(string original)
    {
        // Mantener solo dígitos y limitar a 4
        var allDigits = new string(original.Where(char.IsDigit).ToArray());
        if (allDigits.Length > 4)
            allDigits = allDigits[..4];

        string formatted;
        if (allDigits.Length == 0)
        {
            formatted = string.Empty;
        }
        else if (allDigits.Length <= 2)
        {
            formatted = allDigits;
        }
        else
        {
            // A partir de 3 dígitos, insertar dos puntos: HH:mm
            formatted = allDigits[..2] + ":" + allDigits[2..];
        }

        if (formatted.Length > 5)
            formatted = formatted[..5];

        return (formatted, formatted.Length);
    }
    
    /// <summary>
    /// Verifica si un texto tiene un timestamp en una posición específica
    /// Formato esperado: "HH:mm " (5 caracteres + espacio)
    /// </summary>
    public static bool HasTimestampAt(string text, int position)
    {
        if (string.IsNullOrEmpty(text)) return false;
        if (position + 6 > text.Length) return false;
        
        var substring = text.Substring(position, 6);
        
        // Verificar patrón: DD:DD + espacio
        if (substring.Length < 6) return false;
        if (substring[2] != ':') return false;
        if (substring[5] != ' ') return false;
        
        // Verificar que sean dígitos
        return char.IsDigit(substring[0]) && 
               char.IsDigit(substring[1]) && 
               char.IsDigit(substring[3]) && 
               char.IsDigit(substring[4]);
    }
    
    /// <summary>
    /// Obtiene la posición de inicio de la línea actual en un texto multilínea
    /// </summary>
    public static int GetLineStartPosition(string text, int cursorPos)
    {
        if (string.IsNullOrEmpty(text) || cursorPos == 0) return 0;
        
        // Buscar hacia atrás desde cursorPos hasta encontrar \n
        for (int i = cursorPos - 1; i >= 0; i--)
        {
            if (text[i] == '\n')
            {
                return i + 1; // Retornar posición después del \n
            }
        }
        
        return 0; // Estamos en la primera línea
    }
    
    /// <summary>
    /// Verifica si el cursor está al inicio de una línea sin timestamp
    /// </summary>
    public static bool IsAtStartOfLineWithoutTimestamp(string text, int cursorPos)
    {
        if (string.IsNullOrEmpty(text)) return true;
        
        var lineStart = GetLineStartPosition(text, cursorPos);
        
        // Si estamos al inicio del texto
        if (lineStart == 0 && cursorPos <= 6)
        {
            return !HasTimestampAt(text, 0);
        }
        
        // Si estamos al inicio de una línea después de un salto
        if (lineStart > 0 && cursorPos == lineStart)
        {
            return !HasTimestampAt(text, lineStart);
        }
        
        return false;
    }
    
    /// <summary>
    /// Trunca un string para logs (max length)
    /// </summary>
    public static string TruncateForLog(string? s, int maxLen)
    {
        if (string.IsNullOrEmpty(s)) return "(vacío)";
        if (s.Length <= maxLen) return s;
        return s.Substring(0, maxLen) + "...";
    }
}
