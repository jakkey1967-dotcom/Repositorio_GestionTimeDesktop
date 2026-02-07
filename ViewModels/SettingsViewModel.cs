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
    private SettingsSectionItem? _lastAllowedSection; // 🆕 Última sección permitida
    
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
        
        // 🔥 CRÍTICO: Obtener ROL REAL del usuario actual desde archivo guardado
        var userInfo = Helpers.UserInfoFileStorage.LoadUserInfo(_log);
        var currentRole = UserRole.USER; // Rol por defecto restrictivo si no se encuentra
        
        if (userInfo != null && !string.IsNullOrEmpty(userInfo.UserRole))
        {
            var roleString = userInfo.UserRole.Trim().ToUpperInvariant();
            
            _log?.LogInformation("📋 Analizando rol desde archivo: '{roleOriginal}' -> '{roleNormalized}'", 
                userInfo.UserRole, roleString);
            
            // Mapear string de rol a enum
            currentRole = roleString switch
            {
                "ADMIN" => UserRole.ADMIN,
                "EDITOR" => UserRole.EDITOR,
                "USER" => UserRole.USER,
                _ => UserRole.USER // Default restrictivo
            };
            
            // ⚠️ ADVERTENCIA: Si backend devuelve NULL, UserRoleSafe devuelve "Usuario" (inválido)
            if (!new[] { "ADMIN", "EDITOR", "USER" }.Contains(roleString))
            {
                _log?.LogWarning("⚠️ Rol INVÁLIDO detectado: '{roleString}' (usando USER por defecto)", roleString);
                _log?.LogWarning("   Posible causa: Backend devuelve NULL en campo 'role' → UserRoleSafe = 'Usuario'");
                _log?.LogWarning("   Verificar endpoint de login: /api/v1/auth/login debe incluir 'role' en respuesta");
            }
            
            _log?.LogInformation("✅ Rol mapeado: '{roleString}' -> {roleEnum}", roleString, currentRole);
        }
        else
        {
            _log?.LogWarning("⚠️ No se pudo cargar información de usuario desde archivo");
            _log?.LogWarning("   • userInfo es NULL: {isNull}", userInfo == null);
            _log?.LogWarning("   • userInfo.UserRole vacío: {isEmpty}", userInfo != null && string.IsNullOrEmpty(userInfo.UserRole));
            _log?.LogWarning("   Usando rol por defecto: USER (más restrictivo)");
        }
        
        _permissionService.SetCurrentUserRole(currentRole);
        
        _log?.LogInformation("═══════════════════════════════════════════════════════");
        _log?.LogInformation("✅ SettingsViewModel inicializado con rol: {role}", currentRole);
        _log?.LogInformation("   • Perfil y cuenta: {allowed}", currentRole == UserRole.USER || currentRole == UserRole.EDITOR || currentRole == UserRole.ADMIN);
        _log?.LogInformation("   • Clientes: {allowed}", currentRole == UserRole.EDITOR || currentRole == UserRole.ADMIN);
        _log?.LogInformation("   • Permisos: {allowed}", currentRole == UserRole.ADMIN);
        _log?.LogInformation("═══════════════════════════════════════════════════════");
        
        InitializeSections();
        FilterSections();
    }
    
    /// <summary>Inicializa las secciones de Settings con permisos.</summary>
    private void InitializeSections()
    {
        // 🆕 Obtener rol actual
        var currentRole = _permissionService.GetCurrentRole();
        
        // 1. Perfil y cuenta (USER, EDITOR, ADMIN)
        Sections.Add(CreateSection(
            id: "profile",
            title: "Perfil y cuenta",
            description: "Información personal y preferencias de usuario",
            icon: "\uE77B", // Contact
            allowedRoles: new[] { UserRole.USER, UserRole.EDITOR, UserRole.ADMIN },
            currentRole: currentRole
        ));
        
        // 2. Permisos y roles (ADMIN)
        Sections.Add(CreateSection(
            id: "permissions",
            title: "Permisos y roles",
            description: "Gestión de roles de usuarios",
            icon: "\uE72E", // Shield
            allowedRoles: new[] { UserRole.ADMIN },
            currentRole: currentRole
        ));
        
        // 3. Clientes (ADMIN, EDITOR)
        Sections.Add(CreateSection(
            id: "clients",
            title: "Clientes",
            description: "Gestión de clientes",
            icon: "\uE716", // People
            allowedRoles: new[] { UserRole.ADMIN, UserRole.EDITOR },
            currentRole: currentRole
        ));
        
        // 4. Grupos y Tipos (ADMIN, EDITOR)
        Sections.Add(CreateSection(
            id: "catalog",
            title: "Grupos y Tipos",
            description: "Catálogos de clasificación de partes",
            icon: "\uE8FD", // List
            allowedRoles: new[] { UserRole.ADMIN, UserRole.EDITOR },
            currentRole: currentRole
        ));
        
        // 5. Integraciones (ADMIN)
        Sections.Add(CreateSection(
            id: "integrations",
            title: "Integraciones",
            description: "Configuración de API y servicios externos",
            icon: "\uE774", // Sync
            allowedRoles: new[] { UserRole.ADMIN },
            currentRole: currentRole
        ));
        
        // 6. Importación/Exportación (ADMIN)
        Sections.Add(CreateSection(
            id: "import-export",
            title: "Importación / Exportación",
            description: "Gestión de datos masivos",
            icon: "\uE8E5", // Download
            allowedRoles: new[] { UserRole.ADMIN },
            currentRole: currentRole
        ));
        
        // 7. Usuarios online / Presencia (USER, EDITOR, ADMIN)
        Sections.Add(CreateSection(
            id: "presence",
            title: "Usuarios online / Presencia",
            description: "Configuración de sistema de presencia",
            icon: "\uE8A0", // StatusCircle
            allowedRoles: new[] { UserRole.USER, UserRole.EDITOR, UserRole.ADMIN },
            currentRole: currentRole
        ));
        
        // 8. Parámetros (ADMIN)
        Sections.Add(CreateSection(
            id: "parameters",
            title: "Parámetros",
            description: "Parámetros globales de la aplicación",
            icon: "\uE713", // Settings
            allowedRoles: new[] { UserRole.ADMIN },
            currentRole: currentRole
        ));
        
        // 9. Salir (USER, EDITOR, ADMIN)
        Sections.Add(CreateSection(
            id: "exit",
            title: "Salir",
            description: "Volver a la pantalla principal",
            icon: "\uE7C3", // BackToWindow
            allowedRoles: new[] { UserRole.USER, UserRole.EDITOR, UserRole.ADMIN },
            currentRole: currentRole
        ));
    }
    
    /// <summary>Crea una sección con permisos y candado visual.</summary>
    private SettingsSectionItem CreateSection(string id, string title, string description, string icon, UserRole[] allowedRoles, UserRole currentRole)
    {
        var isAllowed = allowedRoles.Contains(currentRole);
        
        // 🔍 DEBUG: Log para verificar permisos
        _log?.LogDebug("   └─ Sección '{title}': isAllowed={isAllowed} (rol={role}, permitidos={allowed})", 
            title, isAllowed, currentRole, string.Join(",", allowedRoles));
        
        return new SettingsSectionItem
        {
            Id = id,
            Title = title,
            Description = description,
            Icon = icon,
            AllowedRoles = allowedRoles,
            IsAllowed = isAllowed,
            // 🔓 Candado abierto (permitido) vs 🔒 Candado cerrado (bloqueado)
            LockIcon = isAllowed ? "\uE785" : "\uE72E", // LockOpen vs Lock
            // Verde brillante (permitido) vs Amarillo (bloqueado)
            LockBrush = isAllowed 
                ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 76, 175, 80)) // #4CAF50 (Material Green)
                : new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 255, 193, 7))  // #FFC107 (Amber)
        };
    }
    
    /// <summary>Filtra las secciones según búsqueda (NO por permisos - todas siempre visibles).</summary>
    private void FilterSections()
    {
        FilteredSections.Clear();
        
        var query = SearchQuery?.ToLowerInvariant() ?? string.Empty;
        
        foreach (var section in Sections)
        {
            // 🆕 CAMBIO: Ya NO filtramos por permisos, TODAS las secciones son visibles
            // El candado indica visualmente si tiene acceso o no
            
            // Filtro SOLO por búsqueda
            var matchesSearch = string.IsNullOrWhiteSpace(query) ||
                section.Title.ToLowerInvariant().Contains(query) ||
                section.Description.ToLowerInvariant().Contains(query);
            
            if (matchesSearch)
            {
                FilteredSections.Add(section);
            }
        }
        
        // Seleccionar primera sección PERMITIDA por defecto
        if (SelectedSection == null && FilteredSections.Count > 0)
        {
            var firstAllowed = FilteredSections.FirstOrDefault(s => s.IsAllowed);
            SelectedSection = firstAllowed ?? FilteredSections[0];
            _lastAllowedSection = SelectedSection?.IsAllowed == true ? SelectedSection : null;
        }
    }
    
    /// <summary>Se ejecuta cuando cambia la sección seleccionada.</summary>
    private void OnSectionChanged()
    {
        if (SelectedSection == null) return;
        
        // 🆕 BLOQUEO: Si intenta acceder a sección NO permitida, revertir selección
        if (!SelectedSection.IsAllowed)
        {
            _log?.LogWarning("❌ Intento de acceso no autorizado a sección: {section} (Rol actual: {role})", 
                SelectedSection.Title, _permissionService.GetCurrentRole());
            
            // ⚠️ IMPORTANTE: Este evento se dispara DESPUÉS del binding de selección
            // La UI ya muestra el item bloqueado como seleccionado
            // Necesitamos REVERTIR la selección al último item permitido
            // Esto se manejará mejor en el code-behind con SelectionChanging
            
            return;
        }
        
        // Actualizar última sección permitida
        _lastAllowedSection = SelectedSection;
        
        _log?.LogInformation("📄 Sección seleccionada: {section}", SelectedSection.Title);
    }
    
    /// <summary>Obtiene la última sección permitida visitada.</summary>
    public SettingsSectionItem? GetLastAllowedSection()
    {
        return _lastAllowedSection;
    }
    
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
