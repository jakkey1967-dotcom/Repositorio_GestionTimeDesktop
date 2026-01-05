# ?? GUÍA DE PRUEBAS - APLICACIÓN ACTUALIZADA

**Objetivo:** Verificar que la aplicación funciona correctamente con la API en Render

---

## ? PRE-REQUISITOS

Antes de empezar, verifica:

- [ ] Visual Studio 2022 instalado
- [ ] .NET 8 SDK instalado
- [ ] Conexión a Internet activa
- [ ] API en Render accesible: `https://gestiontimeapi.onrender.com`

---

## ?? PASO 1: COMPILAR Y EJECUTAR

### **Opción A: Desde Visual Studio**

1. Abrir `GestionTime.Desktop.sln` en Visual Studio
2. Presionar `F5` (Debug) o `Ctrl+F5` (Sin debug)
3. Esperar a que compile y se abra la ventana

### **Opción B: Desde Terminal**

```powershell
cd C:\GestionTime\GestionTime.Desktop
dotnet build
dotnet run
```

---

## ?? PASO 2: PROBAR LOGIN

### **Test 1: Login Exitoso**

1. En la pantalla de login, ingresar:
   ```
   Email: [tu-email-registrado]
   Password: [tu-contraseña]
   ```

2. Click en "Iniciar Sesión"

3. **Resultados Esperados:**
   - ? Mensaje: "Conectando con el servidor..."
   - ? Mensaje: "Inicio de sesión exitoso (XXXms)"
   - ?? Navegación a DiarioPage
   - ?? Banner superior muestra tu información

4. **Verificar Banner:**
   ```
   ?? Debe mostrar:
   • Nombre de usuario (o email si no hay nombre)
   • Email del usuario
   • Rol del usuario (Admin/Técnico/Usuario)
   ```

---

### **Test 2: Login con Credenciales Incorrectas**

1. Ingresar:
   ```
   Email: usuario-inexistente@test.com
   Password: password-incorrecto
   ```

2. Click en "Iniciar Sesión"

3. **Resultados Esperados:**
   - ? Mensaje de error claro
   - ?? Borde rojo en el mensaje
   - ?? No navega a DiarioPage
   - ?? Log registra el intento fallido

---

### **Test 3: API No Disponible (Simulado)**

**?? IMPORTANTE:** Este test es opcional, solo para verificar manejo de errores

1. Modificar temporalmente `appsettings.json`:
   ```json
   "BaseUrl": "https://api-inexistente-12345.onrender.com"
   ```

2. Reiniciar la aplicación

3. Intentar login

4. **Resultados Esperados:**
   - ? Mensaje: "No se puede conectar: Servidor no encontrado..."
   - ?? Log registra error de conexión
   - ??? Aplicación no se cierra

5. **Restaurar `appsettings.json`:**
   ```json
   "BaseUrl": "https://gestiontimeapi.onrender.com"
   ```

---

## ?? PASO 3: PROBAR CARGA DE PARTES

1. Después de login exitoso, en DiarioPage:

2. **Verificar que carga la lista de partes:**
   - ?? Fecha actual seleccionada
   - ?? Lista de partes cargada (si hay partes creados)
   - ?? Filtro de búsqueda funcional

3. **Probar filtro:**
   - Escribir en el campo de búsqueda
   - Lista se filtra en tiempo real

4. **Probar fecha:**
   - Cambiar fecha en el calendario
   - Lista se recarga automáticamente

---

## ?? PASO 4: PROBAR CREACIÓN DE PARTE

1. Click en botón "Nuevo" (o `Ctrl+N`)

2. **Verificar ventana de edición:**
   - ? Se abre ventana modal
   - ?? Mismo tema que DiarioPage
   - ?? Campos vacíos listos para llenar

3. **Llenar campos:**
   ```
   Cliente: [Seleccionar cliente]
   Tienda: [Nombre de tienda]
   Acción: [Descripción breve]
   Hora Inicio: [HH:MM]
   Grupo: [Seleccionar grupo]
   Tipo: [Seleccionar tipo]
   ```

4. **Guardar:**
   - Click en "Guardar"
   - ? Ventana se cierra
   - ?? Lista se recarga automáticamente
   - ?? Nuevo parte aparece en la lista

---

## ?? PASO 5: PROBAR EDICIÓN DE PARTE

1. Seleccionar un parte de la lista

2. Click en "Editar" (o `Ctrl+E`)

3. **Verificar ventana de edición:**
   - ? Campos pre-llenados con datos del parte
   - ?? Título: "Editar Parte"

4. **Modificar datos:**
   - Cambiar algún campo
   - Click en "Guardar"

5. **Verificar:**
   - ? Cambios guardados correctamente
   - ?? Lista actualizada con cambios

---

## ??? PASO 6: PROBAR ELIMINACIÓN DE PARTE

1. Seleccionar un parte de la lista

2. Click en "Borrar" (o `Delete`)

3. **Verificar diálogo de confirmación:**
   - ?? Mensaje de advertencia claro
   - ?? Enfatiza que es DEFINITIVO

4. **Confirmar eliminación:**
   - Click en "Eliminar definitivamente"
   - ? Parte eliminado
   - ?? Lista actualizada

---

## ?? PASO 7: PROBAR GRÁFICA

1. Click en botón "Gráfica" (o `F8`)

2. **Verificar ventana de gráfica:**
   - ? Se abre ventana nueva
   - ?? Gráfica de dona visible
   - ?? Fecha actual seleccionada

