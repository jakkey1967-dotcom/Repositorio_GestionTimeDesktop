# 🔧 Solución Rápida: Error al Presionar F5

## ❌ Error
```
El proyecto no sabe cómo ejecutar el perfil con el nombre 
"GestionTime.Desktop (Package)" y el comando "MsixPackage".
```

## ✅ Solución Inmediata

### Opción 1: Ejecutar el Script (Automático)
```powershell
.\rebuild-clean.ps1
```
Este script configura automáticamente el perfil correcto.

### Opción 2: Cambiar Perfil Manualmente en Visual Studio

1. **En Visual Studio**, cerca del botón de Play (▶️), verás un dropdown
2. **Cambiar de:**
   ```
   GestionTime.Desktop (Package)  ← INCORRECTO
   ```
3. **A:**
   ```
   GestionTime.Desktop (Unpackaged)  ← CORRECTO
   ```

### Opción 3: Editar Manualmente los Archivos

#### Paso 1: Editar `Properties/launchSettings.json`
```json
{
  "profiles": {
    "GestionTime.Desktop (Unpackaged)": {
      "commandName": "Project",
      "nativeDebugging": false
    },
    "GestionTime.Desktop (Package)": {
      "commandName": "MsixPackage"
    }
  }
}
```
**Nota:** El orden importa. El primer perfil es el predeterminado.

#### Paso 2: Editar `GestionTime.Desktop.csproj.user`
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <ActiveDebugProfile>GestionTime.Desktop (Unpackaged)</ActiveDebugProfile>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Debug|x64'">
    <DebuggerFlavor>ProjectDebugger</DebuggerFlavor>
  </PropertyGroup>
</Project>
```

#### Paso 3: Reiniciar Visual Studio
```powershell
# Cerrar Visual Studio completamente
# Eliminar carpeta .vs
Remove-Item -Recurse -Force .vs

# Reabrir
.\GestionTime.sln
```

---

## 🎯 ¿Por Qué Pasa Esto?

| Perfil | Comando | Descripción | Estado |
|--------|---------|-------------|--------|
| **Package** | `MsixPackage` | Requiere AppxManifest.xml | ❌ No funciona (MSIX deshabilitado) |
| **Unpackaged** | `Project` | Ejecución directa del .exe | ✅ Funciona correctamente |

Como deshabilitamos `EnableMsixTooling` en el `.csproj`, el perfil **Package** ya no funciona.

---

## 📋 Verificación Rápida

### ¿El perfil está configurado correctamente?

```powershell
# Ver contenido del archivo de configuración
Get-Content Properties\launchSettings.json
```

**Debe mostrar `Unpackaged` primero:**
```json
{
  "profiles": {
    "GestionTime.Desktop (Unpackaged)": {  ← PRIMERO
      "commandName": "Project"
    },
    "GestionTime.Desktop (Package)": {
      "commandName": "MsixPackage"
    }
  }
}
```

### ¿Visual Studio usa el perfil correcto?

```powershell
# Ver archivo .user
Get-Content GestionTime.Desktop.csproj.user
```

**Debe contener:**
```xml
<ActiveDebugProfile>GestionTime.Desktop (Unpackaged)</ActiveDebugProfile>
```

---

## 🚀 Después de la Solución

### Presionar F5
✅ **Debería funcionar sin errores**

### Si Aún No Funciona

1. **Cerrar Visual Studio completamente**
2. **Ejecutar:**
   ```powershell
   .\rebuild-clean.ps1
   ```
3. **Abrir Visual Studio de nuevo**
4. **Presionar F5**

---

## 💡 Consejo Pro

### Para Siempre Usar Unpackaged

En Visual Studio:
1. Ir a `Debug` → `Options`
2. Buscar `Startup Project`
3. Marcar: "Use last selected profile"

Esto recordará tu elección de perfil.

---

## 🎨 Visual Studio - Selector de Perfil

```
┌─────────────────────────────────────────────┐
│  [▶️ GestionTime.Desktop (Unpackaged)]  ▼  │  ← Selector
└─────────────────────────────────────────────┘
         │
         └─── Opciones:
              • GestionTime.Desktop (Unpackaged)  ✅ Usar este
              • GestionTime.Desktop (Package)     ❌ No usar
```

---

## 📝 Resumen

| Acción | Comando |
|--------|---------|
| **Solución Rápida** | `.\rebuild-clean.ps1` |
| **Cambio Manual** | Seleccionar perfil "Unpackaged" en VS |
| **Verificar** | Ver dropdown cerca del botón Play |
| **Ejecutar** | Presionar F5 |

---

## ✅ Checklist Final

- [ ] Ejecutar `.\rebuild-clean.ps1`
- [ ] Abrir Visual Studio
- [ ] Verificar que el perfil sea "Unpackaged"
- [ ] Presionar F5
- [ ] ✅ La aplicación debe ejecutarse sin errores

---

**Última actualización:** 2025-01-27  
**Estado:** ✅ Solución verificada
