# Builds a Thunderstore package from the Release build output.
# Package metadata is supplied separately through -PackageSource.
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$PackageSource
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$dll = Join-Path $root "release\plugins\EarlyWarp.dll"
$manifestPath = Join-Path $PackageSource "manifest.json"
$packageReadmePath = Join-Path $PackageSource "README.md"
$iconPath = Join-Path $PackageSource "icon.png"

if (-not (Test-Path -LiteralPath $PackageSource -PathType Container)) {
    throw "Package source directory not found: $PackageSource"
}
if (-not (Test-Path -LiteralPath $dll -PathType Leaf)) {
    throw "Not found: $dll . Build in Release mode first."
}
foreach ($requiredFile in @($manifestPath, $packageReadmePath, $iconPath)) {
    if (-not (Test-Path -LiteralPath $requiredFile -PathType Leaf)) {
        throw "Required package metadata file not found: $requiredFile"
    }
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace($manifest.name) -or
    [string]::IsNullOrWhiteSpace($manifest.version_number)) {
    throw "manifest.json must contain non-empty name and version_number fields."
}

$stage = Join-Path $root "release\package"
if (Test-Path -LiteralPath $stage) {
    $releaseRoot = [System.IO.Path]::GetFullPath((Join-Path $root "release")) + [System.IO.Path]::DirectorySeparatorChar
    $stageFull = [System.IO.Path]::GetFullPath($stage)
    if (-not $stageFull.StartsWith($releaseRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove package stage outside release directory: $stageFull"
    }
    Remove-Item -LiteralPath $stage -Recurse -Force
}

New-Item -ItemType Directory -Path (Join-Path $stage "plugins") -Force | Out-Null
Copy-Item -LiteralPath $dll -Destination (Join-Path $stage "plugins")
Copy-Item -LiteralPath $manifestPath -Destination (Join-Path $stage "manifest.json")
Copy-Item -LiteralPath $packageReadmePath -Destination (Join-Path $stage "README.md")
Copy-Item -LiteralPath $iconPath -Destination (Join-Path $stage "icon.png")
Copy-Item -LiteralPath (Join-Path $root "CHANGELOG.md") -Destination $stage
Copy-Item -LiteralPath (Join-Path $root "LICENSE") -Destination $stage

$packageRoot = Split-Path -Parent $root
$zip = Join-Path $packageRoot ("sinthome-" + $manifest.name + "-" + $manifest.version_number + ".zip")
if (Test-Path -LiteralPath $zip) {
    Remove-Item -LiteralPath $zip -Force
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($stage, $zip)
Write-Host "Created: $zip"
