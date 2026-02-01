# 🚀 PUBLICAR RELEASE v1.2.0-beta EN GITHUB

## ⏱️ TIEMPO: 5 minutos

---

## 📦 PASO 1: IR A GITHUB RELEASES

Abre en tu navegador:
```
https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases/new
```

---

## 📝 PASO 2: CONFIGURAR EL RELEASE

### A. Tag version:
```
v1.2.0-beta
```

### B. Release title:
```
GestionTime Desktop v1.2.0-beta
```

### C. Describe this release:
```markdown
## 🎯 GestionTime Desktop v1.2.0-beta

### ✨ Novedades
- 🔄 Sistema de actualizaciones automático
- 📊 Versión visible en el login
- 🎨 Mejoras en la interfaz
- 📦 Instalador MSI profesional

### 📦 Instalación
1. Descarga `GestionTime-v1.2.0-beta.msi`
2. Ejecuta como Administrador
3. Sigue las instrucciones del asistente

### 🗑️ Desinstalación
- Windows 11: Configuración > Aplicaciones > GestionTime Desktop > Desinstalar
- Windows 10: Panel de Control > Programas > Desinstalar un programa

### ⚠️ Requisitos del Sistema
- Windows 10 (versión 1809 o superior) / Windows 11
- Arquitectura: x64 (64-bit)
- Espacio en disco: ~300 MB
- .NET 8 Runtime (incluido en el instalador)

### 🐛 Problemas conocidos
- Primera versión beta - reporta cualquier problema en Issues

### 📞 Soporte
- Repositorio: https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop
- Issues: https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/issues
```

---

## 📤 PASO 3: ADJUNTAR ARCHIVO

Arrastra y suelta el archivo:
```
C:\GestionTime\GestionTimeDesktop\installers\GestionTime-1.2.0-beta.msi
```

**Tamaño esperado:** ~27.8 MB

---

## ⚙️ PASO 4: CONFIGURAR OPCIONES

✅ Marca: **"Set as a pre-release"** (porque es beta)

❌ NO marques: "Set as the latest release" (todavía)

---

## 🚀 PASO 5: PUBLICAR

Clic en el botón verde: **"Publish release"**

---

## ✅ PASO 6: VERIFICAR

Una vez publicado, verifica que el release esté visible en:
```
https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases
```

Deberías ver:
- ✅ Tag: v1.2.0-beta
- ✅ Título: GestionTime Desktop v1.2.0-beta
- ✅ Badge: "Pre-release"
- ✅ Archivo adjunto: GestionTime-1.2.0-beta.msi (~27.8 MB)

---

## 🧪 PASO 7: PROBAR ACTUALIZACIONES

Ahora SÍ funcionará el sistema de actualizaciones:

1. **Cierra la app** si está ejecutándose

2. **Ejecuta de nuevo:**
   ```powershell
   cd C:\GestionTime\GestionTimeDesktop
   dotnet run --project GestionTime.Desktop.csproj -c Debug
   ```

3. **Espera 5 segundos** después de que aparezca el login

4. **Debería aparecer el diálogo:**
   ```
   🔔 Actualización disponible
   
   ¡Hay una nueva versión disponible!
   
   • Versión actual: 1.1.0
   • Nueva versión: 1.2.0-beta
   
   ¿Deseas abrir la página de descargas?
   
   [📥 Descargar]  [Más tarde]
   ```

5. **Si haces clic en "Descargar":**
   - Se abrirá tu navegador en GitHub Releases
   - Podrás descargar el MSI

---

## 🎉 ¡LISTO!

Una vez que publiques el release:
- ✅ El sistema de actualizaciones funcionará
- ✅ Los usuarios podrán descargar la nueva versión
- ✅ La app detectará automáticamente nuevas versiones

---

## ⚠️ IMPORTANTE

**NO OLVIDES:**
- Después de probar, compila la versión 1.2.0-beta para distribuir
- Cambia la versión del proyecto de 1.1.0 a 1.2.0
- Publica esa versión actualizada

**PARA CAMBIAR A 1.2.0:**
```xml
<!-- En GestionTime.Desktop.csproj -->
<AssemblyVersion>1.2.0.0</AssemblyVersion>
<FileVersion>1.2.0.0</FileVersion>
<Version>1.2.0-beta</Version>
```
