using GestionTime.Desktop.Services.Import;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GestionTime.Desktop.Dialogs;

/// <summary>Diálogo para importar partes desde Excel.</summary>
public sealed partial class ImportExcelDialog : ContentDialog
{
    private ImportResult? _importResult;
    private CancellationTokenSource? _cts;
    private bool _isImporting;

    public bool ImportCompleted { get; private set; }

    public ImportExcelDialog()
    {
        this.InitializeComponent();
    }

    /// <summary>Carga el archivo Excel y muestra preview.</summary>
    public async Task LoadFileAsync(string filePath)
    {
        try
        {
            IsPrimaryButtonEnabled = false;
            TxtFileName.Text = $"Cargando {System.IO.Path.GetFileName(filePath)}...";

            var service = new ExcelPartesImportService();
            _importResult = await service.ReadExcelAsync(filePath, App.Log);

            // Actualizar UI
            TxtFileName.Text = _importResult.FileName;
            TxtTotalRows.Text = _importResult.TotalRows.ToString();
            TxtValidRows.Text = _importResult.ValidItems.Count.ToString();
            TxtErrorRows.Text = _importResult.Errors.Count.ToString();

            SummaryPanel.Visibility = Visibility.Visible;

            // Mostrar errores si hay
            if (_importResult.Errors.Any())
            {
                ErrorListPanel.Visibility = Visibility.Visible;
                ErrorList.ItemsSource = _importResult.Errors.Take(20); // Máximo 20 errores
            }

            // Habilitar botón solo si hay válidos
            IsPrimaryButtonEnabled = _importResult.ValidItems.Any();

            if (!_importResult.ValidItems.Any())
            {
                var dialog = new ContentDialog
                {
                    Title = "⚠️ Sin Registros Válidos",
                    Content = $"No se encontraron registros válidos para importar.\n\nTotal errores: {_importResult.Errors.Count}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot,
                    RequestedTheme = ElementTheme.Dark
                };
                await dialog.ShowAsync();
            }
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error cargando archivo Excel");
            
            var errorDialog = new ContentDialog
            {
                Title = "❌ Error de Lectura",
                Content = $"No se pudo leer el archivo Excel:\n\n{ex.Message}",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot,
                RequestedTheme = ElementTheme.Dark
            };
            await errorDialog.ShowAsync();
            
            Hide();
        }
    }

    /// <summary>Ejecuta la importación al backend.</summary>
    private async void OnImportClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (_importResult == null || !_importResult.ValidItems.Any() || _isImporting)
        {
            args.Cancel = true;
            return;
        }

        args.Cancel = true; // No cerrar automáticamente
        _isImporting = true;

        try
        {
            // Cambiar UI a modo progreso
            IsPrimaryButtonEnabled = false;
            IsSecondaryButtonEnabled = true;
            SecondaryButtonText = "Cancelar";
            SummaryPanel.Visibility = Visibility.Collapsed;
            ProgressPanel.Visibility = Visibility.Visible;

            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            var total = _importResult.ValidItems.Count;
            var success = 0;
            var failed = 0;

            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
            App.Log?.LogInformation("🚀 IMPORTACIÓN MASIVA - Iniciando");
            App.Log?.LogInformation("   Total a importar: {total}", total);

            ImportProgress.Maximum = total;

            for (int i = 0; i < total; i++)
            {
                if (ct.IsCancellationRequested)
                {
                    App.Log?.LogWarning("Importación cancelada por el usuario en fila {i}/{total}", i, total);
                    break;
                }

                var item = _importResult.ValidItems[i];

                try
                {
                    // POST a /api/v1/partes
                    await App.Api.PostAsync<Models.Dtos.ParteCreateRequest, object>("/api/v1/partes", item, ct);
                    success++;
                    
                    App.Log?.LogDebug("✅ Parte {i}/{total} importado: {fecha} - {cliente}", 
                        i + 1, total, item.FechaTrabajo, item.IdCliente);
                }
                catch (Exception ex)
                {
                    failed++;
                    App.Log?.LogWarning("❌ Error importando parte {i}/{total}: {error}", 
                        i + 1, total, ex.Message);
                }

                // Actualizar progreso
                ImportProgress.Value = i + 1;
                TxtProgressDetail.Text = $"{i + 1} / {total}";
                TxtProgress.Text = $"Importando... ({success} exitosos, {failed} fallidos)";

                // Pequeño delay para no saturar servidor
                await Task.Delay(100, ct);
            }

            App.Log?.LogInformation("✅ Importación completada:");
            App.Log?.LogInformation("   • Exitosos: {success}", success);
            App.Log?.LogInformation("   • Fallidos: {failed}", failed);
            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");

            // Mostrar resultado
            ProgressPanel.Visibility = Visibility.Collapsed;
            ResultPanel.Visibility = Visibility.Visible;
            TxtResult.Text = ct.IsCancellationRequested 
                ? "⚠️ Importación Cancelada" 
                : "✅ Importación Completada";
            TxtResultDetail.Text = $"Exitosos: {success}\nFallidos: {failed}\n\n" +
                                  (ct.IsCancellationRequested ? "Proceso interrumpido por el usuario." : "");

            ImportCompleted = success > 0;
            
            IsSecondaryButtonEnabled = false;
            CloseButtonText = "Cerrar";
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error crítico durante importación");
            
            ProgressPanel.Visibility = Visibility.Collapsed;
            ResultPanel.Visibility = Visibility.Visible;
            ResultPanel.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(255, 239, 68, 68));
            TxtResult.Text = "❌ Error de Importación";
            TxtResult.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(255, 239, 68, 68));
            TxtResultDetail.Text = ex.Message;
        }
        finally
        {
            _isImporting = false;
            _cts?.Dispose();
        }
    }

    /// <summary>Cancela la importación.</summary>
    private void OnCancelClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        _cts?.Cancel();
    }
}
