# 📦 INSTALADOR MSI v1.9.0 Beta - Guía Completa

**Fecha**: 30 de Enero, 2025  
**Versión**: 1.9.0 Beta  
**Build**: .NET 8, WinUI 3  

---

## ✅ ARCHIVOS INCLUIDOS EN INSTALADOR

### 📄 Documentación Nueva (v1.9.0)
- ✅ **CHANGELOG.md** - Changelog detallado completo (3800+ líneas)
- ✅ **RELEASE_NOTES_v1.9.0.md** - Notas de versión concisas (1200 líneas)

### 🚀 Aplicación Principal
- ✅ **GestionTime.Desktop.exe** - Ejecutable principal
- ✅ **GestionTime.Desktop.pri** - Recursos XAML compilados (crítico)
- ✅ **window-config.ini** - Configuración de ventana
- ✅ **appsettings.json** - Configuración de API

### 🎨 Assets (14 archivos)
- login_fondoClaro.png / login_fondoOscuro.png
- diario_bg_Claro.png / diario_bg_Oscuro.png
- LogoClaro.png / LogoOscuro.png
- app_logo.ico
- Y más...

### 📦 Runtime (355+ DLLs)
- .NET 8 Desktop Runtime (auto-contenido)
- WinUI 3 Runtime Components
- Microsoft.Extensions.* (Logging, DI, Configuration)
- System.* (Threading, IO, Http)

---

## 🎯 UBICACIÓN DE INSTALACIÓN

**RUTA FIJA (NO MODIFICABLE):**
```
C:\App\GestionTime-Desktop\
```

**Motivo**: La aplicación requiere permisos de escritura en su carpeta para logs, cache y configuración. `Program Files` tiene restricciones de UAC.

---

## 📋 PROCESO DE BUILD MSI

### Comando Automatizado (Recomendado)
```powershell
.\Scripts\Build-MSI-v1.9.0-Beta.ps1
```

**Pasos del script:**
1. ✅ Verifica WiX Toolset v3.14 instalado
2. ✅ Publica aplicación con `dotnet publish`
3. ✅ Copia archivos críticos (.pri, Assets, window-config.ini)
4. ✅ Copia documentación (CHANGELOG.md, RELEASE_NOTES_v1.9.0.md)
5. ✅ Genera componentes WiX con `heat.exe`
6. ✅ Compila MSI con `candle.exe` + `light.exe`
7. ✅ Verifica MSI generado

### Build Manual (Avanzado)
```powershell
# 1. Publicar aplicación
dotnet publish GestionTime.Desktop.csproj -c Release -r win-x64 --self-contained true -o "publish\portable"

# 2. Copiar archivos críticos
Copy-Item "CHANGELOG.md" -Destination "publish\portable\"
Copy-Item "RELEASE_NOTES_v1.9.0.md" -Destination "publish\portable\"
Copy-Item "bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\GestionTime.Desktop.pri" -Destination "publish\portable\"

# 3. Generar componentes WiX
cd WiX-v3-MSI
heat.exe dir "..\publish\portable" -cg HarvestedFiles -gg -sfrag -srd -dr INSTALLFOLDER -var "var.SourceDir" -out "HarvestedFiles.wxs"

# 4. Compilar MSI
candle.exe Product.wxs HarvestedFiles.wxs -ext WixUIExtension -ext WixUtilExtension -arch x64
light.exe Product.wixobj HarvestedFiles.wixobj -ext WixUIExtension -ext WixUtilExtension -out "GestionTime-v1.9.0-Setup.msi"
```

---

## 🛠️ REQUISITOS PARA BUILD

