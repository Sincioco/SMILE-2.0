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

function New-SmileMelody {
    param(
        [Parameter(Mandatory = $true)][string]$RelativePath,
        [Parameter(Mandatory = $true)][double[]]$Frequencies,
        [Parameter(Mandatory = $true)][int[]]$DurationsMilliseconds,
        [int]$RepeatCount = 1
    )

    if ($Frequencies.Count -ne $DurationsMilliseconds.Count -or $Frequencies.Count -eq 0) {
        throw 'Melody frequencies and durations must be non-empty and have matching lengths.'
    }

    $sampleRate = 22050
    $totalMilliseconds = ($DurationsMilliseconds | Measure-Object -Sum).Sum * $RepeatCount
    $sampleCount = [Math]::Max(1, [int]($sampleRate * $totalMilliseconds / 1000))
    $outputPath = Join-Path $root $RelativePath
    [System.IO.Directory]::CreateDirectory((Split-Path -Parent $outputPath)) | Out-Null

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

            for ($repeat = 0; $repeat -lt $RepeatCount; $repeat++) {
                for ($note = 0; $note -lt $Frequencies.Count; $note++) {
                    $frequency = $Frequencies[$note]
                    $noteSamples = [Math]::Max(1, [int]($sampleRate * $DurationsMilliseconds[$note] / 1000))
                    for ($index = 0; $index -lt $noteSamples; $index++) {
                        $time = $index / $sampleRate
                        $attack = [Math]::Min(1.0, $index / ($sampleRate * 0.008))
                        $release = [Math]::Min(1.0, ($noteSamples - $index) / ($sampleRate * 0.025))
                        $envelope = $attack * $release
                        if ($frequency -le 0) {
                            $sample = [int16]0
                        }
                        else {
                            $fundamental = [Math]::Sin(2 * [Math]::PI * $frequency * $time)
                            $harmonic = 0.22 * [Math]::Sin(4 * [Math]::PI * $frequency * $time)
                            $sample = [int16](5600 * $envelope * ($fundamental + $harmonic))
                        }
                        $writer.Write($sample)
                    }
                }
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

function Convert-SmileMusicToMp3 {
    param(
        [Parameter(Mandatory = $true)][string]$WaveRelativePath,
        [Parameter(Mandatory = $true)][string]$Mp3RelativePath
    )

    $ffmpeg = Get-Command ffmpeg -ErrorAction SilentlyContinue
    if ($null -eq $ffmpeg) {
        Write-Warning "FFmpeg is unavailable; keeping any committed $Mp3RelativePath asset."
        return
    }
    $wavePath = Join-Path $root $WaveRelativePath
    $mp3Path = Join-Path $root $Mp3RelativePath
    & $ffmpeg.Source -y -hide_banner -loglevel error -i $wavePath -map_metadata -1 -id3v2_version 0 -codec:a libmp3lame -b:a 128k -write_xing 0 $mp3Path
    if ($LASTEXITCODE -ne 0) {
        throw "FFmpeg could not encode $Mp3RelativePath."
    }
}

New-SmileTone -RelativePath 'examples\Assets\Graphics.wav' -Frequency 660 -DurationMilliseconds 140
New-SmileTone -RelativePath 'games\Snake\Assets\Eat.wav' -Frequency 880 -DurationMilliseconds 90
New-SmileTone -RelativePath 'games\Snake\Assets\GameOver.wav' -Frequency 180 -DurationMilliseconds 360
New-SmileTone -RelativePath 'games\Snake\Assets\Start.wav' -Frequency 523 -DurationMilliseconds 140
New-SmileTone -RelativePath 'games\Tetris\Assets\Move.wav' -Frequency 420 -DurationMilliseconds 45
New-SmileTone -RelativePath 'games\Tetris\Assets\Rotate.wav' -Frequency 620 -DurationMilliseconds 70
New-SmileTone -RelativePath 'games\Tetris\Assets\LineClear.wav' -Frequency 920 -DurationMilliseconds 180
New-SmileTone -RelativePath 'games\Tetris\Assets\GameOver.wav' -Frequency 150 -DurationMilliseconds 400
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
New-SmileTone -RelativePath 'games\MazeMuncher\Assets\Pellet.wav' -Frequency 760 -DurationMilliseconds 35
New-SmileTone -RelativePath 'games\MazeMuncher\Assets\Power.wav' -Frequency 240 -DurationMilliseconds 220
New-SmileTone -RelativePath 'games\MazeMuncher\Assets\EnemyEaten.wav' -Frequency 1120 -DurationMilliseconds 140
New-SmileTone -RelativePath 'games\MazeMuncher\Assets\PlayerCaught.wav' -Frequency 160 -DurationMilliseconds 400
New-SmileTone -RelativePath 'games\MazeMuncher\Assets\Start.wav' -Frequency 520 -DurationMilliseconds 180
New-SmileTone -RelativePath 'games\MazeMuncher\Assets\LevelClear.wav' -Frequency 960 -DurationMilliseconds 360
New-SmileTone -RelativePath 'games\MazeMuncher\Assets\GameOver.wav' -Frequency 120 -DurationMilliseconds 500
