using System;
using System.Collections.ObjectModel;

namespace GestionTime.Desktop.Services.Notifications;

/// <summary>Servicio global de notificaciones in-app para WinUI 3.</summary>
public interface INotificationService
{
    /// <summary>Colección observable de notificaciones activas (para UI binding).</summary>
    ObservableCollection<NotificationItem> ActiveNotifications { get; }
    
    /// <summary>Muestra una notificación de información.</summary>
    /// <param name="message">Mensaje principal</param>
    /// <param name="title">Título opcional</param>
    /// <param name="options">Opciones configurables</param>
    /// <returns>ID de la notificación creada</returns>
    string ShowInfo(string message, string? title = null, NotificationOptions? options = null);
    
    /// <summary>Muestra una notificación de éxito.</summary>
    string ShowSuccess(string message, string? title = null, NotificationOptions? options = null);
    
    /// <summary>Muestra una notificación de advertencia.</summary>
    string ShowWarning(string message, string? title = null, NotificationOptions? options = null);
    
    /// <summary>Muestra una notificación de error.</summary>
    /// <param name="message">Mensaje de error</param>
    /// <param name="title">Título opcional</param>
    /// <param name="exception">Excepción asociada (se loguea)</param>
    /// <param name="options">Opciones configurables</param>
    string ShowError(string message, string? title = null, Exception? exception = null, NotificationOptions? options = null);
    
    /// <summary>Muestra una notificación de error HTTP con detalles del servidor.</summary>
    string ShowHttpError(ApiException apiException, NotificationOptions? options = null);
    
    /// <summary>Cierra una notificación específica por ID.</summary>
    void Close(string notificationId);
    
    /// <summary>Actualiza una notificación existente.</summary>
    /// <returns>True si se encontró y actualizó, False si no existe</returns>
    bool Update(string notificationId, string? newMessage = null, string? newTitle = null);
    
    /// <summary>Cierra todas las notificaciones activas.</summary>
    void ClearAll();
    
    /// <summary>Indica si el servicio está habilitado en configuración.</summary>
    bool IsEnabled { get; }
}
