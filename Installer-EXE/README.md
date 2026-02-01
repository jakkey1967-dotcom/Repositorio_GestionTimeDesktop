# 🚀 INSTALADOR EXE AUTO-EXTRAÍBLE
## GestionTime Desktop v1.2.0-beta

## ✨ **VENTAJAS DE ESTE INSTALADOR:**

- ✅ **Un solo archivo EXE** (~110 MB)
- ✅ **No requiere WiX ni herramientas externas**
- ✅ **Instalación automática** con un solo clic
- ✅ **Crea accesos directos** en Escritorio y Menú Inicio
- ✅ **Desinstalación limpia** desde Panel de Control
- ✅ **Funciona 100% con WinUI 3**

---

## 📋 **CÓMO CREAR EL INSTALADOR:**

### **Paso 1: Compilar la aplicación**

```powershell
# Desde la raíz del proyecto
dotnet publish GestionTime.Desktop.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o publish\portable
```

### **Paso 2: Generar el instalador**

```powershell
# Ir a la carpeta del instalador
cd Installer-EXE

# Ejecutar el script
.\Build-Installer-EXE.ps1
```

### **Resultado:**

Se creará el archivo:
```
installers\GestionTime-Setup-v1.2.0-beta.exe (~110 MB)
```

---

## 📦 **PROBAR EL INSTALADOR:**

1. **Ejecuta el instalador como Administrador:**
   - Clic derecho > "Ejecutar como administrador"

2. **Sigue las instrucciones** en pantalla

3. **La app se instalará en:**
   ```
   C:\Program Files\GestionTime Solutions\GestionTime Desktop\
   ```

4. **Accesos directos creados en:**
   - Escritorio
   - Menú Inicio (busca "GestionTime Desktop")

---

## 🗑️ **DESINSTALAR:**

### **Opción 1: Panel de Control**
1. `Configuración` > `Aplicaciones` > `Aplicaciones instaladas`
2. Busca "GestionTime Desktop"
3. Clic en `Desinstalar`

### **Opción 2: Acceso directo**
1. Menú Inicio > "GestionTime Desktop"
2. Clic en "Desinstalar GestionTime Desktop"

---

## 📤 **PUBLICAR EN GITHUB:**

Una vez creado el instalador, súbelo a GitHub Releases:

1. Ve a: https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases/new

2. **Configura el release:**
   - **Tag:** `v1.2.0-beta`
   - **Title:** `GestionTime Desktop v1.2.0-beta`
   - **Description:**
     ```markdown
     ## 🎯 GestionTime Desktop v1.2.0-beta
     
     ### ✨ Novedades
     - 🔄 Sistema de actualizaciones automático
     - 📊 Versión visible en login
     - 🎨 Mejoras en la interfaz
     
     ### 📦 Instalación
     1. Descarga `GestionTime-Setup-v1.2.0-beta.exe`
     2. Ejecuta como Administrador
     3. Sigue las instrucciones en pantalla
     
     ### ⚠️ Requisitos
     - Windows 10 (1809+) / Windows 11
     - Procesador x64 (64-bit)
     ```

3. **Adjunta el archivo:**
   - Arrastra `GestionTime-Setup-v1.2.0-beta.exe`

4. **Marca como Pre-release** (es una beta)

5. **Publica** 🚀

---

## 🔧 **PERSONALIZACIÓN:**

### **Cambiar versión:**

Edita `Build-Installer-EXE.ps1` línea 9:
```powershell
[string]$Version = "1.2.0-beta"
```

### **Cambiar directorio de instalación:**

Edita `install.bat` línea 24:
```batch
set "INSTALL_DIR=%ProgramFiles%\GestionTime Solutions\GestionTime Desktop"
```

---

## 🐛 **SOLUCIÓN DE PROBLEMAS:**

### **Error: "No se encontró la carpeta publish\portable"**

**Solución:** Primero compila la app:
```powershell
dotnet publish GestionTime.Desktop.csproj -c Release -r win-x64 --self-contained true -o publish\portable
```

### **Error: "Este instalador requiere privilegios de Administrador"**

**Solución:** 
1. Clic derecho sobre el instalador
2. "Ejecutar como administrador"

### **La app no arranca después de instalar**

**Solución:**
1. Verifica que se copió `GestionTime.Desktop.pri`
2. Verifica que se copió la carpeta `Assets\`
3. Revisa logs en: `C:\Program Files\GestionTime Solutions\GestionTime Desktop\logs\`

---

## 📊 **COMPARATIVA: EXE vs MSI vs ZIP**

| Característica | EXE (IExpress) | MSI (WiX) | ZIP + BAT |
|----------------|----------------|-----------|-----------|
| Tamaño archivo | 1 archivo (~110 MB) | 1 archivo (~110 MB) | 1 archivo (~109 MB) |
| Instalación | 1 clic | 1 clic | Manual |
| Privilegios Admin | ✅ Requerido | ✅ Requerido | ✅ Requerido |
| Desinstalación | ✅ Panel Control | ✅ Panel Control | ❌ Manual |
| Accesos directos | ✅ Automático | ✅ Automático | ✅ Manual |
| Actualización | ⚠️ Manual | ✅ Automático | ❌ Manual |
| Complejidad | 🟢 Baja | 🔴 Alta | 🟢 Baja |
| Tiempo creación | 🟢 5 min | 🔴 60 min | 🟢 2 min |

---

## ✅ **CONCLUSIÓN:**

Este instalador EXE es la **mejor opción** para GestionTime Desktop v1.2.0-beta porque:

1. ✅ **Es profesional** - Se ve y funciona como cualquier instalador comercial
2. ✅ **Es simple** - No requiere aprender WiX ni herramientas complejas
3. ✅ **Es rápido** - Crear el instalador toma 5 minutos
4. ✅ **Funciona** - Probado con WinUI 3 y .NET 8
5. ✅ **Es mantenible** - Solo archivos BAT y PowerShell simples

**¡A publicar! 🚀**
