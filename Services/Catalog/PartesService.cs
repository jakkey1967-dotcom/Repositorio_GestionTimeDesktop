using GestionTime.Desktop.Models.Dtos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GestionTime.Desktop.Services.Catalog;

/// <summary>Servicio para gestionar Partes usando /api/v1/partes con filtros avanzados</summary>
public sealed class PartesService
{
    private readonly ApiClient _api;
    private readonly ILogger _log;

    public PartesService(ApiClient api, ILogger logger)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _log = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Listar partes con filtros avanzados</summary>
    public async Task<List<ParteDto>?> ListAsync(
        DateTime? fecha = null,
        DateTime? fechaInicio = null,
        DateTime? fechaFin = null,
        string? search = null,
        int? estado = null,
        int? idCliente = null,
        int? idTipo = null,
        int? idGrupo = null,
        CancellationToken ct = default)
    {
        try
        {
            var queryParams = new List<string>();

            // FILTROS DE FECHA
            if (fecha.HasValue)
            {
                queryParams.Add($"fecha={fecha.Value:yyyy-MM-dd}");
            }
            else
            {
                if (fechaInicio.HasValue)
                    queryParams.Add($"fechaInicio={fechaInicio.Value:yyyy-MM-dd}");
                
                if (fechaFin.HasValue)
                    queryParams.Add($"fechaFin={fechaFin.Value:yyyy-MM-dd}");
            }

            // BÚSQUEDA POR TEXTO
            if (!string.IsNullOrWhiteSpace(search))
            {
                queryParams.Add($"q={Uri.EscapeDataString(search)}");
            }

            // FILTROS POR IDs
            if (estado.HasValue)
                queryParams.Add($"estado={estado.Value}");
            
            if (idCliente.HasValue)
                queryParams.Add($"id_cliente={idCliente.Value}");
            
            if (idTipo.HasValue)
                queryParams.Add($"id_tipo={idTipo.Value}");
            
            if (idGrupo.HasValue)
                queryParams.Add($"id_grupo={idGrupo.Value}");

            var path = queryParams.Count > 0 
                ? $"/api/v1/partes?{string.Join("&", queryParams)}"
                : "/api/v1/partes";
            
            _log.LogInformation("📋 Listando partes - Filtros: {filtros}", string.Join(", ", queryParams));

            var result = await _api.GetAsync<List<ParteDto>>(path, ct);

            if (result != null)
            {
                _log.LogInformation("✅ {count} partes cargados", result.Count);
            }

            return result;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "❌ Error listando partes");
            throw;
        }
    }

    /// <summary>Obtener un parte por ID - ⚠️ ADVERTENCIA: Endpoint NO soportado por backend (405)</summary>
    [Obsolete("El backend NO soporta GET /partes/{id} - Devuelve 405 Method Not Allowed")]
    public async Task<ParteDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        try
        {
            _log.LogWarning("⚠️ GetByIdAsync llamado - El backend NO soporta este endpoint (devuelve 405)");
            
            var result = await _api.GetAsync<ParteDto>($"/api/v1/partes/{id}", ct);
            
            if (result != null)
            {
                _log.LogInformation("✅ Parte encontrado: {cliente} - {fecha}", result.Cliente, result.FechaText);
            }
            else
            {
                _log.LogWarning("⚠️ Parte ID {id} no encontrado", id);
            }

            return result;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "❌ Error obteniendo parte {id} - Backend devuelve 405", id);
            throw;
        }
    }

    /// <summary>Crear un nuevo parte</summary>
    public async Task<ParteDto?> CreateAsync(ParteCreateRequest request, CancellationToken ct = default)
    {
        try
        {
            _log.LogInformation("➕ Creando parte: Cliente {cliente} - Fecha {fecha}", 
                request.IdCliente, request.FechaTrabajo);
            
            var result = await _api.PostAsync<ParteCreateRequest, ParteDto>(
                "/api/v1/partes", request, ct);
            
            if (result != null)
            {
                _log.LogInformation("✅ Parte creado con ID: {id}", result.Id);
            }

            return result;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "❌ Error creando parte");
            throw;
        }
    }

    /// <summary>Actualizar un parte (PUT completo)</summary>
    public async Task<ParteDto?> UpdateAsync(int id, ParteUpdateRequest request, CancellationToken ct = default)
    {
        try
        {
            _log.LogInformation("✏️ Actualizando parte ID: {id}", id);
            
            var result = await _api.PutAsync<ParteUpdateRequest, ParteDto>(
                $"/api/v1/partes/{id}", request, ct);
            
            if (result != null)
            {
                _log.LogInformation("✅ Parte {id} actualizado", id);
            }

            return result;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "❌ Error actualizando parte {id}", id);
            throw;
        }
    }

    /// <summary>Anular un parte (POST /api/v1/partes/{id}/anular)</summary>
    public async Task<bool> AnularAsync(int id, CancellationToken ct = default)
    {
        try
        {
            _log.LogInformation("⛔ Anulando parte ID: {id}", id);
            
            await _api.PostAsync($"/api/v1/partes/{id}/anular", ct);
            
            _log.LogInformation("✅ Parte {id} anulado", id);
            return true;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "❌ Error anulando parte {id}", id);
            throw;
        }
    }

    /// <summary>Cerrar un parte (POST /api/v1/partes/{id}/cerrar)</summary>
    public async Task<bool> CerrarAsync(int id, CancellationToken ct = default)
    {
        try
        {
            _log.LogInformation("✅ Cerrando parte ID: {id}", id);
            
            await _api.PostAsync($"/api/v1/partes/{id}/cerrar", ct);
            
            _log.LogInformation("✅ Parte {id} cerrado", id);
            return true;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "❌ Error cerrando parte {id}", id);
            throw;
        }
    }

    /// <summary>Enviar un parte (POST /api/v1/partes/{id}/enviar)</summary>
    public async Task<bool> EnviarAsync(int id, CancellationToken ct = default)
    {
        try
        {
            _log.LogInformation("📤 Enviando parte ID: {id}", id);
            
            await _api.PostAsync($"/api/v1/partes/{id}/enviar", ct);
            
            _log.LogInformation("✅ Parte {id} enviado", id);
            return true;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "❌ Error enviando parte {id}", id);
            throw;
        }
    }

    /// <summary>Eliminar un parte</summary>
    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        try
        {
            _log.LogInformation("🗑️ Eliminando parte ID: {id}", id);
            
            await _api.DeleteAsync($"/api/v1/partes/{id}", ct);
            
            _log.LogInformation("✅ Parte {id} eliminado", id);
            return true;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "❌ Error eliminando parte {id}", id);
            throw;
        }
    }
}
