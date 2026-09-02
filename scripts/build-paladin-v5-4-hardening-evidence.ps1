[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$evidenceRoot = Join-Path $repositoryRoot `
    'docs\implementation\screenshots\m7c-b1-paladin-v5-4-hardening'
$contactSheet = Join-Path $evidenceRoot '12-iphone-contact-sheet.png'
$indexPath = Join-Path $evidenceRoot 'screenshot-index.md'
$names = @(
    '01-native-idle-front.png',
    '02-native-sword-attack.png',
    '03-native-shield-bash-candidate.png',
    '04-native-ko-grounding.png',
    '05-native-socket-gizmos.png',
    '06-native-material-channels.png',
    '07-web-idle-front.png',
    '08-web-sword-attack.png',
    '09-web-360-orbit.png',
    '10-responsive-layouts.png',
    '11-grid-gizmo-resource-counts.png'
)

Add-Type -AssemblyName System.Drawing.Common
foreach ($name in $names) {
    $path = Join-Path $evidenceRoot $name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Missing evidence image: $path"
    }
}

$sheet = [Drawing.Bitmap]::new(1280, 2400, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [Drawing.Graphics]::FromImage($sheet)
$graphics.Clear([Drawing.Color]::FromArgb(2, 6, 17))
$graphics.CompositingQuality = [Drawing.Drawing2D.CompositingQuality]::HighQuality
$graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::HighQuality
$font = [Drawing.Font]::new('Segoe UI', 17, [Drawing.FontStyle]::Bold)
$brush = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(0, 238, 255))
$border = [Drawing.Pen]::new([Drawing.Color]::FromArgb(25, 118, 164), 2)

try {
    for ($index = 0; $index -lt $names.Count; $index++) {
        $column = $index % 2
        $row = [Math]::Floor($index / 2)
        $cellX = $column * 640
        $cellY = $row * 400
        $graphics.DrawString($names[$index], $font, $brush, $cellX + 18, $cellY + 12)
        $image = [Drawing.Image]::FromFile((Join-Path $evidenceRoot $names[$index]))
        try {
            $maximumWidth = 604
            $maximumHeight = 330
            $scale = [Math]::Min($maximumWidth / $image.Width, $maximumHeight / $image.Height)
            $width = [Math]::Max(1, [Math]::Round($image.Width * $scale))
            $height = [Math]::Max(1, [Math]::Round($image.Height * $scale))
            $x = $cellX + [Math]::Floor((640 - $width) / 2)
            $y = $cellY + 56 + [Math]::Floor(($maximumHeight - $height) / 2)
            $graphics.DrawImage($image, $x, $y, $width, $height)
            $graphics.DrawRectangle($border, $x, $y, $width, $height)
        }
        finally {
            $image.Dispose()
        }
    }
    $sheet.Save($contactSheet, [Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $border.Dispose()
    $brush.Dispose()
    $font.Dispose()
    $graphics.Dispose()
    $sheet.Dispose()
}

$rows = [Collections.Generic.List[string]]::new()
foreach ($name in @($names) + @('12-iphone-contact-sheet.png')) {
    $path = Join-Path $evidenceRoot $name
    $image = [Drawing.Image]::FromFile($path)
    try {
        $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
        $file = Get-Item -LiteralPath $path
        $rows.Add("| ``$name`` | $($image.Width)x$($image.Height) | $($file.Length) | ``$hash`` |")
    }
    finally {
        $image.Dispose()
    }
}

$markdown = @"
# Paladin v5.4 Viewer/Exporter Hardening Evidence

- Stable asset identity: ``sin-star-i.character-1.paladin``
- Candidate version: ``v5.4``
- Source GLB SHA-256: ``D080754339ABD4F3F4CFBCAF4F26146631BDEEE30DD2EAA284682EF896B16CA3``
- Published SM3D SHA-256: ``508063F78C08B97DBD44ED19DC3A0D8C1DAAEF1A093D8F19E5A6929456993023``
- Model budget: 4 parts, 10,296 triangles, 4 materials, 9 textures, 42 bones, 46 nodes, 11 clips, 6 sockets.
- Normal scene budget: 6 draw calls / 10,378 submitted triangles.
- Socket selection budget: four axis objects; all-socket origins share one optional particle batch.
- Native captures: current DirectX renderer and cooked candidate at 1280x720 unless composed otherwise.
- Web captures: current WebGL2 renderer at 1280x720 with responsive-window and cooked-texture orientation parity enabled.
- ``09-web-360-orbit.png`` is the returned-front checkpoint after 72 deterministic 5-degree vertical orbit inputs (360 degrees total).
- ``10-responsive-layouts.png`` compares 800x540 minimum and 1440x700 wide layouts.
- ``11-grid-gizmo-resource-counts.png`` records the single-grid-draw and bounded-gizmo diagnostics.
- Shield Bash remains candidate evidence, not production approval.

| File | Dimensions | Bytes | SHA-256 |
| --- | ---: | ---: | --- |
$($rows -join "`n")
"@
[IO.File]::WriteAllText($indexPath, $markdown, [Text.UTF8Encoding]::new($false))
Write-Host "Built evidence contact sheet and index in $evidenceRoot"
