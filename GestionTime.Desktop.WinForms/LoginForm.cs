using System;
using System.Drawing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using System.IO;

namespace GestionTime.Desktop.WinForms
{
    public partial class LoginForm : Form
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly string _apiBaseUrl;
        
        private TextBox txtEmail;
        private TextBox txtPassword;
        private Button btnLogin;
        private Label lblTitle;
        private Label lblVersion;
        private Panel pnlMain;
        private CheckBox chkRememberMe;
        private LinkLabel lnkForgotPassword;

        public string? UserToken { get; private set; }
        public string? UserName { get; private set; }
        public string? UserEmail { get; private set; }
        public string? UserRole { get; private set; }

        public LoginForm(HttpClient httpClient, ILogger logger, string apiBaseUrl)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiBaseUrl = apiBaseUrl;
            
            InitializeComponent();
            ApplyModernStyling();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            // Form properties
            Text = "GestionTime Desktop - Login";
            Size = new Size(450, 600);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = Color.FromArgb(240, 244, 248);

            // Main panel
            pnlMain = new Panel
            {
                Size = new Size(380, 500),
                Location = new Point(35, 50),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            // Title
            lblTitle = new Label
            {
                Text = "🕐 GestionTime Desktop",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 122, 183),
                Size = new Size(350, 50),
                Location = new Point(15, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Version
            lblVersion = new Label
            {
                Text = "v1.1.0 - Sistema de Gestión de Tiempo",
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.Gray,
                Size = new Size(350, 20),
                Location = new Point(15, 85),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Email label and textbox
            var lblEmail = new Label
            {
                Text = "📧 Email:",
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                ForeColor = Color.FromArgb(33, 37, 41),
                Size = new Size(320, 25),
                Location = new Point(30, 140)
            };

            txtEmail = new TextBox
            {
                Font = new Font("Segoe UI", 12),
                Size = new Size(320, 35),
                Location = new Point(30, 170),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Password label and textbox
            var lblPassword = new Label
            {
                Text = "🔒 Contraseña:",
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                ForeColor = Color.FromArgb(33, 37, 41),
                Size = new Size(320, 25),
                Location = new Point(30, 220)
            };

            txtPassword = new TextBox
            {
                Font = new Font("Segoe UI", 12),
                Size = new Size(320, 35),
                Location = new Point(30, 250),
                BorderStyle = BorderStyle.FixedSingle,
                UseSystemPasswordChar = true
            };

            // Remember me checkbox
            chkRememberMe = new CheckBox
            {
                Text = "Recordar credenciales",
                Font = new Font("Segoe UI", 10),
                Size = new Size(200, 25),
                Location = new Point(30, 300),
                ForeColor = Color.FromArgb(33, 37, 41)
            };

            // Login button
            btnLogin = new Button
            {
                Text = "🚀 Iniciar Sesión",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Size = new Size(320, 45),
                Location = new Point(30, 350),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += BtnLogin_Click;

            // Forgot password link
            lnkForgotPassword = new LinkLabel
            {
                Text = "¿Olvidaste tu contraseña?",
                Font = new Font("Segoe UI", 9),
                Size = new Size(200, 20),
                Location = new Point(30, 420),
                LinkColor = Color.FromArgb(51, 122, 183),
                ActiveLinkColor = Color.FromArgb(40, 96, 144)
            };
            lnkForgotPassword.LinkClicked += (s, e) => MessageBox.Show("Contacte con el administrador para resetear su contraseña.", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Add controls to main panel
            pnlMain.Controls.AddRange(new Control[] 
            {
                lblTitle, lblVersion, lblEmail, txtEmail, 
                lblPassword, txtPassword, chkRememberMe, 
                btnLogin, lnkForgotPassword
            });

            // Add main panel to form
            Controls.Add(pnlMain);

            ResumeLayout(false);
        }

        private void ApplyModernStyling()
        {
            // Add shadow effect to main panel
            pnlMain.Paint += (s, e) =>
            {
                var rect = pnlMain.ClientRectangle;
                using var shadowBrush = new SolidBrush(Color.FromArgb(50, 0, 0, 0));
                e.Graphics.FillRectangle(shadowBrush, rect.X + 3, rect.Y + 3, rect.Width, rect.Height);
                using var whiteBrush = new SolidBrush(Color.White);
                e.Graphics.FillRectangle(whiteBrush, rect);
            };

            // Hover effects for button
            btnLogin.MouseEnter += (s, e) => btnLogin.BackColor = Color.FromArgb(34, 142, 59);
            btnLogin.MouseLeave += (s, e) => btnLogin.BackColor = Color.FromArgb(40, 167, 69);

            // Focus styling for textboxes
            txtEmail.Enter += (s, e) => txtEmail.BackColor = Color.FromArgb(248, 249, 250);
            txtEmail.Leave += (s, e) => txtEmail.BackColor = Color.White;
            txtPassword.Enter += (s, e) => txtPassword.BackColor = Color.FromArgb(248, 249, 250);
            txtPassword.Leave += (s, e) => txtPassword.BackColor = Color.White;
        }

        private async void BtnLogin_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtEmail.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("Por favor, complete todos los campos.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnLogin.Text = "🔄 Conectando...";
            btnLogin.Enabled = false;

            try
            {
                await DoLogin();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el login");
                MessageBox.Show($"Error de conexión: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnLogin.Text = "🚀 Iniciar Sesión";
                btnLogin.Enabled = true;
            }
        }

        private async Task DoLogin()
        {
            var loginData = new
            {
                email = txtEmail.Text.Trim(),
                password = txtPassword.Text
            };

            var json = JsonConvert.SerializeObject(loginData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Intentando login para usuario: {Email}", loginData.email);

            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/v1/auth/login-desktop", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonConvert.DeserializeObject<LoginResponse>(responseContent);
                
                if (result != null && !string.IsNullOrEmpty(result.Token))
                {
                    UserToken = result.Token;
                    UserName = result.User?.Name ?? txtEmail.Text;
                    UserEmail = txtEmail.Text;
                    UserRole = result.User?.Role ?? "user";

                    if (chkRememberMe.Checked)
                    {
                        SaveCredentials();
                    }

                    _logger.LogInformation("Login exitoso para usuario: {Email}", UserEmail);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    MessageBox.Show("Respuesta del servidor inválida.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                _logger.LogWarning("Login fallido: {StatusCode} - {Content}", response.StatusCode, responseContent);
                MessageBox.Show("Email o contraseña incorrectos.", "Error de Autenticación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SaveCredentials()
        {
            try
            {
                var settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GestionTime", "settings.json");
                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
                
                var settings = new { Email = txtEmail.Text, RememberMe = true };
                File.WriteAllText(settingsPath, JsonConvert.SerializeObject(settings, Formatting.Indented));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudieron guardar las credenciales");
            }
        }

        private void LoadSavedCredentials()
        {
            try
            {
                var settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GestionTime", "settings.json");
                if (File.Exists(settingsPath))
                {
                    var content = File.ReadAllText(settingsPath);
                    var settings = JsonConvert.DeserializeObject<dynamic>(content);
                    if (settings?.Email != null)
                    {
                        txtEmail.Text = settings.Email;
                        chkRememberMe.Checked = settings.RememberMe == true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudieron cargar las credenciales guardadas");
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LoadSavedCredentials();
        }
    }

    public class LoginResponse
    {
        public string Token { get; set; } = "";
        public UserInfo? User { get; set; }
    }

    public class UserInfo
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
    }
}