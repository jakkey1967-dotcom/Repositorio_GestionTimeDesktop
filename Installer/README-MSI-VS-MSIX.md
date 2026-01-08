# 📦 INSTALADORES MSI vs MSIX - GESTIONTIME DESKTOP

**Versión:** 1.2.0  
**Fecha:** Enero 2026  
**Propósito:** Explicar las diferencias y cómo crear cada tipo de instalador

---

## 🎯 OPCIONES DE INSTALADOR DISPONIBLES

### **Opción 1: MSIX (Moderno - Recomendado para Windows 11)** ⭐

**¿Qué es MSIX?**
- Formato de instalador **moderno** de Microsoft
- Reemplazo de MSI tradicional
- Nativo de Windows 10/11
- **NO requiere WiX Toolset**

**Ventajas:**
- ✅ **Fácil de crear** (integrado en .NET 8)
- ✅ **Instalación limpia** (contenedor aislado)
- ✅ **Actualizaciones automáticas** (desde Microsoft Store)
- ✅ **Desinstalación completa** (sin residuos)
- ✅ **Seguridad mejorada** (sandboxing)
- ✅ **Menor tamaño** (~40 MB comprimido)

**Desventajas:**
- ⚠️ Requiere Windows 10 1809+ o Windows 11
- ⚠️ Instalación requiere "trust" del certificado (en desarrollo)

**Crear MSIX:**
```powershell
.\CREATE-MSIX-INSTALLER.ps1
```

**Resultado:**
```
AppPackages\GestionTime.Desktop_1.2.0.0_x64\
└── GestionTime.Desktop_1.2.0.0_x64.msix  (~40 MB)
```

---

### **Opción 2: MSI Tradicional (Compatible - Requiere WiX)** 

**¿Qué es MSI?**
- Formato de instalador **tradicional** de Windows
- Compatible con Windows XP+
- **Requiere WiX Toolset**

**Ventajas:**
- ✅ **Compatible** con versiones antiguas de Windows
- ✅ **Group Policy deployment** (empresas)
- ✅ **Instalación silenciosa** estándar
- ✅ **Personalización total** (scripts, conditions, etc.)

**Desventajas:**
- ❌ **Difícil de crear** (requiere aprender WiX XML)
- ❌ **Requiere WiX Toolset** (instalación compleja)
- ❌ **Mayor tamaño** (~52 MB)
- ❌ **Residuos** tras desinstalación (Registry, archivos)

**Instalar WiX:**
```powershell
# Descargar desde:
https://wixtoolset.org/releases/

# O usar winget (si está disponible):
winget install WiXToolset.WiX
```

**Crear MSI:**
```powershell
.\CREATE-MSI-INSTALLER-COMPLETE.ps1
```

**Resultado:**
```
Installer\Output\
└── GestionTime-Desktop-1.2.0-Setup.msi  (~52 MB)
```

---

### **Opción 3: EXE con Inno Setup (Más Fácil)** ⭐⭐

**¿Qué es Inno Setup?**
- Creador de instaladores **EXE gratuito**
- Muy popular (usado por VS Code, Discord, etc.)
- **Muy fácil de usar**

**Ventajas:**
- ✅ **Muy fácil de crear** (script simple)
- ✅ **Instalación rápida** (1-2 minutos)
- ✅ **Interfaz personalizable**
- ✅ **Compatible** con todas las versiones de Windows
- ✅ **Instalación silenciosa** disponible

**Desventajas:**
- ⚠️ No es formato Microsoft oficial
- ⚠️ Algunos antivirus pueden alertar (falso positivo)

**Instalar Inno Setup:**
```
https://jrsoftware.org/isdl.php
```

**Crear EXE:**
```powershell
.\CREATE-INSTALLER-COMPLETE-V3.ps1
```

**Resultado:**
```
Installer\Output\
└── GestionTime-Desktop-1.2.0-Setup.exe  (~52 MB)
```

---

## 📊 COMPARATIVA COMPLETA

| Característica | MSIX ⭐ | MSI | EXE (Inno) ⭐⭐ |
|---------------|---------|-----|----------------|
| **Facilidad de creación** | 🟢 Muy fácil | 🔴 Difícil | 🟢 Fácil |
| **Herramienta requerida** | .NET 8 SDK | WiX Toolset | Inno Setup |
| **Instalación herramienta** | Ya instalado | Compleja | Simple (5 min) |
| **Tamaño instalador** | ~40 MB | ~52 MB | ~52 MB |
| **Velocidad instalación** | Rápida (1-2 min) | Lenta (2-3 min) | Rápida (1-2 min) |
| **Compatibilidad Windows** | 10 1809+ / 11 | XP+ (todas) | XP+ (todas) |
| **Instalación limpia** | ✅ Sí (sandbox) | ⚠️ Parcial | ⚠️ Parcial |
| **Actualizaciones automáticas** | ✅ Sí (Store) | ❌ No | ❌ No |
| **Group Policy** | ⚠️ Limitado | ✅ Sí | ❌ No |
| **Instalación silenciosa** | ✅ Sí | ✅ Sí | ✅ Sí |
| **Desinstalación completa** | ✅ Sí | ⚠️ Parcial | ⚠️ Parcial |
| **Personalización** | ⚠️ Limitada | 🟢 Total | 🟢 Avanzada |
| **Certificado requerido** | ⚠️ Sí (prod) | ❌ No | ❌ No |

---

## 🎯 RECOMENDACIONES POR ESCENARIO

### **Escenario 1: Distribución Interna (Empresa)**

**RECOMENDADO:** MSIX ⭐
```powershell
.\CREATE-MSIX-INSTALLER.ps1
```

**Razones:**
- Instalación moderna y limpia
- Compatible con Windows 11
- Actualizaciones fáciles
- Menor mantenimiento

