# 📦 AGREGAR MSI AL RELEASE v1.4.0-beta

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  📦 AGREGANDO MSI AL RELEASE v1.4.0-beta" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Verificar que el MSI existe
$msiPath = "installers\GestionTime-1.4.0-beta.msi"

if (-not (Test-Path $msiPath)) {
    Write-Host "❌ Error: No se encuentra el archivo MSI" -ForegroundColor Red
    Write-Host "   Ruta esperada: $msiPath" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "💡 Solución: Genera el MSI primero con:" -ForegroundColor Cyan
    Write-Host "   cd WiX-v3-MSI" -ForegroundColor White
    Write-Host "   .\Build-MSI.ps1" -ForegroundColor White
    exit 1
}

$msiSize = [math]::Round((Get-Item $msiPath).Length / 1MB, 2)
Write-Host "✅ MSI encontrado: $msiPath ($msiSize MB)" -ForegroundColor Green
Write-Host ""

# Verificar si gh está instalado
$ghInstalled = Get-Command gh -ErrorAction SilentlyContinue

if ($ghInstalled) {
    Write-Host "✅ GitHub CLI (gh) detectado" -ForegroundColor Green
    Write-Host ""
    Write-Host "📤 Subiendo MSI al release v1.4.0-beta..." -ForegroundColor Yellow
    Write-Host ""
    
    # Subir el asset al release
    gh release upload v1.4.0-beta $msiPath --clobber
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Green
        Write-Host "  ✅ MSI AGREGADO EXITOSAMENTE AL RELEASE" -ForegroundColor Green
        Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Green
        Write-Host ""
        Write-Host "🔗 Verifica en:" -ForegroundColor Cyan
        Write-Host "   https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases/tag/v1.4.0-beta" -ForegroundColor White
        Write-Host ""
    } else {
        Write-Host ""
        Write-Host "❌ Error al subir el MSI" -ForegroundColor Red
        Write-Host "   Intenta manualmente desde GitHub" -ForegroundColor Yellow
        Write-Host ""
    }
} else {
    Write-Host "⚠️ GitHub CLI (gh) no está instalado" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "📋 OPCIÓN 1: Instalar GitHub CLI" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "   Descarga desde: https://cli.github.com/" -ForegroundColor White
    Write-Host "   O instala con winget:" -ForegroundColor White
    Write-Host "   winget install --id GitHub.cli" -ForegroundColor Gray
    Write-Host ""
    Write-Host "   Luego ejecuta este script de nuevo." -ForegroundColor Gray
    Write-Host ""
    Write-Host "📋 OPCIÓN 2: Subir manualmente desde GitHub (RECOMENDADO)" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "   1. Abre: https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases/tag/v1.4.0-beta" -ForegroundColor White
    Write-Host "   2. Clic en 'Edit release' (icono lápiz)" -ForegroundColor White
    Write-Host "   3. Arrastra el MSI a 'Attach binaries'" -ForegroundColor White
    Write-Host "   4. Archivo: $msiPath" -ForegroundColor Gray
    Write-Host "   5. Espera a que se suba (~$msiSize MB)" -ForegroundColor White
    Write-Host "   6. Clic en 'Update release'" -ForegroundColor White
    Write-Host ""
}

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
