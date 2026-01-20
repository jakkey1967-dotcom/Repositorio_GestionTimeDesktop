# 🎯 Sistema de Versión Centralizada

## 📋 Descripción

Este proyecto usa un **sistema de versión centralizada** para evitar inconsistencias y facilitar el mantenimiento. La versión se define en un **único lugar** y se propaga automáticamente a todo el código.

---

## 🔧 Cómo Cambiar la Versión

### ✅ **FUENTE ÚNICA DE VERDAD**

Para cambiar la versión de la aplicación, modificar **SOLO** en el archivo:

```
Directory.Build.props
```

### 📝 Ejemplo:

```xml
<PropertyGroup>
  <!-- 🎯 VERSIÓN DE LA APLICACIÓN - Modificar SOLO AQUÍ -->
  <AppVersionMajor>1</AppVersionMajor>
  <AppVersionMinor>4</AppVersionMinor>
  <AppVersionPatch>2</AppVersionPatch>
  <AppVersionSuffix>-beta</AppVersionSuffix>  <!-- O vacío para release -->
</PropertyGroup>
```

### 📊 Versiones Calculadas Automáticamente

| Propiedad | Ejemplo | Uso |
|-----------|---------|-----|
| `AppVersion` | `1.4.1-beta` | Versión completa con sufijo |
| `AppVersionNumeric` | `1.4.1.0` | Versión numérica sin sufijo |
| `AppVersionMajor.Minor.Patch` | `1.4.1` | Versión semántica |

---

## 🏗️ Arquitectura

### 1️⃣ **Directory.Build.props** (Fuente Única)

Define las variables de versión que se propagan a todos los proyectos:

```xml
<AppVersionMajor>1</AppVersionMajor>
<AppVersionMinor>4</AppVersionMinor>
<AppVersionPatch>1</AppVersionPatch>
<AppVersionSuffix>-beta</AppVersionSuffix>
```

### 2️⃣ **GestionTime.Desktop.csproj** (Hereda)

Usa las variables del `Directory.Build.props`:

```xml
<AssemblyVersion>$(AppVersionNumeric)</AssemblyVersion>
<FileVersion>$(AppVersionNumeric)</FileVersion>
<Version>$(AppVersion)</Version>
<InformationalVersion>$(AppVersion)</InformationalVersion>
```

### 3️⃣ **VersionInfo.cs** (Acceso desde Código)

Clase estática que lee la versión del ensamblado:

```csharp
// Obtener versión desde código
var version = VersionInfo.Version;           // "1.4.1-beta"
var versionWithV = VersionInfo.VersionWithPrefix;  // "v1.4.1-beta"
var numeric = VersionInfo.VersionNumeric;    // "1.4.1.0"
```

---

## 📂 Archivos que Usan la Versión

### ✅ **Actualizados Automáticamente**

Estos archivos **NO requieren modificación manual**:

1. **LoginPage.xaml.cs** → Muestra versión usando `VersionInfo.VersionWithPrefix`
2. **UpdateService.cs** → Obtiene versión actual usando `VersionInfo.Version`
3. **DiarioPage.xaml.cs** → Título y contenido de notas de versión
4. **GestionTime.Desktop.csproj** → AssemblyVersion, FileVersion, InformationalVersion

### ⚠️ **Requieren Actualización Manual**

Estos archivos deben actualizarse manualmente al cambiar de versión:

1. **WiX-v3-MSI\Product.wxs** → `<?define ProductVersion = "1.4.1.0" ?>`
2. **Build-Installer.ps1** → `[string]$Version = "1.4.1-beta"`
3. **WiX-v3-MSI\Build-MSI.ps1** → `[string]$Version = "1.4.1-beta"`
4. **CHANGELOG.md** → Sección `[1.4.1-beta]`
5. **RELEASE_NOTES_*.md** → Título y contenido

---

## 🚀 Proceso de Release

### 1️⃣ **Actualizar Versión**

Editar `Directory.Build.props`:

```xml
<AppVersionMajor>1</AppVersionMajor>
<AppVersionMinor>4</AppVersionMinor>
<AppVersionPatch>2</AppVersionPatch>
<AppVersionSuffix></AppVersionSuffix>  <!-- Vacío para release -->
```

### 2️⃣ **Actualizar Archivos Manuales**

```powershell
# WiX Product.wxs
<?define ProductVersion = "1.4.2.0" ?>

# Build-Installer.ps1
[string]$Version = "1.4.2"

# CHANGELOG.md
## [1.4.2] - 2026-01-XX
```

