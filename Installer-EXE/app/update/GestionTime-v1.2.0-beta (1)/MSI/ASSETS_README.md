# Instrucciones para Assets del Instalador MSI

## Assets Necesarios

Para una experiencia profesional del instalador, necesitas crear estos archivos de imagen:

### 1. Banner del Instalador (installer-banner.bmp)
- **Tamaño**: 493 x 58 píxeles
- **Formato**: BMP de 8 bits
- **Ubicación**: Assets/installer-banner.bmp
- **Contenido**: Logo de GestionTime + "GestionTime Desktop v1.1.0"

### 2. Diálogo del Instalador (installer-dialog.bmp)  
- **Tamaño**: 493 x 312 píxeles
- **Formato**: BMP de 8 bits
- **Ubicación**: Assets/installer-dialog.bmp
- **Contenido**: Imagen de fondo para diálogos principales

### 3. Crear Assets Automáticamente

Puedes crear versiones temporales básicas ejecutando:

```powershell
# Crear assets básicos para testing
Add-Type -AssemblyName System.Drawing
$banner = New-Object System.Drawing.Bitmap(493, 58)
$graphics = [System.Drawing.Graphics]::FromImage($banner)
$graphics.Clear([System.Drawing.Color]::White)
$graphics.DrawString("GestionTime Desktop v1.1.0", [System.Drawing.Font]::new("Arial", 14), [System.Drawing.Brushes]::Blue, 10, 20)
$banner.Save("Assets\installer-banner.bmp", [System.Drawing.Imaging.ImageFormat]::Bmp)

$dialog = New-Object System.Drawing.Bitmap(493, 312) 
$graphics = [System.Drawing.Graphics]::FromImage($dialog)
$graphics.Clear([System.Drawing.Color]::LightBlue)
$dialog.Save("Assets\installer-dialog.bmp", [System.Drawing.Imaging.ImageFormat]::Bmp)
```

### 4. Assets Opcionales
- **app_logo.ico**: Ya existe en Assets/app_logo.ico ✅
- **License.rtf**: Ya existe en Installer/License.rtf ✅

## Assets Actuales
✅ app_logo.ico - Icono principal
✅ License.rtf - Licencia de software
⚠️ installer-banner.bmp - Crear manualmente o con script
⚠️ installer-dialog.bmp - Crear manualmente o con script