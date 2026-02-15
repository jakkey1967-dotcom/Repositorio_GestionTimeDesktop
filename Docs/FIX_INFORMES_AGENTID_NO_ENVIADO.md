# ✅ FIX INFORMES - AgentId No Se Enviaba

**Fecha:** 2026-02-14  
**Versión:** v1.9.5-alpha  
**Estado:** 🟢 Fix completado y verificado

---

## 🎯 Problema

El frontend de `ReportsWindow` mostraba **11 partes** en lugar de **5 partes** para el día 2026-02-09.

### Síntomas
- Usuarios veían partes de TODOS los usuarios, no solo los suyos
- Tiempo cubierto inflado (10h 13m en lugar de 8h 30m)
- Solape incorrecto (4h 54m detectado incorrectamente)

---

## 🔍 Causa Raíz

**El frontend NO estaba enviando el parámetro `agentId` en las consultas al endpoint.**

### Comportamiento del Backend (correcto):
```csharp
// Sin agentId → Devuelve TODOS los partes de TODOS los usuarios
GET /api/v2/informes/resumen?scope=day&date=2026-02-09
→ Resultado: 11 partes (todos los usuarios) ❌

// Con agentId → Devuelve solo los partes del usuario especificado
GET /api/v2/informes/resumen?scope=day&date=2026-02-09&agentId=b455821b-e481-4969-825d-817ee4e85184
→ Resultado: 5 partes (solo del usuario) ✅
```

**El backend estaba funcionando correctamente.** El problema era del frontend.

---

## 🔧 Solución Implementada

### 1. **Services/Reports/InformesService.cs**

#### Cambio de tipo: `int?` → `string?`
```csharp
// GT-BEGIN: Corregir tipo de agentId
public async Task<InformeResumenDto?> GetResumenAsync(
    string scope,
    string? date = null,
    string? weekIso = null,
    string? from = null,
    string? to = null,
    string? agentId = null,  // ← Ahora es string (GUID) en lugar de int
    CancellationToken cancellationToken = default)
{
    // ...
    if (!string.IsNullOrWhiteSpace(agentId))
        queryParams.Add($"agentId={Uri.EscapeDataString(agentId)}");
}
// GT-END
```

**Razón:** El backend usa GUIDs (string) como `agentId`, no enteros.

---

### 2. **ViewModels/Reports/ReportsViewModel.cs**

#### Añadido campo para almacenar el ID del usuario actual
```csharp
[ObservableProperty] private string? _currentUserId;  // ← ID del agente del usuario actual
[ObservableProperty] private string? _selectedAgentId;  // ← Ahora es string (GUID)
```

#### Actualizado constructor para recibir el `currentUserId`
```csharp
public ReportsViewModel(InformesService informesService, UserRole userRole, string? currentUserId = null)
{
    _informesService = informesService;
    _dispatcher = DispatcherQueue.GetForCurrentThread();
    _currentUserRole = userRole;
    _currentUserId = currentUserId;  // ← Nuevo
    _canSelectAgent = userRole is UserRole.EDITOR or UserRole.ADMIN;
}
```

#### Lógica correcta para enviar agentId
```csharp
// GT-BEGIN: Enviar agentId correcto
string? agentIdToSend = null;

if (CurrentUserRole == UserRole.USER)
{
    // Usuarios USER siempre ven solo sus propios partes
    agentIdToSend = CurrentUserId;  // ← Enviar su propio ID
}
else
{
    // EDITOR/ADMIN: Solo envía agentId si seleccionaron uno específicamente
    agentIdToSend = SelectedAgentId;  // ← Opcional, puede ser null
}

var result = await _informesService.GetResumenAsync(
    scope: Scope,
    date: Scope == "day" ? SelectedDate.ToString("yyyy-MM-dd") : null,
    weekIso: Scope == "week" ? WeekIso : null,
    from: Scope == "range" && RangeFrom.HasValue ? RangeFrom.Value.ToString("yyyy-MM-dd") : null,
    to: Scope == "range" && RangeTo.HasValue ? RangeTo.Value.ToString("yyyy-MM-dd") : null,
    agentId: agentIdToSend,  // ← Enviar agentId correcto
    cancellationToken: _cts.Token);
// GT-END
```

