using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.Logging;
using System;
using GestionTime.Desktop.Services;
using Microsoft.UI.Windowing;
using WinRT.Interop;
using Microsoft.UI;

namespace GestionTime.Desktop;

public sealed partial class MainWindow : Window
{
    public Frame Navigator => RootFrame;
    
    // 🆕 NUEVO: Rastrea el tipo de página actual para guardado correcto
    private Type? _currentPageType;
    
    // 🆕 NUEVO: Flag para evitar loops en el cierre
    private bool _isClosingHandled = false;

    public MainWindow()
    {
        InitializeComponent();
        if (Content is FrameworkElement root)
        {
            ThemeService.Instance.ApplyTheme(root);
        }
        ThemeService.Instance.ThemeChanged += OnThemeChanged;
        Closed += OnMainWindowClosed;
        
        Title = "GestionTime Desktop";

        App.Log?.LogInformation("MainWindow inicializada");

        // Escuchar cambios de navegación para ajustar tamaño automáticamente
        RootFrame.Navigated += OnFrameNavigated;
        
        // 🆕 NUEVO: Suscribirse al evento Closing para interceptar cierre de ventana
        SubscribeToWindowClosing();
        
        // Cargar LoginPage al iniciar (el tamaño se ajustará automáticamente)
        RootFrame.Navigate(typeof(Views.LoginPage));
    }

    private void OnThemeChanged(object? sender, ElementTheme theme)
    {
        if (Content is FrameworkElement root)
        {
            root.RequestedTheme = theme;
        }
    }

    private void OnMainWindowClosed(object sender, WindowEventArgs e)
    {
        ThemeService.Instance.ThemeChanged -= OnThemeChanged;
        Closed -= OnMainWindowClosed;
    }

