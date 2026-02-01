using System;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Dispatching;
using WinRT.Interop;
using Windows.Graphics;

namespace GestionTime.Desktop.Services.Windowing;

/// <summary>Servicio para acoplar ventanas secundarias a la ventana principal (docking).</summary>
public sealed class WindowDockService : IDisposable
{
    private readonly ILogger? _log;
    private Window? _mainWindow;
    private Window? _childWindow;
    private AppWindow? _mainAppWindow;
    private AppWindow? _childAppWindow;
    private OverlappedPresenter? _mainPresenter;
    private OverlappedPresenter? _childPresenter;
    
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _snapTimer;
    private RectInt32 _lastMainRect;
    private bool _isSnapping = false;
    private bool _isAttached = false;
    private bool _wasMainMinimized = false;
    
    private const int SnapGap = 0; // ✅ CORREGIDO: Ventanas completamente pegadas (sin separación)
    private const int SnapInterval = 150; // Milisegundos entre checks de posición

    public WindowDockService()
    {
        _log = App.Log;
    }

    /// <summary>
    /// Acopla una ventana secundaria a la derecha de la ventana principal.
    /// La ventana secundaria seguirá automáticamente los movimientos y redimensionamientos de la principal.
    /// </summary>
    /// <param name="mainWindow">Ventana principal (MainWindow).</param>
    /// <param name="childWindow">Ventana secundaria (UsersOnlineWindow).</param>
    public void Attach(Window mainWindow, Window childWindow)
    {
        if (_isAttached)
        {
            _log?.LogWarning("⚠️ WindowDockService ya está attached, detachando primero...");
            Detach();
        }

        try
        {
            _log?.LogInformation("═══════════════════════════════════════════════════════════════");
            _log?.LogInformation("📌 DOCK ATTACH - Iniciando acoplamiento de ventanas");
            _log?.LogInformation("═══════════════════════════════════════════════════════════════");

            _mainWindow = mainWindow;
            _childWindow = childWindow;

            // Obtener AppWindow de ambas ventanas
            _mainAppWindow = GetAppWindow(mainWindow);
            _childAppWindow = GetAppWindow(childWindow);

            if (_mainAppWindow == null || _childAppWindow == null)
            {
                _log?.LogError("❌ No se pudo obtener AppWindow de las ventanas");
                return;
            }

            // Obtener presenters para control de minimizado/restaurado
            _mainPresenter = _mainAppWindow.Presenter as OverlappedPresenter;
            _childPresenter = _childAppWindow.Presenter as OverlappedPresenter;

            // Guardar posición inicial de la ventana principal
            _lastMainRect = _mainAppWindow.Position.X != 0 
                ? new RectInt32(_mainAppWindow.Position.X, _mainAppWindow.Position.Y, _mainAppWindow.Size.Width, _mainAppWindow.Size.Height)
                : new RectInt32(100, 100, 1200, 800); // Valores por defecto si no hay posición aún

            _log?.LogInformation("📐 Posición inicial MainWindow: X={x}, Y={y}, W={w}, H={h}",
                _lastMainRect.X, _lastMainRect.Y, _lastMainRect.Width, _lastMainRect.Height);

            // Aplicar snap inicial
            SnapNow();

            // Iniciar timer de polling para detectar cambios
            StartSnapTimer();

            // Suscribirse a eventos de la ventana principal
            if (_mainWindow != null)
            {
                _mainWindow.Activated += OnMainWindowActivated;
                _mainWindow.VisibilityChanged += OnMainWindowVisibilityChanged;
            }

            // Suscribirse a eventos de la ventana secundaria para detectar cierre
            if (_childWindow != null)
            {
                _childWindow.Closed += OnChildWindowClosed;
            }

            _isAttached = true;

            _log?.LogInformation("✅ DOCK ATTACHED correctamente");
            _log?.LogInformation("   • Timer de snap: {interval}ms", SnapInterval);
            _log?.LogInformation("   • Gap entre ventanas: {gap}px (pegadas completamente)", SnapGap);
            _log?.LogInformation("═══════════════════════════════════════════════════════════════");
        }
        catch (Exception ex)
        {
            _log?.LogError(ex, "❌ Error en Attach()");
        }
    }

