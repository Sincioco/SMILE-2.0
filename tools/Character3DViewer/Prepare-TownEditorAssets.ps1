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
$metropolisRoot = Join-Path (Split-Path $townRoot -Parent) 'NerisMetropolisV1'
$extension = Get-Content -LiteralPath (Join-Path $metropolisRoot 'Authoring\catalog-extension.json') -Raw | ConvertFrom-Json
foreach ($chunk in $extension.chunks) {
    Copy-Item -LiteralPath (Join-Path $metropolisRoot ('Authoring\' + $chunk.file)) -Destination $destination -Force
}
# Wind derivatives preserve the exact catalog part slots and static bind geometry.
foreach ($wind in Get-ChildItem -LiteralPath (Join-Path $townRoot 'Authoring\Wind') -Filter 'Catalog-*.glb') {
    Copy-Item -LiteralPath $wind.FullName -Destination $destination -Force
}
$textureRoot = Join-Path $PSScriptRoot 'Assets\Neris'
$null = New-Item -ItemType Directory -Force -Path $textureRoot
Copy-Item -LiteralPath (Join-Path $townRoot 'Textures\Neris-Grass-Color.png') -Destination $textureRoot -Force
Copy-Item -LiteralPath (Join-Path $metropolisRoot 'Authoring\Town-Palette.png') -Destination $textureRoot -Force
$cityTextures = Join-Path $textureRoot 'Metropolis'
$null = New-Item -ItemType Directory -Force -Path $cityTextures
Get-ChildItem -LiteralPath (Join-Path $metropolisRoot 'Textures') -Filter '*-Glass.png' |
    Copy-Item -Destination $cityTextures -Force
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'games\SinStarI\SourceAssets\Towns\Neris\StoryTownsV1\Textures\Greyglass-Strata.png') -Destination $textureRoot -Force
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'games\SinStarI\SourceAssets\Towns\Neris\StoryTownsV1\Textures\Green-Slope-Tile.png') -Destination $textureRoot -Force
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'games\SinStarI\SourceAssets\Towns\Neris\StoryTownsV1\Textures\Snow-Slope-Tile.png') -Destination $textureRoot -Force

# A deterministic two-metre paving tile. Geometry remains at its authored scale;
# repeating texture coordinates supply fine seams without thousands of draw calls.
Add-Type -AssemblyName System.Drawing
$bitmap = [Drawing.Bitmap]::new(128, 128)
try {
    for ($y = 0; $y -lt 128; $y++) {
        for ($x = 0; $x -lt 128; $x++) {
            # A flat stone face avoids periodic grain turning into wavy moire bands.
            $seam = if ($x -eq 0 -or $y -eq 0) { 8 } else { 0 }
            $bitmap.SetPixel($x, $y, [Drawing.Color]::FromArgb(144 - $seam,
                162 - $seam, 171 - $seam))
        }
    }
    $bitmap.Save((Join-Path $textureRoot 'Neris-Paving.png'), [Drawing.Imaging.ImageFormat]::Png)
} finally {
    $bitmap.Dispose()
}
Write-Host "Prepared $($catalog.chunks.Count) editable town model inputs."
