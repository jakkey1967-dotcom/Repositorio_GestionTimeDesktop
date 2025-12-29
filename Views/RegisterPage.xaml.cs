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

            // Validaciones
            if (string.IsNullOrWhiteSpace(nombre))
            {
                ShowMessage("Por favor, ingrese su nombre completo.", MessageType.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                ShowMessage("Por favor, ingrese su correo electrónico.", MessageType.Warning);
                return;
            }

            if (!IsValidEmail(email))
            {
                ShowMessage("Por favor, ingrese un correo electrónico válido.", MessageType.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowMessage("Por favor, ingrese una contraseña.", MessageType.Warning);
                return;
            }

            if (password.Length < 8)
            {
                ShowMessage("La contraseña debe tener al menos 8 caracteres.", MessageType.Warning);
                return;
            }

            if (password != confirmPassword)
            {
                ShowMessage("Las contraseñas no coinciden.", MessageType.Warning);
                return;
            }

            SetBusy(true, "Registrando usuario...");
            HideMessage();

            try
            {
                App.Log?.LogInformation("???????????????????????????????????????????????????????????????");
                App.Log?.LogInformation("?? REGISTRO DE USUARIO - Iniciando");
                App.Log?.LogInformation("   Email: {email}", email);
                App.Log?.LogInformation("   Nombre: {nombre}", nombre);

                // Llamar al endpoint de registro
                var payload = new RegisterRequest
                {
                    Email = email,
                    Password = password,
                    FullName = nombre,  // ? Cambiado de Nombre a FullName
                    Empresa = empresa
                };

                var result = await App.Api.PostAsync<RegisterRequest, RegisterResponse>("/api/v1/auth/register", payload);

                if (result == null)
                {
                    ShowMessage("Error al registrar usuario. Intente nuevamente.", MessageType.Error);
                    App.Log?.LogError("Respuesta nula del servidor");
                    SetBusy(false, "");
                    return;
                }

                if (!string.IsNullOrEmpty(result.Error))
                {
                    ShowMessage($"Error: {result.Error}", MessageType.Error);
                    App.Log?.LogWarning("Error en registro: {error}", result.Error);
                    SetBusy(false, "");
                    return;
                }

                App.Log?.LogInformation("? Usuario registrado exitosamente");
                ShowMessage("¡Registro exitoso! Redirigiendo al login...", MessageType.Success);

                await Task.Delay(1500);

                // Volver al login
                OnBackClick(sender, e);
            }
            catch (HttpRequestException httpEx)
            {
                App.Log?.LogError(httpEx, "Error de conexión HTTP durante registro");
                ShowMessage("Error de conexión. Verifique su red e intente nuevamente.", MessageType.Error);
            }
            catch (Exception ex)
            {
                App.Log?.LogError(ex, "Error inesperado durante registro");
                ShowMessage($"Error: {ex.Message}", MessageType.Error);
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

        private enum MessageType
        {
            Success,
            Error,
            Warning,
            Info
        }

        private void ShowMessage(string text, MessageType type)
        {
            MsgBox.Visibility = Visibility.Visible;
            LblMsg.Text = text;

            switch (type)
            {
                case MessageType.Success:
                    MsgBox.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 220, 252, 231));
                    MsgBox.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 34, 197, 94));
                    LblMsg.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 21, 128, 61));
                    MsgIcon.Glyph = "\uE73E";
                    MsgIcon.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 34, 197, 94));
                    break;
                
                case MessageType.Error:
                    MsgBox.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 254, 226, 226));
                    MsgBox.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 220, 38, 38));
                    LblMsg.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 127, 29, 29));
                    MsgIcon.Glyph = "\uE783";
                    MsgIcon.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 220, 38, 38));
                    break;
                
                case MessageType.Warning:
                    MsgBox.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 254, 243, 199));
                    MsgBox.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11));
                    LblMsg.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 146, 64, 14));
                    MsgIcon.Glyph = "\uE7BA";
                    MsgIcon.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11));
                    break;
                
                case MessageType.Info:
                    MsgBox.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 224, 242, 254));
                    MsgBox.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 59, 130, 246));
                    LblMsg.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 30, 64, 175));
                    MsgIcon.Glyph = "\uE946";
                    MsgIcon.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 59, 130, 246));
                    break;
            }
        }

        private void HideMessage()
        {
            MsgBox.Visibility = Visibility.Collapsed;
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
