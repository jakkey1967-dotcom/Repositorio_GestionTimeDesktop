# 🚀 CREAR INSTALADOR MSI/MSIX - GUÍA DEFINITIVA

**Versión:** 1.2.0  
**Fecha:** Enero 2026  
**Objetivo:** Crear instalador profesional tipo MSI para GestionTime Desktop

---

## ✅ SOLUCIÓN RECOMENDADA: MSIX (MSI Moderno)

**MSIX** es el **reemplazo moderno de MSI** creado por Microsoft. Es más fácil de crear y **NO requiere WiX Toolset**.

### **Ventajas de MSIX sobre MSI tradicional:**
- ✅ **NO requiere WiX Toolset** (instalación compleja)
- ✅ **Integrado en .NET 8** (ya lo tienes instalado)
- ✅ **Formato oficial de Microsoft** (Windows 10/11)
- ✅ **Instalación más limpia** (sandboxing)
- ✅ **Menor tamaño** (~40 MB vs ~52 MB)
- ✅ **Más rápido de crear** (1 comando)

---

## 🎯 OPCIÓN 1: CREAR MSIX CON VISUAL STUDIO (MÁS FÁCIL)

### **Paso 1: Abrir el Proyecto**

```
1. Abrir Visual Studio 2022
2. File → Open → Project/Solution
3. Seleccionar: C:\GestionTime\GestionTimeDesktop\GestionTime.Desktop.sln
```

### **Paso 2: Crear Paquete MSIX**

```
1. Click derecho en proyecto "GestionTime.Desktop"
2. Seleccionar "Publish"
3. Click en "Create App Packages"
4. Seleccionar:
   ✓ Sideloading
   ✓ Arquitectura: x64
   ✓ Version: 1.2.0.0
5. Click "Create"
6. Esperar 2-3 minutos
```

### **Paso 3: Resultado**

Visual Studio creará:

```
C:\GestionTime\GestionTimeDesktop\AppPackages\
└── GestionTime.Desktop_1.2.0.0_x64_Test\
    ├── GestionTime.Desktop_1.2.0.0_x64.msix  ← INSTALADOR
    ├── GestionTime.Desktop_1.2.0.0_x64.cer   ← Certificado
    ├── Install.ps1                            ← Script de instalación
    └── Dependencies\
        └── x64\
            └── Microsoft.WindowsAppRuntime.1.8.msix
```

### **Paso 4: Instalar**

**Opción A - Doble-clic (recomendado):**
```
1. Hacer doble-clic en: GestionTime.Desktop_1.2.0.0_x64.msix
2. Click en "Instalar"
3. (Si pide certificado) Click en "Más información" → "Instalar de todos modos"
4. Buscar "GestionTime Desktop" en Menú Inicio
```

**Opción B - PowerShell (con dependencias):**
```powershell
cd "C:\GestionTime\GestionTimeDesktop\AppPackages\GestionTime.Desktop_1.2.0.0_x64_Test"
.\Install.ps1
```

---

## 🎯 OPCIÓN 2: CREAR MSIX CON SCRIPT POWERSHELL

### **Paso 1: Ejecutar Script**

```powershell
# Abrir PowerShell como Administrador
cd C:\GestionTime\GestionTimeDesktop

# Permitir ejecución de scripts (solo una vez)
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force

# Ejecutar script
.\CREATE-MSIX-INSTALLER.ps1
```

### **Paso 2: Resultado**

```
═══════════════════════════════════════════════════════
  ✅ PAQUETE MSIX GENERADO EXITOSAMENTE
═══════════════════════════════════════════════════════

📦 ARCHIVO MSIX:
   AppPackages\GestionTime.Desktop_1.2.0.0_x64\
   GestionTime.Desktop_1.2.0.0_x64.msix

📊 TAMAÑO: 42.5 MB
```

---

## 🎯 OPCIÓN 3: SI PREFIERES MSI TRADICIONAL (Requiere WiX)

### **Instalar WiX Toolset**

1. **Descargar WiX:**
   ```
   https://wixtoolset.org/releases/
   ```

2. **Instalar:**
   ```
   • Descargar: wix314.exe
   • Ejecutar instalador
   • Aceptar opciones por defecto
   • Reiniciar PowerShell
   ```

3. **Verificar:**
   ```powershell
   & "C:\Program Files (x86)\WiX Toolset v3.14\bin\candle.exe" -?
   ```

### **Crear MSI**

```powershell
cd C:\GestionTime\GestionTimeDesktop
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force
.\CREATE-MSI-INSTALLER-COMPLETE.ps1
```

**Resultado:**
```
Installer\Output\GestionTime-Desktop-1.2.0-Setup.msi (52 MB)
```

---

## 📊 COMPARACIÓN RÁPIDA

| Característica | MSIX ⭐ | MSI (WiX) | EXE (Inno) |
|---------------|---------|-----------|------------|
| **Facilidad** | 🟢 Muy fácil | 🔴 Difícil | 🟢 Fácil |
| **Requiere instalar** | Nada | WiX Toolset | Inno Setup |
| **Tamaño** | ~40 MB | ~52 MB | ~52 MB |
| **Compatible con** | Win10+/11 | Win XP+ | Win XP+ |
| **Tipo de archivo** | `.msix` | `.msi` | `.exe` |
| **Instalación limpia** | ✅ Sí | ⚠️ Parcial | ⚠️ Parcial |

