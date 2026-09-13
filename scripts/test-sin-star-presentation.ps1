[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$gameRoot = Join-Path $root 'games\SinStarI'
$testRoot = Join-Path $root ('artifacts\tests\SinStarPresentation-' + [Guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $testRoot
[xml]$project = Get-Content -LiteralPath (Join-Path $gameRoot 'SinStarI.smileproj') -Raw
$project.SmileProject.PropertyGroup.ApplicationId = 'smile.tests.sin-star-presentation'
# Keep the brief native fixture beside the chat when the game has saved bounds.
function Get-StorageHash([string]$Value) {
    [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData(
        [Text.Encoding]::UTF8.GetBytes($Value))).ToLowerInvariant()
}
$placementKey = Get-StorageHash '__smile_internal_window_placement_v2'
$gameIdentity = Get-StorageHash 'smile.game.sin-star-i'
$testIdentity = Get-StorageHash $project.SmileProject.PropertyGroup.ApplicationId
$storageRoot = Join-Path $env:LOCALAPPDATA 'SMILE 2.0\Games'
$placementSource = Join-Path $storageRoot "$gameIdentity\Data\$placementKey.bin"
if (Test-Path -LiteralPath $placementSource) {
    $testData = Join-Path $storageRoot "$testIdentity\Data"
    $null = New-Item -ItemType Directory -Path $testData -Force
    Copy-Item -LiteralPath $placementSource -Destination (Join-Path $testData "$placementKey.bin")
}
foreach ($directory in @('Assets', 'Maps', 'TechnicalAssets')) {
    Copy-Item -LiteralPath (Join-Path $gameRoot $directory) -Destination $testRoot -Recurse
}
foreach ($entry in $project.SmileProject.ItemGroup.ChildNodes) {
    if ($entry.Name -eq 'SmileSource' -and $entry.Include -eq 'Program.smile') { continue }
    if ($entry.Name -eq 'Model3DAsset') {
        foreach ($attribute in @('Include', 'Descriptor')) {
            $relative = $entry.GetAttribute($attribute)
            $destination = Join-Path $testRoot $relative
            $null = New-Item -ItemType Directory -Force -Path (Split-Path $destination -Parent)
            Copy-Item -LiteralPath (Join-Path $gameRoot $relative) -Destination $destination
        }
    } elseif ($entry.Name -eq 'Asset') {
        continue
    } else {
        foreach ($attribute in @('Include', 'Descriptor')) {
            if ($entry.HasAttribute($attribute)) {
                $sourcePath = [IO.Path]::GetFullPath((Join-Path $gameRoot $entry.GetAttribute($attribute)))
                $entry.SetAttribute($attribute, [IO.Path]::GetRelativePath($testRoot, $sourcePath))
            }
        }
    }
}
Copy-Item -LiteralPath (Join-Path $gameRoot 'PresentationTests.smile') -Destination (Join-Path $testRoot 'Program.smile')
$workflowEntry = $project.SmileProject.ItemGroup.SmileSource | Where-Object { $_.Include.EndsWith('ViewerWorkflow.smile') }
$workflowText = Get-Content -LiteralPath (Join-Path $root 'tools\Character3DViewer\ViewerWorkflow.smile') -Raw
# Observe the existing private load-error state in the disposable fixture only.
# A partial actor load must not pass just because a later draw clears LastError.
$loadBoundary = 'Call Me.LoadViewer()'
if (-not $workflowText.Contains($loadBoundary)) { throw 'Viewer load boundary changed.' }
$diagnosticLoad = @'
Call Me.LoadViewer()
        If Me.Session.ViewerError Then
            Print "Presentation load failed; stage, actor and renderer codes:"
            Print Me.Session.FirstFailureStage
            Print Me.Session.FirstViewerError
            Print Me.Session.FirstRendererError
        End If
'@
$workflowText = $workflowText.Replace($loadBoundary, $diagnosticLoad)
[IO.File]::WriteAllText((Join-Path $testRoot 'ViewerWorkflow.smile'), $workflowText)
$workflowEntry.SetAttribute('Include', 'ViewerWorkflow.smile')
$projectPath = Join-Path $testRoot 'PresentationTests.smileproj'
$project.Save($projectPath)
$exe = Join-Path $testRoot 'PresentationTests.exe'
& (Join-Path $root 'artifacts\compiler\smilec.exe') --project $projectPath --target windows-x64 --graphics DirectX -o $exe
if ($LASTEXITCODE -ne 0) { throw 'Presentation regression compilation failed.' }
$actual = & (Join-Path $PSScriptRoot 'run-bounded-test.cmd') 60 $exe
if ($LASTEXITCODE -ne 0 -or ($actual -join "`n").Trim() -ne 'Sin Star I presentations passed.') {
    throw "Presentation regression failed: $actual"
}
Write-Host 'PASS: Seven character entries and both battle simulations create, draw and release their actual assets.'
