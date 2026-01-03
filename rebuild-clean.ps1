# Script para limpiar y reconstruir el proyecto sin errores
# Fecha: 2025-01-27
# Solución al error DEP1560 (AppxManifest.xml faltante)

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "  LIMPIEZA Y RECONSTRUCCIÓN        " -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# 1. Cerrar Visual Studio si está abierto
Write-Host "1. Verificando procesos de Visual Studio..." -ForegroundColor Yellow
$vsProcesses = Get-Process -Name "devenv" -ErrorAction SilentlyContinue
if ($vsProcesses) {
    Write-Host "   Cerrando Visual Studio..." -ForegroundColor Yellow
    $vsProcesses | ForEach-Object { $_.CloseMainWindow() | Out-Null }
    Start-Sleep -Seconds 3
    
    $vsProcesses = Get-Process -Name "devenv" -ErrorAction SilentlyContinue
    if ($vsProcesses) {
        $vsProcesses | Stop-Process -Force
        Start-Sleep -Seconds 2
    }
}
Write-Host "   ✅ Visual Studio cerrado" -ForegroundColor Green
Write-Host ""

# 2. Eliminar carpetas de salida
Write-Host "2. Limpiando carpetas de salida..." -ForegroundColor Yellow

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

# Limpiar archivos temporales específicos
Write-Host "   Limpiando archivos temporales..." -ForegroundColor Gray
Get-ChildItem -Recurse -File | Where-Object { 
    $_.Extension -in @('.user', '.suo', '.tmp', '.cache', '.vsidx', '.v2') 
} | Remove-Item -Force -ErrorAction SilentlyContinue

Write-Host "   ✅ Carpetas limpias" -ForegroundColor Green
Write-Host ""

# 3. Configurar perfil de depuración
Write-Host "3. Configurando perfil de depuración..." -ForegroundColor Yellow

# Crear archivo .user con configuración correcta
$userFileContent = @"
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <ActiveDebugProfile>GestionTime.Desktop (Unpackaged)</ActiveDebugProfile>
  </PropertyGroup>
  <PropertyGroup Condition="'`$(Configuration)|`$(Platform)'=='Debug|x64'">
    <DebuggerFlavor>ProjectDebugger</DebuggerFlavor>
  </PropertyGroup>
</Project>
"@

$userFileContent | Out-File -FilePath "GestionTime.Desktop.csproj.user" -Encoding utf8 -Force
Write-Host "   ✅ Perfil de depuración configurado (Unpackaged)" -ForegroundColor Green
Write-Host ""

# 4. Limpiar con dotnet
Write-Host "4. Limpieza con dotnet clean..." -ForegroundColor Yellow
dotnet clean GestionTime.Desktop.csproj -v minimal 2>&1 | Out-Null
dotnet clean GestionTime.Desktop.WinForms\GestionTime.Desktop.WinForms.csproj -v minimal 2>&1 | Out-Null
Write-Host "   ✅ Limpieza de dotnet completada" -ForegroundColor Green
Write-Host ""

# 5. Restaurar paquetes NuGet
Write-Host "5. Restaurando paquetes NuGet..." -ForegroundColor Yellow
dotnet restore GestionTime.Desktop.csproj --force
Write-Host "   ✅ Paquetes restaurados" -ForegroundColor Green
Write-Host ""

# 6. Compilar proyecto
Write-Host "6. Compilando proyecto principal..." -ForegroundColor Yellow
$buildResult = dotnet build GestionTime.Desktop.csproj --no-restore -v minimal 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ Compilación exitosa" -ForegroundColor Green
} else {
    Write-Host "   ❌ Error en la compilación" -ForegroundColor Red
    Write-Host ""
    Write-Host "Detalles del error:" -ForegroundColor Yellow
    Write-Host $buildResult -ForegroundColor Gray
    Write-Host ""
    Write-Host "Presiona cualquier tecla para continuar..." -ForegroundColor Cyan
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 1
}
Write-Host ""

