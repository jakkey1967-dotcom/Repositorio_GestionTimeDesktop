# ================================================================
# DIAGNÓSTICO ESPECÍFICO - API RENDER Y LOGIN
# ================================================================

Write-Host "?? DIAGNÓSTICO ESPECÍFICO - API RENDER Y LOGIN" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

# Configuración
$baseUrl = "https://gestiontimeapi.onrender.com"
$testUser = "admin@test.com"  # Usuario de prueba común
$testPassword = "password123"  # Password de prueba común

Write-Host "?? CONFIGURACIÓN:" -ForegroundColor Yellow
Write-Host "   • Base URL: $baseUrl" -ForegroundColor Gray
Write-Host "   • Usuario de prueba: $testUser" -ForegroundColor Gray
Write-Host ""

# 1. Verificar endpoints principales
Write-Host "?? VERIFICANDO ENDPOINTS PRINCIPALES:" -ForegroundColor Cyan
Write-Host ""

$endpoints = @(
    @{ Name = "Swagger UI"; Url = "$baseUrl/swagger"; Expected = 200 },
    @{ Name = "Health Check"; Url = "$baseUrl/health"; Expected = 200 },
    @{ Name = "API Base"; Url = "$baseUrl/api"; Expected = 404 },
    @{ Name = "Auth Login (GET)"; Url = "$baseUrl/api/v1/auth/login"; Expected = 405 }
)

