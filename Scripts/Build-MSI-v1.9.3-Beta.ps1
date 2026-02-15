# ═══════════════════════════════════════════════════════════════
# BUILD MSI v1.9.3-beta - SCRIPT RÁPIDO
# ═══════════════════════════════════════════════════════════════

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  🚀 BUILD MSI v1.9.3-beta - GestionTime Desktop" -ForegroundColor Cyan
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
$msiPath = Join-Path $installerDir "GestionTime-v1.9.3-beta.msi"

if (-not (Test-Path $msiPath)) {
    Write-Host "  ❌ ERROR: MSI no encontrado en: $msiPath" -ForegroundColor Red
    exit 1
}

$msiSize = (Get-Item $msiPath).Length / 1MB
Write-Host "  ✅ MSI generado: GestionTime-v1.9.3-beta.msi" -ForegroundColor Green
Write-Host "     Tamaño: $([math]::Round($msiSize, 1)) MB" -ForegroundColor Gray
Write-Host "     Ruta: $msiPath" -ForegroundColor Gray

# ════════════════════════════════════════════════════════════════
# PASO 4: Mostrar resumen
# ════════════════════════════════════════════════════════════════

Write-Host ""
Write-Host "[4/4] 📊 Resumen del Build" -ForegroundColor Yellow
Write-Host ""
Write-Host "  🎯 Versión: " -NoNewline; Write-Host "1.9.3-beta" -ForegroundColor Green
Write-Host "  📦 MSI: " -NoNewline; Write-Host "GestionTime-v1.9.3-beta.msi" -ForegroundColor Green
Write-Host "  📏 Tamaño: " -NoNewline; Write-Host "$([math]::Round($msiSize, 1)) MB" -ForegroundColor Green
Write-Host "  📂 Carpeta: " -NoNewline; Write-Host "installers\" -ForegroundColor Green
Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  ✅ BUILD COMPLETADO EXITOSAMENTE" -ForegroundColor Green
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "📝 PRÓXIMOS PASOS:" -ForegroundColor Yellow
Write-Host "  1. Instalar: Doble clic en GestionTime-v1.9.3-beta.msi" -ForegroundColor Gray
Write-Host "  2. Verificar: Menú → Ayuda → Notas de Versión (debe decir v1.9.3-beta)" -ForegroundColor Gray
Write-Host "  3. Probar: Configuración → Perfil → Guardar → Reabrir Settings (debe mostrar datos actualizados)" -ForegroundColor Gray
Write-Host ""

# Abrir carpeta automáticamente (ya lo hace Build-MSI-Local.ps1 pero por si acaso)
if (Test-Path $installerDir) {
    explorer.exe $installerDir
}

Write-Host "Presiona cualquier tecla para salir..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
