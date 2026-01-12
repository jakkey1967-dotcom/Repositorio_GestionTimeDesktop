@echo off
setlocal enabledelayedexpansion

echo.
echo ================================================
echo   CREAR MSI FINAL - METODO DIRECTO
echo ================================================
echo.

cd /d C:\GestionTime\GestionTimeDesktop

set WIX=C:\Program Files\WiX Toolset v6.0\bin\wix.exe
set BINDIR=bin\x64\Debug\net8.0-windows10.0.19041.0
set WXSFILE=Installer\MSI\Product_Final.wxs
set OUTDIR=Installer\Output
set MSIFILE=%OUTDIR%\GestionTime-Desktop-Setup.msi

REM Verificar WiX
if not exist "%WIX%" (
    echo ERROR: WiX v6.0 no encontrado
    pause
    exit /b 1
)

REM Verificar ejecutable
if not exist "%BINDIR%\GestionTime.Desktop.exe" (
    echo ERROR: Compilar primero con dotnet build
    pause
    exit /b 1
)

if not exist "%OUTDIR%" mkdir "%OUTDIR%"

echo [1/3] Copiando window-config.ini...
if exist "Installer\window-config.ini" (
    copy /Y "Installer\window-config.ini" "%BINDIR%\window-config.ini" >nul 2>&1
    echo    OK
)

echo.
echo [2/3] Generando archivo WXS dinamico...

