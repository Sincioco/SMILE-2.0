[CmdletBinding()]
param(
    [switch]$Check
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$assetTool = Join-Path $repositoryRoot 'artifacts\assettool\smileasset.exe'
$sourceModel = Join-Path $repositoryRoot 'examples\Renderer3DAnimationV2Tests\Source\AnimationArticulated.glb'
$descriptor = Join-Path $repositoryRoot 'examples\AetherBladeVfxTests\Source\AetherBladeActor.sm3d.json'
$assetRoot = Join-Path $repositoryRoot 'examples\AetherBladeVfxTests\Assets'
$cookedAsset = Join-Path $assetRoot 'AetherBladeActor.sm3d'
$sourceAtlas = Join-Path $repositoryRoot 'examples\Renderer3DVfxLab\Assets\VfxAtlas.png'
$publishedAtlas = Join-Path $assetRoot 'VfxAtlas.png'

if (-not (Test-Path -LiteralPath $assetTool)) {
    throw 'Build SMILE before generating the AetherBlade fixture.'
}

[System.IO.Directory]::CreateDirectory($assetRoot) | Out-Null

if ($Check) {
    $temporaryRoot = Join-Path (Join-Path $repositoryRoot 'artifacts\temp') ([System.IO.Path]::GetRandomFileName())
    [System.IO.Directory]::CreateDirectory($temporaryRoot) | Out-Null
    try {
        $temporaryAsset = Join-Path $temporaryRoot 'AetherBladeActor.sm3d'
        & $assetTool model $sourceModel --descriptor $descriptor -o $temporaryAsset
        if ($LASTEXITCODE -ne 0) { throw 'The AetherBlade deterministic fixture conversion failed.' }
        if (-not (Test-Path -LiteralPath $cookedAsset)) { throw "Missing $cookedAsset" }
        if ((Get-FileHash -LiteralPath $temporaryAsset -Algorithm SHA256).Hash -cne
            (Get-FileHash -LiteralPath $cookedAsset -Algorithm SHA256).Hash) {
            throw 'The AetherBlade cooked fixture drifted.'
        }
        if ((Get-FileHash -LiteralPath $sourceAtlas -Algorithm SHA256).Hash -cne
            (Get-FileHash -LiteralPath $publishedAtlas -Algorithm SHA256).Hash) {
            throw 'The AetherBlade atlas copy drifted.'
        }
    }
    finally {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
    }
}
else {
    & $assetTool model $sourceModel --descriptor $descriptor -o $cookedAsset
    if ($LASTEXITCODE -ne 0) { throw 'The AetherBlade fixture conversion failed.' }
    Copy-Item -LiteralPath $sourceAtlas -Destination $publishedAtlas -Force
}

$assetHash = (Get-FileHash -LiteralPath $cookedAsset -Algorithm SHA256).Hash
$atlasHash = (Get-FileHash -LiteralPath $publishedAtlas -Algorithm SHA256).Hash
Write-Output "AetherBlade fixture SHA256 $assetHash"
Write-Output "AetherBlade atlas SHA256 $atlasHash"
