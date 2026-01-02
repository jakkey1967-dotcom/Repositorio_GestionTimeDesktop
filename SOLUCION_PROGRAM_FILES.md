# ⚡ Solución Express: App Instalada en Program Files No Arranca

## 🎯 Tu Situación

✅ Instalaste en: **`C:\Program Files\GestionTime`**  
❌ Al ejecutar `GestionTime.Desktop.exe` → No pasa nada

---

## 🚀 Solución en 30 Segundos

### Método 1: Ejecutar como Administrador

1. **Abrir carpeta de instalación:**
   ```
   C:\Program Files\GestionTime
   ```

2. **Click derecho** en `GestionTime.Desktop.exe`

3. **Seleccionar:** "Ejecutar como administrador"

4. **Aceptar** el mensaje de UAC (Control de Cuentas)

5. **✅ Listo** - La aplicación debería abrir

> **Nota:** Solo necesitas hacer esto la primera vez. Después podrás ejecutar normalmente.

---

### Método 2: Script Automático de Diagnóstico

Si tienes PowerShell:

```powershell
# 1. Abrir PowerShell como Administrador
# (Click derecho en PowerShell → Ejecutar como administrador)

# 2. Navegar a la carpeta
cd "C:\Program Files\GestionTime"

# 3. Ejecutar diagnóstico
.\Diagnostico-Program-Files.ps1
```

Este script:
- ✅ Verifica dependencias
- ✅ Crea carpetas necesarias
- ✅ Desbloquea archivos
- ✅ Instala Windows App Runtime (si falta)
- ✅ Te ofrece ejecutar la app

---

## 🤔 ¿Por Qué Pasa Esto?

Windows protege la carpeta `Program Files` con permisos especiales.

**La aplicación necesita:**
- Crear carpeta `logs` para registros
- Escribir archivos de configuración
- Guardar datos temporales

**Sin permisos de Admin:**
- ❌ No puede crear carpetas
- ❌ No puede escribir archivos
- ❌ Falla silenciosamente (no muestra error)

---

## 💡 Solución Definitiva (Recomendada)

**Mover la aplicación a una ubicación sin restricciones:**

### Opción A: Carpeta de Usuario (Recomendado)

```powershell
# Abrir PowerShell como Administrador
Move-Item "C:\Program Files\GestionTime" "$env:USERPROFILE\GestionTime"
```

**Nueva ubicación:** `C:\Users\TuUsuario\GestionTime`

### Opción B: Otro Disco

```powershell
# Si tienes disco D:
Move-Item "C:\Program Files\GestionTime" "D:\GestionTime"
```

### Opción C: Local App Data

```powershell
# Ubicación estándar para apps de usuario
Move-Item "C:\Program Files\GestionTime" "$env:LOCALAPPDATA\GestionTime"
```

**Nueva ubicación:** `C:\Users\TuUsuario\AppData\Local\GestionTime`

---

## 🔍 Verificar que Funcionó

Después de ejecutar como Admin o mover la app:

1. **Verificar carpeta logs creada:**
   ```
   C:\Program Files\GestionTime\logs
   ```
   
2. **Buscar emergency log:**
   ```
   C:\Users\TuUsuario\AppData\Local\GestionTime\emergency.log
   ```

3. **Si emergency.log existe:**
   - Ábrelo con Notepad
   - Muestra el error exacto
   - Comparte el contenido para ayuda específica

---

## ❓ Preguntas Frecuentes

### ¿Necesito ejecutar como Admin siempre?

**No.** Solo la primera vez. Después que la app cree sus carpetas, ya no será necesario.

### ¿Perderé datos si muevo la carpeta?

**No.** Todo se moverá intacto. La aplicación funcionará igual.

### ¿Cuál es la mejor ubicación?

**Recomendación:**
1. 🥇 `C:\Users\TuUsuario\GestionTime` (sin problemas de permisos)
2. 🥈 `D:\GestionTime` (si tienes otro disco)
3. 🥉 `C:\Program Files\GestionTime` (requiere Admin primera vez)

---

## 🆘 Si Aún No Funciona

### Ver Emergency Log

```powershell
notepad "$env:LOCALAPPDATA\GestionTime\emergency.log"
```

### Ver Event Viewer

```powershell
eventvwr.msc
# Ir a: Windows Logs → Application
# Buscar errores de ".NET Runtime" o "GestionTime"
```

### Ejecutar Diagnóstico Completo

```powershell
cd "C:\Program Files\GestionTime"
.\Verificar-Instalacion.ps1
```

---

## 📞 Información para Soporte

Si necesitas ayuda, proporciona:

1. **Output de:**
   ```powershell
   winget list --id Microsoft.WindowsAppRuntime.1.8
   ```

2. **Contenido de:**
   ```
   %LOCALAPPDATA%\GestionTime\emergency.log
   ```

3. **Versión de Windows:**
   ```
   Ejecutar: winver
   ```

---

## ✅ Checklist Rápido

- [ ] Ejecuté como Administrador
- [ ] Creó carpeta `logs` en `C:\Program Files\GestionTime`
- [ ] Revisé `emergency.log` en `AppData\Local\GestionTime`
- [ ] Verifiqué Event Viewer para errores
- [ ] Consideré mover a carpeta de usuario
- [ ] Instalé Windows App Runtime 1.8

---

**💡 Tip:** Si trabajas en equipo, recomienda instalar en `C:\Users\<usuario>\GestionTime` para evitar estos problemas.
