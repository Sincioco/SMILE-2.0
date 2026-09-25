[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$viewer = Join-Path $root 'tools\Character3DViewer'
$compiler = Join-Path $root 'artifacts\compiler\smilec.exe'
$output = Join-Path $viewer 'bin\Release'
$logs = Join-Path $root 'artifacts\tests\neris-town'
$null = New-Item -ItemType Directory -Path $logs -Force
if (-not (Test-Path -LiteralPath (Join-Path $output 'Assets\Neris\Neris-00.sm3d'))) {
    throw 'Build Character3DViewer with -Target Native before running town acceptance.'
}

& $compiler --project (Join-Path $viewer 'NerisTownTests.smileproj') --target windows-x64 `
    -o (Join-Path $logs 'Routes.exe')
if ($LASTEXITCODE -ne 0) { throw 'Town route compilation failed.' }
$routes = & (Join-Path $logs 'Routes.exe') | Out-String
Write-Host $routes.Trim()
if ($routes -notmatch 'PASS Neris Town Routes' -or $routes -match 'FAIL') {
    throw 'Town route acceptance failed.'
}

# Reuse the existing native publication, avoiding another large asset mirror.
[xml]$project = Get-Content -LiteralPath (Join-Path $viewer 'Character3DViewer.smileproj') -Raw
$project.SmileProject.PropertyGroup.StartupFile = 'NerisTownSceneTests.smile'
$project.SmileProject.PropertyGroup.ApplicationId = 'smile.tests.neris-town.run-' + [Guid]::NewGuid().ToString('N')
$project.SmileProject.PropertyGroup.RememberWindowPlacement = 'false'
$entry = $project.SmileProject.ItemGroup.SmileSource | Where-Object StartupOnly -eq 'true'
$entry.SetAttribute('Include', 'NerisTownSceneTests.smile')
foreach ($item in @($project.SmileProject.ItemGroup.ChildNodes)) {
    if ($item.Name -in @('Model3DAsset', 'Asset')) { $null = $item.ParentNode.RemoveChild($item) }
}
$projectPath = Join-Path $viewer 'Character3DViewer.NerisTests.smileproj'
$project.Save($projectPath)
$executable = Join-Path $output 'NerisTownSceneTests.exe'
& $compiler --project $projectPath --target windows-x64 --graphics DirectX -o $executable
if ($LASTEXITCODE -ne 0) { throw 'Town scene compilation failed.' }
$stdout = Join-Path $logs 'scene.txt'
$stderr = Join-Path $logs 'scene-errors.txt'
$process = Start-Process -FilePath $executable -WorkingDirectory $output -WindowStyle Hidden `
    -RedirectStandardOutput $stdout -RedirectStandardError $stderr -PassThru
if (-not $process.WaitForExit(60000)) { throw 'Town scene acceptance exceeded 60 seconds.' }
$scene = Get-Content -LiteralPath $stdout -Raw
Write-Host $scene.Trim()
if ($process.ExitCode -ne 0 -or $scene -notmatch 'PASS Neris Town Scene' -or $scene -match 'FAIL') {
    throw 'Town scene acceptance failed. See artifacts/tests/neris-town.'
}
$snapshot = Get-Content -LiteralPath (Join-Path $root `
    'games\SinStarI\SourceAssets\Characters\Paladin\ArinV57\Calibration\arin-v5.7-pose-calibration.json') -Raw | ConvertFrom-Json
$keys = @($snapshot.clips | Where-Object index -ge 0 | ForEach-Object keyframes).Count
if ($scene -notmatch "Arin Pose Keys $keys(?:\r?\n|$)") { throw 'Town did not load the accepted Arin pose key count.' }
Write-Host 'PASS Neris native route, scene, calibration and resource acceptance.'
