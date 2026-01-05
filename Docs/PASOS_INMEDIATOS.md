# ?? PASOS INMEDIATOS - SOLUCIONAR ERROR DE CONEXIÓN

**Tu error:** La app intenta conectarse a `localhost:2501` en lugar de Render

---

## ? PASO 1: LIMPIAR Y RECOMPILAR

### **En Visual Studio:**

1. Cerrar la aplicación si está ejecutándose
2. Menú: **Build** ? **Clean Solution** 
3. Menú: **Build** ? **Rebuild Solution**
4. Esperar a que termine (sin errores)
5. Presionar `F5` para ejecutar

---

## ? PASO 2: PROBAR LOGIN

1. Ingresar tus credenciales
2. Click "Iniciar Sesión"
3. **IMPORTANTE:** La primera conexión a Render puede tardar **30-60 segundos**
4. Esperar pacientemente (Render en plan gratuito se "duerme")

---

## ? PASO 3: VERIFICAR LOGS

Si sigue sin funcionar, revisar logs:

```powershell
# Abrir logs
notepad C:\Logs\GestionTime\app_20250127.log
```

**Buscar esta línea:**
```
API BaseUrl=https://gestiontimeapi.onrender.com
```

? **Si aparece Render** ? Configuración correcta, solo espera más tiempo  
? **Si aparece localhost** ? El archivo no se copió, pasar al Paso 4

---

## ? PASO 4: VERIFICAR ARCHIVO (Si Paso 3 falló)

```powershell
# Ver si existe el archivo
dir "C:\GestionTime\GestionTime.Desktop\bin\x64\Debug\net8.0-windows10.0.19041.0\win-x64\appsettings.json"
```

? **Si NO existe:**
1. Cerrar Visual Studio
2. Abrir de nuevo
3. Repetir Paso 1

? **Si existe:**
```powershell
# Ver contenido
type "C:\GestionTime\GestionTime.Desktop\bin\x64\Debug\net8.0-windows10.0.19041.0\win-x64\appsettings.json"
```

Debe decir `gestiontimeapi.onrender.com`

---

## ?? SOLUCIÓN RÁPIDA ALTERNATIVA

Si todo lo anterior falla, editar `App.xaml.cs`:

### **Línea ~69, cambiar:**

```csharp
var baseUrl = settings.BaseUrl ?? "https://localhost:2501";
```

### **Por:**

```csharp
var baseUrl = "https://gestiontimeapi.onrender.com";  // ? FORZADO
```

Guardar, recompilar, ejecutar.

---

## ?? RESULTADO ESPERADO

### **Al hacer login:**

```
? "Conectando con el servidor..."
?? Esperando... (puede tardar 30-60s la primera vez)
? "Inicio de sesión exitoso" o error específico de la API
```

### **En los logs:**

```
[INF] API BaseUrl=https://gestiontimeapi.onrender.com LoginPath=/api/v1/auth/login
[INF] LoginAsync iniciado para tu-email@ejemplo.com
[INF] HTTP POST /api/v1/auth/login Payload: ...
```

---

## ?? IMPORTANTE SOBRE RENDER

**Render en plan gratuito:**
- ? Es gratis
- ?? Se "duerme" después de 15 minutos de inactividad
- ?? Primera conexión tarda 30-60 segundos
- ? Conexiones siguientes son rápidas

**Si ves "Conectando..." por más de 60 segundos:**
1. Verificar que la URL de Render esté correcta
2. Probar en navegador: `https://gestiontimeapi.onrender.com`
3. Si no responde en navegador, la API puede estar caída

---

## ?? SI NADA FUNCIONA

Compartir los logs completos:

```powershell
type C:\Logs\GestionTime\app_*.log > C:\log_completo.txt
notepad C:\log_completo.txt
```

Buscar en los logs:
- `[ERR]` ? Errores
- `API BaseUrl=` ? URL que está usando
- `HTTP POST /api/v1/auth/login` ? Intentos de login
- `EXCEPTION` ? Excepciones

---

**Fecha:** 2025-01-27  
**Acción inmediata:** Clean + Rebuild + F5  
**Tiempo esperado primera conexión:** 30-60 segundos  
**Estado:** ? Solución lista para aplicar
