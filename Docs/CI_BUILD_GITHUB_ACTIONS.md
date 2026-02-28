# 🤖 GitHub Actions — Build MSI Automático

## Workflow: `.github/workflows/release.yml`

### ¿Cuándo se ejecuta?

| Evento | Acción |
|--------|--------|
| Push de tag `v*` (ej: `v2.0.0-beta`) | Build MSI + crea GitHub Release |
| `workflow_dispatch` (manual) | Solo build → artifact descargable |

---

## 🚀 Cómo publicar una nueva versión

### 1. Actualizar la versión en `Directory.Build.props`

```xml
<AppVersionMajor>2</AppVersionMajor>
<AppVersionMinor>1</AppVersionMinor>
<AppVersionPatch>0</AppVersionPatch>
<AppVersionSuffix></AppVersionSuffix>   <!-- vacío = release estable -->
```

### 2. Hacer commit y push del tag

```bash
git add Directory.Build.props
git commit -m "chore: bump version to 2.1.0"
git tag v2.1.0
git push origin main --tags
```

### 3. GitHub Actions hace todo lo demás

- ✅ Restaura NuGet
- ✅ Instala WiX Toolset v3.14
- ✅ `dotnet publish` (win-x64, self-contained)
- ✅ `heat → candle → light` → `.msi`
- ✅ Crea el Release en GitHub con el `.msi` adjunto

---

## 📁 Archivos involucrados

| Archivo | Rol |
|---------|-----|
| `.github/workflows/release.yml` | Workflow principal de CI/CD |
| `Scripts/Build-MSI-CI.ps1` | Script de build sin paths hardcodeados |
| `WiX-v3-MSI/Product.wxs` | Definición WiX (versión parametrizable con `-dProductVersion=`) |
| `Directory.Build.props` | **Fuente única de versión** |

---

## 🔧 Build local (sin CI)

```powershell
# Build completo
.\Scripts\Build-MSI-Local.ps1

# Build con script CI (también funciona en local si WiX está en PATH)
.\Scripts\Build-MSI-CI.ps1
```

---

## ⚠️ Notas importantes

### Certificado
El MSI usa un certificado **autofirmado**. Para silenciar SmartScreen en clientes:
1. Comprar certificado OV Sectigo (~70€/año), o
2. Instruir a usuarios: "Más información → Ejecutar de todas formas"

### `appsettings.json`
El build de CI usa el `appsettings.json` del repositorio (Render: `gestiontimeapi.onrender.com`).  
**No subir al repo** credenciales de desarrollo en este archivo.

### Pre-release automático
Si `AppVersionSuffix` contiene `beta`, `alpha` o `rc`, el Release se marca como **Pre-release** en GitHub automáticamente.
