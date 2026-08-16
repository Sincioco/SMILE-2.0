[CmdletBinding()]
param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\examples\RpgBattleGallery\Assets')
)

$ErrorActionPreference = 'Stop'
$sampleRate = 44100

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

function Write-OriginalWave {
    param(
        [Parameter(Mandatory)]
        [string]$Name,

        [Parameter(Mandatory)]
        [double]$DurationSeconds,

        [Parameter(Mandatory)]
        [scriptblock]$Sample
    )

    $targetPath = Join-Path $OutputDirectory $Name
    $sampleCount = [int]($sampleRate * $DurationSeconds)
    $dataLength = $sampleCount * 2
    $stream = [System.IO.File]::Open($targetPath, [System.IO.FileMode]::Create)
    $writer = [System.IO.BinaryWriter]::new($stream)

    try {
        $writer.Write([System.Text.Encoding]::ASCII.GetBytes('RIFF'))
        $writer.Write([int](36 + $dataLength))
        $writer.Write([System.Text.Encoding]::ASCII.GetBytes('WAVE'))
        $writer.Write([System.Text.Encoding]::ASCII.GetBytes('fmt '))
        $writer.Write([int]16)
        $writer.Write([int16]1)
        $writer.Write([int16]1)
        $writer.Write([int]$sampleRate)
        $writer.Write([int]($sampleRate * 2))
        $writer.Write([int16]2)
        $writer.Write([int16]16)
        $writer.Write([System.Text.Encoding]::ASCII.GetBytes('data'))
        $writer.Write([int]$dataLength)

        for ($index = 0; $index -lt $sampleCount; $index++) {
            $time = $index / $sampleRate
            $value = & $Sample $time $DurationSeconds
            $value = [Math]::Max(-1.0, [Math]::Min(1.0, $value))
            $writer.Write([int16]($value * 32760))
        }
    }
    finally {
        $writer.Dispose()
        $stream.Dispose()
    }
}

function Get-SoftEnvelope {
    param([double]$Time, [double]$Duration)

    $attack = [Math]::Min(1.0, $Time / 0.08)
    $release = [Math]::Min(1.0, ($Duration - $Time) / 0.18)
    return [Math]::Max(0.0, $attack * $release)
}

Write-OriginalWave -Name 'OverworldTheme.wav' -DurationSeconds 8.0 -Sample {
    param($time, $duration)
    $notes = @(220.00, 277.18, 329.63, 440.00, 369.99, 329.63, 277.18, 246.94)
    $note = $notes[[int]($time * 2) % $notes.Count]
    $pulse = [Math]::Sin(2 * [Math]::PI * $note * $time) * 0.16
    $pad = [Math]::Sin(2 * [Math]::PI * 110.00 * $time) * 0.08
    $sparkle = [Math]::Sin(2 * [Math]::PI * ($note * 2) * $time) * 0.04
    (Get-SoftEnvelope $time $duration) * ($pulse + $pad + $sparkle)
}

Write-OriginalWave -Name 'TownTheme.wav' -DurationSeconds 8.0 -Sample {
    param($time, $duration)
    $notes = @(261.63, 329.63, 392.00, 523.25, 493.88, 392.00, 349.23, 329.63)
    $note = $notes[[int]($time * 2) % $notes.Count]
    $bell = [Math]::Sin(2 * [Math]::PI * $note * $time) * 0.13
    $bell += [Math]::Sin(2 * [Math]::PI * ($note * 2.01) * $time) * 0.05
    $bass = [Math]::Sin(2 * [Math]::PI * 130.81 * $time) * 0.06
    (Get-SoftEnvelope $time $duration) * ($bell + $bass)
}

Write-OriginalWave -Name 'DungeonTheme.wav' -DurationSeconds 8.0 -Sample {
    param($time, $duration)
    $notes = @(146.83, 155.56, 174.61, 207.65, 196.00, 174.61, 155.56, 138.59)
    $note = $notes[[int]($time * 1.5) % $notes.Count]
    $drone = [Math]::Sin(2 * [Math]::PI * ($note / 2) * $time) * 0.12
    $voice = [Math]::Sin(2 * [Math]::PI * $note * $time) * 0.09
    $shimmer = [Math]::Sin(2 * [Math]::PI * ($note * 3.02) * $time) * 0.025
    (Get-SoftEnvelope $time $duration) * ($drone + $voice + $shimmer)
}

Write-OriginalWave -Name 'Strike.wav' -DurationSeconds 0.22 -Sample {
    param($time, $duration)
    $envelope = [Math]::Pow([Math]::Max(0.0, 1.0 - ($time / $duration)), 3)
    $tone = [Math]::Sin(2 * [Math]::PI * (180 - 90 * $time) * $time)
    $noise = [Math]::Sin(2 * [Math]::PI * 1319 * $time) * [Math]::Sin(2 * [Math]::PI * 997 * $time)
    $envelope * ($tone * 0.32 + $noise * 0.18)
}

Write-OriginalWave -Name 'Ability.wav' -DurationSeconds 0.55 -Sample {
    param($time, $duration)
    $envelope = [Math]::Sin([Math]::PI * $time / $duration)
    $sweep = 360 + 900 * $time
    $tone = [Math]::Sin(2 * [Math]::PI * $sweep * $time) * 0.25
    $chime = [Math]::Sin(2 * [Math]::PI * 1046.50 * $time) * 0.08
    $envelope * ($tone + $chime)
}

Write-OriginalWave -Name 'Victory.wav' -DurationSeconds 1.4 -Sample {
    param($time, $duration)
    $notes = @(392.00, 493.88, 587.33, 783.99)
    $note = $notes[[Math]::Min(3, [int]($time / 0.32))]
    $envelope = Get-SoftEnvelope $time $duration
    $tone = [Math]::Sin(2 * [Math]::PI * $note * $time) * 0.20
    $harmony = [Math]::Sin(2 * [Math]::PI * ($note * 1.25) * $time) * 0.08
    $envelope * ($tone + $harmony)
}

Get-ChildItem -LiteralPath $OutputDirectory -Filter '*.wav' |
    Sort-Object Name |
    Select-Object Name, Length
