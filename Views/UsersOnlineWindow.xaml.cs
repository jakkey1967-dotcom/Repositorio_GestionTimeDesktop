using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.Logging;
using GestionTime.Desktop.ViewModels;

namespace GestionTime.Desktop.Views;

/// <summary>Ventana flotante que muestra usuarios online/offline del sistema.</summary>
public sealed partial class UsersOnlineWindow : Window
{
    private readonly UsersOnlineViewModel _viewModel;
    private readonly ILogger? _log;

    public UsersOnlineWindow()
    {
        InitializeComponent();

        _log = App.Log;
        _viewModel = new UsersOnlineViewModel(DispatcherQueue);

        Title = "Usuarios Online - GestionTime";

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(null);

        Closed += OnWindowClosed;
        
        // 🆕 NUEVO: Iniciar docking cuando la ventana se active
        Activated += OnWindowActivated;

        _log?.LogInformation("📂 UsersOnlineWindow creada");

        _ = InitializeAsync();
    }
    
    /// <summary>
    /// 🆕 NUEVO: Cuando la ventana se activa por primera vez, iniciar el docking
    /// </summary>
    private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        // Solo iniciar docking la primera vez que se activa
        if (args.WindowActivationState != WindowActivationState.Deactivated)
        {
            Activated -= OnWindowActivated; // Desuscribirse después de la primera vez
            
            try
            {
                App.Log?.LogInformation("🔗 Iniciando docking de UsersOnlineWindow...");
                App.MainWindowInstance?.AttachUsersOnlineWindow();
            }
            catch (Exception ex)
            {
                App.Log?.LogError(ex, "❌ Error iniciando docking en Activated");
            }
        }
    }

    private async System.Threading.Tasks.Task InitializeAsync()
    {
        try
        {
            ShowLoading();

            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            await _viewModel.LoadAsync();

            // 🆕 ACTUALIZADO: Verificar colección agrupada
            if (_viewModel.GroupedUsers.Any())
            {
                ShowUsersList();
                UpdateSubtitle();
                _viewModel.StartRefreshTimer();
            }
            else if (!string.IsNullOrEmpty(_viewModel.ErrorMessage))
            {
                ShowError(_viewModel.ErrorMessage);
            }
            else
            {
                ShowError("No hay usuarios disponibles.");
            }
        }
        catch (Exception ex)
        {
            _log?.LogError(ex, "❌ Error inicializando UsersOnlineWindow");
            ShowError("Error al cargar usuarios.");
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            // 🆕 ACTUALIZADO: Escuchar cambios en GroupedUsers
            if (e.PropertyName == nameof(UsersOnlineViewModel.GroupedUsers))
            {
                UsersListView.ItemsSource = _viewModel.GroupedUsers;
                UpdateSubtitle();
            }
            else if (e.PropertyName == nameof(UsersOnlineViewModel.IsLoading))
            {
                if (_viewModel.IsLoading)
                    ShowLoading();
                else if (_viewModel.GroupedUsers.Any())
                    ShowUsersList();
            }
            else if (e.PropertyName == nameof(UsersOnlineViewModel.ErrorMessage))
            {
                if (!string.IsNullOrEmpty(_viewModel.ErrorMessage))
                    ShowError(_viewModel.ErrorMessage);
            }
        });
    }

    private void ShowLoading()
    {
        LoadingPanel.Visibility = Visibility.Visible;
        ErrorPanel.Visibility = Visibility.Collapsed;
        UsersScrollViewer.Visibility = Visibility.Collapsed;
    }

    private void ShowUsersList()
    {
        LoadingPanel.Visibility = Visibility.Collapsed;
        ErrorPanel.Visibility = Visibility.Collapsed;
        UsersScrollViewer.Visibility = Visibility.Visible;
        
        // 🆕 ACTUALIZADO: Bindear colección agrupada
        UsersListView.ItemsSource = _viewModel.GroupedUsers;
    }

    private void ShowError(string message)
    {
        LoadingPanel.Visibility = Visibility.Collapsed;
        ErrorPanel.Visibility = Visibility.Visible;
        UsersScrollViewer.Visibility = Visibility.Collapsed;
        TxtError.Text = message;
    }

    private void UpdateSubtitle()
    {
        // 🆕 ACTUALIZADO: Contar usuarios desde grupos
        var totalUsers = _viewModel.GroupedUsers.SelectMany(g => g.Users).ToList();
        var total = totalUsers.Count;
        var online = totalUsers.Count(u => u.IsOnline);
        
        TxtSubtitle.Text = $"{online} de {total} usuarios online";

        _log?.LogDebug("📊 Usuarios actualizados: {online}/{total} online", online, total);
    }

    /// <summary>Maneja el click del botón de refresh manual.</summary>
    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        try
        {
            _log?.LogInformation("🔄 Refresh manual solicitado por el usuario");

            // Deshabilitar botón temporalmente
            BtnRefresh.IsEnabled = false;

            // Iniciar animación de rotación
            RefreshAnimation.Begin();

            // Actualizar subtítulo
            TxtSubtitle.Text = "Actualizando...";

            // Llamar al refresh del ViewModel
            await _viewModel.RefreshAsync();

            // Actualizar subtítulo con resultados
            UpdateSubtitle();

            _log?.LogInformation("✅ Refresh manual completado");
        }
        catch (Exception ex)
        {
            _log?.LogError(ex, "❌ Error en refresh manual");
            TxtSubtitle.Text = "Error al actualizar";
        }
        finally
        {
            // Esperar a que termine la animación antes de re-habilitar
            await System.Threading.Tasks.Task.Delay(600);
            BtnRefresh.IsEnabled = true;
        }
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        _log?.LogInformation("📂 UsersOnlineWindow cerrada");

        _viewModel.StopRefreshTimer();
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel.Dispose();

        App.UsersWindowInstance = null;
    }
}
