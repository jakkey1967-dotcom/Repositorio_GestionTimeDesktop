# 🚀 CÓMO GENERAR EL MSI - GUÍA DEFINITIVA

**Fecha:** 08/01/2026 14:15  
**Estado:** ✅ Scripts corregidos y funcionando

---

## ✅ **SCRIPTS DISPONIBLES**

### **Opción 1: EJECUTAR-MSI.bat** ⭐ Recomendado
```
Ubicación: C:\GestionTime\GestionTimeDesktop\EJECUTAR-MSI.bat
Uso: Doble-clic
Qué hace: Abre PowerShell ISE con CREATE-MSI-COMPLETE.ps1 cargado
```

### **Opción 2: ABRIR-EN-ISE.ps1**
```
Ubicación: C:\GestionTime\GestionTimeDesktop\ABRIR-EN-ISE.ps1
Uso: Click derecho → "Ejecutar con PowerShell"
Qué hace: Lo mismo que Opción 1 pero desde PowerShell
```

---

## 🎯 **PASOS PARA GENERAR EL MSI**

### **Método Más Fácil (Recomendado):**

```
1. Navegar a: C:\GestionTime\GestionTimeDesktop\

2. Hacer doble-clic en: EJECUTAR-MSI.bat

3. PowerShell ISE se abrirá automáticamente con el script cargado

4. En PowerShell ISE, presionar F5 (o click en ▶ "Run Script")

5. Esperar 1-2 minutos mientras se genera el MSI

6. Se abrirá el explorador mostrando el resultado:
   Installer\Output\GestionTime-Desktop-1.2.0-Complete-Setup.msi
```

---

## 📋 **QUÉ HACE EL SCRIPT**

```
[1/5] Recopilando archivos...
  • Copia window-config.ini de Installer\ a bin\
  • Encuentra 153 archivos

[2/5] Generando componentes WiX con estructura de directorios...
  • Detecta 31 carpetas
  • Genera definiciones XML para cada carpeta
  • Asigna cada archivo a su carpeta correcta
  • Marca window-config.ini con NeverOverwrite

[3/5] Creando archivo WiX completo...
  • Genera archivo temporal .wxs con todos los componentes

[4/5] Compilando MSI...
  • Ejecuta: wix.exe build
  • Compila el MSI con compresión alta
  • Incluye UI de instalación con selección de carpeta

[5/5] Verificando MSI...
  • Verifica que el MSI se creó correctamente
  • Abre explorador en el archivo generado
```

---

## 📦 **RESULTADO ESPERADO**

```
===============================================
  MSI COMPLETO CREADO EXITOSAMENTE
===============================================

ARCHIVO:
  C:\GestionTime\GestionTimeDesktop\Installer\Output\
  GestionTime-Desktop-1.2.0-Complete-Setup.msi

TAMAÑO: ~16 MB

ARCHIVOS INCLUIDOS: 153 archivos
  - Ejecutable principal
  - Todas las DLLs (72 DLLs)
  - Assets y configuración
  - Runtimes nativos

Estructura de carpetas incluida:
  ✅ Assets\
  ✅ Controls\
  ✅ Views\
  ✅ runtimes\win-x64\native\
  ✅ logs\
  ✅ window-config.ini (personalizado, NeverOverwrite)
```

---

## 🧪 **VERIFICAR DESPUÉS DE INSTALAR**

```powershell
# 1. Verificar carpetas
Test-Path "C:\Program Files\GestionTime\Desktop\Assets"
Test-Path "C:\Program Files\GestionTime\Desktop\Views"
Test-Path "C:\Program Files\GestionTime\Desktop\Controls"
Test-Path "C:\Program Files\GestionTime\Desktop\runtimes\win-x64\native"

# 2. Verificar configuración personalizada
Get-Content "C:\Program Files\GestionTime\Desktop\window-config.ini"
# Debe mostrar:
# DiarioPage=1103,800
# LoginPage=749,560
# ParteItemEdit=1140,845

# 3. Ejecutar la aplicación
& "C:\Program Files\GestionTime\Desktop\GestionTime.Desktop.exe"
# Debe arrancar sin errores ✅
```

---

## ⚠️ **SOLUCIÓN DE PROBLEMAS**

### **Si EJECUTAR-MSI.bat no abre PowerShell ISE:**

```
Método alternativo:

1. Abrir PowerShell ISE manualmente:
   - Buscar "PowerShell ISE" en el menú Inicio
   - Click derecho → "Ejecutar como administrador"

2. En PowerShell ISE:
   - File → Open
   - Navegar a: C:\GestionTime\GestionTimeDesktop\
   - Abrir: CREATE-MSI-COMPLETE.ps1

3. Presionar F5
```

### **Si hay errores de compilación de WiX:**

```
Verificar que WiX Toolset v6.0 está instalado:
& "C:\Program Files\WiX Toolset v6.0\bin\wix.exe" --version

Si no está instalado, descargar desde:
https://github.com/wixtoolset/wix/releases
```

### **Si el MSI se genera pero no arranca:**

```
Verificar que todas las carpetas se instalaron:
Get-ChildItem "C:\Program Files\GestionTime\Desktop" -Directory

Debe mostrar:
  Assets
  Controls
  Views
  runtimes
  logs
```

---

## 📝 **ARCHIVOS RELACIONADOS**

```
C:\GestionTime\GestionTimeDesktop\
├── EJECUTAR-MSI.bat                    ← Doble-clic aquí ⭐
├── ABRIR-EN-ISE.ps1                    ← Alternativa
├── CREATE-MSI-COMPLETE.ps1             ← Script principal
├── Installer\
│   ├── window-config.ini                ← Configuración personalizada
│   ├── MSI\
│   │   ├── License.rtf                  ← Licencia mostrada en instalador
│   │   └── Product.wxs                  ← (No usado, solo referencia)
│   └── Output\
│       └── [MSI se genera aquí] ⭐
└── bin\x64\Debug\net8.0-windows10.0.19041.0\
    └── [153 archivos a incluir]
```

---

## 🎯 **RESUMEN RÁPIDO**

**Para generar el MSI:**
```
1. Doble-clic en: EJECUTAR-MSI.bat
2. En PowerShell ISE: Presionar F5
3. Esperar 1-2 minutos
4. MSI listo en: Installer\Output\
```

**Para instalar el MSI:**
```
1. Doble-clic en el MSI
2. Seleccionar carpeta (o dejar por defecto)
3. Click "Instalar"
4. Buscar "GestionTime Desktop" en Menú Inicio
```

**¡Listo!** 🎉

---

## ✅ **ESTADO FINAL**

✅ **EJECUTAR-MSI.bat corregido** - Sintaxis BAT correcta  
✅ **ABRIR-EN-ISE.ps1 creado** - Alternativa desde PowerShell  
✅ **CREATE-MSI-COMPLETE.ps1** - Funcional y probado  
✅ **Documentación completa** - Este README  

**¡Todo listo para generar el MSI!** 🚀

---

*Guía Definitiva MSI - 08/01/2026 14:15*
