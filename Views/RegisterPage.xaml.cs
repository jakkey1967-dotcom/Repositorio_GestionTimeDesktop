using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.Extensions.Logging;
using Windows.Storage;

namespace GestionTime.Desktop.Views
{
    public sealed partial class RegisterPage : Page
    {
        private bool _isPasswordVisible = false;
        private bool _isConfirmPasswordVisible = false;

        public RegisterPage()
        {
            InitializeComponent();
            LoadSavedTheme();
            this.Loaded += OnPageLoaded;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= OnPageLoaded;
            
            try
            {
                var fadeIn = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = new Duration(TimeSpan.FromMilliseconds(500)),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                
                Storyboard.SetTarget(fadeIn, PageRootGrid);
                Storyboard.SetTargetProperty(fadeIn, "Opacity");
                
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeIn);
                storyboard.Begin();
            }
            catch (Exception ex)
            {
                App.Log?.LogWarning(ex, "Error en animación de fade in");
                PageRootGrid.Opacity = 1;
            }
        }

        // =========================
        // Tema
        // =========================

        private void LoadSavedTheme()
        {
            try
            {
                var settings = ApplicationData.Current.LocalSettings.Values;
                
                ElementTheme theme = ElementTheme.Dark;
                
                if (settings.TryGetValue("AppTheme", out var themeObj) && themeObj is string themeName)
                {
                    theme = themeName switch
                    {
                        "Light" => ElementTheme.Light,
                        "Dark" => ElementTheme.Dark,
                        "Default" => ElementTheme.Default,
                        _ => ElementTheme.Dark
                    };
                }
                
                SetTheme(theme);
            }
            catch (Exception ex)
            {
                App.Log?.LogWarning(ex, "Error cargando tema guardado");
                SetTheme(ElementTheme.Dark);
            }
        }

        private void SaveTheme(ElementTheme theme)
        {
            try
            {
                var settings = ApplicationData.Current.LocalSettings.Values;
                var themeName = theme switch
                {
                    ElementTheme.Light => "Light",
                    ElementTheme.Dark => "Dark",
                    ElementTheme.Default => "Default",
                    _ => "Dark"
                };
                
                settings["AppTheme"] = themeName;
            }
            catch (Exception ex)
            {
                App.Log?.LogWarning(ex, "Error guardando tema");
            }
        }

        private void SetTheme(ElementTheme theme)
        {
            this.RequestedTheme = theme;
            ThemeSystemItem.IsChecked = theme == ElementTheme.Default;
            ThemeLightItem.IsChecked = theme == ElementTheme.Light;
            ThemeDarkItem.IsChecked = theme == ElementTheme.Dark;
            SaveTheme(theme);
        }

        private void OnThemeSystem(object sender, RoutedEventArgs e) => SetTheme(ElementTheme.Default);
        private void OnThemeLight(object sender, RoutedEventArgs e) => SetTheme(ElementTheme.Light);
        private void OnThemeDark(object sender, RoutedEventArgs e) => SetTheme(ElementTheme.Dark);

        // =========================
        // Navegación
        // =========================

        private async void OnBackClick(object sender, RoutedEventArgs e)
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
                
