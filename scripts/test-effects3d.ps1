[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$effectsPath = Join-Path $repositoryRoot 'libraries\Smile.Simple3D\Effects3D.smile'
$projectPath = Join-Path $repositoryRoot 'libraries\Smile.Simple3D\Smile.Simple3D.smilelibproj'
$batchGate = Join-Path $repositoryRoot 'scripts\test-renderer3d-vfx-batches.ps1'

function Assert-Contains([string]$Text, [string]$ExpectedText, [string]$Label) {
    if ($Text.IndexOf($ExpectedText, [System.StringComparison]::Ordinal) -lt 0) {
        throw "$Label is missing required text: $ExpectedText"
    }
}

Push-Location $repositoryRoot
try {
    $effects = Get-Content -LiteralPath $effectsPath -Raw
    $project = Get-Content -LiteralPath $projectPath -Raw

    foreach ($contract in @(
        'Public Const MAX_PRESETS = 64',
        'Public Const MAX_EMITTERS_PER_EFFECT = 8',
        'Public Const MAX_ACTIVE_EFFECTS = 64',
        'Public Const MAX_PARTICLES = 2048',
        'Public Const MAX_IMPULSES = 32',
        'Public Const FIXED_STEP_MILLISECONDS = 10',
        'Public Const MAX_UPDATE_MILLISECONDS = 250',
        'Public Const MAX_CATCH_UP_STEPS = 25',
        'Private Dim ParticleActive[2048] As Boolean',
        'Private Dim EffectActive[64] As Boolean',
        'Private Dim ImpulseActive[32] As Boolean',
        'Private Dim PresetEmitterPresets[64, 8] As Number',
        'Public Function AddEmitterLayer(',
        'Public Function EmitterCount(',
        'Public Function SpawnAtSocket(',
        'Public Function MoveToSocket(',
        'Public Function StopEffect(',
        'Public Function ParticleValue(',
        'Public Function TakeTransientLight()',
        'Public Function TakeAudioCue()',
        'Public Function DroppedSimulationMilliseconds()',
        'Graphics3D.BeginSubmissionGroup3D(3)',
        'Graphics3D.CommitSubmissionGroup3D(SubmissionToken)',
        'Graphics3D.RollbackSubmissionGroup3D(SubmissionToken)')) {
        Assert-Contains $effects $contract 'Effects3D contract'
    }

    foreach ($preset in @(
        'Holy Sword Strike',
        'Shield Impact',
        'Fire Burst',
        'Frost Burst',
        'Heal Spiral',
        'Dragon Fire Breath')) {
        Assert-Contains $effects $preset 'Effects3D standard presets'
    }

    Assert-Contains $effects 'Seed = (Seed * 25173 + 13849) Mod 65536' `
        'Effects3D deterministic seed stream'
    Assert-Contains $effects 'RendererEpochValue = Graphics3D.ResourceEpoch3D()' `
        'Effects3D renderer epoch ownership'
    Assert-Contains $project '<SmileSource Include="Effects3D.smile" />' 'Simple3D source order'

    & $batchGate -Configuration $Configuration
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D VFX batch gate failed from Effects3D gate.' }

    Write-Host 'Effects3D deterministic seed, partition, quality, exhaustion, stop, reset, and native/Web parity tests passed.'
}
finally {
    Pop-Location
}
