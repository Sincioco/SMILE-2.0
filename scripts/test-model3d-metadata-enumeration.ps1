[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$language = Join-Path $repositoryRoot 'src\Smile.Language\Syntax.cs'
$nativeAbi = Join-Path $repositoryRoot 'src\Smile.NativeRuntime\graphics\graphics3d.h'
$nativeRuntime = Join-Path $repositoryRoot 'src\Smile.NativeRuntime\graphics\graphics3d_directx.cpp'
$webRuntime = Join-Path $repositoryRoot 'src\Smile.Compiler\WebOutputWriter.cs'
$graphics = Join-Path $repositoryRoot 'libraries\Smile.Simple3D\Graphics3D.smile'
$character = Join-Path $repositoryRoot 'libraries\Smile.Simple3D\Character3D.smile'
$animationGate = Join-Path $repositoryRoot 'scripts\test-renderer3d-animation-v2-hardening.ps1'

function Assert-Contains([string]$Text, [string]$Expected, [string]$Label) {
    if ($Text.IndexOf($Expected, [StringComparison]::Ordinal) -lt 0) {
        throw "$Label is missing required text: $Expected"
    }
}

Push-Location $repositoryRoot
try {
    $languageText = Get-Content -LiteralPath $language -Raw
    $nativeAbiText = Get-Content -LiteralPath $nativeAbi -Raw
    $nativeText = Get-Content -LiteralPath $nativeRuntime -Raw
    $webText = Get-Content -LiteralPath $webRuntime -Raw
    $graphicsText = Get-Content -LiteralPath $graphics -Raw
    $characterText = Get-Content -LiteralPath $character -Raw

    Assert-Contains $languageText 'Renderer3DTextValue' 'Shared language built-in'
    Assert-Contains $nativeAbiText 'SMILE_3D_SET_MODEL_ANIMATOR_TIME = 124' 'Native numeric ABI'
    Assert-Contains $nativeAbiText 'SMILE_3D_TEXT_MODEL_CLIP_NAME = 10' 'Native text ABI'
    Assert-Contains $nativeAbiText 'SMILE_3D_TEXT_MODEL_SOCKET_NAME = 11' 'Native text ABI'
    Assert-Contains $nativeAbiText 'SMILE_3D_TEXT_MODEL_EVENT_NAME = 12' 'Native text ABI'
    Assert-Contains $nativeText 'case SMILE_3D_SET_MODEL_ANIMATOR_TIME:' 'Native seek dispatch'
    Assert-Contains $nativeText 'command == SMILE_3D_TEXT_MODEL_CLIP_NAME' 'Native text dispatch'
    Assert-Contains $webText 'case 124:' 'Web seek dispatch'
    Assert-Contains $webText 'if(command===10&&b>=0&&b<animation.clips.length)' 'Web clip text dispatch'
    Assert-Contains $webText 'if(command===11&&b>=0&&b<animation.sockets.length)' 'Web socket text dispatch'
    Assert-Contains $webText 'if(command===12&&b>0&&b<=animation.events.length)' 'Web event text dispatch'
    Assert-Contains $graphicsText 'Public Function ModelClipName3D' 'Simple3D clip-name API'
    Assert-Contains $graphicsText 'Public Function ModelSocketName3D' 'Simple3D socket-name API'
    Assert-Contains $graphicsText 'Public Function ModelAnimationEventName3D' 'Simple3D event-name API'
    Assert-Contains $graphicsText 'Public Function SetModelAnimatorTime3D' 'Simple3D seek API'
    Assert-Contains $characterText 'Public Function SetAnimationTime' 'Character3D seek API'
    Assert-Contains $characterText 'Public Function ClipSampleCount' 'Character3D sample metadata'
    Assert-Contains $characterText 'Public Function SocketNodeIndex' 'Character3D socket metadata'
    Assert-Contains $characterText 'Public Function EventTime' 'Character3D event metadata'

    & $animationGate -Configuration $Configuration
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D animation metadata native/Web gate failed.' }

    Write-Host 'Model3D exact-name enumeration, numeric metadata, safe seek, stale-handle, and native/Web parity gate passed.'
}
finally {
    Pop-Location
}
