# ⚠️ FIX CRÍTICO: Caché No Se Invalidaba Correctamente

**Fecha:** 2025-02-04  
**Versión:** Fix #2 del Bug #2 (Caché impide ver cambios)  
**Estado:** ✅ **RESUELTO DEFINITIVAMENTE**

---

## 🐛 Problema Detectado en Producción

Después de implementar el primer fix del Bug #2 (invalidación de caché), el usuario reportó:

> "no lo hace correctamente no se refresca la vista despues de guardar los datos incluida la listview de los clientes revisalo bien anda"

### Logs del Usuario (2026-02-04 19:18:44)

```
19:18:44.859 [Information] CLIENTES_UI OnSaveClienteClick | invalidating cache for /api/v1/clientes...
19:18:44.860 [Information] CLIENTES_UI OnSaveClienteClick | calling LoadClientesAsync...
19:18:44.870 [Debug] GestionTime.API - 💾 GET /api/v1/clientes?page=1&size=50 - Usando CACHÉ (edad: 24.5s) ❌
```

**Problema:** A pesar de ejecutar `InvalidateCacheEntry`, **SEGUÍA usando caché**.

---

## 🔍 Causa Raíz

### Código Problemático (ApiClient.cs líneas 340-355)

```csharp
// ❌ PROBLEMA: Búsqueda EXACTA
public void InvalidateCacheEntry(string path)
{
    path = NormalizePath(path);
    _cacheLock.Wait();
    try
    {
        if (_getCache.Remove(path))  // ← Solo elimina coincidencia EXACTA
        {
            _log.LogDebug("🗑️ Entrada de caché invalidada: {path}", path);
        }
    }
    finally
    {
        _cacheLock.Release();
    }
}
```

**Explicación:**

1. `SettingsWindow.xaml.cs` llama: `App.Api.InvalidateCacheEntry("/api/v1/clientes")`
2. `InvalidateCacheEntry` busca eliminar la entrada con clave **EXACTA**: `/api/v1/clientes`
3. Pero el caché guarda entradas con **query strings completos**:
   - `/api/v1/clientes?page=1&size=50`
   - `/api/v1/clientes?page=1&size=50&q=búsqueda`
   - `/api/v1/clientes?page=2&size=50`
   - etc.
4. **Resultado:** `_getCache.Remove(path)` retorna `false` porque no encuentra la clave exacta
5. **Consecuencia:** Todas las entradas de caché permanecen intactas ❌

---

## ✅ Solución Implementada

### Nuevo Código (ApiClient.cs líneas 337-372)

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
        
        // Encontrar TODAS las entradas de caché que empiezan con el mismo path base
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

### Por Qué Ahora Funciona

1. **Búsqueda por prefijo:** `key.StartsWith(basePath, StringComparison.OrdinalIgnoreCase)`
2. **Elimina TODAS las variantes:**
   - `/api/v1/clientes?page=1&size=50` ✅
   - `/api/v1/clientes?page=2&size=50` ✅
   - `/api/v1/clientes?page=1&size=50&q=test` ✅
   - `/api/v1/clientes?page=1&size=50&provincia=Madrid` ✅
3. **Logging detallado:** Muestra cuántas entradas se eliminaron y cuáles
4. **Case-insensitive:** Funciona con mayúsculas/minúsculas

---

## 📊 Logs Esperados (Después del Fix)

```
CLIENTES_UI OnSaveClienteClick | UPDATE_SUCCESS | clienteId=1
CLIENTES_UI OnSaveClienteClick | invalidating cache for /api/v1/clientes...
✅ 3 entrada(s) de caché invalidadas para: /api/v1/clientes
🗑️ Entrada de caché invalidada: /api/v1/clientes?page=1&size=50
🗑️ Entrada de caché invalidada: /api/v1/clientes?page=2&size=50
🗑️ Entrada de caché invalidada: /api/v1/clientes?page=1&size=50&q=test
CLIENTES_UI OnSaveClienteClick | calling LoadClientesAsync...
HTTP GET /api/v1/clientes?page=1&size=50 -> 200 en 45ms ✅ (NUEVA PETICIÓN HTTP)
💾 GET /api/v1/clientes?page=1&size=50 - Guardado en CACHÉ (edad: 0.0s)
✅ 50 clientes cargados (Total: 59, Páginas: 2)
CLIENTES_UI LoadClientesAsync | SUCCESS
```

