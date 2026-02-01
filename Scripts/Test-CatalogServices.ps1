# ===============================================
# Test de Servicios de Catálogo (Desktop)
# ===============================================
# Verifica que los servicios de Clientes, Tipos y Grupos
# están correctamente implementados en el Desktop
# ===============================================

Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🧪 TEST DE SERVICIOS DE CATÁLOGO - Desktop" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

$desktopPath = "C:\GestionTime\GestionTimeDesktop"

if (-not (Test-Path $desktopPath)) {
    Write-Host "❌ Desktop NO encontrado en: $desktopPath" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Desktop encontrado" -ForegroundColor Green
Write-Host ""

# Verificar DTOs
Write-Host "📦 VERIFICANDO DTOs..." -ForegroundColor Yellow
$dtos = @(
    "Models\Dtos\Catalog\ClienteDto.cs",
    "Models\Dtos\Catalog\TipoDto.cs",
    "Models\Dtos\Catalog\GrupoDto.cs",
    "Models\Dtos\Catalog\PagedResponse.cs"
)

$dtosOk = $true
foreach ($dto in $dtos) {
    $fullPath = Join-Path $desktopPath $dto
    if (Test-Path $fullPath) {
        Write-Host "   ✅ $dto" -ForegroundColor Green
    } else {
        Write-Host "   ❌ $dto NO encontrado" -ForegroundColor Red
        $dtosOk = $false
    }
}

Write-Host ""

# Verificar Servicios
Write-Host "🔧 VERIFICANDO SERVICIOS..." -ForegroundColor Yellow
$servicios = @(
    "Services\Catalog\ClientesService.cs",
    "Services\Catalog\TiposService.cs",
    "Services\Catalog\GruposService.cs"
)

$serviciosOk = $true
foreach ($servicio in $servicios) {
    $fullPath = Join-Path $desktopPath $servicio
    if (Test-Path $fullPath) {
        Write-Host "   ✅ $servicio" -ForegroundColor Green
    } else {
        Write-Host "   ❌ $servicio NO encontrado" -ForegroundColor Red
        $serviciosOk = $false
    }
}

Write-Host ""

# Verificar App.xaml.cs
Write-Host "📄 VERIFICANDO REGISTROS EN App.xaml.cs..." -ForegroundColor Yellow
$appXaml = Join-Path $desktopPath "App.xaml.cs"

if (Test-Path $appXaml) {
    $content = Get-Content $appXaml -Raw
    
    $checks = @{
        "ClientesService { get; private set; }" = "Propiedad ClientesService"
        "TiposService { get; private set; }" = "Propiedad TiposService"
        "GruposService { get; private set; }" = "Propiedad GruposService"
        "new Services.Catalog.ClientesService" = "Inicialización ClientesService"
        "new Services.Catalog.TiposService" = "Inicialización TiposService"
        "new Services.Catalog.GruposService" = "Inicialización GruposService"
    }
    
    $appOk = $true
    foreach ($check in $checks.GetEnumerator()) {
        if ($content -match [regex]::Escape($check.Key)) {
            Write-Host "   ✅ $($check.Value)" -ForegroundColor Green
        } else {
            Write-Host "   ❌ $($check.Value) NO encontrado" -ForegroundColor Red
            $appOk = $false
        }
    }
} else {
    Write-Host "   ❌ App.xaml.cs NO encontrado" -ForegroundColor Red
    $appOk = $false
}

Write-Host ""

# Compilación
Write-Host "🔨 VERIFICANDO COMPILACIÓN..." -ForegroundColor Yellow
cd $desktopPath
$buildOutput = dotnet build --no-restore 2>&1
$buildSuccess = $LASTEXITCODE -eq 0

if ($buildSuccess) {
    Write-Host "   ✅ Desktop compila correctamente" -ForegroundColor Green
} else {
    Write-Host "   ❌ Error de compilación" -ForegroundColor Red
    Write-Host ""
    Write-Host "ERRORES:" -ForegroundColor Red
    Write-Host $buildOutput -ForegroundColor Gray
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "📊 RESUMEN" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

if ($dtosOk) {
    Write-Host "   ✅ DTOs correctos" -ForegroundColor Green
} else {
    Write-Host "   ❌ DTOs con problemas" -ForegroundColor Red
}

if ($serviciosOk) {
    Write-Host "   ✅ Servicios correctos" -ForegroundColor Green
} else {
    Write-Host "   ❌ Servicios con problemas" -ForegroundColor Red
}

if ($appOk) {
    Write-Host "   ✅ App.xaml.cs configurado" -ForegroundColor Green
} else {
    Write-Host "   ❌ App.xaml.cs con problemas" -ForegroundColor Red
}

if ($buildSuccess) {
    Write-Host "   ✅ Compilación exitosa" -ForegroundColor Green
} else {
    Write-Host "   ❌ Compilación fallida" -ForegroundColor Red
}

Write-Host ""

if ($dtosOk -and $serviciosOk -and $appOk -and $buildSuccess) {
    Write-Host "════════════════════════════════════════════════" -ForegroundColor Green
    Write-Host "✅ INTEGRACIÓN COMPLETA Y FUNCIONAL" -ForegroundColor Green
    Write-Host "════════════════════════════════════════════════" -ForegroundColor Green
    Write-Host ""
    Write-Host "📝 USO EN CÓDIGO:" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "// Listar clientes paginados" -ForegroundColor Gray
    Write-Host "var clientes = await App.ClientesService.ListAsync(page: 1, pageSize: 50);" -ForegroundColor White
    Write-Host ""
    Write-Host "// Crear cliente" -ForegroundColor Gray
    Write-Host "var request = new ClienteCreateRequest { Nombre = ""Test"" };" -ForegroundColor White
    Write-Host "var cliente = await App.ClientesService.CreateAsync(request);" -ForegroundColor White
    Write-Host ""
    Write-Host "// Actualizar cliente" -ForegroundColor Gray
    Write-Host "var updateReq = new ClienteUpdateRequest { Nombre = ""Nuevo Nombre"" };" -ForegroundColor White
    Write-Host "await App.ClientesService.UpdateAsync(id, updateReq);" -ForegroundColor White
    Write-Host ""
    Write-Host "// Eliminar cliente" -ForegroundColor Gray
    Write-Host "await App.ClientesService.DeleteAsync(id);" -ForegroundColor White
    Write-Host ""
} else {
    Write-Host "════════════════════════════════════════════════" -ForegroundColor Red
    Write-Host "❌ INTEGRACIÓN INCOMPLETA" -ForegroundColor Red
    Write-Host "════════════════════════════════════════════════" -ForegroundColor Red
    Write-Host ""
    Write-Host "Revisa los errores arriba y corrige los problemas." -ForegroundColor Yellow
}

Write-Host ""
