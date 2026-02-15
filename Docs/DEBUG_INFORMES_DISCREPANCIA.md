# 🔍 DIAGNÓSTICO: Discrepancia en Informes de Partes

**Fecha:** 2026-02-09  
**Versión:** v1.9.5-alpha  
**Archivo:** `Docs/DEBUG_INFORMES_DISCREPANCIA.md`

---

## 📊 Problema Detectado

### Datos Esperados (Fuente: DiarioPage)
Para el día **09/02/2026**:

```
📊 TIEMPO REAL OCUPADO (SIN SOLAPAMIENTO)
📦 Cubierto: 8h 30m
📋 Partes con tiempo registrado: 5
🔗 Intervalos cubiertos (unidos): 2

Intervalos:
• 08:30–13:30 (5h)
• 15:00–18:30 (3h 30m)
```

**Total esperado:**
- ✅ **Partes:** 5
- ✅ **Tiempo cubierto:** 8h 30m (510 minutos)
- ✅ **Intervalos:** 2

---

### Datos Recibidos (Fuente: ReportsWindow - Endpoint `/api/v2/informes/resumen`)
Para el día **09/02/2026**:

```
📊 Informes de Partes

✅ Jornada completa (>=8h)

📋 Partes: 11
⏱️ Registrado: 15h 07m (907 minutos)
✅ Real (sin solape): 10h 13m (613 minutos)
⚠️ Solape: 4h 54m (294 minutos)

🕐 Inicio Global: 2026-02-09T07:55:00
🕕 Fin Global: 2026-02-09T19:38:00
```

**Total recibido:**
- ❌ **Partes:** 11 (esperado: 5)
- ❌ **Tiempo cubierto:** 10h 13m (esperado: 8h 30m)
- ❌ **Tiempo registrado:** 15h 07m
- ❌ **Solape:** 4h 54m

---

## 🔍 Discrepancias Identificadas

| Métrica | Esperado | Recibido | Diferencia |
|---------|----------|----------|------------|
| **Partes** | 5 | 11 | +6 partes (120%) |
| **Cubierto** | 8h 30m (510 min) | 10h 13m (613 min) | +1h 43m (+103 min) |
| **Inicio Global** | ~08:30 | 07:55:00 | -35 min |
| **Fin Global** | ~18:30 | 19:38:00 | +1h 08m |

---

## 🧪 Hipótesis

### 1. **Duplicación de Partes en el Backend** ⭐ (Más probable)
- El endpoint `/api/v2/informes/resumen` podría estar devolviendo partes duplicados.
- Verificar si hay un problema de JOIN en la query SQL del backend.

### 2. **Filtro de Fecha Incorrecto**
- El parámetro `date=2026-02-09` no se está aplicando correctamente.
- Podrían estar incluyéndose partes de otros días.

### 3. **Problema de Timezone**
- El backend podría estar usando una zona horaria diferente.
- Los timestamps pueden estar cruzando la frontera del día.

### 4. **Agente Incorrecto**
- Si se está pasando un `agentId` incorrecto, podría estar trayendo partes de otros usuarios.
- El usuario USER solo debería ver sus propios partes.

---

## 🛠️ Diagnóstico Añadido

### Cambios realizados en `InformesService.cs`:

```csharp
// GT-BEGIN: Añadir logging detallado
_log?.LogInformation("📊 [InformesService] Iniciando GetResumenAsync - Scope: {scope}, Date: {date}, WeekIso: {weekIso}, From: {from}, To: {to}, AgentId: {agentId}",
    scope, date, weekIso, from, to, agentId);

// ... construcción de endpoint ...

_log?.LogInformation("📊 [InformesService] Endpoint construido: {endpoint}", endpoint);

// ... respuesta ...

if (result != null)
{
    _log?.LogInformation("📊 [InformesService] Respuesta recibida - Partes: {partes}, Registrado: {recorded}min, Real: {covered}min, Solape: {overlap}min, Inicio: {start}, Fin: {end}",
        result.PartsCount, result.RecordedMinutes, result.CoveredMinutes, result.OverlapMinutes, result.FirstStart, result.LastEnd);
    
    if (result.MergedIntervals?.Count > 0)
    {
        _log?.LogInformation("📊 [InformesService] Intervalos cubiertos: {count}", result.MergedIntervals.Count);
        foreach (var interval in result.MergedIntervals)
        {
            _log?.LogInformation("  ↳ {start} - {end} ({minutes}min)", interval.Start, interval.End, interval.Minutes);
        }
    }
}
// GT-END
```

---

