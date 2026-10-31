param(
    [string]$InstallDirectory = (Join-Path $env:LOCALAPPDATA 'Programs\RGAA Studio')
)

$ErrorActionPreference = 'Stop'
$shortcutPath = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\RGAA Studio.lnk'
if (Test-Path -LiteralPath $shortcutPath) { Remove-Item -LiteralPath $shortcutPath -Force }
if (Test-Path -LiteralPath $InstallDirectory) { Remove-Item -LiteralPath $InstallDirectory -Recurse -Force }
Write-Host 'RGAA Studio was uninstalled. Audit data in LocalAppData was left untouched.'
