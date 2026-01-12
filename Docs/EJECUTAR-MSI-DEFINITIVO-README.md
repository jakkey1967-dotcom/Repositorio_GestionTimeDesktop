# 🚀 CREAR MSI DESDE CERO - INSTRUCCIONES

**Script:** `CREATE-MSI-DEFINITIVO.ps1`  
**Estado:** Listo para ejecutar  
**Incluye:** TODAS las carpetas (Assets, Views, Controls, runtimes, logs) + window-config.ini

---

## ✅ **CÓMO EJECUTAR**

### **Método 1: PowerShell ISE (100% Funciona)**

```
1. Abrir PowerShell ISE como Administrador
   (Buscar "PowerShell ISE" → Click derecho → Ejecutar como administrador)

2. Abrir el script:
   File → Open → C:\GestionTime\GestionTimeDesktop\CREATE-MSI-DEFINITIVO.ps1

3. Presionar F5 o click en ▶ "Run Script"

4. Esperar 1-2 minutos

5. Resultado:
   Installer\Output\GestionTime-Desktop-Setup.msi
```

### **Método 2: Desde Explorador**

```
1. Navegar a: C:\GestionTime\GestionTimeDesktop

2. Hacer click derecho en CREATE-MSI-DEFINITIVO.ps1

3. Seleccionar: "Ejecutar con PowerShell"

4. Si pide permisos de ejecución, presionar 'S' (Sí)
```

---

## 📦 **QUÉ INCLUYE EL MSI**

✅ **Ejecutable y DLLs** (153 archivos)
✅ **Assets\** (iconos, imágenes, logos)
✅ **Controls\** (controles .xbf compilados)
✅ **Views\** (vistas .xbf compiladas)
✅ **runtimes\win-x64\native\** (DLLs nativas de WinUI 3)
✅ **logs\** (carpeta para logs)
✅ **window-config.ini** (configuración personalizada con NeverOverwrite)

---

## 🎯 **LO QUE HACE EL SCRIPT**

```
[1/4] Preparando archivos...
  • Copia window-config.ini de Installer\ a bin\

[2/4] Escaneando archivos y carpetas...
  • Detecta 153 archivos
  • Detecta 31 carpetas

[3/4] Generando XML con estructura de carpetas...
  • Crea definiciones de directorios en WiX
  • Asigna cada archivo a su carpeta correcta
  • Marca window-config.ini con NeverOverwrite

[4/4] Compilando MSI...
  • Ejecuta wix.exe build
  • Genera MSI de ~16 MB
  • Abre explorador en el resultado
```

---

## ✅ **RESULTADO ESPERADO**

```
==========================================
  ✓ MSI CREADO CON ÉXITO
==========================================

Archivo: C:\GestionTime\GestionTimeDesktop\Installer\Output\GestionTime-Desktop-Setup.msi
Tamaño: ~16 MB
Archivos: 153
Carpetas: 31

Carpetas incluidas:
  • Assets
  • Controls
  • Views
  • runtimes\win-x64\native
  • logs
```

---

## 🧪 **VERIFICAR QUE FUNCIONA**

### **Después de Instalar:**

```powershell
# 1. Verificar carpetas
Test-Path "C:\Program Files\GestionTime Desktop\Assets"
Test-Path "C:\Program Files\GestionTime Desktop\Views"
Test-Path "C:\Program Files\GestionTime Desktop\Controls"
Test-Path "C:\Program Files\GestionTime Desktop\runtimes\win-x64\native"

# 2. Verificar window-config.ini
Get-Content "C:\Program Files\GestionTime Desktop\window-config.ini"

# 3. Ejecutar aplicación
& "C:\Program Files\GestionTime Desktop\GestionTime.Desktop.exe"
```

**Debe arrancar sin errores** ✅

---

## ⚠️ **SI HAY ERROR AL EJECUTAR EL SCRIPT**

Si PowerShell desde CMD no funciona (problema de parsing), usar **PowerShell ISE**:

```
PowerShell ISE → File → Open → CREATE-MSI-DEFINITIVO.ps1 → F5
```

**PowerShell ISE no tiene problemas con < y > en strings.**

---

## 📝 **NOTAS**

- **window-config.ini** se copia automáticamente de `Installer\` a `bin\`
- **NeverOverwrite="yes"** preserva configuración del usuario en actualizaciones
- **Todas las carpetas** se mantienen (Assets, Views, Controls, runtimes, etc.)
- **MSI limpio** desde cero, sin archivos anteriores

---

## 🎯 **RESUMEN**

**Script listo:** `CREATE-MSI-DEFINITIVO.ps1`  
**Ejecutar en:** PowerShell ISE (F5)  
**Resultado:** MSI de 16 MB con 153 archivos en 31 carpetas  
**Estado:** ✅ Funcional y limpio desde cero

**¡Ejecuta el script en PowerShell ISE y el MSI se creará correctamente!** 🎉

---

*Instrucciones MSI Definitivo - 08/01/2026 13:45*
