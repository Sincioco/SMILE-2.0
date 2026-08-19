param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot "..\games\SpaceWars\Assets")
)

$ErrorActionPreference = "Stop"

function New-PcmWave {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [double[]]$Samples,
        [int]$SampleRate = 22050
    )

    $stream = [System.IO.MemoryStream]::new()
    $writer = [System.IO.BinaryWriter]::new($stream)
    try {
        $dataLength = $Samples.Length * 2
        $writer.Write([System.Text.Encoding]::ASCII.GetBytes("RIFF"))
        $writer.Write([int](36 + $dataLength))
        $writer.Write([System.Text.Encoding]::ASCII.GetBytes("WAVEfmt "))
        $writer.Write([int]16)
        $writer.Write([int16]1)
        $writer.Write([int16]1)
        $writer.Write([int]$SampleRate)
        $writer.Write([int]($SampleRate * 2))
        $writer.Write([int16]2)
        $writer.Write([int16]16)
        $writer.Write([System.Text.Encoding]::ASCII.GetBytes("data"))
        $writer.Write([int]$dataLength)

        foreach ($sample in $Samples) {
            $bounded = [Math]::Max(-1.0, [Math]::Min(1.0, $sample))
            $writer.Write([int16]($bounded * 30000))
        }

        $writer.Flush()
        [System.IO.File]::WriteAllBytes($Path, $stream.ToArray())
    }
    finally {
        $writer.Dispose()
        $stream.Dispose()
    }
}

function New-ToneSamples {
    param(
        [double]$Duration,
        [scriptblock]$Sample
    )

    $sampleRate = 22050
    $length = [int]($Duration * $sampleRate)
    $values = [double[]]::new($length)
    for ($index = 0; $index -lt $length; $index++) {
        $time = $index / $sampleRate
        $values[$index] = & $Sample $time $Duration $index
    }
    return $values
}

[System.IO.Directory]::CreateDirectory($OutputDirectory) | Out-Null

$laser = New-ToneSamples 0.18 {
    param($time, $duration, $index)
    $frequency = 920 - 610 * ($time / $duration)
    $envelope = [Math]::Pow(1 - $time / $duration, 1.7)
    return [Math]::Sin(2 * [Math]::PI * $frequency * $time) * $envelope * 0.55
}

$random = [System.Random]::new(42019)
$explosion = New-ToneSamples 0.62 {
    param($time, $duration, $index)
    $envelope = [Math]::Pow(1 - $time / $duration, 2.0)
    $noise = $random.NextDouble() * 2 - 1
    $rumble = [Math]::Sin(2 * [Math]::PI * 64 * $time)
    return ($noise * 0.58 + $rumble * 0.42) * $envelope * 0.72
}

$shieldHit = New-ToneSamples 0.28 {
    param($time, $duration, $index)
    $envelope = 1 - $time / $duration
    $carrier = [Math]::Sin(2 * [Math]::PI * 180 * $time)
    $ring = [Math]::Sin(2 * [Math]::PI * 740 * $time)
    return ($carrier * 0.55 + $ring * 0.35) * $envelope * 0.55
}

$missionStart = New-ToneSamples 0.74 {
    param($time, $duration, $index)
    $frequency = if ($time -lt 0.24) { 392 } elseif ($time -lt 0.47) { 523 } else { 784 }
    $segmentTime = if ($time -lt 0.24) { $time } elseif ($time -lt 0.47) { $time - 0.24 } else { $time - 0.47 }
    $envelope = [Math]::Max(0, 1 - $segmentTime / 0.27)
    return [Math]::Sin(2 * [Math]::PI * $frequency * $time) * $envelope * 0.42
}

$missionComplete = New-ToneSamples 0.82 {
    param($time, $duration, $index)
    $frequency = if ($time -lt 0.26) { 440 } elseif ($time -lt 0.52) { 554 } else { 659 }
    $segmentTime = $time % 0.26
    $envelope = [Math]::Max(0, 1 - $segmentTime / 0.3)
    return [Math]::Sin(2 * [Math]::PI * $frequency * $time) * $envelope * 0.4
}

$victory = New-ToneSamples 1.18 {
    param($time, $duration, $index)
    $step = [Math]::Min(3, [int]($time / 0.29))
    $frequency = @(392, 523, 659, 784)[$step]
    $segmentTime = $time % 0.29
    $envelope = [Math]::Max(0, 1 - $segmentTime / 0.34)
    return ([Math]::Sin(2 * [Math]::PI * $frequency * $time) * 0.34 +
        [Math]::Sin(2 * [Math]::PI * ($frequency / 2) * $time) * 0.16) * $envelope
}

New-PcmWave (Join-Path $OutputDirectory "Laser.wav") $laser
New-PcmWave (Join-Path $OutputDirectory "Explosion.wav") $explosion
New-PcmWave (Join-Path $OutputDirectory "ShieldHit.wav") $shieldHit
New-PcmWave (Join-Path $OutputDirectory "MissionStart.wav") $missionStart
New-PcmWave (Join-Path $OutputDirectory "MissionComplete.wav") $missionComplete
New-PcmWave (Join-Path $OutputDirectory "Victory.wav") $victory

Write-Host "Generated original Space Wars sound effects in $OutputDirectory"
