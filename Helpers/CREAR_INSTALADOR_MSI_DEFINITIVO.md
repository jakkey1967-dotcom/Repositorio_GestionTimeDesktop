# ?? SOLUCIÓN DEFINITIVA: CREAR INSTALADOR MSI

**Problema:** Los scripts BAT no pueden instalar Windows App Runtime de manera confiable  
**Solución:** Crear un instalador MSI profesional que incluya todo automáticamente

---

## ? PASOS PARA CREAR EL INSTALADOR MSI

### **1. Configurar el proyecto para MSIX/MSI**

Primero vamos a configurar el proyecto correctamente:

```xml
<!-- En GestionTime.Desktop.csproj -->
<PropertyGroup>
  <OutputType>WinExe</OutputType>
  <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
  <TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
  <UseWinUI>true</UseWinUI>
  <EnableMsixTooling>true</EnableMsixTooling>
  
  <!-- Configuración para instalador -->
  <ApplicationManifest>app.manifest</ApplicationManifest>
  <Platforms>x64</Platforms>
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
  <SelfContained>false</SelfContained>
  
  <!-- Información del producto -->
  <AssemblyTitle>GestionTime Desktop</AssemblyTitle>
  <Product>GestionTime Desktop</Product>
  <Company>Tu Empresa</Company>
  <Copyright>© 2025 Tu Empresa</Copyright>
  <AssemblyVersion>1.1.0.0</AssemblyVersion>
  <FileVersion>1.1.0.0</FileVersion>
</PropertyGroup>

<!-- Referencias necesarias para el instalador -->
<ItemGroup>
  <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.8.251104000" />
  <PackageReference Include="Microsoft.Windows.SDK.BuildTools" Version="10.0.26100.1742" />
</ItemGroup>
```

### **2. Crear Package.appxmanifest para MSIX**

```xml
<?xml version="1.0" encoding="utf-8"?>
<Package 
  xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
  xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
  xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
  IgnorableNamespaces="uap rescap">

  <Identity Name="GestionTime.Desktop"
            Publisher="CN=Tu Empresa"
            Version="1.1.0.0" />

  <Properties>
    <DisplayName>GestionTime Desktop</DisplayName>
    <PublisherDisplayName>Tu Empresa</PublisherDisplayName>
    <Logo>Assets\StoreLogo.png</Logo>
    <Description>Aplicación de gestión de tiempo para partes de trabajo</Description>
  </Properties>

  <Dependencies>
    <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.17763.0" MaxVersionTested="10.0.22621.0" />
    <PackageDependency Name="Microsoft.WindowsAppRuntime.1.8" MinVersion="8000.0.0.0" Publisher="CN=Microsoft Corporation, O=Microsoft Corporation, L=Redmond, S=Washington, C=US" />
  </Dependencies>

  <Resources>
    <Resource Language="es-ES" />
  </Resources>

  <Applications>
    <Application Id="App" Executable="GestionTime.Desktop.exe" EntryPoint="$targetname$.Program">
      <uap:VisualElements DisplayName="GestionTime Desktop"
                          Square150x150Logo="Assets\Square150x150Logo.png"
                          Square44x44Logo="Assets\Square44x44Logo.png"
                          Description="Gestión de partes de trabajo"
                          BackgroundColor="transparent">
        <uap:DefaultTile Wide310x150Logo="Assets\Wide310x150Logo.png" />
        <uap:SplashScreen Image="Assets\SplashScreen.png" />
      </uap:VisualElements>
    </Application>
  </Applications>

  <Capabilities>
    <rescap:Capability Name="runFullTrust" />
  </Capabilities>

</Package>
```

---

## ?? CREAR EL INSTALADOR MSI

### **Método 1: Visual Studio (Recomendado)**

1. **En Visual Studio 2022:**
   ```
   Click derecho en proyecto GestionTime.Desktop
   ? Publish
   ? Create App Packages
   ? Sideloading
   ? Next
   ? Arquitectura: x64
   ? Version: 1.1.0.0
   ? Create new certificate
   ? Publisher: CN=Tu Empresa
   ? Create
   ```

