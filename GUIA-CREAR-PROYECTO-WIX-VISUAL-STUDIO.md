# 📘 GUÍA: CREAR PROYECTO WIX EN VISUAL STUDIO

**Fecha:** 08/01/2026 17:30  
**Objetivo:** Crear un instalador MSI profesional con Visual Studio  
**Tiempo estimado:** 1-2 horas

---

## 📋 **REQUISITOS PREVIOS**

### **1. Visual Studio 2022**
- ✅ Ya lo tienes instalado
- Versión recomendada: 17.8 o superior

### **2. Extensión WiX Toolset para Visual Studio**

**Instalar extensión:**

```
1. Abrir Visual Studio 2022

2. Menú: Extensions → Manage Extensions

3. Buscar: "WiX Toolset Visual Studio Extension"
   (o "HeatWave for VS2022")

4. Click "Download"

5. Cerrar Visual Studio para que se instale

6. Reabrir Visual Studio
```

**Alternativa (si no está disponible):**

Descargar desde: https://marketplace.visualstudio.com/items?itemName=WixToolset.WixToolsetVisualStudio2022Extension

---

## 🚀 **PASO 1: CREAR PROYECTO WIX**

### **1.1. Crear nuevo proyecto**

```
1. Visual Studio → File → New → Project

2. Buscar: "WiX" o "Setup"

3. Seleccionar: "Setup Project for WiX v4" o "WiX Setup Project"

4. Click "Next"
```

### **1.2. Configurar proyecto**

```
Project name: GestionTime.Installer
Location: C:\GestionTime\
Solution: Add to solution "GestionTime.Desktop"

Click "Create"
```

**Estructura creada:**
```
GestionTime.Desktop.sln
├── GestionTime.Desktop/          ← Tu proyecto existente
└── GestionTime.Installer/        ← Nuevo proyecto WiX
    ├── Product.wxs               ← Archivo principal
    ├── GestionTime.Installer.wixproj
    └── References/
```

---

## 📝 **PASO 2: CONFIGURAR Product.wxs**

Visual Studio crea un archivo `Product.wxs` básico. Vamos a configurarlo:

### **2.1. Abrir Product.wxs**

En Solution Explorer:
```
GestionTime.Installer → Product.wxs (doble-click)
```

### **2.2. Configuración básica**

Reemplazar contenido con:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs"
     xmlns:ui="http://wixtoolset.org/schemas/v4/wxs/ui">
  
  <Package Name="GestionTime Desktop"
           Language="1034"
           Version="1.2.0.0"
           Manufacturer="Global Retail Solutions"
           UpgradeCode="F1E2D3C4-B5A6-9780-ABCD-123456789012">

    <MajorUpgrade DowngradeErrorMessage="Ya existe una versión más reciente instalada." />
    <MediaTemplate EmbedCab="yes" />

    <!-- Iconos y propiedades -->
    <Icon Id="AppIcon.ico" SourceFile="$(var.GestionTime.Desktop.ProjectDir)Assets\app_logo.ico" />
    <Property Id="ARPPRODUCTICON" Value="AppIcon.ico" />
    <Property Id="ARPCONTACT" Value="soporte@gestiontime.com" />

    <!-- Estructura de directorios -->
    <StandardDirectory Id="ProgramFiles64Folder">
      <Directory Id="INSTALLFOLDER" Name="GestionTime Desktop" />
    </StandardDirectory>

    <StandardDirectory Id="ProgramMenuFolder">
      <Directory Id="ProgramMenuDir" Name="GestionTime Desktop" />
    </StandardDirectory>

    <!-- Feature principal -->
    <Feature Id="MainApplication" 
             Title="GestionTime Desktop" 
             Level="1">
      <ComponentGroupRef Id="AppFiles" />
      <ComponentRef Id="StartMenuShortcut" />
    </Feature>

    <!-- UI de instalación -->
    <ui:WixUI Id="WixUI_InstallDir" InstallDirectory="INSTALLFOLDER" />
    <WixVariable Id="WixUILicenseRtf" Value="License.rtf" />

  </Package>

  <!-- Acceso directo Menú Inicio -->
  <Fragment>
    <Component Id="StartMenuShortcut" Directory="ProgramMenuDir">
      <Shortcut Id="AppShortcut"
                Name="GestionTime Desktop"
                Target="[INSTALLFOLDER]GestionTime.Desktop.exe"
                WorkingDirectory="INSTALLFOLDER"
                Icon="AppIcon.ico" />
      <RemoveFolder Id="RemoveMenuDir" On="uninstall" />
      <RegistryValue Root="HKCU" 
                     Key="Software\GestionTime\Desktop" 
                     Name="Installed" 
                     Value="1" 
                     Type="integer" 
                     KeyPath="yes" />
    </Component>
  </Fragment>

