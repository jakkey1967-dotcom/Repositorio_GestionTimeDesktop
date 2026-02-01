# 🚀 INSTALADOR MSI PROFESIONAL
## GestionTime Desktop v1.2.0-beta
### WiX Toolset v3.14

---

## ✨ **CARACTERÍSTICAS DEL MSI:**

- ✅ **Instalador estándar de Windows** (.msi)
- ✅ **Instalación con un solo clic**
- ✅ **Actualización automática** (detecta versiones anteriores)
- ✅ **Desinstalación limpia** desde Panel de Control
- ✅ **Accesos directos automáticos** (Escritorio + Menú Inicio)
- ✅ **Integración con Windows** (registro, permisos, etc.)
- ✅ **Solo 64-bit** (Windows 10/11)

---

## 📋 **REQUISITOS:**

### 1. WiX Toolset v3.14 instalado
- Descarga: https://github.com/wixtoolset/wix3/releases/download/wix3141rtm/wix314.exe
- Ejecuta el instalador
- Verifica: `candle.exe -?`

### 2. Archivos publicados
```powershell
dotnet publish GestionTime.Desktop.csproj -c Release -r win-x64 --self-contained true -o publish\portable
```

---

## 🔧 **COMPILAR EL MSI:**

### Método 1: Script automático (RECOMENDADO)

```powershell
# Desde la carpeta WiX-v3-MSI
.\Build-MSI.ps1
```

El script hará todo automáticamente:
- ✅ Verifica archivos críticos
- ✅ Compila con Candle
- ✅ Genera MSI con Light
- ✅ Limpia archivos temporales

### Método 2: Manual

```powershell
# 1. Agregar WiX al PATH
$env:Path += ";C:\Program Files (x86)\WiX Toolset v3.14\bin"

# 2. Compilar con Candle
candle.exe Product.wxs -ext WixUIExtension -arch x64 -dSourceDir=..\publish\portable

# 3. Generar MSI con Light
light.exe Product.wixobj -ext WixUIExtension -out ..\installers\GestionTime-v1.2.0-beta.msi -sval
```

---

## 📦 **RESULTADO:**

Se generará el archivo:
```
installers\GestionTime-v1.2.0-beta.msi (~110 MB)
```

---

## 🧪 **PROBAR EL MSI:**

### Instalación:

1. **Ejecuta el MSI** (doble clic)
2. **Acepta la licencia**
3. **Clic en "Install"**
4. **Espera ~30 segundos**
5. **¡Listo!**

La aplicación se instalará en:
```
C:\Program Files\GestionTime Solutions\GestionTime Desktop\
```

### Accesos directos creados:

- ✅ **Escritorio:** `GestionTime Desktop.lnk`
- ✅ **Menú Inicio:** Busca "GestionTime Desktop"

### Desinstalación:

1. `Configuración` > `Aplicaciones` > `Aplicaciones instaladas`
2. Busca "GestionTime Desktop"
3. Clic en `Desinstalar`
4. Confirmar

---

## 📤 **PUBLICAR EN GITHUB:**

Una vez que el MSI esté creado y probado:

### 1. Crear nuevo release

Ve a: https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases/new

### 2. Configurar el release

- **Tag:** `v1.2.0-beta`
- **Title:** `GestionTime Desktop v1.2.0-beta`
- **Description:**
  ```markdown
  ## 🎯 GestionTime Desktop v1.2.0-beta
  
  ### ✨ Novedades
  - 🔄 Sistema de actualizaciones automático
  - 📊 Versión visible en login
  - 🎨 Mejoras en la interfaz
  - 📦 Instalador MSI profesional
  
  ### 📦 Instalación
  1. Descarga `GestionTime-v1.2.0-beta.msi`
  2. Ejecuta el instalador (doble clic)
  3. Sigue las instrucciones en pantalla
  
  ### 🗑️ Desinstalación
  - Configuración > Aplicaciones > GestionTime Desktop > Desinstalar
  
  ### ⚠️ Requisitos
  - Windows 10 (1809+) / Windows 11
  - Procesador x64 (64-bit)
  ```

