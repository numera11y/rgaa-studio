param(
    [string]$Version = '',
    [string]$LinuxDistro = 'Ubuntu-24.04',
    [string]$WebView2InstallerPath = ''
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
if ([string]::IsNullOrWhiteSpace($Version)) {
    $versionPropertiesPath = Join-Path $repositoryRoot 'Directory.Build.props'
    if (-not (Test-Path -LiteralPath $versionPropertiesPath)) { throw "Shared version properties not found at $versionPropertiesPath" }
    [xml]$versionProperties = [System.IO.File]::ReadAllText($versionPropertiesPath)
    $Version = [string]$versionProperties.Project.PropertyGroup.Version
    if ([string]::IsNullOrWhiteSpace($Version)) { throw "Version is missing from $versionPropertiesPath" }
}
$artifactRoot = Join-Path $repositoryRoot 'artifacts'
if (-not (Test-Path -LiteralPath $artifactRoot)) {
    throw "Expected build output directory is missing: $artifactRoot"
}

$dotnet = Get-Command 'dotnet' -ErrorAction SilentlyContinue
$dotnetExe = if ($dotnet) { $dotnet.Source } elseif (Test-Path -LiteralPath 'C:\Program Files\dotnet\dotnet.exe') { 'C:\Program Files\dotnet\dotnet.exe' } else { throw '.NET SDK 10 is required.' }
$makensis = Get-Command 'makensis' -ErrorAction SilentlyContinue
$makensisExe = if ($makensis) { $makensis.Source } elseif (Test-Path -LiteralPath 'C:\Program Files (x86)\NSIS\makensis.exe') { 'C:\Program Files (x86)\NSIS\makensis.exe' } else { throw 'NSIS 3 is required to build the Windows setup executable.' }
$nsisDirectory = Split-Path -Parent $makensisExe
$nsisLicense = Join-Path $nsisDirectory 'COPYING'
if (-not (Test-Path -LiteralPath $nsisLicense)) { throw "NSIS license notice not found at $nsisLicense" }
$project = Join-Path $repositoryRoot 'src\RgaaStudio.Desktop\RgaaStudio.Desktop.csproj'
$releaseRoot = Join-Path $artifactRoot "packages\$Version"
$windowsPublish = Join-Path $releaseRoot 'windows-publish'
$windowsStage = Join-Path $releaseRoot 'windows-zip'
$linuxPublish = Join-Path $releaseRoot 'linux-x64-publish'
$linuxPackages = Join-Path $releaseRoot 'linux-x64'
$windowsInstaller = Join-Path $releaseRoot "rgaa-studio-$Version-windows-x64-setup.exe"
$headerBitmap = Join-Path $releaseRoot 'installer-header.bmp'
$nsisCopying = Join-Path $releaseRoot 'NSIS-COPYING.txt'
$nsisNotice = Join-Path $releaseRoot 'NSIS-NOTICE.txt'
$releaseLicense = Join-Path $releaseRoot 'LICENSE.txt'
$appIconSource = Join-Path $repositoryRoot 'src\RgaaStudio.Desktop\Assets\rgaa-studio-app-icon.svg'
$appIcon = Join-Path $repositoryRoot 'src\RgaaStudio.Desktop\Assets\RgaaStudio.ico'
$iconBuilder = Join-Path $repositoryRoot 'tools\packaging\RgaaStudio.IconBuilder\RgaaStudio.IconBuilder.csproj'
$webView2Version = '156.0.4314.58'
$webView2Url = 'https://msedge.sf.dl.delivery.mp.microsoft.com/filestreamingservice/files/d5cc3c1c-bb59-4158-aedc-db0035e44466/MicrosoftEdgeWebView2RuntimeInstallerX64.exe'
$webView2Sha256 = '62863f0f0611a16ccf124b4f15650c3b66624504eba74d7cde1e2ae5d951249b'
$webView2Cache = Join-Path $env:LOCALAPPDATA "Numera11y\RGAAStudio\PackagingCache\MicrosoftEdgeWebView2RuntimeInstallerX64-$webView2Version.exe"
$webView2Installer = Join-Path $releaseRoot 'MicrosoftEdgeWebView2RuntimeInstallerX64.exe'
$webView2Notice = Join-Path $releaseRoot 'WEBVIEW2-RUNTIME-NOTICE.txt'
$webView2NoticeSource = Join-Path $PSScriptRoot 'WEBVIEW2-RUNTIME-NOTICE.txt'
$versionCore = ($Version -split '[-+]', 2)[0]
$versionParts = @($versionCore.Split('.'))
if ($versionParts.Count -eq 3) { $versionParts += '0' }
if ($versionParts.Count -ne 4 -or ($versionParts | Where-Object { $_ -notmatch '^\d+$' }).Count -gt 0) {
    throw "Version '$Version' must start with a numeric x.y.z version to stamp Windows executable metadata."
}
$versionNumeric = $versionParts -join '.'

function ConvertTo-WslPath([string]$Path) {
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if ($fullPath -notmatch '^(?<drive>[A-Za-z]):\\(?<rest>.*)$') {
        throw "WSL package paths must be on a mounted drive: $fullPath"
    }
    return "/mnt/$($Matches.drive.ToLowerInvariant())/$($Matches.rest.Replace('\', '/'))"
}

function New-InstallerHeaderBitmap([string]$Path) {
    Add-Type -AssemblyName System.Drawing
    $bitmap = [System.Drawing.Bitmap]::new(150, 57, [System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $gradient = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
        [System.Drawing.Rectangle]::new(0, 0, 150, 57),
        [System.Drawing.Color]::FromArgb(244, 238, 255),
        [System.Drawing.Color]::FromArgb(253, 237, 230),
        [System.Drawing.Drawing2D.LinearGradientMode]::Horizontal)
    $accent = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(33, 75, 154))
    $titleBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(23, 31, 59))
    $captionBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(69, 83, 109))
    $titleFont = [System.Drawing.Font]::new('Segoe UI', 10, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Point)
    $captionFont = [System.Drawing.Font]::new('Segoe UI', 6.5, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Point)
    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
        $graphics.FillRectangle($gradient, 0, 0, 150, 57)
        $graphics.FillRectangle($accent, 0, 0, 4, 57)
        $graphics.DrawString('RGAA Studio', $titleFont, $titleBrush, 12, 8)
        $graphics.DrawString('RGAA  /  WCAG  ·  ACCESSIBILITY AUDIT', $captionFont, $captionBrush, 12, 31)
        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Bmp)
    }
    finally {
        $captionFont.Dispose()
        $titleFont.Dispose()
        $captionBrush.Dispose()
        $titleBrush.Dispose()
        $accent.Dispose()
        $gradient.Dispose()
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

foreach ($path in @($windowsPublish, $windowsStage, $linuxPublish, $linuxPackages, $windowsInstaller, $headerBitmap, $nsisCopying, $nsisNotice, $releaseLicense, $webView2Installer, $webView2Notice)) {
    if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Recurse -Force }
}
New-Item -ItemType Directory -Path $windowsStage -Force | Out-Null
New-Item -ItemType Directory -Path $linuxPackages -Force | Out-Null

