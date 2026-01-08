# 🚀 CREAR INSTALADOR MSI - GUÍA PASO A PASO

**Versión:** 1.2.0  
**Fecha:** 08/01/2026 10:35  
**Tipo:** Instalador MSI Profesional con WiX Toolset

---

## ✅ ARCHIVOS PREPARADOS

**Todos los archivos necesarios ya están creados:**

```
C:\GestionTime\GestionTimeDesktop\
├── Installer\
│   └── MSI\
│       ├── Product.wxs              ← Definición del producto MSI
│       ├── Features_Simple.wxs      ← Componentes a instalar
│       └── License.rtf              ← Licencia de uso
└── BUILD-MSI-FINAL.ps1              ← Script de compilación
```

---

## 📥 PASO 1: INSTALAR WIX TOOLSET (SOLO UNA VEZ)

### **Descargar WiX Toolset:**

```
https://wixtoolset.org/releases/
```

### **Pasos de instalación:**

1. **Descargar:**
   - Archivo: `wix314.exe` (WiX Toolset 3.14)
   - Tamaño: ~25 MB

2. **Ejecutar instalador:**
   - Doble-clic en `wix314.exe`
   - Click "Next" (siguiente)
   - Aceptar licencia
   - Click "Install" (instalar)
   - Esperar 2-3 minutos
   - Click "Finish" (finalizar)

3. **Reiniciar PowerShell**
   - Cerrar todas las ventanas de PowerShell
   - Abrir nueva ventana de PowerShell como Administrador

4. **Verificar instalación:**
   ```powershell
   Test-Path "C:\Program Files (x86)\WiX Toolset v3.14\bin\candle.exe"
   ```
   
   Debe devolver: `True`

---

## 🔨 PASO 2: COMPILAR EL MSI

### **Opción A: Ejecutar script automático** ⭐ **RECOMENDADO**

```powershell
# 1. Abrir PowerShell como Administrador
# 2. Navegar al proyecto
cd C:\GestionTime\GestionTimeDesktop

# 3. Ejecutar script
.\BUILD-MSI-FINAL.ps1
```

### **Resultado esperado:**

```
===============================================
  INSTALADOR MSI CREADO EXITOSAMENTE
===============================================

ARCHIVO MSI:
  C:\GestionTime\GestionTimeDesktop\Installer\Output\
  GestionTime-Desktop-1.2.0-Setup.msi

TAMAÑO:
  ~45-52 MB

VERSION:
  1.2.0
```

---

## 📦 CONTENIDO DEL MSI

El instalador MSI incluye:

✅ **Ejecutable Principal**
- GestionTime.Desktop.exe
- GestionTime.Desktop.dll

✅ **Configuración**
- appsettings.json
- GestionTime.Desktop.deps.json
- GestionTime.Desktop.runtimeconfig.json

✅ **Dependencias Principales**
- Newtonsoft.Json.dll
- Serilog.dll
- RestSharp.dll
- Microsoft.UI.Xaml.dll
- Microsoft.WindowsAppRuntime.dll
- Y muchas más...

✅ **Assets**
- app.ico (icono de la aplicación)
- LogoClaro.png
- LogoOscuro.png
- SplashScreen.scale-200.png
- StoreLogo.png

✅ **Runtimes Nativos**
- runtimes\win-x64\native\Microsoft.WindowsAppRuntime.Bootstrap.dll
- runtimes\win-x64\native\WebView2Loader.dll

✅ **Documentación**
- Manual de Usuario (MANUAL_USUARIO_GESTIONTIME_DESKTOP.md)

✅ **Accesos Directos**
- Menú Inicio → GestionTime Desktop
- Escritorio → GestionTime Desktop (opcional)
- Desinstalador en "Programas y características"

---

## 💾 INSTALAR EL MSI

### **Instalación Normal (GUI):**

```
1. Hacer doble-clic en:
   GestionTime-Desktop-1.2.0-Setup.msi

2. Asistente de instalación:
   - Aceptar licencia
   - Elegir directorio (por defecto: C:\Program Files\GestionTime\Desktop)
   - Click "Install"

3. Esperar 1-2 minutos

4. Buscar "GestionTime Desktop" en Menú Inicio
```

