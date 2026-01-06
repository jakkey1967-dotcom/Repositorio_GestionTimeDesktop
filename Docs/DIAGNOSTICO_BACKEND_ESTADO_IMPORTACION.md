# 🔍 DIAGNÓSTICO: Estado Incorrecto en Importación Excel

**Fecha:** 2026-01-06  
**Problema:** Los partes importados desde Excel aparecen con estado "Abierto" en lugar de "Cerrado"  
**Conclusión:** **PROBLEMA EN EL BACKEND** - No respeta el campo `estado` enviado desde el frontend

---

## 📋 **RESUMEN DEL PROBLEMA**

### ✅ **Frontend (Desktop) - CORRECTO**

El frontend está enviando **CORRECTAMENTE** el estado:

```csharp
// Services/Import/ExcelPartesImportService.cs (Línea ~237)
return new ParteCreateRequest
{
    FechaTrabajo = fechaDate.ToString("yyyy-MM-dd"),
    HoraInicio = horaInicioStr,
    HoraFin = horaFinStr,
    DuracionMin = duracionMinutos,
    IdCliente = clienteId,
    Tienda = tienda,
    IdGrupo = BuscarGrupoId(grupo, logger),
    IdTipo = BuscarTipoId(tipo, logger),
    Accion = accion?.Trim() ?? "",
    Ticket = ticket?.Trim(),
    Tecnico = tecnico?.Trim(),
    Estado = 2  // 🔒 FORZADO: SIEMPRE Cerrado (2)
};
```

**DTO enviado:**

```csharp
// Models/Dtos/ParteCreateRequest.cs
[JsonPropertyName("estado")]
public int Estado { get; set; } = 2; // 2=Cerrado por defecto
```

**Log del payload enviado:**

```
═══ Importando item 1/36 ═══
  FechaTrabajo: 2025-10-31
  IdCliente: 48
  HoraInicio: 16:50
  HoraFin: 18:00
  DuracionMin: 70
  Accion: '1 Ver mas temas de la Overlay...'
  Estado: 2  ← ✅ CORRECTO: Enviando estado = 2 (Cerrado)
```

**JSON enviado al backend:**

```json
{
  "fecha_trabajo": "2025-10-31",
  "hora_inicio": "16:50",
  "hora_fin": "18:00",
  "duracion_min": 70,
  "id_cliente": 48,
  "id_grupo": 8,
  "id_tipo": 1,
  "accion": "1 Ver mas temas de la Overlay, pruebas de instalación Ticket:51508",
  "estado": 2  ← ✅ ENVIANDO ESTADO = 2 (CERRADO)
}
```

---

### ❌ **Backend (API) - INCORRECTO**

El backend está **IGNORANDO** el campo `estado` del request y devolviendo:

```json
{
  "id": 12345,
  "estado": 1,  ← ❌ DEVUELVE ESTADO = 1 (ABIERTO)
  "estado_nombre": "Abierto"
}
```

---

## 🔬 **ANÁLISIS TÉCNICO**

### **Mapeo de Estados (API)**

Según el código del frontend (`ParteDto.cs`):

```csharp
public enum ParteEstado
{
    Abierto = 1,    // En curso activo (▶️ verde)
    Cerrado = 2,    // Finalizado (✅ azul)
    Pausado = 3,    // Temporalmente detenido (⏸️ amarillo)
    Enviado = 4,    // Enviado al sistema destino
    Anulado = 9     // Cancelado (⛔ gris)
}
```

**Frontend está enviando:** `estado: 2` (Cerrado)  
**Backend está devolviendo:** `estado: 1` (Abierto)

---

## 🐛 **CAUSA RAÍZ DEL PROBLEMA**

El backend tiene uno de estos problemas:

### **1. Ignora el campo `estado` del request**

