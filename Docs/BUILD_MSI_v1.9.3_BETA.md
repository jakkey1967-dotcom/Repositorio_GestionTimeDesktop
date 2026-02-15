# 🔨 BUILD MSI v1.9.3-beta - GestionTime Desktop

## ✅ Cambios Completados

### 1️⃣ Versión Actualizada
- ✅ **Directory.Build.props**: `AppVersionPatch=3` → Genera v1.9.3-beta
- ✅ **Scripts\Build-MSI-Local.ps1**: `$Version = "1.9.3"`
- ✅ **Compilación exitosa** con nueva versión

### 2️⃣ Fixes Incluidos en esta Versión
- ✅ **Cache de perfil en Settings**: Ahora invalida cache HTTP + cache interno
- ✅ **Warnings de compilación**: CS8604 y WMC1506 solucionados
- ✅ **Sistema de versionado centralizado**: AppVersionMajor/Minor/Patch en Directory.Build.props

---

## 📋 PASOS PARA GENERAR EL MSI

### Prerrequisitos Verificados
- ✅ .NET 8 SDK instalado
- ✅ WiX Toolset v3.14 en `C:\Program Files (x86)\WiX Toolset v3.14\bin`

### Ejecución

**Opción 1: Compilar TODO desde cero (recomendado)**
```powershell
cd C:\GestionTime\GestionTimeDesktop
.\Scripts\Build-MSI-Local.ps1
```

**Opción 2: Usar binarios ya compilados (más rápido)**
```powershell
cd C:\GestionTime\GestionTimeDesktop
.\Scripts\Build-MSI-Local.ps1 -SkipPublish
```

---

## 📦 Output Esperado

```
installers/
└── GestionTime-v1.9.3-beta.msi  (~110 MB)
```

**Instalación:**
- Ruta: `C:\App\GestionTime-Desktop`
- Tamaño instalado: ~280 MB
- Backend: `https://gestiontimeapi.onrender.com`

---

## 🧪 Verificación Post-Build

1. **Verificar versión del MSI:**
   - Clic derecho en `GestionTime-v1.9.3-beta.msi`
   - Propiedades → Detalles
   - Version: `1.9.3.0`

2. **Instalar y verificar:**
   - Ejecutar el MSI
   - Abrir GestionTime Desktop
   - Ir a menú **Ayuda → Notas de Versión**
   - Verificar: **v1.9.3-beta**

3. **Probar fix de cache:**
   - Ir a menú **Configuración → Perfil y cuenta**
   - Cambiar nombre/email → Guardar
   - Cerrar Settings
   - Volver a abrir Settings → **DEBE mostrar los nuevos datos** ✅

---

## 📝 Notas Importantes

- **ProductId GUID**: WiX genera nuevo GUID automáticamente en cada build
- **UpgradeCode**: Se mantiene igual para permitir actualizaciones in-place
- **Backend**: Apunta a Render (producción)
- **Logs**: `C:\Users\[usuario]\AppData\Local\GestionTime\Logs\app.log`

---

## 🐛 Troubleshooting

### Error: "WiX Toolset no encontrado"
```powershell
# Descargar e instalar:
https://github.com/wixtoolset/wix3/releases/download/wix3141rtm/wix314.exe
```

### Error: "dotnet publish failed"
```powershell
# Limpiar y rebuild:
dotnet clean
dotnet restore
dotnet build -c Release
```

### Error: "light.exe failed"
- Verificar que `publish\portable` existe
- Verificar que `GestionTime.Desktop.exe` está en publish
- Verificar que `Assets` está completo (14 archivos)

---

## 📊 Changelog v1.9.3-beta

### 🔧 Fixes
- **Cache de perfil**: Invalidación doble (HTTP + interno) soluciona datos vacíos en Settings
- **Nullability warnings**: App.Log! en DiarioPage.xaml.cs
- **Binding warnings**: Mode=OneTime en IsUserRole binding

### 🔄 Mejoras
- Sistema de versionado centralizado en Directory.Build.props
- Mejor logging en ProfileService.InvalidateCache()

---

**Build Date:** 2025-01-XX  
**Target Environment:** Windows 10/11 x64  
**Backend:** Render Production (https://gestiontimeapi.onrender.com)
