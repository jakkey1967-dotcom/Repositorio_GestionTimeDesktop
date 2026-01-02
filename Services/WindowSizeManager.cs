using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.Extensions.Logging;
using System;
using WinRT.Interop;

namespace GestionTime.Desktop.Services;

/// <summary>
/// Servicio centralizado para gestionar tamaños de ventana de forma consistente
/// </summary>
public static class WindowSizeManager
{
    // ===== TAMAÑOS PREDEFINIDOS POR PÁGINA =====
    
    /// <summary>
    /// Tamaño para LoginPage (más pequeña, solo formulario)
    /// </summary>
    public static readonly (int Width, int Height) LoginSize = (1100, 750);
    
    /// <summary>
    /// Tamaño para DiarioPage (más grande para ver tabla completa)
    /// </summary>
    public static readonly (int Width, int Height) DiarioSize = (1600, 950);
    
    /// <summary>
    /// Tamaño para ventana de edición de parte (ParteItemEdit)
    /// Aumentado para acomodar todos los campos sin scroll horizontal
    /// </summary>
    public static readonly (int Width, int Height) ParteEditSize = (1800, 1150);
    
    /// <summary>
    /// Tamaño para GraficaPage
    /// </summary>
    public static readonly (int Width, int Height) GraficaSize = (1200, 800);
    
    /// <summary>
    /// Tamaño para RegisterPage
    /// </summary>
    public static readonly (int Width, int Height) RegisterSize = (1200, 750);
    
    /// <summary>
    /// Tamaño para ForgotPasswordPage
    /// </summary>
    public static readonly (int Width, int Height) ForgotPasswordSize = (1100, 650);
    
    // ===== MÉTODOS PÚBLICOS =====
    
    /// <summary>
    /// Establece el tamaño de la ventana principal según la página actual
    /// </summary>
    public static void SetSizeForPage(Window window, Type pageType)
    {
        var size = GetSizeForPageType(pageType);
        SetWindowSizeAndCenter(window, size.Width, size.Height);
    }
    
    /// <summary>
    /// Establece el tamaño de una ventana child (ParteItemEdit, Gráfica)
    /// </summary>
    public static void SetChildWindowSize(Window window, int width, int height, bool resizable = false, bool maximizable = false)
    {
        try
        {
            var hwnd = WindowNative.GetWindowHandle(window);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            
            if (appWindow != null)
            {
                // Obtener área de trabajo del monitor principal
                var displayArea = DisplayArea.Primary;
                var workArea = displayArea.WorkArea;
                
                // Centrar la ventana
                int x = workArea.X + (workArea.Width - width) / 2;
                int y = workArea.Y + (workArea.Height - height) / 2;
                
                appWindow.MoveAndResize(new Windows.Graphics.RectInt32(x, y, width, height));
                
                // Configurar si es redimensionable/maximizable
                if (appWindow.Presenter is OverlappedPresenter presenter)
                {
                    presenter.IsResizable = resizable;
                    presenter.IsMaximizable = maximizable;
                }
                
                App.Log?.LogInformation("Ventana child configurada: {width}x{height} centrada en ({x},{y}) (resizable:{resizable}, maximizable:{maximizable})",
                    width, height, x, y, resizable, maximizable);
            }
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error configurando tamaño de ventana child");
        }
    }
    
    // ===== MÉTODOS PRIVADOS =====
    
    /// <summary>
    /// Obtiene el tamaño apropiado según el tipo de página
    /// </summary>
    private static (int Width, int Height) GetSizeForPageType(Type pageType)
    {
        var pageName = pageType.Name;
        
        return pageName switch
        {
            "LoginPage" => LoginSize,
            "DiarioPage" => DiarioSize,
            "ParteItemEdit" => ParteEditSize,
            "GraficaDiaPage" => GraficaSize,
            "RegisterPage" => RegisterSize,
            "ForgotPasswordPage" => ForgotPasswordSize,
            _ => DiarioSize // Por defecto, tamaño de Diario
        };
    }
    
    /// <summary>
    /// Establece el tamaño de una ventana Y LA CENTRA en pantalla
    /// </summary>
    private static void SetWindowSizeAndCenter(Window window, int width, int height)
    {
        try
        {
            var hwnd = WindowNative.GetWindowHandle(window);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            
            if (appWindow != null)
            {
                // Obtener área de trabajo del monitor principal
                var displayArea = DisplayArea.Primary;
                var workArea = displayArea.WorkArea;
                
                // Calcular posición centrada
                int x = workArea.X + (workArea.Width - width) / 2;
                int y = workArea.Y + (workArea.Height - height) / 2;
                
                // Asegurar que la ventana no se salga de los límites
                x = Math.Max(workArea.X, Math.Min(x, workArea.X + workArea.Width - width));
                y = Math.Max(workArea.Y, Math.Min(y, workArea.Y + workArea.Height - height));
                
                // Mover Y redimensionar (MoveAndResize centra automáticamente)
                appWindow.MoveAndResize(new Windows.Graphics.RectInt32(x, y, width, height));
                
                App.Log?.LogInformation("Ventana redimensionada a {width}x{height} y centrada en ({x},{y})", 
                    width, height, x, y);
            }
            else
            {
                App.Log?.LogWarning("No se pudo obtener AppWindow para redimensionar y centrar");
            }
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error estableciendo tamaño y centrando ventana");
        }
    }
}
