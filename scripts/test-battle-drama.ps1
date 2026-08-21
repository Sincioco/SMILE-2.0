[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$RepositoryRoot = Split-Path -Parent $PSScriptRoot
$Compiler = Join-Path $RepositoryRoot 'artifacts\compiler\smilec.exe'
$Project = Join-Path $RepositoryRoot 'examples\BattleDramaTests\BattleDramaTests.smileproj'
$Expected = Join-Path $RepositoryRoot 'examples\BattleDramaTests\expected.txt'
$Native = Join-Path $RepositoryRoot 'artifacts\tests\BattleDramaTests.exe'
$NativeOutput = Join-Path $RepositoryRoot 'artifacts\temp\BattleDramaTests.out'
$Web = Join-Path $RepositoryRoot 'artifacts\web\BattleDramaTests'

& $Compiler --project $Project --configuration Release -o $Native
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& $Native | Set-Content -LiteralPath $NativeOutput -Encoding utf8
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$ExpectedLines = [IO.File]::ReadAllLines($Expected, [Text.Encoding]::UTF8)
$ActualLines = [IO.File]::ReadAllLines($NativeOutput, [Text.Encoding]::UTF8)
if ([string]::Join("`n", $ExpectedLines) -cne [string]::Join("`n", $ActualLines)) {
    throw 'Battle camera and VFX native output differs from expected output.'
}

& $Compiler --project $Project --target web --configuration Release --output-dir $Web
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& node (Join-Path $RepositoryRoot 'scripts\run-web-test.js') $Web --expected $Expected
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& (Join-Path $RepositoryRoot 'scripts\test-renderer3d-materials.ps1')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host 'Battle camera, VFX, additive-material native/Web validation passed.'
