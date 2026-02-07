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

/// <summary>ViewModel para la ventana de usuarios online/offline.</summary>
public sealed class UsersOnlineViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly ILogger? _log;
    private readonly DispatcherQueue _dispatcher;
    private CancellationTokenSource? _cts;
    private DispatcherQueueTimer? _refreshTimer;

    // 🆕 NUEVO: Colección agrupada por rol
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

    public UsersOnlineViewModel(DispatcherQueue dispatcher)
    {
        _log = App.Log;
        _dispatcher = dispatcher;
        _cts = new CancellationTokenSource();
    }

    /// <summary>Inicia el polling automático de usuarios (cada 15 segundos).</summary>
    public void StartRefreshTimer()
    {
        if (_refreshTimer != null)
            return;

        _refreshTimer = _dispatcher.CreateTimer();
        _refreshTimer.Interval = TimeSpan.FromSeconds(15);
        _refreshTimer.Tick += async (s, e) => await RefreshAsync();
        _refreshTimer.Start();

        _log?.LogInformation("⏰ Timer de refresh iniciado (15s)");
    }

    /// <summary>Detiene el polling automático.</summary>
    public void StopRefreshTimer()
    {
        if (_refreshTimer == null)
            return;

        _refreshTimer.Stop();
        _refreshTimer = null;

        _log?.LogInformation("⏰ Timer de refresh detenido");
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
            _log?.LogInformation("🔄 Cargando usuarios...");

            var users = await PresenceService.Instance.GetUsersAsync(_cts!.Token);

            if (!users.Any())
            {
                ErrorMessage = "No hay usuarios disponibles.";
                _log?.LogWarning("⚠️ No se encontraron usuarios");
                return;
            }

            // 🆕 NUEVO: Agrupar y ordenar por rol
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

    /// <summary>Refresca la lista de usuarios (actualiza estado online/offline).</summary>
    public async Task RefreshAsync()
    {
        if (_cts?.Token.IsCancellationRequested == true)
            return;

        try
        {
            _log?.LogDebug("🔄 Refrescando usuarios...");

            var users = await PresenceService.Instance.GetUsersAsync(_cts!.Token);

            if (!users.Any())
            {
                _log?.LogDebug("⚠️ No hay usuarios para refrescar");
                return;
            }

            // 🆕 NUEVO: Agrupar y ordenar por rol
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
            _log?.LogDebug("🚫 Refresh de usuarios cancelado");
        }
        catch (Exception ex)
        {
            _log?.LogWarning(ex, "⚠️ Error refrescando usuarios");
        }
    }

    /// <summary>Agrupa usuarios por rol en orden fijo (ADMIN/EDITOR/USER) y ordena dentro de cada grupo: Online primero, luego por FullName.</summary>
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

/// <summary>🆕 NUEVO: Grupo de usuarios por rol.</summary>
public sealed class UserRoleGroup
{
    public string GroupName { get; set; } = string.Empty;
    public ObservableCollection<UserCardItem> Users { get; set; } = new();
}

/// <summary>Modelo de vista para un usuario en la tarjeta.</summary>
public sealed class UserCardItem
{
    public Guid Id { get; }
    public string FullName { get; }
    public string Email { get; }
    public string Role { get; }
    public bool IsOnline { get; }
    public DateTime? LastSeenAt { get; }
    public string StatusText => IsOnline ? "Online" : "Offline";
    public string StatusBadgeBackground => IsOnline ? "#E0F7F9" : "#F5F5F5";
    public string StatusBadgeDotColor => IsOnline ? "#0FA7B6" : "#999999";
    public string StatusBadgeTextColor => IsOnline ? "#0FA7B6" : "#999999";
    public string RoleBadge { get; }

    /// <summary>Texto de timestamp: "Actualizado: ..." si online, "Última actividad: ..." si offline.</summary>
    public string DisplayStampText
    {
        get
        {
            if (LastSeenAt == null)
                return "—";

            var formatted = LastSeenAt.Value.ToString("dd/MM/yyyy HH:mm:ss");
            return IsOnline
                ? $"Actualizado: {formatted}"
                : $"Última actividad: {formatted}";
        }
    }

    public UserCardItem(PresenceUserDto dto)
    {
        Id = dto.UserId;
        FullName = dto.FullName;
        Email = dto.Email;
        Role = dto.Role;
        IsOnline = dto.IsOnline;
        LastSeenAt = dto.LastSeenAt;

        RoleBadge = dto.Role?.ToUpperInvariant() switch
        {
            "ADMIN" => "ADMIN",
            "EDITOR" => "EDITOR",
            _ => "USER"
        };
    }
}

