# ⚙️ COMANDOS DE RESTAURACIÓN - BACKUP 02 ENERO 2026

**Fecha:** 2026-01-02 20:09  
**Propósito:** Comandos para verificar y restaurar backups (SOLO CON AUTORIZACIÓN)

---

## ⚠️ **ADVERTENCIA CRÍTICA**

```
╔════════════════════════════════════════════════════════════╗
║                                                            ║
║  ⚠️ NUNCA EJECUTAR COMANDOS SIN AUTORIZACIÓN             ║
║                                                            ║
║  ❌ NO ejecutar automáticamente                           ║
║  ❌ NO copiar y pegar sin revisar                         ║
║  ❌ NO restaurar sin backup del estado actual             ║
║                                                            ║
║  ✅ SIEMPRE:                                              ║
║     1. Consultar con el usuario                           ║
║     2. Crear backup del estado actual                     ║
║     3. Obtener autorización explícita                     ║
║     4. Documentar el motivo de la restauración            ║
║     5. Verificar compilación después de restaurar         ║
║                                                            ║
╚════════════════════════════════════════════════════════════╝
```

---

## 📋 ÍNDICE

1. [Comandos de Verificación (Seguros)](#verificacion)
2. [Comandos de Restauración (Requieren Autorización)](#restauracion)
3. [Proceso Seguro de Restauración](#proceso-seguro)

---

## 🔍 Comandos de Verificación (SEGUROS) {#verificacion}

### **Listar todos los backups**

```powershell
# Ver lista completa de backups con fecha y tamaño
Get-ChildItem BACKUP/*.backup | Select-Object Name, Length, LastWriteTime | Format-Table -AutoSize
```

**Salida esperada:**
```
Name                                        Length LastWriteTime      
----                                        ------ -------------      
2026-01-02_200910_DiarioPage.xaml.cs.backup  78492 02/01/2026 20:01:43
2026-01-02_200910_IntervalMerger.cs.backup    4727 02/01/2026 19:42:42
2026-01-02_DiarioPage.xaml.cs.backup         74724 02/01/2026 17:29:04
2026-01-02_LoginPage.xaml.cs.backup          37137 01/01/2026 19:37:53
2026-01-02_ParteItemEdit.xaml.cs.backup      86461 02/01/2026 11:19:07
```

### **Verificar que los backups existen**

```powershell
# Verificar backup más reciente (20:09)
Test-Path "BACKUP\2026-01-02_200910_DiarioPage.xaml.cs.backup"
Test-Path "BACKUP\2026-01-02_200910_IntervalMerger.cs.backup"

# Verificar backup anterior (18:30)
Test-Path "BACKUP\2026-01-02_DiarioPage.xaml.cs.backup"
Test-Path "BACKUP\2026-01-02_LoginPage.xaml.cs.backup"
Test-Path "BACKUP\2026-01-02_ParteItemEdit.xaml.cs.backup"
```

**Salida esperada:** `True` si el archivo existe

### **Comparar tamaños de archivos**

```powershell
# Comparar backup con archivo actual
$backupSize = (Get-ChildItem "BACKUP\2026-01-02_200910_DiarioPage.xaml.cs.backup").Length
$currentSize = (Get-ChildItem "Views\DiarioPage.xaml.cs").Length

Write-Host "Backup:  $backupSize bytes"
Write-Host "Actual:  $currentSize bytes"
Write-Host "Diff:    $($currentSize - $backupSize) bytes"
```

### **Ver contenido de un backup (sin modificar)**

```powershell
# Ver las primeras 50 líneas
Get-Content "BACKUP\2026-01-02_200910_DiarioPage.xaml.cs.backup" | Select-Object -First 50

# Ver las últimas 50 líneas
Get-Content "BACKUP\2026-01-02_200910_DiarioPage.xaml.cs.backup" | Select-Object -Last 50

# Buscar un método específico
Get-Content "BACKUP\2026-01-02_200910_DiarioPage.xaml.cs.backup" | Select-String "UpdateTimeCoverageTooltip"
```

### **Comparar diferencias entre backup y archivo actual**

```powershell
# Ver diferencias línea por línea
Compare-Object `
    (Get-Content "BACKUP\2026-01-02_200910_DiarioPage.xaml.cs.backup") `
    (Get-Content "Views\DiarioPage.xaml.cs") `
    -IncludeEqual | 
    Where-Object { $_.SideIndicator -ne "==" } | 
    Select-Object -First 20
```

---

## 🔄 Comandos de Restauración (REQUIEREN AUTORIZACIÓN) {#restauracion}

### **⚠️ Restaurar Backup Más Reciente (20:09 - Con Tooltip)**

```powershell
# ❌ NO EJECUTAR SIN AUTORIZACIÓN
# ⚠️ ESTOS COMANDOS SOBREESCRIBIRÁN EL CÓDIGO ACTUAL

# Restaurar DiarioPage con funcionalidad de tooltip
Copy-Item "BACKUP\2026-01-02_200910_DiarioPage.xaml.cs.backup" `
          "Views\DiarioPage.xaml.cs" `
          -Force

# Restaurar IntervalMerger (algoritmo de merge)
Copy-Item "BACKUP\2026-01-02_200910_IntervalMerger.cs.backup" `
          "Helpers\IntervalMerger.cs" `
          -Force

# Verificar compilación
dotnet build
```

### **⚠️ Restaurar Backup Anterior (18:30 - Sin Tooltip)**

```powershell
# ❌ NO EJECUTAR SIN AUTORIZACIÓN
# Para restaurar versión sin funcionalidad de tooltip

# Restaurar DiarioPage (sin tooltip)
Copy-Item "BACKUP\2026-01-02_DiarioPage.xaml.cs.backup" `
          "Views\DiarioPage.xaml.cs" `
          -Force

# Restaurar LoginPage
Copy-Item "BACKUP\2026-01-02_LoginPage.xaml.cs.backup" `
          "Views\LoginPage.xaml.cs" `
          -Force

# Restaurar ParteItemEdit
Copy-Item "BACKUP\2026-01-02_ParteItemEdit.xaml.cs.backup" `
          "Views\ParteItemEdit.xaml.cs" `
          -Force

# ⚠️ IMPORTANTE: Eliminar IntervalMerger si existe (no estaba en backup 18:30)
Remove-Item "Helpers\IntervalMerger.cs" -ErrorAction SilentlyContinue

# Verificar compilación
dotnet build
```

### **⚠️ Restaurar un Solo Archivo**

```powershell
# ❌ NO EJECUTAR SIN AUTORIZACIÓN
# Ejemplo: Restaurar solo IntervalMerger

Copy-Item "BACKUP\2026-01-02_200910_IntervalMerger.cs.backup" `
          "Helpers\IntervalMerger.cs" `
          -Force

dotnet build
```

---

## ✅ Proceso Seguro de Restauración {#proceso-seguro}

### **Paso 1: Crear Backup del Estado Actual**

```powershell
# Obtener timestamp actual
$timestamp = Get-Date -Format "yyyy-MM-dd_HHmmss"

# Crear backup del estado actual ANTES de restaurar
Copy-Item "Views\DiarioPage.xaml.cs" `
          "BACKUP\${timestamp}_DiarioPage.xaml.cs.ANTES_RESTAURAR" `
          -Force

Copy-Item "Helpers\IntervalMerger.cs" `
          "BACKUP\${timestamp}_IntervalMerger.cs.ANTES_RESTAURAR" `
          -Force `
          -ErrorAction SilentlyContinue

Write-Host "✅ Backup del estado actual creado con timestamp: $timestamp"
```

### **Paso 2: Consultar con el Usuario**

```
╔════════════════════════════════════════════════════════════╗
║                                                            ║
║  ⚠️ CONFIRMAR CON EL USUARIO                              ║
║                                                            ║
║  Preguntas a hacer:                                       ║
║                                                            ║
║  1. ¿Qué backup quieres restaurar?                        ║
║     • 20:09 (con tooltip)                                 ║
║     • 18:30 (sin tooltip)                                 ║
║                                                            ║
║  2. ¿Por qué necesitas restaurar?                         ║
║                                                            ║
║  3. ¿Has creado un backup del estado actual?              ║
║                                                            ║
║  4. ¿Entiendes que se perderán los cambios actuales?      ║
║                                                            ║
║  Solo proceder si el usuario confirma explícitamente      ║
║                                                            ║
╚════════════════════════════════════════════════════════════╝
```

### **Paso 3: Ejecutar Restauración**

```powershell
# Solo después de obtener autorización explícita

# Ejemplo: Restaurar backup 20:09
Copy-Item "BACKUP\2026-01-02_200910_DiarioPage.xaml.cs.backup" `
          "Views\DiarioPage.xaml.cs" `
          -Force

Copy-Item "BACKUP\2026-01-02_200910_IntervalMerger.cs.backup" `
          "Helpers\IntervalMerger.cs" `
          -Force

Write-Host "✅ Archivos restaurados desde backup 20:09"
```

### **Paso 4: Verificar Compilación**

```powershell
# Limpiar y compilar
dotnet clean
dotnet build

# Verificar errores
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Compilación exitosa - Restauración completada"
} else {
    Write-Host "❌ ERROR EN COMPILACIÓN - Revisar cambios"
}
```

### **Paso 5: Documentar la Restauración**

```powershell
# Crear archivo de log de la restauración
$logPath = "BACKUP\RESTAURACION_LOG_${timestamp}.txt"

@"
RESTAURACIÓN DE BACKUP
======================
Fecha: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Usuario: $env:USERNAME
Backup restaurado: 2026-01-02_200910
Motivo: [ESPECIFICAR MOTIVO]
Archivos restaurados:
  - Views\DiarioPage.xaml.cs
  - Helpers\IntervalMerger.cs
Compilación: [EXITOSA/FALLIDA]
Notas: [AGREGAR NOTAS]
"@ | Out-File $logPath

Write-Host "✅ Log de restauración creado: $logPath"
```

---

## 🔧 Comandos de Emergencia

### **Revertir una Restauración Fallida**

```powershell
# Si algo salió mal, restaurar el backup del estado previo
$timestamp = "2026-01-02_201500" # Reemplazar con tu timestamp

Copy-Item "BACKUP\${timestamp}_DiarioPage.xaml.cs.ANTES_RESTAURAR" `
          "Views\DiarioPage.xaml.cs" `
          -Force

Copy-Item "BACKUP\${timestamp}_IntervalMerger.cs.ANTES_RESTAURAR" `
          "Helpers\IntervalMerger.cs" `
          -Force `
          -ErrorAction SilentlyContinue

dotnet build
```

### **Comparar Todos los Backups**

```powershell
# Crear reporte de diferencias entre todos los backups
$backups = Get-ChildItem "BACKUP\*DiarioPage*.backup"

foreach ($backup in $backups) {
    $lines = (Get-Content $backup.FullName).Count
    $size = $backup.Length / 1KB
    Write-Host "$($backup.Name): $lines líneas, $([math]::Round($size, 1)) KB"
}
```

---

## 📊 Resumen de Comandos por Categoría

### **Verificación (SIEMPRE SEGUROS):**
- ✅ `Get-ChildItem BACKUP/*.backup`
- ✅ `Test-Path BACKUP\archivo.backup`
- ✅ `Get-Content BACKUP\archivo.backup`
- ✅ `Compare-Object`

### **Restauración (REQUIEREN AUTORIZACIÓN):**
- ⚠️ `Copy-Item BACKUP\... Views\...`
- ⚠️ `Remove-Item Helpers\...`
- ⚠️ `dotnet build`

### **Seguridad (USAR SIEMPRE):**
- ✅ Crear backup con timestamp
- ✅ Consultar con usuario
- ✅ Documentar cambios
- ✅ Verificar compilación

---

**Última actualización:** 2026-01-02 20:15
