[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$compiler = Join-Path $repositoryRoot 'artifacts\compiler\smilec.exe'
$graphicsPath = Join-Path $repositoryRoot 'libraries\Smile.Simple3D\Graphics3D.smile'
$headerPath = Join-Path $repositoryRoot 'src\Smile.NativeRuntime\graphics\graphics3d.h'
$nativePath = Join-Path $repositoryRoot 'src\Smile.NativeRuntime\graphics\graphics3d_directx.cpp'
$webPath = Join-Path $repositoryRoot 'src\Smile.Compiler\WebOutputWriter.cs'
$testProject = Join-Path $repositoryRoot 'examples\Renderer3DVfxLab\Renderer3DVfxTests.smileproj'
$labProject = Join-Path $repositoryRoot 'examples\Renderer3DVfxLab\Renderer3DVfxLab.smileproj'
$expected = Join-Path $repositoryRoot 'examples\Renderer3DVfxLab\expected.txt'
$nativeOutput = Join-Path $repositoryRoot 'artifacts\tests\Renderer3DVfxTests.exe'
$nativeLog = Join-Path $repositoryRoot 'artifacts\temp\Renderer3DVfxTests.out'
$webOutput = Join-Path $repositoryRoot 'artifacts\web\Renderer3DVfxTests'
$labNativeOutput = Join-Path $repositoryRoot 'artifacts\examples\Renderer3DVfxLab.exe'
$labWebOutput = Join-Path $repositoryRoot 'artifacts\web\Renderer3DVfxLab'
$generator = Join-Path $repositoryRoot 'scripts\generate-renderer3d-vfx-fixtures.ps1'

if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'Build SMILE before running the Renderer3D VFX batch gate.'
}

function Assert-Contains([string]$Text, [string]$ExpectedText, [string]$Label) {
    if ($Text.IndexOf($ExpectedText, [System.StringComparison]::Ordinal) -lt 0) {
        throw "$Label is missing required text: $ExpectedText"
    }
}

function Assert-ExactOutput() {
    $expectedText = (Get-Content -LiteralPath $expected -Raw).Trim()
    $actualText = (Get-Content -LiteralPath $nativeLog -Raw).Trim()
    if ($actualText -cne $expectedText) {
        throw "Renderer3D M6 native assertions failed: $actualText"
    }
}

