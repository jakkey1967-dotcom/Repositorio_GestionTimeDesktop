using GestionTime.Desktop.Models.Dtos;
using GestionTime.Desktop.Models.Dtos.Import;
using GestionTime.Desktop.Services.Import;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GestionTime.Desktop.Dialogs;

/// <summary>DiÃ¡logo para importar partes desde Excel via API staging (UPLOAD â†’ VALIDATE â†’ APPLY).</summary>
public sealed partial class ImportExcelDialog : ContentDialog
{
    private string? _filePath;
    private ImportBatchCreateResponse? _uploadResult;
    private ImportBatchValidateResponse? _validateResult;
    private CancellationTokenSource? _cts;
    private bool _isWorking;
    private Guid _selectedTargetUserId;

    public bool ImportCompleted { get; private set; }

    public ImportExcelDialog()
    {
        this.InitializeComponent();
        IsPrimaryButtonEnabled = false;
        _ = LoadUsersAsync();
    }

    // GL-BEGIN: LoadUsers
    private async Task LoadUsersAsync()
    {
        try
        {
            var response = await App.Api.GetAsync<UsersPagedResponse>("/api/v1/users?pageSize=200", default);
            var users = response?.Users ?? new List<UserListItemDto>();

            CmbTargetUser.ItemsSource = users;
            CmbTargetUser.DisplayMemberPath = "FullName";
        }
        catch (Exception ex)
        {
            App.Log?.LogWarning(ex, "No se pudieron cargar usuarios para selector destino");
        }
    }