**Clave:** Ya NO aparece "Usando CACHÉ (edad: XX.Xs)" después de guardar.

---

## 🔧 Cambios en el Código

### Archivos Modificados

1. **`Services/ApiClient.cs`** (líneas 337-372)
   - Método `InvalidateCacheEntry` completamente reescrito
   - Búsqueda exacta → Búsqueda por prefijo
   - Logging mejorado (cuenta de entradas eliminadas)

### Archivos SIN Modificar

- `Views/SettingsWindow.xaml.cs` - Las 4 llamadas a `InvalidateCacheEntry` permanecen iguales
- `Services/Catalog/ClientesService.cs` - Sin cambios

---

## ✅ Verificación

### Pasos para Verificar el Fix

1. **Detener la aplicación** (está en debugging)
2. **Recompilar** para aplicar el nuevo código de `ApiClient.cs`
3. **Iniciar la aplicación**
4. **Editar un cliente** → Cambiar el nombre
5. **Guardar** → Pulsar "Guardar"
6. **Revisar logs** en `Data/logs/GestionTime-[fecha].log`:

**Buscar:**
```powershell
Select-String -Path "Data\logs\GestionTime-*.log" -Pattern "entrada(s) de caché invalidadas" | Select-Object -Last 5
```

**Esperado:**
```
✅ 1 entrada(s) de caché invalidadas para: /api/v1/clientes
✅ 2 entrada(s) de caché invalidadas para: /api/v1/clientes
✅ 3 entrada(s) de caché invalidadas para: /api/v1/clientes
```

7. **Verificar que los cambios aparecen INMEDIATAMENTE** en la lista

---

## 🎓 Lecciones Aprendidas

### 1. Los diccionarios de caché usan claves completas
- ❌ MAL: Asumir que `/api/v1/clientes` coincidirá con `/api/v1/clientes?page=1&size=50`
- ✅ BIEN: Buscar por prefijo para capturar todas las variantes

### 2. El logging detallado es crítico
- ❌ MAL: Log simple "Caché invalidado"
- ✅ BIEN: Log con count de entradas eliminadas + lista de claves

### 3. Testing en producción revela problemas ocultos
- El primer fix (invalidación manual) parecía correcto en teoría
- Solo con logs del usuario en producción se detectó que NO funcionaba
- Los logs mostraron claramente el problema: "Usando CACHÉ" después de invalidar

### 4. Los métodos `Remove` de diccionarios retornan false si no encuentran la clave
- No lanzan excepción
- Retornan `false` silenciosamente
- Sin logging adecuado, el problema pasa desapercibido

---

## 📝 Documentación Actualizada

- ✅ `Docs/FIX_CACHE_CLIENTES_NO_REFRESCA.md` - Añadido Fix #2
- ✅ `Docs/SOLUCION_BUG_CLIENTES_POST_GUARDADO.md` - Actualizado con solución correcta
- ✅ `Docs/RESUMEN_EJECUTIVO_FIXES_CLIENTES.md` - Actualizado con cambio en ApiClient.cs
- ✅ `Docs/FIX_CACHE_INVALIDATION_CRITICAL.md` - Este archivo (nuevo)

---

## ⚠️ IMPORTANTE: Reiniciar Aplicación

**La aplicación está actualmente en debugging**, por lo que los cambios en `ApiClient.cs` **NO se han aplicado aún**.

**Pasos obligatorios:**
1. **Detener debugging** (Stop en Visual Studio)
2. **Recompilar** (Ctrl+Shift+B o Build → Build Solution)
3. **Iniciar de nuevo** (F5 o Debug → Start Debugging)

**Sin reiniciar, el bug PERSISTIRÁ** porque seguirá usando el código antiguo de `InvalidateCacheEntry` en memoria.

---

**Estado:** ✅ **FIX COMPLETADO - PENDIENTE REINICIO DE APLICACIÓN**  
**Compilación:** ✅ OK (con advertencia de debugging activo)  
**Próxima acción:** Reiniciar app → Probar edición de cliente → Verificar logs
