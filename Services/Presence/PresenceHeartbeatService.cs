using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;

namespace GestionTime.Desktop.Services.Presence;

/// <summary>Servicio que envía pings periódicos al backend para mantener el estado "online" del usuario actual.</summary>
public sealed class PresenceHeartbeatService : IDisposable
{
    private static PresenceHeartbeatService? _instance;
    public static PresenceHeartbeatService Instance => _instance ??= new PresenceHeartbeatService();

    private readonly ILogger? _log;
    private DispatcherQueueTimer? _heartbeatTimer;
    private CancellationTokenSource? _cts;
    private bool _isRunning;

    private PresenceHeartbeatService()
    {
        _log = App.Log;
    }

    /// <summary>Inicia el envío de pings cada 60 segundos.</summary>
    public void Start(DispatcherQueue dispatcher)
    {
        if (_isRunning)
        {
            _log?.LogDebug("⚠️ Heartbeat ya está corriendo");
            return;
        }

        _cts = new CancellationTokenSource();
        _heartbeatTimer = dispatcher.CreateTimer();
        _heartbeatTimer.Interval = TimeSpan.FromSeconds(60);
        _heartbeatTimer.Tick += async (s, e) => await SendHeartbeatAsync();
        _heartbeatTimer.Start();

        _isRunning = true;

        _log?.LogInformation("💓 Heartbeat iniciado: ping cada 60 segundos");

        // Enviar ping inicial inmediatamente
        _ = SendHeartbeatAsync();
    }

    /// <summary>Detiene el envío de pings.</summary>
    public void Stop()
    {
        if (!_isRunning)
            return;

        _heartbeatTimer?.Stop();
        _heartbeatTimer = null;

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        _isRunning = false;

        _log?.LogInformation("💔 Heartbeat detenido");
    }

    /// <summary>Envía un ping al backend.</summary>
    private async Task SendHeartbeatAsync()
    {
        if (_cts?.Token.IsCancellationRequested == true)
            return;

        try
        {
            var success = await PresenceService.Instance.PingAsync(_cts!.Token);

            if (success)
            {
                _log?.LogDebug("💓 Heartbeat enviado correctamente");
            }
            else
            {
                _log?.LogDebug("⚠️ Heartbeat falló (endpoint posiblemente no implementado)");
            }
        }
        catch (OperationCanceledException)
        {
            _log?.LogDebug("🚫 Heartbeat cancelado");
        }
        catch (Exception ex)
        {
            _log?.LogWarning(ex, "❌ Error enviando heartbeat");
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
