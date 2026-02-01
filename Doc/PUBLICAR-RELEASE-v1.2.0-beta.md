# 🚀 PUBLICAR RELEASE v1.2.0-beta EN GITHUB

## ✅ ARCHIVOS GENERADOS

- **Paquete:** `GestionTime-v1.2.0-beta.zip` (109.09 MB)
- **Ubicación:** `C:\GestionTime\GestionTimeDesktop\GestionTime-v1.2.0-beta.zip`
- **Versión en .csproj:** 
  - `AssemblyVersion`: 1.2.0.0
  - `FileVersion`: 1.2.0.0
  - `Version`: 1.2.0-beta

---

## 📋 PASOS PARA PUBLICAR EN GITHUB

### 1. Ir a la página de Releases
```
https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases/new
```

### 2. Configurar el Release

**Tag version:**
```
v1.2.0-beta
```

**Release title:**
```
GestionTime Desktop v1.2.0-beta
```

**Descripción sugerida:**
```markdown
## 🎯 GestionTime Desktop v1.2.0-beta

### ✨ Novedades en esta versión
- 🔄 Sistema de verificación de actualizaciones automático
- 📊 Mejoras en la exportación semanal de partes
- 🎨 Optimizaciones de rendimiento en la interfaz
- 🔧 Mejoras internas en el código

### 🐛 Correcciones
- Corrección de errores menores
- Mejoras en la estabilidad general

### 📦 Instalación
1. Descarga el archivo `GestionTime-v1.2.0-beta.zip`
2. Extrae el contenido en una carpeta temporal
3. Ejecuta `INSTALAR.bat` como Administrador
4. Sigue las instrucciones en pantalla

### ⚠️ Nota Importante
Esta es una **versión BETA** para pruebas. Si encuentras algún problema, por favor repórtalo en Issues.

### 📋 Requisitos del Sistema
- Windows 10 (1809+) / Windows 11
- Procesador x64 (64-bit)
- RAM: 4 GB
- Espacio en disco: 500 MB

### 🔗 Más Información
- [Documentación de instalación](https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/blob/main/Installer/README.txt)
- [Guía de uso](https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop)
```

### 3. Configuración Adicional

**Marcar como:**
- ✅ **Set as a pre-release** (porque es BETA)
- ⬜ **Set as the latest release** (dejar desmarcado si 1.1.0 es la estable)

### 4. Adjuntar el Archivo

**Arrastra o selecciona:**
```
GestionTime-v1.2.0-beta.zip
```

### 5. Publicar

Haz clic en **"Publish release"**

---

## 🧪 PROBAR LA ACTUALIZACIÓN

### Con la app en versión 1.1.0:

1. **Cambiar temporalmente a 1.1.0:**
   ```xml
   <!-- En GestionTime.Desktop.csproj -->
   <AssemblyVersion>1.1.0.0</AssemblyVersion>
   <FileVersion>1.1.0.0</FileVersion>
   <Version>1.1.0</Version>
   ```

2. **Compilar y ejecutar:**
   ```powershell
   dotnet clean GestionTime.Desktop.csproj
   dotnet build GestionTime.Desktop.csproj -c Debug
   dotnet run --project GestionTime.Desktop.csproj
   ```

3. **Resultado esperado:**
   - La app detectará la versión **1.2.0-beta** disponible
   - Mostrará una notificación de actualización
   - Al hacer clic en "Ver detalles" abrirá la página de releases

4. **Restaurar la versión:**
   ```xml
   <AssemblyVersion>1.2.0.0</AssemblyVersion>
   <FileVersion>1.2.0.0</FileVersion>
   <Version>1.2.0-beta</Version>
   ```

---

## 📊 CONTENIDO DEL PAQUETE

```
GestionTime-v1.2.0-beta.zip
├── App/
│   ├── GestionTime.Desktop.exe (0.52 MB)
│   ├── GestionTime.Desktop.pri (1.37 MB) ✅
│   ├── GestionTime.Desktop.dll (2.09 MB)
│   ├── appsettings.json ✅
│   ├── Assets/ (14 archivos) ✅
│   └── [355+ archivos DLL y dependencias]
├── INSTALAR.bat
├── README.txt
└── License.rtf
```

**Total:** ~109 MB comprimido, ~280 MB instalado

---

## 🔍 VERIFICACIONES REALIZADAS

✅ Compilación exitosa en modo Release  
✅ Publicación con ReadyToRun (optimización)  
✅ Self-contained (incluye .NET 8 Runtime)  
✅ Archivo .pri copiado correctamente  
✅ Carpeta Assets con 14 imágenes  
✅ appsettings.json incluido  
✅ ZIP creado con compresión óptima  

---

## 🎯 ENDPOINTS DE ACTUALIZACIÓN

La app verificará automáticamente:

**API de GitHub:**
```
https://api.github.com/repos/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases/latest
```

**Respuesta esperada:**
```json
{
  "tag_name": "v1.2.0-beta",
  "name": "GestionTime Desktop v1.2.0-beta",
  "assets": [
    {
      "name": "GestionTime-v1.2.0-beta.zip",
      "browser_download_url": "https://github.com/.../GestionTime-v1.2.0-beta.zip"
    }
  ]
}
```

---

## 📝 NOTAS FINALES

- **Versión actual en .csproj:** 1.2.0-beta
- **Esta es una versión BETA:** No se marcará como "latest release"
- **Usuarios con 1.1.0:** Recibirán notificación de actualización
- **Instalación:** Se instalará en `C:\app\gestiontime-desktop\`

---

## 🚀 SIGUIENTE PASO

Cuando la versión beta esté probada y lista para producción:

1. Cambiar a versión estable:
   ```xml
   <Version>1.2.0</Version>
   ```

2. Publicar release sin marcar "pre-release"
3. Marcar como "Set as the latest release"

---

**¡Listo para publicar! 🎉**
