[CmdletBinding()]
param(
    [string]$OutputBlend,
    [switch]$Publish
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$baseBlend = Join-Path $repositoryRoot `
    'games\SinStarI\SourceAssets\Characters\Paladin\arin-integrated-candidate-v5.4.blend'
$publishedBlend = Join-Path $repositoryRoot `
    'games\SinStarI\SourceAssets\Characters\Paladin\arin-integrated-candidate-v5.5.blend'
$manifest = Join-Path $PSScriptRoot 'build-arin-v5-5-candidate.manifest.json'
$builder = Join-Path $PSScriptRoot 'build-arin-v5-5-candidate.py'
$blender = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'

foreach ($required in @($baseBlend, $manifest, $builder, $blender)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) {
        throw "Required Arin v5.5 build input is missing: $required"
    }
}

$baseHashBefore = (Get-FileHash -LiteralPath $baseBlend -Algorithm SHA256).Hash
$manifestValue = Get-Content -LiteralPath $manifest -Raw | ConvertFrom-Json
$bodySource = Join-Path $repositoryRoot $manifestValue.bodySource
$equipmentSource = Join-Path $repositoryRoot $manifestValue.equipmentSource
foreach ($required in @($bodySource, $equipmentSource)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) {
        throw "Required Arin v5.5 source asset is missing: $required"
    }
}

$temporaryRoot = Join-Path $repositoryRoot `
    ('artifacts\temp\arin-v5-5-build-' + [Guid]::NewGuid().ToString('N'))
$temporaryPrefix = [IO.Path]::GetFullPath((Join-Path $repositoryRoot 'artifacts\temp')) + `
    [IO.Path]::DirectorySeparatorChar
$resolvedTemporary = [IO.Path]::GetFullPath($temporaryRoot)
if (-not $resolvedTemporary.StartsWith($temporaryPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Temporary Arin v5.5 build path escaped artifacts/temp: $resolvedTemporary"
}
$workingBlend = Join-Path $temporaryRoot 'arin-v5.5-working.blend'
$candidateBlend = Join-Path $temporaryRoot 'arin-integrated-candidate-v5.5.blend'

New-Item -ItemType Directory -Force -Path $temporaryRoot | Out-Null
try {
    Copy-Item -LiteralPath $baseBlend -Destination $workingBlend
    & $blender --background $workingBlend --python $builder -- $candidateBlend $manifest
    if ($LASTEXITCODE -ne 0) { throw "Arin v5.5 Blender build failed with exit code $LASTEXITCODE." }
    if (-not (Test-Path -LiteralPath $candidateBlend -PathType Leaf)) {
        throw 'Arin v5.5 Blender build did not produce the candidate Blend.'
    }
    if ((Get-FileHash -LiteralPath $baseBlend -Algorithm SHA256).Hash -cne $baseHashBefore) {
        throw 'The preserved v5.4 Blend changed during the v5.5 build.'
    }

    if ($Publish) {
        $target = if ($OutputBlend) { [IO.Path]::GetFullPath($OutputBlend) } else { $publishedBlend }
        $targetDirectory = Split-Path -Parent $target
        New-Item -ItemType Directory -Force -Path $targetDirectory | Out-Null
        $temporaryTarget = Join-Path $targetDirectory `
            ('.' + [IO.Path]::GetFileName($target) + '.' + [Guid]::NewGuid().ToString('N') + '.tmp')
        Copy-Item -LiteralPath $candidateBlend -Destination $temporaryTarget
        Move-Item -LiteralPath $temporaryTarget -Destination $target -Force
        Write-Host "Published Arin v5.5 Blender candidate: $target"
    }

    Write-Host "Preserved v5.4 Blend SHA256: $baseHashBefore"
    Write-Host "Arin 2K source SHA256: $((Get-FileHash $bodySource -Algorithm SHA256).Hash)"
    Write-Host "Equipment 2K source SHA256: $((Get-FileHash $equipmentSource -Algorithm SHA256).Hash)"
    Write-Host "Candidate Blend SHA256: $((Get-FileHash $candidateBlend -Algorithm SHA256).Hash)"
}
finally {
    if (Test-Path -LiteralPath $temporaryRoot) {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
    }
}
