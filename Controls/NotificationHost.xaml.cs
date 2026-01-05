using GestionTime.Desktop.Services.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;

namespace GestionTime.Desktop.Controls;

/// <summary>Control host para mostrar notificaciones in-app.</summary>
public sealed partial class NotificationHost : UserControl
{
    public NotificationHostViewModel ViewModel { get; }
    
    public NotificationHost()
    {
        this.InitializeComponent();
        ViewModel = new NotificationHostViewModel(App.Notifications);
    }
    
    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string notificationId)
        {
            App.Notifications?.Close(notificationId);
        }
    }
    
    private async void OnActionClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not NotificationAction action)
            return;
        
        try
        {
            if (action.OnClick != null)
            {
                await action.OnClick.Invoke();
            }
            
            if (action.CloseOnClick && button.DataContext is NotificationItem notification)
            {
                App.Notifications?.Close(notification.Id);
            }
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error al ejecutar acción de notificación");
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
