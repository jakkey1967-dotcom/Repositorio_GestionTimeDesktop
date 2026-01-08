# 📄 CÓMO INCLUIR WINDOW-CONFIG.INI EN EL MSI

**Ubicación del archivo:** `C:\GestionTime\GestionTimeDesktop\Installer\window-config.ini`  
**Proceso:** El script copia automáticamente el archivo a `bin\` antes de crear el MSI

---

## ✅ **SOLUCIÓN IMPLEMENTADA**

### **Problema Anterior:**
El script buscaba `window-config.ini` solo en el directorio `bin\`, pero tú querías usar el archivo personalizado de `Installer\`.

### **Solución:**
El script **ahora copia automáticamente** el archivo de `Installer\` a `bin\` antes de generar el MSI.

---

## 🔄 **PROCESO AUTOMÁTICO**

### **Paso 1: Script Detecta el Archivo**
```powershell
$customWindowConfig = "$projectDir\Installer\window-config.ini"

if (Test-Path $customWindowConfig) {
    Write-Host "   Copiando window-config.ini personalizado a bin..." -ForegroundColor Cyan
    Copy-Item -Path $customWindowConfig -Destination "$binDir\window-config.ini" -Force
    Write-Host "   ✓ window-config.ini copiado" -ForegroundColor Green
}
```

### **Paso 2: Script Recopila Archivos**
```powershell
$allFiles = Get-ChildItem -Path $binDir -File -Recurse
# Ahora incluye window-config.ini copiado desde Installer\
```

### **Paso 3: Script Genera Componente con NeverOverwrite**
```xml
<Component Directory="INSTALLFOLDER">
  <File Source="...\bin\...\window-config.ini" 
        Name="window-config.ini" 
        NeverOverwrite="yes" />
</Component>
```

---

## 📂 **UBICACIONES DE ARCHIVOS**

### **Archivo Fuente (Tu Configuración):**
```
C:\GestionTime\GestionTimeDesktop\Installer\window-config.ini

# Contenido:
DiarioPage=1103,800
LoginPage=749,560
ParteItemEdit=1140,845
```

### **Copia Temporal en bin\ (Automática):**
```
C:\GestionTime\GestionTimeDesktop\bin\x64\Debug\net8.0-windows10.0.19041.0\window-config.ini

# Este archivo es copiado automáticamente por el script
# Se sobrescribe cada vez que ejecutas CREATE-MSI-COMPLETE.ps1
```

### **Archivo en MSI (Resultado Final):**
```
C:\Program Files\GestionTime\Desktop\window-config.ini

# Instalado desde el MSI con tus valores personalizados
# NeverOverwrite="yes" preserva configuración del usuario en actualizaciones
```

---

## 🚀 **CÓMO USAR**

### **1. Editar Configuración (Si Necesitas):**
```
Editar: C:\GestionTime\GestionTimeDesktop\Installer\window-config.ini

Cambiar valores:
DiarioPage=1200,900
LoginPage=800,600
ParteItemEdit=1200,900
```

### **2. Generar MSI:**
```
Método A - PowerShell ISE (Recomendado):
  1. Abrir PowerShell ISE como Administrador
  2. Abrir: C:\GestionTime\GestionTimeDesktop\CREATE-MSI-COMPLETE.ps1
  3. Presionar F5

Método B - PowerShell:
  1. Navegar a: C:\GestionTime\GestionTimeDesktop
  2. Shift + Click derecho → "Abrir ventana de PowerShell aquí"
  3. Ejecutar: .\CREATE-MSI-COMPLETE.ps1
```

### **3. Resultado:**
```
[1/5] Recopilando archivos...
   Copiando window-config.ini personalizado a bin...
   ✓ window-config.ini copiado
   Archivos encontrados: 153
   ✓ window-config.ini

[2/5] Generando componentes WiX...
   ✓ window-config.ini agregado con NeverOverwrite
   
