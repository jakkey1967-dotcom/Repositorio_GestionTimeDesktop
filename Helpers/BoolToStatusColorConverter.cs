using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.UI;

namespace GestionTime.Desktop.Helpers;

public class BoolToStatusColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isOnline)
        {
            return isOnline 
                ? new SolidColorBrush(Color.FromArgb(255, 16, 185, 129)) // Verde online
                : new SolidColorBrush(Color.FromArgb(255, 107, 114, 128)); // Gris offline
        }
        return new SolidColorBrush(Color.FromArgb(255, 107, 114, 128));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}