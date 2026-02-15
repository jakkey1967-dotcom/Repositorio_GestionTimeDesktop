# 🔧 Fix: Filtro de byDay Aplicado Incorrectamente

**Fecha:** 2026-02-14  
**Versión:** v1.9.5-alpha  
**Estado:** ✅ Corregido (Frontend + Backend)

---

## ⚠️ ACTUALIZACIÓN IMPORTANTE

**El problema principal estaba en el BACKEND**, no en el frontend. Ver: `GestionTimeApi/docs/FIX_FILTRO_AGENTID_BACKEND.md`

**Resumen:**
- **Frontend:** Filtro de `byDay` tenía lógica incompleta (corregido).
- **Backend:** Filtro de `agentId` no se aplicaba correctamente cuando EDITOR/ADMIN no especificaba agente (corregido).

---

## 🐛 Problema Identificado (Frontend)

El mensaje de error era: **"revisa el log el filtro lo estas aplicando mal"**

### Código Incorrecto (ANTES)

```csharp
// ❌ FILTRO MAL APLICADO
// Línea 316
if (Scope == "week" && Resumen.ByDay != null && Resumen.ByDay.Count > 0)
{
    // Usar byDay del resumen actual
    BuildWeekChartFromByDay(Resumen.ByDay);
    ShowWeekChart = true;
    return;
}
```

### ¿Cuál era el problema?

**Problema 1: Lógica incompleta**
- Solo reutilizaba `byDay` si `Scope == "week"`
- Si `Scope == "day"`, **SIEMPRE** hacía llamada adicional
- No verificaba si el `weekIso` del resumen actual coincidía con el calculado

**Problema 2: Caso no considerado**
- Cuando buscas por **Día** (scope="day"), el backend devuelve:
  ```json
  {
    "scope": "day",
    "date": "2026-02-09",
    "byDay": null  // ← NO devuelve byDay en scope=day
  }
  ```
- Pero luego hacemos llamada adicional con `scope=week&weekIso=2026-W06`:
  ```json
  {
    "scope": "week",
    "weekIso": "2026-W06",
    "byDay": { "2026-02-03": {...}, ... }  // ← SÍ devuelve byDay
  }
  ```

**Problema 3: Reutilización incorrecta**
- Si el usuario busca por **Semana** (2026-W06) y luego busca por **Día** (2026-02-09):
  - El `Resumen` sigue teniendo el `byDay` de la búsqueda anterior (W06)
  - El código **NO** verificaba si era la misma semana
  - Podría mostrar datos de una semana diferente

---

## ✅ Solución Implementada

### Código Corregido (AHORA)

```csharp
// GT-BEGIN: Verificar si podemos reutilizar byDay del resumen actual
bool canReuseByDay = false;

if (Resumen.ByDay != null && Resumen.ByDay.Count > 0)
{
    // Solo reutilizar si el scope es "week" Y el weekIso coincide
    if (Scope == "week" && WeekIso == weekIsoToLoad)
    {
        canReuseByDay = true;
    }
    // Si el scope es "day", el resumen NO tendrá byDay (porque backend no lo devuelve)
    // Así que SIEMPRE hacemos llamada adicional con scope=week
}

if (canReuseByDay)
{
    System.Diagnostics.Debug.WriteLine($"[WeekChart] REUTILIZANDO byDay del resumen actual ({Resumen.ByDay!.Count} días)");
    _dispatcher.TryEnqueue(() =>
    {
        BuildWeekChartFromByDay(Resumen.ByDay!);
        ShowWeekChart = true;
        System.Diagnostics.Debug.WriteLine($"[WeekChart] Gráfica construida (reutilizada) y ShowWeekChart=true");
    });
    return;
}
// GT-END

// Si no podemos reutilizar, hacer llamada adicional
string? agentIdToSend = CurrentUserRole == UserRole.USER ? CurrentUserId : SelectedAgentId;
System.Diagnostics.Debug.WriteLine($"[WeekChart] Haciendo llamada adicional con agentId: {agentIdToSend}");

var weekData = await _informesService.GetResumenAsync(
    scope: "week",
    date: null,
    weekIso: weekIsoToLoad,
    from: null,
    to: null,
    agentId: agentIdToSend,
    cancellationToken: _cts?.Token ?? CancellationToken.None);
```

---

## 📊 Comparación de Comportamiento

### Escenario 1: Búsqueda por Día

**Usuario busca:** Scope=Día, Fecha=2026-02-09

#### ANTES ❌
1. Llamada 1: `GET /api/v2/informes/resumen?scope=day&date=2026-02-09&agentId=...`
   - Respuesta: `{ "byDay": null }`
