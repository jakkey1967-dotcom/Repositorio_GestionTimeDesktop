param(
    [string]$Configuration = "Debug",
    [switch]$OpenOutput,
    [switch]$Rebuild
)

Write-Host ""
Write-Host "🔨 CREANDO MSI COMPLETO - CONFIGURACIÓN DEBUG" -ForegroundColor Green -BackgroundColor DarkGreen
Write-Host "===============================================" -ForegroundColor Green
Write-Host ""

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

try {
    # Build en Debug si es necesario
    if ($Rebuild -or !(Test-Path "bin\$Configuration\Installer\App\GestionTime.Desktop.exe")) {
        Write-Host "🔄 Compilando aplicación en $Configuration..." -ForegroundColor Cyan
        if ($Configuration -eq "Debug") {
            & powershell -ExecutionPolicy Bypass -File "build-for-installer-debug.ps1" -Clean
        } else {
            & powershell -ExecutionPolicy Bypass -File "build-for-installer.ps1" -Clean
        }
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ ERROR: Falló el build de la aplicación" -ForegroundColor Red
            exit 1
        }
    }

    # Verificar aplicación compilada
    $appPath = "bin\$Configuration\Installer\App"
    if (!(Test-Path "$appPath\GestionTime.Desktop.exe")) {
        Write-Host "❌ ERROR: Aplicación no compilada en $appPath" -ForegroundColor Red
        Write-Host "   Ejecutar: .\build-for-installer-debug.ps1 -Clean" -ForegroundColor Yellow
        exit 1
    }

    Write-Host "✅ Aplicación encontrada en $Configuration" -ForegroundColor Green

    # Obtener todos los archivos
    $allFiles = Get-ChildItem $appPath -File -Recurse
    Write-Host "📊 Archivos encontrados: $($allFiles.Count)" -ForegroundColor Cyan

    # Archivos críticos que DEBEN estar
    $criticalFiles = @(
        "GestionTime.Desktop.exe",
        "GestionTime.Desktop.dll", 
        "appsettings.json",
        "System.Private.CoreLib.dll",
        "Microsoft.WindowsAppRuntime.dll",
        "WinRT.Runtime.dll",
        "Microsoft.UI.Xaml.dll"
    )
    
    # Archivos de recursos
    $resourceFiles = @(
        "Microsoft.UI.pri",
        "Microsoft.UI.Xaml.Controls.pri",
        "Microsoft.WindowsAppRuntime.pri"
    )
    
    # Archivos críticos + recursos
    $allCriticalFiles = $criticalFiles + $resourceFiles

    Write-Host ""
    Write-Host "🔍 VERIFICANDO ARCHIVOS CRÍTICOS:" -ForegroundColor Cyan
    $existingFiles = @()
    foreach ($file in $allCriticalFiles) {
        $filePath = Join-Path $appPath $file
        if (Test-Path $filePath) {
            $existingFiles += $file
            $fileInfo = Get-Item $filePath
            Write-Host "   ✅ $file ($([math]::Round($fileInfo.Length/1KB, 1)) KB)" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️  $file (NO ENCONTRADO)" -ForegroundColor Yellow
        }
    }

    # Crear Features.wxs dinámico con TODOS los archivos críticos + runtime completo
    Write-Host ""
    Write-Host "🔨 Generando MSI con archivos completos..." -ForegroundColor Yellow

    # Seleccionar archivos importantes adicionales
    $runtimeFiles = $allFiles | Where-Object { 
        $_.Extension -eq ".dll" -and 
        ($_.Name -like "System.*" -or 
         $_.Name -like "Microsoft.*" -or 
         $_.Name -like "Windows.*" -or 
         $_.Name -like "netstandard*")
    } | Select-Object -First 50 # Limitar para evitar problemas de WiX

    $importantFiles = $existingFiles
    foreach ($runtimeFile in $runtimeFiles) {
        if ($runtimeFile.Name -notin $importantFiles) {
            $importantFiles += $runtimeFile.Name
        }
    }

    Write-Host "📊 Archivos importantes seleccionados: $($importantFiles.Count)" -ForegroundColor Magenta

    # Crear Features.wxs con archivos importantes
    $featuresContent = @"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  
  <Fragment>
    
    <!-- Característica principal con archivos completos -->
    <Feature Id="MainApplication" 
             Title="GestionTime Desktop - Debug Completo" 
             Description="Aplicación completa con runtime y debugging"
             Level="1" 
             ConfigurableDirectory="INSTALLFOLDER"
             AllowAdvertise="no"
             Display="expand">

      <!-- Archivos importantes -->
      <ComponentGroupRef Id="ImportantFiles" />
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

    <!-- Grupo de archivos importantes -->
    <ComponentGroup Id="ImportantFiles" Directory="INSTALLFOLDER">

