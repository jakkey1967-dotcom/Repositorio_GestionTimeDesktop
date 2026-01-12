@echo off
REM =============================================================================
REM GENERAR MSI COMPLETO CON TODAS LAS CARPETAS - VERSION FUNCIONAL
REM =============================================================================

echo.
echo ============================================
echo   GENERAR MSI COMPLETO (153 archivos)
echo ============================================
echo.

cd /d C:\GestionTime\GestionTimeDesktop

set WIXBIN=C:\Program Files\WiX Toolset v6.0\bin
set BINDIR=bin\x64\Debug\net8.0-windows10.0.19041.0
set OUTDIR=Installer\Output
set WXSFILE=Installer\MSI\Product_Generated.wxs
set MSIFILE=%OUTDIR%\GestionTime-Desktop-Setup.msi

REM Paso 1: Crear directorio de salida
if not exist "%OUTDIR%" mkdir "%OUTDIR%"

REM Paso 2: Copiar window-config.ini si existe
echo [1/3] Preparando archivos...
if exist "Installer\window-config.ini" (
    copy /Y "Installer\window-config.ini" "%BINDIR%\window-config.ini" >nul 2>&1
    echo    [OK] window-config.ini copiado
)

REM Paso 3: Generar archivo WXS con RECURSION
echo.
echo [2/3] Generando archivo WXS con carpetas recursivas...

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
"$binDir = '%BINDIR%'; ^
$wxsFile = '%WXSFILE%'; ^
$files = Get-ChildItem -Path $binDir -File -Recurse; ^
Write-Host \"   Archivos encontrados: $($files.Count)\" -ForegroundColor Cyan; ^
$xml = New-Object System.Xml.XmlTextWriter($wxsFile, [System.Text.Encoding]::UTF8); ^
$xml.Formatting = 'Indented'; ^
$xml.WriteStartDocument(); ^
$xml.WriteStartElement('Wix', 'http://wixtoolset.org/schemas/v4/wxs'); ^
$xml.WriteStartElement('Package'); ^
$xml.WriteAttributeString('Name', 'GestionTime Desktop'); ^
$xml.WriteAttributeString('Version', '1.2.0.0'); ^
$xml.WriteAttributeString('Manufacturer', 'Global Retail Solutions'); ^
$xml.WriteAttributeString('UpgradeCode', 'F1E2D3C4-B5A6-9780-ABCD-123456789012'); ^
$xml.WriteAttributeString('Language', '1034'); ^
$xml.WriteStartElement('MajorUpgrade'); ^
$xml.WriteAttributeString('DowngradeErrorMessage', 'Ya existe una version mas reciente.'); ^
$xml.WriteEndElement(); ^
$xml.WriteStartElement('MediaTemplate'); ^
$xml.WriteAttributeString('EmbedCab', 'yes'); ^
$xml.WriteEndElement(); ^
$xml.WriteStartElement('StandardDirectory'); ^
$xml.WriteAttributeString('Id', 'ProgramFiles64Folder'); ^
$xml.WriteStartElement('Directory'); ^
$xml.WriteAttributeString('Id', 'INSTALLFOLDER'); ^
$xml.WriteAttributeString('Name', 'GestionTime Desktop'); ^
$xml.WriteEndElement(); ^
$xml.WriteEndElement(); ^
$xml.WriteStartElement('StandardDirectory'); ^
$xml.WriteAttributeString('Id', 'ProgramMenuFolder'); ^
$xml.WriteStartElement('Directory'); ^
$xml.WriteAttributeString('Id', 'ProgramMenuDir'); ^
$xml.WriteAttributeString('Name', 'GestionTime Desktop'); ^
$xml.WriteEndElement(); ^
$xml.WriteEndElement(); ^
$xml.WriteStartElement('Feature'); ^
$xml.WriteAttributeString('Id', 'Main'); ^
$xml.WriteAttributeString('Title', 'GestionTime Desktop'); ^
$xml.WriteAttributeString('Level', '1'); ^
$xml.WriteStartElement('ComponentGroupRef'); ^
$xml.WriteAttributeString('Id', 'AllFiles'); ^
$xml.WriteEndElement(); ^
$xml.WriteStartElement('ComponentRef'); ^
$xml.WriteAttributeString('Id', 'MenuShortcut'); ^
$xml.WriteEndElement(); ^
$xml.WriteEndElement(); ^
$xml.WriteEndElement(); ^
$xml.WriteStartElement('Fragment'); ^
$xml.WriteStartElement('ComponentGroup'); ^
$xml.WriteAttributeString('Id', 'AllFiles'); ^
$xml.WriteAttributeString('Directory', 'INSTALLFOLDER'); ^
$id = 1; ^
foreach ($f in $files) { ^
    $guid = [System.Guid]::NewGuid().ToString().ToUpper(); ^
    $xml.WriteStartElement('Component'); ^
    $xml.WriteAttributeString('Id', \"C$id\"); ^
    $xml.WriteAttributeString('Guid', $guid); ^
    $xml.WriteStartElement('File'); ^
    $xml.WriteAttributeString('Source', $f.FullName); ^
    $xml.WriteEndElement(); ^
    $xml.WriteEndElement(); ^
    $id++; ^
}; ^
$xml.WriteEndElement(); ^
$xml.WriteEndElement(); ^
$xml.WriteStartElement('Fragment'); ^
$xml.WriteStartElement('Component'); ^
$xml.WriteAttributeString('Id', 'MenuShortcut'); ^
$xml.WriteAttributeString('Directory', 'ProgramMenuDir'); ^
$xml.WriteStartElement('Shortcut'); ^
$xml.WriteAttributeString('Name', 'GestionTime Desktop'); ^
$xml.WriteAttributeString('Target', '[INSTALLFOLDER]GestionTime.Desktop.exe'); ^
$xml.WriteAttributeString('WorkingDirectory', 'INSTALLFOLDER'); ^
$xml.WriteEndElement(); ^
$xml.WriteStartElement('RemoveFolder'); ^
$xml.WriteAttributeString('Id', 'RemoveMenu'); ^
$xml.WriteAttributeString('On', 'uninstall'); ^
$xml.WriteEndElement(); ^
$xml.WriteStartElement('RegistryValue'); ^
$xml.WriteAttributeString('Root', 'HKCU'); ^
$xml.WriteAttributeString('Key', 'Software\\GestionTime'); ^
$xml.WriteAttributeString('Name', 'Installed'); ^
$xml.WriteAttributeString('Value', '1'); ^
$xml.WriteAttributeString('Type', 'integer'); ^
$xml.WriteAttributeString('KeyPath', 'yes'); ^
$xml.WriteEndElement(); ^
$xml.WriteEndElement(); ^
$xml.WriteEndElement(); ^
$xml.WriteEndElement(); ^
$xml.WriteEndDocument(); ^
$xml.Close(); ^
Write-Host \"   [OK] WXS generado con $($files.Count) archivos\" -ForegroundColor Green"

if errorlevel 1 (
    echo ERROR: No se pudo generar WXS
    pause
    exit /b 1
)

REM Paso 4: Compilar MSI
echo.
echo [3/3] Compilando MSI...
echo    (Esto puede tardar 1-2 minutos...)

"%WIXBIN%\wix.exe" build "%WXSFILE%" -arch x64 -out "%MSIFILE%" -bindpath "%BINDIR%" -nologo

if errorlevel 1 (
    echo ERROR: Compilacion fallo
    pause
    exit /b 1
)

REM Verificar resultado
if exist "%MSIFILE%" (
    echo.
    echo ============================================
    echo   MSI COMPLETO CREADO EXITOSAMENTE
    echo ============================================
    echo.
    for %%F in ("%MSIFILE%") do (
        set /a sizeMB=%%~zF/1048576
        echo Tamano: %%~zF bytes (aprox. !sizeMB! MB^)
    )
    echo.
    echo INCLUYE:
    echo   - Todos los archivos recursivos
    echo   - Assets\
    echo   - Views\
    echo   - Controls\
    echo   - runtimes\win-x64\native\
    echo   - logs\
    echo   - window-config.ini
    echo.
    explorer /select,"%MSIFILE%"
    echo [OK] Completado!
) else (
    echo ERROR: MSI no se creo
)

echo.
pause
