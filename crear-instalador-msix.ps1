# Script para crear instalador MSIX de GestionTime Desktop
# Ejecutar desde Visual Studio Developer PowerShell

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "   CREADOR DE INSTALADOR MSIX - GESTIONTIME DESKTOP" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

$projectPath = "C:\GestionTime\GestionTime.Desktop"
$projectFile = "$projectPath\GestionTime.Desktop.csproj"

# Verificar que estamos en el directorio correcto
if (-not (Test-Path $projectFile)) {
    Write-Host "? Error: No se encontró el proyecto en $projectFile" -ForegroundColor Red
    Write-Host "Asegúrate de estar en el directorio correcto" -ForegroundColor Yellow
    exit 1
}

Set-Location $projectPath

Write-Host "? Proyecto encontrado: $projectFile" -ForegroundColor Green
Write-Host ""

# Limpiar builds anteriores
Write-Host "?? Limpiando builds anteriores..." -ForegroundColor Cyan
try {
    dotnet clean --configuration Release
    if (Test-Path "AppPackages") {
        Remove-Item "AppPackages" -Recurse -Force
    }
    if (Test-Path "bin\x64\Release") {
        Remove-Item "bin\x64\Release" -Recurse -Force
    }
    Write-Host "? Limpieza completada" -ForegroundColor Green
} catch {
    Write-Host "??  Error en limpieza: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""

# Restaurar dependencias
Write-Host "?? Restaurando dependencias..." -ForegroundColor Cyan
try {
    dotnet restore
    Write-Host "? Dependencias restauradas" -ForegroundColor Green
} catch {
    Write-Host "? Error restaurando dependencias: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Build del proyecto
Write-Host "?? Compilando proyecto..." -ForegroundColor Cyan
try {
    dotnet build --configuration Release --runtime win-x64 --self-contained false
    Write-Host "? Compilación exitosa" -ForegroundColor Green
} catch {
    Write-Host "? Error en compilación: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Crear el paquete MSIX usando MSBuild
Write-Host "?? Creando paquete MSIX..." -ForegroundColor Cyan
Write-Host "NOTA: Esto puede tomar varios minutos..." -ForegroundColor Yellow
Write-Host ""

try {
    # Crear paquete MSIX
    $msbuildArgs = @(
        $projectFile
        "/p:Configuration=Release"
        "/p:Platform=x64" 
        "/p:AppxBundlePlatforms=x64"
        "/p:AppxPackageDir=AppPackages\"
        "/p:AppxBundle=Never"
        "/p:UapAppxPackageBuildMode=StoreUpload"
        "/p:GenerateAppxPackageOnBuild=true"
        "/verbosity:minimal"
    )
    
    Write-Host "Ejecutando: msbuild $($msbuildArgs -join ' ')" -ForegroundColor Gray
    
    & msbuild $msbuildArgs
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? Paquete MSIX creado exitosamente" -ForegroundColor Green
    } else {
        throw "MSBuild falló con código $LASTEXITCODE"
    }
    
} catch {
    Write-Host "? Error creando paquete MSIX: $($_.Exception.Message)" -ForegroundColor Red
    
    Write-Host ""
    Write-Host "?? ALTERNATIVA - Crear usando Visual Studio:" -ForegroundColor Yellow
    Write-Host "1. Abrir Visual Studio 2022" -ForegroundColor White
    Write-Host "2. Click derecho en proyecto 'GestionTime.Desktop'" -ForegroundColor White
    Write-Host "3. Publish ? Create App Packages" -ForegroundColor White
    Write-Host "4. Seleccionar 'Sideloading'" -ForegroundColor White
    Write-Host "5. Arquitectura: x64" -ForegroundColor White
    Write-Host "6. Version: 1.1.0.0" -ForegroundColor White
    Write-Host "7. Create certificate o usar existente" -ForegroundColor White
    Write-Host "8. Create" -ForegroundColor White
    
    Read-Host "Presiona Enter para continuar"
    exit 1
}

Write-Host ""

# Verificar que el paquete se creó
$packageDir = Get-ChildItem "AppPackages" -Directory | Select-Object -First 1

if ($packageDir) {
    Write-Host "?? Paquete creado en: $($packageDir.FullName)" -ForegroundColor Green
    
    # Buscar archivos importantes
    $msixFile = Get-ChildItem $packageDir.FullName -Filter "*.msix" | Select-Object -First 1
    $installScript = Get-ChildItem $packageDir.FullName -Filter "Install.ps1" | Select-Object -First 1
    $certFile = Get-ChildItem $packageDir.FullName -Filter "*.cer" | Select-Object -First 1
    
    Write-Host ""
    Write-Host "?? ARCHIVOS CREADOS:" -ForegroundColor Cyan
    
    if ($msixFile) {
        Write-Host "? Instalador: $($msixFile.Name)" -ForegroundColor Green
        Write-Host "   Tamaño: $([math]::Round($msixFile.Length/1MB, 2)) MB" -ForegroundColor White
    }
    
    if ($installScript) {
        Write-Host "? Script de instalación: $($installScript.Name)" -ForegroundColor Green
    }
    
    if ($certFile) {
        Write-Host "? Certificado: $($certFile.Name)" -ForegroundColor Green
    }
    
    # Copiar a ubicación final
    $finalInstaller = "GestionTimeDesktop_install.msix"
    if ($msixFile) {
        Copy-Item $msixFile.FullName $finalInstaller
        Write-Host ""
        Write-Host "? Instalador final creado: $finalInstaller" -ForegroundColor Green
        Write-Host "   Ubicación: $(Get-Location)\$finalInstaller" -ForegroundColor White
    }
    
    Write-Host ""
    Write-Host "================================================================" -ForegroundColor Cyan
    Write-Host "   INSTALADOR MSIX CREADO EXITOSAMENTE" -ForegroundColor Cyan
    Write-Host "================================================================" -ForegroundColor Cyan
    Write-Host ""
    
    Write-Host "?? INSTRUCCIONES PARA DISTRIBUCIÓN:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Para usuarios finales:" -ForegroundColor White
    Write-Host "1. Enviar archivo: $finalInstaller" -ForegroundColor White
    Write-Host "2. Usuario hace doble-click para instalar" -ForegroundColor White
    Write-Host "3. Aceptar certificado si aparece" -ForegroundColor White
    Write-Host "4. La aplicación se instala automáticamente con todas las dependencias" -ForegroundColor White
    Write-Host "5. Buscar 'GestionTime Desktop' en menú inicio" -ForegroundColor White
    Write-Host ""
    
    Write-Host "? VENTAJAS DE ESTE INSTALADOR:" -ForegroundColor Green
    Write-Host "• Incluye Windows App Runtime automáticamente" -ForegroundColor White
    Write-Host "• Instalación limpia y segura" -ForegroundColor White
    Write-Host "• Actualizaciones automáticas en el futuro" -ForegroundColor White
    Write-Host "• Desinstalación completa desde Configuración" -ForegroundColor White
    Write-Host "• Sin necesidad de scripts BAT complicados" -ForegroundColor White
    
} else {
    Write-Host "? No se encontró el directorio de paquete en AppPackages" -ForegroundColor Red
    Write-Host "Revisa si hubo errores en el proceso de creación" -ForegroundColor Yellow
}

Write-Host ""
Read-Host "Presiona Enter para cerrar"