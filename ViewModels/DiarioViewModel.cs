using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using GestionTime.Desktop.Models.Dtos;
using GestionTime.Desktop;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;

namespace GestionTime.Desktop.ViewModels;

public partial class DiarioViewModel : ObservableObject
{
    private readonly DispatcherQueue? _dispatcherQueue;
    private Timer? _serviceCheckTimer;

    public ObservableCollection<ParteDto> Partes { get; } = new();

    [ObservableProperty]
    private string title = "Gestor de Tareas";

    [ObservableProperty]
    private string userName = "Usuario";

    [ObservableProperty]
    private string userEmail = "usuario@empresa.com";

    [ObservableProperty]
    private string userRole = "Usuario";

    // 🆕 NUEVO: Propiedades para mostrar en el banner (dinámicas desde perfil)
    [ObservableProperty]
    private string displayName = "Usuario";

    [ObservableProperty]
    private string displayEmail = "usuario@empresa.com";

    [ObservableProperty]
    private string displayPhone = "";

    [ObservableProperty]
    private bool isServiceOnline = false;

    [ObservableProperty]
    private bool isCheckingService = false;

    [ObservableProperty]
    private string lastCheckTime = "--:--";

    [ObservableProperty]
    private bool isBusy = false;

    public DiarioViewModel()
    {
        // Obtener el DispatcherQueue actual
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        
        // Iniciar chequeo automático del servicio cada 10 segundos
        StartServiceMonitoring();
    }

    /// <summary>
    /// Inicia el monitoreo automático del estado del servicio
    /// </summary>
    public void StartServiceMonitoring()
    {
        // Hacer un chequeo inicial inmediato
        _ = CheckServiceAsync();

        // Configurar timer para chequeos periódicos cada 10 segundos
        _serviceCheckTimer = new Timer(
            async _ => await CheckServiceAsync(),
            null,
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(10)
        );

        App.Log?.LogInformation("🔄 Monitoreo de servicio iniciado (cada 10 segundos)");
    }

    /// <summary>
    /// Detiene el monitoreo del servicio
    /// </summary>
    public void StopServiceMonitoring()
    {
        _serviceCheckTimer?.Dispose();
        _serviceCheckTimer = null;
        App.Log?.LogInformation("⏹️ Monitoreo de servicio detenido");
    }

    /// <summary>
    /// Verifica el estado del servicio (API)
    /// </summary>
    public async Task CheckServiceAsync()
    {
        try
        {
            IsCheckingService = true;

            // Intentar hacer ping a la API
            var isOnline = await App.Api.PingAsync();

            // Actualizar en el hilo de UI
            if (_dispatcherQueue != null)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    IsServiceOnline = isOnline;
                    LastCheckTime = DateTime.Now.ToString("HH:mm:ss");
                    
                    App.Log?.LogDebug("🌐 Estado del servicio: {status} a las {time}", 
                        isOnline ? "ONLINE ✅" : "OFFLINE ❌", 
                        LastCheckTime);
                });
            }
        }
        catch (Exception ex)
        {
            if (_dispatcherQueue != null)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    IsServiceOnline = false;
                    LastCheckTime = DateTime.Now.ToString("HH:mm:ss");
                });
            }

            App.Log?.LogWarning(ex, "⚠️ Error verificando estado del servicio");
        }
        finally
        {
            if (_dispatcherQueue != null)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    IsCheckingService = false;
                });
            }
        }
    }

    /// <summary>
    /// Actualiza la información del usuario logueado
    /// </summary>
    public void SetUserInfo(string name, string email, string role)
    {
        UserName = name;
        UserEmail = email;
        UserRole = role;
        App.Log?.LogInformation("👤 Usuario actualizado: {name} ({email}) - Rol: {role}", name, email, role);
    }

    /// <summary>Referencia a la colección de partes visible (inyectada desde DiarioPage).</summary>
    public ObservableCollection<ParteDto>? PartesVisibles { get; set; }

    /// <summary>Comando para exportar partes a Excel por semana.</summary>
    [RelayCommand]
    private async Task ExportWeekAsync()
    {
        // La implementación está en DiarioPage.xaml.cs para acceder al XamlRoot
        // Este comando solo sirve como punto de enlace desde el XAML
        await Task.CompletedTask;
    }
}

