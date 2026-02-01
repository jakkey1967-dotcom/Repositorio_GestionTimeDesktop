# 📦 Instrucciones para Crear el Release v1.4.1-beta

## ✅ Estado Actual

- ✅ **Tag creado**: `v1.4.1-beta`
- ✅ **Tag subido a GitHub**: https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases/tag/v1.4.1-beta
- ✅ **Código actualizado**: Todas las versiones están en v1.4.1-beta
- ✅ **CHANGELOG.md**: Documentado
- ✅ **RELEASE_NOTES_v1.4.1-beta.md**: Creado
- ✅ **Build-Installer.ps1**: Actualizado a v1.4.1-beta

---

## 📋 Pasos para Completar el Release

### 1️⃣ Compilar el Instalador MSI

**Opción A: Desde PowerShell**
```powershell
cd C:\GestionTime\GestionTimeDesktop
powershell -ExecutionPolicy Bypass -File .\Build-Installer.ps1
```

**Opción B: Desde Visual Studio**
1. Abrir `GestionTime.Desktop.sln` en Visual Studio
2. Seleccionar configuración **Release** | **x64**
3. Click derecho en proyecto `GestionTime.Desktop` → **Publish**
4. Click derecho en proyecto `GestionTime.Installer` → **Build**

**Resultado esperado:**
- Archivo MSI generado en: `GestionTime.Installer\bin\x64\Release\`
- Copiado automáticamente a raíz: `GestionTime-Desktop-v1.4.1-beta-Setup.msi`

---

### 2️⃣ Verificar el MSI

**Archivos que debe contener:**
- ✅ GestionTime.Desktop.exe
- ✅ GestionTime.Desktop.pri
- ✅ Assets/ (14 imágenes)
- ✅ appsettings.json
- ✅ window-config.ini
- ✅ Todas las DLLs necesarias

**Prueba de instalación:**
```powershell
# Instalar
msiexec /i GestionTime-Desktop-v1.4.1-beta-Setup.msi /l*v install.log

# Verificar instalación
Test-Path "C:\Program Files\GestionTime\GestionTime Desktop\GestionTime.Desktop.exe"
```

---

### 3️⃣ Crear el Release en GitHub

#### Ir a GitHub Releases

1. Abrir: https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases
2. Click en **"Draft a new release"**

#### Configurar el Release

**Choose a tag:**
- Seleccionar: `v1.4.1-beta` (ya existe)

**Release title:**
```
🚀 GestionTime Desktop v1.4.1-beta
```

**Description:**
Copiar el contenido de `RELEASE_NOTES_v1.4.1-beta.md` (ya está formateado para Markdown de GitHub)

**Opciones:**
- ✅ **Set as a pre-release** (porque es beta)
- ❌ **Set as the latest release** (dejar sin marcar si hay una v1.0.0 estable)

---

### 4️⃣ Adjuntar Assets al Release

**Archivos a subir:**

1. **GestionTime-Desktop-v1.4.1-beta-Setup.msi** (Instalador principal)
   - Descripción: Instalador MSI para Windows 10/11

2. **GestionTime-Desktop-v1.4.1-beta-Portable.zip** (Opcional)
   - Comprimir la carpeta `publish\portable\`
   - Descripción: Versión portable sin instalación

3. **CHANGELOG.md** (Opcional)
   - Historial completo de cambios

**Arrastrar y soltar los archivos en la sección "Attach binaries"**

---

### 5️⃣ Publicar el Release

1. Revisar toda la información
2. Click en **"Publish release"**

**URL del Release:**
```
https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases/tag/v1.4.1-beta
```

---

## 📊 Checklist de Verificación

Antes de publicar, verificar:

- [ ] Tag `v1.4.1-beta` existe y está subido
- [ ] MSI compilado y probado localmente
- [ ] Tamaño del MSI razonable (~100-150 MB)
- [ ] Versión correcta en todas partes (app, diálogo, CHANGELOG)
- [ ] Release notes completas y formateadas
- [ ] Assets adjuntos correctamente
- [ ] Marcado como "pre-release" (beta)

---

## 🔗 Enlaces Importantes

- **Tag en GitHub**: https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases/tag/v1.4.1-beta
- **Código fuente**: https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/tree/v1.4.1-beta
- **Commits del release**: https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/compare/v1.0.0...v1.4.1-beta

---

## 📝 Comandos Git Útiles

```powershell
# Ver tags locales
git tag

# Ver tags remotos
git ls-remote --tags origin

# Ver detalles del tag
git show v1.4.1-beta

# Borrar tag (si necesitas recrearlo)
git tag -d v1.4.1-beta                    # Local
git push origin --delete v1.4.1-beta      # Remoto
```

---

## 🎯 Próximos Pasos (Después del Release)

1. ✅ Anunciar el release en comunicaciones internas
2. ✅ Actualizar wiki con nuevas funcionalidades
3. ✅ Cerrar issues relacionadas con las nuevas features
4. ✅ Comenzar planning para v1.4.2 o v1.5.0

---

## 🐛 Solución de Problemas

### Error al compilar MSI

**Problema**: WiX no encuentra los archivos
**Solución**:
```powershell
# Limpiar todo
dotnet clean GestionTime.Desktop.csproj
dotnet clean GestionTime.Installer/GestionTime.Installer.wixproj

# Re-publicar
dotnet publish GestionTime.Desktop.csproj -c Release -r win-x64
dotnet build GestionTime.Installer/GestionTime.Installer.wixproj -c Release
```

### MSI no instala correctamente

**Problema**: Faltan archivos después de instalar
**Solución**: Verificar que `publish\portable\` contenga todos los archivos antes de compilar el MSI

### Tag ya existe

**Problema**: `fatal: tag 'v1.4.1-beta' already exists`
**Solución**:
```powershell
# Borrar y recrear
git tag -d v1.4.1-beta
git push origin --delete v1.4.1-beta
git tag -a v1.4.1-beta -m "Release v1.4.1-beta"
git push origin v1.4.1-beta
```

---

**¡Listo para crear el release!** 🚀
