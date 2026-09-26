[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$townRoot = Join-Path $repositoryRoot 'games\SinStarI\SourceAssets\Towns\Neris\NerisTownV1'
$catalog = Get-Content -LiteralPath (Join-Path $townRoot 'Authoring\catalog.json') -Raw | ConvertFrom-Json
$destination = Join-Path $PSScriptRoot 'BuildAssets\Neris\Catalog'
$null = New-Item -ItemType Directory -Force -Path $destination
Copy-Item -LiteralPath (Join-Path $townRoot 'Authoring\Catalog.sm3d.json') -Destination $destination -Force
foreach ($chunk in $catalog.chunks) {
    Copy-Item -LiteralPath (Join-Path $townRoot ('Authoring\' + $chunk.file)) -Destination $destination -Force
}
$textureRoot = Join-Path $PSScriptRoot 'Assets\Neris'
$null = New-Item -ItemType Directory -Force -Path $textureRoot
Copy-Item -LiteralPath (Join-Path $townRoot 'Textures\Neris-Grass-Color.png') -Destination $textureRoot -Force

# A deterministic two-metre paving tile. Geometry remains at its authored scale;
# repeating texture coordinates supply fine seams without thousands of draw calls.
Add-Type -AssemblyName System.Drawing
$bitmap = [Drawing.Bitmap]::new(128, 128)
try {
    for ($y = 0; $y -lt 128; $y++) {
        for ($x = 0; $x -lt 128; $x++) {
            $grain = (($x * 73 + $y * 151 + $x * $y * 7) % 5) - 2
            $seam = if ($x -eq 0 -or $y -eq 0) { 14 } else { 0 }
            $bitmap.SetPixel($x, $y, [Drawing.Color]::FromArgb(144 + $grain - $seam,
                162 + $grain - $seam, 171 + $grain - $seam))
        }
    }
    $bitmap.Save((Join-Path $textureRoot 'Neris-Paving.png'), [Drawing.Imaging.ImageFormat]::Png)
} finally {
    $bitmap.Dispose()
}
Write-Host "Prepared $($catalog.chunks.Count) editable town model inputs."