    /// <summary>
    /// Desacopla las ventanas y detiene el seguimiento automático.
    /// </summary>
    public void Detach()
    {
        if (!_isAttached)
            return;

        try
        {
            _log?.LogInformation("═══════════════════════════════════════════════════════════════");
            _log?.LogInformation("📌 DOCK DETACH - Desacoplando ventanas");
            _log?.LogInformation("═══════════════════════════════════════════════════════════════");

            StopSnapTimer();

            // Desuscribirse de eventos
            if (_mainWindow != null)
            {
                _mainWindow.Activated -= OnMainWindowActivated;
                _mainWindow.VisibilityChanged -= OnMainWindowVisibilityChanged;
            }

            if (_childWindow != null)
            {
                _childWindow.Closed -= OnChildWindowClosed;
            }

            _mainWindow = null;
            _childWindow = null;
            _mainAppWindow = null;
            _childAppWindow = null;
            _mainPresenter = null;
            _childPresenter = null;

            _isAttached = false;

            _log?.LogInformation("✅ DOCK DETACHED correctamente");
            _log?.LogInformation("═══════════════════════════════════════════════════════════════");
        }
        catch (Exception ex)
        {
            _log?.LogError(ex, "❌ Error en Detach()");
        }
    }

    /// <summary>
    /// Aplica el snap inmediatamente (reposiciona la ventana secundaria a la derecha de la principal).
    /// </summary>
    public void SnapNow()
    {
        if (!_isAttached || _isSnapping)
            return;

        if (_mainAppWindow == null || _childAppWindow == null)
            return;

        try
        {
            _isSnapping = true;

            // Obtener rectángulo actual de la ventana principal
            var mainRect = new RectInt32(
                _mainAppWindow.Position.X,
                _mainAppWindow.Position.Y,
                _mainAppWindow.Size.Width,
                _mainAppWindow.Size.Height
            );

            // Calcular posición de la ventana secundaria
            var childX = mainRect.X + mainRect.Width + SnapGap;
            var childY = mainRect.Y;
            var childWidth = _childAppWindow.Size.Width;
            var childHeight = mainRect.Height;

            // Aplicar nueva posición y tamaño
            _childAppWindow.Move(new PointInt32(childX, childY));
            _childAppWindow.Resize(new SizeInt32(childWidth, childHeight));

            _log?.LogDebug("🔄 SNAP APPLIED:");
            _log?.LogDebug("   • Main: X={mx}, Y={my}, W={mw}, H={mh}",
                mainRect.X, mainRect.Y, mainRect.Width, mainRect.Height);
            _log?.LogDebug("   • Child: X={cx}, Y={cy}, W={cw}, H={ch}",
                childX, childY, childWidth, childHeight);

            _lastMainRect = mainRect;
        }
        catch (Exception ex)
        {
            _log?.LogWarning(ex, "⚠️ Error aplicando snap");
        }
        finally
        {
            _isSnapping = false;
        }
    }

    /// <summary>
    /// Inicia el timer de polling para detectar cambios de posición/tamaño.
    /// </summary>
    private void StartSnapTimer()
    {
        if (_snapTimer != null)
            return;

        if (_mainWindow?.DispatcherQueue == null)
            return;

        _snapTimer = _mainWindow.DispatcherQueue.CreateTimer();
        _snapTimer.Interval = TimeSpan.FromMilliseconds(SnapInterval);
        _snapTimer.Tick += OnSnapTimerTick;
        _snapTimer.Start();

        _log?.LogDebug("⏰ Timer de snap iniciado ({interval}ms)", SnapInterval);
    }

