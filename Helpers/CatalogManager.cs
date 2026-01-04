using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using GestionTime.Desktop.Models.Dtos;
using GestionTime.Desktop.Services;

namespace GestionTime.Desktop.Helpers;

/// <summary>
/// Gestor centralizado de catálogos (Clientes, Grupos, Tipos) con caché compartido
/// Reduce duplicación de código y centraliza la lógica de carga desde API
/// </summary>
public sealed class CatalogManager
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);
    
    // Cache estático compartido entre todas las instancias
    private static List<ClienteResponse>? _clientesCache;
    private static DateTime? _clientesCacheLoadedAt;
    
    private static List<GrupoResponse>? _gruposCache;
    private static DateTime? _gruposCacheLoadedAt;
    
    private static List<TipoResponse>? _tiposCache;
    private static DateTime? _tiposCacheLoadedAt;
    
    // Tokens de cancelación por instancia
    private CancellationTokenSource? _clienteLoadCts;
    private CancellationTokenSource? _grupoLoadCts;
    private CancellationTokenSource? _tipoLoadCts;
    
    // Flags de estado
    private bool _clientesLoaded;
    private bool _gruposLoaded;
    private bool _tiposLoaded;
    
    // Colecciones observables para UI
    public ObservableCollection<string> ClienteItems { get; } = new();
    public ObservableCollection<string> GrupoItems { get; } = new();
    public ObservableCollection<string> TipoItems { get; } = new();
    
    // ===================== CLIENTES =====================
    
    public async Task LoadClientesAsync()
    {
        App.Log?.LogInformation("🔄 LoadClientesAsync - Cache válido: {valid}", IsClientesCacheValid());
        
        if (_clientesLoaded && IsClientesCacheValid())
        {
            App.Log?.LogDebug("✅ Usando cache de clientes ({count} items)", _clientesCache!.Count);
            return;
        }
        
        try
        {
            _clienteLoadCts?.Cancel();
            _clienteLoadCts = new CancellationTokenSource();
            var ct = _clienteLoadCts.Token;
            
            var path = "/api/v1/catalog/clientes?limit=200&offset=0";
            var response = await App.Api.GetAsync<ClienteResponse[]>(path, ct);
            
            if (response != null && !ct.IsCancellationRequested)
            {
                _clientesCache = response.ToList();
                _clientesCacheLoadedAt = DateTime.Now;
                
                ClienteItems.Clear();
                var clientesValidos = _clientesCache
                    .Where(c => !string.IsNullOrWhiteSpace(c.Nombre))
                    .OrderBy(c => c.Nombre)
                    .ToList();
                
                foreach (var cliente in clientesValidos)
                {
                    ClienteItems.Add(cliente.Nombre);
                }
                
                _clientesLoaded = true;
                App.Log?.LogInformation("📊 Clientes cargados: {count} items", ClienteItems.Count);
            }
        }
        catch (OperationCanceledException)
        {
            App.Log?.LogDebug("🚫 Carga de clientes cancelada");
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error cargando clientes");
        }
    }
    
    public int? GetClienteId(string nombre)
    {
        return _clientesCache?.FirstOrDefault(c => 
            string.Equals(c.Nombre, nombre, StringComparison.OrdinalIgnoreCase))?.Id;
    }
    
    private static bool IsClientesCacheValid()
    {
        if (_clientesCache == null || _clientesCacheLoadedAt == null)
            return false;
        
        var age = DateTime.Now - _clientesCacheLoadedAt.Value;
        return age < CacheDuration;
    }
    
    public static void InvalidateClientesCache()
    {
        _clientesCache = null;
        _clientesCacheLoadedAt = null;
        App.Log?.LogInformation("🗑️ Cache de clientes invalidado");
    }
    
    // ===================== GRUPOS =====================
    
    public async Task LoadGruposAsync()
    {
        App.Log?.LogInformation("🔄 LoadGruposAsync - Cache válido: {valid}", IsGruposCacheValid());
        
        if (_gruposLoaded && IsGruposCacheValid())
        {
            App.Log?.LogDebug("✅ Usando cache de grupos ({count} items)", _gruposCache!.Count);
            return;
        }
        
        try
        {
            _grupoLoadCts?.Cancel();
            _grupoLoadCts = new CancellationTokenSource();
            var ct = _grupoLoadCts.Token;
            
            var path = "/api/v1/catalog/grupos";
            var response = await App.Api.GetAsync<GrupoResponse[]>(path, ct);
            
            if (response != null && !ct.IsCancellationRequested)
            {
                _gruposCache = response.ToList();
                _gruposCacheLoadedAt = DateTime.Now;
                
                GrupoItems.Clear();
                var gruposValidos = _gruposCache
                    .Where(g => !string.IsNullOrWhiteSpace(g.Nombre))
                    .OrderBy(g => g.Nombre)
                    .ToList();
                
                foreach (var grupo in gruposValidos)
                {
                    GrupoItems.Add(grupo.Nombre);
                }
                
                _gruposLoaded = true;
                App.Log?.LogInformation("📊 Grupos cargados: {count} items", GrupoItems.Count);
            }
        }
        catch (OperationCanceledException)
        {
            App.Log?.LogDebug("🚫 Carga de grupos cancelada");
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error cargando grupos");
        }
    }
    
    public int? GetGrupoId(string nombre)
    {
        return _gruposCache?.FirstOrDefault(g => 
            string.Equals(g.Nombre, nombre, StringComparison.OrdinalIgnoreCase))?.Id_grupo;
    }
    
    private static bool IsGruposCacheValid()
    {
        if (_gruposCache == null || _gruposCacheLoadedAt == null)
            return false;
        
        var age = DateTime.Now - _gruposCacheLoadedAt.Value;
        return age < CacheDuration;
    }
    
    public static void InvalidateGruposCache()
    {
        _gruposCache = null;
        _gruposCacheLoadedAt = null;
        App.Log?.LogInformation("🗑️ Cache de grupos invalidado");
    }
    
    // ===================== TIPOS =====================
    
    public async Task LoadTiposAsync()
    {
        App.Log?.LogInformation("🔄 LoadTiposAsync - Cache válido: {valid}", IsTiposCacheValid());
        
        if (_tiposLoaded && IsTiposCacheValid())
        {
            App.Log?.LogDebug("✅ Usando cache de tipos ({count} items)", _tiposCache!.Count);
            return;
        }
        
        try
        {
            _tipoLoadCts?.Cancel();
            _tipoLoadCts = new CancellationTokenSource();
            var ct = _tipoLoadCts.Token;
            
            var path = "/api/v1/catalog/tipos";
            var response = await App.Api.GetAsync<TipoResponse[]>(path, ct);
            
            if (response != null && !ct.IsCancellationRequested)
            {
                _tiposCache = response.ToList();
                _tiposCacheLoadedAt = DateTime.Now;
                
                TipoItems.Clear();
                var tiposValidos = _tiposCache
                    .Where(t => !string.IsNullOrWhiteSpace(t.Nombre))
                    .OrderBy(t => t.Nombre)
                    .ToList();
                
                foreach (var tipo in tiposValidos)
                {
                    TipoItems.Add(tipo.Nombre);
                }
                
                _tiposLoaded = true;
                App.Log?.LogInformation("📊 Tipos cargados: {count} items", TipoItems.Count);
            }
        }
        catch (OperationCanceledException)
        {
            App.Log?.LogDebug("🚫 Carga de tipos cancelada");
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error cargando tipos");
        }
    }
    
    public int? GetTipoId(string nombre)
    {
        return _tiposCache?.FirstOrDefault(t => 
            string.Equals(t.Nombre, nombre, StringComparison.OrdinalIgnoreCase))?.Id_tipo;
    }
    
    private static bool IsTiposCacheValid()
    {
        if (_tiposCache == null || _tiposCacheLoadedAt == null)
            return false;
        
        var age = DateTime.Now - _tiposCacheLoadedAt.Value;
        return age < CacheDuration;
    }
    
    public static void InvalidateTiposCache()
    {
        _tiposCache = null;
        _tiposCacheLoadedAt = null;
        App.Log?.LogInformation("🗑️ Cache de tipos invalidado");
    }
    
    // ===================== BÚSQUEDA DE CLIENTES =====================
    
    public async Task<List<string>> SearchClientesAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<string>();
        
        try
        {
            var path = $"/api/v1/catalog/clientes?q={Uri.EscapeDataString(query)}&limit=20&offset=0";
            var response = await App.Api.GetAsync<ClienteResponse[]>(path);
            
            if (response != null)
            {
                return response
                    .Where(c => !string.IsNullOrWhiteSpace(c.Nombre))
                    .Select(c => c.Nombre)
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error buscando clientes");
        }
        
        return new List<string>();
    }
}
