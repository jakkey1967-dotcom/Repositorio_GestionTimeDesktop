# 🚀 PRUEBA COMPLETA DEL SISTEMA DE ACTUALIZACIONES
## GestionTime Desktop - Test Real con GitHub

---

## 📋 **RESUMEN DEL PROCESO:**

Vamos a simular un escenario REAL:
1. ✅ **Usuario tiene instalada:** v1.1.0
2. ✅ **Nueva versión disponible en GitHub:** v1.2.0-beta
3. ✅ **Sistema detecta actualización:** Aparece diálogo
4. ✅ **Usuario descarga MSI:** Desde GitHub Releases

---

## 🔧 **PASO 1: COMPILAR VERSIÓN 1.2.0-BETA (RELEASE)**

### A. Cambiar la versión del proyecto a 1.2.0-beta

```powershell
# Abrir GestionTime.Desktop.csproj y cambiar:
<AssemblyVersion>1.2.0.0</AssemblyVersion>
<FileVersion>1.2.0.0</FileVersion>
<Version>1.2.0-beta</Version>
```

### B. Compilar la versión Release

```powershell
dotnet publish GestionTime.Desktop.csproj -c Release -r win-x64 --self-contained true -o publish\portable
```

### C. Generar el MSI

```powershell
cd WiX-v3-MSI
.\Build-MSI.ps1
```

**Resultado esperado:**
```
✅ installers\GestionTime-1.2.0-beta.msi (~107 MB)
```

---

## 📤 **PASO 2: PUBLICAR EN GITHUB**

### A. Ir a GitHub Releases

```
https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases/new
```

### B. Configurar el release

- **Tag:** `v1.2.0-beta`
- **Title:** `GestionTime Desktop v1.2.0-beta`
- **Description:**
```markdown
## 🎯 GestionTime Desktop v1.2.0-beta

### ✨ Novedades
- 🔄 Sistema de actualizaciones automático
- 📊 Versión visible en el login
- 🎨 Mejoras en la interfaz
- 📦 Instalador MSI profesional
- 📁 Instalación en C:\App\GestionTime-Desktop\

### 📦 Instalación
1. Descarga `GestionTime-1.2.0-beta.msi`
2. Ejecuta como Administrador
3. Sigue el asistente de instalación

### 🗑️ Desinstalación
- Configuración > Aplicaciones > GestionTime Desktop > Desinstalar

### ⚠️ Requisitos
- Windows 10 (1809+) / Windows 11
- Procesador x64 (64-bit)
```

### C. Adjuntar el MSI

- Arrastra `installers\GestionTime-1.2.0-beta.msi` (~107 MB)

### D. Marcar como Pre-release

- ✅ Marca "This is a pre-release"

### E. Publicar

- Clic en **"Publish release"** 🚀

---

## ⏱️ **PASO 3: ESPERAR (IMPORTANTE)**

GitHub tarda unos minutos en procesar el release:

```
⏳ Espera 2-3 minutos después de publicar
```

Verifica que el release esté visible en:
```
https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases
```

---

## 🔄 **PASO 4: RESTAURAR VERSIÓN 1.1.0 (PARA PROBAR)**

### A. Cambiar el proyecto de vuelta a 1.1.0

```powershell
# En GestionTime.Desktop.csproj cambiar a:
<AssemblyVersion>1.1.0.0</AssemblyVersion>
<FileVersion>1.1.0.0</FileVersion>
<Version>1.1.0</Version>
```

### B. Activar UpdateService REAL (no mock)

```csharp
// En App.xaml.cs línea ~155, cambiar de:
UpdateService = new UpdateServiceMock();

// A:
UpdateService = new UpdateService(LogFactory.CreateLogger<UpdateService>());
```

### C. Compilar versión 1.1.0 en Debug

```powershell
dotnet build GestionTime.Desktop.csproj -c Debug
```

---

## 🧪 **PASO 5: EJECUTAR Y PROBAR**

### A. Ejecutar la versión 1.1.0

```powershell
dotnet run --project GestionTime.Desktop.csproj -c Debug
```

### B. Observar el proceso

**Cronograma esperado:**

