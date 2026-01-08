# 📦 CREAR INSTALADOR MSI - GESTIONTIME DESKTOP

**Versión:** 1.2.0  
**Fecha:** Enero 2026  
**Tipo:** Instalador MSI Profesional con WiX Toolset

---

## 🎯 OBJETIVO

Crear un instalador **MSI profesional** que incluya **TODOS** los archivos necesarios para ejecutar GestionTime Desktop, incluyendo:

✅ Ejecutable principal (`GestionTime.Desktop.exe`)  
✅ Todas las DLLs de dependencias (.NET, WinUI 3, etc.)  
✅ Archivos de configuración (`appsettings.json`)  
✅ Assets (iconos, imágenes, logos)  
✅ Carpetas `runtimes` completas (x64, x86, arm64)  
✅ Accesos directos en Menú Inicio y Escritorio  
✅ Registro en "Programas y características"  
✅ Desinstalador automático

---

## 📋 REQUISITOS PREVIOS

### 1️⃣ **WiX Toolset 3.14 o superior**

**Verificar si está instalado:**
```powershell
Get-Command candle.exe -ErrorAction SilentlyContinue
```

**Instalar WiX Toolset:**

**Opción A - WinGet (Recomendado):**
```powershell
winget install WiXToolset.WiX
```

**Opción B - Descarga Manual:**
1. Ir a: https://wixtoolset.org/releases/
2. Descargar: `wix314.exe` (versión 3.14)
3. Ejecutar instalador
4. Reiniciar PowerShell

**Verificar instalación:**
```powershell
& "C:\Program Files (x86)\WiX Toolset v3.14\bin\candle.exe" -?
```

### 2️⃣ **Proyecto Compilado**

El directorio de origen debe contener el proyecto compilado:

```
C:\GestionTime\GestionTimeDesktop\bin\x64\Debug\net8.0-windows10.0.19041.0\
├── GestionTime.Desktop.exe
├── GestionTime.Desktop.dll
├── appsettings.json
├── Assets\
│   ├── app.ico
│   ├── LogoClaro.png
│   ├── LogoOscuro.png
│   └── ...
├── runtimes\
│   ├── win-x64\
│   ├── win-x86\
│   └── win-arm64\
└── ... (todas las DLLs)
```

**Si NO está compilado, ejecutar:**
```powershell
dotnet build -c Debug -r win-x64
```

---

## 🚀 CREAR EL INSTALADOR MSI

### **Método Automático (Recomendado)**

1. **Abrir PowerShell como Administrador**
   ```
   Click derecho en PowerShell → "Ejecutar como administrador"
   ```

2. **Navegar al directorio del proyecto**
   ```powershell
   cd C:\GestionTime\GestionTimeDesktop
   ```

3. **Ejecutar el script de creación**
   ```powershell
   .\CREATE-MSI-INSTALLER-COMPLETE.ps1
   ```

4. **Resultado esperado:**
   ```
   ========================================
     INSTALADOR MSI CREADO EXITOSAMENTE
   ========================================
   
   Archivo MSI:
     C:\GestionTime\GestionTimeDesktop\Installer\Output\GestionTime-Desktop-1.2.0-Setup.msi
   
   Tamaño:
     45.3 MB
   
   Componentes incluidos:
     347 archivos
   ```

### **Parámetros Opcionales**

```powershell
# Cambiar directorio de origen
.\CREATE-MSI-INSTALLER-COMPLETE.ps1 -SourceDir "C:\OtraRuta\bin\..."

# Cambiar directorio de salida
.\CREATE-MSI-INSTALLER-COMPLETE.ps1 -OutputDir "C:\Instaladores"

# Cambiar versión
.\CREATE-MSI-INSTALLER-COMPLETE.ps1 -Version "1.3.0"
```

---

## 📂 ESTRUCTURA GENERADA

Después de ejecutar el script, se creará la siguiente estructura:

```
C:\GestionTime\GestionTimeDesktop\
├── Installer\
│   ├── MSI\
│   │   ├── Product.wxs          ← Definición del producto
│   │   ├── Features.wxs         ← Componentes (347 archivos)
│   │   ├── License.rtf          ← Licencia de uso
│   │   ├── Product.wixobj       ← Objeto compilado (Product)
│   │   └── Features.wixobj      ← Objeto compilado (Features)
│   └── Output\
│       └── GestionTime-Desktop-1.2.0-Setup.msi  ← INSTALADOR FINAL
```

---

## 🔍 CONTENIDO DEL MSI

El instalador MSI incluye **TODOS** los archivos necesarios:

### **Archivos Principales**
- ✅ `GestionTime.Desktop.exe` (ejecutable principal)
- ✅ `GestionTime.Desktop.dll` (biblioteca principal)
- ✅ `appsettings.json` (configuración)

### **Dependencias .NET**
- ✅ `System.*.dll` (bibliotecas del framework)
- ✅ `Microsoft.*.dll` (bibliotecas de Microsoft)
- ✅ `Newtonsoft.Json.dll`, `Serilog.dll`, etc.

### **WinUI 3 y Windows App Runtime**
- ✅ `Microsoft.UI.Xaml.dll`
- ✅ `Microsoft.WindowsAppRuntime.*.dll`
- ✅ `Microsoft.Windows.SDK.*.dll`

