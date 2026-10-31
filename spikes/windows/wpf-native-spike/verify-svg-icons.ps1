$ErrorActionPreference = 'Stop'

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
$iconSourceRoot = Join-Path $repoRoot 'docs\_sources\images'
$projectFile = Join-Path $PSScriptRoot 'RgaaAuditor.Windows.csproj'
$xamlFile = Join-Path $PSScriptRoot 'MainWindow.xaml'
$codeFile = Join-Path $PSScriptRoot 'MainWindow.xaml.cs'
$chevronFile = Join-Path $PSScriptRoot 'Assets\chevron-right.svg'
$localStateIcons = @('menu-open.svg', 'maximize.svg', 'restore.svg')

$expectedIcons = @(
    'menu.svg', 'projects.svg', 'update.svg', 'panel-right.svg', 'page.svg',
    'minus.svg', 'house.svg', 'close.svg', 'search.svg', 'plus.svg', 'globe.svg'
)

foreach ($icon in $expectedIcons) {
    $path = Join-Path $iconSourceRoot $icon
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required Lucide SVG is missing from docs/_sources/images."
    }

    $source = Get-Content -LiteralPath $path -Raw -Encoding UTF8
    if ($source -notmatch '<svg\b[^>]*\bwidth="24"[^>]*\bheight="24"[^>]*\bviewBox="0 0 24 24"') {
        throw "SVG does not have the expected 24x24 canvas and viewBox."
    }
}

$project = Get-Content -LiteralPath $projectFile -Raw -Encoding UTF8
if ($project -notmatch '<PackageReference\s+Include="SharpVectors\.Wpf"\s+Version="1\.8\.6"') {
    throw 'The pinned SharpVectors.Wpf renderer is missing from the project.'
}
if ($project -notmatch 'docs\\_sources\\images\\\*\.svg' -or
    $project -notmatch 'SHARPVECTORS-LICENSE\.txt' -or
    $project -notmatch 'LUCIDE-LICENSE\.txt' -or
    $project -notmatch 'Assets\\chevron-right\.svg' -or
    $project -notmatch 'Assets\\menu-open\.svg' -or
    $project -notmatch 'Assets\\maximize\.svg' -or
    $project -notmatch 'Assets\\restore\.svg') {
    throw 'The SVG resource mapping or required icon/license notices are missing.'
}
$chevron = Get-Content -LiteralPath $chevronFile -Raw -Encoding UTF8
if ($chevron -notmatch '<svg\b[^>]*\bwidth="24"[^>]*\bheight="24"[^>]*\bviewBox="0 0 24 24"') { throw 'The Lucide card chevron asset is missing or malformed.' }
foreach ($icon in $localStateIcons) {
    $path = Join-Path $PSScriptRoot "Assets\$icon"
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw 'A required local state icon is missing.' }
    $source = Get-Content -LiteralPath $path -Raw -Encoding UTF8
    if ($source -notmatch '<svg\b[^>]*\bwidth="24"[^>]*\bheight="24"[^>]*\bviewBox="0 0 24 24"') { throw 'A local state icon is malformed.' }
}

$xaml = Get-Content -LiteralPath $xamlFile -Raw -Encoding UTF8
$code = Get-Content -LiteralPath $codeFile -Raw -Encoding UTF8
$pattern = '<Image\b(?=[^>]*\bSource="\{svgc:SvgImage Source=/Assets/Icons/(?<file>[^,]+), AppName=RgaaAuditor\.Windows\}")(?=[^>]*\bWidth="(?<width>\d+)")(?=[^>]*\bHeight="(?<height>\d+)")[^>]*>'
$renderedIcons = [regex]::Matches($xaml, $pattern)
$renderedNames = @{}
foreach ($match in $renderedIcons) {
    $name = $match.Groups['file'].Value
    $width = [int] $match.Groups['width'].Value
    $height = [int] $match.Groups['height'].Value
    if ($width -lt 12 -or $width -gt 24 -or $height -lt 12 -or $height -gt 24) {
        throw "Rendered icon size is outside the expected 12-24 DIP range: $name"
    }
    $renderedNames[$name] = $true
}

$expectedRendered = @($expectedIcons) + @($localStateIcons)
foreach ($icon in $expectedRendered) {
    if (-not $renderedNames.ContainsKey($icon)) {
        throw "Required SVG is not referenced by a sized Image in MainWindow.xaml."
    }
}

if ($xaml -notmatch 'chevron-right\.svg' -or $xaml -notmatch 'Reprendre') { throw 'The card resume cue and chevron are not both present.' }
if ($code -notmatch 'foreach\s*\(var\s+menuItem\s+in\s+OptionsMenu\.Items\.OfType<MenuItem>\(\)\)\s*menuItem\.Icon\s*=\s*null;') { throw 'The Options popup must stay text-only, without icons.' }
Write-Output "PASS: $($expectedIcons.Count) 24x24 Lucide source SVGs plus the card chevron, $($renderedIcons.Count) explicitly sized SharpVectors images (12-24 DIPs), resource linking and license notices verified."
