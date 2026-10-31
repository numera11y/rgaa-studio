param(
    [string]$InstallDirectory = (Join-Path $env:LOCALAPPDATA 'Programs\RGAA Studio')
)

$ErrorActionPreference = 'Stop'
$packageDirectory = $PSScriptRoot
$executable = Join-Path $packageDirectory 'RgaaStudio.Desktop.exe'
if (-not (Test-Path -LiteralPath $executable)) {
    throw 'RgaaStudio.Desktop.exe was not found. Run this script from the extracted ZIP folder.'
}

New-Item -ItemType Directory -Path $InstallDirectory -Force | Out-Null
Get-ChildItem -LiteralPath $packageDirectory -Force | Where-Object { $_.Name -notin @('Install-Windows.ps1', 'Uninstall-Windows.ps1') } |
    ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $InstallDirectory -Recurse -Force }

$startMenu = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs'
New-Item -ItemType Directory -Path $startMenu -Force | Out-Null
$shortcutPath = Join-Path $startMenu 'RGAA Studio.lnk'
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = Join-Path $InstallDirectory 'RgaaStudio.Desktop.exe'
$shortcut.WorkingDirectory = $InstallDirectory
$shortcut.Description = 'RGAA Studio — espace de travail d’audit d’accessibilité'
$shortcut.Save()

Write-Host "RGAA Studio installed in: $InstallDirectory"
Write-Host "Start menu shortcut created: $shortcutPath"
Write-Host 'Microsoft Edge WebView2 Runtime is required. Audit data in LocalAppData is not removed by the uninstaller.'
