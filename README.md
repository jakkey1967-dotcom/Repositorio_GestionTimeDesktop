# 📋 GestionTime Desktop

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![WinUI 3](https://img.shields.io/badge/WinUI-3.0-0078D4?logo=windows)](https://microsoft.github.io/microsoft-ui-xaml/)
[![Windows 11](https://img.shields.io/badge/Windows-11-0078D6?logo=windows11)](https://www.microsoft.com/windows/windows-11)
[![Version](https://img.shields.io/badge/version-v2.0.5--beta-orange)](https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases/tag/v2.0.5-beta)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

**Aplicación de escritorio para la gestión de partes de trabajo** en empresas de soporte técnico y mantenimiento. Construida con .NET 8 y WinUI 3, ofrece una interfaz moderna y rendimiento nativo en Windows 11.

---

## ✨ Características Principales

### 📊 Gestión de Partes de Trabajo
- ✅ **Crear, editar y eliminar** partes de trabajo
- ⏱️ **Control de tiempo** (hora inicio/fin con cálculo automático de duración)
- 🔄 **Estados dinámicos**: Abierto, Pausado, Cerrado, Enviado, Anulado
- 🎯 **Asociación completa**: Cliente, tienda, grupo, tipo, ticket

### 📥 Importación Masiva
- 📊 **Importar desde Excel** (.xls/.xlsx)
- ✅ **Validación automática** de datos
- 👀 **Preview antes de importar**
- 🔍 **Detección de errores** con mensajes claros

### 🔍 Búsqueda y Filtros
- 📅 **Filtrado por fecha** (día específico o rango)
- 🔎 **Búsqueda instantánea** con debounce (350ms)
- 🎯 **Búsqueda en múltiples campos**: cliente, ticket, acción, grupo, tipo, estado
- 📊 **Carga inteligente**: Últimos 25 partes por defecto

### 👤 Perfil de Usuario
- 🎨 **Banner dinámico** con información del usuario
- 📝 **Edición de perfil** personal
- 📞 **Información de contacto**: nombre, email, teléfono

### 🎨 Interfaz Moderna
- 🌓 **Temas**: Claro, Oscuro y Automático (según sistema)
- 🎯 **Notificaciones in-app** con 4 tipos (Success, Error, Warning, Info)
- ⚡ **Animaciones fluidas** y transiciones suaves
- 📱 **Responsive design** con virtualización de listas

### 🚀 Rendimiento
- 💾 **Sistema de caché** inteligente (30 minutos)
- 🔄 **Retry automático** con estrategia exponencial (3 intentos)
- ⏱️ **Timeout configurable** (120 segundos)
- 🔀 **Peticiones concurrentes** con semáforos (máx 6 simultáneas)

---

## 🏗️ Arquitectura

### Stack Tecnológico

```
┌─────────────────────────────────────────┐
│         Presentación (WinUI 3)          │
│  - XAML Views                           │
│  - ViewModels (MVVM)                    │
│  - Converters & Helpers                 │
└─────────────────────────────────────────┘
                  ↓
┌─────────────────────────────────────────┐
│       Lógica de Negocio (.NET 8)        │
│  - Services (API, Profile, Catalog)     │
│  - Models (DTOs)                        │
│  - Validation & Mapping                 │
└─────────────────────────────────────────┘
                  ↓
┌─────────────────────────────────────────┐
│        Datos (REST API + Cache)         │
│  - ApiClient (HttpClient)               │
│  - Cache Manager                        │
│  - File Storage                         │
└─────────────────────────────────────────┘
```

### Estructura del Proyecto

```
GestionTime.Desktop/
├── 📁 Views/                    # Páginas XAML
│   ├── DiarioPage.xaml         # Lista de partes (página principal)
│   ├── ParteItemEdit.xaml      # Editor de parte
│   ├── LoginPage.xaml          # Inicio de sesión
│   ├── RegisterPage.xaml       # Registro de usuario
│   └── UserProfilePage.xaml    # Perfil de usuario
│
├── 📁 ViewModels/               # ViewModels MVVM
│   ├── DiarioViewModel.cs
│   └── UserProfileViewModel.cs
│
├── 📁 Services/                 # Servicios
│   ├── ApiClient.cs            # Cliente HTTP con caché y retry
│   ├── ProfileService.cs       # Gestión de perfiles
│   ├── CatalogManager.cs       # Caché de catálogos (clientes, grupos, tipos)
│   ├── ThemeService.cs         # Gestión de temas
│   ├── WindowSizeManager.cs    # Gestión de tamaños de ventanas
│   └── Notifications/          # Sistema de notificaciones
│       ├── NotificationService.cs
│       └── NotificationThrottler.cs
│
├── 📁 Models/                   # Modelos y DTOs
│   └── Dtos/
│       ├── ParteDto.cs         # DTO de parte
│       ├── ParteCreateRequest.cs
│       ├── ProfileResponses.cs
│       └── CatalogResponses.cs
│
├── 📁 Helpers/                  # Utilidades
│   ├── Converters.cs           # Converters XAML
│   ├── DiarioPageHelpers.cs    # Helpers de DiarioPage
│   ├── IntervalMerger.cs       # Cálculo de cobertura de tiempo
│   └── UserInfoFileStorage.cs  # Almacenamiento local
│
├── 📁 Dialogs/                  # Diálogos personalizados
│   ├── CerrarParteDialog.xaml  # Diálogo de cierre de parte
│   └── ImportExcelDialog.xaml  # Importación de Excel
│
├── 📁 Controls/                 # Controles personalizados
│   └── NotificationHost.xaml   # Host de notificaciones
│
├── 📁 Assets/                   # Recursos
│   ├── LogoClaro.png
│   ├── LogoOscuro.png
│   └── diario_bg_*.png
│
├── 📁 Docs/                     # Documentación
│   ├── MANUAL_USUARIO_GESTIONTIME_DESKTOP.md
│   ├── DIAGNOSTICO_CIERRE_TICKETS.md
│   └── SISTEMA_NOTIFICACIONES_IN_APP_COMPLETO.md
│
├── 📁 Installer/                # Instaladores
    ├── MSI/                    # Instalador WiX
    └── MSIX/                   # Paquete MSIX
```

---

## 🚀 Instalación

### Requisitos Previos

- **Sistema Operativo**: Windows 11 (versión 22H2 o superior)
- **Runtime**: .NET 8 Runtime Desktop
- **Memoria**: 4 GB RAM (mínimo), 8 GB recomendado
- **Disco**: 200 MB de espacio libre

### Opción 1: Instalador MSI (Recomendado)

1. Descargar `GestionTime-Setup-v1.0.0.msi` desde [Releases](https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases)
2. Ejecutar el instalador
3. Seguir las instrucciones del asistente
4. Iniciar la aplicación desde el menú de inicio

### Opción 2: Paquete MSIX

1. Descargar `GestionTime_1.0.0.0_x64.msixbundle`
2. Instalar el certificado de firma (si es la primera vez)
3. Hacer doble clic en el archivo `.msixbundle`
4. Click en "Instalar"

### Opción 3: Compilación desde Código Fuente

```bash
# Clonar el repositorio
git clone https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop.git
cd Repositorio_GestionTimeDesktop

# Restaurar dependencias
dotnet restore

# Compilar
dotnet build --configuration Release

# Ejecutar
dotnet run --project GestionTime.Desktop.csproj
```

---

## 🔧 Configuración

### 1. Backend API

Editar `appsettings.json`:

```json
{
  "ApiSettings": {
    "BaseUrl": "https://tu-api.com",
    "Timeout": 120,
    "CacheDuration": 1800,
    "MaxRetries": 3
  }
}
```

### 2. Notificaciones

```json
{
  "NotificationSettings": {
    "DisplayDuration": 5,
    "MaxNotifications": 3,
    "ThrottleInterval": 500
  }
}
```

### 3. Ventanas

```json
{
  "WindowSettings": {
    "LoginSize": { "Width": 1144, "Height": 783 },
    "MainSize": { "Width": 1280, "Height": 900 },
    "EditSize": { "Width": 1000, "Height": 900 }
  }
}
```

---

## 📖 Uso

### Inicio de Sesión

1. Abrir la aplicación
2. Ingresar **email** y **contraseña**
3. (Opcional) Marcar "Recordar sesión"
4. Click en **"Iniciar sesión"**

### Crear Nuevo Parte

**Método 1: Botón**
- Click en **"📝 Nuevo"** (Ctrl+N)
- Rellenar formulario
- Click en **"💾 Guardar"**

**Método 2: Llamada Telefónica**
- Click en **"📞 Teléfono"** (Ctrl+T)
- Se crea automáticamente con ticket "TELEFONO"

### Cerrar un Parte

1. **Click derecho** in the status badge of the part (must be "In Progress" 🟢 or "Paused" 🟡)
2. Select **"Close"**
3. Enter **closing time** (format HH:mm)
4. Confirm

**Note**: Only parts with **Open** or **Paused** status can be closed.

### Importar desde Excel

1. Click en **"📊 Importar"** (Ctrl+I)
2. Seleccionar archivo `.xls` o `.xlsx`
3. Revisar preview de datos
4. Corregir errores si los hay
5. Click en **"Importar"**

**Formato Excel requerido:**

| Columna        | Tipo     | Requerido | Ejemplo          |
|----------------|----------|-----------|------------------|
| fecha_trabajo  | Fecha    | ✅        | 2026-01-12       |
| hora_inicio    | Texto    | ✅        | 09:00            |
| hora_fin       | Texto    | ❌        | 14:30            |
| cliente        | Texto    | ✅        | MERCADONA        |
| tienda         | Texto    | ❌        | Tienda 001       |
| grupo          | Texto    | ❌        | Mantenimiento    |
| tipo           | Texto    | ❌        | Correctivo       |
| accion         | Texto    | ✅        | Reparación de... |
| ticket         | Texto    | ❌        | TICKET-12345     |

---

## ⌨️ Atajos de Teclado

| Atajo           | Acción                      |
|-----------------|-----------------------------|
| `Ctrl + N`      | Nuevo parte                 |
| `Ctrl + T`      | Nueva llamada telefónica    |
| `Ctrl + E`      | Editar parte seleccionado   |
| `Ctrl + I`      | Importar desde Excel        |
| `Delete`        | Borrar parte seleccionado   |
| `F5`            | Refrescar lista             |
| `Ctrl + Q`      | Cerrar sesión               |

---

## 🐛 Solución de Problemas

### No puedo cerrar un parte

**Causa**: El parte ya está cerrado o tiene un estado no válido.

**Solución**:
1. Verificar el badge de estado (debe ser 🟢 verde "En Curso" o 🟡 amarillo "Pausado")
2. Si está cerrado, usar **"Duplicar"** para crear un nuevo parte
3. Presionar **F5** para refrescar la lista

Ver: [`Docs/DIAGNOSTICO_CIERRE_TICKETS.md`](Docs/DIAGNOSTICO_CIERRE_TICKETS.md)

### Notificaciones no aparecen

**Solución**:
1. Verificar que `NotificationHost` está en el XAML de la página
2. Reiniciar la aplicación
3. Revisar logs en: `bin\x64\Debug\net8.0-windows10.0.19041.0\logs`

Ver: [`Docs/FIX_NOTIFICACIONES_NO_VISIBLES_SOLUCION_FINAL.md`](Docs/FIX_NOTIFICACIONES_NO_VISIBLES_SOLUCION_FINAL.md)

### Errores de timeout

**Solución**:
- El sistema tiene **retry automático** (3 intentos)
- Timeout: **120 segundos**
- Si persiste, verificar conexión a internet
- Verificar URL del backend en `appsettings.json`

### Importación de Excel falla

**Causas comunes**:
- Cliente no existe en base de datos
- Formato de hora incorrecto (usar HH:mm)
- Fecha inválida

**Solución**:
1. Revisar preview de errores en el diálogo
2. Corregir errores en Excel
3. Volver a importar

Ver: [`Docs/FIX_IMPORTACION_EXCEL_CLIENTE_DURACION.md`](Docs/FIX_IMPORTACION_EXCEL_CLIENTE_DURACION.md)

---

## 🗂️ Changelog

### v2.0.5-beta (2025)

#### 🐛 Fixes importación Excel
- **Estado correcto**: los partes importados se crean con estado `cerrado` en lugar de `en curso`.
- **Filas vacías ignoradas**: celdas combinadas del Excel (filas sin fecha/proyecto/acción) ya no se procesan como filas inválidas.
- **Grupo/Tipo por nombre flexible**: resolución con fallback `StartsWith` bidireccional (ej. `Tiendas` ↔ `Tienda`).

#### ✨ Nueva funcionalidad
- **Botón "Descartar validación"**: tras validar un Excel, si todos los datos son correctos, el admin puede descartar el batch sin importarlo (limpia el staging sin tocar `partesdetrabajo`).

#### 🔧 API (Render)
- Nuevo endpoint `DELETE /api/v2/admin/import/batches/{id}` para eliminar batches `DRAFT` o `VALIDATED`.

---

## 📊 Logs

Los logs se guardan en:

```
bin\x64\Debug\net8.0-windows10.0.19041.0\logs\
```

**Archivos generados**:
- `gestiontime_YYYYMMDD.log` - Log general de la aplicación
- `gestiontime_data_YYYYMMDD.log` - Log de peticiones de datos
- `gestiontime_api_YYYYMMDD.log` - Log de peticiones HTTP

**Niveles de log**:
- `[Debug]` - Información de depuración
- `[Information]` - Información general
- `[Warning]` - Advertencias
- `[Error]` - Errores

---

## 🤝 Contribuir

¡Las contribuciones son bienvenidas! Por favor:

1. **Fork** el repositorio
2. Crear una **rama** para tu feature (`git checkout -b feature/nueva-funcionalidad`)
3. **Commit** tus cambios (`git commit -m '✨ Agregar nueva funcionalidad'`)
4. **Push** a la rama (`git push origin feature/nueva-funcionalidad`)
5. Abrir un **Pull Request**

### Convenciones de Commits

Usamos [Conventional Commits](https://www.conventionalcommits.org/):

```
✨ feat: Nueva característica
🐛 fix: Corrección de bug
📝 docs: Documentación
🎨 style: Formato, estilo (sin cambios de código)
♻️ refactor: Refactorización de código
⚡ perf: Mejora de rendimiento
✅ test: Agregar o actualizar tests
🔧 chore: Tareas de mantenimiento
```

---

## 📜 Licencia

Este proyecto está licenciado bajo la **MIT License** - ver el archivo [LICENSE](LICENSE) para más detalles.

---

## 👥 Autores

- **Francisco Santos García** - *Desarrollo Principal* - [@jakkey1967-dotcom](https://github.com/jakkey1967-dotcom)

---

## 🙏 Agradecimientos

- Equipo de **Microsoft** por WinUI 3
- Comunidad de **.NET**
- **GitHub Copilot** por asistencia en desarrollo

---

## 📞 Soporte

¿Necesitas ayuda? Abre un [Issue](https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/issues) en GitHub.

---

## 🗺️ Roadmap

### Versión 1.1.0 (Q1 2026)
- [ ] Exportación a PDF de partes
- [ ] Filtros avanzados (múltiples criterios)
- [ ] Gráficos de estadísticas
- [ ] Modo offline con sincronización

### Versión 1.2.0 (Q2 2026)
- [ ] Integración con calendario
- [ ] Recordatorios de partes pendientes
- [ ] Firma digital de partes
- [ ] Multi-idioma (inglés, francés)

### Versión 2.0.0 (Q3 2026)
- [ ] Aplicación móvil (iOS/Android)
- [ ] Modo colaborativo (múltiples usuarios)
- [ ] Chat interno
- [ ] Dashboard ejecutivo

---

## 📈 Estadísticas

![GitHub release (latest by date)](https://img.shields.io/github/v/release/jakkey1967-dotcom/Repositorio_GestionTimeDesktop)
![GitHub all releases](https://img.shields.io/github/downloads/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/total)
![GitHub repo size](https://img.shields.io/github/repo-size/jakkey1967-dotcom/Repositorio_GestionTimeDesktop)

---

<div align="center">

**⭐ Si te gusta este proyecto, dale una estrella en GitHub ⭐**

Made with ❤️ using .NET 8 and WinUI 3

[⬆ Volver arriba](#-gestiontime-desktop)

</div>
