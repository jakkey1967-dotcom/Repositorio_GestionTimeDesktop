using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace GestionTime.Desktop.Helpers;

// GT-BEGIN: Calendario semana desde lunes
/// <summary>Helper reutilizable para fijar FirstDayOfWeek=Monday en calendarios WinUI 3.</summary>
public static class CalendarFirstDayHelper
{
    /// <summary>Fija FirstDayOfWeek=Monday en el control dado.</summary>
    /// <remarks>Soporta CalendarView directo y CalendarDatePicker (vía Opened + popup root).</remarks>
    public static void Attach(FrameworkElement host)
    {
        switch (host)
        {
            case CalendarView cv:
                cv.FirstDayOfWeek = Windows.Globalization.DayOfWeek.Monday;
                break;
            case CalendarDatePicker cdp:
                cdp.Opened -= OnCalendarOpened;
                cdp.Opened += OnCalendarOpened;
                break;
        }
    }

    /// <summary>Busca recursivamente el primer descendiente visual del tipo T.</summary>
    public static T? FindChild<T>(DependencyObject root) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T target) return target;
            var result = FindChild<T>(child);
            if (result is not null) return result;
        }
        return null;
    }

    private static void OnCalendarOpened(object sender, object e)
    {
        if (sender is not CalendarDatePicker cdp) return;

        // Diferir con prioridad baja: CalendarDatePicker reinicia FirstDayOfWeek
        // como valor local DESPUÉS de Opened; lo sobreescribimos tras ese ciclo.
        cdp.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            if (cdp.XamlRoot == null) return;

            // El flyout del CalendarDatePicker vive en el popup root, no en el árbol
            // normal de la página. GetOpenPopupsForXamlRoot lo expone directamente.
            foreach (var popup in VisualTreeHelper.GetOpenPopupsForXamlRoot(cdp.XamlRoot))
            {
                if (popup.Child is not DependencyObject popupChild) continue;
                var calView = FindChild<CalendarView>(popupChild);
                if (calView == null) continue;
                calView.FirstDayOfWeek = Windows.Globalization.DayOfWeek.Monday;
                return;
            }
        });
    }
}
// GT-END
// GT-END
