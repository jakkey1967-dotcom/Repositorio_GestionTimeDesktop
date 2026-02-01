using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using GestionTime.Desktop.Models;
using GestionTime.Desktop.Models.Enums;
using GestionTime.Desktop.Services;
using Microsoft.Extensions.Logging;

namespace GestionTime.Desktop.ViewModels;

/// <summary>ViewModel para la ventana de Settings.</summary>
public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private readonly ILogger? _log;
    private readonly PermissionService _permissionService;
    
    public ObservableCollection<SettingsSectionItem> Sections { get; } = new();
    public ObservableCollection<SettingsSectionItem> FilteredSections { get; } = new();
    
    private SettingsSectionItem? _selectedSection;
    public SettingsSectionItem? SelectedSection
    {
        get => _selectedSection;
        set
        {
            if (_selectedSection != value)
            {
                _selectedSection = value;
                OnPropertyChanged();
                OnSectionChanged();
            }
        }
    }
    
    private string _searchQuery = string.Empty;
    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (_searchQuery != value)
            {
                _searchQuery = value;
                OnPropertyChanged();
                FilterSections();
            }
        }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public SettingsViewModel()
    {
        _log = App.Log;
        _permissionService = new PermissionService();
        
        // TODO: Obtener rol del usuario actual desde App.CurrentUser o similar
        // Por ahora, simulamos con ADMIN para testing
        _permissionService.SetCurrentUserRole(UserRole.ADMIN);
        
        InitializeSections();
        FilterSections();
    }
    
    /// <summary>Inicializa las secciones de Settings con permisos.</summary>
    private void InitializeSections()
    {
        // 1. Perfil y cuenta (USER)
        Sections.Add(new SettingsSectionItem
        {
            Id = "profile",
            Title = "Perfil y cuenta",
            Description = "Información personal y preferencias de usuario",
            Icon = "\uE77B", // Contact
            AllowedRoles = new[] { UserRole.USER, UserRole.EDITOR, UserRole.ADMIN }
        });
        
        // 2. Permisos y roles (ADMIN)
        Sections.Add(new SettingsSectionItem
        {
            Id = "permissions",
            Title = "Permisos y roles",
            Description = "Gestión de roles de usuarios",
            Icon = "\uE72E", // Shield
            AllowedRoles = new[] { UserRole.ADMIN }
        });
        
        // 3. Clientes (ADMIN, EDITOR)
        Sections.Add(new SettingsSectionItem
        {
            Id = "clients",
            Title = "Clientes",
            Description = "Gestión de clientes",
            Icon = "\uE716", // People
            AllowedRoles = new[] { UserRole.ADMIN, UserRole.EDITOR }
        });
        
        // 4. Grupos y Tipos (ADMIN, EDITOR)
        Sections.Add(new SettingsSectionItem
        {
            Id = "catalog",
            Title = "Grupos y Tipos",
            Description = "Catálogos de clasificación de partes",
            Icon = "\uE8FD", // List
            AllowedRoles = new[] { UserRole.ADMIN, UserRole.EDITOR }
        });
        
        // 5. Integraciones (ADMIN)
        Sections.Add(new SettingsSectionItem
        {
            Id = "integrations",
            Title = "Integraciones",
            Description = "Configuración de API y servicios externos",
            Icon = "\uE774", // Sync
            AllowedRoles = new[] { UserRole.ADMIN }
        });
        
        // 6. Importación/Exportación (ADMIN)
        Sections.Add(new SettingsSectionItem
        {
            Id = "import-export",
            Title = "Importación / Exportación",
            Description = "Gestión de datos masivos",
            Icon = "\uE8E5", // Download
            AllowedRoles = new[] { UserRole.ADMIN }
        });
        
        // 7. Usuarios online / Presencia (ADMIN)
        Sections.Add(new SettingsSectionItem
        {
            Id = "presence",
            Title = "Usuarios online / Presencia",
            Description = "Configuración de sistema de presencia",
            Icon = "\uE8A0", // StatusCircle
            AllowedRoles = new[] { UserRole.ADMIN }
        });
        
        // 8. Parámetros (ADMIN)
        Sections.Add(new SettingsSectionItem
        {
            Id = "parameters",
            Title = "Parámetros",
            Description = "Parámetros globales de la aplicación",
            Icon = "\uE713", // Settings
            AllowedRoles = new[] { UserRole.ADMIN }
        });
    }
    
    /// <summary>Filtra las secciones según permisos y búsqueda.</summary>
    private void FilterSections()
    {
        FilteredSections.Clear();
        
        var query = SearchQuery?.ToLowerInvariant() ?? string.Empty;
        
        foreach (var section in Sections)
        {
            // Filtro por permisos
            var hasPermission = _permissionService.CanAccessSection(section.Id, section.AllowedRoles);
            
            // Filtro por búsqueda
            var matchesSearch = string.IsNullOrWhiteSpace(query) ||
                section.Title.ToLowerInvariant().Contains(query) ||
                section.Description.ToLowerInvariant().Contains(query);
            
            if (hasPermission && matchesSearch)
            {
                FilteredSections.Add(section);
            }
        }
        
        // Seleccionar primera sección por defecto
        if (SelectedSection == null && FilteredSections.Count > 0)
        {
            SelectedSection = FilteredSections[0];
        }
    }
    
    /// <summary>Se ejecuta cuando cambia la sección seleccionada.</summary>
    private void OnSectionChanged()
    {
        if (SelectedSection == null) return;
        
        // Verificar permisos nuevamente
        if (!_permissionService.CanAccessSection(SelectedSection.Id, SelectedSection.AllowedRoles))
        {
            _log?.LogWarning("❌ Intento de acceso no autorizado a sección: {section}", SelectedSection.Title);
            SelectedSection = FilteredSections.FirstOrDefault();
            return;
        }
        
        _log?.LogInformation("📄 Sección seleccionada: {section}", SelectedSection.Title);
    }
    
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
