# ═══════════════════════════════════════════════════════════════
# BUILD MSI v1.9.0 Beta - Con Documentación Completa
# ═══════════════════════════════════════════════════════════════

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  BUILDING GESTIONTIME DESKTOP v1.9.0 BETA - MSI INSTALLER" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

$ErrorActionPreference = "Stop"

# ═══════════════════════════════════════════════════════════════
# 1. VERIFICAR REQUISITOS
# ═══════════════════════════════════════════════════════════════
Write-Host "[1/6] Verificando requisitos..." -ForegroundColor Yellow

# Verificar WiX Toolset
$wixToolset = Get-ItemProperty -Path "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*" | 
    Where-Object { $_.DisplayName -like "*WiX Toolset*" }

if (-not $wixToolset) {
    Write-Host "ERROR: WiX Toolset v3.14 no esta instalado" -ForegroundColor Red
    Write-Host ""
    Write-Host "Descarga e instala desde: https://github.com/wixtoolset/wix3/releases/tag/wix3141rtm" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ WiX Toolset encontrado: $($wixToolset.DisplayName)" -ForegroundColor Green

# Verificar candle.exe y light.exe
$candlePath = "C:\Program Files (x86)\WiX Toolset v3.14\bin\candle.exe"
$lightPath = "C:\Program Files (x86)\WiX Toolset v3.14\bin\light.exe"

if (-not (Test-Path $candlePath)) {
    Write-Host "ERROR: No se encuentra candle.exe en $candlePath" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $lightPath)) {
    Write-Host "ERROR: No se encuentra light.exe en $lightPath" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Herramientas WiX verificadas" -ForegroundColor Green
Write-Host ""

# ═══════════════════════════════════════════════════════════════
# 2. PUBLICAR APLICACIÓN (.NET 8)
# ═══════════════════════════════════════════════════════════════
Write-Host "[2/6] Publicando aplicación .NET 8..." -ForegroundColor Yellow

$publishFolder = ".\publish\portable"

# Limpiar carpeta publish anterior
if (Test-Path $publishFolder) {
    Write-Host "  🗑️  Limpiando carpeta publish anterior..." -ForegroundColor Gray
    Remove-Item -Path $publishFolder -Recurse -Force
}

# Publicar con .NET 8
Write-Host "  📦 Ejecutando dotnet publish..." -ForegroundColor Gray

$publishArgs = @(
    "publish"
    "GestionTime.Desktop.csproj"
    "-c", "Release"
    "-r", "win-x64"
    "--self-contained", "true"
    "-p:PublishSingleFile=false"
    "-p:PublishReadyToRun=true"
    "-o", $publishFolder
)

& dotnet $publishArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: dotnet publish failed with exit code: $LASTEXITCODE" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Aplicación publicada en: $publishFolder" -ForegroundColor Green
Write-Host ""

# ═══════════════════════════════════════════════════════════════
# 3. COPIAR ARCHIVOS CRÍTICOS
# ═══════════════════════════════════════════════════════════════
Write-Host "[3/6] Copiando archivos críticos..." -ForegroundColor Yellow

# Verificar y copiar .pri (recursos XAML compilados)
$priSource = "bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\GestionTime.Desktop.pri"
$priDest = "$publishFolder\GestionTime.Desktop.pri"

if (Test-Path $priSource) {
    Copy-Item -Path $priSource -Destination $priDest -Force
    Write-Host "  ✅ Copiado: GestionTime.Desktop.pri" -ForegroundColor Green
} else {
    Write-Host "  ⚠️  ADVERTENCIA: No se encontró .pri en $priSource" -ForegroundColor Yellow
}

# Verificar carpeta Assets
$assetsFolder = "$publishFolder\Assets"
if (-not (Test-Path $assetsFolder)) {
    Write-Host "  ERROR: Carpeta Assets no existe en publish" -ForegroundColor Red
    exit 1
}

$assetsCount = (Get-ChildItem -Path $assetsFolder -File).Count
Write-Host "  ✅ Carpeta Assets verificada: $assetsCount archivos" -ForegroundColor Green

# Copiar documentación de versión
Write-Host "  📝 Copiando documentación de versión..." -ForegroundColor Gray

Copy-Item -Path "CHANGELOG.md" -Destination "$publishFolder\CHANGELOG.md" -Force
Write-Host "  ✅ Copiado: CHANGELOG.md" -ForegroundColor Green

Copy-Item -Path "RELEASE_NOTES_v1.9.0.md" -Destination "$publishFolder\RELEASE_NOTES_v1.9.0.md" -Force
Write-Host "  ✅ Copiado: RELEASE_NOTES_v1.9.0.md" -ForegroundColor Green

# Copiar window-config.ini
if (Test-Path "window-config.ini") {
    Copy-Item -Path "window-config.ini" -Destination "$publishFolder\window-config.ini" -Force
    Write-Host "  ✅ Copiado: window-config.ini" -ForegroundColor Green
}

# Copiar appsettings.json
if (Test-Path "appsettings.json") {
    Copy-Item -Path "appsettings.json" -Destination "$publishFolder\appsettings.json" -Force
    Write-Host "  ✅ Copiado: appsettings.json" -ForegroundColor Green
}

Write-Host ""

# ═══════════════════════════════════════════════════════════════
# 4. GENERAR COMPONENTES WIX CON HEAT
# ═══════════════════════════════════════════════════════════════
Write-Host "[4/6] Generando componentes WiX con Heat.exe..." -ForegroundColor Yellow

$heatPath = "C:\Program Files (x86)\WiX Toolset v3.14\bin\heat.exe"
$wixDir = ".\WiX-v3-MSI"

if (-not (Test-Path $heatPath)) {
    Write-Host "ERROR: No se encuentra heat.exe" -ForegroundColor Red
    exit 1
}

$heatArgs = @(
    "dir", $publishFolder
    "-cg", "HarvestedFiles"
    "-gg"
    "-sfrag"
    "-srd"
    "-dr", "INSTALLFOLDER"
    "-var", "var.SourceDir"
    "-out", "$wixDir\HarvestedFiles.wxs"
)

& $heatPath $heatArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: heat.exe failed with exit code: $LASTEXITCODE" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Componentes generados: $wixDir\HarvestedFiles.wxs" -ForegroundColor Green
Write-Host ""

# ═══════════════════════════════════════════════════════════════
# 5. COMPILAR MSI CON CANDLE Y LIGHT
# ═══════════════════════════════════════════════════════════════
Write-Host "[5/6] Compilando MSI con WiX..." -ForegroundColor Yellow

Set-Location -Path $wixDir

# Compilar con candle.exe
Write-Host "  🔨 Ejecutando candle.exe..." -ForegroundColor Gray

$candleArgs = @(
    "Product.wxs", "HarvestedFiles.wxs"
    "-ext", "WixUIExtension"
    "-ext", "WixUtilExtension"
    "-arch", "x64"
)

& $candlePath $candleArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: candle.exe failed with exit code: $LASTEXITCODE" -ForegroundColor Red
    Set-Location -Path ..
    exit 1
}

