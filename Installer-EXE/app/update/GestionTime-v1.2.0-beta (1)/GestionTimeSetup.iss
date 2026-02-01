[Setup]
; Información del producto
AppName=GestionTime Desktop
AppVersion=1.1.0.0
AppPublisher=GestionTime Solutions
AppPublisherURL=https://gestiontime.com
AppSupportURL=https://gestiontime.com/support
AppUpdatesURL=https://gestiontime.com/updates
DefaultDirName={autopf}\GestionTime Desktop
DefaultGroupName=GestionTime Desktop
AllowNoIcons=yes
LicenseFile=Installer\License.rtf
InfoBeforeFile=Installer\Readme.txt
OutputDir=bin\Release\Installer
OutputBaseFilename=GestionTimeDesktopSetup-1.1.0
SetupIconFile=Assets\app_logo.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern

; Requisitos del sistema
MinVersion=10.0.17763
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; Configuración de instalación
PrivilegesRequired=admin
DisableProgramGroupPage=yes
DisableReadyPage=no
DisableFinishedPage=no
ShowLanguageDialog=auto

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1

[Files]
; Ejecutable principal
Source: "bin\Release\Installer\App\GestionTime.Desktop.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\Installer\App\GestionTime.Desktop.dll"; DestDir: "{app}"; Flags: ignoreversion

; Archivos de configuración
Source: "bin\Release\Installer\App\appsettings.json"; DestDir: "{app}"; Flags: ignoreversion

; Runtime .NET y WindowsAppSDK (todos los archivos)
Source: "bin\Release\Installer\App\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\GestionTime Desktop"; Filename: "{app}\GestionTime.Desktop.exe"
Name: "{autodesktop}\GestionTime Desktop"; Filename: "{app}\GestionTime.Desktop.exe"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\GestionTime Desktop"; Filename: "{app}\GestionTime.Desktop.exe"; Tasks: quicklaunchicon

[Registry]
; Registro para desinstalación en Panel de Control
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop"; ValueType: string; ValueName: "DisplayName"; ValueData: "GestionTime Desktop"
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop"; ValueType: string; ValueName: "DisplayVersion"; ValueData: "1.1.0.0"
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop"; ValueType: string; ValueName: "Publisher"; ValueData: "GestionTime Solutions"
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Uninstall\GestionTimeDesktop"; ValueType: string; ValueName: "DisplayIcon"; ValueData: "{app}\GestionTime.Desktop.exe"

[Run]
Filename: "{app}\GestionTime.Desktop.exe"; Description: "{cm:LaunchProgram,GestionTime Desktop}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"