                Storyboard.SetTarget(fadeOut, PageRootGrid);
                Storyboard.SetTargetProperty(fadeOut, "Opacity");
                
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeOut);
                
                var tcs = new TaskCompletionSource<bool>();
                storyboard.Completed += (s, args) =>
                {
                    tcs.SetResult(true);
                };
                
                storyboard.Begin();
                await tcs.Task;
                
                App.MainWindowInstance?.Navigator?.Navigate(typeof(LoginPage));
            }
            catch (Exception ex)
            {
                App.Log?.LogError(ex, "Error navegando a LoginPage");
                App.MainWindowInstance?.Navigator?.Navigate(typeof(LoginPage));
            }
        }

        // =========================
        // Toggle contraseñas
        // =========================

        private void OnTogglePasswordClick(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;
            
            if (_isPasswordVisible)
            {
                TxtPasswordVisible.Text = TxtPassword.Password;
                TxtPassword.Visibility = Visibility.Collapsed;
                TxtPasswordVisible.Visibility = Visibility.Visible;
                IconPassword.Glyph = "\uED1A";
                ToolTipService.SetToolTip(BtnTogglePassword, "Ocultar contraseña");
                TxtPasswordVisible.Focus(FocusState.Programmatic);
                TxtPasswordVisible.SelectionStart = TxtPasswordVisible.Text.Length;
            }
            else
            {
                TxtPassword.Password = TxtPasswordVisible.Text;
                TxtPasswordVisible.Visibility = Visibility.Collapsed;
                TxtPassword.Visibility = Visibility.Visible;
                IconPassword.Glyph = "\uE7B3";
                ToolTipService.SetToolTip(BtnTogglePassword, "Mostrar contraseña");
                TxtPassword.Focus(FocusState.Programmatic);
            }
        }

        private void OnToggleConfirmPasswordClick(object sender, RoutedEventArgs e)
        {
            _isConfirmPasswordVisible = !_isConfirmPasswordVisible;
            
            if (_isConfirmPasswordVisible)
            {
                TxtConfirmPasswordVisible.Text = TxtConfirmPassword.Password;
                TxtConfirmPassword.Visibility = Visibility.Collapsed;
                TxtConfirmPasswordVisible.Visibility = Visibility.Visible;
                IconConfirmPassword.Glyph = "\uED1A";
                ToolTipService.SetToolTip(BtnToggleConfirmPassword, "Ocultar contraseña");
                TxtConfirmPasswordVisible.Focus(FocusState.Programmatic);
                TxtConfirmPasswordVisible.SelectionStart = TxtConfirmPasswordVisible.Text.Length;
            }
            else
            {
                TxtConfirmPassword.Password = TxtConfirmPasswordVisible.Text;
                TxtConfirmPasswordVisible.Visibility = Visibility.Collapsed;
                TxtConfirmPassword.Visibility = Visibility.Visible;
                IconConfirmPassword.Glyph = "\uE7B3";
                ToolTipService.SetToolTip(BtnToggleConfirmPassword, "Mostrar contraseña");
                TxtConfirmPassword.Focus(FocusState.Programmatic);
            }
        }

        // =========================
        // Registro
        // =========================

        private async void OnRegisterClick(object sender, RoutedEventArgs e)
        {
            var nombre = TxtNombre.Text?.Trim() ?? "";
            var email = TxtEmail.Text?.Trim() ?? "";
            var password = _isPasswordVisible ? TxtPasswordVisible.Text ?? "" : TxtPassword.Password ?? "";
            var confirmPassword = _isConfirmPasswordVisible ? TxtConfirmPasswordVisible.Text ?? "" : TxtConfirmPassword.Password ?? "";
            var empresa = TxtEmpresa.Text?.Trim() ?? "";

            // Validaciones con notificaciones
            if (string.IsNullOrWhiteSpace(nombre))
            {
                App.Notifications?.ShowWarning(
                    "Por favor, ingrese su nombre completo",
                    title: "⚠️ Campo Requerido");
                return;
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                App.Notifications?.ShowWarning(
                    "Por favor, ingrese su correo electrónico",
                    title: "⚠️ Campo Requerido");
                return;
            }

            if (!IsValidEmail(email))
            {
                App.Notifications?.ShowWarning(
                    "Por favor, ingrese un correo electrónico válido",
                    title: "⚠️ Email Inválido");
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                App.Notifications?.ShowWarning(
                    "Por favor, ingrese una contraseña",
                    title: "⚠️ Campo Requerido");
                return;
            }

            if (password.Length < 8)
            {
                App.Notifications?.ShowWarning(
                    "La contraseña debe tener al menos 8 caracteres",
                    title: "⚠️ Contraseña Débil");
                return;
            }

            if (password != confirmPassword)
            {
                App.Notifications?.ShowWarning(
                    "Las contraseñas no coinciden",
                    title: "⚠️ Error de Validación");
                return;
            }

            SetBusy(true, "Registrando usuario...");

            try
            {
                App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
                App.Log?.LogInformation("📝 REGISTRO DE USUARIO - Iniciando");
                App.Log?.LogInformation("   Email: {email}", email);
                App.Log?.LogInformation("   Nombre: {nombre}", nombre);

                // Llamar al endpoint de registro
                var payload = new RegisterRequest
                {
                    Email = email,
                    Password = password,
                    FullName = nombre,
                    Empresa = empresa
                };

                var result = await App.Api.PostAsync<RegisterRequest, RegisterResponse>("/api/v1/auth/register", payload);

                if (result == null)
                {
                    App.Log?.LogError("Respuesta nula del servidor");
                    
                    App.Notifications?.ShowError(
                        "Error al registrar usuario. Intente nuevamente",
                        title: "❌ Error de Registro");
                    
                    SetBusy(false, "");
                    return;
                }

                if (!string.IsNullOrEmpty(result.Error))
                {
                    App.Log?.LogWarning("Error en registro: {error}", result.Error);
                    
                    App.Notifications?.ShowError(
                        result.Error,
                        title: "❌ Error de Registro");
                    
                    SetBusy(false, "");
                    return;
                }

                App.Log?.LogInformation("✅ Usuario registrado exitosamente");
                
                App.Notifications?.ShowSuccess(
                    "¡Registro exitoso! Redirigiendo al login...",
                    title: "✅ Usuario Creado");

                await Task.Delay(1500);

                // Volver al login
                OnBackClick(sender, e);
            }
            catch (HttpRequestException httpEx)
            {
                App.Log?.LogError(httpEx, "Error de conexión HTTP durante registro");
                
                App.Notifications?.ShowError(
                    "Error de conexión. Verifique su red e intente nuevamente",
                    title: "🌐 Error de Conexión");
            }
            catch (Exception ex)
            {
                App.Log?.LogError(ex, "Error inesperado durante registro");
                
                App.Notifications?.ShowError(
                    ex.Message,
                    title: "❌ Error Inesperado");
            }
            finally
            {
                SetBusy(false, "");
            }
        }

        // =========================
        // Validaciones
        // =========================

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        // =========================
        // UI Helpers
        // =========================

        private void SetBusy(bool busy, string status)
        {
            Prg.IsActive = busy;
            Prg.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
            BtnRegister.IsEnabled = !busy;
            TxtNombre.IsEnabled = !busy;
            TxtEmail.IsEnabled = !busy;
            TxtPassword.IsEnabled = !busy;
            TxtPasswordVisible.IsEnabled = !busy;
            TxtConfirmPassword.IsEnabled = !busy;
            TxtConfirmPasswordVisible.IsEnabled = !busy;
            TxtEmpresa.IsEnabled = !busy;
            BtnTogglePassword.IsEnabled = !busy;
            BtnToggleConfirmPassword.IsEnabled = !busy;
            
            TxtStatus.Text = status;
            TxtStatus.Visibility = string.IsNullOrEmpty(status) ? Visibility.Collapsed : Visibility.Visible;
        }

        // =========================
        // DTOs
        // =========================

        private sealed class RegisterRequest
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;  // ? Cambiado de "Nombre" a "FullName"
            public string Empresa { get; set; } = string.Empty;
        }

        private sealed class RegisterResponse
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            public string? Error { get; set; }
        }
    }
}
