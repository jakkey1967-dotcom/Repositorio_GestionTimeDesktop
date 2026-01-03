# ========================================
# 🎨 Script para Crear y Configurar Icono
# GestionTime Desktop
# ========================================

Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🎨 CONFIGURACIÓN DE ICONO DE APLICACIÓN" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Verificar que existe el logo
if (-not (Test-Path "Assets\app_logo.png")) {
    Write-Host "❌ Error: No se encuentra Assets\app_logo.png" -ForegroundColor Red
    Write-Host ""
    Write-Host "Por favor, coloca tu logo como app_logo.png en la carpeta Assets" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ Logo encontrado: Assets\app_logo.png" -ForegroundColor Green
Write-Host ""

# Información sobre conversión
Write-Host "📝 INSTRUCCIONES PARA CREAR EL ICONO:" -ForegroundColor Yellow
Write-Host ""
Write-Host "Para convertir el PNG a ICO, tienes 3 opciones:" -ForegroundColor White
Write-Host ""
Write-Host "🌐 OPCIÓN 1: Convertir Online (Recomendado)" -ForegroundColor Cyan
Write-Host "   1. Abre: https://convertio.co/es/png-ico/" -ForegroundColor Gray
Write-Host "   2. Sube: Assets\app_logo.png" -ForegroundColor Gray
Write-Host "   3. Descarga el .ico generado" -ForegroundColor Gray
Write-Host "   4. Guárdalo como: Assets\app.ico" -ForegroundColor Gray
Write-Host ""
Write-Host "🖥️ OPCIÓN 2: PowerShell con System.Drawing" -ForegroundColor Cyan
Write-Host "   Ejecutar: .\create-icon-from-png.ps1" -ForegroundColor Gray
Write-Host ""
Write-Host "🛠️ OPCIÓN 3: Herramienta Desktop" -ForegroundColor Cyan
Write-Host "   - GIMP (gratuito)" -ForegroundColor Gray
Write-Host "   - Paint.NET (gratuito)" -ForegroundColor Gray
Write-Host "   - IcoFX (comercial)" -ForegroundColor Gray
Write-Host ""

# Verificar si ya existe el icono
if (Test-Path "Assets\app.ico") {
    Write-Host "✅ Icono encontrado: Assets\app.ico" -ForegroundColor Green
    
    # Obtener tamaño del archivo
    $iconSize = (Get-Item "Assets\app.ico").Length / 1KB
    Write-Host "   Tamaño: $("{0:N2}" -f $iconSize) KB" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "🔧 Configurando el proyecto..." -ForegroundColor Yellow
    
    # Leer el .csproj
    $csprojPath = "GestionTime.Desktop.csproj"
    $csprojContent = Get-Content $csprojPath -Raw
    
    # Verificar si ya tiene ApplicationIcon
    if ($csprojContent -match "<ApplicationIcon>") {
        Write-Host "⚠️ El proyecto ya tiene un icono configurado" -ForegroundColor Yellow
        Write-Host "   Verificando configuración actual..." -ForegroundColor Gray
    }
    else {
        Write-Host "📝 Agregando configuración de icono al proyecto..." -ForegroundColor Yellow
        
        # Buscar el primer PropertyGroup
        if ($csprojContent -match "(<PropertyGroup>[\s\S]*?</PropertyGroup>)") {
            $firstPropertyGroup = $matches[1]
            $newPropertyGroup = $firstPropertyGroup -replace "</PropertyGroup>", @"
    
    <!-- Icono de la aplicación -->
    <ApplicationIcon>Assets\app.ico</ApplicationIcon>
  </PropertyGroup>
"@
            $csprojContent = $csprojContent -replace [regex]::Escape($firstPropertyGroup), $newPropertyGroup
            
            # Guardar cambios
            $csprojContent | Out-File -FilePath $csprojPath -Encoding UTF8 -NoNewline
            Write-Host "✅ Proyecto actualizado con referencia al icono" -ForegroundColor Green
        }
    }
    
    # Verificar que el icono esté incluido en el proyecto
    if ($csprojContent -notmatch "app\.ico") {
        Write-Host ""
        Write-Host "📝 Agregando icono a los archivos del proyecto..." -ForegroundColor Yellow
        
        # Buscar ItemGroup de Content
        if ($csprojContent -match "(<ItemGroup>[\s\S]*?<Content Include=`"Assets)") {
            $contentGroup = $matches[0]
            $newContent = $contentGroup + @"

    <Content Include="Assets\app.ico" />
"@
            $csprojContent = $csprojContent -replace [regex]::Escape($contentGroup), $newContent
            $csprojContent | Out-File -FilePath $csprojPath -Encoding UTF8 -NoNewline
            Write-Host "✅ Icono agregado a los archivos del proyecto" -ForegroundColor Green
        }
    }
    
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "✅ CONFIGURACIÓN COMPLETADA" -ForegroundColor Green
    Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "📋 PRÓXIMOS PASOS:" -ForegroundColor Yellow
    Write-Host "   1. Recompilar el proyecto (Clean + Rebuild)" -ForegroundColor White
    Write-Host "   2. El icono aparecerá en:" -ForegroundColor White
    Write-Host "      • Ejecutable (.exe)" -ForegroundColor Gray
    Write-Host "      • Barra de tareas" -ForegroundColor Gray
    Write-Host "      • Explorador de Windows" -ForegroundColor Gray
    Write-Host "      • Instalador MSI" -ForegroundColor Gray
    Write-Host ""
    Write-Host "🔨 Para recompilar:" -ForegroundColor Yellow
    Write-Host "   dotnet clean && dotnet build" -ForegroundColor Gray
    Write-Host ""
}
else {
    Write-Host "❌ No se encuentra Assets\app.ico" -ForegroundColor Red
    Write-Host ""
    Write-Host "⚠️ IMPORTANTE:" -ForegroundColor Yellow
    Write-Host "   1. Convierte Assets\app_logo.png a .ico usando una de las opciones arriba" -ForegroundColor White
    Write-Host "   2. Guarda el resultado como: Assets\app.ico" -ForegroundColor White
    Write-Host "   3. Vuelve a ejecutar este script" -ForegroundColor White
    Write-Host ""
}

Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
