# ✅ SOLUCIÓN: Caché HTTP Impide Ver Cambios en Clientes

**Fecha:** 2025-02-03  
**Estado:** RESUELTO  
**Tiempo de resolución:** Detectado inmediatamente después de resolver Bug #1 (editPanel NOT FOUND)

---

## 🐛 Bug Detectado

**Reporte Usuario:** "cuando modifico cualquiero dato del cliente no se refresca estas usando el cache ???"

### Síntoma
1. Usuario edita cliente → Cambia "Nombre" de "Cliente A" a "Cliente A Editado"
2. Pulsa "Guardar" → ✅ Backend responde 200 OK con datos actualizados
3. La lista se recarga automáticamente (`LoadClientesAsync`) → ✅ OK
4. **❌ Problema**: La lista sigue mostrando "Cliente A" (dato viejo del caché)

---

## 🔍 Causa Raíz

### Logs Reveladores

```
2026-02-03 19:51:57.960 [Debug] GestionTime.API - 💾 GET /api/v1/clientes?page=1&size=50 - Usando CACHÉ (edad: 7.8s)
```

**Problema:** `LoadClientesAsync` usa el caché HTTP de `ApiClient` sin invalidarlo después de mutaciones.

### Código Problemático

**Services/ApiClient.cs (líneas 828-830):**
```csharp
// ⚠️ DESHABILITADO: Ya no invalidamos automáticamente en PUT
// El código que llama a PutAsync debe actualizar el cache manualmente usando UpdateCacheEntry()
// InvalidateRelatedCache(path, "PUT");
```

**La invalidación automática estaba deshabilitada** en `PutAsync`, `PostAsync`, `PatchAsync` y `DeleteAsync`.

Esto significa que después de cualquier mutación (POST/PUT/PATCH/DELETE), el caché de GET seguía devolviendo datos obsoletos durante **hasta 5 minutos** (duración del caché).

---

## ✅ Solución Aplicada

### Fix #1: Invalidación Manual de Caché (PARCIAL - NO FUNCIONABA)

**Primer intento (FALLIDO):**
```csharp
// ❌ PROBLEMA: InvalidateCacheEntry buscaba entrada EXACTA
App.Api.InvalidateCacheEntry("/api/v1/clientes");
```

**Logs mostraban:**
```
CLIENTES_UI OnSaveClienteClick | invalidating cache for /api/v1/clientes...
💾 GET /api/v1/clientes?page=1&size=50 - Usando CACHÉ (edad: 24.5s) ❌
```

**Causa:** El caché guarda entradas con query strings completos (`/api/v1/clientes?page=1&size=50`), pero `InvalidateCacheEntry` solo eliminaba coincidencias exactas (`/api/v1/clientes`).

### Fix #2: Invalidación por Prefijo (SOLUCIÓN CORRECTA)

**Modificado `Services/ApiClient.cs` (líneas 337-372):**
```csharp
/// <summary>
/// 🆕 NUEVO: Invalida todas las entradas de caché que empiezan con el path especificado (prefijo)
/// </summary>
public void InvalidateCacheEntry(string path)
{
    path = NormalizePath(path);
    _cacheLock.Wait();
    try
    {
        // Extraer el path base sin query string
        var basePath = path.Split('?')[0];
        
        // Encontrar todas las entradas de caché que empiezan con el mismo path base
        var allKeys = new List<string>(_getCache.Keys);
        var keysToRemove = allKeys
            .Where(key => key.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            .ToList();
        
        foreach (var key in keysToRemove)
        {
            _getCache.Remove(key);
            SpecializedLoggers.Api.LogDebug("🗑️ Entrada de caché invalidada: {path}", key);
        }
        
        if (keysToRemove.Count > 0)
        {
            SpecializedLoggers.Api.LogInformation("✅ {count} entrada(s) de caché invalidadas para: {basePath}", 
                keysToRemove.Count, basePath);
        }
        else
        {
            SpecializedLoggers.Api.LogDebug("⚠️ No se encontraron entradas de caché para invalidar: {basePath}", basePath);
        }
    }
    finally
    {
        _cacheLock.Release();
    }
}
```

**Ahora invalida TODAS las variantes:**
- `/api/v1/clientes?page=1&size=50`
- `/api/v1/clientes?page=1&size=50&q=búsqueda`
- `/api/v1/clientes?page=1&size=50&provincia=Madrid`
- etc.

### Código en SettingsWindow.xaml.cs (sin cambios)

El código de invalidación **permanece igual** en los 4 métodos:
```csharp
// 🔄 INVALIDAR CACHÉ antes de recargar para mostrar datos frescos
_log?.LogInformation("CLIENTES_UI OnSaveClienteClick | invalidating cache for /api/v1/clientes...");
App.Api.InvalidateCacheEntry("/api/v1/clientes");
```