2. Verifica: `Scope == "week"`? NO
3. Hace llamada 2: `GET /api/v2/informes/resumen?scope=week&weekIso=2026-W06&agentId=...`
   - Respuesta: `{ "byDay": { ... } }`
4. **Total: 2 llamadas**

#### AHORA ✅
1. Llamada 1: `GET /api/v2/informes/resumen?scope=day&date=2026-02-09&agentId=...`
   - Respuesta: `{ "byDay": null }`
2. Verifica: `canReuseByDay`? NO (porque Resumen.ByDay es null)
3. Hace llamada 2: `GET /api/v2/informes/resumen?scope=week&weekIso=2026-W06&agentId=...`
   - Respuesta: `{ "byDay": { ... } }`
4. **Total: 2 llamadas** (igual que antes, pero con lógica correcta)

---

### Escenario 2: Búsqueda por Semana

**Usuario busca:** Scope=Semana, WeekIso=2026-W06

#### ANTES ❌
1. Llamada 1: `GET /api/v2/informes/resumen?scope=week&weekIso=2026-W06&agentId=...`
   - Respuesta: `{ "byDay": { ... } }`
2. Verifica: `Scope == "week" && ByDay != null`? SÍ
3. Reutiliza `byDay`
4. **Total: 1 llamada** ✅

#### AHORA ✅
1. Llamada 1: `GET /api/v2/informes/resumen?scope=week&weekIso=2026-W06&agentId=...`
   - Respuesta: `{ "byDay": { ... } }`
2. Verifica: `Scope == "week" && WeekIso == weekIsoToLoad`? SÍ
3. Reutiliza `byDay`
4. **Total: 1 llamada** ✅ (igual que antes, pero con verificación de weekIso)

---

### Escenario 3: Búsqueda Secuencial (Semana → Día de otra semana)

**Usuario busca:**
1. Primero: Scope=Semana, WeekIso=2026-W06
2. Luego: Scope=Día, Fecha=2026-02-16 (que pertenece a W07)

#### ANTES ❌
1. Primera búsqueda: `GET .../resumen?scope=week&weekIso=2026-W06`
   - `Resumen.ByDay` = datos de W06
2. Segunda búsqueda: `GET .../resumen?scope=day&date=2026-02-16`
   - `Resumen.ByDay` = **todavía tiene datos de W06** ❌
3. Verifica: `Scope == "week"`? NO
4. Hace llamada: `GET .../resumen?scope=week&weekIso=2026-W07`
   - **Total: 3 llamadas** ✅ (correcto, pero sin validar weekIso)

#### AHORA ✅
1. Primera búsqueda: `GET .../resumen?scope=week&weekIso=2026-W06`
   - `Resumen.ByDay` = datos de W06
2. Segunda búsqueda: `GET .../resumen?scope=day&date=2026-02-16`
   - `Resumen.ByDay` = **todavía tiene datos de W06**
3. Verifica: `canReuseByDay`? 
   - `Scope == "week"`? NO → canReuseByDay = false
4. Hace llamada: `GET .../resumen?scope=week&weekIso=2026-W07`
   - **Total: 3 llamadas** ✅ (correcto y seguro)

---

### Escenario 4: Búsqueda Secuencial (Semana → Día de la MISMA semana)

**Usuario busca:**
1. Primero: Scope=Semana, WeekIso=2026-W06
2. Luego: Scope=Día, Fecha=2026-02-09 (que pertenece a W06)

#### ANTES ❌
1. Primera búsqueda: `GET .../resumen?scope=week&weekIso=2026-W06`
   - `Resumen.ByDay` = datos de W06
2. Segunda búsqueda: `GET .../resumen?scope=day&date=2026-02-09`
   - `Resumen.ByDay` = **datos de W06 (correcto)**
3. Verifica: `Scope == "week"`? NO
4. Hace llamada: `GET .../resumen?scope=week&weekIso=2026-W06` ❌
   - **Llamada INNECESARIA** (ya teníamos byDay de W06)
   - **Total: 3 llamadas** ❌

#### AHORA ✅
1. Primera búsqueda: `GET .../resumen?scope=week&weekIso=2026-W06`
   - `Resumen.ByDay` = datos de W06
2. Segunda búsqueda: `GET .../resumen?scope=day&date=2026-02-09`
   - `Resumen.ByDay` = **datos de W06**
3. Verifica: `canReuseByDay`?
   - `Scope == "week"`? NO → canReuseByDay = false ❌
