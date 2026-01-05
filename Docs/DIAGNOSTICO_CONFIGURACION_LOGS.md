# ?? DIAGNÓSTICO COMPLETO DEL SISTEMA DE LOGS - GESTIONTIME DESKTOP

**Fecha:** 29/12/2025  
**Estado:** ?? **DIAGNÓSTICO DE CONFIGURACIÓN DE LOGS**  
**Objetivo:** Verificar ubicación y configuración de archivos de log

---

## ?? **ANÁLISIS DE CONFIGURACIÓN ACTUAL**

### **1. ?? CONFIGURACIÓN EN `appsettings.json`:**
```json
{
  "Api": {
    // ... configuración API ...
  }
  // ? NO HAY SECCIÓN "Logging" configurada
}
```

### **2. ??? RESOLUCIÓN DE PATH EN `App.xaml.cs`:**

#### **Prioridad de rutas (método `ResolveLogPath()`):**

```
1. ?? AppContext.BaseDirectory + "logs/app.log"
   - Ubicación: Junto al ejecutable
   - Ideal para: Debug/Development
   
2. ?? ApplicationData.Current.LocalFolder.Path + "logs/app.log"
   - Ubicación: C:\Users\{User}\AppData\Local\Packages\{AppId}\LocalState\logs\
   - Ideal para: Aplicaciones empaquetadas (MSIX)
   
3. ?? Path.GetTempPath() + "GestionTime/logs/app.log"
   - Ubicación: C:\Users\{User}\AppData\Local\Temp\GestionTime\logs\
   - Ideal para: Último recurso
```

### **3. ?? ARCHIVOS DE LOG GENERADOS:**

#### **Logger Original (`DebugFileLoggerProvider`):**
- ?? **Archivo:** `app.log`
- ?? **Ubicación:** Según prioridad arriba

#### **Logger Optimizado (`RotatingFileLoggerProvider`):**
- ?? **Archivo:** `app_rotating.log` ? `app_YYYYMMDD_rotating.log`
- ?? **Ubicación:** Misma que el original
- ?? **Rotación:** 10MB máximo, 5 archivos históricos

---

## ?? **PROBLEMAS IDENTIFICADOS**

### **? PROBLEMA 1: Sin configuración en appsettings.json**
El archivo `appsettings.json` no tiene sección `Logging`, por lo que:
- No se puede personalizar la ruta de logs
- Usa las rutas por defecto del código

### **? PROBLEMA 2: Dos loggers escribiendo en paralelo**
```csharp
// En modo DEBUG:
builder.AddProvider(new DebugFileLoggerProvider(logPath));           // ? app.log
builder.AddProvider(new RotatingFileLoggerProvider(..._rotating.log)); // ? app_YYYYMMDD_rotating.log
```

### **? PROBLEMA 3: Ruta no determinística**
Dependiendo del ambiente, los logs pueden estar en:
- Debug: `C:\GestionTime\GestionTime.Desktop\bin\Debug\net8.0-windows10.0.19041.0\logs\`
- Packaged: `C:\Users\{User}\AppData\Local\Packages\{PackageFamily}\LocalState\logs\`
- Temp: `C:\Users\{User}\AppData\Local\Temp\GestionTime\logs\`

---

## ? **SOLUCIONES RECOMENDADAS**

### **?? SOLUCIÓN 1: Configurar appsettings.json**

```json
{
  "Api": {
    "BaseUrl": "https://gestiontimeapi.onrender.com",
    "LoginPath": "/api/v1/auth/login",
    "PartesPath": "/api/v1/partes",
    "ClientesPath": "/api/v1/catalog/clientes",
    "GruposPath": "/api/v1/catalog/grupos",
    "TiposPath": "/api/v1/catalog/tipos",
    "MePath": "/api/v1/auth/me"
  },
  "Logging": {
    "LogPath": "C:\\Logs\\GestionTime\\app.log",
    "LogLevel": "Information",
    "MaxFileSizeMB": 10,
    "MaxFiles": 5,
    "EnableRotation": true
  }
}
```

### **?? SOLUCIÓN 2: Unificar loggers**

#### **Opción A: Solo RotatingFileLogger (Recomendado)**
```csharp
LogFactory = LoggerFactory.Create(builder =>
{
    #if DEBUG
        builder.SetMinimumLevel(LogLevel.Debug);
    #else
        builder.SetMinimumLevel(LogLevel.Information);
    #endif
    
    // Solo el logger con rotación
    builder.AddProvider(new RotatingFileLoggerProvider(
        logPath,              // Sin modificar la ruta
        maxFileSize: 10_000_000,
        maxFiles: 5
    ));
});
```

#### **Opción B: Logger condicional**
```csharp
LogFactory = LoggerFactory.Create(builder =>
{
    #if DEBUG
        builder.SetMinimumLevel(LogLevel.Debug);
        // Debug: archivo simple para desarrollo
        builder.AddProvider(new DebugFileLoggerProvider(logPath));
    #else
        builder.SetMinimumLevel(LogLevel.Information);
        // Release: archivo con rotación
        builder.AddProvider(new RotatingFileLoggerProvider(logPath, 10_000_000, 5));
    #endif
});
```

### **?? SOLUCIÓN 3: Ruta fija y predecible**

```csharp
private static string ResolveLogPath()
{
    // Ruta fija para todos los ambientes
    var fixedLogPath = @"C:\Logs\GestionTime\app.log";
    
    try
    {
        var dir = Path.GetDirectoryName(fixedLogPath);
        Directory.CreateDirectory(dir!);
        
        // Test escritura
        File.AppendAllText(fixedLogPath, $"--- log test {DateTime.Now:O} ---{Environment.NewLine}");
        return fixedLogPath;
    }
    catch
    {
        // Fallback a la implementación actual
        return ResolveLogPathFallback();
    }
}

