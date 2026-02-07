# 📦 GENERACIÓN DE INSTALADOR MSI LOCAL

**Fecha**: 06/02/2025  
**Versión**: v1.9.0 Beta  
**Backend**: Render (https://gestiontimeapi.onrender.com)  
**Método**: Compilación local (sin GitHub)

---

## ✅ PREREQUISITOS

### 1. **Software Requerido**

- ✅ **.NET 8 SDK**  
  Descarga: https://dotnet.microsoft.com/download/dotnet/8.0

- ✅ **WiX Toolset v3.14**  
  Descarga: https://github.com/wixtoolset/wix3/releases/download/wix3141rtm/wix314.exe

### 2. **Configuración de Backend**

- ✅ `appsettings.json` y `appsettings.Development.json` apuntando a Render
- ✅ BaseUrl: `https://gestiontimeapi.onrender.com`

---

## 🔍 VERIFICACIÓN PRE-BUILD

Antes de generar el MSI, ejecuta el script de verificación:

```powershell
.\Scripts\Verify-MSI-Prerequisites.ps1
```

**Verifica**:
1. Configuración de appsettings.json (debe apuntar a Render)
2. .NET 8 SDK instalado
3. WiX Toolset v3.14 instalado
4. Archivos Product.wxs y License.rtf presentes

---

## 🔨 GENERACIÓN DEL MSI

### **Opción 1: Build Completo (Recomendado)**

Compila, publica y genera el MSI en un solo paso:

```powershell
.\Scripts\Build-MSI-Local.ps1
```

### **Opción 2: Build Rápido (Sin Recompilar)**

Si ya tienes `publish\portable` generado:

```powershell
.\Scripts\Build-MSI-Local.ps1 -SkipPublish
```

### **Opción 3: Build Sin Abrir Carpeta**

```powershell
.\Scripts\Build-MSI-Local.ps1 -OpenFolder:$false
```

---

## 📂 ESTRUCTURA DE ARCHIVOS GENERADOS

```
GestionTime.Desktop/
├── publish/
│   └── portable/                       # Archivos publicados (355+ archivos)
│       ├── GestionTime.Desktop.exe
│       ├── GestionTime.Desktop.pri     # ⚠️ CRÍTICO: Recursos XAML
│       ├── appsettings.json            # Backend configurado a Render
│       ├── Assets/                     # 14 imágenes (logos, fondos)
│       └── [355+ DLLs de WinUI 3]
│
├── installers/
│   └── GestionTime-v1.9.0-win-x64.msi  # 🎯 INSTALADOR FINAL (~108 MB)
│
└── WiX-v3-MSI/
    ├── Product.wxs                     # Configuración del instalador
    ├── License.rtf                     # Licencia mostrada en instalador
    └── Files.wxs                       # ⚠️ Generado automáticamente (no editar)
```

---

## 🎯 RUTA DEL MSI GENERADO

```
C:\GestionTime\GestionTimeDesktop\installers\GestionTime-v1.9.0-win-x64.msi
```

**Tamaño**: ~108 MB (ZIP) → ~280 MB (instalado)

---

## 📋 PROCESO DETALLADO

El script `Build-MSI-Local.ps1` ejecuta los siguientes pasos:

### **[1/6] Verificación de Herramientas**
- Verifica que .NET 8 SDK esté instalado
- Verifica que WiX Toolset v3.14 esté instalado
- Agrega WiX al PATH si es necesario

### **[2/6] Publicación de la Aplicación**
```powershell
dotnet publish GestionTime.Desktop.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=false `
  -p:PublishReadyToRun=true `
  -o publish\portable
```

**Archivos críticos incluidos**:
- ✅ GestionTime.Desktop.exe (binario principal)
- ✅ GestionTime.Desktop.pri (recursos XAML compilados)
- ✅ appsettings.json (configuración de Render)
- ✅ Assets/ (14 imágenes de tema claro/oscuro)
- ✅ 355+ DLLs de WinUI 3, .NET 8, etc.

### **[3/6] Verificación de Archivos Críticos**
Valida que estén presentes:
- `GestionTime.Desktop.exe`
- `GestionTime.Desktop.pri` ⚠️ **CRÍTICO**
- `GestionTime.Desktop.dll`
- `appsettings.json`
- `Microsoft.WinUI.dll`
- `Assets\app_logo.ico`

### **[4/6] Generación de Files.wxs con Heat.exe**
```powershell
heat.exe dir "publish\portable" `
  -cg HarvestedFiles `
  -gg -scom -sreg -sfrag -srd `
  -dr INSTALLFOLDER `
  -var var.SourceDir `
  -out Files.wxs
```

**Resultado**: Archivo `Files.wxs` con la lista completa de archivos (355+)

### **[5/6] Compilación con Candle.exe**
```powershell
candle.exe Product.wxs Files.wxs `
  -dSourceDir="publish\portable" `
  -ext WixUtilExtension `
  -arch x64
```

**Resultado**: Archivos `.wixobj` (intermedios)

### **[6/6] Linkado con Light.exe**
```powershell
light.exe Product.wixobj Files.wixobj `
  -ext WixUIExtension `
  -ext WixUtilExtension `
  -out "installers\GestionTime-v1.9.0-win-x64.msi"
```

**Resultado**: `GestionTime-v1.9.0-win-x64.msi` (instalador final)

---

## 🖥️ INSTALACIÓN EN CLIENTE

### **Ruta de Instalación (FORZADA)**

El instalador **SIEMPRE** instala en:

```
C:\App\GestionTime-Desktop\
```

**⚠️ NO se puede cambiar** (está hardcodeado en `Product.wxs`):

```xml
<SetDirectory Id="INSTALLFOLDER" Value="C:\App\GestionTime-Desktop" />
```

### **Accesos Directos Creados**

1. **Menú Inicio**: `GestionTime Desktop`
2. **Escritorio**: `GestionTime Desktop`
3. **Desinstalar**: `Desinstalar GestionTime Desktop` (en Menú Inicio)

### **Permisos**

El instalador crea la carpeta `logs/` con permisos de escritura para el grupo "Users".

---

## ⚙️ CONFIGURACIÓN DEL INSTALADOR

### **Información del Producto (Product.wxs)**

```xml
<?define ProductName = "GestionTime Desktop" ?>
<?define ProductVersion = "1.9.0.0" ?>
<?define Manufacturer = "GestionTime Solutions" ?>
<?define UpgradeCode = "12345678-1234-1234-1234-123456789012" ?>
```

### **UpgradeCode (IMPORTANTE)**

- El `UpgradeCode` permite actualizar versiones anteriores automáticamente
- **NO cambiar** entre versiones, solo incrementar `ProductVersion`

### **Actualización Automática**

El instalador detecta versiones anteriores y las desinstala automáticamente:

```xml
<MajorUpgrade DowngradeErrorMessage="Ya existe una version mas reciente..." 
              AllowSameVersionUpgrades="yes" />
```

---

## 🔧 TROUBLESHOOTING

### **Error: "WiX Toolset no encontrado"**

**Solución**:
```powershell
# Descargar e instalar WiX v3.14
Start-Process "https://github.com/wixtoolset/wix3/releases/download/wix3141rtm/wix314.exe"
```

### **Error: "GestionTime.Desktop.pri no encontrado"**

**Causa**: El archivo `.pri` no se copió durante la publicación.

**Solución**:
1. Verificar que el `.csproj` tiene el target `CopyPriFile`:
   ```xml
   <Target Name="CopyPriFile" AfterTargets="Publish">
     <Copy SourceFiles="$(OutDir)GestionTime.Desktop.pri" 
           DestinationFolder="$(PublishDir)" />
   </Target>
   ```

2. Recompilar:
   ```powershell
   .\Scripts\Build-MSI-Local.ps1
   ```

### **Error: "Heat.exe falló"**

**Causa**: Carpeta `publish\portable` no existe o está vacía.

**Solución**:
```powershell
# Regenerar publish
.\Scripts\Build-MSI-Local.ps1
```

### **Error: "Light.exe falló"**

**Causa**: Archivo `License.rtf` no encontrado.

**Solución**:
```powershell
# Verificar que existe
Test-Path "WiX-v3-MSI\License.rtf"
```

---

## 🧪 TESTING POST-BUILD

### **1. Verificar Tamaño del MSI**

```powershell
$msi = Get-Item "installers\GestionTime-v1.9.0-win-x64.msi"
[math]::Round($msi.Length / 1MB, 2)  # Debe ser ~108 MB
```

### **2. Validar Configuración Incluida**

```powershell
# Extraer y verificar appsettings.json del publish
$appsettings = Get-Content "publish\portable\appsettings.json" | ConvertFrom-Json
$appsettings.Api.BaseUrl  # Debe ser: https://gestiontimeapi.onrender.com
```

### **3. Instalar en Máquina de Prueba**

1. Copiar MSI a máquina limpia (sin .NET instalado)
2. Ejecutar doble-click en el MSI
3. Verificar instalación en: `C:\App\GestionTime-Desktop\`
4. Ejecutar aplicación desde Menú Inicio
5. Login con credenciales de Render

---

## 📝 NOTAS ADICIONALES

### **Backend de Render**

El instalador incluye la configuración para conectarse automáticamente a:

```
https://gestiontimeapi.onrender.com
```

**Endpoints configurados**:
- `/api/v1/auth/login-desktop` (Login)
- `/api/v1/partes` (Partes)
- `/api/v1/catalog/clientes` (Clientes)
- `/api/v1/catalog/grupos` (Grupos)
- `/api/v1/catalog/tipos` (Tipos)
- `/api/v1/auth/me` (Perfil usuario)

### **Archivos Excluidos del MSI**

El instalador **NO incluye**:
- Carpeta `logs/` (se crea en instalación)
- Archivos `.pdb` (símbolos de debug)
- Archivos `.xml` de documentación
- Cache de usuario (en `AppData\Local`)

### **Versiones Futuras**

Para actualizar la versión:

1. Editar `Product.wxs`:
   ```xml
   <?define ProductVersion = "2.0.0.0" ?>
   ```

2. Regenerar MSI:
   ```powershell
   .\Scripts\Build-MSI-Local.ps1
   ```

3. El instalador detectará la versión anterior y la actualizará automáticamente.

---

## ✅ CHECKLIST FINAL

Antes de distribuir el MSI, verificar:

- [ ] Versión correcta en `Product.wxs`
- [ ] Backend apunta a Render (no localhost)
- [ ] Archivo `.pri` incluido en `publish\portable`
- [ ] Carpeta `Assets/` completa (14 imágenes)
- [ ] MSI genera correctamente (~108 MB)
- [ ] Instalación en máquina limpia exitosa
- [ ] Login funciona contra Render
- [ ] Tema claro/oscuro cambia correctamente (usa Assets)

---

## 🚀 DISTRIBUCIÓN

### **Ruta del MSI Final**

```
C:\GestionTime\GestionTimeDesktop\installers\GestionTime-v1.9.0-win-x64.msi
```

### **Métodos de Distribución**

1. **USB**: Copiar MSI a pendrive
2. **Red local**: Compartir carpeta `installers\`
3. **Email**: Comprimir MSI en ZIP (reduce a ~60 MB)
4. **OneDrive/Dropbox**: Subir MSI para descarga

### **Instrucciones para Usuario Final**

1. Descargar `GestionTime-v1.9.0-win-x64.msi`
2. Ejecutar con doble-click
3. Seguir asistente de instalación
4. Buscar "GestionTime Desktop" en Menú Inicio
5. Login con credenciales proporcionadas

---

**Generado**: 06/02/2025  
**Autor**: Build System  
**Proyecto**: GestionTime Desktop v1.9.0 Beta
