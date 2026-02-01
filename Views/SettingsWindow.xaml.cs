using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using GestionTime.Desktop.ViewModels;
using GestionTime.Desktop.Models;
using GestionTime.Desktop.Services;
using Microsoft.Extensions.Logging;

namespace GestionTime.Desktop.Views;

/// <summary>Ventana de configuración de la aplicación.</summary>
public sealed partial class SettingsWindow : Window
{
    private readonly ILogger? _log;
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow()
    {
        InitializeComponent();
        
        _log = App.Log;
        _viewModel = new SettingsViewModel();
        
        // Usar WindowSizeManager para tamaño y atajo Ctrl+Alt+P
        WindowSizeManager.SetSizeForPage(this, typeof(SettingsWindow));
        
        // Asignar DataContext
        SectionsList.DataContext = _viewModel;
        
        _log?.LogInformation("📐 SettingsWindow inicializada");
        
        // Cargar primera sección
        LoadSelectedSection();
    }

    /// <summary>Maneja el cambio de texto en el buscador.</summary>
    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _viewModel.SearchQuery = TxtSearch.Text;
    }

    /// <summary>Maneja el click en una sección del menú.</summary>
    private void OnSectionClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is SettingsSectionItem section)
        {
            _viewModel.SelectedSection = section;
            LoadSelectedSection();
        }
    }

    /// <summary>Carga el contenido de la sección seleccionada.</summary>
    private void LoadSelectedSection()
    {
        if (_viewModel.SelectedSection == null)
        {
            TxtSectionTitle.Text = "Selecciona una sección";
            TxtSectionDescription.Text = "Usa el menú de la izquierda para navegar";
            TxtPlaceholder.Visibility = Visibility.Visible;
            return;
        }

        var section = _viewModel.SelectedSection;
        
        TxtSectionTitle.Text = section.Title;
        TxtSectionDescription.Text = section.Description;
        TxtPlaceholder.Visibility = Visibility.Collapsed;

        // Cargar contenido según sección
        LoadSectionContent(section.Id);
    }

    /// <summary>Carga el contenido específico de cada sección.</summary>
    private void LoadSectionContent(string sectionId)
    {
        SectionContentContainer.Child = sectionId switch
        {
            "profile" => CreateProfileContent(),
            "permissions" => CreatePermissionsContent(),
            "clients" => CreateClientsContent(),
            "catalog" => CreateCatalogContent(),
            "integrations" => CreateIntegrationsContent(),
            "import-export" => CreateImportExportContent(),
            "presence" => CreatePresenceContent(),
            "parameters" => CreateParametersContent(),
            _ => new TextBlock
            {
                Text = "Contenido no disponible",
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray)
            }
        };

        _log?.LogInformation("📄 Contenido cargado para sección: {section}", sectionId);
    }

    // ============================================================
    // CREACIÓN DE CONTENIDO POR SECCIÓN
    // ============================================================

    private UIElement CreateProfileContent()
    {
        var stack = new StackPanel { Spacing = 16 };
        
        stack.Children.Add(new TextBlock
        {
            Text = "Información del perfil",
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 230, 237, 243))
        });
        
        stack.Children.Add(new TextBlock
        {
            Text = "Esta sección mostrará información del usuario actual, preferencias y opciones de cuenta.",
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 170, 180, 191))
        });
        
        return stack;
    }

    private UIElement CreatePermissionsContent()
    {
        var stack = new StackPanel { Spacing = 16 };
        
        stack.Children.Add(new TextBlock
        {
            Text = "Gestión de roles de usuarios",
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 230, 237, 243))
        });
        
        stack.Children.Add(new TextBlock
        {
            Text = "⚠️ Solo ADMIN: Asignar roles (ADMIN/EDITOR/USER) a usuarios del sistema.",
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 245, 158, 11))
        });
        
        return stack;
    }

    private UIElement CreateClientsContent()
    {
        var stack = new StackPanel { Spacing = 16 };
        
        stack.Children.Add(new TextBlock
        {
            Text = "Gestión de clientes",
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 230, 237, 243))
        });
        
        stack.Children.Add(new TextBlock
        {
            Text = "CRUD de clientes: Crear, editar, eliminar y buscar clientes.",
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 170, 180, 191))
        });
        
        return stack;
    }

    private UIElement CreateCatalogContent()
    {
        var stack = new StackPanel { Spacing = 16 };
        
        stack.Children.Add(new TextBlock
        {
            Text = "Grupos y Tipos",
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 230, 237, 243))
        });
        
        stack.Children.Add(new TextBlock
        {
            Text = "Gestión de catálogos de clasificación de partes (Grupos y Tipos).",
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 170, 180, 191))
        });
        
        return stack;
    }

    private UIElement CreateIntegrationsContent()
    {
        var stack = new StackPanel { Spacing = 16 };
        
        stack.Children.Add(new TextBlock
        {
            Text = "Integraciones de API",
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 230, 237, 243))
        });
        
        stack.Children.Add(new TextBlock
        {
            Text = "Configuración de API base URL, timeout, test de conexión.",
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 170, 180, 191))
        });
        
        return stack;
    }

    private UIElement CreateImportExportContent()
    {
        var stack = new StackPanel { Spacing = 16 };
        
        stack.Children.Add(new TextBlock
        {
            Text = "Importación y Exportación",
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 230, 237, 243))
        });
        
        stack.Children.Add(new TextBlock
        {
            Text = "Gestión de datos masivos, rutas por defecto, formatos e historial.",
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 170, 180, 191))
        });
        
        return stack;
    }

    private UIElement CreatePresenceContent()
    {
        var stack = new StackPanel { Spacing = 16 };
        
        stack.Children.Add(new TextBlock
        {
            Text = "Sistema de presencia",
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 230, 237, 243))
        });
        
        stack.Children.Add(new TextBlock
        {
            Text = "Configuración de refresco automático y visualización de usuarios online.",
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 170, 180, 191))
        });
        
        return stack;
    }

    private UIElement CreateParametersContent()
    {
        var stack = new StackPanel { Spacing = 16 };
        
        stack.Children.Add(new TextBlock
        {
            Text = "Parámetros globales",
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 230, 237, 243))
        });
        
        stack.Children.Add(new TextBlock
        {
            Text = "Configuración de parámetros globales de la aplicación.",
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 170, 180, 191))
        });
        
        return stack;
    }
}
