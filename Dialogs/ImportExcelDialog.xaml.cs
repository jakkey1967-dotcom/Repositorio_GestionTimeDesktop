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
            IsSecondaryButtonEnabled = false;
            TxtFileName.Text = $"Cargando {System.IO.Path.GetFileName(filePath)}...";

            App.Log?.LogInformation("📂 Leyendo archivo Excel: {file}", System.IO.Path.GetFileName(filePath));

            var service = new ExcelPartesImportService();
            _importResult = await service.ReadExcelAsync(filePath, App.Log);

            App.Log?.LogInformation("✅ Archivo leído: {total} filas, {nuevos} nuevos, {duplicados} duplicados, {errors} errores", 
                _importResult.TotalRows, _importResult.ValidItems.Count, _importResult.ItemsToUpdate.Count, _importResult.Errors.Count);

            // Actualizar UI
            TxtFileName.Text = _importResult.FileName;
            TxtTotalRows.Text = _importResult.TotalRows.ToString();
            TxtValidRows.Text = $"{_importResult.ValidItems.Count} nuevos, {_importResult.ItemsToUpdate.Count} actualizaciones";
            TxtErrorRows.Text = _importResult.Errors.Count.ToString();

            SummaryPanel.Visibility = Visibility.Visible;

            // Mostrar errores si hay
            if (_importResult.Errors.Any())
            {
                ErrorListPanel.Visibility = Visibility.Visible;
                ErrorList.ItemsSource = _importResult.Errors.Take(20); // Máximo 20 errores
            }

            // Habilitar botón si hay válidos O items para actualizar
            IsPrimaryButtonEnabled = _importResult.ValidItems.Any() || _importResult.ItemsToUpdate.Any();
            IsSecondaryButtonEnabled = true;

            if (!_importResult.ValidItems.Any() && !_importResult.ItemsToUpdate.Any())
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
            else if (_importResult.ItemsToUpdate.Any())
            {
                // Mostrar advertencia si hay duplicados que se actualizarán
                var dialog = new ContentDialog
                {
                    Title = "🔄 Duplicados Detectados",
                    Content = $"Se encontraron {_importResult.ItemsToUpdate.Count} registros duplicados que serán ACTUALIZADOS.\n\n" +
                              $"• Nuevos: {_importResult.ValidItems.Count}\n" +
                              $"• Actualizaciones: {_importResult.ItemsToUpdate.Count}\n\n" +
                              $"¿Desea continuar?",
                    PrimaryButtonText = "Sí, Continuar",
                    CloseButtonText = "Cancelar",
                    XamlRoot = this.XamlRoot,
                    RequestedTheme = ElementTheme.Dark
                };
                var result = await dialog.ShowAsync();
                if (result != ContentDialogResult.Primary)
                {
                    IsPrimaryButtonEnabled = false;
                }
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
            
            throw; // Re-lanzar para que DiarioPage lo capture
        }
    }

    /// <summary>Ejecuta la importación al backend.</summary>
    private async void OnImportClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (_importResult == null || (_importResult.ValidItems.Count == 0 && _importResult.ItemsToUpdate.Count == 0) || _isImporting)
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

            var totalNuevos = _importResult.ValidItems.Count;
            var totalActualizaciones = _importResult.ItemsToUpdate.Count;
            var total = totalNuevos + totalActualizaciones;
            var success = 0;
            var updated = 0;
            var failed = 0;

            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
            App.Log?.LogInformation("🚀 IMPORTACIÓN MASIVA - Iniciando");
            App.Log?.LogInformation("   Total a importar: {total} ({nuevos} nuevos + {actualizaciones} actualizaciones)", 
                total, totalNuevos, totalActualizaciones);

            ImportProgress.Maximum = total;
            
            int currentIndex = 0;

            // ========== PASO 1: CREAR NUEVOS PARTES ==========
            for (int i = 0; i < totalNuevos; i++)
            {
                if (ct.IsCancellationRequested)
                {
                    App.Log?.LogWarning("Importación cancelada por el usuario en item {i}/{total}", currentIndex + 1, total);
                    break;
                }

                var item = _importResult.ValidItems[i];

                try
                {
                    App.Log?.LogDebug("✨ [{index}/{total}] CREANDO parte nuevo...", currentIndex + 1, total);
                    App.Log?.LogDebug("   Fecha: {fecha}, Cliente: {cliente}, Acción: {accion}", 
                        item.FechaTrabajo, item.IdCliente, (item.Accion?.Length ?? 0) > 50 ? item.Accion!.Substring(0, 50) + "..." : item.Accion);

                    // 1️⃣ POST a /api/v1/partes
                    var response = await App.Api.PostAsync<Models.Dtos.ParteCreateRequest, Models.Dtos.ParteDto>("/api/v1/partes", item, ct);
                    
                    if (response != null && response.Id > 0)
                    {
                        // 2️⃣ PUT para actualizar estado a Cerrado (2)
                        try
                        {
                            var updatePayload = new Models.Dtos.ParteCreateRequest
                            {
                                FechaTrabajo = item.FechaTrabajo,
                                HoraInicio = item.HoraInicio,
                                HoraFin = item.HoraFin,
                                DuracionMin = item.DuracionMin,
                                IdCliente = item.IdCliente,
                                Tienda = item.Tienda,
                                IdGrupo = item.IdGrupo,
                                IdTipo = item.IdTipo,
                                Accion = item.Accion ?? string.Empty,
                                Ticket = item.Ticket,
                                Tecnico = item.Tecnico,
                                Estado = 2
                            };
                            
                            await App.Api.PutAsync<Models.Dtos.ParteCreateRequest, object>($"/api/v1/partes/{response.Id}", updatePayload, ct);
                            App.Log?.LogInformation("✅ [{index}/{total}] Parte CREADO (ID: {id})", currentIndex + 1, total, response.Id);
                        }
                        catch (Exception updateEx)
                        {
                            App.Log?.LogWarning("⚠️ Parte {id} creado pero fallo al actualizar estado: {error}", response.Id, updateEx.Message);
                        }
                    }
                    
                    success++;
                }
                catch (Services.ApiException apiEx)
                {
                    failed++;
                    App.Log?.LogWarning("❌ [{index}/{total}] Error CREANDO parte:", currentIndex + 1, total);
                    App.Log?.LogWarning("   StatusCode: {code}, Message: {msg}", apiEx.StatusCode, apiEx.Message);
                }
                catch (Exception ex)
                {
                    failed++;
                    App.Log?.LogWarning("❌ [{index}/{total}] Error inesperado CREANDO: {error}", currentIndex + 1, total, ex.Message);
                }

                currentIndex++;
                ImportProgress.Value = currentIndex;
                TxtProgressDetail.Text = $"{currentIndex} / {total}";
                TxtProgress.Text = $"Importando... ({success} creados, {updated} actualizados, {failed} fallidos)";
                await Task.Delay(100, ct);
            }
            
            // ========== PASO 2: ACTUALIZAR DUPLICADOS ==========
            for (int i = 0; i < totalActualizaciones; i++)
            {
                if (ct.IsCancellationRequested)
                {
                    App.Log?.LogWarning("Importación cancelada por el usuario en item {i}/{total}", currentIndex + 1, total);
                    break;
                }

                var updateItem = _importResult.ItemsToUpdate[i];
                var item = updateItem.Data;
                var parteId = updateItem.ParteId;

                try
                {
                    App.Log?.LogDebug("🔄 [{index}/{total}] ACTUALIZANDO parte existente ID={id}...", currentIndex + 1, total, parteId);
                    App.Log?.LogDebug("   Fecha: {fecha}, Cliente: {cliente}, Acción: {accion}", 
                        item.FechaTrabajo, item.IdCliente, (item.Accion?.Length ?? 0) > 50 ? item.Accion!.Substring(0, 50) + "..." : item.Accion);

                    // PUT a /api/v1/partes/{id}
                    var updatePayload = new Models.Dtos.ParteCreateRequest
                    {
                        FechaTrabajo = item.FechaTrabajo,
                        HoraInicio = item.HoraInicio,
                        HoraFin = item.HoraFin,
                        DuracionMin = item.DuracionMin,
                        IdCliente = item.IdCliente,
                        Tienda = item.Tienda,
                        IdGrupo = item.IdGrupo,
                        IdTipo = item.IdTipo,
                        Accion = item.Accion ?? string.Empty,
                        Ticket = item.Ticket,
                        Tecnico = item.Tecnico,
                        Estado = 2
                    };
                    
                    await App.Api.PutAsync<Models.Dtos.ParteCreateRequest, object>($"/api/v1/partes/{parteId}", updatePayload, ct);
                    
                    App.Log?.LogInformation("✅ [{index}/{total}] Parte ACTUALIZADO (ID: {id})", currentIndex + 1, total, parteId);
                    updated++;
                }
                catch (Services.ApiException apiEx)
                {
                    failed++;
                    App.Log?.LogWarning("❌ [{index}/{total}] Error ACTUALIZANDO parte ID={id}:", currentIndex + 1, total, parteId);
                    App.Log?.LogWarning("   StatusCode: {code}, Message: {msg}", apiEx.StatusCode, apiEx.Message);
                }
                catch (Exception ex)
                {
                    failed++;
                    App.Log?.LogWarning("❌ [{index}/{total}] Error inesperado ACTUALIZANDO ID={id}: {error}", currentIndex + 1, total, parteId, ex.Message);
                }

                currentIndex++;
                ImportProgress.Value = currentIndex;
                TxtProgressDetail.Text = $"{currentIndex} / {total}";
                TxtProgress.Text = $"Importando... ({success} creados, {updated} actualizados, {failed} fallidos)";
                await Task.Delay(100, ct);
            }

            App.Log?.LogInformation("✅ Importación completada:");
            App.Log?.LogInformation("   • Creados: {creados}", success);
            App.Log?.LogInformation("   • Actualizados: {actualizados}", updated);
            App.Log?.LogInformation("   • Fallidos: {failed}", failed);
            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");

            // Mostrar resultado
            ProgressPanel.Visibility = Visibility.Collapsed;
            ResultPanel.Visibility = Visibility.Visible;
            TxtResult.Text = ct.IsCancellationRequested 
                ? "⚠️ Importación Cancelada" 
                : "✅ Importación Completada";
            TxtResultDetail.Text = $"Creados: {success}\nActualizados: {updated}\nFallidos: {failed}\n\n" +
                                  (ct.IsCancellationRequested ? "Proceso interrumpido por el usuario." : "");

            ImportCompleted = success > 0 || updated > 0;
            
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
        try
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }
        }
        catch (ObjectDisposedException)
        {
            // CTS ya fue disposed, ignorar
        }
    }
}
