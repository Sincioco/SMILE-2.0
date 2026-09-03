[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$compiler = Join-Path $repositoryRoot 'artifacts\compiler\smilec.exe'
$nativePath = Join-Path $repositoryRoot 'src\Smile.NativeRuntime\graphics\graphics3d_directx.cpp'
$testRoot = Join-Path $repositoryRoot 'examples\Renderer3DGpuParticles'
$testProject = Join-Path $testRoot 'Renderer3DGpuParticleD3DTests.smileproj'
$fallbackProject = Join-Path $testRoot 'Renderer3DGpuParticleD3DFallbackTests.smileproj'
$expected = Join-Path $testRoot 'd3d-expected.txt'
$fallbackExpected = Join-Path $testRoot 'd3d-fallback-expected.txt'
$nativeOutput = Join-Path $repositoryRoot 'artifacts\tests\Renderer3DGpuParticleD3DTests.exe'
$fallbackOutput = Join-Path $repositoryRoot 'artifacts\tests\Renderer3DGpuParticleD3DFallbackTests.exe'
$nativeLog = Join-Path $repositoryRoot 'artifacts\temp\Renderer3DGpuParticleD3DTests.out'
$fallbackLog = Join-Path $repositoryRoot 'artifacts\temp\Renderer3DGpuParticleD3DFallbackTests.out'

if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'Build SMILE before running the Renderer3D D3D11 GPU particle gate.'
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

function Assert-ExactOutput(
    [string]$ActualPath,
    [string]$ExpectedPath,
    [string]$Label
) {
    $expectedText = (Get-Content -LiteralPath $ExpectedPath -Raw).Trim()
    $actualText = (Get-Content -LiteralPath $ActualPath -Raw).Trim()
    if ($actualText -cne $expectedText) {
        throw "$Label failed: $actualText"
    }
}

function Invoke-Fallback(
    [string]$Variable,
    [string]$Label
) {
    try {
        Set-Item -Path "Env:$Variable" -Value '1'
        & 'scripts\run-bounded-test.cmd' 60 $fallbackOutput |
            Set-Content -LiteralPath $fallbackLog -Encoding utf8
        if ($LASTEXITCODE -ne 0) { throw "$Label execution failed." }
        Assert-ExactOutput $fallbackLog $fallbackExpected $Label
    }
    finally {
        Remove-Item -Path "Env:$Variable" -ErrorAction SilentlyContinue
    }
}

Push-Location $repositoryRoot
try {
    $native = Get-Content -LiteralPath $nativePath -Raw

    Assert-Contains $native 'D3D11_BIND_SHADER_RESOURCE | D3D11_BIND_UNORDERED_ACCESS' `
        'Structured GPU state buffers'
    Assert-Contains $native 'D3D11_RESOURCE_MISC_BUFFER_STRUCTURED' `
        'Structured GPU state layout'
    Assert-Contains $native '[numthreads(256,1,1)]' 'Bounded compute group size'
    Assert-Contains $native 'context->Dispatch((system->capacity + 255) / 256, 1, 1)' `
        'Capacity-derived compute dispatch'
    Assert-Contains $native 'SV_InstanceID' 'Direct GPU-state rendering'
    Assert-Contains $native 'context->DrawIndexedInstanced(6, system->capacity, 0, 0, 0)' `
        'Capacity-bounded GPU draw'
    Assert-Contains $native 'SMILE_TEST_RENDERER3D_FORCE_GPU_PARTICLE_SHADER_FAILURE' `
        'Shader failure fallback hook'
    Assert-Contains $native 'SMILE_TEST_RENDERER3D_FORCE_GPU_PARTICLE_BUFFER_FAILURE' `
        'Buffer failure fallback hook'
    Assert-Contains $native 'smile_gpu_particle_restart_count3d++' `
        'Device-loss restart accounting'
    Assert-NotContains $native 'GetData(' 'GPU state readback prohibition'
    Assert-NotContains $native 'CopyResource(system->gpu_states' `
        'GPU state readback prohibition'

    & $compiler --project $testProject --target windows-x64 --configuration $Configuration `
        --graphics DirectX -o $nativeOutput
    if ($LASTEXITCODE -ne 0) { throw 'D3D11 GPU particle test compilation failed.' }
    & 'scripts\run-bounded-test.cmd' 60 $nativeOutput |
        Set-Content -LiteralPath $nativeLog -Encoding utf8
    if ($LASTEXITCODE -ne 0) { throw 'D3D11 GPU particle test execution failed.' }
    Assert-ExactOutput $nativeLog $expected 'D3D11 GPU particle assertions'

    & $compiler --project $fallbackProject --target windows-x64 `
        --configuration $Configuration --graphics DirectX -o $fallbackOutput
    if ($LASTEXITCODE -ne 0) { throw 'D3D11 GPU particle fallback compilation failed.' }
    Invoke-Fallback 'SMILE_TEST_RENDERER3D_FORCE_GPU_PARTICLE_SHADER_FAILURE' `
        'D3D11 GPU particle shader fallback'
    Invoke-Fallback 'SMILE_TEST_RENDERER3D_FORCE_GPU_PARTICLE_BUFFER_FAILURE' `
        'D3D11 GPU particle buffer fallback'

    Write-Host 'Renderer3D M7E-D D3D11 compute, direct draw, fallback, coexistence, and no-readback tests passed.'
}
finally {
    Pop-Location
}
