# 🚀 GestionTime Desktop — Novedades v1.9.5 Beta

**© 2025 EcoNexia Tech S.L. · CEO: Francisco Santos Díaz**

---

## 📊 1. SISTEMA DE INFORMES

### Ventana dedicada de Informes
- Acceso directo desde la pantalla principal (DiarioPage).
- **3 alcances de consulta**: Día, Semana y Rango personalizado.
- Selector de semanas rápido (últimas 12 semanas).
- Los roles EDITOR y ADMIN pueden consultar informes de cualquier agente.

### Gráfica semanal
- Barras Lunes a Sábado con horas registradas por día.
- Indicador visual: ✅ verde si ≥ 8h, ⚠️ ámbar si < 8h.
- Porcentaje de distribución y total semanal con horario global.

### Exportación
- **Excel** (ClosedXML) con gráfica de barras y fórmulas sumables.
- **PDF** (QuestPDF) con logo, barras de colores y esquinas redondeadas.
- **Email** con resumen automático listo para enviar.

### Detección de solapamientos de tiempo
- Tabla detallada de partes solapados (ID, Fecha, Cliente, Ticket, Inicio/Fin, Duración).
- Edición inline de hora Inicio/Fin con validación en tiempo real (formato HH:mm).
- Botón **"Solución Automática"**: algoritmo greedy que resuelve solapes sin intervención manual.

---

## 🗒️ 2. NOTAS DE CLIENTE — GLOBAL + PERSONAL

### Nota global (por cliente)
- Una sola nota compartida, visible por todos los usuarios.
- Solo EDITOR y ADMIN pueden editarla.
- Indicador de quién y cuándo fue la última edición.

### Nota personal (por usuario y cliente)
- Cada usuario tiene su propia nota privada por cliente.
- Solo él puede verla y editarla — nadie más tiene acceso.

### Acceso rápido desde ParteItemEdit
- Icono 📝 junto al campo Cliente.
- Tooltip con preview (primeros 200 caracteres).
- Diálogo con 2 secciones separadas y guardado independiente.

| Acción | USER | EDITOR | ADMIN |
|--------|:----:|:------:|:-----:|
| Ver nota global | ✅ Solo lectura | ✅ | ✅ |
| Editar nota global | ❌ | ✅ | ✅ |
| Ver/editar su nota personal | ✅ | ✅ | ✅ |
| Ver notas personales de otros | ❌ | ❌ | ❌ |

---

## ⚙️ 3. MENÚ DE CONFIGURACIÓN (SETTINGS)

### Secciones disponibles según rol
- **Perfil y cuenta** — todos los roles.
- **Clientes** — EDITOR y ADMIN.
- **Grupos y Tipos** — EDITOR y ADMIN.
- **Permisos y roles** — solo ADMIN.
- **Integraciones** — solo ADMIN.
- **Importación/Exportación** — solo ADMIN.
- **Parámetros** — solo ADMIN.
- **Usuarios online / Presencia** — todos los roles.

### Indicadores visuales
- Candado 🟢 abierto = acceso permitido.
- Candado 🔴 cerrado = sección restringida para tu rol.
- Secciones sin permiso aparecen deshabilitadas (no ocultas).

---

## 👤 4. MI PERFIL

- Edición inline directamente en Settings (layout de 2 columnas).
- **11 campos editables**: nombre, apellidos, teléfono, dirección, ciudad, provincia, CP, país, departamento, cargo, idioma.
- El campo **email no es editable** (vinculado a la cuenta).
- Los datos se recargan desde el servidor cada vez que se abre Settings.
- Icono de usuario en la sección de perfil.

---

## 🔑 5. RECORDAR CONTRASEÑA EN LOGIN *(Próximamente)*

> **Estado:** Planificado para una versión futura.

- Opción "Recordar mis credenciales" en la pantalla de login.
- Almacenamiento seguro del token de sesión (Windows Credential Manager).
- Auto-login al abrir la aplicación si la sesión no ha expirado.
- Botón "Olvidé mi contraseña" con flujo de recuperación por email.

---

## 🏷️ 6. TAGS POR PARTE

- Hasta **5 tags** por parte de trabajo.
- Autocompletado inteligente con sugerencias del backend (desde 1 carácter).
- Chips visuales con botón ✕ para eliminar.
- Contador visible (n/5).
- Validaciones: no duplicados, no vacíos, máximo 5.
- Navegación por teclado: ↑/↓/Enter/Escape.
- Los tags se exportan correctamente a Excel y se copian al duplicar un parte.

