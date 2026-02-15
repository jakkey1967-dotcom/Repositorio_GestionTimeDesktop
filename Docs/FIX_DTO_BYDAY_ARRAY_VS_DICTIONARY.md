# 🔧 Fix: DTO ByDay Desalineado (Array vs Dictionary)

**Fecha:** 2026-02-14  
**Versión:** v1.9.5-alpha  
**Estado:** ✅ Corregido  
**Archivo:** `Models/Dtos/Reports/InformeResumenDto.cs`

---

## 🐛 Problema Identificado

El backend devolvía `byDay` como un **Array** de objetos con propiedad `date`, pero el frontend esperaba un **Dictionary** con clave string.

### JSON del Backend (Real)

```json
{
  "byDay": [
    {
      "date": "2026-01-26T00:00:00Z",
      "partsCount": 7,
      "recordedMinutes": 480,
      "coveredMinutes": 480,
      "overlapMinutes": 0
    },
    {
      "date": "2026-01-27T00:00:00Z",
      "partsCount": 9,
      "recordedMinutes": 520,
      ...
    }
  ]
}
```

### DTO del Frontend (Incorrecto ANTES)

```csharp
public class InformeResumenDto
{
    ...
    public Dictionary<string, DayStatsDto>? ByDay { get; set; }  // ❌ DICTIONARY
}

public class DayStatsDto
{
    public int PartsCount { get; set; }
    // ❌ NO tenía propiedad Date
    ...
}
```

### Síntoma

- Backend devolvía datos válidos (37 partes, 7 días)
- Frontend mostraba: **"No se encontraron datos para los filtros seleccionados"**
- Causa: El deserializador JSON no podía mapear el array a Dictionary, devolviendo `null`

---

## ✅ Solución Implementada

### 1. Cambiar ByDay de Dictionary a List

```csharp
public class InformeResumenDto
{
    public int PartsCount { get; set; }
    public int RecordedMinutes { get; set; }
    public int CoveredMinutes { get; set; }
    public int OverlapMinutes { get; set; }
    public string? FirstStart { get; set; }
    public string? LastEnd { get; set; }
    public List<IntervalDto> MergedIntervals { get; set; } = new();
    public List<GapDto> Gaps { get; set; } = new();
    
    // ✅ CAMBIO: Backend devuelve array, no dictionary
    public List<DayStatsDto>? ByDay { get; set; }
}
```

### 2. Añadir propiedad Date a DayStatsDto

```csharp
public class DayStatsDto
{
    public DateTime Date { get; set; }  // ✅ Backend devuelve "2026-01-26T00:00:00Z"
    public int PartsCount { get; set; }
    public int RecordedMinutes { get; set; }
    public int CoveredMinutes { get; set; }
    public int OverlapMinutes { get; set; }
    public string? FirstStart { get; set; }
    public string? LastEnd { get; set; }
}
```

### 3. Actualizar BuildWeekChartFromByDay en ViewModel

**ANTES:**
```csharp
private void BuildWeekChartFromByDay(Dictionary<string, DayStatsDto> byDay)
{
    var sortedDays = byDay.OrderBy(kvp => DateTime.Parse(kvp.Key)).ToList();
    
    for (int i = 0; i < sortedDays.Count; i++)
    {
        var kvp = sortedDays[i];
        var dayLabel = i < daysOfWeek.Length ? daysOfWeek[i] : kvp.Key.Substring(8, 2);
        var minutes = kvp.Value.CoveredMinutes;
        ...
    }
}
```

**AHORA:**
```csharp
private void BuildWeekChartFromByDay(List<DayStatsDto> byDay)
{
    var sortedDays = byDay.OrderBy(d => d.Date).ToList();
    
    for (int i = 0; i < sortedDays.Count; i++)
    {
        var day = sortedDays[i];
        var dayLabel = i < daysOfWeek.Length ? daysOfWeek[i] : day.Date.ToString("dd");
        var minutes = day.CoveredMinutes;
        ...
    }
}
```

---

## 📊 Comparación de Comportamiento

### ANTES ❌

1. Backend devuelve JSON con `byDay: [...]`
2. Frontend intenta deserializar a `Dictionary<string, DayStatsDto>`
3. Deserializador falla silenciosamente, devuelve `ByDay = null`
4. ViewModel recibe `result != null` pero `result.ByDay == null`
5. Gráfica semanal: "No hay datos disponibles para esta semana"
6. Resumen principal: "No se encontraron datos" (aunque `PartsCount = 37`)

### AHORA ✅

1. Backend devuelve JSON con `byDay: [...]`
2. Frontend deserializa correctamente a `List<DayStatsDto>`
3. Deserializador mapea cada objeto con su propiedad `Date`
4. ViewModel recibe `result != null` y `result.ByDay.Count == 7`
5. Gráfica semanal: Se construye correctamente con 7 barras
6. Resumen principal: Muestra 37 partes y estadísticas

---

## 🎯 Beneficios de la Corrección

### ✅ Beneficio 1: Deserialización correcta
**Antes:** Backend devolvía array, frontend esperaba dictionary → `null`  
**Ahora:** Backend devuelve array, frontend espera array → Mapeado correcto

### ✅ Beneficio 2: Datos accesibles
**Antes:** `ByDay` siempre era `null`, no se podía acceder a datos por día  
**Ahora:** `ByDay` contiene 7 elementos (Lun-Dom) con estadísticas

### ✅ Beneficio 3: Gráfica semanal funciona
**Antes:** Nunca se mostraba porque `ByDay == null`  
**Ahora:** Se muestra correctamente con barras proporcionales