    /// <summary>
    /// Detiene el timer de polling.
    /// </summary>
    private void StopSnapTimer()
    {
        if (_snapTimer == null)
            return;

        _snapTimer.Stop();
        _snapTimer.Tick -= OnSnapTimerTick;
        _snapTimer = null;

        _log?.LogDebug("⏰ Timer de snap detenido");
    }

    /// <summary>
    /// Timer tick: Detecta cambios en la ventana principal y aplica snap si es necesario.
    /// </summary>
    private void OnSnapTimerTick(object? sender, object e)
    {
        if (!_isAttached || _isSnapping)
            return;

        if (_mainAppWindow == null || _childAppWindow == null)
            return;

        try
        {
            // Obtener posición actual de la ventana principal
            var currentRect = new RectInt32(
                _mainAppWindow.Position.X,
                _mainAppWindow.Position.Y,
                _mainAppWindow.Size.Width,
                _mainAppWindow.Size.Height
            );

            // Detectar si ha cambiado posición o tamaño
            var hasChanged = currentRect.X != _lastMainRect.X ||
                            currentRect.Y != _lastMainRect.Y ||
                            currentRect.Width != _lastMainRect.Width ||
                            currentRect.Height != _lastMainRect.Height;

            if (hasChanged)
            {
                _log?.LogDebug("🔍 Detectado cambio en MainWindow - Aplicando snap...");
                SnapNow();
            }

            // Sincronizar estado de minimizado/restaurado
            SyncMinimizeState();
        }
        catch (Exception ex)
        {
            _log?.LogWarning(ex, "⚠️ Error en timer tick");
        }
    }

    /// <summary>
    /// Sincroniza el estado de minimizado/restaurado entre ventanas.
    /// </summary>
    private void SyncMinimizeState()
    {
        if (_mainPresenter == null || _childPresenter == null)
            return;

        try
        {
            var mainState = _mainPresenter.State;
            var isMainMinimized = mainState == OverlappedPresenterState.Minimized;

            // Si la principal se minimizó, minimizar la secundaria
            if (isMainMinimized && !_wasMainMinimized)
            {
                _log?.LogInformation("⬇️ MainWindow MINIMIZADA - Minimizando UsersOnlineWindow");
                _childPresenter.Minimize();
                _wasMainMinimized = true;
            }
            // Si la principal se restauró, restaurar la secundaria
            else if (!isMainMinimized && _wasMainMinimized)
            {
                _log?.LogInformation("⬆️ MainWindow RESTAURADA - Restaurando UsersOnlineWindow");
                _childPresenter.Restore();
                _wasMainMinimized = false;
                
                // Aplicar snap después de restaurar
                SnapNow();
            }
        }
        catch (Exception ex)
        {
            _log?.LogWarning(ex, "⚠️ Error sincronizando estado minimizado");
        }
    }

    /// <summary>
    /// Manejador de evento Activated de la ventana principal.
    /// </summary>
    private void OnMainWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated)
            return;

        // Aplicar snap al activar
        SnapNow();
    }

    /// <summary>
    /// Manejador de evento VisibilityChanged de la ventana principal.
    /// </summary>
    private void OnMainWindowVisibilityChanged(object sender, WindowVisibilityChangedEventArgs args)
    {
        if (!args.Visible)
            return;

        // Aplicar snap al hacerse visible
        SnapNow();
    }

    /// <summary>
    /// Manejador de evento Closed de la ventana secundaria.
    /// </summary>
    private void OnChildWindowClosed(object sender, WindowEventArgs args)
    {
        _log?.LogInformation("📂 Ventana secundaria cerrada - Detachando dock service");
        Detach();
    }

    /// <summary>
    /// Obtiene el AppWindow de una Window de WinUI 3.
    /// </summary>
    private AppWindow? GetAppWindow(Window window)
    {
        try
        {
            var hWnd = WindowNative.GetWindowHandle(window);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(windowId);
        }
        catch (Exception ex)
        {
            _log?.LogError(ex, "❌ Error obteniendo AppWindow");
            return null;
        }
    }

    public void Dispose()
    {
        Detach();
    }
}
