# 🎯 PLAN DE PRUEBAS REALES - SISTEMA DE ACTUALIZACIONES
## GestionTime Desktop v1.3.0-beta

---

## ✅ **PASO 1: PUBLICAR v1.3.0-beta EN GITHUB** (5 minutos)

### A. Ir a GitHub Releases
```
https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases/new
```

### B. Configurar el release

**Tag:** `v1.3.0-beta`

**Title:** `GestionTime Desktop v1.3.0-beta`

**Description:**
```markdown
## 🎯 GestionTime Desktop v1.3.0-beta

### ✨ Novedades de esta versión
- 🔄 Sistema de actualizaciones automático (MEJORADO)
- 📊 Versión visible en login (ahora incluye sufijos)
- 🎨 Detección consistente de versiones
- 📦 Instalador MSI profesional con WiX v3
- 📁 Instalación automática en C:\App\GestionTime-Desktop\
- ✅ Sistema de actualizaciones completamente funcional

### 🔧 Mejoras técnicas
- Login y UpdateService leen la misma versión (InformationalVersion)
- Comparación de versiones mejorada
- Mejor manejo de sufijos (-beta, -alpha, etc.)
- Logs mejorados para debugging

### 📦 Instalación
1. Descarga `GestionTime-1.3.0-beta.msi`
2. Ejecuta como Administrador
3. Sigue el asistente de instalación

### 🗑️ Desinstalación
- Windows 11: Configuración > Aplicaciones > GestionTime Desktop > Desinstalar
- Windows 10: Panel de Control > Programas > Desinstalar un programa

### ⚠️ Requisitos del Sistema
- Windows 10 (versión 1809 o superior) / Windows 11
- Arquitectura: x64 (64-bit)
- Espacio en disco: ~300 MB
- .NET 8 Runtime (incluido en el instalador)

### 📞 Soporte
- Repositorio: https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop
- Issues: https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/issues
```

### C. Adjuntar el MSI
```
C:\GestionTime\GestionTimeDesktop\installers\GestionTime-1.3.0-beta.msi
```
Tamaño: ~107.65 MB

### D. Marcar como Pre-release
✅ "This is a pre-release"

### E. Publicar
Clic en **"Publish release"** 🚀

### F. Esperar 2-3 minutos
GitHub necesita procesar el release antes de que la API lo detecte.

---

## ✅ **PASO 2: CAMBIAR LOCAL A v1.2.0-beta** (2 minutos)

Ahora vamos a cambiar la versión LOCAL para simular que el usuario tiene una versión antigua instalada.

### A. Editar GestionTime.Desktop.csproj

```xml
<AssemblyVersion>1.2.0.0</AssemblyVersion>
<FileVersion>1.2.0.0</FileVersion>
<Version>1.2.0-beta</Version>
```

### B. Compilar Debug
```powershell
dotnet build GestionTime.Desktop.csproj -c Debug
```

---

## ✅ **PASO 3: PROBAR DETECCIÓN DE ACTUALIZACIÓN** (5 minutos)

### A. Ejecutar la app
```powershell
dotnet run --project GestionTime.Desktop.csproj -c Debug
```

### B. Observar el login
- Debe mostrar: `v1.2.0-beta` (esquina inferior derecha)

### C. Esperar 5 segundos
Después de que aparezca el login, el sistema verifica actualizaciones automáticamente.

### D. Debe aparecer el diálogo:

```
╔═══════════════════════════════════════╗
║ 🔔 Actualización disponible           ║
║                                       ║
║ ¡Hay una nueva versión disponible!   ║
║                                       ║
║ • Versión actual: 1.2.0-beta         ║
║ • Nueva versión: 1.3.0-beta          ║
║                                       ║
║ ¿Deseas abrir la página de descargas?║
║                                       ║
║   [📥 Descargar]    [Más tarde]      ║
╚═══════════════════════════════════════╝
```

### E. Probar botón "Descargar"
- Debe abrir el navegador en GitHub Releases
- Debe mostrar la versión v1.3.0-beta
- Debe estar disponible el archivo GestionTime-1.3.0-beta.msi

### F. Revisar los logs
```
C:\App\GestionTime-Desktop\logs\app.log
```

Buscar estas líneas:
```
[Information] Verificando actualizaciones disponibles...
[Information] Nueva versión disponible: 1.3.0-beta (actual: 1.2.0-beta)
[Information] Mostrando diálogo de actualización
```

---

## ✅ **PASO 4: PROBAR CON v1.1.0** (5 minutos)

Para asegurarnos de que funciona con diferencias más grandes de versión.

### A. Cambiar a v1.1.0
```xml
<AssemblyVersion>1.1.0.0</AssemblyVersion>
<FileVersion>1.1.0.0</FileVersion>
<Version>1.1.0</Version>
```

### B. Compilar y ejecutar
```powershell
dotnet build GestionTime.Desktop.csproj -c Debug
dotnet run --project GestionTime.Desktop.csproj -c Debug
```

