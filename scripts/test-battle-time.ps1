[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$RepositoryRoot = Split-Path -Parent $PSScriptRoot
$Compiler = Join-Path $RepositoryRoot 'artifacts\compiler\smilec.exe'
$Project = Join-Path $RepositoryRoot 'examples\BattleTimeTests\BattleTimeTests.smileproj'
$Expected = Join-Path $RepositoryRoot 'examples\BattleTimeTests\expected.txt'
$Native = Join-Path $RepositoryRoot 'artifacts\tests\BattleTimeTests.exe'
$NativeOutput = Join-Path $RepositoryRoot 'artifacts\temp\BattleTimeTests.out'
$Web = Join-Path $RepositoryRoot 'artifacts\web\BattleTimeTests'

& $Compiler --project $Project --configuration Release -o $Native
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& $Native | Set-Content -LiteralPath $NativeOutput -Encoding utf8
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$ExpectedLines = [IO.File]::ReadAllLines($Expected, [Text.Encoding]::UTF8)
$ActualLines = [IO.File]::ReadAllLines($NativeOutput, [Text.Encoding]::UTF8)
if ([string]::Join("`n", $ExpectedLines) -cne [string]::Join("`n", $ActualLines)) {
    throw 'BattleTime native output differs from expected output.'
}

& $Compiler --project $Project --target web --configuration Release --output-dir $Web
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& node (Join-Path $RepositoryRoot 'scripts\run-web-test.js') $Web --expected $Expected
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host 'BattleTime deterministic native/Web validation passed.'
