using GestionTime.Desktop.Models.Dtos.Catalog;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GestionTime.Desktop.Services.Catalog;

/// <summary>Servicio para gestionar Tipos usando /api/v1/tipos</summary>
public sealed class TiposService
{
    private readonly ApiClient _api;
    private readonly ILogger _log;

    public TiposService(ApiClient api, ILogger logger)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _log = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Listar tipos con paginación y búsqueda</summary>
    public async Task<PagedResponse<TipoDto>?> ListAsync(
        int page = 1, 
        int pageSize = 50, 
        string? search = null, 
        CancellationToken ct = default)
    {
        try
        {
            var queryParams = new List<string>
            {
                $"page={page}",
                $"pageSize={pageSize}"
            };

            if (!string.IsNullOrWhiteSpace(search))
            {
                queryParams.Add($"search={Uri.EscapeDataString(search)}");
            }

            var path = $"/api/v1/tipos?{string.Join("&", queryParams)}";
            
            _log.LogInformation("📋 Listando tipos - Página: {page}, Tamaño: {pageSize}, Búsqueda: '{search}'", 
                page, pageSize, search ?? "(sin filtro)");

            var result = await _api.GetAsync<PagedResponse<TipoDto>>(path, ct);

            if (result != null)
            {
                _log.LogInformation("✅ {count} tipos cargados (Total: {total})", 
                    result.Items.Count, result.TotalCount);
            }

            return result;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "❌ Error listando tipos");
            throw;
        }
    }

    /// <summary>Obtener un tipo por ID</summary>
    public async Task<TipoDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        try
        {
            _log.LogInformation("🔍 Obteniendo tipo ID: {id}", id);
            
            var result = await _api.GetAsync<TipoDto>($"/api/v1/tipos/{id}", ct);
            
            if (result != null)
            {
                _log.LogInformation("✅ Tipo encontrado: {nombre}", result.Nombre);
            }

            return result;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "❌ Error obteniendo tipo {id}", id);
            throw;
        }
    }

    /// <summary>Crear un nuevo tipo</summary>
    public async Task<TipoDto?> CreateAsync(TipoCreateRequest request, CancellationToken ct = default)
    {
        try
        {
            _log.LogInformation("➕ Creando tipo: {nombre}", request.Nombre);
            
            var result = await _api.PostAsync<TipoCreateRequest, TipoDto>(
                "/api/v1/tipos", request, ct);
            
            if (result != null)
            {
                _log.LogInformation("✅ Tipo creado con ID: {id}", result.Id);
            }

            return result;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "❌ Error creando tipo: {nombre}", request.Nombre);
            throw;
        }
    }

    /// <summary>Actualizar un tipo</summary>
    public async Task<TipoDto?> UpdateAsync(int id, TipoUpdateRequest request, CancellationToken ct = default)
    {
        try
        {
            _log.LogInformation("✏️ Actualizando tipo ID: {id}", id);
            
            var result = await _api.PutAsync<TipoUpdateRequest, TipoDto>(
                $"/api/v1/tipos/{id}", request, ct);
            
            if (result != null)
            {
                _log.LogInformation("✅ Tipo {id} actualizado", id);
            }

            return result;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "❌ Error actualizando tipo {id}", id);
            throw;
        }
    }

    /// <summary>Eliminar un tipo</summary>
    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        try
        {
            _log.LogInformation("🗑️ Eliminando tipo ID: {id}", id);
            
            await _api.DeleteAsync($"/api/v1/tipos/{id}", ct);
            
            _log.LogInformation("✅ Tipo {id} eliminado", id);
            return true;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "❌ Error eliminando tipo {id}", id);
            throw;
        }
    }
}
