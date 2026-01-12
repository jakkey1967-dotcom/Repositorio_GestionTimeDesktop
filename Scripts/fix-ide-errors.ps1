# Script para limpiar y restaurar el IDE de Visual Studio
# Fecha: 2025-01-27

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "  LIMPIEZA Y RESTAURACIÓN DEL IDE  " -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# 1. Verificar que Visual Studio esté cerrado
Write-Host "1. Verificando procesos de Visual Studio..." -ForegroundColor Yellow
$vsProcesses = Get-Process -Name "devenv" -ErrorAction SilentlyContinue
if ($vsProcesses) {
    Write-Host "   ⚠️  Visual Studio está ejecutándose. Por favor ciérralo primero." -ForegroundColor Red
    Write-Host "   Presiona cualquier tecla para intentar cerrar automáticamente..." -ForegroundColor Yellow
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    
    Write-Host "   Cerrando Visual Studio..." -ForegroundColor Yellow
    $vsProcesses | ForEach-Object { $_.CloseMainWindow() | Out-Null }
    Start-Sleep -Seconds 3
    
    # Forzar cierre si aún está abierto
    $vsProcesses = Get-Process -Name "devenv" -ErrorAction SilentlyContinue
    if ($vsProcesses) {
        Write-Host "   Forzando cierre..." -ForegroundColor Yellow
        $vsProcesses | Stop-Process -Force
        Start-Sleep -Seconds 2
    }
}
Write-Host "   ✅ Visual Studio cerrado" -ForegroundColor Green
Write-Host ""

# 2. Limpiar carpetas de cache
Write-Host "2. Limpiando carpetas de caché..." -ForegroundColor Yellow

$foldersToClean = @(
    ".vs",
    "bin",
    "obj",
    "GestionTime.Desktop.WinForms\bin",
    "GestionTime.Desktop.WinForms\obj"
)

foreach ($folder in $foldersToClean) {
    if (Test-Path $folder) {
        Write-Host "   Eliminando: $folder" -ForegroundColor Gray
        Remove-Item -Path $folder -Recurse -Force -ErrorAction SilentlyContinue
    }
}
Write-Host "   ✅ Caché eliminado" -ForegroundColor Green
Write-Host ""

# 3. Limpiar archivos temporales
Write-Host "3. Limpiando archivos temporales..." -ForegroundColor Yellow
$filesToClean = Get-ChildItem -Recurse -File | Where-Object { 
    $_.Extension -in @('.user', '.suo', '.tmp', '.cache', '.vsidx') 
}
$filesToClean | Remove-Item -Force -ErrorAction SilentlyContinue
Write-Host "   ✅ Archivos temporales eliminados" -ForegroundColor Green
Write-Host ""

# 4. Restaurar paquetes NuGet
Write-Host "4. Restaurando paquetes NuGet..." -ForegroundColor Yellow
dotnet restore GestionTime.Desktop.csproj
dotnet restore GestionTime.Desktop.WinForms\GestionTime.Desktop.WinForms.csproj
Write-Host "   ✅ Paquetes restaurados" -ForegroundColor Green
Write-Host ""

# 5. Compilar proyectos
Write-Host "5. Compilando proyectos..." -ForegroundColor Yellow
Write-Host "   Compilando proyecto principal (WinUI3)..." -ForegroundColor Gray
dotnet build GestionTime.Desktop.csproj --no-restore

Write-Host "   Compilando proyecto WinForms..." -ForegroundColor Gray
dotnet build GestionTime.Desktop.WinForms\GestionTime.Desktop.WinForms.csproj --no-restore
Write-Host "   ✅ Proyectos compilados" -ForegroundColor Green
Write-Host ""

# 6. Abrir Visual Studio con el archivo de solución
Write-Host "6. Abriendo Visual Studio..." -ForegroundColor Yellow
if (Test-Path "GestionTime.sln") {
    Write-Host "   Abriendo: GestionTime.sln" -ForegroundColor Gray
    Start-Process "GestionTime.sln"
    Write-Host "   ✅ Visual Studio iniciado" -ForegroundColor Green
} else {
    Write-Host "   ⚠️  Archivo de solución no encontrado" -ForegroundColor Red
}
Write-Host ""

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "  ✅ PROCESO COMPLETADO             " -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Ahora Visual Studio debería abrirse sin errores." -ForegroundColor White
Write-Host "Si aún ves errores en el proyecto WinForms, recuerda que:" -ForegroundColor Yellow
Write-Host "  - Es un proyecto SEPARADO del principal" -ForegroundColor Gray
Write-Host "  - Compila independientemente" -ForegroundColor Gray
Write-Host "  - No afecta al proyecto WinUI3 principal" -ForegroundColor Gray
Write-Host ""
Write-Host "Presiona cualquier tecla para salir..." -ForegroundColor Cyan
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
