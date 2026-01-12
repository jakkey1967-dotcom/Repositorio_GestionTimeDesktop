param(
    [string]$AppPath = "bin\Release\Installer\App",
    [string]$OutputPath = "Installer\MSI\Features_Generated.wxs"
)

Write-Host ""
Write-Host "🔨 GENERADOR AUTOMÁTICO DE COMPONENTES WiX" -ForegroundColor Green -BackgroundColor DarkGreen
Write-Host "===============================================" -ForegroundColor Green
Write-Host ""

if (!(Test-Path $AppPath)) {
    Write-Host "❌ ERROR: Directorio de aplicación no encontrado: $AppPath" -ForegroundColor Red
    Write-Host "   Ejecutar primero: .\build-for-installer.ps1 -Clean" -ForegroundColor Yellow
    exit 1
}

# Obtener todos los archivos
$allFiles = Get-ChildItem $AppPath -File -Recurse
Write-Host "📊 Archivos encontrados: $($allFiles.Count)" -ForegroundColor Cyan

# Crear el contenido WXS
$wxsContent = @'
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  
  <Fragment>
    
    <!-- Característica principal completa -->
    <Feature Id="MainApplication" 
             Title="GestionTime Desktop - Aplicación Completa" 
             Description="Aplicación completa de gestión de tiempo con todos los componentes"
             Level="1" 
             ConfigurableDirectory="INSTALLFOLDER"
             AllowAdvertise="no"
             Display="expand">

'@

# Agregar referencias a todos los componentes
$componentRefs = @()

# Componente principal (ejecutable)
$wxsContent += @"
      <!-- Componente ejecutable principal -->
      <ComponentRef Id="MainExecutable" />
      <ComponentRef Id="StartMenuShortcuts" />

"@
$componentRefs += "MainExecutable"

# Generar componentes para cada archivo
$componentId = 1
$guidCounter = [System.Collections.Generic.Dictionary[string,string]]::new()

foreach ($file in $allFiles) {
    $relativePath = $file.FullName.Replace((Get-Item $AppPath).FullName, "").TrimStart("\")
    $fileName = $file.Name
    $fileExtension = $file.Extension.ToLower()
    
    # Agrupar archivos por tipo para evitar demasiados componentes
    $componentType = switch ($fileExtension) {
        ".dll" { "DLL" }
        ".exe" { "EXE" }
        ".json" { "CONFIG" }
        ".xml" { "CONFIG" }
        ".config" { "CONFIG" }
        ".pri" { "RESOURCE" }
        ".xbf" { "RESOURCE" }
        ".pdb" { "DEBUG" }
        default { "MISC" }
    }
    
    # Crear GUID único para cada archivo
    if (!$guidCounter.ContainsKey($componentType + $componentId)) {
        $guidCounter[$componentType + $componentId] = [System.Guid]::NewGuid().ToString().ToUpper()
    }
    
    # Agregar referencia del componente (excepto el ejecutable principal que ya está)
    if ($fileName -ne "GestionTime.Desktop.exe") {
        $compRef = "${componentType}Component$componentId"
        $wxsContent += "      <ComponentRef Id=`"$compRef`" />`r`n"
        $componentRefs += $compRef
    }
    
    $componentId++
}

# Característica opcional: Acceso directo en escritorio
$wxsContent += @'

    </Feature>

    <!-- Característica opcional: Acceso directo en escritorio -->
    <Feature Id="DesktopShortcut" 
             Title="Acceso Directo en Escritorio"
             Description="Crear acceso directo en el escritorio" 
             Level="1"
             AllowAdvertise="no">
      <ComponentRef Id="DesktopShortcutComponent" />
    </Feature>

'@

# Generar componentes reales
$wxsContent += @'
    <!-- Componente: Ejecutable principal -->
    <Component Id="MainExecutable" Directory="INSTALLFOLDER" Guid="11111111-1111-1111-1111-111111111111">
      <File Id="GestionTimeDesktopExe" 
            Source="..\..\bin\Release\Installer\App\GestionTime.Desktop.exe" 
            KeyPath="yes" />
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

'@

# Ahora generar componentes para todos los demás archivos
$componentId = 1
foreach ($file in $allFiles) {
    $relativePath = $file.FullName.Replace((Get-Item $AppPath).FullName, "").TrimStart("\")
    $fileName = $file.Name
    $fileExtension = $file.Extension.ToLower()
    
    # Saltar el ejecutable principal (ya está definido)
    if ($fileName -eq "GestionTime.Desktop.exe") {
        continue
    }
    
    $componentType = switch ($fileExtension) {
        ".dll" { "DLL" }
        ".exe" { "EXE" }
        ".json" { "CONFIG" }
        ".xml" { "CONFIG" }
        ".config" { "CONFIG" }
        ".pri" { "RESOURCE" }
        ".xbf" { "RESOURCE" }
        ".pdb" { "DEBUG" }
        default { "MISC" }
    }
    
    $compId = "${componentType}Component$componentId"
    $fileId = "${componentType}File$componentId"
    $guid = $guidCounter[$componentType + $componentId]
    
    # Determinar directorio (para archivos en subdirectorios)
    $directory = "INSTALLFOLDER"
    if ($relativePath.Contains("\")) {
        # Para simplificar, todos van a INSTALLFOLDER por ahora
        # En una implementación más compleja, crearías subdirectorios
    }
    
    $wxsContent += @"
    <!-- Componente: $fileName -->
    <Component Id="$compId" Directory="$directory" Guid="$guid">
      <File Id="$fileId" 
            Source="..\..\bin\Release\Installer\App\$relativePath" 
            KeyPath="yes" />
    </Component>

"@
    
    $componentId++
}

$wxsContent += @'
  </Fragment>
</Wix>
'@

# Guardar el archivo
$wxsContent | Out-File -FilePath $OutputPath -Encoding UTF8

Write-Host "✅ Features generadas automáticamente" -ForegroundColor Green
Write-Host "   • Archivo: $OutputPath" -ForegroundColor White
Write-Host "   • Componentes generados: $($allFiles.Count)" -ForegroundColor White
Write-Host "   • Tamaño del archivo: $([math]::Round((Get-Item $OutputPath).Length/1KB, 1)) KB" -ForegroundColor White
Write-Host ""

Write-Host "📋 SIGUIENTES PASOS:" -ForegroundColor Yellow
Write-Host "   1. Revisar el archivo generado: $OutputPath" -ForegroundColor White
Write-Host "   2. Reemplazar Features.wxs con Features_Generated.wxs" -ForegroundColor White
Write-Host "   3. Compilar MSI: .\build-msi-complete.ps1" -ForegroundColor White
Write-Host ""

Write-Host "🎯 ESTADÍSTICAS:" -ForegroundColor Magenta
$filesByType = $allFiles | Group-Object Extension | Sort-Object Count -Descending
foreach ($group in $filesByType) {
    Write-Host "   • $($group.Name): $($group.Count) archivos" -ForegroundColor White
}

Write-Host ""
Write-Host "🔨 GENERACIÓN COMPLETADA" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green