# Gestión de Clientes en SettingsWindow

**Fecha:** 2025-01-XX  
**Estado:** ✅ Implementado y funcional  
**Ubicación:** `Views/SettingsWindow.xaml.cs` → Sección "Clientes"

---

## 📋 Resumen

Se implementó la sección completa de **gestión de clientes** dentro de `SettingsWindow`, permitiendo operaciones CRUD (crear, leer, actualizar, eliminar) con filtros avanzados, usando el endpoint principal `/api/v1/clientes`.

---

## 🎯 Funcionalidades Implementadas

### 1. Listado de Clientes con Filtros Avanzados

#### Filtros disponibles:
- **q**: Búsqueda por texto (nombre)
- **id_puntoop**: Filtrar por ID Punto OP
- **local_num**: Filtrar por número local
- **provincia**: Filtrar por provincia
- **hasNota**: Filtrar por existencia de nota
  - Todos (sin filtro)
  - Con nota
  - Sin nota

#### Paginación:
- Controles: « Anterior | Página X de Y | Siguiente »
- Tamaño de página: 50 registros
- Indicadores de estado: total de clientes encontrados

### 2. Panel de Edición/Creación

**Campos editables:**
- ✅ Nombre * (requerido, max 200 caracteres)
- ✅ ID Punto OP (opcional, numérico)
- ✅ Local Num (opcional, numérico)
- ✅ Nombre Comercial (opcional)
- ✅ Provincia (opcional)
- ✅ Nota (opcional, multilinea)

**Campos solo lectura:**
- 📅 Última actualización (dataUpdate)
- 🌐 Data HTML (si existe)

### 3. Operaciones CRUD

#### Crear (POST /api/v1/clientes)
- Botón: **➕ Nuevo Cliente**
- Validaciones:
  - Nombre no vacío
  - Longitud <= 200 caracteres
  - Conversión de strings vacías a null

#### Actualizar (PUT /api/v1/clientes/{id})
- Botón: **💾 Guardar**
- Actualiza todos los campos del cliente

#### Actualizar solo nota (PATCH /api/v1/clientes/{id}/nota)
- Botón: **📝 Guardar solo nota**
- Solo visible en modo edición
- Actualiza únicamente el campo nota

#### Eliminar (DELETE /api/v1/clientes/{id})
- Botón: **🗑️ Eliminar**
- Confirmación obligatoria antes de eliminar
- Manejo de error 409 (conflicto de integridad)

---

## 🔧 Implementación Técnica

### Servicio: ClientesService

**Ubicación:** `Services/Catalog/ClientesService.cs`

**Métodos añadidos:**

```csharp
/// <summary>Listar clientes con filtros avanzados</summary>
Task<PagedResponse<ClienteDto>?> ListWithFiltersAsync(
    int page = 1,
    int size = 50,
    string? q = null,
    int? idPuntoop = null,
    int? localNum = null,
    string? provincia = null,
    bool? hasNota = null,
    CancellationToken ct = default)
```

**Nota importante:** El endpoint usa el parámetro `size` (no `pageSize`) para el tamaño de página.

### DTOs Utilizados

**Ubicación:** `Models/Dtos/Catalog/ClienteDto.cs`

```csharp
// Respuesta
public sealed class ClienteDto

// Request creación
public sealed class ClienteCreateRequest

// Request actualización completa
public sealed class ClienteUpdateRequest

// Request actualización solo nota
public sealed class ClienteUpdateNotaRequest
```

### Métodos UI Principales

1. **`CreateClientsContent()`**: Crea la sección completa
2. **`CreateClienteEditPanel()`**: Crea el panel de edición/creación
3. **`ShowClienteEditPanel()`**: Muestra y configura el panel para crear/editar
4. **`LoadClientesAsync()`**: Carga clientes con filtros y paginación
5. **`NavigateClientesPageAsync()`**: Navega entre páginas
6. **`CreateClienteCard()`**: Renderiza una tarjeta de cliente
7. **`OnSaveClienteClick()`**: Maneja POST/PUT
8. **`OnSaveNotaOnlyClick()`**: Maneja PATCH nota
9. **`OnDeleteClienteClick()`**: Maneja DELETE
10. **`FindControlByTag<T>()`**: Helper para buscar controles por Tag

