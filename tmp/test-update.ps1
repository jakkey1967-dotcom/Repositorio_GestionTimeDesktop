$ErrorActionPreference = "Continue"

Write-Host ""
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  TEST UPDATE SERVICE - DIAGNÓSTICO RÁPIDO" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# 1. Verificar versión del ejecutable
$exe = "bin\x64\Debug\net8.0-windows10.0.19041.0\GestionTime.Desktop.exe"
if (Test-Path $exe) {
    $version = (Get-Item $exe).VersionInfo
    Write-Host "✅ Versión del ejecutable:" -ForegroundColor Green
    Write-Host "   FileVersion: $($version.FileVersion)" -ForegroundColor Cyan
    Write-Host "   ProductVersion: $($version.ProductVersion)" -ForegroundColor Cyan
} else {
    Write-Host "❌ Ejecutable no encontrado: $exe" -ForegroundColor Red
    exit
}

Write-Host ""

# 2. Verificar qué hay en GitHub
Write-Host "🔍 Consultando GitHub API..." -ForegroundColor Yellow
try {
    $headers = @{ "User-Agent" = "GestionTime-Test" }
    $response = Invoke-RestMethod -Uri "https://api.github.com/repos/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases" -Method Get -Headers $headers
    
    if ($response -and $response.Count -gt 0) {
        $latest = $response[0]
        Write-Host "✅ Release más reciente en GitHub:" -ForegroundColor Green
        Write-Host "   Tag: $($latest.tag_name)" -ForegroundColor Cyan
        Write-Host "   Name: $($latest.name)" -ForegroundColor Cyan
        Write-Host "   PreRelease: $($latest.prerelease)" -ForegroundColor Cyan
        Write-Host "   Published: $($latest.published_at)" -ForegroundColor Cyan
        Write-Host "   Assets: $($latest.assets.Count)" -ForegroundColor Cyan
        
        if ($latest.assets.Count -gt 0) {
            Write-Host ""
            Write-Host "   📦 Archivos disponibles:" -ForegroundColor Yellow
            foreach ($asset in $latest.assets) {
                $sizeMB = [math]::Round($asset.size / 1MB, 2)
                Write-Host "     - $($asset.name) ($sizeMB MB)" -ForegroundColor Gray
            }
        }
    } else {
        Write-Host "⚠️ No se encontraron releases" -ForegroundColor Yellow
        exit
    }
} catch {
    Write-Host "❌ Error al consultar GitHub API:" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor Red
    exit
}

Write-Host ""

# 3. Comparación manual
Write-Host "🔢 Comparación de versiones:" -ForegroundColor Yellow
$current = $version.ProductVersion -replace '\+.*', ''  # Remover commit hash
$latest = $response[0].tag_name.TrimStart('v')

Write-Host "   📍 Versión actual:  $current" -ForegroundColor Cyan
Write-Host "   📍 Versión GitHub:  $latest" -ForegroundColor Cyan

if ($latest) {
    # Parsear versiones (remover sufijos como -beta)
    $currentParts = $current -replace '-.*', '' -split '\.'
    $latestParts = $latest -replace '-.*', '' -split '\.'
    
    $currentMajor = [int]$currentParts[0]
    $currentMinor = [int]$currentParts[1]
    $currentPatch = [int]$currentParts[2]
    
    $latestMajor = [int]$latestParts[0]
    $latestMinor = [int]$latestParts[1]
    $latestPatch = [int]$latestParts[2]
    
    Write-Host ""
    Write-Host "   🔍 Parseado:" -ForegroundColor Yellow
    Write-Host "      Actual: $currentMajor.$currentMinor.$currentPatch" -ForegroundColor Gray
    Write-Host "      GitHub: $latestMajor.$latestMinor.$latestPatch" -ForegroundColor Gray
    Write-Host ""
    
    $shouldUpdate = ($latestMajor -gt $currentMajor) -or 
                    (($latestMajor -eq $currentMajor) -and ($latestMinor -gt $currentMinor)) -or
                    (($latestMajor -eq $currentMajor) -and ($latestMinor -eq $currentMinor) -and ($latestPatch -gt $currentPatch))
    
    if ($shouldUpdate) {
        Write-Host "═══════════════════════════════════════════════" -ForegroundColor Green
        Write-Host "  ✅ DEBERÍA APARECER EL DIÁLOGO" -ForegroundColor Green
        Write-Host "═══════════════════════════════════════════════" -ForegroundColor Green
        Write-Host ""
        Write-Host "Si NO aparece, el problema es:" -ForegroundColor Yellow
        Write-Host "  1. UpdateService no se está ejecutando" -ForegroundColor Gray
        Write-Host "  2. Hay una excepción silenciosa" -ForegroundColor Gray
        Write-Host "  3. XamlRoot no está disponible" -ForegroundColor Gray
    } else {
        Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
        Write-Host "  ℹ️ NO DEBERÍA APARECER EL DIÁLOGO" -ForegroundColor Cyan
        Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "Las versiones son iguales o actual es más nueva" -ForegroundColor Gray
    }
} else {
    Write-Host "⚠️ No se puede comparar" -ForegroundColor Yellow
}

Write-Host ""
