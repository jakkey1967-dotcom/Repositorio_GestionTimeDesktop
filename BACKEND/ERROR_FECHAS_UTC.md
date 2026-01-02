# 🐛 **ERROR CRÍTICO DEL BACKEND - FECHAS UTC**

**Fecha:** 2026-01-02  
**Prioridad:** 🔴 **CRÍTICO**  
**Afectado:** Backend API (PostgreSQL)  
**Estado:** ⚠️ **WORKAROUND APLICADO EN FRONTEND**

---

## 🔴 **ERROR DETECTADO**

```
System.ArgumentException: Cannot write DateTime with Kind=Unspecified to PostgreSQL 
type 'timestamp with time zone', only UTC is supported.
```

---

## 📍 **UBICACIÓN DEL ERROR**

**Archivo backend:**
```
GestionTime.Api/Controllers/PartesDeTrabajoController.cs
Línea: 60
Método: List(...)
```

**Endpoint afectado:**
```
GET /api/v1/partes?created_from=2025-12-03&created_to=2026-01-02
```

---

## 🔍 **CAUSA RAÍZ**

PostgreSQL **requiere** que los `DateTime` tengan `Kind = DateTimeKind.Utc` cuando se usan con columnas `timestamp with time zone`.

El backend está creando `DateTime` sin especificar el `Kind` (por defecto es `Unspecified`):

```csharp
// ❌ INCORRECTO (causa el error)
DateTime fecha = new DateTime(2025, 12, 3);
// fecha.Kind = DateTimeKind.Unspecified ⚠️

// Query a PostgreSQL
var partes = await _context.Partes
    .Where(p => p.FechaTrabajo >= fecha) // ❌ ERROR aquí
    .ToListAsync();
```

---

## ✅ **SOLUCIÓN EN EL BACKEND**

### **Opción 1: Especificar UTC al crear DateTime**
```csharp
// ✅ CORRECTO
DateTime fechaInicio = DateTime.SpecifyKind(
    new DateTime(2025, 12, 3), 
    DateTimeKind.Utc
);

DateTime fechaFin = DateTime.SpecifyKind(
    new DateTime(2026, 1, 2), 
    DateTimeKind.Utc
);

var partes = await _context.Partes
    .Where(p => p.FechaTrabajo >= fechaInicio && p.FechaTrabajo <= fechaFin)
    .ToListAsync();
```

### **Opción 2: Convertir parámetros recibidos a UTC**
```csharp
[HttpGet]
public async Task<IActionResult> List(
    DateTime? created_from = null, 
    DateTime? created_to = null)
{
    // Convertir a UTC si no lo están
    var fechaInicio = created_from.HasValue 
        ? DateTime.SpecifyKind(created_from.Value, DateTimeKind.Utc)
        : DateTime.UtcNow.AddDays(-30);
        
    var fechaFin = created_to.HasValue
        ? DateTime.SpecifyKind(created_to.Value, DateTimeKind.Utc)
        : DateTime.UtcNow;
    
    var partes = await _context.Partes
        .Where(p => p.FechaTrabajo >= fechaInicio && p.FechaTrabajo <= fechaFin)
        .ToListAsync();
    
    return Ok(partes);
}
```

### **Opción 3: Configurar Entity Framework Core globalmente**
```csharp
// En Startup.cs o Program.cs
services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        // Configurar manejo de fechas
        npgsqlOptions.EnableLegacyTimestampBehavior(); // Para EF Core 6+
    });
});
```

**O agregar en `DbContext`:**
```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    
    base.OnConfiguring(optionsBuilder);
}
```

---

## 🔧 **WORKAROUND TEMPORAL EN FRONTEND**

Mientras se corrige el backend, el frontend está usando el **método LEGACY** que hace peticiones individuales por día:

```csharp
// En DiarioPage.xaml.cs - LoadPartesAsync()

// ⚠️ TEMPORAL: Usar método LEGACY directamente
SpecializedLoggers.Data.LogWarning(
    "⚠️ Usando método LEGACY temporalmente (backend tiene issue con fechas UTC)"
);
await LoadPartesAsync_Legacy();

// 🔄 CÓDIGO ORIGINAL (comentado temporalmente)
// Reactivar cuando el backend esté corregido
/*
var path = $"/api/v1/partes?created_from={fromDate:yyyy-MM-dd}&created_to={toDate:yyyy-MM-dd}";
_cache30dias = await App.Api.GetAsync<List<ParteDto>>(path, CancellationToken.None);
*/
```

**Ventajas del método LEGACY:**
- ✅ Funciona correctamente (hace peticiones por día)
- ✅ No depende del endpoint con bug
- ⚠️ Hace 30 peticiones en lugar de 1 (más lento)

---

## 📊 **IMPACTO**

| Aspecto | Sin Workaround | Con Workaround |
|---------|----------------|----------------|
| **Carga de partes** | ❌ ERROR 500 | ✅ Funciona |
| **Rendimiento** | N/A | ⚠️ 30 peticiones HTTP |
| **Tiempo de carga** | N/A | ~3-5 segundos |
| **Cache** | ❌ No funciona | ✅ Funciona por día |

