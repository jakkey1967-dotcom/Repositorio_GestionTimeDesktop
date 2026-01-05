using GestionTime.Desktop.Services.Notifications;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace GestionTime.Desktop.Controls;

/// <summary>Control host para mostrar notificaciones in-app.</summary>
public sealed partial class NotificationHost : UserControl
{
    public NotificationHostViewModel ViewModel { get; }
    
    public NotificationHost()
    {
        this.InitializeComponent();
        
        // Obtener el servicio de notificaciones desde App
        ViewModel = new NotificationHostViewModel(App.Notifications);
    }
    
    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string notificationId)
        {
            App.Notifications?.Close(notificationId);
        }
    }
}

/// <summary>ViewModel simple para el NotificationHost.</summary>
public sealed class NotificationHostViewModel
{
    private readonly INotificationService? _notificationService;
    
    public ObservableCollection<NotificationItem> ActiveNotifications =>
        _notificationService?.ActiveNotifications ?? new();
    
    public NotificationHostViewModel(INotificationService? notificationService)
    {
        _notificationService = notificationService;
    }
}
