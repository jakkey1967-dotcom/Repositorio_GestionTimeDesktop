using System;
using System.Linq;
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
    public sealed partial class ForgotPasswordPage : Page
    {
        private string? _codigoEnviado;
        private string? _emailIngresado;

        public ForgotPasswordPage()
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
        // Recuperar contraseña - PASO 1: Solicitar Código
        // =========================

        private async void OnSolicitarCodigoClick(object sender, RoutedEventArgs e)
        {
            var email = TxtEmail.Text?.Trim() ?? "";

            // Validaciones
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

            SetBusySolicitar(true);
            HideMessage();

            try
            {
                App.Log?.LogInformation("????????????????????????????????????????");
                App.Log?.LogInformation("?? RECUPERAR CONTRASEÑA - PASO 1: Solicitar Código");
                App.Log?.LogInformation("   Email: {email}", email);

                // Llamar al endpoint para solicitar código
                var payload = new ForgotPasswordRequestStep1
                {
                    Email = email
                };

                var result = await App.Api.PostAsync<ForgotPasswordRequestStep1, ForgotPasswordResponseStep1>(
                    "/api/v1/auth/forgot-password", 
                    payload
                );

                if (result == null)
                {
                    ShowMessage("Error al procesar la solicitud. Intente nuevamente.", MessageType.Error);
                    App.Log?.LogError("Respuesta nula del servidor");
                    SetBusySolicitar(false);
                    return;
                }

                if (!string.IsNullOrEmpty(result.Error))
                {
                    ShowMessage($"Error: {result.Error}", MessageType.Error);
                    App.Log?.LogWarning("Error en solicitud: {error}", result.Error);
                    SetBusySolicitar(false);
                    return;
                }

                // Éxito: guardar email y mostrar paso 2
                _emailIngresado = email;
                App.Log?.LogInformation("? Código enviado exitosamente");
                ShowMessage($"? {result.Message ?? "Código enviado a tu correo. Revisa tu bandeja de entrada."}", MessageType.Success);
                
                // Mostrar campos de verificación
                MostrarPasoVerificacion();

            }
            catch (HttpRequestException httpEx)
            {
                App.Log?.LogError(httpEx, "Error HTTP durante solicitud de código");
                
                var statusCode = httpEx.StatusCode;
                string errorMsg = statusCode switch
                {
                    System.Net.HttpStatusCode.Unauthorized => "Error 401: Endpoint no disponible o requiere configuración.",
                    System.Net.HttpStatusCode.NotFound => "Error 404: El endpoint no existe en el servidor.",
                    _ => "Error de conexión. Verifique su red e intente nuevamente."
                };
                
                ShowMessage(errorMsg, MessageType.Error);
            }
            catch (Exception ex)
            {
                App.Log?.LogError(ex, "Error inesperado durante solicitud de código");
                ShowMessage($"Error: {ex.Message}", MessageType.Error);
            }
            finally
            {
                SetBusySolicitar(false);
            }
        }

        // =========================
        // Recuperar contraseña - PASO 2: Verificar Código y Cambiar Contraseña
        // =========================

        private async void OnCambiarPasswordClick(object sender, RoutedEventArgs e)
        {
            var codigo = TxtCodigo.Text?.Trim() ?? "";
            var password = TxtPassword.Password?.Trim() ?? "";
            var confirmPassword = TxtConfirmPassword.Password?.Trim() ?? "";

            // Validaciones
            if (string.IsNullOrWhiteSpace(codigo))
            {
                ShowMessage("Por favor, ingrese el código de verificación.", MessageType.Warning);
                return;
            }

            if (codigo.Length != 6 || !IsNumeric(codigo))
            {
                ShowMessage("El código debe ser de 6 dígitos numéricos.", MessageType.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowMessage("Por favor, ingrese la nueva contraseña.", MessageType.Warning);
                return;
            }

            if (password.Length < 6)
            {
                ShowMessage("La contraseña debe tener al menos 6 caracteres.", MessageType.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(confirmPassword))
            {
                ShowMessage("Por favor, confirme la contraseña.", MessageType.Warning);
                return;
            }

            if (password != confirmPassword)
            {
                ShowMessage("Las contraseñas no coinciden.", MessageType.Warning);
                return;
            }

            SetBusyCambiar(true);
            HideMessage();

            try
            {
                App.Log?.LogInformation("????????????????????????????????????????");
                App.Log?.LogInformation("?? RECUPERAR CONTRASEÑA - PASO 2: Validar Código y Cambiar Contraseña");
                App.Log?.LogInformation("   Código: {codigo}", codigo);

                // Llamar al endpoint para resetear con código
                var payload = new ResetPasswordRequest
                {
                    Token = codigo,
                    Email = _emailIngresado,
                    NewPassword = password
                };

                var result = await App.Api.PostAsync<ResetPasswordRequest, ResetPasswordResponse>(
                    "/api/v1/auth/reset-password", 
                    payload
                );

                if (result == null)
                {
                    ShowMessage("Error al cambiar la contraseña. Intente nuevamente.", MessageType.Error);
                    App.Log?.LogError("Respuesta nula del servidor");
                    SetBusyCambiar(false);
                    return;
                }

                if (!string.IsNullOrEmpty(result.Error))
                {
                    ShowMessage($"Error: {result.Error}", MessageType.Error);
                    App.Log?.LogWarning("Error en cambio de contraseña: {error}", result.Error);
                    SetBusyCambiar(false);
                    return;
                }

                App.Log?.LogInformation("? Contraseña cambiada exitosamente");
                ShowMessage($"? {result.Message ?? "Contraseña actualizada correctamente. Ya puedes iniciar sesión."}", MessageType.Success);

                // Esperar 2 segundos y volver al login
                await Task.Delay(2000);
                OnBackClick(sender, e);

            }
            catch (HttpRequestException httpEx)
            {
                App.Log?.LogError(httpEx, "Error HTTP durante cambio de contraseña");
                
                var statusCode = httpEx.StatusCode;
                string errorMsg = statusCode switch
                {
                    System.Net.HttpStatusCode.BadRequest => "Código inválido o expirado. Solicita uno nuevo.",
                    System.Net.HttpStatusCode.Unauthorized => "Error de autorización. Verifica el código.",
                    System.Net.HttpStatusCode.NotFound => "Error 404: El endpoint no existe en el servidor.",
                    _ => "Error de conexión. Verifique su red e intente nuevamente."
                };
                
                ShowMessage(errorMsg, MessageType.Error);
            }
            catch (Exception ex)
            {
                App.Log?.LogError(ex, "Error inesperado durante cambio de contraseña");
                ShowMessage($"Error: {ex.Message}", MessageType.Error);
            }
            finally
            {
                SetBusyCambiar(false);
            }
        }

        // =========================
        // UI Helpers
        // =========================

        private void MostrarPasoVerificacion()
        {
            // Deshabilitar email
            TxtEmail.IsEnabled = false;
            BtnSolicitarCodigo.IsEnabled = false;

            // Cambiar instrucciones
            TxtInstrucciones.Text = "Paso 2: Ingresa el código que recibiste por correo y tu nueva contraseña.";

            // Mostrar separador y campos de verificación
            Separador.Visibility = Visibility.Visible;
            PanelVerificacion.Visibility = Visibility.Visible;

            // Focus en el campo de código
            TxtCodigo.Focus(FocusState.Programmatic);
        }

        private void SetBusySolicitar(bool busy)
        {
            PrgSolicitar.IsActive = busy;
            PrgSolicitar.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
            BtnSolicitarCodigo.IsEnabled = !busy;
            TxtEmail.IsEnabled = !busy;
        }

        private void SetBusyCambiar(bool busy)
        {
            PrgCambiar.IsActive = busy;
            PrgCambiar.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
            BtnCambiarPassword.IsEnabled = !busy;
            TxtCodigo.IsEnabled = !busy;
            TxtPassword.IsEnabled = !busy;
            TxtConfirmPassword.IsEnabled = !busy;
            
            TxtStatus.Text = busy ? "Procesando..." : "";
            TxtStatus.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
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

        private static bool IsNumeric(string text)
        {
            return !string.IsNullOrWhiteSpace(text) && text.All(char.IsDigit);
        }

        // =========================
        // UI Message Helpers
        // =========================

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

        // PASO 1: Solicitar código
        private sealed class ForgotPasswordRequestStep1
        {
            public string Email { get; set; } = string.Empty;
        }

        private sealed class ForgotPasswordResponseStep1
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            public string? Error { get; set; }
        }

        // PASO 2: Validar código y cambiar contraseña
        private sealed class ResetPasswordRequest
        {
            public string Token { get; set; } = string.Empty;
            public string? Email { get; set; }
            public string NewPassword { get; set; } = string.Empty;
        }

        private sealed class ResetPasswordResponse
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            public string? Error { get; set; }
        }
    }
}
