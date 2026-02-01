# ============================================
# SCRIPT ALTERNATIVO - CREAR MSI SIMPLE
# ============================================

Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "   CREANDO MSI - GESTIONTIME DESKTOP v1.4.1-beta" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Verificar que los archivos publicados existen
if (-not (Test-Path "publish\portable\GestionTime.Desktop.exe")) {
    Write-Host "❌ ERROR: No se encontraron los archivos publicados" -ForegroundColor Red
    Write-Host "   Ejecuta primero: dotnet publish ..." -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ Archivos publicados encontrados" -ForegroundColor Green
Write-Host ""

# Opción 1: Usar WiX 3 si está disponible
Write-Host "🔍 Buscando WiX Toolset..." -ForegroundColor Yellow

$wixPath = "C:\Program Files (x86)\WiX Toolset v3.14\bin\candle.exe"
if (Test-Path $wixPath) {
    Write-Host "✅ WiX 3 encontrado" -ForegroundColor Green
    Write-Host ""
    Write-Host "📝 Usa el archivo Product.wxs en WiX-v3-MSI\" -ForegroundColor Cyan
    Write-Host "   Comando:" -ForegroundColor Yellow
    Write-Host '   cd WiX-v3-MSI' -ForegroundColor Gray
    Write-Host '   & "C:\Program Files (x86)\WiX Toolset v3.14\bin\candle.exe" Product.wxs' -ForegroundColor Gray
    Write-Host '   & "C:\Program Files (x86)\WiX Toolset v3.14\bin\light.exe" Product.wixobj' -ForegroundColor Gray
} else {
    Write-Host "⚠️ WiX 3 no encontrado" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "   ALTERNATIVA: Crear MSI manualmente" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "1️⃣ Usar Advanced Installer (Recomendado)" -ForegroundColor Green
Write-Host "   - Descargar: https://www.advancedinstaller.com/" -ForegroundColor Gray
Write-Host "   - Crear proyecto nuevo → Simple" -ForegroundColor Gray
Write-Host "   - Agregar archivos desde publish\portable\" -ForegroundColor Gray
Write-Host ""

Write-Host "2️⃣ Usar Inno Setup (Alternativa)" -ForegroundColor Green
Write-Host "   - Descargar: https://jrsoftware.org/isdl.php" -ForegroundColor Gray
Write-Host "   - Crear script .iss" -ForegroundColor Gray
Write-Host ""

Write-Host "3️⃣ Crear ZIP portable (Más rápido)" -ForegroundColor Yellow
Write-Host "   Ejecutando..." -ForegroundColor Gray

# Crear ZIP portable
$zipName = "GestionTime-Desktop-v1.4.1-beta-Portable.zip"
if (Test-Path $zipName) {
    Remove-Item $zipName -Force
}

Compress-Archive -Path "publish\portable\*" -DestinationPath $zipName -CompressionLevel Optimal

if (Test-Path $zipName) {
    Write-Host "✅ ZIP portable creado: $zipName" -ForegroundColor Green
    $zipSize = (Get-Item $zipName).Length / 1MB
    Write-Host "   Tamaño: $([math]::Round($zipSize, 2)) MB" -ForegroundColor Cyan
} else {
    Write-Host "❌ Error creando ZIP" -ForegroundColor Red
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "   ✅ PROCESO COMPLETADO" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Host "📦 Archivos disponibles para distribuir:" -ForegroundColor Cyan
Write-Host "   - $zipName (portable)" -ForegroundColor Yellow
Write-Host ""
Write-Host "📝 Para crear MSI, sigue las instrucciones en:" -ForegroundColor Cyan
Write-Host "   INSTRUCCIONES_RELEASE_v1.4.1-beta.md" -ForegroundColor Yellow
