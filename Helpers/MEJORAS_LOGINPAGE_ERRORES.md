# Mejoras al LoginPage - Manejo de Errores de Conexión

## ?? Resumen de Cambios

Se han implementado mejoras significativas en `LoginPage.xaml.cs` y `LoginPage.xaml` para mostrar claramente los errores de conexión y mejorar la experiencia del usuario.

---

## ? Mejoras Implementadas

### 1. **Manejo Específico de Errores de Conexión**

Se han agregado dos métodos nuevos para convertir excepciones técnicas en mensajes amigables:

#### `GetHttpErrorMessage(HttpRequestException ex)`

Detecta y traduce errores HTTP comunes:

| Error Técnico | Mensaje Usuario |
|---------------|-----------------|
| "No such host is known" | "No se puede conectar: Servidor no encontrado. Verifica la URL del API." |
| "Connection refused" | "Conexión rechazada: El servidor no está disponible." |
| "Connection timed out" | "Timeout: El servidor no responde a tiempo." |
| HTTP 401 | "Credenciales incorrectas (401 No autorizado)." |
| HTTP 403 | "Acceso denegado (403 Prohibido)." |
| HTTP 404 | "Endpoint no encontrado (404). Verifica la configuración del API." |
| HTTP 500 | "Error del servidor (500). Contacta al administrador." |
| HTTP 502 | "Error de gateway (502). El servidor no está accesible." |
| HTTP 503 | "Servicio no disponible (503). El servidor está temporalmente fuera de línea." |

#### `GetFriendlyErrorMessage(Exception ex)`

Detecta otros tipos de excepciones:

- `HttpRequestException` ? Redirige a `GetHttpErrorMessage`
- `TaskCanceledException` ? "Operación cancelada o timeout."
- `SocketException` ? "Error de red: No se puede establecer conexión."
- Otros ? Muestra el mensaje de la excepción

---

### 2. **Try-Catch Mejorado en OnLoginClick**

```csharp
// Llamada real al API
ApiClient.LoginResponse? res = null;

try
{
    res = await App.Api.LoginAsync(email, pass);
}
catch (HttpRequestException httpEx)
{
    // Error de conexión HTTP específico
    var errorMsg = GetHttpErrorMessage(httpEx);
    ShowMessage(errorMsg, MessageType.Error);
    SetBusy(false, "");
    return;
}
catch (TaskCanceledException)
{
    // Timeout
    ShowMessage("Timeout: El servidor no responde. Verifica tu conexión.", MessageType.Error);
    SetBusy(false, "");
    return;
}
```

**Beneficios:**
- Captura errores de conexión ANTES de que lleguen al catch general
- Muestra mensajes claros y accionables
- Restaura el estado de la UI correctamente

---

### 3. **Cuadro de Mensajes Mejorado (XAML)**

#### Antes:
```xaml
<Border x:Name="MsgBox"
        BorderThickness="1"
        Padding="8">
```

#### Después:
```xaml
<Border x:Name="MsgBox"
        BorderThickness="2"
        CornerRadius="8"
        Padding="10,8"
        Margin="0,6,0,4">
    <Grid ColumnDefinitions="Auto,*">
        <FontIcon x:Name="MsgIcon"
                  FontSize="14"
                  VerticalAlignment="Top"
                  Margin="0,1,8,0"/>
        <TextBlock x:Name="LblMsg"
                   TextWrapping="Wrap"
                   FontSize="11"
                   LineHeight="16"/>
    </Grid>
</Border>
```

**Mejoras visuales:**
- ? Borde más grueso (2px) para mayor visibilidad
- ? Esquinas redondeadas (8px)
- ? Mejor espaciado (padding 10,8)
- ? Icono dinámico que cambia según el tipo de mensaje
- ? Texto con mejor line-height para legibilidad

---

### 4. **Iconos Dinámicos en Mensajes**

El método `ShowMessage` ahora cambia el icono según el tipo:

| Tipo | Icono | Glyph | Color |
|------|-------|-------|-------|
| Success | ? CheckMark | `\uE73E` | Verde `#22C55E` |
| Error | ?? Warning | `\uE783` | Rojo `#DC2626` |
| Warning | ?? Info | `\uE7BA` | Amarillo `#F59E0B` |
| Info | ?? Info | `\uE946` | Azul `#3B82F6` |

```csharp
switch (type)
{
    case MessageType.Success:
        MsgIcon.Glyph = "\uE73E"; // CheckMark
        MsgIcon.Foreground = new SolidColorBrush(...);
        break;
    
    case MessageType.Error:
        MsgIcon.Glyph = "\uE783"; // Warning
        MsgIcon.Foreground = new SolidColorBrush(...);
        break;
    // ...
}
```

---

### 5. **Status Text Visibility Mejorado**

```csharp
private void SetBusy(bool busy, string status)
{
    // ...existing code...
    TxtStatus.Visibility = string.IsNullOrEmpty(status) 
        ? Visibility.Collapsed 
        : Visibility.Visible;
}
```

**Beneficio:** El texto de estado solo se muestra cuando hay un mensaje activo.

---

### 6. **Mejoras en Colores de Mensajes**

Los colores se han actualizado para mejor contraste y accesibilidad:

#### Success (Verde)
- Background: `#DCFCE7` (verde muy claro)
- Border: `#22C55E` (verde vibrante)
- Text: `#15803D` (verde oscuro)

#### Error (Rojo)
- Background: `#FEE2E2` (rojo muy claro)
- Border: `#DC2626` (rojo vibrante)
- Text: `#7F1D1D` (rojo oscuro)

