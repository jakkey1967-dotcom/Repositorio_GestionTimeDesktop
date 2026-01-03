# Changelog

Todos los cambios notables en este proyecto serán documentados en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/),
y este proyecto adhiere a [Semantic Versioning](https://semver.org/lang/es/).

## [1.0.0] - 2025-01-27

### 🎉 Release Inicial

#### Añadido
- 🔐 Sistema de autenticación con JWT y refresh tokens
- 📝 CRUD completo de partes de trabajo (crear, editar, pausar, cerrar, duplicar)
- 📊 Visualización gráfica de cobertura horaria diaria
- 🎨 Soporte para temas Light/Dark con cambio en tiempo real
- ⚡ Sistema de caché inteligente de 30 días
- 🔄 Sincronización automática con backend
- 📱 Interfaz moderna y adaptativa con WinUI 3
- 🔍 Filtros avanzados por fecha y texto
- 📈 Cálculo de cobertura horaria con detección de solapamientos
- 🎯 Control de roles (Admin, Responsable, Operario)
- 📝 Sistema de logging completo
- 🚀 Instalador MSI profesional con WiX Toolset v4
- 📦 Versión portable (ZIP) para instalación sin permisos admin
- ⌨️ Atajos de teclado para operaciones frecuentes (F2-F9)
- 🎨 Iconos personalizados en ejecutable y accesos directos

#### Técnico
- .NET 8.0 con C# 12
- WinUI 3 (Windows App SDK 1.8)
- Arquitectura MVVM con CommunityToolkit
- HttpClient con políticas de retry
- Serialización JSON con System.Text.Json
- Logging con Microsoft.Extensions.Logging
- Gestión de errores centralizada

### 🔧 Configuración
- Archivo `appsettings.json` para configuración externa
- URL del backend: https://gestiontimeapi.onrender.com
- Logs configurables con ruta personalizable

### 📚 Documentación
- README completo con instalación y uso
- Guías técnicas en carpeta Helpers/
- Scripts PowerShell para build automatizado
- Documentación de troubleshooting

---

## [Unreleased] - Próximas Versiones

### 🔜 Planificado para v1.1.0
- [ ] Exportación de reportes a PDF
- [ ] Configuración de perfiles de usuario
- [ ] Notificaciones push desde backend
- [ ] Sincronización en segundo plano

### 🔜 Planificado para v1.2.0
- [ ] Modo offline con sincronización diferida
- [ ] Estadísticas avanzadas por período
- [ ] Exportación a Excel/CSV
- [ ] Importación masiva de partes

### 🔜 Planificado para v2.0.0
- [ ] Multi-tenant support
- [ ] Sistema de roles granular
- [ ] Plantillas de partes
- [ ] Integración con calendario

---

## Tipos de Cambios

- `Añadido` - Para nuevas características
- `Cambiado` - Para cambios en funcionalidad existente
- `Deprecado` - Para características que serán removidas
- `Eliminado` - Para características removidas
- `Corregido` - Para corrección de bugs
- `Seguridad` - Para actualizaciones de seguridad

---

## Enlaces

- **Repositorio**: https://github.com/jakkey1967-dotcom/GestionTime.Desktop
- **Backend API**: https://github.com/jakkey1967-dotcom/GestionTimeApi
- **Documentación**: Ver carpeta `Helpers/`
