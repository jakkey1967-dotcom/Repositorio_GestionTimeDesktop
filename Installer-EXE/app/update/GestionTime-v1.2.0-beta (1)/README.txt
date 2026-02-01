═══════════════════════════════════════════════════════════════
GESTIONTIME DESKTOP - INSTALADOR PORTABLE
═══════════════════════════════════════════════════════════════

Versión: 1.1.0
Fecha: 08 de enero de 2025

═══════════════════════════════════════════════════════════════

📦 CONTENIDO DEL PAQUETE

Este instalador portable incluye:
✅ Aplicación GestionTime Desktop v1.1.0
✅ Todas las dependencias necesarias (.NET 8 Runtime incluido)
✅ Archivo de configuración window-config.ini
✅ Icono de la aplicación
✅ Script de desinstalación automática

═══════════════════════════════════════════════════════════════

🚀 INSTRUCCIONES DE INSTALACIÓN

MÉTODO 1: Instalación Automática (Recomendado)
-----------------------------------------------
1. Haz clic derecho en "InstallPortable.ps1"
2. Selecciona "Ejecutar con PowerShell"
3. Si aparece un aviso de seguridad, confirma la ejecución
4. El instalador se ejecutará automáticamente con permisos de administrador
5. Sigue las instrucciones en pantalla

MÉTODO 2: Instalación Manual
-----------------------------
1. Abre PowerShell como Administrador
2. Navega a esta carpeta:
   cd "C:\ruta\a\la\carpeta\Installer"
3. Ejecuta el script:
   .\InstallPortable.ps1
4. Sigue las instrucciones en pantalla

MÉTODO 3: Instalación Personalizada
------------------------------------
Puedes cambiar la ubicación de instalación:

.\InstallPortable.ps1 -InstallPath "D:\MisAplicaciones\GestionTime"

Instalación silenciosa (sin preguntas):

.\InstallPortable.ps1 -Silent

Sin crear acceso directo en el escritorio:

.\InstallPortable.ps1 -CreateDesktopShortcut:$false

═══════════════════════════════════════════════════════════════

📂 UBICACIÓN DE INSTALACIÓN PREDETERMINADA

C:\app\gestiontime-desktop\

Contendrá:
  📄 GestionTime.Desktop.exe (Ejecutable principal)
  📄 window-config.ini (Configuración de ventanas)
  📄 appsettings.json (Configuración de la API)
  📄 Uninstall.ps1 (Script de desinstalación)
  📄 LEEME.txt (Información adicional)
  📁 Assets\ (Recursos e imágenes)
  📁 [Bibliotecas DLL necesarias]

═══════════════════════════════════════════════════════════════

🎯 CARACTERÍSTICAS DE ESTA INSTALACIÓN

✨ Portable: No modifica el registro de Windows
✨ Self-Contained: Incluye todo lo necesario (.NET 8 Runtime)
✨ Accesos directos: Escritorio + Menú Inicio
✨ Fácil desinstalación: Script automático incluido
✨ Configuración personalizable: window-config.ini
✨ Optimizada: ReadyToRun para arranque rápido

═══════════════════════════════════════════════════════════════

⚙️ REQUISITOS DEL SISTEMA

Sistema Operativo:
  • Windows 10 (versión 1809 o superior)
  • Windows 11 (todas las versiones)

Hardware mínimo:
  • Procesador: x64 (64-bit)
  • RAM: 4 GB
  • Espacio en disco: 500 MB

Permisos:
  • Se requiere permisos de Administrador para la instalación
  • Después de instalar, la aplicación se ejecuta con permisos normales

═══════════════════════════════════════════════════════════════

🗑️ DESINSTALACIÓN

MÉTODO 1: Script automático
---------------------------
1. Ejecuta: C:\app\gestiontime-desktop\Uninstall.ps1
2. O usa el acceso directo del menú Inicio:
   "Desinstalar GestionTime Desktop"

MÉTODO 2: Manual
----------------
1. Elimina la carpeta: C:\app\gestiontime-desktop
2. Elimina el acceso directo del escritorio (si existe)
3. Elimina la carpeta del menú Inicio:
   %AppData%\Microsoft\Windows\Start Menu\Programs\GestionTime Solutions

═══════════════════════════════════════════════════════════════

📋 CONFIGURACIÓN POST-INSTALACIÓN

1. CONFIGURACIÓN DE VENTANAS (window-config.ini)
   Personaliza el tamaño de las ventanas:
   
   DiarioPage=1103,800
   LoginPage=749,560
   ParteItemEdit=1140,845

2. CONFIGURACIÓN DE LA API (appsettings.json)
   Configura la URL del backend y otros parámetros

3. DATOS DE USUARIO
   Los datos locales se guardan en:
   %LocalAppData%\GestionTime.Desktop

═══════════════════════════════════════════════════════════════

📞 SOPORTE Y CONTACTO

🌐 GitHub Repository:
   https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop

📧 Email de Soporte:
   soporte@gestiontime.com

═══════════════════════════════════════════════════════════════

🎉 ¡GRACIAS POR ELEGIR GESTIONTIME DESKTOP!

Para comenzar a usar la aplicación:
  1. Ejecuta el instalador (InstallPortable.ps1)
  2. Usa el acceso directo creado en tu escritorio
  3. Inicia sesión con tus credenciales

═══════════════════════════════════════════════════════════════
