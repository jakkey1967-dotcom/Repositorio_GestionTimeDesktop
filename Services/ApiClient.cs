using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GestionTime.Desktop.Diagnostics;

namespace GestionTime.Desktop.Services
{
    /// <summary>
    /// Cliente HTTP centralizado para GestionTime.Api.
    /// - Gestiona BaseUrl
    /// - Gestiona Login + Bearer token
    /// - Expone helpers Get/Post/Put genéricos tipados
    /// - Loguea Request/Response con duración
    /// </summary>
    public sealed class ApiClient
    {
        private readonly HttpClient _http;
        private readonly ILogger _log;
        private readonly JsonSerializerOptions _jsonRead;
        private readonly JsonSerializerOptions _jsonWrite;

        public string BaseUrl { get; }
        public string LoginPath { get; }

        public string? AccessToken { get; private set; }
        public string? RefreshToken { get; private set; }
        
        // 🆕 NUEVO: Tracking de expiración del token
        private DateTime? _tokenExpiresAt;
        private readonly SemaphoreSlim _refreshLock = new(1, 1);
        private bool _isRefreshing = false;
        
        // 🆕 NUEVO: Caché simple para GET requests (key = path, value = (response, timestamp))
        private readonly Dictionary<string, (string response, DateTime timestamp)> _getCache = new();
        private readonly TimeSpan _getCacheDuration = TimeSpan.FromMinutes(5); // Caché válido 5 minutos
        private readonly SemaphoreSlim _cacheLock = new(1, 1);

