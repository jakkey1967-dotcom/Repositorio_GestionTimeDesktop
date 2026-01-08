# 🚀 INSTALADOR PROFESIONAL - GESTIONTIME DESKTOP

**Versión:** 1.2.0  
**Fecha:** Enero 2026  
**Tipo:** Instalador EXE con Inno Setup (incluye TODAS las carpetas)

---

## ✅ SOLUCIÓN FINAL - INSTALADOR COMPLETO

Este instalador incluye **ABSOLUTAMENTE TODO** lo necesario para ejecutar GestionTime Desktop:

### 📦 Contenido Completo

✅ **Ejecutable Principal**
- `GestionTime.Desktop.exe`
- `GestionTime.Desktop.dll`

✅ **Configuración**
- `appsettings.json`
- Archivos `.deps.json`
- Archivos `.runtimeconfig.json`

✅ **Dependencias (.NET 8 + WinUI 3)**
- Todas las DLLs necesarias
- Bibliotecas de terceros (Newtonsoft.Json, Serilog, etc.)
- Microsoft.UI.Xaml completo

✅ **Assets (Recursos)**
- Iconos (app.ico, logos, etc.)
- Imágenes de fondo
- Splash screens
- Logos claros y oscuros

✅ **Runtimes (Bibliotecas Nativas)**
- `runtimes\win-x64\` (x64 - principal)
- `runtimes\win-x86\` (x86 - compatibilidad)
- `runtimes\win-arm64\` (ARM64 - futuro)

✅ **Documentación**
- Manual de Usuario completo
- Readme.txt
- License.txt

✅ **Accesos Directos**
- Menú Inicio
- Escritorio (opcional)
- Desinstalador

---

## 🎯 PASO 1: INSTALAR INNO SETUP (SOLO UNA VEZ)

### **¿Qué es Inno Setup?**

Inno Setup es un **creador de instaladores GRATUITO** y profesional usado por miles de aplicaciones (incluyendo VS Code, Discord, etc.).

### **Instalación (5 minutos)**

1. **Descargar Inno Setup:**
   ```
   https://jrsoftware.org/isdl.php
   ```

2. **Ejecutar el instalador:**
   - Descargar: `innosetup-6.x.x.exe`
   - Hacer doble-clic
   - Click en "Next" hasta "Finish"

3. **Verificar instalación:**
   ```powershell
   Test-Path "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
   ```
   
   Debe devolver: `True`

---

## 🚀 PASO 2: CREAR EL INSTALADOR

### **Ejecución Automática (Recomendado)**

1. **Abrir PowerShell como Administrador:**
   ```
   Click derecho en PowerShell → "Ejecutar como administrador"
   ```

2. **Navegar al directorio del proyecto:**
   ```powershell
   cd C:\GestionTime\GestionTimeDesktop
   ```

3. **Ejecutar el script:**
   ```powershell
   .\CREATE-INSTALLER-COMPLETE-V3.ps1
   ```

4. **Resultado esperado:**
   ```
   ═══════════════════════════════════════════════════════
     ✅ INSTALADOR CREADO EXITOSAMENTE
   ═══════════════════════════════════════════════════════
   
   📦 ARCHIVO:
      C:\GestionTime\GestionTimeDesktop\Installer\Output\
      GestionTime-Desktop-1.2.0-Setup.exe
   
   📊 TAMAÑO:
      52.3 MB
   
   📋 COMPONENTES INCLUIDOS:
      ✓ Ejecutable principal
      ✓ Todas las DLLs
      ✓ Assets completos
      ✓ Runtimes (x64, x86, arm64)
      ✓ Documentacion
      ✓ Configuracion
   ```

### **Parámetros Opcionales**

```powershell
# Cambiar directorio de origen
.\CREATE-INSTALLER-COMPLETE-V3.ps1 -SourceDir "C:\OtraRuta"
```

---

## 📂 ESTRUCTURA GENERADA

```
C:\GestionTime\GestionTimeDesktop\
├── Installer\
│   ├── GestionTime-Setup.iss      ← Script de Inno Setup
│   ├── License.txt                ← Licencia de uso
│   ├── Readme.txt                 ← Guía rápida
│   └── Output\
│       └── GestionTime-Desktop-1.2.0-Setup.exe  ← 🎯 INSTALADOR FINAL
```

---

## 💾 INSTALACIÓN DEL PROGRAMA

### **Instalación Normal (Con GUI)**

1. Hacer **doble-clic** en:
   ```
   GestionTime-Desktop-1.2.0-Setup.exe
   ```

2. El instalador mostrará:
   - 📄 Licencia de uso (aceptar)
   - 📁 Directorio de instalación (por defecto: `C:\Program Files\GestionTime\Desktop`)
   - 🖼️ Iconos adicionales (escritorio, inicio rápido)
   - ⚙️ Confirmación final

3. Click en **"Instalar"**

4. Esperar a que termine (1-2 minutos)

5. **✅ Listo!** Buscar "GestionTime Desktop" en el Menú Inicio

### **Instalación Silenciosa (Sin GUI)**

```cmd
GestionTime-Desktop-1.2.0-Setup.exe /VERYSILENT /NORESTART
```

**Parámetros:**
- `/VERYSILENT` - Sin ventanas ni mensajes
- `/NORESTART` - No reiniciar automáticamente
- `/DIR="C:\MiCarpeta"` - Directorio personalizado
- `/LOG="C:\install.log"` - Guardar log de instalación

**Ejemplo completo:**
```cmd
GestionTime-Desktop-1.2.0-Setup.exe /VERYSILENT /NORESTART /DIR="C:\GestionTime" /LOG="C:\install_log.txt"
```

---

## 🗑️ DESINSTALACIÓN

### **Desde Windows**

```
Panel de Control → Programas y características → GestionTime Desktop → Desinstalar
```

O:

```
Menú Inicio → GestionTime Desktop → Desinstalar GestionTime Desktop
```

### **Desde CMD (Silencioso)**

```cmd
"C:\Program Files\GestionTime\Desktop\unins000.exe" /VERYSILENT /NORESTART
```

---

## 📦 CONTENIDO DETALLADO DEL INSTALADOR

### **Archivos Instalados**

```
C:\Program Files\GestionTime\Desktop\
├── GestionTime.Desktop.exe         ← Ejecutable principal (5.2 MB)
├── GestionTime.Desktop.dll         ← Biblioteca principal
├── appsettings.json                ← Configuración
├── *.dll                           ← Dependencias (.NET, WinUI 3, etc.)
├── *.deps.json                     ← Descriptores de dependencias
├── *.runtimeconfig.json            ← Configuración de runtime
│
├── Assets\                         ← Recursos visuales
│   ├── app.ico                     ← Icono de la aplicación
│   ├── LogoClaro.png               ← Logo tema claro
│   ├── LogoOscuro.png              ← Logo tema oscuro
│   ├── diario_bg_dark.png          ← Fondo oscuro
│   ├── Diario_bg_claro.png         ← Fondo claro
│   └── ... (más iconos y sprites)
│
├── runtimes\                       ← Bibliotecas nativas
│   ├── win-x64\                    ← Windows x64 (principal)
│   │   └── native\
│   │       ├── Microsoft.WindowsAppRuntime.Bootstrap.dll
│   │       ├── WebView2Loader.dll
│   │       └── ... (más DLLs nativas)
│   ├── win-x86\                    ← Windows x86 (compatibilidad)
│   └── win-arm64\                  ← Windows ARM64 (futuro)
│
├── Microsoft.UI.Xaml\              ← WinUI 3 Assets
│   └── Assets\
│       └── NoiseAsset_256x256_PNG.png
│
├── Docs\                           ← Documentación
│   ├── MANUAL_USUARIO_GESTIONTIME_DESKTOP.md
│   └── ... (más documentos)
│
├── Readme.txt                      ← Guía rápida
├── License.txt                     ← Licencia
└── unins000.exe                    ← Desinstalador
```

### **Tamaño Total**

- **Instalador comprimido:** ~52 MB
- **Instalado en disco:** ~145 MB

---

## 🛠️ SOLUCIÓN DE PROBLEMAS

### ❌ **Error: "Inno Setup no encontrado"**

**Solución:**
```powershell
# Descargar e instalar desde:
https://jrsoftware.org/isdl.php

