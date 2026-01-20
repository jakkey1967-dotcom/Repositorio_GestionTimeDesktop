# 🚀 GestionTime Desktop v1.4.1-beta

## 📋 Notas de la Versión

Esta versión incluye mejoras importantes en la importación de Excel, gestión de estados de partes, y un nuevo sistema de notas de versión integrado.

---

## ✨ Nuevas Funcionalidades

### 📊 Importación Excel Mejorada

- **Detección automática de duplicados**: Valida por fecha + hora inicio + cliente + acción
- **Actualización inteligente**: Los registros duplicados se actualizan (UPDATE) en lugar de crear duplicados
- **Soporte columna INCIDENCIA**: Ahora acepta `INCIDENCIA` como alias de `Ticket`
- **Validación opcional de Grupo y Tipo**: Si no existen en el catálogo, se guarda como `null` (sin error)
- **Estadísticas detalladas**: Muestra X nuevos, Y actualizados, Z errores
- **Confirmación al usuario**: Pregunta antes de importar si hay duplicados detectados
- **Log mejorado**: Muestra columnas detectadas con longitud exacta y alias

### ▶️ Gestión de Estados Mejorada - Reanudar Parte

Cuando se reanuda un parte pausado:

1. ✅ **Solicita confirmación de hora de cierre** del parte pausado
2. ✅ **Cierra el parte pausado** con la hora confirmada
3. ✅ **Crea nuevo parte duplicado** automáticamente
4. ✅ **Hora inicio del nuevo = Hora cierre del anterior** (continuidad perfecta)
5. ✅ **Mantiene todos los datos**: ticket, cliente, acción, grupo, tipo, técnico
6. ✅ **Abre el editor** para confirmar antes de guardar

**Beneficio**: Trazabilidad completa de cada sesión de trabajo con el mismo ticket.

### 📖 Sistema de Notas de Versión

- ✅ **Botón "Ayuda"** en toolbar principal de DiarioPage
- ✅ **Diálogo profesional** con scroll y diseño oscuro
- ✅ **Link directo a GitHub Releases** para ver historial completo
- ✅ **CHANGELOG.md** actualizado automáticamente

---

## 🔧 Mejoras Técnicas

### Importación de Excel

- **Normalización de texto**: Búsqueda robusta sin acentos y en mayúsculas
- **Cache de partes existentes**: Últimos 60 días para validación rápida
- **Búsqueda inteligente de clientes**: Por nombre exacto o parcial
- **Trim automático**: Evita errores por espacios en nombres de columnas

### Logging y Trazabilidad

- **Log detallado por fila** en modo debug
- **Registro de duplicados** con ID del parte existente
- **Estadísticas separadas**: Creados vs Actualizados vs Fallidos
- **Contadores independientes** para análisis

---

## 📋 Documentación

- ✅ **CHANGELOG.md**: Registro completo de cambios por versión
- ✅ **SISTEMA_NOTAS_VERSION.md**: Guía completa del sistema de notas
- ✅ **Link a GitHub Releases**: https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases

---

## 📦 Instalación

### Requisitos

- Windows 10 version 1809 (build 17763) o superior
- Windows 11 (recomendado)
- .NET 8.0 Runtime (incluido en el instalador)
- ~280 MB de espacio en disco

### Instalador MSI

1. Descargar `GestionTime-Desktop-v1.4.1-beta-Setup.msi`
2. Ejecutar el instalador (doble clic)
3. Seguir el asistente de instalación
4. La aplicación se instalará en `C:\Program Files\GestionTime\GestionTime Desktop`

### Actualización desde versiones anteriores

El instalador detecta versiones previas y las actualiza automáticamente.

---

## 🐛 Problemas Conocidos

- Ninguno reportado en esta versión

---

## 🔗 Enlaces Útiles

- **Repositorio**: https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop
- **Issues**: https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/issues
- **Wiki**: https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/wiki
- **Changelog Completo**: [CHANGELOG.md](https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/blob/main/CHANGELOG.md)

---

## 👥 Contribuciones

Gracias a todos los que han contribuido a esta versión.

---

## 📝 Changelog Completo

Para ver el historial completo de cambios, consulta el archivo [CHANGELOG.md](https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/blob/main/CHANGELOG.md).

---

**¡Disfruta de GestionTime Desktop v1.4.1-beta!** 🎉
