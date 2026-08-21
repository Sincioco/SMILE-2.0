[CmdletBinding()]
param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\games\Dragonfall\Assets')
)

$ErrorActionPreference = 'Stop'
$sampleRate = 22050

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

function Get-Envelope {
    param([double]$Time, [double]$Duration, [double]$Attack = 0.03, [double]$Release = 0.12)

    $attackValue = [Math]::Min(1.0, $Time / $Attack)
    $releaseValue = [Math]::Min(1.0, ($Duration - $Time) / $Release)
    return [Math]::Max(0.0, $attackValue * $releaseValue)
}

Write-OriginalWave -Name 'BattleTheme.wav' -DurationSeconds 10.0 -Sample {
    param($time, $duration)
    $minorNotes = @(110.00, 130.81, 146.83, 164.81, 196.00, 174.61, 146.83, 123.47)
    $note = $minorNotes[[int]($time * 4) % $minorNotes.Count]
    $kickPhase = $time % 0.5
    $kick = [Math]::Sin(2 * [Math]::PI * (72 - 55 * $kickPhase) * $kickPhase) * [Math]::Exp(-14 * $kickPhase) * 0.34
    $bass = [Math]::Sin(2 * [Math]::PI * $note * $time) * 0.15
    $brass = [Math]::Sin(2 * [Math]::PI * ($note * 2) * $time) * 0.08
    $fifth = [Math]::Sin(2 * [Math]::PI * ($note * 3) * $time) * 0.035
    (Get-Envelope $time $duration 0.08 0.25) * ($kick + $bass + $brass + $fifth)
}

Write-OriginalWave -Name 'Impact.wav' -DurationSeconds 0.34 -Sample {
    param($time, $duration)
    $falloff = [Math]::Pow([Math]::Max(0.0, 1.0 - $time / $duration), 3)
    $boom = [Math]::Sin(2 * [Math]::PI * (118 - 170 * $time) * $time) * 0.48
    $metal = [Math]::Sin(2 * [Math]::PI * 1260 * $time) * [Math]::Sin(2 * [Math]::PI * 913 * $time) * 0.20
    $falloff * ($boom + $metal)
}

Write-OriginalWave -Name 'Spell.wav' -DurationSeconds 0.75 -Sample {
    param($time, $duration)
    $envelope = [Math]::Sin([Math]::PI * $time / $duration)
    $sweep = 260 + 1450 * $time
    $arcane = [Math]::Sin(2 * [Math]::PI * $sweep * $time) * 0.28
    $spark = [Math]::Sin(2 * [Math]::PI * 1760 * $time) * 0.10
    $envelope * ($arcane + $spark)
}

Write-OriginalWave -Name 'Breath.wav' -DurationSeconds 1.05 -Sample {
    param($time, $duration)
    $envelope = Get-Envelope $time $duration 0.05 0.25
    $roar = [Math]::Sin(2 * [Math]::PI * (95 + 35 * [Math]::Sin(21 * $time)) * $time) * 0.31
    $flame = [Math]::Sin(2 * [Math]::PI * 337 * $time) * [Math]::Sin(2 * [Math]::PI * 701 * $time) * 0.18
    $envelope * ($roar + $flame)
}

Write-OriginalWave -Name 'Enrage.wav' -DurationSeconds 1.35 -Sample {
    param($time, $duration)
    $envelope = Get-Envelope $time $duration 0.02 0.30
    $rise = 65 + 180 * $time
    $roar = [Math]::Sin(2 * [Math]::PI * $rise * $time) * 0.37
    $overtone = [Math]::Sin(2 * [Math]::PI * ($rise * 2.02) * $time) * 0.14
    $envelope * ($roar + $overtone)
}

Write-OriginalWave -Name 'Victory.wav' -DurationSeconds 2.1 -Sample {
    param($time, $duration)
    $notes = @(261.63, 329.63, 392.00, 523.25, 659.25, 783.99)
    $note = $notes[[Math]::Min($notes.Count - 1, [int]($time / 0.32))]
    $envelope = Get-Envelope $time $duration 0.04 0.35
    $lead = [Math]::Sin(2 * [Math]::PI * $note * $time) * 0.23
    $harmony = [Math]::Sin(2 * [Math]::PI * ($note * 1.5) * $time) * 0.10
    $bass = [Math]::Sin(2 * [Math]::PI * ($note / 2) * $time) * 0.08
    $envelope * ($lead + $harmony + $bass)
}

Get-ChildItem -LiteralPath $OutputDirectory -Filter '*.wav' |
    Sort-Object Name |
    Select-Object Name, Length
