#requires -Version 5.1
<#
Dead Sector asset organizer - TEST PROJECT ONLY.
Run without -Apply to preview matching archives.
Run with -Apply to extract and copy FBX/OBJ/DAE plus image textures.
Never deletes downloads; never overwrites Unity asset files.
Archives containing only .blend / .glb are staged outside Assets for manual conversion.
Sketchfab generic ZIP filenames MUST be renamed to the "asset_id" from CSV:
  makarov_scape.zip, aks74_armoredwave.zip, etc.
Poly Haven original named archives like fir_tree_01_2k.blend.zip are recognized.
#>
[CmdletBinding()]
param(
    [string]$ProjectRoot = "D:\DEAD SECTOR\Dead Sector_Test",
    [string]$Downloads = (Join-Path $env:USERPROFILE "Downloads"),
    [string]$StagingRoot = "",
    [switch]$Apply
)
$ErrorActionPreference = "Stop"

$ProjectRoot = [System.IO.Path]::GetFullPath($ProjectRoot).TrimEnd('\')
if (-not (Test-Path -LiteralPath $ProjectRoot -PathType Container)) {
    throw "Project folder not found: $ProjectRoot"
}
if ((Split-Path -Path $ProjectRoot -Leaf) -notmatch '(?i)test') {
    throw "Safety stop: ProjectRoot must point to the TEST copy, not your original game."
}
$assetRoot = Join-Path $ProjectRoot "Assets\ExternalAssets"
$manifestPath = Join-Path $assetRoot "ASSET_DOWNLOAD_MANIFEST.csv"
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw "Cannot find manifest. Pull the 'test/assets-library' branch first: $manifestPath"
}
if (-not (Test-Path -LiteralPath (Join-Path $ProjectRoot 'Assets\DeadSector') -PathType Container)) {
    throw "Safety stop: this folder does not look like the Dead Sector Unity project."
}
if (-not (Test-Path -LiteralPath $Downloads -PathType Container)) {
    throw "Downloads folder not found: $Downloads"
}
$git = Get-Command git -ErrorAction SilentlyContinue
if ($git) {
    $branch = ((& git -C $ProjectRoot branch --show-current 2>$null) | Out-String).Trim()
    if ($LASTEXITCODE -ne 0 -or $branch -ne 'test/assets-library') {
        throw "Safety stop: current Git branch is '$branch', expected test/assets-library. No files copied."
    }
} else {
    throw "Git is required to check the current branch safely."
}
if ([string]::IsNullOrWhiteSpace($StagingRoot)) {
    $StagingRoot = Join-Path (Split-Path -Parent $ProjectRoot) "DeadSector_AssetStaging"
}
$StagingRoot = [System.IO.Path]::GetFullPath($StagingRoot)

$modelExt = @('.fbx','.obj','.dae','.mtl')
$textureExt = @('.png','.jpg','.jpeg','.tga','.tif','.tiff','.exr','.bmp','.psd','.dds')
$eligible = @('TO_DOWNLOAD','ALREADY_DOWNLOADED_LOCAL')
$rows = @(Import-Csv -LiteralPath $manifestPath -Encoding UTF8)
$archiveFiles = @(Get-ChildItem -LiteralPath $Downloads -File)
[int]$matched = 0
[int]$copied = 0
[int]$skipped = 0
Write-Host "Dead Sector TEST Asset Importer" -ForegroundColor Cyan
Write-Host "Project: $ProjectRoot"
Write-Host "Mode: $(if ($Apply) { 'APPLY (copy only)' } else { 'PREVIEW (no changes)' })"
Write-Host "Manifest entries: $($rows.Count). Source: $Downloads"

