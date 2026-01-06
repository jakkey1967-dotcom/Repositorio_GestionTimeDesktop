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
                    // 🆕 NUEVO: Log detallado del item ANTES de enviar
                    App.Log?.LogDebug("═══ Importando item {i}/{total} ═══", i + 1, total);
                    App.Log?.LogDebug("  FechaTrabajo: {fecha}", item.FechaTrabajo);
                    App.Log?.LogDebug("  IdCliente: {id}", item.IdCliente);
                    App.Log?.LogDebug("  Tienda: '{tienda}'", item.Tienda ?? "(null)");
                    App.Log?.LogDebug("  HoraInicio: {inicio}", item.HoraInicio);
                    App.Log?.LogDebug("  HoraFin: {fin}", item.HoraFin ?? "(null)");
                    App.Log?.LogDebug("  DuracionMin: {duracion}", item.DuracionMin?.ToString() ?? "(null)");
                    App.Log?.LogDebug("  Accion: '{accion}'", item.Accion?.Length > 50 ? item.Accion.Substring(0, 50) + "..." : item.Accion);
                    App.Log?.LogDebug("  Ticket: '{ticket}'", item.Ticket ?? "(null)");
                    App.Log?.LogDebug("  IdGrupo: {id}", item.IdGrupo?.ToString() ?? "(null)");
                    App.Log?.LogDebug("  IdTipo: {id}", item.IdTipo?.ToString() ?? "(null)");
                    App.Log?.LogDebug("  Estado: {estado}", item.Estado);

                    // POST a /api/v1/partes
                    var response = await App.Api.PostAsync<Models.Dtos.ParteCreateRequest, object>("/api/v1/partes", item, ct);
                    success++;
                    
                    App.Log?.LogDebug("✅ Parte {i}/{total} importado correctamente", i + 1, total);
                }
                catch (Services.ApiException apiEx)
                {
                    failed++;
                    App.Log?.LogWarning("❌ Error importando parte {i}/{total}:", i + 1, total);
                    App.Log?.LogWarning("   • StatusCode: {code}", apiEx.StatusCode);
                    App.Log?.LogWarning("   • Message: {msg}", apiEx.Message);
                    App.Log?.LogWarning("   • ServerMessage: {serverMsg}", apiEx.ServerMessage ?? "(null)");
                    App.Log?.LogWarning("   • ServerError: {serverError}", apiEx.ServerError ?? "(null)");
                    
                    // 🆕 NUEVO: Log del payload que causó el error
                    App.Log?.LogWarning("   📦 Payload que falló:");
                    App.Log?.LogWarning("      - FechaTrabajo: {fecha}", item.FechaTrabajo);
                    App.Log?.LogWarning("      - IdCliente: {id}", item.IdCliente);
                    App.Log?.LogWarning("      - Accion: {accion}", item.Accion?.Length > 100 ? item.Accion.Substring(0, 100) + "..." : item.Accion);
                }
                catch (Exception ex)
                {
                    failed++;
                    App.Log?.LogWarning("❌ Error inesperado importando parte {i}/{total}:", i + 1, total);
                    App.Log?.LogWarning("   • Exception: {type}", ex.GetType().Name);
                    App.Log?.LogWarning("   • Message: {error}", ex.Message);
                    App.Log?.LogWarning("   • StackTrace: {stack}", ex.StackTrace?.Split('\n').FirstOrDefault() ?? "(no stack)");
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
