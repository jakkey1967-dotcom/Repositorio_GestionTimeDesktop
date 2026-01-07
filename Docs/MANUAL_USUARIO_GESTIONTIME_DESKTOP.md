# 📘 Manual de Usuario - GestionTime Desktop

**Versión:** 1.2.0  
**Fecha:** Enero 2026  
**Sistema:** Gestión de Partes de Trabajo para Empresas de Soporte Técnico

---

## 📋 Índice

1. [Introducción](#introducción)
2. [Inicio de Sesión](#inicio-de-sesión)
3. [Página Principal (Diario)](#página-principal-diario)
4. [Gestión de Partes](#gestión-de-partes)
5. [Importación de Excel](#importación-de-excel)
6. [Perfil de Usuario](#perfil-de-usuario)
7. [Configuración del Sistema](#configuración-del-sistema)
8. [Atajos de Teclado](#atajos-de-teclado)
9. [Solución de Problemas](#solución-de-problemas)

---

## 🎯 Introducción

### ¿Qué es GestionTime Desktop?

**GestionTime Desktop** es una aplicación de escritorio para **Windows 11** diseñada para gestionar **partes de trabajo** de técnicos en empresas de soporte técnico y mantenimiento.

### Características Principales

✅ **Gestión de Partes de Trabajo**
- Crear, editar y eliminar partes
- Control de tiempo (hora inicio/fin)
- Estados: Abierto, Pausado, Cerrado, Enviado, Anulado
- Asociación a clientes, tiendas y tickets

✅ **Importación Masiva**
- Importar partes desde archivos Excel (.xls/.xlsx)
- Validación automática de datos
- Preview antes de importar

✅ **Filtros y Búsquedas**
- Filtrar por fecha
- Búsqueda por cliente, ticket, acción, etc.
- Carga inteligente (últimos 25 partes)

✅ **Perfil Dinámico**
- Banner con información del usuario
- Nombre completo, email y teléfono
- Edición de perfil personal

✅ **Temas Personalizables**
- Tema claro, oscuro o automático (según sistema)
- Interfaz moderna y responsive

✅ **Sistema de Notificaciones**
- Alertas visuales de operaciones
- Mensajes de éxito, error, advertencia e información

---

## 🔐 Inicio de Sesión

### Pantalla de Login

![Login Screen](../Assets/screenshots/login.png)

#### Campos Requeridos

| Campo | Descripción | Ejemplo |
|-------|-------------|---------|
| **📧 Email** | Correo electrónico corporativo | `psantos@global-retail.com` |
| **🔒 Contraseña** | Contraseña de acceso | `••••••••` |
| **☑️ Recordar sesión** | Guarda el email para próximos inicios | Opcional |

#### Funcionalidades

- **👁️ Mostrar/Ocultar Contraseña**: Click en el icono del ojo
- **🔄 Recuperar Contraseña**: Link "¿Olvidaste tu contraseña?"
- **📝 Registrar Cuenta**: Link "Crear cuenta nueva"
- **🎨 Cambiar Tema**: Menú superior derecho (☰)

#### Opciones de Tema

- **Automático (según sistema)**: Se adapta al tema de Windows
- **Claro**: Colores claros para mejor visibilidad diurna
- **Oscuro**: Colores oscuros para reducir fatiga visual

#### Modo Desarrollo

Para desarrolladores, existe un acceso directo usando el usuario `dev` (sin contraseña).

---

## 🏠 Página Principal (Diario)

### Banner Superior

```
┌─────────────────────────────────────────────────────────────┐
│ 🏢 Logo  │  📅 Diario de Partes de Trabajo             │ 👤 │
│          │                                              │    │
│          │  👤 Francisco Santos García     [Mi Perfil] │    │
│          │     psantos@global-retail.com               │ ☰  │
│          │     965268092                                │    │
└─────────────────────────────────────────────────────────────┘
```

#### Elementos del Banner

1. **🏢 Logo de la Empresa**: Cambia según el tema (claro/oscuro)

2. **📅 Título de la Página**: "Diario de Partes de Trabajo"

3. **👤 Información del Usuario**:
   - **Nombre completo**: Cargado desde el perfil del backend
   - **Email**: Correo usado en el login
   - **Teléfono**: Número de contacto (si está disponible)
   - **Botón [Mi Perfil]**: Acceso directo a la página de perfil

4. **🌐 Estado del Servicio**: LED que indica conexión con el servidor
   - 🟢 **Verde**: Servicio en línea
   - 🔴 **Rojo**: Sin conexión

5. **☰ Menú de Tema**: Cambiar entre temas claro/oscuro/automático

### Filtros

```
┌─────────────────────────────────────────────────────────────┐
│ 📅 Fecha: [02/01/2026] 🔄                                   │
│ 🔍 Buscar: [cliente, ticket, acción...]                     │
└─────────────────────────────────────────────────────────────┘
```

#### Filtro por Fecha

- **Selector de Fecha**: Click en el campo para abrir calendario
- **Formato**: DD/MM/YYYY
- **Comportamiento**:
  - **HOY**: Carga los últimos 25 partes (más recientes primero)
  - **Fecha Específica**: Carga solo los partes de ese día
- **Botón 🔄 Refrescar** (F5): Restaura la fecha a HOY y recarga datos

#### Búsqueda

Campo de texto con búsqueda instantánea (debounce 350ms) que filtra por:
- Cliente
- Tienda
- Acción/Descripción
- Ticket
- Grupo
- Tipo
- Técnico
- Estado

### Barra de Botones

```
┌─────────────────────────────────────────────────────────────┐
│ 📞 Teléfono │ 📝 Nuevo │ ✏️ Editar │ │ 🗑️ Borrar │ 📊 Importar │ 🚪 Salir │
└─────────────────────────────────────────────────────────────┘
```

#### Botones Disponibles

| Botón | Atajo | Descripción |
|-------|-------|-------------|
| **📞 Teléfono** | `Ctrl+T` | Crear llamada telefónica rápida (ticket automático "TELEFONO") |
| **📝 Nuevo** | `Ctrl+N` | Crear nuevo parte de trabajo |
| **✏️ Editar** | `Ctrl+E` | Editar parte seleccionado (requiere selección) |
| **🗑️ Borrar** | `Delete` | Eliminar parte seleccionado (confirmación requerida) |
| **📊 Importar** | `Ctrl+I` | Importar partes desde Excel |
| **🚪 Salir** | `Ctrl+Q` | Cerrar sesión y volver al login |

### Lista de Partes

```
┌────────────────────────────────────────────────────────────────────────────┐
│ Fecha      │ Cliente    │ Tienda │ Acción           │ Inicio│ Fin  │ Estado │
├────────────────────────────────────────────────────────────────────────────┤
│ 02/01/2026 │ MERCADONA  │ 001    │ Mantenimiento... │ 09:00 │10:30 │ Cerrado│
│ 02/01/2026 │ CARREFOUR  │ 015    │ Instalación...   │ 11:00 │      │ Abierto│
└────────────────────────────────────────────────────────────────────────────┘
```

#### Columnas de la Tabla

| Columna | Descripción | Tooltip |
|---------|-------------|---------|
| **Fecha** | Fecha del trabajo (DD/MM/YYYY) | - |
| **Cliente** | Nombre del cliente | Tooltip con nombre completo |
| **Tienda** | Código o nombre de tienda | - |
| **Acción** | Descripción del trabajo (hasta 2 líneas) | Tooltip con texto completo |
| **Inicio** | Hora de inicio (HH:mm) | - |
| **Fin** | Hora de finalización (HH:mm) | - |
| **Dur.** | Duración calculada | **Tooltip dinámico** con estadísticas |
| **Ticket** | Número de ticket | - |
| **Grupo** | Grupo de trabajo | - |
| **Tipo** | Tipo de servicio | - |
| **Estado** | Estado actual con icono y color | Click para menú de acciones |

#### Tooltip de Duración

Al pasar el ratón sobre el header **"Dur."**, se muestra:

```
⏱️ COBERTURA DE TIEMPO
━━━━━━━━━━━━━━━━━━━
📊 Estadísticas:
• Partes: 15
• Intervalos: 12 (3 fusionados)
• Cubierto: 6h 45m (sin solapamiento)
• Solapado: 15m (eliminado del total)

📈 Intervalos:
1. 08:00 - 09:30 (1h 30m)
2. 09:45 - 11:00 (1h 15m)
3. 11:15 - 14:00 (2h 45m)
```

#### Estados de Partes

| Estado | Color | Icono | Descripción |
|--------|-------|-------|-------------|
| **🟢 Abierto** | Verde `#10B981` | ✓ | Parte en progreso, puede pausarse o cerrarse |
| **🟡 Pausado** | Amarillo `#F59E0B` | ⏸ | Parte pausado temporalmente, puede reanudarse |
| **🔵 Cerrado** | Azul `#3B82F6` | ✓ | Parte completado, puede duplicarse |
| **🟣 Enviado** | Púrpura `#8B5CF6` | ✉ | Parte enviado al sistema de facturación |
| **🔴 Anulado** | Rojo `#EF4444` | ✕ | Parte cancelado, solo visible para referencia |

#### Acciones por Estado

Click derecho en el **badge de estado** para ver acciones disponibles:

**🟢 Abierto:**
- ⏸️ **Pausar**: Pausar trabajo temporalmente
- ✅ **Cerrar**: Finalizar y cerrar parte

**🟡 Pausado:**
- ▶️ **Reanudar**: Volver a estado Abierto

**🔵 Cerrado:**
- 📋 **Duplicar**: Crear nuevo parte con mismos datos

#### Características Visuales

- **Zebra Rows**: Filas alternadas con fondo turquesa (40% opacity) para mejor legibilidad
- **Hover**: Fila resaltada al pasar el ratón
- **Selección**: Fondo azul claro al seleccionar
- **Multiline en Acción**: Hasta 2 líneas con ellipsis si es muy largo

---

## 📝 Gestión de Partes

### Crear Nuevo Parte

**Atajo:** `Ctrl+N` o botón **📝 Nuevo**

#### Ventana de Edición

```
┌────────────────────────────────────────────────────────────┐
│ 🏢 Logo  │  Nuevo Parte                              │ 🟢   │
│          │  👤 Francisco Santos │ psantos@...        │ Abi  │
│          │     965268092                              │ erto │
├────────────────────────────────────────────────────────────┤
│                                                             │
│ ╔═══════════════════════════════════════════════════════╗  │
│ ║ DATOS GENERALES                                       ║  │
│ ╠═══════════════════════════════════════════════════════╣  │
│ ║ Fecha: [02/01/2026]   Cliente: [MERCADONA]           ║  │
│ ║ Tienda: [Valencia 001]                                ║  │
│ ╚═══════════════════════════════════════════════════════╝  │
│                                                             │
│ ╔═══════════════════════════════════════════════════════╗  │
│ ║ INFORMACIÓN DE TIEMPO                                 ║  │
│ ╠═══════════════════════════════════════════════════════╣  │
│ ║ Inicio: [09:00] Fin: [10:30] Ticket: [TK-2026-0001]  ║  │
│ ║ Grupo: [Mantenimiento] Tipo: [Correctivo]            ║  │
│ ╚═══════════════════════════════════════════════════════╝  │
│                                                             │
│ ╔═══════════════════════════════════════════════════════╗  │
│ ║ DESCRIPCIÓN / ACCIÓN                                  ║  │
│ ╠═══════════════════════════════════════════════════════╣  │
│ ║ [09:00 Revisión de equipos TPV                       │║  │
│ ║  09:30 Sustitución de cable de red                   │║  │
│ ║  10:15 Pruebas finales y entrega]                    │║  │
│ ╚═══════════════════════════════════════════════════════╝  │
│                                                             │
├────────────────────────────────────────────────────────────┤
│                   [ 💾 Guardar ] [ 💾 Guardar y Cerrar ]   │
│                   [ ❌ Anular ] [ 🚪 Salir ]               │
└────────────────────────────────────────────────────────────┘
```

#### Sección: Datos Generales

| Campo | Tipo | Obligatorio | Descripción |
|-------|------|-------------|-------------|
| **Fecha** | Date Picker | ✅ Sí | Fecha del trabajo (por defecto: HOY) |
| **Cliente** | AutoComplete | ✅ Sí | Busca clientes existentes o crea uno nuevo |
| **Tienda** | Text | ❌ No | Nombre o código de tienda |

**Cliente AutoComplete:**
- Búsqueda dinámica con debounce (350ms)
- Muestra sugerencias al escribir
- Acepta texto libre si no encuentra coincidencias
- `Enter` para auto-completar con primera sugerencia

#### Sección: Información de Tiempo

| Campo | Formato | Obligatorio | Descripción |
|-------|---------|-------------|-------------|
| **Hora Inicio** | HH:mm | ✅ Sí | Hora de inicio del trabajo |
| **Hora Fin** | HH:mm | ❌ No | Hora de finalización (vacío = parte abierto) |
| **Ticket** | Text | ❌ No | Número de ticket o referencia |
| **Grupo** | ComboBox | ❌ No | Grupo de trabajo (ej: Mantenimiento, Instalación) |
| **Tipo** | ComboBox | ❌ No | Tipo de servicio (ej: Correctivo, Preventivo) |

**Formato de Hora:**
- Entrada automática: `0900` → `09:00`
- Click en el campo borra el contenido previo
- Validación automática (00:00 - 23:59)
- `Enter` para avanzar al siguiente campo

**ComboBox Inteligente:**
- `F4` o `Alt+↓` para abrir lista
- Autocompletado al escribir
- `Enter` para confirmar y avanzar
- Acepta texto libre si no encuentra coincidencia

#### Sección: Descripción / Acción

Campo de texto multilínea con **timestamps automáticos**:

**Funcionalidades:**
- **Auto-timestamp**: Al recibir foco inserta `HH:mm ` (hora actual)
- **Nueva línea**: `Enter` inserta salto de línea + timestamp nuevo
- **Guardar desde campo**: `Ctrl+Enter` guarda el parte directamente

**Ejemplo de uso:**
```
09:00 Llegada a tienda Valencia 001
09:15 Revisión de equipos TPV - 3 terminales operativos
09:45 Detección de fallo en impresora de tickets
10:00 Sustitución de cable USB defectuoso
10:20 Pruebas de impresión - OK
10:30 Entrega conforme a responsable de tienda
```

#### Navegación entre Campos

**Orden de navegación con `Enter` o `Tab`:**

1. Fecha → 2. Cliente → 3. Tienda → 4. Hora Inicio → 5. Hora Fin  
→ 6. Ticket → 7. Grupo → 8. Tipo → 9. Descripción → 10. Guardar

**Atajos Especiales:**
- `Ctrl+Enter` en Descripción: Guardar parte
- `Esc`: Cancelar edición (si hay cambios, pide confirmación)
- `F4` en ComboBox: Abrir lista desplegable

#### Botones de Acción

| Botón | Atajo | Descripción |
|-------|-------|-------------|
| **💾 Guardar** | `Ctrl+S` | Guarda cambios y mantiene la ventana abierta |
| **💾 Guardar y Cerrar** | - | Guarda cambios, cierra parte (estado=Cerrado) y cierra ventana |
| **❌ Anular** | `Esc` | Cancela cambios (pide confirmación si hay modificaciones) |
| **🚪 Salir** | - | Cierra ventana sin guardar (pide confirmación si hay cambios) |

#### Lógica de Estados al Guardar

**Parte NUEVO:**
- **Guardar**: Estado = **Abierto** (0)
- **Guardar y Cerrar**: Estado = **Cerrado** (2)

**Parte EXISTENTE (Edición):**
- **Guardar**:
  - Si estaba Cerrado → Mantiene **Cerrado**
  - Si NO estaba Cerrado → Cambia a **Abierto**
- **Guardar y Cerrar**: Estado = **Cerrado** (2) (siempre)

### Editar Parte Existente

**Atajo:** `Ctrl+E` (requiere selección)

La ventana de edición es idéntica a la de creación, pero:
- Título: "Editar Parte" (en lugar de "Nuevo Parte")
- Badge de estado muestra el estado actual (Abierto, Pausado, Cerrado, etc.)
- Campos pre-rellenados con datos existentes
- Cliente, Grupo y Tipo se seleccionan automáticamente si existen en catálogo

### Eliminar Parte

**Atajo:** `Delete` (requiere selección)

1. Click en botón **🗑️ Borrar**
2. Aparece diálogo de confirmación con datos del parte:
   ```
   ⚠️ Confirmar eliminación DEFINITIVA
   
   ¿Estás seguro de que deseas ELIMINAR DEFINITIVAMENTE el parte ID 1234?
   
   Cliente: MERCADONA
   Fecha: 02/01/2026
   Acción: Mantenimiento de TPV...
   
   ⚠️ ATENCIÓN: Esta acción NO se puede deshacer.
   El registro se borrará permanentemente de la base de datos.
   
   [ Eliminar definitivamente ]  [ Cancelar ]
   ```
3. Confirmación elimina el parte del servidor y actualiza la lista local

### Llamada Telefónica Rápida

**Atajo:** `Ctrl+T`

Crea un parte pre-configurado para llamadas telefónicas:

- **Fecha**: HOY
- **Hora Inicio**: Hora actual
- **Ticket**: "TELEFONO" (automático)
- **Acción**: "Llamada telefónica" + timestamp
- **Estado**: Abierto

Útil para registrar rápidamente llamadas de soporte sin rellenar todos los campos.

### Cambiar Estado de un Parte

Click en el **badge de estado** (columna Estado) para abrir menú contextual:

#### Estado: Abierto 🟢

**Acciones disponibles:**
- **⏸️ Pausar**: Pausa el parte temporalmente
  - Cambia estado a **Pausado**
  - NO modifica hora de fin
  
- **✅ Cerrar**: Finaliza y cierra el parte
  - Muestra diálogo para confirmar **hora de cierre**
  - Valida que hora fin > hora inicio
  - Cambia estado a **Cerrado**

#### Diálogo de Cierre

```
┌────────────────────────────────────────────────────────┐
│ 🔒 Cerrar Parte #1234                                  │
├────────────────────────────────────────────────────────┤
│                                                         │
│ 📋 Información del Parte:                              │
│ • Cliente: MERCADONA                                   │
│ • Tienda: Valencia 001                                 │
│ • Fecha: 02/01/2026                                    │
│ • Hora Inicio: 09:00                                   │
│                                                         │
│ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│                                                         │
│ ⏰ Hora de Cierre:                                     │
│                                                         │
│         ┌─────────┐                                    │
│         │ [10:30] │  ← Hora actual por defecto        │
│         └─────────┘                                    │
│                                                         │
│ 💡 Sugerencias:                                        │
│ • Ajusta la hora si el trabajo terminó antes/después  │
│ • Formato: HH:mm (ej: 14:30)                          │
│                                                         │
│        [ ✅ Cerrar Parte ]  [ ❌ Cancelar ]            │
└────────────────────────────────────────────────────────┘
```

**Validaciones:**
- Hora de cierre debe ser posterior a hora de inicio
- Formato HH:mm obligatorio
- Si está vacío, usa hora actual por defecto

#### Estado: Pausado 🟡

**Acciones disponibles:**
- **▶️ Reanudar**: Vuelve a estado Abierto
  - Permite seguir trabajando en el parte

#### Estado: Cerrado 🔵

**Acciones disponibles:**
- **📋 Duplicar**: Crea nuevo parte con datos copiados
  - Fecha: HOY
  - Hora Inicio: Ahora
  - Hora Fin: Vacío
  - Cliente, Tienda, Grupo, Tipo: Copiados del original
  - Acción: Copiada del original
  - Estado: Abierto

---

## 📊 Importación de Excel

### Abrir Importador

**Atajo:** `Ctrl+I` o botón **📊 Importar**

### Diálogo de Importación

```
┌─────────────────────────────────────────────────────────────┐
│ 📊 Importación de Partes desde Excel                        │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│ 📁 Archivo: C:\Documentos\partes_enero.xlsx                 │
│ 📄 Hoja: "Partes"                                           │
│ 📊 Registros encontrados: 45                                │
│                                                              │
│ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│                                                              │
│ 🔍 PREVIEW DE DATOS (Primeras 5 filas):                     │
│                                                              │
│ ┌────┬────────────┬───────────┬──────────┬───────┬─────┐   │
│ │ #  │ Fecha      │ Cliente   │ Inicio   │ Fin   │ Val │   │
│ ├────┼────────────┼───────────┼──────────┼───────┼─────┤   │
│ │ 1  │ 02/01/2026 │ MERCADONA │ 09:00    │ 10:30 │  ✅  │   │
│ │ 2  │ 02/01/2026 │ CARREFOUR │ 11:00    │ 12:45 │  ✅  │   │
│ │ 3  │ 02/01/2026 │ DIA       │ 14:00    │ -     │  ⚠️  │   │
│ │ 4  │ 03/01/2026 │ -         │ 08:30    │ 09:15 │  ❌  │   │
│ │ 5  │ 03/01/2026 │ ALCAMPO   │ 10:00    │ 11:30 │  ✅  │   │
│ └────┴────────────┴───────────┴──────────┴───────┴─────┘   │
│                                                              │
│ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│                                                              │
│ 📈 RESUMEN:                                                  │
│ • ✅ Válidos: 42 registros                                   │
│ • ⚠️ Advertencias: 2 registros (hora fin vacía)             │
│ • ❌ Errores: 1 registro (cliente vacío)                     │
│                                                              │
│ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│                                                              │
│ ⚙️ OPCIONES:                                                │
│ ☑️ Importar solo válidos (42 registros)                     │
│ ☑️ Omitir duplicados (por fecha + cliente + inicio)         │
│ ☑️ Crear clientes nuevos automáticamente                    │
│                                                              │
│        [ ✅ Importar (42) ] [ ❌ Cancelar ]                 │
└─────────────────────────────────────────────────────────────┘
```

### Formato de Excel Soportado

#### Columnas Requeridas

| Columna | Descripción | Ejemplo | Obligatorio |
|---------|-------------|---------|-------------|
| **fecha** | Fecha del trabajo | 02/01/2026 o 2026-01-02 | ✅ Sí |
| **cliente** | Nombre del cliente | MERCADONA | ✅ Sí |
| **hora_inicio** | Hora de inicio | 09:00 o 9:00 | ✅ Sí |

#### Columnas Opcionales

| Columna | Descripción | Ejemplo | Por Defecto |
|---------|-------------|---------|-------------|
| **hora_fin** | Hora de finalización | 10:30 | (vacío) |
| **tienda** | Nombre o código tienda | Valencia 001 | (vacío) |
| **accion** | Descripción del trabajo | Mantenimiento TPV | (vacío) |
| **ticket** | Número de ticket | TK-2026-0001 | (vacío) |
| **grupo** | Grupo de trabajo | Mantenimiento | (vacío) |
| **tipo** | Tipo de servicio | Correctivo | (vacío) |

### Validaciones Automáticas

#### ✅ Válido
- Todos los campos obligatorios presentes
- Formatos correctos
- Cliente existe en catálogo (o se puede crear)

#### ⚠️ Advertencia
- Hora fin vacía (parte quedará abierto)
- Cliente nuevo (se creará automáticamente)
- Grupo/Tipo no encontrados (se usará texto libre)

#### ❌ Error
- Campo obligatorio vacío (fecha, cliente, hora inicio)
- Formato de fecha inválido
- Formato de hora inválido (debe ser HH:mm)
- Hora fin anterior a hora inicio

### Proceso de Importación

1. **Selección de Archivo**
   - Click en botón **📊 Importar**
   - Selector de archivos (`.xls` o `.xlsx`)

2. **Análisis y Preview**
   - Sistema lee el archivo
   - Valida cada fila
   - Muestra preview de primeras 5 filas
   - Genera resumen de validación

3. **Configuración de Opciones**
   - **Importar solo válidos**: Omite filas con errores
   - **Omitir duplicados**: No importa si ya existe (fecha + cliente + hora inicio)
   - **Crear clientes nuevos**: Crea automáticamente clientes que no existen

4. **Confirmación**
   - Click en **✅ Importar (X)**
   - Muestra barra de progreso

5. **Resultado**
   ```
   ✅ IMPORTACIÓN COMPLETADA
   
   📊 Resumen:
   • Registros procesados: 45
   • Importados exitosamente: 42
   • Omitidos (duplicados): 1
   • Errores: 2
   
   [ ✅ Ver lista actualizada ]
   ```

6. **Actualización Automática**
   - La lista de partes se recarga automáticamente
   - Se muestra notificación de éxito
   - Los nuevos partes aparecen en la lista

### Plantilla Excel Recomendada

```
| fecha      | cliente   | tienda       | hora_inicio | hora_fin | accion                    | ticket        | grupo          | tipo        |
|------------|-----------|--------------|-------------|----------|---------------------------|---------------|----------------|-------------|
| 02/01/2026 | MERCADONA | Valencia 001 | 09:00       | 10:30    | Mantenimiento TPV         | TK-2026-0001  | Mantenimiento  | Correctivo  |
| 02/01/2026 | CARREFOUR | Madrid 015   | 11:00       | 12:45    | Instalación lectores RFID | TK-2026-0002  | Instalación    | Proyecto    |
| 02/01/2026 | DIA       | Barcelona 03 | 14:00       |          | Soporte remoto            | TK-2026-0003  | Soporte        | Incidencia  |
```

---

## 👤 Perfil de Usuario

### Acceder al Perfil

**Desde:** Banner superior → Botón **[Mi Perfil]**

### Pantalla de Perfil

```
┌─────────────────────────────────────────────────────────────┐
│ 🏢 Logo  │  👤 Mi Perfil de Usuario                     │ 🎨│
├─────────────────────────────────────────────────────────────┤
│                                                              │
│ ╔═══════════════════════════════════════════════════════╗   │
│ ║ 📝 INFORMACIÓN PERSONAL                               ║   │
│ ╠═══════════════════════════════════════════════════════╣   │
│ ║ Nombre:    [Francisco]     Apellidos: [Santos García]║   │
│ ║ Teléfono:  [965268092]     Móvil: [654321098]        ║   │
│ ║ Email:     psantos@global-retail.com (solo lectura)  ║   │
│ ╚═══════════════════════════════════════════════════════╝   │
│                                                              │
│ ╔═══════════════════════════════════════════════════════╗   │
│ ║ 🏢 INFORMACIÓN LABORAL                                ║   │
│ ╠═══════════════════════════════════════════════════════╣   │
│ ║ Departamento: [Soporte Técnico]                       ║   │
│ ║ Puesto:       [Técnico Senior]                        ║   │
│ ║ Tipo:         [○ Permanente  ○ Temporal  ○ Becario]  ║   │
│ ║ Fecha Alta:   [15/03/2020]                            ║   │
│ ╚═══════════════════════════════════════════════════════╝   │
│                                                              │
│ ╔═══════════════════════════════════════════════════════╗   │
│ ║ 🏠 DIRECCIÓN                                          ║   │
│ ╠═══════════════════════════════════════════════════════╣   │
│ ║ Dirección:    [Calle Mayor, 123, 3º B]               ║   │
│ ║ Ciudad:       [Valencia]   Código Postal: [46001]    ║   │
│ ╚═══════════════════════════════════════════════════════╝   │
│                                                              │
│ ╔═══════════════════════════════════════════════════════╗   │
│ ║ 📸 AVATAR Y NOTAS                                     ║   │
│ ╠═══════════════════════════════════════════════════════╣   │
│ ║ Avatar URL: [https://example.com/avatar.jpg]         ║   │
│ ║ Notas:      [Especialista en TPV y sistemas de caja] ║   │
│ ╚═══════════════════════════════════════════════════════╝   │
│                                                              │
├─────────────────────────────────────────────────────────────┤
│         [ 💾 Guardar Cambios ] [ ❌ Cancelar ] [ ⬅ Volver ] │
└─────────────────────────────────────────────────────────────┘
```

### Campos Editables

#### Información Personal

| Campo | Descripción | Editable |
|-------|-------------|----------|
| **Nombre** | Nombre del usuario | ✅ Sí |
| **Apellidos** | Apellidos completos | ✅ Sí |
| **Teléfono** | Teléfono fijo | ✅ Sí |
| **Móvil** | Teléfono móvil | ✅ Sí |
| **Email** | Correo electrónico | ❌ No (solo lectura) |

#### Información Laboral

| Campo | Descripción | Editable |
|-------|-------------|----------|
| **Departamento** | Departamento de la empresa | ✅ Sí |
| **Puesto** | Cargo o puesto de trabajo | ✅ Sí |
| **Tipo de Empleado** | Permanente / Temporal / Becario | ✅ Sí |
| **Fecha Alta** | Fecha de incorporación | ✅ Sí |

#### Dirección

| Campo | Descripción | Editable |
|-------|-------------|----------|
| **Dirección** | Dirección completa | ✅ Sí |
| **Ciudad** | Ciudad de residencia | ✅ Sí |
| **Código Postal** | Código postal | ✅ Sí |

#### Otros

| Campo | Descripción | Editable |
|-------|-------------|----------|
| **Avatar URL** | URL de imagen de perfil | ✅ Sí |
| **Notas** | Notas adicionales (especialidades, etc.) | ✅ Sí |

### Guardar Cambios

1. Modificar los campos deseados
2. Click en **💾 Guardar Cambios**
3. Sistema valida datos
4. Actualiza perfil en el servidor
5. Muestra notificación de éxito
6. Vuelve automáticamente a la página principal

**Nota:** El **nombre completo** y **teléfono** del banner se actualizan automáticamente.

### Cancelar Edición

- Click en **❌ Cancelar**: Descarta cambios (pide confirmación si hay modificaciones)
- Click en **⬅ Volver**: Vuelve a la página principal (pide confirmación si hay cambios)

---

## ⚙️ Configuración del Sistema

### Acceder a Configuración

**Método 1:** Menú del banner → **⚙️ Configuración** (si está disponible)  
**Método 2:** `F12` (si está habilitado)

**Nota:** Solo disponible para usuarios con rol **Administrador** o **Técnico**.

### Ventana de Configuración

```
┌─────────────────────────────────────────────────────────────┐
│ ⚙️ Configuración del Sistema - GestionTime                  │
├─────────────────────────────────────────────────────────────┤
│ ╔═══════════════════════════════════════════════════════╗   │
│ ║ ⚡ CONFIGURACIÓN DE CONEXIÓN                          ║   │
│ ╠═══════════════════════════════════════════════════════╣   │
│ ║ URL del Servidor API:                                 ║   │
│ ║ [https://api.gestiontime.com]          [🔍 Probar]    ║   │
│ ║                                                        ║   │
│ ║ Timeout (segundos): [30]  Reintentos: [3]            ║   │
│ ║ ☑️ Ignorar certificados SSL en desarrollo             ║   │
│ ╚═══════════════════════════════════════════════════════╝   │
│                                                              │
│ ╔═══════════════════════════════════════════════════════╗   │
│ ║ 📋 CONFIGURACIÓN DE LOGS                              ║   │
│ ╠═══════════════════════════════════════════════════════╣   │
│ ║ ☑️ Habilitar sistema de logging completo              ║   │
│ ║                                                        ║   │
│ ║ Nivel de detalle:                                     ║   │
│ ║ [🐛 Debug - Información detallada para depuración]   ║   │
│ ║                                                        ║   │
│ ║ 📁 Directorio principal:                              ║   │
│ ║ [C:\Logs\GestionTime] [📂 Examinar] [🔍 Abrir] [🧪]  ║   │
│ ║                                                        ║   │
│ ║ 💾 ARCHIVOS:                                          ║   │
│ ║ ☑️ Guardar logs en archivos locales                   ║   │
│ ║   ☑️ 📝 Log Principal (gestiontime_YYYYMMDD.log)      ║   │
│ ║   ☑️ ❌ Log de Errores (errors_YYYYMMDD.log)          ║   │
│ ║   ☐ 🌐 Log HTTP/API (http_YYYYMMDD.log)              ║   │
│ ║   ☐ 🐛 Log Debug/Performance (debug_YYYYMMDD.log)    ║   │
│ ║                                                        ║   │
│ ║ 🔄 Rotación: [📅 Diaria] Retención: [30] días        ║   │
│ ╚═══════════════════════════════════════════════════════╝   │
│                                                              │
│ ╔═══════════════════════════════════════════════════════╗   │
│ ║ ⚙️ CONFIGURACIÓN DE APLICACIÓN                        ║   │
│ ╠═══════════════════════════════════════════════════════╣   │
│ ║ Tema: [Automático (según sistema)]                    ║   │
│ ║ Auto-actualización: [30] segundos                     ║   │
│ ║                                                        ║   │
│ ║ Opciones de inicio:                                   ║   │
│ ║ ☐ Auto-login   ☐ Iniciar minimizado   ☐ Modo debug  ║   │
│ ╚═══════════════════════════════════════════════════════╝   │
│                                                              │
├─────────────────────────────────────────────────────────────┤
│ Configuración cargada correctamente                         │
│                   [ 💾 Guardar ] [ ✅ Validar ] [ ❌ Cerrar ]│
└─────────────────────────────────────────────────────────────┘
```

### Sección: Conexión

#### URL del Servidor API

- **Campo:** URL completa del servidor (ej: `https://api.gestiontime.com`)
- **Botón 🔍 Probar:** Verifica conectividad con el servidor
- **Validación:** Debe empezar con `http://` o `https://`

#### Timeout y Reintentos

- **Timeout:** Segundos de espera antes de considerar fallo (5-300)
- **Reintentos:** Número de intentos automáticos en caso de fallo (0-10)

#### Ignorar SSL

- **⚠️ Solo para desarrollo**
- Permite conexiones con certificados SSL autofirmados
- **NO recomendado en producción**

### Sección: Logs

#### Habilitar Logging

- **☑️ Activado:** Sistema de logs completo habilitado
- **☐ Desactivado:** Solo logs críticos

#### Nivel de Detalle

| Nivel | Descripción | Recomendado para |
|-------|-------------|------------------|
| **🔴 Error** | Solo errores críticos | Producción (mínimo) |
| **⚠️ Warning** | Advertencias y errores | Producción (normal) |
| **ℹ️ Info** | Información general | Producción (detallado) |
| **🐛 Debug** | Información de depuración | Desarrollo |
| **🔍 Trace** | Todos los eventos | Diagnóstico profundo |

#### Directorio de Logs

- **Campo:** Ruta completa del directorio (ej: `C:\Logs\GestionTime`)
- **Botón 📂 Examinar:** Selector de carpetas
- **Botón 🔍 Abrir:** Abre el explorador de archivos
- **Botón 🧪 Probar:** Crea archivos de prueba en el directorio

#### Archivos de Log

**Automáticamente generados:**

| Archivo | Contenido | Obligatorio |
|---------|-----------|-------------|
| **📝 gestiontime_YYYYMMDD.log** | Log principal de la aplicación | ✅ Sí (si logging activado) |
| **❌ errors_YYYYMMDD.log** | Solo errores críticos | ✅ Recomendado |
| **🌐 http_YYYYMMDD.log** | Llamadas HTTP/API completas | ❌ Opcional |
| **🐛 debug_YYYYMMDD.log** | Debug y métricas de rendimiento | ❌ Opcional |

**Ejemplo de nombre de archivo:**
- `gestiontime_20260102.log` (2 de enero de 2026)
- `errors_20260102.log`

#### Rotación de Logs

- **📅 Diaria:** Nuevo archivo cada día (recomendado)
- **📊 Por tamaño (10MB):** Nuevo archivo al alcanzar 10MB
- **📆 Semanal:** Nuevo archivo cada semana
- **🗓️ Mensual:** Nuevo archivo cada mes

#### Retención

- **Días:** Número de días que se conservan los archivos antiguos
- **Ejemplo:** 30 días = archivos mayores a 30 días se eliminan automáticamente

### Sección: Aplicación

#### Tema

- **Automático:** Se adapta al tema de Windows 11
- **Claro:** Colores claros (modo día)
- **Oscuro:** Colores oscuros (modo noche)

#### Auto-actualización

- **Segundos:** Frecuencia de actualización automática de la lista de partes
- **Rango:** 10-300 segundos
- **Por defecto:** 30 segundos

#### Opciones de Inicio

- **☐ Auto-login:** Inicia sesión automáticamente (si hay credenciales guardadas)
- **☐ Iniciar minimizado:** Abre la aplicación minimizada en la barra de tareas
- **☐ Modo debug:** Habilita opciones de depuración avanzadas (solo admins)

### Guardar Configuración

1. **Modificar opciones** según necesidades
2. **Click en ✅ Validar** (opcional): Verifica que la configuración es válida
3. **Click en 💾 Guardar**: Aplica y guarda cambios
4. Sistema actualiza `appsettings.json`
5. Cambios se aplican **inmediatamente** (sin reiniciar)

### Probar Directorio de Logs

1. **Click en botón 🧪 Probar**
2. Sistema crea 4 archivos de prueba:
   ```
   ✅ PRUEBA EXITOSA
   
   📁 Directorio: C:\Logs\GestionTime
   📝 Archivos creados: 4
   
   • gestiontime_test_20260102_143052.log - 486 bytes
   • errors_test_20260102_143052.log - 423 bytes
   • http_test_20260102_143052.log - 512 bytes
   • debug_test_20260102_143052.log - 498 bytes
   
   [ 📂 Ver archivos ] [ 🗑️ Eliminar ahora ] [ OK ]
   ```
3. **Ver archivos:** Abre el explorador en el directorio
4. **Eliminar ahora:** Borra los archivos de prueba
5. **OK:** Deja los archivos para análisis

---

## ⌨️ Atajos de Teclado

### Globales (DiarioPage)

| Atajo | Acción | Contexto |
|-------|--------|----------|
| **F5** | Refrescar lista | Restaura fecha a HOY y recarga |
| **F12** | Configuración | Solo Admin/Técnico |
| **Ctrl+N** | Nuevo parte | Abre editor de nuevo parte |
| **Ctrl+T** | Nueva llamada | Crea llamada telefónica rápida |
| **Ctrl+E** | Editar parte | Requiere selección |
| **Ctrl+I** | Importar Excel | Abre selector de archivos |
| **Ctrl+Q** | Cerrar sesión | Vuelve al login |
| **Delete** | Borrar parte | Requiere selección y confirmación |

### Editor de Partes

| Atajo | Acción | Contexto |
|-------|--------|----------|
| **Enter** | Siguiente campo | En cualquier campo (excepto Descripción) |
| **Tab** | Siguiente campo | En cualquier campo |
| **Shift+Tab** | Campo anterior | Navegación inversa |
| **Ctrl+Enter** | Guardar | Desde campo Descripción |
| **Ctrl+S** | Guardar | Desde cualquier campo |
| **Esc** | Cancelar/Salir | Pide confirmación si hay cambios |
| **F4** | Abrir ComboBox | En campos Grupo/Tipo |

### Navegación

| Atajo | Acción | Contexto |
|-------|--------|----------|
| **↑/↓** | Navegar por lista | En ListView de partes |
| **Enter** | Abrir edición | En parte seleccionado |
| **Ctrl+Home** | Primer parte | Selecciona primer elemento |
| **Ctrl+End** | Último parte | Selecciona último elemento |

---

## 🔧 Solución de Problemas

### Error: "Sin conexión con el servidor"

**Síntomas:**
- LED rojo en el banner
- Notificación: "Servicio: Offline"

**Soluciones:**
1. **Verificar servidor:**
   - Abrir navegador y acceder a `https://api.gestiontime.com/health`
   - Debería mostrar: `{"status":"healthy"}`

2. **Verificar URL en configuración:**
   - Abrir Configuración (F12)
   - Sección "Conexión"
   - Click en "🔍 Probar" para verificar conectividad

3. **Verificar firewall:**
   - Asegurar que GestionTime.Desktop.exe tiene permiso de red
   - Puerto HTTPS (443) debe estar abierto

### Error: "Credenciales incorrectas"

**Síntomas:**
- Mensaje rojo en login: "Email o contraseña incorrectos"

**Soluciones:**
1. **Verificar credenciales:**
   - Email correcto (ej: `psantos@global-retail.com`)
   - Contraseña correcta (case-sensitive)

2. **Recuperar contraseña:**
   - Click en "¿Olvidaste tu contraseña?"
   - Seguir instrucciones por email

3. **Contactar administrador:**
   - Si el problema persiste, contactar al administrador del sistema

### Error: "No se puede guardar en el directorio de logs"

**Síntomas:**
- Notificación: "Error de permisos: 🚫 Sin permisos de escritura"

**Soluciones:**
1. **Ejecutar como administrador:**
   - Click derecho en GestionTime.Desktop.exe
   - "Ejecutar como administrador"

2. **Cambiar directorio de logs:**
   - Abrir Configuración (F12)
   - Sección "Logs" → Directorio
   - Elegir directorio en carpeta de usuario: `C:\Users\TuUsuario\Logs\GestionTime`

3. **Verificar permisos:**
   - Click derecho en carpeta → Propiedades → Seguridad
   - Asegurar que tu usuario tiene "Control total"

### Error: "Parte no se actualiza en la lista"

**Síntomas:**
- Editas un parte, guardas, pero no se refleja en la lista

**Soluciones:**
1. **Refrescar manualmente:**
   - Presionar **F5** o click en botón 🔄
   - Esto recarga la lista desde el servidor

2. **Verificar fecha del filtro:**
   - Si el parte tiene fecha diferente a la seleccionada, no aparecerá
   - Cambiar filtro de fecha a la fecha del parte

3. **Limpiar caché:**
   - Cerrar sesión (Ctrl+Q)
   - Volver a iniciar sesión

### Error: "Importación de Excel falla"

**Síntomas:**
- Mensaje: "❌ Error leyendo archivo Excel"

**Soluciones:**
1. **Verificar formato del archivo:**
   - Debe ser `.xls` o `.xlsx`
   - NO usar formatos CSV o TXT

2. **Verificar columnas obligatorias:**
   - Debe tener columnas: `fecha`, `cliente`, `hora_inicio`
   - Nombres exactos (minúsculas, sin espacios)

3. **Verificar datos:**
   - Fechas en formato `DD/MM/YYYY` o `YYYY-MM-DD`
   - Horas en formato `HH:mm` (ej: `09:00`)
   - Cliente no vacío

4. **Descargar plantilla:**
   - Contactar administrador para plantilla oficial

### Aplicación se cierra inesperadamente

**Soluciones:**
1. **Revisar logs:**
   - Abrir directorio de logs: `C:\Logs\GestionTime`
   - Ver archivo más reciente: `gestiontime_YYYYMMDD.log`
   - Buscar líneas con `ERROR` o `CRITICAL`

2. **Verificar requisitos del sistema:**
   - Windows 11 (64-bit)
   - .NET 8 Runtime instalado
   - 4GB RAM mínimo

3. **Reinstalar aplicación:**
   - Desinstalar desde "Agregar o quitar programas"
   - Descargar instalador más reciente
   - Reinstalar

### Banner muestra "Usuario" en lugar del nombre

**Síntomas:**
- Banner muestra "Usuario" y email genérico

**Soluciones:**
1. **Verificar perfil en el backend:**
   - Click en **[Mi Perfil]**
   - Verificar que campos Nombre y Apellidos estén rellenados
   - Guardar si están vacíos

2. **Cerrar sesión y volver a entrar:**
   - Ctrl+Q para cerrar sesión
   - Volver a iniciar sesión
   - El perfil se carga automáticamente

3. **Crear perfil básico:**
   - Si aparece mensaje "Perfil No Encontrado"
   - Click en "Crear Perfil Básico"
   - Sistema crea perfil automáticamente con datos del login

---

## 📞 Soporte Técnico

### Información de Contacto

**Email:** soporte@gestiontime.com  
**Teléfono:** +34 900 123 456  
**Horario:** Lunes a Viernes, 9:00 - 18:00 (CET)

### Antes de Contactar

Por favor, ten lista la siguiente información:

1. **Versión de la aplicación:**
   - Abrir aplicación → Banner → "GestionTime Desktop v1.2.0"

2. **Sistema operativo:**
   - Windows 11 (versión completa: Windows + Pausa → "Acerca de")

3. **Descripción del problema:**
   - ¿Qué estabas haciendo cuando ocurrió el error?
   - ¿Mensaje de error exacto?

4. **Logs (si es posible):**
   - Archivo `gestiontime_YYYYMMDD.log` del día del problema
   - Ubicación: `C:\Logs\GestionTime\`

### Recursos Adicionales

- **Documentación Técnica:** Ver carpeta `Docs/` en el repositorio
- **Guías de Implementación:** Ver archivos `.md` en `Docs/`
- **Código Fuente:** [GitHub Repository](https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop)

---

## 📝 Historial de Cambios

### Versión 1.2.0 (Enero 2026)

**🎉 Nuevas Funcionalidades:**
- ✅ Banner dinámico con perfil completo (nombre + email + teléfono)
- ✅ Importación masiva de partes desde Excel
- ✅ Sistema de notificaciones in-app (éxito, error, advertencia, info)
- ✅ Tooltip dinámico de cobertura de tiempo en columna "Dur."
- ✅ Página de perfil de usuario editable
- ✅ Botón "Llamada Telefónica Rápida" (Ctrl+T)
- ✅ Diálogo mejorado de cierre de partes con validación

**🔧 Mejoras:**
- ⚡ Carga inteligente: Últimos 25 partes en lugar de 30 días
- ⚡ Filtro por fecha específica: Solo ese día (1 petición HTTP)
- ⚡ Zebra rows en ListView con virtualización
- ⚡ Timestamps automáticos en campo Descripción
- ⚡ ComboBox con autocompletado inteligente

**🐛 Correcciones:**
- ✅ Email del login siempre se guarda correctamente
- ✅ Perfil se carga correctamente desde API
- ✅ Teléfono solo se muestra si tiene valor
- ✅ Validación de horas mejorada (HH:mm)
- ✅ Estados de partes ahora respetan lógica de negocio

---

## 📄 Licencia

**GestionTime Desktop** © 2026 Global Retail Solutions  
Todos los derechos reservados.

Este software es propiedad de Global Retail Solutions y está protegido por leyes de derechos de autor internacionales.

---

**🎯 ¡Gracias por usar GestionTime Desktop!**

*Manual de Usuario - Versión 1.2.0 - Enero 2026*