### 3️⃣ **Compilar**

```powershell
# Limpiar
dotnet clean GestionTime.Desktop.csproj -c Release

# Publicar
dotnet publish GestionTime.Desktop.csproj -c Release -r win-x64 --self-contained true -o "publish\portable"

# Crear MSI
cd WiX-v3-MSI
.\Build-MSI.ps1
```

### 4️⃣ **Verificar**

Ejecutar la aplicación y verificar:

- LoginPage: `v1.4.2` ✅
- Menú Ayuda → Notas de Versión: `1.4.2` ✅
- Propiedades del .exe: `1.4.2.0` ✅

---

## 🎯 Ventajas del Sistema

### ✅ **Consistencia Garantizada**

- Versión única definida en `Directory.Build.props`
- Propagación automática a ensamblados
- Acceso desde código con `VersionInfo`

### ✅ **Menos Errores**

- No más versiones diferentes en login vs MSI
- Fácil de actualizar (un solo lugar)
- Menos archivos para modificar manualmente

### ✅ **Mejor Mantenimiento**

- Código más limpio (sin lógica de versión duplicada)
- Documentación clara del proceso
- Fácil de entender para nuevos desarrolladores

---

## 🔍 Ejemplo Completo de Cambio de Versión

### De v1.4.1-beta → v1.4.2 (Release)

#### **Paso 1: Directory.Build.props**

```xml
<!-- ANTES -->
<AppVersionPatch>1</AppVersionPatch>
<AppVersionSuffix>-beta</AppVersionSuffix>

<!-- DESPUÉS -->
<AppVersionPatch>2</AppVersionPatch>
<AppVersionSuffix></AppVersionSuffix>  <!-- Vacío = release -->
```

#### **Paso 2: WiX-v3-MSI\Product.wxs**

```xml
<!-- ANTES -->
<?define ProductVersion = "1.4.1.0" ?>

<!-- DESPUÉS -->
<?define ProductVersion = "1.4.2.0" ?>
```

#### **Paso 3: Compilar**

```powershell
dotnet clean -c Release
dotnet publish -c Release -r win-x64 --self-contained true -o "publish\portable"
cd WiX-v3-MSI
.\Build-MSI.ps1
```

#### **Resultado:**

- ✅ LoginPage muestra: `v1.4.2`
- ✅ Notas de versión: `GestionTime Desktop v1.4.2`
- ✅ UpdateService detecta: `1.4.2`
- ✅ Ensamblado: `1.4.2.0`
- ✅ MSI: `GestionTime-1.4.2.msi`

---

## 📝 Checklist para Cambiar Versión

- [ ] Modificar `Directory.Build.props` (AppVersionMajor/Minor/Patch/Suffix)
- [ ] Actualizar `WiX-v3-MSI\Product.wxs` (ProductVersion)
- [ ] Actualizar `Build-Installer.ps1` (Version)
- [ ] Actualizar `WiX-v3-MSI\Build-MSI.ps1` (Version)
- [ ] Actualizar `CHANGELOG.md` (nueva sección [X.X.X])
- [ ] Crear `RELEASE_NOTES_vX.X.X.md`
- [ ] Compilar y verificar versión en LoginPage
- [ ] Crear MSI y verificar propiedades del archivo
- [ ] Crear tag de git: `git tag -a vX.X.X -m "Release vX.X.X"`
- [ ] Push del tag: `git push origin vX.X.X`
- [ ] Crear GitHub Release con el MSI

---

## 🛠️ Troubleshooting

### ❌ Problema: LoginPage muestra versión incorrecta

**Causa**: Ensamblado no recompilado con la nueva versión.

**Solución**:
```powershell
dotnet clean -c Release
dotnet build -c Release
```

### ❌ Problema: MSI tiene versión antigua dentro

**Causa**: MSI compilado antes de actualizar la versión.

**Solución**:
```powershell
# 1. Eliminar carpeta publish
Remove-Item -Recurse -Force publish\portable

# 2. Publicar de nuevo
dotnet publish -c Release -r win-x64 --self-contained true -o "publish\portable"

# 3. Compilar MSI
cd WiX-v3-MSI
.\Build-MSI.ps1
```

### ❌ Problema: UpdateService no detecta nueva versión

**Causa**: Versión en `Directory.Build.props` no incrementada correctamente.

**Solución**: Verificar que `AppVersionPatch` o `AppVersionMinor` sea mayor que la versión anterior.

---

**¿Preguntas?** Revisa el código de `VersionInfo.cs` o contacta al equipo de desarrollo.
