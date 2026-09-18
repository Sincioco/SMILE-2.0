[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$earthRepository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$earthProject = Join-Path $PSScriptRoot 'EarthLabTests.generated.smileproj'
$earthText = (Get-Content -LiteralPath (Join-Path $PSScriptRoot 'EarthVfxLab.smileproj') -Raw).Replace('Program.smile','EarthLabTests.smile').Replace('smile.tools.earth-vfx-lab','smile.tests.earth-vfx-lab')
try {
    [IO.File]::WriteAllText($earthProject,$earthText)
    $earthOutput = Join-Path $PSScriptRoot 'bin\Tests\EarthLabTests.exe'
    & (Join-Path $earthRepository 'artifacts\compiler\smilec.exe') --project $earthProject --target windows-x64 --configuration Release --graphics DirectX -o $earthOutput
    if ($LASTEXITCODE -ne 0) { throw 'Earth Lab fixture compilation failed.' }
    $earthResult = & (Join-Path $earthRepository 'scripts\run-bounded-test.cmd') 30 $earthOutput
    if ($LASTEXITCODE -ne 0 -or (($earthResult -join "`n").Trim() -ne 'Earth Lab native failures: 0')) { throw "Earth Lab checks failed: $earthResult" }
    Write-Host ($earthResult -join "`n")
} finally {
    if (Test-Path -LiteralPath $earthProject) { Remove-Item -LiteralPath $earthProject }
}
