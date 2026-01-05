using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace GestionTime.Desktop.Services.Notifications;

/// <summary>Implementación del servicio de notificaciones in-app.</summary>
public sealed class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService>? _log;
    private readonly DispatcherQueue _dispatcher;
    private readonly NotificationThrottler _throttler;
    private readonly int _maxVisible;
    private readonly int _defaultDurationMs;
    private readonly bool _isEnabled;
    
    public ObservableCollection<NotificationItem> ActiveNotifications { get; } = new();
    
    public bool IsEnabled => _isEnabled;
    
    public NotificationService(ILogger<NotificationService>? logger = null)
    {
        _log = logger;
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        
        // Cargar configuración de appsettings.json
        var config = LoadConfiguration();
        _isEnabled = config.Enabled;
        _maxVisible = config.MaxVisible;
        _defaultDurationMs = config.DefaultDurationMs;
        _throttler = new NotificationThrottler(config.ThrottleWindowMs);
        
        _log?.LogInformation("NotificationService inicializado. Enabled={enabled}, MaxVisible={max}, DefaultDuration={dur}ms",
            _isEnabled, _maxVisible, _defaultDurationMs);
    }
    
    public string ShowInfo(string message, string? title = null, NotificationOptions? options = null)
    {
        return ShowNotification(NotificationType.Info, message, title, null, options);
    }
    
    public string ShowSuccess(string message, string? title = null, NotificationOptions? options = null)
    {
        return ShowNotification(NotificationType.Success, message, title, null, options);
    }
    
    public string ShowWarning(string message, string? title = null, NotificationOptions? options = null)
    {
        return ShowNotification(NotificationType.Warning, message, title, null, options);
    }
    
    public string ShowError(string message, string? title = null, Exception? exception = null, NotificationOptions? options = null)
    {
        if (exception != null)
        {
            _log?.LogError(exception, "Notificación de error: {message}", message);
        }
        
        return ShowNotification(NotificationType.Error, message, title ?? "Error", exception, options);
    }
    
    public string ShowHttpError(ApiException apiException, NotificationOptions? options = null)
    {
        var title = $"Error HTTP {(int)apiException.StatusCode}";
        var message = $"{apiException.Path}\n{apiException.Message}";
        
        if (!string.IsNullOrEmpty(apiException.ServerMessage))
        {
            message += $"\n\nServidor: {TruncateMessage(apiException.ServerMessage, 150)}";
        }
        
        _log?.LogError("Notificación HTTP Error: {status} - {path} - {msg}",
            apiException.StatusCode, apiException.Path, apiException.Message);
        
        return ShowNotification(NotificationType.Error, message, title, apiException, options);
    }
    
    public void Close(string notificationId)
    {
        if (!_isEnabled)
            return;
        
        _dispatcher.TryEnqueue(() =>
        {
            var notification = ActiveNotifications.FirstOrDefault(n => n.Id == notificationId);
            if (notification != null)
            {
                ActiveNotifications.Remove(notification);
                _log?.LogDebug("Notificación cerrada: {id}", notificationId);
            }
        });
    }
    
    public bool Update(string notificationId, string? newMessage = null, string? newTitle = null)
    {
        if (!_isEnabled)
            return false;
        
        bool updated = false;
        
        _dispatcher.TryEnqueue(() =>
        {
            var notification = ActiveNotifications.FirstOrDefault(n => n.Id == notificationId);
            if (notification != null)
            {
                // No se puede modificar directamente un objeto inmutable
                // Crear una nueva notificación con los mismos datos pero mensaje/título actualizado
                var index = ActiveNotifications.IndexOf(notification);
                
                var updatedNotification = new NotificationItem
                {
                    Id = notification.Id,
                    Type = notification.Type,
                    Title = newTitle ?? notification.Title,
                    Message = newMessage ?? notification.Message,
                    Timestamp = notification.Timestamp,
                    DurationMs = notification.DurationMs,
                    Actions = notification.Actions,
                    CorrelationId = notification.CorrelationId,
                    IsRead = notification.IsRead,
                    Icon = notification.Icon
                };
                
                ActiveNotifications[index] = updatedNotification;
                updated = true;
                
                _log?.LogDebug("Notificación actualizada: {id}", notificationId);
            }
        });
        
        return updated;
    }
    
    public void ClearAll()
    {
        if (!_isEnabled)
            return;
        
        _dispatcher.TryEnqueue(() =>
        {
            var count = ActiveNotifications.Count;
            ActiveNotifications.Clear();
            _log?.LogInformation("Todas las notificaciones cerradas ({count})", count);
        });
    }
    
    private string ShowNotification(
        NotificationType type,
        string message,
        string? title,
        Exception? exception,
        NotificationOptions? options)
    {
        if (!_isEnabled)
        {
            _log?.LogDebug("NotificationService deshabilitado, ignorando notificación: {msg}", message);
            return string.Empty;
        }
        
        try
        {
            // Throttling (si está habilitado)
            if (options?.AllowDuplicates != true)
            {
                var throttleKey = NotificationThrottler.GenerateKey(message, type);
                if (!_throttler.ShouldShow(throttleKey))
                {
                    _log?.LogDebug("Notificación throttled: {msg}", TruncateMessage(message, 50));
                    return string.Empty;
                }
            }
            
            var notification = CreateNotificationItem(type, message, title, options);
            
            _dispatcher.TryEnqueue(() =>
            {
                // Verificar si existe una notificación con el mismo CorrelationId
                if (!string.IsNullOrEmpty(notification.CorrelationId))
                {
                    var existing = ActiveNotifications.FirstOrDefault(n => n.CorrelationId == notification.CorrelationId);
                    if (existing != null)
                    {
                        // Actualizar notificación existente
                        var index = ActiveNotifications.IndexOf(existing);
                        ActiveNotifications[index] = notification;
                        
                        _log?.LogDebug("Notificación actualizada por CorrelationId: {id}", notification.CorrelationId);
                        return;
                    }
                }
                
                // Agregar nueva notificación
                ActiveNotifications.Add(notification);
                
                // Limitar cantidad visible
                while (ActiveNotifications.Count > _maxVisible)
                {
                    ActiveNotifications.RemoveAt(0);
                }
                
                _log?.LogInformation("Notificación mostrada: [{type}] {title} - {msg}",
                    type, title ?? "(sin título)", TruncateMessage(message, 50));
                
                // Auto-cerrar después del tiempo especificado
                if (notification.DurationMs > 0)
                {
                    Task.Delay(notification.DurationMs).ContinueWith(_ =>
                    {
                        Close(notification.Id);
                    });
                }
            });
            
            return notification.Id;
        }
        catch (Exception ex)
        {
            // Fail-safe: NO crashear la app
            _log?.LogError(ex, "Error mostrando notificación (fail-safe activado)");
            return string.Empty;
        }
    }
    
    private NotificationItem CreateNotificationItem(
        NotificationType type,
        string message,
        string? title,
        NotificationOptions? options)
    {
        var icon = options?.CustomIcon ?? GetDefaultIcon(type);
        var duration = options?.DurationMs ?? _defaultDurationMs;
        
        return new NotificationItem
        {
            Type = type,
            Title = title,
            Message = TruncateMessage(message, 300),
            Icon = icon,
            DurationMs = duration,
            Actions = options?.Actions ?? new(),
            CorrelationId = options?.CorrelationId
        };
    }
    
    private static string GetDefaultIcon(NotificationType type) => type switch
    {
        NotificationType.Success => "\uE73E", // CheckMark
        NotificationType.Warning => "\uE7BA", // Warning
        NotificationType.Error => "\uE783",   // ErrorBadge
        _ => "\uE946"                         // Info
    };
    
    private static string TruncateMessage(string message, int maxLength)
    {
        if (string.IsNullOrEmpty(message) || message.Length <= maxLength)
            return message;
        
        return message.Substring(0, maxLength - 3) + "...";
    }
    
    private static NotificationConfig LoadConfiguration()
    {
        try
        {
            // Cargar de appsettings.json usando System.Text.Json
            var appSettingsPath = System.IO.Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            
            if (!System.IO.File.Exists(appSettingsPath))
            {
                return NotificationConfig.Default;
            }
            
            var json = System.IO.File.ReadAllText(appSettingsPath);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            
            if (doc.RootElement.TryGetProperty("Notifications", out var notifSection))
            {
                return new NotificationConfig
                {
                    Enabled = notifSection.TryGetProperty("Enabled", out var enabled) && enabled.GetBoolean(),
                    MaxVisible = notifSection.TryGetProperty("MaxVisible", out var max) ? max.GetInt32() : 5,
                    DefaultDurationMs = notifSection.TryGetProperty("DefaultDurationMs", out var dur) ? dur.GetInt32() : 4000,
                    ThrottleWindowMs = notifSection.TryGetProperty("ThrottleWindowMs", out var throttle) ? throttle.GetInt32() : 2000
                };
            }
            
            return NotificationConfig.Default;
        }
        catch
        {
            return NotificationConfig.Default;
        }
    }
    
    private class NotificationConfig
    {
        public bool Enabled { get; init; }
        public int MaxVisible { get; init; }
        public int DefaultDurationMs { get; init; }
        public int ThrottleWindowMs { get; init; }
        
        public static NotificationConfig Default => new()
        {
            Enabled = true,
            MaxVisible = 5,
            DefaultDurationMs = 4000,
            ThrottleWindowMs = 2000
        };
    }
}
