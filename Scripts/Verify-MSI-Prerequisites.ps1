# ═══════════════════════════════════════════════════════════════
# VERIFICAR CONFIGURACIÓN ANTES DE BUILD MSI
# ═══════════════════════════════════════════════════════════════

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  🔍 VERIFICACIÓN PRE-BUILD MSI" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

$ProjectRoot = Split-Path $PSScriptRoot -Parent
$allOk = $true

# ════════════════════════════════════════════════════════════════
# 1. VERIFICAR APPSETTINGS.JSON
# ════════════════════════════════════════════════════════════════

Write-Host "[1/4] 📋 Verificando appsettings.json..." -ForegroundColor Yellow

$appsettingsPath = Join-Path $ProjectRoot "appsettings.json"
if (Test-Path $appsettingsPath) {
    $appsettings = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
    $baseUrl = $appsettings.Api.BaseUrl
    
    if ($baseUrl -eq "https://gestiontimeapi.onrender.com") {
        Write-Host "  ✅ Backend: Render (correcto para instalador)" -ForegroundColor Green
    } else {
        Write-Host "  ⚠️  Backend: $baseUrl" -ForegroundColor Yellow
        Write-Host "     Nota: El instalador usará esta URL" -ForegroundColor Gray
    }
} else {
    Write-Host "  ❌ appsettings.json NO encontrado" -ForegroundColor Red
    $allOk = $false
}

# ════════════════════════════════════════════════════════════════
# 2. VERIFICAR .NET 8 SDK
# ════════════════════════════════════════════════════════════════

Write-Host ""
Write-Host "[2/4] 🔧 Verificando .NET 8 SDK..." -ForegroundColor Yellow

try {
    $dotnetVersion = & dotnet --version 2>&1
    if ($dotnetVersion -like "8.*") {
        Write-Host "  ✅ .NET SDK: $dotnetVersion" -ForegroundColor Green
    } else {
        Write-Host "  ⚠️  .NET SDK: $dotnetVersion (se recomienda 8.x)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  ❌ .NET SDK NO encontrado" -ForegroundColor Red
    $allOk = $false
}

# ════════════════════════════════════════════════════════════════
# 3. VERIFICAR WIX TOOLSET v3.14
# ════════════════════════════════════════════════════════════════

Write-Host ""
Write-Host "[3/4] 🔥 Verificando WiX Toolset v3.14..." -ForegroundColor Yellow

$wixPath = "C:\Program Files (x86)\WiX Toolset v3.14\bin"
if (Test-Path $wixPath) {
    $candlePath = Join-Path $wixPath "candle.exe"
    if (Test-Path $candlePath) {
        Write-Host "  ✅ WiX v3.14 instalado correctamente" -ForegroundColor Green
    } else {
        Write-Host "  ❌ WiX instalado pero candle.exe no encontrado" -ForegroundColor Red
        $allOk = $false
    }
} else {
    Write-Host "  ❌ WiX Toolset v3.14 NO encontrado" -ForegroundColor Red
    Write-Host ""
    Write-Host "     Descarga desde:" -ForegroundColor Yellow
    Write-Host "     https://github.com/wixtoolset/wix3/releases/download/wix3141rtm/wix314.exe" -ForegroundColor White
    $allOk = $false
}

# ════════════════════════════════════════════════════════════════
# 4. VERIFICAR ARCHIVOS WIX
# ════════════════════════════════════════════════════════════════

Write-Host ""
Write-Host "[4/4] 📄 Verificando archivos WiX..." -ForegroundColor Yellow

$wixDir = Join-Path $ProjectRoot "WiX-v3-MSI"
$productWxs = Join-Path $wixDir "Product.wxs"
$licenseRtf = Join-Path $wixDir "License.rtf"

if (Test-Path $productWxs) {
    Write-Host "  ✅ Product.wxs encontrado" -ForegroundColor Green
} else {
    Write-Host "  ❌ Product.wxs NO encontrado" -ForegroundColor Red
    $allOk = $false
}

if (Test-Path $licenseRtf) {
    Write-Host "  ✅ License.rtf encontrado" -ForegroundColor Green
} else {
    Write-Host "  ❌ License.rtf NO encontrado" -ForegroundColor Red
    $allOk = $false
}

# ════════════════════════════════════════════════════════════════
# RESUMEN
# ════════════════════════════════════════════════════════════════

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan

if ($allOk) {
    Write-Host "  ✅ VERIFICACIÓN EXITOSA - LISTO PARA BUILD" -ForegroundColor Green
    Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "▶️  Para generar el MSI ejecuta:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "   .\Scripts\Build-MSI-Local.ps1" -ForegroundColor White
    Write-Host ""
} else {
    Write-Host "  ❌ VERIFICACIÓN FALLIDA - CORREGIR ERRORES" -ForegroundColor Red
    Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""
    exit 1
}