Push-Location $repositoryRoot
try {
    & $generator -Check

    $graphics = Get-Content -LiteralPath $graphicsPath -Raw
    $header = Get-Content -LiteralPath $headerPath -Raw
    $native = Get-Content -LiteralPath $nativePath -Raw
    $web = Get-Content -LiteralPath $webPath -Raw

    Assert-Contains $header 'SMILE_3D_PARTICLE_BATCH = 119' 'Native numeric ABI'
    Assert-Contains $header 'SMILE_3D_RIBBON_BATCH = 120' 'Native numeric ABI'
    Assert-Contains $header 'SMILE_3D_M6_VALUE = 121' 'Native numeric ABI'
    Assert-Contains $graphics 'Private Const COMMAND_PARTICLE_BATCH = 119' 'SMILE numeric ABI'
    Assert-Contains $graphics 'Private Const COMMAND_RIBBON_BATCH = 120' 'SMILE numeric ABI'
    Assert-Contains $graphics 'Private Const COMMAND_M6_VALUE = 121' 'SMILE numeric ABI'
    Assert-Contains $web 'case 119:return renderer3DParticleBatchCommand(a,b,c,d,e,f,g,h,i);' `
        'Web numeric dispatch'
    Assert-Contains $web 'case 120:return renderer3DRibbonBatchCommand(a,b,c,d,e,f,g,h,i,j);' `
        'Web numeric dispatch'
    Assert-Contains $web 'case 121:return renderer3DM6Value(a,b);' 'Web numeric dispatch'

    foreach ($limit in @(
        'SMILE_3D_MAX_PARTICLE_BATCHES 32',
        'SMILE_3D_MAX_PARTICLES_PER_BATCH 4096',
        'SMILE_3D_MAX_STAGED_PARTICLES 8192',
        'SMILE_3D_MAX_RIBBON_BATCHES 16',
        'SMILE_3D_MAX_RIBBON_POINTS_PER_BATCH 8192',
        'SMILE_3D_MAX_STAGED_RIBBON_POINTS 32768')) {
        Assert-Contains $native $limit 'Native bounded VFX storage'
    }

    Assert-Contains $native 'DrawIndexedInstanced(6, batch->count, 0, 0, 0)' `
        'Native instanced particle path'
    Assert-Contains $native 'D3D11_MAP_WRITE_DISCARD' 'Native dynamic upload path'
    Assert-Contains $native 'context->Draw(batch->count * 2, 0)' 'Native ribbon path'
    Assert-Contains $web 'gl.drawElementsInstanced(gl.TRIANGLES,6,gl.UNSIGNED_SHORT,0,batch.count)' `
        'Web instanced particle path'
    Assert-Contains $web 'gl.drawArrays(gl.TRIANGLE_STRIP,0,batch.count*2)' 'Web ribbon path'
    Assert-Contains $native 'context->OMSetDepthStencilState(smile_depth_read_state3d, 0)' `
        'Native depth-read/no-write state'
    Assert-Contains $web 'gl.depthMask(false)' 'Web depth-read/no-write state'
    Assert-Contains $native 'SMILE_3D_SUBMISSION_PARTICLE_BATCH' 'Native tagged queue'
    Assert-Contains $native 'SMILE_3D_SUBMISSION_RIBBON_BATCH' 'Native tagged queue'
    Assert-Contains $web 'renderer3DSubmissionParticleBatch' 'Web tagged queue'
    Assert-Contains $web 'renderer3DSubmissionRibbonBatch' 'Web tagged queue'

    $webDrawStart = $web.IndexOf('function renderer3DDrawVfxImmediate(', [System.StringComparison]::Ordinal)
    $webDrawEnd = $web.IndexOf('function renderer3DDrawVfxBatch(', $webDrawStart, [System.StringComparison]::Ordinal)
    if ($webDrawStart -lt 0 -or $webDrawEnd -le $webDrawStart) {
        throw 'Web VFX draw path was not found.'
    }
    $webDraw = $web.Substring($webDrawStart, $webDrawEnd - $webDrawStart)
    foreach ($forbidden in @('new Array', 'new Float32Array', '.map(', '.filter(', '.reduce(', 'renderer3DCompile')) {
        if ($webDraw.IndexOf($forbidden, [System.StringComparison]::Ordinal) -ge 0) {
            throw "Web VFX draw path contains forbidden hot-path text: $forbidden"
        }
    }

    & $compiler --project $testProject --target windows-x64 --configuration $Configuration `
        --graphics DirectX -o $nativeOutput
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D M6 native test compilation failed.' }

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    & 'scripts\run-bounded-test.cmd' 60 $nativeOutput |
        Set-Content -LiteralPath $nativeLog -Encoding utf8
    $stopwatch.Stop()
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D M6 native test execution failed.' }
    Assert-ExactOutput

    & $compiler --project $testProject --target web --configuration $Configuration --output-dir $webOutput
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D M6 Web test compilation failed.' }
    & node --check (Join-Path $webOutput 'game.js')
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D M6 Web game syntax check failed.' }
    & node --check (Join-Path $webOutput 'smile-runtime.js')
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D M6 Web runtime syntax check failed.' }
    & node 'scripts\run-web-test.js' $webOutput --expected $expected --timeout 60000 --renderer3d
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D M6 Web assertions failed.' }

    & $compiler --project $labProject --target windows-x64 --configuration $Configuration `
        --graphics DirectX -o $labNativeOutput
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D VFX Lab native compilation failed.' }
    & $compiler --project $labProject --target web --configuration $Configuration --output-dir $labWebOutput
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D VFX Lab Web compilation failed.' }
    & node --check (Join-Path $labWebOutput 'game.js')
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D VFX Lab Web game syntax check failed.' }
    & node --check (Join-Path $labWebOutput 'smile-runtime.js')
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D VFX Lab Web runtime syntax check failed.' }

    Write-Host ("Renderer3D M6 native/Web batch, queue, lifecycle, 1,024-instance, " +
        "HDR/direct-LDR, and hot-path tests passed in {0} ms native runtime." -f $stopwatch.ElapsedMilliseconds)
}
finally {
    Pop-Location
}
