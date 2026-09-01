[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$graphicsPath = Join-Path $repositoryRoot 'libraries\Smile.Simple3D\Graphics3D.smile'
$effectsPath = Join-Path $repositoryRoot 'libraries\Smile.Simple3D\Effects3D.smile'
$testsPath = Join-Path $repositoryRoot 'examples\Renderer3DVfxLab\Tests.smile'
$nativePath = Join-Path $repositoryRoot 'src\Smile.NativeRuntime\graphics\graphics3d_directx.cpp'
$webPath = Join-Path $repositoryRoot 'src\Smile.Compiler\WebOutputWriter.cs'
$effectsGate = Join-Path $repositoryRoot 'scripts\test-effects3d.ps1'

function Assert-Contains([string]$Text, [string]$ExpectedText, [string]$Label) {
    if ($Text.IndexOf($ExpectedText, [System.StringComparison]::Ordinal) -lt 0) {
        throw "$Label is missing required text: $ExpectedText"
    }
}

function Assert-NotContains([string]$Text, [string]$UnexpectedText, [string]$Label) {
    if ($Text.IndexOf($UnexpectedText, [System.StringComparison]::Ordinal) -ge 0) {
        throw "$Label contains forbidden text: $UnexpectedText"
    }
}

function Get-Region([string]$Text, [string]$Start, [string]$End, [string]$Label) {
    $startIndex = $Text.IndexOf($Start, [System.StringComparison]::Ordinal)
    $endIndex = $Text.IndexOf($End, $startIndex + $Start.Length, [System.StringComparison]::Ordinal)
    if ($startIndex -lt 0 -or $endIndex -le $startIndex) {
        throw "$Label region was not found."
    }

    return $Text.Substring($startIndex, $endIndex - $startIndex)
}

Push-Location $repositoryRoot
try {
    $graphics = Get-Content -LiteralPath $graphicsPath -Raw
    $effects = Get-Content -LiteralPath $effectsPath -Raw
    $tests = Get-Content -LiteralPath $testsPath -Raw
    $native = Get-Content -LiteralPath $nativePath -Raw
    $web = Get-Content -LiteralPath $webPath -Raw

    foreach ($contract in @(
        'VFX_QUERY_RESOURCE_STAGING_REVISION = 37',
        'VFX_QUERY_RESOURCE_UPLOADED_REVISION = 38',
        'VFX_QUERY_RESOURCE_STATE = 39',
        'VFX_QUERY_RESOURCE_PENDING_DESTRUCTION = 40',
        'VFX_QUERY_RESOURCE_COMMITTED_BYTES = 41')) {
        Assert-Contains $graphics $contract 'M6.1 diagnostics'
    }

    foreach ($contract in @(
        'SmileParticleInstance3D* committed_instances;',
        'SmileRibbonVertex3D* staging_vertices;',
        'smile_3d_upload_particle_data(batch, batch->instances, count, revision)',
        'smile_3d_upload_ribbon_data(batch, batch->staging_vertices, count, revision)',
        'batch->committed_instances = batch->instances;',
        'batch->vertices = batch->staging_vertices;')) {
        Assert-Contains $native $contract 'Native staging/committed isolation'
    }

    $nativeParticleCommand = Get-Region $native 'static long long smile_3d_particle_batch_command' `
        'static long long smile_3d_ribbon_batch_command' 'Native particle command'
    $nativeParticleSetters = Get-Region $nativeParticleCommand 'if (operation == 2)' `
        'if (operation == 4)' 'Native particle staging setters'
    Assert-NotContains $nativeParticleSetters 'batch->in_flight != 0' 'Native particle staging setters'

    foreach ($contract in @(
        'function renderer3DEnsureParticleBatchGpu(batch)',
        'function renderer3DEnsureRibbonBatchGpu(batch)',
        'batch.committedInstances.set(batch.instances)',
        'batch.vertices=batch.stagingVertices',
        'renderer3DEnsureParticleBatchGpu(batch):renderer3DEnsureRibbonBatchGpu(batch)')) {
        Assert-Contains $web $contract 'Web staging/restoration isolation'
    }

    if ([regex]::Matches($web, 'addEventListener\("webglcontextlost"').Count -ne 1 -or
        [regex]::Matches($web, 'addEventListener\("webglcontextrestored"').Count -ne 1) {
        throw 'Web context-loss listeners must be installed exactly once in generated runtime source.'
    }

    foreach ($contract in @(
        'Dim CandidateAlphaBatch As Core.ParticleBatch3D',
        'Dim CandidateAdditiveBatch As Core.ParticleBatch3D',
        'Dim CandidateRibbonBatch As Core.RibbonBatch3D',
        'Private Function FreeImpulseCount() As Number',
        'Private Dim LightRequestX[32] As Number',
        'Private Dim AudioRequestCue[32] As Number',
        'LightRequestCount >= MAX_TRANSIENT_REQUESTS',
        'AudioRequestCount >= MAX_TRANSIENT_REQUESTS',
        'Stopped = StopEffect(Value)')) {
        Assert-Contains $effects $contract 'Effects3D transactional/request contract'
    }

    foreach ($contract in @(
        'PartitionStateHash(1, AlphaMaterial, AdditiveMaterial, RibbonMaterial)',
        'For Index = 2 To 5',
        'For Index = 1 To Effects3D.MAX_IMPULSES',
        'For Index = 1 To 10',
        'VFX_RESOURCE_STATE_IN_FLIGHT',
        'VFX_QUERY_RESOURCE_PENDING_DESTRUCTION')) {
        Assert-Contains $tests $contract 'M6.1 adversarial test matrix'
    }

    & $effectsGate -Configuration $Configuration
    if ($LASTEXITCODE -ne 0) { throw 'Effects3D focused gate failed from M6.1 hardening gate.' }

    Write-Host ('Renderer3D M6.1 native/Web revision isolation, transactional lifecycle, ' +
        'determinism, request capacity, socket invalidation, restoration, and hot-path tests passed.')
}
finally {
    Pop-Location
}
