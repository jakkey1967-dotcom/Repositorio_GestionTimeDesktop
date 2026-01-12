# ⚠️ PROBLEMA CON POWERSHELL Y SCRIPTS MSI

**Fecha:** 08/01/2026 14:45  
**Problema:** PowerShell (tanto CMD como ISE) tiene problemas de parsing con XML inline

---

## 🐛 **EL PROBLEMA REAL**

PowerShell **NO puede ejecutar** scripts que contienen XML inline con caracteres `<` y `>` porque los interpreta como operadores de redirección, incluso dentro de strings.

**Error típico:**
```
El operador '<' está reservado para uso futuro.
```

**Esto ocurre en:**
- ❌ PowerShell desde CMD
- ❌ PowerShell ISE presionando F5
- ❌ Ejecutando el script directamente

---

## ✅ **SOLUCIONES DISPONIBLES**

### **Opción 1: Usar WiX Project en Visual Studio** ⭐ MÁS FÁCIL

```
1. Abrir Visual Studio 2022
2. Abrir: GestionTime.Desktop.sln
3. Agregar nuevo proyecto:
   - Tipo: "Setup Project for WiX v4" o "WiX Installer"
   - Nombre: GestionTime.Installer
4. Configurar el proyecto WiX:
   - Referenciar GestionTime.Desktop
   - Configurar Product.wxs
5. Build → Build Solution
6. MSI generado en: bin\Release\
```

### **Opción 2: Usar CREATE-MSI-XMLWRITER.ps1** (Sin estructura de carpetas)

**ADVERTENCIA:** Este script instala todos los archivos en la raíz (sin carpetas Assets, Views, etc.)

```powershell
cd C:\GestionTime\GestionTimeDesktop
powershell -ExecutionPolicy Bypass -File ".\CREATE-MSI-XMLWRITER.ps1"
```

**Limitación:** NO mantiene estructura de carpetas, la app puede no funcionar correctamente.

### **Opción 3: Generar WXS manualmente y compilar**

```powershell
# 1. Generar solo el archivo WXS
cd C:\GestionTime\GestionTimeDesktop\Installer\MSI

# 2. Editar Product.wxs manualmente

# 3. Compilar con WiX
& "C:\Program Files\WiX Toolset v6.0\bin\wix.exe" build Product.wxs -arch x64 -out ..\Output\GestionTime-Setup.msi -bindpath ..\..\bin\x64\Debug\net8.0-windows10.0.19041.0
```

---

## 📝 **POR QUÉ LOS SCRIPTS NO FUNCIONAN**

### **CREATE-MSI-COMPLETE.ps1:**
- Contiene XML inline con `<` y `>`
- PowerShell lo parsea antes de ejecutar
- ERROR: "El operador '<' está reservado"

### **CREATE-MSI-XMLWRITER.ps1:**
- Usa System.Xml.XmlWriter para evitar `<` y `>`
- ✅ Se ejecuta sin errores de parsing
- ❌ NO mantiene estructura de carpetas
- ❌ Instala todo en la raíz (Assets, Views mezclados)
- ❌ La app puede no arrancar

---

## ⭐ **RECOMENDACIÓN FINAL**

**Usa Visual Studio con un proyecto WiX:**

1. Es más fácil
2. Tiene editor visual
3. Mantiene estructura de carpetas
4. Genera MSI profesional
5. No hay problemas de parsing

**Alternativa: Usar Advanced Installer o InnoSetup:**

Estos son instaladores que no requieren XML y tienen interfaces visuales.

---

## 🎯 **CONCLUSIÓN**

**Los scripts PowerShell para generar MSI con WiX tienen limitaciones inherentes:**

- PowerShell no puede manejar XML inline correctamente
- XmlWriter funciona pero complica mantener estructura de carpetas
- La mejor solución es usar Visual Studio o herramientas visuales

**Si necesitas usar PowerShell, el único script que funciona es `CREATE-MSI-XMLWRITER.ps1` pero instala todo sin carpetas.**

---

*Problema Real Scripts MSI - 08/01/2026 14:45*
