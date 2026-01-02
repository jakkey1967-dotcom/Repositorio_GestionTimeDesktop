using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using FluentAssertions;

namespace GestionTime.Desktop.Views;

public sealed class ConfiguracionWindow
{
    private Microsoft.UI.Xaml.Window _window;
    private DiarioPage _parentPage;
    private Grid _rootGrid;
    private TextBlock _txtUserRole;
    private TextBox _txtApiUrl;
    private NumberBox _numTimeout;
    private NumberBox _numRetries;
    private CheckBox _chkIgnoreSSL;
    private Border _connectionSection;
    private CheckBox _chkEnableLogging;
    private ComboBox _cmbLogLevel;
    private CheckBox _chkLogToFile;
    private CheckBox _chkLogHttp;
    private CheckBox _chkLogErrors;
    private CheckBox _chkLogDebug;
    private TextBox _txtLogDirectory;
    private ComboBox _cmbLogRotation;
    private TextBox _txtLogRetention;
    private ComboBox _cmbTheme;
    private NumberBox _numAutoRefresh;
    private CheckBox _chkAutoLogin;
    private CheckBox _chkStartMinimized;
    private CheckBox _chkDebugMode;
    private TextBlock _txtConfigStatus;
    private Button _btnTestConnection;
    private Button _btnSave;
    private Button _btnDebugConfig;

    public void ShowWindow(DiarioPage parentPage)
    {
        _parentPage = parentPage;
        CreateWindow();
        ConfigureWindow();
        _window.Activate();
    }

    private void CreateWindow()
    {
        _window = new Microsoft.UI.Xaml.Window
        {
            Title = "⚙️ Configuración del Sistema - GestionTime"
        };

        CreateContent();
        _window.Content = _rootGrid;
        
        LoadConfigurationAsync();
    }

    private void CreateContent()
    {
        // Grid principal
        _rootGrid = new Grid
        {
            Padding = new Thickness(10),
            RowSpacing = 10,
            Opacity = 0
        };

        // Fondo igual a DiarioPage
        var backgroundBrush = new ImageBrush
        {
            ImageSource = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(
                new Uri("ms-appx:///Assets/diario_bg_dark.png")),
            Stretch = Stretch.UniformToFill,
            AlignmentX = AlignmentX.Center,
            AlignmentY = AlignmentY.Center
        };
        _rootGrid.Background = backgroundBrush;

        _rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        _rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Banner
        CreateBanner();
        
        // Contenido
        CreateMainContent();
        
        // Botones inferiores
        CreateBottomButtons();

        // Animación de entrada
        AnimateEntry();
    }

