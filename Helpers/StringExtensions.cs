using System.Globalization;
using System.Text;

namespace GestionTime.Desktop.Helpers;

/// <summary>Extensiones para manipulación de cadenas de texto.</summary>
public static class StringExtensions
{
    /// <summary>Normaliza texto eliminando acentos y convirtiendo a minúsculas para búsquedas insensibles.</summary>
    public static string RemoveAccents(this string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Convertir a minúsculas
        var normalized = text.ToLowerInvariant();

        // Remover acentos usando normalización Unicode
        var normalizedString = normalized.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }
}
