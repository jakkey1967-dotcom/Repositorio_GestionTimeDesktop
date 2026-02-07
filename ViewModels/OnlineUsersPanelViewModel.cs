using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using GestionTime.Desktop.Models.Dtos;
using GestionTime.Desktop.Services.Presence;
using System.Collections.Generic;

namespace GestionTime.Desktop.ViewModels;

/// <summary>ViewModel para el panel de usuarios online integrado en DiarioPage.</summary>
public sealed class OnlineUsersPanelViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly ILogger? _log;
    private readonly DispatcherQueue _dispatcher;
    private CancellationTokenSource? _cts;
    private DispatcherQueueTimer? _refreshTimer;

    public ObservableCollection<UserRoleGroup> GroupedUsers { get; } = new();

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isRefreshing;
    public bool IsRefreshing
    {
        get => _isRefreshing;
        set
        {
            if (_isRefreshing != value)
            {
                _isRefreshing = value;
                OnPropertyChanged();
            }
        }
    }

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (_errorMessage != value)
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public OnlineUsersPanelViewModel(DispatcherQueue dispatcher)
    {
        _log = App.Log;
        _dispatcher = dispatcher;
        _cts = new CancellationTokenSource();
    }

    /// <summary>Inicia el polling automático de usuarios (cada 30 segundos).</summary>
    public void StartRefreshTimer()
    {
        if (_refreshTimer != null)
            return;

        _refreshTimer = _dispatcher.CreateTimer();
        _refreshTimer.Interval = TimeSpan.FromSeconds(30);
        _refreshTimer.Tick += async (s, e) => await RefreshAsync();
        _refreshTimer.Start();

        _log?.LogInformation("⏰ Timer de refresh iniciado (30s) - Panel integrado");
    }

    /// <summary>Detiene el polling automático.</summary>
    public void StopRefreshTimer()
    {
        if (_refreshTimer == null)
            return;

        _refreshTimer.Stop();
        _refreshTimer = null;

        _log?.LogInformation("⏰ Timer de refresh detenido - Panel integrado");
    }

    /// <summary>Carga inicial de usuarios.</summary>
    public async Task LoadAsync()
    {
        if (_cts?.Token.IsCancellationRequested == true)
            return;

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            _log?.LogInformation("🔄 Cargando usuarios en panel integrado...");

            var users = await PresenceService.Instance.GetUsersAsync(_cts!.Token);

            if (!users.Any())
            {
                ErrorMessage = "No hay usuarios disponibles.";
                _log?.LogWarning("⚠️ No se encontraron usuarios");
                return;
            }

            var grouped = GroupAndSortUsers(users);

            _dispatcher.TryEnqueue(() =>
            {
                GroupedUsers.Clear();
                foreach (var group in grouped)
                {
                    GroupedUsers.Add(group);
                }
            });

            _log?.LogInformation("✅ Usuarios cargados: {count} usuarios en {groups} grupos", 
                users.Count, GroupedUsers.Count);
        }
        catch (OperationCanceledException)
        {
            _log?.LogDebug("🚫 Carga de usuarios cancelada");
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error al cargar usuarios.";
            _log?.LogError(ex, "❌ Error cargando usuarios");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Refresca la lista de usuarios.</summary>
    public async Task RefreshAsync()
    {
        if (_cts?.Token.IsCancellationRequested == true)
            return;

        // Evitar solapamientos
        if (IsRefreshing)
        {
            _log?.LogDebug("⏭️ Refresh ya en curso, saltando...");
            return;
        }

        IsRefreshing = true;

        try
        {
            _log?.LogDebug("🔄 Refrescando usuarios en panel integrado...");

            var users = await PresenceService.Instance.GetUsersAsync(_cts!.Token);

            if (!users.Any())
            {
                _log?.LogDebug("⚠️ No hay usuarios para refrescar");
                return;
            }

            var grouped = GroupAndSortUsers(users);

            _dispatcher.TryEnqueue(() =>
            {
                GroupedUsers.Clear();
                foreach (var group in grouped)
                {
                    GroupedUsers.Add(group);
                }
            });

            _log?.LogDebug("✅ Usuarios refrescados: {count} usuarios", users.Count);
        }
        catch (OperationCanceledException)
        {
            _log?.LogDebug("🚫 Refresh cancelado");
        }
        catch (Exception ex)
        {
            _log?.LogWarning(ex, "⚠️ Error refrescando usuarios");
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    /// <summary>Agrupa usuarios por rol y ordena: Online primero, luego por nombre.</summary>
    private List<UserRoleGroup> GroupAndSortUsers(List<PresenceUserDto> users)
    {
        var roleOrder = new Dictionary<string, int>
        {
            { "ADMIN", 1 },
            { "EDITOR", 2 },
            { "USER", 3 }
        };

        var grouped = users
            .GroupBy(u => u.Role?.ToUpperInvariant() ?? "USER")
            .OrderBy(g => roleOrder.ContainsKey(g.Key) ? roleOrder[g.Key] : 4)
            .Select(g => new UserRoleGroup
            {
                GroupName = g.Key,
                Users = new ObservableCollection<UserCardItem>(
                    g.OrderByDescending(u => u.IsOnline)
                     .ThenBy(u => u.FullName)
                     .Select(u => new UserCardItem(u))
                )
            })
            .ToList();

        return grouped;
    }

    public void Dispose()
    {
        StopRefreshTimer();
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
