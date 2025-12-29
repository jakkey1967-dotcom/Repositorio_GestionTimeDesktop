# ?? CREAR INSTALADOR MSIX PASO A PASO EN VISUAL STUDIO

Si el script automático no funciona, seguir estos pasos manuales:

---

## ?? MÉTODO 1: VISUAL STUDIO 2022 (RECOMENDADO)

### **Paso 1: Abrir el proyecto**
1. Abrir **Visual Studio 2022**
2. Abrir proyecto: `C:\GestionTime\GestionTime.Desktop\GestionTime.Desktop.csproj`
3. Asegurarse de que compile sin errores (Build ? Build Solution)

### **Paso 2: Crear App Package**
1. **Click derecho** en proyecto `GestionTime.Desktop` en Solution Explorer
2. Seleccionar **"Publish"** 
3. Click en **"Create App Packages..."**

### **Paso 3: Configurar el paquete**
1. Seleccionar **"Sideloading"** (distribución directa)
2. Click **"Next"**

### **Paso 4: Configuración de arquitectura**
1. **Desmarcar** ARM64 y x86 
2. **Dejar marcado solo** x64
3. **Configuration:** Release
4. Click **"Next"**

### **Paso 5: Versioning**
1. **Version:** 1.1.0.0 (o incrementar)
2. **Automatically increment:** Marcado
3. Click **"Next"**

### **Paso 6: Certificado**
1. **Si ya tienes certificado:** Seleccionarlo
2. **Si no tienes:** Click "Create..." para crear nuevo
   - **Publisher name:** GestionTime Solutions
   - **Common name:** GestionTime Desktop
   - Click "OK"
3. Click **"Create"**

### **Paso 7: Esperar creación**
- El proceso puede tomar 5-10 minutos
- Visual Studio descargará e incluirá Windows App Runtime automáticamente
- Se mostrarán los archivos creados al final

---

## ?? RESULTADO ESPERADO

El paquete se creará en:
```
C:\GestionTime\GestionTime.Desktop\AppPackages\GestionTime.Desktop_1.1.0.0_x64_Test\
```

### **Archivos creados:**
- `GestionTime.Desktop_1.1.0.0_x64.msix` ? **INSTALADOR PRINCIPAL**
- `Install.ps1` ? Script de instalación alternativo
- `GestionTime.Desktop_1.1.0.0_x64.cer` ? Certificado
- `Dependencies\x64\` ? Carpeta con Windows App Runtime incluido

---

## ?? MÉTODO 2: COMMAND LINE (ALTERNATIVO)

Si Visual Studio no está disponible:

### **Prerrequisitos:**
```powershell
# Instalar herramientas necesarias
winget install Microsoft.VisualStudio.2022.BuildTools
winget install Microsoft.WindowsSDK.10.0.22621
```

### **Crear paquete:**
```powershell
cd C:\GestionTime\GestionTime.Desktop

# Limpiar
dotnet clean --configuration Release

# Restaurar
dotnet restore

# Crear paquete MSIX
msbuild GestionTime.Desktop.csproj /p:Configuration=Release /p:Platform=x64 /p:AppxPackageDir=AppPackages\ /p:AppxBundle=Never /p:GenerateAppxPackageOnBuild=true
```

---

## ?? DISTRIBUCIÓN DEL INSTALADOR

### **Para enviar a usuarios:**

1. **Renombrar el archivo:**
   ```
   GestionTime.Desktop_1.1.0.0_x64.msix
   ?
   GestionTimeDesktop_install.msix
   ```

2. **Crear ZIP con instrucciones:**
   ```
   GestionTimeDesktop_Installer.zip
   ??? GestionTimeDesktop_install.msix
   ??? INSTRUCCIONES_INSTALACION.txt
   ??? certificado.cer (si es necesario)
   ```

### **Instrucciones para usuarios:**

```
INSTALACIÓN GESTIONTIME DESKTOP v1.1.0

1. Descomprimir GestionTimeDesktop_Installer.zip

2. Hacer DOBLE-CLICK en: GestionTimeDesktop_install.msix

3. Si aparece advertencia de certificado:
   - Click en "Más información"
   - Click en "Instalar de todas formas"

4. Seguir el asistente de instalación

5. Una vez instalado, buscar "GestionTime Desktop" en el menú inicio

6. ¡Listo! La aplicación ya incluye todas las dependencias necesarias

NOTA: Este instalador incluye automáticamente Windows App Runtime,
no es necesario instalarlo por separado.

Soporte: [tu-email]
```

---

## ? VENTAJAS DEL INSTALADOR MSIX

- ? **Incluye Windows App Runtime automáticamente**
- ? **Instalación con un solo click**
- ? **Actualizaciones automáticas**
- ? **Desinstalación limpia**
- ? **Sandboxing de seguridad**
- ? **Compatible con Windows Store**
- ? **No requiere scripts BAT complicados**

---

## ?? TROUBLESHOOTING

### **Error: "Package could not be registered"**
- Ejecutar PowerShell como Administrador:
```powershell
Add-AppxPackage -Path "ruta\al\archivo.msix" -ForceApplicationShutdown
```

### **Error: "Certificate not trusted"**
- Instalar certificado manualmente:
```powershell
Import-Certificate -FilePath "certificado.cer" -CertStoreLocation Cert:\LocalMachine\TrustedPeople
```

### **Error en Visual Studio**
- Verificar que Windows SDK 10.0.22621 esté instalado
- Verificar que .NET 8 SDK esté instalado
- Limpiar solución: Build ? Clean Solution

---

**¡Con este instalador MSIX, el problema de Windows App Runtime queda resuelto definitivamente!** ??