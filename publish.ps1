<#
.SYNOPSIS
    Builds MouseYoke as a self-contained portable app and drops it in dist\.

.DESCRIPTION
    Produces a standalone MouseYoke.exe with the .NET runtime bundled inside - no .NET
    install needed, no MSFS SDK, nothing else. (MSFS_SDK is only a build-time requirement
    here, for locating the SimConnect files to bundle.)

    Not a literal single file: Microsoft.FlightSimulator.SimConnect.dll is a mixed-mode
    (C++/CLI) assembly, which .NET's single-file publish cannot embed at all (it crashes
    on startup if forced). It and the native SimConnect.dll it wraps ship as two small
    loose files next to MouseYoke.exe instead - still just 3 files total, keep them
    together.
#>

$ErrorActionPreference = "Stop"
$repoRoot = $PSScriptRoot
$project = Join-Path $repoRoot "MouseYoke\MouseYoke.csproj"
$distDir = Join-Path $repoRoot "dist"

if (-not $env:MSFS_SDK) {
    throw "MSFS_SDK environment variable is not set. Install the MSFS 2024 SDK (see README.md) and set MSFS_SDK to its install path before publishing."
}

$dotnet = "dotnet"
if (Test-Path "C:\Program Files\dotnet\dotnet.exe") {
    $dotnet = "C:\Program Files\dotnet\dotnet.exe"
}

Write-Host "Publishing self-contained single-file exe..." -ForegroundColor Cyan

# MSFS_SDK is read from the process environment by MSBuild automatically ($(MSFS_SDK) in the
# csproj) - deliberately NOT passed as an explicit -p: here, since a trailing backslash in a
# quoted command-line argument gets mis-parsed by Windows' argv escaping rules.
& $dotnet publish $project `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$publishDir = Join-Path $repoRoot "MouseYoke\bin\Release\net8.0-windows\win-x64\publish"
$sourceExe = Join-Path $publishDir "MouseYoke.exe"

if (-not (Test-Path $sourceExe)) {
    throw "Expected published exe not found at $sourceExe - publish output layout may have changed."
}

if (Test-Path $distDir) {
    Remove-Item $distDir -Recurse -Force
}
New-Item -ItemType Directory -Path $distDir | Out-Null

Copy-Item $sourceExe (Join-Path $distDir "MouseYoke.exe")
Copy-Item (Join-Path $repoRoot "README.md") $distDir
Copy-Item (Join-Path $repoRoot "NOTICE.md") $distDir

foreach ($dll in @("Microsoft.FlightSimulator.SimConnect.dll", "SimConnect.dll")) {
    $src = Join-Path $publishDir $dll
    if (-not (Test-Path $src)) {
        throw "Expected $dll next to the published exe but it wasn't there - mixed-mode exclusion may not be working."
    }
    Copy-Item $src $distDir
}

$finalExe = Join-Path $distDir "MouseYoke.exe"
$sizeMb = [Math]::Round((Get-Item $finalExe).Length / 1MB, 1)

Write-Host ""
Write-Host "Done: $finalExe ($sizeMb MB)" -ForegroundColor Green
Write-Host "This folder (dist\) is what you zip up and hand to a user:"
Get-ChildItem $distDir | ForEach-Object { Write-Host "  $($_.Name)" }
Write-Host "MouseYoke.exe, Microsoft.FlightSimulator.SimConnect.dll, and SimConnect.dll must stay together in the same folder."
