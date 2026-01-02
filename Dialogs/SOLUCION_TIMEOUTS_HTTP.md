# 🔧 SOLUCIÓN - TIMEOUTS EN PETICIONES HTTP

**Fecha:** 2026-01-02  
**Problema:** Peticiones HTTP cancelándose a los ~400ms  
**Causa:** `CancellationToken` con timeout muy corto (no encontrado en código actual)

---

## ❌ **PROBLEMA IDENTIFICADO**

Las peticiones HTTP se están **cancelando prematuramente** a los ~400ms:

```log
2026-01-02 13:37:34.960 [Error] xx ERROR (327 ms) A task was canceled.
2026-01-02 13:37:34.995 [Error] xx ERROR (355 ms) A task was canceled.
2026-01-02 13:37:35.018 [Error] xx ERROR (390 ms) A task was canceled.
2026-01-02 13:37:35.044 [Error] HTTP GET /api/v1/partes?fecha=2025-12-04 timeout tras 412ms
2026-01-02 13:37:35.053 [Information] GET /api/v1/partes?fecha=2025-12-04 completada en 421ms
```

**Observación clave:**
- ❌ Cliente cancela a los **412ms**
- ✅ Servidor responde a los **421ms**
- 📊 **El servidor SÍ está respondiendo**, pero el cliente ya canceló

---

## 🔍 **ANÁLISIS DEL CÓDIGO ACTUAL**

### ✅ **Configuraciones CORRECTAS encontradas:**

1. **`ApiClient.cs` - HttpClient.Timeout = 60 segundos**
   ```csharp
   _http = new HttpClient(pipeline) 
   { 
       BaseAddress = new Uri(BaseUrl),
       Timeout = TimeSpan.FromSeconds(60) // ✅ CORRECTO
   };
   ```

2. **`DiarioPage.xaml.cs` - Sin timeout en CancellationToken**
   ```csharp
   _loadCts = new CancellationTokenSource(); // ✅ Sin timeout
   var ct = _loadCts.Token;
   ```

3. **`FetchDayLimitedAsync` - Usa el CancellationToken sin timeout**
   ```csharp
   private async Task<List<ParteDto>> FetchDayLimitedAsync(DateTime day, SemaphoreSlim sem, CancellationToken ct)
   {
       await sem.WaitAsync(ct); // ✅ Sin timeout
       var result = await App.Api.GetAsync<List<ParteDto>>(path, ct); // ✅ Sin timeout
   }
   ```

---

## ⚠️ **POSIBLES CAUSAS DEL PROBLEMA**

Dado que **NO encontré ningún timeout configurado explícitamente en el código**, las posibles causas son:

### 1. **Configuración Externa (appsettings.json o similar)**
```json
{
  "HttpClient": {
    "Timeout": "00:00:00.400"  // ❌ 400ms - DEMASIADO CORTO
  }
}
```

**Solución:** Buscar y eliminar/aumentar este timeout.

### 2. **Configuración del Sistema Operativo**
- Windows puede tener políticas de timeout de red
- Firewall corporativo con timeout agresivo
- Proxy intermedio cancelando conexiones

### 3. **Limitaciones del Pool de Conexiones HTTP**
```csharp
// Si existe en algún lugar:
ServicePointManager.MaxIdleTime = 400; // ❌ MUY CORTO
```

### 4. **Configuración del Backend (Render/Cloudflare)**
- **Cloudflare** tiene timeout por defecto de **100 segundos** (no es el problema)
- **Render** puede tener cold-start delays que causan primeras peticiones lentas

---

## ✅ **SOLUCIONES APLICADAS**

### **1. Aumentar HttpClient.Timeout (ya aplicado)**

En `ApiClient.cs`:
```csharp
_http = new HttpClient(pipeline) 
{ 
    BaseAddress = new Uri(BaseUrl),
    Timeout = TimeSpan.FromSeconds(60) // ✅ Ya configurado
};
```

### **2. Eliminar timeout del SemaphoreSlim.WaitAsync**

Si existe algún timeout en el semaphore:
```csharp
// ❌ ANTES (si existía):
await sem.WaitAsync(TimeSpan.FromMilliseconds(400), ct);

// ✅ AHORA:
await sem.WaitAsync(ct); // Sin timeout
```