2. **Resultado:** Se creará en `AppPackages\` folder:
   ```
   GestionTime.Desktop_1.1.0.0_x64_Test\
   ??? GestionTime.Desktop_1.1.0.0_x64.msix
   ??? Install.ps1 (script de instalación)
   ??? Dependencies\ (incluye Windows App Runtime)
   ```

### **Método 2: WiX Toolset (Profesional)**

Si necesitas un verdadero MSI, usar WiX Toolset:

1. **Instalar WiX:**
   ```
   winget install WiXToolset.WiX
   ```

2. **Crear archivo GestionTime.wxs:**
   ```xml
   <?xml version="1.0" encoding="UTF-8"?>
   <Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
     <Product Id="*" 
              Name="GestionTime Desktop" 
              Language="1034" 
              Version="1.1.0.0" 
              Manufacturer="Tu Empresa" 
              UpgradeCode="12345678-1234-1234-1234-123456789ABC">
       
       <Package InstallerVersion="200" Compressed="yes" InstallScope="perMachine" />
       
       <!-- Incluir Windows App Runtime como prerequisito -->
       <PropertyRef Id="WIX_IS_NETFRAMEWORK_48_OR_LATER_INSTALLED"/>
       <Condition Message="Requiere Windows App Runtime 1.8">
         <![CDATA[Installed OR WIX_IS_NETFRAMEWORK_48_OR_LATER_INSTALLED]]>
       </Condition>
       
       <!-- Estructura de directorios -->
       <Directory Id="TARGETDIR" Name="SourceDir">
         <Directory Id="ProgramFiles64Folder">
           <Directory Id="INSTALLFOLDER" Name="GestionTime Desktop" />
         </Directory>
         <Directory Id="ProgramMenuFolder" />
       </Directory>
       
       <!-- Componentes -->
       <ComponentGroup Id="ProductComponents" Directory="INSTALLFOLDER">
         <Component Id="MainExecutable">
           <File Source="publish\GestionTime.Desktop.exe" />
           <File Source="publish\appsettings.json" />
           <!-- Incluir todas las DLLs necesarias -->
         </Component>
       </ComponentGroup>
       
       <!-- Feature -->
       <Feature Id="ProductFeature" Title="GestionTime Desktop" Level="1">
         <ComponentGroupRef Id="ProductComponents" />
       </Feature>
       
     </Product>
   </Wix>
   ```

---

## ?? VENTAJAS DEL INSTALADOR MSI/MSIX

### **MSIX (Recomendado):**
- ? **Incluye Windows App Runtime automáticamente**
- ? **Instalación limpia y segura**
- ? **Actualizaciones automáticas**
- ? **Desinstalación completa**
- ? **Sandboxing de seguridad**

### **MSI Tradicional:**
- ? **Compatible con sistemas corporativos**
- ? **Group Policy deployment**
- ? **Silent installation**
- ? **Personalizable al 100%**

---

## ?? PRÓXIMOS PASOS INMEDIATOS

1. **Actualizar GestionTime.Desktop.csproj** con la configuración arriba
2. **Crear Package.appxmanifest** en la raíz del proyecto  
3. **Generar el MSIX** usando Visual Studio
4. **Probar la instalación** en una máquina limpia
5. **Distribuir el MSIX** en lugar del ZIP portable

---

## ?? RESULTADO ESPERADO

**Archivo final:** `GestionTimeDesktop_install.msix` o `GestionTimeDesktop_install.msi`

**Para usuarios:**
```
1. Descargar GestionTimeDesktop_install.msix
2. Doble-click para instalar
3. Aceptar certificado si aparece
4. ¡Listo! Buscar "GestionTime" en menú inicio
```

**Sin necesidad de:**
- ? Scripts BAT complicados
- ? Instalación manual de Windows App Runtime  
- ? Troubleshooting de dependencias
- ? Extraer archivos ZIP

---

¿Quieres que empecemos configurando el proyecto para crear el MSIX package?