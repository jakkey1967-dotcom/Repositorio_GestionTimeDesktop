using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Windows.Forms;

namespace GestionTime.Desktop.WinForms
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                // Configure logging
                using var loggerFactory = LoggerFactory.Create(builder => 
                    builder.AddConsole().SetMinimumLevel(LogLevel.Information));
                var logger = loggerFactory.CreateLogger("GestionTime.WinForms");

                logger.LogInformation("🚀 Iniciando GestionTime Desktop WinForms v1.1.0");

                // Load configuration
                var config = LoadConfiguration();
                var apiBaseUrl = config["Api:BaseUrl"] ?? "https://gestiontimeapi.onrender.com";

                logger.LogInformation("📡 API Base URL: {ApiUrl}", apiBaseUrl);

                // Create HTTP client
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                // Show login form
                var loginForm = new LoginForm(httpClient, logger, apiBaseUrl);
                var loginResult = loginForm.ShowDialog();

                if (loginResult == DialogResult.OK && !string.IsNullOrEmpty(loginForm.UserToken))
                {
                    logger.LogInformation("✅ Login exitoso, abriendo aplicación principal");
                    
                    // Configure HTTP client with auth token
                    httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginForm.UserToken);

                    // Show main form
                    var mainForm = new MainForm(httpClient, logger, apiBaseUrl, loginForm.UserName!, loginForm.UserEmail!, loginForm.UserRole!);
                    Application.Run(mainForm);
                }
                else
                {
                    logger.LogInformation("❌ Login cancelado por el usuario");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error crítico al iniciar la aplicación:\n\n{ex.Message}", 
                              "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static IConfiguration LoadConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            return builder.Build();
        }
    }
}