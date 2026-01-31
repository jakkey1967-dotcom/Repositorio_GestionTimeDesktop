using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.Logging;
using GestionTime.Desktop.ViewModels;

namespace GestionTime.Desktop.Views.Controls;

/// <summary>Panel de usuarios online integrado en DiarioPage.</summary>
public sealed partial class OnlineUsersPanel : UserControl
{
    private readonly ILogger? _log;
    private OnlineUsersPanelViewModel? _viewModel;

    public OnlineUsersPanel()
    {
        try
        {
            this.InitializeComponent();
            _log = App.Log;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error en OnlineUsersPanel constructor: {ex.Message}");
        }
    }

    /// <summary>Inicializa el ViewModel y carga datos.</summary>
    public void Initialize(OnlineUsersPanelViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        _ = LoadAsync();
    }

    /// <summary>Limpia recursos al cerrar.</summary>
    public void Cleanup()
    {
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.StopRefreshTimer();
        }
    }

    private async System.Threading.Tasks.Task LoadAsync()
    {
        if (_viewModel == null) return;

        ShowLoading();

        await _viewModel.LoadAsync();

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
            ShowError("No hay usuarios disponibles");
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (e.PropertyName == nameof(OnlineUsersPanelViewModel.GroupedUsers))
            {
                UsersListView.ItemsSource = _viewModel?.GroupedUsers;
                UpdateSubtitle();
            }
            else if (e.PropertyName == nameof(OnlineUsersPanelViewModel.IsLoading))
            {
                if (_viewModel?.IsLoading == true)
                    ShowLoading();
                else if (_viewModel?.GroupedUsers.Any() == true)
                    ShowUsersList();
            }
            else if (e.PropertyName == nameof(OnlineUsersPanelViewModel.ErrorMessage))
            {
                if (!string.IsNullOrEmpty(_viewModel?.ErrorMessage))
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

        UsersListView.ItemsSource = _viewModel?.GroupedUsers;
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
        if (_viewModel == null) return;

        var totalUsers = _viewModel.GroupedUsers.SelectMany(g => g.Users).ToList();
        var total = totalUsers.Count;
        var online = totalUsers.Count(u => u.IsOnline);

        TxtSubtitle.Text = $"Online: {online} · Total: {total}";
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;

        try
        {
            _log?.LogInformation("🔄 Refresh manual desde panel integrado");

            BtnRefresh.IsEnabled = false;
            RefreshAnimation.Begin();
            TxtSubtitle.Text = "Actualizando...";

            await _viewModel.RefreshAsync();

            UpdateSubtitle();

            _log?.LogInformation("✅ Refresh completado");
        }
        catch (Exception ex)
        {
            _log?.LogError(ex, "❌ Error en refresh manual");
            TxtSubtitle.Text = "Error al actualizar";
        }
        finally
        {
            await System.Threading.Tasks.Task.Delay(600);
            BtnRefresh.IsEnabled = true;
        }
    }
}
