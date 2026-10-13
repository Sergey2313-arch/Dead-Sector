#requires -Version 5.1
# Dead Sector: READ ONLY audit of imported Unity assets and Blender staging.
[CmdletBinding()]
param(
    [string]$ProjectRoot = "D:\DEAD SECTOR\Dead Sector_Test",
    [string]$StagingRoot = ""
)
$ErrorActionPreference = "Stop"
$ProjectRoot = [System.IO.Path]::GetFullPath($ProjectRoot).TrimEnd("\")
if ((Split-Path -Path $ProjectRoot -Leaf) -notmatch "(?i)test") {
    throw "Safety stop: project folder must be the TEST copy."
}
$assetRoot = Join-Path $ProjectRoot "Assets\ExternalAssets"
$manifest = Join-Path $assetRoot "ASSET_DOWNLOAD_MANIFEST.csv"
if (-not (Test-Path -LiteralPath $manifest -PathType Leaf)) {
    throw "Asset manifest was not found. Import library files first."
}
if ([string]::IsNullOrWhiteSpace($StagingRoot)) {
    $StagingRoot = Join-Path (Split-Path -Parent $ProjectRoot) "DeadSector_AssetStaging"
}
$modelExtensions = @(".fbx", ".obj", ".dae")
$textureExtensions = @(".png", ".jpg", ".jpeg", ".tga", ".tif", ".tiff", ".exr", ".bmp", ".dds")
$allFiles = @(Get-ChildItem -LiteralPath $assetRoot -Recurse -File -ErrorAction Stop)
$models = @($allFiles | Where-Object { $modelExtensions -contains $_.Extension.ToLowerInvariant() })
$textures = @($allFiles | Where-Object { $textureExtensions -contains $_.Extension.ToLowerInvariant() })
$assetFolders = @($allFiles | Where-Object { $_.Name -eq "SOURCE_AND_LICENSE.txt" })
$totalMB = [math]::Round((($allFiles | Measure-Object -Property Length -Sum).Sum / 1MB), 1)
Write-Host "==== Dead Sector asset audit (read-only) ====" -ForegroundColor Cyan
Write-Host "Project: $ProjectRoot"
Write-Host "Asset files: $($allFiles.Count), disk used: $totalMB MB"
Write-Host "Importable model files (FBX/OBJ/DAE): $($models.Count)"
Write-Host "Image textures: $($textures.Count)"
Write-Host "Asset folders created by organizer: $($assetFolders.Count)"
Write-Host ""
Write-Host "==== Model formats ===="
if ($models.Count -gt 0) {
    $models | Group-Object Extension | Sort-Object Name | ForEach-Object {
        Write-Host ("{0,-8} {1,5}" -f $_.Name, $_.Count)
    }
} else {
    Write-Host "No FBX/OBJ/DAE files found in ExternalAssets."
}
Write-Host ""
Write-Host "==== By imported asset ID ===="
$rows = @(Import-Csv -LiteralPath $manifest -Encoding UTF8)
$ready = 0
$needsConversion = 0
foreach ($row in $rows) {
    if ($row.status -notin @("TO_DOWNLOAD", "ALREADY_DOWNLOADED_LOCAL")) { continue }
    $id = [string]$row.asset_id
    $relative = [string]$row.target_folder_under_Assets_ExternalAssets
    if ($id -notmatch "^[A-Za-z0-9_]+$" -or $relative -notmatch "^[A-Za-z0-9_/-]+$") { continue }
    $folder = Join-Path (Join-Path $assetRoot ($relative.Replace("/", "\"))) $id
    if (-not (Test-Path -LiteralPath $folder -PathType Container)) { continue }
    $inFolder = @(Get-ChildItem -LiteralPath $folder -Recurse -File)
    $geo = @($inFolder | Where-Object { $modelExtensions -contains $_.Extension.ToLowerInvariant() })
    $tex = @($inFolder | Where-Object { $textureExtensions -contains $_.Extension.ToLowerInvariant() })
    if ($geo.Count -gt 0) {
        $ready++
        Write-Host ("READY   {0,-38} models={1,2} textures={2,2}" -f $id,$geo.Count,$tex.Count) -ForegroundColor Green
    } elseif ($tex.Count -gt 0) {
        $needsConversion++
        Write-Host ("TEXTURE {0,-38} images={1,2}; source geometry needed" -f $id,$tex.Count) -ForegroundColor Yellow
    }
}
Write-Host ""
Write-Host "Assets with model geometry: $ready; textures-only candidates: $needsConversion"
if (Test-Path -LiteralPath $StagingRoot -PathType Container) {
    $stageGeom = @(Get-ChildItem -LiteralPath $StagingRoot -Recurse -File | Where-Object {
        $_.Extension.ToLowerInvariant() -in @(".blend", ".glb", ".gltf", ".usd", ".usdz")
    })
    Write-Host "Staged Blender/GLTF/USD source files (outside Unity): $($stageGeom.Count)"
    if ($stageGeom.Count -gt 0) {
        Write-Host "First 15 conversion candidates:"
        $stageGeom | Select-Object -First 15 | ForEach-Object { Write-Host ("  " + $_.FullName) }
    }
} else {
    Write-Host "Blender staging folder not found: $StagingRoot"
}
Write-Host ""
Write-Host "==== 10 largest files ===="
$allFiles | Sort-Object Length -Descending | Select-Object -First 10 | ForEach-Object {
    Write-Host ("{0,8:N1} MB  {1}" -f ($_.Length / 1MB), $_.FullName.Substring($assetRoot.Length + 1))
}
Write-Host ""
Write-Host "No project or downloaded files were changed."
