# 🔄 GUÍA: Migrar a Nuevo Repositorio GitHub

## 📋 Situación Actual

Tienes múltiples repositorios duplicados en GitHub:
- ❌ `jakkey1967-dotcom/Repository-Git` (eliminar)
- ❌ `jakkey1967-dotcom/Repositorio_Gestion...` (eliminar)
- ❌ `jakkey1967-dotcom/GestionTime.desktop...` (eliminar)
- ❌ `jakkey1967-dotcom/GestionTime.Desktop...` (el actual - eliminar)
- ✅ `GestionTimeApi` (**NO TOCAR**)

**Objetivo:** Crear un repositorio limpio `GestionTime.Desktop` y eliminar los demás.

---

## 🚀 Método 1: Script Automatizado (Recomendado)

### **Paso 1: Crear Repositorio en GitHub**

1. Ve a: https://github.com/new
2. **Nombre:** `GestionTime.Desktop`
3. **Descripción:** `Aplicación desktop WinUI 3 para gestión de partes de trabajo`
4. **Privado**
5. ✅ **NO marcar** "Initialize with README"
6. Click **"Create repository"**

### **Paso 2: Ejecutar Script de Migración**

```powershell
# Desde C:\GestionTime\GestionTime.Desktop\
.\migrate-to-new-repo.ps1
```

El script hará:
1. ✅ Crear backup de tu trabajo actual
2. ✅ Cambiar el remote al nuevo repositorio
3. ✅ Hacer push de todo tu código
4. ✅ Darte instrucciones para eliminar repos viejos

---

## 🛠️ Método 2: Manual (Paso a Paso)

### **Paso 1: Crear Repositorio en GitHub**

(Igual que arriba)

### **Paso 2: Hacer Commit de Cambios Actuales**

```powershell
# Ver cambios pendientes
git status

# Agregar todos los cambios
git add -A

# Hacer commit
git commit -m "Migración a nuevo repositorio - $(Get-Date -Format 'yyyy-MM-dd')"
```

### **Paso 3: Cambiar Remote**

```powershell
# Ver remote actual
git remote -v

# Eliminar remote viejo
git remote remove origin

# Agregar nuevo remote
git remote add origin https://github.com/jakkey1967-dotcom/GestionTime.Desktop.git

# Verificar
git remote -v
```

### **Paso 4: Push Inicial**

```powershell
# Push de la rama main
git push -u origin main --force

# Push de todas las ramas (si hay otras)
git push origin --all --force

# Push de tags (si hay)
git push origin --tags --force
```

### **Paso 5: Verificar en GitHub**

Ve a: https://github.com/jakkey1967-dotcom/GestionTime.Desktop

Deberías ver:
- ✅ Todo tu código
- ✅ Historial de commits
- ✅ Todas las ramas
- ✅ README.md, appsettings.json, etc.

---

## 🗑️ Eliminar Repositorios Viejos

### **Para cada repositorio a eliminar:**

1. Ve al repositorio en GitHub:
   - https://github.com/jakkey1967-dotcom/Repository-Git
   - https://github.com/jakkey1967-dotcom/Repositorio_Gestion...
   - https://github.com/jakkey1967-dotcom/GestionTime.desktop...
   - El viejo GestionTime.Desktop (si existe otro)

2. Click en **"Settings"** (arriba a la derecha)

3. Scroll hasta el final de la página

4. En la sección **"Danger Zone"**, click en **"Delete this repository"**

5. Confirma escribiendo el nombre completo del repositorio

6. Click **"I understand the consequences, delete this repository"**

7. Repetir para cada repositorio

---

## ✅ Verificación Final

### **1. Verificar Remote Local**

```powershell
git remote -v
```

Debe mostrar:
```
origin  https://github.com/jakkey1967-dotcom/GestionTime.Desktop.git (fetch)
origin  https://github.com/jakkey1967-dotcom/GestionTime.Desktop.git (push)
```

