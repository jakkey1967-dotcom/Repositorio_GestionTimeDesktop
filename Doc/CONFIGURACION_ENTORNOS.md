# 🔧 CONFIGURACIÓN DE ENTORNOS - Local vs Producción

## 📅 Fecha: 2025-01-25

---

## ✅ SISTEMA IMPLEMENTADO

Ahora puedes cambiar fácilmente entre:
- **Backend LOCAL** (localhost:5000)
- **Backend PRODUCCIÓN** (Render)

---

## 🚀 CÓMO USAR

### **OPCIÓN 1: Usar Script Automático (RECOMENDADO)**

#### **A) Cambiar a LOCAL:**
```powershell
.\tmp\Switch-Environment.ps1 -Environment Local
```

#### **B) Cambiar a PRODUCCIÓN:**
```powershell
.\tmp\Switch-Environment.ps1 -Environment Production
```

---

### **OPCIÓN 2: Manual**

#### **A) Para trabajar en LOCAL:**
1. Copia `appsettings.Development.json` → `appsettings.json`
2. Asegúrate de que el backend local esté corriendo:
   ```powershell
   cd ..\GestionTimeApi
   dotnet run
   ```
3. Ejecuta la aplicación:
   ```powershell
   dotnet run
   ```

#### **B) Para trabajar en PRODUCCIÓN:**
1. Restaura el `appsettings.json` original
2. O edita manualmente:
   ```json
   {
     "Api": {
       "BaseUrl": "https://gestiontimeapi.onrender.com",
       ...
     }
   }
   ```

---

## 📝 ARCHIVOS DE CONFIGURACIÓN

### **appsettings.json** (Activo)
- Este es el archivo que la aplicación LEE
- Se usa en desarrollo y producción
- ⚠️ **NO commitear con configuración local**

### **appsettings.Development.json** (Template LOCAL)
- Configuración para backend local
- BaseUrl: `http://localhost:5000`
- ⚠️ **NO commitear** (está en .gitignore)

### **appsettings.backup.json**
- Backup automático del archivo anterior
- Se crea al usar el script
- ⚠️ **NO commitear** (está en .gitignore)

---

## 🔍 CONFIGURACIÓN ACTUAL

### **PRODUCCIÓN (Render):**
```json
{
  "Api": {
    "BaseUrl": "https://gestiontimeapi.onrender.com",
    "LoginPath": "/api/v1/auth/login-desktop",
    "PartesPath": "/api/v1/partes"
  }
}
```

### **LOCAL (Desarrollo):**
```json
{
  "Api": {
    "BaseUrl": "http://localhost:2501",
    "LoginPath": "/api/v1/auth/login-desktop",
    "PartesPath": "/api/v1/partes"
  }
}
```

**Nota:** Tu API local corre en:
- HTTP: `http://localhost:2501`
- HTTPS: `https://localhost:2502`

---

## ✅ VERIFICACIÓN

### **Cómo saber qué entorno estás usando:**

1. **Al iniciar la aplicación**, verás en los logs:
   ```
   ═══════════════════════════════════════════════════════════════
   🌐 CONFIGURACIÓN DE API:
      BaseUrl: http://localhost:5000
      LoginPath: /api/v1/auth/login-desktop
      PartesPath: /api/v1/partes
   ═══════════════════════════════════════════════════════════════
   ```

2. **En Visual Studio Output:**
   ```
   === CONFIGURACIÓN CARGADA ===
   BaseUrl: 'http://localhost:5000'
   LoginPath: '/api/v1/auth/login-desktop'
   ```

---

## 🛠️ FLUJO DE TRABAJO RECOMENDADO

### **Durante Desarrollo:**

```powershell
# 1. Cambiar a LOCAL
.\tmp\Switch-Environment.ps1 -Environment Local

# 2. Iniciar backend local (en otra terminal)
cd ..\GestionTimeApi
dotnet run

# 3. Iniciar frontend
dotnet run

# 4. Hacer cambios y probar
# ...

# 5. Cuando termines, volver a PRODUCCIÓN
.\tmp\Switch-Environment.ps1 -Environment Production
```

---

## ⚠️ IMPORTANTE

### **Antes de Commitear:**
1. ✅ Asegúrate de que `appsettings.json` apunta a PRODUCCIÓN
2. ✅ Verifica que `appsettings.Development.json` NO se suba (está en .gitignore)
3. ✅ Ejecuta el script de vuelta a producción antes de push:
   ```powershell
   .\tmp\Switch-Environment.ps1 -Environment Production
   ```

### **Archivos en .gitignore:**
```
appsettings.Development.json
appsettings.backup.json
```

Estos archivos **NO se suben** al repositorio para evitar conflictos.

---

## 🐛 TROUBLESHOOTING

### **Error: "No se puede conectar a localhost"**
- ✅ Verifica que el backend esté corriendo en `http://localhost:5000`
- ✅ Ejecuta: `cd ..\GestionTimeApi && dotnet run`

### **Error: "appsettings.Development.json no existe"**
- ✅ Créalo manualmente copiando el template de este documento

### **Error: "Sigo usando producción aunque cambié a local"**
- ✅ Verifica el contenido de `appsettings.json` (no Development)
- ✅ Reinicia la aplicación

---

## 📚 REFERENCIAS

- **Script:** `tmp/Switch-Environment.ps1`
- **Config Producción:** `appsettings.json` (commitear)
- **Config Local:** `appsettings.Development.json` (NO commitear)
- **Código:** `App.xaml.cs` línea ~158

---

## 🎯 RESUMEN

```powershell
# QUICK START:

# 1. Cambiar a LOCAL para desarrollo
.\tmp\Switch-Environment.ps1 -Environment Local

# 2. Iniciar backend local
cd ..\GestionTimeApi && dotnet run

# 3. Iniciar frontend (en otra terminal)
dotnet run

# 4. Volver a PRODUCCIÓN antes de commitear
.\tmp\Switch-Environment.ps1 -Environment Production
```

---

**Creado:** 2025-01-25  
**Proyecto:** GestionTime Desktop v1.5.0-beta  
**Estado:** ✅ Sistema de entornos implementado
