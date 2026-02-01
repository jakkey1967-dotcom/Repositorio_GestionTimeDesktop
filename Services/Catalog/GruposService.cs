using GestionTime.Desktop.Models.Dtos.Catalog;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GestionTime.Desktop.Services.Catalog;

/// <summary>Servicio para gestionar Grupos usando /api/v1/grupos</summary>
public sealed class GruposService
{
    private readonly ApiClient _api;
    private readonly ILogger _log;

    public GruposService(ApiClient api, ILogger logger)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _log = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Listar grupos con paginación y búsqueda</summary>
    public async Task<PagedResponse<GrupoDto>?> ListAsync(
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

            var path = $"/api/v1/grupos?{string.Join("&", queryParams)}";
            
            _log.LogInformation("📋 Listando grupos - Página: {page}, Tamaño: {pageSize}, Búsqueda: '{search}'", 
                page, pageSize, search ?? "(sin filtro)");

            var result = await _api.GetAsync<PagedResponse<GrupoDto>>(path, ct);

            if (result != null)
            {
                _log.LogInformation("✅ {count} grupos cargados (Total: {total})", 
                    result.Items.Count, result.TotalCount);
            }

            return result;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "❌ Error listando grupos");
            throw;
        }
    }

    /// <summary>Obtener un grupo por ID</summary>
    public async Task<GrupoDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        try
        {
            _log.LogInformation("🔍 Obteniendo grupo ID: {id}", id);
            
            var result = await _api.GetAsync<GrupoDto>($"/api/v1/grupos/{id}", ct);
            
            if (result != null)
            {
                _log.LogInformation("✅ Grupo encontrado: {nombre}", result.Nombre);
            }

            return result;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "❌ Error obteniendo grupo {id}", id);
            throw;
        }
    }

    /// <summary>Crear un nuevo grupo</summary>
    public async Task<GrupoDto?> CreateAsync(GrupoCreateRequest request, CancellationToken ct = default)
    {
        try
        {
            _log.LogInformation("➕ Creando grupo: {nombre}", request.Nombre);
            
            var result = await _api.PostAsync<GrupoCreateRequest, GrupoDto>(
                "/api/v1/grupos", request, ct);
            
            if (result != null)
            {
                _log.LogInformation("✅ Grupo creado con ID: {id}", result.Id);
            }

            return result;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "❌ Error creando grupo: {nombre}", request.Nombre);
            throw;
        }
    }

    /// <summary>Actualizar un grupo</summary>
    public async Task<GrupoDto?> UpdateAsync(int id, GrupoUpdateRequest request, CancellationToken ct = default)
    {
        try
        {
            _log.LogInformation("✏️ Actualizando grupo ID: {id}", id);
            
            var result = await _api.PutAsync<GrupoUpdateRequest, GrupoDto>(
                $"/api/v1/grupos/{id}", request, ct);
            
            if (result != null)
            {
                _log.LogInformation("✅ Grupo {id} actualizado", id);
            }

            return result;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "❌ Error actualizando grupo {id}", id);
            throw;
        }
    }

    /// <summary>Eliminar un grupo</summary>
    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        try
        {
            _log.LogInformation("🗑️ Eliminando grupo ID: {id}", id);
            
            await _api.DeleteAsync($"/api/v1/grupos/{id}", ct);
            
            _log.LogInformation("✅ Grupo {id} eliminado", id);
            return true;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "❌ Error eliminando grupo {id}", id);
            throw;
        }
    }
}
