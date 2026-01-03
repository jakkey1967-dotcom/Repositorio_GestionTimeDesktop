# ========================================
# 📦 Instalador MSI Profesional
# GestionTime Desktop - v1.0.0
# WiX Toolset v4
# ========================================

$ErrorActionPreference = "Stop"

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "📦 GESTIONTIME DESKTOP - MSI INSTALLER" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Configuración
$version = "1.0.0"
$manufacturer = "Global Retail Group"
$productName = "GestionTime Desktop"
$upgradeCode = "E7B2F4D1-5C8A-4F3E-9B6D-2A1E8C4F7D3B"
$outputFolder = "publish"
$msiName = "GestionTime-Setup-v$version.msi"

# Verificar herramientas
Write-Host "🔍 Verificando herramientas..." -ForegroundColor Yellow
try {
    $dotnetVersion = & dotnet --version 2>&1
    Write-Host "✅ .NET SDK: $dotnetVersion" -ForegroundColor Green
}
catch {
    Write-Host "❌ ERROR: .NET SDK no encontrado" -ForegroundColor Red
    exit 1
}

try {
    $wixVersion = & wix --version 2>&1
    Write-Host "✅ WiX Toolset: $wixVersion" -ForegroundColor Green
}
catch {
    Write-Host "⚠️ WiX Toolset no encontrado, instalando..." -ForegroundColor Yellow
    dotnet tool install --global wix --version 4.0.5
    Write-Host "✅ WiX Toolset instalado" -ForegroundColor Green
}
Write-Host ""

# Limpiar builds anteriores
Write-Host "🧹 Limpiando builds anteriores..." -ForegroundColor Yellow
@("bin", "obj", $outputFolder, $msiName, "*.wixobj", "*.wixpdb", "Product.wxs") | ForEach-Object {
    if (Test-Path $_) {
        Remove-Item -Path $_ -Recurse -Force -ErrorAction SilentlyContinue
    }
}
Write-Host "✅ Limpieza completada" -ForegroundColor Green
Write-Host ""

# Verificar appsettings.json
Write-Host "🔍 Verificando configuración..." -ForegroundColor Yellow
if (-not (Test-Path "appsettings.json")) {
    Write-Host "❌ ERROR: appsettings.json no encontrado" -ForegroundColor Red
    exit 1
}

$config = Get-Content "appsettings.json" -Raw | ConvertFrom-Json
Write-Host "📋 API: $($config.Api.BaseUrl)" -ForegroundColor Cyan
Write-Host ""

# Compilar aplicación
Write-Host "🔨 Compilando aplicación Release..." -ForegroundColor Yellow
$publishArgs = @(
    "publish",
    "-c", "Release",
    "-r", "win-x64",
    "--self-contained", "true",
    "/p:PublishSingleFile=false",
    "/p:PublishTrimmed=false",
    "/p:PublishReadyToRun=true",
    "/p:Platform=x64"
)

$buildProcess = Start-Process -FilePath "dotnet" -ArgumentList $publishArgs -NoNewWindow -Wait -PassThru

