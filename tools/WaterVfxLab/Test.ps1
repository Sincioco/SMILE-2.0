[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$waterRepository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$waterVs = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" `
    -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
if (-not $waterVs) { throw 'Visual C++ tools unavailable.' }
$waterSetup = Join-Path $waterVs 'VC\Auxiliary\Build\vcvars64.bat'
Push-Location $waterRepository
try {
    & cmd.exe /d /c "call `"$waterSetup`" >nul && cl /nologo /EHsc /O2 tools\WaterVfxLab\WaterSurfaceTests.cpp /Foartifacts\tests\WaterSurfaceTests.obj /Feartifacts\tests\WaterSurfaceTests.exe"
    if ($LASTEXITCODE -ne 0) { throw 'Water surface regression compilation failed.' }
    & artifacts\tests\WaterSurfaceTests.exe
    if ($LASTEXITCODE -ne 0) { throw 'Water surface regression failed.' }
} finally { Pop-Location }
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