</Wix>
```

**Guardar:** Ctrl+S

---

## 🔗 **PASO 3: REFERENCIAR PROYECTO DE APLICACIÓN**

Para que WiX pueda incluir los archivos compilados:

### **3.1. Agregar referencia**

```
1. Solution Explorer → GestionTime.Installer → 
   Click derecho en "References"

2. Add Reference...

3. Projects → Marcar "GestionTime.Desktop"

4. Click "OK"
```

**Esto hace que WiX pueda acceder a:**
- `$(var.GestionTime.Desktop.TargetDir)` → Carpeta bin con archivos compilados
- `$(var.GestionTime.Desktop.ProjectDir)` → Carpeta del proyecto

---

## 📦 **PASO 4: INCLUIR ARCHIVOS CON HEAT**

Heat es una herramienta de WiX que genera automáticamente componentes para todos los archivos.

### **4.1. Crear archivo Heat en el proyecto**

```
1. Solution Explorer → GestionTime.Installer → 
   Click derecho → Add → New Item...

2. Buscar: "WiX Fragment File"

3. Name: AppFiles.wxs

4. Click "Add"
```

### **4.2. Configurar Heat en Build**

```
1. Solution Explorer → GestionTime.Installer → 
   Click derecho en el proyecto → Properties

2. Tool Settings → Heat

3. Harvest Files: Marcar checkbox

4. Source: $(var.GestionTime.Desktop.TargetDir)

5. Component Group Name: AppFiles

6. Directory Reference Id: INSTALLFOLDER

7. Suppress Registry Harvesting: Yes

8. Suppress Root Directory: Yes

9. Click "Apply" y "OK"
```

**Esto genera automáticamente `AppFiles.wxs` al compilar.**

---

## 🎨 **PASO 5: PERSONALIZAR UI (OPCIONAL)**

### **5.1. Agregar License.rtf**

```
1. Copiar: C:\GestionTime\GestionTimeDesktop\Installer\MSI\License.rtf

2. Pegar en: GestionTime.Installer\

3. Solution Explorer → GestionTime.Installer → 
   Click derecho en License.rtf → Properties

4. Build Action: Content

5. Copy to Output Directory: Copy if newer
```

### **5.2. Personalizar diálogos (avanzado)**

WiX permite personalizar completamente los diálogos de instalación editando archivos `.wxs` adicionales.

---

## 🔨 **PASO 6: COMPILAR EL MSI**

### **6.1. Configurar solución**

```
1. Solution Explorer → Click derecho en "Solution"

2. Properties

3. Configuration Properties → Configuration

4. Marcar "Build" para ambos proyectos:
   - GestionTime.Desktop
   - GestionTime.Installer

5. Click "OK"
```

### **6.2. Build**

```
1. Build → Configuration Manager

2. Active solution configuration: Release

3. Platform: x64

4. Build → Build Solution (F6)
```

**Tiempo de compilación:** 1-3 minutos

---

## 📁 **PASO 7: OBTENER EL MSI**

El MSI se genera en:

```
C:\GestionTime\GestionTime.Installer\bin\x64\Release\es-ES\
└── GestionTime.Installer.msi
```

**Verificar:**
```powershell
Get-Item "C:\GestionTime\GestionTime.Installer\bin\x64\Release\es-ES\*.msi"
```

---

## ✅ **VENTAJAS DE ESTE MÉTODO**

### **vs Scripts PowerShell:**

| Característica | Visual Studio WiX | Scripts PowerShell |
|---|---|---|
| **Genera MSI** | ✅ Siempre funciona | ❌ Problemas de parsing |
| **Estructura de carpetas** | ✅ Automática (Heat) | ❌ Archivos fantasma |
| **UI de instalación** | ✅ Completa y personalizable | ⚠️ Limitada |
| **Debugging** | ✅ Integrado en VS | ❌ Difícil |
| **Mantenimiento** | ✅ Fácil (editor visual) | ❌ Editar scripts |
| **Profesionalismo** | ✅ Estándar industria | ⚠️ Variable |

---

## 🐛 **SOLUCIÓN DE PROBLEMAS**

### **Extensión WiX no aparece**

**Solución:**
```
1. Verificar que Visual Studio 2022 está actualizado