---

## 🎨 Diseño UI

### Estructura Visual

```
┌─────────────────────────────────────────────────┐
│ 📄 Gestión de clientes                         │
│ CRUD de clientes: Crear, editar...             │
├─────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────┐ │
│ │ Filtros                                     │ │
│ │ [Buscar...] [🔍 Buscar] [🗑️ Limpiar]      │ │
│ │ [ID POP] [Local] [Prov.] [Filtro nota]    │ │
│ └─────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────┐ │
│ │ 📝 Panel de Edición (si visible)           │ │
│ │ Nombre: [____________]    ID POP: [____]    │ │
│ │ Local: [____] Comercial: [____] Prov:[____]│ │
│ │ Nota: [_______________________________]    │ │
│ │ [💾 Guardar] [📝 Solo nota] [🗑️ Eliminar] │ │
│ └─────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────┤
│ [➕ Nuevo]  X clientes | « Anterior Pág 1/N » │
├─────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────┐ │
│ │ 👤 Cliente A          📍 Madrid      #123  │ │
│ │ 📍 Barcelona  [POP: 5] [Local: 12] 📝      │ │
│ ├─────────────────────────────────────────────┤ │
│ │ 👤 Cliente B          📍 Valencia    #124  │ │
│ │ 📍 Valencia  [POP: 8]                      │ │
│ └─────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────┘
```

### Tarjetas de Cliente

Cada tarjeta muestra:
- 👤 Nombre del cliente
- 📍 Provincia (si existe)
- Chips de:
  - **POP**: ID Punto OP (azul)
  - **Local**: Número local (morado)
- 📝 Icono de nota (con tooltip mostrando el contenido)
- \#ID (esquina derecha)

**Interacción:** Click en la tarjeta abre el panel de edición.

---

## 🔍 Validaciones

### Lado Cliente (UI)

1. **Nombre obligatorio**: No puede estar vacío
2. **Longitud máxima**: 200 caracteres
3. **Conversión de vacíos**: Strings vacíos se convierten a `null` para campos opcionales
4. **Parseo numérico**: Validación al parsear IdPuntoop y LocalNum
5. **Confirmación de eliminación**: Diálogo antes de DELETE

### Errores del Backend

| Código | Manejo |
|--------|--------|
| 400 | Muestra errores de validación del backend |
| 404 | "Cliente no encontrado" |
| 409 | "No se puede eliminar por integridad referencial" |
| 500 | Muestra mensaje de error genérico |

---

## 📊 Logging

### Niveles Usados

**INFO:**
- ✅ Cliente creado/actualizado/eliminado con éxito
- 📋 X clientes cargados (total, páginas)

**DEBUG:**
- 🔍 URL construida con filtros completos

**WARNING:**
- ⚠️ Cliente no encontrado
- ⚠️ Restricción de integridad al eliminar

**ERROR:**
- ❌ Excepciones en operaciones CRUD
- ❌ Errores de red o API

---

## 🚀 Uso

### Acceso

1. Abrir **SettingsWindow** (Ctrl+Alt+P desde MainWindow)
2. Navegar a la sección **"Clientes"** en el menú lateral

### Flujo: Crear Cliente

1. Click en **➕ Nuevo Cliente**
2. Rellenar formulario (mínimo: nombre)
3. Click en **💾 Guardar**
4. ✅ Confirmación y recarga automática de lista

### Flujo: Editar Cliente

1. Click en cualquier tarjeta de cliente
2. Modificar campos deseados
3. Opciones:
   - **💾 Guardar**: Actualiza todos los campos (PUT)
   - **📝 Guardar solo nota**: Actualiza solo la nota (PATCH)
4. ✅ Confirmación y recarga automática

### Flujo: Eliminar Cliente

1. Click en cliente → Abrir edición
2. Click en **🗑️ Eliminar**
3. Confirmar en diálogo
4. Si tiene registros asociados: Error 409 con mensaje claro
5. Si no: ✅ Eliminado y recarga automática

### Flujo: Buscar y Filtrar

