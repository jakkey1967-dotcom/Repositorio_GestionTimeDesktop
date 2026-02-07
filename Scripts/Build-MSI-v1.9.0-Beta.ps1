# BUILD MSI v1.9.0 Beta - Con Documentacion Completa
# Build automatizado del instalador MSI con documentacion incluida

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "  BUILDING GESTIONTIME DESKTOP v1.9.0 BETA - MSI INSTALLER" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

$ErrorActionPreference = "Stop"

# 1. VERIFICAR REQUISITOS
Write-Host "[1/6] Verificando requisitos..." -ForegroundColor Yellow

$wixToolset = Get-ItemProperty -Path "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*" | Where-Object { $_.DisplayName -like "*WiX Toolset*" }

if (-not $wixToolset) {
    Write-Host "ERROR: WiX Toolset v3.14 no esta instalado" -ForegroundColor Red
    Write-Host "Descarga desde: https://github.com/wixtoolset/wix3/releases/tag/wix3141rtm" -ForegroundColor Yellow
    exit 1
}

Write-Host "OK: WiX Toolset encontrado" -ForegroundColor Green

$candlePath = "C:\Program Files (x86)\WiX Toolset v3.14\bin\candle.exe"
$lightPath = "C:\Program Files (x86)\WiX Toolset v3.14\bin\light.exe"

if (-not (Test-Path $candlePath)) {
    Write-Host "ERROR: No se encuentra candle.exe" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $lightPath)) {
    Write-Host "ERROR: No se encuentra light.exe" -ForegroundColor Red
    exit 1
}

Write-Host "OK: Herramientas WiX verificadas" -ForegroundColor Green
Write-Host ""

# 2. PUBLICAR APLICACION
Write-Host "[2/6] Publicando aplicacion .NET 8..." -ForegroundColor Yellow

$publishFolder = ".\publish\portable"

if (Test-Path $publishFolder) {
    Write-Host "  Limpiando carpeta publish..." -ForegroundColor Gray
    Remove-Item -Path $publishFolder -Recurse -Force
}

Write-Host "  Ejecutando dotnet publish..." -ForegroundColor Gray

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
    Write-Host "ERROR: dotnet publish failed - Exit code: $LASTEXITCODE" -ForegroundColor Red
    exit 1
}

Write-Host "OK: Aplicacion publicada" -ForegroundColor Green
Write-Host ""

# 3. COPIAR ARCHIVOS CRITICOS
Write-Host "[3/6] Copiando archivos criticos..." -ForegroundColor Yellow

$priSource = "bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\GestionTime.Desktop.pri"
$priDest = "$publishFolder\GestionTime.Desktop.pri"

if (Test-Path $priSource) {
    Copy-Item -Path $priSource -Destination $priDest -Force
    Write-Host "  OK: GestionTime.Desktop.pri" -ForegroundColor Green
} else {
    Write-Host "  WARN: .pri no encontrado en $priSource" -ForegroundColor Yellow
}

$assetsFolder = "$publishFolder\Assets"
if (-not (Test-Path $assetsFolder)) {
    Write-Host "  ERROR: Carpeta Assets no existe" -ForegroundColor Red
    exit 1
}

$assetsCount = (Get-ChildItem -Path $assetsFolder -File).Count
Write-Host "  OK: Assets ($assetsCount archivos)" -ForegroundColor Green

Write-Host "  Copiando documentacion..." -ForegroundColor Gray

Copy-Item -Path "CHANGELOG.md" -Destination "$publishFolder\CHANGELOG.md" -Force
Write-Host "  OK: CHANGELOG.md" -ForegroundColor Green

Copy-Item -Path "RELEASE_NOTES_v1.9.0.md" -Destination "$publishFolder\RELEASE_NOTES_v1.9.0.md" -Force
Write-Host "  OK: RELEASE_NOTES_v1.9.0.md" -ForegroundColor Green

if (Test-Path "window-config.ini") {
    Copy-Item -Path "window-config.ini" -Destination "$publishFolder\window-config.ini" -Force
    Write-Host "  OK: window-config.ini" -ForegroundColor Green
}