### 3. Adjuntar archivos

- Arrastra `GestionTime-v1.2.0-beta.msi` (~110 MB)

### 4. Marcar como Pre-release

- ✅ Marca "This is a pre-release" (es una beta)

### 5. Publicar

- Clic en **"Publish release"** 🚀

---

## 🔧 **PERSONALIZACIÓN:**

### Cambiar versión:

Edita `Product.wxs` línea 14:
```xml
<?define ProductVersion = "1.2.0.0" ?>
```

### Cambiar directorio de instalación:

El MSI instala por defecto en:
```
C:\Program Files\GestionTime Solutions\GestionTime Desktop\
```

Para cambiar, edita `Product.wxs` líneas 54-58.

### Cambiar UpgradeCode:

**⚠️ IMPORTANTE:** El `UpgradeCode` debe ser el MISMO para todas las versiones de tu app. Solo cámbialo si es una app completamente diferente.

Está en `Product.wxs` línea 13:
```xml
<?define UpgradeCode = "12345678-1234-1234-1234-123456789012" ?>
```

---

## 🐛 **SOLUCIÓN DE PROBLEMAS:**

### Error: "candle.exe no reconocido"

**Solución:**
```powershell
$env:Path += ";C:\Program Files (x86)\WiX Toolset v3.14\bin"
```

### Error: "No se encontró publish\portable"

**Solución:**
```powershell
dotnet publish GestionTime.Desktop.csproj -c Release -r win-x64 --self-contained true -o publish\portable
```

### Error: "Falta GestionTime.Desktop.pri"

**Solución:**
El archivo `.pri` se genera durante `dotnet publish`. Asegúrate de que esté en `publish\portable\`.

### Error al instalar: "Este instalador requiere Windows Installer 2.0"

**Solución:**
Windows 10/11 ya incluyen Windows Installer 5.0+. Verifica que no estés en Windows 7/8.

### La app no arranca después de instalar

**Solución:**
1. Verifica que se copió `GestionTime.Desktop.pri`
2. Verifica que se copió la carpeta `Assets\`
3. Ejecuta el MSI como Administrador
4. Revisa logs en: `C:\Program Files\GestionTime Solutions\GestionTime Desktop\logs\`

---

## 📊 **COMPARATIVA: MSI vs ZIP**

| Característica | MSI (WiX v3) | ZIP + BAT |
|----------------|--------------|-----------|
| Tamaño archivo | ~110 MB | ~109 MB |
| Instalación | 1 clic | 2 clics (extraer + BAT) |
| Profesionalidad | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ |
| Privilegios Admin | ✅ Requerido | ✅ Requerido |
| Desinstalación | ✅ Panel Control | ✅ Panel Control |
| Actualización | ✅ Automática | ❌ Manual |
| Rollback | ✅ Sí | ❌ No |
| Registro Windows | ✅ Sí | ✅ Sí |
| Complejidad | 🟡 Media | 🟢 Baja |

---

## ✅ **CONCLUSIÓN:**

Este instalador MSI con WiX v3 es la **solución profesional definitiva** para GestionTime Desktop porque:

1. ✅ **Estándar de la industria** - Usado por Microsoft, Adobe, etc.
2. ✅ **Actualización automática** - Detecta y desinstala versiones anteriores
3. ✅ **Integración completa** - Registro, permisos, logs, etc.
4. ✅ **Experiencia profesional** - Los usuarios confían en archivos .msi
5. ✅ **Mantenible** - WiX v3 es estable y bien documentado

**¡A publicar! 🚀**

---

## 📞 **SOPORTE:**

- **Repositorio:** https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop
- **Issues:** https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/issues
- **WiX v3 Docs:** https://wixtoolset.org/docs/wix3/
