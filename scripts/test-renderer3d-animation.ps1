[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$compiler = Join-Path $repositoryRoot 'artifacts\compiler\smilec.exe'
$project = Join-Path $repositoryRoot 'examples\Renderer3DAnimationTests\Renderer3DAnimationTests.smileproj'
$expected = Join-Path $repositoryRoot 'examples\Renderer3DAnimationTests\expected.txt'
$nativeOutput = Join-Path $repositoryRoot 'artifacts\tests\Renderer3DAnimationTests.exe'
$nativeLog = Join-Path $repositoryRoot 'artifacts\temp\Renderer3DAnimationTests.out'
$webOutput = Join-Path $repositoryRoot 'artifacts\web\Renderer3DAnimationTests'

Push-Location $repositoryRoot
try {
    & $compiler --project $project --target windows-x64 --configuration $Configuration `
        --graphics DirectX -o $nativeOutput
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D native animation test compilation failed.' }
    & 'scripts\run-bounded-test.cmd' 30 $nativeOutput | Set-Content -LiteralPath $nativeLog -Encoding utf8
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D native animation test execution failed.' }
    $expectedText = (Get-Content -LiteralPath $expected -Raw).Trim()
    $nativeText = (Get-Content -LiteralPath $nativeLog -Raw).Trim()
    if ($nativeText -cne $expectedText) { throw "Renderer3D native animation assertions failed: $nativeText" }

    & $compiler --project $project --target web --configuration $Configuration --output-dir $webOutput
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D Web animation test compilation failed.' }
    & node 'scripts\run-web-test.js' $webOutput --expected $expected --timeout 30000 --renderer3d
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D Web animation assertions failed.' }

    Write-Host 'Renderer3D native/Web hierarchy, TRS interpolation, events, GPU skinning, and timing tests passed.'
}
finally {
    Pop-Location
}
