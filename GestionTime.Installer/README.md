# 📦 INSTALADOR MSI PROFESIONAL - GESTIONTIME DESKTOP

## ✅ ¿QUÉ ES UN INSTALADOR MSI?

Un instalador **MSI (Microsoft Installer)** es el formato estándar de Windows para instalar aplicaciones profesionalmente:

### **Ventajas sobre el instalador Portable:**

✅ **Instalación guiada con asistente visual**
✅ **Se registra en el Panel de Control de Windows**
✅ **Desinstalación desde "Agregar o quitar programas"**
✅ **Soporte para actualizaciones automáticas**
✅ **Instalación corporativa con GPO (Group Policy)**
✅ **Verificación de integridad de archivos**
✅ **Rollback automático si falla la instalación**
✅ **Compatible con Windows Installer Service**

---

## 🚀 COMPILAR EL INSTALADOR MSI

### **Opción 1: Desde Visual Studio (Recomendado)**

1. **Instalar HeatWave** (extensión WiX para Visual Studio):
   - Descargar: https://marketplace.visualstudio.com/items?itemName=FireGiant.FireGiantHeatWaveDev17
   - Ejecutar el `.vsix` descargado
   - Reiniciar Visual Studio

2. **Abrir la solución:**
   ```
   GestionTime.sln
   ```

3. **Establecer configuración Release:**
   - Menú: `Build` → `Configuration Manager`
   - Configuration: `Release`
   - Platform: `x64`

4. **Compilar el instalador:**
   - Clic derecho en `GestionTime.Installer`
   - `Build` o `Rebuild`

5. **Resultado:**
   ```
   GestionTime.Installer\bin\Release\GestionTime.Desktop.msi
   ```

---

### **Opción 2: Desde PowerShell (Automatizado)**

```powershell
# Compilar versión Release
.\Build-Installer.ps1

# Compilar versión específica
.\Build-Installer.ps1 -Version "1.2.0"

# Compilar Debug
.\Build-Installer.ps1 -Configuration Debug
```

**Resultado:**
```
GestionTime-Desktop-v1.2.0-Setup.msi
```

---

## 📋 REQUISITOS PREVIOS

### **1. WiX Toolset v6.0 (Ya instalado)**
```powershell
wix.exe --version
# Debe mostrar: 6.0.2.0
```

### **2. .NET 8 SDK (Ya instalado)**
```powershell
dotnet --version
# Debe mostrar: 8.x.x
```

### **3. Visual Studio 2022 con carga de trabajo:**
- ✅ Desarrollo de escritorio de .NET
- ✅ Desarrollo de aplicaciones de Windows con C++

---

## 🔧 CONFIGURACIÓN DEL INSTALADOR

El archivo `Package.wxs` define:

### **Información del Producto:**
- **Nombre:** GestionTime Desktop
- **Versión:** 1.2.0.0 (definida en `<?define ProductVersion = "1.2.0.0" ?>`)
- **Fabricante:** GestionTime Solutions
- **UpgradeCode:** GUID único para actualizaciones

### **Ubicación de Instalación por Defecto:**
```
C:\Program Files\GestionTime Solutions\GestionTime Desktop\
```

### **Características Instaladas:**
1. ✅ Ejecutable principal (`GestionTime.Desktop.exe`)
2. ✅ Archivo PRI (recursos XAML) - **CRÍTICO**
3. ✅ Configuración (`appsettings.json`)
4. ✅ Carpeta Assets (14 imágenes)
5. ✅ Todas las dependencias DLL (.NET Runtime incluido)
6. ✅ Acceso directo en Menú Inicio
7. ✅ Acceso directo en Escritorio
8. ✅ Acceso directo para desinstalar

### **Interfaz de Usuario:**
- Asistente moderno de instalación (WixUI)
- Selección de carpeta de destino
- Barra de progreso
- Confirmación final

---

## 🎯 PROCESO COMPLETO DE COMPILACIÓN

### **Paso a Paso Automatizado:**

```
┌──────────────────────────────────────────────┐
│ 1. Limpiar compilaciones anteriores          │
│    dotnet clean                              │
└──────────────────────────────────────────────┘
                    ↓
┌──────────────────────────────────────────────┐
│ 2. Publicar aplicación .NET                  │
│    dotnet publish (self-contained)           │
│    → publish\portable\                       │
└──────────────────────────────────────────────┘
                    ↓
┌──────────────────────────────────────────────┐
│ 3. Verificar archivos críticos               │
│    ✅ .exe, .pri, .json, Assets/            │
└──────────────────────────────────────────────┘
                    ↓
┌──────────────────────────────────────────────┐
│ 4. Compilar instalador MSI con WiX           │
│    dotnet build GestionTime.Installer        │
└──────────────────────────────────────────────┘
                    ↓
┌──────────────────────────────────────────────┐
│ 5. Resultado Final                           │
│    GestionTime-Desktop-v1.2.0-Setup.msi      │
│    ~110 MB                                   │
└──────────────────────────────────────────────┘
```

