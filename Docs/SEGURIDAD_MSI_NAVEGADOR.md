# 🔒 SEGURIDAD MSI - Advertencias del Navegador y SmartScreen

## ❓ Problema

Al descargar el MSI desde GitHub Releases (o cualquier URL), los navegadores y Windows muestran advertencias:

| Componente | Mensaje |
|---|---|
| **Chrome** | "GestionTime-v1.9.5-win-x64.msi puede ser peligroso" → "Conservar" |
| **Edge** | "GestionTime-v1.9.5-win-x64.msi no se descarga de forma habitual" |
| **Windows SmartScreen** | "Windows protegió su equipo" → "Más información" → "Ejecutar de todas formas" |
| **Windows Defender** | Puede marcar como sospechoso temporalmente |

### ¿Por qué ocurre?

1. **El MSI no está firmado digitalmente** (no tiene certificado de code signing)
2. **Mark of the Web (MOTW)**: Windows marca archivos descargados de Internet con un flag de zona
3. **SmartScreen Reputation**: Archivos nuevos sin historial de descargas son marcados como sospechosos
4. **Tamaño del archivo** (~125 MB): Los archivos grandes .msi descargados son más sospechosos para los filtros

---

## ✅ Soluciones (de menor a mayor efectividad)

### 🟡 Nivel 1: Instrucciones para el usuario (inmediato, sin coste)

**Al descargar en Chrome:**
1. Clic en `^` junto a la descarga → "Conservar"
2. O ir a `chrome://downloads` → "Conservar archivo peligroso"

**Al descargar en Edge:**
1. Clic en `...` → "Conservar"
2. Si pide confirmación → "Mostrar más" → "Conservar de todos modos"

**Windows SmartScreen al ejecutar:**
1. Clic en **"Más información"**
2. Clic en **"Ejecutar de todas formas"**

**Eliminar Mark of the Web (MOTW) después de descargar:**
```powershell
# Opción 1: PowerShell (como administrador)
Unblock-File -Path "C:\Users\$env:USERNAME\Downloads\GestionTime-v1.9.5-win-x64.msi"

# Opción 2: Clic derecho en el MSI → Propiedades → Marcar "Desbloquear" → Aceptar
```

---

### 🟢 Nivel 2: Certificado autofirmado (reduce advertencias)

Un certificado autofirmado **reduce** las advertencias pero no las elimina completamente (el certificado no es de una CA reconocida).

**Generar y firmar:**
```powershell
cd C:\GestionTime\GestionTimeDesktop
.\Scripts\Sign-MSI.ps1
```

**El script automáticamente:**
1. Crea un certificado autofirmado de code signing (válido 3 años)
2. Lo almacena en `Cert:\CurrentUser\My`
3. Exporta el `.pfx` a `certs\GestionTime-CodeSigning.pfx`
4. Firma el MSI más reciente en `installers\`

**Instalar certificado en máquinas cliente (para evitar SmartScreen):**
```powershell
# En la máquina del usuario (como administrador):
Import-PfxCertificate -FilePath "GestionTime-CodeSigning.pfx" `
    -CertStoreLocation "Cert:\LocalMachine\TrustedPublisher" `
    -Password (ConvertTo-SecureString "GT2025!" -AsPlainText -Force)
```

> ⚠️ **Cambia la password** `GT2025!` por una segura antes de distribuir.

---

### 🔵 Nivel 3: Certificado de CA reconocida (elimina advertencias — recomendado para producción)

Un certificado de code signing emitido por una CA reconocida **elimina** las advertencias de SmartScreen y navegadores.

**Proveedores recomendados:**

| Proveedor | Precio/año | Tipo |
|---|---|---|
| [Sectigo/Comodo](https://sectigo.com/ssl-certificates-tls/code-signing) | ~70-200€ | OV Code Signing |
| [DigiCert](https://www.digicert.com/signing/code-signing-certificates) | ~400€ | OV Code Signing |
| [GlobalSign](https://www.globalsign.com/en/code-signing-certificate) | ~250€ | OV Code Signing |
| [SSL.com](https://www.ssl.com/certificates/ev-code-signing/) | ~240€ | EV Code Signing |

**Tipos de certificado:**
- **OV (Organization Validation)**: Valida la organización. Elimina la mayoría de advertencias.
- **EV (Extended Validation)**: Validación exhaustiva + hardware token (USB). Elimina **todas** las advertencias desde la primera descarga (reputación instantánea en SmartScreen).

**Firmar con certificado de CA:**
```powershell
# Si tienes el .pfx del proveedor:
.\Scripts\Sign-MSI.ps1 -PfxPath "C:\certs\mi-certificado.pfx" -PfxPassword "mi-password"
```

---

### 🟣 Nivel 4: GitHub Releases con checksums (verificación de integridad)

Añadir checksums SHA256 al release para que los usuarios puedan verificar la integridad:

```powershell
# Generar checksum:
$hash = (Get-FileHash "installers\GestionTime-v1.9.5-win-x64.msi" -Algorithm SHA256).Hash
Write-Host "SHA256: $hash"

# El usuario verifica después de descargar:
# (Get-FileHash "GestionTime-v1.9.5-win-x64.msi" -Algorithm SHA256).Hash
# Debe coincidir con el publicado en GitHub Releases
```

---

## 📋 Resumen de efectividad

| Solución | Coste | SmartScreen | Chrome/Edge | Esfuerzo |
|---|---|---|---|---|
| Instrucciones al usuario | 0€ | ⚠️ Manual | ⚠️ Manual | Bajo |
| Certificado autofirmado | 0€ | 🟡 Reduce | 🟡 Reduce | Medio |
| Certificado OV de CA | ~70-200€/año | ✅ Elimina* | ✅ Elimina* | Medio |
| Certificado EV de CA | ~240-400€/año | ✅ Elimina | ✅ Elimina | Alto |

\* Los certificados OV nuevos pueden necesitar acumular reputación en SmartScreen (unas semanas de descargas).

---

## 🚀 Recomendación para GestionTime

### Corto plazo (ahora):
1. ✅ Usar certificado autofirmado (`.\Scripts\Sign-MSI.ps1`)
2. ✅ Incluir instrucciones de "Desbloquear" en el README del Release
3. ✅ Publicar checksum SHA256 en GitHub Releases

### Medio plazo (cuando haya presupuesto):
1. 🔵 Comprar certificado OV de Sectigo (~70€/año)
2. 🔵 Integrar firma en el pipeline de build

---

## 🔧 Integración en Build Pipeline

Para firmar automáticamente en cada build, añadir al final de `Build-MSI-Local.ps1`:

```powershell
# Después de generar el MSI:
$signScript = Join-Path $ProjectRoot "Scripts\Sign-MSI.ps1"
if (Test-Path $signScript) {
    Write-Host "[7/7] Firmando MSI..." -ForegroundColor Yellow
    & $signScript -MsiPath $msiPath
}
```

---

## 📂 Archivos relacionados

- `Scripts/Sign-MSI.ps1` — Script de firma automática
- `Scripts/Build-MSI-Local.ps1` — Script de build principal
- `Scripts/Build-MSI-v1.9.5-Beta.ps1` — Script de build versionado
- `certs/` — Carpeta para certificados (añadir a .gitignore)
- `WiX-v3-MSI/Product.wxs` — Definición del instalador
