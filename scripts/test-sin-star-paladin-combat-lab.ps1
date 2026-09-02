[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [switch]$SkipEvidence
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$compiler = Join-Path $repositoryRoot 'artifacts\compiler\smilec.exe'
$project = Join-Path $repositoryRoot 'games\SinStarI\PaladinCombatLab.smileproj'
$source = Join-Path $repositoryRoot 'games\SinStarI\PaladinCombatLab.smile'
$review = Join-Path $repositoryRoot `
    'docs\implementation\paladin-combat-presentation-m7d-a.review.json'
$evidenceRoot = Join-Path $repositoryRoot `
    'docs\implementation\screenshots\m7d-paladin-combat-presentation'

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function Assert-Contains([string]$Text, [string]$Expected, [string]$Label) {
    if ($Text.IndexOf($Expected, [StringComparison]::Ordinal) -lt 0) {
        throw "$Label is missing required text: $Expected"
    }
}

function Invoke-Compiler([string[]]$Arguments, [string]$Failure) {
    & $compiler @Arguments
    if ($LASTEXITCODE -ne 0) { throw $Failure }
}

if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) {
    throw 'Build SMILE before running the Sin Star I Paladin Combat Lab gate.'
}

Push-Location $repositoryRoot
try {
    $projectText = Get-Content -LiteralPath $project -Raw
    $sourceText = Get-Content -LiteralPath $source -Raw
    Assert-Contains $projectText '<ApplicationId>smile.sinstari.paladin-combat-lab</ApplicationId>' `
        'Combat Lab project identity'
    Assert-Contains $projectText '<Model3DAsset Include=' 'Automatic Model3D cooking'
    Assert-Contains $projectText 'Identity="sin-star-i.character-1.paladin"' `
        'Canonical Paladin identity'
    foreach ($state in @(
        'STATE_READY', 'STATE_RUN', 'STATE_SWORD_ATTACK', 'STATE_SHIELD_BASH',
        'STATE_DEFEND', 'STATE_BLOCK_IMPACT', 'STATE_HIT', 'STATE_KO',
        'STATE_VICTORY')) {
        Assert-Contains $sourceText $state 'Combat presentation state'
    }
    foreach ($event in @(
        'SwordTrailOn', 'SwordImpact', 'SwordTrailOff', 'ShieldImpact',
        'FootstepLeft', 'FootstepRight')) {
        Assert-Contains $sourceText $event 'Combat presentation event'
    }
    Assert-Contains $sourceText 'AuthorizedImpactPending' 'Presentation authorization isolation'
    Assert-Contains $sourceText 'event-fallback-used' 'Deterministic action timeout fallback'
    Assert-Contains $sourceText 'Effects3D.SpawnAtSocket' 'Socket-following effects'
    Assert-Contains $sourceText 'TakeTransientLight' 'Caller-owned transient light request'
    Assert-Contains $sourceText 'TakeAudioCue' 'Caller-owned audio cue request'
    Assert-Contains $sourceText 'SetMaterialInspection3D' 'Material channel inspection'
    Assert-Contains $sourceText 'SetAnimationTime' 'Tooling timeline seek'
    Assert-Contains $sourceText 'DrawParticleBatch3D(SocketMarkers)' 'All-socket visualization'
    Assert-True (-not $sourceText.Contains('Damage')) `
        'Combat Lab must not contain gameplay damage authority.'

    Invoke-Compiler @('--project', $project, '--target', 'windows-x64',
        '--configuration', $Configuration, '--graphics', 'DirectX', '-o',
        'artifacts\games\PaladinCombatLab.exe') 'Paladin Combat Lab native compile failed.'
    Invoke-Compiler @('--project', $project, '--target', 'web',
        '--configuration', $Configuration, '--output-dir',
        'artifacts\web\PaladinCombatLab') 'Paladin Combat Lab Web compile failed.'
    & node --check 'artifacts\web\PaladinCombatLab\game.js'
    if ($LASTEXITCODE -ne 0) { throw 'Paladin Combat Lab Web game syntax failed.' }
    & node --check 'artifacts\web\PaladinCombatLab\smile-runtime.js'
    if ($LASTEXITCODE -ne 0) { throw 'Paladin Combat Lab Web runtime syntax failed.' }

    if (Test-Path -LiteralPath $review) {
        $record = Get-Content -LiteralPath $review -Raw | ConvertFrom-Json
        Assert-True ($record.assetId -ceq 'sin-star-i.character-1.paladin') `
            'Review record stable asset ID changed.'
        Assert-True ($record.clips.Count -eq 11) 'Review record must contain eleven clips.'
        Assert-True ($record.events.Count -eq 8) 'Review record must contain eight events.'
        Assert-True ($record.sockets.Count -eq 10) 'Review record must contain ten sockets.'
    }

    if (-not $SkipEvidence) {
        $required = @(
            '01-idle-ready-native.png', '02-run-native.png',
            '03-sword-anticipation-native.png', '04-sword-impact-native.png',
            '05-shield-bash-native.png', '06-defend-block-native.png',
            '07-hit-ko-native.png', '08-victory-native.png',
            '09-sockets-native.png', '10-sword-impact-web.png',
            '11-shield-bash-web.png', '12-native-web-comparison.png',
            '13-material-channels.png', '14-iphone-contact-sheet.png'
        )
        foreach ($name in $required) {
            $path = Join-Path $evidenceRoot $name
            Assert-True (Test-Path -LiteralPath $path -PathType Leaf) `
                "Missing M7D evidence PNG: $path"
            $bytes = [IO.File]::ReadAllBytes($path)
            Assert-True ($bytes.Length -ge 1024) "M7D evidence PNG is too small: $path"
            Assert-True ($bytes[0] -eq 0x89 -and $bytes[1] -eq 0x50 -and
                $bytes[2] -eq 0x4E -and $bytes[3] -eq 0x47) `
                "M7D evidence is not a true PNG: $path"
        }
        Assert-True (Test-Path -LiteralPath (Join-Path $evidenceRoot 'screenshot-index.md')) `
            'M7D screenshot index is missing.'
    }

    Write-Host 'Sin Star I Paladin presentation-state, event/VFX, socket, timeline, authority-isolation, and native/Web build gate passed.'
}
finally {
    Pop-Location
}
