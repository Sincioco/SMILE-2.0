[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$RepositoryRoot = Split-Path -Parent $PSScriptRoot
$Compiler = Join-Path $RepositoryRoot 'artifacts\compiler\smilec.exe'
$Project = Join-Path $RepositoryRoot 'examples\Battle3DTests\Battle3DTests.smileproj'
$Expected = Join-Path $RepositoryRoot 'examples\Battle3DTests\expected.txt'
$Native = Join-Path $RepositoryRoot 'artifacts\tests\Battle3DTests.exe'
$NativeOutput = Join-Path $RepositoryRoot 'artifacts\temp\Battle3DTests.out'
$Web = Join-Path $RepositoryRoot 'artifacts\web\Battle3DTests'

& $Compiler --project $Project --configuration Release -o $Native
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& $Native | Set-Content -LiteralPath $NativeOutput -Encoding utf8
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$ExpectedLines = [IO.File]::ReadAllLines($Expected, [Text.Encoding]::UTF8)
$ActualLines = [IO.File]::ReadAllLines($NativeOutput, [Text.Encoding]::UTF8)
if ([string]::Join("`n", $ExpectedLines) -cne [string]::Join("`n", $ActualLines)) {
    throw 'Battle3D native output differs from expected output.'
}

& $Compiler --project $Project --target web --configuration Release --output-dir $Web
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& node (Join-Path $RepositoryRoot 'scripts\run-web-test.js') $Web --expected $Expected
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host 'Battle3D native/Web validation passed.'
