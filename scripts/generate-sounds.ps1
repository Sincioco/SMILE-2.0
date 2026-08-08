param()

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

function New-SmileTone {
    param(
        [Parameter(Mandatory = $true)][string]$RelativePath,
        [Parameter(Mandatory = $true)][double]$Frequency,
        [Parameter(Mandatory = $true)][int]$DurationMilliseconds
    )

    $sampleRate = 22050
    $sampleCount = [Math]::Max(1, [int]($sampleRate * $DurationMilliseconds / 1000))
    $outputPath = Join-Path $root $RelativePath
    $directory = Split-Path -Parent $outputPath
    [System.IO.Directory]::CreateDirectory($directory) | Out-Null

    $stream = [System.IO.File]::Create($outputPath)
    try {
        $writer = [System.IO.BinaryWriter]::new($stream)
        try {
            $dataLength = $sampleCount * 2
            $writer.Write([Text.Encoding]::ASCII.GetBytes('RIFF'))
            $writer.Write(36 + $dataLength)
            $writer.Write([Text.Encoding]::ASCII.GetBytes('WAVE'))
            $writer.Write([Text.Encoding]::ASCII.GetBytes('fmt '))
            $writer.Write(16)
            $writer.Write([int16]1)
            $writer.Write([int16]1)
            $writer.Write($sampleRate)
            $writer.Write($sampleRate * 2)
            $writer.Write([int16]2)
            $writer.Write([int16]16)
            $writer.Write([Text.Encoding]::ASCII.GetBytes('data'))
            $writer.Write($dataLength)

            for ($index = 0; $index -lt $sampleCount; $index++) {
                $time = $index / $sampleRate
                $attack = [Math]::Min(1.0, $index / ($sampleRate * 0.01))
                $release = [Math]::Min(1.0, ($sampleCount - $index) / ($sampleRate * 0.04))
                $envelope = $attack * $release
                $sample = [int16](9000 * $envelope * [Math]::Sin(2 * [Math]::PI * $Frequency * $time))
                $writer.Write($sample)
            }
        }
        finally {
            $writer.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
}

New-SmileTone -RelativePath 'examples\Assets\Graphics.wav' -Frequency 660 -DurationMilliseconds 140
New-SmileTone -RelativePath 'games\Snake\Assets\Eat.wav' -Frequency 880 -DurationMilliseconds 90
New-SmileTone -RelativePath 'games\Snake\Assets\GameOver.wav' -Frequency 180 -DurationMilliseconds 360
New-SmileTone -RelativePath 'games\Snake\Assets\Start.wav' -Frequency 523 -DurationMilliseconds 140
New-SmileTone -RelativePath 'games\FallingBlocks\Assets\Move.wav' -Frequency 420 -DurationMilliseconds 45
New-SmileTone -RelativePath 'games\FallingBlocks\Assets\Rotate.wav' -Frequency 620 -DurationMilliseconds 70
New-SmileTone -RelativePath 'games\FallingBlocks\Assets\LineClear.wav' -Frequency 920 -DurationMilliseconds 180
New-SmileTone -RelativePath 'games\FallingBlocks\Assets\GameOver.wav' -Frequency 150 -DurationMilliseconds 400
New-SmileTone -RelativePath 'games\PaddleBall\Assets\Paddle.wav' -Frequency 720 -DurationMilliseconds 65
New-SmileTone -RelativePath 'games\PaddleBall\Assets\Wall.wav' -Frequency 440 -DurationMilliseconds 50
New-SmileTone -RelativePath 'games\PaddleBall\Assets\Score.wav' -Frequency 920 -DurationMilliseconds 160
New-SmileTone -RelativePath 'games\PaddleBall\Assets\GameOver.wav' -Frequency 210 -DurationMilliseconds 420
New-SmileTone -RelativePath 'games\BrickBreaker\Assets\Paddle.wav' -Frequency 680 -DurationMilliseconds 60
New-SmileTone -RelativePath 'games\BrickBreaker\Assets\Wall.wav' -Frequency 410 -DurationMilliseconds 45
New-SmileTone -RelativePath 'games\BrickBreaker\Assets\Brick.wav' -Frequency 840 -DurationMilliseconds 70
New-SmileTone -RelativePath 'games\BrickBreaker\Assets\LoseLife.wav' -Frequency 260 -DurationMilliseconds 240
New-SmileTone -RelativePath 'games\BrickBreaker\Assets\LevelClear.wav' -Frequency 1040 -DurationMilliseconds 320
New-SmileTone -RelativePath 'games\BrickBreaker\Assets\GameOver.wav' -Frequency 170 -DurationMilliseconds 450
