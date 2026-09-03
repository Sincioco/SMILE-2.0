[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$compiler = Join-Path $repositoryRoot 'artifacts\compiler\smilec.exe'
$corePath = Join-Path $repositoryRoot 'libraries\Smile.Simple3D\Core.smile'
$graphicsPath = Join-Path $repositoryRoot 'libraries\Smile.Simple3D\Graphics3D.smile'
$headerPath = Join-Path $repositoryRoot 'src\Smile.NativeRuntime\graphics\graphics3d.h'
$nativePath = Join-Path $repositoryRoot 'src\Smile.NativeRuntime\graphics\graphics3d_directx.cpp'
$webPath = Join-Path $repositoryRoot 'src\Smile.Compiler\WebOutputWriter.cs'
$testRoot = Join-Path $repositoryRoot 'examples\Renderer3DGpuParticles'
$testProject = Join-Path $testRoot 'Renderer3DGpuParticleTests.smileproj'
$expected = Join-Path $testRoot 'expected.txt'
$nativeOutput = Join-Path $repositoryRoot 'artifacts\tests\Renderer3DGpuParticleTests.exe'
$nativeLog = Join-Path $repositoryRoot 'artifacts\temp\Renderer3DGpuParticleTests.out'
$webOutput = Join-Path $repositoryRoot 'artifacts\web\Renderer3DGpuParticleTests'

if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'Build SMILE before running the Renderer3D GPU particle common gate.'
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

function Assert-ExactOutput([string]$ActualPath, [string]$ExpectedPath, [string]$Label) {
    $expectedText = (Get-Content -LiteralPath $ExpectedPath -Raw).Trim()
    $actualText = (Get-Content -LiteralPath $ActualPath -Raw).Trim()
    if ($actualText -cne $expectedText) {
        throw "$Label failed: $actualText"
    }
}

Push-Location $repositoryRoot
try {
    $core = Get-Content -LiteralPath $corePath -Raw
    $graphics = Get-Content -LiteralPath $graphicsPath -Raw
    $header = Get-Content -LiteralPath $headerPath -Raw
    $native = Get-Content -LiteralPath $nativePath -Raw
    $web = Get-Content -LiteralPath $webPath -Raw

    Assert-Contains $header 'SMILE_3D_GPU_PARTICLE_SYSTEM = 127' 'Native numeric ABI'
    Assert-Contains $graphics 'Private Const COMMAND_GPU_PARTICLE_SYSTEM = 127' 'SMILE numeric ABI'
    Assert-Contains $web 'case 127:return renderer3DGpuParticleCommand(a,b,c,d,e,f,g,h,i,j);' `
        'Web numeric ABI'
    Assert-Contains $core 'Public Type GpuParticleSystem3D' 'SMILE resource contract'
    Assert-Contains $native 'static_assert(sizeof(SmileGpuParticleState3D) == 80' `
        'Native 80-byte state schema'
    Assert-Contains $web 'new ArrayBuffer(capacity*80)' 'Web 80-byte state schema'
    Assert-Contains $native 'SMILE_3D_MAX_GPU_PARTICLE_SYSTEMS 8' 'Native system bound'
    Assert-Contains $native 'SMILE_3D_MAX_GPU_SPAWN_COMMANDS 512' 'Native spawn bound'
    Assert-Contains $web 'commandBuffer=new ArrayBuffer(512*80)' 'Web bounded spawn state'
    Assert-Contains $native 'system->read_index = (unsigned char)(1 - system->read_index)' `
        'Native ping-pong state transition'
    Assert-Contains $web 'system.readIndex=1-system.readIndex' 'Web ping-pong state transition'
    Assert-Contains $native 'elapsed_ms > 250 ? 250U' 'Native bounded time acceptance'
    Assert-Contains $web 'const accepted=Math.min(elapsed,250)' 'Web bounded time acceptance'
    Assert-NotContains $web 'getBufferSubData' 'Web GPU readback prohibition'

    & $compiler --project $testProject --target windows-x64 --configuration $Configuration `
        --graphics DirectX -o $nativeOutput
    if ($LASTEXITCODE -ne 0) { throw 'GPU particle native test compilation failed.' }
    & 'scripts\run-bounded-test.cmd' 60 $nativeOutput |
        Set-Content -LiteralPath $nativeLog -Encoding utf8
    if ($LASTEXITCODE -ne 0) { throw 'GPU particle native test execution failed.' }
    Assert-ExactOutput $nativeLog $expected 'GPU particle native assertions'

    & $compiler --project $testProject --target web --configuration $Configuration `
        --output-dir $webOutput
    if ($LASTEXITCODE -ne 0) { throw 'GPU particle Web test compilation failed.' }
    & node --check (Join-Path $webOutput 'game.js')
    if ($LASTEXITCODE -ne 0) { throw 'GPU particle Web game syntax check failed.' }
    & node --check (Join-Path $webOutput 'smile-runtime.js')
    if ($LASTEXITCODE -ne 0) { throw 'GPU particle Web runtime syntax check failed.' }
    & node 'scripts\run-web-test.js' $webOutput --expected $expected --timeout 60000 --renderer3d
    if ($LASTEXITCODE -ne 0) { throw 'GPU particle Web assertions failed.' }

    Write-Host 'Renderer3D M7E-C native/Web persistent-resource, fixed-slot, scheduler, lifecycle, no-readback, and legacy-particle tests passed.'
}
finally {
    Pop-Location
}
