using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using WinRT.Interop;
using System;
using Microsoft.Extensions.Logging;

namespace GestionTime.Desktop.Helpers;

/// <summary>
/// Helper para configurar tamaño, posición y escala DPI de ventanas
/// </summary>
public static class WindowHelper
{
    /// <summary>
    /// Redimensiona una ventana sin escala DPI (píxeles físicos)
    /// </summary>
    /// <param name="window">Ventana a redimensionar</param>
    /// <param name="width">Ancho en píxeles físicos</param>
    /// <param name="height">Alto en píxeles físicos</param>
    public static void ResizePhysical(Window window, int width, int height)
    {
        try
        {
            var appWindow = GetAppWindow(window);
            if (appWindow == null)
            {
                App.Log?.LogWarning("ResizePhysical: No se pudo obtener AppWindow");
                return;
            }

            appWindow.Resize(new SizeInt32(width, height));
            App.Log?.LogInformation("Ventana redimensionada a {width}x{height} píxeles físicos", width, height);
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error en ResizePhysical");
        }
    }

    /// <summary>
    /// Redimensiona una ventana considerando el factor de escala DPI
    /// </summary>
    /// <param name="window">Ventana a redimensionar</param>
    /// <param name="width">Ancho en píxeles lógicos</param>
    /// <param name="height">Alto en píxeles lógicos</param>
    public static void Resize(Window window, int width, int height)
    {
        try
        {
            var appWindow = GetAppWindow(window);
            if (appWindow == null)
            {
                App.Log?.LogWarning("Resize: No se pudo obtener AppWindow");
                return;
            }

            var scalingFactor = GetDpiScalingFactor(window);
            var scaledWidth = (int)(width * scalingFactor);
            var scaledHeight = (int)(height * scalingFactor);

            App.Log?.LogInformation("Redimensionando ventana: {logicalWidth}x{logicalHeight} lógicos ? {physicalWidth}x{physicalHeight} físicos (DPI factor: {factor})",
                width, height, scaledWidth, scaledHeight, scalingFactor);

            appWindow.Resize(new SizeInt32(scaledWidth, scaledHeight));
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error en Resize");
        }
    }

    /// <summary>
    /// Redimensiona y centra una ventana (usando píxeles físicos directamente, sin escala DPI)
    /// </summary>
    /// <param name="window">Ventana a redimensionar y centrar</param>
    /// <param name="width">Ancho en píxeles</param>
    /// <param name="height">Alto en píxeles</param>
    public static void ResizeAndCenter(Window window, int width, int height)
    {
        try
        {
            App.Log?.LogInformation("ResizeAndCenter llamado con {width}x{height}", width, height);
            
            // Usar píxeles físicos directamente (sin escala DPI)
            ResizePhysical(window, width, height);
            
            // Esperar un momento para que el resize se aplique
            System.Threading.Thread.Sleep(50);
            
            CenterWindow(window);
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error en ResizeAndCenter");
        }
    }

    /// <summary>
    /// Centra una ventana en la pantalla
    /// </summary>
    public static void CenterWindow(Window window)
    {
        try
        {
            var hWnd = WindowNative.GetWindowHandle(window);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            if (appWindow != null)
            {
                var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
                if (displayArea != null)
                {
                    var windowWidth = appWindow.Size.Width;
                    var windowHeight = appWindow.Size.Height;
                    var x = (displayArea.WorkArea.Width - windowWidth) / 2 + displayArea.WorkArea.X;
                    var y = (displayArea.WorkArea.Height - windowHeight) / 2 + displayArea.WorkArea.Y;

                    appWindow.Move(new PointInt32 { X = x, Y = y });
                    
                    App.Log?.LogInformation("Ventana centrada en ({x}, {y}) - WorkArea: {waWidth}x{waHeight}",
                        x, y, displayArea.WorkArea.Width, displayArea.WorkArea.Height);
                }
            }
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error en CenterWindow");
        }
    }

    /// <summary>
    /// Obtiene el AppWindow de una ventana
    /// </summary>
    public static AppWindow? GetAppWindow(Window window)
    {
        try
        {
            var hWnd = WindowNative.GetWindowHandle(window);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(windowId);
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error en GetAppWindow");
            return null;
        }
    }

    /// <summary>
    /// Obtiene el factor de escala DPI de una ventana
    /// </summary>
    public static double GetDpiScalingFactor(Window window)
    {
        try
        {
            var hWnd = WindowNative.GetWindowHandle(window);
            
            // Obtener DPI del monitor
            int dpiX = GetDpiForWindow(hWnd);
            
            // 96 DPI es 100% (1.0), 120 DPI es 125% (1.25), 144 DPI es 150% (1.5), etc.
            var factor = dpiX / 96.0;
            
            App.Log?.LogDebug("DPI detectado: {dpi} ? factor de escala: {factor}", dpiX, factor);
            
            return factor;
        }
        catch (Exception ex)
        {
            App.Log?.LogWarning(ex, "Error obteniendo DPI, usando factor 1.0");
            return 1.0; // Factor por defecto si no se puede determinar
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int GetDpiForWindow(nint hwnd);

    /// <summary>
    /// Configura si la ventana es redimensionable
    /// </summary>
    public static void SetResizable(Window window, bool isResizable)
    {
        try
        {
            var appWindow = GetAppWindow(window);
            if (appWindow == null) return;

            var presenter = appWindow.Presenter as OverlappedPresenter;
            if (presenter != null)
            {
                presenter.IsResizable = isResizable;
                App.Log?.LogInformation("Ventana configurada como {state}", isResizable ? "redimensionable" : "tamaño fijo");
            }
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error en SetResizable");
        }
    }

    /// <summary>
    /// Configura si la ventana puede maximizarse
    /// </summary>
    public static void SetMaximizable(Window window, bool isMaximizable)
    {
        try
        {
            var appWindow = GetAppWindow(window);
            if (appWindow == null) return;

            var presenter = appWindow.Presenter as OverlappedPresenter;
            if (presenter != null)
            {
                presenter.IsMaximizable = isMaximizable;
                App.Log?.LogInformation("Ventana configurada: maximizable={state}", isMaximizable);
            }
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error en SetMaximizable");
        }
    }

    /// <summary>
    /// Configura si la ventana puede minimizarse
    /// </summary>
    public static void SetMinimizable(Window window, bool isMinimizable)
    {
        try
        {
            var appWindow = GetAppWindow(window);
            if (appWindow == null) return;

            var presenter = appWindow.Presenter as OverlappedPresenter;
            if (presenter != null)
            {
                presenter.IsMinimizable = isMinimizable;
                App.Log?.LogInformation("Ventana configurada: minimizable={state}", isMinimizable);
            }
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error en SetMinimizable");
        }
    }
}