---

## 🧪 **CÓMO VERIFICAR LA CORRECCIÓN EN EL BACKEND**

### **Test 1: Probar endpoint con curl**
```bash
curl -X GET "https://gestiontimeapi.onrender.com/api/v1/partes?created_from=2025-12-03&created_to=2026-01-02" \
  -H "Authorization: Bearer TOKEN"
```

**Resultado esperado:**
- ✅ HTTP 200 OK
- ✅ JSON con array de partes
- ❌ NO debe retornar HTTP 500

### **Test 2: Verificar en logs del backend**
Buscar en logs del backend:
```
✅ Sin errores de "Cannot write DateTime with Kind=Unspecified"
✅ Query exitoso a PostgreSQL
```

### **Test 3: Probar en frontend**
1. Descomentar código original en `LoadPartesAsync()`
2. Comentar `await LoadPartesAsync_Legacy()`
3. Ejecutar app y verificar que carga correctamente

---

## 📝 **CÓDIGO COMPLETO DE LA CORRECCIÓN**

```csharp
// ===================================================================
// ARCHIVO: GestionTime.Api/Controllers/PartesDeTrabajoController.cs
// MÉTODO: List
// LÍNEA: ~60
// ===================================================================

[HttpGet]
public async Task<IActionResult> List(
    DateTime? fecha = null,
    DateTime? created_from = null,
    DateTime? created_to = null,
    string? q = null,
    int? estado = null)
{
    try
    {
        IQueryable<ParteDeTrabajo> query = _context.Partes
            .Include(p => p.Cliente)
            .Include(p => p.Grupo)
            .Include(p => p.Tipo);

        // 🆕 CORRECCIÓN: Convertir fechas a UTC antes de usar en query
        if (fecha.HasValue)
        {
            var fechaUtc = DateTime.SpecifyKind(fecha.Value.Date, DateTimeKind.Utc);
            query = query.Where(p => p.FechaTrabajo == fechaUtc);
        }
        else if (created_from.HasValue && created_to.HasValue)
        {
            // ✅ CLAVE: Especificar DateTimeKind.Utc para ambas fechas
            var fromUtc = DateTime.SpecifyKind(created_from.Value.Date, DateTimeKind.Utc);
            var toUtc = DateTime.SpecifyKind(created_to.Value.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);
            
            query = query.Where(p => p.FechaTrabajo >= fromUtc && p.FechaTrabajo <= toUtc);
        }

        // Filtro por texto (q)
        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(p => 
                p.Cliente.Nombre.Contains(q) ||
                p.Accion.Contains(q) ||
                p.Ticket.Contains(q)
            );
        }

        // Filtro por estado
        if (estado.HasValue)
        {
            query = query.Where(p => p.Estado == estado.Value);
        }

        var partes = await query
            .OrderByDescending(p => p.FechaTrabajo)
            .ThenByDescending(p => p.HoraInicio)
            .ToListAsync();

        return Ok(partes);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error obteniendo partes");
        return StatusCode(500, "Error interno del servidor");
    }
}
```

---

## 🎯 **CHECKLIST DE CORRECCIÓN**

### **Backend:**
- [ ] **Aplicar corrección** en `PartesDeTrabajoController.cs` línea 60
- [ ] **Usar `DateTime.SpecifyKind(..., DateTimeKind.Utc)`** para parámetros
- [ ] **Probar con curl** que el endpoint funciona
- [ ] **Verificar logs** sin errores de PostgreSQL
- [ ] **Deploy** a producción

### **Frontend (después de corrección del backend):**
- [ ] **Descomentar** código original en `LoadPartesAsync()`
- [ ] **Comentar/eliminar** llamada a `LoadPartesAsync_Legacy()`
- [ ] **Probar** carga de partes en app
- [ ] **Verificar logs** que usa endpoint de rango (1 petición)
- [ ] **Commit** y push

---

## 📚 **REFERENCIAS**

- [Npgsql DateTime Handling](https://www.npgsql.org/doc/types/datetime.html)
- [PostgreSQL Timestamp with Time Zone](https://www.postgresql.org/docs/current/datatype-datetime.html)
- [Entity Framework Core DateTime](https://docs.microsoft.com/en-us/ef/core/what-is-new/ef-core-6.0/breaking-changes#timestamp-behavior)

---

## 💬 **RESUMEN EJECUTIVO**

**Problema:** Backend lanza error 500 al consultar partes con rango de fechas porque PostgreSQL requiere DateTimeKind.Utc.

**Solución:** Especificar `DateTime.SpecifyKind(..., DateTimeKind.Utc)` en los parámetros de fecha antes de usarlos en queries.

**Workaround:** Frontend usa método LEGACY (30 peticiones individuales) temporalmente.

**Acción requerida:** Desarrollador backend debe aplicar corrección en `PartesDeTrabajoController.cs` línea 60.

---

**Autor:** GitHub Copilot  
**Fecha:** 2026-01-02  
**Prioridad:** 🔴 CRÍTICO  
**Estado:** ⚠️ **PENDIENTE DE CORRECCIÓN EN BACKEND**

