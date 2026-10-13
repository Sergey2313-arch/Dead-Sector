#requires -Version 5.1
<#
Exports ONE selected .blend asset from DeadSector_AssetStaging to FBX.
Default: dry_branches_medium_01.
Preview is read-only. -Apply invokes installed Blender in background.
Never overwrites FBX, changes existing Unity .meta, or deletes ZIP archives.
#>
[CmdletBinding()]
param(
    [string]$ProjectRoot = "D:\DEAD SECTOR\Dead Sector_Test",
    [string]$AssetId = "dry_branches_medium_01",
    [string]$StagingRoot = "",
    [string]$BlenderExe = "",
    [int]$MaxTriangles = 100000,
    [switch]$Apply
)
$ErrorActionPreference = "Stop"

$ProjectRoot = [System.IO.Path]::GetFullPath($ProjectRoot).TrimEnd("\")
if ((Split-Path -Path $ProjectRoot -Leaf) -notmatch "(?i)test") {
    throw "Safety stop: choose the TEST Unity project only."
}
if ($AssetId -notmatch "^[A-Za-z0-9_]+$") {
    throw "Invalid AssetId."
}
$manifest = Join-Path $ProjectRoot "Assets\ExternalAssets\ASSET_DOWNLOAD_MANIFEST.csv"
if (-not (Test-Path -LiteralPath $manifest -PathType Leaf)) {
    throw "Manifest not found. Restore the asset library into the test project first."
}
if (-not (Test-Path -LiteralPath (Join-Path $ProjectRoot "Assets\DeadSector") -PathType Container)) {
    throw "Not a Dead Sector Unity project."
}
$branch = ((& git -C $ProjectRoot branch --show-current 2>$null) | Out-String).Trim()
if ($LASTEXITCODE -ne 0 -or $branch -notin @("test/assets-library", "assets/forest-ground-03")) {
    throw "Safety stop: unsupported Git branch '$branch'."
}
$row = @(Import-Csv -LiteralPath $manifest -Encoding UTF8 |
    Where-Object { $_.asset_id -eq $AssetId } | Select-Object -First 1)
if ($row.Count -ne 1) {
    throw "AssetId not present in manifest: $AssetId"
}
if ($row[0].status -notin @("TO_DOWNLOAD", "ALREADY_DOWNLOADED_LOCAL")) {
    throw "Asset not approved for import (status: $($row[0].status))."
}
$relative = [string]$row[0].target_folder_under_Assets_ExternalAssets
if ($relative -notmatch "^[A-Za-z0-9_/-]+$" -or $relative.Contains("..")) {
    throw "Unsafe target path."
}
if ([string]::IsNullOrWhiteSpace($StagingRoot)) {
    $StagingRoot = Join-Path (Split-Path -Parent $ProjectRoot) "DeadSector_AssetStaging"
}
$sourceFolder = Join-Path $StagingRoot $AssetId
if (-not (Test-Path -LiteralPath $sourceFolder -PathType Container)) {
    throw "Blender staging for asset not found: $sourceFolder"
}
$blends = @(Get-ChildItem -LiteralPath $sourceFolder -File -Recurse -Filter "*.blend" |
    Where-Object { $_.Extension.ToLowerInvariant() -eq ".blend" } |
    Sort-Object FullName)
if ($blends.Count -lt 1) {
    throw "No .blend file found in staging for $AssetId"
}
if ($blends.Count -gt 1) {
    Write-Host "Multiple .blend files found; stopping to avoid choosing wrong LOD:" -ForegroundColor Yellow
    $blends | ForEach-Object { Write-Host ("  " + $_.FullName) }
    throw "Choose the proper source manually before continuing."
}
$sourceBlend = $blends[0].FullName
$destination = Join-Path (Join-Path $ProjectRoot ("Assets\ExternalAssets\" + $relative.Replace("/", "\"))) $AssetId
$fbxPath = Join-Path (Join-Path $destination "Models\Converted") ($AssetId + ".fbx")
$pythonScript = Join-Path $PSScriptRoot "Export-DeadSectorBlends.py"
if (-not (Test-Path -LiteralPath $pythonScript -PathType Leaf)) {
    throw "Required exporter script not found: $pythonScript"
}
Write-Host "Dead Sector TEST Blender exporter" -ForegroundColor Cyan
Write-Host "Asset: $AssetId"
Write-Host "Blend: $sourceBlend"
Write-Host "FBX output: $fbxPath"
Write-Host "Triangle limit: $MaxTriangles"
if (Test-Path -LiteralPath $fbxPath) {
    throw "Output already exists; refusing to overwrite: $fbxPath"
}
if (-not $Apply) {
    Write-Host "PREVIEW only; no files changed. Use -Apply after checking paths." -ForegroundColor Green
    return
}
if ([string]::IsNullOrWhiteSpace($BlenderExe)) {
    $command = Get-Command "blender.exe" -ErrorAction SilentlyContinue
    if ($command) { $BlenderExe = $command.Source }
    if (-not $BlenderExe) {
        $installations = @(Get-ChildItem "C:\Program Files\Blender Foundation\Blender *\blender.exe" -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending)
        if ($installations.Count -gt 0) { $BlenderExe = $installations[0].FullName }
    }
}
if (-not $BlenderExe -or -not (Test-Path -LiteralPath $BlenderExe -PathType Leaf)) {
    throw "Blender.exe not found. Specify -BlenderExe with full Blender executable path."
}
if ($MaxTriangles -lt 1) { throw "MaxTriangles must be positive." }
New-Item -ItemType Directory -Path (Split-Path -Parent $fbxPath) -Force | Out-Null
Write-Host "Running Blender in background..." -ForegroundColor Cyan
& $BlenderExe --background --factory-startup $sourceBlend --python $pythonScript -- $fbxPath "$MaxTriangles"
if ($LASTEXITCODE -ne 0) { throw "Blender export failed (exit $LASTEXITCODE). Original file remains untouched." }
if (-not (Test-Path -LiteralPath $fbxPath -PathType Leaf)) {
    throw "Blender finished but no FBX was created."
}
Write-Host ("SUCCESS: " + $fbxPath) -ForegroundColor Green
Write-Host "Next: open Unity and set up URP/Lit materials using the existing textures."
