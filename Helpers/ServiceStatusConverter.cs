using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.UI;

namespace GestionTime.Desktop.Helpers;

/// <summary>
/// Converter para transformar el estado booleano del servicio en un Brush (color del LED)
/// </summary>
public class ServiceStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isOnline)
        {
            // Verde brillante cuando está online, rojo cuando está offline
            return isOnline 
                ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)) // #10B981 (verde)
                : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68));  // #EF4444 (rojo)
        }
        
        // Por defecto, gris si no hay estado definido
        return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 156, 163, 175)); // #9CA3AF (gris)
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter para transformar el estado booleano del servicio en texto descriptivo
/// </summary>
public class ServiceStatusToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isOnline)
        {
            return isOnline ? "Online" : "Offline";
        }
        
        return "Desconocido";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter para mostrar/ocultar el icono de advertencia cuando está offline
/// </summary>
public class ServiceStatusToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isOnline)
        {
            // Invertir: mostrar advertencia solo cuando está OFFLINE
            return isOnline 
                ? Microsoft.UI.Xaml.Visibility.Collapsed 
                : Microsoft.UI.Xaml.Visibility.Visible;
        }
        
        return Microsoft.UI.Xaml.Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
