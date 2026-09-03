[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$compiler = Join-Path $repositoryRoot 'artifacts\compiler\smilec.exe'
$effectsPath = Join-Path $repositoryRoot 'libraries\Smile.Simple3D\Effects3D.smile'
$bladePath = Join-Path $repositoryRoot 'libraries\Smile.Simple3D\AetherBlade3D.smile'
$testProject = Join-Path $repositoryRoot 'examples\AetherBladeVfxTests\AetherBladeVfxTests.smileproj'
$labProject = Join-Path $repositoryRoot 'examples\AetherBladeVfxTests\AetherBladeVfxLab.smileproj'
$expected = Join-Path $repositoryRoot 'examples\AetherBladeVfxTests\expected.txt'
$nativeOutput = Join-Path $repositoryRoot 'artifacts\tests\AetherBladeVfxTests.exe'
$nativeLog = Join-Path $repositoryRoot 'artifacts\temp\AetherBladeVfxTests.out'
$webOutput = Join-Path $repositoryRoot 'artifacts\web\AetherBladeVfxTests'
$labNativeOutput = Join-Path $repositoryRoot 'artifacts\examples\AetherBladeVfxLab.exe'
$labWebOutput = Join-Path $repositoryRoot 'artifacts\web\AetherBladeVfxLab'
$generator = Join-Path $repositoryRoot 'scripts\generate-aetherblade-vfx-fixture.ps1'
$evidenceRoot = Join-Path $repositoryRoot 'docs\implementation\screenshots\m7e-0-vfx3-preflight'
$evidenceIndex = Join-Path $evidenceRoot 'screenshot-index.md'

if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) {
    throw 'Build SMILE before running the VFX Generation 3 preflight gate.'
}

function Assert-Contains([string]$Text, [string]$ExpectedText, [string]$Label) {
    if ($Text.IndexOf($ExpectedText, [System.StringComparison]::Ordinal) -lt 0) {
        throw "$Label is missing required text: $ExpectedText"
    }
}

Push-Location $repositoryRoot
try {
    & $generator -Check

    $effects = Get-Content -LiteralPath $effectsPath -Raw
    $blade = Get-Content -LiteralPath $bladePath -Raw

    foreach ($contract in @(
        'SIMULATION_CPU_DETERMINISTIC',
        'SIMULATION_GPU_FAST',
        'SIMULATION_AUTO',
        'SOFT_DEPTH_OFF',
        'DISTORTION_OFF',
        'GPU_BACKEND_OFF',
        'FLAME_SHADING_BASIC')) {
        Assert-Contains $effects $contract 'Effects3D M7E-0 policy surface'
    }

    Assert-Contains $blade 'LAYER_SAMPLE_COUNT = 24' 'AetherBlade bounded core layers'
    Assert-Contains $blade 'TRAIL_SAMPLE_COUNT = 16' 'AetherBlade bounded trail history'
    Assert-Contains $blade 'TRAIL_SAMPLE_MILLISECONDS = 12' 'AetherBlade fixed sampling'
    Assert-Contains $blade 'Loop Until TrailAccumulator < TRAIL_SAMPLE_MILLISECONDS Or CatchUp >= 4' `
        'AetherBlade bounded catch-up'
    Assert-Contains $blade 'Not Graphics3D.ParticleBatchHandleValid3D(ParticleBatch)' `
        'AetherBlade reset invalidation'

    & $compiler --project $testProject --target windows-x64 --configuration $Configuration `
        --graphics DirectX -o $nativeOutput
    if ($LASTEXITCODE -ne 0) { throw 'M7E-0 native test compilation failed.' }

    & 'scripts\run-bounded-test.cmd' 60 $nativeOutput |
        Set-Content -LiteralPath $nativeLog -Encoding utf8
    if ($LASTEXITCODE -ne 0) { throw 'M7E-0 native test execution failed.' }

    $expectedText = (Get-Content -LiteralPath $expected -Raw).Trim()
    $actualText = (Get-Content -LiteralPath $nativeLog -Raw).Trim()
    if ($actualText -cne $expectedText) {
        throw "M7E-0 native assertions failed: $actualText"
    }

    & $compiler --project $testProject --target web --configuration $Configuration --output-dir $webOutput
    if ($LASTEXITCODE -ne 0) { throw 'M7E-0 Web test compilation failed.' }
    & node --check (Join-Path $webOutput 'game.js')
    if ($LASTEXITCODE -ne 0) { throw 'M7E-0 Web game syntax check failed.' }
    & node --check (Join-Path $webOutput 'smile-runtime.js')
    if ($LASTEXITCODE -ne 0) { throw 'M7E-0 Web runtime syntax check failed.' }
    & node 'scripts\run-web-test.js' $webOutput --expected $expected --timeout 60000 --renderer3d
    if ($LASTEXITCODE -ne 0) { throw 'M7E-0 Web assertions failed.' }

    & $compiler --project $labProject --target windows-x64 --configuration $Configuration `
        --graphics DirectX -o $labNativeOutput
    if ($LASTEXITCODE -ne 0) { throw 'M7E-0 native lab compilation failed.' }
    & $compiler --project $labProject --target web --configuration $Configuration --output-dir $labWebOutput
    if ($LASTEXITCODE -ne 0) { throw 'M7E-0 Web lab compilation failed.' }
    & node --check (Join-Path $labWebOutput 'game.js')
    if ($LASTEXITCODE -ne 0) { throw 'M7E-0 Web lab game syntax check failed.' }
    & node --check (Join-Path $labWebOutput 'smile-runtime.js')
    if ($LASTEXITCODE -ne 0) { throw 'M7E-0 Web lab runtime syntax check failed.' }

    $indexText = Get-Content -LiteralPath $evidenceIndex -Raw
    try {
        Add-Type -AssemblyName System.Drawing.Common
    }
    catch {
        Add-Type -AssemblyName System.Drawing
    }
    foreach ($name in @(
        '01-energy-blade-idle-native.png',
        '02-energy-blade-swing-native.png',
        '03-energy-blade-idle-web.png',
        '04-energy-blade-swing-web.png',
        '05-cpu-fallback-vfx-lab.png',
        '06-capability-diagnostics.png',
        '07-iphone-contact-sheet.png')) {
        $path = Join-Path $evidenceRoot $name
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing M7E-0 evidence: $name" }
        $bytes = [System.IO.File]::ReadAllBytes($path)
        if ($bytes.Length -lt 1024 -or $bytes[0] -ne 0x89 -or $bytes[1] -ne 0x50 -or
            $bytes[2] -ne 0x4E -or $bytes[3] -ne 0x47) {
            throw "M7E-0 evidence is not a bounded true PNG: $name"
        }
        $image = [System.Drawing.Image]::FromFile($path)
        try {
            if ($image.Width -lt 320 -or $image.Height -lt 240) {
                throw "M7E-0 evidence dimensions are invalid: $name"
            }
        }
        finally {
            $image.Dispose()
        }
        $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
        Assert-Contains $indexText $name 'M7E-0 screenshot index'
        Assert-Contains $indexText $hash 'M7E-0 screenshot index'
    }

    Write-Host 'VFX Generation 3 M7E-0 native/Web policy, fallback, AetherBlade, and lifecycle tests passed.'
}
finally {
    Pop-Location
}
