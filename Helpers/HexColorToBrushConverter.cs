using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace GestionTime.Desktop.Helpers;

/// <summary>
/// Converter que convierte un color hexadecimal string (#AARRGGBB) a SolidColorBrush
/// </summary>
public class HexColorToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string hexColor && !string.IsNullOrWhiteSpace(hexColor))
        {
            try
            {
                // Quitar el # si existe
                hexColor = hexColor.TrimStart('#');
                
                // Parsear el color
                byte a = 255, r = 0, g = 0, b = 0;
                
                if (hexColor.Length == 8) // ARGB
                {
                    a = System.Convert.ToByte(hexColor.Substring(0, 2), 16);
                    r = System.Convert.ToByte(hexColor.Substring(2, 2), 16);
                    g = System.Convert.ToByte(hexColor.Substring(4, 2), 16);
                    b = System.Convert.ToByte(hexColor.Substring(6, 2), 16);
                }
                else if (hexColor.Length == 6) // RGB
                {
                    r = System.Convert.ToByte(hexColor.Substring(0, 2), 16);
                    g = System.Convert.ToByte(hexColor.Substring(2, 2), 16);
                    b = System.Convert.ToByte(hexColor.Substring(4, 2), 16);
                }
                
                return new SolidColorBrush(Windows.UI.Color.FromArgb(a, r, g, b));
            }
            catch
            {
                // Si falla, retornar transparente
                return new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));
            }
        }
        
        return new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
