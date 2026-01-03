# 🎨 GUÍA: Cómo Cambiar el Icono de GestionTime Desktop

## ❌ Problema Actual
La aplicación muestra un cuadrado genérico en lugar del logo personalizado.

## ✅ Solución Paso a Paso

### **Paso 1: Convertir Logo a ICO (Formato Correcto)**

El archivo `.ico` debe tener un formato especial que PowerShell no puede crear correctamente.
**Usa una herramienta online profesional:**

#### 🌐 **Opción A: Convertio (Recomendado)**
1. Abre: https://convertio.co/es/png-ico/
2. Haz clic en "Elegir archivos"
3. Selecciona: `Assets\app_logo.png`
4. Asegúrate de seleccionar múltiples tamaños (16x16, 32x32, 48x48, 256x256)
5. Haz clic en "Convertir"
6. Descarga el archivo `.ico` generado
7. **Guárdalo como:** `Assets\app.ico` (reemplazar si existe)

#### 🖼️ **Opción B: ICO Convert**
1. Abre: https://icoconvert.com/
2. Sube `Assets\app_logo.png`
3. Selecciona los tamaños:
   - ☑️ 16x16
   - ☑️ 32x32
   - ☑️ 48x48
   - ☑️ 256x256
4. Haz clic en "Convert ICO"
5. Descarga y guarda como `Assets\app.ico`

### **Paso 2: Verificar el Archivo ICO**

```powershell
# Verificar que el archivo existe y tiene un tamaño razonable
Get-Item Assets\app.ico | Select-Object Name, Length, LastWriteTime
```

**Tamaño esperado:** Entre 15 KB y 150 KB

### **Paso 3: Configurar el Proyecto**

Edita `GestionTime.Desktop.csproj` y agrega estas líneas:

```xml
<PropertyGroup>
  <!-- ...otras propiedades... -->
  
  <!-- 🎨 Icono de la aplicación -->
  <ApplicationIcon>Assets\app.ico</ApplicationIcon>
  
  <!-- ...más propiedades... -->
</PropertyGroup>
```

También agregar en el `<ItemGroup>` de Content:

```xml
<ItemGroup>
  <Content Include="Assets\app.ico" />
  <Content Include="Assets\SplashScreen.scale-200.png" />
  <!-- ...otros archivos... -->
</ItemGroup>
```

### **Paso 4: Recompilar**

```powershell
# Limpiar builds anteriores
dotnet clean

# Compilar con el nuevo icono
dotnet build -c Release -p:Platform=x64
```

### **Paso 5: Verificar**

El icono debe aparecer en:
- ✅ `bin\Release\...\GestionTime.Desktop.exe` (click derecho → Propiedades → Icono)
- ✅ Barra de tareas al ejecutar
- ✅ Explorador de Windows
- ✅ Instalador MSI (si lo regeneras)

## 🚀 Script Automatizado (Una Vez que Tengas el ICO)

```powershell
# Archivo: configure-icon.ps1
param(
    [string]$IconPath = "Assets\app.ico"
)

if (-not (Test-Path $IconPath)) {
    Write-Host "❌ Error: No se encuentra $IconPath" -ForegroundColor Red
    Write-Host ""
    Write-Host "Por favor, convierte tu logo a ICO usando:" -ForegroundColor Yellow
    Write-Host "https://convertio.co/es/png-ico/" -ForegroundColor Cyan
    exit 1
}

Write-Host "✅ Icono encontrado: $IconPath" -ForegroundColor Green
Write-Host ""

# Verificar tamaño
$size = (Get-Item $IconPath).Length / 1KB
Write-Host "💾 Tamaño: $("{0:N2}" -f $size) KB" -ForegroundColor Gray

if ($size -lt 5 -or $size -gt 500) {
    Write-Host "⚠️ ADVERTENCIA: Tamaño inusual para un ICO" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "🔧 Configurando proyecto..." -ForegroundColor Yellow

# Leer csproj
$csproj = Get-Content "GestionTime.Desktop.csproj" -Raw

# Agregar ApplicationIcon si no existe
if ($csproj -notmatch "<ApplicationIcon>") {
    Write-Host "   • Agregando ApplicationIcon..." -ForegroundColor Gray
    $csproj = $csproj -replace "(<Description>.*?</Description>)", "`$1`r`n    `r`n    <!-- Icono de la aplicación -->`r`n    <ApplicationIcon>$IconPath</ApplicationIcon>"
}

