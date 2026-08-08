$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot

function Require-File {
    param([string]$RelativePath)

    $path = Join-Path $repositoryRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required artifact is missing: $RelativePath"
    }
    return $path
}

function Assert-NativeGuiX64 {
    param([string]$RelativePath)

    $path = Require-File $RelativePath
    $bytes = [System.IO.File]::ReadAllBytes($path)
    if ($bytes.Length -lt 512 -or $bytes[0] -ne 0x4D -or $bytes[1] -ne 0x5A) {
        throw "$RelativePath is not a valid PE image."
    }

    $peOffset = [BitConverter]::ToInt32($bytes, 0x3C)
    if ($bytes[$peOffset] -ne 0x50 -or $bytes[$peOffset + 1] -ne 0x45) {
        throw "$RelativePath has no PE signature."
    }

    $machine = [BitConverter]::ToUInt16($bytes, $peOffset + 4)
    $optionalHeader = $peOffset + 24
    $magic = [BitConverter]::ToUInt16($bytes, $optionalHeader)
    $subsystem = [BitConverter]::ToUInt16($bytes, $optionalHeader + 68)
    $clrDirectory = $optionalHeader + 112 + (14 * 8)
    $clrSize = [BitConverter]::ToUInt32($bytes, $clrDirectory + 4)

    if ($machine -ne 0x8664) { throw "$RelativePath is not x64 (machine 0x$($machine.ToString('X4')))." }
    if ($magic -ne 0x20B) { throw "$RelativePath is not PE32+." }
    if ($subsystem -ne 2) { throw "$RelativePath is not a Windows GUI executable (subsystem $subsystem)." }
    if ($clrSize -ne 0) { throw "$RelativePath contains a CLR header." }

    Write-Host "Native x64 GUI verified: $RelativePath"
}

function Assert-WaveCopy {
    param([string]$Game, [string]$Name)

    $sourceRelative = "games\$Game\Assets\$Name"
    $outputRelative = "artifacts\games\$Game\Assets\$Name"
    $source = Require-File $sourceRelative
    $output = Require-File $outputRelative
    $bytes = [System.IO.File]::ReadAllBytes($output)
    if ($bytes.Length -lt 12 -or [Text.Encoding]::ASCII.GetString($bytes, 0, 4) -ne 'RIFF' -or
        [Text.Encoding]::ASCII.GetString($bytes, 8, 4) -ne 'WAVE') {
        throw "$outputRelative is not a RIFF/WAVE asset."
    }
    if ((Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $output).Hash) {
        throw "$outputRelative does not match its project asset."
    }
}

Require-File 'artifacts\compiler\smilec.exe' | Out-Null
$vsixPath = Require-File 'artifacts\vsix\Smile.VisualStudio.vsix'

$nativePrograms = @(
    'artifacts\games\GraphicsBasics.exe',
    'artifacts\games\Snake\Snake.exe',
    'artifacts\games\FallingBlocks\FallingBlocks.exe',
    'artifacts\games\PaddleBall\PaddleBall.exe',
    'artifacts\games\BrickBreaker\BrickBreaker.exe'
)
foreach ($program in $nativePrograms) {
    Assert-NativeGuiX64 $program
}

$assetSets = @{
    Snake = @('Eat.wav', 'GameOver.wav', 'Start.wav')
    FallingBlocks = @('GameOver.wav', 'LineClear.wav', 'Move.wav', 'Rotate.wav')
    PaddleBall = @('GameOver.wav', 'Paddle.wav', 'Score.wav', 'Wall.wav')
    BrickBreaker = @('Brick.wav', 'GameOver.wav', 'LevelClear.wav', 'LoseLife.wav', 'Paddle.wav', 'Wall.wav')
}
foreach ($game in $assetSets.Keys) {
    foreach ($asset in $assetSets[$game]) {
        Assert-WaveCopy $game $asset
    }
}
Write-Host 'Game asset copies verified.'

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($vsixPath)
try {
    $entries = @($archive.Entries | ForEach-Object { $_.FullName.Replace('\', '/') })
    $requiredEntries = @(
        'Smile.Language.dll',
        'Smile.VisualStudio.dll',
        'Compiler/smilec.exe',
        'Compiler/Smile.Language.dll',
        'Compiler/Smile.NativeRuntime.lib',
        'ProjectTemplates/Smile/1033/SmileConsole/SmileConsole.smileproj',
        'ProjectTemplates/Smile/1033/SmileConsole/SmileConsole.vstemplate',
        'ProjectTemplates/Smile/1033/SmileGame/SmileGame.smileproj',
        'ProjectTemplates/Smile/1033/SmileGame/SmileGame.vstemplate'
    )
    foreach ($entry in $requiredEntries) {
        if ($entries -notcontains $entry) {
            throw "VSIX entry is missing: $entry"
        }
    }
}
finally {
    $archive.Dispose()
}
Write-Host 'VSIX compiler, shared-language, and project-template payload verified.'

$scaleCases = @(
    @{ Width = 1920; Height = 1080; ExpectedWidth = 1920; ExpectedHeight = 1080; X = 0; Y = 0 },
    @{ Width = 3840; Height = 2160; ExpectedWidth = 3840; ExpectedHeight = 2160; X = 0; Y = 0 },
    @{ Width = 1920; Height = 1200; ExpectedWidth = 1920; ExpectedHeight = 1080; X = 0; Y = 60 }
)
foreach ($case in $scaleCases) {
    if ($case.Width * 540 -le $case.Height * 960) {
        $width = $case.Width
        $height = [math]::Floor($case.Width * 540 / 960)
    }
    else {
        $height = $case.Height
        $width = [math]::Floor($case.Height * 960 / 540)
    }
    $x = [math]::Floor(($case.Width - $width) / 2)
    $y = [math]::Floor(($case.Height - $height) / 2)
    if ($width -ne $case.ExpectedWidth -or $height -ne $case.ExpectedHeight -or $x -ne $case.X -or $y -ne $case.Y) {
        throw "Scaling check failed for $($case.Width)x$($case.Height)."
    }
}
Write-Host '960x540 scale math verified for 1080p, 4K, and letterboxed clients.'
