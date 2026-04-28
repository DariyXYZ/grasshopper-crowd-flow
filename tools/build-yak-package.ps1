#Requires -Version 5.1
param(
    [string]$Configuration = "Release",
    [string]$YakExe = "C:\Program Files\Rhino 8\System\yak.exe"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot     = Split-Path $PSScriptRoot -Parent
$ArtifactBase = Join-Path $RepoRoot "artifacts\bin\GrasshopperComponents\$Configuration"
$DistRoot     = Join-Path $RepoRoot "dist\yak\crowdflow"
$Frameworks   = @("net48", "net8.0")
$ExcludeExt   = @(".pdb", ".xml")

foreach ($tfm in $Frameworks) {
    Write-Host "Building $Configuration $tfm ..." -ForegroundColor Cyan
    & dotnet build "$RepoRoot\src\GrasshopperComponents\GrasshopperComponents.csproj" `
        -c $Configuration -f $tfm --nologo -v quiet -p:DeployToGrasshopper=false
    if ($LASTEXITCODE -ne 0) { throw "Build failed for $tfm (exit $LASTEXITCODE)" }
}

foreach ($tfm in $Frameworks) {
    $dir = Join-Path $DistRoot $tfm
    if (Test-Path $dir) { Remove-Item "$dir\*" -Recurse -Force }
    else { New-Item -ItemType Directory $dir | Out-Null }
}

foreach ($tfm in $Frameworks) {
    $src = Join-Path $ArtifactBase $tfm
    $dst = Join-Path $DistRoot $tfm
    if (-not (Test-Path $src)) { throw "Artifact dir not found: $src" }
    Get-ChildItem $src -File | Where-Object { $ExcludeExt -notcontains $_.Extension -and $_.Name -notlike "*.runtimeconfig.json" } | ForEach-Object {
        Copy-Item $_.FullName -Destination $dst -Force
    }
    $count = (Get-ChildItem $dst -File | Measure-Object).Count
    Write-Host "  $tfm payload: $count files" -ForegroundColor Gray
}

$IconSrc = Join-Path $RepoRoot "icon.png"
if (Test-Path $IconSrc) {
    Copy-Item $IconSrc -Destination $DistRoot -Force
    Write-Host "  icon.png copied" -ForegroundColor Gray
} else {
    Write-Warning "icon.png not found at repo root"
}

$ReadmeSrc = Join-Path $RepoRoot "README.md"
if (Test-Path $ReadmeSrc) {
    $MiscDir = Join-Path $DistRoot "misc"
    if (-not (Test-Path $MiscDir)) { New-Item -ItemType Directory $MiscDir | Out-Null }
    Copy-Item $ReadmeSrc -Destination $MiscDir -Force
}

$LicenseSrc = Join-Path $RepoRoot "LICENSE.txt"
if (Test-Path $LicenseSrc) {
    $MiscDir = Join-Path $DistRoot "misc"
    if (-not (Test-Path $MiscDir)) { New-Item -ItemType Directory $MiscDir | Out-Null }
    Copy-Item $LicenseSrc -Destination $MiscDir -Force
}

$ManifestPath = Join-Path $DistRoot "manifest.yml"
if (-not (Test-Path $ManifestPath)) {
    throw "manifest.yml not found at $ManifestPath - create it before running this script"
}

if (-not (Test-Path $YakExe)) {
    throw "yak.exe not found at $YakExe"
}

Write-Host "`nRunning yak build ..." -ForegroundColor Cyan
Push-Location $DistRoot
try {
    & $YakExe build
    if ($LASTEXITCODE -ne 0) { throw "yak build failed (exit $LASTEXITCODE)" }
} finally {
    Pop-Location
}

$YakFile = Get-ChildItem $DistRoot -Filter "*.yak" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($YakFile) {
    $sizeKB = [math]::Round($YakFile.Length / 1KB, 1)
    Write-Host "`nPackage built: $($YakFile.FullName)" -ForegroundColor Green
    Write-Host "Size: $sizeKB KB"
    Write-Host "`nTest server push (requires yak login):"
    Write-Host "  yak push --source https://test.yak.rhino3d.com `"$($YakFile.FullName)`""
    Write-Host "`nPublic push (requires yak login + explicit confirmation):"
    Write-Host "  yak push `"$($YakFile.FullName)`""
} else {
    Write-Warning "yak build ran but no .yak file found in $DistRoot"
}
