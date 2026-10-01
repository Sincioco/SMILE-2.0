[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$viewer = Join-Path $root 'tools/Character3DViewer'
$folder = Join-Path $root ('artifacts/tests/terrain-acceptance-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
$null = New-Item -ItemType Directory -Path $folder
$output = Join-Path $folder 'Terrain-and-Flowing-Water-MVP.town'
$source = [IO.File]::ReadAllText((Join-Path $viewer 'Fixtures/TerrainAcceptance.smile'))
$generated = Join-Path $folder 'TerrainAcceptance.smile'
[IO.File]::WriteAllText($generated, $source.Replace('__TERRAIN_OUTPUT__', $output))
[xml]$project = Get-Content -LiteralPath (Join-Path $viewer 'TownEditorTests.smileproj') -Raw
$project.SmileProject.PropertyGroup.StartupFile = $generated
$project.SmileProject.PropertyGroup.ApplicationId = 'smile.tests.terrain-acceptance'
foreach ($node in @($project.SmileProject.ItemGroup.ChildNodes)) {
    if ($node.LocalName -eq 'SmileSource' -and $node.GetAttribute('StartupOnly') -eq 'true') {
        $node.SetAttribute('Include', $generated)
    } elseif ($node.HasAttribute('Include')) {
        $node.SetAttribute('Include', [IO.Path]::GetFullPath((Join-Path $viewer $node.GetAttribute('Include'))))
    }
}
$projectPath = Join-Path $folder 'TerrainAcceptance.smileproj'
$project.Save($projectPath)
$exe = Join-Path $folder 'TerrainAcceptance.exe'
& (Join-Path $root 'artifacts/compiler/smilec.exe') --project $projectPath --target windows-x64 -o $exe
if ($LASTEXITCODE -ne 0) { throw 'Terrain acceptance generator did not compile.' }
$process = Start-Process -FilePath $exe -WorkingDirectory $folder -WindowStyle Hidden -PassThru `
    -RedirectStandardOutput (Join-Path $folder 'generate.log') -RedirectStandardError (Join-Path $folder 'generate.errors.log')
if (-not $process.WaitForExit(45000)) { throw 'Terrain acceptance generator exceeded 45 seconds.' }
$log = Get-Content -LiteralPath (Join-Path $folder 'generate.log') -Raw
Write-Host $log
if ($process.ExitCode -ne 0 -or $log -match 'FAIL' -or -not (Test-Path -LiteralPath $output)) {
    throw 'Terrain acceptance generation failed.'
}
Write-Host $output