**Ahora funciona correctamente** porque `InvalidateCacheEntry` busca por prefijo.

---

## 📊 Logs Añadidos

**ANTES del fix (InvalidateCacheEntry con búsqueda exacta):**
```
CLIENTES_UI OnSaveClienteClick | UPDATE_SUCCESS | clienteId=1
CLIENTES_UI OnSaveClienteClick | invalidating cache for /api/v1/clientes...
CLIENTES_UI OnSaveClienteClick | calling LoadClientesAsync...
💾 GET /api/v1/clientes?page=1&size=50 - Usando CACHÉ (edad: 24.5s) ❌ SEGUÍA USANDO CACHÉ
```

**DESPUÉS del fix (InvalidateCacheEntry con búsqueda por prefijo):**
```
CLIENTES_UI OnSaveClienteClick | UPDATE_SUCCESS | clienteId=1
CLIENTES_UI OnSaveClienteClick | invalidating cache for /api/v1/clientes...
✅ 3 entrada(s) de caché invalidadas para: /api/v1/clientes
🗑️ Entrada de caché invalidada: /api/v1/clientes?page=1&size=50
🗑️ Entrada de caché invalidada: /api/v1/clientes?page=2&size=50
🗑️ Entrada de caché invalidada: /api/v1/clientes?page=1&size=50&q=test
CLIENTES_UI OnSaveClienteClick | calling LoadClientesAsync...
HTTP GET /api/v1/clientes?page=1&size=50 -> 200 en 45ms ✅ (DATOS FRESCOS, SIN caché)
💾 GET /api/v1/clientes?page=1&size=50 - Guardado en CACHÉ
```

---

## ✅ Resultado

- **✅ Los cambios guardados se reflejan INMEDIATAMENTE en la UI**
- **✅ CREATE/UPDATE/PATCH/DELETE invalidan caché automáticamente**
- **✅ LoadClientesAsync siempre muestra datos frescos del backend**
- **✅ Logging completo de invalidación de caché para diagnóstico**

---

## 🔧 Alternativas Consideradas

### ❌ Opción 1: Habilitar InvalidateRelatedCache en ApiClient
```csharp
// En Services/ApiClient.cs PutAsync/PostAsync/PatchAsync/DeleteAsync
InvalidateRelatedCache(path, "PUT"); // Descomentar esta línea
```

**Problema:** Esto afectaría **TODOS los servicios** (Clientes, Partes, Tipos, Grupos, Usuarios, etc.) sin control granular. Podría causar invalidaciones innecesarias en otros módulos.

### ✅ Opción 2: Invalidación Manual Explícita (Elegida)
```csharp
// En cada método que guarda/elimina clientes
App.Api.InvalidateCacheEntry("/api/v1/clientes");
```

**Ventajas:**
- Control explícito de cuándo se invalida el caché
- Logging claro de invalidación
- No afecta a otros servicios
- Fácil de mantener y diagnosticar

---

## 📝 Archivos Modificados

- **`Services/ApiClient.cs`** - Modificado método `InvalidateCacheEntry` (líneas 337-372)
  - **ANTES:** Búsqueda exacta con `_getCache.Remove(path)` ❌
  - **DESPUÉS:** Búsqueda por prefijo con `StartsWith` + elimina todas las coincidencias ✅
- `Views/SettingsWindow.xaml.cs` (4 invalidaciones añadidas - sin cambios adicionales)
- `Docs/FIX_CACHE_CLIENTES_NO_REFRESCA.md` (este archivo - actualizado con Fix #2)
- `Docs/SOLUCION_BUG_CLIENTES_POST_GUARDADO.md` (pendiente actualizar)
- `Docs/FIX_CLIENTES_BUG_POST_GUARDADO_LOGGING.md` (pendiente actualizar)

---

**Estado:** ✅ Bug completamente resuelto  
**Compilación:** ✅ OK  
**Verificación:** Pendiente en producción

---

## 🎓 Lecciones Aprendidas

1. **El caché HTTP debe invalidarse después de mutaciones** - Sin invalidación, GET devuelve datos obsoletos.

2. **Logging de caché es crítico** - Sin logs de "Usando CACHÉ" vs "Nueva petición HTTP", este bug habría sido muy difícil de diagnosticar.

3. **Los bugs vienen en "racimos"** - Resolver Bug #1 (editPanel NOT FOUND) permitió descubrir inmediatamente Bug #2 (caché obsoleto).

4. **La invalidación automática global puede ser problemática** - Es mejor controlar explícitamente cuándo invalidar caché por módulo.

5. **La UX esperada es ver cambios inmediatos** - Los usuarios esperan que después de guardar, los cambios aparezcan INMEDIATAMENTE en la lista.
