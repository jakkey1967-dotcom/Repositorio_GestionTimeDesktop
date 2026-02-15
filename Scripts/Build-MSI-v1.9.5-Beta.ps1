# ═══════════════════════════════════════════════════════════════
# BUILD MSI v1.9.5-beta - SCRIPT RÁPIDO
# ═══════════════════════════════════════════════════════════════

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  🚀 BUILD MSI v1.9.5-beta - GestionTime Desktop" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Verificar que estamos en la raíz del proyecto
$ProjectRoot = $PSScriptRoot | Split-Path -Parent

if (-not (Test-Path "$ProjectRoot\GestionTime.Desktop.csproj")) {
    Write-Host "❌ ERROR: No se encontró GestionTime.Desktop.csproj" -ForegroundColor Red
    Write-Host "   Ejecuta este script desde la carpeta Scripts\" -ForegroundColor Yellow
    exit 1
}

Write-Host "📂 Proyecto: $ProjectRoot" -ForegroundColor Gray
Write-Host ""

# ════════════════════════════════════════════════════════════════
# PASO 1: Verificar herramientas
# ════════════════════════════════════════════════════════════════

Write-Host "[1/4] 🔍 Verificando herramientas..." -ForegroundColor Yellow

# Verificar .NET 8
try {
    $dotnetVersion = & dotnet --version 2>&1
    Write-Host "  ✅ .NET SDK $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "  ❌ ERROR: .NET 8 SDK no instalado" -ForegroundColor Red
    exit 1
}

# Verificar WiX v3.14
$wixPath = "C:\Program Files (x86)\WiX Toolset v3.14\bin"
if (-not (Test-Path "$wixPath\candle.exe")) {
    Write-Host "  ❌ ERROR: WiX Toolset v3.14 no encontrado" -ForegroundColor Red
    Write-Host ""
    Write-Host "Descarga: https://github.com/wixtoolset/wix3/releases/download/wix3141rtm/wix314.exe" -ForegroundColor Yellow
    exit 1
}

$env:Path += ";$wixPath"
Write-Host "  ✅ WiX Toolset v3.14" -ForegroundColor Green

Write-Host ""

# ════════════════════════════════════════════════════════════════
# PASO 2: Ejecutar build completo
# ════════════════════════════════════════════════════════════════

Write-Host "[2/4] 🔨 Ejecutando Build-MSI-Local.ps1..." -ForegroundColor Yellow
Write-Host ""

$buildScript = Join-Path $ProjectRoot "Scripts\Build-MSI-Local.ps1"

if (-not (Test-Path $buildScript)) {
    Write-Host "  ❌ ERROR: Build-MSI-Local.ps1 no encontrado" -ForegroundColor Red
    exit 1
}

# Ejecutar script principal
try {
    & $buildScript -OpenFolder:$true
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "❌ Build falló con código: $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }
} catch {
    Write-Host ""
    Write-Host "❌ ERROR durante el build:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Yellow
    exit 1
}

# ════════════════════════════════════════════════════════════════
# PASO 3: Verificar output
# ════════════════════════════════════════════════════════════════

Write-Host ""
Write-Host "[3/4] ✅ Verificando MSI generado..." -ForegroundColor Yellow

$installerDir = Join-Path $ProjectRoot "installers"
$msiPath = Join-Path $installerDir "GestionTime-v1.9.5-win-x64.msi"

if (-not (Test-Path $msiPath)) {
    Write-Host "  ❌ ERROR: MSI no encontrado en: $msiPath" -ForegroundColor Red
    exit 1
}

$msiSize = (Get-Item $msiPath).Length / 1MB
Write-Host "  ✅ MSI generado: GestionTime-v1.9.5-win-x64.msi" -ForegroundColor Green
Write-Host "  📦 Tamaño: $([math]::Round($msiSize, 2)) MB" -ForegroundColor Gray

# ════════════════════════════════════════════════════════════════
# PASO 4: Resumen
# ════════════════════════════════════════════════════════════════

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "  ✅ BUILD v1.9.5-beta COMPLETADO" -ForegroundColor Green
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Host "📦 Archivo:  GestionTime-v1.9.5-win-x64.msi" -ForegroundColor Cyan
Write-Host "📂 Carpeta:  $installerDir" -ForegroundColor White
Write-Host "💾 Tamaño:   $([math]::Round($msiSize, 2)) MB" -ForegroundColor Gray
Write-Host ""
Write-Host "📋 PRÓXIMOS PASOS:" -ForegroundColor Yellow
Write-Host "  1. Probar instalación en máquina limpia" -ForegroundColor White
Write-Host "  2. Verificar versión en Ayuda > Notas de Versión" -ForegroundColor White
Write-Host "  3. Subir MSI a GitHub Releases" -ForegroundColor White
Write-Host ""