# 7. Verificar archivos generados
Write-Host "7. Verificando archivos generados..." -ForegroundColor Yellow
$outputPath = "bin\x64\Debug\net8.0-windows10.0.19041.0"

if (Test-Path $outputPath) {
    $exePath = Join-Path $outputPath "GestionTime.Desktop.exe"
    if (Test-Path $exePath) {
        Write-Host "   ✅ Ejecutable encontrado: $exePath" -ForegroundColor Green
        
        # Verificar DLLs necesarias
        $dllsRequeridas = @(
            "Microsoft.WindowsAppRuntime.Bootstrap.dll",
            "Microsoft.WindowsAppRuntime.dll",
            "Microsoft.Windows.ApplicationModel.DynamicDependency.dll"
        )
        
        $dllsFaltantes = @()
        foreach ($dll in $dllsRequeridas) {
            $dllPath = Join-Path $outputPath $dll
            if (!(Test-Path $dllPath)) {
                $dllsFaltantes += $dll
            }
        }
        
        if ($dllsFaltantes.Count -eq 0) {
            Write-Host "   ✅ Todas las DLLs necesarias están presentes" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️  DLLs faltantes:" -ForegroundColor Yellow
            foreach ($dll in $dllsFaltantes) {
                Write-Host "      - $dll" -ForegroundColor Gray
            }
        }
    } else {
        Write-Host "   ⚠️  Ejecutable no encontrado" -ForegroundColor Yellow
    }
} else {
    Write-Host "   ⚠️  Carpeta de salida no encontrada: $outputPath" -ForegroundColor Yellow
}
Write-Host ""

# 8. Mostrar información
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "  INFORMACIÓN DEL PROYECTO         " -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Configuración:" -ForegroundColor White
Write-Host "  • EnableMsixTooling: false (sin empaquetado MSIX)" -ForegroundColor Gray
Write-Host "  • WindowsPackageType: None (aplicación tradicional)" -ForegroundColor Gray
Write-Host "  • OutputType: WinExe (aplicación de escritorio)" -ForegroundColor Gray
Write-Host "  • Perfil de depuración: Unpackaged (sin paquete)" -ForegroundColor Gray
Write-Host ""
Write-Host "Ruta de salida:" -ForegroundColor White
Write-Host "  $outputPath" -ForegroundColor Gray
Write-Host ""
Write-Host "Para ejecutar la aplicación:" -ForegroundColor White
Write-Host "  .\$outputPath\GestionTime.Desktop.exe" -ForegroundColor Cyan
Write-Host ""
Write-Host "Para depurar en Visual Studio:" -ForegroundColor White
Write-Host "  1. Abrir GestionTime.sln" -ForegroundColor Gray
Write-Host "  2. Presionar F5 (el perfil Unpackaged se usa automáticamente)" -ForegroundColor Gray
Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "  ✅ PROCESO COMPLETADO             " -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# 9. Preguntar si desea abrir Visual Studio
Write-Host "¿Deseas abrir Visual Studio? (S/N): " -NoNewline -ForegroundColor Yellow
$respuesta = Read-Host

if ($respuesta -eq "S" -or $respuesta -eq "s") {
    Write-Host "Abriendo Visual Studio..." -ForegroundColor Yellow
    if (Test-Path "GestionTime.sln") {
        Start-Process "GestionTime.sln"
        Write-Host "✅ Visual Studio iniciado" -ForegroundColor Green
        Write-Host ""
        Write-Host "📝 Nota: El perfil de depuración 'Unpackaged' está configurado por defecto." -ForegroundColor Cyan
        Write-Host "   Simplemente presiona F5 para ejecutar sin errores." -ForegroundColor Cyan
    } else {
        Write-Host "⚠️  Archivo de solución no encontrado" -ForegroundColor Red
    }
} else {
    Write-Host "✅ Listo para trabajar" -ForegroundColor Green
}

Write-Host ""
Write-Host "Presiona cualquier tecla para salir..." -ForegroundColor Cyan
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
