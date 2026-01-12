# Scripts de GestionTime Desktop

Esta carpeta contiene todos los scripts de automatización para el proyecto GestionTime Desktop.

## 📂 Estructura

### Scripts PowerShell (.ps1)
- **55 scripts PowerShell** para diversas tareas de build, instalación y diagnóstico

### Scripts Batch (.bat)
- **14 scripts batch** para ejecución rápida desde línea de comandos

## 🔧 Categorías de Scripts

### Build y Compilación
- `build-final-fixed.ps1`
- `build-for-installer.ps1`
- `build-for-installer-debug.ps1`
- `build-msi-complete.ps1`
- `BUILD-MSI-FINAL.ps1`
- `BUILD-MSI.ps1`
- `rebuild-clean.ps1`

### Generación de Instaladores MSI
- `CREAR-MSI-AHORA.ps1`
- `CREATE-MSI-DEFINITIVO.ps1`
- `CREATE-MSI-COMPLETE.ps1`
- `CREATE-MSI-INSTALLER-COMPLETE.ps1`
- `CREATE-MSI-LIMPIO-FINAL.ps1`
- `CREATE-MSI-SIMPLE-LIMPIO.ps1`
- `CREATE-MSI-WITH-DIRS.ps1`
- `CREATE-MSI-WIX6-SIMPLE.ps1`
- `CREATE-MSI-XMLWRITER.ps1`
- `create-complete-msi.ps1`
- `create-complete-msi-with-all-files.ps1`
- `create-improved-msi.ps1`
- `create-msi-debug-complete.ps1`
- `create-msi-installer.ps1`
- `create-professional-msi.ps1`
- `GENERAR-MSI-SIMPLE-FUNCIONAL.ps1`
- `GENERAR-MSI-WIX3.ps1`
- `generate-wix-components.ps1`

### Generación de Instaladores MSIX
- `CREATE-MSIX-INSTALLER.ps1`
- `GENERAR-MSIX-VISUAL-STUDIO.ps1`

### Generación de Instaladores Portables y EXE
- `CREATE-PORTABLE-FROM-DEBUG.ps1`
- `CREATE-PORTABLE.ps1`
- `GENERAR-INSTALADOR-PORTABLE.ps1`
- `CREATE-EXE-INSTALLER-FINAL.ps1`
- `CREATE-INSTALLER-EXE.ps1`
- `CREATE-INSTALLER-COMPLETE-V3.ps1`
- `CREATE-SIMPLE-INSTALLER.ps1`
- `create-selfextracting-installer.ps1`
- `create-improved-selfextracting-installer.ps1`
- `create-inno-installer.ps1`

### Paquetes y Publicación
- `CREATE-CLEAN-PACKAGE.ps1`
- `CREATE-FINAL-PACKAGE.ps1`
- `publish-release.ps1`

### Diagnóstico y Validación
- `diagnose-installation.ps1`
- `diagnose-runtime-issues.ps1`
- `diagnose-selfextracting-installer.ps1`
- `diagnose-winui-app.ps1`
- `validate-msi.ps1`
- `analyze-missing-files.ps1`
- `compare-all-installers.ps1`

### Utilidades
- `ABRIR-EN-ISE.ps1` - Abre scripts en PowerShell ISE
- `INSTALAR-WIX3.ps1` - Instala WiX Toolset v3
- `VERIFICAR-REQUISITOS.ps1` - Verifica requisitos del sistema
- `fix-ide-errors.ps1`
- `fix-missing-files-quick.ps1`
- `clean-project-structure.ps1`
- `test-app-direct.ps1`

## 🎯 Scripts Batch (.bat)

### Generación de Instaladores
- `CREAR-MSI-FINAL.bat`
- `EJECUTAR-CREATE-MSI.bat`
- `EJECUTAR-MSI.bat`
- `GENERAR-INSTALADOR-AHORA.bat`
- `GENERAR-INSTALADOR-MENU.bat`
- `GENERAR-INSTALADOR-MSIX.bat`
- `GENERAR-INSTALADOR-PORTABLE.bat`
- `GENERAR-MSI-CON-HEAT.bat`
- `GENERAR-MSI-DIRECTO.bat`
- `GENERAR-MSI-FINAL-FUNCIONAL.bat`
- `GENERAR-MSI-SIMPLE.bat`

### Utilidades
- `launch-app.bat` - Lanza la aplicación directamente
- `PUBLICAR-RELEASE.bat` - Publica una release
- `VERIFICAR-REQUISITOS.bat` - Verifica requisitos del sistema

## 📖 Uso

### Ejecutar desde PowerShell
```powershell
# Navegar a la carpeta Scripts
cd Scripts

# Ejecutar un script
.\CREAR-MSI-AHORA.ps1
```

### Ejecutar desde CMD
```cmd
REM Navegar a la carpeta Scripts
cd Scripts

REM Ejecutar un script batch
GENERAR-INSTALADOR-MENU.bat
```

## ⚠️ Notas Importantes

1. Algunos scripts requieren permisos de administrador
2. Asegúrate de tener instaladas las dependencias necesarias (WiX Toolset, .NET SDK, etc.)
3. Ejecuta `VERIFICAR-REQUISITOS.bat` antes de usar los scripts de instaladores
4. Revisa la documentación en la carpeta `Docs` para más información

## 📚 Documentación Relacionada

- `Docs/COMO-GENERAR-INSTALADOR.txt` - Guía para generar instaladores
- `Docs/GUIA-VISUAL-INSTALADORES.txt` - Guía visual de instaladores
- `Docs/LEEME-PRIMERO.txt` - Información general
