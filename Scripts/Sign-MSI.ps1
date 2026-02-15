# ═══════════════════════════════════════════════════════════════
# FIRMAR MSI CON CERTIFICADO - GestionTime Desktop
# Reduce advertencias de SmartScreen y navegadores al descargar
# ═══════════════════════════════════════════════════════════════
#
# USO:
#   .\Scripts\Sign-MSI.ps1                          → Firma con certificado autofirmado (crea si no existe)
#   .\Scripts\Sign-MSI.ps1 -PfxPath "cert.pfx"      → Firma con certificado externo (.pfx)
#   .\Scripts\Sign-MSI.ps1 -PfxPath "cert.pfx" -PfxPassword "pass"
#
# NOTA: Un certificado autofirmado REDUCE las advertencias pero NO las elimina
#       completamente. Para eliminarlas se necesita un certificado de una CA
#       reconocida (Sectigo, DigiCert, etc.) — ver Docs\SEGURIDAD_MSI_NAVEGADOR.md
# ═══════════════════════════════════════════════════════════════

param(
    [string]$PfxPath = "",
    [string]$PfxPassword = "",
    [string]$MsiPath = ""
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "  FIRMAR MSI - GestionTime Desktop" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

$ProjectRoot = $PSScriptRoot | Split-Path -Parent

# ================================================================
# PASO 1: Localizar el MSI
# ================================================================

if ([string]::IsNullOrEmpty($MsiPath)) {
    $installerDir = Join-Path $ProjectRoot "installers"
    $latestMsi = Get-ChildItem $installerDir -Filter "*.msi" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($null -eq $latestMsi) {
        Write-Host "[ERROR] No se encontro ningun MSI en: $installerDir" -ForegroundColor Red
        Write-Host "  Ejecuta primero: .\Scripts\Build-MSI-Local.ps1" -ForegroundColor Yellow
        exit 1
    }

    $MsiPath = $latestMsi.FullName
}

if (-not (Test-Path $MsiPath)) {
    Write-Host "[ERROR] MSI no encontrado: $MsiPath" -ForegroundColor Red
    exit 1
}

$msiName = [System.IO.Path]::GetFileName($MsiPath)
Write-Host "[1/3] MSI encontrado: $msiName" -ForegroundColor Green
Write-Host "  Ruta: $MsiPath" -ForegroundColor Gray
Write-Host ""

# ================================================================
# PASO 2: Obtener o crear certificado
# ================================================================

Write-Host "[2/3] Configurando certificado..." -ForegroundColor Yellow

$certSubject = "CN=GestionTime Solutions, O=GestionTime, L=Spain"
$certStorePath = "Cert:\CurrentUser\My"

if ([string]::IsNullOrEmpty($PfxPath)) {
    # Buscar certificado autofirmado existente
    $existingCert = Get-ChildItem $certStorePath -ErrorAction SilentlyContinue |
        Where-Object { $_.Subject -like "*GestionTime Solutions*" -and $_.NotAfter -gt (Get-Date) } |
        Select-Object -First 1

    if ($null -ne $existingCert) {
        Write-Host "  Certificado existente encontrado:" -ForegroundColor Green
        Write-Host "    Subject: $($existingCert.Subject)" -ForegroundColor Gray
        Write-Host "    Thumbprint: $($existingCert.Thumbprint)" -ForegroundColor Gray
        Write-Host "    Expira: $($existingCert.NotAfter.ToString('yyyy-MM-dd'))" -ForegroundColor Gray
        $signingCert = $existingCert
    }
    else {
        Write-Host "  Creando certificado autofirmado (code signing)..." -ForegroundColor Yellow

        $signingCert = New-SelfSignedCertificate `
            -Type CodeSigningCert `
            -Subject $certSubject `
            -CertStoreLocation $certStorePath `
            -NotAfter (Get-Date).AddYears(3) `
            -KeyAlgorithm RSA `
            -KeyLength 2048 `
            -HashAlgorithm SHA256

        Write-Host "  Certificado creado:" -ForegroundColor Green
        Write-Host "    Subject: $($signingCert.Subject)" -ForegroundColor Gray
        Write-Host "    Thumbprint: $($signingCert.Thumbprint)" -ForegroundColor Gray
        Write-Host "    Expira: $($signingCert.NotAfter.ToString('yyyy-MM-dd'))" -ForegroundColor Gray

        # Instalar en Trusted Root para que la firma sea valida
        try {
            $rootStore = New-Object System.Security.Cryptography.X509Certificates.X509Store("Root", "CurrentUser")
            $rootStore.Open("ReadWrite")
            $rootStore.Add($signingCert)
            $rootStore.Close()
            Write-Host "  Instalado en Trusted Root (CurrentUser)" -ForegroundColor Green
        }
        catch {
            Write-Host "  [AVISO] No se pudo instalar en Trusted Root: $($_.Exception.Message)" -ForegroundColor Yellow
        }

        # Exportar PFX para backup
        $pfxBackupDir = Join-Path $ProjectRoot "certs"
        if (-not (Test-Path $pfxBackupDir)) {
            New-Item -ItemType Directory -Path $pfxBackupDir | Out-Null
        }

        $pfxBackupPath = Join-Path $pfxBackupDir "GestionTime-CodeSigning.pfx"
        $securePwd = ConvertTo-SecureString -String "GT2025!" -Force -AsPlainText
        Export-PfxCertificate -Cert $signingCert -FilePath $pfxBackupPath -Password $securePwd | Out-Null

        Write-Host ""
        Write-Host "  PFX exportado a: $pfxBackupPath" -ForegroundColor Cyan
        Write-Host "  Password PFX: GT2025!" -ForegroundColor Cyan
        Write-Host "  [IMPORTANTE] Cambia la password y guarda el .pfx en lugar seguro" -ForegroundColor Yellow
    }
}
else {
    # Usar certificado PFX externo
    Write-Host "  Importando certificado desde: $PfxPath" -ForegroundColor Yellow

    if (-not (Test-Path $PfxPath)) {
        Write-Host "  [ERROR] Archivo PFX no encontrado: $PfxPath" -ForegroundColor Red
        exit 1
    }

    if ([string]::IsNullOrEmpty($PfxPassword)) {
        $securePwd = Read-Host "  Introduce la password del PFX" -AsSecureString
    }
    else {
        $securePwd = ConvertTo-SecureString -String $PfxPassword -Force -AsPlainText
    }

    $signingCert = Import-PfxCertificate -FilePath $PfxPath -CertStoreLocation $certStorePath -Password $securePwd
    Write-Host "  Certificado importado: $($signingCert.Subject)" -ForegroundColor Green
}

Write-Host ""

# ================================================================
# PASO 3: Firmar el MSI
# ================================================================

Write-Host "[3/3] Firmando MSI..." -ForegroundColor Yellow

try {
    # Verificar que signtool existe (Windows SDK)
    $signtoolPaths = @(
        "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe",
        "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.22000.0\x64\signtool.exe",
        "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.19041.0\x64\signtool.exe",
        "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe"
    )

    $signtool = $null
    # Also search dynamically
    $sdkBinDir = "${env:ProgramFiles(x86)}\Windows Kits\10\bin"
    if (Test-Path $sdkBinDir) {
        $found = Get-ChildItem $sdkBinDir -Recurse -Filter "signtool.exe" -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -like "*x64*" } |
            Sort-Object FullName -Descending |
            Select-Object -First 1
        if ($null -ne $found) {
            $signtool = $found.FullName
        }
    }

    if ($null -eq $signtool) {
        foreach ($p in $signtoolPaths) {
            if (Test-Path $p) { $signtool = $p; break }
        }
    }

    if ($null -eq $signtool) {
        Write-Host ""
        Write-Host "  [AVISO] signtool.exe no encontrado (Windows SDK no instalado)" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "  Alternativa: usando Set-AuthenticodeSignature de PowerShell..." -ForegroundColor Yellow

        $result = Set-AuthenticodeSignature -FilePath $MsiPath -Certificate $signingCert -TimestampServer "http://timestamp.digicert.com" -HashAlgorithm SHA256

        if ($result.Status -eq "Valid") {
            Write-Host "  MSI firmado correctamente (PowerShell)" -ForegroundColor Green
        }
        else {
            Write-Host "  Estado de firma: $($result.Status)" -ForegroundColor Yellow
            Write-Host "  Mensaje: $($result.StatusMessage)" -ForegroundColor Gray
        }
    }
    else {
        Write-Host "  signtool encontrado: $signtool" -ForegroundColor Gray

        $thumbprint = $signingCert.Thumbprint

        $signArgs = @(
            "sign",
            "/sha1", $thumbprint,
            "/fd", "SHA256",
            "/tr", "http://timestamp.digicert.com",
            "/td", "SHA256",
            "/d", "GestionTime Desktop",
            "/du", "https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop",
            "`"$MsiPath`""
        )

        $signOutput = & $signtool @signArgs 2>&1
        $signExitCode = $LASTEXITCODE

        if ($signExitCode -eq 0) {
            Write-Host "  MSI firmado correctamente (signtool)" -ForegroundColor Green
        }
        else {
            Write-Host "  [ERROR] signtool fallo (codigo: $signExitCode)" -ForegroundColor Red
            Write-Host $signOutput -ForegroundColor Gray

            Write-Host ""
            Write-Host "  Intentando con Set-AuthenticodeSignature..." -ForegroundColor Yellow
            $result = Set-AuthenticodeSignature -FilePath $MsiPath -Certificate $signingCert -TimestampServer "http://timestamp.digicert.com" -HashAlgorithm SHA256

            if ($result.Status -eq "Valid") {
                Write-Host "  MSI firmado correctamente (PowerShell fallback)" -ForegroundColor Green
            }
            else {
                Write-Host "  Estado de firma: $($result.Status)" -ForegroundColor Yellow
            }
        }
    }
}
catch {
    Write-Host "  [ERROR] Fallo al firmar: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "  El MSI se genero sin firma. Consulta:" -ForegroundColor Yellow
    Write-Host "  Docs\SEGURIDAD_MSI_NAVEGADOR.md" -ForegroundColor Cyan
}

# ================================================================
# RESUMEN
# ================================================================

Write-Host ""
Write-Host "================================================================" -ForegroundColor Green
Write-Host "  PROCESO COMPLETADO" -ForegroundColor Green
Write-Host "================================================================" -ForegroundColor Green
Write-Host ""
Write-Host "MSI: $msiName" -ForegroundColor Cyan
Write-Host "Ruta: $MsiPath" -ForegroundColor White
Write-Host ""

# Verificar firma
$sig = Get-AuthenticodeSignature $MsiPath -ErrorAction SilentlyContinue
if ($null -ne $sig -and $sig.Status -ne "NotSigned") {
    Write-Host "Firma: $($sig.Status)" -ForegroundColor Green
    Write-Host "Emisor: $($sig.SignerCertificate.Subject)" -ForegroundColor Gray
}
else {
    Write-Host "Firma: Sin firmar" -ForegroundColor Yellow
}
Write-Host ""
