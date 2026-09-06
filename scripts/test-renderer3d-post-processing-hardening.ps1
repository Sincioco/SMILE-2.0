[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$graphicsPath = Join-Path $repositoryRoot 'libraries\Smile.Simple3D\Graphics3D.smile'
$characterPath = Join-Path $repositoryRoot 'libraries\Smile.Simple3D\Character3D.smile'
$nativeHeaderPath = Join-Path $repositoryRoot 'src\Smile.NativeRuntime\graphics\graphics3d.h'
$nativePath = Join-Path $repositoryRoot 'src\Smile.NativeRuntime\graphics\graphics3d_directx.cpp'
$webPath = Join-Path $repositoryRoot 'src\Smile.Compiler\WebOutputWriter.cs'
$baseGate = Join-Path $repositoryRoot 'scripts\test-renderer3d-post-processing.ps1'

function Assert-Contains([string]$Text, [string]$Expected, [string]$Label) {
    if ($Text.IndexOf($Expected, [System.StringComparison]::Ordinal) -lt 0) {
        throw "$Label is missing required text: $Expected"
    }
}

function Assert-Near([double]$Actual, [double]$Expected, [double]$Tolerance, [string]$Label) {
    if ([Math]::Abs($Actual - $Expected) -gt $Tolerance) {
        throw "$Label expected $Expected but received $Actual."
    }
}

function Convert-SrgbToLinear([double]$Value) {
    if ($Value -le 0.04045) { return $Value / 12.92 }
    return [Math]::Pow(($Value + 0.055) / 1.055, 2.4)
}

function Convert-LinearToSrgb([double]$Value) {
    if ($Value -le 0.0031308) { return $Value * 12.92 }
    return 1.055 * [Math]::Pow($Value, 1.0 / 2.4) - 0.055
}

function Convert-AcesToneMap([double]$Value) {
    $mapped = ($Value * (2.51 * $Value + 0.03)) / ($Value * (2.43 * $Value + 0.59) + 0.14)
    return [Math]::Max(0.0, [Math]::Min(1.0, $mapped))
}

Push-Location $repositoryRoot
try {
    $graphicsSource = Get-Content -LiteralPath $graphicsPath -Raw
    $characterSource = Get-Content -LiteralPath $characterPath -Raw
    $nativeHeader = Get-Content -LiteralPath $nativeHeaderPath -Raw
    $nativeSource = Get-Content -LiteralPath $nativePath -Raw
    $webSource = Get-Content -LiteralPath $webPath -Raw

    Assert-Contains $nativeHeader 'SMILE_3D_SUBMISSION_GROUP = 118' 'Native numeric ABI'
    Assert-Contains $nativeHeader 'SMILE_3D_IMAGE_CREATE_PBR_TEXTURE = 2' 'Native image ABI'
    Assert-Contains $nativeHeader 'SMILE_3D_TEXT_TAKE_MODEL_ANIMATOR_EVENT = 9' 'Native text ABI'
    Assert-Contains $graphicsSource 'Private Const COMMAND_SUBMISSION_GROUP = 118' 'SMILE numeric ABI'
    Assert-Contains $webSource 'case 118:return renderer3DSubmissionGroup(a,b);' 'Web numeric dispatch'

    foreach ($kind in @(
        '#define SMILE_3D_SUBMISSION_OBJECT 1',
        '#define SMILE_3D_SUBMISSION_PARTICLE_BATCH 2',
        '#define SMILE_3D_SUBMISSION_RIBBON_BATCH 3')) {
        Assert-Contains $nativeSource $kind 'Native tagged submission protocol'
    }
    foreach ($kind in @(
        'const renderer3DSubmissionObject = 1;',
        'const renderer3DSubmissionParticleBatch = 2;',
        'const renderer3DSubmissionRibbonBatch = 3;')) {
        Assert-Contains $webSource $kind 'Web tagged submission protocol'
    }

    Assert-Contains $nativeSource 'SmileSubmission3D smile_frame_submissions3d[SMILE_3D_MAX_FRAME_SUBMISSIONS]' `
        'Native fixed submission storage'
    Assert-Contains $nativeSource 'SmilePaletteSnapshot3D smile_frame_palettes3d[SMILE_3D_MAX_FRAME_PALETTES]' `
        'Native fixed palette storage'
    Assert-Contains $webSource 'const renderer3DSubmissions = new Float64Array(512);' `
        'Web fixed submission storage'
    Assert-Contains $webSource 'palette:new Float32Array(128*16)' 'Web fixed palette storage'
    Assert-Contains $nativeSource 'mesh->in_flight++' 'Native mesh in-flight ownership'
    Assert-Contains $nativeSource 'texture->in_flight++' 'Native texture in-flight ownership'
    Assert-Contains $nativeSource 'texture != 0 && texture->in_flight != 0' 'Native model in-flight ownership'
    Assert-Contains $webSource 'mesh.inFlight+=1' 'Web mesh in-flight ownership'
    Assert-Contains $webSource '.inFlight+=1' 'Web texture in-flight ownership'
    Assert-Contains $webSource 'texture&&texture.inFlight)return false' 'Web model in-flight ownership'
    Assert-Contains $nativeSource 'mesh->in_flight != 0' 'Native mutation protection'
    Assert-Contains $webSource 'if (mesh.inFlight)' 'Web mutation protection'

    Assert-Contains $graphicsSource 'Public Function BeginSubmissionGroup3D(Capacity As Number) As Number' `
        'SMILE group begin API'
    Assert-Contains $graphicsSource 'Public Function CommitSubmissionGroup3D(SubmissionToken As Number) As Boolean' `
        'SMILE group commit API'
    Assert-Contains $graphicsSource 'Public Function RollbackSubmissionGroup3D(SubmissionToken As Number) As Boolean' `
        'SMILE group rollback API'
    Assert-Contains $nativeSource 'value != smile_submission_group_token3d' 'Native stale-token protection'
    Assert-Contains $webSource 'value!==renderer3DSubmissionGroupToken' 'Web stale-token protection'
    Assert-Contains $characterSource 'SubmissionToken = Graphics3D.BeginSubmissionGroup3D(ActorPartCounts[Slot])' `
        'Character3D atomic draw begin'
    Assert-Contains $characterSource 'Graphics3D.CommitSubmissionGroup3D(SubmissionToken)' `
        'Character3D atomic draw commit'
    Assert-Contains $characterSource 'CHARACTER_ERROR_DRAW_SUBMISSION' 'Character3D draw failure category'

    Assert-Contains $nativeSource 'smile_shadow_double_raster_state3d' 'Native double-sided shadow state'
    Assert-Contains $nativeSource 'entry->double_sided' `
        'Native per-submission shadow culling'
    Assert-Contains $webSource 'renderer3DApplyCull(object,!!(material&&material.doubleSided))' `
        'Web per-submission shadow culling'
    Assert-Contains $webSource 'if(mode===1||(mode===0&&doubleSided))gl.disable(gl.CULL_FACE)' `
        'Web explicit or material-driven two-sided culling'
    Assert-Contains $nativeSource 'roundf(light_x / texel_x) * texel_x' 'Native directional shadow snapping'
    Assert-Contains $webSource 'Math.round(lightX/texelX)*texelX-lightX' 'Web directional shadow snapping'
    Assert-Contains $nativeSource 'constants.shadow_light, smile_local_lights3d[smile_shadow_slot3d].position' `
        'Native selected spot shadow bias input'
    Assert-Contains $webSource 'renderer3DLocalPositionType[offset+2],2' 'Web selected spot shadow bias input'

    Assert-Contains $nativeSource 'SmileM5TargetState3D previous' 'Native target transaction'
    Assert-Contains $nativeSource 'smile_3d_restore_target_state(previous)' 'Native target rollback'
    Assert-Contains $webSource 'function renderer3DCaptureM5Bundle()' 'Web target transaction capture'
    Assert-Contains $webSource 'renderer3DApplyM5Bundle(previous)' 'Web target rollback'
    Assert-Contains $nativeSource 'smile_m5_resource_generation3d++' 'Native target generation'
    Assert-Contains $webSource 'renderer3DM5ResourceGeneration+=1' 'Web target generation'

    # M5 predates the GPU particle backend's bounded allocation/first-dispatch probes.
    # Keep the no-poll rule for every other renderer path, including MSAA targets.
    $nonParticleProbeSource = ($webSource -split '\r?\n' | Where-Object {
        $_ -notmatch '^\s*function renderer3DGpuParticle(CreateGpu|GpuStep)\('
    }) -join "`n"
    if ($nonParticleProbeSource.IndexOf('gl.getError()', [System.StringComparison]::Ordinal) -ge 0) {
        throw 'Renderer3D Web source contains an unbounded or non-particle stale-error poll.'
    }

    $frameStart = $webSource.IndexOf('function renderer3DBegin(', [System.StringComparison]::Ordinal)
    $frameEnd = $webSource.IndexOf('function renderer3DReset()', $frameStart, [System.StringComparison]::Ordinal)
    if ($frameStart -lt 0 -or $frameEnd -le $frameStart) {
        throw 'Renderer3D Web frame path was not found.'
    }
    $frameSource = $webSource.Substring($frameStart, $frameEnd - $frameStart)
    foreach ($forbidden in @('new Float32Array', 'new Float64Array', '.push(', '.map(', 'renderer3DCompile')) {
        if ($frameSource.IndexOf($forbidden, [System.StringComparison]::Ordinal) -ge 0) {
            throw "Renderer3D Web frame path contains forbidden hot-path text: $forbidden"
        }
    }

    Assert-Near (Convert-SrgbToLinear 0.04045) 0.0031308 0.0000001 'sRGB low-segment decode'
    $linearHalf = Convert-SrgbToLinear 0.5
    Assert-Near $linearHalf 0.21404114 0.0000001 'sRGB half decode'
    Assert-Near (Convert-LinearToSrgb $linearHalf) 0.5 0.0000001 'sRGB round trip'
    Assert-Near (Convert-AcesToneMap 1.0) 0.80379747 0.0000001 'ACES reference'
    Assert-Near (0.4 + 2 * 0.24 + 2 * 0.06) 1.0 0.0000001 'Bloom blur energy'
    Assert-Near ([Math]::Max(0.25, [Math]::Max(1.5, 0.75))) 1.5 0.0000001 'Bloom max-RGB brightness'

    $texel = 500.0 / 2048.0
    $gridPoint = 10.0 * $texel
    $baseSnap = [Math]::Round($gridPoint / $texel) * $texel
    $subTexelSnap = [Math]::Round(($gridPoint + 0.49 * $texel) / $texel) * $texel
    $nextTexelSnap = [Math]::Round(($gridPoint + 0.51 * $texel) / $texel) * $texel
    Assert-Near $subTexelSnap $baseSnap 0.0000001 'Directional sub-texel stability'
    Assert-Near $nextTexelSnap ($baseSnap + $texel) 0.0000001 'Directional texel advance'

    Assert-Contains $nativeSource 'SMILE_3D_SUBMISSION_SNAPSHOT_BYTES 512' 'Native snapshot byte accounting'
    Assert-Contains $nativeSource 'SMILE_3D_PALETTE_SNAPSHOT_BYTES 8208' 'Native palette byte accounting'
    Assert-Contains $webSource 'renderer3DSubmissionCount*512+renderer3DPaletteSnapshotCount*8208' `
        'Web snapshot byte accounting'
    Assert-Contains ([regex]::Replace($nativeSource, '\s+', ' ')) 'smile_shadow_bytes3d + smile_scene_bytes3d + smile_bloom_bytes3d' `
        'Native target byte accounting'
    Assert-Contains $webSource 'renderer3DShadowBytes+renderer3DSceneBytes+renderer3DBloomBytes' `
        'Web target byte accounting'

    & $baseGate -Configuration $Configuration
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D M5 base gate failed from the hardening gate.' }

    Write-Host 'Renderer3D M5.1 native/Web snapshot, group, ownership, shadow, target, color, and hot-path hardening tests passed.'
}
finally {
    Pop-Location
}
