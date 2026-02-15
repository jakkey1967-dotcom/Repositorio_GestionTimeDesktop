using GestionTime.Desktop.Models.Dtos.Catalog;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GestionTime.Desktop.Services.Catalog;

/// <summary>Servicio para gestionar Clientes usando /api/v1/clientes</summary>
public sealed class ClientesService
{
    private readonly ApiClient _api;
    private readonly ILogger _log;

    public ClientesService(ApiClient api, ILogger logger)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _log = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Listar clientes con paginación y búsqueda</summary>
    public async Task<PagedResponse<ClienteDto>?> ListAsync(
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

            var path = $"/api/v1/clientes?{string.Join("&", queryParams)}";
            
            _log.LogInformation("📋 Listando clientes - Página: {page}, Tamaño: {pageSize}, Búsqueda: '{search}'", 
                page, pageSize, search ?? "(sin filtro)");

            var result = await _api.GetAsync<PagedResponse<ClienteDto>>(path, ct);

            if (result != null)
            {
                _log.LogInformation("✅ {count} clientes cargados (Total: {total})", 
                    result.Items.Count, result.TotalCount);
            }

            return result;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "❌ Error listando clientes");
            throw;
        }
    }

    /// <summary>Listar clientes con filtros avanzados</summary>
    public async Task<PagedResponse<ClienteDto>?> ListWithFiltersAsync(
        int page = 1,
        int size = 50,
        string? q = null,
        int? idPuntoop = null,
        int? localNum = null,
        string? provincia = null,
        bool? hasNota = null,
        CancellationToken ct = default)
    {
        try
        {
            var queryParams = new List<string>
            {
                $"page={page}",
                $"size={size}"
            };

            if (!string.IsNullOrWhiteSpace(q))
                queryParams.Add($"q={Uri.EscapeDataString(q)}");

            if (idPuntoop.HasValue)
                queryParams.Add($"id_puntoop={idPuntoop.Value}");

            if (localNum.HasValue)
                queryParams.Add($"local_num={localNum.Value}");

            if (!string.IsNullOrWhiteSpace(provincia))
                queryParams.Add($"provincia={Uri.EscapeDataString(provincia)}");

            if (hasNota.HasValue)
                queryParams.Add($"hasNota={hasNota.Value.ToString().ToLowerInvariant()}");

            var path = $"/api/v1/clientes?{string.Join("&", queryParams)}";
            
            _log.LogDebug("📋 Listando clientes con filtros - URL: {path}", path);
            _log.LogInformation("📋 Filtros: q='{q}', idPuntoop={idPuntoop}, localNum={localNum}, provincia='{provincia}', hasNota={hasNota}, page={page}, size={size}",
                q ?? "(vacío)", idPuntoop, localNum, provincia ?? "(vacío)", hasNota, page, size);

            var result = await _api.GetAsync<PagedResponse<ClienteDto>>(path, ct);

            if (result != null)
            {
                _log.LogInformation("✅ {count} clientes cargados (Total: {total}, Páginas: {totalPages})",
                    result.Items.Count, result.TotalCount, result.TotalPages);
            }

            return result;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "❌ Error listando clientes con filtros");
            throw;
        }
    }

    /// <summary>Obtener un cliente por ID</summary>
    public async Task<ClienteDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        try
        {
            _log.LogInformation("🔍 Obteniendo cliente ID: {id}", id);
            
            var result = await _api.GetAsync<ClienteDto>($"/api/v1/clientes/{id}", ct);
            
            if (result != null)
            {
                _log.LogInformation("✅ Cliente encontrado: {nombre}", result.Nombre);
            }
            else
            {
                _log.LogWarning("⚠️ Cliente ID {id} no encontrado", id);
            }

            return result;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "❌ Error obteniendo cliente {id}", id);
            throw;
        }
    }

    /// <summary>Crear un nuevo cliente</summary>
    public async Task<ClienteDto?> CreateAsync(ClienteCreateRequest request, CancellationToken ct = default)
    {
        try
        {
            _log.LogInformation("➕ Creando cliente: {nombre}", request.Nombre);
            
            var result = await _api.PostAsync<ClienteCreateRequest, ClienteDto>(
                "/api/v1/clientes", request, ct);
            
            if (result != null)
            {
                _log.LogInformation("✅ Cliente creado con ID: {id}", result.Id);
            }

            return result;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "❌ Error creando cliente: {nombre}", request.Nombre);
            throw;
        }
    }

    /// <summary>Actualizar un cliente (PUT completo)</summary>
    public async Task<ClienteDto?> UpdateAsync(int id, ClienteUpdateRequest request, CancellationToken ct = default)
    {
        try
        {
            _log.LogInformation("✏️ Actualizando cliente ID: {id}", id);
            
            var result = await _api.PutAsync<ClienteUpdateRequest, ClienteDto>(
                $"/api/v1/clientes/{id}", request, ct);
            
            if (result != null)
            {
                _log.LogInformation("✅ Cliente {id} actualizado", id);
            }

            return result;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "❌ Error actualizando cliente {id}", id);
            throw;
        }
    }

    /// <summary>Actualizar solo la nota de un cliente (PATCH)</summary>
    public async Task<ClienteDto?> UpdateNotaAsync(int id, string? nota, CancellationToken ct = default)
    {
        try
        {
            _log.LogInformation("📝 Actualizando nota del cliente ID: {id}", id);
            
            var request = new ClienteUpdateNotaRequest { Nota = nota };
            
            var result = await _api.PatchAsync<ClienteUpdateNotaRequest, ClienteDto>(
                $"/api/v1/clientes/{id}/nota", request, ct);
            
            if (result != null)
            {
                _log.LogInformation("✅ Nota del cliente {id} actualizada", id);
            }

            return result;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "❌ Error actualizando nota del cliente {id}", id);
            throw;
        }
    }

    /// <summary>Eliminar un cliente</summary>
    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        try
        {
            _log.LogInformation("🗑️ Eliminando cliente ID: {id}", id);

            await _api.DeleteAsync($"/api/v1/clientes/{id}", ct);

            _log.LogInformation("✅ Cliente {id} eliminado", id);
            return true;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "❌ Error eliminando cliente {id}", id);
            throw;
        }
    }

    // GT-BEGIN: Notas v2 (global + personal)

    /// <summary>Obtiene las notas (global + personal) de un cliente.</summary>
    public async Task<ClienteNotasResponse?> GetNotasAsync(int clienteId, CancellationToken ct = default)
    {
        try
        {
            _log.LogInformation("📝 GetNotas start - ClienteId: {id}", clienteId);

            var result = await _api.GetAsync<ClienteNotasResponse>(
                $"/api/v2/clientes/{clienteId}/notas", ct);

            _log.LogInformation("✅ GetNotas end - ClienteId: {id}", clienteId);
            return result;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "❌ Error obteniendo notas del cliente {id}", clienteId);
            throw;
        }
    }

    /// <summary>Guarda la nota global de un cliente (solo EDITOR/ADMIN).</summary>
    public async Task<ClienteNotaItem?> SaveNotaGlobalAsync(int clienteId, string? text, CancellationToken ct = default)
    {
        try
        {
            _log.LogInformation("💾 SaveNotaGlobal start - ClienteId: {id}", clienteId);

            var request = new ClienteNotaUpdateRequest { Text = text };
            var result = await _api.PutAsync<ClienteNotaUpdateRequest, ClienteNotaItem>(
                $"/api/v2/clientes/{clienteId}/notas/global", request, ct);

            _log.LogInformation("✅ SaveNotaGlobal end - ClienteId: {id}", clienteId);
            return result;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "❌ Error guardando nota global del cliente {id}", clienteId);
            throw;
        }
    }

    /// <summary>Guarda la nota personal del usuario autenticado para un cliente.</summary>
    public async Task<ClienteNotaItem?> SaveNotaPersonalAsync(int clienteId, string? text, CancellationToken ct = default)
    {
        try
        {
            _log.LogInformation("💾 SaveNotaPersonal start - ClienteId: {id}", clienteId);

            var request = new ClienteNotaUpdateRequest { Text = text };
            var result = await _api.PutAsync<ClienteNotaUpdateRequest, ClienteNotaItem>(
                $"/api/v2/clientes/{clienteId}/notas/personal", request, ct);

            _log.LogInformation("✅ SaveNotaPersonal end - ClienteId: {id}", clienteId);
            return result;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "❌ Error guardando nota personal del cliente {id}", clienteId);
            throw;
        }
    }

    // GT-END
}
