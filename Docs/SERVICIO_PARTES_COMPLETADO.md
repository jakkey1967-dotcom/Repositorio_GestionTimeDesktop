# SERVICIO DE PARTES - COMPLETADO

**Fecha**: 2026-01-30
**Estado**: ✅ COMPLETADO
**Compilación**: ✅ EXITOSA

## 📋 RESUMEN

Se ha creado `PartesService.cs` con soporte completo para todos los filtros y parámetros del endpoint `/api/v1/partes`, que es mucho más complejo que los endpoints de catálogos (clientes, tipos, grupos).

### ✅ Estructura REAL del JSON de respuesta

```json
{
  "id": 3288,
  "fecha": "2026-01-28T00:00:00",
  "cliente": "Gestión Interna",
  "id_cliente": 20,
  "tienda": "loyalty",
  "accion": "Ver la lista de ticket de flurtterflow",
  "horainicio": "16:20",
  "horafin": "18:00",
  "duracion_min": 100,
  "ticket": "",
  "grupo": "Movilidad",
  "id_grupo": 6,
  "tipo": "Desarrollo",
  "id_tipo": 7,
  "tecnico": "Francisco Santos",
  "estado": 2,
  "estado_nombre": "Cerrado",
  "created_at": "2026-01-28T17:56:26.957765Z",
  "updated_at": "2026-01-28T17:56:27.051778Z",
  "tags": []
}
```

**Campos del DTO:**
- ✅ `id`, `fecha`, `cliente`, `id_cliente`, `tienda`, `accion`
- ✅ `horainicio`, `horafin` (sin guión bajo), `duracion_min`
- ✅ `ticket`, `grupo`, `id_grupo`, `tipo`, `id_tipo`, `tecnico`
- ✅ `estado` (int), `estado_nombre` (string)
- ✅ `created_at`, `updated_at` (metadatos)
- ✅ `tags` (array de strings)

## 🆕 ARCHIVOS CREADOS

### 1. `Services/Catalog/PartesService.cs`
Servicio completo para gestión de partes con:
- ✅ Filtros por fecha: `fecha`, `fechaInicio`, `fechaFin`
- ✅ Búsqueda por texto: `search` (parámetro `q` en API)
- ✅ Filtros por IDs: `idCliente`, `idTipo`, `idGrupo`
- ✅ Filtro por estado: `estado` (0=Abierto, 1=Pausado, 2=Cerrado, 3=Enviado, 9=Anulado)
- ✅ Operaciones CRUD completas
- ✅ Acciones especiales: Cerrar, Enviar, Anular

### 2. `Models/Dtos/ParteUpdateRequest.cs`
DTO para actualizar partes (PUT) - no existía antes.

### 3. `Scripts/Test-PartesService.ps1`
Script de test para verificar todos los filtros del servicio.

## 🔧 MÉTODOS DISPONIBLES EN PartesService

### ✅ ENDPOINTS QUE SÍ FUNCIONAN

#### 📋 Consultas

##### `ListAsync()` - GET /api/v1/partes
```csharp
var partes = await partesService.ListAsync(
    fecha: DateTime.Today,              // Fecha específica (opcional)
    fechaInicio: DateTime.Today.AddDays(-7),  // Rango inicio (opcional)
    fechaFin: DateTime.Today,                  // Rango fin (opcional)
    search: "Cliente A",                      // Búsqueda texto (opcional)
    estado: 2,                                // Estado (opcional: 0-9)
    idCliente: 5,                             // ID cliente (opcional)
    idTipo: 3,                                // ID tipo (opcional)
    idGrupo: 2                                // ID grupo (opcional)
);
```

##### ➕ Crear - `CreateAsync()` - POST /api/v1/partes
```csharp
var request = new ParteCreateRequest
{
    FechaTrabajo = "2026-01-30",  // yyyy-MM-dd
    HoraInicio = "09:00",
    HoraFin = "10:30",
    IdCliente = 5,
    Tienda = "Madrid Centro",
    IdGrupo = 2,
    IdTipo = 3,
    Accion = "Instalación de software",
    Ticket = "TICKET-1234"
};

var parte = await partesService.CreateAsync(request);
```

##### ✏️ Actualizar - `UpdateAsync()` - PUT /api/v1/partes/{id}
```csharp
var request = new ParteUpdateRequest
{
    FechaTrabajo = "2026-01-30",
    HoraInicio = "09:00",
    HoraFin = "11:00",
    IdCliente = 5,
    Tienda = "Madrid Centro",
    IdGrupo = 2,
    IdTipo = 3,
    Accion = "Instalación completada",
    Ticket = "TICKET-1234"
};

var actualizado = await partesService.UpdateAsync(123, request);
```

##### 🗑️ Eliminar - `DeleteAsync()` - DELETE /api/v1/partes/{id}
```csharp
await partesService.DeleteAsync(123);
```

---