if (-not [string]::IsNullOrWhiteSpace($WebView2InstallerPath)) {
    $webView2Source = (Resolve-Path -LiteralPath $WebView2InstallerPath).Path
    $sourceHash = (Get-FileHash -LiteralPath $webView2Source -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($sourceHash -ne $webView2Sha256) { throw "WebView2 installer hash mismatch: expected $webView2Sha256, received $sourceHash." }
    New-Item -ItemType Directory -Path (Split-Path -Parent $webView2Cache) -Force | Out-Null
    Copy-Item -LiteralPath $webView2Source -Destination $webView2Cache -Force
}
if (-not (Test-Path -LiteralPath $webView2Cache) -or (Get-FileHash -LiteralPath $webView2Cache -Algorithm SHA256).Hash.ToLowerInvariant() -ne $webView2Sha256) {
    $curl = Get-Command 'curl.exe' -ErrorAction SilentlyContinue
    if (-not $curl) { throw 'curl.exe is required to fetch the Microsoft WebView2 Evergreen standalone installer.' }
    New-Item -ItemType Directory -Path (Split-Path -Parent $webView2Cache) -Force | Out-Null
    Write-Host "Downloading Microsoft WebView2 Evergreen Standalone Runtime $webView2Version (x64)..."
    & $curl.Source -fL --retry 2 --connect-timeout 30 --max-time 900 --output $webView2Cache $webView2Url
    if ($LASTEXITCODE -ne 0) { throw "WebView2 standalone download failed with exit code $LASTEXITCODE." }
    $downloadHash = (Get-FileHash -LiteralPath $webView2Cache -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($downloadHash -ne $webView2Sha256) { Remove-Item -LiteralPath $webView2Cache -Force; throw "Downloaded WebView2 installer hash mismatch: expected $webView2Sha256, received $downloadHash." }
}
Copy-Item -LiteralPath $webView2Cache -Destination $webView2Installer -Force
Copy-Item -LiteralPath $webView2NoticeSource -Destination $webView2Notice -Force
Write-Host "Using verified Microsoft WebView2 standalone runtime $webView2Version (SHA-256 $webView2Sha256)."

Write-Host 'Generating the Windows executable icon from the numera11y application SVG...'
& $dotnetExe run --project $iconBuilder -c Release -- $appIconSource $appIcon
if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $appIcon)) { throw 'RGAA Studio application icon generation failed.' }