# Verificar:
Test-Path "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
```

### ❌ **Error: "No se encontró GestionTime.Desktop.exe"**

**Solución:**
```powershell
# Compilar el proyecto primero:
cd C:\GestionTime\GestionTimeDesktop
dotnet build -c Debug -r win-x64

# Verificar:
Test-Path ".\bin\x64\Debug\net8.0-windows10.0.19041.0\GestionTime.Desktop.exe"
```

### ❌ **Error: "Access Denied" al compilar**

**Solución:**
```powershell
# Ejecutar PowerShell como Administrador
# Click derecho en PowerShell → "Ejecutar como administrador"
```

### ❌ **El instalador no incluye todos los archivos**

**Verificar:**
```powershell
# Revisar que existan las carpetas:
Test-Path ".\bin\x64\Debug\net8.0-windows10.0.19041.0\Assets"
Test-Path ".\bin\x64\Debug\net8.0-windows10.0.19041.0\runtimes"
Test-Path ".\bin\x64\Debug\net8.0-windows10.0.19041.0\Docs"
```

---

## ✅ CHECKLIST PRE-DISTRIBUCIÓN

Antes de distribuir el instalador:

- [ ] Inno Setup instalado (v6.x)
- [ ] Proyecto compilado correctamente
- [ ] Archivo `GestionTime.Desktop.exe` existe
- [ ] Carpeta `Assets` completa
- [ ] Carpeta `runtimes` completa
- [ ] Carpeta `Docs` completa (opcional)
- [ ] Script ejecutado sin errores
- [ ] Instalador generado en `Installer\Output`
- [ ] Tamaño del instalador > 50 MB
- [ ] Instalación probada en máquina limpia
- [ ] Desinstalación funciona
- [ ] Accesos directos funcionan
- [ ] Aplicación ejecuta sin errores

---

## 📊 VENTAJAS DE ESTE INSTALADOR

### **✅ Completo**
- Incluye **TODOS** los archivos necesarios
- Todas las carpetas (Assets, runtimes, Docs)
- Sin dependencias externas

### **✅ Profesional**
- Asistente gráfico estilo Windows
- Licencia personalizada
- Readme informativo
- Desinstalador automático

### **✅ Fácil de Usar**
- Doble-clic para instalar
- Sin configuración manual
- Accesos directos automáticos

### **✅ Flexible**
- Instalación normal (con GUI)
- Instalación silenciosa (sin GUI)
- Directorio personalizable

### **✅ Mantenible**
- Script generado automáticamente
- Fácil de actualizar (cambiar versión)
- Sin archivos hardcodeados

---

## 📞 SOPORTE

**Email:** soporte@gestiontime.com  
**Tel:** +34 900 123 456  
**GitHub:** https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop

---

## 📄 LICENCIA

**GestionTime Desktop** © 2026 Global Retail Solutions  
Todos los derechos reservados.

---

## 🎯 COMPARATIVA: MSI vs EXE (Inno Setup)

| Característica | MSI (WiX) | EXE (Inno Setup) |
|---------------|-----------|------------------|
| **Dificultad de creación** | 🔴 Difícil | 🟢 Fácil |
| **Instalación requerida** | WiX Toolset (complejo) | Inno Setup (simple) |
| **Tamaño del instalador** | ~45 MB | ~52 MB |
| **Velocidad de instalación** | Lenta (2-3 min) | Rápida (1-2 min) |
| **Interfaz** | Estándar Windows | Personalizable |
| **Instalación silenciosa** | ✅ Sí | ✅ Sí |
| **Group Policy** | ✅ Sí (empresas) | ❌ No |
| **Facilidad de distribución** | ✅ Sí | ✅ Sí |
| **Desinstalación limpia** | ✅ Sí | ✅ Sí |

**Recomendación:** **Inno Setup (EXE)** para la mayoría de casos.

---

**🎯 ¡Instalador listo para distribuir!**

*Guía Completa - Versión 1.2.0 - Enero 2026*