### Software Requerido
- ✅ **Visual Studio 2022** (o .NET 8 SDK standalone)
- ✅ **WiX Toolset v3.14** - [Descargar aquí](https://github.com/wixtoolset/wix3/releases/tag/wix3141rtm)
- ✅ **PowerShell 5.1+** (Windows 10/11 incluye)

### Verificar Instalación
```powershell
.\Scripts\Verify-MSI-Prerequisites.ps1
```

---

## 📝 CAMBIOS EN WIX PRODUCT.WXS (v1.9.0)

### Nuevo Component: DocumentationFiles
```xml
<Component Id="DocumentationFiles" Guid="C3333333-3333-3333-3333-333333333333">
  <File Id="ChangelogMd" 
        Source="$(var.SourceDir)\CHANGELOG.md" 
        KeyPath="yes" />
  <File Id="ReleaseNotesMd" 
        Source="$(var.SourceDir)\RELEASE_NOTES_v1.9.0.md" />
  
  <RegistryValue Root="HKCU" 
                 Key="Software\GestionTime Solutions\GestionTime Desktop" 
                 Name="DocsInstalled" 
                 Type="integer" 
                 Value="1" />
</Component>
```

### Feature Actualizado
```xml
<Feature Id="Complete" Title="$(var.ProductName)" Level="1">
  <ComponentGroupRef Id="HarvestedFiles" />
  <ComponentRef Id="ApplicationShortcut" />
  <ComponentRef Id="DesktopShortcut" />
  <ComponentRef Id="LogsFolder" />
  <ComponentRef Id="DocumentationFiles" /> <!-- NUEVO -->
</Feature>
```

---

## 🧪 TESTING DEL INSTALADOR

### Checklist Pre-Distribución
- [ ] MSI compila sin errores
- [ ] Instalar en VM o PC limpio
- [ ] Verificar que crea carpeta `C:\App\GestionTime-Desktop\`
- [ ] Verificar que copia todos los archivos (exe, pri, Assets, docs)
- [ ] Verificar que crea shortcuts (Start Menu + Desktop)
- [ ] Ejecutar aplicación y verificar funcionalidad
- [ ] Verificar que CHANGELOG.md está presente en carpeta instalación
- [ ] Verificar que RELEASE_NOTES_v1.9.0.md está presente
- [ ] Desinstalar y verificar que limpia correctamente

### Verificación Rápida Post-Instalación
```powershell
# Verificar archivos críticos
Test-Path "C:\App\GestionTime-Desktop\GestionTime.Desktop.exe"
Test-Path "C:\App\GestionTime-Desktop\GestionTime.Desktop.pri"
Test-Path "C:\App\GestionTime-Desktop\CHANGELOG.md"
Test-Path "C:\App\GestionTime-Desktop\RELEASE_NOTES_v1.9.0.md"
Test-Path "C:\App\GestionTime-Desktop\Assets\LogoClaro.png"

# Verificar shortcuts
Test-Path "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\GestionTime Desktop\GestionTime Desktop.lnk"
Test-Path "$env:USERPROFILE\Desktop\GestionTime Desktop.lnk"
```

---

## 🎯 DISTRIBUCIÓN

### Tamaño Esperado
- **MSI**: ~110-120 MB
- **Instalado**: ~280-300 MB

### Nombre de Archivo
```
GestionTime-v1.9.0-Setup.msi
```

### Hash (Opcional para Verificación)
```powershell
Get-FileHash "WiX-v3-MSI\GestionTime-v1.9.0-Setup.msi" -Algorithm SHA256
```

---

## 📦 CREAR TAG DE GIT

Después de verificar que MSI funciona correctamente:

```powershell
# 1. Commit de cambios finales
git add .
git commit -m "Release v1.9.0 Beta - MSI con documentación completa"

# 2. Crear tag anotado
git tag -a v1.9.0-beta -m "Release v1.9.0 Beta

Principales mejoras:
- Sistema de Tags (5 por parte)
- Notas de Cliente
- Hora de Inicio Inteligente
- Exportación Excel mejorada (10,000 partes)
- Sistema de Usuarios Online
- Roles y Permisos
- 35+ fixes críticos

Ver CHANGELOG.md para detalles completos."

# 3. Push a GitHub
git push origin main
git push origin v1.9.0-beta

# 4. Crear Release en GitHub
# - Ir a https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases
# - Click "Draft a new release"
# - Tag version: v1.9.0-beta
# - Release title: "GestionTime Desktop v1.9.0 Beta"
# - Description: Pegar contenido de RELEASE_NOTES_v1.9.0.md
# - Upload MSI: GestionTime-v1.9.0-Setup.msi
# - Marcar "This is a pre-release" (es BETA)
# - Click "Publish release"
```

---

## 🐛 TROUBLESHOOTING

### Error: "WiX Toolset no instalado"
**Solución**: Descargar e instalar WiX Toolset v3.14 desde:
https://github.com/wixtoolset/wix3/releases/tag/wix3141rtm

### Error: "candle.exe no encontrado"
**Solución**: Agregar WiX al PATH:
```powershell
$env:Path += ";C:\Program Files (x86)\WiX Toolset v3.14\bin"
```

### Error: "light.exe falla con ICE61/ICE69"
**Solución**: Ya está suprimido en script con `-sice:ICE61 -sice:ICE69`

### Error: ".pri no se copia"
**Solución**: Verificar que existe en:
```
bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\GestionTime.Desktop.pri
```

Si no existe, compilar con:
```powershell
dotnet build GestionTime.Desktop.csproj -c Release
```

### Error: "MSI se instala pero app no arranca"
**Causas comunes:**
1. Falta GestionTime.Desktop.pri → App no carga recursos XAML
2. Falta carpeta Assets → Assets no encuentran imágenes
3. Falta window-config.ini → App usa configuración default
4. Backend no accesible → Verificar appsettings.json

**Diagnóstico:**
```powershell
# Ver logs de aplicación
Get-Content "$env:LOCALAPPDATA\GestionTime\logs\app.log" -Tail 50
```

---

## 📞 SOPORTE

**Documentación Completa:**
- `CHANGELOG.md` - En carpeta de instalación
- `RELEASE_NOTES_v1.9.0.md` - En carpeta de instalación
- `Docs\BUILD_MSI_LOCAL.md` - En repositorio

**Scripts Útiles:**
- `Scripts\Build-MSI-v1.9.0-Beta.ps1` - Build completo automatizado
- `Scripts\Verify-MSI-Prerequisites.ps1` - Verificar requisitos
- `Scripts\Build-MSI-Local.ps1` - Build alternativo

**GitHub:**
- Repository: https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop
- Issues: https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/issues

---

## ✅ CHECKLIST FINAL

Antes de distribuir MSI v1.9.0 Beta:

- [ ] Build exitoso sin errores
- [ ] MSI generado en `WiX-v3-MSI\GestionTime-v1.9.0-Setup.msi`
- [ ] Tamaño ~110-120 MB (verificado)
- [ ] Instalación en entorno limpio exitosa
- [ ] Carpeta `C:\App\GestionTime-Desktop\` creada correctamente
- [ ] CHANGELOG.md presente en instalación
- [ ] RELEASE_NOTES_v1.9.0.md presente en instalación
- [ ] Shortcuts creados (Start Menu + Desktop)
- [ ] Aplicación ejecuta correctamente
- [ ] Funcionalidades principales testeadas
- [ ] Desinstalación limpia verificada
- [ ] Tag de Git creado: `v1.9.0-beta`
- [ ] Push a GitHub completado
- [ ] Release en GitHub creado con MSI adjunto
- [ ] Documentación actualizada en repositorio

---

**¡GestionTime Desktop v1.9.0 Beta listo para distribución!** 🎉

**GestionTime Team**  
30 de Enero, 2025
