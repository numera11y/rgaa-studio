param(
    [Parameter(Mandatory = $true)][string]$OutputDirectory,
    [Parameter(Mandatory = $true)][string]$DatabasePath,
    [switch]$MenuOnly,
    [switch]$CreateOnly,
    [string]$DotnetPath = 'C:\Program Files\dotnet\dotnet.exe'
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$output = [System.IO.Path]::GetFullPath($OutputDirectory)
$database = [System.IO.Path]::GetFullPath($DatabasePath)
New-Item -ItemType Directory -Path $output -Force | Out-Null

Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class PressKitNative {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int command);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
}
'@

function Wait-Ui([int]$Milliseconds = 800) { Start-Sleep -Milliseconds $Milliseconds; [System.Windows.Forms.Application]::DoEvents() }
Add-Type -AssemblyName System.Windows.Forms

function Get-AppWindow {
    $process = Get-Process | Where-Object { $_.ProcessName -eq 'RgaaStudio.Desktop' } | Select-Object -First 1
    if (-not $process) { throw 'RGAA Studio process not found.' }
    $windows = [System.Windows.Automation.AutomationElement]::RootElement.FindAll(
        [System.Windows.Automation.TreeScope]::Children,
        [System.Windows.Automation.Condition]::TrueCondition)
    foreach ($element in $windows) {
        if ($element.Current.ProcessId -eq $process.Id -and $element.Current.Name -eq 'RGAA Studio') {
            return [pscustomobject]@{ Id = $process.Id; MainWindowHandle = [IntPtr]$element.Current.NativeWindowHandle }
        }
    }
    $process.Refresh()
    if ($process.MainWindowHandle) { return [pscustomobject]@{ Id = $process.Id; MainWindowHandle = $process.MainWindowHandle } }
    throw 'RGAA Studio main window not found.'
}

function Get-AppElement([string]$Name, [System.Windows.Automation.ControlType]$Type = [System.Windows.Automation.ControlType]::Button) {
    $window = Get-AppWindow
    $roots = @(
        [System.Windows.Automation.AutomationElement]::FromHandle($window.MainWindowHandle),
        [System.Windows.Automation.AutomationElement]::RootElement)
    $found = $null
    foreach ($root in $roots) {
        $condition = [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty, $Name)
        $matches = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $condition)
        foreach ($candidate in $matches) {
            if ($candidate.Current.ControlType -eq $Type -and -not $candidate.Current.IsOffscreen) { $found = $candidate; break }
        }
        if ($found) { break }
    }
    if (-not $found) { throw "UI element not found: $Name" }
    return $found
}

function Get-AppElementMatch([string]$Pattern, [System.Windows.Automation.ControlType]$Type = [System.Windows.Automation.ControlType]::Button) {
    $window = Get-AppWindow
    $roots = @(
        [System.Windows.Automation.AutomationElement]::FromHandle($window.MainWindowHandle),
        [System.Windows.Automation.AutomationElement]::RootElement)
    foreach ($root in $roots) {
        $matches = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
        foreach ($candidate in $matches) {
            if ($candidate.Current.ControlType -eq $Type -and -not $candidate.Current.IsOffscreen -and $candidate.Current.Name -match $Pattern) { return $candidate }
        }
    }
    throw "UI element matching pattern not found: $Pattern"
}

function Invoke-AppButton([string]$Name) {
    $element = $null
    foreach ($type in @([System.Windows.Automation.ControlType]::Button, [System.Windows.Automation.ControlType]::MenuItem)) {
        try { $element = Get-AppElement $Name $type; break } catch { }
    }
    if (-not $element) { throw "Clickable UI element not found: $Name" }
    $pattern = $null
    if ($element.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$pattern)) { $pattern.Invoke() }
    else { $element.SetFocus(); [System.Windows.Forms.SendKeys]::SendWait('{ENTER}') }
    Wait-Ui 900
}

