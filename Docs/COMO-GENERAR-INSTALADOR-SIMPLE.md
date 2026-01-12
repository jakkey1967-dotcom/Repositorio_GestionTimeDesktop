# 🚀 Cómo Generar el Instalador - GestionTime Desktop

**Versión:** 1.2.0  
**Última actualización:** Enero 2026

---

## ⚡ MÉTODO RÁPIDO (Recomendado)

### **Opción 1: Instalador Portable (ZIP)**

✅ **MÁS FÁCIL** - No requiere instalación de herramientas adicionales

```
1. Doble clic en: GENERAR-INSTALADOR-PORTABLE.bat
2. Esperar 1-2 minutos
3. Se abrirá el explorador con el ZIP generado
```

**Resultado:**  
`Installer\Output\GestionTime-Desktop-1.2.0-Portable.zip` (~45 MB)

**Ventajas:**
- ✅ No requiere WiX Toolset ni herramientas extra
- ✅ Solo necesita .NET SDK 8 (ya instalado)
- ✅ Genera un ZIP listo para distribuir
- ✅ Los usuarios solo extraen y ejecutan
- ✅ Funciona en cualquier carpeta

---

### **Opción 2: Instalador MSIX (Moderno)**

✅ **RECOMENDADO para Windows 10/11** - Requiere Visual Studio

```
1. Doble clic en: GENERAR-MSIX-VISUAL-STUDIO.bat
2. Visual Studio se abrirá automáticamente
3. Seguir las instrucciones en pantalla:
   • Click derecho en proyecto "GestionTime.Desktop"
   • Publish > Create App Packages
   • Sideloading > x64 > Create
4. Esperar 2-3 minutos
```

**Resultado:**  
`AppPackages\GestionTime.Desktop_1.2.0.0_x64_Test\GestionTime.Desktop_1.2.0.0_x64.msix`

**Ventajas:**
- ✅ Instalador MSI moderno de Microsoft
- ✅ Integración con Windows 10/11
- ✅ Instalación/desinstalación limpia
- ✅ Actualizaciones automáticas
- ✅ Sandboxing de seguridad

---

## 📋 Requisitos

### Para Opción 1 (Portable):
- ✅ .NET SDK 8 - [Descargar](https://dotnet.microsoft.com/download/dotnet/8.0)
- ✅ PowerShell (incluido en Windows)

### Para Opción 2 (MSIX):
- ✅ Visual Studio 2022 (Community o superior)
- ✅ Workload: ".NET Desktop Development"
- ✅ Workload: "Universal Windows Platform development"

---

## 📦 ¿Qué método usar?

| Situación | Método Recomendado |
|-----------|-------------------|
| **Desarrollo interno / Testing** | Portable (ZIP) |
| **Distribución a usuarios finales Windows 10/11** | MSIX |
| **Necesitas algo rápido YA** | Portable (ZIP) |
| **Instalación corporativa / GPO** | MSI tradicional (WiX)* |
| **Compatibilidad con Windows 7/8** | EXE (Inno Setup)* |

\*Requiere instalación de herramientas adicionales (ver abajo)

---

## 🛠️ Métodos Avanzados

### **Opción 3: MSI Tradicional (con WiX Toolset)**

**Requisitos adicionales:**
1. Descargar e instalar: [WiX Toolset v3.14](https://wixtoolset.org/releases/)
2. Reiniciar PowerShell después de instalar

**Ejecutar:**
```powershell
.\CREATE-MSI-INSTALLER-COMPLETE.ps1
```

**Resultado:**  
`Installer\Output\GestionTime-Desktop-1.2.0-Setup.msi` (~52 MB)

**Ventajas:**
- ✅ Compatible con Windows XP+
- ✅ Group Policy deployment
- ✅ Estándar corporativo
- ❌ Requiere WiX Toolset (difícil de instalar)

---

### **Opción 4: EXE con Inno Setup**

**Requisitos adicionales:**
1. Descargar e instalar: [Inno Setup](https://jrsoftware.org/isinfo.php)

**Ejecutar:**
```powershell
.\CREATE-INSTALLER-COMPLETE-V3.ps1
```

**Resultado:**  
`Installer\Output\GestionTime-Desktop-1.2.0-Setup.exe` (~52 MB)

**Ventajas:**
- ✅ Interfaz de instalación amigable
- ✅ Sin advertencias de certificado
- ✅ Compatible con todas las versiones de Windows
- ❌ Requiere Inno Setup

---

## 📂 Estructura de Archivos

Después de generar los instaladores:

```
GestionTimeDesktop/
│
├── GENERAR-INSTALADOR-PORTABLE.bat       ⭐ Ejecuta esto
├── GENERAR-INSTALADOR-PORTABLE.ps1       (Script PowerShell)
├── GENERAR-MSIX-VISUAL-STUDIO.bat        ⭐ O esto
├── GENERAR-MSIX-VISUAL-STUDIO.ps1        (Script PowerShell)
│
├── Installer/
│   └── Output/
│       ├── GestionTime-Desktop-1.2.0-Portable.zip      (Portable)
│       ├── GestionTime-Desktop-1.2.0-Setup.msi         (MSI)
│       └── GestionTime-Desktop-1.2.0-Setup.exe         (EXE)
│
└── AppPackages/
    └── GestionTime.Desktop_1.2.0.0_x64_Test/
        └── GestionTime.Desktop_1.2.0.0_x64.msix        (MSIX)
```

---

## ❓ Problemas Comunes

### Error: ".NET SDK no instalado"
**Solución:**
1. Descargar: https://dotnet.microsoft.com/download/dotnet/8.0
2. Instalar el SDK (no solo runtime)
3. Reiniciar PowerShell

### Error: "Visual Studio no encontrado"
**Solución:**
- Usa **Opción 1 (Portable)** que no requiere Visual Studio

### Error de compilación
**Solución:**
1. Abrir Visual Studio
2. Abrir `GestionTime.Desktop.sln`
3. Build > Clean Solution
4. Build > Rebuild Solution
5. Si compila sin errores, volver a ejecutar el script

### El MSIX da error de "Certificado no confiable"
**Solución:**
1. Click en "Más información"
2. Click en "Instalar de todos modos"
3. Es normal en desarrollo (sin certificado de código comercial)

---

## 📞 Soporte

- **Email:** soporte@gestiontime.com
- **GitHub:** https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop
- **Documentación:** Ver carpeta `Docs/`

---

## ✅ Verificación

Para verificar que el instalador funciona:

### ZIP/Portable:
```
1. Extraer el ZIP en una carpeta de prueba
2. Ejecutar: GestionTime.Desktop.exe
3. Verificar que la app inicia correctamente
```

### MSIX:
```
1. Doble-clic en el archivo .msix
2. Click "Instalar"
3. Buscar "GestionTime Desktop" en Menú Inicio
4. Ejecutar y verificar funcionalidad
```

### MSI/EXE:
```
1. Ejecutar el instalador
2. Seguir el asistente de instalación
3. Al finalizar, ejecutar desde Menú Inicio
```

---

## 🎯 Recomendación Final

**Para el 90% de los casos:**

```
1. Ejecuta: GENERAR-INSTALADOR-PORTABLE.bat
2. Distribuye el ZIP generado
3. Los usuarios extraen y ejecutan
```

**Simple, rápido y funciona siempre** ✅

---

**Última actualización:** Enero 2026  
**Versión de la aplicación:** 1.2.0
