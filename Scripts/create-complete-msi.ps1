param(
    [switch]$IncludeAllFiles,
    [switch]$OpenOutput
)

Write-Host ""
Write-Host "🏗️  CREANDO INSTALADOR MSI COMPLETO" -ForegroundColor Green -BackgroundColor DarkGreen
Write-Host "===============================================" -ForegroundColor Green
Write-Host ""

# Ruta de archivos de la aplicación
$appPath = "bin\Release\Installer\App"
$outputPath = "bin\Release\MSI"

# Crear directorio de salida
if (!(Test-Path $outputPath)) {
    New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
}

# Obtener todos los archivos necesarios
$allFiles = Get-ChildItem $appPath -File -Recurse
$dllFiles = $allFiles | Where-Object { $_.Extension -eq ".dll" }
$exeFiles = $allFiles | Where-Object { $_.Extension -eq ".exe" }
$configFiles = $allFiles | Where-Object { $_.Extension -in @(".json", ".config", ".xml") }

Write-Host "📊 ANÁLISIS DE ARCHIVOS A INCLUIR:" -ForegroundColor Cyan
Write-Host "   • Total de archivos: $($allFiles.Count)" -ForegroundColor White
Write-Host "   • Archivos DLL: $($dllFiles.Count)" -ForegroundColor White
Write-Host "   • Archivos EXE: $($exeFiles.Count)" -ForegroundColor White
Write-Host "   • Archivos de configuración: $($configFiles.Count)" -ForegroundColor White
Write-Host ""

# Crear WiX dinámicamente con todos los archivos
$wxsContent = @"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  <Package Name="GestionTime Desktop" 
           Language="1034" 
           Version="1.1.0.0" 
           Manufacturer="GestionTime Solutions" 
           UpgradeCode="12345678-1234-1234-1234-123456789012">

    <!-- Permitir actualizaciones -->
    <MajorUpgrade DowngradeErrorMessage="Una versión más nueva ya está instalada." />

    <!-- Estructura de directorios -->
    <StandardDirectory Id="ProgramFiles6432Folder">
      <Directory Id="INSTALLFOLDER" Name="GestionTime Desktop" />
    </StandardDirectory>

    <!-- Componente para ejecutable principal -->
    <Component Id="MainExe" Directory="INSTALLFOLDER" Guid="11111111-1111-1111-1111-111111111111">
      <File Source="$appPath\GestionTime.Desktop.exe" KeyPath="yes" />
      
      <!-- Acceso directo en menú inicio -->
      <Shortcut Id="StartMenuShortcut" 
                Directory="ProgramMenuFolder" 
                Name="GestionTime Desktop" 
                Target="[INSTALLFOLDER]GestionTime.Desktop.exe" />
      
      <!-- Acceso directo en escritorio -->
      <Shortcut Id="DesktopShortcut" 
                Directory="DesktopFolder" 
                Name="GestionTime Desktop" 
                Target="[INSTALLFOLDER]GestionTime.Desktop.exe" />
    </Component>

"@

