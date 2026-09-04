param(
    [switch]$SkipWeb
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$assetSource = Join-Path $repositoryRoot 'TechnicalAssets\Generation3\Lightning'
$testRoot = Join-Path $repositoryRoot 'examples\LightningVfxFoundationTests'
$assetTarget = Join-Path $testRoot 'Assets\Lightning'
$nativeOutput = Join-Path $repositoryRoot 'artifacts\tests\LightningVfxFoundationTests.exe'
$nativeLog = Join-Path $repositoryRoot 'artifacts\temp\LightningVfxFoundationTests.out'
$webOutput = Join-Path $repositoryRoot 'artifacts\web\LightningVfxFoundationTests'

New-Item -ItemType Directory -Force -Path $assetTarget | Out-Null
Copy-Item -LiteralPath (Join-Path $assetSource 'lightning-ribbon.png') -Destination $assetTarget -Force
Copy-Item -LiteralPath (Join-Path $assetSource 'lightning-spark.png') -Destination $assetTarget -Force

Push-Location $repositoryRoot

try {
    & artifacts\compiler\smilec.exe --project examples\LightningVfxFoundationTests\LightningVfxFoundationTests.smileproj `
        --target windows-x64 --configuration Debug --graphics DirectX -o $nativeOutput

    if ($LASTEXITCODE -ne 0) {
        throw 'LightningVfxFoundationTests native compilation failed.'
    }

    $testOutput = & scripts\run-bounded-test.cmd 60 $nativeOutput
    $testOutput | Set-Content -LiteralPath $nativeLog -Encoding utf8

    if ($LASTEXITCODE -ne 0 -or $testOutput -notcontains 'LightningVfx3D foundation tests passed') {
        throw "LightningVfxFoundationTests failed. See $nativeLog"
    }

    if (-not $SkipWeb) {
        & artifacts\compiler\smilec.exe --project examples\LightningVfxFoundationTests\LightningVfxFoundationTests.smileproj `
            --target web --configuration Debug --output-dir $webOutput

        if ($LASTEXITCODE -ne 0) {
            throw 'LightningVfxFoundationTests Web compilation failed.'
        }

        & node scripts\run-web-test.js $webOutput `
            --expected (Join-Path $testRoot 'expected.txt') --renderer3d --frames 20 --timeout 60000

        if ($LASTEXITCODE -ne 0) {
            throw 'LightningVfxFoundationTests Web execution failed.'
        }
    }

    Write-Host 'LightningVfx3D foundation tests passed.'
}
finally {
    Pop-Location
}
