# 📄 WINDOW-CONFIG.INI EN EL INSTALADOR MSI

**Archivo:** window-config.ini  
**Propósito:** Almacenar configuración de ventanas (posición, tamaño, estado)  
**Ubicación en MSI:** Raíz de la instalación

---

## 📋 **QUÉ ES WINDOW-CONFIG.INI**

### **Propósito:**
Archivo de configuración que guarda:
- Posición de la ventana principal (X, Y)
- Tamaño de la ventana (Width, Height)
- Estado de la ventana (Maximizada, Normal, Minimizada)
- Última configuración guardada

### **Ejemplo de Contenido:**
```ini
[MainWindow]
Width=1200
Height=800
X=100
Y=100
State=Normal
LastSaved=2026-01-08 12:45:30
```

### **Uso en la Aplicación:**
```csharp
// Services\WindowConfigService.cs
public class WindowConfigService
{
    private readonly string _configPath = 
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "window-config.ini");
    
    public void SaveWindowPosition(double x, double y, double width, double height)
    {
        // Guarda configuración en window-config.ini
    }
    
    public WindowConfig LoadWindowPosition()
    {
        // Lee configuración desde window-config.ini
    }
}
```

---

## 📂 **UBICACIÓN EN EL MSI**

### **Dónde se instala:**
```
C:\Program Files\GestionTime\Desktop\
├── GestionTime.Desktop.exe
├── window-config.ini          ⭐ AQUÍ (en la raíz)
├── appsettings.json
├── resources.pri
├── Assets\
├── Views\
└── ...
```

### **Por qué en la raíz:**
- ✅ Fácil acceso desde `AppDomain.CurrentDomain.BaseDirectory`
- ✅ No requiere rutas relativas complejas
- ✅ Estándar para archivos de configuración de aplicación
- ✅ Persistente entre actualizaciones (si se configura correctamente)

---

## ✅ **CÓMO SE INCLUYE EN EL MSI**

### **Proceso Automático:**

**1. El script `CREATE-MSI-COMPLETE.ps1` detecta el archivo:**
```powershell
# [1/5] Recopilando archivos...
$allFiles = Get-ChildItem -Path $binDir -File -Recurse

# Incluye window-config.ini automáticamente
```

**2. Verifica que exista:**
```powershell
$criticalFiles = @(
    "GestionTime.Desktop.exe",
    "resources.pri",
    "window-config.ini",  # ⭐ Verificado
    "appsettings.json"
)

foreach ($criticalFile in $criticalFiles) {
    $found = $allFiles | Where-Object { $_.Name -eq $criticalFile }
    if ($found) {
        Write-Host "   ✓ $criticalFile" -ForegroundColor Green
    }
}
```

**3. Lo asigna a INSTALLFOLDER (raíz):**
```powershell
# Detecta que está en la raíz de bin\
$fileDir = Split-Path $file.FullName -Parent
# $fileDir == C:\...\bin\x64\Debug\net8.0-windows10.0.19041.0

# Lo mapea a INSTALLFOLDER
$targetDirId = $dirMap[$fileDir]  # "INSTALLFOLDER"
```

**4. Genera componente WiX:**
```xml
<Component Id="Cmp15" Directory="INSTALLFOLDER" Guid="...">
  <File Id="File15" 
        Source="...\window-config.ini" 
        Name="window-config.ini" 
        KeyPath="yes" />
</Component>
```

---

## 🔍 **VERIFICAR QUE SE INCLUYE**

### **Durante la Compilación del MSI:**
```powershell
cd C:\GestionTime\GestionTimeDesktop
.\CREATE-MSI-COMPLETE.ps1

# Salida esperada:
# [1/5] Recopilando archivos...
#    Archivos encontrados: 153
#    ✓ GestionTime.Desktop.exe
#    ✓ resources.pri
#    ✓ window-config.ini           ⭐ DEBE APARECER
#    ✓ appsettings.json
```

### **Después de la Instalación:**
```powershell
# Verificar que el archivo se instaló
$installDir = "C:\Program Files\GestionTime\Desktop"
Test-Path "$installDir\window-config.ini"
# Debe devolver: True

# Ver contenido
Get-Content "$installDir\window-config.ini"
```

---

