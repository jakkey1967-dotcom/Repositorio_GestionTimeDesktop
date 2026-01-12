# 🚀 Sistema de Generación de Instaladores - GestionTime Desktop

**Versión:** 1.2.0  
**Estado:** ✅ Listo para usar  
**Última actualización:** Enero 2026

---

## 📌 ARCHIVOS PRINCIPALES

### 🎯 **Para Usuarios (Doble Clic)**

| Archivo | Descripción | Uso |
|---------|-------------|-----|
| **VERIFICAR-REQUISITOS.bat** | Verifica que tu sistema esté listo | Ejecuta esto primero |
| **GENERAR-INSTALADOR-MENU.bat** | Menú interactivo con todas las opciones | Recomendado para primera vez |
| **GENERAR-INSTALADOR-PORTABLE.bat** | Genera ZIP portable en 1-2 min | ⚡ Más rápido |
| **GENERAR-INSTALADOR-MSIX.bat** | Genera instalador MSIX profesional | 🏢 Más profesional |

### 📚 **Documentación**

| Archivo | Contenido |
|---------|-----------|
| **INICIO-RAPIDO.md** | Guía rápida para empezar (2 min lectura) |
| **COMO-GENERAR-INSTALADOR-SIMPLE.md** | Guía completa paso a paso |
| **GUIA-VISUAL-INSTALADORES.txt** | Referencia visual rápida |
| **Installer/README-CREAR-MSI-MSIX-DEFINITIVO.md** | Documentación técnica avanzada |

---

## ⚡ INICIO RÁPIDO (30 segundos)

### **Paso 1: Verificar requisitos**
```
Doble clic en: VERIFICAR-REQUISITOS.bat
```

### **Paso 2: Generar instalador**
```
Doble clic en: GENERAR-INSTALADOR-PORTABLE.bat
```

### **Paso 3: Listo**
```
El ZIP se generará en: Installer\Output\
```

---

## 📦 TIPOS DE INSTALADORES DISPONIBLES

### 1️⃣ **Portable (ZIP)** - ⭐ RECOMENDADO

**Ventajas:**
- ✅ Más rápido (1-2 minutos)
- ✅ Más simple (solo requiere .NET SDK 8)
- ✅ No requiere instalación
- ✅ Funciona en cualquier carpeta
- ✅ Ideal para desarrollo y testing

**Cómo generar:**
```
GENERAR-INSTALADOR-PORTABLE.bat
```

**Resultado:**
```
Installer\Output\GestionTime-Desktop-1.2.0-Portable.zip (~45 MB)
```

---

### 2️⃣ **MSIX** - 🏢 PROFESIONAL

**Ventajas:**
- ✅ Instalador moderno de Windows 10/11
- ✅ Integración con Windows
- ✅ Instalación/desinstalación limpia
- ✅ Actualizaciones automáticas
- ✅ Ideal para distribución a usuarios finales

**Cómo generar:**
```
GENERAR-INSTALADOR-MSIX.bat
```

**Requisitos adicionales:**
- Visual Studio 2022

**Resultado:**
```
AppPackages\GestionTime.Desktop_1.2.0.0_x64_Test\
└── GestionTime.Desktop_1.2.0.0_x64.msix (~42 MB)
```

---

### 3️⃣ **MSI Tradicional** - 🏭 CORPORATIVO

**Ventajas:**
- ✅ Compatible con Windows XP+
- ✅ Group Policy deployment
- ✅ Estándar corporativo

**Cómo generar:**
1. Instalar WiX Toolset v3.14
2. Ejecutar: `CREATE-MSI-INSTALLER-COMPLETE.ps1`

**Ver documentación:** `Installer\README-MSI.md`

---

### 4️⃣ **EXE con Inno Setup** - 📦 UNIVERSAL

**Ventajas:**
- ✅ Interfaz amigable
- ✅ Sin advertencias de certificado
- ✅ Máxima compatibilidad

**Cómo generar:**
1. Instalar Inno Setup
2. Ejecutar: `CREATE-INSTALLER-COMPLETE-V3.ps1`

**Ver documentación:** `Installer\README-INSTALADOR-COMPLETO.md`

---

## 📊 COMPARACIÓN RÁPIDA

| Característica | Portable | MSIX | MSI | EXE |
|---------------|----------|------|-----|-----|
| **Facilidad** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐ |
| **Velocidad** | 1-2 min | 2-3 min | 5-10 min | 5-10 min |
| **Requisitos** | .NET SDK | VS 2022 | WiX + VS | Inno Setup |
| **Tamaño** | ~45 MB | ~42 MB | ~52 MB | ~52 MB |
| **Win 10/11** | ✅ | ✅ | ✅ | ✅ |
| **Win 7/8** | ✅ | ❌ | ✅ | ✅ |
| **Instalador** | No | Sí | Sí | Sí |

---

## 🔧 REQUISITOS DEL SISTEMA

