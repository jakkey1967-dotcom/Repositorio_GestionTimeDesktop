# ⚙️ SETTINGS - TEMAS PENDIENTES

**Fecha**: 2025-02-01  
**Estado**: SettingsWindow implementado y funcional (menú, roles, navegación)

---

## ✅ IMPLEMENTADO

### 1. Estructura base
- ✅ SettingsWindow (ventana independiente 1600x950)
- ✅ Layout 2 columnas (menú + contenido)
- ✅ Buscador funcional
- ✅ Sistema de roles (ADMIN por defecto)
- ✅ Filtrado de secciones por permisos
- ✅ Atajo **Ctrl+Alt+P** para guardar tamaño

### 2. Secciones funcionales
- ✅ **Perfil y cuenta**: Muestra datos de `App.CurrentUserProfile`
- ✅ **Permisos y roles**: Botón abre `UsersOnlineWindow` ✅ **FUNCIONA**
- ✅ **Usuarios online**: Botón abre `UsersOnlineWindow` ✅ **FUNCIONA**

### 3. Placeholders
- ✅ Clientes (pendiente CRUD)
- ✅ Grupos y Tipos (pendiente CRUD)
- ✅ Integraciones (pendiente edición de appsettings.json)
- ✅ Import/Export (pendiente integración con ExcelPartesImportService)
- ✅ Parámetros (pendiente definir parámetros globales)

---

## ❌ PENDIENTE DE IMPLEMENTAR

### 🔴 PRIORIDAD ALTA

#### 1. **Editar Perfil** (UserProfilePage)
**Estado**: Botón existe pero NO hace nada  
**Archivo**: `SettingsWindow.xaml.cs` línea ~238

**Acción necesaria**:
```csharp
// Opción A: Abrir UserProfilePage en MainWindow
App.MainWindowInstance.Navigate(typeof(UserProfilePage));
this.Close(); // Cerrar Settings

// Opción B: Abrir UserProfilePage en ventana modal/child
var profileWindow = new Window();
var profilePage = new UserProfilePage();
profileWindow.Content = profilePage;
profileWindow.Activate();
```

**Prerequisitos**:
- Verificar que `UserProfilePage` existe y está funcional
- Decidir si abrir en MainWindow o en ventana nueva

**Archivos afectados**:
- `Views/SettingsWindow.xaml.cs` (handler del botón)
- `Views/UserProfilePage.xaml.cs` (si necesita ajustes)

---

### 🟡 PRIORIDAD MEDIA

#### 2. **Clientes (CRUD)**
**Servicio existente**: `App.ClientesService`  
**Endpoint backend**: `/api/v1/catalog/clientes`

**Acción necesaria**:
- Crear panel con:
  - Lista de clientes (ListView/DataGrid)
  - Botones: Nuevo, Editar, Eliminar
  - Buscador
  - Paginación (opcional)

**Archivos a crear/modificar**:
- `Views/SettingsWindow.xaml.cs` → `CreateClientsContent()`
- Reutilizar `ClientesService` existente

---

#### 3. **Grupos y Tipos (CRUD)**
**Servicios existentes**: 
- `App.GruposService`
- `App.TiposService`

**Endpoint backend**: 
- `/api/v1/catalog/grupos`
- `/api/v1/catalog/tipos`

**Acción necesaria**:
- Crear panel con 2 tabs:
  - Tab "Grupos": CRUD de grupos
  - Tab "Tipos": CRUD de tipos

**Archivos a crear/modificar**:
- `Views/SettingsWindow.xaml.cs` → `CreateCatalogContent()`
- Reutilizar `GruposService` y `TiposService`

---

#### 4. **Integraciones (API Config)**
**Archivo a editar**: `appsettings.json`

**Acción necesaria**:
- Crear panel con:
  - TextBox: Base URL del API
  - TextBox: Timeout (segundos)
  - Button: Test conexión (ping /health)
  - Button: Guardar cambios

**Lógica**:
```csharp
// Leer appsettings.json
var config = App.ConfiguracionService.Instance;

// Modificar valores
config.ApiBaseUrl = txtBaseUrl.Text;
config.ApiTimeout = int.Parse(txtTimeout.Text);

// Guardar en appsettings.json (usar System.IO)
File.WriteAllText("appsettings.json", jsonString);
```

**Archivos a crear/modificar**:
- `Views/SettingsWindow.xaml.cs` → `CreateIntegrationsContent()`
- `Services/ConfiguracionService.cs` (añadir método Save)

---

#### 5. **Import/Export (Configuración)**
**Servicio existente**: `ExcelPartesImportService`

**Acción necesaria**:
- Crear panel con:
  - TextBox: Ruta por defecto de exportación
  - ComboBox: Formato (Excel 2007+, Excel 97-2003, CSV)
  - ListView: Historial de últimas importaciones/exportaciones
  - Buttons: Limpiar historial, Cambiar ruta

**Archivos a crear/modificar**:
- `Views/SettingsWindow.xaml.cs` → `CreateImportExportContent()`
- `Services/Import/ExcelPartesImportService.cs` (añadir historial)

---

### 🟢 PRIORIDAD BAJA

#### 6. **Parámetros Globales**
**Definir qué parámetros son necesarios**:
- ¿Auto-refresh de datos?
- ¿Tiempo de inactividad para logout?
- ¿Mostrar/ocultar tooltips?
- ¿Activar/desactivar notificaciones?

**Acción necesaria**:
- Definir lista de parámetros
- Crear modelo `AppParameter`
- Guardar en `appsettings.json` o base de datos

**Archivos a crear/modificar**:
- `Views/SettingsWindow.xaml.cs` → `CreateParametersContent()`
- `Models/AppParameter.cs` (nuevo)
- `Services/ConfiguracionService.cs` (añadir gestión de parámetros)

---

## 📝 NOTAS TÉCNICAS

### Sistema de Roles actual
```csharp
// SettingsViewModel.cs línea ~58
_permissionService.SetCurrentUserRole(UserRole.ADMIN); // TEMPORAL
```

**TODO**: Integrar con backend cuando devuelva `Role` en `/api/v1/users/me`

### Servicios disponibles
- ✅ `App.ClientesService`
- ✅ `App.GruposService`
- ✅ `App.TiposService`
- ✅ `Services.Admin.AdminUsersService`
- ✅ `Services.Import.ExcelPartesImportService`
- ✅ `Services.ConfiguracionService`

---

## 🔗 Referencias

- **SettingsWindow**: `Views/SettingsWindow.xaml.cs`
- **SettingsViewModel**: `ViewModels/SettingsViewModel.cs`
- **PermissionService**: `Services/PermissionService.cs`
- **Enum Roles**: `Models/Enums/UserRole.cs`

---

## ✅ CHECKLIST FINAL

- [ ] Implementar "Editar Perfil"
- [ ] Implementar CRUD Clientes
- [ ] Implementar CRUD Grupos y Tipos
- [ ] Implementar configuración de API (Integraciones)
- [ ] Implementar configuración Import/Export
- [ ] Definir e implementar Parámetros Globales
- [ ] Integrar rol real desde backend
- [ ] Testing completo de todas las secciones
- [ ] Documentar API de Settings para futuras extensiones

---

**Última actualización**: 2025-02-01 18:15