    /// <summary>
    /// Se ejecuta cada vez que navegamos a una nueva página
    /// Ajusta automáticamente el tamaño de la ventana según la página
    /// </summary>
    private void OnFrameNavigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        if (e.SourcePageType != null)
        {
            // 🆕 NUEVO: Guardar el tipo de página actual
            _currentPageType = e.SourcePageType;
            
            App.Log?.LogInformation("📐 Navegando a {page}, ajustando tamaño de ventana...", e.SourcePageType.Name);
            
            // Pasar la página correcta al WindowSizeManager
            WindowSizeManager.SetSizeForPage(this, e.SourcePageType, _currentPageType);
        }
    }
    
    /// <summary>
    /// 🆕 NUEVO: Obtiene el tipo de página actualmente visible
    /// </summary>
    public Type? GetCurrentPageType()
    {
        return _currentPageType;
    }

    /// <summary>
    /// 🆕 NUEVO: Suscribe al evento Closing de AppWindow para interceptar el cierre
    /// </summary>
    private void SubscribeToWindowClosing()
    {
        try
        {
            var hWnd = WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            if (appWindow != null)
            {
                appWindow.Closing += OnAppWindowClosing;
                App.Log?.LogInformation("✅ Suscrito al evento AppWindow.Closing para interceptar cierre de ventana");
            }
            else
            {
                App.Log?.LogWarning("⚠️ No se pudo obtener AppWindow para suscribirse al evento Closing");
            }
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error suscribiéndose al evento Closing");
        }
    }

    /// <summary>
    /// 🆕 NUEVO: Intercepta el cierre de la ventana (X / Alt+F4)
    /// Muestra dialog de confirmación y ejecuta logout en lugar de cerrar la app
    /// </summary>
    private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        // Evitar loops si ya estamos manejando el cierre
        if (_isClosingHandled)
        {
            return;
        }

        try
        {
            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
            App.Log?.LogInformation("🚪 CIERRE DE VENTANA INTERCEPTADO (X / Alt+F4)");
            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");

            // Si estamos en LoginPage, limpiar recursos y permitir el cierre de la app
            if (_currentPageType == typeof(Views.LoginPage))
            {
                App.Log?.LogInformation("✅ Actualmente en LoginPage - Limpiando recursos antes de cerrar");
                
                // 🔧 FIX ACCESS VIOLATION: Limpieza global ANTES de cerrar
                try
                {
                    App.CleanupResources();
                    App.Log?.LogInformation("✅ Recursos globales limpiados desde LoginPage");
                }
                catch (Exception cleanupEx)
                {
                    App.Log?.LogError(cleanupEx, "Error en limpieza desde LoginPage (no crítico)");
                }
                
                App.Log?.LogInformation("👋 BYE - Cerrando aplicación");
                return; // NO cancelar, permitir cierre
            }

            // Cancelar el cierre de la ventana
            args.Cancel = true;
            App.Log?.LogInformation("⏸️ Cierre cancelado - Mostrando dialog de confirmación");

            // Ejecutar el flujo de logout de forma asíncrona
            _isClosingHandled = true;
            _ = ExecuteLogoutFlowAsync();
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error interceptando cierre de ventana");
            // En caso de error, permitir el cierre
            args.Cancel = false;
        }
    }

    /// <summary>
    /// 🆕 NUEVO: Ejecuta el flujo completo de logout (confirmación + limpieza + navegación)
    /// Reutilizable desde botón Salir o cierre de ventana (X)
    /// </summary>
    private async System.Threading.Tasks.Task ExecuteLogoutFlowAsync()
    {
        try
        {
            App.Log?.LogInformation("🔐 Iniciando flujo de logout...");

            // Mostrar dialog de confirmación
            var confirmed = await ConfirmLogoutAsync();

            if (confirmed)
            {
                App.Log?.LogInformation("✅ Logout confirmado por el usuario");
                await PerformLogoutAsync();
            }
            else
            {
                App.Log?.LogInformation("❌ Logout cancelado por el usuario");
            }
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error en flujo de logout");
        }
        finally
        {
            // Resetear flag para permitir futuros intentos
            _isClosingHandled = false;
        }
    }

    /// <summary>
    /// 🆕 NUEVO: Muestra el ContentDialog de confirmación de logout
    /// Retorna true si confirma, false si cancela
    /// </summary>
    private async System.Threading.Tasks.Task<bool> ConfirmLogoutAsync()
    {
        try
        {
            // Necesitamos obtener el XamlRoot desde el contenido de la ventana
            if (RootFrame.Content is not FrameworkElement currentPage)
            {
                App.Log?.LogWarning("⚠️ No se pudo obtener XamlRoot - Página actual no es FrameworkElement");
                return false;
            }

            var confirmDialog = new ContentDialog
            {
                Title = "Cerrar sesión",
                Content = "¿Estás seguro de que deseas cerrar la sesión?",
                PrimaryButtonText = "Cerrar sesión",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = currentPage.XamlRoot,
                RequestedTheme = ThemeService.Instance.CurrentTheme
            };

            var result = await confirmDialog.ShowAsync();

            return result == ContentDialogResult.Primary;
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error mostrando dialog de confirmación");
            return false;
        }
    }

    /// <summary>
    /// 🆕 NUEVO: Ejecuta la limpieza de sesión y navega a LoginPage
    /// </summary>
    private async System.Threading.Tasks.Task PerformLogoutAsync()
    {
        try
        {
            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
            App.Log?.LogInformation("🚪 LOGOUT - Limpiando sesión y datos");
            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");

            // 🆕 NUEVO: Detener docking service antes de cerrar ventana
            DetachUsersOnlineWindow();

            // 1️⃣ Cerrar ventana de Usuarios Online si está abierta
            CloseUsersOnlineWindow();
            
            // 🆕 NUEVO: Detener heartbeat de presencia
            try
            {
                App.PresenceHeartbeat.Stop();
                App.Log?.LogInformation("✅ Heartbeat de presencia detenido");
            }
            catch (Exception heartbeatEx)
            {
                App.Log?.LogError(heartbeatEx, "Error deteniendo heartbeat");
            }
            
            // 🔧 FIX ACCESS VIOLATION: Limpieza global de recursos
            try
            {
                App.CleanupResources();
                App.Log?.LogInformation("✅ Recursos globales limpiados");
            }
            catch (Exception cleanupEx)
            {
                App.Log?.LogError(cleanupEx, "Error en limpieza global de recursos");
            }

            // 2️⃣ Limpiar información del usuario
            try
            {
                Helpers.UserInfoFileStorage.ClearUserInfo(App.Log);
                App.Log?.LogInformation("✅ Información de usuario limpiada del archivo");
            }
            catch (Exception fileEx)
            {
                App.Log?.LogError(fileEx, "Error limpiando archivo de usuario");
            }

            // 3️⃣ Limpiar token y caché de API
            if (App.Api != null)
            {
                App.Api.ClearToken();
                App.Log?.LogInformation("✅ Token de autenticación eliminado");

                App.Api.ClearGetCache();

                App.Log?.LogInformation("✅ Caché de peticiones limpiado");
            }

            // 4️⃣ Limpiar perfil del usuario actual
            App.CurrentUserProfile = null;
            App.CurrentLoginEmail = null;
            App.CurrentAuthenticatedUser = null;
            App.Log?.LogInformation("✅ Datos de sesión limpiados");

            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
            App.Log?.LogInformation("✅ LOGOUT COMPLETADO - Navegando al login");
            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");

            // 5️⃣ Navegar a LoginPage
            await System.Threading.Tasks.Task.Delay(100); // Pequeña pausa para asegurar que se procesen los cambios
            
            DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    RootFrame.Navigate(typeof(Views.LoginPage));
                    App.Log?.LogInformation("✅ Navegación a LoginPage exitosa");
                }
                catch (Exception navEx)
                {
                    App.Log?.LogError(navEx, "❌ Error navegando a LoginPage");
                }
            });
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error durante el logout");
        }
    }

    /// <summary>
    /// 🆕 NUEVO: Cierra la ventana de Usuarios Online si está abierta
    /// </summary>
    private void CloseUsersOnlineWindow()
    {
        try
        {
            if (App.UsersWindowInstance != null)
            {
                App.Log?.LogInformation("🔒 Cerrando ventana de Usuarios Online...");
                
                try
                {
                    App.UsersWindowInstance.Close();
                    App.Log?.LogInformation("✅ Ventana de Usuarios Online cerrada correctamente");
                }
                catch (Exception ex)
                {
                    App.Log?.LogWarning(ex, "⚠️ Error cerrando ventana de Usuarios Online (puede ya estar cerrada)");
                }
                finally
                {
                    App.UsersWindowInstance = null;
                }
            }
            else
            {
                App.Log?.LogDebug("ℹ️ No hay ventana de Usuarios Online abierta");
            }
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error verificando/cerrando ventana de Usuarios Online");
        }
    }

    /// <summary>
    /// 🆕 NUEVO: Método público para ejecutar logout desde otros lugares (ej: botón Salir)
    /// </summary>
    public async System.Threading.Tasks.Task RequestLogoutAsync()
    {
        await ExecuteLogoutFlowAsync();
    }

    /// <summary>
    /// 🆕 NUEVO: Inicia el docking de la ventana de usuarios online (se llama cuando se abre la ventana)
    /// </summary>
    public void AttachUsersOnlineWindow()
    {
        try
        {
            if (App.UsersWindowInstance == null)
            {
                App.Log?.LogWarning("⚠️ No se puede attached docking: UsersWindowInstance es null");
                return;
            }

            // Crear servicio si no existe
            if (App.WindowDockService == null)
            {
                App.WindowDockService = new Services.Windowing.WindowDockService();
            }

            // Attach docking
            App.WindowDockService.Attach(this, App.UsersWindowInstance);

            App.Log?.LogInformation("✅ Docking de UsersOnlineWindow iniciado correctamente");
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error iniciando docking de UsersOnlineWindow");
        }
    }

    /// <summary>
    /// 🆕 NUEVO: Detiene el docking de la ventana de usuarios online
    /// </summary>
    public void DetachUsersOnlineWindow()
    {
        try
        {
            if (App.WindowDockService != null)
            {
                App.WindowDockService.Detach();
                App.Log?.LogInformation("✅ Docking de UsersOnlineWindow detenido correctamente");
            }
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error deteniendo docking de UsersOnlineWindow");
        }
    }
}

