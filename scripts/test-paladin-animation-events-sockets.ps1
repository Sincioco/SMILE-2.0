[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$assetTool = Join-Path $repositoryRoot 'artifacts\assettool\smileasset.exe'
$dragonfallSourceRoot = Join-Path $repositoryRoot 'games\Dragonfall\SourceAssets\Arin'
$sinStarSourceRoot = Join-Path $repositoryRoot `
    'games\SinStarI\SourceAssets\Characters\Paladin\CombatLab'
$descriptorPath = Join-Path $dragonfallSourceRoot 'ArinV55.sm3d.json'
$dragonfallGlb = Join-Path $dragonfallSourceRoot 'arin-integrated-candidate-v5.5.glb'
$sinStarGlb = Join-Path $sinStarSourceRoot 'arin-integrated-candidate-v5.5.glb'
$committedSm3d = Join-Path $repositoryRoot `
    'games\Dragonfall\Assets\Generation2\ArinV55\ArinV55.sm3d'
$preservedV54Blend = Join-Path $repositoryRoot `
    'games\SinStarI\SourceAssets\Characters\Paladin\arin-integrated-candidate-v5.4.blend'
$preservedV54Glb = Join-Path $dragonfallSourceRoot 'arin-integrated-candidate-v5.4.glb'

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

if (-not (Test-Path -LiteralPath $assetTool -PathType Leaf)) {
    throw 'Build SMILE before running the Paladin animation/event/socket gate.'
}

Push-Location $repositoryRoot
try {
    $descriptor = Get-Content -LiteralPath $descriptorPath -Raw | ConvertFrom-Json
    $expectedClips = @(
        'Idle', 'Walk', 'Run', 'Ready', 'SwordAttack', 'ShieldBashCandidate',
        'Defend', 'BlockImpact', 'Hit', 'KO', 'Victory'
    )
    $expectedSockets = @(
        'Root', 'Head', 'Chest', 'SwordBase', 'SwordTip', 'ShieldCenter',
        'HandRight', 'HandLeft', 'FootLeft', 'FootRight'
    )
    $actualClips = @($descriptor.clips.PSObject.Properties.Name)
    $actualSockets = @($descriptor.sockets.PSObject.Properties.Name)

    Assert-True ($descriptor.version -eq 1) 'Paladin descriptor version changed.'
    Assert-True ($descriptor.sampleRate -eq 30) 'Paladin sample rate must remain 30 Hz.'
    Assert-True (($actualClips -join '|') -ceq ($expectedClips -join '|')) `
        'Paladin clip names/order differ from the eleven-action review contract.'
    Assert-True (($actualSockets -join '|') -ceq ($expectedSockets -join '|')) `
        'Paladin socket names/order differ from the production socket contract.'
    Assert-True (Test-Path -LiteralPath $preservedV54Blend -PathType Leaf) `
        'The accepted v5.4 Blender candidate was not preserved.'
    Assert-True (Test-Path -LiteralPath $preservedV54Glb -PathType Leaf) `
        'The accepted v5.4 GLB candidate was not preserved.'

    $events = @(
        @('Walk', 200, 'FootstepLeft', 2001),
        @('Walk', 766, 'FootstepRight', 2002),
        @('Run', 117, 'FootstepLeft', 2001),
        @('Run', 483, 'FootstepRight', 2002),
        @('SwordAttack', 300, 'SwordTrailOn', 1001),
        @('SwordAttack', 633, 'SwordImpact', 1002),
        @('SwordAttack', 967, 'SwordTrailOff', 1003),
        @('ShieldBashCandidate', 500, 'ShieldImpact', 1101)
    )
    foreach ($expected in $events) {
        $matches = @($descriptor.clips.($expected[0]).events | Where-Object {
            $_.timeMs -eq $expected[1] -and $_.name -ceq $expected[2] -and
            $_.value -eq $expected[3]
        })
        Assert-True ($matches.Count -eq 1) `
            "Missing or duplicate event $($expected[2]) for clip $($expected[0])."
    }

    Assert-True ((Get-FileHash $dragonfallGlb -Algorithm SHA256).Hash -ceq
        (Get-FileHash $sinStarGlb -Algorithm SHA256).Hash) `
        'Dragonfall alias and canonical Sin Star I candidate no longer share source identity.'

    $temporaryRoot = Join-Path $repositoryRoot `
        ('artifacts\temp\m7d-paladin-cook-' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $temporaryRoot | Out-Null
    try {
        $first = Join-Path $temporaryRoot 'first.sm3d'
        $second = Join-Path $temporaryRoot 'second.sm3d'
        & $assetTool model $dragonfallGlb --format-version 2 --descriptor $descriptorPath -o $first
        if ($LASTEXITCODE -ne 0) { throw 'First M7D Paladin cook failed.' }
        & $assetTool model $dragonfallGlb --format-version 2 --descriptor $descriptorPath -o $second
        if ($LASTEXITCODE -ne 0) { throw 'Second M7D Paladin cook failed.' }
        Assert-True ((Get-FileHash $first -Algorithm SHA256).Hash -ceq
            (Get-FileHash $second -Algorithm SHA256).Hash) `
            'Two clean M7D Paladin cooks were not byte-identical.'
        Assert-True ((Get-FileHash $first -Algorithm SHA256).Hash -ceq
            (Get-FileHash $committedSm3d -Algorithm SHA256).Hash) `
            'Committed Paladin SM3D differs from a clean descriptor cook.'
        $inspection = (& $assetTool inspect $first | Out-String)
        Assert-True ($inspection.Contains('Clips: 11')) 'Cooked Paladin clip count is not eleven.'
        Assert-True ($inspection.Contains('Events: 8')) 'Cooked Paladin event count is not eight.'
        Assert-True ($inspection.Contains('Sockets: 10')) 'Cooked Paladin socket count is not ten.'
    }
    finally {
        if (Test-Path -LiteralPath $temporaryRoot) {
            Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
        }
    }

    Write-Host ('Paladin v5.5 eleven-clip, eight-event, ten-socket, source-identity, ' +
        'v5.4 preservation, and deterministic-cook gate passed.')
}
finally {
    Pop-Location
}
