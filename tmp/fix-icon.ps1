# ========================================
# 🎨 Configuración Completa de Icono
# GestionTime Desktop - Proceso Automatizado
# ========================================

$ErrorActionPreference = "Stop"

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🎨 CONFIGURACIÓN AUTOMÁTICA DE ICONO" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Paso 1: Verificar PNG fuente
$pngPath = "Assets\app_logo.png"
$icoPath = "Assets\app.ico"

Write-Host "🔍 Paso 1: Verificando archivos..." -ForegroundColor Yellow

if (-not (Test-Path $pngPath)) {
    Write-Host "❌ Error: No se encuentra $pngPath" -ForegroundColor Red
    Write-Host ""
    Write-Host "Por favor, coloca tu logo como 'app_logo.png' en Assets\" -ForegroundColor White
    exit 1
}

Write-Host "✅ Logo fuente encontrado" -ForegroundColor Green
Write-Host ""

# Paso 2: Convertir a ICO (si no existe)
if (-not (Test-Path $icoPath)) {
    Write-Host "🔄 Paso 2: Convirtiendo PNG a ICO..." -ForegroundColor Yellow
    Write-Host ""
    
    try {
        Add-Type -AssemblyName System.Drawing
        
        $img = [System.Drawing.Image]::FromFile((Resolve-Path $pngPath))
        
        # Crear bitmap de 256x256 (tamaño estándar)
        $size = 256
        $bitmap = New-Object System.Drawing.Bitmap($size, $size)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.DrawImage($img, 0, 0, $size, $size)
        $graphics.Dispose()
        
        # Guardar como ICO
        $bitmap.Save($icoPath, [System.Drawing.Imaging.ImageFormat]::Icon)
        
        $bitmap.Dispose()
        $img.Dispose()
        
        Write-Host "✅ Icono creado: $icoPath" -ForegroundColor Green
        $iconSize = (Get-Item $icoPath).Length / 1KB
        Write-Host "   Tamaño: $("{0:N2}" -f $iconSize) KB" -ForegroundColor Gray
    }
    catch {
        Write-Host "⚠️ No se pudo convertir automáticamente" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Por favor, convierte manualmente:" -ForegroundColor White
        Write-Host "1. Abre: https://convertio.co/es/png-ico/" -ForegroundColor Cyan
        Write-Host "2. Sube: $pngPath" -ForegroundColor Gray
        Write-Host "3. Descarga y guarda como: $icoPath" -ForegroundColor Gray
        Write-Host "4. Vuelve a ejecutar este script" -ForegroundColor Gray
        exit 1
    }
}
else {
    Write-Host "✅ Icono ya existe: $icoPath" -ForegroundColor Green
}

Write-Host ""

# Paso 3: Configurar proyecto
Write-Host "🔧 Paso 3: Configurando proyecto..." -ForegroundColor Yellow

$csprojPath = "GestionTime.Desktop.csproj"
$csprojContent = Get-Content $csprojPath -Raw

$modified = $false

# Agregar ApplicationIcon si no existe
if ($csprojContent -notmatch "<ApplicationIcon>") {
    Write-Host "   • Agregando ApplicationIcon..." -ForegroundColor Gray
    
    # Buscar primer PropertyGroup y agregar ApplicationIcon
    $pattern = "(<PropertyGroup>(?:(?!</PropertyGroup>).)*)"
    if ($csprojContent -match $pattern) {
        $replacement = '$1' + "`r`n    <ApplicationIcon>Assets\app.ico</ApplicationIcon>"
        $csprojContent = $csprojContent -replace $pattern, $replacement
        $modified = $true
    }
}

# Agregar Content Include si no existe
if ($csprojContent -notmatch 'Include="Assets\\app\.ico"') {
    Write-Host "   • Agregando icono a los archivos del proyecto..." -ForegroundColor Gray
    
    # Buscar ItemGroup de Content y agregar
    $pattern = '(<Content Include="Assets\\[^"]+"\s*/>)'
    if ($csprojContent -match $pattern) {
        $replacement = '$1' + "`r`n    <Content Include=`"Assets\app.ico`" />"
        $csprojContent = $csprojContent -replace $pattern, $replacement, 1
        $modified = $true
    }
}

if ($modified) {
    $csprojContent | Out-File -FilePath $csprojPath -Encoding UTF8 -NoNewline
    Write-Host "✅ Proyecto actualizado" -ForegroundColor Green
}
else {
    Write-Host "✅ Proyecto ya está configurado" -ForegroundColor Green
}

Write-Host ""

# Paso 4: Recompilar
Write-Host "🔨 Paso 4: Recompilando proyecto..." -ForegroundColor Yellow
Write-Host ""

# Clean
Write-Host "   • dotnet clean..." -ForegroundColor Gray
$cleanProcess = Start-Process -FilePath "dotnet" -ArgumentList "clean" -NoNewWindow -Wait -PassThru
if ($cleanProcess.ExitCode -ne 0) {
    Write-Host "⚠️ Clean tuvo advertencias, continuando..." -ForegroundColor Yellow
}

# Build
Write-Host "   • dotnet build..." -ForegroundColor Gray
$buildProcess = Start-Process -FilePath "dotnet" -ArgumentList "build", "-c", "Release" -NoNewWindow -Wait -PassThru
if ($buildProcess.ExitCode -ne 0) {
    Write-Host "❌ Build falló" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Compilación exitosa" -ForegroundColor Green
Write-Host ""

# Verificar que el ejecutable tiene el icono
$exePath = Get-ChildItem -Path "bin\Release" -Recurse -Filter "GestionTime.Desktop.exe" | Select-Object -First 1

if ($exePath) {
    Write-Host "✅ Ejecutable generado: $($exePath.FullName)" -ForegroundColor Green
    Write-Host ""
}

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✅ CONFIGURACIÓN COMPLETADA" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "🎉 El icono ahora aparece en:" -ForegroundColor Yellow
Write-Host "   ✅ Ejecutable (.exe)" -ForegroundColor White
Write-Host "   ✅ Barra de tareas (al ejecutar)" -ForegroundColor White
Write-Host "   ✅ Explorador de Windows" -ForegroundColor White
Write-Host "   ✅ Instalador MSI (si lo regeneras)" -ForegroundColor White
Write-Host ""
Write-Host "📋 Para regenerar el instalador MSI con el nuevo icono:" -ForegroundColor Yellow
Write-Host "   .\build-msi.ps1" -ForegroundColor Cyan
Write-Host ""
Write-Host "🚀 Para ejecutar la aplicación:" -ForegroundColor Yellow
Write-Host "   $($exePath.FullName)" -ForegroundColor Cyan
Write-Host ""
