param(
    [string] $AuditRoot = (Join-Path $env:LOCALAPPDATA 'RgaaStudioSpike\AuditDossier'),
    [ValidateRange(1, 100)] [int] $MinimumAudits = 2
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $AuditRoot -PathType Container)) {
    throw "Audit dossier directory not found. Create synthetic audits first."
}

$validAudits = @()
foreach ($directory in Get-ChildItem -LiteralPath $AuditRoot -Directory) {
    $auditId = [Guid]::Empty
    if (-not [Guid]::TryParseExact($directory.Name, 'N', [ref] $auditId)) {
        continue # Ignore older, non-GUID demo folders from earlier spike iterations.
    }

    $manifestPath = Join-Path $directory.FullName 'manifest.json'
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        throw "Manifest missing for one synthetic audit."
    }

    $manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($manifest.Id -ne $directory.Name) {
        throw "Manifest ID does not match its dossier directory."
    }

    if ($null -ne $manifest.PSObject.Properties['FaviconFile'] -and $null -ne $manifest.FaviconFile -and [string] $manifest.FaviconFile -ne '') {
        if ([string] $manifest.FaviconFile -ne 'favicon.png') {
            throw "Favicon metadata must use the fixed local name favicon.png."
        }
        if (-not (Test-Path -LiteralPath (Join-Path $directory.FullName 'favicon.png') -PathType Leaf)) {
            throw "Manifest favicon metadata points to a missing local PNG."
        }
    }

    $decisionFiles = @(Get-ChildItem -LiteralPath (Join-Path $directory.FullName 'decisions') -Filter '*.json' -File -ErrorAction SilentlyContinue)
    $decisionIds = @{}
    foreach ($file in $decisionFiles) {
        $decision = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($decision.AuditId -ne $directory.Name) {
            throw "Decision event is stored under a different audit ID."
        }
        if ($decisionIds.ContainsKey([string] $decision.Id)) {
            throw "Duplicate decision event ID in one audit."
        }
        $decisionIds[[string] $decision.Id] = $true
    }

    $proofFiles = @(Get-ChildItem -LiteralPath $directory.FullName -Filter 'synthetique-*.json' -File)

    foreach ($file in $proofFiles) {
        $proof = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($proof.AuditId -ne $directory.Name) {
            throw "Evidence record is stored under a different audit ID."
        }
        if ([IO.Path]::GetFileName([string] $proof.ScreenshotFile) -ne [string] $proof.ScreenshotFile) {
            throw "Evidence image name is not a local file name."
        }
        if (-not (Test-Path -LiteralPath (Join-Path $directory.FullName $proof.ScreenshotFile) -PathType Leaf)) {
            throw "Evidence screenshot is missing from its audit dossier."
        }
    }

    # Open/new dossiers are valid before they have decisions or evidence. Count only
    # dossiers containing both records as the completed synthetic cross-association smoke set.
    if ($decisionFiles.Count -gt 0 -and $proofFiles.Count -gt 0) { $validAudits += $directory.Name }
}

if ($validAudits.Count -lt $MinimumAudits) {
    throw "Expected at least $MinimumAudits valid synthetic audit dossiers; found $($validAudits.Count)."
}

Write-Output "PASS: $($validAudits.Count) synthetic audit dossiers have matching manifests, decision events, and locally associated screenshots."
