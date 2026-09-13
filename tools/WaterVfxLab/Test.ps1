[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$waterRepository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$projectPath = Join-Path $PSScriptRoot 'WaterLabTests.generated.smileproj'
$projectText = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'WaterVfxLab.smileproj') -Raw
$projectText = $projectText.Replace('Program.smile', 'WaterLabTests.smile').Replace(
    '<ApplicationId>smile.tools.water-vfx-lab</ApplicationId>',
    '<ApplicationId>smile.tests.water-vfx-lab</ApplicationId>')
try {
    [IO.File]::WriteAllText($projectPath, $projectText)
    $output = Join-Path $PSScriptRoot 'bin\Tests\WaterLabTests.exe'
    & (Join-Path $waterRepository 'artifacts\compiler\smilec.exe') --project $projectPath `
        --target windows-x64 --configuration Release --graphics DirectX -o $output
    if ($LASTEXITCODE -ne 0) { throw 'Water Lab native fixture compilation failed.' }
    $result = & (Join-Path $waterRepository 'scripts\run-bounded-test.cmd') 30 $output
    if ($LASTEXITCODE -ne 0 -or (($result -join "`n").Trim() -ne 'Water Lab native failures: 0')) {
        throw "Water Lab native checks failed: $result"
    }
    Write-Host ($result -join "`n")
} finally {
    if (Test-Path -LiteralPath $projectPath) { Remove-Item -LiteralPath $projectPath }
}