private static string ResolveLogPathFallback()
{
    // Implementación actual como fallback...
}
```

---

## ?? **DIAGNÓSTICO PRÁCTICO**

### **Para verificar dónde están tus logs AHORA:**

1. **Ejecutar aplicación en Debug**
2. **Buscar en Visual Studio Output:** "LOG PATH ="
3. **Verificar archivos generados:**
   ```
   ?? app.log (DebugFileLoggerProvider)
   ?? app_20251229_rotating.log (RotatingFileLoggerProvider) 
   ```

### **Rutas probables según ambiente:**

#### **??? DESARROLLO (Visual Studio Debug):**
```
C:\GestionTime\GestionTime.Desktop\bin\Debug\net8.0-windows10.0.19041.0\logs\
??? app.log
??? app_20251229_rotating.log
```

#### **?? MSIX PACKAGE:**
```
C:\Users\{User}\AppData\Local\Packages\{PackageFamily}\LocalState\logs\
??? app.log
??? app_20251229_rotating.log
```

#### **??? PORTABLE:**
```
{DirectorioAplicacion}\logs\
??? app.log
??? app_20251229_rotating.log
```

---

## ?? **IMPLEMENTACIÓN RECOMENDADA**

### **PASO 1: Actualizar appsettings.json**

```json
{
  "Api": {
    "BaseUrl": "https://gestiontimeapi.onrender.com",
    "LoginPath": "/api/v1/auth/login",
    "PartesPath": "/api/v1/partes",
    "ClientesPath": "/api/v1/catalog/clientes",
    "GruposPath": "/api/v1/catalog/grupos",
    "TiposPath": "/api/v1/catalog/tipos",
    "MePath": "/api/v1/auth/me"
  },
  "Logging": {
    "LogPath": "C:\\Logs\\GestionTime\\app.log"
  }
}
```

### **PASO 2: Simplificar logger en App.xaml.cs**

```csharp
LogFactory = LoggerFactory.Create(builder =>
{
    #if DEBUG
        builder.SetMinimumLevel(LogLevel.Debug);
    #else
        builder.SetMinimumLevel(LogLevel.Information);
    #endif
    
    // Solo logger con rotación automática
    builder.AddProvider(new RotatingFileLoggerProvider(
        logPath,
        maxFileSize: 10_000_000,  // 10MB
        maxFiles: 5               // 5 archivos históricos
    ));
});
```

### **PASO 3: Verificar logs**

Después de los cambios, los logs estarán en:
```
C:\Logs\GestionTime\
??? app_20251229.log          (actual)
??? app_20251228_143022.log   (rotado)
??? app_20251227.log
??? app_20251226.log
??? app_20251225.log          (máximo 5)
```

---

## ?? **CHECKLIST DE VERIFICACIÓN**

### **? PARA VERIFICAR CONFIGURACIÓN ACTUAL:**
- [ ] Ejecutar app en Debug
- [ ] Buscar "LOG PATH =" en Visual Studio Output
- [ ] Verificar archivos en directorio mostrado
- [ ] Confirmar que se generan ambos archivos (.log y _rotating.log)

### **? PARA IMPLEMENTAR MEJORAS:**
- [ ] Actualizar `appsettings.json` con sección Logging
- [ ] Simplificar configuración en `App.xaml.cs`
- [ ] Probar que logs se generan en ruta esperada
- [ ] Verificar rotación automática al superar 10MB

---

## ?? **COMANDOS DE VERIFICACIÓN**

### **PowerShell - Encontrar logs actuales:**
```powershell
# Buscar archivos de log de GestionTime
Get-ChildItem -Path C:\ -Recurse -Include "*app*.log" -ErrorAction SilentlyContinue | 
    Where-Object { $_.DirectoryName -like "*GestionTime*" } |
    Select-Object FullName, Length, LastWriteTime
```

### **Comando - Verificar paths comunes:**
```cmd
dir "C:\Logs\GestionTime\*.log" /s 2>nul
dir "%LOCALAPPDATA%\Packages\*GestionTime*\LocalState\logs\*.log" /s 2>nul
dir "%TEMP%\GestionTime\logs\*.log" /s 2>nul
```

---

## ?? **RESULTADO FINAL ESPERADO**

Con las optimizaciones implementadas:

? **Logs unificados** en una sola ruta predecible  
? **Configuración clara** en appsettings.json  
? **Rotación automática** sin duplicación  
? **Fácil localización** para debugging y soporte  

**?? Logs finales en: `C:\Logs\GestionTime\app_YYYYMMDD.log`**

---

**Preparado:** 29/12/2025  
**Estado:** Diagnóstico completo - Listo para implementar mejoras