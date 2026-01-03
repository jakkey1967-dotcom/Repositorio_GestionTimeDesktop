# 🛠️ Scripts y Herramientas - GestionTime.Desktop

Este directorio contiene todos los scripts PowerShell y herramientas de desarrollo para el proyecto GestionTime.Desktop.

## 📁 Organización de Scripts

### 🏗️ Scripts de Construcción e Instalación
- **build-installer.ps1** - Constructor de instalador principal
- **build-msi-installer.ps1** - Constructor de instalador MSI
- **build-msi.ps1** - Script de construcción MSI
- **publish-msix.ps1** - Publicación en formato MSIX
- **crear-instalador-msix.ps1** - Creador de instalador MSIX

### 🎨 Scripts de Iconos y Recursos
- **create-icon-from-png.ps1** - Crear icono desde PNG
- **fix-icon.ps1** - Reparar problemas de iconos
- **setup-icon.ps1** - Configurar icono de la aplicación

### 🔄 Scripts de Git y Migración
- **migrate-to-new-repo.ps1** - Migrar a nuevo repositorio
- **prepare-and-migrate.ps1** - Preparar y migrar repositorio
- **setup-git-backup.ps1** - Configurar backup de Git
- **check-git-status.ps1** - Verificar estado de Git
- **create-github-repo.ps1** - Crear repositorio en GitHub

### 🧪 Scripts de Pruebas y Diagnóstico
- **test-logging-rapido.ps1** - Prueba rápida de logging
- **test-logging-real.ps1** - Prueba real de logging
- **test-logging-real-mejorado.ps1** - Prueba mejorada de logging
- **test-sistema-logging.ps1** - Prueba completa del sistema de logging
- **diagnostico-logs-tiempo-real.ps1** - Diagnóstico de logs en tiempo real

### 🌐 Scripts de API y Conexión
- **despertar-api-render.ps1** - Despertar API en Render
- **diagnostico-api-render.ps1** - Diagnóstico de API en Render
- **diagnostico-conexion-api.ps1** - Diagnóstico de conexión API
- **verificacion-api-corregida.ps1** - Verificación de API corregida
- **prueba-final-hardcoded.ps1** - Prueba final con URLs fijas

### 🏥 Scripts de Diagnóstico de Sistema
- **Diagnostico-Program-Files.ps1** - Diagnóstico de Program Files
- **Verificar-Instalacion.ps1** - Verificar instalación del sistema
- **verificar-ubicacion-logs.ps1** - Verificar ubicación de logs
- **verificar-ubicaciones-reales.ps1** - Verificar ubicaciones reales

### ⚙️ Scripts de Configuración
- **debug-configuracion-extensivo.ps1** - Debug extensivo de configuración
- **crear-directorios-logs.ps1** - Crear directorios de logs
- **abrir-visual-studio-msix.ps1** - Abrir Visual Studio con configuración MSIX

### 🧹 Scripts de Limpieza
- **clean-project-structure.ps1** - Limpiar estructura del proyecto

## 📖 Cómo Usar

### Para Desarrolladores
```powershell
# Ejecutar cualquier script
.\nombre-del-script.ps1

# Verificar estado general
.\check-git-status.ps1

# Crear instalador
.\build-installer.ps1

# Diagnosticar problemas
.\Diagnostico-Program-Files.ps1
```

### Para Debugging
```powershell
# Verificar logs en tiempo real
.\diagnostico-logs-tiempo-real.ps1

# Probar sistema de logging
.\test-sistema-logging.ps1

# Verificar conexión API
.\diagnostico-conexion-api.ps1
```

### Para Deployment
```powershell
# Crear instalador MSIX
.\crear-instalador-msix.ps1

# Publicar aplicación
.\publish-msix.ps1
```

## ⚠️ Notas Importantes

- **Ejecutar como Administrador**: Algunos scripts requieren permisos elevados
- **Política de Ejecución**: Asegúrate de que PowerShell permita ejecución de scripts
- **Backup**: Los scripts de migración crean backups automáticamente
- **Logs**: Muchos scripts generan logs en `logs/` para debugging

## 🔧 Mantenimiento

Este directorio se mantiene limpio automáticamente. Los scripts obsoletos se mueven aquí desde el directorio raíz para mantener la estructura del proyecto organizada.

---

*Scripts mantenidos y actualizados con cada versión del proyecto.*