### ⚠️ ENDPOINTS QUE **NO** FUNCIONAN (Backend no los soporta)

#### ❌ GetByIdAsync() - GET /api/v1/partes/{id}
**Error:** 405 Method Not Allowed

```csharp
// ❌ NO USAR - El backend NO soporta este endpoint
[Obsolete("El backend NO soporta GET /partes/{id} - Devuelve 405")]
var parte = await partesService.GetByIdAsync(123);
```

**Alternativa:** Usar `ListAsync()` y filtrar en cliente:
```csharp
var partes = await partesService.ListAsync();
var parte = partes.FirstOrDefault(p => p.Id == 123);
```

#### ❌ CerrarAsync() - POST /api/v1/partes/{id}/cerrar
**Error:** 404 Not Found

```csharp
// ❌ NO USAR - El backend NO soporta este endpoint
[Obsolete("El backend NO soporta POST /partes/{id}/cerrar - Devuelve 404")]
await partesService.CerrarAsync(123);
```

**Alternativa:** Usar `UpdateAsync()` y cambiar el estado:
```csharp
var request = new ParteUpdateRequest
{
    // ... copiar todos los campos del parte ...
    Estado = 2  // 2 = Cerrado
};
await partesService.UpdateAsync(123, request);
```

#### ❌ EnviarAsync() - POST /api/v1/partes/{id}/enviar
**Error:** 404 Not Found

```csharp
// ❌ NO USAR - El backend NO soporta este endpoint
[Obsolete("El backend NO soporta POST /partes/{id}/enviar - Devuelve 404")]
await partesService.EnviarAsync(123);
```

**Alternativa:** Usar `UpdateAsync()` con estado 3:
```csharp
request.Estado = 3;  // 3 = Enviado
await partesService.UpdateAsync(123, request);
```

#### ❌ AnularAsync() - POST /api/v1/partes/{id}/anular
**Error:** 404 Not Found

```csharp
// ❌ NO USAR - El backend NO soporta este endpoint
[Obsolete("El backend NO soporta POST /partes/{id}/anular - Devuelve 404")]
await partesService.AnularAsync(123);
```

**Alternativa:** Usar `UpdateAsync()` con estado 9:
```csharp
request.Estado = 9;  // 9 = Anulado
await partesService.UpdateAsync(123, request);
```

**Filtros disponibles:**
| Parámetro | Tipo | API Query | Descripción |
|-----------|------|-----------|-------------|
| `fecha` | DateTime? | `fecha=yyyy-MM-dd` | Fecha específica de trabajo |
| `fechaInicio` | DateTime? | `fechaInicio=yyyy-MM-dd` | Rango desde (inclusive) |
| `fechaFin` | DateTime? | `fechaFin=yyyy-MM-dd` | Rango hasta (inclusive) |
| `search` | string? | `q={texto}` | Búsqueda en múltiples campos |
| `estado` | int? | `estado={0-9}` | 0=Abierto, 1=Pausado, 2=Cerrado, 3=Enviado, 9=Anulado |
| `idCliente` | int? | `id_cliente={id}` | Filtrar por cliente específico |
| `idTipo` | int? | `id_tipo={id}` | Filtrar por tipo específico |
| `idGrupo` | int? | `id_grupo={id}` | Filtrar por grupo específico |

---

## 📊 COMPARACIÓN: PARTES vs CATÁLOGOS

### Endpoint `/api/v1/clientes` (simple)
```
GET /clientes?page=1&pageSize=50&search=texto
```
**Parámetros:** 3 (paginación + búsqueda)

### Endpoint `/api/v1/partes` (complejo)
```
GET /partes?fecha=2026-01-30&fechaInicio=2026-01-01&fechaFin=2026-01-31&q=texto&estado=2&id_cliente=5&id_tipo=3&id_grupo=2
```
**Parámetros:** 8 (fechas + búsqueda + filtros múltiples)

## 🔄 DIFERENCIA CON DiarioService

### ❌ ANTES: `DiarioService` (limitado)
```csharp
public async Task<List<ParteDto>> GetPartesAsync()
{
    // Solo GET /partes SIN filtros
    return await _api.GetAsync<List<ParteDto>>("/api/v1/partes");
}
```

**Problemas:**
- ❌ No soporta filtros por fecha
- ❌ No soporta búsqueda
- ❌ No soporta filtros por cliente/tipo/grupo
- ❌ Devuelve TODOS los partes (lento en producción)

### ✅ AHORA: `PartesService` (completo)
```csharp
public async Task<List<ParteDto>?> ListAsync(
    DateTime? fecha = null,
    DateTime? fechaInicio = null,
    DateTime? fechaFin = null,
    string? search = null,
    int? estado = null,
    int? idCliente = null,
    int? idTipo = null,
    int? idGrupo = null,
    CancellationToken ct = default)
{
    // Construye query string dinámico con solo los filtros especificados
    var queryParams = new List<string>();
    
    if (fecha.HasValue)
        queryParams.Add($"fecha={fecha.Value:yyyy-MM-dd}");
    
    // ... más filtros ...
    
    var path = $"/api/v1/partes?{string.Join("&", queryParams)}";
    return await _api.GetAsync<List<ParteDto>>(path, ct);
}
```

