using System.Threading.Tasks;
using GestionTime.Desktop.Models;

namespace GestionTime.Desktop.Services;

/// <summary>Servicio MOCK para simular actualizaciones (SOLO PARA PRUEBAS).</summary>
public class UpdateServiceMock : IUpdateService
{
    public string GetCurrentVersion() => "1.1.0";

    public Task<UpdateInfo> CheckForUpdatesAsync()
    {
        // Simular que hay una versión 1.2.0-beta disponible
        return Task.FromResult(new UpdateInfo
        {
            CurrentVersion = "1.1.0",
            LatestVersion = "1.2.0-beta",
            UpdateAvailable = true,
            DownloadUrl = "https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases/download/v1.2.0-beta/GestionTime-v1.2.0-beta.zip",
            ReleaseNotes = "## ✨ Novedades\n- Sistema de actualizaciones automático\n- Versión visible en login\n\n## 🐛 Correcciones\n- Mejoras generales",
            PublishedAt = System.DateTime.Now.AddDays(-1),
            ReleaseName = "GestionTime Desktop v1.2.0-beta"
        });
    }

    public void OpenReleasesPage()
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases",
            UseShellExecute = true
        });
    }

    public Task<bool> DownloadUpdateAsync(string downloadUrl, string destinationPath)
    {
        return Task.FromResult(false);
    }
}
