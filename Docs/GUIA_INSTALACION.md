# ?? GUÍA DE INSTALACIÓN - GestionTime Desktop

**Fecha:** 2025-01-27  
**Versión:** 1.0.0  
**Plataforma:** Windows 10/11 (x64)

---

## ?? OPCIONES DE DISTRIBUCIÓN

Tienes **3 opciones** para distribuir la aplicación:

| Opción | Ventajas | Desventajas | Recomendado para |
|--------|----------|-------------|------------------|
| **MSIX** ? | Instalación moderna, actualizaciones automáticas, sandboxing | Requiere Windows 10+ | **Producción** |
| **Setup.exe** | Compatible con Windows 7+, familiar | Requiere herramientas adicionales | Compatibilidad |
| **Portable** | Sin instalación, USB-friendly | Sin acceso a APIs avanzadas | Testing rápido |

---

## ?? OPCIÓN 1: MSIX (RECOMENDADO)

### **Paso 1: Preparar el Proyecto**

Tu proyecto ya tiene habilitado `EnableMsixTooling`. Solo necesitas configurar el manifiesto.

#### **1.1 Buscar/Crear Package.appxmanifest**

```powershell
# Buscar el archivo
dir Package.appxmanifest -Recurse
```

Si no existe, Visual Studio lo creará automáticamente al publicar.

#### **1.2 Configurar Información del Paquete**

Edita `Package.appxmanifest` (se abrirá en el editor visual):

```xml
<?xml version="1.0" encoding="utf-8"?>
<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
         xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
         xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities">
  
  <Identity Name="GestionTime.Desktop"
            Publisher="CN=TuEmpresa"
            Version="1.0.0.0" />
  
  <Properties>
    <DisplayName>GestionTime</DisplayName>
    <PublisherDisplayName>Tu Empresa</PublisherDisplayName>
    <Logo>Assets\StoreLogo.png</Logo>
    <Description>Sistema de gestión de tiempo y partes de trabajo</Description>
  </Properties>
  
  <Dependencies>
    <TargetDeviceFamily Name="Windows.Desktop" 
                        MinVersion="10.0.17763.0" 
                        MaxVersionTested="10.0.22621.0" />
  </Dependencies>
  
  <Resources>
    <Resource Language="es-ES" />
  </Resources>
  
  <Applications>
    <Application Id="GestionTime.Desktop"
                 Executable="$targetnametoken$.exe"
                 EntryPoint="$targetentrypoint$">
      <uap:VisualElements
        DisplayName="GestionTime"
        Description="Sistema de gestión de tiempo"
        BackgroundColor="transparent"
        Square150x150Logo="Assets\Square150x150Logo.png"
        Square44x44Logo="Assets\Square44x44Logo.png">
        <uap:DefaultTile Wide310x150Logo="Assets\Wide310x150Logo.png" />
        <uap:SplashScreen Image="Assets\SplashScreen.png" />
      </uap:VisualElements>
    </Application>
  </Applications>
  
  <Capabilities>
    <Capability Name="internetClient" />
    <rescap:Capability Name="runFullTrust" />
  </Capabilities>
</Package>
```

---

### **Paso 2: Crear Certificado de Firma**

MSIX requiere un certificado para firmarse. Tienes 2 opciones:

#### **Opción A: Certificado de Prueba (Desarrollo)**

```powershell
# Crear certificado auto-firmado
New-SelfSignedCertificate -Type Custom `
  -Subject "CN=TuEmpresa" `
  -KeyUsage DigitalSignature `
  -FriendlyName "GestionTime Certificate" `
  -CertStoreLocation "Cert:\CurrentUser\My" `
  -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")
```

Luego exporta el certificado:

```powershell
# Encontrar el certificado
Get-ChildItem Cert:\CurrentUser\My | Where-Object {$_.Subject -match "TuEmpresa"}

