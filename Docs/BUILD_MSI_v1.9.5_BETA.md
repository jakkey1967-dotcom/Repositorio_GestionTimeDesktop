# 🔨 BUILD MSI v1.9.5-beta - GestionTime Desktop

## ✅ Build Completado

### Datos del Instalador
- **Archivo**: `GestionTime-v1.9.5-win-x64.msi`
- **Tamaño**: ~125 MB
- **Versión**: 1.9.5.0
- **Firma digital**: GestionTime Solutions (certificado autofirmado, válido hasta 2029)
- **SHA256**: `082DEAA41237542AEA89BB963B4F36424000C2967169D173737E574403DAD3BD`
- **Ruta**: `installers\GestionTime-v1.9.5-win-x64.msi`

### Cambios Incluidos (vs v1.9.3)
- ✅ **Sistema de Informes**: Ventana dedicada con gráficas semanales
- ✅ **Exportación PDF/Excel/Email**: Con barras de colores y logo
- ✅ **Detección de solapamientos**: Tabla con edición inline + solución automática
- ✅ **Notas de cliente**: Global (EDITOR/ADMIN) + Personal (todos)
- ✅ **Perfil de usuario**: Edición inline en Settings
- ✅ **Seguridad por roles**: USER/EDITOR/ADMIN en Informes

---

## 📋 PASOS PARA REGENERAR EL MSI

### Prerrequisitos
- ✅ .NET 8 SDK
- ✅ WiX Toolset v3.14 en `C:\Program Files (x86)\WiX Toolset v3.14\bin`

### Build completo (desde cero)
```powershell
cd C:\GestionTime\GestionTimeDesktop
.\Scripts\Build-MSI-Local.ps1
```

### Build rápido (binarios existentes)
```powershell
.\Scripts\Build-MSI-Local.ps1 -SkipPublish
```

### Firmar el MSI
```powershell
.\Scripts\Sign-MSI.ps1
```

---

## 🔒 Seguridad del Navegador

### Problema
Los navegadores y SmartScreen muestran advertencias al descargar el MSI porque usa un certificado autofirmado.

### Solución rápida para usuarios
1. **Chrome**: Clic en `^` → "Conservar"
2. **Edge**: Clic en `...` → "Conservar"
3. **SmartScreen**: "Más información" → "Ejecutar de todas formas"
4. **Desbloquear**: Clic derecho en MSI → Propiedades → Marcar "Desbloquear"

### Solución definitiva
Comprar un certificado OV de Sectigo (~70€/año). Ver `Docs\SEGURIDAD_MSI_NAVEGADOR.md`.

---

## 📦 Instalación

1. Ejecutar `GestionTime-v1.9.5-win-x64.msi`
2. Si SmartScreen aparece → "Más información" → "Ejecutar de todas formas"
3. Se instala en `C:\App\GestionTime-Desktop`
4. Crea accesos directos en Menú Inicio y Escritorio
5. Backend: `https://gestiontimeapi.onrender.com`

### Verificar instalación
1. Abrir GestionTime Desktop
2. Menú Ayuda → Notas de Versión → debe mostrar **v1.9.5-beta**

---

## 🔐 Certificado de Firma

- **Subject**: CN=GestionTime Solutions, O=GestionTime, L=Spain
- **Tipo**: Code Signing (autofirmado)
- **Thumbprint**: `5E0F0E88AA65F9310499F258D51232E3DDD90463`
- **Válido hasta**: 2029-02-15
- **PFX backup**: `certs\GestionTime-CodeSigning.pfx` (NO subir a Git)

---

## 🧪 Verificación Post-Build

```powershell
# Verificar firma
Get-AuthenticodeSignature "installers\GestionTime-v1.9.5-win-x64.msi"

# Verificar hash
(Get-FileHash "installers\GestionTime-v1.9.5-win-x64.msi" -Algorithm SHA256).Hash
# Esperado: 082DEAA41237542AEA89BB963B4F36424000C2967169D173737E574403DAD3BD
```
