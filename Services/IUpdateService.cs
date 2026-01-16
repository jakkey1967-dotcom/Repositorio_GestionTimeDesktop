using GestionTime.Desktop.Models;

namespace GestionTime.Desktop.Services;

public interface IUpdateService
{
    /// <summary>
    /// Verifica si hay una nueva versión disponible en GitHub
    /// </summary>
    Task<UpdateInfo> CheckForUpdatesAsync();

    /// <summary>
    /// Obtiene la versión actual de la aplicación
    /// </summary>
    string GetCurrentVersion();

    /// <summary>
    /// Abre el navegador en la página de releases de GitHub
    /// </summary>
    void OpenReleasesPage();

    /// <summary>
    /// Descarga la última versión
    /// </summary>
    Task<bool> DownloadUpdateAsync(string downloadUrl, string destinationPath);
}
