# 📦 Guía para Crear Instalador MSI - GestionTime Desktop

## 🚀 Uso Rápido

### Opción 1: Script Automático (Recomendado)

```powershell
.\BUILD-MSI.ps1
```

Este comando:
- ✅ Verifica herramientas necesarias
- ✅ Instala WiX Toolset si no está presente
- ✅ Publica la aplicación en Release
- ✅ Genera componentes WiX automáticamente
- ✅ Compila el instalador MSI
- ✅ Abre la carpeta con el instalador al finalizar

### Opción 2: Usar Archivos Existentes

Si ya publicaste la aplicación y quieres regenerar solo el MSI:

```powershell
.\BUILD-MSI.ps1 -SkipPublish
```

### Opción 3: Versión Personalizada

```powershell
.\BUILD-MSI.ps1 -Version "1.2.0.0"
```

---

## 📋 Requisitos Previos

| Herramienta | Versión Mínima | Se Instala Automáticamente |
|-------------|----------------|----------------------------|
| .NET SDK | 8.0 | ❌ No (debes instalarlo) |
| WiX Toolset | 5.0 | ✅ Sí (el script lo instala) |

### Instalar .NET SDK 8

Si no tienes .NET SDK 8:

```powershell
# Descargar desde:
# https://dotnet.microsoft.com/download/dotnet/8.0

# O instalar con winget:
winget install Microsoft.DotNet.SDK.8
```

---

## 📂 Estructura de Archivos Generados

```
GestionTimeDesktop/
├── bin/
│   └── Release/
│       ├── MSI/
│       │   └── GestionTimeDesktop-1.1.0.0.msi  ← INSTALADOR FINAL
│       ├── Installer/
│       │   └── App/
│       │       └── (archivos de la aplicación)
│       └── net8.0-windows10.0.19041.0/
│           └── win-x64/
│               └── publish/
│                   └── (archivos publicados)
└── Installer/
    └── MSI/
        ├── GestionTimeDesktop.wixproj
        ├── Product.wxs
        ├── Features.wxs  ← Generado automáticamente
        └── UI.wxs
```

---

## 🎯 Resultado

Al ejecutar el script obtendrás:

```
bin\Release\MSI\GestionTimeDesktop-1.1.0.0.msi
```

### Características del Instalador

- ✅ **Instala en:** `C:\Program Files\GestionTime Solutions\GestionTime Desktop\`
- ✅ **Acceso directo:** Menú Inicio y Escritorio (opcional)
- ✅ **Desinstalación:** Panel de Control → Programas
- ✅ **Actualización:** Permite instalar sobre versión anterior
- ✅ **Plataforma:** x64
- ✅ **Tamaño aproximado:** 150-200 MB

---

## 🔧 Comandos de Instalación

### Instalación Normal (con interfaz)

```powershell
msiexec /i "bin\Release\MSI\GestionTimeDesktop-1.1.0.0.msi"
```

### Instalación Silenciosa (sin interfaz)

```powershell
msiexec /i "bin\Release\MSI\GestionTimeDesktop-1.1.0.0.msi" /quiet /norestart
```

### Instalación con Log

```powershell
msiexec /i "bin\Release\MSI\GestionTimeDesktop-1.1.0.0.msi" /l*v "install.log"
```

### Desinstalación

```powershell
msiexec /x "bin\Release\MSI\GestionTimeDesktop-1.1.0.0.msi"
```

### Desinstalación Silenciosa

```powershell
msiexec /x "bin\Release\MSI\GestionTimeDesktop-1.1.0.0.msi" /quiet /norestart
```

---

## ⚙️ Parámetros del Script

| Parámetro | Tipo | Predeterminado | Descripción |
|-----------|------|----------------|-------------|
| `-Version` | String | "1.1.0.0" | Versión del instalador |
| `-SkipPublish` | Switch | false | Salta la publicación de la app |
| `-OpenAfter` | Switch | true | Abre carpeta al finalizar |

### Ejemplos

```powershell
# Versión 2.0.0
.\BUILD-MSI.ps1 -Version "2.0.0.0"

# Usar archivos existentes y no abrir carpeta
.\BUILD-MSI.ps1 -SkipPublish -OpenAfter:$false

