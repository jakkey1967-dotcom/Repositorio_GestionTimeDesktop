# Changelog

Todos los cambios notables en este proyecto serán documentados en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/),
y este proyecto adhiere a [Semantic Versioning](https://semver.org/lang/es/).

## [Unreleased]

### Planeado
- Exportación a PDF de partes
- Filtros avanzados (múltiples criterios simultáneos)
- Gráficos de estadísticas
- Modo offline con sincronización

---

## [1.0.0] - 2026-01-12

### ✨ Funcionalidades Principales

#### Gestión de Partes
- **Crear, editar y eliminar** partes de trabajo
- **Control de tiempo** con hora inicio/fin y duración automática
- **Estados dinámicos**: Abierto (🟢), Pausado (🟡), Cerrado (🔵), Enviado, Anulado
- **Asociación completa**: Cliente, tienda, grupo, tipo, ticket, acción

#### Importación Excel
- **Importar masivamente** desde archivos .xls/.xlsx
- **Validación automática** de datos antes de importar
- **Preview en tiempo real** con detección de errores
- **Corrección inline** de errores en el diálogo

#### Búsqueda y Filtros
- **Filtrado por fecha** (día específico o últimos 25 partes)
- **Búsqueda instantánea** en múltiples campos
- **Debounce inteligente** (350ms) para evitar sobrecarga
- **Ordenamiento** por fecha y hora descendente

#### Perfil de Usuario
- **Banner dinámico** con foto y datos del usuario
- **Edición de perfil** personal
- **Gestión de información** de contacto

#### Interfaz y Experiencia
- **Temas**: Claro, Oscuro y Automático
- **Notificaciones in-app** con 4 tipos (Success, Error, Warning, Info)
- **Animaciones fluidas** en botones y transiciones
- **Zebra rows** en listas para mejor legibilidad
- **Responsive design** con virtualización

#### Rendimiento
- **Sistema de caché** (30 minutos de validez)
- **Retry automático** (3 intentos con backoff exponencial)
- **Timeout configurable** (120 segundos)
- **Peticiones concurrentes** limitadas (6 simultáneas)

#### Seguridad
- **Autenticación JWT** con refresh token
- **Almacenamiento seguro** de credenciales
- **Sesión recordada** entre reinicios
- **Limpieza automática** al cerrar sesión

### 🔧 Mejoras Técnicas

#### Arquitectura
- Patrón **MVVM** para separación de responsabilidades
- **ApiClient robusto** con caché, retry y throttling
- **Servicios especializados** (Profile, Catalog, Theme, Notifications)
- **DTOs tipados** para comunicación con API

#### Logging
- Sistema de **logs estructurados** con Serilog
- **3 niveles de log**: General, Data, API
- **Rotación automática** diaria
- **Logs detallados** de errores con stack traces

#### Configuración
- **Archivo appsettings.json** para configuración
- **Window-config.ini** para guardar tamaños de ventanas
- **Configuración de timeout**, caché y retry
- **Gestión de temas** persistente

### 🐛 Correcciones

#### Cierre de Partes
- Corregido problema al cerrar partes ya cerrados
- Mensaje claro cuando se intenta cerrar un parte cerrado
- Sugerencia de usar "Duplicar" para re-trabajar partes cerrados

#### Notificaciones
- Corregido bug de notificaciones no visibles
- Implementado NotificationHost en todas las páginas
- Throttling para evitar spam de notificaciones

#### Importación Excel
- Validación de clientes existentes antes de importar
- Corrección de formato de duración (HH:mm)
- Mejor manejo de errores con mensajes claros

#### Performance
- Optimización de carga inicial (solo 25 partes)
- Invalidación correcta de caché al modificar datos
- Reducción de peticiones HTTP con caché inteligente

### 📝 Documentación

- **README.md** completo con instrucciones de instalación y uso
- **CONTRIBUTING.md** con guías de contribución
- **MANUAL_USUARIO** detallado
- **Docs/** con diagnósticos y soluciones
- **Issue templates** para bugs y features
- **Pull request template** con checklist

### 🏗️ Infraestructura

- **Instalador MSI** (WiX Toolset)
- **Paquete MSIX** para Microsoft Store
- **GitHub Actions** preparado para CI/CD (futuro)
- **.gitignore** optimizado para .NET y Visual Studio

### ⌨️ Atajos de Teclado

- `Ctrl+N` - Nuevo parte
- `Ctrl+T` - Nueva llamada telefónica
- `Ctrl+E` - Editar parte
- `Ctrl+I` - Importar Excel
- `Delete` - Borrar parte
- `F5` - Refrescar lista
- `Ctrl+Q` - Cerrar sesión

### 🎨 Assets

- Logo claro y oscuro adaptativos
- Backgrounds sutiles para tema claro/oscuro
- Iconos consistentes en toda la aplicación

---

## Convenciones de Versionado

Este proyecto usa [Semantic Versioning](https://semver.org/):

- **MAJOR** (1.x.x): Cambios incompatibles con versiones anteriores
- **MINOR** (x.1.x): Nuevas funcionalidades compatibles
- **PATCH** (x.x.1): Correcciones de bugs

### Tipos de Cambios

- `✨ Added` - Nuevas funcionalidades
- `🔧 Changed` - Cambios en funcionalidad existente
- `🗑️ Deprecated` - Funcionalidad que será removida
- `❌ Removed` - Funcionalidad removida
- `🐛 Fixed` - Correcciones de bugs
- `🔒 Security` - Parches de seguridad

---

[Unreleased]: https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases/tag/v1.0.0
