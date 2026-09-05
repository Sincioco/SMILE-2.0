[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$compiler = Join-Path $repositoryRoot 'artifacts\compiler\smilec.exe'
$project = Join-Path $repositoryRoot 'tools\Character3DViewer\ActorIsolationTests.smileproj'
$expected = Join-Path $repositoryRoot 'tools\Character3DViewer\ActorIsolationTests.expected.txt'
$nativeOutput = Join-Path $repositoryRoot 'artifacts\tests\Character3DViewerActorIsolationTests.exe'
$nativeLog = Join-Path $repositoryRoot 'artifacts\temp\Character3DViewerActorIsolationTests.out'
$fallbackLog = Join-Path $repositoryRoot 'artifacts\temp\Character3DViewerActorIsolationFallbackTests.out'
$webOutput = Join-Path $repositoryRoot 'artifacts\web\Character3DViewerActorIsolationTests'

function Assert-ExactOutput([string]$ActualPath, [string]$ExpectedPath, [string]$Label) {
    $actualText = (Get-Content -LiteralPath $ActualPath -Raw).Trim()
    $expectedText = (Get-Content -LiteralPath $ExpectedPath -Raw).Trim()
    if ($actualText -cne $expectedText) { throw "$Label failed: $actualText" }
}

if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) {
    throw 'Build SMILE before running the Character Viewer actor-isolation gate.'
}

Push-Location $repositoryRoot
try {
    & 'tools\Character3DViewer\Prepare-BuildAssets.ps1'
    if (-not $?) { throw 'Character Viewer asset preparation failed.' }

    & $compiler --project $project --target windows-x64 --configuration $Configuration `
        --graphics DirectX -o $nativeOutput
    if ($LASTEXITCODE -ne 0) { throw 'Two-Orin native fixture compilation failed.' }

    & 'scripts\run-bounded-test.cmd' 60 $nativeOutput |
        Set-Content -LiteralPath $nativeLog -Encoding utf8
    if ($LASTEXITCODE -ne 0) { throw 'Two-Orin native fixture execution failed.' }
    Assert-ExactOutput $nativeLog $expected 'Two-Orin native assertions'

    try {
        $env:SMILE_TEST_RENDERER3D_FORCE_GPU_PARTICLE_SHADER_FAILURE = '1'
        & 'scripts\run-bounded-test.cmd' 60 $nativeOutput |
            Set-Content -LiteralPath $fallbackLog -Encoding utf8
        if ($LASTEXITCODE -ne 0) { throw 'Two-Orin native fallback execution failed.' }
        Assert-ExactOutput $fallbackLog $expected 'Two-Orin native fallback assertions'
    }
    finally {
        Remove-Item Env:SMILE_TEST_RENDERER3D_FORCE_GPU_PARTICLE_SHADER_FAILURE `
            -ErrorAction SilentlyContinue
    }

    & $compiler --project $project --target web --configuration $Configuration `
        --output-dir $webOutput
    if ($LASTEXITCODE -ne 0) { throw 'Two-Orin Web fixture compilation failed.' }
    & node --check (Join-Path $webOutput 'game.js')
    if ($LASTEXITCODE -ne 0) { throw 'Two-Orin Web program syntax check failed.' }
    & node --check (Join-Path $webOutput 'smile-runtime.js')
    if ($LASTEXITCODE -ne 0) { throw 'Two-Orin Web runtime syntax check failed.' }
    & node 'scripts\run-web-test.js' $webOutput --expected $expected --timeout 60000 `
        --renderer3d
    if ($LASTEXITCODE -ne 0) { throw 'Two-Orin Web renderer assertions failed.' }
    & node 'scripts\run-web-test.js' $webOutput --expected $expected --timeout 60000 `
        --renderer3d --force-renderer3d-gpu-particle-shader-failure
    if ($LASTEXITCODE -ne 0) { throw 'Two-Orin Web fallback assertions failed.' }

    Write-Host 'Character Viewer two-Orin native/Web ownership, fallback, and isolation checks passed.'
}
finally {
    Pop-Location
}