### ✅ Beneficio 4: Orden correcto
**Antes:** Al parsear claves string había riesgo de orden incorrecto  
**Ahora:** Se ordena por `DateTime` nativo, orden garantizado

---

## 📝 Logs Esperados (AHORA)

### Caso 1: Búsqueda exitosa con datos

```
[InformesService] 📊 Iniciando GetResumenAsync - Scope: week, WeekIso: 2026-W05, AgentId: b455821b-...
[InformesService] 📊 Endpoint construido: /api/v2/informes/resumen?scope=week&weekIso=2026-W05&agentId=b455821b-...
[InformesService] 📊 Respuesta recibida - Partes: 37, Registrado: 2470min, Real: 2470min, Solape: 0min
[WeekChart] ===== INICIO LoadWeekChartIfNeededAsync =====
[WeekChart] Scope: week, Resumen != null: True
[WeekChart] Scope=week, weekIsoToLoad: 2026-W05
[WeekChart] REUTILIZANDO byDay del resumen actual (7 días)
[WeekChart] BuildWeekChartFromByDay iniciado con 7 días
[WeekChart] Añadido: Lun = 8h 00m (barWidth: 200px, isUnder: False)
[WeekChart] Añadido: Mar = 8h 40m (barWidth: 217px, isUnder: False)
[WeekChart] Añadido: Mié = 8h 00m (barWidth: 200px, isUnder: False)
[WeekChart] Añadido: Jue = 8h 00m (barWidth: 200px, isUnder: False)
[WeekChart] Añadido: Vie = 8h 30m (barWidth: 212px, isUnder: False)
[WeekChart] Añadido: Sáb = 0h 00m (barWidth: 10px, isUnder: True)
[WeekChart] Añadido: Dom = 0h 00m (barWidth: 10px, isUnder: True)
[WeekChart] BuildWeekChartFromByDay completado. Total items: 7
[WeekChart] Gráfica construida (reutilizada) y ShowWeekChart=true
[WeekChart] ===== FIN LoadWeekChartIfNeededAsync =====
```

---

## 🔄 Archivos Modificados

### Frontend (GestionTimeDesktop)
- ✅ `Models/Dtos/Reports/InformeResumenDto.cs` (ByDay: Dictionary → List, DayStatsDto + Date)
- ✅ `ViewModels/Reports/ReportsViewModel.cs` (BuildWeekChartFromByDay con List)

### Backend (GestionTimeApi)
- ℹ️ **NO requiere cambios** (ya devolvía array correctamente)

---

## ✅ Testing

### Test 1: Búsqueda por Semana con datos
```
1. Abrir GestionTimeDesktop (F5)
2. Ir a Informes
3. Seleccionar Scope=Semana, WeekIso=2026-W05
4. Click en Buscar
5. Verificar: Gráfica semanal aparece con 7 barras (Lun-Dom)
6. Verificar: Resumen muestra "37 partes, 2470min registrados"
```

**Resultado esperado:** ✅ Gráfica visible con datos correctos

### Test 2: Búsqueda por Día
```
1. Seleccionar Scope=Día, Fecha=2026-01-27
2. Click en Buscar
3. Verificar: Llamada adicional con scope=week
4. Verificar: Gráfica semanal aparece (aunque buscaste por día)
```

**Resultado esperado:** ✅ Gráfica carga después de llamada adicional

### Test 3: Semana sin datos
```
1. Seleccionar Scope=Semana, WeekIso=2026-W10 (futura)
2. Click en Buscar
3. Verificar: Mensaje "No hay datos disponibles para esta semana"
4. Verificar: NO muestra "No se encontraron datos" (ese es error de DTO)
```

**Resultado esperado:** ✅ Mensaje correcto para semana vacía

---

## 🚀 Deployment

### 1. Compilar y reiniciar frontend
```powershell
cd GestionTimeDesktop
dotnet build
# F5 para ejecutar en Visual Studio
```

### 2. Verificar logs en Output window
```
Debug → Windows → Output → "Debug"
Buscar líneas: [WeekChart], [InformesService]
```

### 3. Testing manual (5 minutos)
- Buscar W05: Debe mostrar gráfica
- Buscar W06: Debe mostrar mensaje "No hay datos"
- Buscar por Día en W05: Debe cargar gráfica después

---

## ✅ Conclusión

### Problema Original
- DTO desalineado: Frontend esperaba Dictionary, backend devolvía Array
- Deserialización fallaba silenciosamente, `ByDay` siempre `null`
- Gráfica semanal nunca funcionaba, mensaje "No se encontraron datos" confuso

### Solución Implementada
- ✅ ByDay cambiado de `Dictionary<string, DayStatsDto>` a `List<DayStatsDto>`
- ✅ DayStatsDto añadido propiedad `Date` (DateTime)
- ✅ BuildWeekChartFromByDay actualizado para usar List ordenada por Date
- ✅ Compilación exitosa sin errores

### Resultado
- ✅ Deserialización correcta del JSON del backend
- ✅ Gráfica semanal funciona correctamente
- ✅ Orden de días garantizado (Lun-Dom)
- ✅ Backend NO requiere cambios (ya estaba correcto)

---

**Versión:** v1.9.5-alpha  
**Fecha:** 2026-02-14  
**Estado:** ✅ Fix aplicado y compilado  
**Hot Reload:** ⚠️ Reiniciar app (F5) para aplicar cambios en DTOs

**FIN DEL DOCUMENTO**