#### Warning (Amarillo)
- Background: `#FEF3C7` (amarillo muy claro)
- Border: `#F59E0B` (amarillo/naranja)
- Text: `#92400E` (marrón)

#### Info (Azul)
- Background: `#E0F2FE` (azul muy claro)
- Border: `#3B82F6` (azul vibrante)
- Text: `#1E40AF` (azul oscuro)

---

## ?? Escenarios de Error Cubiertos

### ? 1. Servidor no encontrado
**Causa:** URL del API incorrecta o DNS no resuelve  
**Mensaje:** "No se puede conectar: Servidor no encontrado. Verifica la URL del API."

### ? 2. Servidor apagado/rechaza conexión
**Causa:** El servidor no está corriendo o puerto cerrado  
**Mensaje:** "Conexión rechazada: El servidor no está disponible."

### ? 3. Timeout
**Causa:** Servidor responde muy lento o red inestable  
**Mensaje:** "Timeout: El servidor no responde a tiempo."

### ? 4. Credenciales incorrectas (401)
**Causa:** Usuario/contraseña inválidos  
**Mensaje:** "Credenciales incorrectas (401 No autorizado)."

### ? 5. Acceso denegado (403)
**Causa:** Usuario válido pero sin permisos  
**Mensaje:** "Acceso denegado (403 Prohibido)."

### ? 6. Endpoint no encontrado (404)
**Causa:** Ruta del API incorrecta  
**Mensaje:** "Endpoint no encontrado (404). Verifica la configuración del API."

### ? 7. Error del servidor (500)
**Causa:** Excepción no controlada en el servidor  
**Mensaje:** "Error del servidor (500). Contacta al administrador."

### ? 8. Gateway error (502/503)
**Causa:** Proxy/balanceador no puede alcanzar el servidor  
**Mensaje:** "Error de gateway (502). El servidor no está accesible."

---

## ?? Ejemplo Visual

### Antes (sin mejoras):
```
?? [Error genérico]
"System.Net.Http.HttpRequestException: No connection could be made..."
```

### Después (con mejoras):
```
?? Conexión rechazada: El servidor no está disponible.
```

---

## ?? Pruebas Recomendadas

### Prueba 1: Servidor Apagado
1. Apagar el API backend
2. Intentar login
3. **Esperado:** "Conexión rechazada: El servidor no está disponible."

### Prueba 2: URL Incorrecta
1. Cambiar `BaseUrl` en `appsettings.json` a `http://servidor-no-existe.com`
2. Intentar login
3. **Esperado:** "No se puede conectar: Servidor no encontrado. Verifica la URL del API."

### Prueba 3: Timeout
1. Configurar el servidor para responder muy lento (>30s)
2. Intentar login
3. **Esperado:** "Timeout: El servidor no responde. Verifica tu conexión."

### Prueba 4: Credenciales Incorrectas
1. Usar email/password inválidos
2. Intentar login
3. **Esperado:** "Credenciales incorrectas (401 No autorizado)."

### Prueba 5: Endpoint Incorrecto
1. Cambiar `LoginPath` a `/api/auth/login-error`
2. Intentar login
3. **Esperado:** "Endpoint no encontrado (404). Verifica la configuración del API."

---

## ?? Archivos Modificados

1. **Views/LoginPage.xaml.cs**
   - Agregado: `GetHttpErrorMessage(HttpRequestException ex)`
   - Agregado: `GetFriendlyErrorMessage(Exception ex)`
   - Mejorado: `OnLoginClick` con try-catch específicos
   - Mejorado: `ShowMessage` con iconos dinámicos
   - Mejorado: `SetBusy` con visibilidad de status

2. **Views/LoginPage.xaml**
   - Mejorado: `MsgBox` con mejor padding y bordes
   - Agregado: `MsgIcon` con nombre para manipulación dinámica
   - Mejorado: Layout del mensaje con Grid de 2 columnas

---

## ?? Próximos Pasos Opcionales

### 1. **Reintentar Automáticamente**
```csharp
// Agregar lógica de retry con backoff exponencial
for (int i = 0; i < 3; i++)
{
    try
    {
        res = await App.Api.LoginAsync(email, pass);
        break;
    }
    catch (HttpRequestException) when (i < 2)
    {
        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i)));
    }
}
```

### 2. **Botón "Reintentar" en Error**
```xaml
<Button Content="Reintentar"
        Click="OnRetryClick"
        Visibility="{x:Bind HasError, Mode=OneWay}"/>
```

### 3. **Link a Ayuda Contextual**
```csharp
if (errorType == ErrorType.ServerNotFound)
{
    ShowHelp("Como-configurar-url-del-servidor");
}
```

### 4. **Logging Detallado**
Los errores ya se loguean en `App.Log` con nivel `Error`:
```csharp
App.Log?.LogError(httpEx, "Error de conexión HTTP: {msg}", errorMsg);
```

---

## ? Resultado Final

### Antes:
- ? Errores genéricos no descriptivos
- ? Mensaje pequeño y poco visible
- ? Sin iconos diferenciadores
- ? Colores poco contrastados

### Después:
- ? Mensajes claros y accionables
- ? Cuadro de mensaje grande y visible
- ? Iconos dinámicos según tipo de error
- ? Colores con alto contraste
- ? Manejo específico de 9+ tipos de errores
- ? Logs detallados para debugging

---

**Fecha:** 2024-12-24  
**Estado:** ? Implementado y Compilado  
**Compilación:** ? Sin errores  
**Archivos:** 2 modificados (LoginPage.xaml, LoginPage.xaml.cs)