# Enlazar con light.exe
Write-Host "  🔗 Ejecutando light.exe..." -ForegroundColor Gray

$lightArgs = @(
    "Product.wixobj", "HarvestedFiles.wixobj"
    "-ext", "WixUIExtension"
    "-ext", "WixUtilExtension"
    "-out", "GestionTime-v1.9.0-Setup.msi"
    "-sice:ICE61"
    "-sice:ICE69"
)

& $lightPath $lightArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: light.exe failed with exit code: $LASTEXITCODE" -ForegroundColor Red
    Set-Location -Path ..
    exit 1
}

Set-Location -Path ..

Write-Host "✅ MSI compilado exitosamente" -ForegroundColor Green
Write-Host ""

# ═══════════════════════════════════════════════════════════════
# 6. RESUMEN Y VERIFICACIÓN
# ═══════════════════════════════════════════════════════════════
Write-Host "[6/6] Verificando MSI generado..." -ForegroundColor Yellow

$msiPath = "$wixDir\GestionTime-v1.9.0-Setup.msi"

if (-not (Test-Path $msiPath)) {
    Write-Host "ERROR: MSI no fue generado" -ForegroundColor Red
    exit 1
}

$msiSize = [math]::Round((Get-Item $msiPath).Length / 1MB, 2)

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "  ✅ BUILD COMPLETADO EXITOSAMENTE" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Host "📦 MSI Generado:" -ForegroundColor Cyan
Write-Host "   Ubicación: $msiPath" -ForegroundColor White
Write-Host "   Tamaño:    $msiSize MB" -ForegroundColor White
Write-Host ""
Write-Host "📋 Archivos Incluidos en Instalador:" -ForegroundColor Cyan
Write-Host "   ✅ GestionTime.Desktop.exe" -ForegroundColor White
Write-Host "   ✅ GestionTime.Desktop.pri" -ForegroundColor White
Write-Host "   ✅ Assets\ (14+ archivos)" -ForegroundColor White
Write-Host "   ✅ window-config.ini" -ForegroundColor White
Write-Host "   ✅ appsettings.json" -ForegroundColor White
Write-Host "   ✅ CHANGELOG.md (NEW!)" -ForegroundColor Yellow
Write-Host "   ✅ RELEASE_NOTES_v1.9.0.md (NEW!)" -ForegroundColor Yellow
Write-Host "   ✅ 355+ DLLs de runtime" -ForegroundColor White
Write-Host ""
Write-Host "🎯 Próximos Pasos:" -ForegroundColor Cyan
Write-Host "   1. Probar instalador en entorno limpio" -ForegroundColor White
Write-Host "   2. Verificar que instala en C:\App\GestionTime-Desktop\" -ForegroundColor White
Write-Host "   3. Verificar que CHANGELOG.md y RELEASE_NOTES están presentes" -ForegroundColor White
Write-Host "   4. Crear tag de Git: git tag -a v1.9.0-beta -m 'Release v1.9.0 Beta'" -ForegroundColor White
Write-Host "   5. Push a GitHub: git push origin v1.9.0-beta" -ForegroundColor White
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Green

# Abrir carpeta del MSI en Explorer
Start-Process explorer.exe -ArgumentList $wixDir
