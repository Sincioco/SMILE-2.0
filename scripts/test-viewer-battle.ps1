[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$viewerRoot = Join-Path $root 'tools\Character3DViewer'
$compiler = Join-Path $root 'artifacts\compiler\smilec.exe'
$testRoot = Join-Path $viewerRoot 'bin\BattleTests'
$null = New-Item -ItemType Directory -Force -Path $testRoot

# Reuse the prepared native project inventory; no second asset ownership list.
[xml]$project = Get-Content -LiteralPath (Join-Path $viewerRoot 'Character3DViewer.smileproj') -Raw
$project.SmileProject.PropertyGroup.StartupFile = 'BattleSceneTests.smile'
$project.SmileProject.PropertyGroup.ApplicationId = 'smile.tests.viewer-battle'
$project.SmileProject.PropertyGroup.RememberWindowPlacement = 'false'
$entry = $project.SmileProject.ItemGroup.SmileSource | Where-Object { $_.StartupOnly -eq 'true' }
$entry.SetAttribute('Include', 'BattleSceneTests.smile')
$projectPath = Join-Path $viewerRoot 'Character3DViewer.BattleTests.smileproj'
$project.Save($projectPath)

$planningExe = Join-Path $testRoot 'BattlePlanningTests.exe'
& $compiler --project (Join-Path $viewerRoot 'BattlePlanningTests.smileproj') --target windows-x64 -o $planningExe
if ($LASTEXITCODE -ne 0) { throw 'Battle planning compilation failed.' }
$planning = & $planningExe | Out-String
Write-Host $planning.Trim()
if ($planning -notmatch 'PASS Viewer Battle Planning' -or $planning -match 'FAIL') {
    throw 'Battle planning regression failed.'
}

$sceneExe = Join-Path $testRoot 'BattleSceneTests.exe'
& $compiler --project $projectPath --target windows-x64 --graphics DirectX -o $sceneExe
if ($LASTEXITCODE -ne 0) { throw 'Battle scene compilation failed.' }
$scene = & $sceneExe | Out-String
Write-Host $scene.Trim()
if ($scene -notmatch 'PASS Viewer Battle Scene' -or $scene -match 'FAIL') {
    throw 'Battle scene regression failed.'
}
