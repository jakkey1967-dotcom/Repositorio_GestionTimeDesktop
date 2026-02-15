using System;
using System.Threading;
using System.Threading.Tasks;
using GestionTime.Desktop.Models.Dtos.Reports;
using Microsoft.Extensions.Logging;

namespace GestionTime.Desktop.Services.Reports;

/// <summary>Servicio para consultar informes de partes (endpoint /api/v2/informes/resumen).</summary>
public class InformesService
{
    private readonly ApiClient _api;
    private readonly ILogger? _log;

    public InformesService(ApiClient api)
    {
        _api = api;
        _log = App.Log;
    }

    /// <summary>Obtiene resumen de informes según scope.</summary>
    /// <param name="scope">day, week, range</param>
    /// <param name="date">YYYY-MM-DD (solo si scope=day)</param>
    /// <param name="weekIso">YYYY-Www (solo si scope=week)</param>
    /// <param name="from">YYYY-MM-DD (solo si scope=range)</param>
    /// <param name="to">YYYY-MM-DD (solo si scope=range)</param>
    /// <param name="agentId">Opcional (GUID string), para filtrar por agente específico</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    public async Task<InformeResumenDto?> GetResumenAsync(
        string scope,
        string? date = null,
        string? weekIso = null,
        string? from = null,
        string? to = null,
        string? agentId = null,
        CancellationToken cancellationToken = default)
    {
        _log?.LogInformation("📊 [InformesService] Iniciando GetResumenAsync - Scope: {scope}, Date: {date}, WeekIso: {weekIso}, From: {from}, To: {to}, AgentId: {agentId}",
            scope, date, weekIso, from, to, agentId);

        var queryParams = new System.Collections.Generic.List<string>
        {
            $"scope={Uri.EscapeDataString(scope)}"
        };

        if (!string.IsNullOrWhiteSpace(date))
            queryParams.Add($"date={Uri.EscapeDataString(date)}");

        if (!string.IsNullOrWhiteSpace(weekIso))
            queryParams.Add($"weekIso={Uri.EscapeDataString(weekIso)}");

        if (!string.IsNullOrWhiteSpace(from))
            queryParams.Add($"from={Uri.EscapeDataString(from)}");

        if (!string.IsNullOrWhiteSpace(to))
            queryParams.Add($"to={Uri.EscapeDataString(to)}");

        if (!string.IsNullOrWhiteSpace(agentId))
            queryParams.Add($"agentId={Uri.EscapeDataString(agentId)}");

        var query = string.Join("&", queryParams);
        var endpoint = $"/api/v2/informes/resumen?{query}";

        _log?.LogInformation("📊 [InformesService] Endpoint construido: {endpoint}", endpoint);

        var result = await _api.GetAsync<InformeResumenDto>(endpoint, cancellationToken);

        if (result != null)
        {
            _log?.LogInformation("📊 [InformesService] Respuesta recibida - Partes: {partes}, Registrado: {recorded}min, Real: {covered}min, Solape: {overlap}min, Inicio: {start}, Fin: {end}",
                result.PartsCount, result.RecordedMinutes, result.CoveredMinutes, result.OverlapMinutes, result.FirstStart, result.LastEnd);

            if (result.MergedIntervals?.Count > 0)
            {
                _log?.LogInformation("📊 [InformesService] Intervalos cubiertos: {count}", result.MergedIntervals.Count);
                foreach (var interval in result.MergedIntervals)
                {
                    _log?.LogInformation("  ↳ {start} - {end} ({minutes}min)", interval.Start, interval.End, interval.Minutes);
                }
            }
        }
        else
        {
            _log?.LogWarning("⚠️ [InformesService] La respuesta es null");
        }

        return result;
    }
}