```
00:00 - App inicia
00:01 - Aparece LoginPage con versión "v1.1.0"
00:05 - 🔔 DEBE APARECER EL DIÁLOGO:

╔═══════════════════════════════════════╗
║ 🔔 Actualización disponible           ║
║                                       ║
║ ¡Hay una nueva versión disponible!   ║
║                                       ║
║ • Versión actual: 1.1.0              ║
║ • Nueva versión: 1.2.0-beta          ║
║                                       ║
║ ¿Deseas abrir la página de descargas?║
║                                       ║
║   [📥 Descargar]    [Más tarde]      ║
╚═══════════════════════════════════════╝
```

### C. Probar las opciones

**Si haces clic en "Descargar":**
- ✅ Se abre el navegador en GitHub Releases
- ✅ Ves el archivo GestionTime-1.2.0-beta.msi
- ✅ Puedes descargarlo

**Si haces clic en "Más tarde":**
- ✅ El diálogo se cierra
- ✅ Puedes seguir usando la app

---

## 📊 **PASO 6: VERIFICAR LOGS**

### A. Revisar los logs de la app

```
C:\App\GestionTime-Desktop\logs\app.log
```

### B. Buscar estas líneas

```
[Information] Verificando actualizaciones disponibles...
[Information] Nueva versión disponible: 1.2.0-beta
[Information] Mostrando diálogo de actualización
[Information] Usuario decidió descargar la actualización
```

**O si no aparece el diálogo:**

```
[Warning] No se pudo verificar actualizaciones
[Error] ...detalles del error...
```

---

## ✅ **RESULTADOS ESPERADOS:**

### ✅ **TODO FUNCIONA:**

1. ✅ El diálogo aparece después de 5 segundos
2. ✅ Muestra las versiones correctas (1.1.0 → 1.2.0-beta)
3. ✅ Al hacer clic en "Descargar", abre GitHub
4. ✅ El MSI está disponible para descargar

### ❌ **SI ALGO FALLA:**

#### Problema 1: No aparece el diálogo

**Posibles causas:**
- El release no está publicado correctamente en GitHub
- La API de GitHub no responde
- El UpdateService tiene un bug

**Diagnóstico:**
```powershell
# Verificar que el release existe:
curl https://api.github.com/repos/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases/latest
```

#### Problema 2: Aparece error en logs

**Revisar:**
```
C:\App\GestionTime-Desktop\logs\app.log
```

#### Problema 3: El diálogo no tiene XamlRoot

**Solución:** Ya está corregido en el código actual

---

## 🎯 **DESPUÉS DE PROBAR:**

### Si TODO funciona correctamente:

1. ✅ **Dejar la versión 1.2.0-beta en el proyecto**
2. ✅ **Compilar la versión final Release:**
```powershell
# Cambiar a 1.2.0-beta en .csproj
dotnet publish -c Release -r win-x64 --self-contained true -o publish\portable
cd WiX-v3-MSI
.\Build-MSI.ps1
```

3. ✅ **Actualizar el release en GitHub:**
   - Sube el nuevo MSI (si hay cambios)
   - Marca como "Latest release" (quita Pre-release)

---

## 📝 **CHECKLIST COMPLETO:**

```
□ 1. Cambiar versión a 1.2.0-beta
□ 2. Compilar Release
□ 3. Generar MSI
□ 4. Publicar en GitHub (Tag: v1.2.0-beta)
□ 5. Esperar 2-3 minutos
□ 6. Verificar que el release está visible
□ 7. Cambiar versión de vuelta a 1.1.0
□ 8. Activar UpdateService real (no mock)
□ 9. Compilar Debug
□ 10. Ejecutar la app
□ 11. Esperar 5 segundos
□ 12. Verificar que aparece el diálogo
□ 13. Probar "Descargar" y "Más tarde"
□ 14. Revisar logs
□ 15. Confirmar que TODO funciona
```

---

## 🚀 **¿EMPEZAMOS?**

Dime cuando estés listo y voy paso a paso contigo:

1. **Primero:** Cambio la versión a 1.2.0-beta y compilo
2. **Segundo:** Te ayudo a publicar en GitHub
3. **Tercero:** Restauramos a 1.1.0 y probamos

**¿Procedemos con el PASO 1?** 🎯