REM Crear archivo WXS con PowerShell
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
"$binDir = '%BINDIR%'; " ^
"$wxsFile = '%WXSFILE%'; " ^
"$allFiles = Get-ChildItem -Path $binDir -File -Recurse; " ^
"Write-Host \"   Archivos: $($allFiles.Count)\"; " ^
"$xml = New-Object System.Xml.XmlTextWriter($wxsFile, [System.Text.Encoding]::UTF8); " ^
"$xml.Formatting = 'Indented'; " ^
"$xml.WriteStartDocument(); " ^
"$xml.WriteStartElement('Wix', 'http://wixtoolset.org/schemas/v4/wxs'); " ^
"$xml.WriteStartElement('Package'); " ^
"$xml.WriteAttributeString('Name', 'GestionTime Desktop'); " ^
"$xml.WriteAttributeString('Version', '1.2.0.0'); " ^
"$xml.WriteAttributeString('Manufacturer', 'Global Retail Solutions'); " ^
"$xml.WriteAttributeString('UpgradeCode', 'F1E2D3C4-B5A6-9780-ABCD-123456789012'); " ^
"$xml.WriteAttributeString('Language', '1034'); " ^
"$xml.WriteStartElement('MajorUpgrade'); " ^
"$xml.WriteAttributeString('DowngradeErrorMessage', 'Ya existe version mas reciente'); " ^
"$xml.WriteEndElement(); " ^
"$xml.WriteStartElement('MediaTemplate'); " ^
"$xml.WriteAttributeString('EmbedCab', 'yes'); " ^
"$xml.WriteEndElement(); " ^
"$xml.WriteStartElement('StandardDirectory'); " ^
"$xml.WriteAttributeString('Id', 'ProgramFiles64Folder'); " ^
"$xml.WriteStartElement('Directory'); " ^
"$xml.WriteAttributeString('Id', 'INSTALLFOLDER'); " ^
"$xml.WriteAttributeString('Name', 'GestionTime Desktop'); " ^
"$xml.WriteEndElement(); " ^
"$xml.WriteEndElement(); " ^
"$xml.WriteStartElement('StandardDirectory'); " ^
"$xml.WriteAttributeString('Id', 'ProgramMenuFolder'); " ^
"$xml.WriteStartElement('Directory'); " ^
"$xml.WriteAttributeString('Id', 'MenuDir'); " ^
"$xml.WriteAttributeString('Name', 'GestionTime Desktop'); " ^
"$xml.WriteEndElement(); " ^
"$xml.WriteEndElement(); " ^
"$xml.WriteStartElement('Feature'); " ^
"$xml.WriteAttributeString('Id', 'Main'); " ^
"$xml.WriteAttributeString('Level', '1'); " ^
"$xml.WriteStartElement('ComponentGroupRef'); " ^
"$xml.WriteAttributeString('Id', 'Files'); " ^
"$xml.WriteEndElement(); " ^
"$xml.WriteStartElement('ComponentRef'); " ^
"$xml.WriteAttributeString('Id', 'Menu'); " ^
"$xml.WriteEndElement(); " ^
"$xml.WriteEndElement(); " ^
"$xml.WriteEndElement(); " ^
"$xml.WriteStartElement('Fragment'); " ^
"$xml.WriteStartElement('ComponentGroup'); " ^
"$xml.WriteAttributeString('Id', 'Files'); " ^
"$xml.WriteAttributeString('Directory', 'INSTALLFOLDER'); " ^
"$i = 1; " ^
"foreach ($f in $allFiles) { " ^
"  $guid = [System.Guid]::NewGuid().ToString().ToUpper(); " ^
"  $xml.WriteStartElement('Component'); " ^
"  $xml.WriteAttributeString('Id', \"F$i\"); " ^
"  $xml.WriteAttributeString('Guid', $guid); " ^
"  $xml.WriteStartElement('File'); " ^
"  $xml.WriteAttributeString('Source', $f.FullName); " ^
"  $xml.WriteEndElement(); " ^
"  $xml.WriteEndElement(); " ^
"  $i++; " ^
"}; " ^
"$xml.WriteEndElement(); " ^
"$xml.WriteEndElement(); " ^
"$xml.WriteStartElement('Fragment'); " ^
"$xml.WriteStartElement('Component'); " ^
"$xml.WriteAttributeString('Id', 'Menu'); " ^
"$xml.WriteAttributeString('Directory', 'MenuDir'); " ^
"$xml.WriteStartElement('Shortcut'); " ^
"$xml.WriteAttributeString('Name', 'GestionTime Desktop'); " ^
"$xml.WriteAttributeString('Target', '[INSTALLFOLDER]GestionTime.Desktop.exe'); " ^
"$xml.WriteAttributeString('WorkingDirectory', 'INSTALLFOLDER'); " ^
"$xml.WriteEndElement(); " ^
"$xml.WriteStartElement('RemoveFolder'); " ^
"$xml.WriteAttributeString('Id', 'Del'); " ^
"$xml.WriteAttributeString('On', 'uninstall'); " ^
"$xml.WriteEndElement(); " ^
"$xml.WriteStartElement('RegistryValue'); " ^
"$xml.WriteAttributeString('Root', 'HKCU'); " ^
"$xml.WriteAttributeString('Key', 'Software\\GestionTime'); " ^
"$xml.WriteAttributeString('Name', 'Installed'); " ^
"$xml.WriteAttributeString('Value', '1'); " ^
"$xml.WriteAttributeString('Type', 'integer'); " ^
"$xml.WriteAttributeString('KeyPath', 'yes'); " ^
"$xml.WriteEndElement(); " ^
"$xml.WriteEndElement(); " ^
"$xml.WriteEndElement(); " ^
"$xml.WriteEndElement(); " ^
"$xml.WriteEndDocument(); " ^
"$xml.Close();"

if errorlevel 1 (
    echo ERROR: No se pudo generar WXS
    pause
    exit /b 1
)

echo    OK - WXS generado

echo.
echo [3/3] Compilando MSI con WiX...

"%WIX%" build "%WXSFILE%" -arch x64 -out "%MSIFILE%" -bindpath "%BINDIR%" -nologo

if errorlevel 1 (
    echo ERROR: Compilacion fallo
    pause
    exit /b 1
)

if exist "%MSIFILE%" (
    echo.
    echo ================================================
    echo   MSI CREADO EXITOSAMENTE
    echo ================================================
    echo.
    for %%F in ("%MSIFILE%") do (
        set /a mb=%%~zF/1048576
        echo Archivo: %%~nxF
        echo Tamano: !mb! MB
    )
    echo Archivos: 153
    echo.
    explorer /select,"%MSIFILE%"
    echo OK
) else (
    echo ERROR: MSI no creado
)

pause
