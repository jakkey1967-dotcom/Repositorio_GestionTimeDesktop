# 🚀 INICIO RÁPIDO - Generar Instalador

**¿Primera vez generando el instalador? Lee esto primero.**

---

## ⚡ MÉTODO MÁS RÁPIDO

### **PASO 1: Verifica que tienes .NET SDK 8**

Abre PowerShell y ejecuta:
```powershell
dotnet --version
```

Si muestra algo como `8.0.xxx` → ✅ Todo listo  
Si da error → ❌ Instala .NET SDK 8 desde:  
https://dotnet.microsoft.com/download/dotnet/8.0

---

### **PASO 2: Genera el instalador**

**Opción A - Con menú interactivo:**
```
Doble clic en: GENERAR-INSTALADOR-MENU.bat
```
Te mostrará un menú con todas las opciones.

**Opción B - Directo al portable (más rápido):**
```
Doble clic en: GENERAR-INSTALADOR-PORTABLE.bat
```
Genera un ZIP en 1-2 minutos sin preguntas.

**Opción C - MSIX con Visual Studio:**
```
Doble clic en: GENERAR-INSTALADOR-MSIX.bat
```
Requiere Visual Studio 2022 instalado.

---

### **PASO 3: Encuentra tu instalador**

El instalador se generará en:
```
Installer\Output\GestionTime-Desktop-1.2.0-Portable.zip
```

O para MSIX:
```
AppPackages\GestionTime.Desktop_1.2.0.0_x64_Test\GestionTime.Desktop_1.2.0.0_x64.msix
```

---

## 📦 ¿Qué método usar?

| Si necesitas... | Usa... |
|----------------|--------|
| Algo rápido para probar | Portable (ZIP) |
| Distribuir a usuarios finales | MSIX |
| Compatibilidad máxima | MSI/EXE (ver docs) |

---

## 🆘 ¿Problemas?

### "No se reconoce dotnet como comando"
**Solución:**  
Instala .NET SDK 8: https://dotnet.microsoft.com/download/dotnet/8.0

### "Error al compilar el proyecto"
**Solución:**
1. Abre Visual Studio
2. Abre `GestionTime.Desktop.sln`
3. Build > Rebuild Solution
4. Si funciona, vuelve a ejecutar el script

### "Visual Studio no encontrado"
**Solución:**  
Usa el método Portable (no requiere Visual Studio)

---

## 📚 Documentación Completa

Para más información detallada, consulta:
- `COMO-GENERAR-INSTALADOR-SIMPLE.md` - Guía paso a paso
- `Installer\README-CREAR-MSI-MSIX-DEFINITIVO.md` - Referencia completa

---

## ✅ Resumen de Archivos

**Ejecutables (doble clic):**
- ✅ `GENERAR-INSTALADOR-MENU.bat` - Menú interactivo
- ✅ `GENERAR-INSTALADOR-PORTABLE.bat` - ZIP directo
- ✅ `GENERAR-INSTALADOR-MSIX.bat` - MSIX con VS

**Scripts PowerShell (automáticos):**
- `GENERAR-INSTALADOR-PORTABLE.ps1`
- `GENERAR-MSIX-VISUAL-STUDIO.ps1`
- `CREATE-MSIX-INSTALLER.ps1` (avanzado)

**Documentación:**
- `INICIO-RAPIDO.md` (este archivo)
- `COMO-GENERAR-INSTALADOR-SIMPLE.md`
- `Installer\README-*.md`

---

**🎯 Recomendación:** Si es tu primera vez, usa `GENERAR-INSTALADOR-MENU.bat`

**⏱️ Tiempo estimado:** 1-3 minutos dependiendo del método

**📧 Soporte:** soporte@gestiontime.com