# Agregar Content si no existe
if ($csproj -notmatch 'Include="Assets\\app\.ico"') {
    Write-Host "   • Agregando icono al proyecto..." -ForegroundColor Gray
    $csproj = $csproj -replace '(<Content Include="Assets\\SplashScreen.*?>)', "<Content Include=`"$IconPath`" />`r`n    `$1"
}

# Guardar cambios
$csproj | Out-File "GestionTime.Desktop.csproj" -Encoding UTF8 -NoNewline
Write-Host "✅ Proyecto configurado" -ForegroundColor Green
Write-Host ""

# Recompilar
Write-Host "🔨 Recompilando..." -ForegroundColor Yellow
dotnet clean | Out-Null
dotnet build -c Release -p:Platform=x64

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✅ ICONO CONFIGURADO CORRECTAMENTE" -ForegroundColor Green
    Write-Host ""
    Write-Host "El icono ahora aparece en el ejecutable y el instalador MSI" -ForegroundColor White
}
else {
    Write-Host ""
    Write-Host "❌ Error en la compilación" -ForegroundColor Red
}
```

## 📝 Notas Importantes

### ⚠️ **Por Qué No Funciona PowerShell**
PowerShell/System.Drawing genera ICOs en formato simple que Windows no siempre acepta.
Los ICOs necesitan:
- Múltiples resoluciones (16x16, 32x32, 48x48, 256x256)
- Formato específico de Windows
- Metadatos correctos

### ✅ **Por Qué Usar Herramientas Online**
Las herramientas profesionales:
- Generan ICOs con formato correcto
- Incluyen múltiples resoluciones automáticamente
- Son compatibles con todos los compiladores de .NET
- Optimizan el tamaño del archivo

### 🎯 **Resultado Esperado**
Después de seguir estos pasos:
- ✅ El ejecutable `.exe` tendrá tu logo
- ✅ Al ejecutar, aparecerá en la barra de tareas
- ✅ En el Explorador de Windows se verá el icono
- ✅ El instalador MSI usará tu icono

## 🆘 Solución de Problemas

### **Error: "La secuencia de iconos no está en el formato esperado"**
**Causa:** El archivo `.ico` no tiene el formato correcto.
**Solución:** Convertir nuevamente usando https://convertio.co/es/png-ico/

### **El icono no aparece después de compilar**
1. Verificar que `Assets\app.ico` existe
2. Verificar que está en el `.csproj`
3. Hacer `dotnet clean` antes de `dotnet build`
4. Verificar en propiedades del `.exe` (click derecho → Propiedades)

### **El icono aparece en el exe pero no en la barra de tareas**
- Reiniciar el explorador de Windows: `taskkill /f /im explorer.exe && start explorer`
- Limpiar caché de iconos de Windows

## 🎉 Ejemplo Completo

```powershell
# 1. Convertir logo a ICO (usando herramienta online)
# Visitar: https://convertio.co/es/png-ico/
# Subir: Assets\app_logo.png
# Descargar como: Assets\app.ico

# 2. Verificar archivo
Get-Item Assets\app.ico

# 3. Ejecutar script de configuración
.\configure-icon.ps1

# 4. Verificar resultado
.\bin\Release\net8.0-windows10.0.19041.0\win-x64\GestionTime.Desktop.exe
```

## 📦 Para el Instalador MSI

Una vez configurado el icono en el proyecto:

```powershell
# Regenerar el instalador MSI con el nuevo icono
.\build-msi.ps1
```

El MSI usará automáticamente el icono del ejecutable para:
- Accesos directos del Menú Inicio
- Accesos directos del Escritorio
- Icono en Panel de Control

---

**Fecha:** 2025-01-27
**Archivo:** `GUIA_CONFIGURAR_ICONO.md`
