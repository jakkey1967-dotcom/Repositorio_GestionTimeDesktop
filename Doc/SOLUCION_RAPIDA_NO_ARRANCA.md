# 🔥 Solución Rápida: Aplicación No Arranca

## ⚡ Problema
La aplicación se instala pero no abre ninguna ventana cuando ejecutas `GestionTime.Desktop.exe`.

---

## 🎯 Solución en 3 Pasos

### 1️⃣ Ejecuta el Script de Verificación

```powershell
# Abre PowerShell en la carpeta de la aplicación y ejecuta:
.\Verificar-Instalacion.ps1
```

Este script verificará automáticamente:
- ✅ Windows App Runtime
- ✅ Archivos necesarios
- ✅ Permisos
- ✅ Configuración

---

### 2️⃣ Instala Windows App Runtime (SI ES NECESARIO)

Si el script indica que falta, ejecuta:

```powershell
winget install Microsoft.WindowsAppRuntime.1.8
```

**O descarga manualmente:**
https://aka.ms/windowsappsdk/1.8/latest/windowsappruntimeinstall-x64.exe

---

### 3️⃣ Desbloquea Archivos

```powershell
# Desbloquear todos los archivos descargados
Get-ChildItem -Recurse | Unblock-File
```

---

## 🔍 Si Aún No Arranca

### Ver el Log de Emergencia

El log de emergencia contiene información crítica sobre por qué falló:

```powershell
# Abrir el log
notepad "$env:LOCALAPPDATA\GestionTime\emergency.log"
```

### Ver Event Viewer de Windows

1. Presiona `Win + R`
2. Escribe: `eventvwr.msc`
3. Ve a: **Windows Logs → Application**
4. Busca errores recientes de "GestionTime" o ".NET Runtime"

---

## 💡 Causas Comunes y Soluciones

| Problema | Síntoma | Solución |
|----------|---------|----------|
| **Windows App Runtime faltante** | No pasa nada al ejecutar | Instalar desde winget o descarga manual |
| **Archivos bloqueados** | Windows bloquea archivos descargados | `Unblock-File` o desbloquear manualmente |
| **Sin permisos** | No puede crear carpeta logs | Ejecutar como Administrador (una vez) |
| **appsettings.json faltante** | No conecta a API | Copiar desde el ZIP completo |
| **DLLs faltantes** | Crash silencioso | Extraer TODOS los archivos del ZIP |

---

## 🚀 Ejecutar Como Administrador (Primera Vez)

Si todo lo demás falla:

1. Click derecho en `GestionTime.Desktop.exe`
2. Seleccionar **"Ejecutar como administrador"**
3. Aceptar el UAC prompt
4. Después de la primera ejecución exitosa, ya no será necesario

---

## 📝 Comandos Útiles para Diagnóstico

```powershell
# Ver si el proceso arrancó
Get-Process | Where-Object {$_.ProcessName -like "*GestionTime*"}

# Ver versión de Windows
winver

# Ver .NET instalado (si usas framework-dependent)
dotnet --list-runtimes

# Ver Windows App Runtime instalado
winget list | Select-String "WindowsAppRuntime"

# Ver logs de la aplicación (si logró crearlos)
Get-Content -Path "logs\app.log" -Tail 50
```

---

## 🆘 Última Opción: Recompilar desde Código

Si tienes acceso al código fuente:

```powershell
# Limpiar y recompilar
dotnet clean
dotnet build -c Release
dotnet publish -c Release -r win-x64 --self-contained true
```

---

## 📞 Obtener Ayuda

Si nada de esto funciona, necesitarás compartir:

1. **Output de** `Verificar-Instalacion.ps1`
2. **Contenido de** `emergency.log`
3. **Errores de Event Viewer** (si hay)
4. **Versión de Windows** (`winver`)

Con esta información se puede diagnosticar el problema específico.

---

## ✅ Checklist Final

Antes de pedir ayuda, verifica:

- [ ] Windows 10 versión 1809+ o Windows 11
- [ ] Windows App Runtime 1.8 instalado
- [ ] Todos los archivos extraídos del ZIP
- [ ] Archivos desbloqueados (sin Zone.Identifier)
- [ ] `appsettings.json` existe y está bien formado
- [ ] Carpeta `logs` existe o puede crearse
- [ ] Ejecutado como Administrador al menos una vez
- [ ] `emergency.log` revisado
- [ ] Event Viewer revisado

---

**💡 Nota:** El 95% de los casos se resuelven instalando Windows App Runtime y desbloqueando archivos.
