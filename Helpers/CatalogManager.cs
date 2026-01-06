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
        if (_gruposCache == null || !_gruposCache.Any())
        {
            App.Log?.LogWarning("⚠️ GetGrupoId: Cache de grupos está vacío o null");
            return null;
        }
        
        App.Log?.LogDebug("🔍 GetGrupoId: Buscando '{nombre}' en {count} grupos", nombre, _gruposCache.Count);
        
        // 🆕 NUEVO: Normalizar errores ortográficos comunes
        var nombreNormalizado = NormalizarNombreGrupo(nombre);
        
        if (!string.Equals(nombre, nombreNormalizado, StringComparison.Ordinal))
        {
            App.Log?.LogDebug("📝 Corrección ortográfica: '{original}' → '{corregido}'", nombre, nombreNormalizado);
        }
        
        // 🆕 NUEVO: Normalizar para comparación (sin acentos, mayúsculas)
        var nombreBusquedaNormalizado = NormalizarTextoParaBusqueda(nombreNormalizado);
        App.Log?.LogDebug("🔍 Búsqueda normalizada: '{original}' → '{normalizado}'", nombreNormalizado, nombreBusquedaNormalizado);
        
        var resultado = _gruposCache.FirstOrDefault(g => 
            string.Equals(NormalizarTextoParaBusqueda(g.Nombre), nombreBusquedaNormalizado, StringComparison.Ordinal));
        
        if (resultado != null)
        {
            App.Log?.LogDebug("✅ Encontrado: [{id}] '{nombre}'", resultado.Id_grupo, resultado.Nombre);
            return resultado.Id_grupo;
        }
        else
        {
            App.Log?.LogDebug("❌ NO encontrado: '{nombre}'", nombreNormalizado);
            App.Log?.LogDebug("   Comparando con: {grupos}", string.Join(", ", _gruposCache.Select(g => $"'{g.Nombre}'").Take(5)));
        }
        
        return null;
    }
    
    /// <summary>Normaliza nombres de grupos corrigiendo errores ortográficos comunes.</summary>
    private static string NormalizarNombreGrupo(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return nombre;
        
        // Diccionario de correcciones ortográficas comunes
        var correcciones = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Mobilidad", "Movilidad" },  // b → v (error común de escritura)
            { "Movibilidad", "Movilidad" }, // doble error
            { "Mobiilidad", "Movilidad" },  // doble 'i'
        };
        
        if (correcciones.TryGetValue(nombre, out var nombreCorregido))
        {
            return nombreCorregido;
        }
        
        return nombre;
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
        if (_tiposCache == null || !_tiposCache.Any())
        {
            App.Log?.LogWarning("⚠️ GetTipoId: Cache de tipos está vacío o null");
            return null;
        }
        
        App.Log?.LogDebug("🔍 GetTipoId: Buscando '{nombre}' en {count} tipos", nombre, _tiposCache.Count);
        
        // 🆕 NUEVO: Normalizar errores ortográficos comunes
        var nombreNormalizado = NormalizarNombreTipo(nombre);
        
        if (!string.Equals(nombre, nombreNormalizado, StringComparison.Ordinal))
        {
            App.Log?.LogDebug("📝 Corrección ortográfica: '{original}' → '{corregido}'", nombre, nombreNormalizado);
        }
        
        // 🆕 NUEVO: Normalizar para comparación (sin acentos, mayúsculas)
        var nombreBusquedaNormalizado = NormalizarTextoParaBusqueda(nombreNormalizado);
        App.Log?.LogDebug("🔍 Búsqueda normalizada: '{original}' → '{normalizado}'", nombreNormalizado, nombreBusquedaNormalizado);
        
        var resultado = _tiposCache.FirstOrDefault(t => 
            string.Equals(NormalizarTextoParaBusqueda(t.Nombre), nombreBusquedaNormalizado, StringComparison.Ordinal));
        
        if (resultado != null)
        {
            App.Log?.LogDebug("✅ Encontrado: [{id}] '{nombre}'", resultado.Id_tipo, resultado.Nombre);
            return resultado.Id_tipo;
        }
        else
        {
            App.Log?.LogDebug("❌ NO encontrado: '{nombre}'", nombreNormalizado);
            App.Log?.LogDebug("   Comparando con: {tipos}", string.Join(", ", _tiposCache.Select(t => $"'{t.Nombre}'").Take(5)));
        }
        
        return null;
    }
    
    /// <summary>Normaliza nombres de tipos corrigiendo errores ortográficos comunes.</summary>
    private static string NormalizarNombreTipo(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return nombre;
        
        // Diccionario de correcciones ortográficas comunes
        var correcciones = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Insidencia", "Incidencia" },  // s → c
            { "LLamada", "Llamada" },        // LL mayúscula → Ll
            { "LLamada Overlay", "Llamada Overlay" },  // LL mayúscula → Ll (con Overlay)
        };
        
        if (correcciones.TryGetValue(nombre, out var nombreCorregido))
        {
            return nombreCorregido;
        }
        
        return nombre;
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
    
    // ===================== MÉTODOS DE ACCESO A LISTAS COMPLETAS =====================
    
    /// <summary>Obtiene la lista completa de grupos cargados en el cache.</summary>
    public List<GrupoResponse> GetAllGrupos()
    {
        return _gruposCache ?? new List<GrupoResponse>();
    }
    
    /// <summary>Obtiene la lista completa de tipos cargados en el cache.</summary>
    public List<TipoResponse> GetAllTipos()
    {
        return _tiposCache ?? new List<TipoResponse>();
    }
    
    /// <summary>Obtiene la lista completa de clientes cargados en el cache.</summary>
    public List<ClienteResponse> GetAllClientes()
    {
        return _clientesCache ?? new List<ClienteResponse>();
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
    
    /// <summary>Normaliza texto para búsqueda: sin acentos, en mayúsculas, sin espacios duplicados.</summary>
    private static string NormalizarTextoParaBusqueda(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return string.Empty;
        
        // 1. Convertir a mayúsculas
        var textoNormalizado = texto.ToUpperInvariant();
        
        // 2. Eliminar acentos
        textoNormalizado = RemoverAcentos(textoNormalizado);
        
        // 3. Eliminar espacios múltiples
        textoNormalizado = System.Text.RegularExpressions.Regex.Replace(textoNormalizado, @"\s+", " ");
        
        // 4. Trim final
        return textoNormalizado.Trim();
    }
    
    /// <summary>Elimina acentos y diacríticos de un texto.</summary>
    private static string RemoverAcentos(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return texto;
        
        // Normalizar a FormD (descomponer caracteres con acentos)
        var normalizedString = texto.Normalize(System.Text.NormalizationForm.FormD);
        var stringBuilder = new System.Text.StringBuilder();
        
        foreach (var c in normalizedString)
        {
            // Solo agregar caracteres que NO sean marcas de acento
            var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }
        
        // Re-normalizar a FormC (composición)
        return stringBuilder.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }
}