foreach ($row in $rows) {
    if ($eligible -notcontains $row.status) { continue }
    $id = [string]$row.asset_id
    $relativeTarget = [string]$row.target_folder_under_Assets_ExternalAssets
    if ($id -notmatch '^[A-Za-z0-9_]+$') { Write-Warning "Invalid asset_id: $id"; continue }
    if ($relativeTarget -notmatch '^[A-Za-z0-9_/-]+$' -or $relativeTarget.Contains('..')) {
        Write-Warning "Unsafe target path for $id"; continue
    }
    $rx = '^' + [regex]::Escape($id) + '([_.-]|$)'
    $sources = @($archiveFiles | Where-Object {
        $_.Name -match $rx -and $_.Extension.ToLowerInvariant() -in @(
            '.zip','.fbx','.obj','.dae','.glb','.gltf','.blend',
            '.jpg','.jpeg','.png','.exr','.tga','.tif','.tiff','.dds','.bmp')
    })
    if ($sources.Count -eq 0) { continue }

    $matched += $sources.Count
    $assetDestination = Join-Path (Join-Path $assetRoot ($relativeTarget -replace '/','\')) $id
    Write-Host ""
    Write-Host ("[{0}] {1}  ->  {2}" -f $id,$sources.Count,$assetDestination) -ForegroundColor Green

    foreach ($source in $sources) {
        if (-not $Apply) {
            Write-Host ("  Would process: {0}" -f $source.Name)
            continue
        }
        $isZip = $source.Extension -ieq '.zip'
        if ($isZip) {
            $stageName = [System.IO.Path]::GetFileNameWithoutExtension($source.Name) -replace '[^A-Za-z0-9_.-]','_'
            $stage = Join-Path (Join-Path $StagingRoot $id) $stageName
            New-Item -ItemType Directory -Path $stage -Force | Out-Null
            try {
                Expand-Archive -LiteralPath $source.FullName -DestinationPath $stage -Force
            } catch {
                Write-Warning ("Cannot extract {0}: {1}" -f $source.Name,$_.Exception.Message)
                continue
            }
            $base = $stage
            $payload = @(Get-ChildItem -LiteralPath $stage -File -Recurse)
        } else {
            $base = $source.DirectoryName
            $payload = @($source)
        }

        $localCopied = 0
        foreach ($file in $payload) {
            $ext = $file.Extension.ToLowerInvariant()
            if ($modelExt -contains $ext) { $bucket = 'Models' }
            elseif ($textureExt -contains $ext) { $bucket = 'Textures' }
            else { $skipped++; continue }
            # Retain internal archive directories to avoid duplicate base file names.
            $rel = $file.FullName.Substring($base.Length).TrimStart(
                [System.IO.Path]::DirectorySeparatorChar,
                [System.IO.Path]::AltDirectorySeparatorChar
            )
            $destination = Join-Path (Join-Path $assetDestination $bucket) $rel
            if (Test-Path -LiteralPath $destination) {
                Write-Host ("  Skip existing: {0}" -f $destination) -ForegroundColor DarkYellow
                $skipped++; continue
            }
            New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
            Copy-Item -LiteralPath $file.FullName -Destination $destination -ErrorAction Stop
            $copied++
            $localCopied++
        }
        $unhandled = @($payload | Where-Object { $_.Extension.ToLowerInvariant() -in @('.blend','.glb','.gltf','.usd','.usdz') })
        if ($unhandled.Count -gt 0) {
            Write-Warning ("{0}: {1} BLEND/GLB/GLTF files kept in downloaded ZIP or staging; export FBX using Blender before Unity import." -f $id,$unhandled.Count)
        }
        if ($localCopied -gt 0) {
            $meta = Join-Path $assetDestination 'SOURCE_AND_LICENSE.txt'
            if (-not (Test-Path -LiteralPath $meta)) {
                @(
                    "Asset ID: $id",
                    "Source: $($row.original_source_url)",
                    "License (review at source): $($row.license)",
                    "Notes / author: $($row.notes)",
                    "Downloaded archive: $($source.Name)",
                    "Imported: $(Get-Date -Format yyyy-MM-dd)",
                    "Imported automatically: files only; materials/prefabs/LOD/rig still require Unity setup."
                ) | Set-Content -LiteralPath $meta -Encoding UTF8
            }
        }
        Write-Host ("  Copied compatible files from {0}: {1}" -f $source.Name,$localCopied)
    }
}
Write-Host ""
Write-Host "Matched downloaded files: $matched; copied: $copied; skipped: $skipped" -ForegroundColor Cyan
if (-not $Apply) {
    Write-Host 'PREVIEW only. Re-run with -Apply to copy. No files were changed.'
} else {
    Write-Host 'Done. Review Unity Console, materials and scale. Git commit only after testing.'
}