function Invoke-AppButtonMatch([string]$Pattern) {
    $element = $null
    foreach ($type in @([System.Windows.Automation.ControlType]::Button, [System.Windows.Automation.ControlType]::MenuItem)) {
        try { $element = Get-AppElementMatch $Pattern $type; break } catch { }
    }
    if (-not $element) { throw "Clickable UI element matching pattern not found: $Pattern" }
    $invoke = $null
    if ($element.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$invoke)) { $invoke.Invoke() }
    else { $element.SetFocus(); [System.Windows.Forms.SendKeys]::SendWait('{ENTER}') }
    Wait-Ui 850
}

function Set-AppText([string]$Name, [string]$Value) {
    $element = Get-AppElement $Name ([System.Windows.Automation.ControlType]::Edit)
    $pattern = $null
    if (-not $element.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref]$pattern)) { throw "No value pattern: $Name" }
    $pattern.SetValue($Value)
}

function Click-At([int]$X, [int]$Y) {
    [PressKitNative]::SetCursorPos($X, $Y) | Out-Null
    [PressKitNative]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    [PressKitNative]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    Wait-Ui 600
}

function Save-AppScreenshot([string]$Name) {
    $window = Get-AppWindow
    [PressKitNative]::SetForegroundWindow($window.MainWindowHandle) | Out-Null
    [PressKitNative]::SetCursorPos(1000, 75) | Out-Null
    Wait-Ui 350
    $rect = New-Object PressKitNative+RECT
    [PressKitNative]::GetWindowRect($window.MainWindowHandle, [ref]$rect) | Out-Null
    $width = $rect.Right - $rect.Left
    $height = $rect.Bottom - $rect.Top
    $bitmap = New-Object System.Drawing.Bitmap($width, $height, [System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.CopyFromScreen($rect.Left, $rect.Top, 0, 0, $bitmap.Size)
    $path = Join-Path $output ($Name + '.png')
    $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $graphics.Dispose(); $bitmap.Dispose()
    Write-Host "Captured $path (${width}x${height})"
}

$env:RGAA_STUDIO_DATABASE_PATH = $database
$env:RGAA_STUDIO_AUDIT_ROOT = Join-Path (Split-Path $database) 'audit-data'
$env:RGAA_STUDIO_SKIP_LEGACY_IMPORT = '1'
& $DotnetPath run --project (Join-Path $PSScriptRoot 'PressKit.Seed.csproj') -- $database
if ($LASTEXITCODE -ne 0) { throw "Press-kit seed failed: $LASTEXITCODE" }

$appProject = Join-Path $projectRoot 'src\RgaaStudio.Desktop\RgaaStudio.Desktop.csproj'
$appProcess = Start-Process -FilePath $DotnetPath -ArgumentList @('run', '--no-build', '--project', $appProject) -WorkingDirectory $projectRoot -PassThru
$window = $null
try {
    $deadline = (Get-Date).AddSeconds(45)
    do {
        Start-Sleep -Milliseconds 500
        $window = Get-Process | Where-Object { $_.MainWindowTitle -eq 'RGAA Studio' } | Select-Object -First 1
    } while (-not $window -and (Get-Date) -lt $deadline)
    if (-not $window -or -not $window.MainWindowHandle) { throw 'RGAA Studio did not open its main window.' }
    [PressKitNative]::ShowWindow($window.MainWindowHandle, 3) | Out-Null
    [PressKitNative]::SetForegroundWindow($window.MainWindowHandle) | Out-Null
    Wait-Ui 2500
    Save-AppScreenshot '01-mes-audits'
    Invoke-AppButton 'Options'
    Save-AppScreenshot '02-menu-options'
    Invoke-AppButtonMatch '^Param.tres$'
    Wait-Ui 600
    Save-AppScreenshot '03-parametres'
    Invoke-AppButton 'Annuler'
    Wait-Ui 500
    Invoke-AppButton 'Options'
    Invoke-AppButtonMatch '^. propos$'
    Wait-Ui 600
    Save-AppScreenshot '04-a-propos'
    Invoke-AppButton 'Fermer'
    Wait-Ui 500

    Click-At 790 185
    Wait-Ui 2000
    Save-AppScreenshot '05-tableau-de-bord'
    Click-At 90 280
    Wait-Ui 1500
    Save-AppScreenshot '06-espace-audit-site'
    Click-At 90 197
    Wait-Ui 800
    Save-AppScreenshot '07-tableau-de-bord'
    Click-At 90 280
    Wait-Ui 1200
    Click-At 315 142
    Wait-Ui 250
    Save-AppScreenshot '08-menu-vues-page'
    if ($MenuOnly) { return }
    Click-At 320 240
    Wait-Ui 800
    Save-AppScreenshot '09-code-source'
    Click-At 315 142
    Click-At 320 205
    Wait-Ui 700
    Click-At 315 142
    Click-At 320 270
    Wait-Ui 700
    Save-AppScreenshot '10-site-sans-css'
    Click-At 315 142
    Click-At 320 300
    Wait-Ui 700
    Save-AppScreenshot '11-html5-semantique'
    Click-At 315 142
    Click-At 320 335
    Wait-Ui 700
    Save-AppScreenshot '12-images-medias'
    Click-At 315 142
    Click-At 320 365
    Wait-Ui 700
    Save-AppScreenshot '13-vue-aria'
    Click-At 315 142
    Click-At 320 373
    Wait-Ui 900
    Save-AppScreenshot '14-technologies-seo'
    Click-At 460 142
    Wait-Ui 2500
    Save-AppScreenshot '15-resultats-axe-core'

    Click-At 315 142
    Click-At 320 205
    Wait-Ui 900
    Click-At 90 350
    Wait-Ui 2200
    Save-AppScreenshot '16-page-offre-audit'
    Click-At 90 435
    Wait-Ui 1800
    Save-AppScreenshot '17-page-offres'
    Click-At 90 505
    Wait-Ui 1800
    Save-AppScreenshot '18-page-realisations'
    Click-At 90 580
    Wait-Ui 1800
    Save-AppScreenshot '19-page-rendez-vous'
    Click-At 90 650
    Wait-Ui 1800
    Save-AppScreenshot '20-page-ressources'
    Click-At 90 720
    Wait-Ui 1800
    Save-AppScreenshot '21-page-mentions-legales'
    Click-At 90 280
    Wait-Ui 1700

    Click-At 590 142
    Wait-Ui 350
    Click-At 500 500
    Wait-Ui 700
    Save-AppScreenshot '22-pipette-contraste'
    Click-At 1752 23
    Wait-Ui 450
    Save-AppScreenshot '23-espace-web-etendu'
    Click-At 1752 23
    Wait-Ui 450
    Click-At 210 155
    Wait-Ui 450
    Save-AppScreenshot '24-liste-pages-repliee'

    Click-At 1669 23
    Wait-Ui 700
    Click-At 700 314
    [System.Windows.Forms.SendKeys]::SendWait('https://www.numera11y.fr')
    Click-At 522 364
    Click-At 800 470
    Wait-Ui 300
    Click-At 560 512
    Wait-Ui 500
    Click-At 800 545
    Wait-Ui 300
    Click-At 560 588
    Wait-Ui 500
    Save-AppScreenshot '25-creation-audit'
    Click-At 800 470
    Wait-Ui 250
    Click-At 560 540
    Wait-Ui 400
    Click-At 800 550
    Wait-Ui 250
    Click-At 560 590
    Wait-Ui 450
    Click-At 800 630
    Wait-Ui 250
    Click-At 560 706
    Wait-Ui 450
    Save-AppScreenshot '26-creation-wcag-niveau'
    if ($CreateOnly) { return }
}
finally {
    if ($window) { Stop-Process -Id $window.Id -Force -ErrorAction SilentlyContinue }
    if ($appProcess -and -not $appProcess.HasExited) { Stop-Process -Id $appProcess.Id -Force -ErrorAction SilentlyContinue }
}
