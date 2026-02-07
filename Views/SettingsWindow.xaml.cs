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
        
        // 🆕 NUEVO: Al cerrar, volver a mostrar MainWindow
        Closed += OnSettingsWindowClosed;
        
        // Cargar primera sección
        LoadSelectedSection();
    }
    
    /// <summary>Maneja el cierre de la ventana de Settings.</summary>
    private void OnSettingsWindowClosed(object sender, WindowEventArgs e)
    {
        try
        {
            _log?.LogInformation("🔙 SettingsWindow cerrada, mostrando MainWindow");
            
            // Volver a mostrar MainWindow usando AppWindow
            if (App.MainWindowInstance != null)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance);
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
                
                if (appWindow != null)
                {
                    appWindow.Show();
                    App.MainWindowInstance.Activate();
                }
            }
        }
        catch (Exception ex)
        {
            _log?.LogError(ex, "❌ Error al cerrar SettingsWindow");
        }
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
            // 🆕 VERIFICAR PERMISOS: Si NO tiene acceso, mostrar InfoBar y NO navegar
            if (!section.IsAllowed)
            {
                _log?.LogWarning("❌ Intento de acceso bloqueado a sección: {section} (Rol actual: {role})", 
                    section.Title, _viewModel.GetLastAllowedSection()?.Title ?? "N/A");
                
                // Mostrar InfoBar de acceso denegado
                AccessDeniedInfoBar.IsOpen = true;
                
                // NO cambiar la sección seleccionada
                // NO cargar contenido
                return;
            }
            
            // Si tiene permiso, navegar normalmente
            _viewModel.SelectedSection = section;
            LoadSelectedSection();
        }
    }
    
    /// <summary>Maneja el cierre del InfoBar de acceso denegado.</summary>
    private void OnAccessDeniedInfoBarClosed(InfoBar sender, InfoBarClosedEventArgs args)
    {
        // Opcionalmente, limpiar estado aquí
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
        
        // 🆕 VERIFICAR PERMISOS: NO cargar contenido si no tiene acceso
        if (!section.IsAllowed)
        {
            _log?.LogWarning("❌ Intento de cargar contenido sin permisos: {section}", section.Title);
            return;
        }
        
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
            "exit" => CreateExitContent(), // 🆕 NUEVO: Manejo del botón Salir
            _ => CreatePlaceholder("Contenido no disponible")
        };

        _log?.LogInformation("📄 Contenido cargado para sección: {section}", sectionId);
        
        // 🆕 NUEVO: Si es "exit", cerrar la ventana inmediatamente
        if (sectionId == "exit")
        {
            Close();
        }
    }

    // ============================================================
    // CREACIÓN DE CONTENIDO POR SECCIÓN
    // ============================================================

    /// <summary>Placeholder genérico.</summary>
    private UIElement CreatePlaceholder(string message)
    {
        return new TextBlock
        {
            Text = message,
            FontSize = 14,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 170, 180, 191)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    /// <summary>1. Perfil y cuenta (USER) - Muestra datos de App.CurrentUserProfile.</summary>
    private UIElement CreateProfileContent()
    {
        var stack = new StackPanel { Spacing = 20 };
        
        // Título con icono de perfil (igual que en DiarioPage)
        var titlePanel = new StackPanel 
        { 
            Orientation = Orientation.Horizontal, 
            Spacing = 8,
            Margin = new Thickness(0, 0, 0, 12)
        };
        
        titlePanel.Children.Add(new FontIcon
        {
            Glyph = "\uE77B", // Icono de usuario (igual que DiarioPage)
            FontSize = 18,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 230, 237, 243))
        });
        
        titlePanel.Children.Add(new TextBlock
        {
            Text = "Información del perfil",
            FontSize = 18,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 230, 237, 243)),
            VerticalAlignment = VerticalAlignment.Center
        });
        
        stack.Children.Add(titlePanel);
        
        var profile = App.CurrentUserProfile;
        
        if (profile == null)
        {
            stack.Children.Add(new TextBlock
            {
                Text = "⚠️ No hay información de perfil disponible.",
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 245, 158, 11))
            });
            return stack;
        }
        
        // Card contenedor
        var card = new Border
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 26, 35, 50)),
            BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 45, 62, 80)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(20, 20, 20, 20)
        };
        
        var cardContent = new StackPanel { Spacing = 16 };
        
        // Nombre completo
        if (!string.IsNullOrEmpty(profile.FullNameFromBackend))
        {
            cardContent.Children.Add(CreateProfileField("👤 Nombre completo", profile.FullNameFromBackend));
        }
        
        // Email
        if (!string.IsNullOrEmpty(App.CurrentLoginEmail))
        {
            cardContent.Children.Add(CreateProfileField("📧 Email", App.CurrentLoginEmail));
        }
        
        // Teléfono
        if (!string.IsNullOrEmpty(profile.Phone))
        {
            cardContent.Children.Add(CreateProfileField("📞 Teléfono", profile.Phone));
        }
        
        // Móvil
        if (!string.IsNullOrEmpty(profile.Mobile))
        {
            cardContent.Children.Add(CreateProfileField("📱 Móvil", profile.Mobile));
        }
        
        // Dirección
        if (!string.IsNullOrEmpty(profile.Address))
        {
            cardContent.Children.Add(CreateProfileField("🏠 Dirección", profile.Address));
        }
        
        // Ciudad
        if (!string.IsNullOrEmpty(profile.City))
        {
            cardContent.Children.Add(CreateProfileField("🏙️ Ciudad", profile.City));
        }
        
        // Código postal
        if (!string.IsNullOrEmpty(profile.PostalCode))
        {
            cardContent.Children.Add(CreateProfileField("📮 Código Postal", profile.PostalCode));
        }
        
        // Departamento
        if (!string.IsNullOrEmpty(profile.Department))
        {
            cardContent.Children.Add(CreateProfileField("🏢 Departamento", profile.Department));
        }
        
        // Posición
        if (!string.IsNullOrEmpty(profile.Position))
        {
            cardContent.Children.Add(CreateProfileField("💼 Posición", profile.Position));
        }
        
        // Tipo de empleado
        if (!string.IsNullOrEmpty(profile.EmployeeType))
        {
            cardContent.Children.Add(CreateProfileField("👔 Tipo de empleado", profile.EmployeeType));
        }
        
        // Fecha de contratación
        if (profile.HireDate.HasValue)
        {
            cardContent.Children.Add(CreateProfileField("📅 Fecha de contratación", profile.HireDate.Value.ToString("dd/MM/yyyy")));
        }
        
        card.Child = cardContent;
        stack.Children.Add(card);
        
        // Botón para abrir Mi Perfil completo (si existe UserProfilePage)
        var btnOpenProfile = new Button
        {
            Content = "📝 Editar Perfil Completo",
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 22, 168, 184)),
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
            Padding = new Thickness(20, 12, 20, 12),
            CornerRadius = new CornerRadius(6),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Margin = new Thickness(0, 12, 0, 0)
        };
        
        btnOpenProfile.Click += (s, e) =>
        {
            try
            {
                _log?.LogInformation("📝 Abriendo UserProfilePage para editar perfil completo...");
                
                // Navegar a UserProfilePage en MainWindow
                if (App.MainWindowInstance?.Navigator != null)
                {
                    App.MainWindowInstance.Navigator.Navigate(typeof(UserProfilePage));
                    _log?.LogInformation("✅ Navegación a UserProfilePage iniciada");
                    
                    // Cerrar Settings después de navegar
                    this.Close();
                }
                else
                {
                    _log?.LogError("❌ No se pudo navegar: MainWindowInstance o Navigator es null");
                    
                    // Mostrar mensaje de error al usuario
                    var errorDialog = new Microsoft.UI.Xaml.Controls.ContentDialog
                    {
                        Title = "Error",
                        Content = "No se pudo abrir la página de edición de perfil.\nIntenta cerrar y volver a abrir Settings.",
                        CloseButtonText = "OK",
                        XamlRoot = this.Content.XamlRoot
                    };
                    _ = errorDialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                _log?.LogError(ex, "Error abriendo edición de perfil");
                
                // Mostrar mensaje de error al usuario
                var errorDialog = new Microsoft.UI.Xaml.Controls.ContentDialog
                {
                    Title = "Error",
                    Content = $"Error al abrir la página de edición:\n{ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                _ = errorDialog.ShowAsync();
            }
        };
        
        stack.Children.Add(btnOpenProfile);
        
        return stack;
    }
    
    /// <summary>Helper: Crea un campo de perfil con label y valor.</summary>
    private UIElement CreateProfileField(string label, string value)
    {
        var fieldStack = new StackPanel { Spacing = 4 };
        
        // Label
        fieldStack.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 12,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 170, 180, 191))
        });
        
        // Valor
        fieldStack.Children.Add(new TextBlock
        {
            Text = value,
            FontSize = 14,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 230, 237, 243)),
            TextWrapping = TextWrapping.Wrap
        });
        
        return fieldStack;
    }

    /// <summary>2. Permisos y roles (ADMIN) - Gestión inline de usuarios.</summary>
    private UIElement CreatePermissionsContent()
    {
        var mainStack = new StackPanel { Spacing = 16 };
        
        // Título
        mainStack.Children.Add(new TextBlock
        {
            Text = "Gestión de roles de usuarios",
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 230, 237, 243))
        });
        
        mainStack.Children.Add(new TextBlock
        {
            Text = "⚠️ Solo ADMIN: Asignar roles (ADMIN/EDITOR/USER) y habilitar/deshabilitar usuarios.",
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 245, 158, 11))
        });
        
        // Barra de herramientas
        var toolbar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Margin = new Thickness(0, 12, 0, 0)
        };
        
        // Botón Actualizar
        var btnRefresh = new Button
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 22, 168, 184)),
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
            Padding = new Thickness(12, 8, 12, 8),
            CornerRadius = new CornerRadius(6),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        
        // Icono de refresh
        var refreshIcon = new FontIcon
        {
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets"),
            Glyph = "\uE72C", // Icono de refresh circular
            FontSize = 16
        };
        
        btnRefresh.Content = refreshIcon;
        btnRefresh.Click += async (s, e) =>
        {
            // 🆕 CRÍTICO: Limpiar caché antes de refrescar
            App.Api.ClearGetCache();
            _log?.LogInformation("🗑️ Caché limpiado antes de refrescar manualmente");
            
            await LoadUsersInlineAsync(mainStack);
        };
        
        toolbar.Children.Add(btnRefresh);
        
        // TextBlock de estado
        var txtStatus = new TextBlock
        {
            Text = "Cargando usuarios...",
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 170, 180, 191))
        };
        toolbar.Children.Add(txtStatus);
        
        mainStack.Children.Add(toolbar);
        
        // Contenedor de lista de usuarios (se llenará dinámicamente)
        var usersContainer = new StackPanel { Spacing = 16, Margin = new Thickness(0, 12, 0, 0) };
        mainStack.Children.Add(usersContainer);
        
        // Cargar usuarios al inicializar
        _ = LoadUsersInlineAsync(mainStack);
        
        return mainStack;
    }
    
    /// <summary>Carga usuarios y los muestra agrupados por rol.</summary>
    private async System.Threading.Tasks.Task LoadUsersInlineAsync(StackPanel container)
    {
        try
        {
            _log?.LogInformation("🔄 Iniciando carga de usuarios inline...");
            
            // Buscar toolbar y status text
            var toolbar = container.Children.OfType<StackPanel>().FirstOrDefault(sp => sp.Orientation == Orientation.Horizontal);
            var txtStatus = toolbar?.Children.OfType<TextBlock>().FirstOrDefault();
            var usersContainer = container.Children.OfType<StackPanel>().LastOrDefault();
            
            if (txtStatus != null)
                txtStatus.Text = "Cargando usuarios...";
            
            usersContainer?.Children.Clear();
            
            // 1. Cargar usuarios del sistema
            _log?.LogInformation("📡 Llamando a /api/v1/users?pageSize=100...");
            
            var usersResponse = await App.Api.GetAsync<GestionTime.Desktop.Models.Dtos.UsersPagedResponse>(
                "/api/v1/users?pageSize=100",
                System.Threading.CancellationToken.None
            );
            
            _log?.LogInformation("📊 Respuesta recibida: {response}", usersResponse != null ? "OK" : "NULL");
            
            if (usersResponse != null)
            {
                _log?.LogInformation("📋 Usuarios en respuesta: {count}", usersResponse.Users?.Count ?? 0);
            }
            
            if (usersResponse == null || usersResponse.Users == null || !usersResponse.Users.Any())
            {
                _log?.LogWarning("⚠️ No hay usuarios en la respuesta del backend");
                
                if (txtStatus != null)
                    txtStatus.Text = "No hay usuarios disponibles";
                
                // Mostrar error más detallado
                usersContainer?.Children.Add(new TextBlock
                {
                    Text = "❌ No se pudieron cargar usuarios desde el backend.\nVerifica que:\n• El backend esté corriendo\n• El token JWT sea válido\n• El endpoint /api/v1/users responda correctamente",
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 239, 68, 68)),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 12, 0, 0)
                });
                
                return;
            }
            
            // 2. Cargar presencia
            _log?.LogInformation("🔄 Cargando presencia...");
            var presenceUsers = await Services.Presence.PresenceService.Instance.GetUsersAsync();
            _log?.LogInformation("👥 Usuarios con presencia: {count}", presenceUsers?.Count ?? 0);
            
            // 3. Combinar datos
            var users = usersResponse.Users.Select(u => new Models.UserViewModel
            {
            Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Roles = u.Roles ?? new[] { "USER" },
                Enabled = u.Enabled,
                IsOnline = presenceUsers?.Any(p => p.UserId == u.Id && p.IsOnline) ?? false
            }).ToList();
            
            _log?.LogInformation("✅ {count} usuarios combinados", users.Count);
            
            // 4. Agrupar por rol
            var grouped = users
                .GroupBy(u => u.RolePrincipal)
                .OrderBy(g => g.Key == "ADMIN" ? 0 : g.Key == "EDITOR" ? 1 : 2)
                .Select(g => new Models.UserRoleGroup
                {
                    RoleName = g.Key,
                    Users = g.OrderByDescending(u => u.IsOnline).ThenBy(u => u.FullName).ToList()
                })
                .ToList();
            
            _log?.LogInformation("📊 Grupos creados: {count}", grouped.Count);
            
            // 5. Renderizar grupos
            foreach (var group in grouped)
            {
                _log?.LogInformation("🎨 Renderizando grupo: {roleName} ({userCount} usuarios)", group.RoleName, group.Users.Count);
                var groupStack = CreateUserGroupUI(group);
                usersContainer?.Children.Add(groupStack);
            }
            
            var totalOnline = users.Count(u => u.IsOnline);
            if (txtStatus != null)
                txtStatus.Text = $"{users.Count} usuarios ({totalOnline} online)";
            
            _log?.LogInformation("✅ {count} usuarios cargados ({online} online)", users.Count, totalOnline);
        }
        catch (Exception ex)
        {
            _log?.LogError(ex, "❌ Error cargando usuarios inline");
            
            var toolbar = container.Children.OfType<StackPanel>().FirstOrDefault(sp => sp.Orientation == Orientation.Horizontal);
            var txtStatus = toolbar?.Children.OfType<TextBlock>().FirstOrDefault();
            if (txtStatus != null)
                txtStatus.Text = $"❌ Error: {ex.Message}";
            
            // Mostrar error en UI
            var usersContainer = container.Children.OfType<StackPanel>().LastOrDefault();
            usersContainer?.Children.Add(new TextBlock
            {
                Text = $"❌ Error cargando usuarios:\n\n{ex.Message}\n\n{ex.StackTrace}",
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 239, 68, 68)),
                TextWrapping = TextWrapping.Wrap,
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                FontSize = 11
            });
        }
    }
    
    /// <summary>Crea UI de un grupo de usuarios (ej: ADMIN (2)).</summary>
    private UIElement CreateUserGroupUI(Models.UserRoleGroup group)
    {
        var stack = new StackPanel { Spacing = 8 };
        
        // Header del grupo
        stack.Children.Add(new TextBlock
        {
            Text = group.Header,
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 22, 168, 184)),
            Margin = new Thickness(0, 8, 0, 4)
        });
        
        // Cards de usuarios
        foreach (var user in group.Users)
        {
            stack.Children.Add(CreateUserCardUI(user));
        }
        
        return stack;
    }
    
    /// <summary>Crea card de un usuario con botón de acciones.</summary>
    private UIElement CreateUserCardUI(Models.UserViewModel user)
    {
        var card = new Border
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 26, 35, 50)),
            BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 45, 62, 80)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 12, 12, 12)
        };
        
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        
        // Info del usuario
        var infoStack = new StackPanel { Spacing = 4 };
        
        infoStack.Children.Add(new TextBlock
        {
            Text = $"👤 {user.FullName}",
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White)
        });
        
        infoStack.Children.Add(new TextBlock
        {
            Text = $"📧 {user.Email}",
            FontSize = 12,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 170, 180, 191))
        });
        
        var statusStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        statusStack.Children.Add(new TextBlock
        {
            Text = user.StatusIcon,
            FontSize = 12
        });
        statusStack.Children.Add(new TextBlock
        {
            Text = user.StatusText,
            FontSize = 12,
            Foreground = user.IsOnline 
                ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 16, 185, 129))
                : new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 170, 180, 191))
        });
        
        if (!user.Enabled)
        {
            statusStack.Children.Add(new TextBlock
            {
                Text = "⛔ Deshabilitado",
                FontSize = 12,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 239, 68, 68))
            });
        }
        
        infoStack.Children.Add(statusStack);
        
        Grid.SetColumn(infoStack, 0);
        grid.Children.Add(infoStack);
        
        // Botón de acciones
        var btnActions = new Button
        {
            Content = "⋯",
            FontSize = 20,
            Width = 40,
            Height = 40,
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 45, 62, 80)),
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
            CornerRadius = new CornerRadius(20),
            VerticalAlignment = VerticalAlignment.Center
        };
        
        btnActions.Click += (s, e) => ShowUserActionsFlyout(s as Button, user);
        
        Grid.SetColumn(btnActions, 1);
        grid.Children.Add(btnActions);
        
        card.Child = grid;
        return card;
    }
    
    /// <summary>Muestra flyout con acciones del usuario.</summary>
    private async void ShowUserActionsFlyout(Button? button, Models.UserViewModel user)
    {
        if (button == null) return;
        
        try
        {
            // Cargar roles disponibles
            var availableRoles = await Services.Admin.AdminUsersService.Instance.GetRolesAsync();
            
            var flyout = new Flyout
            {
                Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Left
            };
            
            var stack = new StackPanel { Spacing = 12, Width = 280 };
            
            // Título
            stack.Children.Add(new TextBlock
            {
                Text = $"👤 {user.FullName}",
                FontSize = 14,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White)
            });
            
            // ComboBox de roles
            stack.Children.Add(new TextBlock
            {
                Text = "Rol:",
                FontSize = 12,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 170, 180, 191))
            });
            
            var cmbRoles = new ComboBox
            {
                ItemsSource = availableRoles,
                SelectedItem = user.RolePrincipal,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            stack.Children.Add(cmbRoles);
            
            // ToggleSwitch de enabled
            var toggleEnabled = new ToggleSwitch
            {
                Header = "Usuario habilitado",
                IsOn = user.Enabled
            };
            stack.Children.Add(toggleEnabled);
            
            // Botón Kick (solo si está online)
            if (user.CanKick)
            {
                var btnKick = new Button
                {
                    Content = "🚪 Echar usuario",
                    Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 220, 38, 38)),
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Padding = new Thickness(12, 8, 12, 8),
                    CornerRadius = new CornerRadius(6),
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                };
                
                btnKick.Click += async (s, e) =>
                {
                    flyout.Hide();
                    await KickUserAsync(user);
                };
                
                stack.Children.Add(btnKick);
            }
            
            // Botón Guardar
            var btnSave = new Button
            {
                Content = "💾 Guardar cambios",
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 22, 168, 184)),
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Padding = new Thickness(12, 8, 12, 8),
                CornerRadius = new CornerRadius(6),
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            };
            
            btnSave.Click += async (s, e) =>
            {
                // 🆕 CRÍTICO: Capturar valores ANTES de cerrar el flyout
                var selectedRole = cmbRoles.SelectedItem?.ToString() ?? user.RolePrincipal;
                var isEnabled = toggleEnabled.IsOn;
                
                _log?.LogInformation("🔘 Usuario pulsó Guardar: Rol={role}, Enabled={enabled}", selectedRole, isEnabled);
                
                // Cerrar flyout DESPUÉS de capturar valores
                flyout.Hide();
                
                // Guardar cambios con los valores capturados
                await SaveUserChangesAsync(user, selectedRole, isEnabled);
            };
            
            stack.Children.Add(btnSave);
            
            flyout.Content = stack;
            flyout.ShowAt(button);
        }
        catch (Exception ex)
        {
            _log?.LogError(ex, "❌ Error mostrando flyout de acciones");
        }
    }
    
    /// <summary>Guarda cambios de un usuario (rol y/o enabled).</summary>
    private async System.Threading.Tasks.Task SaveUserChangesAsync(Models.UserViewModel user, string newRole, bool newEnabled)
    {
        StackPanel? permissionsContent = null;
        TextBlock? txtStatus = null;
        
        try
        {
            user.IsBusy = true;
            
            // Obtener referencia al contenedor principal
            permissionsContent = SectionContentContainer.Child as StackPanel;
            if (permissionsContent != null)
            {
                var toolbar = permissionsContent.Children.OfType<StackPanel>().FirstOrDefault(sp => sp.Orientation == Orientation.Horizontal);
                txtStatus = toolbar?.Children.OfType<TextBlock>().FirstOrDefault();
                
                if (txtStatus != null)
                    txtStatus.Text = "Guardando cambios...";
            }
            
            bool roleChanged = newRole != user.RolePrincipal;
            bool enabledChanged = newEnabled != user.Enabled;
            
            if (!roleChanged && !enabledChanged)
            {
                _log?.LogInformation("ℹ️ Sin cambios para el usuario {user}", user.FullName);
                if (txtStatus != null)
                    txtStatus.Text = "Sin cambios";
                return;
            }
            
            _log?.LogInformation("💾 Guardando cambios para {user}: Rol={newRole}, Enabled={newEnabled}", 
                user.FullName, newRole, newEnabled);
            
            // Actualizar rol si cambió
            if (roleChanged)
            {
                var success = await Services.Admin.AdminUsersService.Instance.UpdateUserRolesAsync(
                    user.Id, 
                    new[] { newRole }
                );
                
                if (!success)
                {
                    _log?.LogError("❌ Error actualizando roles del usuario {user}", user.FullName);
                    if (txtStatus != null)
                        txtStatus.Text = $"❌ Error actualizando rol de {user.FullName}";
                    return;
                }
            }
            
            // Actualizar enabled si cambió
            if (enabledChanged)
            {
                var success = await Services.Admin.AdminUsersService.Instance.UpdateUserEnabledAsync(
                    user.Id, 
                    newEnabled
                );
                
                if (!success)
                {
                    _log?.LogError("❌ Error actualizando enabled del usuario {user}", user.FullName);
                    if (txtStatus != null)
                        txtStatus.Text = $"❌ Error actualizando estado de {user.FullName}";
                    return;
                }
            }
            
            _log?.LogInformation("✅ Usuario {user} actualizado correctamente", user.FullName);
            
            // 🆕 CRÍTICO: Limpiar caché para forzar recarga real
            App.Api.ClearGetCache();
            _log?.LogInformation("🗑️ Caché limpiado para forzar recarga de datos actualizados");
            
            // Refrescar lista completa
            if (permissionsContent != null)
            {
                if (txtStatus != null)
                    txtStatus.Text = "Recargando usuarios...";
                
                await LoadUsersInlineAsync(permissionsContent);
                
                _log?.LogInformation("✅ Lista de usuarios refrescada después de guardar cambios");
            }
        }
        catch (Exception ex)
        {
            _log?.LogError(ex, "❌ Error guardando cambios del usuario {user}", user.FullName);
            
            if (txtStatus != null)
                txtStatus.Text = $"❌ Error: {ex.Message}";
        }
        finally
        {
            user.IsBusy = false;
        }
    }

    /// <summary>3. Clientes (ADMIN, EDITOR).</summary>
    private UIElement CreateClientsContent()
    {
        var mainStack = new StackPanel { Spacing = 8 };
        
        // Barra de filtros compacta
        var filtersCard = new Border
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 26, 35, 50)),
            BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 45, 62, 80)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 0)
        };
        
        var filtersStack = new StackPanel { Spacing = 8 };
        
        // Una sola fila compacta con filtros reorganizados
        var searchGrid = new Grid { ColumnSpacing = 8 };
        searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) }); // Búsqueda
        searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(240, GridUnitType.Pixel) }); // Nombre comercial
        searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200, GridUnitType.Pixel) }); // Provincia
        searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150, GridUnitType.Pixel) }); // Nota
        searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Buscar
        searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Limpiar
        
        var txtSearchClientes = new TextBox
        {
            PlaceholderText = "Buscar por nombre...",
            Tag = "searchQ",
            Height = 32,
            MinWidth = 180
        };
        txtSearchClientes.KeyDown += (s, e) =>
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                e.Handled = true;
                _ = LoadClientesAsync(mainStack);
            }
            else if (e.Key == Windows.System.VirtualKey.Escape)
            {
                e.Handled = true;
                txtSearchClientes.Text = "";
            }
        };
        Grid.SetColumn(txtSearchClientes, 0);
        searchGrid.Children.Add(txtSearchClientes);
        
        var txtNombreComercial = new TextBox
        {
            PlaceholderText = "Nombre comercial",
            Tag = "filterNombreComercial",
            Height = 32
        };
        Grid.SetColumn(txtNombreComercial, 1);
        searchGrid.Children.Add(txtNombreComercial);
        
        var txtProvincia = new TextBox
        {
            PlaceholderText = "Provincia",
            Tag = "filterProvincia",
            Height = 32
        };
        Grid.SetColumn(txtProvincia, 2);
        searchGrid.Children.Add(txtProvincia);
        
        var cmbHasNota = new ComboBox
        {
            PlaceholderText = "Nota",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Tag = "filterHasNota",
            Height = 32
        };
        cmbHasNota.Items.Add("Todos");
        cmbHasNota.Items.Add("Con nota");
        cmbHasNota.Items.Add("Sin nota");
        cmbHasNota.SelectedIndex = 0;
        Grid.SetColumn(cmbHasNota, 3);
        searchGrid.Children.Add(cmbHasNota);
        
        // Botón Buscar con icono WinUI
        var btnSearchClientes = new Button
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 22, 168, 184)),
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
            Width = 40,
            Height = 40,
            CornerRadius = new CornerRadius(4),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Padding = new Thickness(0)
        };
        var searchIcon = new SymbolIcon(Symbol.Find);
        btnSearchClientes.Content = searchIcon;
        ToolTipService.SetToolTip(btnSearchClientes, "Buscar");
        Grid.SetColumn(btnSearchClientes, 4);
        searchGrid.Children.Add(btnSearchClientes);
        
        // Botón Limpiar con icono WinUI
        var btnClearFilters = new Button
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 45, 62, 80)),
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
            Width = 40,
            Height = 40,
            CornerRadius = new CornerRadius(4),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Padding = new Thickness(0)
        };
        var clearIcon = new SymbolIcon(Symbol.Clear);
        btnClearFilters.Content = clearIcon;
        ToolTipService.SetToolTip(btnClearFilters, "Limpiar filtros");
        Grid.SetColumn(btnClearFilters, 5);
        searchGrid.Children.Add(btnClearFilters);
        
        filtersStack.Children.Add(searchGrid);
        
        filtersCard.Child = filtersStack;
        mainStack.Children.Add(filtersCard);
        
        
        // Panel de edición (arriba) - CRÍTICO: Añadir al mainStack ANTES de la lista
        var editPanel = CreateClienteEditPanel();
        editPanel.Visibility = Visibility.Collapsed;
        editPanel.Tag = "editPanel";
        // Guardar referencia para acceso rápido
        editPanel.Name = "ClienteEditPanel"; // Nombre único para FindName()
        mainStack.Children.Add(editPanel);
        
        // Barra de estado y paginación (una sola línea)
        var statusBar = new Grid { Margin = new Thickness(0, 8, 0, 4) };
        statusBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        statusBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        statusBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        statusBar.ColumnSpacing = 12;
        
        var btnNewCliente = new Button
        {
            Content = "➕ Nuevo",
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 34, 197, 94)),
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
            Padding = new Thickness(12, 6, 12, 6),
            CornerRadius = new CornerRadius(4),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Height = 32
        };
        Grid.SetColumn(btnNewCliente, 0);
        statusBar.Children.Add(btnNewCliente);
        
        var txtStatusClientes = new TextBlock
        {
            Text = "Cargando...",
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 12,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 170, 180, 191)),
            Tag = "statusText"
        };
        Grid.SetColumn(txtStatusClientes, 1);
        statusBar.Children.Add(txtStatusClientes);
        
        // Paginación compacta
        var paginationStack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Tag = "pagination"
        };
        
        var btnPrevPage = new Button
        {
            Content = "«",
            Padding = new Thickness(8, 4, 8, 4),
            Tag = "prevPage",
            IsEnabled = false,
            Height = 28,
            MinWidth = 32
        };
        paginationStack.Children.Add(btnPrevPage);
        
        var txtPageInfo = new TextBlock
        {
            Text = "1/1",
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 12,
            Margin = new Thickness(4, 0, 4, 0),
            Tag = "pageInfo"
        };
        paginationStack.Children.Add(txtPageInfo);
        
        var btnNextPage = new Button
        {
            Content = "»",
            Padding = new Thickness(8, 4, 8, 4),
            Tag = "nextPage",
            IsEnabled = false,
            Height = 28,
            MinWidth = 32
        };
        paginationStack.Children.Add(btnNextPage);
        
        Grid.SetColumn(paginationStack, 2);
        statusBar.Children.Add(paginationStack);
        
        mainStack.Children.Add(statusBar);
        
        // Lista de clientes (más densa)
        var clientesContainer = new StackPanel
        {
            Spacing = 4,
            Margin = new Thickness(0, 0, 0, 0),
            Tag = "clientesContainer"
        };
        mainStack.Children.Add(clientesContainer);
        
        // Conectar eventos
        btnSearchClientes.Click += async (s, e) => await LoadClientesAsync(mainStack);
        btnClearFilters.Click += async (s, e) =>
        {
            _log?.LogInformation("CLIENTES_UI btnClearFilters_Click | START");
            
            // Limpiar todos los filtros
            txtSearchClientes.Text = "";
            txtNombreComercial.Text = "";
            txtProvincia.Text = "";
            cmbHasNota.SelectedIndex = 0;
            
            // Cerrar panel de edición si está abierto
            var editPanel = mainStack.Children.OfType<Border>().FirstOrDefault(b => "editPanel".Equals(b.Tag));
            if (editPanel != null)
            {
                _log?.LogInformation("CLIENTES_UI btnClearFilters_Click | closing editPanel");
                editPanel.Visibility = Visibility.Collapsed;
            }
            
            // Recargar lista
            _log?.LogInformation("CLIENTES_UI btnClearFilters_Click | calling LoadClientesAsync");
            await LoadClientesAsync(mainStack);
            _log?.LogInformation("CLIENTES_UI btnClearFilters_Click | END");
        };
        btnNewCliente.Click += (s, e) =>
        {
            _log?.LogInformation("CLIENTES_UI btnNewCliente_Click | START");
            
            // Cerrar panel de edición existente antes de crear nuevo
            var editPanel = mainStack.Children.OfType<Border>().FirstOrDefault(b => "editPanel".Equals(b.Tag));
            if (editPanel != null)
            {
                _log?.LogInformation("CLIENTES_UI btnNewCliente_Click | closing previous editPanel");
                editPanel.Visibility = Visibility.Collapsed;
            }
            
            _log?.LogInformation("CLIENTES_UI btnNewCliente_Click | calling ShowClienteEditPanel");
            ShowClienteEditPanel(mainStack, null);
            _log?.LogInformation("CLIENTES_UI btnNewCliente_Click | END");
        };
        btnPrevPage.Click += async (s, e) => await NavigateClientesPageAsync(mainStack, -1);
        btnNextPage.Click += async (s, e) => await NavigateClientesPageAsync(mainStack, 1);
        
        // Cargar inicial
        _ = LoadClientesAsync(mainStack);
        
        return mainStack;
    }
    
    /// <summary>Crea el panel de edición de cliente.</summary>
    private Border CreateClienteEditPanel()
    {
        var panel = new Border
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 26, 35, 50)),
            BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 22, 168, 184)),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 12, 0, 0)
        };
        
        var stack = new StackPanel { Spacing = 12 };
        
        // Título
        var titleGrid = new Grid();
        titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        
        var txtTitle = new TextBlock
        {
            Text = "Nuevo Cliente",
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 22, 168, 184)),
            Tag = "editTitle"
        };
        Grid.SetColumn(txtTitle, 0);
        titleGrid.Children.Add(txtTitle);
        
        var btnClose = new Button
        {
            Content = "✕",
            FontSize = 18,
            Width = 32,
            Height = 32,
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
            Tag = "closeEdit"
        };
        Grid.SetColumn(btnClose, 1);
        titleGrid.Children.Add(btnClose);
        
        stack.Children.Add(titleGrid);
        
        // Formulario
        var formGrid = new Grid { ColumnSpacing = 12, RowSpacing = 12 };
        formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        
        // Fila 1: Nombre (span 2) + IdPuntoop
        var txtNombre = new TextBox
        {
            Header = "Nombre *",
            PlaceholderText = "Nombre del cliente",
            Tag = "editNombre"
        };
        Grid.SetColumn(txtNombre, 0);
        Grid.SetColumnSpan(txtNombre, 2);
        Grid.SetRow(txtNombre, 0);
        formGrid.Children.Add(txtNombre);
        
        var txtEditIdPuntoop = new TextBox
        {
            Header = "ID Punto OP",
            PlaceholderText = "Opcional",
            Tag = "editIdPuntoop"
        };
        Grid.SetColumn(txtEditIdPuntoop, 2);
        Grid.SetRow(txtEditIdPuntoop, 0);
        formGrid.Children.Add(txtEditIdPuntoop);
        
        // Fila 2: LocalNum + NombreComercial + Provincia
        var txtEditLocalNum = new TextBox
        {
            Header = "Local Num",
            PlaceholderText = "Opcional",
            Tag = "editLocalNum"
        };
        Grid.SetColumn(txtEditLocalNum, 0);
        Grid.SetRow(txtEditLocalNum, 1);
        formGrid.Children.Add(txtEditLocalNum);
        
        var txtNombreComercial = new TextBox
        {
            Header = "Nombre Comercial",
            PlaceholderText = "Opcional",
            Tag = "editNombreComercial"
        };
        Grid.SetColumn(txtNombreComercial, 1);
        Grid.SetRow(txtNombreComercial, 1);
        formGrid.Children.Add(txtNombreComercial);
        
        var txtEditProvincia = new TextBox
        {
            Header = "Provincia",
            PlaceholderText = "Opcional",
            Tag = "editProvincia"
        };
        Grid.SetColumn(txtEditProvincia, 2);
        Grid.SetRow(txtEditProvincia, 1);
        formGrid.Children.Add(txtEditProvincia);
        
        // Fila 3: Nota (span 3)
        var txtNota = new TextBox
        {
            Header = "Nota",
            PlaceholderText = "Nota interna (opcional)",
            AcceptsReturn = true,
            Height = 80,
            Tag = "editNota"
        };
        Grid.SetColumn(txtNota, 0);
        Grid.SetColumnSpan(txtNota, 3);
        Grid.SetRow(txtNota, 2);
        formGrid.Children.Add(txtNota);
        
        stack.Children.Add(formGrid);
        
        // Campos solo lectura (si es edición)
        var readOnlyStack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 16,
            Visibility = Visibility.Collapsed,
            Tag = "readOnlyFields"
        };
        
        var txtDataUpdate = new TextBlock
        {
            FontSize = 12,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 170, 180, 191)),
            Tag = "dataUpdate"
        };
        readOnlyStack.Children.Add(txtDataUpdate);
        
        stack.Children.Add(readOnlyStack);
        
        // Botones de acción
        var actionsGrid = new Grid { Margin = new Thickness(0, 8, 0, 0) };
        actionsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        actionsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        actionsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        actionsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        actionsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        actionsGrid.ColumnSpacing = 8;
        
        var btnSave = new Button
        {
            Content = "💾 Guardar",
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 22, 168, 184)),
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
            Padding = new Thickness(20, 10, 20, 10),
            CornerRadius = new CornerRadius(6),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Tag = "btnSave"
        };
        Grid.SetColumn(btnSave, 0);
        actionsGrid.Children.Add(btnSave);
        
        var btnSaveNotaOnly = new Button
        {
            Content = "📝 Guardar solo nota",
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 59, 130, 246)),
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
            Padding = new Thickness(16, 10, 16, 10),
            CornerRadius = new CornerRadius(6),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Visibility = Visibility.Collapsed,
            Tag = "btnSaveNotaOnly"
        };
        Grid.SetColumn(btnSaveNotaOnly, 1);
        actionsGrid.Children.Add(btnSaveNotaOnly);
        
        var btnDelete = new Button
        {
            Content = "🗑️ Eliminar",
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 220, 38, 38)),
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
            Padding = new Thickness(16, 10, 16, 10),
            CornerRadius = new CornerRadius(6),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Visibility = Visibility.Collapsed,
            Tag = "btnDelete"
        };
        Grid.SetColumn(btnDelete, 4);
        actionsGrid.Children.Add(btnDelete);
        
        stack.Children.Add(actionsGrid);
        
        panel.Child = stack;
        return panel;
    }
    
    /// <summary>Muestra el panel de edición de cliente.</summary>
    private void ShowClienteEditPanel(StackPanel container, Models.Dtos.Catalog.ClienteDto? cliente)
    {
        _log?.LogInformation("CLIENTES_UI ShowClienteEditPanel | START | mode={mode} clienteId={id}", 
            cliente == null ? "create" : "edit", cliente?.Id);
        
        // CRÍTICO: Buscar el editPanel usando múltiples métodos
        Border? editPanel = null;
        
        // Método 1: Por Tag (más común)
        editPanel = container.Children.OfType<Border>().FirstOrDefault(b => "editPanel".Equals(b.Tag));
        
        // Método 2: Si no se encuentra, buscar por Name (fallback)
        if (editPanel == null)
        {
            editPanel = container.Children.OfType<Border>().FirstOrDefault(b => "ClienteEditPanel".Equals(b.Name));
            if (editPanel != null)
            {
                _log?.LogInformation("CLIENTES_UI ShowClienteEditPanel | editPanel found by Name (Tag was lost)");
                // Restaurar el Tag
                editPanel.Tag = "editPanel";
            }
        }
        
        // Método 3: Último recurso - buscar por tipo y estructura interna
        if (editPanel == null)
        {
            editPanel = container.Children.OfType<Border>().FirstOrDefault(b => 
                b.Child is StackPanel sp && sp.Children.OfType<TextBlock>().Any(tb => tb.Tag?.ToString() == "editTitle")
            );
            if (editPanel != null)
            {
                _log?.LogInformation("CLIENTES_UI ShowClienteEditPanel | editPanel found by structure (Tag and Name were lost)");
                // Restaurar referencias
                editPanel.Name = "ClienteEditPanel";
                editPanel.Tag = "editPanel";
            }
        }
        
        if (editPanel == null)
        {
            _log?.LogError("CLIENTES_UI ShowClienteEditPanel | editPanel NOT FOUND after 3 attempts | ABORT");
            _log?.LogError("CLIENTES_UI ShowClienteEditPanel | container has {count} children", container.Children.Count);
            // Log detallado de lo que hay en el container
            foreach (var child in container.Children)
            {
                _log?.LogError("CLIENTES_UI ShowClienteEditPanel | child: Type={type}, Name={name}, Tag={tag}", 
                    child.GetType().Name, 
                    (child as FrameworkElement)?.Name ?? "(null)", 
                    (child as FrameworkElement)?.Tag ?? "(null)");
            }
            return;
        }
        
        var stack = editPanel.Child as StackPanel;
        if (stack == null)
        {
            _log?.LogError("CLIENTES_UI ShowClienteEditPanel | stack=NULL | ABORT");
            return;
        }
        
        // Buscar controles
        var txtTitle = FindControlByTag<TextBlock>(stack, "editTitle");
        var txtNombre = FindControlByTag<TextBox>(stack, "editNombre");
        var txtIdPuntoop = FindControlByTag<TextBox>(stack, "editIdPuntoop");
        var txtLocalNum = FindControlByTag<TextBox>(stack, "editLocalNum");
        var txtNombreComercial = FindControlByTag<TextBox>(stack, "editNombreComercial");
        var txtProvincia = FindControlByTag<TextBox>(stack, "editProvincia");
        var txtNota = FindControlByTag<TextBox>(stack, "editNota");
        var readOnlyFields = FindControlByTag<StackPanel>(stack, "readOnlyFields");
        var txtDataUpdate = FindControlByTag<TextBlock>(stack, "dataUpdate");
        var btnSave = FindControlByTag<Button>(stack, "btnSave");
        var btnSaveNotaOnly = FindControlByTag<Button>(stack, "btnSaveNotaOnly");
        var btnDelete = FindControlByTag<Button>(stack, "btnDelete");
        var btnClose = FindControlByTag<Button>(stack, "closeEdit");
        
        if (cliente == null)
        {
            // Modo creación
            if (txtTitle != null) txtTitle.Text = "Nuevo Cliente";
            if (txtNombre != null) txtNombre.Text = "";
            if (txtIdPuntoop != null) txtIdPuntoop.Text = "";
            if (txtLocalNum != null) txtLocalNum.Text = "";
            if (txtNombreComercial != null) txtNombreComercial.Text = "";
            if (txtProvincia != null) txtProvincia.Text = "";
            if (txtNota != null) txtNota.Text = "";
            if (readOnlyFields != null) readOnlyFields.Visibility = Visibility.Collapsed;
            if (btnSaveNotaOnly != null) btnSaveNotaOnly.Visibility = Visibility.Collapsed;
            if (btnDelete != null) btnDelete.Visibility = Visibility.Collapsed;
            
            // Guardar referencia al container en el Tag
            editPanel.Tag = new { Mode = "create", Cliente = (Models.Dtos.Catalog.ClienteDto?)null, Container = container };
        }
        else
        {
            // Modo edición
            if (txtTitle != null) txtTitle.Text = $"Editar Cliente #{cliente.Id}";
            if (txtNombre != null) txtNombre.Text = cliente.Nombre ?? "";
            if (txtIdPuntoop != null) txtIdPuntoop.Text = cliente.IdPuntoop?.ToString() ?? "";
            if (txtLocalNum != null) txtLocalNum.Text = cliente.LocalNum?.ToString() ?? "";
            if (txtNombreComercial != null) txtNombreComercial.Text = cliente.NombreComercial ?? "";
            if (txtProvincia != null) txtProvincia.Text = cliente.Provincia ?? "";
            if (txtNota != null) txtNota.Text = cliente.Nota ?? "";
            
            if (readOnlyFields != null) readOnlyFields.Visibility = Visibility.Visible;
            if (txtDataUpdate != null && cliente.DataUpdate.HasValue)
                txtDataUpdate.Text = $"Última actualización: {cliente.DataUpdate.Value:dd/MM/yyyy HH:mm}";
            
            if (btnSaveNotaOnly != null) btnSaveNotaOnly.Visibility = Visibility.Visible;
            if (btnDelete != null) btnDelete.Visibility = Visibility.Visible;
            
            // Guardar referencia al container en el Tag
            editPanel.Tag = new { Mode = "edit", Cliente = cliente, Container = container };
        }
        
        _log?.LogInformation("CLIENTES_UI ShowClienteEditPanel | disconnecting old handlers...");
        
        // Desconectar eventos anteriores
        if (btnSave != null)
        {
            btnSave.Click -= OnSaveClienteClick;
        }
        
        if (btnSaveNotaOnly != null)
        {
            btnSaveNotaOnly.Click -= OnSaveNotaOnlyClick;
        }
        
        if (btnDelete != null)
        {
            btnDelete.Click -= OnDeleteClienteClick;
        }
        
        if (btnClose != null)
        {
            btnClose.Click -= OnCloseEditPanelClick;
        }
        
        _log?.LogInformation("CLIENTES_UI ShowClienteEditPanel | reconnecting new handlers...");
        
        // Reconectar eventos
        if (btnSave != null)
        {
            btnSave.Click += OnSaveClienteClick;
        }
        
        if (btnSaveNotaOnly != null)
        {
            btnSaveNotaOnly.Click += OnSaveNotaOnlyClick;
        }
        
        if (btnDelete != null)
        {
            btnDelete.Click += OnDeleteClienteClick;
        }
        
        if (btnClose != null)
        {
            btnClose.Click += OnCloseEditPanelClick;
        }
        
        _log?.LogInformation("CLIENTES_UI ShowClienteEditPanel | showing editPanel...");
        editPanel.Visibility = Visibility.Visible;
        
        // Scroll y foco
        if (txtNombre != null)
            txtNombre.Focus(FocusState.Programmatic);
        
        _log?.LogInformation("CLIENTES_UI ShowClienteEditPanel | END | editPanel.IsVisible={visible}", 
            editPanel.Visibility == Visibility.Visible);
    }
    
    /// <summary>Maneja el cierre del panel de edición.</summary>
    private void OnCloseEditPanelClick(object sender, RoutedEventArgs e)
    {
        _log?.LogInformation("CLIENTES_UI OnCloseEditPanelClick | START");
        
        try
        {
            var button = sender as Button;
            if (button == null)
            {
                _log?.LogWarning("CLIENTES_UI OnCloseEditPanelClick | button=null");
                return;
            }
            
            // Buscar el panel de edición
            var panel = button.Parent;
            while (panel != null && panel is not Border)
                panel = (panel as FrameworkElement)?.Parent;
            
            if (panel is Border editPanel)
            {
                _log?.LogInformation("CLIENTES_UI OnCloseEditPanelClick | closing editPanel");
                editPanel.Visibility = Visibility.Collapsed;
                // IMPORTANTE: NO limpiar Tag ni Name para que se pueda encontrar después
                // editPanel.Tag = null; ← COMENTADO (era el bug)
                _log?.LogInformation("CLIENTES_UI OnCloseEditPanelClick | editPanel closed (Tag preserved)");
            }
            else
            {
                _log?.LogWarning("CLIENTES_UI OnCloseEditPanelClick | editPanel NOT FOUND");
            }
        }
        catch (Exception ex)
        {
            _log?.LogError(ex, "CLIENTES_UI OnCloseEditPanelClick | EXCEPTION");
        }
        finally
        {
            _log?.LogInformation("CLIENTES_UI OnCloseEditPanelClick | END");
        }
    }
    
    /// <summary>Helper para buscar controles por Tag.</summary>
    private T? FindControlByTag<T>(Panel panel, string tag) where T : FrameworkElement
    {
        foreach (var child in panel.Children)
        {
            if (child is T element && tag.Equals(element.Tag))
                return element;
            
            if (child is Panel childPanel)
            {
                var result = FindControlByTag<T>(childPanel, tag);
                if (result != null) return result;
            }
            else if (child is Border border && border.Child is Panel borderPanel)
            {
                var result = FindControlByTag<T>(borderPanel, tag);
                if (result != null) return result;
            }
            else if (child is Grid grid)
            {
                foreach (var gridChild in grid.Children)
                {
                    if (gridChild is T gridElement && tag.Equals(gridElement.Tag))
                        return gridElement;
                    
                    if (gridChild is Panel gridPanel)
                    {
                        var result = FindControlByTag<T>(gridPanel, tag);
                        if (result != null) return result;
                    }
                }
            }
        }
        
        return null;
    }
    
    /// <summary>Carga clientes con filtros.</summary>
    private async System.Threading.Tasks.Task LoadClientesAsync(StackPanel container, int? targetPage = null)
    {
        _log?.LogInformation("CLIENTES_UI LoadClientesAsync | START | targetPage={page} | thread={thread}", 
            targetPage, Environment.CurrentManagedThreadId);
        
        try
        {
            var txtStatus = FindControlByTag<TextBlock>(container, "statusText");
            var clientesContainer = FindControlByTag<StackPanel>(container, "clientesContainer");
            var txtPageInfo = FindControlByTag<TextBlock>(container, "pageInfo");
            var btnPrevPage = FindControlByTag<Button>(container, "prevPage");
            var btnNextPage = FindControlByTag<Button>(container, "nextPage");
            
            _log?.LogInformation("CLIENTES_UI LoadClientesAsync | found txtStatus={s} container={c} pageInfo={p}", 
                txtStatus != null, clientesContainer != null, txtPageInfo != null);
            
            if (txtStatus != null)
                txtStatus.Text = "Cargando clientes...";
            
            // CRÍTICO: Solo limpiar el clientesContainer, NO tocar el editPanel
            clientesContainer?.Children.Clear();
            _log?.LogInformation("CLIENTES_UI LoadClientesAsync | clientesContainer cleared (editPanel preserved)");
            
            // Leer filtros
            var txtSearch = FindControlByTag<TextBox>(container, "searchQ");
            var txtNombreComercial = FindControlByTag<TextBox>(container, "filterNombreComercial");
            var txtProvincia = FindControlByTag<TextBox>(container, "filterProvincia");
            var cmbHasNota = FindControlByTag<ComboBox>(container, "filterHasNota");
            
            // Combinar búsqueda de nombre y nombre comercial en el parámetro 'q'
            var searchText = txtSearch?.Text?.Trim();
            var nombreComercial = txtNombreComercial?.Text?.Trim();
            
            string? q = null;
            if (!string.IsNullOrWhiteSpace(searchText) && !string.IsNullOrWhiteSpace(nombreComercial))
            {
                // Si ambos están rellenos, combinar (el backend busca en ambos campos con 'q')
                q = $"{searchText} {nombreComercial}";
            }
            else if (!string.IsNullOrWhiteSpace(searchText))
            {
                q = searchText;
            }
            else if (!string.IsNullOrWhiteSpace(nombreComercial))
            {
                q = nombreComercial;
            }
            
            var provincia = txtProvincia?.Text?.Trim();
            bool? hasNota = cmbHasNota?.SelectedIndex switch
            {
                1 => true,
                2 => false,
                _ => null
            };
            
            _log?.LogInformation("CLIENTES_UI LoadClientesAsync | filters: q={q} prov={prov} nota={nota}", q, provincia, hasNota);
            
            // Paginación
            var currentPage = (int?)container.Tag ?? 1;
            var page = targetPage ?? currentPage;
            container.Tag = page;
            
            _log?.LogInformation("CLIENTES_UI LoadClientesAsync | calling API | page={page} size=50...", page);
            
            // Llamar servicio
            var service = new Services.Catalog.ClientesService(App.Api, App.Log);
            var result = await service.ListWithFiltersAsync(
                page: page,
                size: 50,
                q: q,
                idPuntoop: null,
                localNum: null,
                provincia: string.IsNullOrWhiteSpace(provincia) ? null : provincia,
                hasNota: hasNota,
                ct: System.Threading.CancellationToken.None
            );
            
            _log?.LogInformation("CLIENTES_UI LoadClientesAsync | API response | result={r} items={count}", 
                result != null, result?.Items?.Count ?? 0);
            
            if (result == null || result.Items == null || result.Items.Count == 0)
            {
                _log?.LogInformation("CLIENTES_UI LoadClientesAsync | NO_RESULTS");
                
                if (txtStatus != null)
                    txtStatus.Text = "No se encontraron clientes";
                
                clientesContainer?.Children.Add(new TextBlock
                {
                    Text = "No hay clientes que coincidan con los filtros",
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 170, 180, 191)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 0)
                });
                
                return;
            }
            
            // Actualizar UI
            if (txtStatus != null)
                txtStatus.Text = $"{result.TotalCount} encontrado(s)";
            
            if (txtPageInfo != null)
                txtPageInfo.Text = $"{result.Page}/{result.TotalPages}";
            
            if (btnPrevPage != null)
                btnPrevPage.IsEnabled = result.HasPreviousPage;
            
            if (btnNextPage != null)
                btnNextPage.IsEnabled = result.HasNextPage;
            
            _log?.LogInformation("CLIENTES_UI LoadClientesAsync | rendering {count} cards...", result.Items.Count);
            
            // Renderizar clientes
            foreach (var cliente in result.Items)
            {
                var card = CreateClienteCard(container, cliente);
                clientesContainer?.Children.Add(card);
            }
            
            _log?.LogInformation("CLIENTES_UI LoadClientesAsync | SUCCESS | total={total} page={page}/{pages} | editPanel preserved | thread={thread}", 
                result.TotalCount, result.Page, result.TotalPages, Environment.CurrentManagedThreadId);
        }
        catch (Exception ex)
        {
            _log?.LogError(ex, "CLIENTES_UI LoadClientesAsync | EXCEPTION");
            
            var txtStatus = FindControlByTag<TextBlock>(container, "statusText");
            if (txtStatus != null)
                txtStatus.Text = $"❌ Error: {ex.Message}";
        }
    }
    
    /// <summary>Navega entre páginas de clientes.</summary>
    private async System.Threading.Tasks.Task NavigateClientesPageAsync(StackPanel container, int direction)
    {
        var currentPage = (int?)container.Tag ?? 1;
        var newPage = currentPage + direction;
        
        if (newPage < 1) newPage = 1;
        
        await LoadClientesAsync(container, newPage);
    }
    
    /// <summary>Crea una card de cliente.</summary>
    private Border CreateClienteCard(StackPanel container, Models.Dtos.Catalog.ClienteDto cliente)
    {
        var card = new Border
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 26, 35, 50)),
            BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 45, 62, 80)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 6, 8, 6)
        };
        
        // Handler para click en la card
        card.PointerPressed += (s, e) => OnClienteCardClick(container, cliente);
        
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        
        var infoStack = new StackPanel { Spacing = 2 };
        
        // Nombre
        infoStack.Children.Add(new TextBlock
        {
            Text = cliente.Nombre,
            FontSize = 13,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White)
        });
        
        // Detalles en segunda línea
        var detailsStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        
        if (!string.IsNullOrWhiteSpace(cliente.Provincia))
        {
            detailsStack.Children.Add(new TextBlock
            {
                Text = $"📍 {cliente.Provincia}",
                FontSize = 11,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 170, 180, 191))
            });
        }
        
        if (cliente.IdPuntoop.HasValue)
        {
            detailsStack.Children.Add(new Border
            {
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 59, 130, 246)),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(4, 1, 4, 1),
                Child = new TextBlock
                {
                    Text = $"POP:{cliente.IdPuntoop}",
                    FontSize = 10,
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White)
                }
            });
        }
        
        if (cliente.LocalNum.HasValue)
        {
            detailsStack.Children.Add(new Border
            {
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 139, 92, 246)),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(4, 1, 4, 1),
                Child = new TextBlock
                {
                    Text = $"L:{cliente.LocalNum}",
                    FontSize = 10,
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White)
                }
            });
        }
        
        if (!string.IsNullOrWhiteSpace(cliente.Nota))
        {
            var notaIcon = new TextBlock
            {
                Text = "📝",
                FontSize = 12
            };
            ToolTipService.SetToolTip(notaIcon, cliente.Nota);
            detailsStack.Children.Add(notaIcon);
        }
        
        infoStack.Children.Add(detailsStack);
        
        Grid.SetColumn(infoStack, 0);
        grid.Children.Add(infoStack);
        
        // ID discreto
        var txtId = new TextBlock
        {
            Text = $"#{cliente.Id}",
            FontSize = 10,
            Opacity = 0.6,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 170, 180, 191)),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(txtId, 1);
        grid.Children.Add(txtId);
        
        card.Child = grid;
        return card;
    }
    
    /// <summary>Maneja el click en una card de cliente.</summary>
    private void OnClienteCardClick(StackPanel container, Models.Dtos.Catalog.ClienteDto cliente)
    {
        _log?.LogInformation("CLIENTES_UI OnClienteCardClick | clienteId={id} nombre={nombre}", cliente.Id, cliente.Nombre);
        ShowClienteEditPanel(container, cliente);
    }
    
    /// <summary>Maneja el guardado de cliente (POST o PUT).</summary>
    private async void OnSaveClienteClick(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        if (button == null)
        {
            _log?.LogWarning("CLIENTES_UI OnSaveClienteClick | button=null");
            return;
        }
        
        _log?.LogInformation("CLIENTES_UI OnSaveClienteClick | START | thread={thread}", Environment.CurrentManagedThreadId);
        
        // Buscar el panel de edición navegando hacia arriba
        FrameworkElement? current = button;
        Border? editPanel = null;
        
        while (current != null)
        {
            if (current is Border border && border.Tag != null)
            {
                // Verificar si es el editPanel viendo si tiene las propiedades esperadas
                try
                {
                    dynamic tagData = border.Tag;
                    if (tagData.Mode != null)
                    {
                        editPanel = border;
                        string foundMode = Convert.ToString(tagData.Mode);
                        _log?.LogInformation("CLIENTES_UI OnSaveClienteClick | editPanel FOUND | mode={mode}", foundMode);
                        break;
                    }
                }
                catch
                {
                    // No es el panel correcto, continuar
                }
            }
            current = current.Parent as FrameworkElement;
        }
        
        if (editPanel == null)
        {
            _log?.LogError("CLIENTES_UI OnSaveClienteClick | editPanel=NULL | ABORT");
            return;
        }
        
        var stack = editPanel.Child as StackPanel;
        if (stack == null)
        {
            _log?.LogError("CLIENTES_UI OnSaveClienteClick | stack=NULL | ABORT");
            return;
        }
        
        // Obtener metadata del Tag
        dynamic? metadata = editPanel.Tag;
        var mode = metadata?.Mode as string;
        var cliente = metadata?.Cliente as Models.Dtos.Catalog.ClienteDto;
        var mainStack = metadata?.Container as StackPanel;
        
        _log?.LogInformation("CLIENTES_UI OnSaveClienteClick | mode={mode} clienteId={id} container={hasContainer}", 
            mode, cliente?.Id, mainStack != null);
        
        if (mainStack == null)
        {
            _log?.LogError("CLIENTES_UI OnSaveClienteClick | mainStack=NULL | ABORT");
            return;
        }
        
        // Deshabilitar botón mientras guarda
        button.IsEnabled = false;
        _log?.LogInformation("CLIENTES_UI OnSaveClienteClick | button.IsEnabled=false");
        
        try
        {
            // Leer valores del formulario
            var txtNombre = FindControlByTag<TextBox>(stack, "editNombre");
            var txtIdPuntoop = FindControlByTag<TextBox>(stack, "editIdPuntoop");
            var txtLocalNum = FindControlByTag<TextBox>(stack, "editLocalNum");
            var txtNombreComercial = FindControlByTag<TextBox>(stack, "editNombreComercial");
            var txtProvincia = FindControlByTag<TextBox>(stack, "editProvincia");
            var txtNota = FindControlByTag<TextBox>(stack, "editNota");
            
            var nombre = txtNombre?.Text?.Trim();
            
            _log?.LogInformation("CLIENTES_UI OnSaveClienteClick | nombre={nombre}", nombre ?? "(null)");
            
            // Validación básica
            if (string.IsNullOrWhiteSpace(nombre))
            {
                _log?.LogWarning("CLIENTES_UI OnSaveClienteClick | VALIDATION_ERROR | nombre vacío");
                await ShowMessageDialogAsync("Error de validación", "El nombre es obligatorio.");
                return;
            }
            
            if (nombre.Length > 200)
            {
                _log?.LogWarning("CLIENTES_UI OnSaveClienteClick | VALIDATION_ERROR | nombre demasiado largo");
                await ShowMessageDialogAsync("Error de validación", "El nombre no puede superar los 200 caracteres.");
                return;
            }
            
            var service = new Services.Catalog.ClientesService(App.Api, App.Log);
            
            if (mode == "create")
            {
                _log?.LogInformation("CLIENTES_UI OnSaveClienteClick | MODE=CREATE | calling API...");
                
                // Crear nuevo
                var request = new Models.Dtos.Catalog.ClienteCreateRequest
                {
                    Nombre = nombre,
                    IdPuntoop = string.IsNullOrWhiteSpace(txtIdPuntoop?.Text) ? null : int.TryParse(txtIdPuntoop.Text, out var idp) ? idp : null,
                    LocalNum = string.IsNullOrWhiteSpace(txtLocalNum?.Text) ? null : int.TryParse(txtLocalNum.Text, out var ln) ? ln : null,
                    NombreComercial = string.IsNullOrWhiteSpace(txtNombreComercial?.Text) ? null : txtNombreComercial.Text.Trim(),
                    Provincia = string.IsNullOrWhiteSpace(txtProvincia?.Text) ? null : txtProvincia.Text.Trim(),
                    Nota = string.IsNullOrWhiteSpace(txtNota?.Text) ? null : txtNota.Text.Trim()
                };
                
                var result = await service.CreateAsync(request, System.Threading.CancellationToken.None);
                
                if (result != null)
                {
                    _log?.LogInformation("CLIENTES_UI OnSaveClienteClick | CREATE_SUCCESS | newId={id}", result.Id);
                    await ShowMessageDialogAsync("Éxito", $"Cliente '{nombre}' creado correctamente.");
                    
                    // Cerrar panel primero
                    _log?.LogInformation("CLIENTES_UI OnSaveClienteClick | closing editPanel...");
                    editPanel.Visibility = Visibility.Collapsed;
                    editPanel.Tag = null;
                    
                    // 🔄 INVALIDAR CACHÉ antes de recargar para mostrar datos frescos
                    _log?.LogInformation("CLIENTES_UI OnSaveClienteClick | invalidating cache for /api/v1/clientes...");
                    App.Api.InvalidateCacheEntry("/api/v1/clientes");
                    
                    // Recargar lista
                    _log?.LogInformation("CLIENTES_UI OnSaveClienteClick | calling LoadClientesAsync...");
                    await LoadClientesAsync(mainStack);
                    _log?.LogInformation("CLIENTES_UI OnSaveClienteClick | LoadClientesAsync COMPLETED");
                }
                else
                {
                    _log?.LogError("CLIENTES_UI OnSaveClienteClick | CREATE_FAILED | result=null");
                }
            }
            else if (mode == "edit" && cliente != null)
            {
                _log?.LogInformation("CLIENTES_UI OnSaveClienteClick | MODE=EDIT | clienteId={id} | calling API...", cliente.Id);
                
                // Actualizar existente
                var request = new Models.Dtos.Catalog.ClienteUpdateRequest
                {
                    Nombre = nombre,
                    IdPuntoop = string.IsNullOrWhiteSpace(txtIdPuntoop?.Text) ? null : int.TryParse(txtIdPuntoop.Text, out var idp) ? idp : null,
                    LocalNum = string.IsNullOrWhiteSpace(txtLocalNum?.Text) ? null : int.TryParse(txtLocalNum.Text, out var ln) ? ln : null,
                    NombreComercial = string.IsNullOrWhiteSpace(txtNombreComercial?.Text) ? null : txtNombreComercial.Text.Trim(),
                    Provincia = string.IsNullOrWhiteSpace(txtProvincia?.Text) ? null : txtProvincia.Text.Trim(),
                    Nota = string.IsNullOrWhiteSpace(txtNota?.Text) ? null : txtNota.Text.Trim()
                };
                
                var result = await service.UpdateAsync(cliente.Id, request, System.Threading.CancellationToken.None);
                
                if (result != null)
                {
                    _log?.LogInformation("CLIENTES_UI OnSaveClienteClick | UPDATE_SUCCESS | clienteId={id}", cliente.Id);
                    await ShowMessageDialogAsync("Éxito", $"Cliente '{nombre}' actualizado correctamente.");
                    
                    // Cerrar panel primero
                    _log?.LogInformation("CLIENTES_UI OnSaveClienteClick | closing editPanel...");
                    editPanel.Visibility = Visibility.Collapsed;
                    editPanel.Tag = null;
                    
                    // 🔄 INVALIDAR CACHÉ antes de recargar para mostrar datos frescos
                    _log?.LogInformation("CLIENTES_UI OnSaveClienteClick | invalidating cache for /api/v1/clientes...");
                    App.Api.InvalidateCacheEntry("/api/v1/clientes");
                    
                    // Recargar lista
                    _log?.LogInformation("CLIENTES_UI OnSaveClienteClick | calling LoadClientesAsync...");
                    await LoadClientesAsync(mainStack);
                    _log?.LogInformation("CLIENTES_UI OnSaveClienteClick | LoadClientesAsync COMPLETED");
                }
                else
                {
                    _log?.LogError("CLIENTES_UI OnSaveClienteClick | UPDATE_FAILED | result=null");
                }
            }
        }
        catch (Exception ex)
        {
            _log?.LogError(ex, "CLIENTES_UI OnSaveClienteClick | EXCEPTION");
            await ShowMessageDialogAsync("Error", $"Error al guardar cliente:\n\n{ex.Message}");
        }
        finally
        {
            // CRÍTICO: Re-habilitar botón SIEMPRE
            button.IsEnabled = true;
            _log?.LogInformation("CLIENTES_UI OnSaveClienteClick | END | button.IsEnabled=true | thread={thread}", Environment.CurrentManagedThreadId);
        }
    }
    
    /// <summary>Maneja el guardado solo de la nota (PATCH).</summary>
    private async void OnSaveNotaOnlyClick(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        if (button == null)
        {
            _log?.LogWarning("CLIENTES_UI OnSaveNotaOnlyClick | button=null");
            return;
        }
        
        _log?.LogInformation("CLIENTES_UI OnSaveNotaOnlyClick | START | thread={thread}", Environment.CurrentManagedThreadId);
        
        // Buscar el panel de edición
        FrameworkElement? current = button;
        Border? editPanel = null;
        
        while (current != null)
        {
            if (current is Border border && border.Tag != null)
            {
                try
                {
                    dynamic tagData = border.Tag;
                    if (tagData.Mode != null)
                    {
                        editPanel = border;
                        break;
                    }
                }
                catch { }
            }
            current = current.Parent as FrameworkElement;
        }
        
        if (editPanel == null)
        {
            _log?.LogError("CLIENTES_UI OnSaveNotaOnlyClick | editPanel=NULL | ABORT");
            return;
        }
        
        var stack = editPanel.Child as StackPanel;
        if (stack == null)
        {
            _log?.LogError("CLIENTES_UI OnSaveNotaOnlyClick | stack=NULL | ABORT");
            return;
        }
        
        dynamic? metadata = editPanel.Tag;
        var cliente = metadata?.Cliente as Models.Dtos.Catalog.ClienteDto;
        var mainStack = metadata?.Container as StackPanel;
        
        if (cliente == null || mainStack == null)
        {
            _log?.LogError("CLIENTES_UI OnSaveNotaOnlyClick | cliente={c} mainStack={m} | ABORT", cliente != null, mainStack != null);
            return;
        }
        
        _log?.LogInformation("CLIENTES_UI OnSaveNotaOnlyClick | clienteId={id}", cliente.Id);
        
        // Deshabilitar botón mientras guarda
        button.IsEnabled = false;
        
        try
        {
            var txtNota = FindControlByTag<TextBox>(stack, "editNota");
            var nota = string.IsNullOrWhiteSpace(txtNota?.Text) ? null : txtNota.Text.Trim();
            
            _log?.LogInformation("CLIENTES_UI OnSaveNotaOnlyClick | calling API PATCH...");
            
            var service = new Services.Catalog.ClientesService(App.Api, App.Log);
            var result = await service.UpdateNotaAsync(cliente.Id, nota, System.Threading.CancellationToken.None);
            
            if (result != null)
            {
                _log?.LogInformation("CLIENTES_UI OnSaveNotaOnlyClick | UPDATE_SUCCESS | clienteId={id}", cliente.Id);
                await ShowMessageDialogAsync("Éxito", "Nota actualizada correctamente.");
                
                editPanel.Visibility = Visibility.Collapsed;
                editPanel.Tag = null;
                
                // 🔄 INVALIDAR CACHÉ antes de recargar para mostrar datos frescos
                _log?.LogInformation("CLIENTES_UI OnSaveNotaOnlyClick | invalidating cache for /api/v1/clientes...");
                App.Api.InvalidateCacheEntry("/api/v1/clientes");
                
                _log?.LogInformation("CLIENTES_UI OnSaveNotaOnlyClick | calling LoadClientesAsync...");
                await LoadClientesAsync(mainStack);
                _log?.LogInformation("CLIENTES_UI OnSaveNotaOnlyClick | LoadClientesAsync COMPLETED");
            }
            else
            {
                _log?.LogError("CLIENTES_UI OnSaveNotaOnlyClick | UPDATE_FAILED | result=null");
            }
        }
        catch (Exception ex)
        {
            _log?.LogError(ex, "CLIENTES_UI OnSaveNotaOnlyClick | EXCEPTION");
            await ShowMessageDialogAsync("Error", $"Error al actualizar nota:\n\n{ex.Message}");
        }
        finally
        {
            button.IsEnabled = true;
            _log?.LogInformation("CLIENTES_UI OnSaveNotaOnlyClick | END | button.IsEnabled=true");
        }
    }
    
    /// <summary>Maneja la eliminación de cliente.</summary>
    private async void OnDeleteClienteClick(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        if (button == null)
        {
            _log?.LogWarning("CLIENTES_UI OnDeleteClienteClick | button=null");
            return;
        }
        
        _log?.LogInformation("CLIENTES_UI OnDeleteClienteClick | START");
        
        // Buscar el panel de edición
        FrameworkElement? current = button;
        Border? editPanel = null;
        
        while (current != null)
        {
            if (current is Border border && border.Tag != null)
            {
                try
                {
                    dynamic tagData = border.Tag;
                    if (tagData.Mode != null)
                    {
                        editPanel = border;
                        break;
                    }
                }
                catch { }
            }
            current = current.Parent as FrameworkElement;
        }
        
        if (editPanel == null)
        {
            _log?.LogError("CLIENTES_UI OnDeleteClienteClick | editPanel=NULL | ABORT");
            return;
        }
        
        dynamic? metadata = editPanel.Tag;
        var cliente = metadata?.Cliente as Models.Dtos.Catalog.ClienteDto;
        var mainStack = metadata?.Container as StackPanel;
        
        if (cliente == null || mainStack == null)
        {
            _log?.LogError("CLIENTES_UI OnDeleteClienteClick | cliente={c} mainStack={m} | ABORT", cliente != null, mainStack != null);
            return;
        }
        
        _log?.LogInformation("CLIENTES_UI OnDeleteClienteClick | clienteId={id} nombre={nombre}", cliente.Id, cliente.Nombre);
        
        // Deshabilitar botón mientras procesa
        button.IsEnabled = false;
        
        try
        {
            // Confirmar
            var dialog = new ContentDialog
            {
                Title = "¿Eliminar cliente?",
                Content = $"¿Estás seguro de que deseas eliminar el cliente '{cliente.Nombre}'?\n\nEsta acción no se puede deshacer.",
                PrimaryButtonText = "🗑️ Eliminar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.Content.XamlRoot
            };
            
            var dialogResult = await dialog.ShowAsync();
            _log?.LogInformation("CLIENTES_UI OnDeleteClienteClick | dialog result={result}", dialogResult);
            
            if (dialogResult != ContentDialogResult.Primary)
            {
                _log?.LogInformation("CLIENTES_UI OnDeleteClienteClick | USER_CANCELLED");
                return;
            }
            
            _log?.LogInformation("CLIENTES_UI OnDeleteClienteClick | calling API DELETE...");
            
            var service = new Services.Catalog.ClientesService(App.Api, App.Log);
            var success = await service.DeleteAsync(cliente.Id, System.Threading.CancellationToken.None);
            
            if (success)
            {
                _log?.LogInformation("CLIENTES_UI OnDeleteClienteClick | DELETE_SUCCESS | clienteId={id}", cliente.Id);
                await ShowMessageDialogAsync("Éxito", $"Cliente '{cliente.Nombre}' eliminado correctamente.");
                
                editPanel.Visibility = Visibility.Collapsed;
                editPanel.Tag = null;
                
                // 🔄 INVALIDAR CACHÉ antes de recargar para mostrar datos frescos
                _log?.LogInformation("CLIENTES_UI OnDeleteClienteClick | invalidating cache for /api/v1/clientes...");
                App.Api.InvalidateCacheEntry("/api/v1/clientes");
                
                _log?.LogInformation("CLIENTES_UI OnDeleteClienteClick | calling LoadClientesAsync...");
                await LoadClientesAsync(mainStack);
                _log?.LogInformation("CLIENTES_UI OnDeleteClienteClick | LoadClientesAsync COMPLETED");
            }
            else
            {
                _log?.LogError("CLIENTES_UI OnDeleteClienteClick | DELETE_FAILED | success=false");
            }
        }
        catch (System.Net.Http.HttpRequestException httpEx) when (httpEx.Message.Contains("409"))
        {
            _log?.LogWarning("CLIENTES_UI OnDeleteClienteClick | CONFLICT_409 | clienteId={id}", cliente.Id);
            await ShowMessageDialogAsync("No se puede eliminar", 
                "Este cliente no puede ser eliminado porque está siendo utilizado en otros registros del sistema.");
        }
        catch (Exception ex)
        {
            _log?.LogError(ex, "CLIENTES_UI OnDeleteClienteClick | EXCEPTION | clienteId={id}", cliente.Id);
            await ShowMessageDialogAsync("Error", $"Error al eliminar cliente:\n\n{ex.Message}");
        }
        finally
        {
            button.IsEnabled = true;
            _log?.LogInformation("CLIENTES_UI OnDeleteClienteClick | END | button.IsEnabled=true");
        }
    }
    
    /// <summary>Muestra un diálogo de mensaje.</summary>
    private async System.Threading.Tasks.Task ShowMessageDialogAsync(string title, string content)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            CloseButtonText = "OK",
            XamlRoot = this.Content.XamlRoot
        };
        
        await dialog.ShowAsync();
    }

    /// <summary>4. Grupos y Tipos (ADMIN, EDITOR).</summary>
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
        
        stack.Children.Add(new TextBlock
        {
            Text = "🔧 Pendiente de implementar. Reutilizará GruposService y TiposService existentes.",
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 245, 158, 11)),
            Margin = new Thickness(0, 12, 0, 0)
        });
        
        return stack;
    }

    /// <summary>5. Integraciones (ADMIN).</summary>
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
        
        stack.Children.Add(new TextBlock
        {
            Text = "🔧 Pendiente de implementar. Editará appsettings.json.",
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 245, 158, 11)),
            Margin = new Thickness(0, 12, 0, 0)
        });
        
        return stack;
    }

    /// <summary>6. Importación/Exportación (ADMIN).</summary>
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
        
        stack.Children.Add(new TextBlock
        {
            Text = "🔧 Pendiente de implementar. Usará ExcelPartesImportService existente.",
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 245, 158, 11)),
            Margin = new Thickness(0, 12, 0, 0)
        });
        
        return stack;
    }

    /// <summary>7. Usuarios online/Presencia (ADMIN) - Abre UsersOnlineWindow existente.</summary>
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
        
        // Botón para abrir UsersOnlineWindow
        var btnOpenPresence = new Button
        {
            Content = "👥 Ver Usuarios Online",
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 22, 168, 184)),
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
            Padding = new Thickness(20, 12, 20, 12),
            CornerRadius = new CornerRadius(6),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Margin = new Thickness(0, 12, 0, 0)
        };
        
        btnOpenPresence.Click += (s, e) =>
        {
            try
            {
                _log?.LogInformation("👥 Abriendo ventana de usuarios online desde Settings");
                App.ShowUsersWindow();
            }
            catch (Exception ex)
            {
                _log?.LogError(ex, "Error abriendo ventana de usuarios online");
            }
        };
        
        stack.Children.Add(btnOpenPresence);
        
        return stack;
    }

    /// <summary>8. Parámetros (ADMIN).</summary>
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
        
        stack.Children.Add(new TextBlock
        {
            Text = "🔧 Pendiente de implementar.",
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 245, 158, 11)),
            Margin = new Thickness(0, 12, 0, 0)
        });
        
        return stack;
    }
    
    /// <summary>9. Salir - Vuelve a MainWindow.</summary>
    private UIElement CreateExitContent()
    {
        var stack = new StackPanel { Spacing = 16 };
        
        stack.Children.Add(new TextBlock
        {
            Text = "Volviendo a la pantalla principal...",
            FontSize = 16,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 230, 237, 243))
        });
        
        return stack;
    }
    
    /// <summary>Echa a un usuario online (revoca todas sus sesiones activas).</summary>
    private async System.Threading.Tasks.Task KickUserAsync(Models.UserViewModel user)
    {
        StackPanel? permissionsContent = null;
        TextBlock? txtStatus = null;
        
        try
        {
            // Confirmar acción
            var dialog = new ContentDialog
            {
                Title = "¿Echar usuario?",
                Content = $"¿Seguro que quieres echar a {user.FullName}?\n\n" +
                          "Se cerrarán todas sus sesiones activas y será marcado como offline.",
                PrimaryButtonText = "🚪 Echar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.Content.XamlRoot
            };
            
            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return;
            
            // Obtener referencia al contenedor principal
            permissionsContent = SectionContentContainer.Child as StackPanel;
            if (permissionsContent != null)
            {
                var toolbar = permissionsContent.Children.OfType<StackPanel>().FirstOrDefault(sp => sp.Orientation == Orientation.Horizontal);
                txtStatus = toolbar?.Children.OfType<TextBlock>().FirstOrDefault();
                
                if (txtStatus != null)
                    txtStatus.Text = $"⏳ Echando a {user.FullName}...";
            }
            
            user.IsBusy = true;
            
            _log?.LogInformation("🚪 Echando usuario {user}...", user.FullName);
            
            var success = await Services.Admin.AdminUsersService.Instance.KickUserAsync(user.Id);
            
            if (success)
            {
                _log?.LogInformation("✅ Usuario {user} echado correctamente", user.FullName);
                
                // Actualizar estado local
                user.IsOnline = false;
                
                // Limpiar caché y refrescar
                App.Api.ClearGetCache();
                
                if (permissionsContent != null)
                {
                    if (txtStatus != null)
                        txtStatus.Text = "Recargando usuarios...";
                    
                    await LoadUsersInlineAsync(permissionsContent);
                    
                    if (txtStatus != null)
                        txtStatus.Text = $"✅ {user.FullName} ha sido echado correctamente";
                }
            }
            else
            {
                _log?.LogError("❌ Error echando usuario {user}", user.FullName);
                
                if (txtStatus != null)
                    txtStatus.Text = $"❌ Error al echar a {user.FullName}";
            }
        }
        catch (Exception ex)
        {
            _log?.LogError(ex, "❌ Error en KickUserAsync para {user}", user.FullName);
            
            if (txtStatus != null)
                txtStatus.Text = $"❌ Error: {ex.Message}";
        }
        finally
        {
            user.IsBusy = false;
        }
    }
}