### **2. Verificar GitHub**

Ve a: https://github.com/jakkey1967-dotcom

Debes ver:
- ✅ `GestionTime.Desktop` (el nuevo)
- ✅ `GestionTimeApi` (sin tocar)
- ❌ Los demás eliminados

### **3. Hacer un Test Push**

```powershell
# Crear archivo de prueba
echo "# Test" > TEST.md

# Commit y push
git add TEST.md
git commit -m "Test push al nuevo repo"
git push

# Eliminar archivo de prueba
git rm TEST.md
git commit -m "Remover archivo de prueba"
git push
```

---

## 🔐 Autenticación en GitHub

Si te pide usuario/contraseña al hacer push:

### **Opción A: Personal Access Token (Recomendado)**

1. Ve a: https://github.com/settings/tokens
2. Click **"Generate new token"** → **"Generate new token (classic)"**
3. **Note:** `GestionTime Desktop`
4. **Expiration:** 90 días (o sin expiración)
5. **Scopes:** Marca `repo` (acceso completo a repositorios)
6. Click **"Generate token"**
7. **COPIA EL TOKEN** (solo se muestra una vez)

Cuando hagas `git push`, usa:
- **Username:** `jakkey1967-dotcom`
- **Password:** `[el token que copiaste]`

### **Opción B: GitHub CLI**

```powershell
# Instalar GitHub CLI
winget install --id GitHub.cli

# Autenticar
gh auth login
```

Sigue las instrucciones en pantalla.

---

## 📊 Comparación de Repositorios

| Repositorio | Estado | Acción |
|---|---|---|
| `Repository-Git` | ❌ Viejo | **ELIMINAR** |
| `Repositorio_Gestion...` | ❌ Viejo | **ELIMINAR** |
| `GestionTime.desktop...` | ❌ Duplicado | **ELIMINAR** |
| `GestionTime.Desktop` (viejo) | ❌ Con problemas | **ELIMINAR** |
| **GestionTime.Desktop** (nuevo) | ✅ Limpio | **MANTENER** |
| `GestionTimeApi` | ✅ Backend | **NO TOCAR** |

---

## 🆘 Solución de Problemas

### **Error: "remote origin already exists"**

```powershell
git remote remove origin
git remote add origin https://github.com/jakkey1967-dotcom/GestionTime.Desktop.git
```

### **Error: "failed to push some refs"**

```powershell
# Forzar push (solo si es repo nuevo)
git push -u origin main --force
```

### **Error: "Authentication failed"**

Usa Personal Access Token en lugar de contraseña (ver sección de Autenticación arriba).

### **Perdí mi código**

Si ejecutaste el script automatizado, hay un backup en una rama:
```powershell
# Ver ramas
git branch

# Cambiar a rama de backup
git checkout backup-old-repo-YYYYMMDD-HHMMSS
```

---

## 📝 Checklist Final

Antes de eliminar los repositorios viejos, verifica:

- [ ] El nuevo repositorio `GestionTime.Desktop` existe en GitHub
- [ ] Hiciste push de todo tu código
- [ ] El remote local apunta al nuevo repositorio
- [ ] Puedes hacer `git push` sin errores
- [ ] Verificaste que todos los archivos están en GitHub
- [ ] Hiciste backup local por si acaso

**Solo después de verificar todo, elimina los repositorios viejos.**

---

## 🎯 Resultado Esperado

**Antes:**
```
jakkey1967-dotcom/
├── Repository-Git (confusión)
├── Repositorio_Gestion... (duplicado)
├── GestionTime.desktop... (duplicado)
├── GestionTime.Desktop (con problemas)
└── GestionTimeApi ✅
```

**Después:**
```
jakkey1967-dotcom/
├── GestionTime.Desktop ✅ (nuevo y limpio)
└── GestionTimeApi ✅ (sin tocar)
```

---

**Fecha:** 2025-01-27  
**Archivo:** `GUIA_MIGRACION_REPO.md`
