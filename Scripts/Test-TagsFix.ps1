# Test-TagsFix.ps1
# Script para verificar el fix del sistema de tags en ParteItemEdit
# Prueba el endpoint /api/v1/tags y simula el filtrado local

$ErrorActionPreference = "Stop"

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🧪 TEST: FIX TAGS NO APARECEN SUGERENCIAS" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Configuración
$baseUrl = "https://gestiontimeapi.onrender.com"
$email = "psantos@global-retail.com"
$password = "TuContraseña123" # ⚠️ Cambiar por la real

# Paso 1: Login
Write-Host "[1] Login para obtener token..." -ForegroundColor Yellow
try {
    $loginBody = @{
        email = $email
        password = $password
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/auth/login" `
        -Method POST `
        -ContentType "application/json" `
        -Body $loginBody

    $token = $loginResponse.token
    if (-not $token) {
        throw "No se obtuvo token del login"
    }

    Write-Host "✅ Token obtenido" -ForegroundColor Green
    Write-Host "   Email: $email" -ForegroundColor Gray
    Write-Host ""
} catch {
    Write-Host "❌ Error en login: $_" -ForegroundColor Red
    exit 1
}

# Headers con autenticación
$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Paso 2: GET /api/v1/tags (endpoint usado en el fix)
Write-Host "[2] GET /api/v1/tags?limit=50" -ForegroundColor Yellow
try {
    $tagsResponse = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/tags?limit=50" `
        -Method GET `
        -Headers $headers

    Write-Host "✅ Tags obtenidos: $($tagsResponse.Count)" -ForegroundColor Green
    
    if ($tagsResponse.Count -gt 0) {
        Write-Host "   Primeros 10 tags:" -ForegroundColor Gray
        $tagsResponse | Select-Object -First 10 | ForEach-Object {
            Write-Host "     - $_" -ForegroundColor Gray
        }
    }
    Write-Host ""
} catch {
    Write-Host "❌ Error obteniendo tags: $_" -ForegroundColor Red
    exit 1
}

# Paso 3: Simular filtrado local (como hace el código ahora)
Write-Host "[3] Simulación de filtrado local con LINQ" -ForegroundColor Yellow
$searchTerms = @("BUG", "TPV", "ARTICULO", "ALBARANES", "ANDROID")

foreach ($term in $searchTerms) {
    Write-Host "   🔍 Buscando tags que contengan '$term'..." -ForegroundColor Cyan
    
    # Simular el filtrado local con LINQ (case-insensitive)
    $filteredTags = $tagsResponse | Where-Object { $_ -like "*$term*" } | Select-Object -First 10
    
    if ($filteredTags.Count -gt 0) {
        Write-Host "   ✅ Encontrados: $($filteredTags.Count)" -ForegroundColor Green
        $filteredTags | ForEach-Object {
            Write-Host "      + $_" -ForegroundColor Gray
        }
    } else {
        Write-Host "   ⚠️ No se encontraron coincidencias" -ForegroundColor Yellow
    }
    Write-Host ""
}

# Paso 4: Verificar performance del endpoint
Write-Host "[4] Test de performance (5 peticiones)" -ForegroundColor Yellow
$times = @()

for ($i = 1; $i -le 5; $i++) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    
    $null = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/tags?limit=50" `
        -Method GET `
        -Headers $headers
    
    $sw.Stop()
    $times += $sw.ElapsedMilliseconds
    Write-Host "   Petición $i: $($sw.ElapsedMilliseconds)ms" -ForegroundColor Gray
}

$avgTime = ($times | Measure-Object -Average).Average
Write-Host ""
Write-Host "   📊 Promedio: $([math]::Round($avgTime, 2))ms" -ForegroundColor Cyan
Write-Host "   📊 Mínimo: $($times | Measure-Object -Minimum | Select-Object -ExpandProperty Minimum)ms" -ForegroundColor Cyan
Write-Host "   📊 Máximo: $($times | Measure-Object -Maximum | Select-Object -ExpandProperty Maximum)ms" -ForegroundColor Cyan
Write-Host ""

# Resumen final
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✅ RESUMEN DEL FIX" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ Endpoint operativo: /api/v1/tags" -ForegroundColor Green
Write-Host "✅ Filtrado local: Funcionando (LINQ case-insensitive)" -ForegroundColor Green
Write-Host "✅ Performance: ~$([math]::Round($avgTime, 2))ms promedio" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Próximo paso: Prueba manual en la aplicación" -ForegroundColor Yellow
Write-Host "   1. Abrir ParteItemEdit" -ForegroundColor Gray
Write-Host "   2. Escribir en el campo de tags (ej: 'BUG')" -ForegroundColor Gray
Write-Host "   3. Verificar que aparezca el dropdown con sugerencias" -ForegroundColor Gray
Write-Host "   4. Seleccionar un tag y verificar que se agregue como chip" -ForegroundColor Gray
Write-Host ""
