[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$waterRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\TechnicalAssets\Generation3\Water'))
New-Item -ItemType Directory -Path $waterRoot -Force | Out-Null

# Authored procedural textures: reproducible with Windows components, no downloads.
foreach ($kind in @('water-sheet', 'water-drop')) {
    $bitmap = [Drawing.Bitmap]::new(256, 256, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        for ($y = 0; $y -lt 256; $y++) {
            for ($x = 0; $x -lt 256; $x++) {
                $u = $x / 255.0
                $v = $y / 255.0
                if ($kind -eq 'water-sheet') {
                    $wave = [Math]::Sin($u * 37.699 + [Math]::Sin($v * 18.849) * 1.5)
                    $cross = [Math]::Sin($v * 25.133 + [Math]::Sin($u * 18.849) * 1.2)
                    $caustic = [Math]::Pow([Math]::Max(0, 1 - [Math]::Abs($wave + $cross) * 3), 5)
                    $alpha = [int](145 + 55 * $caustic)
                    $red = [int](20 + 200 * $caustic)
                    $green = [int](145 + 105 * $caustic)
                    $blue = 255
                } else {
                    $dx = ($u - .5) * 2
                    $dy = ($v - .5) * 2
                    $radius = [Math]::Sqrt($dx * $dx + $dy * $dy)
                    $body = [Math]::Exp(-$radius * $radius * 3.5)
                    $highlight = [Math]::Exp(-((($dx + .25) * ($dx + .25) + ($dy + .3) * ($dy + .3)) / .025))
                    $fade = [Math]::Max(0, [Math]::Min(1, (.95 - $radius) * 12))
                    $alpha = [int]($fade * (220 * $body))
                    $red = [int](120 + 130 * $highlight)
                    $green = [int](210 + 40 * $highlight)
                    $blue = 255
                }
                $bitmap.SetPixel($x, $y, [Drawing.Color]::FromArgb($alpha, $red, $green, $blue))
            }
        }
        $bitmap.Save((Join-Path $waterRoot "$kind.png"), [Drawing.Imaging.ImageFormat]::Png)
    } finally { $bitmap.Dispose() }
}
# Layered noise, low pressure and a short high splash: original PCM effects, no sampled media.
foreach ($sound in @(@{Name='water-splash'; Seconds=.95; Seed=713},
                     @{Name='water-barrier-hit'; Seconds=1.2; Seed=919},
                     @{Name='water-tsunami'; Seconds=2.4; Seed=1201})) {
    $sampleRate = 24000
    $sampleCount = [int]($sampleRate * $sound.Seconds)
    $stream = [IO.File]::Create((Join-Path $waterRoot "$($sound.Name).wav"))
    $writer = [IO.BinaryWriter]::new($stream)
    try {
        $writer.Write([Text.Encoding]::ASCII.GetBytes('RIFF'))
        $writer.Write([int](36 + $sampleCount * 2))
        $writer.Write([Text.Encoding]::ASCII.GetBytes('WAVEfmt '))
        $writer.Write([int]16); $writer.Write([int16]1); $writer.Write([int16]1)
        $writer.Write([int]$sampleRate); $writer.Write([int]($sampleRate * 2))
        $writer.Write([int16]2); $writer.Write([int16]16)
        $writer.Write([Text.Encoding]::ASCII.GetBytes('data'))
        $writer.Write([int]($sampleCount * 2))
        $random = [Random]::new($sound.Seed)
        $low = 0.0
        for ($sample = 0; $sample -lt $sampleCount; $sample++) {
            $t = $sample / [double]$sampleRate
            $noise = $random.NextDouble() * 2 - 1
            $low = $low * .94 + $noise * .06
            $envelope = [Math]::Min(1, $t * 85) * [Math]::Exp(-$t * 4 / $sound.Seconds)
            $splash = $noise * .16 + $low * 2.4
            $pressure = [Math]::Sin(6.283185 * (65 * $t - 12 * $t * $t)) * [Math]::Exp(-$t * 12) * .25
            if ($sound.Name -eq 'water-tsunami') {
                $envelope = [Math]::Pow([Math]::Sin([Math]::PI * $sample / $sampleCount), .8)
            }
            $value = [Math]::Clamp(($splash + $pressure) * $envelope, -.95, .95)
            $writer.Write([int16]($value * 29000))
        }
    } finally { $writer.Dispose(); $stream.Dispose() }
}
Write-Host "Prepared reusable water textures and audio: $waterRoot"
