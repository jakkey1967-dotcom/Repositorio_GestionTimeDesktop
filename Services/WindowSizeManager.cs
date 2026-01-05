using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.Extensions.Logging;
using System;
using WinRT.Interop;

namespace GestionTime.Desktop.Services;

/// <summary>
/// Servicio centralizado para gestionar tamaños de ventana de forma consistente
/// Soporta configuración personalizada desde window-config.ini
/// </summary>
public static class WindowSizeManager
{
    // ===== TAMAÑOS POR DEFECTO (FALLBACK) =====
    
    private static readonly (int Width, int Height) DefaultLoginSize = (1110, 760);
    private static readonly (int Width, int Height) DefaultDiarioSize = (1600, 950);
    private static readonly (int Width, int Height) DefaultParteEditSize = (1400, 900);
    private static readonly (int Width, int Height) DefaultGraficaSize = (1200, 800);
    private static readonly (int Width, int Height) DefaultRegisterSize = (1200, 750);
    private static readonly (int Width, int Height) DefaultForgotPasswordSize = (1100, 650);
    // 🆕 NUEVO: Tamaño por defecto para UserProfilePage
    private static readonly (int Width, int Height) DefaultUserProfileSize = (1300, 850);
    
    // ===== PROPIEDADES PÚBLICAS (CON CONFIG INI) =====
    
    /// <summary>
    /// Tamaño para LoginPage (carga desde INI o usa default)
    /// </summary>
    public static (int Width, int Height) LoginSize => 
        WindowConfigService.Instance.GetSizeForPage("LoginPage") ?? DefaultLoginSize;
    
    /// <summary>
    /// Tamaño para DiarioPage (carga desde INI o usa default)
    /// </summary>
    public static (int Width, int Height) DiarioSize => 
        WindowConfigService.Instance.GetSizeForPage("DiarioPage") ?? DefaultDiarioSize;
    
    /// <summary>
    /// Tamaño para ParteItemEdit (carga desde INI o usa default)
    /// </summary>
    public static (int Width, int Height) ParteEditSize => 
        WindowConfigService.Instance.GetSizeForPage("ParteItemEdit") ?? DefaultParteEditSize;
    
    /// <summary>
    /// Tamaño para GraficaPage (carga desde INI o usa default)
    /// </summary>
    public static (int Width, int Height) GraficaSize => 
        WindowConfigService.Instance.GetSizeForPage("GraficaDiaPage") ?? DefaultGraficaSize;
    
    /// <summary>
    /// Tamaño para RegisterPage (carga desde INI o usa default)
    /// </summary>
    public static (int Width, int Height) RegisterSize => 
        WindowConfigService.Instance.GetSizeForPage("RegisterPage") ?? DefaultRegisterSize;
    
    /// <summary>
    /// Tamaño para ForgotPasswordPage (carga desde INI o usa default)
    /// </summary>
    public static (int Width, int Height) ForgotPasswordSize => 
        WindowConfigService.Instance.GetSizeForPage("ForgotPasswordPage") ?? DefaultForgotPasswordSize;
    
    /// <summary>
    /// 🆕 NUEVO: Tamaño para UserProfilePage (carga desde INI o usa default)
    /// </summary>
    public static (int Width, int Height) UserProfileSize => 
        WindowConfigService.Instance.GetSizeForPage("UserProfilePage") ?? DefaultUserProfileSize;
    
    // ===== MÉTODOS PÚBLICOS =====
    
    /// <summary>
    /// Establece el tamaño de la ventana principal según la página actual
    /// </summary>
    public static void SetSizeForPage(Window window, Type pageType, Type? currentPageType = null)
    {
        var size = GetSizeForPageType(pageType);
        SetWindowSizeAndCenter(window, size.Width, size.Height);
        
        // 🆕 MODIFICADO: Pasar el pageType correcto al registro de atajo
        RegisterSaveHotkey(window, currentPageType ?? pageType);
    }
    
