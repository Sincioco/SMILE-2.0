param(
    [string]$OutputGlb,
    [switch]$Publish
)

$ErrorActionPreference = 'Stop'
if (-not ('SmileAtomicFile' -as [Type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

public static class SmileAtomicFile
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool MoveFileEx(string existingPath, string newPath, int flags);
}
'@
}
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$sourceBlend = Join-Path $repositoryRoot 'games\SinStarI\SourceAssets\Characters\Paladin\arin-integrated-candidate-v5.4.blend'
$publishedGlb = Join-Path $repositoryRoot 'games\Dragonfall\SourceAssets\Arin\arin-integrated-candidate-v5.4.glb'
$descriptor = Join-Path $repositoryRoot 'games\Dragonfall\SourceAssets\Arin\ArinV54.sm3d.json'
$exporter = Join-Path $PSScriptRoot 'export-arin-v5-4-viewer.py'
$blender = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
$assetTool = Join-Path $repositoryRoot 'artifacts\assettool\smileasset.exe'

if (-not (Test-Path -LiteralPath $sourceBlend)) { throw "Missing canonical Arin source: $sourceBlend" }
if (-not (Test-Path -LiteralPath $blender)) { throw "Blender 5.2 is required: $blender" }
if (-not (Test-Path -LiteralPath $assetTool)) { throw "Build smileasset first: $assetTool" }

$sourceHashBefore = (Get-FileHash -LiteralPath $sourceBlend -Algorithm SHA256).Hash
$temporaryRoot = Join-Path $repositoryRoot ('artifacts\temp\arin-v5-4-export-' + [Guid]::NewGuid().ToString('N'))
$resolvedTemporaryRoot = [IO.Path]::GetFullPath($temporaryRoot)
$allowedTemporaryRoot = [IO.Path]::GetFullPath((Join-Path $repositoryRoot 'artifacts\temp')) + [IO.Path]::DirectorySeparatorChar
if (-not $resolvedTemporaryRoot.StartsWith($allowedTemporaryRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Temporary export path escaped the repository artifact root: $resolvedTemporaryRoot"
}
$copyPath = Join-Path $temporaryRoot 'arin-v5.4.export-copy.blend'
$candidateGlb = Join-Path $temporaryRoot 'arin-integrated-candidate-v5.4.glb'
$candidateSm3d = Join-Path $temporaryRoot 'arin-v5.4.candidate.sm3d'

New-Item -ItemType Directory -Force -Path $temporaryRoot | Out-Null

try {
    Copy-Item -LiteralPath $sourceBlend -Destination $copyPath
    & $blender --background $copyPath --python $exporter -- $candidateGlb
    if ($LASTEXITCODE -ne 0) { throw "Blender exporter failed with exit code $LASTEXITCODE." }

    & $assetTool model $candidateGlb --format-version 2 --descriptor $descriptor -o $candidateSm3d
    if ($LASTEXITCODE -ne 0) { throw "SM3D cooking failed with exit code $LASTEXITCODE." }
    if (-not (Test-Path -LiteralPath $candidateSm3d)) { throw 'The validated SM3D candidate was not produced.' }

    $sourceHashAfter = (Get-FileHash -LiteralPath $sourceBlend -Algorithm SHA256).Hash
    if ($sourceHashAfter -cne $sourceHashBefore) { throw 'Canonical Arin source changed during disposable export.' }

    if ($Publish) {
        $publishTarget = if ($OutputGlb) { [IO.Path]::GetFullPath($OutputGlb) } else { $publishedGlb }
        $publishDirectory = Split-Path -Parent $publishTarget
        New-Item -ItemType Directory -Force -Path $publishDirectory | Out-Null
        $sameDirectoryTemporary = Join-Path $publishDirectory ('.' + [IO.Path]::GetFileName($publishTarget) + '.' + [Guid]::NewGuid().ToString('N') + '.tmp')
        Copy-Item -LiteralPath $candidateGlb -Destination $sameDirectoryTemporary
        if (-not [SmileAtomicFile]::MoveFileEx($sameDirectoryTemporary, $publishTarget, 9)) {
            $errorCode = [Runtime.InteropServices.Marshal]::GetLastWin32Error()
            throw "Atomic GLB publication failed with Windows error $errorCode."
        }
        Copy-Item -LiteralPath ($candidateGlb + '.export.json') -Destination ($publishTarget + '.export.json') -Force
        $exportMetadata = Get-Content -LiteralPath ($candidateGlb + '.export.json') -Raw | ConvertFrom-Json
        foreach ($texture in $exportMetadata.textureFiles) {
            Copy-Item -LiteralPath (Join-Path $temporaryRoot $texture.name) `
                -Destination (Join-Path $publishDirectory $texture.name) -Force
        }
        Write-Host "Published validated Arin GLB: $publishTarget"
    } else {
        Write-Host "Validated disposable Arin GLB: $candidateGlb"
    }

    Write-Host "Canonical source SHA256: $sourceHashBefore"
    Write-Host "Candidate GLB SHA256: $((Get-FileHash -LiteralPath $candidateGlb -Algorithm SHA256).Hash)"
    Write-Host "Candidate SM3D SHA256: $((Get-FileHash -LiteralPath $candidateSm3d -Algorithm SHA256).Hash)"
} finally {
    if (Test-Path -LiteralPath $temporaryRoot) {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
    }
}
