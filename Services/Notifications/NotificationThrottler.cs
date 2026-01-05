using System;
using System.Collections.Concurrent;

namespace GestionTime.Desktop.Services.Notifications;

/// <summary>Previene notificaciones duplicadas en un intervalo de tiempo.</summary>
internal sealed class NotificationThrottler
{
    private readonly ConcurrentDictionary<string, DateTime> _lastShown = new();
    private readonly int _throttleWindowMs;
    
    public NotificationThrottler(int throttleWindowMs)
    {
        _throttleWindowMs = throttleWindowMs;
    }
    
    /// <summary>Determina si una notificación debe mostrarse o está siendo throttled.</summary>
    /// <param name="key">Clave única (ej: hash del mensaje)</param>
    /// <returns>True si debe mostrarse, False si está throttled</returns>
    public bool ShouldShow(string key)
    {
        var now = DateTime.Now;
        
        if (_lastShown.TryGetValue(key, out var lastTime))
        {
            var elapsed = (now - lastTime).TotalMilliseconds;
            
            if (elapsed < _throttleWindowMs)
            {
                // Aún en ventana de throttling
                return false;
            }
        }
        
        // Actualizar o agregar timestamp
        _lastShown[key] = now;
        
        // Limpiar entradas antiguas (evitar memory leak)
        CleanupOldEntries(now);
        
        return true;
    }
    
    private void CleanupOldEntries(DateTime now)
    {
        // Limpiar cada 100 entradas
        if (_lastShown.Count < 100)
            return;
        
        var threshold = now.AddMilliseconds(-_throttleWindowMs * 2);
        
        foreach (var kvp in _lastShown)
        {
            if (kvp.Value < threshold)
            {
                _lastShown.TryRemove(kvp.Key, out _);
            }
        }
    }
    
    /// <summary>Genera una clave de throttling basada en mensaje y tipo.</summary>
    public static string GenerateKey(string message, NotificationType type)
    {
        return $"{type}:{message.GetHashCode():X}";
    }
}