    private void CreateBanner()
    {
        var banner = new Border
        {
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 11, 140, 153)),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(16)
        };
        Grid.SetRow(banner, 0);
        _rootGrid.Children.Add(banner);

        var bannerGrid = new Grid { ColumnSpacing = 16 };
        bannerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        bannerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        bannerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Logo
        var logo = new Image
        {
            Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(
                new Uri("ms-appx:///Assets/LogoOscuro.png")),
            Stretch = Stretch.Uniform,
            MaxHeight = 60,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        Grid.SetColumn(logo, 0);
        bannerGrid.Children.Add(logo);

        // Título
        var titleStack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 4,
            Margin = new Thickness(8, 0, 0, 0)
        };

        titleStack.Children.Add(new TextBlock
        {
            Text = "⚙️ Configuración del Sistema",
            FontSize = 22,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255))
        });

        titleStack.Children.Add(new TextBlock
        {
            Text = "Configuración avanzada de conexión, logs y aplicación",
            FontSize = 14,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
            Opacity = 0.8
        });

        Grid.SetColumn(titleStack, 1);
        bannerGrid.Children.Add(titleStack);

        // Estado del usuario
        var statusStack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 8
        };

        var statusGrid = new Grid { ColumnSpacing = 8 };
        statusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        statusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var indicator = new Ellipse
        {
            Width = 12,
            Height = 12,
            Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129))
        };
        Grid.SetColumn(indicator, 0);
        statusGrid.Children.Add(indicator);

        var rolePanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6
        };

        _txtUserRole = new TextBlock
        {
            Text = "Admin",
            FontSize = 12,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255))
        };
        rolePanel.Children.Add(_txtUserRole);

        rolePanel.Children.Add(new TextBlock
        {
            Text = "configurando",
            FontSize = 12,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
            Opacity = 0.8
        });

        Grid.SetColumn(rolePanel, 1);
        statusGrid.Children.Add(rolePanel);

        statusStack.Children.Add(statusGrid);
        Grid.SetColumn(statusStack, 2);
        bannerGrid.Children.Add(statusStack);

        banner.Child = bannerGrid;
    }

    private void CreateMainContent()
    {
        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Padding = new Thickness(0, 10, 0, 0)
        };
        Grid.SetRow(scrollViewer, 1);
        _rootGrid.Children.Add(scrollViewer);

        var mainStack = new StackPanel { Spacing = 20 };

        // Sección de conexión
        CreateConnectionSection(mainStack);
        
        // Sección de logs
        CreateLogsSection(mainStack);
        
        // Sección de aplicación
        CreateAppSection(mainStack);

        scrollViewer.Content = mainStack;
    }

    private void CreateConnectionSection(StackPanel parent)
    {
        _connectionSection = new Border
        {
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 26, 29, 33)),
            BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 48, 54, 61)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(25, 12, 25, 12),
            Margin = new Thickness(0, 0, 0, 15)
        };

        var stack = new StackPanel { Spacing = 20 };

        stack.Children.Add(new TextBlock
        {
            Text = "⚡ Configuración de Conexión",
            FontSize = 18,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 237, 239, 242))
        });

        // API URL
        var urlGrid = new Grid { RowSpacing = 8 };
        urlGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        urlGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        urlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        urlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        urlGrid.Children.Add(new TextBlock
        {
            Text = "URL del Servidor API",
            FontSize = 14,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 168, 176, 184))
        });

        _txtApiUrl = new TextBox
        {
            PlaceholderText = "https://localhost:2501",
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 17, 19)),
            BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 48, 54, 61)),
            BorderThickness = new Thickness(1),
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 237, 239, 242)),
            Padding = new Thickness(12, 8, 12, 8),
            CornerRadius = new CornerRadius(6),
            Margin = new Thickness(0, 0, 10, 0)
        };
        Grid.SetRow(_txtApiUrl, 1);
        Grid.SetColumn(_txtApiUrl, 0);
        urlGrid.Children.Add(_txtApiUrl);

        _btnTestConnection = new Button
        {
            Content = "🔍 Probar",
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 11, 140, 153)),
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(15, 8, 15, 8),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        _btnTestConnection.Click += OnTestConnectionClick;
        Grid.SetRow(_btnTestConnection, 1);
        Grid.SetColumn(_btnTestConnection, 1);
        urlGrid.Children.Add(_btnTestConnection);

        stack.Children.Add(urlGrid);

        // Timeout y Reintentos
        var configGrid = new Grid { ColumnSpacing = 20 };
        configGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        configGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Timeout
        var timeoutStack = new StackPanel { Spacing = 8 };
        timeoutStack.Children.Add(new TextBlock
        {
            Text = "Timeout (segundos)",
            FontSize = 14,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 168, 176, 184))
        });
        _numTimeout = new NumberBox
        {
            Minimum = 5,
            Maximum = 300,
            Value = 30,
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 17, 19)),
            BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 48, 54, 61)),
            BorderThickness = new Thickness(1),
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 237, 239, 242)),
            CornerRadius = new CornerRadius(6)
        };
        timeoutStack.Children.Add(_numTimeout);
        Grid.SetColumn(timeoutStack, 0);
        configGrid.Children.Add(timeoutStack);

        // Reintentos
        var retriesStack = new StackPanel { Spacing = 8 };
        retriesStack.Children.Add(new TextBlock
        {
            Text = "Número de reintentos",
            FontSize = 14,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 168, 176, 184))
        });
        _numRetries = new NumberBox
        {
            Minimum = 0,
            Maximum = 10,
            Value = 3,
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 17, 19)),
            BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 48, 54, 61)),
            BorderThickness = new Thickness(1),
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 237, 239, 242)),
            CornerRadius = new CornerRadius(6)
        };
        retriesStack.Children.Add(_numRetries);
        Grid.SetColumn(retriesStack, 1);
        configGrid.Children.Add(retriesStack);

        stack.Children.Add(configGrid);

        // Opciones SSL
        _chkIgnoreSSL = new CheckBox
        {
            Content = "Ignorar certificados SSL en desarrollo (no recomendado en producción)",
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 237, 239, 242)),
            FontSize = 14
        };
        stack.Children.Add(_chkIgnoreSSL);

        _connectionSection.Child = stack;
        parent.Children.Add(_connectionSection);
    }

    private void CreateLogsSection(StackPanel parent)
    {
        var section = new Border
        {
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 26, 29, 33)),
            BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 48, 54, 61)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(25),
            Margin = new Thickness(0, 0, 0, 15)
        };

        var stack = new StackPanel { Spacing = 20 };

        // Título con estado actual
        var titleGrid = new Grid();
        titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        titleGrid.Children.Add(new TextBlock
        {
            Text = "📋 Configuración de Logs",
            FontSize = 18,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 237, 239, 242)),
            VerticalAlignment = VerticalAlignment.Center
        });

        // Estado actual del logging
        var statusStack = new StackPanel { Spacing = 4, HorizontalAlignment = HorizontalAlignment.Right };
        statusStack.Children.Add(new TextBlock
        {
            Text = "🟢 Sistema Activo",
            FontSize = 12,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)),
            HorizontalAlignment = HorizontalAlignment.Right
        });
        statusStack.Children.Add(new TextBlock
        {
            Text = "Nivel: Debug",
            FontSize = 11,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 168, 176, 184)),
            HorizontalAlignment = HorizontalAlignment.Right
        });
        Grid.SetColumn(statusStack, 1);
        titleGrid.Children.Add(statusStack);

        stack.Children.Add(titleGrid);

        // Habilitar logging con descripción expandida
        var loggingStack = new StackPanel { Spacing = 8 };
        _chkEnableLogging = new CheckBox
        {
            Content = "Habilitar sistema de logging completo",
            IsChecked = true,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 237, 239, 242)),
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        loggingStack.Children.Add(_chkEnableLogging);
        
        loggingStack.Children.Add(new TextBlock
        {
            Text = "Registra eventos de la aplicación, errores, operaciones de base de datos y llamadas HTTP",
            FontSize = 12,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 168, 176, 184)),
            Margin = new Thickness(24, 0, 0, 0),
            TextWrapping = TextWrapping.Wrap
        });
        stack.Children.Add(loggingStack);

        // Nivel de log mejorado
        var levelStack = new StackPanel { Spacing = 8 };
        levelStack.Children.Add(new TextBlock
        {
            Text = "Nivel de detalle en los logs",
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 168, 176, 184))
        });

        _cmbLogLevel = new ComboBox
        {
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 17, 19)),
            BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 48, 54, 61)),
            BorderThickness = new Thickness(1),
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 237, 239, 242)),
            CornerRadius = new CornerRadius(6),
            MinWidth = 500,
            SelectedIndex = 2,
            FontSize = 13
        };
        _cmbLogLevel.Items.Add(new ComboBoxItem { Content = "🔴 Error - Solo errores críticos y fallos del sistema" });
        _cmbLogLevel.Items.Add(new ComboBoxItem { Content = "⚠️ Warning - Advertencias, errores y situaciones inusuales" });
        _cmbLogLevel.Items.Add(new ComboBoxItem { Content = "ℹ️ Info - Información general de operaciones importantes" });
        _cmbLogLevel.Items.Add(new ComboBoxItem { Content = "🐛 Debug - Información detallada para depuración" });
        _cmbLogLevel.Items.Add(new ComboBoxItem { Content = "🔍 Trace - Información muy detallada (todos los eventos)" });
        levelStack.Children.Add(_cmbLogLevel);
        stack.Children.Add(levelStack);

        // SECCIÓN: DIRECTORIO DE LOGS
        var directorySection = new Border
        {
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 17, 19)),
            BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 59, 130, 246)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(20),
            Margin = new Thickness(0, 5, 0, 5)
        };

        var directoryStack = new StackPanel { Spacing = 15 };
        
        // Título de la sección de directorio
        directoryStack.Children.Add(new TextBlock
        {
            Text = "📁 Configuración de Directorio de Logs",
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 59, 130, 246))
        });

        // Directorio principal
        var mainDirStack = new StackPanel { Spacing = 8 };
        mainDirStack.Children.Add(new TextBlock
        {
            Text = "Directorio principal de logs",
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 168, 176, 184))
        });

        var dirGrid = new Grid { ColumnSpacing = 10 };
        dirGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        dirGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        dirGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        dirGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var txtLogDirectory = new TextBox
        {
            Text = @"C:\Logs\GestionTime",
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 26, 29, 33)),
            BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 48, 54, 61)),
            BorderThickness = new Thickness(1),
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 237, 239, 242)),
            Padding = new Thickness(12, 8, 12, 8),
            CornerRadius = new CornerRadius(6),
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
            FontSize = 12
        };
        _txtLogDirectory = txtLogDirectory; // Asignar referencia
        Grid.SetColumn(txtLogDirectory, 0);
        dirGrid.Children.Add(txtLogDirectory);

        var btnBrowse = new Button
        {
            Content = "📂 Examinar",
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 59, 130, 246)),
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12, 8, 12, 8),
            FontSize = 12
        };
        btnBrowse.Click += (s, e) => OnBrowseLogDirectoryClick(txtLogDirectory);
        Grid.SetColumn(btnBrowse, 1);
        dirGrid.Children.Add(btnBrowse);

        var btnOpenDir = new Button
        {
            Content = "🔍 Abrir",
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)),
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12, 8, 12, 8),
            FontSize = 12
        };
        btnOpenDir.Click += (s, e) => OnOpenLogDirectoryClick(txtLogDirectory);
        Grid.SetColumn(btnOpenDir, 2);
        dirGrid.Children.Add(btnOpenDir);

        // Agregar botón de prueba
        var btnTest = new Button
        {
            Content = "🧪 Probar",
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 168, 85, 247)),
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12, 8, 12, 8),
            FontSize = 12,
            Margin = new Thickness(5, 0, 0, 0)
        };
        btnTest.Click += (s, e) => OnTestLogDirectoryClick(txtLogDirectory);
        Grid.SetColumn(btnTest, 3);
        dirGrid.Children.Add(btnTest);

        mainDirStack.Children.Add(dirGrid);
        directoryStack.Children.Add(mainDirStack);

        directorySection.Child = directoryStack;
        stack.Children.Add(directorySection);

        // SECCIÓN: CONFIGURACIÓN DE ARCHIVOS DE LOG
        var filesSection = new Border
        {
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 17, 19)),
            BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(20),
            Margin = new Thickness(0, 5, 0, 5)
        };

        var filesStack = new StackPanel { Spacing = 15 };
        
        filesStack.Children.Add(new TextBlock
        {
            Text = "💾 Configuración de Archivos de Log",
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129))
        });

        // Checkbox principal para archivos
        _chkLogToFile = new CheckBox
        {
            Content = "Guardar logs en archivos locales",
            IsChecked = true,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 237, 239, 242)),
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        filesStack.Children.Add(_chkLogToFile);

        // Grid de tipos de archivos
        var fileTypesGrid = new Grid { ColumnSpacing = 25, RowSpacing = 15, Margin = new Thickness(24, 10, 0, 0) };
        fileTypesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        fileTypesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        
        fileTypesGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        fileTypesGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        fileTypesGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Archivo principal de aplicación
        var appLogStack = new StackPanel { Spacing = 6 };
        appLogStack.Children.Add(new CheckBox
        {
            Content = "📝 Log Principal de Aplicación",
            IsChecked = true,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 237, 239, 242)),
            FontSize = 13,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        });
        appLogStack.Children.Add(new TextBlock
        {
            Text = "📄 Archivo: gestiontime_YYYYMMDD.log",
            FontSize = 11,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 168, 176, 184)),
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas")
        });
        Grid.SetRow(appLogStack, 0);
        Grid.SetColumn(appLogStack, 0);
        fileTypesGrid.Children.Add(appLogStack);

        // Archivo de errores
        var errorLogStack = new StackPanel { Spacing = 6 };
        _chkLogErrors = new CheckBox
        {
            Content = "❌ Log de Errores Separado",
            IsChecked = true,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 248, 113, 113)),
            FontSize = 13,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        errorLogStack.Children.Add(_chkLogErrors);
        errorLogStack.Children.Add(new TextBlock
        {
            Text = "📄 Archivo: errors_YYYYMMDD.log",
            FontSize = 11,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 168, 176, 184)),
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas")
        });
        Grid.SetRow(errorLogStack, 0);
        Grid.SetColumn(errorLogStack, 1);
        fileTypesGrid.Children.Add(errorLogStack);

        // Archivo de logs HTTP
        var httpLogStack = new StackPanel { Spacing = 6 };
        _chkLogHttp = new CheckBox
        {
            Content = "🌐 Log de HTTP/API Separado",
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 59, 130, 246)),
            FontSize = 13,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        httpLogStack.Children.Add(_chkLogHttp);
        httpLogStack.Children.Add(new TextBlock
        {
            Text = "📄 Archivo: http_YYYYMMDD.log",
            FontSize = 11,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 168, 176, 184)),
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas")
        });
        Grid.SetRow(httpLogStack, 1);
        Grid.SetColumn(httpLogStack, 0);
        fileTypesGrid.Children.Add(httpLogStack);

        // Archivo de debug/performance
        var debugLogStack = new StackPanel { Spacing = 6 };
        _chkLogDebug = new CheckBox
        {
            Content = "🐛 Log de Debug/Performance",
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 168, 85, 247)),
            FontSize = 13,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        debugLogStack.Children.Add(_chkLogDebug);
        debugLogStack.Children.Add(new TextBlock
        {
            Text = "📄 Archivo: debug_YYYYMMDD.log",
            FontSize = 11,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 168, 176, 184)),
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas")
        });
        Grid.SetRow(debugLogStack, 1);
        Grid.SetColumn(debugLogStack, 1);
        fileTypesGrid.Children.Add(debugLogStack);

        // Configuración de rotación
        var rotationStack = new StackPanel { Spacing = 6 };
        rotationStack.Children.Add(new TextBlock
        {
            Text = "🔄 Configuración de Rotación:",
            FontSize = 13,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 251, 191, 36))
        });

        var rotationGrid = new Grid { ColumnSpacing = 15 };
        rotationGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        rotationGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var cmbRotation = new ComboBox
        {
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 26, 29, 33)),
            BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 48, 54, 61)),
            BorderThickness = new Thickness(1),
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 237, 239, 242)),
            CornerRadius = new CornerRadius(4),
            SelectedIndex = 0,
            FontSize = 12,
            MinWidth = 160
        };
        cmbRotation.Items.Add(new ComboBoxItem { Content = "📅 Diaria" });
        cmbRotation.Items.Add(new ComboBoxItem { Content = "📊 Por tamaño (10MB)" });
        cmbRotation.Items.Add(new ComboBoxItem { Content = "📆 Semanal" });
        cmbRotation.Items.Add(new ComboBoxItem { Content = "🗓️ Mensual" });
        _cmbLogRotation = cmbRotation; // Asignar referencia
        Grid.SetColumn(cmbRotation, 0);
        rotationGrid.Children.Add(cmbRotation);

        var txtRetention = new TextBox
        {
            Text = "30",
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 26, 29, 33)),
            BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 48, 54, 61)),
            BorderThickness = new Thickness(1),
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 237, 239, 242)),
            CornerRadius = new CornerRadius(4),
            Width = 60,
            FontSize = 12,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        _txtLogRetention = txtRetention; // Asignar referencia
        Grid.SetColumn(txtRetention, 1);
        rotationGrid.Children.Add(txtRetention);

        rotationGrid.Children.Add(new TextBlock
        {
            Text = "días de retención",
            FontSize = 12,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 168, 176, 184)),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0)
        });

        rotationStack.Children.Add(rotationGrid);
        Grid.SetRow(rotationStack, 2);
        Grid.SetColumnSpan(rotationStack, 2);
        fileTypesGrid.Children.Add(rotationStack);

        filesStack.Children.Add(fileTypesGrid);
        filesSection.Child = filesStack;
        stack.Children.Add(filesSection);

        // Información adicional mejorada con detalles de appsettings.json
        var infoSection = new Border
        {
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 17, 19)),
            BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 251, 191, 36)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(15),
            Margin = new Thickness(0, 10, 0, 0)
        };

        var infoStack = new StackPanel { Spacing = 8 };
        infoStack.Children.Add(new TextBlock
        {
            Text = "ℹ️ Información del Sistema de Logs",
            FontSize = 13,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 251, 191, 36))
        });

        infoStack.Children.Add(new TextBlock
        {
            Text = "🔧 CONFIGURACIÓN INTEGRADA:\n" +
                   "• La configuración base se carga desde appsettings.json\n" +
                   "• Las modificaciones del usuario sobrescriben los valores por defecto\n" +
                   "• Al guardar, se actualiza tanto la configuración local como appsettings.json\n\n" +
                   
                   "📁 ARCHIVOS GENERADOS:\n" +
                   "• gestiontime_YYYYMMDD.log - Log principal de la aplicación\n" +
                   "• errors_YYYYMMDD.log - Errores críticos y excepciones\n" +
                   "• http_YYYYMMDD.log - Llamadas HTTP/API completas\n" +
                   "• debug_YYYYMMDD.log - Debug y métricas de rendimiento\n\n" +
                   
                   "⚙️ ROTACIÓN AUTOMÁTICA:\n" +
                   "• Los archivos se crean diariamente con fecha en el nombre\n" +
                   "• Auto-limpieza basada en la configuración de retención\n" +
                   "• Cada nivel incluye todos los niveles superiores",
            FontSize = 12,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 168, 176, 184)),
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 18
        });

        infoSection.Child = infoStack;
        stack.Children.Add(infoSection);

        section.Child = stack;
        parent.Children.Add(section);
    }

    private void CreateAppSection(StackPanel parent)
    {
        var section = new Border
        {
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 26, 29, 33)),
            BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 48, 54, 61)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(25),
            Margin = new Thickness(0, 0, 0, 15)
        };

        var stack = new StackPanel { Spacing = 20 };

        stack.Children.Add(new TextBlock
        {
            Text = "⚙️ Configuración de Aplicación",
            FontSize = 18,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 237, 239, 242))
        });

        // Tema y Auto-refresh
        var configGrid = new Grid { ColumnSpacing = 25 };
        configGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        configGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Tema
        var themeStack = new StackPanel { Spacing = 8 };
        themeStack.Children.Add(new TextBlock
        {
            Text = "Tema de la aplicación",
            FontSize = 14,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 168, 176, 184))
        });
        _cmbTheme = new ComboBox
        {
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 17, 19)),
            BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 48, 54, 61)),
            BorderThickness = new Thickness(1),
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 237, 239, 242)),
            CornerRadius = new CornerRadius(6),
            MinWidth = 200,
            SelectedIndex = 0
        };
        _cmbTheme.Items.Add(new ComboBoxItem { Content = "Automático (según sistema)" });
        _cmbTheme.Items.Add(new ComboBoxItem { Content = "Claro" });
        _cmbTheme.Items.Add(new ComboBoxItem { Content = "Oscuro" });
        themeStack.Children.Add(_cmbTheme);
        Grid.SetColumn(themeStack, 0);
        configGrid.Children.Add(themeStack);

        // Auto-refresh
        var refreshStack = new StackPanel { Spacing = 8 };
        refreshStack.Children.Add(new TextBlock
        {
            Text = "Auto-actualización (segundos)",
            FontSize = 14,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 168, 176, 184))
        });
        _numAutoRefresh = new NumberBox
        {
            Minimum = 10,
            Maximum = 300,
            Value = 30,
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 17, 19)),
            BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 48, 54, 61)),
            BorderThickness = new Thickness(1),
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 237, 239, 242)),
            CornerRadius = new CornerRadius(6)
        };
        refreshStack.Children.Add(_numAutoRefresh);
        Grid.SetColumn(refreshStack, 1);
        configGrid.Children.Add(refreshStack);

        stack.Children.Add(configGrid);

        // Opciones de inicio
        var startupStack = new StackPanel { Spacing = 12 };
        startupStack.Children.Add(new TextBlock
        {
            Text = "Opciones de inicio de aplicación",
            FontSize = 14,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 168, 176, 184))
        });

        var startupGrid = new Grid { ColumnSpacing = 25 };
        startupGrid.ColumnDefinitions.Add(new ColumnDefinition());
        startupGrid.ColumnDefinitions.Add(new ColumnDefinition());
        startupGrid.ColumnDefinitions.Add(new ColumnDefinition());

        _chkAutoLogin = new CheckBox
        {
            Content = "Auto-login al iniciar",
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 237, 239, 242)),
            FontSize = 14
        };
        Grid.SetColumn(_chkAutoLogin, 0);
        startupGrid.Children.Add(_chkAutoLogin);

        _chkStartMinimized = new CheckBox
        {
            Content = "Iniciar minimizado",
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 237, 239, 242)),
            FontSize = 14
        };
        Grid.SetColumn(_chkStartMinimized, 1);
        startupGrid.Children.Add(_chkStartMinimized);

        _chkDebugMode = new CheckBox
        {
            Content = "Modo debug",
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 237, 239, 242)),
            FontSize = 14
        };
        Grid.SetColumn(_chkDebugMode, 2);
        startupGrid.Children.Add(_chkDebugMode);

        startupStack.Children.Add(startupGrid);
        stack.Children.Add(startupStack);

        section.Child = stack;
        parent.Children.Add(section);
    }

    private void CreateBottomButtons()
    {
        var bottomGrid = new Grid { Margin = new Thickness(0, 10, 0, 0) };
        bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetRow(bottomGrid, 2);
        _rootGrid.Children.Add(bottomGrid);

        // Info de configuración
        _txtConfigStatus = new TextBlock
        {
            Text = "Configuración cargada correctamente",
            FontSize = 12,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 168, 176, 184)),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(_txtConfigStatus, 0);
        bottomGrid.Children.Add(_txtConfigStatus);

        // Botones de acción
        var buttonsStack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 15
        };

        _btnSave = new Button
        {
            Content = "💾 Guardar",
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 11, 140, 153)),
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(25, 12, 25, 12),
            MinWidth = 150,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        _btnSave.Click += OnSaveClick;
        buttonsStack.Children.Add(_btnSave);

        var btnValidate = new Button
        {
            Content = "✅ Validar",
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)),
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(20, 12, 20, 12),
            MinWidth = 120,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        btnValidate.Click += (s, e) => ValidateConfiguration();
        buttonsStack.Children.Add(btnValidate);

        var btnClose = new Button
        {
            Content = "❌ Cerrar",
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0)),
            BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 48, 54, 61)),
            BorderThickness = new Thickness(1),
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 168, 176, 184)),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(20, 12, 20, 12),
            MinWidth = 100
        };
        btnClose.Click += OnCloseClick;
        buttonsStack.Children.Add(btnClose);

        Grid.SetColumn(buttonsStack, 1);
        bottomGrid.Children.Add(buttonsStack);
    }

    private void AnimateEntry()
    {
        var fadeIn = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = new Duration(TimeSpan.FromMilliseconds(500)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        Storyboard.SetTarget(fadeIn, _rootGrid);
        Storyboard.SetTargetProperty(fadeIn, "Opacity");

        var storyboard = new Storyboard();
        storyboard.Children.Add(fadeIn);
        storyboard.Begin();
    }

    private void ConfigureWindow()
    {
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(_window);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

        if (appWindow != null)
        {
            var displayArea = Microsoft.UI.Windowing.DisplayArea.Primary;
            var workArea = displayArea.WorkArea;

            // Tamaño completo como DiarioPage
            int width = 1600;
            int height = 900;
            int x = workArea.X + (workArea.Width - width) / 2;
            int y = workArea.Y + (workArea.Height - height) / 2;

            appWindow.MoveAndResize(new Windows.Graphics.RectInt32(x, y, width, height));

            if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
            {
                presenter.IsResizable = true;
                presenter.IsMaximizable = true;
            }
        }
    }

    private async void LoadConfigurationAsync()
    {
        try
        {
            var config = App.ConfiguracionService.Configuracion;
            var userRole = GetCurrentUserRole();

            // Mostrar rol en el banner
            _txtUserRole.Text = userRole;

            // CONEXIÓN
            _txtApiUrl.Text = config.ApiUrl;
            _numTimeout.Value = config.TimeoutSeconds;
            _numRetries.Value = config.MaxRetries;
            _chkIgnoreSSL.IsChecked = config.IgnoreSSL;

            // Deshabilitar para usuarios normales
            if (userRole == "Usuario")
            {
                foreach (var child in ((StackPanel)_connectionSection.Child).Children)
                {
                    if (child is Control control)
                    {
                        control.IsEnabled = false;
                    }
                }
                _connectionSection.Opacity = 0.5;
            }

            // LOGS
            _chkEnableLogging.IsChecked = config.EnableLogging;
            _cmbLogLevel.SelectedIndex = (int)config.LogLevel;
            _chkLogToFile.IsChecked = config.LogToFile;
            _chkLogHttp.IsChecked = config.LogHttp;
            _chkLogErrors.IsChecked = config.LogErrors;
            _chkLogDebug.IsChecked = config.LogDebug;
            _txtLogDirectory.Text = config.LogPath;
            _cmbLogRotation.SelectedIndex = (int)config.LogRotation;
            _txtLogRetention.Text = config.LogRetentionDays.ToString();

            // APLICACIÓN
            _cmbTheme.SelectedIndex = (int)config.Theme;
            _numAutoRefresh.Value = config.AutoRefreshSeconds;
            _chkAutoLogin.IsChecked = config.AutoLogin;
            _chkStartMinimized.IsChecked = config.StartMinimized;
            _chkDebugMode.IsChecked = config.DebugMode;

            // Debug solo para admin/técnico
            if (userRole == "Usuario")
            {
                _chkDebugMode.Visibility = Visibility.Collapsed;
            }

            _txtConfigStatus.Text = $"Configuración cargada - Rol: {userRole}";
            App.Log?.Log(LogLevel.Information, "✅ Configuración cargada en ventana independiente");
        }
        catch (Exception ex)
        {
            _txtConfigStatus.Text = "Error cargando configuración";
            App.Log?.Log(LogLevel.Error, ex, "Error cargando configuración");
        }
    }

    private async void OnTestConnectionClick(object sender, RoutedEventArgs e)
    {
        _btnTestConnection.IsEnabled = false;
        var originalContent = _btnTestConnection.Content;
        _btnTestConnection.Content = "🔄 Probando...";

        try
        {
            var apiUrl = _txtApiUrl.Text?.Trim();
            if (string.IsNullOrEmpty(apiUrl))
            {
                _btnTestConnection.Content = "❌ URL vacía";
                _txtConfigStatus.Text = "URL del API está vacía";
                await Task.Delay(2000);
                return;
            }

            using var client = new System.Net.Http.HttpClient();
            client.Timeout = TimeSpan.FromSeconds((int)_numTimeout.Value);

            var response = await client.GetAsync($"{apiUrl}/health");

            if (response.IsSuccessStatusCode)
            {
                _btnTestConnection.Content = "✅ Conexión OK";
                _txtConfigStatus.Text = "Conexión establecida correctamente";
            }
            else
            {
                _btnTestConnection.Content = $"⚠️ Error {response.StatusCode}";
                _txtConfigStatus.Text = $"Error en la conexión: {response.StatusCode}";
            }

            await Task.Delay(3000);
        }
        catch (Exception ex)
        {
            _btnTestConnection.Content = "❌ Sin conexión";
            _txtConfigStatus.Text = $"No se pudo conectar: {ex.Message}";
            await Task.Delay(3000);
        }
        finally
        {
            _btnTestConnection.Content = originalContent;
            _btnTestConnection.IsEnabled = true;
        }
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        try
        {
            _btnSave.IsEnabled = false;
            _btnSave.Content = "💾 Guardando...";
            _txtConfigStatus.Text = "Guardando configuración...";

            var config = App.ConfiguracionService.Configuracion;
            var userRole = GetCurrentUserRole();

            // Aplicar configuración de conexión (solo si es admin/técnico)
            if (userRole != "Usuario")
            {
                config.ApiUrl = _txtApiUrl.Text ?? "";
                config.TimeoutSeconds = (int)_numTimeout.Value;
                config.MaxRetries = (int)_numRetries.Value;
                config.IgnoreSSL = _chkIgnoreSSL.IsChecked ?? false;
            }

            // Aplicar configuración de logs
            config.EnableLogging = _chkEnableLogging.IsChecked ?? true;
            config.LogLevel = (Models.LogLevel)_cmbLogLevel.SelectedIndex;
            config.LogToFile = _chkLogToFile.IsChecked ?? true;
            config.LogHttp = _chkLogHttp.IsChecked ?? false;
            config.LogErrors = _chkLogErrors.IsChecked ?? true;
            config.LogDebug = _chkLogDebug.IsChecked ?? false;
            config.LogPath = _txtLogDirectory.Text ?? @"C:\Logs\GestionTime";
            config.LogRotation = (Models.LogRotation)_cmbLogRotation.SelectedIndex;
            
            if (int.TryParse(_txtLogRetention.Text, out var retentionDays))
            {
                config.LogRetentionDays = retentionDays;
            }

            // Aplicar configuración de aplicación
            config.Theme = (Models.AppTheme)_cmbTheme.SelectedIndex;
            config.AutoRefreshSeconds = (int)_numAutoRefresh.Value;
            config.AutoLogin = _chkAutoLogin.IsChecked ?? false;
            config.StartMinimized = _chkStartMinimized.IsChecked ?? false;

            if (userRole != "Usuario")
            {
                config.DebugMode = _chkDebugMode.IsChecked ?? false;
            }

            await App.ConfiguracionService.SaveConfigurationAsync();

            // 🆕 Aplicar tema GLOBALMENTE usando ThemeService
            var theme = _cmbTheme.SelectedIndex switch
            {
                0 => ElementTheme.Default,
                1 => ElementTheme.Light,
                2 => ElementTheme.Dark,
                _ => ElementTheme.Default
            };
            
            // 🔥 CLAVE: Usar ThemeService para que el cambio se propague a TODAS las ventanas
            Services.ThemeService.Instance.SetTheme(theme);

            _btnSave.Content = "✅ Guardado";
            _txtConfigStatus.Text = "Configuración guardada correctamente";

            App.Log?.Log(LogLevel.Information, "✅ Configuración guardada desde ventana independiente");

            // Debug: Verificar que la configuración se guardó correctamente
            await VerifyConfigurationSavedAsync();

            await Task.Delay(1500);
            _btnSave.Content = "💾 Guardar";
        }
        catch (Exception ex)
        {
            _btnSave.Content = "❌ Error";
            _txtConfigStatus.Text = $"Error guardando: {ex.Message}";
            App.Log?.Log(LogLevel.Error, ex, "Error guardando configuración");
            await Task.Delay(1500);
            _btnSave.Content = "💾 Guardar";
        }
        finally
        {
            _btnSave.IsEnabled = true;
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        _window?.Close();
    }

    private string GetCurrentUserRole()
    {
        try
        {
            var settings = Windows.Storage.ApplicationData.Current.LocalSettings.Values;
            return settings.TryGetValue("UserRole", out var roleObj) && roleObj is string role
                ? role
                : "Usuario";
        }
        catch
        {
            return "Usuario";
        }
    }

    private async void OnBrowseLogDirectoryClick(TextBox textBox)
    {
        try
        {
            // Crear picker de carpetas
            var folderPicker = new Windows.Storage.Pickers.FolderPicker
            {
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary,
                ViewMode = Windows.Storage.Pickers.PickerViewMode.List
            };
            folderPicker.FileTypeFilter.Add("*");

            // Obtener ventana para el picker
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(_window);
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hWnd);

            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                try
                {
                    // Probar crear el directorio y verificar permisos
                    await App.ConfiguracionService.CreateLogDirectoryAsync(folder.Path);
                    
                    textBox.Text = folder.Path;
                    _txtConfigStatus.Text = $"✅ Directorio de logs configurado: {folder.Path}";
                    App.Log?.Log(LogLevel.Information, "📁 Directorio de logs seleccionado y verificado: {path}", folder.Path);
                }
                catch (Exception ex)
                {
                    _txtConfigStatus.Text = $"❌ Error configurando directorio: {ex.Message}";
                    App.Log?.Log(LogLevel.Error, ex, "Error configurando directorio de logs");
                    
                    // Mostrar diálogo de error
                    var errorDialog = new ContentDialog
                    {
                        Title = "Error de Permisos",
                        Content = $"No se pudo configurar el directorio seleccionado:\n\n{ex.Message}\n\n¿Deseas elegir otro directorio?",
                        PrimaryButtonText = "Elegir otro",
                        CloseButtonText = "Cancelar",
                        XamlRoot = _window.Content.XamlRoot,
                        RequestedTheme = ElementTheme.Dark
                    };
                    
                    var result = await errorDialog.ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        // Recursivo: volver a abrir el selector
                        OnBrowseLogDirectoryClick(textBox);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _txtConfigStatus.Text = "Error seleccionando directorio de logs";
            App.Log?.Log(LogLevel.Error, ex, "Error en selector de carpeta de logs");
        }
    }

    private async void OnOpenLogDirectoryClick(TextBox textBox)
    {
        try
        {
            var logPath = textBox.Text?.Trim();
            if (string.IsNullOrEmpty(logPath))
            {
                logPath = @"C:\Logs\GestionTime";
                textBox.Text = logPath;
            }

            // Crear directorio si no existe y verificar permisos
            try
            {
                await App.ConfiguracionService.CreateLogDirectoryAsync(logPath);
            }
            catch (Exception ex)
            {
                _txtConfigStatus.Text = $"⚠️ Advertencia: {ex.Message}";
                // Intentar crear directorio básico sin verificación
                System.IO.Directory.CreateDirectory(logPath);
            }

            // Abrir explorador de archivos
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{logPath}\"", // Agregar comillas para rutas con espacios
                UseShellExecute = true
            });

            _txtConfigStatus.Text = $"📁 Directorio abierto: {logPath}";
            App.Log?.Log(LogLevel.Information, "📁 Directorio de logs abierto: {path}", logPath);
        }
        catch (Exception ex)
        {
            _txtConfigStatus.Text = "❌ Error abriendo directorio de logs";
            App.Log?.Log(LogLevel.Error, ex, "Error abriendo directorio de logs");
            
            // Mostrar mensaje de error al usuario
            var errorDialog = new ContentDialog
            {
                Title = "Error",
                Content = $"No se pudo abrir el directorio de logs:\n\n{ex.Message}",
                CloseButtonText = "OK",
                XamlRoot = _window.Content.XamlRoot,
                RequestedTheme = ElementTheme.Dark
            };
            await errorDialog.ShowAsync();
        }
    }

    private async Task VerifyConfigurationSavedAsync()
    {
        try
        {
            // Verificar que el directorio existe
            var logPath = _txtLogDirectory.Text?.Trim();
            if (!string.IsNullOrEmpty(logPath))
            {
                _txtConfigStatus.Text = $"🔍 Verificando directorio: {logPath}";
                
                if (System.IO.Directory.Exists(logPath))
                {
                    _txtConfigStatus.Text = $"✅ Directorio existe: {logPath}";
                    
                    // Crear un archivo de prueba para confirmar que se puede escribir
                    var testFileName = CreateUniqueLogFileName("config_verification", logPath);
                    var testFile = System.IO.Path.Combine(logPath, testFileName);
                    var testContent = $"=== VERIFICACIÓN DE CONFIGURACIÓN ===\n" +
                                    $"Fecha: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                                    $"Directorio: {logPath}\n" +
                                    $"Usuario: {Environment.UserName}\n" +
                                    $"Máquina: {Environment.MachineName}\n" +
                                    $"Configurado desde: Ventana de Configuración\n" +
                                    $"Estado: VERIFICACIÓN EXITOSA ✅\n" +
                                    $"\n" +
                                    $"CONFIGURACIÓN APLICADA:\n" +
                                    $"• Nivel de logs: {App.ConfiguracionService.Configuracion.LogLevel}\n" +
                                    $"• Log a archivo: {App.ConfiguracionService.Configuracion.LogToFile}\n" +
                                    $"• Log HTTP: {App.ConfiguracionService.Configuracion.LogHttp}\n" +
                                    $"• Log errores: {App.ConfiguracionService.Configuracion.LogErrors}\n" +
                                    $"• Log debug: {App.ConfiguracionService.Configuracion.LogDebug}\n" +
                                    $"• Rotación: {App.ConfiguracionService.Configuracion.LogRotation}\n" +
                                    $"• Retención (días): {App.ConfiguracionService.Configuracion.LogRetentionDays}\n" +
                                    $"======================================\n";
                    
                    _txtConfigStatus.Text = $"📝 Creando archivo de prueba: {System.IO.Path.GetFileName(testFile)}";
                    
                    try
                    {
                        await System.IO.File.WriteAllTextAsync(testFile, testContent);
                        
                        // Verificar inmediatamente que existe
                        if (System.IO.File.Exists(testFile))
                        {
                            var fileInfo = new System.IO.FileInfo(testFile);
                            _txtConfigStatus.Text = $"✅ Archivo creado exitosamente: {fileInfo.Name} ({fileInfo.Length} bytes)";
                            App.Log?.Log(LogLevel.Information, "✅ Archivo de prueba creado: {file} ({size} bytes)", testFile, fileInfo.Length);
                            
                            // Mostrar diálogo de confirmación
                            var confirmDialog = new ContentDialog
                            {
                                Title = "✅ Configuración Verificada",
                                Content = $"✅ CONFIGURACIÓN GUARDADA EXITOSAMENTE\n\n" +
                                         $"📁 Directorio configurado: {logPath}\n" +
                                         $"📝 Archivo de prueba: {fileInfo.Name}\n" +
                                         $"💾 Tamaño: {fileInfo.Length} bytes\n" +
                                         $"🕒 Creado: {fileInfo.CreationTime:HH:mm:ss}\n\n" +
                                         $"NOTA: El archivo permanecerá en el directorio para tu referencia.",
                                PrimaryButtonText = "🔍 Ver archivo",
                                SecondaryButtonText = "🗑️ Eliminar archivo",
                                CloseButtonText = "OK",
                                XamlRoot = _window.Content.XamlRoot,
                                RequestedTheme = ElementTheme.Dark
                            };
                            
                            var result = await confirmDialog.ShowAsync();
                            if (result == ContentDialogResult.Primary)
                            {
                                // Abrir el archivo con el editor predeterminado
                                try
                                {
                                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                    {
                                        FileName = testFile,
                                        UseShellExecute = true
                                    });
                                }
                                catch
                                {
                                    // Si no puede abrir el archivo, abrir la carpeta
                                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                    {
                                        FileName = "explorer.exe",
                                        Arguments = $"/select,\"{testFile}\"",
                                        UseShellExecute = true
                                    });
                                }
                            }
                            else if (result == ContentDialogResult.Secondary)
                            {
                                // Solo eliminar si el usuario lo solicita explícitamente
                                try
                                {
                                    if (System.IO.File.Exists(testFile))
                                    {
                                        System.IO.File.Delete(testFile);
                                        App.Log?.Log(LogLevel.Information, "🗑️ Archivo de prueba eliminado por solicitud del usuario: {file}", testFile);
                                    }
                                }
                                catch (Exception deleteEx)
                                {
                                    App.Log?.Log(LogLevel.Warning, deleteEx, "⚠️ No se pudo eliminar archivo de prueba: {file}", testFile);
                                }
                            }
                        }
                        else
                        {
                            _txtConfigStatus.Text = $"❌ Error: archivo no se creó correctamente";
                            App.Log?.Log(LogLevel.Error, "❌ Archivo de prueba no existe después de crearlo: {file}", testFile);
                        }
                    }
                    catch (UnauthorizedAccessException uaEx)
                    {
                        _txtConfigStatus.Text = $"🚫 Sin permisos de escritura: {uaEx.Message}";
                        App.Log?.Log(LogLevel.Error, uaEx, "🚫 Error de permisos escribiendo archivo de prueba");
                        
                        // Mostrar diálogo de error de permisos
                        var permDialog = new ContentDialog
                        {
                            Title = "🚫 Error de Permisos",
                            Content = $"No se pueden crear archivos en el directorio seleccionado:\n\n" +
                                     $"📁 Directorio: {logPath}\n" +
                                     $"❌ Error: {uaEx.Message}\n\n" +
                                     $"💡 Sugerencias:\n" +
                                     $"• Ejecutar la aplicación como administrador\n" +
                                     $"• Elegir un directorio en tu carpeta de usuario\n" +
                                     $"• Verificar permisos de la carpeta",
                            CloseButtonText = "OK",
                            XamlRoot = _window.Content.XamlRoot,
                            RequestedTheme = ElementTheme.Dark
                        };
                        await permDialog.ShowAsync();
                    }
                    catch (Exception ex)
                    {
                        _txtConfigStatus.Text = $"❌ Error escribiendo archivo: {ex.Message}";
                        App.Log?.Log(LogLevel.Error, ex, "❌ Error general escribiendo archivo de prueba");
                        
                        // Mostrar diálogo de error general
                        var errorDialog = new ContentDialog
                        {
                            Title = "❌ Error de Escritura",
                            Content = $"Error creando archivo de prueba:\n\n{ex.Message}",
                            CloseButtonText = "OK",
                            XamlRoot = _window.Content.XamlRoot,
                            RequestedTheme = ElementTheme.Dark
                        };
                        await errorDialog.ShowAsync();
                    }
                }
                else
                {
                    _txtConfigStatus.Text = $"❌ Directorio no existe: {logPath}";
                    App.Log?.Log(LogLevel.Error, "❌ Directorio de logs no existe: {path}", logPath);
                    
                    // Intentar crear el directorio
                    try
                    {
                        System.IO.Directory.CreateDirectory(logPath);
                        _txtConfigStatus.Text = $"✅ Directorio creado: {logPath}";
                        
                        // Reintentar la verificación
                        await VerifyConfigurationSavedAsync();
                    }
                    catch (Exception createEx)
                    {
                        _txtConfigStatus.Text = $"❌ No se pudo crear directorio: {createEx.Message}";
                        App.Log?.Log(LogLevel.Error, createEx, "❌ Error creando directorio de logs");
                    }
                }
            }
            else
            {
                _txtConfigStatus.Text = "⚠️ Directorio de logs vacío durante verificación";
                App.Log?.Log(LogLevel.Warning, "⚠️ Directorio de logs vacío durante verificación");
            }
        }
        catch (Exception ex)
        {
            _txtConfigStatus.Text = $"❌ Error en verificación: {ex.Message}";
            App.Log?.Log(LogLevel.Error, ex, "❌ Error general verificando configuración guardada");
        }
    }

    private async void OnDebugConfigClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var config = App.ConfiguracionService.Configuracion;
            
            var debugInfo = $"🔧 DEBUG DE CONFIGURACIÓN\n\n" +
                           $"📁 DIRECTORIOS:\n" +
                           $"• Directorio de logs: {config.LogPath}\n" +
                           $"• Directorio de app: {System.AppDomain.CurrentDomain.BaseDirectory}\n" +
                           $"• appsettings.json existe: {System.IO.File.Exists(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "appsettings.json"))}\n\n" +
                           
                           $"⚙️ CONFIGURACIÓN ACTUAL:\n" +
                           $"• API URL: {config.ApiUrl}\n" +
                           $"• Logging habilitado: {config.EnableLogging}\n" +
                           $"• Nivel de log: {config.LogLevel}\n" +
                           $"• Log HTTP: {config.LogHttp}\n" +
                           $"• Log errores: {config.LogErrors}\n" +
                           $"• Log debug: {config.LogDebug}\n" +
                           $"• Rotación: {config.LogRotation}\n" +
                           $"• Retención (días): {config.LogRetentionDays}\n\n" +
                           
                           $"🔧 ESTADO DEL SISTEMA:\n" +
                           $"• Tema: {config.Theme}\n" +
                           $"• Auto-refresh: {config.AutoRefreshSeconds}s\n" +
                           $"• Timeout: {config.TimeoutSeconds}s\n" +
                           $"• Reintentos: {config.MaxRetries}\n" +
                           $"• Ignorar SSL: {config.IgnoreSSL}";

            var debugDialog = new ContentDialog
            {
                Title = "🔧 Debug de Configuración",
                Content = new ScrollViewer
                {
                    Content = new TextBlock 
                    { 
                        Text = debugInfo, 
                        FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                        FontSize = 12,
                        IsTextSelectionEnabled = true,
                        TextWrapping = TextWrapping.Wrap
                    },
                    MaxHeight = 500
                },
                PrimaryButtonText = "📋 Copiar",
                CloseButtonText = "Cerrar",
                XamlRoot = _window.Content.XamlRoot,
                RequestedTheme = ElementTheme.Dark
            };

            var result = await debugDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                // Copiar al portapapeles
                var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
                dataPackage.SetText(debugInfo);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
                
                _txtConfigStatus.Text = "📋 Información de debug copiada al portapapeles";
            }
            
            App.Log?.Log(LogLevel.Information, "🔧 Debug de configuración solicitado");
        }
        catch (Exception ex)
        {
            _txtConfigStatus.Text = $"❌ Error en debug: {ex.Message}";
            App.Log?.Log(LogLevel.Error, ex, "Error en debug de configuración");
        }
    }

    private async void OnTestLogDirectoryClick(TextBox textBox)
    {
        try
        {
            var logPath = textBox.Text?.Trim();
            
            // Usar Fluent Assertions para validar entrada
            logPath.Should().NotBeNullOrEmpty("Especifica un directorio primero");
            
            if (!ValidateLogDirectory(logPath))
            {
                return;
            }

            _txtConfigStatus.Text = "🧪 Probando configuración de logs...";

            // Crear directorio si no existe
            if (!System.IO.Directory.Exists(logPath))
            {
                System.IO.Directory.CreateDirectory(logPath);
                _txtConfigStatus.Text = "📁 Directorio creado, continuando prueba...";
            }

            // Verificar que el directorio fue creado correctamente
            System.IO.Directory.Exists(logPath).Should().BeTrue($"El directorio debería existir después de crearlo: {logPath}");

            // Crear múltiples archivos de prueba simulando el sistema real
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var testFiles = new[]
            {
                (CreateUniqueLogFileName("gestiontime_test", logPath), "📝 LOG PRINCIPAL", "Evento de inicio de aplicación\nPrueba de sistema de logging\nOperación exitosa"),
                (CreateUniqueLogFileName("errors_test", logPath), "❌ LOG DE ERRORES", "Simulación de error controlado\nPrueba del sistema de errores\nTodo funcionando correctamente"),
                (CreateUniqueLogFileName("http_test", logPath), "🌐 LOG HTTP", "GET /api/v1/test HTTP/1.1\nHost: localhost:2501\nUser-Agent: GestionTime\n\nRespuesta: 200 OK\nTiempo: 150ms"),
                (CreateUniqueLogFileName("debug_test", logPath), "🐛 LOG DEBUG", "Inicio de sesión de depuración\nMemoria utilizada: 45MB\nOperaciones por segundo: 1200\nRendimiento: ÓPTIMO")
            };

            var createdFiles = new List<(string path, string name, long size)>();

            foreach (var (fileName, tipo, content) in testFiles)
            {
                try
                {
                    var filePath = System.IO.Path.Combine(logPath, fileName);
                    var fullContent = $"=== {tipo} ===\n" +
                                    $"Fecha: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                                    $"Aplicación: GestionTime Desktop\n" +
                                    $"Versión: 1.0.0\n" +
                                    $"Usuario: {Environment.UserName}\n" +
                                    $"Máquina: {Environment.MachineName}\n" +
                                    $"Directorio: {logPath}\n" +
                                    $"===========================\n\n" +
                                    $"{content}\n\n" +
                                    $"Prueba realizada desde ventana de configuración\n" +
                                    $"Estado: EXITOSO ✅\n";

                    await System.IO.File.WriteAllTextAsync(filePath, fullContent);
                    
                    // Usar Fluent Assertions para verificar que el archivo se creó
                    System.IO.File.Exists(filePath).Should().BeTrue($"El archivo {fileName} debería haberse creado");
                    
                    if (System.IO.File.Exists(filePath))
                    {
                        var fileInfo = new System.IO.FileInfo(filePath);
                        
                        // Validar propiedades del archivo
                        fileInfo.Length.Should().BeGreaterThan(0, "El archivo no debería estar vacío");
                        fileInfo.Name.Should().Be(fileName, "El nombre del archivo debería coincidir");
                        
                        createdFiles.Add((filePath, fileName, fileInfo.Length));
                    }
                }
                catch (Exception fileEx)
                {
                    _txtConfigStatus.Text = $"❌ Error creando {fileName}: {fileEx.Message}";
                    return;
                }
            }

            // Usar Fluent Assertions para verificar resultado
            createdFiles.Should().HaveCount(testFiles.Length, "Deberían haberse creado todos los archivos de prueba");
            createdFiles.Should().OnlyContain(f => f.size > 0, "Todos los archivos deberían tener contenido");

            _txtConfigStatus.Text = $"✅ Prueba exitosa: {createdFiles.Count} archivos creados";

            var testSummary = "🧪 PRUEBA DE CONFIGURACIÓN EXITOSA\n\n" +
                            $"📁 Directorio: {logPath}\n" +
                            $"📝 Archivos creados: {createdFiles.Count}\n\n";

            foreach (var file in createdFiles)
            {
                testSummary += $"• {file.name} - {file.size} bytes\n";
            }

            testSummary += $"\n💡 Los archivos permanecerán en el directorio para tu análisis.\n" +
                          "Puedes eliminarlos manualmente si lo deseas.";

            var testDialog = new ContentDialog
            {
                Title = "✅ Prueba Exitosa",
                Content = new ScrollViewer
                {
                    Content = new TextBlock
                    {
                        Text = testSummary,
                        FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                        FontSize = 12,
                        TextWrapping = TextWrapping.Wrap,
                        IsTextSelectionEnabled = true
                    },
                    MaxHeight = 400
                },
                PrimaryButtonText = "📂 Ver archivos",
                SecondaryButtonText = "🗑️ Eliminar ahora",
                CloseButtonText = "OK",
                XamlRoot = _window.Content.XamlRoot,
                RequestedTheme = ElementTheme.Dark
            };

            var result = await testDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // Abrir explorador con los archivos
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{logPath}\"",
                    UseShellExecute = true
                });
            }
            else if (result == ContentDialogResult.Secondary)
            {
                // Eliminar archivos inmediatamente
                foreach (var file in createdFiles)
                {
                    try
                    {
                        System.IO.File.Delete(file.path);
                    }
                    catch { /* Ignorar errores de eliminación */ }
                }
                _txtConfigStatus.Text = "🗑️ Archivos de prueba eliminados";
            }
            else
            {
                // *** ELIMINADO: No más eliminación automática ***
                // Los archivos permanecen para análisis del usuario
                _txtConfigStatus.Text = "📁 Archivos de prueba guardados permanentemente";
            }

            App.Log?.Log(LogLevel.Information, "🧪 Prueba de directorio exitosa: {path}, {count} archivos", logPath, createdFiles.Count);
        }
        catch (Exception ex)
        {
            _txtConfigStatus.Text = $"❌ Error en prueba: {ex.Message}";
            App.Log?.Log(LogLevel.Error, ex, "Error en prueba de directorio de logs");
        }
    }

    private void ValidateConfiguration()
    {
        try
        {
            var config = App.ConfiguracionService.Configuracion;
            
            // Validaciones usando Fluent Assertions
            config.Should().NotBeNull("La configuración no debería ser nula");
            config.LogPath.Should().NotBeNullOrEmpty("El directorio de logs debe estar especificado");
            config.ApiUrl.Should().NotBeNullOrEmpty("La URL del API debe estar especificada");
            config.TimeoutSeconds.Should().BeInRange(5, 300, "El timeout debe estar entre 5 y 300 segundos");
            config.MaxRetries.Should().BeInRange(0, 10, "Los reintentos deben estar entre 0 y 10");
            config.LogRetentionDays.Should().BePositive("Los días de retención deben ser positivos");
            
            // Validaciones específicas
            if (config.LogToFile)
            {
                System.IO.Directory.Exists(config.LogPath).Should().BeTrue($"El directorio de logs debe existir: {config.LogPath}");
            }
            
            if (!string.IsNullOrEmpty(config.ApiUrl))
            {
                config.ApiUrl.Should().StartWith("http", "La URL debe comenzar con http o https");
            }
            
            _txtConfigStatus.Text = "✅ Configuración validada correctamente";
            App.Log?.Log(LogLevel.Information, "✅ Configuración validada con Fluent Assertions");
        }
        catch (Exception ex)
        {
            _txtConfigStatus.Text = $"❌ Error en validación: {ex.Message}";
            App.Log?.Log(LogLevel.Error, ex, "Error validando configuración");
            
            // Mostrar diálogo de error de validación
            _ = Task.Run(async () =>
 {
                var errorDialog = new ContentDialog
                {
                    Title = "❌ Error de Validación",
                    Content = $"La configuración no es válida:\n\n{ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = _window.Content.XamlRoot,
                    RequestedTheme = ElementTheme.Dark
                };
                await errorDialog.ShowAsync();
            });
        }
    }

    private bool ValidateLogDirectory(string logPath)
    {
        try
        {
            // Usar Fluent Assertions para validaciones más expresivas
            logPath.Should().NotBeNullOrWhiteSpace("El directorio de logs no puede estar vacío");
            logPath.Should().NotContain("*", "El directorio no puede contener caracteres inválidos");
            logPath.Should().NotContain("?", "El directorio no puede contener caracteres inválidos");
            logPath.Should().NotContain("<", "El directorio no puede contener caracteres inválidos");
            logPath.Should().NotContain(">", "El directorio no puede contener caracteres inválidos");
            logPath.Should().NotContain("|", "El directorio no puede contener caracteres inválidos");
            
            // Validar longitud del path
            logPath.Length.Should().BeLessThanOrEqualTo(260, "La ruta es demasiado larga para Windows");
            
            // Verificar que no es una ruta de sistema crítica
            var systemPaths = new[] { @"C:\Windows", @"C:\System32", @"C:\Program Files" };
            systemPaths.Should().NotContain(p => logPath.StartsWith(p, StringComparison.OrdinalIgnoreCase), 
                "No se puede usar un directorio del sistema para logs");
            
            return true;
        }
        catch (Exception ex)
        {
            _txtConfigStatus.Text = $"❌ Directorio inválido: {ex.Message}";
            return false;
        }
    }

    private string CreateUniqueLogFileName(string baseName, string logPath)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var fileName = $"{baseName}_{timestamp}.log";
        var fullPath = System.IO.Path.Combine(logPath, fileName);
        
        // Si ya existe, agregar un número incremental
        int counter = 1;
        while (System.IO.File.Exists(fullPath))
        {
            fileName = $"{baseName}_{timestamp}_{counter:D2}.log";
            fullPath = System.IO.Path.Combine(logPath, fileName);
            counter++;
        }
        
        return fileName;
    }
}