@echo off
cd /d "C:\app\GestionTimeDesktop"

echo 🔧 Configurando entorno para WinUI 3...
set DOTNET_EnableWriteXorExecute=0
set DOTNET_DefaultDiagnosticPortSuspend=0
set COMPlus_EnableDiagnostics=1
set WINRT_RPC_USE_WEBSOCKET=1

echo 🚀 Ejecutando GestionTime Desktop...
start "" "GestionTime.Desktop.exe"

echo ✅ Aplicación iniciada
timeout /t 3