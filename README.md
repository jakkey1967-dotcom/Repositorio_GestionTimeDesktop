# ?? GestionTime Desktop

<div align="center">

**Aplicación de escritorio profesional para gestión de tiempo y partes de trabajo**

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=.net)](https://dotnet.microsoft.com/)
[![WinUI 3](https://img.shields.io/badge/WinUI-3.0-0078D4?style=for-the-badge&logo=windows)](https://docs.microsoft.com/en-us/windows/apps/winui/)
[![Windows](https://img.shields.io/badge/Windows-10%2F11-0078D6?style=for-the-badge&logo=windows)](https://www.microsoft.com/windows/)
[![License](https://img.shields.io/badge/License-Private-red?style=for-the-badge)]()

</div>

---

## ?? Descripción

**GestionTime Desktop** es una aplicación moderna de escritorio desarrollada en .NET 8 y WinUI 3 para la gestión eficiente de partes de trabajo y control de tiempo. Diseñada para empresas que necesitan un seguimiento detallado de las actividades laborales con una interfaz intuitiva y moderna.

### ? Características Principales

- ?? **Dashboard Intuitivo** - Vista completa de partes y estadísticas
- ?? **Autenticación Segura** - Login con JWT y gestión de sesiones
- ?? **CRUD Completo** - Crear, editar, pausar y cerrar partes
- ?? **Gráficas Interactivas** - Visualización de datos con charts personalizados
- ?? **Temas Adaptativos** - Soporte para modo claro y oscuro
- ?? **Sincronización API** - Integración robusta con backend en la nube
- ?? **UI Moderna** - Interfaz WinUI 3 con animaciones fluidas
- ??? **Manejo de Errores** - Sistema robusto de recuperación y logs

---

## ?? Instalación

### Opción 1: Instalador MSIX (Recomendado)

1. **Descargar** el instalador desde [Releases](../../releases)
2. **Ejecutar** `GestionTimeDesktop_install.msix`
3. **Aceptar** el certificado si aparece
4. **¡Listo!** Buscar "GestionTime Desktop" en el menú inicio

### Opción 2: Versión Portable

1. **Descargar** el ZIP portable desde [Releases](../../releases)
2. **Extraer** en una carpeta de tu elección
3. **Ejecutar** `INSTALAR_Y_EJECUTAR.bat`
4. **Seguir** las instrucciones en pantalla

### Requisitos del Sistema

- **OS:** Windows 10 (versión 1809+) o Windows 11
- **Framework:** Windows App Runtime 1.8+ (incluido en instalador)
- **RAM:** Mínimo 4GB
- **Espacio:** 100MB libres
- **Red:** Conexión a internet para sincronización

---

## ??? Desarrollo

### Tecnologías Utilizadas

| Tecnología | Versión | Propósito |
|-----------|---------|-----------|
| **.NET** | 8.0 | Runtime principal |
| **WinUI 3** | 1.8 | Framework de UI moderna |
| **CommunityToolkit.Mvvm** | 8.4.0 | Patrón MVVM |
| **System.Text.Json** | 10.0.1 | Serialización JSON |
| **Microsoft.Extensions.Logging** | 10.0.1 | Sistema de logs |

### Estructura del Proyecto

```
?? GestionTime.Desktop/
??? ?? Views/                 # Páginas XAML
?   ??? DiarioPage.xaml      # Dashboard principal
?   ??? LoginPage.xaml       # Autenticación
?   ??? GraficaDiaPage.xaml  # Gráficas
??? ?? ViewModels/            # Lógica MVVM
??? ?? Services/              # Servicios de negocio
?   ??? ApiClient.cs         # Cliente HTTP
?   ??? DiarioService.cs     # Gestión de partes
??? ?? Models/                # Modelos de datos
?   ??? Dtos/                # Data Transfer Objects
??? ?? Controls/              # Controles personalizados
??? ?? Assets/                # Recursos gráficos
??? ?? Helpers/               # Documentación técnica
```

---

## ?? Uso

### 1. Inicio de Sesión
- Ingresar credenciales proporcionadas por el administrador
- El sistema recordará la sesión para futuros accesos

### 2. Gestión de Partes
- **Crear:** Ctrl+N o botón "Nuevo"
- **Editar:** Doble-click en un parte o Ctrl+E
- **Pausar/Reanudar:** Menú contextual (click derecho)
- **Cerrar:** Especificar hora de finalización

### 3. Filtros y Búsqueda
- **Filtro de fecha:** Calendario en la parte superior
- **Búsqueda:** Campo de texto para filtrar por cliente, acción, etc.
- **Actualización:** F5 o botón refresh

### 4. Gráficas y Reportes
- **F8:** Abrir ventana de gráficas del día
- **Análisis visual** de distribución de tiempo
- **Estadísticas detalladas** por cliente/tipo

---

## ?? Configuración

### Archivo de Configuración (`appsettings.json`)

```json
{
  "Api": {
    "BaseUrl": "https://gestiontimeapi.onrender.com",
    "LoginPath": "/api/v1/auth/login",
    "PartesPath": "/api/v1/partes"
  },
  "Logging": {
    "LogPath": "C:\\Logs\\GestionTime\\app.log"
  }
}
```

---

## ?? Métricas del Proyecto

### Estadísticas de Desarrollo

| Métrica | Valor |
|---------|-------|
| **Líneas de Código** | ~3,500 |
| **Archivos C#** | 35+ |
| **Páginas XAML** | 8 |
| **Tiempo de Desarrollo** | 6-8 semanas |
| **Documentación** | 25+ archivos |

### Performance

| Aspecto | Métrica |
|---------|---------|
| **Tiempo de Inicio** | < 3 segundos |
| **Uso de RAM** | ~50-80 MB |
| **Respuesta UI** | < 100ms |
| **Sincronización API** | < 2 segundos |

---

## ?? Troubleshooting

### Problemas Comunes

#### 1. Error "Windows App Runtime no encontrado"
```bash
# Solución automática
.\INSTALAR_WINDOWS_APP_RUNTIME.bat

# Solución manual
winget install Microsoft.WindowsAppRuntime.1.8
```

#### 2. Error de conexión API
- Verificar conexión a internet
- Comprobar URL del servidor en `appsettings.json`
- Revisar logs en `C:\Logs\GestionTime\app.log`

#### 3. Problemas de certificado MSIX
- Instalar certificado manualmente desde la carpeta del instalador
- Ejecutar como administrador si es necesario

### Logs y Diagnóstico

Los logs se almacenan en:
- **Windows 10/11:** `C:\Logs\GestionTime\app.log`
- **Portable:** `Logs\app.log` (directorio de la app)

---

## ?? Seguridad

- ? **Autenticación JWT** con tokens seguros
- ? **HTTPS** para todas las comunicaciones
- ? **Validación de entrada** en formularios
- ? **Manejo seguro** de credenciales
- ? **Logs auditables** de acciones del usuario

---

## ?? Contribución

### Para Desarrolladores

1. **Clonar** el repositorio
```bash
git clone https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop.git
```

2. **Abrir** en Visual Studio 2022
3. **Restaurar** paquetes NuGet
4. **Configurar** `appsettings.json` con tu API
5. **Compilar** y ejecutar

### Estándares de Código

- **C# 12** con nullable reference types
- **Async/await** para operaciones asíncronas
- **MVVM pattern** estricto
- **Clean architecture** por capas
- **Unit tests** para lógica crítica

---

## ?? Changelog

### v1.1.0 (Actual)
- ? Interfaz WinUI 3 completa
- ? CRUD de partes con estados
- ? Gráficas interactivas
- ? Themes claro/oscuro
- ? Instalador MSIX con dependencias
- ? Sistema de backup automático
- ?? Corrección error 405 al cerrar partes
- ?? Manejo robusto de respuestas null
- ?? Documentación técnica completa

### Próximas Versiones
- ?? **v1.2.0:** Reportes PDF exportables
- ?? **v1.3.0:** Sincronización offline
- ?? **v2.0.0:** Multi-tenant y roles

---

## ?? Soporte

### Documentación Técnica

Para información detallada, consulta:
- ?? [`Helpers/RESUMEN_EJECUTIVO_FINAL.md`](Helpers/RESUMEN_EJECUTIVO_FINAL.md)
- ?? [`Helpers/GUIA_INSTALACION.md`](Helpers/GUIA_INSTALACION.md)
- ?? [`Helpers/SOLUCION_ERROR_CONEXION.md`](Helpers/SOLUCION_ERROR_CONEXION.md)

### Contacto

- **Issues:** [Crear issue](../../issues) en este repositorio
- **Email:** soporte@empresa.com
- **Documentación:** Ver archivos en `Helpers/`

---

## ?? Licencia

Este proyecto es **privado** y propietario. Todos los derechos reservados.

**© 2025 GestionTime Solutions. Uso interno únicamente.**

---

<div align="center">

**Desarrollado con ?? usando .NET 8 y WinUI 3**

![Footer](https://img.shields.io/badge/Made%20with-?%20in%20Spain-red?style=for-the-badge)

</div>