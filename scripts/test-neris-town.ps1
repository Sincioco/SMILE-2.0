[CmdletBinding()]
param([string]$PublicationDirectory)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$viewer = Join-Path $root 'tools/Character3DViewer'
$compiler = Join-Path $root 'artifacts/compiler/smilec.exe'
$publication = Join-Path $viewer 'bin/Release'
if ($PublicationDirectory) { $publication = [IO.Path]::GetFullPath($PublicationDirectory) }
$logs = Join-Path $root ('artifacts/tests/neris-town-' + [Guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $logs
$check = Join-Path $PSScriptRoot 'Invoke-TownNativeCheck.ps1'
& (Join-Path $viewer 'Check-Publication.ps1') -Directory $publication

# Retain the bounded legacy/static route contract separately from authored scene setup.
[xml]$routes = Get-Content (Join-Path $viewer 'NerisTownTests.smileproj') -Raw
$identity = $routes.CreateElement('ApplicationId')
$null = $routes.SmileProject.PropertyGroup.AppendChild($identity)
$routes.SmileProject.PropertyGroup.ApplicationId = 'smile.tests.neris-routes.run-' + [Guid]::NewGuid().ToString('N')
$routes.SmileProject.PropertyGroup.StartupFile = Join-Path $viewer 'NerisTownTests.smile'
foreach ($node in $routes.SmileProject.ItemGroup.ChildNodes) {
    if ($node.HasAttribute('Include')) {
        $node.SetAttribute('Include', [IO.Path]::GetFullPath((Join-Path $viewer $node.GetAttribute('Include'))))
    }
}
$routeProject = Join-Path $logs 'Routes.smileproj'
$sceneProject = Join-Path $logs 'Scene.smileproj'
$routes.Save($routeProject)
try {
    $routeExe = Join-Path $logs 'Routes.exe'
    & $compiler --project $routeProject --target windows-x64 -o $routeExe *> "$routeExe.compile.log"
    if ($LASTEXITCODE -ne 0) { throw "Town route compilation failed: $routeExe.compile.log" }
    $text = & $check -Executable $routeExe -Expected 'PASS Neris Town Routes' -LogPrefix "$logs/routes" -TimeoutSeconds 20
    Write-Host $text.Trim()

    # Reuse the verified publication, without publishing into the running Studio folder.
    [xml]$project = Get-Content (Join-Path $viewer 'Character3DViewer.smileproj') -Raw
    $manifest = Join-Path $publication ($project.SmileProject.PropertyGroup.ApplicationId + '.smile-assets.json')
    foreach ($asset in (Get-Content $manifest -Raw | ConvertFrom-Json).assets) {
        $target = Join-Path $logs $asset
        $null = New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force
        Copy-Item -LiteralPath (Join-Path $publication $asset) -Destination $target
    }
    $source = Join-Path $logs 'Scene.smile'
    $town = Join-Path $root 'games/SinStarI/SourceAssets/Towns/Neris/NerisHorizonV1/Town/r009/Neris-Town-r009.town'
    [IO.File]::WriteAllText($source, [IO.File]::ReadAllText((Join-Path $viewer 'NerisTownSceneTests.smile')).Replace('@NERIS_TOWN@', $town))
    $project.SmileProject.PropertyGroup.StartupFile = $source
    $project.SmileProject.PropertyGroup.ApplicationId = 'smile.tests.neris-town.run-' + [Guid]::NewGuid().ToString('N')
    $project.SmileProject.PropertyGroup.RememberWindowPlacement = 'false'
    foreach ($node in @($project.SelectNodes('//NativeWorkerScript | //Model3DAsset | //Asset'))) {
        $null = $node.ParentNode.RemoveChild($node)
    }
    foreach ($node in $project.SmileProject.ItemGroup.ChildNodes) {
        if ($node.HasAttribute('Include')) {
            $path = [IO.Path]::GetFullPath((Join-Path $viewer $node.GetAttribute('Include')))
            if ($node.GetAttribute('StartupOnly') -eq 'true') { $path = $source }
            $node.SetAttribute('Include', $path)
        }
    }
    $inspection = $project.CreateElement('SmileSource')
    $inspection.SetAttribute('Include', (Join-Path $viewer 'NerisTownInspectionTests.smile'))
    $null = $project.SmileProject.ItemGroup.AppendChild($inspection)
    $project.Save($sceneProject)
    $executable = Join-Path $logs 'Scene.exe'
    & $compiler --project $sceneProject --target windows-x64 --graphics DirectX -o $executable *> "$executable.compile.log"
    if ($LASTEXITCODE -ne 0) { Get-Content "$executable.compile.log" -Tail 15; throw 'Town scene compilation failed.' }
    $scene = & $check -Executable $executable -Expected 'PASS Neris Town Scene' -LogPrefix "$logs/scene"
    Write-Host $scene.Trim()
    $snapshot = Get-Content (Join-Path $root 'games/SinStarI/SourceAssets/Characters/Paladin/ArinV57/Calibration/arin-v5.7-pose-calibration.json') -Raw | ConvertFrom-Json
    $keys = @($snapshot.clips | Where-Object index -ge 0 | ForEach-Object keyframes).Count
    if ($scene -notmatch "Arin Pose Keys $keys(?:\r?\n|$)") { throw 'Town did not load current accepted Arin calibration.' }
    Write-Host "PASS Neris native route, authored scene, calibration and resource acceptance. Evidence: $logs"
} finally {
    foreach ($path in @($routeProject, $sceneProject)) {
        Remove-Item -LiteralPath $path -ErrorAction SilentlyContinue
    }
}
