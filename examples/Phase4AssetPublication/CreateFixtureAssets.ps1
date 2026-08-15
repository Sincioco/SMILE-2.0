$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

$directories = @(
    'Assets\Exact',
    'Assets\UI',
    'Assets\UI\Sub',
    'Assets\Audio',
    'Assets\Audio\Sub',
    'Assets\Empty',
    'Assets\Unlisted'
)
foreach ($relative in $directories) {
    New-Item -ItemType Directory -Force -Path (Join-Path $root $relative) | Out-Null
}

# Valid 1x1 RGBA PNG, sufficient for publication/load tests.
$png = [Convert]::FromBase64String(
    'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M/wHwAF/gL+XoX3WQAAAABJRU5ErkJggg=='
)
[IO.File]::WriteAllBytes((Join-Path $root 'Assets\UI\Window.png'), $png)
[IO.File]::WriteAllBytes((Join-Path $root 'Assets\UI\Icon.png'), $png)
[IO.File]::WriteAllBytes((Join-Path $root 'Assets\UI\Sub\Nested.png'), $png)

function Write-TinyWav([string]$Path, [int]$Frequency) {
    $sampleRate = 8000
    $samples = 400
    $bytes = New-Object byte[] ($samples * 2)
    for ($i = 0; $i -lt $samples; $i++) {
        $value = [int16](1200 * [Math]::Sin(2 * [Math]::PI * $Frequency * $i / $sampleRate))
        $bytes[$i * 2] = [byte]($value -band 0xff)
        $bytes[$i * 2 + 1] = [byte](($value -shr 8) -band 0xff)
    }
    $stream = [IO.File]::Create($Path)
    try {
        $writer = [IO.BinaryWriter]::new($stream)
        try {
            $writer.Write([Text.Encoding]::ASCII.GetBytes('RIFF'))
            $writer.Write([int](36 + $bytes.Length))
            $writer.Write([Text.Encoding]::ASCII.GetBytes('WAVE'))
            $writer.Write([Text.Encoding]::ASCII.GetBytes('fmt '))
            $writer.Write([int]16)
            $writer.Write([int16]1)
            $writer.Write([int16]1)
            $writer.Write([int]$sampleRate)
            $writer.Write([int]($sampleRate * 2))
            $writer.Write([int16]2)
            $writer.Write([int16]16)
            $writer.Write([Text.Encoding]::ASCII.GetBytes('data'))
            $writer.Write([int]$bytes.Length)
            $writer.Write($bytes)
        } finally { $writer.Dispose() }
    } finally { $stream.Dispose() }
}

Write-TinyWav (Join-Path $root 'Assets\Audio\Click.wav') 440
Write-TinyWav (Join-Path $root 'Assets\Audio\Sub\Confirm.wav') 660
Write-TinyWav (Join-Path $root 'Assets\UI\Click.wav') 330

[IO.File]::WriteAllText((Join-Path $root 'Assets\Exact\Readme.txt'), 'EXACT ASSET')
[IO.File]::WriteAllText((Join-Path $root 'Assets\Audio\Sub\Notes.txt'), 'MUST Not PUBLISH')
[IO.File]::WriteAllText((Join-Path $root 'Assets\Unlisted\Secret.txt'), 'MUST Not PUBLISH')

Write-Output 'Phase 4.2 asset fixture generated.'