### C. Verificar diálogo
```
• Versión actual: 1.1.0
• Nueva versión: 1.3.0-beta
```

---

## ✅ **PASO 5: PROBAR SIN ACTUALIZACIÓN** (2 minutos)

Verificar que NO aparece el diálogo cuando estamos actualizados.

### A. Cambiar a v1.3.0-beta
```xml
<AssemblyVersion>1.3.0.0</AssemblyVersion>
<FileVersion>1.3.0.0</FileVersion>
<Version>1.3.0-beta</Version>
```

### B. Compilar y ejecutar
```powershell
dotnet build GestionTime.Desktop.csproj -c Debug
dotnet run --project GestionTime.Desktop.csproj -c Debug
```

### C. Verificar que NO aparece diálogo
- Login muestra: `v1.3.0-beta`
- Después de 5 segundos NO debe aparecer el diálogo
- En logs debe decir: "La aplicación está actualizada"

---

## ✅ **PASO 6: INSTALAR MSI Y PROBAR** (10 minutos)

Prueba final con el MSI instalado.

### A. Instalar v1.3.0-beta
1. Ve a `installers\GestionTime-1.3.0-beta.msi`
2. Ejecuta como Administrador
3. Sigue el asistente
4. Verifica instalación en: `C:\App\GestionTime-Desktop\`

### B. Ejecutar app instalada
```
C:\App\GestionTime-Desktop\GestionTime.Desktop.exe
```

### C. Verificar:
- ✅ Login muestra: `v1.3.0-beta`
- ✅ NO aparece diálogo de actualización (porque está actualizado)
- ✅ App funciona correctamente

### D. Desinstalar
```
Configuración > Aplicaciones > GestionTime Desktop > Desinstalar
```

---

## 📊 **RESULTADOS ESPERADOS:**

### ✅ Todo funciona correctamente si:

1. **Login:**
   - Muestra la versión correcta (con sufijo -beta)
   - Consistente con la versión del proyecto

2. **UpdateService:**
   - Detecta correctamente la versión actual
   - Compara correctamente con la versión de GitHub
   - Muestra diálogo SOLO cuando hay actualización

3. **Diálogo:**
   - Aparece después de 5 segundos del login
   - Muestra versiones correctas
   - Botón "Descargar" abre GitHub

4. **GitHub:**
   - Release v1.3.0-beta visible
   - MSI disponible para descargar
   - API responde correctamente

5. **MSI:**
   - Instala en C:\App\GestionTime-Desktop\
   - Crea accesos directos
   - App ejecuta correctamente
   - Desinstalación limpia

---

## ❌ **SI ALGO FALLA:**

### Problema: No aparece el diálogo

**Diagnóstico:**
1. Revisar logs en `C:\App\GestionTime-Desktop\logs\app.log`
2. Verificar que el release esté publicado en GitHub
3. Verificar conexión a internet

**Solución:**
```powershell
# Probar API de GitHub manualmente:
curl https://api.github.com/repos/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases/latest
```

### Problema: Diálogo muestra versiones incorrectas

**Diagnóstico:**
- Versión actual incorrecta → Problema en GetCurrentVersion()
- Versión nueva incorrecta → Problema en CheckForUpdatesAsync()

**Solución:**
- Verificar que InformationalVersion esté correcta
- Verificar tag en GitHub (debe ser `v1.3.0-beta`)

### Problema: Comparación de versiones no funciona

**Diagnóstico:**
- Revisar método IsNewerVersion()
- Verificar que ParseVersion() maneje sufijos correctamente

---

## 🎯 **CHECKLIST COMPLETO:**

```
□ 1. Publicar v1.3.0-beta en GitHub
□ 2. Esperar 2-3 minutos
□ 3. Verificar que release está visible
□ 4. Cambiar local a v1.2.0-beta
□ 5. Compilar Debug
□ 6. Ejecutar app
□ 7. Verificar login muestra v1.2.0-beta
□ 8. Esperar 5 segundos
□ 9. Verificar que aparece diálogo
□ 10. Verificar versiones correctas en diálogo
□ 11. Probar botón "Descargar"
□ 12. Verificar que abre GitHub
□ 13. Cambiar local a v1.1.0
□ 14. Repetir prueba
□ 15. Cambiar local a v1.3.0-beta
□ 16. Verificar que NO aparece diálogo
□ 17. Instalar MSI
□ 18. Ejecutar app instalada
□ 19. Verificar funcionamiento
□ 20. Desinstalar

```

---

## ✅ **TODO LISTO PARA PROBAR**

Ahora tienes:
- ✅ v1.3.0-beta compilada y lista
- ✅ MSI generado en `installers\GestionTime-1.3.0-beta.msi`
- ✅ Plan de pruebas completo
- ✅ Checklist paso a paso

**¿Empezamos con el PASO 1 (publicar en GitHub)?** 🚀
