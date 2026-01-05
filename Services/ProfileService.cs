using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using GestionTime.Desktop.Models.Dtos;
using GestionTime.Desktop.Helpers;

namespace GestionTime.Desktop.Services;

/// <summary>Servicio para gestionar el perfil del usuario autenticado con cache local.</summary>
public sealed class ProfileService
{
    private readonly ILogger<ProfileService>? _log;
    private readonly ApiClient _apiClient;
    private UserProfileResponse? _cachedProfile;
    private DateTime? _cacheLoadedAt;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

    // 🆕 NUEVO: Evento que se dispara cuando el perfil se actualiza
    public event EventHandler<UserProfileResponse>? ProfileUpdated;

    public ProfileService(ApiClient apiClient, ILogger<ProfileService>? logger = null)
    {
        _apiClient = apiClient;
        _log = logger;
    }

    /// <summary>Obtiene el perfil del usuario autenticado actual (con cache de 15 minutos).</summary>
    public async Task<UserProfileResponse?> GetCurrentUserProfileAsync(CancellationToken ct = default)
    {
        // Verificar cache
        if (_cachedProfile != null && _cacheLoadedAt != null)
        {
            var age = DateTime.Now - _cacheLoadedAt.Value;
            if (age < CacheDuration)
            {
                _log?.LogDebug("✅ Usando perfil desde cache (edad: {age:F1}s)", age.TotalSeconds);
                return _cachedProfile;
            }
        }

        try
        {
            _log?.LogInformation("🔄 Cargando perfil del usuario desde API...");
            _log?.LogInformation("   Endpoint: GET /api/v1/profiles/me");
            _log?.LogInformation("   Token presente: {hasToken}", !string.IsNullOrEmpty(_apiClient.AccessToken));
            
            var profile = await _apiClient.GetAsync<UserProfileResponse>("/api/v1/profiles/me", ct);
            
            if (profile != null)
            {
                _cachedProfile = profile;
                _cacheLoadedAt = DateTime.Now;
                
                _log?.LogInformation("✅ Perfil cargado correctamente: {name} ({position})", 
                    profile.FullName, 
                    profile.Position ?? "Sin cargo");
                
                return profile;
            }
            
            _log?.LogError("⚠️ API devolvió null al cargar perfil");
            _log?.LogError("   Esto significa que:");
            _log?.LogError("   1. El endpoint devolvió status 200 pero body vacío");
            _log?.LogError("   2. O la deserialización falló");
            _log?.LogError("   Revisa los logs HTTP para más detalles");
            return null;
        }
        catch (ApiException apiEx)
        {
            _log?.LogError("❌ ApiException al cargar perfil:");
            _log?.LogError("   StatusCode: {statusCode}", apiEx.StatusCode);
            _log?.LogError("   Message: {message}", apiEx.Message);
            _log?.LogError("   Path: {path}", apiEx.Path);
            
            if (apiEx.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _log?.LogError("   → El endpoint /api/v1/profiles/me NO EXISTE o devolvió 404");
                _log?.LogError("   → Verifica que el backend tenga este endpoint implementado");
            }
            else if (apiEx.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _log?.LogError("   → El token de autenticación es inválido o expiró");
                _log?.LogError("   → El usuario debe hacer login nuevamente");
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _log?.LogError(ex, "❌ Error inesperado cargando perfil del usuario");
            _log?.LogError("   Tipo: {type}", ex.GetType().Name);
            _log?.LogError("   Message: {message}", ex.Message);
            return null;
        }
    }

    /// <summary>Actualiza el perfil del usuario autenticado. El perfil DEBE existir previamente.</summary>
    public async Task<UserProfileResponse?> UpdateUserProfileAsync(UpdateProfileRequest request, CancellationToken ct = default)
    {
        try
        {
            _log?.LogInformation("📝 Actualizando perfil del usuario...");
            _log?.LogDebug("   • Nombre: {first} {last}", request.FirstName, request.LastName);
            _log?.LogDebug("   • Cargo: {position}", request.Position ?? "(sin cambios)");
            _log?.LogDebug("   • Departamento: {dept}", request.Department ?? "(sin cambios)");
            
            // ✅ PASO 1: Obtener el perfil actual para verificar que existe
            _log?.LogDebug("🔄 Obteniendo perfil actual para verificar que existe...");
            var currentProfile = await GetCurrentUserProfileAsync(ct);
            
            if (currentProfile == null || string.IsNullOrEmpty(currentProfile.Id))
            {
                // ❌ ERROR CRÍTICO: El perfil NO existe (problema de sincronización)
                _log?.LogError("❌ ERROR CRÍTICO: El perfil del usuario NO existe en el backend");
                _log?.LogError("   Este es un problema de sincronización de datos.");
                _log?.LogError("   El perfil debería haberse creado automáticamente al registrar el usuario.");
                _log?.LogError("   Contacta al administrador del sistema para resolver este problema.");
                
                throw new InvalidOperationException(
                    "Tu perfil de usuario no existe en el sistema. " +
                    "Esto es un problema de sincronización de datos. " +
                    "Por favor, contacta al administrador del sistema.");
            }
            
            // ✅ PASO 2: Actualizar usando PUT /api/v1/profiles/me (sin ID en la URL)
            var putEndpoint = "/api/v1/profiles/me";
            
            _log?.LogDebug("🔄 Actualizando perfil del usuario autenticado: PUT {endpoint}", putEndpoint);
            _log?.LogDebug("   • Profile ID: {id}", currentProfile.Id);
            
            UserProfileResponse? updatedProfile = null;
            
            try
            {
                updatedProfile = await _apiClient.PutAsync<UpdateProfileRequest, UserProfileResponse>(
                    putEndpoint, 
                    request, 
                    ct);
                
                if (updatedProfile != null)
                {
                    _log?.LogInformation("✅ Perfil actualizado correctamente: {name}", updatedProfile.FullName);
                    
                    // Actualizar cache
                    _cachedProfile = updatedProfile;
                    _cacheLoadedAt = DateTime.Now;
                    
                    // Actualizar archivo JSON con los nuevos datos
                    UpdateUserInfoFile(updatedProfile);
                    
                    // 🆕 NUEVO: Notificar a suscriptores que el perfil se actualizó
                    ProfileUpdated?.Invoke(this, updatedProfile);
                    _log?.LogDebug("📢 Evento ProfileUpdated disparado");
                    
                    return updatedProfile;
                }
                else
                {
                    _log?.LogWarning("⚠️ API devolvió null al actualizar perfil");
                    return null;
                }
            }
            catch (ApiException apiEx) when (apiEx.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // ❌ ERROR CRÍTICO: El perfil existía en GET pero no en PUT (inconsistencia)
                _log?.LogError("❌ ERROR CRÍTICO: Inconsistencia de datos detectada");
                _log?.LogError("   • GET /api/v1/profiles/me devolvió perfil con ID={id}", currentProfile.Id);
                _log?.LogError("   • PUT /api/v1/profiles/me devolvió 404 Not Found");
                _log?.LogError("   Esto indica un problema grave de sincronización en el backend.");
                
                throw new InvalidOperationException(
                    "Error de sincronización de datos en el servidor. " +
                    $"Tu perfil (ID={currentProfile.Id}) fue encontrado pero no se puede actualizar. " +
                    "Por favor, contacta al administrador del sistema.");
            }
            catch (Exception updateEx)
            {
                _log?.LogError(updateEx, "❌ Error actualizando perfil (PUT)");
                throw;
            }
        }
        catch (InvalidOperationException)
        {
            // Re-lanzar errores de validación sin modificar
            throw;
        }
        catch (Exception ex)
        {
            _log?.LogError(ex, "❌ Error en UpdateUserProfileAsync");
            return null;
        }
    }

    /// <summary>Invalida el cache del perfil (forzar recarga en la próxima petición).</summary>
    public void InvalidateCache()
    {
        _cachedProfile = null;
        _cacheLoadedAt = null;
        _log?.LogDebug("🗑️ Cache de perfil invalidado");
    }

    /// <summary>🆕 MODIFICADO: Guarda datos básicos del perfil en archivo JSON para acceso rápido.</summary>
    private void UpdateUserInfoFile(UserProfileResponse profile)
    {
        try
        {
            // Cargar información actual (para preservar el email)
            var currentInfo = UserInfoFileStorage.LoadUserInfo(_log);
            
            // Actualizar con datos del perfil
            var userName = !string.IsNullOrWhiteSpace(profile.FullName) 
                ? profile.FullName 
                : currentInfo?.UserName;
                
            var userRole = !string.IsNullOrWhiteSpace(profile.Position) 
                ? profile.Position 
                : currentInfo?.UserRole;
                
            // 🔥 CRÍTICO: SIEMPRE preservar email existente (del login)
            var userEmail = currentInfo?.UserEmail;
            
            var userAvatar = !string.IsNullOrWhiteSpace(profile.AvatarUrl) 
                ? profile.AvatarUrl 
                : currentInfo?.UserAvatar;
            
            _log?.LogDebug("💾 Actualizando archivo JSON con perfil...");
            _log?.LogDebug("   • UserName: {name}", userName);
            _log?.LogDebug("   • UserEmail: {email} (PRESERVADO)", userEmail);
            _log?.LogDebug("   • UserRole: {role}", userRole);
            _log?.LogDebug("   • UserAvatar: {avatar}", userAvatar ?? "(sin avatar)");
            
            // Guardar en archivo JSON
            UserInfoFileStorage.SaveUserInfo(userName, userEmail, userRole, userAvatar, _log);
            
            _log?.LogDebug("✅ Archivo JSON actualizado con datos del perfil");
        }
        catch (Exception ex)
        {
            _log?.LogWarning(ex, "⚠️ Error actualizando archivo JSON con perfil");
        }
    }

    /// <summary>🆕 MODIFICADO: Carga el perfil del usuario al iniciar sesión y actualiza archivo JSON.</summary>
    /// <param name="log">Logger opcional para registrar el proceso</param>
    /// <param name="loginEmail">Email del usuario que acaba de hacer login (para NO sobrescribirlo)</param>
    public static async Task<bool> LoadProfileAfterLoginAsync(ILogger? log = null, string? loginEmail = null)
    {
        try
        {
            log?.LogInformation("🔄 Cargando perfil después del login...");
            
            // Crear instancia temporal solo para la carga inicial
            var profileService = new ProfileService(App.Api, null);
            var profile = await profileService.GetCurrentUserProfileAsync();
            
            if (profile != null && profile.IsComplete)
            {
                log?.LogInformation("✅ Perfil cargado: {name} - {position}", 
                    profile.FullName, 
                    profile.Position ?? "Sin cargo");
                
                // 🆕 MODIFICADO: Actualizar archivo JSON directamente PRESERVANDO el email del login
                try
                {
                    // Cargar información actual
                    var currentInfo = UserInfoFileStorage.LoadUserInfo(log);
                    
                    // 🔥 CRÍTICO: Usar el email del login si está disponible
                    var userEmail = !string.IsNullOrWhiteSpace(loginEmail) 
                        ? loginEmail 
                        : currentInfo?.UserEmail;
                    
                    // Actualizar con datos del perfil
                    var userName = !string.IsNullOrWhiteSpace(profile.FullName) 
                        ? profile.FullName 
                        : currentInfo?.UserName;
                        
                    var userRole = !string.IsNullOrWhiteSpace(profile.Position) 
                        ? profile.Position 
                        : currentInfo?.UserRole;
                    
                    var userAvatar = !string.IsNullOrWhiteSpace(profile.AvatarUrl) 
                        ? profile.AvatarUrl 
                        : currentInfo?.UserAvatar;
                    
                    log?.LogInformation("💾 Actualizando archivo JSON con perfil...");
                    log?.LogInformation("   • UserName: {name}", userName);
                    log?.LogInformation("   • UserEmail: {email} (PRESERVADO del login)", userEmail);
                    log?.LogInformation("   • UserRole: {role}", userRole);
                    log?.LogInformation("   • UserAvatar: {avatar}", userAvatar ?? "(sin avatar)");
                    
                    // Guardar en archivo JSON
                    UserInfoFileStorage.SaveUserInfo(userName, userEmail, userRole, userAvatar, log);
                    
                    log?.LogDebug("✅ Archivo JSON actualizado con datos del perfil después del login");
                }
                catch (Exception updateEx)
                {
                    log?.LogWarning(updateEx, "⚠️ Error actualizando archivo JSON con perfil después del login");
                }
                
                return true;
            }
            else if (profile != null && !profile.IsComplete)
            {
                log?.LogWarning("⚠️ Perfil incompleto: faltan datos obligatorios");
                return false;
            }
            
            log?.LogWarning("⚠️ No se pudo cargar el perfil del usuario");
            return false;
        }
        catch (Exception ex)
        {
            log?.LogError(ex, "❌ Error cargando perfil después del login");
            return false;
        }
    }
}
