# ===========================================================================
# SCRIPT: Crear Instalador EXE COMPLETO - GestionTime Desktop  
# VERSION: 3.0 - ENERO 2026
# DESCRIPCION: Instalador profesional que incluye TODAS las carpetas
# ===========================================================================

param(
    [string]$SourceDir = "C:\GestionTime\GestionTimeDesktop\bin\x64\Debug\net8.0-windows10.0.19041.0"
)

$ErrorActionPreference = "Stop"
$Version = "1.2.0"
$OutputDir = "C:\GestionTime\GestionTimeDesktop\Installer\Output"
$InstallerDir = "C:\GestionTime\GestionTimeDesktop\Installer"

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  INSTALADOR EXE PROFESIONAL - GESTIONTIME DESKTOP  " -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# ===========================================================================
# VERIFICAR REQUISITOS
# ===========================================================================

Write-Host "🔍 Verificando requisitos..." -ForegroundColor Yellow
Write-Host ""

if (-not (Test-Path $SourceDir)) {
    Write-Host "❌ ERROR: Directorio no existe: $SourceDir" -ForegroundColor Red
    exit 1
}

$exePath = Join-Path $SourceDir "GestionTime.Desktop.exe"
if (-not (Test-Path $exePath)) {
    Write-Host "❌ ERROR: No se encontro GestionTime.Desktop.exe" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Directorio fuente: OK" -ForegroundColor Green
Write-Host "✅ Ejecutable: OK" -ForegroundColor Green

# Verificar Inno Setup
$innoPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $innoPath)) {
    Write-Host ""
    Write-Host "❌ Inno Setup NO esta instalado" -ForegroundColor Red
    Write-Host ""
    Write-Host "📥 INSTALAR Inno Setup (GRATIS y FACIL):" -ForegroundColor Yellow
    Write-Host "   1. Visitar: https://jrsoftware.org/isdl.php" -ForegroundColor White
    Write-Host "   2. Descargar: innosetup-6.x.x.exe" -ForegroundColor White
    Write-Host "   3. Instalar (siguiente, siguiente, finalizar)" -ForegroundColor White
    Write-Host "   4. Volver a ejecutar este script" -ForegroundColor White
    Write-Host ""
    
    $response = Read-Host "¿Abrir pagina de descarga ahora? (S/N)"
    if ($response -match '^[Ss]$') {
        Start-Process "https://jrsoftware.org/isdl.php"
    }
    exit 1
}

Write-Host "✅ Inno Setup: OK" -ForegroundColor Green
Write-Host ""

# ===========================================================================
# CREAR DIRECTORIOS
# ===========================================================================

Write-Host "📁 Creando estructura de directorios..." -ForegroundColor Yellow

@($InstallerDir, $OutputDir) | ForEach-Object {
    if (-not (Test-Path $_)) {
        New-Item -ItemType Directory -Path $_ -Force | Out-Null
        Write-Host "   ✅ Creado: $_" -ForegroundColor Green
    }
}

Write-Host ""

# ===========================================================================
# GENERAR SCRIPT INNO SETUP (.ISS)
# ===========================================================================

Write-Host "📝 Generando script de Inno Setup..." -ForegroundColor Yellow

$issScript = @"
; ═══════════════════════════════════════════════════════════════════════
; GESTIONTIME DESKTOP - INSTALADOR PROFESIONAL
; Version: $Version
; Fecha: $(Get-Date -Format "dd/MM/yyyy HH:mm")
; Generado automaticamente
; ═══════════════════════════════════════════════════════════════════════

#define MyAppName "GestionTime Desktop"
#define MyAppVersion "$Version"
#define MyAppPublisher "Global Retail Solutions"
#define MyAppURL "https://github.com/jakkey1967-dotcom"
#define MyAppExeName "GestionTime.Desktop.exe"

