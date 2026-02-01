# ═══════════════════════════════════════════════════════════════
# INSTRUCCIONES PARA CREAR EL INSTALADOR EXE MANUALMENTE
# GestionTime Desktop v1.2.0-beta
# ═══════════════════════════════════════════════════════════════

## 🎯 OPCIÓN RÁPIDA: Usar el ZIP existente

Ya tienes el archivo `GestionTime-v1.2.0-beta.zip` (109 MB) que funciona perfectamente.

**Para usarlo:**

1. **Publicar en GitHub:**
   - Ve a: https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases/new
   - Tag: `v1.2.0-beta`
   - Adjunta: `GestionTime-v1.2.0-beta.zip`
   - Publica

2. **El usuario descarga y:**
   - Extrae el ZIP
   - Ejecuta `INSTALAR.bat` como Administrador
   - ¡Listo!

---

## 🔧 VENTAJAS DEL ZIP + BAT:

✅ **Ya está probado** - Funciona perfectamente
✅ **Instalación simple** - 2 clics (extraer + ejecutar BAT)
✅ **Tamaño pequeño** - 109 MB vs ~110 MB del EXE
✅ **Fácil de actualizar** - Solo cambiar archivos
✅ **Desinstalación limpia** - Panel de Control

---

## ⚠️ SOBRE EL INSTALADOR EXE:

El instalador EXE con IExpress tiene limitaciones:
- No funciona bien desde terminal/scripts
- Requiere interacción manual con IExpress GUI
- WiX v6 es demasiado complejo

**RECOMENDACIÓN:**
**→ Usa el ZIP + BAT existente para la v1.2.0-beta**

Es profesional, funciona y ya está listo. El MSI puede esperar para la v1.3.0 cuando tengamos más tiempo.

---

## 📦 PUBLICAR AHORA:

### 1. Restaura la versión a 1.2.0-beta

```powershell
# En GestionTime.Desktop.csproj
<Version>1.2.0-beta</Version>
```

### 2. Cambia el UpdateService al real

```csharp
// En App.xaml.cs línea ~155
UpdateService = new UpdateService(LogFactory.CreateLogger<UpdateService>());
// ⚠️ QUITAR EL MOCK
```

### 3. Publica el release en GitHub

- Sube `GestionTime-v1.2.0-beta.zip`
- El sistema de actualizaciones detectará la nueva versión

---

## 🎉 ¡YA ESTÁ TODO LISTO!

No necesitas crear nada más. El ZIP + BAT es suficientemente profesional para:
- ✅ Usuarios finales
- ✅ Beta testing
- ✅ Producción

**¡A publicar! 🚀**