        public ApiClient(string baseUrl, string loginPath, ILogger log)
        {
            BaseUrl = NormalizeBaseUrl(baseUrl);
            LoginPath = NormalizePath(loginPath);
            _log = log;

            // Opciones para deserializar (leer) - case insensitive
            _jsonRead = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // Opciones para serializar (escribir) - ignora valores null
            _jsonWrite = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var handler = new HttpClientHandler 
            { 
                UseCookies = true, 
                CookieContainer = new CookieContainer(),
                // ⚠️ DESHABILITAR VALIDACIÓN SSL SOLO PARA DESARROLLO LOCAL
                // Para producción (Render), comentar esta línea ya que usa certificados válidos
                // ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };
            var pipeline = new HttpLoggingHandler(_log, maxBodyChars: 200000) { InnerHandler = handler };
            _http = new HttpClient(pipeline) 
            { 
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(120) // ✅ TIMEOUT aumentado a 120 segundos (2 minutos) para manejar latencias altas
            };
            
            _log.LogInformation("🌐 ApiClient inicializado - BaseUrl: {url}, Timeout: {timeout}s", 
                BaseUrl, _http.Timeout.TotalSeconds);
        }

        // =========================
        // AUTH
        // =========================

        public async Task<LoginResponse?> LoginAsync(string email, string password, CancellationToken ct = default)
        {
            var req = new LoginRequest
            {
                Email = email ?? "",
                Password = password ?? ""
            };

            _log.LogInformation("LoginAsync iniciado para {email}", email);

            var res = await PostAsync<LoginRequest, LoginResponse>(LoginPath, req, ct);
            
            // CASO 1: Token en la respuesta JSON (AccessToken)
            if (res != null && !string.IsNullOrWhiteSpace(res.AccessToken))
            {
                SetBearerToken(res.AccessToken!, res.RefreshToken);
                _log.LogInformation("Token extraído de JSON response ✅");
            }
            // CASO 2: Token en cookies (autenticación basada en cookies)
            else if (res != null)
            {
                // El servidor usa cookies (Set-Cookie), no devuelve token en JSON
                // Simulamos que tenemos token para que el código funcione
                AccessToken = "COOKIE_AUTH";
                _log.LogInformation("Autenticación basada en cookies detectada ✅");
            }
            
            return res;
        }

        public async Task<ChangePasswordResponse?> ChangePasswordAsync(string email, string currentPassword, string newPassword, CancellationToken ct = default)
        {
            var req = new ChangePasswordRequest
            {
                Email = email,
                CurrentPassword = currentPassword,
                NewPassword = newPassword
            };

            _log.LogInformation("ChangePasswordAsync iniciado para {email}", email);

            return await PostAsync<ChangePasswordRequest, ChangePasswordResponse>("/api/v1/auth/change-password", req, ct);
        }

        public void SetBearerToken(string accessToken, string? refreshToken = null)
        {
            AccessToken = accessToken;
            RefreshToken = refreshToken;
            
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            
            // 🆕 NUEVO: Calcular cuándo expira el token (decodificando JWT)
            try
            {
                var tokenExpiry = GetTokenExpiration(accessToken);
                if (tokenExpiry.HasValue)
                {
                    _tokenExpiresAt = tokenExpiry.Value;
                    var minutesUntilExpiry = (tokenExpiry.Value - DateTime.UtcNow).TotalMinutes;
                    _log.LogInformation("AUTH: Token expira en {minutes:F1} minutos (a las {time})", 
                        minutesUntilExpiry, tokenExpiry.Value.ToLocalTime().ToString("HH:mm:ss"));
                }
                else
                {
                    // Si no podemos decodificar, asumir 1 hora
                    _tokenExpiresAt = DateTime.UtcNow.AddHours(1);
                    _log.LogWarning("AUTH: No se pudo decodificar expiración del token, asumiendo 1 hora");
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Error decodificando token JWT, asumiendo 1 hora de validez");
                _tokenExpiresAt = DateTime.UtcNow.AddHours(1);
            }
            
            _log.LogInformation("AUTH: Bearer token seteado (len={len}, refreshToken={hasRefresh}).", 
                accessToken.Length, refreshToken != null);
        }

        public void ClearToken()
        {
            AccessToken = null;
            RefreshToken = null;
            _tokenExpiresAt = null;
            _http.DefaultRequestHeaders.Authorization = null;
            _log.LogInformation("AUTH: token limpiado.");
        }
        
        /// <summary>
        /// 🆕 NUEVO: Obtiene la fecha de expiración de un token JWT
        /// </summary>
        private DateTime? GetTokenExpiration(string token)
        {
            try
            {
                // JWT tiene 3 partes separadas por puntos: header.payload.signature
                var parts = token.Split('.');
                if (parts.Length != 3)
                    return null;
                
                // Decodificar el payload (segunda parte)
                var payload = parts[1];
                
                // Agregar padding si es necesario
                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }
                
                var payloadBytes = Convert.FromBase64String(payload);
                var payloadJson = Encoding.UTF8.GetString(payloadBytes);
                
                // Parsear JSON para obtener 'exp' (expiration timestamp)
                using var doc = JsonDocument.Parse(payloadJson);
                if (doc.RootElement.TryGetProperty("exp", out var expElement))
                {
                    var expSeconds = expElement.GetInt64();
                    // 'exp' es Unix timestamp (segundos desde 1970)
                    return DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime;
                }
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "Error decodificando token JWT");
            }
            
            return null;
        }
        
        /// <summary>
        /// 🆕 NUEVO: Verifica si el token está próximo a expirar y lo refresca si es necesario
        /// </summary>
        private async Task<bool> EnsureTokenValidAsync(CancellationToken ct = default)
        {
            // Si no tenemos token o refresh token, no podemos refrescar
            if (string.IsNullOrEmpty(AccessToken) || string.IsNullOrEmpty(RefreshToken))
                return true; // Dejar que la petición falle con 401
            
            // Si ya estamos refrescando en otro thread, esperar
            if (_isRefreshing)
            {
                await _refreshLock.WaitAsync(ct);
                _refreshLock.Release();
                return true; // Ya se refrescó
            }
            
            // Si el token no está próximo a expirar (más de 5 minutos), está OK
            if (_tokenExpiresAt.HasValue && _tokenExpiresAt.Value > DateTime.UtcNow.AddMinutes(5))
                return true;
            
            // Token próximo a expirar o ya expirado, refrescar
            await _refreshLock.WaitAsync(ct);
            try
            {
                _isRefreshing = true;
                
                _log.LogInformation("🔄 Token próximo a expirar, refrescando...");
                
                var refreshRequest = new { refreshToken = RefreshToken };
                var json = JsonSerializer.Serialize(refreshRequest, _jsonWrite);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                using var response = await _http.PostAsync("/api/v1/auth/refresh", content, ct);
                
                if (!response.IsSuccessStatusCode)
                {
                    _log.LogWarning("❌ Error refrescando token: {statusCode}", response.StatusCode);
                    
                    // Si el refresh falló con 401, el refresh token también expiró
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        _log.LogWarning("⚠️ Refresh token expirado, usuario debe hacer login nuevamente");
                        ClearToken();
                        
                        // Notificar a la UI que debe redirigir al login
                        TokenExpired?.Invoke(this, EventArgs.Empty);
                    }
                    
                    return false;
                }
                
                var body = await response.Content.ReadAsStringAsync(ct);
                var refreshResponse = JsonSerializer.Deserialize<RefreshTokenResponse>(body, _jsonRead);
                
                if (refreshResponse != null && !string.IsNullOrEmpty(refreshResponse.AccessToken))
                {
                    SetBearerToken(refreshResponse.AccessToken, refreshResponse.RefreshToken ?? RefreshToken);
                    _log.LogInformation("✅ Token refrescado exitosamente");
                    return true;
                }
                
                _log.LogWarning("❌ Respuesta de refresh no contiene token válido");
                return false;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error refrescando token");
                return false;
            }
            finally
            {
                _isRefreshing = false;
                _refreshLock.Release();
            }
        }
        