3. **Cambiar fecha:**
   - Seleccionar otra fecha
   - ?? Gráfica se actualiza automáticamente

---

## ?? PASO 8: VERIFICAR LOGS

### **1. Abrir Carpeta de Logs**

```powershell
explorer C:\Logs\GestionTime
```

### **2. Verificar Archivos de Log**

Deben existir:
- `app_YYYYMMDD.log` ? Logs generales
- `http_YYYYMMDD.log` ? Logs HTTP (si está habilitado)

### **3. Buscar Entradas Específicas**

**En `app_YYYYMMDD.log`:**

```
?? Buscar "LoginAsync iniciado"
? Debe mostrar email del usuario

?? Buscar "Token extraído"
? Si login exitoso

?? Buscar "Guardando información de usuario"
? Debe mostrar userName, userEmail, userRole

?? Buscar "devolvió null"
?? Si hay, indica que la API no devolvió algún dato

?? Buscar "ERROR"
? Si hay, revisar detalles del error
```

---

## ??? PASO 9: PROBAR MANEJO DE ERRORES

### **Test A: API Lenta (Timeout Simulado)**

1. Render en plan gratuito puede tardar hasta 60 segundos la primera vez

2. Hacer login y esperar

3. **Resultados Esperados:**
   - ? Spinner girando
   - ?? Mensaje "Conectando con el servidor..."
   - ?? Eventualmente conecta o muestra timeout

---

### **Test B: Respuesta Null del Servidor**

Este test ya está cubierto automáticamente por el código. Si la API devuelve null:

- ? La app usa valores por defecto
- ?? Log registra advertencia
- ??? No hay crash

---

### **Test C: Datos Faltantes**

Si la API no devuelve `userName`, `userEmail` o `userRole`:

- ? Banner muestra valores por defecto
- ?? Log indica qué campos vinieron null
- ??? Aplicación sigue funcionando

---

## ?? PASO 10: MONITOREO EN TIEMPO REAL

### **Abrir Logs en Vivo**

```powershell
# Opción 1: Notepad++
notepad++ C:\Logs\GestionTime\app_20250127.log

# Opción 2: PowerShell tail
Get-Content C:\Logs\GestionTime\app_20250127.log -Wait -Tail 50

# Opción 3: Visual Studio Code
code C:\Logs\GestionTime\app_20250127.log
```

### **Realizar Operaciones y Ver Logs**

1. Hacer login ? Ver logs de autenticación
2. Cargar partes ? Ver logs de GET
3. Crear parte ? Ver logs de POST
4. Editar parte ? Ver logs de PUT
5. Eliminar parte ? Ver logs de DELETE

---

## ? CHECKLIST DE VERIFICACIÓN

Marca cada item después de probarlo:

### **Login**
- [ ] Login exitoso con credenciales correctas
- [ ] Error claro con credenciales incorrectas
- [ ] Banner muestra información del usuario
- [ ] Logs registran el login correctamente

### **DiarioPage**
- [ ] Lista de partes carga correctamente
- [ ] Filtro de búsqueda funciona
- [ ] Cambio de fecha recarga lista
- [ ] Zebra rows visible en la lista
- [ ] Botones habilitados/deshabilitados correctamente

### **CRUD de Partes**
- [ ] Nuevo parte se crea correctamente
- [ ] Editar parte funciona
- [ ] Eliminar parte funciona
- [ ] Ventanas modales usan el mismo tema

### **Gráfica**
- [ ] Ventana de gráfica se abre
- [ ] Gráfica muestra datos correctos
- [ ] Cambio de fecha actualiza gráfica

### **Manejo de Errores**
- [ ] Mensajes de error claros y amigables
- [ ] Aplicación no se cierra ante errores
- [ ] Logs registran errores detalladamente
- [ ] Valores por defecto cuando la API no devuelve datos

### **Logs**
- [ ] Logs generales se crean en C:\Logs\GestionTime\
- [ ] Logs incluyen información útil
- [ ] No hay errores críticos inesperados

---

## ?? REPORTE DE PROBLEMAS

Si encuentras algún problema, recopilar:

1. **Descripción del Problema:**
   - ¿Qué estabas haciendo?
   - ¿Qué esperabas que pasara?
   - ¿Qué pasó realmente?

2. **Logs:**
   ```powershell
   # Copiar logs del día
   copy C:\Logs\GestionTime\app_*.log C:\Logs\Backup\
   ```

3. **Pasos para Reproducir:**
   1. Paso 1...
   2. Paso 2...
   3. Error ocurre en paso X

4. **Información del Sistema:**
   - Windows version
   - .NET version
   - Visual Studio version

---

## ?? CONTACTO

Si encuentras problemas o necesitas ayuda:

1. Revisar `Helpers/RESUMEN_EJECUTIVO_FINAL.md`
2. Revisar `Helpers/ACTUALIZACION_API_RENDER.md`
3. Revisar logs en `C:\Logs\GestionTime\`

---

## ?? RESULTADO ESPERADO

Después de completar todas las pruebas:

```
???????????????????????????????????????????
?  ? TODAS LAS PRUEBAS EXITOSAS          ?
?                                         ?
?  ?? Login funcional                     ?
?  ?? CRUD de partes funcional            ?
?  ?? Gráficas funcionales                ?
?  ??? Manejo de errores robusto          ?
?  ?? Logs completos                      ?
?                                         ?
?  ?? APLICACIÓN LISTA PARA USAR          ?
???????????????????????????????????????????
```

---

**Fecha:** 2025-01-27  
**Versión:** 1.0  
**Estado:** ? Lista para testing