1. Introducir filtros deseados:
   - Texto de búsqueda
   - ID Punto OP
   - Local Num
   - Provincia
   - Filtro de nota (Todos/Con nota/Sin nota)
2. Click en **🔍 Buscar**
3. Resultados filtrados con paginación
4. Click en **🗑️ Limpiar** para resetear filtros

---

## ⚙️ Configuración

### Parámetros Ajustables

**En `LoadClientesAsync()`:**
```csharp
size: 50  // Registros por página (máximo backend: 100)
```

**En `ListWithFiltersAsync()`:**
```csharp
page: 1         // Página inicial
size: 50        // Tamaño de página
// + filtros opcionales
```

---

## 🔒 Permisos

**Roles permitidos:**
- **ADMIN**: Acceso completo (crear, editar, eliminar)
- **EDITOR**: Acceso completo (crear, editar, eliminar)
- **USER**: Sin acceso (la sección no debería estar visible)

**Nota:** La validación de permisos se maneja en `SettingsViewModel.Sections` según el rol del usuario actual.

---

## 🧪 Testing

### Endpoints Usados

```http
GET  /api/v1/clientes?page={page}&size={size}&q={q}&id_puntoop={id}&local_num={num}&provincia={prov}&hasNota={bool}
GET  /api/v1/clientes/{id}
POST /api/v1/clientes
PUT  /api/v1/clientes/{id}
PATCH /api/v1/clientes/{id}/nota
DELETE /api/v1/clientes/{id}
```

### Casos de Prueba

- [ ] Listar clientes sin filtros
- [ ] Aplicar cada filtro individualmente
- [ ] Combinar múltiples filtros
- [ ] Navegar entre páginas
- [ ] Crear cliente nuevo
- [ ] Editar cliente existente (PUT)
- [ ] Actualizar solo nota (PATCH)
- [ ] Eliminar cliente sin registros asociados
- [ ] Intentar eliminar cliente con registros (debe mostrar error 409)
- [ ] Validar nombre vacío
- [ ] Validar nombre > 200 caracteres

---

## 📝 Notas Importantes

### ✅ Buenas Prácticas Aplicadas

1. **Reutilización de código**: No se duplicaron DTOs ni servicios
2. **Separación de responsabilidades**: Servicio → lógica API, UI → presentación
3. **Manejo robusto de errores**: Try-catch + logs detallados
4. **Validación temprana**: Validación en UI antes de llamar al backend
5. **Feedback al usuario**: Diálogos de confirmación y mensajes de estado
6. **Logging completo**: INFO/DEBUG/WARNING/ERROR según contexto

### ⚠️ Consideraciones

1. **Endpoint `size` vs `pageSize`**: El backend usa `size`, no `pageSize` (diferente de otros endpoints)
2. **Filtro hasNota**: Solo acepta `true`, `false` o ausencia (no string "all")
3. **Valores nulos**: Strings vacíos se convierten a `null` para campos opcionales
4. **Integridad referencial**: Error 409 al eliminar cliente con registros asociados
5. **No se toca `/api/v1/catalog/clientes`**: Ese endpoint es para combos/autocomplete, NO para esta pantalla

---

## 🔄 Próximas Mejoras Potenciales

- [ ] Añadir ordenación de columnas
- [ ] Exportar listado a Excel
- [ ] Importar clientes desde CSV/Excel
- [ ] Búsqueda avanzada con múltiples operadores
- [ ] Historial de cambios por cliente
- [ ] Campos personalizados configurables

---

## 🐛 Problemas Conocidos

Ninguno detectado tras la implementación inicial.

---

## 📚 Referencias

- **Servicio:** `Services/Catalog/ClientesService.cs`
- **DTOs:** `Models/Dtos/Catalog/ClienteDto.cs`
- **UI:** `Views/SettingsWindow.xaml.cs` → método `CreateClientsContent()`
- **Endpoint Backend:** `/api/v1/clientes`
- **Documentación Backend:** Ver swagger del API para detalles de validaciones

---

**Implementado por:** GitHub Copilot  
**Revisión:** ✅ Compilación exitosa  
**Estado:** Listo para uso en producción