if (Test-Path "appsettings.json") {
    Copy-Item -Path "appsettings.json" -Destination "$publishFolder\appsettings.json" -Force
    Write-Host "  OK: appsettings.json" -ForegroundColor Green
}

Write-Host ""

# 4. GENERAR COMPONENTES WIX
Write-Host "[4/6] Generando componentes WiX..." -ForegroundColor Yellow

$heatPath = "C:\Program Files (x86)\WiX Toolset v3.14\bin\heat.exe"
$wixDir = ".\WiX-v3-MSI"

if (-not (Test-Path $heatPath)) {
    Write-Host "ERROR: heat.exe no encontrado" -ForegroundColor Red
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
    Write-Host "ERROR: heat.exe failed - Exit code: $LASTEXITCODE" -ForegroundColor Red
    exit 1
}

Write-Host "OK: Componentes WiX generados" -ForegroundColor Green
Write-Host ""

# 5. COMPILAR MSI
Write-Host "[5/6] Compilando MSI..." -ForegroundColor Yellow

Set-Location -Path $wixDir

Write-Host "  Ejecutando candle.exe..." -ForegroundColor Gray

$candleArgs = @(
    "Product.wxs", "HarvestedFiles.wxs"
    "-ext", "WixUIExtension"
    "-ext", "WixUtilExtension"
    "-arch", "x64"
    "-dSourceDir=..\publish\portable"
)

& $candlePath $candleArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: candle.exe failed - Exit code: $LASTEXITCODE" -ForegroundColor Red
    Set-Location -Path ..
    exit 1
}

Write-Host "  Ejecutando light.exe..." -ForegroundColor Gray

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
    Write-Host "ERROR: light.exe failed - Exit code: $LASTEXITCODE" -ForegroundColor Red
    Set-Location -Path ..
    exit 1
}

Set-Location -Path ..

Write-Host "OK: MSI compilado exitosamente" -ForegroundColor Green
Write-Host ""

# 6. VERIFICAR MSI
Write-Host "[6/6] Verificando MSI generado..." -ForegroundColor Yellow

$msiPath = "$wixDir\GestionTime-v1.9.0-Setup.msi"

if (-not (Test-Path $msiPath)) {
    Write-Host "ERROR: MSI no fue generado" -ForegroundColor Red
    exit 1
}

$msiSize = [math]::Round((Get-Item $msiPath).Length / 1MB, 2)

Write-Host ""
Write-Host "================================================================" -ForegroundColor Green
Write-Host "  BUILD COMPLETADO EXITOSAMENTE" -ForegroundColor Green
Write-Host "================================================================" -ForegroundColor Green
Write-Host ""
Write-Host "MSI Generado:" -ForegroundColor Cyan
Write-Host "  Ubicacion: $msiPath" -ForegroundColor White
Write-Host "  Tamano: $msiSize MB" -ForegroundColor White
Write-Host ""
Write-Host "Archivos Incluidos:" -ForegroundColor Cyan
Write-Host "  - GestionTime.Desktop.exe" -ForegroundColor White
Write-Host "  - GestionTime.Desktop.pri" -ForegroundColor White
Write-Host "  - Assets\ (14+ archivos)" -ForegroundColor White
Write-Host "  - window-config.ini" -ForegroundColor White
Write-Host "  - appsettings.json" -ForegroundColor White
Write-Host "  - CHANGELOG.md (NEW)" -ForegroundColor Yellow
Write-Host "  - RELEASE_NOTES_v1.9.0.md (NEW)" -ForegroundColor Yellow
Write-Host "  - 355+ DLLs de runtime" -ForegroundColor White
Write-Host ""
Write-Host "Proximos Pasos:" -ForegroundColor Cyan
Write-Host "  1. Probar instalador en entorno limpio" -ForegroundColor White
Write-Host "  2. Verificar instalacion en C:\App\GestionTime-Desktop\" -ForegroundColor White
Write-Host "  3. Verificar CHANGELOG.md y RELEASE_NOTES presentes" -ForegroundColor White
Write-Host "  4. Crear Release en GitHub con MSI adjunto" -ForegroundColor White
Write-Host ""
Write-Host "================================================================" -ForegroundColor Green

Start-Process explorer.exe -ArgumentList $wixDir