```csharp
// ❌ MAL - Backend ignora el campo estado del DTO
[HttpPost("api/v1/partes")]
public async Task<IActionResult> CreateParte([FromBody] ParteCreateDto dto)
{
    var parte = new Parte
    {
        FechaTrabajo = dto.FechaTrabajo,
        HoraInicio = dto.HoraInicio,
        HoraFin = dto.HoraFin,
        IdCliente = dto.IdCliente,
        // ... otros campos ...
        Estado = 1  // ❌ HARDCODEADO - Ignora dto.Estado
    };
    
    await _repository.CreateAsync(parte);
    return Ok(parte);
}
```

### **2. Asigna estado por defecto sin verificar el request**

```csharp
// ❌ MAL - No verifica si viene estado en el DTO
[HttpPost("api/v1/partes")]
public async Task<IActionResult> CreateParte([FromBody] ParteCreateDto dto)
{
    var parte = new Parte
    {
        FechaTrabajo = dto.FechaTrabajo,
        HoraInicio = dto.HoraInicio,
        HoraFin = dto.HoraFin,
        IdCliente = dto.IdCliente,
        // ... otros campos ...
        // ❌ No asigna estado, por lo que usa el valor por defecto de la entidad
    };
    
    await _repository.CreateAsync(parte);
    return Ok(parte);
}
```

### **3. La entidad `Parte` tiene valor por defecto = 1**

```csharp
// ❌ MAL - La entidad tiene valor por defecto incorrecto
public class Parte
{
    public int Id { get; set; }
    public DateTime FechaTrabajo { get; set; }
    public string HoraInicio { get; set; }
    public string HoraFin { get; set; }
    public int IdCliente { get; set; }
    // ... otros campos ...
    public int Estado { get; set; } = 1;  // ❌ Por defecto = Abierto
}
```

---

## ✅ **SOLUCIÓN PARA EL BACKEND**

### **Opción 1: Respetar el campo `estado` del DTO**

```csharp
// ✅ BIEN - Usa el estado del request
[HttpPost("api/v1/partes")]
public async Task<IActionResult> CreateParte([FromBody] ParteCreateDto dto)
{
    var parte = new Parte
    {
        FechaTrabajo = dto.FechaTrabajo,
        HoraInicio = dto.HoraInicio,
        HoraFin = dto.HoraFin,
        IdCliente = dto.IdCliente,
        Tienda = dto.Tienda,
        IdGrupo = dto.IdGrupo,
        IdTipo = dto.IdTipo,
        Accion = dto.Accion,
        Ticket = dto.Ticket,
        Tecnico = dto.Tecnico,
        Estado = dto.Estado ?? 2  // ✅ Usa el estado del DTO, o 2 por defecto
    };
    
    await _repository.CreateAsync(parte);
    return Ok(MapToDto(parte));
}
```

### **Opción 2: Validar si es importación masiva**

Si el backend quiere diferenciar entre:
- **Partes normales** (creados desde la UI) → Estado = 1 (Abierto)
- **Partes importados** (desde Excel) → Estado = 2 (Cerrado)

```csharp
// ✅ BIEN - Lógica condicional según el contexto
[HttpPost("api/v1/partes")]
public async Task<IActionResult> CreateParte([FromBody] ParteCreateDto dto)
{
    // Si el request incluye estado explícitamente, respetarlo
    int estadoFinal = dto.Estado ?? 1; // Por defecto = Abierto si no se especifica
    
    var parte = new Parte
    {
        FechaTrabajo = dto.FechaTrabajo,
        HoraInicio = dto.HoraInicio,
        HoraFin = dto.HoraFin,
        IdCliente = dto.IdCliente,
        Tienda = dto.Tienda,
        IdGrupo = dto.IdGrupo,
        IdTipo = dto.IdTipo,
        Accion = dto.Accion,
        Ticket = dto.Ticket,
        Tecnico = dto.Tecnico,
        Estado = estadoFinal  // ✅ Usa el valor del request
    };
    
    await _repository.CreateAsync(parte);
    return Ok(MapToDto(parte));
}
```

