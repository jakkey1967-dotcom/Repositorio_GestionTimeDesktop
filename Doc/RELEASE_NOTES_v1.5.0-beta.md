# 🚀 GestionTime Desktop v1.5.0-beta

## 📋 Notas de la Versión

Esta versión incluye un **sistema de versión centralizada** que elimina inconsistencias y simplifica el proceso de actualización, además de mejoras en el formato de visualización de duración de partes.

---

## ✨ Nuevas Funcionalidades

### 🎯 Sistema de Versión Centralizada

- **Fuente única de verdad** en `Directory.Build.props`
  - Un solo lugar para cambiar la versión de toda la aplicación
  - Se propaga automáticamente a todos los ensamblados
  - Elimina inconsistencias entre LoginPage, MSI y sistema de actualizaciones

- **Clase `VersionInfo`** para acceso desde código
  - `VersionInfo.Version` → `"1.5.0-beta"`
  - `VersionInfo.VersionWithPrefix` → `"v1.5.0-beta"`
  - `VersionInfo.VersionNumeric` → `"1.5.0.0"`

- **Propagación automática**
  - `GestionTime.Desktop.csproj` usa `$(AppVersion)`
  - `LoginPage.xaml.cs` usa `VersionInfo.VersionWithPrefix`
  - `UpdateService.cs` usa `VersionInfo.Version`
  - `DiarioPage.xaml.cs` (notas de versión) usa `VersionInfo.Version`

- **Menos errores al cambiar de versión**
  - Antes: Actualizar 5+ archivos manualmente
  - Ahora: Actualizar 1 archivo (`Directory.Build.props`)

### 🔧 Mejoras

#### Formato de Duración Mejorado

- **Formato HH:mm estándar**: La columna de duración ahora muestra `01:45` en lugar de `105 min`
- **Más legible**: Formato estándar de horas y minutos (ej: `00:30`, `02:15`)
- **Consistente**: Alineado con formatos de hora inicio/fin

---

## 📋 Documentación

### Nueva Documentación

- **`Docs/SISTEMA_VERSION_CENTRALIZADA.md`**
  - Guía completa del sistema de versión centralizada
  - Proceso de release paso a paso
  - Checklist para cambiar versión
  - Troubleshooting común

### Documentación Actualizada

- **`CHANGELOG.md`**: Nueva sección [1.5.0-beta] con todos los cambios
- **Sistema de notas de versión**: Actualizado para usar `VersionInfo`

---

## 🔄 Actualización desde v1.4.1-beta

### Detección Automática

Si tienes instalada la **v1.4.1-beta**, la aplicación:

1. ✅ Detectará automáticamente la nueva versión disponible
2. ✅ Mostrará notificación de actualización
3. ✅ Permitirá descargar e instalar v1.5.0-beta

### Instalación Manual

Si prefieres instalar manualmente:

1. Descargar `GestionTime-1.5.0-beta.msi`
2. Ejecutar el MSI (doble clic)
3. El instalador detectará y actualizará la versión anterior automáticamente
4. ✅ Actualización completa sin pérdida de datos

---

## 📦 Instalación

### Requisitos

- Windows 10 version 1809 (build 17763) o superior
- Windows 11 (recomendado)
- .NET 8.0 Runtime (incluido en el instalador)
- ~280 MB de espacio en disco

### Instalador MSI

1. Descargar `GestionTime-1.5.0-beta.msi`
2. Ejecutar el instalador (doble clic)
3. Seguir el asistente de instalación
4. La aplicación se instalará en `C:\App\GestionTime-Desktop`

---

## 🔧 Cambios Técnicos

### Arquitectura de Versión

**Antes (v1.4.1):**
```
LoginPage.xaml.cs (35 líneas) → "1.4.1-beta"
UpdateService.cs (45 líneas) → "1.4.1-beta"
GestionTime.Desktop.csproj → "1.4.1.0"
DiarioPage.xaml.cs → "1.4.1-beta"
```

**Ahora (v1.5.0):**
```
Directory.Build.props → AppVersionMinor = 5 ← FUENTE ÚNICA
  ↓ (propagación automática)
  ├─ GestionTime.Desktop.csproj → $(AppVersion)
  ├─ LoginPage.xaml.cs → VersionInfo.VersionWithPrefix
  ├─ UpdateService.cs → VersionInfo.Version
  └─ DiarioPage.xaml.cs → VersionInfo.Version
```

### Reducción de Código

- **LoginPage.xaml.cs**: De 35 líneas a 7 líneas (80% menos)
- **UpdateService.cs**: De 45 líneas a 12 líneas (73% menos)
- **Código duplicado eliminado**: 100%

---

## 🐛 Problemas Conocidos

- Ninguno reportado en esta versión

---

## 🔗 Enlaces Útiles

- **Repositorio**: https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop
- **Issues**: https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/issues
- **Wiki**: https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/wiki
- **Changelog Completo**: [CHANGELOG.md](https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/blob/main/CHANGELOG.md)
- **Documentación del Sistema de Versión**: [SISTEMA_VERSION_CENTRALIZADA.md](https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/blob/main/Docs/SISTEMA_VERSION_CENTRALIZADA.md)

---

## 📊 Comparación de Versiones

| Característica | v1.4.1-beta | v1.5.0-beta |
|----------------|-------------|-------------|
| Sistema de versión | Manual (5+ archivos) | ✅ Centralizado (1 archivo) |
| Formato duración | `105 min` | ✅ `01:45` (HH:mm) |
| Código duplicado | Sí | ✅ No |
| Documentación | Básica | ✅ Completa |
| Mantenibilidad | Media | ✅ Alta |

---

## 👥 Contribuciones

Gracias a todos los que han contribuido a esta versión.

---

**¡Disfruta de GestionTime Desktop v1.5.0-beta!** 🎉