---

## 🚀 INSTALACIÓN DEL PAQUETE MSIX

### **Método 1: Doble-clic (Usuario final)**

```
1. Doble-clic en: GestionTime.Desktop_1.2.0.0_x64.msix
2. Click "Instalar"
3. Esperar 30 segundos
4. Buscar "GestionTime Desktop" en Menú Inicio
```

### **Método 2: PowerShell (Con script)**

```powershell
# Navegar al directorio del paquete
cd "AppPackages\GestionTime.Desktop_1.2.0.0_x64_Test"

# Ejecutar script de instalación
.\Install.ps1
```

### **Método 3: PowerShell (Manual)**

```powershell
# Instalar dependencias primero
Add-AppxPackage "Dependencies\x64\Microsoft.WindowsAppRuntime.1.8.msix"

# Instalar aplicación
Add-AppxPackage "GestionTime.Desktop_1.2.0.0_x64.msix"
```

---

## ⚠️ SOLUCIÓN: "Certificado no confiable"

Si Windows muestra advertencia de certificado:

### **Opción A: Confiar en el certificado (Desarrollo)**

```powershell
# Importar certificado
certutil -addstore TrustedPeople "GestionTime.Desktop_1.2.0.0_x64.cer"

# Luego instalar
Add-AppxPackage "GestionTime.Desktop_1.2.0.0_x64.msix"
```

### **Opción B: Instalar de todos modos**

```
1. Click en "Más información"
2. Click en "Instalar de todos modos"
```

### **Opción C: Firmar con certificado de código (Producción)**

```powershell
# Comprar certificado de código de una CA confiable
# Firmar el MSIX
SignTool sign /f MyCert.pfx /p password /fd SHA256 GestionTime.Desktop.msix
```

---

## 🗑️ DESINSTALACIÓN

### **Desde Windows:**

```
Configuración → Aplicaciones → GestionTime Desktop → Desinstalar
```

### **Desde PowerShell:**

```powershell
Get-AppxPackage *GestionTime* | Remove-AppxPackage
```

---

## ✅ VERIFICACIÓN POST-INSTALACIÓN

```powershell
# Verificar que está instalado
Get-AppxPackage | Where-Object { $_.Name -like "*GestionTime*" }

# Resultado esperado:
# Name              : GestionTime.Desktop
# Version           : 1.2.0.0
# Architecture      : X64
# Publisher         : CN=Global Retail Solutions
# InstallLocation   : C:\Program Files\WindowsApps\...
```

---

## 📋 RESUMEN DE ARCHIVOS CREADOS

Después de seguir esta guía, tendrás:

```
✅ CREATE-MSIX-INSTALLER.ps1          ← Script automático MSIX
✅ CREATE-MSI-INSTALLER-COMPLETE.ps1  ← Script automático MSI (WiX)
✅ CREATE-INSTALLER-COMPLETE-V3.ps1   ← Script automático EXE (Inno)

📁 AppPackages\                       ← Paquetes MSIX generados
   └── GestionTime.Desktop_1.2.0.0_x64_Test\
       └── GestionTime.Desktop_1.2.0.0_x64.msix

📁 Installer\Output\                  ← Instaladores MSI/EXE
   ├── GestionTime-Desktop-1.2.0-Setup.msi  (si usas WiX)
   └── GestionTime-Desktop-1.2.0-Setup.exe  (si usas Inno)

📄 README-MSI-VS-MSIX.md              ← Comparación detallada
📄 README-INSTALADOR-COMPLETO.md      ← Guía EXE (Inno Setup)
📄 README-MSI.md                      ← Guía MSI (WiX)
```

---

## 🎯 RECOMENDACIÓN FINAL

### **Para Windows 11 / Windows 10 1809+:**
```
👉 Usar MSIX (Opción 1 o 2)
   • Más fácil
   • Más rápido
   • Más limpio
   • NO requiere WiX
```

### **Para versiones antiguas de Windows o empresas con GPO:**
```
👉 Usar MSI con WiX (Opción 3)
   • Compatible con todas las versiones
   • Group Policy deployment
   • Estándar corporativo
```

### **Para distribución pública:**
```
👉 Usar EXE con Inno Setup
   • Sin advertencias de certificado
   • Interfaz amigable
   • Script: CREATE-INSTALLER-COMPLETE-V3.ps1
```

---

## 📞 SOPORTE

**Email:** soporte@gestiontime.com  
**Tel:** +34 900 123 456  
**GitHub:** https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop

---

## 📄 DOCUMENTACIÓN ADICIONAL

- `Installer/README-MSI-VS-MSIX.md` - Comparación detallada
- `Installer/README-MSI.md` - Guía MSI con WiX
- `Installer/README-INSTALADOR-COMPLETO.md` - Guía EXE con Inno Setup

---

**🎯 ¡Instalador MSI/MSIX listo para distribuir!**

*Guía Definitiva MSI/MSIX - Versión 1.2.0 - Enero 2026*
