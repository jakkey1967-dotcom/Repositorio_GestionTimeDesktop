# Script de Publicación Automática
# GestionTime Desktop - v1.0.0

Write-Host "????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "  ?? GestionTime - Publicación MSIX" -ForegroundColor Cyan
Write-Host "????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

# 1. Configuración
$projectPath = "C:\GestionTime\GestionTime.Desktop"
$outputPath = "$projectPath\AppPackages"
$publishPath = "$projectPath\publish\msix"

Write-Host "?? Configuración:" -ForegroundColor Yellow
Write-Host "   Proyecto: $projectPath"
Write-Host "   Salida: $outputPath"
Write-Host ""

# 2. Limpiar build anterior
Write-Host "?? Limpiando builds anteriores..." -ForegroundColor Yellow
Set-Location $projectPath
dotnet clean --configuration Release

# 3. Restaurar paquetes
Write-Host "?? Restaurando paquetes NuGet..." -ForegroundColor Yellow
dotnet restore

# 4. Compilar en Release
Write-Host "??? Compilando proyecto (Release)..." -ForegroundColor Yellow
dotnet build --configuration Release --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "? Error en compilación" -ForegroundColor Red
    exit 1
}

Write-Host "? Compilación exitosa" -ForegroundColor Green
Write-Host ""

# 5. Publicar MSIX
Write-Host "?? Creando paquete MSIX..." -ForegroundColor Yellow
Write-Host ""
Write-Host "?? NOTA:" -ForegroundColor Yellow
Write-Host "Para crear el paquete MSIX completo con certificado," -ForegroundColor Yellow
Write-Host "es necesario usar Visual Studio." -ForegroundColor Yellow
Write-Host ""
Write-Host "Puedes hacerlo manualmente:" -ForegroundColor Yellow
Write-Host "1. Click derecho en proyecto" -ForegroundColor White
Write-Host "2. Publish ? Create App Packages" -ForegroundColor White
Write-Host "3. Seguir wizard (5 minutos)" -ForegroundColor White
Write-Host ""

# Alternativa: Publicar como self-contained portable
Write-Host "?? Creando versión portable como alternativa..." -ForegroundColor Yellow
dotnet publish --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=false -o "$publishPath"

if ($LASTEXITCODE -eq 0) {
    Write-Host "? Versión portable creada en: $publishPath" -ForegroundColor Green
    Write-Host ""
    Write-Host "?? Archivos generados:" -ForegroundColor Cyan
    Get-ChildItem $publishPath | Select-Object Name, Length | Format-Table
    
    # Crear ZIP portable
    $zipPath = "$projectPath\GestionTime_Portable_v1.0.0.zip"
    Write-Host "??? Creando ZIP portable..." -ForegroundColor Yellow
    Compress-Archive -Path "$publishPath\*" -DestinationPath $zipPath -Force
    Write-Host "? ZIP creado: $zipPath" -ForegroundColor Green
} else {
    Write-Host "? Error en publicación" -ForegroundColor Red
}

Write-Host ""
Write-Host "????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "  ? PROCESO COMPLETADO" -ForegroundColor Cyan
Write-Host "????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""
Write-Host "?? Próximos pasos:" -ForegroundColor Yellow
Write-Host "1. Para MSIX: Usar Visual Studio (ver PASOS_PUBLICACION_CLICKS.md)" -ForegroundColor White
Write-Host "2. Para Portable: Distribuir el ZIP generado" -ForegroundColor White
Write-Host ""
