[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$compiler = Join-Path $repositoryRoot 'artifacts\compiler\smilec.exe'
$assetTool = Join-Path $repositoryRoot 'artifacts\assettool\smileasset.exe'
$testRoot = Join-Path $repositoryRoot 'examples\Renderer3DPbrTests'
$testProject = Join-Path $testRoot 'Renderer3DPbrTests.smileproj'
$labProject = Join-Path $repositoryRoot 'examples\Renderer3DPbrLab\Renderer3DPbrLab.smileproj'
$expected = Join-Path $testRoot 'expected.txt'
$nativeOutput = Join-Path $repositoryRoot 'artifacts\tests\Renderer3DPbrTests.exe'
$nativeLog = Join-Path $repositoryRoot 'artifacts\temp\Renderer3DPbrTests.out'
$webOutput = Join-Path $repositoryRoot 'artifacts\web\Renderer3DPbrTests'
$labNativeOutput = Join-Path $repositoryRoot 'artifacts\examples\Renderer3DPbrLab.exe'
$labWebOutput = Join-Path $repositoryRoot 'artifacts\web\Renderer3DPbrLab'
$fixtureGenerator = Join-Path $repositoryRoot 'scripts\generate-renderer3d-pbr-fixtures.ps1'
$runtimeSource = Join-Path $repositoryRoot 'src\Smile.Compiler\WebOutputWriter.cs'

if (-not (Test-Path -LiteralPath $compiler) -or -not (Test-Path -LiteralPath $assetTool)) {
    throw 'Build SMILE before running the Renderer3D PBR gate.'
}

Push-Location $repositoryRoot
try {
    & $fixtureGenerator -Check
    & $assetTool inspect (Join-Path $testRoot 'Assets\PbrLab.sm3d') | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'The PBR Lab SM3D fixture failed inspection.' }

    Add-Type -AssemblyName System.Drawing
    $bitmap = [System.Drawing.Bitmap]::FromFile((Join-Path $testRoot 'Assets\Textures\Pbr-base-color.png'))

    try {
        $transparent = $bitmap.GetPixel(1, 0)
        if ($bitmap.Width -ne 4 -or $bitmap.Height -ne 4 -or $transparent.A -ne 0 -or
            ($transparent.R -eq 0 -and $transparent.G -eq 0 -and $transparent.B -eq 0)) {
            throw 'The PBR base-color fixture does not preserve nonzero straight RGB under alpha zero.'
        }
    }
    finally {
        $bitmap.Dispose()
    }

    $webSource = Get-Content -LiteralPath $runtimeSource -Raw
    $drawStart = $webSource.IndexOf('function renderer3DDrawPbr', [System.StringComparison]::Ordinal)
    $drawEnd = $webSource.IndexOf('function renderer3DBegin', $drawStart, [System.StringComparison]::Ordinal)

    if ($drawStart -lt 0 -or $drawEnd -le $drawStart) {
        throw 'The Renderer3D Web PBR draw path was not found.'
    }

    $drawSource = $webSource.Substring($drawStart, $drawEnd - $drawStart)
    if ($drawSource.Contains('new Float32Array') -or $drawSource.Contains('.map(') -or
        $drawSource.Contains('...') -or $drawSource.Contains('generateMipmap') -or
        $drawSource.Contains('renderer3DCompile')) {
        throw 'The Renderer3D Web PBR draw path contains per-draw allocation, mip generation, or shader compilation.'
    }

    & $compiler --project $testProject --target windows-x64 --configuration $Configuration `
        --graphics DirectX -o $nativeOutput
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D native PBR test compilation failed.' }
    & 'scripts\run-bounded-test.cmd' 60 $nativeOutput | Set-Content -LiteralPath $nativeLog -Encoding utf8
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D native PBR test execution failed.' }

    $expectedText = (Get-Content -LiteralPath $expected -Raw).Trim()
    $nativeText = (Get-Content -LiteralPath $nativeLog -Raw).Trim()
    if ($nativeText -cne $expectedText) {
        throw "Renderer3D native PBR assertions failed: $nativeText"
    }

    & $compiler --project $testProject --target web --configuration $Configuration --output-dir $webOutput
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D Web PBR test compilation failed.' }
    & node --check (Join-Path $webOutput 'game.js')
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D Web PBR game JavaScript syntax validation failed.' }
    & node --check (Join-Path $webOutput 'smile-runtime.js')
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D Web PBR runtime JavaScript syntax validation failed.' }
    & node 'scripts\run-web-test.js' $webOutput --expected $expected --timeout 60000 --renderer3d
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D Web PBR assertions failed.' }

    & $compiler --project $labProject --target windows-x64 --configuration $Configuration `
        --graphics DirectX -o $labNativeOutput
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D PBR Lab native compilation failed.' }
    & $compiler --project $labProject --target web --configuration $Configuration --output-dir $labWebOutput
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D PBR Lab Web compilation failed.' }
    & node --check (Join-Path $labWebOutput 'game.js')
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D PBR Lab Web JavaScript syntax validation failed.' }
    & node --check (Join-Path $labWebOutput 'smile-runtime.js')
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D PBR Lab Web runtime JavaScript syntax validation failed.' }

    Write-Host 'Renderer3D PBR native/Web parity, atomic ownership, lights, samplers, diagnostics, lifecycle, and PBR Lab build tests passed.'
}
finally {
    Pop-Location
}
