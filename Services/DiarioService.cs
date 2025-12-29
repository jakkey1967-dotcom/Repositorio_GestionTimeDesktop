using GestionTime.Desktop.Models.Dtos;
using GestionTime.Desktop.Models.Requests;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace GestionTime.Desktop.Services;

/// <summary>
/// Servicio de negocio para la pantalla Diario:
/// - Carga catálogos (clientes/grupos/tipos)
/// - Carga lista de partes
/// - Crea un parte (Grabar)
/// </summary>
public sealed class DiarioService
{
    private readonly ApiClient _api;
    private readonly ILogger _log;

    public DiarioService(ApiClient api, ILogger log)
    {
        _api = api;
        _log = log;
    }

    /// <summary>Obtiene el catálogo de clientes para rellenar el ComboBox.</summary>
   // public async Task<List<ClienteDto>> GetClientesAsync()
   //     => await _api.GetAsync<List<ClienteDto>>(App.ClientesPath) ?? new List<ClienteDto>();

   // public async Task<List<GrupoDto>> GetGruposAsync()
    //    => await _api.GetAsync<List<GrupoDto>>(App.GruposPath) ?? new List<GrupoDto>();
    //
    //public async Task<List<TipoDto>> GetTiposAsync()
    //    => await _api.GetAsync<List<TipoDto>>(App.TiposPath) ?? new List<TipoDto>();

    public async Task<List<ParteDto>> GetPartesAsync()
    {
        try
        {
            var result = await _api.GetAsync<List<ParteDto>>(App.PartesPath);
            
            if (result == null)
            {
                _log.LogWarning("GetPartesAsync devolvió null - retornando lista vacía");
                return new List<ParteDto>();
            }
            
            // Filtrar elementos null de la lista (por si acaso)
            var filteredResult = result.Where(p => p != null).ToList();
            
            if (filteredResult.Count < result.Count)
            {
                _log.LogWarning("GetPartesAsync contenía {count} elementos null - fueron filtrados", result.Count - filteredResult.Count);
            }
            
            return filteredResult;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error obteniendo partes - retornando lista vacía");
            return new List<ParteDto>();
        }
    }
}