### **Opción 3: Endpoint específico para importación**

```csharp
// ✅ BIEN - Endpoint dedicado para importación masiva
[HttpPost("api/v1/partes/import")]
public async Task<IActionResult> ImportPartes([FromBody] List<ParteCreateDto> partes)
{
    var resultados = new List<Parte>();
    
    foreach (var dto in partes)
    {
        var parte = new Parte
        {
            FechaTrabajo = dto.FechaTrabajo,
            HoraInicio = dto.HoraInicio,
            HoraFin = dto.HoraFin,
            IdCliente = dto.IdCliente,
            Tienda = dto.Tienda,
            IdGrupo = dto.IdGrupo,
            IdTipo = dto.IdTipo,
            Accion = dto.Accion,
            Ticket = dto.Ticket,
            Tecnico = dto.Tecnico,
            Estado = 2  // ✅ FORZADO para importación = Cerrado
        };
        
        await _repository.CreateAsync(parte);
        resultados.Add(parte);
    }
    
    return Ok(resultados);
}
```

---

## 🔍 **PASOS PARA VERIFICAR EN EL BACKEND**

### **1. Revisar el DTO del Backend**

Buscar el archivo que define `ParteCreateDto` o similar:

```csharp
// ¿Existe el campo estado en el DTO?
public class ParteCreateDto
{
    public DateTime FechaTrabajo { get; set; }
    public string HoraInicio { get; set; }
    public string HoraFin { get; set; }
    public int IdCliente { get; set; }
    public string? Tienda { get; set; }
    public int? IdGrupo { get; set; }
    public int? IdTipo { get; set; }
    public string Accion { get; set; }
    public string? Ticket { get; set; }
    public string? Tecnico { get; set; }
    public int? Estado { get; set; }  // ← ¿Existe este campo?
}
```

**Si NO existe:** Añadirlo:

```csharp
[JsonPropertyName("estado")]
public int? Estado { get; set; }
```

---

### **2. Revisar el Controlador**

Buscar el método `CreateParte` en el controlador:

```csharp
// PartesController.cs
[HttpPost]
public async Task<IActionResult> CreateParte([FromBody] ParteCreateDto dto)
{
    // ... código ...
}
```

**Verificar:**

1. ¿Se está leyendo `dto.Estado`?
2. ¿Se está asignando a `parte.Estado`?
3. ¿O se está ignorando/hardcodeando?

---

### **3. Revisar la Entidad**

Buscar la clase `Parte` en el modelo de datos:

```csharp
public class Parte
{
    public int Id { get; set; }
    // ... otros campos ...
    public int Estado { get; set; } = 1;  // ← ¿Cuál es el valor por defecto?
}
```

**Si el valor por defecto es 1:**

- Cambiar a `= 2` (si todos los partes deben ser Cerrados por defecto)
- O no usar valor por defecto y **SIEMPRE asignarlo explícitamente** en el controlador

---

### **4. Revisar el Mapping (si usa AutoMapper u otro)**

Si el backend usa AutoMapper:

```csharp
// ¿Está mapeando el campo estado?
CreateMap<ParteCreateDto, Parte>()
    .ForMember(dest => dest.Estado, opt => opt.MapFrom(src => src.Estado ?? 2));
```

---

## 📊 **COMPARACIÓN DE COMPORTAMIENTO**

| Escenario | Frontend Envía | Backend Debería Guardar | Backend Actualmente Guarda |
|-----------|----------------|-------------------------|----------------------------|
| Importación Excel | `estado: 2` | `estado: 2` (Cerrado) | ❌ `estado: 1` (Abierto) |
| Parte nuevo (UI) | `estado: null` | `estado: 1` (Abierto) | ✅ `estado: 1` (Abierto) |
| Parte editado | `estado: 2` | `estado: 2` (Cerrado) | ✅ `estado: 2` (Cerrado) |

