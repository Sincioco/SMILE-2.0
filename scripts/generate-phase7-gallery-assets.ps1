param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$galleryRoot = Join-Path $RepositoryRoot 'games\RPGSystems'
$assetRoot = Join-Path $galleryRoot 'Assets\World'
$mapRoot = Join-Path $galleryRoot 'Maps\World'
[IO.Directory]::CreateDirectory($assetRoot) | Out-Null
[IO.Directory]::CreateDirectory($mapRoot) | Out-Null

function New-Canvas([int]$width, [int]$height) {
    $bitmap = [Drawing.Bitmap]::new($width, $height, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.Clear([Drawing.Color]::Transparent)
    return @($bitmap, $graphics)
}

function Save-Canvas($canvas, [string]$path) {
    $canvas[1].Dispose()
    $canvas[0].Save($path, [Drawing.Imaging.ImageFormat]::Png)
    $canvas[0].Dispose()
}

function New-Brush([string]$hex) {
    return [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml($hex))
}

function Draw-Tile($graphics, [int]$column, [int]$row, [string]$base, [string]$accent, [string]$kind) {
    $x = $column * 128
    $y = $row * 128
    $baseBrush = New-Brush $base
    $accentBrush = New-Brush $accent
    if ($kind -notin @('tree', 'roof', 'mountain')) { $graphics.FillRectangle($baseBrush, $x, $y, 128, 128) }
    if ($kind -eq 'grass') {
        for ($i = 0; $i -lt 26; $i++) {
            $px = $x + (($i * 47 + 13) % 120)
            $py = $y + (($i * 71 + 19) % 120)
            $graphics.FillEllipse($accentBrush, $px, $py, 5, 9)
        }
    } elseif ($kind -eq 'path') {
        for ($i = 0; $i -lt 8; $i++) {
            $graphics.FillRectangle($accentBrush, $x + 5 + ($i % 3) * 42, $y + 7 + [math]::Floor($i / 3) * 40, 34, 25)
        }
    } elseif ($kind -eq 'water') {
        for ($i = 0; $i -lt 5; $i++) {
            $graphics.DrawArc([Drawing.Pen]::new($accentBrush.Color, 4), $x + 5 + $i * 20, $y + 18 + ($i % 2) * 34, 42, 22, 15, 150)
        }
    } elseif ($kind -eq 'wall') {
        $pen = [Drawing.Pen]::new($accentBrush.Color, 5)
        for ($i = 0; $i -le 128; $i += 32) { $graphics.DrawLine($pen, $x, $y + $i, $x + 128, $y + $i) }
        for ($i = 0; $i -le 128; $i += 32) { $graphics.DrawLine($pen, $x + $i, $y, $x + $i, $y + 128) }
        $pen.Dispose()
    } elseif ($kind -eq 'tree') {
        $trunk = New-Brush '#70452a'
        $graphics.FillRectangle($trunk, $x + 54, $y + 70, 20, 50)
        $graphics.FillEllipse($baseBrush, $x + 14, $y + 10, 100, 92)
        $graphics.FillEllipse($accentBrush, $x + 29, $y + 22, 58, 47)
        $trunk.Dispose()
    } elseif ($kind -eq 'roof') {
        $points = [Drawing.Point[]]@([Drawing.Point]::new($x + 4, $y + 120), [Drawing.Point]::new($x + 64, $y + 8), [Drawing.Point]::new($x + 124, $y + 120))
        $graphics.FillPolygon($baseBrush, $points)
        for ($i = 0; $i -lt 4; $i++) { $graphics.DrawLine([Drawing.Pen]::new($accentBrush.Color, 3), $x + 23 + $i * 20, $y + 95, $x + 64, $y + 18) }
    } elseif ($kind -eq 'floor') {
        $pen = [Drawing.Pen]::new($accentBrush.Color, 2)
        for ($i = 0; $i -le 128; $i += 32) { $graphics.DrawLine($pen, $x + $i, $y, $x + $i, $y + 128); $graphics.DrawLine($pen, $x, $y + $i, $x + 128, $y + $i) }
        $pen.Dispose()
    } elseif ($kind -eq 'wild') {
        for ($i = 0; $i -lt 16; $i++) {
            $graphics.FillPolygon($accentBrush, [Drawing.Point[]]@([Drawing.Point]::new($x + (($i * 31) % 118), $y + (($i * 43) % 118) + 8), [Drawing.Point]::new($x + (($i * 31) % 118) + 5, $y + (($i * 43) % 118)), [Drawing.Point]::new($x + (($i * 31) % 118) + 10, $y + (($i * 43) % 118) + 8)))
        }
    } elseif ($kind -eq 'mountain') {
        $points = [Drawing.Point[]]@([Drawing.Point]::new($x + 5, $y + 118), [Drawing.Point]::new($x + 61, $y + 10), [Drawing.Point]::new($x + 123, $y + 118))
        $graphics.FillPolygon($baseBrush, $points)
        $graphics.FillPolygon($accentBrush, [Drawing.Point[]]@([Drawing.Point]::new($x + 38, $y + 55), [Drawing.Point]::new($x + 61, $y + 10), [Drawing.Point]::new($x + 86, $y + 57), [Drawing.Point]::new($x + 70, $y + 49), [Drawing.Point]::new($x + 58, $y + 63), [Drawing.Point]::new($x + 49, $y + 49)))
    }
    $baseBrush.Dispose()
    $accentBrush.Dispose()
}

$tiles = New-Canvas 640 256
Draw-Tile $tiles[1] 0 0 '#4d9d78' '#76c79b' 'grass'
Draw-Tile $tiles[1] 1 0 '#c69b63' '#e3be80' 'path'
Draw-Tile $tiles[1] 2 0 '#347fa0' '#8dd6dc' 'water'
Draw-Tile $tiles[1] 3 0 '#6a6678' '#aba3b5' 'wall'
Draw-Tile $tiles[1] 4 0 '#326b4d' '#6abf6d' 'tree'
Draw-Tile $tiles[1] 0 1 '#733e57' '#d29a68' 'roof'
Draw-Tile $tiles[1] 1 1 '#725b49' '#a98a66' 'floor'
Draw-Tile $tiles[1] 2 1 '#729b58' '#add072' 'wild'
Draw-Tile $tiles[1] 3 1 '#695a7e' '#d2d0db' 'mountain'
Draw-Tile $tiles[1] 4 1 '#204357' '#58b8b5' 'water'
Save-Canvas $tiles (Join-Path $assetRoot 'WorldTiles.png')

function New-ActorSheet([string]$path, [string]$coat, [string]$hair) {
    $canvas = New-Canvas 288 512
    $graphics = $canvas[1]
    $coatBrush = New-Brush $coat
    $hairBrush = New-Brush $hair
    $skinBrush = New-Brush '#f2c7a5'
    $bootBrush = New-Brush '#293546'
    for ($direction = 0; $direction -lt 4; $direction++) {
        for ($frame = 0; $frame -lt 3; $frame++) {
            $x = $frame * 96
            $y = $direction * 128
            $bob = if ($frame -eq 1) { -4 } else { 0 }
            $graphics.FillEllipse($hairBrush, $x + 23, $y + 9 + $bob, 50, 48)
            $graphics.FillEllipse($skinBrush, $x + 29, $y + 22 + $bob, 38, 39)
            $graphics.FillRectangle($coatBrush, $x + 18, $y + 54 + $bob, 60, 52)
            $leftStep = if ($frame -eq 0) { -6 } elseif ($frame -eq 2) { 6 } else { 0 }
            $graphics.FillRectangle($bootBrush, $x + 24 + $leftStep, $y + 101 + $bob, 20, 22)
            $graphics.FillRectangle($bootBrush, $x + 53 - $leftStep, $y + 101 + $bob, 20, 22)
            if ($direction -eq 0) { $graphics.FillRectangle($hairBrush, $x + 32, $y + 36 + $bob, 33, 22) }
            if ($direction -eq 1) { $graphics.FillEllipse($bootBrush, $x + 59, $y + 36 + $bob, 6, 6) }
            if ($direction -eq 2) { $graphics.FillEllipse($bootBrush, $x + 39, $y + 38 + $bob, 6, 6); $graphics.FillEllipse($bootBrush, $x + 55, $y + 38 + $bob, 6, 6) }
            if ($direction -eq 3) { $graphics.FillEllipse($bootBrush, $x + 33, $y + 36 + $bob, 6, 6) }
        }
    }
    $coatBrush.Dispose(); $hairBrush.Dispose(); $skinBrush.Dispose(); $bootBrush.Dispose()
    Save-Canvas $canvas $path
}

New-ActorSheet (Join-Path $assetRoot 'Hero.png') '#2b7f8c' '#263447'
New-ActorSheet (Join-Path $assetRoot 'Companion.png') '#9a4d72' '#e7c96a'
New-ActorSheet (Join-Path $assetRoot 'Npc.png') '#7562a8' '#526044'

$title = New-Canvas 960 540
$g = $title[1]
$g.FillRectangle((New-Brush '#13263b'), 0, 0, 960, 540)
$g.FillEllipse((New-Brush '#e9c46a'), 650, -120, 360, 360)
$g.FillPolygon((New-Brush '#326b6d'), [Drawing.Point[]]@([Drawing.Point]::new(0, 430), [Drawing.Point]::new(210, 170), [Drawing.Point]::new(400, 430)))
$g.FillPolygon((New-Brush '#244a5b'), [Drawing.Point[]]@([Drawing.Point]::new(250, 440), [Drawing.Point]::new(550, 110), [Drawing.Point]::new(850, 440)))
$g.FillRectangle((New-Brush '#1c3745'), 0, 430, 960, 110)
$font = [Drawing.Font]::new('Segoe UI', 58, [Drawing.FontStyle]::Bold)
$subFont = [Drawing.Font]::new('Segoe UI', 23, [Drawing.FontStyle]::Regular)
$g.DrawString('LUMEN TRAIL', $font, (New-Brush '#f3e9d2'), 58, 48)
$g.DrawString('A SMILE 2.0 top-down RPG world', $subFont, (New-Brush '#72d6c9'), 66, 124)
$font.Dispose(); $subFont.Dispose()
Save-Canvas $title (Join-Path $assetRoot 'TitleBackground.png')

$panel = New-Canvas 64 64
$panel[1].Clear([Drawing.Color]::FromArgb(190, 10, 18, 30))
Save-Canvas $panel (Join-Path $assetRoot 'PanelOverlay.png')

$encounter = New-Canvas 960 540
$g = $encounter[1]
$g.FillRectangle((New-Brush '#172941'), 0, 0, 960, 540)
for ($i = 0; $i -lt 12; $i++) { $g.FillEllipse((New-Brush '#3b6b77'), $i * 90 - 40, 250 + (($i * 37) % 80), 180, 180) }
$g.FillRectangle((New-Brush '#7c6a53'), 0, 430, 960, 110)
Save-Canvas $encounter (Join-Path $assetRoot 'EncounterBackground.png')

$enemy = New-Canvas 512 512
$g = $enemy[1]
$g.FillEllipse((New-Brush '#563d73'), 66, 95, 380, 320)
$g.FillEllipse((New-Brush '#8b69a4'), 120, 40, 272, 350)
$g.FillEllipse((New-Brush '#f2c14e'), 172, 155, 52, 64)
$g.FillEllipse((New-Brush '#f2c14e'), 288, 155, 52, 64)
$g.FillPie((New-Brush '#2a1e35'), 171, 222, 170, 110, 0, 180)
$g.FillPolygon((New-Brush '#d9e6e0'), [Drawing.Point[]]@([Drawing.Point]::new(190, 252), [Drawing.Point]::new(215, 297), [Drawing.Point]::new(235, 248)))
$g.FillPolygon((New-Brush '#d9e6e0'), [Drawing.Point[]]@([Drawing.Point]::new(277, 248), [Drawing.Point]::new(297, 297), [Drawing.Point]::new(322, 252)))
Save-Canvas $enemy (Join-Path $assetRoot 'MireWarden.png')

function Write-Wave([string]$path) {
    $sampleRate = 22050
    $seconds = 4
    $samples = $sampleRate * $seconds
    $stream = [IO.File]::Create($path)
    $writer = [IO.BinaryWriter]::new($stream)
    $dataSize = $samples * 2
    $writer.Write([Text.Encoding]::ASCII.GetBytes('RIFF'))
    $writer.Write(36 + $dataSize)
    $writer.Write([Text.Encoding]::ASCII.GetBytes('WAVEfmt '))
    $writer.Write(16); $writer.Write([int16]1); $writer.Write([int16]1)
    $writer.Write($sampleRate); $writer.Write($sampleRate * 2); $writer.Write([int16]2); $writer.Write([int16]16)
    $writer.Write([Text.Encoding]::ASCII.GetBytes('data')); $writer.Write($dataSize)
    for ($i = 0; $i -lt $samples; $i++) {
        $t = $i / $sampleRate
        $fade = [math]::Min(1.0, [math]::Min($t * 2, ($seconds - $t) * 2))
        $sample = ([math]::Sin(2 * [math]::PI * 220 * $t) + 0.55 * [math]::Sin(2 * [math]::PI * 277.18 * $t) + 0.35 * [math]::Sin(2 * [math]::PI * 329.63 * $t)) / 1.9
        $writer.Write([int16]($sample * $fade * 5200))
    }
    $writer.Dispose(); $stream.Dispose()
}
Write-Wave (Join-Path $assetRoot 'LumenTheme.wav')

function New-Layer([int]$width, [int]$height, [int]$value) {
    $values = [int[]]::new($width * $height)
    for ($i = 0; $i -lt $values.Length; $i++) { $values[$i] = $value }
    return $values
}

function Set-Cell($layer, [int]$width, [int]$x, [int]$y, [int]$value) { $layer[$y * $width + $x] = $value }

function Write-Map([string]$path, [int]$width, [int]$height, $ground, $detail, $foreground, $collision, $regions) {
    $builder = [Text.StringBuilder]::new()
    [void]$builder.AppendLine('SMILE-MAP 1')
    [void]$builder.AppendLine("SIZE $width $height")
    [void]$builder.AppendLine('CELL 64 64')
    foreach ($section in @(@('GROUND', $ground), @('DETAIL', $detail), @('FOREGROUND', $foreground), @('COLLISION', $collision), @('REGIONS', $regions))) {
        [void]$builder.AppendLine($section[0])
        for ($y = 0; $y -lt $height; $y++) {
            $row = for ($x = 0; $x -lt $width; $x++) { $section[1][$y * $width + $x] }
            [void]$builder.AppendLine(($row -join ' '))
        }
    }
    [void]$builder.AppendLine('END')
    [IO.File]::WriteAllText($path, $builder.ToString(), [Text.UTF8Encoding]::new($false))
}

$w = 28; $h = 20
$ground = New-Layer $w $h 1; $detail = New-Layer $w $h 0; $foreground = New-Layer $w $h 0; $collision = New-Layer $w $h 0; $regions = New-Layer $w $h 0
for ($x = 0; $x -lt $w; $x++) { Set-Cell $collision $w $x 0 1; Set-Cell $collision $w $x ($h - 1) 1 }
for ($y = 0; $y -lt $h; $y++) { Set-Cell $collision $w 0 $y 1; Set-Cell $collision $w ($w - 1) $y 1 }
for ($x = 1; $x -lt $w - 1; $x++) { Set-Cell $ground $w $x 10 2 }
for ($y = 1; $y -lt $h - 1; $y++) { Set-Cell $ground $w 13 $y 2; Set-Cell $ground $w 14 $y 2 }
foreach ($building in @(@(4,3,5,4), @(18,3,6,4), @(4,13,6,4), @(18,13,6,4))) {
    for ($y = $building[1]; $y -lt $building[1] + $building[3]; $y++) { for ($x = $building[0]; $x -lt $building[0] + $building[2]; $x++) { Set-Cell $detail $w $x $y 4; Set-Cell $collision $w $x $y 1 } }
    for ($x = $building[0]; $x -lt $building[0] + $building[2]; $x++) { Set-Cell $foreground $w $x $building[1] 6 }
    $doorX = $building[0] + [math]::Floor($building[2] / 2); $doorY = $building[1] + $building[3] - 1
    Set-Cell $detail $w $doorX $doorY 2; Set-Cell $foreground $w $doorX $building[1] 0; Set-Cell $collision $w $doorX $doorY 0
}
Set-Cell $regions $w 6 6 2; Set-Cell $regions $w 6 7 2
Set-Cell $regions $w 21 6 3; Set-Cell $regions $w 21 7 3
Set-Cell $regions $w 13 18 1; Set-Cell $regions $w 14 18 1
foreach ($tree in @(@(2,2),@(11,3),@(16,2),@(25,2),@(2,8),@(24,9),@(2,17),@(11,16),@(16,17),@(25,16))) { Set-Cell $detail $w $tree[0] $tree[1] 5; Set-Cell $collision $w $tree[0] $tree[1] 1 }
Write-Map (Join-Path $mapRoot 'Town.smilemap') $w $h $ground $detail $foreground $collision $regions

$w = 16; $h = 12
$ground = New-Layer $w $h 7; $detail = New-Layer $w $h 0; $foreground = New-Layer $w $h 0; $collision = New-Layer $w $h 0; $regions = New-Layer $w $h 0
for ($x = 0; $x -lt $w; $x++) { Set-Cell $detail $w $x 0 4; Set-Cell $collision $w $x 0 1; Set-Cell $detail $w $x ($h - 1) 4; Set-Cell $collision $w $x ($h - 1) 1 }
for ($y = 0; $y -lt $h; $y++) { Set-Cell $detail $w 0 $y 4; Set-Cell $collision $w 0 $y 1; Set-Cell $detail $w ($w - 1) $y 4; Set-Cell $collision $w ($w - 1) $y 1 }
for ($x = 4; $x -le 11; $x++) { Set-Cell $detail $w $x 4 4; Set-Cell $collision $w $x 4 1 }
Set-Cell $regions $w 8 10 6
Write-Map (Join-Path $mapRoot 'Shop.smilemap') $w $h $ground $detail $foreground $collision $regions

$w = 24; $h = 18
$ground = New-Layer $w $h 8; $detail = New-Layer $w $h 0; $foreground = New-Layer $w $h 0; $collision = New-Layer $w $h 0; $regions = New-Layer $w $h 5
for ($x = 0; $x -lt $w; $x++) { Set-Cell $detail $w $x 0 9; Set-Cell $collision $w $x 0 1; Set-Cell $detail $w $x ($h - 1) 9; Set-Cell $collision $w $x ($h - 1) 1 }
for ($y = 0; $y -lt $h; $y++) { Set-Cell $detail $w 0 $y 9; Set-Cell $collision $w 0 $y 1; Set-Cell $detail $w ($w - 1) $y 9; Set-Cell $collision $w ($w - 1) $y 1 }
for ($y = 3; $y -lt 16; $y++) { Set-Cell $ground $w 11 $y 2; Set-Cell $ground $w 12 $y 2; Set-Cell $regions $w 11 $y 0; Set-Cell $regions $w 12 $y 0 }
for ($x = 8; $x -le 15; $x++) { Set-Cell $ground $w $x 14 2; Set-Cell $regions $w $x 14 0 }
Set-Cell $regions $w 11 15 4; Set-Cell $regions $w 12 15 4
foreach ($mountain in @(@(3,3),@(5,4),@(18,3),@(20,5),@(3,12),@(19,13),@(7,8),@(16,9))) { Set-Cell $detail $w $mountain[0] $mountain[1] 9; Set-Cell $collision $w $mountain[0] $mountain[1] 1; Set-Cell $regions $w $mountain[0] $mountain[1] 0 }
Write-Map (Join-Path $mapRoot 'Overworld.smilemap') $w $h $ground $detail $foreground $collision $regions

Write-Host "Generated original Phase 7 gallery assets and maps beneath $galleryRoot"
