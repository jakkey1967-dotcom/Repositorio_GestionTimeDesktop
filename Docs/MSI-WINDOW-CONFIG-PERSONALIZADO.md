# ✅ WINDOW-CONFIG.INI PERSONALIZADO EN MSI

**Actualización:** 08/01/2026 13:15  
**Funcionalidad:** Incluir window-config.ini personalizado con configuración inicial para Windows 10

---

## 🎯 **CAMBIO IMPLEMENTADO**

El instalador MSI ahora incluye un archivo `window-config.ini` **personalizado** desde:

```
C:\GestionTime\GestionTimeDesktop\Installer\window-config.ini
```

En lugar del archivo genérico que está en el directorio `bin\`.

---

## 📄 **CONTENIDO DEL ARCHIVO PERSONALIZADO**

```ini
# ============================================
# CONFIGURACIÓN DE TAMAÑOS DE VENTANA
# GestionTime Desktop
# ============================================
#
# Formato: PageName=Width,Height
# 
# Páginas disponibles:
#   - LoginPage
#   - DiarioPage
#   - ParteItemEdit
#   - GraficaDiaPage
#   - RegisterPage
#   - ForgotPasswordPage
#
# Última actualización: 2026-01-08 12:23:19
# ============================================

DiarioPage=1103,800
LoginPage=749,560
ParteItemEdit=1140,845
```

**Optimizado para Windows 10:**
- ✅ Tamaños adaptados a resolución 1920x1080
- ✅ DiarioPage: 1103x800 (vista principal)
- ✅ LoginPage: 749x560 (ventana de inicio)
- ✅ ParteItemEdit: 1140x845 (edición de partes)

---

## ⚙️ **CÓMO FUNCIONA EN EL SCRIPT**

### **CREATE-MSI-COMPLETE.ps1 - Cambios:**

**1. Detecta el archivo personalizado:**
```powershell
$customWindowConfig = "$projectDir\Installer\window-config.ini"
$hasCustomConfig = Test-Path $customWindowConfig
```

**2. Omite el archivo del bin:**
```powershell
foreach ($file in $allFiles) {
    # Si es window-config.ini del bin, lo saltamos
    if ($file.Name -eq "window-config.ini" -and $hasCustomConfig) {
        Write-Host "   ⚠ Omitiendo window-config.ini de bin" -ForegroundColor Yellow
        continue
    }
    ...
}
```

**3. Agrega el personalizado con NeverOverwrite:**
```powershell
if ($hasCustomConfig) {
    <Component Directory="INSTALLFOLDER">
      <File Source="$customWindowConfig" 
            Name="window-config.ini" 
            NeverOverwrite="yes" />  # ⭐ No sobrescribir en actualizaciones
    </Component>
}
```

---

## 🔧 **ATRIBUTO NeverOverwrite="yes"**

### **¿Qué significa?**

El atributo `NeverOverwrite="yes"` en WiX indica que:

✅ **Primera instalación:**
- Se copia `window-config.ini` con los valores optimizados
- Usuario ve ventanas con tamaños correctos desde el inicio

✅ **Usuario modifica configuración:**
- Cambia posición o tamaño de ventanas
- `window-config.ini` se actualiza con preferencias del usuario

✅ **Actualización a v1.3.0:**
- MSI **NO sobrescribe** `window-config.ini`
- Configuración del usuario se **preserva**
- No se pierden los ajustes personalizados

---

## 📊 **COMPORTAMIENTO DETALLADO**

### **Escenario 1: Primera Instalación**
```
1. MSI instala window-config.ini personalizado
2. Archivo copiado a: C:\Program Files\GestionTime\Desktop\window-config.ini
3. Contenido inicial:
   DiarioPage=1103,800
   LoginPage=749,560
   ParteItemEdit=1140,845
```

### **Escenario 2: Usuario Personaliza**
```
1. Usuario ajusta tamaño de DiarioPage manualmente
2. WindowConfigService actualiza window-config.ini:
   DiarioPage=1400,900  # ⭐ Nuevo tamaño preferido
   LoginPage=749,560
   ParteItemEdit=1140,845
```

### **Escenario 3: Actualización a v1.3.0**
```
1. Usuario instala MSI de v1.3.0
2. WiX detecta window-config.ini existe
3. NeverOverwrite="yes" → NO sobrescribe
4. window-config.ini mantiene:
   DiarioPage=1400,900  # ✅ Se preserva configuración del usuario
   LoginPage=749,560
   ParteItemEdit=1140,845