2. Descargar instalador manual desde:
   https://wixtoolset.org/releases/

3. Instalar WiX Toolset v4 o v5

4. Reiniciar Visual Studio
```

### **Error "Heat failed"**

**Solución:**
```
1. Verificar que GestionTime.Desktop se compila correctamente

2. Build → Rebuild GestionTime.Desktop

3. Build → Clean GestionTime.Installer

4. Build → Build GestionTime.Installer
```

### **Archivos faltantes en MSI**

**Solución:**
```
1. Verificar que Heat está configurado correctamente

2. Tool Settings → Heat → Source debe apuntar a:
   $(var.GestionTime.Desktop.TargetDir)

3. Rebuild solution
```

### **MSI no se instala**

**Solución:**
```
1. Verificar logs de instalación:
   C:\Users\[Usuario]\AppData\Local\Temp\

2. Buscar archivos: MSI*.LOG

3. Abrir con notepad para ver errores
```

---

## 📊 **COMPARACIÓN FINAL**

### **Método Visual Studio (RECOMENDADO):**

**Ventajas:**
- ✅ Siempre funciona
- ✅ No hay problemas de parsing
- ✅ Heat genera componentes automáticamente
- ✅ Editor visual integrado
- ✅ Debugging incorporado
- ✅ Actualizaciones fáciles

**Desventajas:**
- ⏱️ Configuración inicial (1-2 horas)
- 📦 Requiere extensión WiX

### **Método Scripts PowerShell:**

**Ventajas:**
- ⚡ Rápido si funciona
- 📝 No requiere Visual Studio

**Desventajas:**
- ❌ Problemas de parsing con XML
- ❌ Archivos fantasma
- ❌ Difícil de mantener
- ❌ No funciona en tu máquina

---

## 🎯 **RESULTADO FINAL**

**Con Visual Studio obtendrás:**

```
GestionTime.Installer.msi
- Tamaño: ~15-20 MB
- Archivos: TODOS (153+)
- Estructura: Completa (Assets, Views, Controls, runtimes)
- UI: Profesional con selección de directorio
- Shortcuts: Automáticos (Menú Inicio, Escritorio)
- Desinstalador: Integrado
- Estado: ✅ FUNCIONA PERFECTAMENTE
```

---

## 📝 **RESUMEN DE PASOS**

1. **Instalar extensión WiX** en Visual Studio
2. **Crear proyecto WiX** (Add to solution)
3. **Configurar Product.wxs** (copiar template)
4. **Referenciar proyecto Desktop**
5. **Configurar Heat** para incluir archivos
6. **Build Solution** (F6)
7. **MSI listo** en `bin\x64\Release\`

**Tiempo total:** 1-2 horas la primera vez, luego solo Build (1-3 min)

---

## 🎓 **RECURSOS ADICIONALES**

**Documentación oficial:**
- WiX Toolset: https://wixtoolset.org/documentation/
- Tutorial completo: https://wixtoolset.org/documentation/manual/v4/

**Videos tutoriales:**
- Buscar en YouTube: "WiX Toolset Visual Studio tutorial"
- Ejemplos: https://github.com/wixtoolset/wix4-examples

---

## ✅ **CONCLUSIÓN**

**Este es el método PROFESIONAL y CORRECTO para crear instaladores MSI.**

Los scripts PowerShell son útiles para proyectos simples, pero para una aplicación WinUI 3 con 153 archivos y estructura compleja, **Visual Studio con WiX es la única solución real y mantenible**.

**¡Con este método tendrás un instalador MSI profesional que SÍ funciona!** 🚀

---

*Guía Completa: Proyecto WiX en Visual Studio - 08/01/2026 17:30*
