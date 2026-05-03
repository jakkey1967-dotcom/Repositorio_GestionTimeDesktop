using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GestionTime.Desktop.Services.Import;

/// <summary>Servicio de importación Excel ADMIN: valida, dedupe y aplica partes para cualquier agente.</summary>
public sealed class AdminImportService
{
    private readonly ILogger<AdminImportService>? _log;

    public AdminImportService()
    {
        _log = App.LogFactory?.CreateLogger<AdminImportService>();
    }

    // GL-BEGIN: ParseAndValidate
    /// <summary>Sube el Excel a la API v2 y valida el batch. Devuelve el resumen con estados por fila.</summary>
    public async Task<ImportSummary> ParseAndValidateAsync(
        string filePath,
        Guid targetUserId,
        CancellationToken ct = default)
    {
        _log?.LogInformation("[AdminImport] Upload+Validate: {file} para usuario {uid}", Path.GetFileName(filePath), targetUserId);

        var svc = new ImportBatchApiService();

        // 1) Upload
        var uploadResult = await svc.UploadAsync(filePath, targetUserId, notes: null, ct);
        _log?.LogInformation("[AdminImport] Batch creado: id={id} filas={rows} dups={dups}",
            uploadResult.BatchId, uploadResult.RowsLoaded, uploadResult.DupsInFile);

        // 2) Validate
        var validateResult = await svc.ValidateAsync(uploadResult.BatchId, ct);
        _log?.LogInformation("[AdminImport] Validación: ok={ok} dupFile={df} dupDB={dd} invalid={inv}",
            validateResult.RowsOk, validateResult.DupsInFile, validateResult.DupsInDb, validateResult.Invalid);

        // 3) Mapear a ImportSummary para la UI
        var summary = new ImportSummary
        {
            BatchId   = uploadResult.BatchId,
            Ok        = validateResult.RowsOk,
            DupInFile = validateResult.DupsInFile,
            DupExists = validateResult.DupsInDb,
            Invalid   = validateResult.Invalid,
        };

        foreach (var row in validateResult.Rows)
        {
            var status = row.ValidationStatus switch
            {
                "OK"         => ImportRowStatus.OK,
                "DUP_IN_FILE"=> ImportRowStatus.DUP_IN_FILE,
                "DUP_IN_DB"  => ImportRowStatus.DUP_EXISTS,
                _            => ImportRowStatus.INVALID
            };

            summary.Rows.Add(new ImportPreviewRow
            {
                RowNumber = row.RowNumber,
                Fecha     = row.FechaTrabajo,
                Inicio    = row.HoraInicio,
                Fin       = row.HoraFin,
                Cliente   = row.IdCliente.ToString(),
                Ticket    = row.Ticket,
                Accion    = row.Accion,
                Grupo     = row.IdGrupo?.ToString(),
                Tipo      = row.IdTipo?.ToString(),
                Status    = status,
                Mensaje   = status == ImportRowStatus.OK ? "Lista para importar" : (row.ValidationError ?? status.ToString()),
                DedupeKey = string.Empty
            });
        }

        summary.Total = summary.Rows.Count;

        return summary;
    }
    // GL-END: ParseAndValidate

    // GL-BEGIN: ApplyImport
    /// <summary>Importa las filas OK del batch subido via API v2 admin import.</summary>
    public async Task<(int imported, int failed)> ApplyImportAsync(
        ImportSummary summary,
        Guid targetUserId,
        CancellationToken ct = default)
    {
        var svc = new ImportBatchApiService();

        // 1) Subir el Excel (si ya tenemos batchId en el summary, reutilizarlo)
        if (summary.BatchId <= 0)
            throw new InvalidOperationException("El batch no fue subido correctamente. Vuelve a validar.");

        // 2) Apply directo contra el endpoint nativo
        _log?.LogInformation("[AdminImport] Apply batchId={id} para agente {uid}", summary.BatchId, targetUserId);

        var applyResult = await svc.ApplyAsync(summary.BatchId, ct);

        _log?.LogInformation("[AdminImport] Insertados={ok} Skipped={skip}", applyResult.Inserted, applyResult.SkippedDups);
        return (applyResult.Inserted, applyResult.SkippedDups);
    }
    // GL-END: ApplyImport

    // GL-BEGIN: CsvReport
    /// <summary>Genera el CSV de reporte con resultados por fila.</summary>
    public string GenerateCsvReport(ImportSummary summary, string agentName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("RowNumber,Fecha,Inicio,Fin,Cliente,Ticket,Accion,Resultado,Mensaje");

        foreach (var row in summary.Rows)
        {
            var cols = new[]
            {
                row.RowNumber.ToString(),
                CsvEscape(row.Fecha),
                CsvEscape(row.Inicio),
                CsvEscape(row.Fin),
                CsvEscape(row.Cliente),
                CsvEscape(row.Ticket),
                CsvEscape(row.Accion),
                row.Status.ToString(),
                CsvEscape(row.Mensaje)
            };
            sb.AppendLine(string.Join(",", cols));
        }

        return sb.ToString();
    }

    public string GetReportFileName(string agentName)
    {
        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
        var safe = string.Join("_", agentName.Split(Path.GetInvalidFileNameChars()));
        return $"import_result_{stamp}_{safe}.csv";
    }
    // GL-END: CsvReport

    // GL-BEGIN: Helpers
    private static string CsvEscape(string? val)
    {
        if (val == null) return "";
        if (val.Contains(',') || val.Contains('"') || val.Contains('\n'))
            return $"\"{val.Replace("\"", "\"\"")}\"";
        return val;
    }
    // GL-END: Helpers
}
