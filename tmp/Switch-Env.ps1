param([string]$Env="Local"); if($Env -eq "Local"){Copy-Item "appsettings.Development.json" "appsettings.json" -Force; Write-Host "LOCAL OK"}else{Write-Host "PROD OK"}
