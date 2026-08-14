param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\examples\MenuGallery\Assets')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$output = [IO.Path]::GetFullPath($OutputDirectory)
[IO.Directory]::CreateDirectory($output) | Out-Null

function New-Canvas([int]$width, [int]$height) {
    [Drawing.Bitmap]::new($width, $height, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
}

function New-Brush([int]$alpha, [int]$red, [int]$green, [int]$blue) {
    [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb($alpha, $red, $green, $blue))
}

function Save-Png([Drawing.Bitmap]$bitmap, [string]$name) {
    $bitmap.Save((Join-Path $output $name), [Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
}

# Original high-resolution illustrated background.
$background = New-Canvas 1920 1080
$graphics = [Drawing.Graphics]::FromImage($background)
$graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
$sky = [Drawing.Drawing2D.LinearGradientBrush]::new(
    [Drawing.Rectangle]::new(0, 0, 1920, 1080),
    [Drawing.Color]::FromArgb(255, 8, 18, 52),
    [Drawing.Color]::FromArgb(255, 66, 28, 92),
    [Drawing.Drawing2D.LinearGradientMode]::Vertical)
$graphics.FillRectangle($sky, 0, 0, 1920, 1080)
$sky.Dispose()
for ($index = 0; $index -lt 90; $index++) {
    $star = New-Brush (90 + ($index % 4) * 35) 190 230 255
    $radius = 2 + ($index % 5)
    $graphics.FillEllipse($star, ($index * 281 + 71) % 1920, ($index * 149 + 43) % 650, $radius, $radius)
    $star.Dispose()
}
$moon = New-Brush 245 220 245 255
$graphics.FillEllipse($moon, 1280, 130, 260, 260)
$moon.Dispose()
$land = New-Brush 255 11 23 52
$graphics.FillPolygon($land, [Drawing.Point[]]@(
    [Drawing.Point]::new(0, 760), [Drawing.Point]::new(330, 540),
    [Drawing.Point]::new(670, 770), [Drawing.Point]::new(1020, 500),
    [Drawing.Point]::new(1370, 780), [Drawing.Point]::new(1650, 590),
    [Drawing.Point]::new(1920, 790), [Drawing.Point]::new(1920, 1080),
    [Drawing.Point]::new(0, 1080)))
$land.Dispose()
$water = [Drawing.Drawing2D.LinearGradientBrush]::new(
    [Drawing.Rectangle]::new(0, 760, 1920, 320),
    [Drawing.Color]::FromArgb(255, 13, 39, 72),
    [Drawing.Color]::FromArgb(255, 4, 10, 26),
    [Drawing.Drawing2D.LinearGradientMode]::Vertical)
$graphics.FillRectangle($water, 0, 760, 1920, 320)
$water.Dispose()
$graphics.Dispose()
Save-Png $background 'Background.png'

# 768x768 alpha nine-slice skin with 192px source borders.
$skin = New-Canvas 768 768
$graphics = [Drawing.Graphics]::FromImage($skin)
$graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.Clear([Drawing.Color]::Transparent)
$shadow = New-Brush 100 0 0 0
$graphics.FillRectangle($shadow, 42, 52, 684, 674)
$shadow.Dispose()
$center = [Drawing.Drawing2D.LinearGradientBrush]::new(
    [Drawing.Rectangle]::new(64, 64, 640, 640),
    [Drawing.Color]::FromArgb(230, 21, 37, 83),
    [Drawing.Color]::FromArgb(230, 8, 16, 42),
    [Drawing.Drawing2D.LinearGradientMode]::Vertical)
$graphics.FillRectangle($center, 64, 64, 640, 640)
$center.Dispose()
for ($index = 0; $index -lt 7; $index++) {
    $pen = [Drawing.Pen]::new([Drawing.Color]::FromArgb(230 - $index * 20, 80 + $index * 12, 190 + $index * 7, 255), 10)
    $graphics.DrawRectangle($pen, 64 + $index * 10, 64 + $index * 10, 640 - $index * 20, 640 - $index * 20)
    $pen.Dispose()
}
$highlight = New-Brush 95 170 242 255
$graphics.FillRectangle($highlight, 130, 95, 508, 18)
$graphics.FillRectangle($highlight, 95, 130, 18, 508)
$highlight.Dispose()
$graphics.Dispose()
Save-Png $skin 'WindowSkin.png'

function New-Indicator([string]$name, [Drawing.Point[]]$points, [int]$size) {
    $bitmap = New-Canvas $size $size
    $drawing = [Drawing.Graphics]::FromImage($bitmap)
    $drawing.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $drawing.Clear([Drawing.Color]::Transparent)
    $glow = New-Brush 70 80 225 255
    $drawing.FillEllipse($glow, 4, 4, $size - 8, $size - 8)
    $glow.Dispose()
    $main = New-Brush 245 220 250 255
    $drawing.FillPolygon($main, $points)
    $main.Dispose()
    $drawing.Dispose()
    Save-Png $bitmap $name
}

New-Indicator 'Cursor.png' ([Drawing.Point[]]@(
    [Drawing.Point]::new(22, 18), [Drawing.Point]::new(108, 64),
    [Drawing.Point]::new(22, 110), [Drawing.Point]::new(45, 64))) 128
New-Indicator 'Continue.png' ([Drawing.Point[]]@(
    [Drawing.Point]::new(16, 30), [Drawing.Point]::new(80, 30),
    [Drawing.Point]::new(48, 70))) 96

# Fixed 96-glyph, 16-column, 64x64-cell bitmap-font atlas.
$atlas = New-Canvas 1024 384
$graphics = [Drawing.Graphics]::FromImage($atlas)
$graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.TextRenderingHint = [Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$graphics.Clear([Drawing.Color]::Transparent)
$font = [Drawing.Font]::new('Consolas', 36, [Drawing.FontStyle]::Bold, [Drawing.GraphicsUnit]::Pixel)
$shadowText = New-Brush 175 3 12 35
$lightText = New-Brush 255 214 247 255
$format = [Drawing.StringFormat]::new()
$format.Alignment = [Drawing.StringAlignment]::Center
$format.LineAlignment = [Drawing.StringAlignment]::Center
for ($index = 0; $index -lt 96; $index++) {
    $column = $index % 16
    $row = [Math]::Floor($index / 16)
    $rectangle = [Drawing.RectangleF]::new($column * 64, $row * 64, 64, 64)
    $shadowRectangle = [Drawing.RectangleF]::new($rectangle.X + 3, $rectangle.Y + 4, 64, 64)
    $character = [string][char](32 + $index)
    $graphics.DrawString($character, $font, $shadowText, $shadowRectangle, $format)
    $graphics.DrawString($character, $font, $lightText, $rectangle, $format)
}
$format.Dispose()
$lightText.Dispose()
$shadowText.Dispose()
$font.Dispose()
$graphics.Dispose()
Save-Png $atlas 'BitmapFont.png'

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
            $envelope = [Math]::Min(1.0, $sample / 90.0) * [Math]::Min(1.0, ($sampleCount - $sample) / 180.0)
            $pcm = [int16]([Math]::Max(-32767, [Math]::Min(32767, $value / $frequencies.Length * 32767.0 * $volume * $envelope)))
            $writer.Write($pcm)
        }
    }
    finally {
        $writer.Dispose()
        $stream.Dispose()
    }
}

Write-Wave 'Move.wav' @(440.0, 660.0) 0.08 0.18
Write-Wave 'Confirm.wav' @(523.25, 783.99) 0.14 0.22
Write-Wave 'Cancel.wav' @(330.0, 246.94) 0.16 0.20

Get-ChildItem -LiteralPath $output | Sort-Object Name | Select-Object Name, Length