# Exportar (copia el Thumbprint del comando anterior)
$cert = Get-ChildItem Cert:\CurrentUser\My\THUMBPRINT_AQUI
Export-Certificate -Cert $cert -FilePath "GestionTime.cer"
```

#### **Opción B: Certificado Comercial (Producción)**

Para producción, necesitas un certificado de una CA reconocida:
- **DigiCert** (~$300/año)
- **Sectigo** (~$200/año)
- **SSL.com** (~$150/año)

---

### **Paso 3: Publicar MSIX desde Visual Studio**

1. **Click derecho** en el proyecto ? **Publish** ? **Create App Packages**

2. **Seleccionar distribución:**
   - ? **Sideloading** (para distribuir manualmente)
   - ? Microsoft Store (si no vas a publicar en la tienda)

3. **Configurar ubicación:**
   ```
   C:\GestionTime\Publish\MSIX
   ```

4. **Seleccionar arquitectura:**
   - ? **x64** (recomendado)
   - ? x86 (opcional)
   - ? ARM64 (opcional)

5. **Configuración de versión:**
   ```
   Version: 1.0.0.0
   Automatically increment: ?
   ```

6. **Seleccionar certificado:**
   - Usar el que creaste en Paso 2
   - O crear uno nuevo desde el diálogo

7. **Click "Create"**

**Resultado:**
```
C:\GestionTime\Publish\MSIX\
??? GestionTime.Desktop_1.0.0.0_x64.msix
??? GestionTime.Desktop_1.0.0.0_x64_Test\
?   ??? GestionTime.Desktop_1.0.0.0_x64.msix
?   ??? Dependencies\
?   ??? Install.ps1
??? GestionTime.cer (certificado)
```

---

### **Paso 4: Instalar en el Cliente**

#### **Primera vez (Instalar certificado):**

```powershell
# Ejecutar como Administrador
Import-Certificate -FilePath "GestionTime.cer" -CertStoreLocation Cert:\LocalMachine\TrustedPeople
```

O hacer doble click en `GestionTime.cer` ? **Instalar certificado** ? **Equipo local** ? **Colocar todos los certificados en el siguiente almacén** ? **Personas de confianza**

#### **Instalar la aplicación:**

```powershell
# Opción 1: PowerShell
Add-AppxPackage -Path "GestionTime.Desktop_1.0.0.0_x64.msix"

# Opción 2: Script incluido
.\Install.ps1

# Opción 3: Doble click en el .msix
```

---

## ?? OPCIÓN 2: SETUP.EXE (TRADICIONAL)

Para crear un instalador .exe tradicional, usa **Inno Setup** o **WiX**.

### **Usando Inno Setup (Más fácil)**

1. **Descargar Inno Setup:**
   ```
   https://jrsoftware.org/isdl.php
   ```

2. **Publicar como Self-Contained:**

```powershell
cd C:\GestionTime\GestionTime.Desktop

dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish/win-x64
```

3. **Crear script Inno Setup** (`GestionTime.iss`):

```ini
#define MyAppName "GestionTime"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Tu Empresa"
#define MyAppURL "https://gestiontimeapi.onrender.com"
#define MyAppExeName "GestionTime.Desktop.exe"

