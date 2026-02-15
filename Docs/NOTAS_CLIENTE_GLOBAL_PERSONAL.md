# Sistema de Notas de Cliente (Global + Personal)

**Fecha**: 2025-02-02  
**Estado**: ✅ Implementado y compilado (backend + desktop)

---

## 📋 Resumen

Sistema de notas asociadas al cliente con dos niveles:
- **Nota global**: Una por cliente, visible para todos, editable solo por EDITOR/ADMIN
- **Nota personal**: Una por cliente y usuario, editable por el propio usuario

---

## 🔐 Matriz de Permisos

| Acción | USER | EDITOR | ADMIN |
|--------|------|--------|-------|
| Ver nota global | ✅ (readonly) | ✅ | ✅ |
| Editar nota global | ❌ | ✅ | ✅ |
| Ver su nota personal | ✅ | ✅ | ✅ |
| Editar su nota personal | ✅ | ✅ | ✅ |
| Ver notas personales de otros | ❌ | ❌ | ❌ |

---

## 🧠 Backend — Archivos Modificados/Creados

### Nuevos

1. **`GestionTime.Domain/Work/ClienteNota.cs`**
   - Entidad: `id`, `cliente_id`, `owner_user_id` (null=global), `nota`, timestamps, audit

2. **`Contracts/Catalog/ClienteNotasDto.cs`**
   - `ClienteNotasResponseDto` (GET response: global + personal)
   - `ClienteNotaItemDto` (text, updatedAt, updatedByName)
   - `ClienteNotaUpdateDto` (PUT request: text)

3. **`Controllers/V2/ClienteNotasController.cs`**
   - `GET /api/v2/clientes/{id}/notas` — Devuelve nota global + personal del usuario
   - `PUT /api/v2/clientes/{id}/notas/global` — Upsert global (solo EDITOR/ADMIN, 403 si USER)
   - `PUT /api/v2/clientes/{id}/notas/personal` — Upsert personal del usuario autenticado

4. **`scripts/Migration-ClienteNotas.sql`**
   - Script SQL idempotente para crear tabla `pss_dvnx.cliente_notas`
   - Unique indexes: `uq_cliente_notas_global` + `uq_cliente_notas_personal`

### Modificados

5. **`GestionTime.Infrastructure/Persistence/GestionTimeDbContext.cs`**
   - Añadido `DbSet<ClienteNota> ClienteNotas`
   - Añadida configuración EF Core: tabla `cliente_notas`, columnas, índices únicos

---

## 🖥️ Desktop — Archivos Modificados/Creados

### Nuevos

1. **`Models/Dtos/Catalog/ClienteNotasDto.cs`**
   - `ClienteNotasResponse`, `ClienteNotaItem`, `ClienteNotaUpdateRequest`

### Modificados

2. **`Services/Catalog/ClientesService.cs`**
   - `GetNotasAsync(clienteId)` → GET /api/v2/clientes/{id}/notas
   - `SaveNotaGlobalAsync(clienteId, text)` → PUT /api/v2/.../global
   - `SaveNotaPersonalAsync(clienteId, text)` → PUT /api/v2/.../personal

3. **`Views/ParteItemEdit.xaml`**
   - ContentDialog reemplazado: 2 secciones (global + personal)
   - Nota global: TextBox readonly para USER, editable para EDITOR/ADMIN + botón Guardar
   - Nota personal: TextBox editable para todos + botón Guardar
   - Indicador de carga, metadatos de última edición, estados inline

4. **`Views/ParteItemEdit.xaml.cs`**
   - `OnClienteNotaClick`: Determina rol, configura UI, carga notas v2
   - `LoadNotasV2Async`: GET desde endpoint v2 con fallback a nota legacy
   - `OnSaveNotaGlobal_Click`: PUT global (con fallback legacy si v2 no disponible)
   - `OnSaveNotaPersonal_Click`: PUT personal
   - `UpdateNotaTooltip`: Preview combinada (global + personal)
   - Campos nuevos: `_clienteNotasV2`, `_canEditGlobalNota`

---

## 🔄 Compatibilidad y Fallback

- Si el backend NO tiene el endpoint `/api/v2/clientes/{id}/notas`:
  - Se detecta 404 y se usa la nota legacy (campo `Nota` de `ClienteDto`)
  - El usuario puede seguir editando con el endpoint `PATCH /api/v1/clientes/{id}/nota`
- Endpoints v1 existentes NO se modifican

---

## 📦 Checklist de Despliegue

### Backend (producción)

1. [ ] Ejecutar `scripts/Migration-ClienteNotas.sql` en PostgreSQL
2. [ ] Desplegar nueva versión del API con el controller V2
3. [ ] Verificar que `GET /api/v2/clientes/{id}/notas` responde correctamente
4. [ ] Probar con USER → nota global readonly, nota personal editable
5. [ ] Probar con EDITOR/ADMIN → ambas editables
6. [ ] Verificar que endpoints v1 siguen funcionando

### Desktop

1. [ ] Compilar y desplegar nueva versión
2. [ ] Verificar que el diálogo muestra 2 secciones
3. [ ] Probar con USER → solo puede guardar nota personal
4. [ ] Probar con EDITOR → puede guardar ambas notas
5. [ ] Verificar fallback si backend no actualizado (nota legacy funciona)

---

## 🔒 Seguridad

- El backend valida rol desde JWT (`ClaimTypes.Role`)
- `PUT /global` devuelve **403 Forbidden** si el rol no es EDITOR/ADMIN
- `PUT /personal` siempre usa `userId` del JWT (ignora cualquier body)
- El desktop no permite UI de guardar global si el usuario es USER
- Notas personales de otros usuarios NUNCA se exponen
