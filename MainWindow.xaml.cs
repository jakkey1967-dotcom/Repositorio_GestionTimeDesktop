using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.Logging;
using System;
using GestionTime.Desktop.Services;

namespace GestionTime.Desktop;

public sealed partial class MainWindow : Window
{
    public Frame Navigator => RootFrame;
    
    // 🆕 NUEVO: Rastrea el tipo de página actual para guardado correcto
    private Type? _currentPageType;

    public MainWindow()
    {
        InitializeComponent();
        
        Title = "GestionTime Desktop";

        App.Log?.LogInformation("MainWindow inicializada");

        // Escuchar cambios de navegación para ajustar tamaño automáticamente
        RootFrame.Navigated += OnFrameNavigated;
        
        // Cargar LoginPage al iniciar (el tamaño se ajustará automáticamente)
        RootFrame.Navigate(typeof(Views.LoginPage));
    }

    /// <summary>
    /// Se ejecuta cada vez que navegamos a una nueva página
    /// Ajusta automáticamente el tamaño de la ventana según la página
    /// </summary>
    private void OnFrameNavigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        if (e.SourcePageType != null)
        {
            // 🆕 NUEVO: Guardar el tipo de página actual
            _currentPageType = e.SourcePageType;
            
            App.Log?.LogInformation("📐 Navegando a {page}, ajustando tamaño de ventana...", e.SourcePageType.Name);
            
            // Pasar la página correcta al WindowSizeManager
            WindowSizeManager.SetSizeForPage(this, e.SourcePageType, _currentPageType);
        }
    }
    
    /// <summary>
    /// 🆕 NUEVO: Obtiene el tipo de página actualmente visible
    /// </summary>
    public Type? GetCurrentPageType()
    {
        return _currentPageType;
    }
}