[Setup]
AppId={{GUID-UNICO-AQUI}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=C:\GestionTime\Installers
OutputBaseFilename=GestionTime_Setup_v{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear icono en el escritorio"; GroupDescription: "Iconos adicionales:"
Name: "quicklaunchicon"; Description: "Crear icono en inicio rápido"; GroupDescription: "Iconos adicionales:"; Flags: unchecked

[Files]
Source: "C:\GestionTime\GestionTime.Desktop\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Desinstalar {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Ejecutar {#MyAppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{userappdata}\GestionTime"
Type: filesandordirs; Name: "{localappdata}\GestionTime"
```

4. **Compilar:**
   - Abrir `GestionTime.iss` en Inno Setup
   - Menu: **Build** ? **Compile**
   - Resultado: `C:\GestionTime\Installers\GestionTime_Setup_v1.0.0.exe`

---

## ?? OPCIÓN 3: PORTABLE (SIN INSTALACIÓN)

Ideal para testing o distribución rápida sin instalador.

### **Crear Versión Portable:**

```powershell
cd C:\GestionTime\GestionTime.Desktop

# Publicar self-contained
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o ./publish/portable
```

### **Empaquetar:**

```powershell
# Crear ZIP
Compress-Archive -Path ./publish/portable/* -DestinationPath "GestionTime_Portable_v1.0.0.zip"
```

**Contenido del ZIP:**
```
GestionTime_Portable_v1.0.0.zip
??? GestionTime.Desktop.exe
??? appsettings.json
??? *.dll (dependencias)
??? README.txt
```

**README.txt:**
```
GestionTime - Versión Portable

INSTRUCCIONES:
1. Extraer todo el contenido a una carpeta
2. Ejecutar GestionTime.Desktop.exe
3. Los logs se guardarán en: C:\Logs\GestionTime\

REQUISITOS:
- Windows 10/11 (x64)
- .NET 8 Desktop Runtime (se descarga automáticamente si no está instalado)

NOTAS:
- Esta es una versión portable, no requiere instalación
- Puede ejecutarse desde USB
- Los datos de configuración se guardan en el perfil del usuario
```

---

## ?? CONFIGURACIÓN POST-INSTALACIÓN

### **1. appsettings.json en Producción**

El archivo `appsettings.json` se copia automáticamente. Para producción, asegúrate de que tenga:

```json
{
  "Api": {
    "BaseUrl": "https://gestiontimeapi.onrender.com",
    "LoginPath": "/api/v1/auth/login",
    "PartesPath": "/api/v1/partes",
    "ClientesPath": "/api/v1/catalog/clientes",
    "GruposPath": "/api/v1/catalog/grupos",
    "TiposPath": "/api/v1/catalog/tipos",
    "MePath": "/api/v1/auth/me"
  }
}
```

### **2. Carpeta de Logs**

La aplicación crea automáticamente:
```
C:\Logs\GestionTime\
```

Si necesitas cambiar la ubicación, edita `appsettings.json`:

```json
{
  "Logging": {
    "LogPath": "C:\\MiEmpresa\\Logs\\GestionTime\\app.log"
  }
}
```

---

## ?? CHECKLIST PRE-DISTRIBUCIÓN

Antes de distribuir la aplicación, verifica:

### **Funcionalidad**
- [ ] Login funciona con API de Render
- [ ] CRUD de partes funciona
- [ ] Gráficas se muestran correctamente
- [ ] Logs se generan en la carpeta correcta
- [ ] Tema claro/oscuro funciona

### **Configuración**
- [ ] `appsettings.json` apunta a producción
- [ ] Certificado de firma válido (MSIX)
- [ ] Versión correcta en todos los archivos
- [ ] Assets (iconos, imágenes) incluidos

### **Testing**
- [ ] Probado en Windows 10
- [ ] Probado en Windows 11
- [ ] Instalación limpia funciona
- [ ] Actualización funciona (MSIX)
- [ ] Desinstalación limpia

---

## ?? DISTRIBUCIÓN

### **Distribución Interna (Empresa)**

1. **Servidor de archivos compartido:**
   ```
   \\servidor\compartido\GestionTime\
   ??? v1.0.0\
   ?   ??? GestionTime.Desktop_1.0.0.0_x64.msix
   ?   ??? GestionTime.cer
   ?   ??? Instrucciones.pdf
   ??? Latest\  (symlink a la última versión)
   ```

2. **Instrucciones para usuarios:**
   - Descargar de la carpeta compartida
   - Instalar certificado (solo primera vez)
   - Doble click en .msix

### **Distribución Externa (Clientes)**

1. **Portal web:**
   ```
   https://descargas.tuempresa.com/gestiontime/
   ```

2. **Email con link directo:**
   ```
   Estimado cliente,

   Descarga GestionTime desde:
   https://descargas.tuempresa.com/gestiontime/latest

   Instrucciones:
   1. Descargar GestionTime_Setup_v1.0.0.exe
   2. Ejecutar como administrador
   3. Seguir el asistente de instalación

   Soporte: soporte@tuempresa.com
   ```

---

## ?? ACTUALIZACIONES

### **MSIX (Automático)**

MSIX soporta actualizaciones automáticas. Configura en Visual Studio:

1. **Project** ? **Properties** ? **Package**
2. Habilitar **Auto-increment package version**
3. Configurar **Update settings**

Cada vez que publiques una nueva versión, los usuarios recibirán la actualización automáticamente.

### **Setup.exe (Manual)**

Crea un nuevo instalador con versión incrementada:
```
GestionTime_Setup_v1.0.1.exe
GestionTime_Setup_v1.0.2.exe
...
```

Los usuarios deben descargar e instalar manualmente.

---

## ?? COMPARACIÓN DE MÉTODOS

| Característica | MSIX | Setup.exe | Portable |
|----------------|------|-----------|----------|
| **Facilidad de instalación** | ????? | ???? | ????? |
| **Actualizaciones automáticas** | ? Sí | ? No | ? No |
| **Compatibilidad** | Win10+ | Win7+ | Win10+ |
| **Tamaño del instalador** | ~50MB | ~80MB | ~100MB |
| **Requiere admin** | ? No* | ? Sí | ? No |
| **Sandboxing** | ? Sí | ? No | ? No |
| **Certificado requerido** | ? Sí | ? No | ? No |
| **Desinstalación limpia** | ????? | ???? | ??? |

\* Requiere admin solo para instalar el certificado (primera vez)

---

## ?? TROUBLESHOOTING

### **Problema: "La aplicación no inicia"**

**Causa:** Falta .NET 8 Desktop Runtime

**Solución:**
```
Descargar e instalar:
https://dotnet.microsoft.com/download/dotnet/8.0/runtime
```

O publicar como **self-contained** (incluye runtime, pero pesa más).

---

### **Problema: "Error al instalar MSIX - Certificado no confiable"**

**Causa:** Certificado no instalado en "Personas de confianza"

**Solución:**
```powershell
# Ejecutar como Admin
Import-Certificate -FilePath "GestionTime.cer" -CertStoreLocation Cert:\LocalMachine\TrustedPeople
```

---

### **Problema: "Error al conectar con la API"**

**Causa:** Firewall o antivirus bloqueando

**Solución:**
```
Agregar excepción en firewall para:
- GestionTime.Desktop.exe
- gestiontimeapi.onrender.com
```

---

## ?? RECURSOS ADICIONALES

- **MSIX Documentation:** https://docs.microsoft.com/windows/msix/
- **Inno Setup:** https://jrsoftware.org/isinfo.php
- **WiX Toolset:** https://wixtoolset.org/
- **.NET Publishing:** https://docs.microsoft.com/dotnet/core/deploying/

---

## ? RESUMEN EJECUTIVO

### **Recomendación para tu caso:**

```
??????????????????????????????????????????????
?  ?? MÉTODO RECOMENDADO: MSIX               ?
?                                            ?
?  ? Instalación moderna y segura           ?
?  ? Actualizaciones automáticas            ?
?  ? Compatible con Windows 10/11           ?
?  ? Ya tienes todo configurado             ?
?                                            ?
?  ?? Tamaño: ~50MB                          ?
?  ?? Tiempo de preparación: 15 min          ?
?  ?? Distribución: Fácil                    ?
??????????????????????????????????????????????
```

### **Pasos Rápidos:**

1. ? **Publish** ? **Create App Packages**
2. ? Seleccionar **Sideloading**
3. ? Elegir **x64**
4. ? Crear/Seleccionar certificado
5. ? Click **Create**
6. ? Distribuir la carpeta resultante

---

**Fecha:** 2025-01-27  
**Guía preparada para:** GestionTime Desktop v1.0.0  
**Estado:** ? Lista para seguir
