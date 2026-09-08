[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$smileRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location $smileRoot
try {
    $compiler = Join-Path $smileRoot 'artifacts/compiler/smilec.exe'
    $project = 'examples/Precision3DTests/Precision3DTests.smileproj'
    $native = 'artifacts/tests/Precision3DTests.exe'
    $web = 'artifacts/web/Precision3DTests'
    $expected = 'examples/Precision3DTests/expected.txt'
    & $compiler --project $project --target windows-x64 --graphics DirectX -o $native
    if ($LASTEXITCODE -ne 0) { throw 'Precision3D native compilation failed.' }
    $output = & scripts/run-bounded-test.cmd 30 $native
    if ($LASTEXITCODE -ne 0 -or ($output -join "`n").Trim() -cne (Get-Content $expected -Raw).Trim()) {
        throw "Precision3D native assertions failed: $output"
    }
    & $compiler --project $project --target web --output-dir $web
    if ($LASTEXITCODE -ne 0) { throw 'Precision3D Web compilation failed.' }
    & node scripts/run-web-test.js $web --expected $expected --renderer3d
    if ($LASTEXITCODE -ne 0) { throw 'Precision3D Web assertions failed.' }
    Write-Output 'Precision3D native/Web camera, transform, capture, reflection and curve checks passed.'
}
finally {
    Pop-Location
}
