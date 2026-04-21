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

/// <summary>Dialogo para importar partes desde Excel via API staging (UPLOAD -> VALIDATE -> APPLY).</summary>
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
    /// <summary>Guarda la ruta del archivo. La subida real ocurre al pulsar Importar.</summary>
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

            // FASE 1: UPLOAD
            TxtProgress.Text = "Subiendo archivo al servidor...";
            TxtProgressDetail.Text = "Fase 1 / 3";
            ImportProgress.IsIndeterminate = true;

            _uploadResult = await service.UploadAsync(_filePath, _selectedTargetUserId, null, ct);
            App.Log?.LogInformation("Upload OK: batch={id} filas={rows}", _uploadResult.BatchId, _uploadResult.RowsLoaded);

            // FASE 2: VALIDATE
            TxtProgress.Text = "Validando contra base de datos...";
            TxtProgressDetail.Text = "Fase 2 / 3";

            _validateResult = await service.ValidateAsync(_uploadResult.BatchId, ct);
            App.Log?.LogInformation("Validate OK: ok={ok} dupDB={dd} invalid={inv}", _validateResult.RowsOk, _validateResult.DupsInDb, _validateResult.Invalid);

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

            if (_validateResult.DupsInDb > 0 || _validateResult.Invalid > 0)
            {
                ImportProgress.IsIndeterminate = false;
                ProgressPanel.Visibility = Visibility.Collapsed;
                ShowValidationSummary();

                var confirm = new ContentDialog
                {
                    Title = "Confirmar importacion",
                    Content = $"Se importaran {_validateResult.RowsOk} partes nuevos.\n" +
                              $"{_validateResult.DupsInDb} ya existen en la BD y seran omitidos.\n" +
                              $"{_validateResult.Invalid} filas tienen errores y seran omitidas.\n\nDeseas continuar?",
                    PrimaryButtonText = "Importar",
                    CloseButtonText = "Cancelar",
                    XamlRoot = this.XamlRoot
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

            // FASE 3: APPLY
            TxtProgress.Text = "Aplicando partes a la base de datos...";
            TxtProgressDetail.Text = "Fase 3 / 3";
            ImportProgress.IsIndeterminate = true;

            var applyResult = await service.ApplyAsync(_uploadResult.BatchId, ct);

            ImportProgress.IsIndeterminate = false;
            ProgressPanel.Visibility = Visibility.Collapsed;
            ResultPanel.Visibility = Visibility.Visible;
            TxtResult.Text = "Importacion completada";
            TxtResultDetail.Text =
                $"Partes insertados: {applyResult.Inserted}\n" +
                $"Duplicados omitidos: {applyResult.SkippedDups}\n" +
                $"Filas con error o invalidas: {_validateResult.Invalid}";

            ImportCompleted = applyResult.Inserted > 0;
        }
        catch (OperationCanceledException)
        {
            ImportProgress.IsIndeterminate = false;
            ProgressPanel.Visibility = Visibility.Collapsed;
            ResultPanel.Visibility = Visibility.Visible;
            TxtResult.Text = "Cancelado";
            TxtResultDetail.Text = "La operacion fue cancelada por el usuario.";
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error en importacion Excel API");
            ImportProgress.IsIndeterminate = false;
            ProgressPanel.Visibility = Visibility.Collapsed;
            ResultPanel.Visibility = Visibility.Visible;
            TxtResult.Text = "Error de importacion";
            TxtResult.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(255, 239, 68, 68));
            TxtResultDetail.Text = TraducirErrorServidor(ex.Message);
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

    // GL-BEGIN: TraducirError
    private static string TraducirErrorServidor(string mensaje)
    {
        if (string.IsNullOrWhiteSpace(mensaje))
            return "Error desconocido. Revisa la conexion con el servidor.";

        if (mensaje.Contains("\"error\""))
        {
            try
            {
                var doc = System.Text.Json.JsonDocument.Parse(mensaje);
                if (doc.RootElement.TryGetProperty("error", out var errProp))
                    return errProp.GetString() ?? mensaje;
            }
            catch { }
        }

        if (mensaje.Contains("Central Directory") || mensaje.Contains("End of Central"))
            return "El archivo Excel esta danado o no es un .xlsx valido. Vuelve a guardarlo desde Excel e intentalo de nuevo.";
        if (mensaje.Contains("file type") || mensaje.Contains("ExcelType"))
            return "Formato de archivo no reconocido. Asegurate de subir un archivo .xlsx.";
        if (mensaje.Contains("Unauthorized") || mensaje.Contains("401"))
            return "No tienes permiso para realizar esta operacion. Vuelve a iniciar sesion.";
        if (mensaje.Contains("timeout") || mensaje.Contains("TaskCanceled"))
            return "La operacion tardo demasiado. Verifica tu conexion e intentalo de nuevo.";
        if (mensaje.Contains("500") || mensaje.Contains("Internal Server"))
            return "Error interno del servidor. Intentalo de nuevo en unos minutos.";
        if (mensaje.Contains("No such host") || mensaje.Contains("SocketException") || mensaje.Contains("HttpRequest"))
            return "No se puede conectar al servidor. Verifica tu conexion a internet.";

        return mensaje;
    }
    // GL-END: TraducirError

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
                Reason = TraducirEstadoFila(r.ValidationStatus, r.ValidationError),
                RawData = $"{r.FechaTrabajo} {r.HoraInicio}-{r.HoraFin} Cliente:{r.IdCliente}"
            }).ToList();

        if (errors.Any())
        {
            ErrorListPanel.Visibility = Visibility.Visible;
            ErrorList.ItemsSource = errors.Take(20);
        }
    }

    private static string TraducirEstadoFila(string? estado, string? errorDetalle)
    {
        var descripcion = estado switch
        {
            "DUP_IN_FILE" => "Duplicado dentro del Excel",
            "DUP_IN_DB"   => "Ya existe en la base de datos",
            "INVALID"     => "Fila invalida",
            _             => estado ?? "Desconocido"
        };
        return string.IsNullOrWhiteSpace(errorDetalle)
            ? descripcion
            : $"{descripcion}: {errorDetalle}";
    }

    private void OnCancelClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        try { _cts?.Cancel(); } catch (ObjectDisposedException) { }
    }
}