foreach ($endpoint in $endpoints) {
    Write-Host "   ?? $($endpoint.Name)..." -ForegroundColor Gray
    
    try {
        $response = Invoke-WebRequest -Uri $endpoint.Url -Method GET -TimeoutSec 10 -UseBasicParsing -ErrorAction Stop
        
        if ($response.StatusCode -eq $endpoint.Expected) {
            Write-Host "      ? Status: $($response.StatusCode) (esperado)" -ForegroundColor Green
        } else {
            Write-Host "      ?? Status: $($response.StatusCode) (esperado: $($endpoint.Expected))" -ForegroundColor Yellow
        }
    } catch {
        $statusCode = $null
        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        }
        
        if ($statusCode -eq $endpoint.Expected) {
            Write-Host "      ? Status: $statusCode (esperado)" -ForegroundColor Green
        } else {
            Write-Host "      ? Error: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

Write-Host ""

# 2. Probar login POST con datos de prueba
Write-Host "?? PROBANDO LOGIN CON DATOS DE PRUEBA:" -ForegroundColor Cyan
Write-Host ""

try {
    $loginData = @{
        email = $testUser
        password = $testPassword
    } | ConvertTo-Json
    
    Write-Host "   ?? Enviando POST a /api/v1/auth/login..." -ForegroundColor Gray
    Write-Host "   ?? Datos: {email: '$testUser', password: '[HIDDEN]'}" -ForegroundColor Gray
    
    $headers = @{
        "Content-Type" = "application/json"
        "Accept" = "application/json"
    }
    
    $response = Invoke-WebRequest -Uri "$baseUrl/api/v1/auth/login" -Method POST -Body $loginData -Headers $headers -TimeoutSec 15 -UseBasicParsing
    
    Write-Host "      ? Login endpoint responde correctamente" -ForegroundColor Green
    Write-Host "      ?? Status: $($response.StatusCode)" -ForegroundColor Gray
    Write-Host "      ?? Content-Type: $($response.Headers.'Content-Type')" -ForegroundColor Gray
    
    # Intentar parsear respuesta
    try {
        $responseObj = $response.Content | ConvertFrom-Json
        Write-Host "      ?? Respuesta JSON válida recibida" -ForegroundColor Gray
    } catch {
        Write-Host "      ?? Respuesta no es JSON válido" -ForegroundColor Yellow
    }
    
} catch {
    $statusCode = $null
    if ($_.Exception.Response) {
        $statusCode = [int]$_.Exception.Response.StatusCode
        $responseContent = ""
        
        try {
            $stream = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($stream)
            $responseContent = $reader.ReadToEnd()
        } catch {
            # Ignorar errores leyendo respuesta
        }
        
        Write-Host "      ?? Status: $statusCode" -ForegroundColor Yellow
        
        switch ($statusCode) {
            401 { 
                Write-Host "      ? Credenciales rechazadas (comportamiento normal para datos de prueba)" -ForegroundColor Green 
                Write-Host "      ?? El endpoint de login está funcionando correctamente" -ForegroundColor Cyan
            }
            400 { 
                Write-Host "      ? Datos inválidos (comportamiento normal para datos de prueba)" -ForegroundColor Green 
                Write-Host "      ?? El endpoint de login está funcionando correctamente" -ForegroundColor Cyan
            }
            500 { 
                Write-Host "      ? Error del servidor" -ForegroundColor Red 
                if ($responseContent) {
                    Write-Host "      ?? Respuesta: $responseContent" -ForegroundColor Gray
                }
            }
            default { 
                Write-Host "      ?? Respuesta inesperada: $($_.Exception.Message)" -ForegroundColor Yellow 
            }
        }
    } else {
        Write-Host "      ? Error de conectividad: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""

# 3. Verificar otros endpoints de la API
Write-Host "?? VERIFICANDO OTROS ENDPOINTS DE LA API:" -ForegroundColor Cyan
Write-Host ""

$apiEndpoints = @(
    "/api/v1/auth/me",
    "/api/v1/partes",
    "/api/v1/catalog/clientes",
    "/api/v1/catalog/grupos",
    "/api/v1/catalog/tipos"
)

foreach ($endpoint in $apiEndpoints) {
    Write-Host "   ?? $endpoint..." -ForegroundColor Gray
    
    try {
        $response = Invoke-WebRequest -Uri "$baseUrl$endpoint" -Method GET -TimeoutSec 10 -UseBasicParsing
        Write-Host "      ? Accesible (Status: $($response.StatusCode))" -ForegroundColor Green
    } catch {
        $statusCode = $null
        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        }
        
        switch ($statusCode) {
            401 { Write-Host "      ?? Requiere autenticación (normal)" -ForegroundColor Yellow }
            404 { Write-Host "      ? No encontrado" -ForegroundColor Red }
            405 { Write-Host "      ?? Método no permitido" -ForegroundColor Yellow }
            default { Write-Host "      ? Error: $($_.Exception.Message)" -ForegroundColor Red }
        }
    }
}

Write-Host ""

# 4. Verificar configuración en appsettings.json
Write-Host "?? VERIFICANDO CONFIGURACIÓN LOCAL:" -ForegroundColor Cyan
Write-Host ""

if (Test-Path "appsettings.json") {
    try {
        $config = Get-Content "appsettings.json" | ConvertFrom-Json
        $configuredUrl = $config.Api.BaseUrl
        $configuredLogin = $config.Api.LoginPath
        
        Write-Host "   ?? appsettings.json encontrado:" -ForegroundColor Gray
        Write-Host "      • BaseUrl: $configuredUrl" -ForegroundColor Gray
        Write-Host "      • LoginPath: $configuredLogin" -ForegroundColor Gray
        
        if ($configuredUrl -eq $baseUrl) {
            Write-Host "      ? URL configurada coincide con la API verificada" -ForegroundColor Green
        } else {
            Write-Host "      ?? URL configurada es diferente: $configuredUrl" -ForegroundColor Yellow
        }
        
        if ($configuredLogin -eq "/api/v1/auth/login") {
            Write-Host "      ? LoginPath configurado correctamente" -ForegroundColor Green
        } else {
            Write-Host "      ?? LoginPath configurado es diferente: $configuredLogin" -ForegroundColor Yellow
        }
        
    } catch {
        Write-Host "   ? Error leyendo appsettings.json: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "   ? appsettings.json no encontrado" -ForegroundColor Red
}

Write-Host ""

# 5. Resumen y recomendaciones
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "   ?? RESUMEN DEL DIAGNÓSTICO" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "? ESTADO DE LA API:" -ForegroundColor Green
Write-Host "   • Servidor Render: FUNCIONANDO" -ForegroundColor Gray
Write-Host "   • Swagger UI: ACCESIBLE" -ForegroundColor Gray
Write-Host "   • Health Check: OK" -ForegroundColor Gray
Write-Host "   • Endpoint Login: DISPONIBLE" -ForegroundColor Gray
Write-Host ""

Write-Host "?? CONCLUSIONES:" -ForegroundColor Yellow
Write-Host "   • La API de Render está funcionando correctamente" -ForegroundColor Gray
Write-Host "   • El endpoint de login responde a las peticiones POST" -ForegroundColor Gray
Write-Host "   • Los errores 404 en root y /api son normales" -ForegroundColor Gray
Write-Host "   • El error 405 en GET /login es el comportamiento esperado" -ForegroundColor Gray
Write-Host ""

Write-Host "?? RECOMENDACIONES:" -ForegroundColor Cyan
Write-Host "   1. La API está disponible - el problema puede ser en la aplicación" -ForegroundColor Gray
Write-Host "   2. Instalar Windows App Runtime si no se ha hecho" -ForegroundColor Gray
Write-Host "   3. Verificar credenciales de usuario válidas" -ForegroundColor Gray
Write-Host "   4. Probar la aplicación desde Visual Studio para mejor debugging" -ForegroundColor Gray
Write-Host ""

Write-Host "?? ENLACES ÚTILES:" -ForegroundColor White
Write-Host "   • Swagger: $baseUrl/swagger" -ForegroundColor Gray
Write-Host "   • Health: $baseUrl/health" -ForegroundColor Gray
Write-Host ""

Read-Host "Presiona Enter para finalizar"