## 📝 Pasos para Reproducir

1. **Desde Desktop:**
   - Abrir `ReportsWindow` (menú Informes)
   - Seleccionar alcance "Día"
   - Fecha: `09/02/2026`
   - Clic en "🔍 Buscar"
   - Observar discrepancia

2. **Desde Backend (PowerShell):**
   ```powershell
   .\Scripts\Test-InformesEndpoint.ps1
   ```
   - Introducir token JWT
   - Revisar respuesta JSON

---

## 🔧 Solución Propuesta

### 1. **Verificar el Backend**

Revisar la implementación del endpoint en el backend:
```
GestionTimeApi/Controllers/InformesController.cs
```

Verificar:
- ✅ Query SQL no genera duplicados (revisar JOINs)
- ✅ Filtro por fecha se aplica correctamente
- ✅ Filtro por `agentId` funciona (cuando se proporciona)
- ✅ Timezone es consistente con el frontend

### 2. **Validar en Base de Datos**

Ejecutar query directa:
```sql
SELECT 
    COUNT(*) AS total_partes,
    SUM(TIMESTAMPDIFF(MINUTE, hIni, hFin)) AS minutos_registrados
FROM partes
WHERE DATE(hIni) = '2026-02-09'
  AND agente_id = <tu_agente_id>;
```

Debería devolver:
- `total_partes = 5`
- `minutos_registrados` cercano a 510 minutos

### 3. **Comparar con DiarioPage**

El endpoint usado en `DiarioPage` para el banner de "Tiempo Real Ocupado":
```
GET /api/v2/partes/intervalos-cubiertos?from=2026-02-09&to=2026-02-09
```

Este endpoint **SÍ** devuelve los datos correctos. Comparar implementación con `/api/v2/informes/resumen`.

---

## 📊 Archivos Modificados

1. **`Services/Reports/InformesService.cs`**
   - Añadido logging detallado para diagnóstico
   - Logs de parámetros, endpoint y respuesta

2. **`Scripts/Test-InformesEndpoint.ps1`** (NUEVO)
   - Script para diagnosticar endpoint directamente desde PowerShell
   - Muestra respuesta JSON completa
   - Detecta discrepancias automáticamente

---

## ⚠️ Estado Actual

**✅ PROBLEMA RESUELTO - Fix implementado.**

### 🎯 Causa Raíz Identificada

El problema NO estaba en el backend. **El backend funciona correctamente**.

**Causa real:** El frontend NO estaba enviando el `agentId` en las consultas, por lo que el backend devolvía **todos los partes de todos los usuarios** para la fecha solicitada.

### 📊 Comparación de Consultas

#### ❌ ANTES (Sin agentId):
```
GET /api/v2/informes/resumen?scope=day&date=2026-02-09
```
**Resultado:** 11 partes (todos los usuarios) ❌

#### ✅ DESPUÉS (Con agentId):
```
GET /api/v2/informes/resumen?scope=day&date=2026-02-09&agentId=b455821b-e481-4969-825d-817ee4e85184
```
**Resultado:** 5 partes (solo del usuario) ✅

### 🔧 Fix Implementado

#### 1. **InformesService.cs**
- Cambiado tipo de `agentId` de `int?` a `string?` (GUID)
- Actualizado query param para escapar correctamente el GUID

#### 2. **ReportsViewModel.cs**
- Añadido campo `CurrentUserId` (GUID del usuario)
- Cambiado `SelectedAgentId` de `int?` a `string?`
- **Lógica correcta:**
  - `USER`: Siempre envía su propio `agentId`
  - `EDITOR`/`ADMIN`: Envía `agentId` seleccionado o `null` (ve todos)

#### 3. **ReportsWindow.xaml.cs**
- Pasa `App.CurrentUserProfile?.Id` al constructor del ViewModel

### ✅ Resultado Final

**Para usuarios USER:**
```json
{
  "agentId": "b455821b-e481-4969-825d-817ee4e85184",
  "partsCount": 5,
  "recordedMinutes": 510,
  "coveredMinutes": 510,
  "overlapMinutes": 0,
  "mergedIntervals": [
    { "start": "2026-02-09T08:30:00", "end": "2026-02-09T13:30:00", "minutes": 300 },
    { "start": "2026-02-09T15:00:00", "end": "2026-02-09T18:30:00", "minutes": 210 }
  ]
}
```

**✅ Datos correctos:** 5 partes, 8h 30m cubierto

---

**Última actualización:** 2026-02-14 (Problema resuelto)  
**Estado:** 🟢 Fix completado e implementado