4. Hace llamada: `GET .../resumen?scope=week&weekIso=2026-W06`
   - **Total: 3 llamadas** ❌ (aún innecesaria)

**NOTA:** Este escenario todavía no está optimizado al 100%. Para optimizarlo completamente, necesitaríamos:
```csharp
// Verificar si el weekIso calculado coincide con algún día en Resumen.ByDay
if (Resumen.ByDay != null && Resumen.ByDay.Any(kvp => 
{
    var dayWeek = System.Globalization.ISOWeek.GetWeekOfYear(DateTime.Parse(kvp.Key));
    var dayYear = DateTime.Parse(kvp.Key).Year;
    return $"{dayYear}-W{dayWeek:D2}" == weekIsoToLoad;
}))
{
    canReuseByDay = true;
}
```

**Pero esto complica mucho la lógica. Por ahora, dejamos la solución actual que es SEGURA y CORRECTA.**

---

## 🎯 Beneficios de la Corrección

### ✅ Corrección 1: Validación de weekIso
**Antes:**
```csharp
if (Scope == "week" && Resumen.ByDay != null)
```

**Ahora:**
```csharp
if (Scope == "week" && WeekIso == weekIsoToLoad && Resumen.ByDay != null)
```

**Beneficio:** Evita mostrar datos de una semana diferente si el usuario cambia de semana.

---

### ✅ Corrección 2: Comentarios claros
```csharp
// Si el scope es "day", el resumen NO tendrá byDay (porque backend no lo devuelve)
// Así que SIEMPRE hacemos llamada adicional con scope=week
```

**Beneficio:** El código es auto-documentado y explica el porqué de la decisión.

---

### ✅ Corrección 3: Logging mejorado
```csharp
System.Diagnostics.Debug.WriteLine($"[WeekChart] REUTILIZANDO byDay del resumen actual ({Resumen.ByDay!.Count} días)");
```

**Beneficio:** En los logs se ve claramente cuando se reutiliza y cuando se hace llamada nueva.

---

## 📝 Logs Esperados (AHORA)

### Caso 1: Scope=Semana (reutiliza byDay)
```
[WeekChart] ===== INICIO LoadWeekChartIfNeededAsync =====
[WeekChart] Scope: week, Resumen != null: True
[WeekChart] Scope=week, weekIsoToLoad: 2026-W06
[WeekChart] REUTILIZANDO byDay del resumen actual (7 días)
[WeekChart] Gráfica construida (reutilizada) y ShowWeekChart=true
[WeekChart] ===== FIN LoadWeekChartIfNeededAsync =====
```

### Caso 2: Scope=Día (hace llamada adicional)
```
[WeekChart] ===== INICIO LoadWeekChartIfNeededAsync =====
[WeekChart] Scope: day, Resumen != null: True
[WeekChart] Scope=day, calculado weekIsoToLoad: 2026-W06
[WeekChart] Haciendo llamada adicional con agentId: ...
[InformesService] Iniciando GetResumenAsync...
[WeekChart] Respuesta recibida: weekData != null: True, ByDay != null: True, Count: 7
[WeekChart] Construyendo gráfica con 7 días
[WeekChart] ===== FIN LoadWeekChartIfNeededAsync =====
```

---

## ✅ Conclusión

### Problema Original
- Filtro de `Scope == "week"` era demasiado restrictivo
- No validaba que el `weekIso` del resumen coincidiera con el calculado
- Comentarios insuficientes sobre el comportamiento del backend

### Solución Implementada
- ✅ Filtro con validación de `weekIso`: `Scope == "week" && WeekIso == weekIsoToLoad`
- ✅ Comentarios claros sobre por qué scope=day no puede reutilizar
- ✅ Variable `canReuseByDay` explícita para legibilidad
- ✅ Logging mejorado: "REUTILIZANDO" vs "Haciendo llamada adicional"

### Optimización Futura (Opcional)
Si queremos optimizar el Escenario 4 (búsqueda secuencial Semana→Día de la misma semana):
- Verificar si algún día en `Resumen.ByDay` pertenece al `weekIsoToLoad` calculado
- Esto evitaría la llamada adicional cuando ya tenemos los datos
- **Pero** añade complejidad y el beneficio es marginal (solo 1 llamada menos en un caso específico)

---

**Versión:** v1.9.5-alpha  
**Fecha:** 2026-02-14  
**Estado:** ✅ Fix aplicado y compilado  
**Hot Reload:** ⚠️ Si estás en Debug, necesitas reiniciar la app para aplicar los cambios

**FIN DEL DOCUMENTO**