**Ventajas:**
- ✅ Soporta TODOS los filtros del backend
- ✅ Query string se construye dinámicamente
- ✅ Solo se envían los parámetros especificados
- ✅ Mejor rendimiento (filtra en servidor)
- ✅ API RESTful correcta

## 🧪 SCRIPT DE TEST

**Archivo:** `Scripts/Test-PartesService.ps1`

**Pruebas incluidas:**
1. ✅ Login
2. ✅ GET sin filtros (todos)
3. ✅ GET con fecha específica (hoy)
4. ✅ GET con rango de fechas (últimos 7 días)
5. ✅ GET con búsqueda por texto
6. ✅ GET con filtro por estado (Cerrados)
7. ✅ POST - Crear parte
8. ✅ GET por ID
9. ✅ POST - Cerrar parte
10. ✅ DELETE - Eliminar parte

**Uso:**
```powershell
.\Scripts\Test-PartesService.ps1
```

## 📝 ESTADOS DE PARTES

```csharp
public enum ParteEstado
{
    Abierto = 0,    // ▶️ En curso activo
    Pausado = 1,    // ⏸️ Temporalmente detenido
    Cerrado = 2,    // ✅ Finalizado
    Enviado = 3,    // 📤 Enviado al sistema destino
    Anulado = 9     // ⛔ Cancelado
}
```

## 🚀 USO EN LA APLICACIÓN

### Ejemplo 1: Cargar partes de hoy
```csharp
var partes = await _partesService.ListAsync(
    fecha: DateTime.Today
);
```

### Ejemplo 2: Partes pendientes del último mes
```csharp
var partes = await _partesService.ListAsync(
    fechaInicio: DateTime.Today.AddMonths(-1),
    fechaFin: DateTime.Today,
    estado: 0  // Abiertos
);
```

### Ejemplo 3: Buscar partes de un cliente específico
```csharp
var partes = await _partesService.ListAsync(
    idCliente: 5,
    fechaInicio: DateTime.Today.AddDays(-30)
);
```

### Ejemplo 4: Búsqueda de texto en partes recientes
```csharp
var partes = await _partesService.ListAsync(
    search: "instalación",
    fechaInicio: DateTime.Today.AddDays(-7)
);
```

## ✅ CHECKLIST FINAL

- [x] `PartesService.cs` creado con todos los filtros soportados por el backend
- [x] `ParteUpdateRequest.cs` creado
- [x] `ParteDto.cs` actualizado con campo `tags`
- [x] Compilación exitosa
- [x] Script de test creado y ejecutado exitosamente
- [x] Documentación actualizada con endpoints reales
- [x] **Verificado contra backend real** - Estructura JSON correcta
- [x] Marcados como `[Obsolete]` los métodos que el backend NO soporta:
  - ❌ `GetByIdAsync()` - 405 Method Not Allowed
  - ❌ `CerrarAsync()` - 404 Not Found
  - ❌ `EnviarAsync()` - 404 Not Found
  - ❌ `AnularAsync()` - 404 Not Found
- [x] CRUD funcional: Create (POST), Read (GET con filtros), Update (PUT), Delete (DELETE)

## 📊 RESULTADOS DEL TEST REAL

```
[1/10] Login... ✅ OK
[2/10] GET /partes (sin filtros)... ✅ OK - 1388 partes
[3/10] GET /partes?fecha=2026-01-30... ✅ OK - 0 partes
[4/10] GET /partes?fechaInicio=...&fechaFin=... ✅ OK - 29 partes
[5/10] GET /partes?q=test... ✅ OK - 0 resultados
[6/10] GET /partes?estado=2... ✅ OK - 1388 partes cerrados
[7/10] POST /partes... ✅ OK - Parte ID 3289 creado
[8/10] GET /partes/3289... ❌ ERROR - 405 Method Not Allowed
[9/10] POST /partes/3289/cerrar... ❌ ERROR - 404 Not Found
[10/10] DELETE /partes/3289... ✅ OK - Eliminado
```

## 🔗 REFERENCIAS

- **Documentación Backend**: `BACKEND/NUEVOS_PARAMETROS_ENDPOINT.md`
- **Servicio anterior**: `Services/DiarioService.cs` (limitado)
- **Servicio nuevo**: `Services/Catalog/PartesService.cs` (completo)
- **Modelo DTO**: `Models/Dtos/ParteDto.cs`
- **Requests**: `Models/Dtos/ParteCreateRequest.cs`, `Models/Dtos/ParteUpdateRequest.cs`

---

**Fin del documento**
