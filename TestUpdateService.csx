// TEST MANUAL DEL UPDATE SERVICE
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using GestionTime.Desktop.Services;

// Setup básico de logging
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<UpdateService>();

var updateService = new UpdateService(logger);

Console.WriteLine("=== TEST UPDATE SERVICE ===\n");

// 1. Obtener versión actual
var currentVersion = updateService.GetCurrentVersion();
Console.WriteLine($"✅ Versión actual detectada: {currentVersion}\n");

// 2. Verificar actualizaciones
Console.WriteLine("🔍 Verificando actualizaciones en GitHub...\n");
var updateInfo = await updateService.CheckForUpdatesAsync();

Console.WriteLine($"\n=== RESULTADO ===");
Console.WriteLine($"CurrentVersion: {updateInfo.CurrentVersion}");
Console.WriteLine($"LatestVersion: {updateInfo.LatestVersion}");
Console.WriteLine($"UpdateAvailable: {updateInfo.UpdateAvailable}");
Console.WriteLine($"ReleaseName: {updateInfo.ReleaseName}");
Console.WriteLine($"DownloadUrl: {updateInfo.DownloadUrl}");

Console.WriteLine($"\n=== ANÁLISIS ===");
if (updateInfo.UpdateAvailable)
{
    Console.WriteLine($"✅ NUEVA VERSIÓN DISPONIBLE: {updateInfo.CurrentVersion} -> {updateInfo.LatestVersion}");
}
else
{
    Console.WriteLine($"ℹ️ NO HAY ACTUALIZACIÓN (Actual: {updateInfo.CurrentVersion}, GitHub: {updateInfo.LatestVersion})");
}
