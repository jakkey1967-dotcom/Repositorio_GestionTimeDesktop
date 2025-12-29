using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using GestionTime.Desktop.Helpers;
using Microsoft.Extensions.Logging;

namespace GestionTime.Desktop;

public sealed partial class MainWindow : Window
{
    public Frame Navigator => RootFrame;

    // ⚠️ AUTO-LOGIN para desarrollo - poner a false en producción
    private const bool AutoLoginEnabled = false;  // ← DESHABILITADO: usuario no existe
    private const string AutoLoginEmail = "psantos@global-retail.com";
    private const string AutoLoginPassword = "Nimda2008@2020";

    public MainWindow()
    {
        InitializeComponent();
        
        // Configurar la ventana DESPUÉS de InitializeComponent
        this.Activated += OnFirstActivated;
        
        // Escuchar cambios de navegación para ajustar el tamaño
        RootFrame.Navigated += OnFrameNavigated;
        
        // Navegar a LoginPage (se hará auto-login si está habilitado)
        RootFrame.Navigate(typeof(Views.LoginPage));
    }

    private async void OnFirstActivated(object sender, WindowActivatedEventArgs args)
    {
        // Solo ejecutar una vez
        this.Activated -= OnFirstActivated;
        
        try
        {
            App.Log?.LogInformation("MainWindow activándose, configurando tamaño inicial...");
            
            // Tamaño inicial para LoginPage (1050x720 píxeles físicos)
            WindowHelper.ResizeAndCenter(this, 1050, 720);
            
            App.Log?.LogInformation("MainWindow configurada correctamente");

            // Auto-login si está habilitado
            if (AutoLoginEnabled)
            {
                App.Log?.LogInformation("🔓 Auto-login habilitado, intentando login automático...");
                await PerformAutoLoginAsync();
            }
        }
        catch (System.Exception ex)
        {
            App.Log?.LogError(ex, "Error configurando MainWindow");
        }
    }

    private async System.Threading.Tasks.Task PerformAutoLoginAsync()
    {
        try
        {
            var result = await App.Api.LoginAsync(AutoLoginEmail, AutoLoginPassword);
            
            if (result != null)
            {
                App.Log?.LogInformation("✅ Auto-login exitoso, navegando a DiarioPage");
                RootFrame.Navigate(typeof(Views.DiarioPage));
            }
            else
            {
                App.Log?.LogWarning("⚠️ Auto-login falló, permaneciendo en LoginPage");
            }
        }
        catch (System.Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error en auto-login");
        }
    }

    private async void OnFrameNavigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        try
        {
            App.Log?.LogInformation("Navegación detectada a: {page}", e.SourcePageType.Name);

            // Pequeña espera para que la página se cargue antes de redimensionar
            await System.Threading.Tasks.Task.Delay(50);

            // Ajustar tamaño según la página
            if (e.SourcePageType == typeof(Views.LoginPage))
            {
                // LoginPage: ventana pequeña y fija
                WindowHelper.ResizeAndCenter(this, 1050, 720);
                WindowHelper.SetResizable(this, false);
                WindowHelper.SetMaximizable(this, false);
                App.Log?.LogInformation("Ventana ajustada para LoginPage (1050x720)");
            }
            else if (e.SourcePageType == typeof(Views.DiarioPage))
            {
                // DiarioPage: ventana grande y redimensionable
                WindowHelper.ResizeAndCenter(this, 1600, 900);
                WindowHelper.SetResizable(this, true);
                WindowHelper.SetMaximizable(this, true);
                App.Log?.LogInformation("Ventana ajustada para DiarioPage (1600x900)");
            }
        }
        catch (System.Exception ex)
        {
            App.Log?.LogError(ex, "Error ajustando tamaño de ventana tras navegación");
        }
    }
}