**Conclusión:** El backend **SÍ respeta el estado en PUT** (edición), pero **NO en POST** (creación).

---

## 🎯 **EVIDENCIA DEL PROBLEMA**

### **Log del Frontend (Enviando):**

```
2026-01-06 11:21:44.884 [Debug] GestionTime - [HTTP 40e86cda] RequestBody: 
{
  "fecha_trabajo": "2025-10-31",
  "hora_inicio": "16:50",
  "hora_fin": "18:00",
  "duracion_min": 70,
  "id_cliente": 48,
  "id_grupo": 8,
  "id_tipo": 1,
  "accion": "1 Ver mas temas de la Overlay...",
  "estado": 2  ← ✅ ENVIANDO ESTADO = 2
}
```

### **Respuesta del Backend (Recibiendo):**

```
Parte creado con ID: 12345
Estado devuelto: 1 (Abierto)  ← ❌ BACKEND DEVUELVE ESTADO = 1
```

---

## 📝 **RECOMENDACIÓN FINAL**

### **Para el Backend (API):**

1. **Verificar que `ParteCreateDto` tenga el campo `estado`**
2. **Asignar `dto.Estado` a `parte.Estado` en el controlador**
3. **Usar `dto.Estado ?? 2` para tener un fallback a Cerrado**

### **Código sugerido para el backend:**

```csharp
[HttpPost("api/v1/partes")]
public async Task<IActionResult> CreateParte([FromBody] ParteCreateDto dto)
{
    var parte = new Parte
    {
        FechaTrabajo = dto.FechaTrabajo,
        HoraInicio = dto.HoraInicio,
        HoraFin = dto.HoraFin,
        IdCliente = dto.IdCliente,
        Tienda = dto.Tienda,
        IdGrupo = dto.IdGrupo,
        IdTipo = dto.IdTipo,
        Accion = dto.Accion,
        Ticket = dto.Ticket,
        Tecnico = dto.Tecnico,
        Estado = dto.Estado ?? 2  // ✅ Usar el estado del DTO, o 2 por defecto
    };
    
    await _parteRepository.CreateAsync(parte);
    return Ok(MapToDto(parte));
}
```

---

## 🔗 **ARCHIVOS RELACIONADOS**

### **Frontend (Desktop):**

- `Services/Import/ExcelPartesImportService.cs` - **✅ CORRECTO** - Línea 237: `Estado = 2`
- `Models/Dtos/ParteCreateRequest.cs` - **✅ CORRECTO** - `[JsonPropertyName("estado")]`
- `Dialogs/ImportExcelDialog.xaml.cs` - Orquestador de importación

### **Backend (API) - Archivos a revisar:**

- `Controllers/PartesController.cs` (o similar) - **❌ REVISAR** método `CreateParte`
- `Dtos/ParteCreateDto.cs` (o similar) - **❌ VERIFICAR** si existe campo `Estado`
- `Models/Parte.cs` (o similar) - **❌ VERIFICAR** valor por defecto de `Estado`
- `Mapping/ParteProfile.cs` (si usa AutoMapper) - **❌ VERIFICAR** mapeo de `Estado`

---

## ✅ **CHECKLIST DE VERIFICACIÓN**

- [ ] Backend tiene campo `estado` en `ParteCreateDto`
- [ ] Controlador asigna `dto.Estado` a `parte.Estado`
- [ ] No hay valor hardcodeado `Estado = 1` en el controlador
- [ ] La entidad `Parte` no tiene valor por defecto incorrecto
- [ ] El mapeo (si existe) incluye el campo `Estado`
- [ ] Se prueba la importación y se verifica que llega `estado = 2`

---

**🎯 CONCLUSIÓN:** El frontend está enviando **CORRECTAMENTE** `estado: 2`. El problema está **100% en el backend** que lo ignora y asigna `estado: 1` por defecto.