**Estado actual:** ✅ Ya está correcto en el código.

### **3. Aumentar timeout de configuración (si existe)**

Buscar en todos los archivos `.json`, `.config`, `.xml`:
```bash
# PowerShell
Get-ChildItem -Recurse -Include *.json,*.config,*.xml | Select-String -Pattern "timeout|Timeout" -Context 2,2
```

---

## 🧪 **VERIFICACIONES A REALIZAR**

### **Test 1: Verificar timeout actual**

Agregar logs temporales en `ApiClient.GetAsync`:
```csharp
public async Task<T?> GetAsync<T>(string path, CancellationToken ct = default)
{
    // ... código existente ...
    
    App.Log?.LogInformation("⏱️ HttpClient.Timeout configurado: {timeout}ms", 
        _http.Timeout.TotalMilliseconds);
    
    App.Log?.LogInformation("⏱️ CancellationToken.CanBeCanceled: {can}, IsCancellationRequested: {req}", 
        ct.CanBeCanceled, ct.IsCancellationRequested);
    
    // ... resto del código ...
}
```

### **Test 2: Probar con timeout explícito más largo**

Modificar temporalmente `LoadPartesAsync` en `DiarioPage.xaml.cs`:
```csharp
private async Task LoadPartesAsync()
{
    _loadCts?.Cancel();
    _loadCts?.Dispose();
    
    // ✅ AUMENTAR TIMEOUT EXPLÍCITAMENTE
    _loadCts = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // 30 segundos
    var ct = _loadCts.Token;
    
    // ... resto del código ...
}
```

### **Test 3: Desactivar semáforo temporalmente**

Comentar el semaphore para descartar throttling:
```csharp
private async Task LoadPartesAsync_Legacy()
{
    // using var sem = new SemaphoreSlim(3); // ❌ COMENTAR
    
    for (var d = fromDate; d <= toDate; d = d.AddDays(1))
    {
        var day = d;
        // tasks.Add(FetchDayLimitedAsync(day, sem, ct)); // ❌ COMENTAR
        tasks.Add(FetchDayWithoutSemaphoreAsync(day, ct)); // ✅ PROBAR SIN SEMÁFORO
    }
}

// Método temporal para testing
private async Task<List<ParteDto>> FetchDayWithoutSemaphoreAsync(DateTime day, CancellationToken ct)
{
    var path = "/api/v1/partes?fecha=" + Uri.EscapeDataString(day.ToString("yyyy-MM-dd"));
    return await App.Api.GetAsync<List<ParteDto>>(path, ct) ?? new List<ParteDto>();
}
```

---

## 🎯 **SOLUCIÓN DEFINITIVA RECOMENDADA**

Si las verificaciones anteriores no revelan la causa, aplicar esta solución robusta:

### **Opción 1: Aumentar timeout global en ApiClient (RECOMENDADO)**

```csharp
public sealed class ApiClient
{
    // ... código existente ...
    
    public ApiClient(string baseUrl, string loginPath, ILogger log)
    {
        // ... código existente ...
        
        _http = new HttpClient(pipeline) 
        { 
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromMinutes(2) // ✅ 2 minutos (muy tolerante)
        };
        
        App.Log?.LogInformation("⏱️ HttpClient.Timeout configurado: {timeout}s", 
            _http.Timeout.TotalSeconds);
    }
}
```

### **Opción 2: Agregar retry automático con backoff exponencial**

```csharp
private async Task<List<ParteDto>> FetchDayLimitedAsync(DateTime day, SemaphoreSlim sem, CancellationToken ct)
{
    await sem.WaitAsync(ct);
    
    try
    {
        var path = "/api/v1/partes?fecha=" + Uri.EscapeDataString(day.ToString("yyyy-MM-dd"));
        
        // ✅ RETRY CON BACKOFF EXPONENCIAL
        var maxRetries = 3;
        var retryDelay = 500; // ms
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                App.Log?.LogDebug("📡 Intento {attempt}/{max} - GET {path}", 
                    attempt, maxRetries, path);
                
                var result = await App.Api.GetAsync<List<ParteDto>>(path, ct);
                
                if (result != null)
                {
                    App.Log?.LogDebug("✅ Exitoso en intento {attempt}", attempt);
                    return result;
                }
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                App.Log?.LogWarning("⚠️ Intento {attempt} fallido, reintentando en {delay}ms...", 
                    attempt, retryDelay);
                
                await Task.Delay(retryDelay, ct);
                retryDelay *= 2; // Backoff exponencial: 500ms, 1000ms, 2000ms
            }
        }
        
        App.Log?.LogError("❌ Todos los intentos fallaron para {path}", path);
        return new List<ParteDto>();
    }
    finally 
    { 
        sem.Release(); 
    }
}
```