Write-Host "Publishing Windows x64 ($Version)..."
& $dotnetExe publish $project -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -p:Version=$Version `
    -p:AssemblyVersion=$versionNumeric -p:FileVersion=$versionNumeric -p:InformationalVersion=$Version -o $windowsPublish
if ($LASTEXITCODE -ne 0) { throw "Windows publish failed with exit code $LASTEXITCODE." }

$appVersionInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $windowsPublish 'RgaaStudio.Desktop.exe'))
if ($appVersionInfo.ProductName -ne 'RGAA Studio' -or -not $appVersionInfo.ProductVersion.StartsWith($Version, [StringComparison]::Ordinal)) {
    throw "The Windows app binary does not contain the requested product/version metadata ($($appVersionInfo.ProductName), $($appVersionInfo.ProductVersion))."
}
Write-Host "Windows app binary metadata: Product=$($appVersionInfo.ProductName), FileVersion=$($appVersionInfo.FileVersion), ProductVersion=$($appVersionInfo.ProductVersion)"

Copy-Item -Path (Join-Path $windowsPublish '*') -Destination $windowsStage -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Install-Windows.ps1') -Destination $windowsStage
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Uninstall-Windows.ps1') -Destination $windowsStage
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'NSIS-NOTICE.txt') -Destination $windowsStage
Copy-Item -LiteralPath $nsisLicense -Destination (Join-Path $windowsStage 'NSIS-COPYING.txt')
Copy-Item -LiteralPath $webView2Notice -Destination $windowsStage
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'LICENSE') -Destination $releaseLicense
Copy-Item -LiteralPath $releaseLicense -Destination $windowsStage
$windowsReadme = [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot 'README-Windows.txt')).Replace('@APP_VERSION@', $Version)
[System.IO.File]::WriteAllText((Join-Path $windowsStage 'README-Windows.txt'), $windowsReadme, [System.Text.UTF8Encoding]::new($true))
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'DISTRIBUTION-NOTICE.txt') -Destination $windowsStage
$windowsZip = Join-Path $releaseRoot "rgaa-studio-$Version-windows-x64.zip"
Compress-Archive -Path (Join-Path $windowsStage '*') -DestinationPath $windowsZip -CompressionLevel Optimal -Force

Write-Host "Building branded Windows setup executable ($Version)..."
New-InstallerHeaderBitmap -Path $headerBitmap
Copy-Item -LiteralPath $nsisLicense -Destination $nsisCopying
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'NSIS-NOTICE.txt') -Destination $nsisNotice
& $makensisExe /INPUTCHARSET UTF8 "/DAPP_VERSION=$Version" "/DAPP_VERSION_NUMERIC=$versionNumeric" "/DAPP_PAYLOAD_DIR=$windowsPublish" `
    "/DAPP_SETUP_OUT=$windowsInstaller" "/DAPP_HEADER_BITMAP=$headerBitmap" "/DAPP_ICON=$(Join-Path $repositoryRoot 'src\RgaaStudio.Desktop\Assets\RgaaStudio.ico')" `
    "/DAPP_WEBVIEW2_INSTALLER=$webView2Installer" "/DAPP_WEBVIEW2_NOTICE=$webView2Notice" `
    "/DAPP_LICENSE_FILE=$(Join-Path $repositoryRoot 'LICENSE')" (Join-Path $PSScriptRoot 'RGAAStudioSetup.nsi')