### **Instalación Silenciosa (Sin GUI):**

```cmd
msiexec /i "GestionTime-Desktop-1.2.0-Setup.msi" /qn /norestart
```

**Parámetros:**
- `/i` - Instalar
- `/qn` - Silencioso (sin interfaz)
- `/norestart` - No reiniciar automáticamente

---

## 🗑️ DESINSTALACIÓN

### **Desde Windows:**

```
Panel de Control → Programas y características → GestionTime Desktop → Desinstalar
```

### **Desde CMD (Silencioso):**

```cmd
msiexec /x "GestionTime-Desktop-1.2.0-Setup.msi" /qn /norestart
```

---

## 🛠️ SOLUCIÓN DE PROBLEMAS

### ❌ **Error: "WiX Toolset no encontrado"**

**Solución:**
```
1. Instalar WiX Toolset desde: https://wixtoolset.org/releases/
2. Descargar wix314.exe
3. Ejecutar instalador
4. Reiniciar PowerShell
5. Volver a ejecutar BUILD-MSI-FINAL.ps1
```

### ❌ **Error: "No se encuentra el ejecutable"**

**Solución:**
```powershell
# Compilar el proyecto primero
cd C:\GestionTime\GestionTimeDesktop
dotnet build -c Debug -r win-x64

# Verificar que existe
Test-Path "bin\x64\Debug\net8.0-windows10.0.19041.0\GestionTime.Desktop.exe"
```

### ❌ **Error al compilar: "Error CNDL0001"**

**Solución:**
```
1. Verificar que Product.wxs y Features_Simple.wxs existen
2. Verificar que las rutas en Features_Simple.wxs son correctas
3. Ejecutar como Administrador
```

### ❌ **Error al enlazar: "Error LGHT0001"**

**Solución:**
```
1. Verificar permisos de escritura en Installer\Output\
2. Cerrar otros MSI si están abiertos
3. Ejecutar como Administrador
```

---

## 📊 COMPARACIÓN: MSI vs ZIP Portable

| Característica | MSI (este) | ZIP Portable (anterior) |
|---------------|-----------|------------------------|
| **Instalación** | Asistente profesional | Copiar y ejecutar |
| **Accesos directos** | ✅ Automáticos | ⚠️ Manuales |
| **Registro en Windows** | ✅ Automático | ⚠️ Manual |
| **Desinstalador** | ✅ Integrado | ⚠️ Script BAT |
| **Group Policy** | ✅ Compatible | ❌ No |
| **Distribución** | ✅ Profesional | ✅ Simple |
| **Tamaño** | ~50 MB | 68 MB |
| **Requiere herramientas** | ✅ WiX Toolset | ❌ Ninguna |

---

## ✅ CHECKLIST PRE-DISTRIBUCIÓN

Antes de distribuir el MSI:

- [ ] WiX Toolset instalado (v3.14)
- [ ] Proyecto compilado (dotnet build)
- [ ] BUILD-MSI-FINAL.ps1 ejecutado sin errores
- [ ] MSI generado en Installer\Output\
- [ ] Tamaño del MSI > 40 MB
- [ ] Instalación probada en máquina limpia
- [ ] Desinstalación funciona correctamente
- [ ] Accesos directos funcionan
- [ ] Aplicación ejecuta sin errores

---

## 🎯 RESUMEN

**✅ ARCHIVOS LISTOS:**
- Product.wxs (definición del MSI)
- Features_Simple.wxs (componentes)
- License.rtf (licencia)
- BUILD-MSI-FINAL.ps1 (script de compilación)

**📥 INSTALAR WIX:**
```
https://wixtoolset.org/releases/ → wix314.exe
```

**🔨 COMPILAR MSI:**
```powershell
.\BUILD-MSI-FINAL.ps1
```

**📦 RESULTADO:**
```
Installer\Output\GestionTime-Desktop-1.2.0-Setup.msi
```

---

## 📞 SOPORTE

**Email:** soporte@gestiontime.com  
**Tel:** +34 900 123 456  
**GitHub:** https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop

---

**🎯 ¡Instalador MSI listo para crear!**

*Solo falta instalar WiX Toolset y ejecutar el script*

*Guía Completa MSI - Versión 1.2.0 - 08/01/2026*