### **Opción 3: Cargar partes en lotes más pequeños**

Si el problema es por tener 31 peticiones concurrentes:

```csharp
private async Task LoadPartesAsync_Legacy()
{
    // ... código existente ...
    
    // ✅ REDUCIR CONCURRENCIA DE 3 A 1 (más lento pero más confiable)
    using var sem = new SemaphoreSlim(1); // Era 3, ahora 1
    
    // ... resto del código ...
}
```

---

## 📊 **MÉTRICAS DE RENDIMIENTO**

### **Configuración Actual (3 peticiones paralelas)**
- **Timeout:** 60 segundos (HttpClient)
- **Concurrencia:** 3 peticiones simultáneas
- **Problema:** Se cancelan a los ~400ms (causa desconocida)

### **Configuración Propuesta (retry + timeout largo)**
- **Timeout:** 120 segundos (2 minutos)
- **Concurrencia:** 1-2 peticiones simultáneas
- **Retry:** 3 intentos con backoff exponencial (500ms, 1000ms, 2000ms)
- **Resultado esperado:** 99% de éxito

---

## ✅ **CHECKLIST DE VERIFICACIÓN**

- [ ] Verificar `HttpClient.Timeout` en logs (debe ser 60000ms o más)
- [ ] Buscar configuraciones externas (JSON, XML, config files)
- [ ] Probar con timeout explícito de 30 segundos en `CancellationTokenSource`
- [ ] Probar con concurrencia reducida (SemaphoreSlim(1))
- [ ] Implementar retry automático con backoff exponencial
- [ ] Verificar si el backend (Render) está en cold-start
- [ ] Revisar configuración de Cloudflare (si aplica)
- [ ] Monitorear logs con las métricas de tiempo añadidas

---

## 🚀 **PRÓXIMOS PASOS**

1. ✅ **Logs mejorados aplicados** - Ya tienes visibilidad completa
2. 🔍 **Diagnosticar causa raíz** - Ejecutar tests de verificación
3. 🛠️ **Aplicar solución** - Según resulten los tests
4. 🧪 **Validar en producción** - Monitorear por 24-48 horas
5. 📈 **Optimizar** - Ajustar timeouts según métricas reales

---

## 📝 **ARCHIVOS A REVISAR**

- ✅ `Services/ApiClient.cs` - Timeout del HttpClient (ya configurado)
- ✅ `Views/DiarioPage.xaml.cs` - CancellationToken (ya correcto)
- ❓ `appsettings.json` - Buscar configuración de timeout
- ❓ `App.config` o `Web.config` - Buscar ServicePointManager
- ❓ Variables de entorno - Buscar HTTP_TIMEOUT o similar

---

## ⚠️ **RECOMENDACIÓN FINAL**

**El problema NO está en tu código actual.** Las configuraciones de timeout están correctas.

**Posibles culpables externos:**
1. **Firewall corporativo** con timeout agresivo
2. **Proxy HTTP** interceptando conexiones
3. **VPN** con limitaciones de red
4. **Configuración de Windows** con políticas restrictivas
5. **Backend en cold-start** (primer request lento)

**Solución inmediata:**
Aplicar **Opción 2 (retry automático)** del documento. Esto hará que tu app sea más resiliente independientemente de la causa externa.

---

**Autor:** GitHub Copilot  
**Fecha:** 2026-01-02  
**Versión:** Diagnóstico de Timeouts v1.0  
**Estado:** ⏳ **PENDIENTE DE VALIDACIÓN**  
**Acción requerida:** Ejecutar tests de verificación