```

---

## 🚀 **CÓMO REGENERAR EL MSI**

### **Desde PowerShell ISE (Recomendado):**

```
1. Abrir PowerShell ISE como Administrador
2. Abrir: C:\GestionTime\GestionTimeDesktop\CREATE-MSI-COMPLETE.ps1
3. Presionar F5 (Ejecutar)
```

**PowerShell ISE no tiene problemas con < y > en strings.**

### **Desde CMD:**

```cmd
powershell -ExecutionPolicy Bypass -Command "& {cd 'C:\GestionTime\GestionTimeDesktop'; . '.\CREATE-MSI-COMPLETE.ps1'}"
```

### **Desde PowerShell (Explorador):**

```
1. Navegar a: C:\GestionTime\GestionTimeDesktop
2. Hacer Shift + Click derecho en carpeta vacía
3. Seleccionar "Abrir ventana de PowerShell aquí"
4. Ejecutar:
   .\CREATE-MSI-COMPLETE.ps1
```

---

## ✅ **RESULTADO ESPERADO**

```
===============================================
  MSI COMPLETO CREADO EXITOSAMENTE
===============================================

[1/5] Recopilando archivos...
   Archivos encontrados: 153
   ✓ GestionTime.Desktop.exe
   ✓ resources.pri
   ✓ window-config.ini
   ✓ appsettings.json

[2/5] Generando componentes WiX con estructura de directorios...
   ⚠ Omitiendo window-config.ini de bin (se usará versión personalizada)
   ✓ window-config.ini personalizado agregado (NeverOverwrite)
   Directorios: 31
   Componentes: 154

[3/5] Creando archivo WiX completo...
[4/5] Compilando MSI...
[5/5] Verificando MSI...

ARCHIVO: GestionTime-Desktop-1.2.0-Complete-Setup.msi
TAMAÑO: 16.32 MB
```

---

## 📂 **ESTRUCTURA INSTALADA**

```
C:\Program Files\GestionTime\Desktop\
├── GestionTime.Desktop.exe
├── window-config.ini          ⭐ PERSONALIZADO (NeverOverwrite)
│   # Contenido inicial optimizado para Windows 10
│   DiarioPage=1103,800
│   LoginPage=749,560
│   ParteItemEdit=1140,845
├── appsettings.json
├── Assets\
├── Views\
└── ...
```

---

## 🔍 **VERIFICAR DESPUÉS DE INSTALAR**

```powershell
# Ver contenido instalado
Get-Content "C:\Program Files\GestionTime\Desktop\window-config.ini"

# Resultado esperado:
# DiarioPage=1103,800
# LoginPage=749,560
# ParteItemEdit=1140,845
```

---

## 📝 **VENTAJAS DE ESTA SOLUCIÓN**

✅ **Primera Experiencia Optimizada:**
- Usuario ve ventanas con tamaños correctos desde el inicio
- No necesita ajustar manualmente

✅ **Preserva Preferencias:**
- Actualizaciones no borran configuración del usuario
- `NeverOverwrite="yes"` protege ajustes personalizados

✅ **Fácil Mantenimiento:**
- Cambiar valores: Editar `Installer\window-config.ini`
- Regenerar MSI con nuevos valores por defecto

✅ **Reset Manual Disponible:**
- Usuario puede eliminar `window-config.ini`
- App regenerará con valores por defecto

---

## 🎯 **MODIFICAR VALORES POR DEFECTO**

### **Para cambiar tamaños iniciales:**

```
1. Editar:
   C:\GestionTime\GestionTimeDesktop\Installer\window-config.ini

2. Cambiar valores (ejemplo):
   DiarioPage=1200,900
   LoginPage=800,600
   ParteItemEdit=1200,900

3. Regenerar MSI:
   .\CREATE-MSI-COMPLETE.ps1

4. Nuevo MSI tendrá los nuevos valores por defecto
```

---

## 📊 **COMPARACIÓN**

| Característica | Antes | Ahora ⭐ |
|---|---|---|
| **Archivo usado** | Del bin\ | De Installer\ personalizado |
| **Valores iniciales** | Por defecto del código | Optimizados para Win 10 |
| **Preserva preferencias** | ❌ Se sobreescribe | ✅ NeverOverwrite="yes" |
| **Tamaños ventanas** | Genéricos | Específicos por página |
| **Fácil modificar** | ❌ Recompilar código | ✅ Editar .ini |

---

## ✅ **RESUMEN**

**Cambio:**  
MSI ahora usa `Installer\window-config.ini` personalizado

**Contenido:**  
Tamaños optimizados para Windows 10 (1920x1080)

**Protección:**  
`NeverOverwrite="yes"` preserva preferencias del usuario

**Beneficio:**  
- Primera experiencia optimizada
- Actualizaciones no borran configuración
- Fácil modificar valores por defecto

**Ubicación en instalación:**  
`C:\Program Files\GestionTime\Desktop\window-config.ini`

**¡Configuración inicial perfecta para Windows 10!** 🎉

---

*window-config.ini Personalizado - GestionTime Desktop v1.2.0 - 08/01/2026*
