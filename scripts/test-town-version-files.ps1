[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
. (Join-Path $root 'tools/Character3DViewer/TownFileWorker.ps1')
function HashText([string]$Text) {
    [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($Text))).ToLowerInvariant()
}
$folder = Join-Path $root ('artifacts/tests/town-versions-' + [Guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $folder
$path = Join-Path $folder '2026-09-28 0015 - Test Town.town'
for ($index = 1; $index -le 12; $index++) {
    Preserve-TownVersion $folder $path
    Write-TownPayload $path ([byte[]](84,87,78,1,$index))
    Register-TownVersion $folder 'Test Town' $path
}
$entries = @(Read-TownVersionIndex $folder)
if ($entries.Count -ne 10 -or @(Get-ChildItem $folder -Filter '*.town').Count -ne 10) { throw 'Retention must keep ten versions.' }
$latest = $entries | Sort-Object SavedUtc -Descending | Select-Object -First 1
if ($latest.Path -ne $path -or (Read-TownPayload $path)[4] -ne 12) { throw 'Newest version differs.' }
Publish-TownVersions $folder 'Test Town' 7
$recent = Read-TownPayload (Join-Path $folder ((HashText 'TownEditor.File.Recent') + '.bin'))
if ([BitConverter]::ToUInt32($recent, 4) -ne 7 -or [BitConverter]::ToUInt32($recent, 8) -ne 10) { throw 'Recent list header differs.' }
$count = [BitConverter]::ToUInt32($recent, 12)
if ([Text.Encoding]::UTF32.GetString($recent, 16, $count*4) -ne $path) { throw 'Recent versions must be newest first.' }
$old = $entries | Sort-Object SavedUtc | Select-Object -First 1
[IO.File]::WriteAllText($old.Path, 'Externally replaced; preserve me.')
Preserve-TownVersion $folder $path
Write-TownPayload $path ([byte[]](84,87,78,1,13))
Register-TownVersion $folder 'Test Town' $path
if ([IO.File]::ReadAllText($old.Path) -ne 'Externally replaced; preserve me.') { throw 'Retention deleted an externally replaced file.' }
$corrupt = [IO.File]::ReadAllBytes($path); $corrupt[44] = 0
[IO.File]::WriteAllBytes($path, $corrupt)
$rejected = $false
try { $null = Read-TownPayload $path } catch { $rejected = $true }
if (-not $rejected) { throw 'Corrupt envelope accepted.' }
Write-Host 'PASS ten-version retention, same-minute saves, newest-first list, external-file protection and checksum rejection'