    /// <summary>
    /// Guarda el tamaño actual de la ventana en window-config.ini
    /// </summary>
    public static void SaveCurrentWindowSize(Window window, Type pageType)
    {
        try
        {
            App.Log?.LogInformation("════════════════════════════════════════════════════════════════");
            App.Log?.LogInformation("💾 GUARDANDO TAMAÑO DE VENTANA");
            App.Log?.LogInformation("════════════════════════════════════════════════════════════════");
            
            var hwnd = WindowNative.GetWindowHandle(window);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            
            if (appWindow != null)
            {
                var size = appWindow.Size;
                var pageName = pageType.Name;
                
                App.Log?.LogInformation("📝 Datos capturados:");
                App.Log?.LogInformation("   • Página: {page}", pageName);
                App.Log?.LogInformation("   • Ancho: {width}px", size.Width);
                App.Log?.LogInformation("   • Alto: {height}px", size.Height);
                
                // 🔍 Verificar el estado ANTES de guardar
                var sizeBefore = WindowConfigService.Instance.GetSizeForPage(pageName);
                if (sizeBefore.HasValue)
                {
                    App.Log?.LogInformation("   ℹ️ Tamaño anterior: {width}x{height}", 
                        sizeBefore.Value.Width, sizeBefore.Value.Height);
                }
                else
                {
                    App.Log?.LogInformation("   ℹ️ No había tamaño guardado previamente");
                }
                
                WindowConfigService.Instance.SaveSizeForPage(pageName, size.Width, size.Height);
                
                // 🔍 VERIFICAR inmediatamente después de guardar
                var sizeAfter = WindowConfigService.Instance.GetSizeForPage(pageName);
                if (sizeAfter.HasValue)
                {
                    App.Log?.LogInformation("✅ Verificación: Tamaño guardado correctamente: {width}x{height}", 
                        sizeAfter.Value.Width, sizeAfter.Value.Height);
                    
                    if (sizeAfter.Value.Width == size.Width && sizeAfter.Value.Height == size.Height)
                    {
                        App.Log?.LogInformation("   ✓ Tamaño coincide con el esperado");
                    }
                    else
                    {
                        App.Log?.LogWarning("   ⚠️ Tamaño NO coincide! Esperado: {expW}x{expH}, Guardado: {actW}x{actH}",
                            size.Width, size.Height, sizeAfter.Value.Width, sizeAfter.Value.Height);
                    }
                }
                else
                {
                    App.Log?.LogError("   ❌ ERROR: No se pudo leer el tamaño después de guardar!");
                }
                
                App.Log?.LogInformation("════════════════════════════════════════════════════════════════");
                
                // Mostrar notificación al usuario
                ShowSaveNotification(window, pageName, size.Width, size.Height);
            }
            else
            {
                App.Log?.LogError("❌ No se pudo obtener AppWindow para guardar tamaño");
            }
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error guardando tamaño de ventana");
        }
    }
    
    /// <summary>
    /// Establece el tamaño de una ventana child (ParteItemEdit, Gráfica)
    /// 🆕 NUEVO: Ahora también registra el atajo Ctrl+Alt+P
    /// </summary>
    public static void SetChildWindowSize(Window window, Type pageType, int width, int height, bool resizable = false, bool maximizable = false)
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
            
            // 🆕 NUEVO: REGISTRAR ATAJO Ctrl+Alt+P también para ventanas child
            RegisterSaveHotkey(window, pageType);
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
            "UserProfilePage" => UserProfileSize, // 🆕 NUEVO
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
    
    /// <summary>Registra el atajo de teclado Ctrl+Alt+P para guardar tamaño.</summary>
    private static void RegisterSaveHotkey(Window window, Type pageType)
    {
        try
        {
            if (window.Content is FrameworkElement rootElement)
            {
                // Crear handler que captura la ventana Y obtiene la página actual dinámicamente
                KeyEventHandler handler = (sender, e) =>
                {
                    try
                    {
                        // Verificar Ctrl+Alt+P
                        var ctrlState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control);
                        var altState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Menu);
                        
                        bool isCtrlPressed = (ctrlState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;
                        bool isAltPressed = (altState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;
                        
                        if (isCtrlPressed && isAltPressed && e.Key == Windows.System.VirtualKey.P)
                        {
                            // 🆕 CORREGIDO: Obtener la página ACTUAL desde MainWindow
                            Type? currentPage = null;
                            
                            if (window is MainWindow mainWindow)
                            {
                                currentPage = mainWindow.GetCurrentPageType();
                                App.Log?.LogInformation("🔍 Página actual detectada desde MainWindow: {page}", currentPage?.Name ?? "null");
                            }
                            else
                            {
                                // Fallback para ventanas child
                                currentPage = pageType;
                                App.Log?.LogInformation("🔍 Usando pageType para ventana child: {page}", pageType.Name);
                            }
                            
                            if (currentPage != null)
                            {
                                SaveCurrentWindowSize(window, currentPage);
                            }
                            else
                            {
                                App.Log?.LogWarning("⚠️ No se pudo determinar la página actual para guardar tamaño");
                            }
                            
                            e.Handled = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        App.Log?.LogError(ex, "Error en handler de atajo de teclado");
                    }
                };
                
                // Agregar handler
                rootElement.KeyDown += handler;
                
                App.Log?.LogDebug("✅ Atajo Ctrl+Alt+P registrado para {page}", pageType.Name);
            }
        }
        catch (Exception ex)
        {
            App.Log?.LogWarning(ex, "Error registrando atajo de teclado");
        }
    }
    
    /// <summary>Muestra una notificación temporal al usuario.</summary>
    private static async void ShowSaveNotification(Window window, string pageName, int width, int height)
    {
        try
        {
            var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
            {
                Title = "💾 Tamaño Guardado",
                Content = $"Tamaño de ventana guardado para {pageName}:\n\n" +
                         $"📏 {width} x {height} píxeles\n\n" +
                         $"📄 Archivo: window-config.ini\n\n" +
                         $"Este tamaño se usará la próxima vez que abras esta página.",
                CloseButtonText = "OK",
                XamlRoot = window.Content.XamlRoot
            };
            
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            App.Log?.LogWarning(ex, "Error mostrando notificación");
        }
    }
}
