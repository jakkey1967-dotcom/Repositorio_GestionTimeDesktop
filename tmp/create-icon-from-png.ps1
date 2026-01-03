# ========================================
# 🎨 Convertir PNG a ICO con PowerShell
# Requiere .NET Framework
# ========================================

Add-Type -AssemblyName System.Drawing

Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🎨 CONVERSIÓN DE PNG A ICO" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

$pngPath = "Assets\app_logo.png"
$icoPath = "Assets\app.ico"

# Verificar que existe el PNG
if (-not (Test-Path $pngPath)) {
    Write-Host "❌ Error: No se encuentra $pngPath" -ForegroundColor Red
    exit 1
}

Write-Host "📂 Archivo fuente: $pngPath" -ForegroundColor White
Write-Host ""

try {
    Write-Host "🔄 Cargando imagen..." -ForegroundColor Yellow
    
    # Cargar la imagen PNG
    $img = [System.Drawing.Image]::FromFile((Resolve-Path $pngPath))
    
    Write-Host "✅ Imagen cargada: $($img.Width)x$($img.Height) píxeles" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "🔄 Creando icono con múltiples tamaños..." -ForegroundColor Yellow
    
    # Crear icono con diferentes tamaños
    $sizes = @(16, 32, 48, 256)
    $icons = @()
    
    foreach ($size in $sizes) {
        Write-Host "   • Generando $size x $size..." -ForegroundColor Gray
        
        $bitmap = New-Object System.Drawing.Bitmap($size, $size)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.DrawImage($img, 0, 0, $size, $size)
        $graphics.Dispose()
        
        $icons += $bitmap
    }
    
    Write-Host ""
    Write-Host "💾 Guardando como: $icoPath" -ForegroundColor Yellow
    
    # Guardar el primer bitmap como ICO
    # Nota: PowerShell tiene limitaciones para crear ICOs multi-tamaño
    # Por eso recomendamos usar herramientas online
    $icons[0].Save($icoPath, [System.Drawing.Imaging.ImageFormat]::Icon)
    
    # Limpiar recursos
    foreach ($icon in $icons) {
        $icon.Dispose()
    }
    $img.Dispose()
    
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "✅ CONVERSIÓN COMPLETADA" -ForegroundColor Green
    Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "📁 Icono creado: $icoPath" -ForegroundColor White
    
    $iconSize = (Get-Item $icoPath).Length / 1KB
    Write-Host "💾 Tamaño: $("{0:N2}" -f $iconSize) KB" -ForegroundColor Gray
    Write-Host ""
    Write-Host "⚠️ NOTA IMPORTANTE:" -ForegroundColor Yellow
    Write-Host "   Esta conversión básica crea un ICO simple." -ForegroundColor White
    Write-Host "   Para mejor calidad, usa una herramienta online:" -ForegroundColor White
    Write-Host "   https://convertio.co/es/png-ico/" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "📋 PRÓXIMOS PASOS:" -ForegroundColor Yellow
    Write-Host "   1. Ejecutar: .\setup-icon.ps1" -ForegroundColor White
    Write-Host "   2. Recompilar el proyecto" -ForegroundColor White
    Write-Host ""
}
catch {
    Write-Host ""
    Write-Host "❌ Error durante la conversión: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "💡 ALTERNATIVA:" -ForegroundColor Yellow
    Write-Host "   Usa una herramienta online para convertir:" -ForegroundColor White
    Write-Host "   1. Abre: https://convertio.co/es/png-ico/" -ForegroundColor Cyan
    Write-Host "   2. Sube: Assets\app_logo.png" -ForegroundColor Gray
    Write-Host "   3. Descarga y guarda como: Assets\app.ico" -ForegroundColor Gray
    Write-Host "   4. Ejecuta: .\setup-icon.ps1" -ForegroundColor Gray
    Write-Host ""
}