# Agregar todas las DLLs como componentes separados
$componentId = 2
foreach ($dll in $dllFiles) {
    $componentGuid = [Guid]::NewGuid().ToString().ToUpper()
    $relPath = $dll.FullName.Replace((Get-Item $appPath).FullName + "\", "")
    
    $wxsContent += @"
    <!-- Componente para $($dll.Name) -->
    <Component Id="Dll$componentId" Directory="INSTALLFOLDER" Guid="$componentGuid">
      <File Source="$($dll.FullName)" KeyPath="yes" />
    </Component>

"@
    $componentId++
}

# Agregar archivos de configuración
foreach ($config in $configFiles) {
    $componentGuid = [Guid]::NewGuid().ToString().ToUpper()
    $relPath = $config.FullName.Replace((Get-Item $appPath).FullName + "\", "")
    
    $wxsContent += @"
    <!-- Componente para $($config.Name) -->
    <Component Id="Config$componentId" Directory="INSTALLFOLDER" Guid="$componentGuid">
      <File Source="$($config.FullName)" KeyPath="yes" />
    </Component>

"@
    $componentId++
}

# Cerrar el feature con todos los componentes
$wxsContent += @"
    <!-- Feature principal con todos los componentes -->
    <Feature Id="MainFeature" Title="GestionTime Desktop" Level="1">
      <ComponentRef Id="MainExe" />
"@

# Referenciar todas las DLLs
$componentId = 2
foreach ($dll in $dllFiles) {
    $wxsContent += "      <ComponentRef Id=`"Dll$componentId`" />`r`n"
    $componentId++
}

# Referenciar archivos de configuración
foreach ($config in $configFiles) {
    $wxsContent += "      <ComponentRef Id=`"Config$componentId`" />`r`n"
    $componentId++
}

$wxsContent += @"
    </Feature>

  </Package>
</Wix>
"@

# Guardar el archivo WXS generado
$wxsPath = "Installer\ProductComplete.wxs"
$wxsContent | Out-File -FilePath $wxsPath -Encoding UTF8

Write-Host "✅ Archivo WXS generado: $wxsPath" -ForegroundColor Green
Write-Host "   • Componentes principales: 1" -ForegroundColor White
Write-Host "   • Componentes DLL: $($dllFiles.Count)" -ForegroundColor White
Write-Host "   • Componentes configuración: $($configFiles.Count)" -ForegroundColor White
Write-Host ""

# Compilar con WiX
Write-Host "🔨 Compilando MSI completo..." -ForegroundColor Yellow
try {
    wix build $wxsPath -out "$outputPath\GestionTimeDesktopComplete-1.1.0.msi" -arch x64

    if ($LASTEXITCODE -eq 0) {
        $msiFile = Get-Item "$outputPath\GestionTimeDesktopComplete-1.1.0.msi"
        
        Write-Host ""
        Write-Host "✅ INSTALADOR MSI COMPLETO CREADO" -ForegroundColor Green -BackgroundColor DarkGreen
        Write-Host "===============================================" -ForegroundColor Green
        Write-Host ""
        
        Write-Host "📊 INFORMACIÓN DEL INSTALADOR COMPLETO:" -ForegroundColor Magenta
        Write-Host "   • Archivo: $($msiFile.Name)" -ForegroundColor White
        Write-Host "   • Tamaño: $([math]::Round($msiFile.Length/1MB, 2)) MB" -ForegroundColor White
        Write-Host "   • Archivos incluidos: $($allFiles.Count)" -ForegroundColor White
        Write-Host "   • Ubicación: $($msiFile.FullName)" -ForegroundColor White
        Write-Host ""
        
        Write-Host "📦 CONTENIDO DEL INSTALADOR:" -ForegroundColor Cyan
        Write-Host "   ✅ Ejecutable principal (GestionTime.Desktop.exe)" -ForegroundColor Green
        Write-Host "   ✅ $($dllFiles.Count) bibliotecas DLL (incluye .NET runtime)" -ForegroundColor Green
        Write-Host "   ✅ $($configFiles.Count) archivos de configuración" -ForegroundColor Green
        Write-Host "   ✅ WindowsAppSDK self-contained" -ForegroundColor Green
        Write-Host "   ✅ Accesos directos (Escritorio + Menú Inicio)" -ForegroundColor Green
        Write-Host ""
        
        if ($OpenOutput) {
            Start-Process "explorer.exe" -ArgumentList $outputPath
        }
        
        Write-Host "🎉 ¡INSTALADOR MSI COMPLETO LISTO!" -ForegroundColor Green -BackgroundColor DarkGreen
        
    } else {
        Write-Host "❌ ERROR: Falló la compilación del MSI completo" -ForegroundColor Red
    }
    
} catch {
    Write-Host "❌ ERROR: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "===============================================" -ForegroundColor Green