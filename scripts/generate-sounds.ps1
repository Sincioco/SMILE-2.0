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