**Nota:** Para evitar advertencia de certificado, firmar con certificado corporativo.

---

### **Escenario 2: Distribución Pública (Clientes externos)**

**RECOMENDADO:** EXE (Inno Setup) ⭐⭐
```powershell
.\CREATE-INSTALLER-COMPLETE-V3.ps1
```

**Razones:**
- Sin advertencias de certificado
- Compatible con versiones antiguas de Windows
- Interfaz profesional
- Instalación rápida

---

### **Escenario 3: Empresa con Group Policy**

**RECOMENDADO:** MSI Tradicional
```powershell
# 1. Instalar WiX Toolset
https://wixtoolset.org/releases/

# 2. Crear MSI
.\CREATE-MSI-INSTALLER-COMPLETE.ps1
```

**Razones:**
- Deployment centralizado con GPO
- Compatible con SCCM/Intune
- Estándar empresarial

---

### **Escenario 4: Desarrollo/Pruebas**

**RECOMENDADO:** MSIX (más rápido) ⭐
```powershell
.\CREATE-MSIX-INSTALLER.ps1
```

**Razones:**
- Muy rápido de crear
- Fácil de probar
- Desinstalación limpia

---

## 🚀 GUÍA RÁPIDA: CREAR MSIX (SIN WIX)

### **Paso 1: Ejecutar Script**

```powershell
cd C:\GestionTime\GestionTimeDesktop
.\CREATE-MSIX-INSTALLER.ps1
```

### **Paso 2: Resultado**

```
═══════════════════════════════════════════════════════
  ✅ PAQUETE MSIX GENERADO EXITOSAMENTE
═══════════════════════════════════════════════════════

📦 ARCHIVO MSIX:
   C:\GestionTime\GestionTimeDesktop\AppPackages\
   GestionTime.Desktop_1.2.0.0_x64\
   GestionTime.Desktop_1.2.0.0_x64.msix

📊 TAMAÑO:
   42.5 MB

🚀 INSTALACIÓN:
   1. Hacer doble-clic en el archivo .msix
   2. Click en 'Instalar'
   3. Buscar 'GestionTime Desktop' en Menú Inicio
```

### **Paso 3: Instalar**

1. Hacer **doble-clic** en el archivo `.msix`

2. Si aparece advertencia de certificado:
   ```
   "El editor no es de confianza"
   
   → Click en "Más información"
   → Click en "Instalar de todos modos"
   ```
   
   **Nota:** Esto es normal en desarrollo sin certificado de código.

3. La aplicación se instalará en:
   ```
   C:\Program Files\WindowsApps\GestionTime.Desktop_1.2.0.0_x64__...
   ```

4. Buscar **"GestionTime Desktop"** en el Menú Inicio

---

## 🛠️ SOLUCIÓN DE PROBLEMAS

### ❌ **Error: "No se pudo crear el paquete MSIX"**

**Solución:**
```powershell
# Limpiar proyecto
dotnet clean

# Restaurar dependencias
dotnet restore

# Volver a intentar
.\CREATE-MSIX-INSTALLER.ps1
```

### ❌ **Error: "El certificado no es de confianza"**

**Solución (Desarrollo):**
1. Click en "Más información"
2. Click en "Instalar de todos modos"

**Solución (Producción):**
1. Obtener certificado de firma de código
2. Firmar el MSIX:
   ```powershell
   SignTool sign /f MyCert.pfx /p password /fd SHA256 GestionTime.Desktop.msix
   ```

### ❌ **Prefiero MSI tradicional (con WiX)**

**Solución:**
1. Instalar WiX Toolset:
   ```
   https://wixtoolset.org/releases/
   ```

2. Ejecutar:
   ```powershell
   .\CREATE-MSI-INSTALLER-COMPLETE.ps1
   ```

---

## 📋 CHECKLIST DE DECISIÓN

**¿Qué instalador usar?**

```
┌─────────────────────────────────────────────────┐
│ ¿Necesitas Group Policy deployment?            │
│   ├─ SÍ  → MSI Tradicional                     │
│   └─ NO  → Continuar                            │
├─────────────────────────────────────────────────┤
│ ¿Tus usuarios tienen Windows 11 / Win10 1809+? │
│   ├─ SÍ  → MSIX (Recomendado) ⭐                │
│   └─ NO  → Continuar                            │
├─────────────────────────────────────────────────┤
│ ¿Quieres evitar advertencias de certificado?   │
│   ├─ SÍ  → EXE (Inno Setup) ⭐⭐                 │
│   └─ NO  → MSIX                                 │
└─────────────────────────────────────────────────┘
```

---

## 📦 RESUMEN: ARCHIVOS Y SCRIPTS

| Script | Instalador | Tamaño | Requiere |
|--------|------------|--------|----------|
| `CREATE-MSIX-INSTALLER.ps1` | MSIX (moderno) | ~40 MB | .NET 8 SDK |
| `CREATE-MSI-INSTALLER-COMPLETE.ps1` | MSI (tradicional) | ~52 MB | WiX Toolset |
| `CREATE-INSTALLER-COMPLETE-V3.ps1` | EXE (Inno Setup) | ~52 MB | Inno Setup |

---

## 📞 SOPORTE

**Email:** soporte@gestiontime.com  
**GitHub:** https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop

---

## 📄 LICENCIA

**GestionTime Desktop** © 2026 Global Retail Solutions  
Todos los derechos reservados.

---

**🎯 Recomendación Final:**

- **Desarrollo/Pruebas:** MSIX (rápido y fácil)
- **Distribución General:** EXE (Inno Setup)
- **Empresas con GPO:** MSI (WiX)

*Guía Completa MSI vs MSIX - Versión 1.2.0 - Enero 2026*
