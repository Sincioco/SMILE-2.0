[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$arenaRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$arenaProject = Join-Path $arenaRoot 'examples\Arena3DTests\Arena3DTests.smileproj'
$arenaOutput = Join-Path $arenaRoot 'artifacts\tests\Arena3DTests.exe'
$arenaLog = Join-Path $arenaRoot 'artifacts\tests\Arena3DTests.out'
$arenaError = Join-Path $arenaRoot 'artifacts\tests\Arena3DTests.err'
& (Join-Path $PSScriptRoot 'copy-arena-assets.ps1') -ProjectDirectory (Split-Path $arenaProject)
& (Join-Path $arenaRoot 'artifacts\compiler\smilec.exe') --project $arenaProject `
    --target windows-x64 --configuration Release --graphics DirectX -o $arenaOutput
if ($LASTEXITCODE -ne 0) { throw 'Shared arena native compilation failed.' }
$arenaProcess = Start-Process -FilePath $arenaOutput -WindowStyle Hidden -PassThru `
    -RedirectStandardOutput $arenaLog -RedirectStandardError $arenaError
if (-not $arenaProcess.WaitForExit(15000)) {
    $null = $arenaProcess.CloseMainWindow()
    throw 'The focused arena fixture did not finish within 15 seconds.'
}
$arenaResult = (Get-Content -LiteralPath $arenaLog -Raw).Trim()
if ($arenaProcess.ExitCode -ne 0 -or $arenaResult -notmatch '^Shared arena checks passed: \d+$') {
    throw "Shared arena validation failed: $arenaResult $(Get-Content -LiteralPath $arenaError -Raw)"
}
Write-Host $arenaResult
