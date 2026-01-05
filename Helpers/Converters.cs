using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.UI;
using GestionTime.Desktop.Models.Dtos;

namespace GestionTime.Desktop.Helpers;

/// <summary>
/// Convierte bool a Visibility (true = Visible, false = Collapsed)
/// </summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b)
            return b ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Visibility v)
            return v == Visibility.Visible;
        return false;
    }
}

/// <summary>
/// Invierte un bool (true -> false, false -> true)
/// </summary>
public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b)
            return !b;
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b)
            return !b;
        return false;
    }
}

/// <summary>
/// Convierte IsAbierto (bool) a color de badge - LEGACY
/// </summary>
public sealed class BoolToColorConverter : IValueConverter
{
    private static readonly SolidColorBrush OpenBrush = new(Color.FromArgb(255, 16, 185, 129));   // Verde
    private static readonly SolidColorBrush ClosedBrush = new(Color.FromArgb(255, 107, 114, 128)); // Gris

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isAbierto)
            return isAbierto ? OpenBrush : ClosedBrush;
        return ClosedBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Convierte ParteEstado a color del icono:
/// - Abierto = Verde (#10B981)
/// - Pausado = Amarillo (#F59E0B)
/// - Cerrado = Rojo (#EF4444)
/// - Anulado = Gris (#6B7280)
/// </summary>
public sealed class EstadoToColorConverter : IValueConverter
{
    private static readonly SolidColorBrush AbiertoBrush = new(Color.FromArgb(255, 16, 185, 129));   // Verde
    private static readonly SolidColorBrush PausadoBrush = new(Color.FromArgb(255, 245, 158, 11));   // Amarillo
    private static readonly SolidColorBrush CerradoBrush = new(Color.FromArgb(255, 239, 68, 68));    // Rojo
    private static readonly SolidColorBrush AnuladoBrush = new(Color.FromArgb(255, 107, 114, 128));  // Gris

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ParteEstado estado)
        {
            return estado switch
            {
                ParteEstado.Abierto => AbiertoBrush,
                ParteEstado.Pausado => PausadoBrush,
                ParteEstado.Cerrado => CerradoBrush,
                ParteEstado.Anulado => AnuladoBrush,
                _ => AbiertoBrush
            };
        }
        return AbiertoBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Convierte ParteEstado a icono Segoe MDL2 Assets:
/// - Abierto = ?? Play (E768)
/// - Pausado = ?? Pause (E769)
/// - Cerrado = ? CheckMark (E73E)
/// - Anulado = ? Cancel (E711)
/// </summary>
public sealed class EstadoToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ParteEstado estado)
        {
            return estado switch
            {
                ParteEstado.Abierto => "\uE768",  // Play
                ParteEstado.Pausado => "\uE769",  // Pause
                ParteEstado.Cerrado => "\uE73E",  // CheckMark
                ParteEstado.Anulado => "\uE711",  // Cancel
                _ => "\uE768"
            };
        }
        return "\uE768";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Convierte ParteEstado a Visibility para acciones específicas.
/// Usar con ConverterParameter: "Pausar", "Reanudar", "Cerrar", "Duplicar"
/// </summary>
public sealed class EstadoToActionVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not ParteEstado estado || parameter is not string action)
            return Visibility.Collapsed;

        var visible = action.ToLowerInvariant() switch
        {
            "pausar" => estado == ParteEstado.Abierto,
            "reanudar" => estado == ParteEstado.Pausado,
            "cerrar" => estado == ParteEstado.Abierto || estado == ParteEstado.Pausado,
            "duplicar" => estado == ParteEstado.Cerrado,
            _ => false
        };

        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>Convierte string a Visibility (null/empty = Collapsed, otherwise = Visible).</summary>
public sealed class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>Convierte un count (int) a Visibility (0 = Collapsed, >0 = Visible).</summary>
public sealed class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int count)
            return count > 0 ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
