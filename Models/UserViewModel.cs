using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace GestionTime.Desktop.Models;

/// <summary>ViewModel simplificado para mostrar usuarios en Settings (gestión inline de roles/enabled).</summary>
public sealed class UserViewModel : INotifyPropertyChanged
{
    private bool _isBusy;
    private bool _enabled;
    private string[] _roles = Array.Empty<string>();

    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    public string[] Roles
    {
        get => _roles;
        set
        {
            if (_roles != value)
            {
                _roles = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RolePrincipal));
            }
        }
    }
    
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled != value)
            {
                _enabled = value;
                OnPropertyChanged();
            }
        }
    }
    
    public bool IsOnline { get; set; }
    
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (_isBusy != value)
            {
                _isBusy = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Rol principal (el primero del array o "USER" por defecto).</summary>
    public string RolePrincipal => Roles?.FirstOrDefault() ?? "USER";

    /// <summary>Icono de estado online/offline.</summary>
    public string StatusIcon => IsOnline ? "🟢" : "⚪";

    /// <summary>Texto de estado online/offline.</summary>
    public string StatusText => IsOnline ? "Online" : "Offline";
    
    /// <summary>Indica si se puede echar al usuario (solo si está online).</summary>
    public bool CanKick => IsOnline;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>Grupo de usuarios por rol (para ItemsControl agrupado).</summary>
public sealed class UserRoleGroup
{
    public string RoleName { get; set; } = string.Empty;
    public List<UserViewModel> Users { get; set; } = new();
    
    /// <summary>Header del grupo (ej: "ADMIN (2)").</summary>
    public string Header => $"{RoleName} ({Users.Count})";
}