---

## 📦 DISTRIBUCIÓN

### **Archivo final a distribuir:**
```
GestionTime-Desktop-v1.2.0-Setup.msi
```

### **Experiencia del usuario final:**

1. **Descargar** el archivo `.msi`
2. **Doble clic** en el archivo
3. **Asistente de instalación** se abre automáticamente:
   - Aceptar licencia (si se configura)
   - Elegir carpeta de instalación
   - Clic en "Instalar"
4. **Windows se encarga de todo:**
   - Copiar archivos
   - Crear accesos directos
   - Registrar en Panel de Control
5. **¡Listo!** - Aparece en Menú Inicio

---

## 🔄 ACTUALIZACIÓN AUTOMÁTICA

### **Configurado en Package.wxs:**
```xml
<MajorUpgrade DowngradeErrorMessage="Ya existe una versión más reciente..."
              AllowSameVersionUpgrades="yes" />
```

### **Comportamiento:**
- Si el usuario ejecuta una nueva versión del MSI:
  - Windows **desinstala automáticamente** la versión anterior
  - **Instala la nueva versión** manteniendo configuración del usuario
  - **Sin pérdida de datos** (carpeta LocalAppData se mantiene)

---

## 🗑️ DESINSTALACIÓN

### **El usuario puede desinstalar desde:**

1. **Panel de Control:**
   - `Panel de Control` → `Programas y características`
   - Buscar "GestionTime Desktop"
   - Clic derecho → `Desinstalar`

2. **Configuración de Windows 10/11:**
   - `Configuración` → `Aplicaciones` → `Aplicaciones instaladas`
   - Buscar "GestionTime Desktop"
   - `...` → `Desinstalar`

3. **Menú Inicio:**
   - Carpeta "GestionTime Desktop"
   - Clic en "Desinstalar GestionTime Desktop"

### **Limpieza automática:**
- ✅ Elimina todos los archivos de instalación
- ✅ Elimina accesos directos (Menú Inicio + Escritorio)
- ✅ Limpia entradas del registro
- ⚠️ **Mantiene** los datos del usuario en `LocalAppData`

---

## 🧪 PRUEBAS

### **Probar el instalador:**

1. **Instalar:**
   ```powershell
   msiexec /i GestionTime-Desktop-v1.2.0-Setup.msi /l*v install.log
   ```

2. **Desinstalar:**
   ```powershell
   msiexec /x GestionTime-Desktop-v1.2.0-Setup.msi /l*v uninstall.log
   ```

3. **Instalación silenciosa (sin UI):**
   ```powershell
   msiexec /i GestionTime-Desktop-v1.2.0-Setup.msi /quiet /l*v silent-install.log
   ```

---

## 🎨 PERSONALIZACIÓN

### **Cambiar versión:**
Editar `Package.wxs` línea 9:
```xml
<?define ProductVersion = "1.3.0.0" ?>
```

### **Cambiar ubicación por defecto:**
```xml
<StandardDirectory Id="ProgramFiles64Folder">
  <Directory Id="CompanyFolder" Name="GestionTime Solutions">
    <Directory Id="INSTALLFOLDER" Name="GestionTime Desktop" />
  </Directory>
</StandardDirectory>
```

### **Agregar archivos adicionales:**
En `Package.wxs`, sección `<ComponentGroup Id="ApplicationFiles">`:
```xml
<Component Id="MiNuevoArchivo" Guid="NUEVO-GUID-UNICO">
  <File Id="MiArchivo" Source="$(var.PublishDir)\archivo.txt" />
</Component>
```

---

## 📚 RECURSOS

- **WiX Toolset v6 Docs:** https://wixtoolset.org/docs/intro/
- **HeatWave Extension:** https://marketplace.visualstudio.com/items?itemName=FireGiant.FireGiantHeatWaveDev17
- **Tutorial WiX:** https://www.firegiant.com/wix/tutorial/

---

## ✅ CHECKLIST DE COMPILACIÓN

- [ ] WiX Toolset v6.0 instalado
- [ ] HeatWave extension instalada en Visual Studio
- [ ] Proyecto agregado a la solución
- [ ] Archivos .wxs creados
- [ ] Versión actualizada en Package.wxs
- [ ] Script Build-Installer.ps1 ejecutado
- [ ] Archivo MSI generado exitosamente
- [ ] MSI probado en máquina limpia
- [ ] Instalación y desinstalación verificadas

---

**🎉 ¡Instalador MSI profesional listo para distribución!**
