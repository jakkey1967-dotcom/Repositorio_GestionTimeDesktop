# ?? SOLUCIÓN - ERROR DE CONEXIÓN A LOCALHOST

**Error visto:**
```
Error: Error de conexión al servidor: No se puede establecer una conexión 
ya que el equipo de destino denegó expresamente dicha conexión. (localhost:2501). 
Verifica que la API esté accesible en https://localhost:2501
```

---

## ?? CAUSA DEL PROBLEMA

La aplicación está intentando conectarse a `localhost:2501` en lugar de `https://gestiontimeapi.onrender.com`.

**Esto ocurre porque el archivo `appsettings.json` no se está copiando a la carpeta de salida** cuando compilas la aplicación.

---

## ? SOLUCIÓN APLICADA

He modificado `GestionTime.Desktop.csproj` para asegurar que `appsettings.json` se copie a la carpeta de salida:

```xml
<ItemGroup>
  <None Update="appsettings.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

---

## ?? PASOS PARA SOLUCIONAR

### **1. Limpiar y Recompilar**

Necesitas hacer un **Clean** y **Rebuild** completo:

#### **Opción A: Desde Visual Studio**

1. Menú: **Build** ? **Clean Solution**
2. Menú: **Build** ? **Rebuild Solution**
3. Presionar `F5` para ejecutar

#### **Opción B: Desde Terminal**

```powershell
# Ir a la carpeta del proyecto
cd C:\GestionTime\GestionTime.Desktop

# Limpiar
dotnet clean

# Recompilar
dotnet build

# Ejecutar
dotnet run
```

---

### **2. Verificar que appsettings.json se Copió**

Después de compilar, verificar que el archivo existe en la carpeta de salida:

```powershell
# Para Debug x64
dir "C:\GestionTime\GestionTime.Desktop\bin\x64\Debug\net8.0-windows10.0.19041.0\win-x64\appsettings.json"

# O la carpeta que uses (x86, ARM64, etc.)
```

**Debe existir el archivo** con la configuración de Render.

---

### **3. Verificar Contenido del Archivo**

```powershell
# Ver contenido
type "C:\GestionTime\GestionTime.Desktop\bin\x64\Debug\net8.0-windows10.0.19041.0\win-x64\appsettings.json"
```

**Debe mostrar:**
```json
{
  "Api": {
    "BaseUrl": "https://gestiontimeapi.onrender.com",
    "LoginPath": "/api/v1/auth/login",
    ...
  }
}
```

---

## ?? VERIFICACIÓN EN LOGS

Después de ejecutar la aplicación, revisar los logs en `C:\Logs\GestionTime\app_YYYYMMDD.log`:

### **Buscar estas líneas al inicio:**

```
[INF] App() inicializada. Log en: ...
[INF] API BaseUrl=https://gestiontimeapi.onrender.com LoginPath=/api/v1/auth/login PartesPath=/api/v1/partes
```

? **Si dice `gestiontimeapi.onrender.com`** ? Configuración correcta
? **Si dice `localhost:2501`** ? El archivo no se cargó

---

## ??? SOLUCIÓN ALTERNATIVA (Si persiste el problema)

Si después de rebuild sigue sin funcionar, puedes **hardcodear temporalmente** la URL en `App.xaml.cs`:

### **Ubicación:** `App.xaml.cs` línea ~69

**Cambiar de:**
```csharp
var baseUrl = settings.BaseUrl ?? "https://localhost:2501";
```

**A:**
```csharp
var baseUrl = "https://gestiontimeapi.onrender.com";  // ? FORZAR URL DE RENDER
```

Esto **fuerza** la aplicación a usar Render sin importar el archivo de configuración.

**?? IMPORTANTE:** Esto es temporal. La solución correcta es que `appsettings.json` se copie correctamente.

---

## ?? PRUEBA DESPUÉS DE SOLUCIONAR

1. **Ejecutar la aplicación** (`F5`)

2. **Intentar login** con tus credenciales

3. **Resultado esperado:**
   - ? "Conectando con el servidor..."
   - ?? **Puede tardar 30-60 segundos la primera vez** (Render en plan gratuito se "duerme")
   - ? Login exitoso o mensaje de error específico de la API

4. **Si tarda mucho:**
   - Es normal en el primer request a Render (cold start)
   - Esperar pacientemente
   - Los siguientes requests serán más rápidos

---

## ?? DIAGNÓSTICO AVANZADO

### **Ver qué URL está usando la app en tiempo real:**

Agregar temporalmente este log en `LoginPage.xaml.cs` antes del login:

```csharp
// En OnLoginClick, antes de la llamada al API
App.Log?.LogInformation("?? DEBUG: Intentando conectar a: {url}", App.Api.BaseUrl);
```

Esto mostrará en logs exactamente a qué URL está intentando conectarse.

---

## ?? CHECKLIST DE SOLUCIÓN

- [ ] Modificado `GestionTime.Desktop.csproj` con `<CopyToOutputDirectory>`
- [ ] Ejecutado **Clean Solution**
- [ ] Ejecutado **Rebuild Solution**
- [ ] Verificado que `appsettings.json` existe en `bin\...\`
- [ ] Verificado contenido de `appsettings.json` en `bin\...\`
- [ ] Logs muestran URL de Render
- [ ] Login funciona (o muestra error de la API, no de conexión)

---

## ? RESULTADO ESPERADO

Después de aplicar la solución:

```
??????????????????????????????????????????????
?  ? Aplicación se conecta a Render         ?
?                                            ?
?  ?? URL: gestiontimeapi.onrender.com      ?
?  ?? appsettings.json cargado correctamente ?
?  ?? Primera conexión puede tardar ~60s    ?
?  ? Login funcional                        ?
??????????????????????????????????????????????
```

---

## ?? SI AÚN NO FUNCIONA

1. **Compartir logs completos:**
   ```powershell
   type C:\Logs\GestionTime\app_*.log > log_completo.txt
   ```

2. **Verificar conectividad a Render:**
   ```powershell
   # Probar desde PowerShell
   Invoke-WebRequest -Uri "https://gestiontimeapi.onrender.com/health" -Method GET
   ```

3. **Probar en navegador:**
   - Abrir: `https://gestiontimeapi.onrender.com/health`
   - Debe responder (aunque sea con error 404, significa que está accesible)

---

**Fecha:** 2025-01-27  
**Estado:** ? Solución aplicada  
**Próximo paso:** Clean + Rebuild + Run
