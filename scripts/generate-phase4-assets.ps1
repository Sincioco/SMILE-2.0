param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\examples\Phase4VisualSlice\Assets')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$output = [IO.Path]::GetFullPath($OutputDirectory)
[IO.Directory]::CreateDirectory($output) | Out-Null

function New-Canvas([int]$width, [int]$height) {
    return [Drawing.Bitmap]::new($width, $height, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
}

function Save-Png([Drawing.Bitmap]$bitmap, [string]$name) {
    $path = Join-Path $output $name
    $bitmap.Save($path, [Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
}

function New-Brush([int]$alpha, [int]$red, [int]$green, [int]$blue) {
    return [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb($alpha, $red, $green, $blue))
}

$background = New-Canvas 2304 1296
$graphics = [Drawing.Graphics]::FromImage($background)
$graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
$sky = [Drawing.Drawing2D.LinearGradientBrush]::new(
    [Drawing.Rectangle]::new(0, 0, 2304, 1296),
    [Drawing.Color]::FromArgb(255, 13, 24, 65),
    [Drawing.Color]::FromArgb(255, 95, 36, 92),
    [Drawing.Drawing2D.LinearGradientMode]::Vertical)
$graphics.FillRectangle($sky, 0, 0, 2304, 1296)
$sky.Dispose()

for ($i = 0; $i -lt 70; $i++) {
    $x = ($i * 397 + 113) % 2304
    $y = ($i * 173 + 47) % 640
    $radius = 2 + ($i % 5)
    $star = New-Brush (95 + ($i % 4) * 32) 226 236 255
    $graphics.FillEllipse($star, $x, $y, $radius, $radius)
    $star.Dispose()
}

for ($ring = 7; $ring -ge 0; $ring--) {
    $glow = New-Brush (10 + (7 - $ring) * 6) 130 220 255
    $size = 250 + $ring * 54
    $graphics.FillEllipse($glow, 1770 - $size / 2, 265 - $size / 2, $size, $size)
    $glow.Dispose()
}
$moon = New-Brush 245 222 245 255
$graphics.FillEllipse($moon, 1650, 145, 240, 240)
$moon.Dispose()

$farHill = New-Brush 255 30 35 82
$graphics.FillPolygon($farHill, [Drawing.Point[]]@(
    [Drawing.Point]::new(0, 880), [Drawing.Point]::new(300, 610), [Drawing.Point]::new(620, 820),
    [Drawing.Point]::new(930, 500), [Drawing.Point]::new(1260, 790), [Drawing.Point]::new(1570, 560),
    [Drawing.Point]::new(1900, 800), [Drawing.Point]::new(2304, 590), [Drawing.Point]::new(2304, 1296),
    [Drawing.Point]::new(0, 1296)))
$farHill.Dispose()
$nearHill = New-Brush 255 12 22 49
$graphics.FillPolygon($nearHill, [Drawing.Point[]]@(
    [Drawing.Point]::new(0, 1010), [Drawing.Point]::new(380, 760), [Drawing.Point]::new(740, 1020),
    [Drawing.Point]::new(1110, 710), [Drawing.Point]::new(1530, 1040), [Drawing.Point]::new(1950, 735),
    [Drawing.Point]::new(2304, 940), [Drawing.Point]::new(2304, 1296), [Drawing.Point]::new(0, 1296)))
$nearHill.Dispose()

$water = [Drawing.Drawing2D.LinearGradientBrush]::new(
    [Drawing.Rectangle]::new(0, 930, 2304, 366),
    [Drawing.Color]::FromArgb(255, 14, 30, 62),
    [Drawing.Color]::FromArgb(255, 5, 13, 30),
    [Drawing.Drawing2D.LinearGradientMode]::Vertical)
$graphics.FillRectangle($water, 0, 930, 2304, 366)
$water.Dispose()
for ($i = 0; $i -lt 24; $i++) {
    $shine = New-Brush (28 + ($i % 3) * 12) 126 207 255
    $width = 80 + (($i * 83) % 480)
    $graphics.FillEllipse($shine, (($i * 263) % 2180), 950 + $i * 13, $width, 6)
    $shine.Dispose()
}
$graphics.Dispose()
Save-Png $background 'Background.png'

$sheet = New-Canvas 2048 1024
$graphics = [Drawing.Graphics]::FromImage($sheet)
$graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.CompositingQuality = [Drawing.Drawing2D.CompositingQuality]::HighQuality
for ($row = 0; $row -lt 2; $row++) {
    for ($frame = 0; $frame -lt 4; $frame++) {
        $left = $frame * 512
        $top = $row * 512
        $bob = if ($row -eq 0) { @(0, -9, 0, 7)[$frame] } else { @(-4, 8, -12, 4)[$frame] }
        $lean = if ($row -eq 0) { 0 } else { @(-24, -8, 17, 7)[$frame] }

        $shadow = New-Brush 70 4 7 20
        $graphics.FillEllipse($shadow, $left + 126, $top + 448, 260, 34)
        $shadow.Dispose()
        $cape = New-Brush 205 63 38 135
        $graphics.FillPolygon($cape, [Drawing.Point[]]@(
            [Drawing.Point]::new($left + 204 + $lean, $top + 180 + $bob),
            [Drawing.Point]::new($left + 114 + $lean, $top + 426 + $bob),
            [Drawing.Point]::new($left + 360 + $lean, $top + 438 + $bob),
            [Drawing.Point]::new($left + 305 + $lean, $top + 176 + $bob)))
        $cape.Dispose()
        $body = New-Brush 255 48 154 201
        $graphics.FillEllipse($body, $left + 178 + $lean, $top + 190 + $bob, 164, 224)
        $body.Dispose()
        $trim = [Drawing.Pen]::new([Drawing.Color]::FromArgb(235, 151, 235, 255), 16)
        $graphics.DrawArc($trim, $left + 186 + $lean, $top + 207 + $bob, 148, 190, 25, 132)
        $trim.Dispose()
        $face = New-Brush 255 242 190 157
        $graphics.FillEllipse($face, $left + 194 + $lean, $top + 92 + $bob, 128, 134)
        $face.Dispose()
        $hair = New-Brush 255 36 31 72
        $graphics.FillPie($hair, $left + 180 + $lean, $top + 66 + $bob, 158, 160, 178, 184)
        $hair.Dispose()
        $glowAlpha = if ($row -eq 0) { 90 } else { 180 }
        $glow = New-Brush $glowAlpha 108 225 255
        $orbX = $left + 366 + $lean + ($frame - 1) * 8
        $orbY = $top + 206 + $bob - $row * 35
        $graphics.FillEllipse($glow, $orbX - 48, $orbY - 48, 96, 96)
        $glow.Dispose()
        $orb = New-Brush 230 210 251 255
        $graphics.FillEllipse($orb, $orbX - 23, $orbY - 23, 46, 46)
        $orb.Dispose()
        $boot = New-Brush 255 22 31 59
        $stride = if ($row -eq 0) { @(-8, 0, 8, 0)[$frame] } else { @(-22, -8, 18, 6)[$frame] }
        $graphics.FillEllipse($boot, $left + 184 + $lean + $stride, $top + 392 + $bob, 74, 67)
        $graphics.FillEllipse($boot, $left + 264 + $lean - $stride, $top + 392 + $bob, 74, 67)
        $boot.Dispose()
    }
}
$graphics.Dispose()
Save-Png $sheet 'CharacterSheet.png'

$foreground = New-Canvas 1920 1080
$graphics = [Drawing.Graphics]::FromImage($foreground)
$graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
$mist = [Drawing.Drawing2D.LinearGradientBrush]::new(
    [Drawing.Rectangle]::new(0, 700, 1920, 380),
    [Drawing.Color]::FromArgb(0, 90, 190, 235),
    [Drawing.Color]::FromArgb(150, 27, 74, 108),
    [Drawing.Drawing2D.LinearGradientMode]::Vertical)
$graphics.FillRectangle($mist, 0, 700, 1920, 380)
$mist.Dispose()
for ($i = 0; $i -lt 13; $i++) {
    $plant = New-Brush (115 + ($i % 3) * 28) (18 + $i) (55 + $i * 2) (68 + $i * 3)
    $x = ($i * 167) % 1920
    $height = 160 + (($i * 79) % 300)
    $graphics.FillEllipse($plant, $x - 70, 1080 - $height, 150, $height + 80)
    $plant.Dispose()
}
for ($i = 0; $i -lt 9; $i++) {
    $ribbon = [Drawing.Pen]::new([Drawing.Color]::FromArgb(55, 122 + $i * 8, 218, 255), 12 + $i)
    $graphics.DrawArc($ribbon, 900 + $i * 38, 60 + $i * 9, 560, 560, 195, 105)
    $ribbon.Dispose()
}
$graphics.Dispose()
Save-Png $foreground 'Foreground.png'

$pixel = New-Canvas 37 53
$graphics = [Drawing.Graphics]::FromImage($pixel)
$graphics.Clear([Drawing.Color]::Transparent)
for ($y = 0; $y -lt 53; $y += 5) {
    for ($x = 0; $x -lt 37; $x += 5) {
        $checker = if ((($x / 5) + ($y / 5)) % 2 -eq 0) {
            New-Brush 235 85 240 255
        } else {
            New-Brush 180 244 76 171
        }
        $graphics.FillRectangle($checker, $x, $y, 5, 5)
        $checker.Dispose()
    }
}
$graphics.Dispose()
Save-Png $pixel 'PixelProof.png'

function Write-Wave([string]$name, [double[]]$frequencies, [double]$seconds, [double]$volume) {
    $sampleRate = 22050
    $sampleCount = [int]($sampleRate * $seconds)
    $dataBytes = $sampleCount * 2
    $stream = [IO.File]::Create((Join-Path $output $name))
    $writer = [IO.BinaryWriter]::new($stream)
    try {
        $writer.Write([Text.Encoding]::ASCII.GetBytes('RIFF'))
        $writer.Write([int](36 + $dataBytes))
        $writer.Write([Text.Encoding]::ASCII.GetBytes('WAVEfmt '))
        $writer.Write([int]16)
        $writer.Write([int16]1)
        $writer.Write([int16]1)
        $writer.Write([int]$sampleRate)
        $writer.Write([int]($sampleRate * 2))
        $writer.Write([int16]2)
        $writer.Write([int16]16)
        $writer.Write([Text.Encoding]::ASCII.GetBytes('data'))
        $writer.Write([int]$dataBytes)
        for ($sample = 0; $sample -lt $sampleCount; $sample++) {
            $time = $sample / [double]$sampleRate
            $value = 0.0
            foreach ($frequency in $frequencies) {
                $value += [Math]::Sin(2.0 * [Math]::PI * $frequency * $time)
            }
            $envelope = [Math]::Min(1.0, $sample / 220.0) * [Math]::Min(1.0, ($sampleCount - $sample) / 500.0)
            $pcm = [int16]([Math]::Max(-32767, [Math]::Min(32767,
                $value / $frequencies.Length * 32767.0 * $volume * $envelope)))
            $writer.Write($pcm)
        }
    }
    finally {
        $writer.Dispose()
        $stream.Dispose()
    }
}

Write-Wave 'ToneOne.wav' @(392.0, 523.25) 0.42 0.28
Write-Wave 'ToneTwo.wav' @(659.25, 783.99) 0.50 0.24
Write-Wave 'Music.wav' @(110.0, 164.81, 220.0) 3.0 0.10

Get-ChildItem -LiteralPath $output | Sort-Object Name | Select-Object Name, Length
