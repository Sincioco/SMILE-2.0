[CmdletBinding()]
param([switch]$Check)

$ErrorActionPreference = 'Stop'
$profile = Get-Content (Join-Path $PSScriptRoot 'TownSurfaceLayers.json') -Raw | ConvertFrom-Json -AsHashtable
$culture = [Globalization.CultureInfo]::InvariantCulture
$lines = @("''' Generated from TownSurfaceLayers.json; change the shared profile, then prepare assets.",
    'Module Smile.Tools.TownSurfaceLayers', '', 'Option Explicit', '')
foreach ($entry in $profile.GetEnumerator()) {
    $number = ([double]$entry.Value).ToString('0.0############', $culture)
    $lines += 'Public Const ' + $entry.Key + ' = ' + $number
}
$lines += @('', 'End Module', '')
$source = $lines -join "`n"
$path = Join-Path $PSScriptRoot 'TownSurfaceLayers.smile'
$previous = if (Test-Path $path) { [IO.File]::ReadAllText($path).Replace("`r`n", "`n") } else { '' }
if ($previous -ne $source) {
    if ($Check) { throw 'Town surface layers are stale. Run Prepare-SurfaceLayers.ps1.' }
    [IO.File]::WriteAllText($path, $source)
}