        /// <summary>
        /// 🆕 NUEVO: Evento que se dispara cuando el token expira definitivamente
        /// </summary>
        public event EventHandler? TokenExpired;
        
        /// <summary>
        /// 🆕 NUEVO: Limpia el caché de GET requests
        /// </summary>
        public void ClearGetCache()
        {
            _cacheLock.Wait();
            try
            {
                var count = _getCache.Count;
                _getCache.Clear();
                _log.LogInformation("🗑️ Caché de GET limpiado ({count} entradas eliminadas)", count);
            }
            finally
            {
                _cacheLock.Release();
            }
        }
    
        /// <summary>
        /// 🆕 NUEVO: Invalida una entrada específica del caché
        /// </summary>
        public void InvalidateCacheEntry(string path)
        {
            path = NormalizePath(path);
            _cacheLock.Wait();
            try
            {
                if (_getCache.Remove(path))
                {
                    _log.LogDebug("🗑️ Entrada de caché invalidada: {path}", path);
                }
            }
            finally
            {
                _cacheLock.Release();
            }
        }
    
        /// <summary>
        /// 🆕 NUEVO: Invalida automáticamente las entradas de caché relacionadas después de un POST/PUT/DELETE
        /// </summary>
        private void InvalidateRelatedCache(string modifiedPath, string method)
        {
            _cacheLock.Wait();
            try
            {
                // Extraer el path base sin query string
                var basePath = modifiedPath.Split('?')[0];
                
                // Encontrar todas las entradas de caché que empiezan con el mismo path base
                var allKeys = new List<string>(_getCache.Keys);
                var keysToRemove = allKeys
                    .Where(key => key.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                
                foreach (var key in keysToRemove)
                {
                    _getCache.Remove(key);
                    _log.LogDebug("🗑️ Caché invalidado por {method}: {path}", method, key);
                }
                
                if (keysToRemove.Count > 0)
                {
                    _log.LogInformation("✅ {count} entrada(s) de caché invalidadas por {method} a {path}", 
                        keysToRemove.Count, method, basePath);
                }
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        // =========================
        // UTIL - EXTRACCIÓN DE ERRORES
        // =========================

        /// <summary>
        /// Detecta si un texto contiene HTML
        /// </summary>
        private static bool IsHtmlContent(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;
            
            return text.Contains("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ||
                   text.Contains("<html", StringComparison.OrdinalIgnoreCase) ||
                   text.Contains("<HTML", StringComparison.OrdinalIgnoreCase) ||
                   text.Contains("<head>", StringComparison.OrdinalIgnoreCase) ||
                   text.Contains("<body>", StringComparison.OrdinalIgnoreCase) ||
                   text.Contains("<meta", StringComparison.OrdinalIgnoreCase) ||
                   (text.TrimStart().StartsWith("<") && text.Contains("</"));
        }

        /// <summary>
        /// Extrae el mensaje de error del body de respuesta del servidor
        /// </summary>
        private (string? message, string? error) ExtractErrorFromBody(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return (null, null);

            // 🆕 Si el body es HTML, no intentar parsearlo como JSON
            if (IsHtmlContent(body))
            {
                _log.LogDebug("ExtractErrorFromBody: Detectado HTML en lugar de JSON, retornando null");
                return (null, null);
            }

            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                // Intentar extraer "message"
                string? message = null;
                if (root.TryGetProperty("message", out var msgProp))
                {
                    message = msgProp.GetString();
                }

                // Intentar extraer "error"
                string? error = null;
                if (root.TryGetProperty("error", out var errProp))
                {
                    error = errProp.GetString();
                }

                // Si hay "errors" (array de validación), concatenar
                if (root.TryGetProperty("errors", out var errorsProp) && errorsProp.ValueKind == JsonValueKind.Object)
                {
                    var errorMessages = new System.Collections.Generic.List<string>();
                    foreach (var prop in errorsProp.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var errMsg in prop.Value.EnumerateArray())
                            {
                                var msg = errMsg.GetString();
                                if (!string.IsNullOrWhiteSpace(msg))
                                    errorMessages.Add(msg);
                            }
                        }
                    }
                    
                    if (errorMessages.Count > 0)
                    {
                        error = string.Join(", ", errorMessages);
                    }
                }

                return (message, error);
            }
            catch (JsonException jsonEx)
            {
                // Si falla el parsing JSON, loguear y retornar null
                _log.LogDebug(jsonEx, "ExtractErrorFromBody: Error parseando JSON, body no es JSON válido");
                return (null, null);
            }
            catch
            {
                // Cualquier otro error, retornar null
                return (null, null);
            }
        }

        // =========================
        // HTTP HELPERS
        // =========================

        public async Task<T?> GetAsync<T>(string path, CancellationToken ct = default)
        {
            // 🆕 NUEVO: Verificar y refrescar token si es necesario
            await EnsureTokenValidAsync(ct);
            
            path = NormalizePath(path);

            // 🆕 NUEVO: Verificar caché primero
            await _cacheLock.WaitAsync(ct);
            try
            {
                if (_getCache.TryGetValue(path, out var cached))
                {
                    var age = DateTime.UtcNow - cached.timestamp;
                    if (age < _getCacheDuration)
                    {
                        SpecializedLoggers.Api.LogDebug("💾 GET {url} - Usando CACHÉ (edad: {age:F1}s)", path, age.TotalSeconds);
                        
                        try
                        {
                            var cachedResult = JsonSerializer.Deserialize<T>(cached.response, _jsonRead);
                            return cachedResult;
                        }
                        catch (JsonException)
                        {
                            // Si falla deserialización del caché, continuar con petición normal
                            _getCache.Remove(path);
                        }
                    }
                    else
                    {
                        // Caché expirado, eliminar
                        _getCache.Remove(path);
                        SpecializedLoggers.Api.LogDebug("🗑️ GET {url} - Caché expirado (edad: {age:F1}s)", path, age.TotalSeconds);
                    }
                }
            }
            finally
            {
                _cacheLock.Release();
            }

            var sw = Stopwatch.StartNew();
            using var performanceScope = PerformanceLogger.BeginScope(SpecializedLoggers.Api, $"GET {path}");
            
            SpecializedLoggers.Api.LogInformation("HTTP GET {url}", path);

            try
            {
                using var resp = await _http.GetAsync(path, ct);
                var body = await resp.Content.ReadAsStringAsync(ct);

                sw.Stop();
                SpecializedLoggers.Api.LogInformation("HTTP GET {url} -> {code} en {ms}ms", path, (int)resp.StatusCode, sw.ElapsedMilliseconds);

                if (!resp.IsSuccessStatusCode)
                {
                    SpecializedLoggers.Api.LogWarning("HTTP GET {url} ERROR {code}. Body: {body}", path, (int)resp.StatusCode, LogSanitizer.Truncate(body, 1200));
                    
                    // Extraer mensaje de error del servidor
                    var (message, error) = ExtractErrorFromBody(body);
                    throw new ApiException(resp.StatusCode, path, message, error);
                }

                if (string.IsNullOrWhiteSpace(body))
                {
                    SpecializedLoggers.Api.LogWarning("HTTP GET {url} devolvió body vacío - retornando default", path);
                    return default;
                }

                // 🆕 NUEVO: Guardar en caché antes de deserializar
                await _cacheLock.WaitAsync(ct);
                try
                {
                    _getCache[path] = (body, DateTime.UtcNow);
                    SpecializedLoggers.Api.LogDebug("💾 GET {url} - Guardado en CACHÉ", path);
                }
                finally
                {
                    _cacheLock.Release();
                }

                try
                {
                    var result = JsonSerializer.Deserialize<T>(body, _jsonRead);
                    
                    // Log si el resultado es null pero el body no estaba vacío
                    if (result == null && !string.IsNullOrWhiteSpace(body))
                    {
                        SpecializedLoggers.Api.LogWarning("HTTP GET {url} deserialización resultó en null. Body: {body}", path, LogSanitizer.Truncate(body, 500));
                    }
                    
                    return result;
                }
                catch (JsonException jsonEx)
                {
                    SpecializedLoggers.Api.LogError(jsonEx, "HTTP GET {url} error deserializando JSON. Body: {body}", path, LogSanitizer.Truncate(body, 1000));
                    
                    // Retornar default en lugar de lanzar excepción para mantener la aplicación funcionando
                    return default;
                }
            }
            catch (ApiException)
            {
                // Re-lanzar ApiException sin modificar
                throw;
            }
            catch (HttpRequestException httpEx)
            {
                sw.Stop();
                SpecializedLoggers.Api.LogError(httpEx, "HTTP GET {url} error de conexión tras {ms}ms. Verifica conectividad de red y URL del servidor.", path, sw.ElapsedMilliseconds);
                throw new Exception($"Error de conexión al servidor: {httpEx.Message}. Verifica que la API esté accesible en {BaseUrl}", httpEx);
            }
            catch (TaskCanceledException timeoutEx)
            {
                sw.Stop();
                SpecializedLoggers.Api.LogError(timeoutEx, "HTTP GET {url} timeout tras {ms}ms", path, sw.ElapsedMilliseconds);
                throw new Exception($"Tiempo de espera agotado conectando al servidor. La API puede estar lenta o inaccesible.", timeoutEx);
            }
            catch (Exception ex)
            {
                sw.Stop();
                SpecializedLoggers.Api.LogError(ex, "HTTP GET {url} EXCEPTION tras {ms}ms", path, sw.ElapsedMilliseconds);
                throw;
            }
        }

        public async Task<TRes?> PostAsync<TReq, TRes>(string path, TReq payload, CancellationToken ct = default)
        {
            // 🆕 NUEVO: Verificar y refrescar token si es necesario
            await EnsureTokenValidAsync(ct);
            
            path = NormalizePath(path);

            var json = JsonSerializer.Serialize(payload, _jsonWrite);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var sw = Stopwatch.StartNew();
            using var performanceScope = PerformanceLogger.BeginScope(SpecializedLoggers.Api, $"POST {path}");
            
            SpecializedLoggers.Api.LogInformation("HTTP POST {url} Payload: {payload}", path, LogSanitizer.SanitizeJson(json));

            try
            {
                using var resp = await _http.PostAsync(path, content, ct);
                var body = await resp.Content.ReadAsStringAsync(ct);

                sw.Stop();
                SpecializedLoggers.Api.LogInformation("HTTP POST {url} -> {code} en {ms}ms", path, (int)resp.StatusCode, sw.ElapsedMilliseconds);

                if (!resp.IsSuccessStatusCode)
                {
                    SpecializedLoggers.Api.LogWarning("HTTP POST {url} ERROR {code}. Body: {body}", path, (int)resp.StatusCode, LogSanitizer.Truncate(body, 1200));
                    
                    // Extraer mensaje de error del servidor
                    var (message, error) = ExtractErrorFromBody(body);
                    throw new ApiException(resp.StatusCode, path, message, error);
                }

                // 🆕 NUEVO: POST exitoso - invalidar caché de GET relacionados
                InvalidateRelatedCache(path, "POST");

                if (string.IsNullOrWhiteSpace(body))
                {
                    SpecializedLoggers.Api.LogWarning("HTTP POST {url} devolvió body vacío - retornando default", path);
                    return default;
                }

                try
                {
                    var result = JsonSerializer.Deserialize<TRes>(body, _jsonRead);
                    
                    // Log si el resultado es null pero el body no estaba vacío
                    if (result == null && !string.IsNullOrWhiteSpace(body))
                    {
                        SpecializedLoggers.Api.LogWarning("HTTP POST {url} deserialización resultó en null. Body: {body}", path, LogSanitizer.Truncate(body, 500));
                    }
                    
                    return result;
                }
                catch (JsonException jsonEx)
                {
                    SpecializedLoggers.Api.LogError(jsonEx, "HTTP POST {url} error deserializando JSON. Body: {body}", path, LogSanitizer.Truncate(body, 1000));
                    
                    // Retornar default en lugar de lanzar excepción para mantener la aplicación funcionando
                    return default;
                }
            }
            catch (ApiException)
            {
                // Re-lanzar ApiException sin modificar
                throw;
            }
            catch (HttpRequestException httpEx)
            {
                sw.Stop();
                SpecializedLoggers.Api.LogError(httpEx, "HTTP POST {url} error de conexión tras {ms}ms. Verifica conectividad de red y URL del servidor.", path, sw.ElapsedMilliseconds);
                throw new Exception($"Error de conexión al servidor: {httpEx.Message}. Verifica que la API esté accesible en {BaseUrl}", httpEx);
            }
            catch (TaskCanceledException timeoutEx)
            {
                sw.Stop();
                SpecializedLoggers.Api.LogError(timeoutEx, "HTTP POST {url} timeout tras {ms}ms", path, sw.ElapsedMilliseconds);
                throw new Exception($"Tiempo de espera agotado conectando al servidor. La API puede estar lenta o inaccesible.", timeoutEx);
            }
            catch (Exception ex)
            {
                sw.Stop();
                SpecializedLoggers.Api.LogError(ex, "HTTP POST {url} EXCEPTION tras {ms}ms", path, sw.ElapsedMilliseconds);
                throw;
            }
        }

        public async Task<TRes?> PutAsync<TReq, TRes>(string path, TReq payload, CancellationToken ct = default)
        {
            // 🆕 NUEVO: Verificar y refrescar token si es necesario
            await EnsureTokenValidAsync(ct);
            
            path = NormalizePath(path);

            var json = JsonSerializer.Serialize(payload, _jsonWrite);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var sw = Stopwatch.StartNew();
            _log.LogInformation("HTTP PUT {url} Payload: {payload}", path, SafePayloadForLog(json));

            try
            {
                using var resp = await _http.PutAsync(path, content, ct);
                var body = await resp.Content.ReadAsStringAsync(ct);

                sw.Stop();
                _log.LogInformation("HTTP PUT {url} -> {code} en {ms}ms", path, (int)resp.StatusCode, sw.ElapsedMilliseconds);

                if (!resp.IsSuccessStatusCode)
                {
                    _log.LogWarning("HTTP PUT {url} ERROR {code}. Body: {body}", path, (int)resp.StatusCode, Trim(body, 1200));
                    
                    // Extraer mensaje de error del servidor
                    var (message, error) = ExtractErrorFromBody(body);
                    throw new ApiException(resp.StatusCode, path, message, error);
                }

                // 🆕 NUEVO: PUT exitoso - invalidar caché de GET relacionados
                InvalidateRelatedCache(path, "PUT");

                if (string.IsNullOrWhiteSpace(body))
                {
                    _log.LogWarning("HTTP PUT {url} devolvió body vacío - retornando default", path);
                    return default;
                }

                try
                {
                    var result = JsonSerializer.Deserialize<TRes>(body, _jsonRead);
                    
                    // Log si el resultado es null pero el body no estaba vacío
                    if (result == null && !string.IsNullOrWhiteSpace(body))
                    {
                        _log.LogWarning("HTTP PUT {url} deserialización resultó en null. Body: {body}", path, Trim(body, 500));
                    }
                    
                    return result;
                }
                catch (JsonException jsonEx)
                {
                    _log.LogError(jsonEx, "HTTP PUT {url} error deserializando JSON. Body: {body}", path, Trim(body, 1000));
                    
                    // Retornar default en lugar de lanzar excepción para mantener la aplicación funcionando
                    return default;
                }
            }
            catch (ApiException)
            {
                // Re-lanzar ApiException sin modificar
                throw;
            }
            catch (HttpRequestException httpEx)
            {
                sw.Stop();
                _log.LogError(httpEx, "HTTP PUT {url} error de conexión tras {ms}ms. Verifica conectividad de red y URL del servidor.", path, sw.ElapsedMilliseconds);
                throw new Exception($"Error de conexión al servidor: {httpEx.Message}. Verifica que la API esté accesible en {BaseUrl}", httpEx);
            }
            catch (TaskCanceledException timeoutEx)
            {
                sw.Stop();
                _log.LogError(timeoutEx, "HTTP PUT {url} timeout tras {ms}ms", path, sw.ElapsedMilliseconds);
                throw new Exception($"Tiempo de espera agotado conectando al servidor. La API puede estar lenta o inaccesible.", timeoutEx);
            }
            catch (Exception ex)
            {
                sw.Stop();
                _log.LogError(ex, "HTTP PUT {url} EXCEPCIÓN tras {ms}ms", path, sw.ElapsedMilliseconds);
                throw;
            }
        }

        /// <summary>
        /// POST sin payload (para acciones como anular)
        /// </summary>
        public async Task PostAsync(string path, CancellationToken ct = default)
        {
            path = NormalizePath(path);

            var sw = Stopwatch.StartNew();
            _log.LogInformation("HTTP POST {url} (sin payload)", path);

            try
            {
                using var content = new StringContent("", Encoding.UTF8, "application/json");
                using var resp = await _http.PostAsync(path, content, ct);
                var body = await resp.Content.ReadAsStringAsync(ct);

                sw.Stop();
                _log.LogInformation("HTTP POST {url} -> {code} en {ms}ms", path, (int)resp.StatusCode, sw.ElapsedMilliseconds);

                if (!resp.IsSuccessStatusCode)
                {
                    _log.LogWarning("HTTP POST {url} ERROR {code}. Body: {body}", path, (int)resp.StatusCode, Trim(body, 1200));
                    
                    // Extraer mensaje de error del servidor
                    var (message, error) = ExtractErrorFromBody(body);
                    throw new ApiException(resp.StatusCode, path, message, error);
                }
                
                // 🆕 NUEVO: POST exitoso - invalidar caché de GET relacionados
                InvalidateRelatedCache(path, "POST");
            }
            catch (ApiException)
            {
                // Re-lanzar ApiException sin modificar
                throw;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _log.LogError(ex, "HTTP POST {url} EXCEPCIÓN tras {ms}ms", path, sw.ElapsedMilliseconds);
                throw;
            }
        }

        /// <summary>
        /// DELETE request
        /// </summary>
        public async Task DeleteAsync(string path, CancellationToken ct = default)
        {
            path = NormalizePath(path);

            var sw = Stopwatch.StartNew();
            _log.LogInformation("HTTP DELETE {url}", path);

            try
            {
                using var resp = await _http.DeleteAsync(path, ct);
                var body = await resp.Content.ReadAsStringAsync(ct);

                sw.Stop();
                _log.LogInformation("HTTP DELETE {url} -> {code} en {ms}ms", path, (int)resp.StatusCode, sw.ElapsedMilliseconds);

                if (!resp.IsSuccessStatusCode)
                {
                    _log.LogWarning("HTTP DELETE {url} ERROR {code}. Body: {body}", path, (int)resp.StatusCode, Trim(body, 1200));
                    
                    // Extraer mensaje de error del servidor
                    var (message, error) = ExtractErrorFromBody(body);
                    throw new ApiException(resp.StatusCode, path, message, error);
                }
                
                // 🆕 NUEVO: DELETE exitoso - invalidar caché de GET relacionados
                InvalidateRelatedCache(path, "DELETE");
            }
            catch (ApiException)
            {
                // Re-lanzar ApiException sin modificar
                throw;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _log.LogError(ex, "HTTP DELETE {url} EXCEPCIÓN tras {ms}ms", path, sw.ElapsedMilliseconds);
                throw;
            }
        }

        /// <summary>
        /// Verifica si el servidor está disponible (ping)
        /// </summary>
        public async Task<bool> PingAsync(CancellationToken ct = default)
        {
            try
            {
                // Intentar varios endpoints comunes para verificar disponibilidad
                var pingPaths = new[] { "/api/v1/health", "/health", "/api/health", "/" };
                
                foreach (var pingPath in pingPaths)
                {
                    try
                    {
                        var sw = Stopwatch.StartNew();
                        using var resp = await _http.GetAsync(pingPath, ct);
                        sw.Stop();

                        var isOnline = resp.IsSuccessStatusCode;
                        
                        if (isOnline)
                        {
                            _log.LogDebug("PING {url} -> OK ({ms}ms)", pingPath, sw.ElapsedMilliseconds);
                            return true;
                        }
                        else
                        {
                            _log.LogDebug("PING {url} -> {code} ({ms}ms)", pingPath, (int)resp.StatusCode, sw.ElapsedMilliseconds);
                        }
                    }
                    catch
                    {
                        // Continuar con el siguiente endpoint
                        continue;
                    }
                }
                
                _log.LogDebug("PING failed: ningún endpoint respondió correctamente");
                return false;
            }
            catch (HttpRequestException ex)
            {
                _log.LogDebug("PING failed: {msg}", ex.Message);
                return false;
            }
            catch (TaskCanceledException)
            {
                _log.LogDebug("PING timeout");
                return false;
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "PING exception");
                return false;
            }
        }

