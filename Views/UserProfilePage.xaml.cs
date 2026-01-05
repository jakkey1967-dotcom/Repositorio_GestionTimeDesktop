using GestionTime.Desktop.Models.Dtos;
using GestionTime.Desktop.Services;
using GestionTime.Desktop.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Threading.Tasks;

namespace GestionTime.Desktop.Views;

/// <summary>Página completa para visualizar y editar el perfil del usuario actual.</summary>
public sealed partial class UserProfilePage : Page
{
    private readonly ProfileService _profileService;
    private UserProfileResponse? _originalProfile;
    private bool _hasChanges = false;

    public UserProfilePage()
    {
        InitializeComponent();

        // 🆕 MODIFICADO: Usar ProfileService singleton de App
        _profileService = App.ProfileService;

        ThemeService.Instance.ApplyTheme(this);
        ThemeService.Instance.ThemeChanged += OnGlobalThemeChanged;

        this.Unloaded += OnPageUnloaded;
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        ThemeService.Instance.ThemeChanged -= OnGlobalThemeChanged;
        App.Log?.LogInformation("UserProfilePage Unloaded - Recursos limpiados");
    }

    private void OnGlobalThemeChanged(object? sender, ElementTheme theme)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            this.RequestedTheme = theme;
            UpdateThemeAssets(theme);
            App.Log?.LogDebug("🎨 UserProfilePage: Tema actualizado por cambio global a {theme}", theme);
        });
    }

    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        this.Loaded -= OnPageLoaded;

        try
        {
            App.Log?.LogInformation("UserProfilePage Loaded ✅");

            UpdateThemeAssets(this.RequestedTheme);

            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(500)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            Storyboard.SetTarget(fadeIn, RootGrid);
            Storyboard.SetTargetProperty(fadeIn, "Opacity");

            var storyboard = new Storyboard();
            storyboard.Children.Add(fadeIn);
            storyboard.Begin();

            await LoadUserProfileAsync();
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error en OnPageLoaded() de UserProfilePage");
        }
    }

    private async Task LoadUserProfileAsync()
    {
        LoadingPanel.Visibility = Visibility.Visible;
        ContentScroll.Visibility = Visibility.Collapsed;
        
        try
        {
            App.Log?.LogInformation("📥 Cargando perfil del usuario...");

            var profile = await _profileService.GetCurrentUserProfileAsync();

            if (profile == null)
            {
                App.Log?.LogError("❌ ERROR: GetCurrentUserProfileAsync() devolvió null");
                
                LoadingPanel.Visibility = Visibility.Collapsed;
                ContentScroll.Visibility = Visibility.Collapsed;
                
                await ShowCriticalErrorAsync(
                    "❌ Error de Sincronización de Datos",
                    "Tu perfil de usuario no existe en el sistema.\n\n" +
                    "Esto es un problema de sincronización que debe resolver el administrador.\n\n" +
                    "Acciones recomendadas:\n" +
                    "1. Cerrar sesión\n" +
                    "2. Volver a iniciar sesión\n" +
                    "3. Si el problema persiste, contactar al administrador\n\n" +
                    "Código de error: PROFILE_NOT_FOUND");
                
                return; // ✅ IMPORTANTE: Salir inmediatamente
            }

            // ✅ Si llegamos aquí, el perfil existe
            App.Log?.LogInformation("✅ Perfil cargado: {firstName} {lastName}", profile.FirstName, profile.LastName);
            
            _originalProfile = profile;
            PopulateFields(profile);

            LoadingPanel.Visibility = Visibility.Collapsed;
            ContentScroll.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Excepción cargando perfil de usuario");
            
            LoadingPanel.Visibility = Visibility.Collapsed;
            ContentScroll.Visibility = Visibility.Collapsed;
            
            await ShowCriticalErrorAsync(
                "❌ Error al Cargar Perfil",
                $"No se pudo cargar tu perfil de usuario.\n\n" +
                $"Error: {ex.Message}\n\n" +
                $"Por favor, intenta cerrar sesión y volver a iniciar.");
        }
    }

    private void PopulateFields(UserProfileResponse profile)
    {
        TxtFirstName.Text = profile.FirstName ?? "";
        TxtLastName.Text = profile.LastName ?? "";
        TxtPhone.Text = profile.Phone ?? "";
        TxtMobile.Text = profile.Mobile ?? "";
        TxtAddress.Text = profile.Address ?? "";
        TxtCity.Text = profile.City ?? "";
        TxtPostalCode.Text = profile.PostalCode ?? "";
        TxtDepartment.Text = profile.Department ?? "";
        TxtPosition.Text = profile.Position ?? "";
        TxtAvatarUrl.Text = profile.AvatarUrl ?? "";
        TxtNotes.Text = profile.Notes ?? "";

        if (!string.IsNullOrEmpty(profile.EmployeeType))
        {
            for (int i = 0; i < CmbEmployeeType.Items.Count; i++)
            {
                if (((ComboBoxItem)CmbEmployeeType.Items[i]).Content?.ToString() == profile.EmployeeType)
                {
                    CmbEmployeeType.SelectedIndex = i;
                    break;
                }
            }
        }

        if (profile.HireDate.HasValue)
        {
            DpHireDate.Date = new DateTimeOffset(profile.HireDate.Value);
        }

        _hasChanges = false;
        BtnGuardar.IsEnabled = false;

        App.Log?.LogDebug("✅ Campos poblados con datos del perfil");
    }

    private void OnFieldChanged(object? sender, object e)
    {
        if (_originalProfile == null)
            return;

        _hasChanges = true;
        BtnGuardar.IsEnabled = true;

        App.Log?.LogDebug("🔧 Campo modificado, botón Guardar habilitado");
    }

    private async void OnGuardarClick(object sender, RoutedEventArgs e)
    {
        if (!_hasChanges || _originalProfile == null)
        {
            App.Log?.LogDebug("⚠️ No hay cambios para guardar");
            return;
        }

        try
        {
            App.Log?.LogInformation("💾 Guardando cambios del perfil...");

            BtnGuardar.IsEnabled = false;
            BtnCancelar.IsEnabled = false;

            var updateRequest = BuildUpdateRequest();

            var updatedProfile = await _profileService.UpdateUserProfileAsync(updateRequest);

            if (updatedProfile != null)
            {
                _originalProfile = updatedProfile;
                _hasChanges = false;

                App.Log?.LogInformation("✅ Perfil actualizado exitosamente");

                await ShowSuccessAsync("✅ Perfil actualizado correctamente");

                await Task.Delay(1500);

                OnVolverClick(sender, e);
            }
            else
            {
                App.Log?.LogWarning("⚠️ No se pudo actualizar el perfil");
                await ShowErrorAsync("No se pudo actualizar el perfil");
            }
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error guardando perfil");
            await ShowErrorAsync($"Error guardando perfil:\n\n{ex.Message}");
        }
        finally
        {
            BtnGuardar.IsEnabled = true;
            BtnCancelar.IsEnabled = true;
        }
    }

    private UpdateProfileRequest BuildUpdateRequest()
    {
        var request = new UpdateProfileRequest
        {
            FirstName = TxtFirstName.Text?.Trim(),
            LastName = TxtLastName.Text?.Trim(),
            Phone = TxtPhone.Text?.Trim(),
            Mobile = TxtMobile.Text?.Trim(),
            Address = TxtAddress.Text?.Trim(),
            City = TxtCity.Text?.Trim(),
            PostalCode = TxtPostalCode.Text?.Trim(),
            Department = TxtDepartment.Text?.Trim(),
            Position = TxtPosition.Text?.Trim(),
            EmployeeType = (CmbEmployeeType.SelectedItem as ComboBoxItem)?.Content?.ToString(),
            AvatarUrl = TxtAvatarUrl.Text?.Trim(),
            Notes = TxtNotes.Text?.Trim()
        };

        if (DpHireDate.Date.HasValue)
        {
            request.HireDate = DpHireDate.Date.Value.ToString("yyyy-MM-dd");
        }

        return request;
    }

    private async void OnCancelarClick(object sender, RoutedEventArgs e)
    {
        if (_hasChanges)
        {
            var dialog = new ContentDialog
            {
                Title = "⚠️ Cambios sin guardar",
                Content = "Hay cambios sin guardar. ¿Estás seguro de que deseas salir?",
                PrimaryButtonText = "Salir sin guardar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result != ContentDialogResult.Primary)
            {
                return;
            }
        }

        OnVolverClick(sender, e);
    }

    private async void OnVolverClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = new Duration(TimeSpan.FromMilliseconds(300)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            Storyboard.SetTarget(fadeOut, RootGrid);
            Storyboard.SetTargetProperty(fadeOut, "Opacity");

            var storyboard = new Storyboard();
            storyboard.Children.Add(fadeOut);

            var tcs = new TaskCompletionSource<bool>();
            storyboard.Completed += (s, args) => tcs.SetResult(true);

            storyboard.Begin();
            await tcs.Task;

            App.MainWindowInstance?.Navigator?.Navigate(typeof(DiarioPage));
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error navegando a DiarioPage");
            App.MainWindowInstance?.Navigator?.Navigate(typeof(DiarioPage));
        }
    }

    private void UpdateThemeAssets(ElementTheme theme)
    {
        var effectiveTheme = theme;
        if (theme == ElementTheme.Default)
        {
            var uiSettings = new Windows.UI.ViewManagement.UISettings();
            var foreground = uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Foreground);
            effectiveTheme = foreground.R == 255 && foreground.G == 255 && foreground.B == 255
                ? ElementTheme.Dark
                : ElementTheme.Light;
        }

        if (effectiveTheme == ElementTheme.Dark)
        {
            LogoImageBanner.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(
                new Uri("ms-appx:///Assets/LogoOscuro.png"));

            BackgroundImageBrush.ImageSource = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(
                new Uri("ms-appx:///Assets/diario_bg_dark.png"));
            BackgroundImageBrush.Opacity = 1.0;
        }
        else
        {
            LogoImageBanner.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(
                new Uri("ms-appx:///Assets/LogoClaro.png"));

            BackgroundImageBrush.ImageSource = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(
                new Uri("ms-appx:///Assets/Diario_bg_claro.png"));
            BackgroundImageBrush.Opacity = 0.15;
        }

        App.Log?.LogDebug("Tema actualizado en UserProfilePage: {theme} (efectivo: {effective})", theme, effectiveTheme);
    }

    private async Task ShowErrorAsync(string message)
    {
        try
        {
            var dlg = new ContentDialog
            {
                Title = "❌ Error",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };

            await dlg.ShowAsync();
        }
        catch { }
    }

    private async Task ShowSuccessAsync(string message)
    {
        try
        {
            var dlg = new ContentDialog
            {
                Title = "✅ Éxito",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };

            await dlg.ShowAsync();
        }
        catch { }
    }
    
    /// <summary>🆕 NUEVO: Muestra un mensaje informativo al usuario.</summary>
    private async Task ShowInfoAsync(string message)
    {
        try
        {
            var dlg = new ContentDialog
            {
                Title = "ℹ️ Información",
                Content = message,
                CloseButtonText = "Entendido",
                XamlRoot = this.XamlRoot
            };

            await dlg.ShowAsync();
        }
        catch { }
    }
    
    /// <summary>Muestra un error crítico con opción de volver a DiarioPage.</summary>
    private async Task ShowCriticalErrorAsync(string title, string message)
    {
        try
        {
            var dlg = new ContentDialog
            {
                Title = title,
                Content = message,
                PrimaryButtonText = "Volver al Inicio",
                CloseButtonText = "Cerrar Sesión",
                XamlRoot = this.XamlRoot
            };

            var result = await dlg.ShowAsync();
            
            if (result == ContentDialogResult.Primary)
            {
                // Volver a DiarioPage
                App.MainWindowInstance?.Navigator?.Navigate(typeof(DiarioPage));
            }
            else
            {
                // Cerrar sesión
                App.Api.ClearToken();
                App.Api.ClearGetCache();
                App.MainWindowInstance?.Navigator?.Navigate(typeof(LoginPage));
            }
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error mostrando diálogo de error crítico");
            
            // Fallback: volver a login
            App.Api.ClearToken();
            App.MainWindowInstance?.Navigator?.Navigate(typeof(LoginPage));
        }
    }
}
