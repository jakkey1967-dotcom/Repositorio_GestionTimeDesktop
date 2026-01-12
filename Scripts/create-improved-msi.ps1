param(
    [switch]$OpenOutput
)

Write-Host ""
Write-Host "🔨 CREANDO MSI MEJORADO CON ARCHIVOS CRÍTICOS" -ForegroundColor Green -BackgroundColor DarkGreen
Write-Host "===============================================" -ForegroundColor Green
Write-Host ""

# Verificar aplicación compilada
$appPath = "bin\Release\Installer\App"
if (!(Test-Path "$appPath\GestionTime.Desktop.exe")) {
    Write-Host "❌ ERROR: Aplicación no compilada" -ForegroundColor Red
    Write-Host "   Ejecutar: .\build-for-installer.ps1 -Clean" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ Aplicación encontrada" -ForegroundColor Green

# Analizar archivos críticos
$criticalFiles = @(
    "GestionTime.Desktop.exe",
    "GestionTime.Desktop.dll", 
    "appsettings.json",
    "System.Private.CoreLib.dll",
    "Microsoft.WindowsAppRuntime.dll",
    "WinRT.Runtime.dll",
    "Microsoft.UI.Xaml.dll",
    "Microsoft.UI.pri",
    "Microsoft.UI.Xaml.Controls.pri",
    "Microsoft.WindowsAppRuntime.pri"
)

Write-Host "🔍 VERIFICANDO ARCHIVOS CRÍTICOS:" -ForegroundColor Cyan
$existingFiles = @()
foreach ($file in $criticalFiles) {
    $filePath = Join-Path $appPath $file
    if (Test-Path $filePath) {
        $existingFiles += $file
        $fileInfo = Get-Item $filePath
        Write-Host "   ✅ $file ($([math]::Round($fileInfo.Length/1KB, 1)) KB)" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️  $file (NO ENCONTRADO)" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "📊 ARCHIVOS CRÍTICOS ENCONTRADOS: $($existingFiles.Count)/$($criticalFiles.Count)" -ForegroundColor Magenta

# Crear Features.wxs mejorado
$featuresContent = @'
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  
  <Fragment>
    
    <!-- Característica principal mejorada -->
    <Feature Id="MainApplication" 
             Title="GestionTime Desktop - Versión Mejorada" 
             Description="Aplicación con archivos críticos incluidos"
             Level="1" 
             ConfigurableDirectory="INSTALLFOLDER"
             AllowAdvertise="no"
             Display="expand">

      <!-- Archivos críticos -->
      <ComponentGroupRef Id="CriticalFiles" />
      <ComponentRef Id="StartMenuShortcuts" />
    </Feature>

    <!-- Característica opcional: Acceso directo en escritorio -->
    <Feature Id="DesktopShortcut" 
             Title="Acceso Directo en Escritorio"
             Description="Crear acceso directo en el escritorio" 
             Level="1"
             AllowAdvertise="no">
      <ComponentRef Id="DesktopShortcutComponent" />
    </Feature>

    <!-- Grupo de archivos críticos -->
    <ComponentGroup Id="CriticalFiles" Directory="INSTALLFOLDER">

'@

# Generar componentes para archivos existentes
$componentIndex = 1
foreach ($file in $existingFiles) {
    $componentId = "CriticalFile$componentIndex"
    $fileId = "File$componentIndex"
    $guid = [System.Guid]::NewGuid().ToString().ToUpper()
    
    $keyPath = if ($file -eq "GestionTime.Desktop.exe") { ' KeyPath="yes"' } else { '' }
    
    $featuresContent += @"
      <!-- $file -->
      <Component Id="$componentId" Guid="$guid">
        <File Id="$fileId" Source="..\..\bin\Release\Installer\App\$file"$keyPath />
      </Component>

"@
    $componentIndex++
}

$featuresContent += @'
    </ComponentGroup>

    <!-- Componente: Accesos directos en Menú Inicio -->
    <Component Id="StartMenuShortcuts" Directory="ApplicationProgramsFolder" Guid="66666666-6666-6666-6666-666666666666">
      <Shortcut Id="ApplicationStartMenuShortcut"
                Name="GestionTime Desktop"
                Description="Aplicación mejorada de gestión de tiempo"
                Target="[INSTALLFOLDER]GestionTime.Desktop.exe"
                WorkingDirectory="INSTALLFOLDER"
                Icon="app_logo.ico" />
      <RemoveFolder Id="ApplicationProgramsFolder" On="uninstall" />
      <RegistryValue Root="HKCU" 
                     Key="Software\GestionTime Solutions\GestionTime Desktop" 
                     Name="installed" 
                     Type="integer" 
                     Value="1" 
                     KeyPath="yes" />
    </Component>

    <!-- Componente: Acceso directo en Escritorio -->
    <Component Id="DesktopShortcutComponent" Directory="DesktopFolder" Guid="77777777-7777-7777-7777-777777777777">
      <Shortcut Id="ApplicationDesktopShortcut"
                Name="GestionTime Desktop"
                Description="Aplicación mejorada de gestión de tiempo"
                Target="[INSTALLFOLDER]GestionTime.Desktop.exe"
                WorkingDirectory="INSTALLFOLDER"
                Icon="app_logo.ico" />
      <RegistryValue Root="HKCU" 
                     Key="Software\GestionTime Solutions\GestionTime Desktop" 
                     Name="DesktopShortcut" 
                     Type="integer" 
                     Value="1" 
                     KeyPath="yes" />
    </Component>

  </Fragment>
</Wix>
'@

# Guardar Features mejorado
$featuresImprovedPath = "Installer\MSI\Features_Improved.wxs"
$featuresContent | Out-File -FilePath $featuresImprovedPath -Encoding UTF8

Write-Host "✅ Features mejoradas generadas: $featuresImprovedPath" -ForegroundColor Green

# Hacer backup del original y aplicar mejorado
Copy-Item "Installer\MSI\Features.wxs" "Installer\MSI\Features_Backup.wxs" -Force
Copy-Item $featuresImprovedPath "Installer\MSI\Features.wxs" -Force

Write-Host "✅ Features.wxs actualizado con versión mejorada" -ForegroundColor Green

# Compilar MSI mejorado
Write-Host ""
Write-Host "🔨 Compilando MSI mejorado..." -ForegroundColor Yellow

Push-Location "Installer\MSI"
try {
    wix build Product.wxs Features.wxs UI.wxs -out "..\..\bin\Release\MSI\GestionTimeDesktop-Improved-1.1.0.msi" -arch x64

    if ($LASTEXITCODE -eq 0) {
        $msiFile = Get-Item "..\..\bin\Release\MSI\GestionTimeDesktop-Improved-1.1.0.msi"
        
        Write-Host ""
        Write-Host "✅ MSI MEJORADO CREADO EXITOSAMENTE" -ForegroundColor Green -BackgroundColor DarkGreen
        Write-Host "===============================================" -ForegroundColor Green
        Write-Host ""
        
        Write-Host "📊 INFORMACIÓN DEL MSI MEJORADO:" -ForegroundColor Magenta
        Write-Host "   • Archivo: $($msiFile.Name)" -ForegroundColor White
        Write-Host "   • Tamaño: $([math]::Round($msiFile.Length/1MB, 2)) MB" -ForegroundColor White
        Write-Host "   • Archivos críticos incluidos: $($existingFiles.Count)" -ForegroundColor White
        Write-Host "   • Ubicación: $($msiFile.FullName)" -ForegroundColor White
        Write-Host ""
        
        Write-Host "🎯 ARCHIVOS INCLUIDOS EN EL MSI:" -ForegroundColor Cyan
        foreach ($file in $existingFiles) {
            Write-Host "   ✅ $file" -ForegroundColor Green
        }
        Write-Host ""
        
        Write-Host "📋 COMPARACIÓN DE INSTALADORES:" -ForegroundColor Blue
        Write-Host "   • MSI Básico:    ~1 MB, 3 archivos ❌ (NO FUNCIONA)" -ForegroundColor Red
        Write-Host "   • MSI Mejorado:  $([math]::Round($msiFile.Length/1MB, 2)) MB, $($existingFiles.Count) archivos ⚠️ (PUEDE FUNCIONAR)" -ForegroundColor Yellow
        Write-Host "   • Auto-extraíble: 126 MB, 520+ archivos ✅ (GARANTIZADO)" -ForegroundColor Green
        Write-Host ""
        
        Write-Host "📋 COMANDOS PARA PROBAR:" -ForegroundColor Yellow
        Write-Host "   # Instalar MSI mejorado:" -ForegroundColor White
        Write-Host "   msiexec /i `"$($msiFile.Name)`"" -ForegroundColor Gray
        Write-Host ""
        Write-Host "   # Desinstalar:" -ForegroundColor White
        Write-Host "   msiexec /x `"$($msiFile.Name)`" /quiet" -ForegroundColor Gray
        Write-Host ""
        
        if ($OpenOutput) {
            Start-Process "explorer.exe" -ArgumentList (Split-Path $msiFile.FullName)
        }
        
        Write-Host "🎉 MSI MEJORADO LISTO PARA PRUEBAS" -ForegroundColor Green
        
    } else {
        Write-Host "❌ ERROR: Falló la compilación del MSI mejorado" -ForegroundColor Red
    }
    
} finally {
    Pop-Location
}

Write-Host ""
Write-Host "💡 RECOMENDACIÓN:" -ForegroundColor Blue
Write-Host "   Para garantizar funcionamiento completo, usar:" -ForegroundColor White
Write-Host "   📁 bin\Release\SelfExtractingInstaller\GestionTimeDesktopInstaller.bat" -ForegroundColor Green
Write-Host "      (126 MB con TODOS los archivos)" -ForegroundColor Green

Write-Host ""
Write-Host "🔨 GENERACIÓN COMPLETADA" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green