        // =========================
        // UTIL
        // =========================

        private static string NormalizeBaseUrl(string url)
        {
            url = (url ?? "").Trim();
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("baseUrl vacío");

            // Asegura trailing slash para BaseAddress
            if (!url.EndsWith("/"))
                url += "/";

            return url;
        }

        private static string NormalizePath(string path)
        {
            path = (path ?? "").Trim();
            if (string.IsNullOrWhiteSpace(path))
                return "/";

            // Asegura leading slash
            if (!path.StartsWith("/"))
                path = "/" + path;

            return path;
        }

        private static string Trim(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.Length <= max) return s;
            return s.Substring(0, max) + "…";
        }

        /// <summary>
        /// Oculta password en logs si viene en JSON.
        /// </summary>
        private static string SafePayloadForLog(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return "";
            // Muy simple pero efectivo:
            // "password":"xxxxx"  -> "password":"***"
            return System.Text.RegularExpressions.Regex.Replace(
                json,
                "\"password\"\\s*:\\s*\"(.*?)\"",
                "\"password\":\"***\"",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
        }

        // =========================
        // MODELOS MINIMOS
        // =========================

        public sealed class LoginRequest
        {
            public string Email { get; set; } = "";
            public string Password { get; set; } = "";
        }

        public sealed class LoginResponse
        {
            // AJUSTA nombres según tu API real si cambia
            public string? AccessToken { get; set; }
            public string? RefreshToken { get; set; }
            public string? Message { get; set; }
            public string? UserName { get; set; }
            public string? UserEmail { get; set; }
            public string? UserRole { get; set; }
            
            // Campos para cambio obligatorio de contraseña
            public bool MustChangePassword { get; set; }
            public bool PasswordExpired { get; set; }
            public int DaysUntilExpiration { get; set; }
            
            /// <summary>
            /// Devuelve UserName o valor por defecto si es null/vacío
            /// </summary>
            [System.Text.Json.Serialization.JsonIgnore]
            public string UserNameSafe => string.IsNullOrWhiteSpace(UserName) ? "Usuario" : UserName;
            
            /// <summary>
            /// Devuelve UserEmail o valor por defecto si es null/vacío
            /// </summary>
            [System.Text.Json.Serialization.JsonIgnore]
            public string UserEmailSafe => string.IsNullOrWhiteSpace(UserEmail) ? "usuario@empresa.com" : UserEmail;
            
            /// <summary>
            /// Devuelve UserRole o valor por defecto si es null/vacío
            /// </summary>
            [System.Text.Json.Serialization.JsonIgnore]
            public string UserRoleSafe => string.IsNullOrWhiteSpace(UserRole) ? "Usuario" : UserRole;
        }

        public sealed class ChangePasswordRequest
        {
            public string Email { get; set; } = "";
            public string CurrentPassword { get; set; } = "";
            public string NewPassword { get; set; } = "";
        }

        public sealed class ChangePasswordResponse
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            public string? Error { get; set; }
        }

        public sealed class RefreshTokenResponse
        {
            public string? AccessToken { get; set; }
            public string? RefreshToken { get; set; }
        }
    }
}
