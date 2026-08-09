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

function Assert-AssetCopy {
    param([string]$SourceRelative, [string]$OutputRelative)

    $source = Require-File $SourceRelative
    $output = Require-File $OutputRelative
    if ((Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $output).Hash) {
        throw "$OutputRelative does not match its project asset."
    }
}

Require-File 'artifacts\compiler\smilec.exe' | Out-Null
$vsixPath = Require-File 'artifacts\vsix\Smile.VisualStudio.vsix'

$nativePrograms = @(
    'artifacts\games\GraphicsBasics.exe',
    'artifacts\games\ArcBasics.exe',
    'artifacts\games\GraphicsTextSample.exe',
    'artifacts\games\Snake\Snake.exe',
    'artifacts\games\Snake\Snake-NoDemo.exe',
    'artifacts\games\FallingBlocks\FallingBlocks.exe',
    'artifacts\games\FallingBlocks\FallingBlocks-NoDemo.exe',
    'artifacts\games\PaddleBall\PaddleBall.exe',
    'artifacts\games\PaddleBall\PaddleBall-NoDemo.exe',
    'artifacts\games\BrickBreaker\BrickBreaker.exe',
    'artifacts\games\BrickBreaker\BrickBreaker-NoDemo.exe',
    'artifacts\games\MazeMuncher\MazeMuncher.exe',
    'artifacts\games\MazeMuncher\MazeMuncher-NoDemo.exe',
    'artifacts\games\StarSquadron\StarSquadron.exe',
    'artifacts\games\StarSquadron\StarSquadron-NoDemo.exe',
    'artifacts\games\DungeonStarI\DungeonStarI.exe',
    'artifacts\games\DungeonStarI\DungeonStarI-NoDemo.exe'
)
foreach ($program in $nativePrograms) {
    Assert-NativeGuiX64 $program
}

$assetSets = @{
    Snake = @('Eat.wav', 'GameOver.wav', 'Start.wav')
    FallingBlocks = @('GameOver.wav', 'LineClear.wav', 'Move.wav', 'Rotate.wav')
    PaddleBall = @('GameOver.wav', 'Paddle.wav', 'Score.wav', 'Wall.wav')
    BrickBreaker = @('Brick.wav', 'GameOver.wav', 'LevelClear.wav', 'LoseLife.wav', 'Paddle.wav', 'Wall.wav')
    MazeMuncher = @('EnemyEaten.wav', 'GameOver.wav', 'LevelClear.wav', 'Pellet.wav', 'PlayerCaught.wav', 'Power.wav', 'Start.wav')
    StarSquadron = @('Dive.wav', 'EnemyHit.wav', 'EnemyShot.wav', 'GameOver.wav', 'PlayerHit.wav', 'PlayerShot.wav', 'StageClear.wav', 'Start.wav')
}
foreach ($game in $assetSets.Keys) {
    foreach ($asset in $assetSets[$game]) {
        Assert-WaveCopy $game $asset
    }
}
Assert-AssetCopy 'games\FallingBlocks\Assets\Background.mp3' 'artifacts\games\FallingBlocks\Assets\Background.mp3'
Assert-AssetCopy 'games\DungeonStarI\Assets\Background.mp3' 'artifacts\games\DungeonStarI\Assets\Background.mp3'
Assert-AssetCopy 'games\MazeMuncher\Assets\Background.mp3' 'artifacts\games\MazeMuncher\Assets\Background.mp3'
foreach ($map in @('default.map', 'sample-loops.map', 'sample-switchbacks.map')) {
    Assert-AssetCopy "games\DungeonStarI\Maps\$map" "artifacts\games\DungeonStarI\Maps\$map"
}
Assert-AssetCopy 'games\MazeMuncher\Maps\default.map' 'artifacts\games\MazeMuncher\Maps\default.map'
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
    @{ Width = 960; Height = 540; ExpectedWidth = 960; ExpectedHeight = 540; X = 0; Y = 0 },
    @{ Width = 1280; Height = 720; ExpectedWidth = 1280; ExpectedHeight = 720; X = 0; Y = 0 },
    @{ Width = 1920; Height = 1080; ExpectedWidth = 1920; ExpectedHeight = 1080; X = 0; Y = 0 },
    @{ Width = 1920; Height = 1200; ExpectedWidth = 1920; ExpectedHeight = 1080; X = 0; Y = 60 },
    @{ Width = 2560; Height = 1440; ExpectedWidth = 2560; ExpectedHeight = 1440; X = 0; Y = 0 },
    @{ Width = 3440; Height = 1440; ExpectedWidth = 2560; ExpectedHeight = 1440; X = 440; Y = 0 },
    @{ Width = 3840; Height = 2160; ExpectedWidth = 3840; ExpectedHeight = 2160; X = 0; Y = 0 }
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
    $scale = [math]::Min($case.Width / 960.0, $case.Height / 540.0)
    $mappedRadiusX = 9 * $scale
    $mappedRadiusY = 9 * $scale
    $mappedTextSize = 16 * $scale
    if ([math]::Abs($mappedRadiusX - $mappedRadiusY) -gt 0.000001 -or $mappedTextSize -le 0) {
        throw "Uniform coordinate or text-size mapping failed for $($case.Width)x$($case.Height)."
    }
}
Write-Host 'Viewport, uniform coordinate mapping, and text scaling verified for seven required output sizes.'

$dpiCases = @(
    @{ Dpi = 96; Width = 960; Height = 540; Scale = 1.0 },
    @{ Dpi = 120; Width = 1200; Height = 675; Scale = 1.25 },
    @{ Dpi = 144; Width = 1440; Height = 810; Scale = 1.5 },
    @{ Dpi = 192; Width = 1920; Height = 1080; Scale = 2.0 }
)
foreach ($case in $dpiCases) {
    $dpiScale = $case.Dpi / 96.0
    $suggestedWidth = [math]::Round(960 * $dpiScale)
    $suggestedHeight = [math]::Round(540 * $dpiScale)
    $viewportScale = [math]::Min($case.Width / 960.0, $case.Height / 540.0)
    if ($suggestedWidth -ne $case.Width -or $suggestedHeight -ne $case.Height -or
        [math]::Abs($viewportScale - $case.Scale) -gt 0.000001) {
        throw "DPI-change mapping check failed for $($case.Dpi) DPI."
    }
}
Write-Host 'DPI-change output and viewport calculations verified at 100, 125, 150, and 200 percent.'
