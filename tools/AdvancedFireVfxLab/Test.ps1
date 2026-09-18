[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$fireRepository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$projectPath = Join-Path $PSScriptRoot 'FireLabTests.generated.smileproj'
$projectText = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'AdvancedFireVfxLab.smileproj') -Raw
$projectText = $projectText.Replace('Program.smile', 'FireLabTests.smile').Replace(
    '<ApplicationId>smile.examples.advanced-fire-vfx-lab</ApplicationId>',
    '<ApplicationId>smile.tests.kael-fire-lab</ApplicationId>')
try {
    [IO.File]::WriteAllText($projectPath, $projectText)
    $output = Join-Path $PSScriptRoot 'bin\Tests\FireLabTests.exe'
    & (Join-Path $fireRepository 'artifacts\compiler\smilec.exe') --project $projectPath `
        --target windows-x64 --configuration Release --graphics DirectX -o $output
    if ($LASTEXITCODE -ne 0) { throw 'Fire Lab native fixture compilation failed.' }
    $result = & (Join-Path $fireRepository 'scripts\run-bounded-test.cmd') 30 $output
    if ($LASTEXITCODE -ne 0 -or (($result -join "`n").Trim() -ne 'Kael Fire Lab native failures: 0')) {
        throw "Fire Lab native checks failed: $result"
    }
    Write-Host ($result -join "`n")
} finally {
    if (Test-Path -LiteralPath $projectPath) { Remove-Item -LiteralPath $projectPath }
}
