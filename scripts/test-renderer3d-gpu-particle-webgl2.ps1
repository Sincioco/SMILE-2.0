[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$compiler = Join-Path $repositoryRoot 'artifacts\compiler\smilec.exe'
$webPath = Join-Path $repositoryRoot 'src\Smile.Compiler\WebOutputWriter.cs'
$runnerPath = Join-Path $repositoryRoot 'scripts\run-web-test.js'
$testRoot = Join-Path $repositoryRoot 'examples\Renderer3DGpuParticles'
$gpuProject = Join-Path $testRoot 'Renderer3DGpuParticleWebTests.smileproj'
$fallbackProject = Join-Path $testRoot 'Renderer3DGpuParticleWebFallbackTests.smileproj'
$gpuExpected = Join-Path $testRoot 'web-expected.txt'
$fallbackExpected = Join-Path $testRoot 'web-fallback-expected.txt'
$gpuOutput = Join-Path $repositoryRoot 'artifacts\web\Renderer3DGpuParticleWebTests'
$fallbackOutput = Join-Path $repositoryRoot 'artifacts\web\Renderer3DGpuParticleWebFallbackTests'

if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'Build SMILE before running the Renderer3D WebGL2 GPU particle gate.'
}

function Assert-Contains([string]$Text, [string]$ExpectedText, [string]$Label) {
    if ($Text.IndexOf($ExpectedText, [System.StringComparison]::Ordinal) -lt 0) {
        throw "$Label is missing required text: $ExpectedText"
    }
}

function Assert-NotContains([string]$Text, [string]$RejectedText, [string]$Label) {
    if ($Text.IndexOf($RejectedText, [System.StringComparison]::Ordinal) -ge 0) {
        throw "$Label contains forbidden text: $RejectedText"
    }
}

Push-Location $repositoryRoot
try {
    $web = Get-Content -LiteralPath $webPath -Raw
    $runner = Get-Content -LiteralPath $runnerPath -Raw
    $hotPathStart = $web.IndexOf('function renderer3DGpuParticleGpuStep', [System.StringComparison]::Ordinal)
    $hotPathEnd = $web.IndexOf('function renderer3DGpuParticleStep', $hotPathStart + 1, [System.StringComparison]::Ordinal)

    if ($hotPathStart -lt 0 -or $hotPathEnd -le $hotPathStart) {
        throw 'WebGL2 GPU particle hot path was not found.'
    }

    $hotPath = $web.Substring($hotPathStart, $hotPathEnd - $hotPathStart)

    Assert-Contains $web 'layout(location=0) in vec4 statePositionAge;' 'WebGL2 state offset 0'
    Assert-Contains $web 'layout(location=4) in vec4 stateSeedFlagsGradientFrame;' 'WebGL2 state offset 64'
    Assert-Contains $web 'gl.vertexAttribPointer(attribute,4,gl.FLOAT,false,80,attribute*16)' `
        'WebGL2 80-byte simulation layout'
    Assert-Contains $web 'gl.transformFeedbackVaryings(simulationHandle,' `
        'WebGL2 transform-feedback link contract'
    Assert-Contains $web 'gl.enable(gl.RASTERIZER_DISCARD)' 'WebGL2 discard simulation pass'
    Assert-Contains $web 'gl.drawArrays(gl.POINTS,0,system.capacity)' 'WebGL2 capacity dispatch'
    Assert-Contains $web 'gl.bufferSubData(gl.ARRAY_BUFFER,slot*80,system.commandF,source,20)' `
        'WebGL2 changed-slot spawn upload'
    Assert-Contains $web 'renderer3DGpuParticleHandleContextLoss()' 'WebGL2 context-loss recovery'
    Assert-Contains $runner 'nextSeedFlagsGradientFrame' 'WebGL2 varying-order test'
    Assert-NotContains $hotPath 'new ' 'WebGL2 simulation hot path allocation'
    Assert-NotContains $hotPath '.map(' 'WebGL2 simulation hot path map allocation'
    Assert-NotContains $hotPath '.filter(' 'WebGL2 simulation hot path filter allocation'
    Assert-NotContains $hotPath '.reduce(' 'WebGL2 simulation hot path reduce allocation'
    Assert-NotContains $hotPath 'createBuffer' 'WebGL2 simulation hot path buffer creation'
    Assert-NotContains $hotPath 'createVertexArray' 'WebGL2 simulation hot path VAO creation'
    Assert-NotContains $web 'getBufferSubData(' 'WebGL2 runtime readback prohibition'
    Assert-NotContains $web 'readPixels(' 'WebGL2 runtime pixel-readback prohibition'

    & $compiler --project $gpuProject --target web --configuration $Configuration `
        --output-dir $gpuOutput
    if ($LASTEXITCODE -ne 0) { throw 'WebGL2 GPU particle test compilation failed.' }
    & node --check (Join-Path $gpuOutput 'game.js')
    if ($LASTEXITCODE -ne 0) { throw 'WebGL2 GPU particle game syntax check failed.' }
    & node --check (Join-Path $gpuOutput 'smile-runtime.js')
    if ($LASTEXITCODE -ne 0) { throw 'WebGL2 GPU particle runtime syntax check failed.' }
    & node 'scripts\run-web-test.js' $gpuOutput --expected $gpuExpected --timeout 60000 `
        --renderer3d-gpu-particles
    if ($LASTEXITCODE -ne 0) { throw 'WebGL2 GPU particle assertions failed.' }

    & $compiler --project $fallbackProject --target web --configuration $Configuration `
        --output-dir $fallbackOutput
    if ($LASTEXITCODE -ne 0) { throw 'WebGL2 GPU particle fallback test compilation failed.' }
    & node 'scripts\run-web-test.js' $fallbackOutput --expected $fallbackExpected --timeout 60000 `
        --renderer3d --force-renderer3d-gpu-particle-shader-failure
    if ($LASTEXITCODE -ne 0) { throw 'WebGL2 shader-failure fallback assertions failed.' }
    & node 'scripts\run-web-test.js' $fallbackOutput --expected $fallbackExpected --timeout 60000 `
        --renderer3d --force-renderer3d-gpu-particle-attribute-failure
    if ($LASTEXITCODE -ne 0) { throw 'WebGL2 attribute-failure fallback assertions failed.' }

    Write-Host 'Renderer3D M7E-E WebGL2 transform-feedback simulation, rendering, bounds, fallback, recovery, and no-readback tests passed.'
}
finally {
    Pop-Location
}
