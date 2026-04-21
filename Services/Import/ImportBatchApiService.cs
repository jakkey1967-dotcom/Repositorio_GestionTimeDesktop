using GestionTime.Desktop.Models.Dtos.Import;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace GestionTime.Desktop.Services.Import;

/// <summary>Servicio que gestiona el flujo STAGING → VALIDATE → APPLY contra la API v2.</summary>
public sealed class ImportBatchApiService
{
    private readonly ILogger? _log;

    public ImportBatchApiService()
    {
        _log = App.Log;
    }

    // GL-BEGIN: Upload
    /// <summary>Sube el Excel a la API y crea el batch en staging (DRAFT).</summary>
    public async Task<ImportBatchCreateResponse> UploadAsync(
        string filePath,
        Guid targetUserId,
        string? notes = null,
        CancellationToken ct = default)
    {
        _log?.LogInformation("📤 IMPORT Upload: {file} → target={target}", Path.GetFileName(filePath), targetUserId);

        var fileName = Path.GetFileName(filePath);

        // GL-BEGIN: BuildMultipart
        var isXlsx = filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase);
        var fileBytes = await File.ReadAllBytesAsync(filePath, ct);
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            isXlsx
                ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                : "application/vnd.ms-excel");

        using var form = new MultipartFormDataContent();
        form.Add(fileContent, "file", fileName);
        form.Add(new StringContent(targetUserId.ToString()), "targetUserId");
        if (!string.IsNullOrEmpty(notes))
            form.Add(new StringContent(notes), "notes");
        // GL-END: BuildMultipart

        var result = await App.Api.PostMultipartAsync<ImportBatchCreateResponse>(
            "/api/v2/admin/import/batches", form, ct);

        _log?.LogInformation("✅ Batch creado: id={id} filas={rows} dups={dups}",
            result?.BatchId, result?.RowsLoaded, result?.DupsInFile);

        return result ?? throw new InvalidOperationException("No se recibió respuesta del servidor.");
    }
    // GL-END: Upload

    // GL-BEGIN: Validate
    /// <summary>Valida el batch contra la BD (marca OK / DUP_IN_DB / INVALID).</summary>
    public async Task<ImportBatchValidateResponse> ValidateAsync(long batchId, CancellationToken ct = default)
    {
        _log?.LogInformation("🔍 IMPORT Validate: batchId={id}", batchId);

        var result = await App.Api.PostAsync<object, ImportBatchValidateResponse>(
            $"/api/v2/admin/import/batches/{batchId}/validate", new { }, ct);

        _log?.LogInformation("✅ Validación: OK={ok} DupFile={df} DupDB={dd} Invalid={inv}",
            result?.RowsOk, result?.DupsInFile, result?.DupsInDb, result?.Invalid);

        return result ?? throw new InvalidOperationException("No se recibió respuesta del servidor.");
    }
    // GL-END: Validate

    // GL-BEGIN: Apply
    /// <summary>Aplica las filas OK del batch a partesdetrabajo via INSERT NOT EXISTS. Irreversible.</summary>
    public async Task<ImportBatchApplyResponse> ApplyAsync(long batchId, CancellationToken ct = default)
    {
        _log?.LogInformation("🚀 IMPORT Apply: batchId={id}", batchId);

        var result = await App.Api.PostAsync<object, ImportBatchApplyResponse>(
            $"/api/v2/admin/import/batches/{batchId}/apply", new { }, ct);

        _log?.LogInformation("✅ Apply: insertados={ins} skipped={skip}", result?.Inserted, result?.SkippedDups);

        return result ?? throw new InvalidOperationException("No se recibió respuesta del servidor.");
    }
    // GL-END: Apply
}