    private void OnTargetUserChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbTargetUser.SelectedItem is UserListItemDto u)
        {
            _selectedTargetUserId = u.Id;
            UpdatePrimaryButton();
        }
    }
    // GL-END: LoadUsers

    // GL-BEGIN: LoadFile
    /// <summary>Guarda la ruta del archivo y muestra el nombre. La subida real ocurre al pulsar Importar.</summary>
    public Task LoadFileAsync(string filePath)
    {
        _filePath = filePath;
        TxtFileName.Text = System.IO.Path.GetFileName(filePath);
        UpdatePrimaryButton();
        return Task.CompletedTask;
    }
    // GL-END: LoadFile

    private void UpdatePrimaryButton()
    {
        IsPrimaryButtonEnabled = !_isWorking
            && !string.IsNullOrEmpty(_filePath)
            && _selectedTargetUserId != Guid.Empty;
    }

    // GL-BEGIN: Import (3 fases)
    private async void OnImportClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        args.Cancel = true;
        if (_isWorking || _filePath == null || _selectedTargetUserId == Guid.Empty) return;

        _isWorking = true;
        IsPrimaryButtonEnabled = false;
        IsSecondaryButtonEnabled = true;
        SecondaryButtonText = "Cancelar";
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        SummaryPanel.Visibility = Visibility.Collapsed;
        ProgressPanel.Visibility = Visibility.Visible;

        try
        {
            var service = new ImportBatchApiService();

            // â”€â”€ FASE 1: UPLOAD â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            TxtProgress.Text = "â¬†ï¸ Subiendo archivo al servidor...";
            TxtProgressDetail.Text = "Fase 1 / 3";
            ImportProgress.IsIndeterminate = true;

            _uploadResult = await service.UploadAsync(_filePath, _selectedTargetUserId, null, ct);

            App.Log?.LogInformation("âœ… Upload OK: batch={id} filas={rows} dups={dups}",
                _uploadResult.BatchId, _uploadResult.RowsLoaded, _uploadResult.DupsInFile);

            // â”€â”€ FASE 2: VALIDATE â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            TxtProgress.Text = "ðŸ” Validando contra base de datos...";
            TxtProgressDetail.Text = "Fase 2 / 3";

            _validateResult = await service.ValidateAsync(_uploadResult.BatchId, ct);

            App.Log?.LogInformation("âœ… Validate OK: ok={ok} dupDB={dd} invalid={inv}",
                _validateResult.RowsOk, _validateResult.DupsInDb, _validateResult.Invalid);

            // Si no hay filas OK, mostrar resumen y no aplicar
            if (_validateResult.RowsOk == 0)
            {
                ImportProgress.IsIndeterminate = false;
                ProgressPanel.Visibility = Visibility.Collapsed;
                ShowValidationSummary();
                _isWorking = false;
                IsSecondaryButtonEnabled = false;
                CloseButtonText = "Cerrar";
                return;
            }

            // ConfirmaciÃ³n si hay dups en DB
            if (_validateResult.DupsInDb > 0 || _validateResult.Invalid > 0)
            {
                ImportProgress.IsIndeterminate = false;
                ProgressPanel.Visibility = Visibility.Collapsed;
                ShowValidationSummary();

                var confirm = new ContentDialog
                {
                    Title = "ðŸ“‹ Resumen de ValidaciÃ³n",
                    Content = $"âœ… Filas listas para importar: {_validateResult.RowsOk}\n" +
                              $"ðŸ” Duplicados en BD (se omitirÃ¡n): {_validateResult.DupsInDb}\n" +
                              $"âŒ InvÃ¡lidas: {_validateResult.Invalid}\n\n" +
                              $"Â¿Deseas aplicar las {_validateResult.RowsOk} filas vÃ¡lidas?",
                    PrimaryButtonText = $"Aplicar {_validateResult.RowsOk} partes",
                    CloseButtonText = "Cancelar",
                    XamlRoot = this.XamlRoot,
                    RequestedTheme = ElementTheme.Dark
                };
                var choice = await confirm.ShowAsync();
                if (choice != ContentDialogResult.Primary)
                {
                    _isWorking = false;
                    IsSecondaryButtonEnabled = false;
                    CloseButtonText = "Cerrar";
                    return;
                }

                ProgressPanel.Visibility = Visibility.Visible;
            }

            // â”€â”€ FASE 3: APPLY â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            TxtProgress.Text = "ðŸš€ Aplicando partes a la base de datos...";
            TxtProgressDetail.Text = "Fase 3 / 3";
            ImportProgress.IsIndeterminate = true;

            var applyResult = await service.ApplyAsync(_uploadResult.BatchId, ct);

            ImportProgress.IsIndeterminate = false;
            ProgressPanel.Visibility = Visibility.Collapsed;
            ResultPanel.Visibility = Visibility.Visible;
            TxtResult.Text = "âœ… ImportaciÃ³n Completada";
            TxtResultDetail.Text =
                $"â€¢ Partes insertados: {applyResult.Inserted}\n" +
                $"â€¢ Duplicados omitidos: {applyResult.SkippedDups}\n" +
                $"â€¢ Filas con error/invÃ¡lidas: {_validateResult.Invalid}";

            ImportCompleted = applyResult.Inserted > 0;
        }
        catch (OperationCanceledException)
        {
            ImportProgress.IsIndeterminate = false;
            ProgressPanel.Visibility = Visibility.Collapsed;
            ResultPanel.Visibility = Visibility.Visible;
            TxtResult.Text = "âš ï¸ Cancelado";
            TxtResultDetail.Text = "La operaciÃ³n fue cancelada por el usuario.";
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error en importaciÃ³n Excel API");
            ImportProgress.IsIndeterminate = false;
            ProgressPanel.Visibility = Visibility.Collapsed;
            ResultPanel.Visibility = Visibility.Visible;
            TxtResult.Text = "âŒ Error de ImportaciÃ³n";
            TxtResult.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(255, 239, 68, 68));
            TxtResultDetail.Text = ex.Message;
        }
        finally
        {
            _isWorking = false;
            IsSecondaryButtonEnabled = false;
            CloseButtonText = "Cerrar";
            _cts?.Dispose();
        }
    }
    // GL-END: Import

    private void ShowValidationSummary()
    {
        if (_validateResult == null) return;
        SummaryPanel.Visibility = Visibility.Visible;
        TxtTotalRows.Text = _validateResult.TotalRows.ToString();
        TxtValidRows.Text = $"{_validateResult.RowsOk} OK  |  {_validateResult.DupsInDb} dup BD  |  {_validateResult.DupsInFile} dup archivo";
        TxtErrorRows.Text = _validateResult.Invalid.ToString();

        var errors = _validateResult.Rows
            .Where(r => r.ValidationStatus != "OK")
            .Select(r => new ImportError
            {
                RowIndex = r.RowNumber,
                Reason = $"[{r.ValidationStatus}] {r.ValidationError}",
                RawData = $"{r.FechaTrabajo} {r.HoraInicio}-{r.HoraFin} Cliente:{r.IdCliente}"
            }).ToList();

        if (errors.Any())
        {
            ErrorListPanel.Visibility = Visibility.Visible;
            ErrorList.ItemsSource = errors.Take(20);
        }
    }

    private void OnCancelClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        try { _cts?.Cancel(); } catch (ObjectDisposedException) { }
    }
}