## 📝 **COMPORTAMIENTO EN TIEMPO DE EJECUCIÓN**

### **Primera Ejecución:**
```
1. GestionTime.Desktop.exe inicia
2. WindowConfigService busca window-config.ini
3. Si existe: Lee configuración guardada
4. Si NO existe: Usa valores por defecto
5. Al cerrar: Guarda configuración actual
```

### **Actualizaciones del MSI:**
```
Opción A: Preservar configuración existente
  - WiX puede configurarse para NO sobrescribir
  - La configuración del usuario se mantiene

Opción B: Reset en actualizaciones
  - Se instala window-config.ini limpio
  - Usuario pierde posición de ventana guardada
```

**Configuración actual en WiX:**
```xml
<!-- Por defecto, WiX sobrescribe archivos en actualizaciones -->
<File Source="window-config.ini" />

<!-- Para NO sobrescribir (preservar configuración usuario): -->
<File Source="window-config.ini" NeverOverwrite="yes" />
```

---

## 🎯 **SI QUIERES PRESERVAR CONFIGURACIÓN**

### **Modificar CREATE-MSI-COMPLETE.ps1:**

Si quieres que el MSI **NO sobrescriba** `window-config.ini` en actualizaciones:

```powershell
# En el bucle de generación de componentes
foreach ($file in $allFiles) {
    # ...
    
    # Caso especial para window-config.ini
    if ($file.Name -eq "window-config.ini") {
        [void]$componentsXml.AppendLine("      <Component Id=`"Cmp$componentId`" Directory=`"$targetDirId`" Guid=`"$guid`">")
        [void]$componentsXml.AppendLine("        <File Id=`"$uniqueFileId`" Source=`"$($file.FullName)`" Name=`"$($file.Name)`" KeyPath=`"yes`" NeverOverwrite=`"yes`" />")
        [void]$componentsXml.AppendLine("      </Component>")
    } else {
        # Archivos normales
        [void]$componentsXml.AppendLine("      <Component Id=`"Cmp$componentId`" Directory=`"$targetDirId`" Guid=`"$guid`">")
        [void]$componentsXml.AppendLine("        <File Id=`"$uniqueFileId`" Source=`"$($file.FullName)`" Name=`"$($file.Name)`" KeyPath=`"yes`" />")
        [void]$componentsXml.AppendLine("      </Component>")
    }
}
```

**Efecto:**
- ✅ Primera instalación: Se copia window-config.ini
- ✅ Usuario modifica configuración (posición ventana)
- ✅ Actualización a v1.3.0: window-config.ini NO se sobrescribe
- ✅ Configuración del usuario se preserva

---

## 📊 **ESTRUCTURA COMPLETA EN MSI**

```
GestionTime-Desktop-1.2.0-Complete-Setup.msi (16.32 MB)
└── INSTALLFOLDER (C:\Program Files\GestionTime\Desktop\)
    ├── GestionTime.Desktop.exe
    ├── window-config.ini         ⭐ Configuración de ventana
    ├── appsettings.json          (Configuración de app)
    ├── resources.pri             (Recursos WinUI 3)
    ├── Assets\
    ├── Views\
    ├── Controls\
    └── runtimes\
```

---

## ✅ **RESUMEN**

**Estado actual:**
- ✅ `window-config.ini` se incluye automáticamente en el MSI
- ✅ Se instala en la raíz: `C:\Program Files\GestionTime\Desktop\window-config.ini`
- ✅ La aplicación puede leer/escribir desde `AppDomain.CurrentDomain.BaseDirectory`
- ✅ Verificación explícita en el script de compilación

**Ubicación en instalación:**
```
C:\Program Files\GestionTime\Desktop\window-config.ini
```

**Propósito:**
- Guardar posición y tamaño de la ventana principal
- Persistir configuración entre ejecuciones
- Restaurar estado de la ventana al reiniciar

**Comportamiento:**
- Primera instalación: Se copia desde bin\
- Actualizaciones: Se sobrescribe (por defecto)
- Para preservar: Agregar `NeverOverwrite="yes"` en WiX

**¡El archivo ya está incluido correctamente en el MSI!** ✅

---

*window-config.ini en MSI - GestionTime Desktop v1.2.0 - 08/01/2026*
