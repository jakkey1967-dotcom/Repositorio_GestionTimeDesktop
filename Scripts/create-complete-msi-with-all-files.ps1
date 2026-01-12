param(
    [string]$AppPath = "bin\Release\Installer\App",
    [string]$OutputPath = "Installer\MSI\Features_Complete.wxs"
)

Write-Host ""
Write-Host "🔨 GENERADOR COMPLETO DE MSI CON TODOS LOS ARCHIVOS" -ForegroundColor Green -BackgroundColor DarkGreen
Write-Host "===============================================" -ForegroundColor Green
Write-Host ""

if (!(Test-Path $AppPath)) {
    Write-Host "❌ ERROR: Directorio de aplicación no encontrado: $AppPath" -ForegroundColor Red
    exit 1
}

# Obtener todos los archivos
$allFiles = Get-ChildItem $AppPath -File -Recurse
Write-Host "📊 Archivos encontrados: $($allFiles.Count)" -ForegroundColor Cyan

# Crear contenido WXS simplificado
$wxsContent = @'
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  
  <Fragment>
    
    <!-- Característica principal -->
    <Feature Id="MainApplication" 
             Title="GestionTime Desktop - Aplicación Completa" 
             Description="Aplicación completa de gestión de tiempo"
             Level="1" 
             ConfigurableDirectory="INSTALLFOLDER"
             AllowAdvertise="no"
             Display="expand">

      <!-- Componente principal con TODOS los archivos -->
      <ComponentRef Id="AllApplicationFiles" />
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

    <!-- Componente único con TODOS los archivos -->
    <Component Id="AllApplicationFiles" Directory="INSTALLFOLDER" Guid="AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA">
'@

# Agregar TODOS los archivos al componente único
foreach ($file in $allFiles) {
    $relativePath = $file.FullName.Replace((Get-Item $AppPath).FullName, "").TrimStart("\")
    $fileName = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
    $fileExt = $file.Extension
    
    # Crear ID único y válido para el archivo
    $fileId = ($fileName + $fileExt).Replace(".", "_").Replace("-", "_").Replace(" ", "_")
    if ($fileId -match '^\d') { $fileId = "File_" + $fileId }
    if ($fileId.Length -gt 70) { $fileId = $fileId.Substring(0, 70) }
    
    $wxsContent += "      <File Id=`"$fileId`" Source=`"..\..\bin\Release\Installer\App\$relativePath`" />`r`n"
}

# Establecer el KeyPath en el ejecutable principal
$wxsContent = $wxsContent.Replace('Source="..\..\bin\Release\Installer\App\GestionTime.Desktop.exe"', 'Source="..\..\bin\Release\Installer\App\GestionTime.Desktop.exe" KeyPath="yes"')

$wxsContent += @'
    </Component>

    <!-- Componente: Accesos directos en Menú Inicio -->
    <Component Id="StartMenuShortcuts" Directory="ApplicationProgramsFolder" Guid="66666666-6666-6666-6666-666666666666">
      <Shortcut Id="ApplicationStartMenuShortcut"
                Name="GestionTime Desktop"
                Description="Aplicación de gestión de tiempo y partes de trabajo"
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
                Description="Aplicación de gestión de tiempo y partes de trabajo"
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

# Guardar el archivo
$wxsContent | Out-File -FilePath $OutputPath -Encoding UTF8

Write-Host "✅ Features completas generadas" -ForegroundColor Green
Write-Host "   • Archivo: $OutputPath" -ForegroundColor White
Write-Host "   • Archivos incluidos: $($allFiles.Count)" -ForegroundColor White
Write-Host "   • Enfoque: Un solo componente con todos los archivos" -ForegroundColor White
Write-Host ""

Write-Host "📋 APLICANDO AUTOMÁTICAMENTE:" -ForegroundColor Yellow
Copy-Item $OutputPath "Installer\MSI\Features.wxs" -Force
Write-Host "   ✅ Features.wxs actualizado" -ForegroundColor Green

Write-Host ""
Write-Host "🔨 COMPILANDO MSI COMPLETO..." -ForegroundColor Cyan

# Compilar directamente
Push-Location "Installer\MSI"
try {
    wix build Product.wxs Features.wxs UI.wxs -out "..\..\bin\Release\MSI\GestionTimeDesktop-Complete-1.1.0.msi" -arch x64 -sw1150

    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "✅ MSI COMPLETO CREADO EXITOSAMENTE" -ForegroundColor Green -BackgroundColor DarkGreen
        
        $msiFile = Get-Item "..\..\bin\Release\MSI\GestionTimeDesktop-Complete-1.1.0.msi"
        Write-Host "📊 INFORMACIÓN DEL MSI COMPLETO:" -ForegroundColor Magenta
        Write-Host "   • Archivo: $($msiFile.Name)" -ForegroundColor White
        Write-Host "   • Tamaño: $([math]::Round($msiFile.Length/1MB, 2)) MB" -ForegroundColor White
        Write-Host "   • Archivos incluidos: $($allFiles.Count)" -ForegroundColor White
        Write-Host "   • Ubicación: $($msiFile.FullName)" -ForegroundColor White
        
        Write-Host ""
        Write-Host "🎯 COMPARACIÓN:" -ForegroundColor Cyan
        Write-Host "   • MSI básico (anterior): ~0.9 MB con 3 archivos" -ForegroundColor White
        Write-Host "   • MSI completo (nuevo): $([math]::Round($msiFile.Length/1MB, 2)) MB con $($allFiles.Count) archivos" -ForegroundColor Green
        
        Write-Host ""
        Write-Host "📋 PARA INSTALAR:" -ForegroundColor Yellow
        Write-Host "   msiexec /i `"$($msiFile.Name)`"" -ForegroundColor White
        
    } else {
        Write-Host "❌ ERROR: Falló la compilación" -ForegroundColor Red
    }
} finally {
    Pop-Location
}

Write-Host ""
Write-Host "🎉 GENERACIÓN COMPLETADA" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green