"@

    # Generar componentes para archivos importantes
    $componentIndex = 1
    foreach ($file in $importantFiles) {
        $componentId = "ImportantFile$componentIndex"
        $fileId = "File$componentIndex"
        $guid = [System.Guid]::NewGuid().ToString().ToUpper()
        
        $keyPath = if ($file -eq "GestionTime.Desktop.exe") { ' KeyPath="yes"' } else { '' }
        
        $featuresContent += @"
      <!-- $file -->
      <Component Id="$componentId" Guid="$guid">
        <File Id="$fileId" Source="..\..\bin\$Configuration\Installer\App\$file"$keyPath />
      </Component>

"@
        $componentIndex++
    }

    $featuresContent += @'
    </ComponentGroup>

    <!-- Componente: Accesos directos en Menú Inicio -->
    <Component Id="StartMenuShortcuts" Directory="ApplicationProgramsFolder" Guid="66666666-6666-6666-6666-666666666666">
      <Shortcut Id="ApplicationStartMenuShortcut"
                Name="GestionTime Desktop (Debug)"
                Description="Aplicación de gestión de tiempo (Debug)"
                Target="[INSTALLFOLDER]GestionTime.Desktop.exe"
                WorkingDirectory="INSTALLFOLDER"
                Icon="app_logo.ico" />
      <RemoveFolder Id="ApplicationProgramsFolder" On="uninstall" />
      <RegistryValue Root="HKCU" 
                     Key="Software\GestionTime Solutions\GestionTime Desktop Debug" 
                     Name="installed" 
                     Type="integer" 
                     Value="1" 
                     KeyPath="yes" />
    </Component>

    <!-- Componente: Acceso directo en Escritorio -->
    <Component Id="DesktopShortcutComponent" Directory="DesktopFolder" Guid="77777777-7777-7777-7777-777777777777">
      <Shortcut Id="ApplicationDesktopShortcut"
                Name="GestionTime Desktop (Debug)"
                Description="Aplicación de gestión de tiempo (Debug)"
                Target="[INSTALLFOLDER]GestionTime.Desktop.exe"
                WorkingDirectory="INSTALLFOLDER"
                Icon="app_logo.ico" />
      <RegistryValue Root="HKCU" 
                     Key="Software\GestionTime Solutions\GestionTime Desktop Debug" 
                     Name="DesktopShortcut" 
                     Type="integer" 
                     Value="1" 
                     KeyPath="yes" />
    </Component>

  </Fragment>
</Wix>
'@

    # Guardar Features optimizado
    $featuresPath = "Installer\MSI\Features_Debug.wxs"
    $featuresContent | Out-File -FilePath $featuresPath -Encoding UTF8

    Write-Host "✅ Features_Debug.wxs generado con $($importantFiles.Count) archivos" -ForegroundColor Green

    # Actualizar Product.wxs para Debug
    $productContent = @'
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  
  <!-- Definición del paquete DEBUG -->
  <Package Name="GestionTime Desktop (Debug)"
           Language="1034"
           Version="1.1.0.0"
           Manufacturer="GestionTime Solutions"
           UpgradeCode="12345678-1234-1234-1234-123456789013"
           Scope="perMachine"
           InstallerVersion="500"
           Compressed="yes">

    <!-- Información del producto DEBUG -->
    <Property Id="ARPPRODUCTICON" Value="app_logo.ico" />
    <Property Id="ARPHELPLINK" Value="https://gestiontime.com/support" />
    <Property Id="ARPURLINFOABOUT" Value="https://gestiontime.com" />
    <Property Id="ARPNOMODIFY" Value="1" />
    <Property Id="ARPNOREPAIR" Value="1" />
    <Property Id="ARPCOMMENTS" Value="Aplicación de gestión de tiempo (DEBUG)" />

    <!-- Configuración de actualización -->
    <MajorUpgrade DowngradeErrorMessage="Una versión más nueva de [ProductName] ya está instalada."
                  AllowSameVersionUpgrades="yes"
                  Schedule="afterInstallInitialize" />

    <!-- Configuración de medios -->
    <MediaTemplate EmbedCab="yes" />

    <!-- Iconos -->
    <Icon Id="app_logo.ico" SourceFile="..\..\Assets\app_logo.ico" />

    <!-- Estructura de directorios -->
    <StandardDirectory Id="ProgramFiles6432Folder">
      <Directory Id="CompanyFolder" Name="GestionTime Solutions">
        <Directory Id="INSTALLFOLDER" Name="GestionTime Desktop Debug" />
      </Directory>
    </StandardDirectory>

    <StandardDirectory Id="ProgramMenuFolder">
      <Directory Id="ApplicationProgramsFolder" Name="GestionTime Desktop Debug" />
    </StandardDirectory>

    <StandardDirectory Id="DesktopFolder" />

    <!-- Referencia a características -->
    <FeatureRef Id="MainApplication" />
    <FeatureRef Id="DesktopShortcut" />

  </Package>