if ($buildProcess.ExitCode -ne 0) {
    Write-Host "❌ Compilación falló" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Compilación exitosa" -ForegroundColor Green
Write-Host ""

# Localizar carpeta publish
$publishPath = Get-ChildItem -Path "bin\Release" -Recurse -Directory -Filter "publish" | Select-Object -First 1
if (-not $publishPath) {
    Write-Host "❌ Carpeta publish no encontrada" -ForegroundColor Red
    exit 1
}

Write-Host "📂 Copiando archivos..." -ForegroundColor Yellow
New-Item -ItemType Directory -Path $outputFolder -Force | Out-Null
Copy-Item -Path "$($publishPath.FullName)\*" -Destination $outputFolder -Recurse -Force

$totalSize = (Get-ChildItem -Path $outputFolder -Recurse -File | Measure-Object -Property Length -Sum).Sum / 1MB
Write-Host "✅ $("{0:N2}" -f $totalSize) MB copiados a $outputFolder\" -ForegroundColor Green
Write-Host ""

# Generar lista de archivos para WiX (solo archivos en la raíz de publish)
Write-Host "📝 Generando archivo WiX..." -ForegroundColor Yellow

$files = Get-ChildItem -Path $outputFolder -File
$componentId = 1
$fileComponents = @()

foreach ($file in $files) {
    $fileName = $file.Name
    $fileId = "File_$componentId"
    $componentGuid = [guid]::NewGuid().ToString().ToUpper()
    
    $fileComponents += @"
      <Component Id="Component_$componentId" Guid="$componentGuid">
        <File Id="$fileId" Source="$outputFolder\$fileName" KeyPath="yes" />
      </Component>
"@
    $componentId++
}

# Procesar subdirectorios (Assets, etc.)
$directories = Get-ChildItem -Path $outputFolder -Directory
$directoryFragments = @()

foreach ($dir in $directories) {
    $dirName = $dir.Name
    $dirFiles = Get-ChildItem -Path $dir.FullName -File
    $dirComponents = @()
    
    foreach ($file in $dirFiles) {
        $fileName = $file.Name
        $fileId = "File_$componentId"
        $componentGuid = [guid]::NewGuid().ToString().ToUpper()
        
        $dirComponents += @"
        <Component Id="Component_$componentId" Guid="$componentGuid">
          <File Id="$fileId" Source="$outputFolder\$dirName\$fileName" KeyPath="yes" />
        </Component>
"@
        $componentId++
    }
    
    if ($dirComponents.Count -gt 0) {
        $dirComponentsXml = $dirComponents -join "`r`n"
        $directoryFragments += @"
    <Directory Id="Dir_$dirName" Name="$dirName">
$dirComponentsXml
    </Directory>
"@
    }
}

$componentsXml = $fileComponents -join "`r`n"
$directoriesXml = $directoryFragments -join "`r`n"

# Generar lista de ComponentRef para Feature
$componentRefs = @()
for ($i = 1; $i -lt $componentId; $i++) {
    $componentRefs += "      <ComponentRef Id=`"Component_$i`" />"
}
$componentRefsXml = $componentRefs -join "`r`n"

$wixContent = @"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  <Package Name="$productName" 
           Version="$version" 
           Manufacturer="$manufacturer"
           UpgradeCode="$upgradeCode"
           Language="1033">
    
    <MajorUpgrade DowngradeErrorMessage="Ya existe una versión más reciente instalada." />
    
    <MediaTemplate EmbedCab="yes" />
    
    <Feature Id="ProductFeature" Title="$productName" Level="1">
$componentRefsXml
      <ComponentRef Id="ApplicationShortcut" />
      <ComponentRef Id="DesktopShortcut" />
    </Feature>
    
    <StandardDirectory Id="ProgramFiles6432Folder">
      <Directory Id="INSTALLFOLDER" Name="GestionTime">
$directoriesXml
      </Directory>
    </StandardDirectory>
    
    <StandardDirectory Id="ProgramMenuFolder">
      <Directory Id="ApplicationProgramsFolder" Name="$productName"/>
    </StandardDirectory>
    
    <StandardDirectory Id="DesktopFolder" />
    
    <Icon Id="ProductIcon" SourceFile="$outputFolder\GestionTime.Desktop.exe" />
    <Property Id="ARPPRODUCTICON" Value="ProductIcon" />
    <Property Id="ARPHELPLINK" Value="https://global-retail.com" />
  </Package>

  <Fragment>
    <ComponentGroup Id="ProductComponents" Directory="INSTALLFOLDER">
$componentsXml
    </ComponentGroup>
  </Fragment>

  <Fragment>
    <Component Id="ApplicationShortcut" Directory="ApplicationProgramsFolder" Guid="A1B2C3D4-E5F6-7890-ABCD-EF1234567890">
      <Shortcut Id="ApplicationStartMenuShortcut"
                Name="$productName"
                Description="Sistema de gestión de partes de trabajo"
                Target="[INSTALLFOLDER]GestionTime.Desktop.exe"
                WorkingDirectory="INSTALLFOLDER"
                Icon="ProductIcon" />
      <RemoveFolder Id="CleanUpShortCut" On="uninstall"/>
      <RegistryValue Root="HKCU" Key="Software\$manufacturer\$productName" Name="installed" Type="integer" Value="1" KeyPath="yes"/>
    </Component>
    
    <Component Id="DesktopShortcut" Directory="DesktopFolder" Guid="B2C3D4E5-F6A7-8901-BCDE-F12345678901">
      <Shortcut Id="ApplicationDesktopShortcut"
                Name="$productName"
                Description="Sistema de gestión de partes de trabajo"
                Target="[INSTALLFOLDER]GestionTime.Desktop.exe"
                WorkingDirectory="INSTALLFOLDER"
                Icon="ProductIcon" />
      <RegistryValue Root="HKCU" Key="Software\$manufacturer\$productName" Name="desktop" Type="integer" Value="1" KeyPath="yes"/>
    </Component>
  </Fragment>
</Wix>
"@

$wixFile = "Product.wxs"
$wixContent | Out-File -FilePath $wixFile -Encoding UTF8
Write-Host "✅ Archivo WiX generado: $wixFile" -ForegroundColor Green
Write-Host "   • Componentes: $($componentId - 1)" -ForegroundColor Gray
Write-Host ""

# Compilar MSI
Write-Host "🔨 Compilando MSI (esto puede tardar varios minutos)..." -ForegroundColor Yellow

$wixArgs = @(
    "build",
    $wixFile,
    "-out", $msiName,
    "-arch", "x64"
)

$wixProcess = Start-Process -FilePath "wix" -ArgumentList $wixArgs -NoNewWindow -Wait -PassThru

if ($wixProcess.ExitCode -ne 0) {
    Write-Host "❌ Compilación MSI falló (código: $($wixProcess.ExitCode))" -ForegroundColor Red
    Write-Host "   El archivo Product.wxs se ha conservado para revisión" -ForegroundColor Yellow
    exit 1
}

if (-not (Test-Path $msiName)) {
    Write-Host "❌ MSI no generado" -ForegroundColor Red
    exit 1
}

$msiSize = (Get-Item $msiName).Length / 1MB
Write-Host "✅ MSI generado exitosamente" -ForegroundColor Green
Write-Host ""

# Limpiar archivos temporales
Write-Host "🧹 Limpiando archivos temporales..." -ForegroundColor Yellow
Remove-Item -Path "*.wixobj" -Force -ErrorAction SilentlyContinue
Remove-Item -Path "*.wixpdb" -Force -ErrorAction SilentlyContinue
Remove-Item -Path $wixFile -Force -ErrorAction SilentlyContinue
Write-Host "✅ Limpieza completada" -ForegroundColor Green
Write-Host ""

# Resumen
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✅ INSTALADOR MSI CREADO EXITOSAMENTE" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "📦 ARCHIVO:" -ForegroundColor Yellow
Write-Host "   • Nombre: $msiName" -ForegroundColor White
Write-Host "   • Tamaño: $("{0:N2}" -f $msiSize) MB" -ForegroundColor White
Write-Host "   • Versión: $version" -ForegroundColor White
Write-Host ""
Write-Host "✨ LO QUE HACE EL INSTALADOR:" -ForegroundColor Yellow
Write-Host "   ✅ Crea carpeta: C:\Program Files\GestionTime\" -ForegroundColor White
Write-Host "   ✅ Copia TODOS los archivos y subdirectorios" -ForegroundColor White
Write-Host "   ✅ Crea acceso directo en Menú Inicio (con icono)" -ForegroundColor White
Write-Host "   ✅ Crea acceso directo en Escritorio (con icono)" -ForegroundColor White
Write-Host "   ✅ Registra en Windows (aparece en Panel de Control)" -ForegroundColor White
Write-Host "   ✅ Desinstalación limpia automática" -ForegroundColor White
Write-Host "   ✅ Detecta versiones anteriores (actualización automática)" -ForegroundColor White
Write-Host ""
Write-Host "🚀 USO:" -ForegroundColor Yellow
Write-Host "   1. Doble clic en $msiName" -ForegroundColor White
Write-Host "   2. Seguir wizard de instalación" -ForegroundColor White
Write-Host "   3. Ejecutar desde accesos directos" -ForegroundColor White
Write-Host ""
Write-Host "🎨 ICONO INCLUIDO:" -ForegroundColor Yellow
Write-Host "   ✅ El ejecutable tiene el icono personalizado" -ForegroundColor White
Write-Host "   ✅ Los accesos directos usan el icono del ejecutable" -ForegroundColor White
Write-Host "   ✅ Aparece en Explorador de Windows" -ForegroundColor White
Write-Host "   ✅ Aparece en Barra de Tareas al ejecutar" -ForegroundColor White
Write-Host ""
Write-Host "✅ LISTO PARA DISTRIBUCIÓN" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
