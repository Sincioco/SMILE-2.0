[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$compiler = Join-Path $repositoryRoot 'artifacts\compiler\smilec.exe'
$project = Join-Path $repositoryRoot 'examples\Renderer3DLifecycleTests\Renderer3DLifecycleTests.smileproj'
$expected = Join-Path $repositoryRoot 'examples\Renderer3DLifecycleTests\expected.txt'
$nativeOutput = Join-Path $repositoryRoot 'artifacts\tests\Renderer3DLifecycleTests.exe'
$nativeLog = Join-Path $repositoryRoot 'artifacts\temp\Renderer3DLifecycleTests.out'
$webOutput = Join-Path $repositoryRoot 'artifacts\web\Renderer3DLifecycleTests'

Push-Location $repositoryRoot
try {
    & $compiler --project $project --target windows-x64 --configuration $Configuration `
        --graphics GDI -o $nativeOutput

    if ($LASTEXITCODE -ne 0) {
        throw 'Renderer3D native lifecycle test compilation failed.'
    }

    & 'scripts\run-bounded-test.cmd' 30 $nativeOutput | Set-Content -LiteralPath $nativeLog -Encoding utf8

    if ($LASTEXITCODE -ne 0) {
        throw 'Renderer3D native lifecycle test execution failed.'
    }

    $expectedText = (Get-Content -LiteralPath $expected -Raw).Trim()
    $nativeText = (Get-Content -LiteralPath $nativeLog -Raw).Trim()

    if ($nativeText -cne $expectedText) {
        throw "Renderer3D native lifecycle assertions failed: $nativeText"
    }

    & $compiler --project $project --target web --configuration $Configuration --output-dir $webOutput

    if ($LASTEXITCODE -ne 0) {
        throw 'Renderer3D Web lifecycle test compilation failed.'
    }

    & node 'scripts\run-web-test.js' $webOutput --expected $expected --timeout 20000 --renderer3d

    if ($LASTEXITCODE -ne 0) {
        throw 'Renderer3D Web lifecycle assertions failed.'
    }

    Write-Host 'Renderer3D native/Web lifecycle, counters, exhaustion, restart, and frame-cycle tests passed.'
}
finally {
    Pop-Location
}
