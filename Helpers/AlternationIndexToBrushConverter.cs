using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace GestionTime.Desktop.Helpers;

/// <summary>
/// Converter para crear filas alternadas (zebra rows) en ListView.
/// Convierte el AlternationIndex en un Brush de fondo sutil.
/// </summary>
public class AlternationIndexToBrushConverter : IValueConverter
{
    /// <summary>
    /// Brush para filas pares (transparente)
    /// </summary>
    public Brush EvenBrush { get; set; } = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));

    /// <summary>
    /// Brush para filas impares (negro muy sutil)
    /// </summary>
    public Brush OddBrush { get; set; } = new SolidColorBrush(Windows.UI.Color.FromArgb(20, 0, 0, 0)); // #14000000

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int index)
        {
            // Index 0 = par (even) ? Transparent
            // Index 1 = impar (odd) ? Sutil negro
            return index % 2 == 0 ? EvenBrush : OddBrush;
        }

        return EvenBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