### **Requisitos Mínimos (todos los métodos)**
- ✅ Windows 10/11
- ✅ .NET SDK 8 - [Descargar](https://dotnet.microsoft.com/download/dotnet/8.0)
- ✅ PowerShell 5.1+

### **Requisitos Adicionales por Método**

**Para MSIX:**
- Visual Studio 2022 (Community o superior)

**Para MSI:**
- WiX Toolset v3.14 - [Descargar](https://wixtoolset.org/releases/)
- Visual Studio 2022

**Para EXE:**
- Inno Setup - [Descargar](https://jrsoftware.org/isinfo.php)

---

## 📂 ESTRUCTURA DEL PROYECTO

```
GestionTimeDesktop/
│
├── 🎯 Archivos de inicio rápido
│   ├── VERIFICAR-REQUISITOS.bat
│   ├── GENERAR-INSTALADOR-MENU.bat
│   ├── GENERAR-INSTALADOR-PORTABLE.bat
│   └── GENERAR-INSTALADOR-MSIX.bat
│
├── 📚 Documentación
│   ├── INICIO-RAPIDO.md
│   ├── COMO-GENERAR-INSTALADOR-SIMPLE.md
│   ├── GUIA-VISUAL-INSTALADORES.txt
│   └── README-SISTEMA-INSTALADORES.md (este archivo)
│
├── ⚙️ Scripts PowerShell (automáticos)
│   ├── VERIFICAR-REQUISITOS.ps1
│   ├── GENERAR-INSTALADOR-PORTABLE.ps1
│   ├── GENERAR-MSIX-VISUAL-STUDIO.ps1
│   ├── CREATE-MSIX-INSTALLER.ps1
│   ├── CREATE-MSI-INSTALLER-COMPLETE.ps1
│   └── CREATE-INSTALLER-COMPLETE-V3.ps1
│
├── 📦 Salida de instaladores
│   ├── Installer/Output/              (ZIP, MSI, EXE)
│   └── AppPackages/                   (MSIX)
│
└── 📖 Documentación técnica
    └── Installer/
        ├── README-CREAR-MSI-MSIX-DEFINITIVO.md
        ├── README-MSI-VS-MSIX.md
        ├── README-MSI.md
        └── README-INSTALADOR-COMPLETO.md
```

---

## 🎯 FLUJO DE TRABAJO RECOMENDADO

### **Primera vez:**
1. Lee: `INICIO-RAPIDO.md` (2 minutos)
2. Ejecuta: `VERIFICAR-REQUISITOS.bat`
3. Ejecuta: `GENERAR-INSTALADOR-MENU.bat`
4. Elige: Opción 1 (Portable)

### **Ya conoces el sistema:**
1. Ejecuta: `GENERAR-INSTALADOR-PORTABLE.bat`
2. Espera: 1-2 minutos
3. Distribuye: El ZIP generado

### **Para producción:**
1. Ejecuta: `GENERAR-INSTALADOR-MSIX.bat`
2. Sigue: Instrucciones en Visual Studio
3. Distribuye: El MSIX generado

---

## 🆘 SOLUCIÓN DE PROBLEMAS

### ❌ "dotnet no se reconoce como comando"
**Solución:**  
Instala .NET SDK 8: https://dotnet.microsoft.com/download/dotnet/8.0

### ❌ "Error al compilar el proyecto"
**Solución:**
```
1. Abrir Visual Studio
2. Abrir: GestionTime.Desktop.sln
3. Build > Clean Solution
4. Build > Rebuild Solution
5. Si compila OK, volver a ejecutar el script
```

### ❌ "Visual Studio no encontrado"
**Solución:**  
Usa el método Portable (no requiere Visual Studio)

### ❌ "El MSIX pide certificado no confiable"
**Solución:**
```
1. Click en "Más información"
2. Click en "Instalar de todos modos"
3. Es normal en desarrollo sin certificado comercial
```

### ❌ "Script bloqueado por política de ejecución"
**Solución:**
```powershell
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force
```

---

## ✅ VERIFICACIÓN POST-GENERACIÓN

### **Portable (ZIP):**
1. Extraer ZIP en carpeta de prueba
2. Ejecutar: `GestionTime.Desktop.exe`
3. Verificar que abre correctamente
4. Probar funcionalidad básica

### **MSIX:**
1. Doble-clic en archivo `.msix`
2. Click "Instalar"
3. Buscar "GestionTime Desktop" en Menú Inicio
4. Ejecutar y verificar funcionalidad

### **MSI/EXE:**
1. Ejecutar instalador
2. Seguir asistente de instalación
3. Al finalizar, ejecutar desde Menú Inicio
4. Verificar funcionalidad completa

---

## 📞 SOPORTE Y RECURSOS

### **Contacto**
- **Email:** soporte@gestiontime.com
- **GitHub:** https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop

### **Documentación Adicional**
- [Manual de Usuario](Docs/MANUAL_USUARIO_GESTIONTIME_DESKTOP.md)
- [Documentación Técnica](Docs/)
- [Notas de Versión](CHANGELOG.md)

### **Recursos Externos**
- [.NET SDK 8](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/downloads/)
- [WiX Toolset](https://wixtoolset.org/releases/)
- [Inno Setup](https://jrsoftware.org/isinfo.php)

---

## 📝 NOTAS IMPORTANTES

### **Compilación**
- ✅ El proyecto compila correctamente sin errores
- ✅ Todas las dependencias están incluidas
- ✅ Los assets están correctamente referenciados

### **Seguridad**
- ⚠️ Los instaladores de desarrollo no están firmados digitalmente
- ⚠️ Para producción, se recomienda firmar con certificado de código
- ✅ El código fuente es seguro y no contiene malware

### **Distribución**
- ✅ Portable: Distribuye el ZIP directamente
- ✅ MSIX: Los usuarios pueden instalarlo con doble-clic
- ✅ MSI/EXE: Instaladores estándar de Windows

---

## 🎯 RESUMEN EJECUTIVO

**Para el 90% de los casos:**

```
1. Ejecuta: GENERAR-INSTALADOR-PORTABLE.bat
2. Espera: 1-2 minutos
3. Distribuye: El ZIP de Installer\Output\
```

**Simple, rápido y funciona siempre** ✅

---

## 📄 LICENCIA

© 2025 GestionTime Solutions  
Todos los derechos reservados.

---

**Versión del sistema:** 1.2.0  
**Fecha:** Enero 2026  
**Mantenedor:** Equipo GestionTime Desktop