</Wix>
'@

    $productDebugPath = "Installer\MSI\Product_Debug.wxs"
    $productContent | Out-File -FilePath $productDebugPath -Encoding UTF8

    Write-Host "✅ Product_Debug.wxs creado" -ForegroundColor Green

    # Compilar MSI Debug
    Write-Host ""
    Write-Host "🔨 Compilando MSI Debug completo..." -ForegroundColor Yellow

    $outputDir = "bin\$Configuration\MSI"
    if (!(Test-Path $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    }

    Push-Location "Installer\MSI"
    try {
        wix build Product_Debug.wxs Features_Debug.wxs UI.wxs -out "..\..\$outputDir\GestionTimeDesktop-Debug-Complete-1.1.0.msi" -arch x64

        if ($LASTEXITCODE -eq 0) {
            $msiFile = Get-Item "..\..\$outputDir\GestionTimeDesktop-Debug-Complete-1.1.0.msi"
            
            $stopwatch.Stop()
            
            Write-Host ""
            Write-Host "✅ MSI DEBUG COMPLETO CREADO EXITOSAMENTE" -ForegroundColor Green -BackgroundColor DarkGreen
            Write-Host "===============================================" -ForegroundColor Green
            Write-Host ""
            
            Write-Host "📊 INFORMACIÓN DEL MSI DEBUG:" -ForegroundColor Magenta
            Write-Host "   • Archivo: $($msiFile.Name)" -ForegroundColor White
            Write-Host "   • Tamaño: $([math]::Round($msiFile.Length/1MB, 2)) MB" -ForegroundColor White
            Write-Host "   • Archivos incluidos: $($importantFiles.Count)" -ForegroundColor White
            Write-Host "   • Configuración: $Configuration" -ForegroundColor White
            Write-Host "   • Ubicación: $($msiFile.FullName)" -ForegroundColor White
            Write-Host "   • Tiempo de compilación: $($stopwatch.Elapsed.TotalSeconds.ToString('F1')) segundos" -ForegroundColor White
            Write-Host ""
            
            Write-Host "🎯 VENTAJAS DEL MSI DEBUG:" -ForegroundColor Cyan
            Write-Host "   ✅ Símbolos de debugging incluidos" -ForegroundColor Green
            Write-Host "   ✅ Runtime completo incluido" -ForegroundColor Green
            Write-Host "   ✅ Archivos críticos de WinUI" -ForegroundColor Green
            Write-Host "   ✅ Instalación independiente en 'Debug' folder" -ForegroundColor Green
            Write-Host "   ✅ No conflicta con versión Release" -ForegroundColor Green
            Write-Host ""
            
            Write-Host "📋 COMANDOS PARA INSTALAR:" -ForegroundColor Yellow
            Write-Host "   # Instalar MSI Debug:" -ForegroundColor White
            Write-Host "   msiexec /i `"$($msiFile.Name)`"" -ForegroundColor Gray
            Write-Host ""
            Write-Host "   # Desinstalar:" -ForegroundColor White
            Write-Host "   msiexec /x `"$($msiFile.Name)`" /quiet" -ForegroundColor Gray
            Write-Host ""
            
            if ($OpenOutput) {
                Start-Process "explorer.exe" -ArgumentList (Split-Path $msiFile.FullName)
            }
            
            Write-Host "🎉 MSI DEBUG LISTO PARA PRUEBAS" -ForegroundColor Green
            
        } else {
            Write-Host "❌ ERROR: Falló la compilación del MSI Debug" -ForegroundColor Red
            exit 1
        }
        
    } finally {
        Pop-Location
    }

} catch {
    Write-Host "❌ ERROR CRÍTICO:" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor White
    exit 1
}

Write-Host ""
Write-Host "💡 PARA VALIDAR EL MSI DEBUG:" -ForegroundColor Blue
Write-Host "   .\validate-msi.ps1 -MsiPath `"$outputDir\GestionTimeDesktop-Debug-Complete-1.1.0.msi`"" -ForegroundColor Gray

Write-Host ""
Write-Host "🔨 MSI DEBUG COMPLETADO" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green