**Lógica:**
- **Usuarios USER:** Siempre envían su propio `agentId` → ven solo sus partes
- **EDITOR/ADMIN:** Pueden seleccionar un usuario específico o dejar en blanco para ver todos

---

### 3. **Views/Reports/ReportsWindow.xaml.cs**

#### Pasar el ID del usuario actual al ViewModel
```csharp
public ReportsWindow(InformesService informesService, UserRole userRole, Window parentWindow)
{
    // Obtener el ID del usuario actual desde el perfil
    var currentUserId = App.CurrentUserProfile?.Id;  // ← Obtener GUID del usuario
    
    ViewModel = new ReportsViewModel(informesService, userRole, currentUserId);  // ← Pasar al ViewModel
    _parentWindow = parentWindow;

    this.InitializeComponent();
    // ...
}
```

---

## ✅ Resultado

### Antes del Fix
```json
{
  "partsCount": 11,  // ❌ Todos los usuarios
  "recordedMinutes": 907,
  "coveredMinutes": 613,
  "overlapMinutes": 294
}
```

### Después del Fix
```json
{
  "agentId": "b455821b-e481-4969-825d-817ee4e85184",  // ✅ Usuario específico
  "partsCount": 5,  // ✅ Solo del usuario
  "recordedMinutes": 510,
  "coveredMinutes": 510,
  "overlapMinutes": 0,
  "mergedIntervals": [
    { "start": "2026-02-09T08:30:00", "end": "2026-02-09T13:30:00", "minutes": 300 },
    { "start": "2026-02-09T15:00:00", "end": "2026-02-09T18:30:00", "minutes": 210 }
  ]
}
```

---

## 📊 Comparación

| Métrica | ANTES (sin agentId) | DESPUÉS (con agentId) |
|---------|---------------------|----------------------|
| **Partes** | 11 (todos) ❌ | 5 (solo usuario) ✅ |
| **Tiempo Registrado** | 907 min ❌ | 510 min ✅ |
| **Tiempo Cubierto** | 613 min ❌ | 510 min ✅ |
| **Solape** | 294 min ❌ | 0 min ✅ |
| **Intervalos** | Incorrectos ❌ | Correctos ✅ |

---

## 📦 Archivos Modificados

| Archivo | Cambios |
|---------|---------|
| `Services/Reports/InformesService.cs` | Tipo `agentId`: `int?` → `string?` |
| `ViewModels/Reports/ReportsViewModel.cs` | Añadido `CurrentUserId`, lógica para enviar `agentId` |
| `Views/Reports/ReportsWindow.xaml.cs` | Pasar `App.CurrentUserProfile?.Id` al ViewModel |
| `Docs/DEBUG_INFORMES_DISCREPANCIA.md` | Actualizado con el fix |

---

## 🎯 Lecciones Aprendidas

1. **Siempre verificar los parámetros enviados al backend**
   - El backend estaba funcionando correctamente
   - El problema era que NO se enviaba el parámetro `agentId`

2. **Tipos de datos consistentes**
   - Backend usa `string` (GUID) para `agentId`
   - Frontend ahora también usa `string` (no `int`)

3. **Roles y permisos**
   - `USER`: Debe ver SOLO sus partes (agentId obligatorio)
   - `EDITOR`/`ADMIN`: Puede ver todos o filtrar por usuario

---

## ✅ Verificación

**Comando para verificar:**
```csharp
// En logs, buscar:
[InformesService] Endpoint construido: /api/v2/informes/resumen?scope=day&date=2026-02-09&agentId=...
```

**Debe incluir `&agentId=<GUID>` para usuarios USER.**

---

**Versión:** v1.9.5-alpha  
**Estado:** 🟢 Fix completado y verificado  
**Compilación:** ✅ Sin errores

---

**FIN DEL DOCUMENTO**