if ($LASTEXITCODE -ne 0) { throw "NSIS setup build failed with exit code $LASTEXITCODE." }
$setupVersionInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($windowsInstaller)
if ($setupVersionInfo.FileVersion -ne $versionNumeric -or -not $setupVersionInfo.ProductVersion.StartsWith($Version, [StringComparison]::Ordinal)) {
    throw "Setup executable version resources did not match $Version ($($setupVersionInfo.FileVersion), $($setupVersionInfo.ProductVersion))."
}
Write-Host "Setup executable metadata: FileVersion=$($setupVersionInfo.FileVersion), ProductVersion=$($setupVersionInfo.ProductVersion)"
Remove-Item -LiteralPath $headerBitmap -Force
Remove-Item -LiteralPath $webView2Installer -Force

Write-Host "Publishing Linux x64 payload ($Version)..."
& $dotnetExe publish $project -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=false `
    -p:UseAppHost=true -p:DebugType=None -p:DebugSymbols=false -p:Version=$Version -o $linuxPublish
if ($LASTEXITCODE -ne 0) { throw "Linux publish failed with exit code $LASTEXITCODE." }

$linuxPackagingScript = ConvertTo-WslPath (Join-Path $PSScriptRoot 'build-linux-packages.sh')
$linuxPayloadPath = ConvertTo-WslPath $linuxPublish
$linuxPackagePath = ConvertTo-WslPath $linuxPackages

Write-Host "Assembling Debian and RPM packages in WSL ($LinuxDistro)..."
& wsl.exe -d $LinuxDistro -- bash $linuxPackagingScript $linuxPayloadPath $linuxPackagePath $Version
if ($LASTEXITCODE -ne 0) { throw "Linux package assembly failed with exit code $LASTEXITCODE." }

$packageFiles = @(Get-ChildItem -LiteralPath $linuxPackages -File) + @(Get-Item -LiteralPath $windowsZip)
$packageFiles += Get-Item -LiteralPath $windowsInstaller
$packageFiles += Get-Item -LiteralPath $nsisCopying
$packageFiles += Get-Item -LiteralPath $nsisNotice
$packageFiles += Get-Item -LiteralPath $webView2Notice
$packageFiles += Get-Item -LiteralPath $releaseLicense
$checksumLines = foreach ($package in $packageFiles) {
    $hash = (Get-FileHash -LiteralPath $package.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    $relativePath = $package.FullName.Substring($releaseRoot.TrimEnd('\').Length + 1).Replace('\', '/')
    "$hash  $relativePath"
}
$checksumPath = Join-Path $releaseRoot 'SHA256SUMS.txt'
[System.IO.File]::WriteAllLines($checksumPath, $checksumLines, [System.Text.UTF8Encoding]::new($false))

Remove-Item -LiteralPath $windowsPublish, $windowsStage, $linuxPublish -Recurse -Force

Write-Host 'Packages created:'
foreach ($package in $packageFiles) { Write-Host $package.FullName }
Write-Host "Checksums: $checksumPath"
Write-Host 'Release notice: Linux runtime installation still requires validation on Ubuntu 26.04 and Fedora 44.'