MSI creado: Installer\Output\GestionTime-Desktop-1.2.0-Complete-Setup.msi
```

---

## ✅ **VERIFICACIÓN**

### **Después de Ejecutar el Script:**
```powershell
# Verificar que se copió a bin
Test-Path "C:\GestionTime\GestionTimeDesktop\bin\x64\Debug\net8.0-windows10.0.19041.0\window-config.ini"
# Debe devolver: True

# Ver contenido copiado
Get-Content "C:\GestionTime\GestionTimeDesktop\bin\x64\Debug\net8.0-windows10.0.19041.0\window-config.ini"
# Debe mostrar:
# DiarioPage=1103,800
# LoginPage=749,560
# ParteItemEdit=1140,845
```

### **Después de Instalar el MSI:**
```powershell
# Verificar en instalación
Test-Path "C:\Program Files\GestionTime\Desktop\window-config.ini"
# Debe devolver: True

# Ver contenido instalado
Get-Content "C:\Program Files\GestionTime\Desktop\window-config.ini"
# Debe mostrar los mismos valores
```

---

## 🎯 **FLUJO COMPLETO**

```
1. TU ARCHIVO (Fuente)
   ↓
   Installer\window-config.ini
   (Tu configuración personalizada)

2. COPIA AUTOMÁTICA (Script)
   ↓
   bin\x64\Debug\...\window-config.ini
   (Copia temporal para MSI)

3. MSI GENERADO
   ↓
   Installer\Output\GestionTime-Desktop-1.2.0-Complete-Setup.msi
   (Incluye window-config.ini con NeverOverwrite)

4. INSTALACIÓN
   ↓
   C:\Program Files\GestionTime\Desktop\window-config.ini
   (Archivo final con tu configuración)
```

---

## 📝 **VENTAJAS DE ESTE ENFOQUE**

✅ **Archivo Fuente Separado:**
- `Installer\window-config.ini` es tu fuente de verdad
- Fácil de editar y versionar en Git
- No se mezcla con archivos de compilación

✅ **Copia Automática:**
- El script copia automáticamente a `bin\`
- No necesitas copiar manualmente
- Siempre usa la versión más reciente

✅ **NeverOverwrite:**
- Primera instalación: Usa tus valores
- Actualizaciones: Preserva configuración del usuario
- Reset manual: Usuario elimina el archivo y se regenera

✅ **Versionable:**
- `Installer\window-config.ini` está en Git
- Cambios quedan registrados
- Fácil rollback si es necesario

---

## 🔧 **MODIFICAR CONFIGURACIÓN INICIAL**

### **Para cambiar valores por defecto:**

```
1. Editar archivo fuente:
   Notepad: C:\GestionTime\GestionTimeDesktop\Installer\window-config.ini

2. Modificar valores:
   DiarioPage=1200,900   # Cambiar de 1103,800
   LoginPage=800,600     # Cambiar de 749,560
   ParteItemEdit=1200,900 # Cambiar de 1140,845

3. Guardar cambios

4. Regenerar MSI:
   .\CREATE-MSI-COMPLETE.ps1

5. Nuevo MSI tendrá los nuevos valores por defecto
```

---

## 📊 **COMPARACIÓN**

| Método | Antes | Ahora ⭐ |
|--------|-------|---------|
| **Ubicación fuente** | Solo en bin\ | En Installer\ |
| **Copia a bin\** | Manual | ✅ Automática |
| **Versionable en Git** | ⚠️ Difícil | ✅ Fácil |
| **Modificar valores** | Editar en bin\ | Editar en Installer\ |
| **Regenerar MSI** | Buscar archivo | ✅ Script lo encuentra |

---

## ✅ **RESUMEN**

**Ubicación del archivo:**  
`C:\GestionTime\GestionTimeDesktop\Installer\window-config.ini`

**Proceso:**  
Script copia automáticamente a `bin\` y lo incluye en MSI

**Ventaja:**  
Archivo fuente separado, fácil de editar y versionar

**Resultado:**  
MSI con tu configuración personalizada y NeverOverwrite

**¡No necesitas mover el archivo manualmente!** 🎉

---

*Proceso de Inclusión de window-config.ini - GestionTime Desktop v1.2.0 - 08/01/2026*