# Solo regenerar el MSI
.\BUILD-MSI.ps1 -SkipPublish
```

---

## 🐛 Solución de Problemas

### Error: "wix no encontrado"

**Solución:**
```powershell
dotnet tool install --global wix --version 5.0.2
```

### Error: "Archivo crítico faltante"

**Solución:**
```powershell
# Limpiar y recompilar
.\rebuild-clean.ps1
.\BUILD-MSI.ps1
```

### Error: "No se puede compilar GestionTimeDesktop.wixproj"

**Solución:**
```powershell
# Verificar que existan los archivos WiX
dir Installer\MSI\

# Si faltan, el script los genera automáticamente
.\BUILD-MSI.ps1
```

### Error: ".NET SDK no encontrado"

**Solución:**
1. Descargar de: https://dotnet.microsoft.com/download/dotnet/8.0
2. Instalar .NET SDK 8
3. Reiniciar PowerShell
4. Ejecutar de nuevo

---

## 📊 Verificación del Instalador

### Ver Información del MSI

```powershell
# PowerShell
$msi = "bin\Release\MSI\GestionTimeDesktop-1.1.0.0.msi"
Get-Item $msi | Select-Object Name, Length, LastWriteTime
```

### Probar Instalación

```powershell
# Instalar en modo de prueba (sin logs)
msiexec /i "bin\Release\MSI\GestionTimeDesktop-1.1.0.0.msi" /l*v "test-install.log"

# Revisar el log
notepad test-install.log
```

---

## 🎨 Personalización

### Cambiar Icono de la Aplicación

Editar `Installer\MSI\Product.wxs`:

```xml
<Icon Id="app_logo.ico" SourceFile="..\..\Assets\MI_ICONO.ico" />
```

### Cambiar Carpeta de Instalación Predeterminada

Editar `Installer\MSI\Product.wxs`:

```xml
<Directory Id="CompanyFolder" Name="Mi Empresa">
  <Directory Id="INSTALLFOLDER" Name="Mi Aplicación" />
</Directory>
```

### Agregar Licencia

1. Crear archivo `Installer\MSI\License.rtf`
2. El instalador lo mostrará automáticamente

---

## 📦 Distribución del Instalador

### Subir a Servidor

```powershell
# Copiar a servidor de red
Copy-Item "bin\Release\MSI\GestionTimeDesktop-1.1.0.0.msi" "\\servidor\compartido\instaladores\"
```

### Crear Archivo de Firma Digital (Opcional)

```powershell
# Requiere certificado de firma de código
signtool sign /f "certificado.pfx" /p "password" /t http://timestamp.digicert.com "GestionTimeDesktop-1.1.0.0.msi"
```

---

## ✅ Checklist de Creación

- [ ] .NET SDK 8 instalado
- [ ] Código compilando sin errores (`dotnet build`)
- [ ] Ejecutable funciona localmente
- [ ] Ejecutar `.\BUILD-MSI.ps1`
- [ ] Verificar que el MSI se generó
- [ ] Probar instalación en máquina limpia
- [ ] Verificar accesos directos creados
- [ ] Probar desinstalación
- [ ] Documentar versión y cambios

---

## 📝 Notas

### Actualizaciones

Cuando publiques una nueva versión:

1. Actualizar versión en `GestionTime.Desktop.csproj`:
   ```xml
   <AssemblyVersion>1.2.0.0</AssemblyVersion>
   <FileVersion>1.2.0.0</FileVersion>
   ```

2. Crear nuevo instalador:
   ```powershell
   .\BUILD-MSI.ps1 -Version "1.2.0.0"
   ```

3. El instalador detectará la versión anterior y la actualizará automáticamente

### Espacio en Disco

- **Compilación completa:** ~500 MB
- **Instalador MSI:** ~150-200 MB
- **Instalación en cliente:** ~250-300 MB

---

## 🆘 Soporte

Si tienes problemas:

1. **Ver logs del script:** El script muestra errores detallados
2. **Leer documentación:** `SOLUCION-DEP1560.md`, `README-IDE-ERRORS.md`
3. **Limpiar y reconstruir:** `.\rebuild-clean.ps1` y luego `.\BUILD-MSI.ps1`

---

**Última actualización:** 2025-01-27  
**Versión de la guía:** 1.0  
**Script:** `BUILD-MSI.ps1`