[Setup]
AppId={{F1E2D3C4-B5A6-9780-FEDC-BA9876543210}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
DefaultDirName={autopf}\GestionTime\Desktop
DefaultGroupName={#MyAppName}
OutputDir=$OutputDir
OutputBaseFilename=GestionTime-Desktop-{#MyAppVersion}-Setup
SetupIconFile=$SourceDir\Assets\app.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayIcon={app}\{#MyAppExeName}
DisableProgramGroupPage=yes
LicenseFile=$InstallerDir\License.txt
InfoBeforeFile=$InstallerDir\Readme.txt

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear icono en el escritorio"; GroupDescription: "Iconos adicionales:"; Flags: unchecked
Name: "quicklaunchicon"; Description: "Crear icono en inicio rapido"; GroupDescription: "Iconos adicionales:"; Flags: unchecked

[Files]
; ═══════════════════════════════════════════════════════════════════════
; ARCHIVOS PRINCIPALES
; ═══════════════════════════════════════════════════════════════════════
Source: "$SourceDir\GestionTime.Desktop.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "$SourceDir\GestionTime.Desktop.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "$SourceDir\appsettings.json"; DestDir: "{app}"; Flags: ignoreversion onlyifdoesntexist

; ═══════════════════════════════════════════════════════════════════════
; TODAS LAS DLLs (INCLUYENDO DEPENDENCIAS)
; ═══════════════════════════════════════════════════════════════════════
Source: "$SourceDir\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "$SourceDir\*.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "$SourceDir\*.deps.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "$SourceDir\*.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion

; ═══════════════════════════════════════════════════════════════════════
; ASSETS (ICONOS, IMAGENES, LOGOS)
; ═══════════════════════════════════════════════════════════════════════
Source: "$SourceDir\Assets\*"; DestDir: "{app}\Assets"; Flags: ignoreversion recursesubdirs createallsubdirs

; ═══════════════════════════════════════════════════════════════════════
; DOCS (DOCUMENTACION)
; ═══════════════════════════════════════════════════════════════════════
Source: "$SourceDir\Docs\*"; DestDir: "{app}\Docs"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: DirExists('$SourceDir\Docs')

; ═══════════════════════════════════════════════════════════════════════
; RUNTIMES (BIBLIOTECAS NATIVAS POR ARQUITECTURA)
; ═══════════════════════════════════════════════════════════════════════
Source: "$SourceDir\runtimes\*"; DestDir: "{app}\runtimes"; Flags: ignoreversion recursesubdirs createallsubdirs

; Runtimes especificos (asegurar que se incluyan)
Source: "$SourceDir\runtimes\win-x64\*"; DestDir: "{app}\runtimes\win-x64"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "$SourceDir\runtimes\win-x86\*"; DestDir: "{app}\runtimes\win-x86"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: DirExists('$SourceDir\runtimes\win-x86')
Source: "$SourceDir\runtimes\win-arm64\*"; DestDir: "{app}\runtimes\win-arm64"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: DirExists('$SourceDir\runtimes\win-arm64')

; ═══════════════════════════════════════════════════════════════════════
; MICROSOFT.UI.XAML (WINUI 3)
; ═══════════════════════════════════════════════════════════════════════
Source: "$SourceDir\Microsoft.UI.Xaml\*"; DestDir: "{app}\Microsoft.UI.Xaml"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: DirExists('$SourceDir\Microsoft.UI.Xaml')

; ═══════════════════════════════════════════════════════════════════════
; DOCUMENTACION DEL INSTALADOR
; ═══════════════════════════════════════════════════════════════════════
Source: "$InstallerDir\Readme.txt"; DestDir: "{app}"; Flags: ignoreversion isreadme
Source: "$InstallerDir\License.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Manual de Usuario"; Filename: "{app}\Docs\MANUAL_USUARIO_GESTIONTIME_DESKTOP.md"; Check: FileExists(ExpandConstant('{app}\Docs\MANUAL_USUARIO_GESTIONTIME_DESKTOP.md'))
Name: "{group}\Desinstalar {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Iniciar {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Code]
function InitializeSetup(): Boolean;
begin
  Result := True;
  // Aqui se pueden agregar verificaciones adicionales
end;

"@

$issPath = Join-Path $InstallerDir "GestionTime-Setup.iss"
$issScript | Out-File -FilePath $issPath -Encoding UTF8

Write-Host "   ✅ Script .iss generado" -ForegroundColor Green
Write-Host ""

# ===========================================================================
# CREAR ARCHIVOS AUXILIARES
# ===========================================================================

Write-Host "📄 Creando archivos auxiliares..." -ForegroundColor Yellow

# License.txt
$license = @"
═══════════════════════════════════════════════════════════════
LICENCIA DE USO - GESTIONTIME DESKTOP
═══════════════════════════════════════════════════════════════

VERSION: $Version
FECHA: $(Get-Date -Format "dd/MM/yyyy")
EMPRESA: Global Retail Solutions

TERMINOS Y CONDICIONES
═══════════════════════════════════════════════════════════════

Este software es propiedad de Global Retail Solutions y esta
protegido por leyes de derechos de autor internacionales.

1. LICENCIA DE USO
   Se concede una licencia no exclusiva para usar este software
   en equipos de la organizacion autorizada.

2. RESTRICCIONES
   - No redistribucion sin autorizacion
   - No ingenieria inversa
   - Software "tal cual" sin garantias

3. SOPORTE TECNICO
   Email: soporte@gestiontime.com
   Tel: +34 900 123 456
   Web: https://github.com/jakkey1967-dotcom

© 2026 Global Retail Solutions. Todos los derechos reservados.
"@

$license | Out-File -FilePath (Join-Path $InstallerDir "License.txt") -Encoding UTF8

# Readme.txt
$readme = @"
═══════════════════════════════════════════════════════════════
GESTIONTIME DESKTOP - MANUAL DE INICIO RAPIDO
═══════════════════════════════════════════════════════════════

VERSION: $Version
FECHA: $(Get-Date -Format "dd/MM/yyyy")

REQUISITOS DEL SISTEMA
═══════════════════════════════════════════════════════════════
✓ Windows 11 (64-bit)
✓ .NET 8 Desktop Runtime
✓ 4 GB RAM minimo
✓ 500 MB espacio en disco

PRIMER INICIO
═══════════════════════════════════════════════════════════════
1. Buscar "GestionTime Desktop" en el Menu Inicio
2. Hacer clic para ejecutar
3. Iniciar sesion con tus credenciales corporativas

SOPORTE TECNICO
═══════════════════════════════════════════════════════════════
Email: soporte@gestiontime.com
Tel: +34 900 123 456
Horario: Lunes a Viernes, 9:00 - 18:00 (CET)

DOCUMENTACION COMPLETA
═══════════════════════════════════════════════════════════════
Manual de Usuario: Docs\MANUAL_USUARIO_GESTIONTIME_DESKTOP.md
Repositorio GitHub: https://github.com/jakkey1967-dotcom

© 2026 Global Retail Solutions
"@

$readme | Out-File -FilePath (Join-Path $InstallerDir "Readme.txt") -Encoding UTF8

Write-Host "   ✅ License.txt creado" -ForegroundColor Green
Write-Host "   ✅ Readme.txt creado" -ForegroundColor Green
Write-Host ""

# ===========================================================================
# COMPILAR INSTALADOR
# ===========================================================================

Write-Host "🔨 Compilando instalador EXE..." -ForegroundColor Yellow
Write-Host ""

try {
    $compileArgs = @(
        "/Q",  # Quiet mode (menos output)
        "`"$issPath`""
    )
    
    $proc = Start-Process -FilePath $innoPath -ArgumentList $compileArgs -Wait -PassThru -NoNewWindow
    
    if ($proc.ExitCode -ne 0) {
        throw "Error al compilar (codigo de salida: $($proc.ExitCode))"
    }
    
    Write-Host "✅ Compilacion exitosa" -ForegroundColor Green
    Write-Host ""
    
} catch {
    Write-Host ""
    Write-Host "❌ ERROR al compilar:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

# ===========================================================================
# VERIFICAR RESULTADO
# ===========================================================================

$installerPath = Join-Path $OutputDir "GestionTime-Desktop-$Version-Setup.exe"

if (Test-Path $installerPath) {
    $installerFile = Get-Item $installerPath
    
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Green
    Write-Host "  ✅ INSTALADOR CREADO EXITOSAMENTE  " -ForegroundColor Green
    Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Green
    Write-Host ""
    Write-Host "📦 ARCHIVO:" -ForegroundColor Cyan
    Write-Host "   $($installerFile.FullName)" -ForegroundColor White
    Write-Host ""
    Write-Host "📊 TAMAÑO:" -ForegroundColor Cyan
    Write-Host "   $([math]::Round($installerFile.Length / 1MB, 2)) MB" -ForegroundColor White
    Write-Host ""
    Write-Host "🔖 VERSION:" -ForegroundColor Cyan
    Write-Host "   $Version" -ForegroundColor White
    Write-Host ""
    Write-Host "📋 COMPONENTES INCLUIDOS:" -ForegroundColor Cyan
    Write-Host "   ✓ Ejecutable principal (GestionTime.Desktop.exe)" -ForegroundColor White
    Write-Host "   ✓ Todas las DLLs de dependencias" -ForegroundColor White
    Write-Host "   ✓ Assets completos (iconos, logos, fondos)" -ForegroundColor White
    Write-Host "   ✓ Runtimes (win-x64, win-x86, win-arm64)" -ForegroundColor White
    Write-Host "   ✓ Documentacion (Manual de Usuario)" -ForegroundColor White
    Write-Host "   ✓ Configuracion (appsettings.json)" -ForegroundColor White
    Write-Host ""
    Write-Host "🚀 INSTRUCCIONES DE INSTALACION:" -ForegroundColor Yellow
    Write-Host "   1. Hacer doble-clic en el archivo EXE" -ForegroundColor White
    Write-Host "   2. Aceptar permisos de administrador" -ForegroundColor White
    Write-Host "   3. Seguir el asistente de instalacion" -ForegroundColor White
    Write-Host "   4. Buscar 'GestionTime Desktop' en Menu Inicio" -ForegroundColor White
    Write-Host ""
    Write-Host "💻 INSTALACION SILENCIOSA:" -ForegroundColor Yellow
    Write-Host "   $($installerFile.Name) /VERYSILENT /NORESTART" -ForegroundColor White
    Write-Host ""
    Write-Host "🗑️  DESINSTALACION:" -ForegroundColor Yellow
    Write-Host "   Panel de Control → Programas → GestionTime Desktop" -ForegroundColor White
    Write-Host ""
    
    # Abrir explorador de archivos
    Start-Process explorer.exe -ArgumentList "/select,`"$($installerFile.FullName)`""
    
} else {
    Write-Host ""
    Write-Host "❌ ERROR: No se genero el instalador" -ForegroundColor Red
    exit 1
}

Write-Host "Presiona cualquier tecla para salir..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
