# 🚀 QUICK START - GENERAR MSI

## ⚡ Método Rápido (1 comando)

```powershell
.\Scripts\Build-MSI-Local.ps1
```

**¿Qué hace?**
1. ✅ Compila el proyecto en Release
2. ✅ Publica en `publish\portable`
3. ✅ Genera MSI en `installers\`
4. ✅ Abre la carpeta automáticamente

---

## 📂 Resultado

```
installers\GestionTime-v1.9.0-win-x64.msi
```

**Tamaño**: ~108 MB  
**Instala en**: `C:\App\GestionTime-Desktop\`  
**Backend**: Render (https://gestiontimeapi.onrender.com)

---

## 🔍 Verificar Antes

Si quieres verificar prerequisitos primero:

```powershell
.\Scripts\Verify-MSI-Prerequisites.ps1
```

---

## 📖 Documentación Completa

Ver: `Docs\BUILD_MSI_LOCAL.md`
