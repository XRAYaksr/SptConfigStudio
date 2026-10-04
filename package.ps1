<#
    Builds the two Windows x64 distribution archives under release/.

    framework-dependent : small, needs ".NET Desktop Runtime 10.0.x" installed.
    self-contained      : bundles the runtime, nothing to install.

    Each archive holds exactly one wrapping folder named after the archive, with no
    files at the archive root, so unzipping always lands in a clean directory.
#>
param(
    [string]$Version = '1.1.0',
    [string]$Rid = 'win-x64'
)

$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
# .NET APIs resolve relative paths against [Environment]::CurrentDirectory, which
# Set-Location does not move, so every path handed to a .NET type is made absolute here.
$root = (Resolve-Path -LiteralPath $PSScriptRoot).Path

$fd = "SptConfigStudio-$Version-$Rid-framework-dependent"
$sc = "SptConfigStudio-$Version-$Rid-self-contained"

# Staging only. release/ holds shipped archives and notes and is never wiped here.
Remove-Item -Recurse -Force pkg-fd, pkg-sc -ErrorAction SilentlyContinue

foreach ($job in @(@{ Name = $fd; Dir = 'pkg-fd'; SelfContained = 'false' },
                  @{ Name = $sc; Dir = 'pkg-sc'; SelfContained = 'true' })) {
    $out = Join-Path $root (Join-Path $job.Dir $job.Name)
    Write-Output ("publish " + $job.Name)
    dotnet publish (Join-Path $root 'SptConfigStudio.csproj') -c Release -r $Rid `
        --self-contained $job.SelfContained `
        -p:DebugType=none -p:DebugSymbols=false `
        -o $out
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed for $($job.Name)" }

    Get-ChildItem -LiteralPath $out -Recurse -Filter *.pdb | Remove-Item -Force
    Copy-Item (Join-Path $root 'README.md'), (Join-Path $root 'LICENSE') -Destination $out
}

New-Item -ItemType Directory -Force (Join-Path $root 'release') | Out-Null

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

foreach ($job in @(@{ Name = $fd; Dir = 'pkg-fd' }, @{ Name = $sc; Dir = 'pkg-sc' })) {
    $zip = Join-Path $root (Join-Path 'release' ($job.Name + '.zip'))
    if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
    # Zipping the staging dir (not its contents) is what yields the wrapping folder,
    # and System.IO.Compression always writes '/' entry separators.
    [System.IO.Compression.ZipFile]::CreateFromDirectory(
        (Join-Path $root $job.Dir), $zip,
        [System.IO.Compression.CompressionLevel]::Optimal, $false)
}

foreach ($zip in Get-ChildItem -LiteralPath (Join-Path $root 'release') ("SptConfigStudio-$Version-*.zip")) {
    $archive = [System.IO.Compression.ZipFile]::OpenRead($zip.FullName)
    $count = $archive.Entries.Count
    $rootFiles = @($archive.Entries | Where-Object { $_.FullName -notmatch '/' }).Count
    $separators = @($archive.Entries | Where-Object { $_.FullName -match '\\' }).Count
    $archive.Dispose()
    # Windows PowerShell 5.1 carries the .NET Framework zip writer, which emits '\' entry
    # names; those unpack as a single flat filename off-platform. Run this script with pwsh.
    if ($separators -gt 0) { throw "$($zip.Name): $separators entries use '\' separators - run with pwsh, not powershell.exe" }
    if ($rootFiles -gt 0) { throw "$($zip.Name): $rootFiles files at archive root, expected the wrapping folder only" }
    $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $zip.FullName).Hash.ToLower()
    Write-Output ('')
    Write-Output ($zip.Name)
    Write-Output ('  entries=' + $count)
    Write-Output ('  size=' + $zip.Length + ' B (' + [math]::Round($zip.Length / 1MB, 1) + ' MiB)')
    Write-Output ('  sha256=' + $hash)
}