### **Carpetas Runtimes**
- ✅ `runtimes\win-x64\` (bibliotecas nativas x64)
- ✅ `runtimes\win-x86\` (bibliotecas nativas x86)
- ✅ `runtimes\win-arm64\` (bibliotecas nativas ARM64)

### **Assets**
- ✅ `Assets\*.png` (iconos, logos, fondos)
- ✅ `Assets\app.ico` (icono de la aplicación)

### **Accesos Directos**
- ✅ Menú Inicio → `GestionTime Desktop`
- ✅ Escritorio → `GestionTime Desktop`
- ✅ Desinstalador en "Programas y características"

---

## 💾 INSTALAR EL MSI

### **Instalación Normal (GUI)**

1. Hacer **doble-clic** en el archivo MSI:
   ```
   GestionTime-Desktop-1.2.0-Setup.msi
   ```

2. Seguir el asistente de instalación:
   - Aceptar licencia
   - Elegir directorio (por defecto: `C:\Program Files\GestionTime\Desktop`)
   - Click en "Instalar"

3. Buscar **"GestionTime Desktop"** en el Menú Inicio

### **Instalación Silenciosa (Sin GUI)**

```cmd
msiexec /i "GestionTime-Desktop-1.2.0-Setup.msi" /qn /norestart
```

### **Desinstalación**

**Desde Windows:**
```
Panel de Control → Programas y características → GestionTime Desktop → Desinstalar
```

**Desde CMD (silencioso):**
```cmd
msiexec /x "GestionTime-Desktop-1.2.0-Setup.msi" /qn /norestart
```

---

## 🛠️ SOLUCIÓN DE PROBLEMAS

### ❌ **Error: "WiX Toolset no encontrado"**

**Solución:**
```powershell
# Instalar WiX Toolset
winget install WiXToolset.WiX

# Reiniciar PowerShell
exit
# Abrir PowerShell de nuevo como Administrador
```

### ❌ **Error: "No existe el directorio de origen"**

**Solución:**
```powershell
# Compilar el proyecto primero
dotnet build -c Debug -r win-x64

# Verificar que existe el ejecutable
Test-Path "C:\GestionTime\GestionTimeDesktop\bin\x64\Debug\net8.0-windows10.0.19041.0\GestionTime.Desktop.exe"
```

### ❌ **Error: "Error al compilar Product.wxs"**

**Causas comunes:**
- Falta el archivo `app.ico` en la carpeta `Assets`
- Ruta demasiado larga (>260 caracteres)
- Permisos insuficientes

**Solución:**
```powershell
# Ejecutar PowerShell como Administrador
# Verificar que existe app.ico
Test-Path ".\Assets\app.ico"

# Si no existe, crear uno temporal
# O comentar la línea del icono en Product.wxs
```

### ❌ **Error: "light.exe : error LGHT0001 : System.UnauthorizedAccessException"**

**Solución:**
```powershell
# Ejecutar PowerShell como Administrador
# O cambiar el directorio de salida a uno con permisos:
.\CREATE-MSI-INSTALLER-COMPLETE.ps1 -OutputDir "$env:USERPROFILE\Desktop"
```

---

## 📊 VERIFICACIÓN DEL MSI

### **Verificar contenido del MSI**

```powershell
# Instalar Orca (herramienta de Microsoft)
# https://learn.microsoft.com/en-us/windows/win32/msi/orca-exe

# O usar msiexec para listar archivos
msiexec /a "GestionTime-Desktop-1.2.0-Setup.msi" /qb TARGETDIR="C:\Temp\MSI-Extract"
```

### **Verificar firma digital (opcional)**

```powershell
Get-AuthenticodeSignature ".\GestionTime-Desktop-1.2.0-Setup.msi"
```

---

## 📝 PERSONALIZACIÓN

### **Cambiar información del producto**

Editar `Installer\MSI\Product.wxs`:

```xml
<Product Id="*" 
         Name="GestionTime Desktop"           ← Cambiar nombre
         Manufacturer="Global Retail Solutions" ← Cambiar empresa
         Version="1.2.0.0">                    ← Cambiar versión
```

### **Cambiar directorio de instalación predeterminado**

Editar `Installer\MSI\Product.wxs`:

```xml
<Directory Id="ProgramFiles64Folder">
  <Directory Id="ManufacturerFolder" Name="MiEmpresa">  ← Cambiar
    <Directory Id="INSTALLFOLDER" Name="MiApp" />        ← Cambiar
  </Directory>
</Directory>
```

### **Agregar más accesos directos**

Editar `Installer\MSI\Product.wxs` y agregar:

```xml
<Shortcut Id="DocumentationShortcut"
          Name="Manual de Usuario"
          Target="[INSTALLFOLDER]Docs\Manual.pdf"
          WorkingDirectory="INSTALLFOLDER" />
```

---

## ✅ CHECKLIST PRE-DISTRIBUCIÓN

Antes de distribuir el MSI, verificar:

- [ ] WiX Toolset instalado (v3.14+)
- [ ] Proyecto compilado correctamente
- [ ] Archivo `GestionTime.Desktop.exe` existe
- [ ] Archivo `appsettings.json` existe
- [ ] Carpeta `Assets` completa
- [ ] Carpeta `runtimes` completa
- [ ] Script ejecutado sin errores
- [ ] MSI generado en `Installer\Output`
- [ ] Tamaño del MSI > 40 MB (indica que tiene todo)
- [ ] Instalación probada en máquina limpia
- [ ] Desinstalación funciona correctamente
- [ ] Accesos directos funcionan
- [ ] Aplicación ejecuta sin errores

---

## 📞 SOPORTE

**Email:** soporte@gestiontime.com  
**GitHub:** https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop

---

## 📄 LICENCIA

**GestionTime Desktop** © 2026 Global Retail Solutions  
Todos los derechos reservados.

---

**🎯 ¡Instalador MSI listo para distribuir!**

*Guía de Instalación MSI - Versión 1.2.0 - Enero 2026*