---

## 🔀 7. VALIDACIÓN DE SOLAPAMIENTOS EN INFORMES

- Detección automática de partes con horas solapadas.
- Tabla de conflictos con columnas: ID, Fecha, Cliente, Ticket, Inicio, Fin, Duración, "Solapa con".
- **Edición inline**: corregir horas directamente en la tabla con validación en tiempo real.
- **Máscara HH:mm**: al escribir 4 dígitos se inserta automáticamente los dos puntos.
- **Solución Automática**: algoritmo que reordena/ajusta los partes sin crear nuevos solapes.
- Tras la corrección, la cache se invalida y los datos se recargan automáticamente.

---

## 📈 8. ESTADÍSTICAS

### Métricas en tiempo real
- **Total de partes** registrados en el periodo seleccionado.
- **Horas registradas** vs **horas reales** (con delta).
- **Solapes detectados** con indicador visual.
- **Horario global**: primera entrada y última salida del día/semana.

### Gráfica semanal
- Distribución de horas por día (Lunes a Sábado).
- Objetivo visual de 8h/día con indicadores de cumplimiento.
- Porcentaje de distribución semanal.
- Total semanal acumulado.

### Exportación de estadísticas
- Excel con fórmulas sumables y gráficas.
- PDF con diseño profesional, logo y barras de colores.

---

## 🔐 9. CONTROL DE ROLES Y PERMISOS

### Roles definidos
| Rol | Descripción |
|-----|-------------|
| **ADMIN** | Acceso total sin restricciones. |
| **EDITOR** | Gestión de clientes, grupos, tipos e informes de otros agentes. |
| **USER** | Solo sus propios datos: partes, informes personales, perfil y notas personales. |

### Permisos por funcionalidad
| Funcionalidad | USER | EDITOR | ADMIN |
|---------------|:----:|:------:|:-----:|
| Ver/editar sus partes | ✅ | ✅ | ✅ |
| Informes propios | ✅ | ✅ | ✅ |
| Informes de otros agentes | ❌ | ✅ | ✅ |
| Gestión de clientes | ❌ | ✅ | ✅ |
| Gestión de grupos/tipos | ❌ | ✅ | ✅ |
| Notas globales de cliente | ❌ | ✅ | ✅ |
| Cambiar roles de usuario | ❌ | ❌ | ✅ |
| Kick de usuarios | ❌ | ❌ | ✅ |
| Integraciones (Freshdesk) | ❌ | ❌ | ✅ |
| Parámetros del sistema | ❌ | ❌ | ✅ |

### Gestión de usuarios (solo ADMIN)
- Lista paginada con búsqueda.
- Cambio de rol inline.
- Botón "Kick" para desconectar usuarios.
- Tarjetas con avatar, estado y última actividad.

---

## 🛡️ 10. SEGURIDAD

### Autenticación
- **JWT** con refresh automático de token.
- Manejo de **401 Unauthorized** con re-login transparente.
- Logout limpia la presencia del usuario en el backend.

### Comunicaciones
- Todas las conexiones por **HTTPS**.
- Validación de certificado SSL.
- Headers `User-Agent` identificativo en todas las llamadas API.

### Control de acceso
- Permisos validados tanto en **frontend** (UI deshabilitada) como en **backend** (403 Forbidden).
- El backend extrae el rol desde el token JWT — no confía en datos del cliente.
- Las notas personales están aisladas por `owner_user_id` — imposible ver las de otro usuario.

### Presencia y monitorización
- **Heartbeat** cada 60 segundos para mantener estado "online".
- Sistema de presencia: Online → Ausente (>5 min sin ping) → Offline.
- El ADMIN puede ver el estado de todos los usuarios en tiempo real.
- Botón "Kick" para forzar desconexión de sesiones sospechosas.

### Instalador MSI
- Instalación en ruta fija: `C:\App\GestionTime-Desktop`.
- Acuerdo de licencia mostrado antes de instalar.
- Detección de versiones anteriores con upgrade automático.
- Solo compatible con Windows 10+ de 64 bits.

---

> **GestionTime Desktop v1.9.5-beta**
> © 2025 EcoNexia Tech S.L. · CEO: Francisco Santos Díaz
