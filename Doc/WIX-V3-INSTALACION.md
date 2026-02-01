# ═══════════════════════════════════════════════════════════════
# INSTALADOR MSI DEFINITIVO CON WIX v3.14
# GestionTime Desktop v1.2.0-beta
# ═══════════════════════════════════════════════════════════════

## 📦 PASO 1: INSTALAR WIX TOOLSET v3.14

### Opción A: Descargar e instalar (RECOMENDADO)

1. **Descarga WiX v3.14:**
   - https://github.com/wixtoolset/wix3/releases/download/wix3141rtm/wix314.exe
   - Tamaño: ~27 MB

2. **Ejecuta el instalador:**
   - Doble clic en `wix314.exe`
   - Acepta la licencia
   - Instalación típica
   - Finish

3. **Verifica la instalación:**
   ```powershell
   candle.exe -?
   ```
   Debería mostrar la ayuda de WiX

### Opción B: Usar WiX sin instalación (Portable)

Si no quieres instalar, puedes usar WiX en modo portable:

1. Descarga: https://github.com/wixtoolset/wix3/releases/download/wix3141rtm/wix314-binaries.zip
2. Extrae a: `C:\Tools\wix314`
3. Agrega al PATH temporalmente:
   ```powershell
   $env:Path += ";C:\Tools\wix314"
   ```

---

## 📝 PASO 2: ESTRUCTURA DEL PROYECTO MSI

Después de instalar WiX v3, ejecuta:

```powershell
# Crear estructura
mkdir WiX-MSI-Installer
cd WiX-MSI-Installer

# El script generará automáticamente:
# - Product.wxs (definición del instalador)
# - Build-MSI.bat (script de compilación)
# - Files.wxs (lista de archivos - generado automáticamente)
```

---

## 🔧 PASO 3: COMPILAR EL MSI

```powershell
# Generar lista de archivos automáticamente
heat.exe dir "..\publish\portable" -cg ApplicationFiles -gg -scom -sreg -sfrag -srd -dr INSTALLFOLDER -out Files.wxs

# Compilar el instalador
candle.exe Product.wxs Files.wxs -ext WixUIExtension
light.exe -out GestionTime-v1.2.0-beta.msi Product.wixobj Files.wixobj -ext WixUIExtension -ext WixUtilExtension
```

---

## ✅ VENTAJAS DEL MSI CON WIX v3:

✅ **Profesional** - Estándar de Windows
✅ **Actualización automática** - Detecta versiones anteriores
✅ **Desinstalación limpia** - Panel de Control
✅ **Accesos directos** - Automáticos
✅ **Integración Windows** - Registro, permisos, etc.
✅ **Control total** - Puedes personalizar todo

---

## 📋 PRÓXIMOS PASOS:

### 1. Instala WiX v3.14

Descarga: https://github.com/wixtoolset/wix3/releases/download/wix3141rtm/wix314.exe

### 2. Yo crearé los archivos .wxs automáticamente

Una vez que confirmes que WiX v3 está instalado, generaré:
- `Product.wxs` - Configuración del instalador
- `Build-MSI.ps1` - Script automático de compilación
- Todo el proyecto MSI completo

---

## ⏱️ TIEMPO ESTIMADO:

- **Instalar WiX v3:** 2 minutos
- **Generar archivos .wxs:** Automático (yo lo hago)
- **Compilar MSI:** 30 segundos
- **Probar MSI:** 2 minutos

**TOTAL: ~5 minutos** después de instalar WiX

---

## 🎯 ¿PROCEDEMOS?

**Por favor:**

1. **Descarga e instala WiX v3.14:**
   - https://github.com/wixtoolset/wix3/releases/download/wix3141rtm/wix314.exe

2. **Verifica que funciona:**
   ```powershell
   candle.exe -?
   ```

3. **Dime cuando esté listo** y genero todo el proyecto MSI automáticamente

**¿Instalas WiX v3 ahora?**
