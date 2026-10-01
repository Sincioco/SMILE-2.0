[CmdletBinding()]
param([switch]$SkipRendering, [string]$SavedTown, [string]$LinkedTown, [string]$AirportTown,
    [string]$PublicationDirectory)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$viewer = Join-Path $root 'tools\Character3DViewer'
& (Join-Path $viewer 'Prepare-SurfaceLayers.ps1') -Check
$compiler = Join-Path $root 'artifacts\compiler\smilec.exe'
$output = Join-Path $root 'artifacts\tests\town-editor'
$null = New-Item -ItemType Directory -Path $output -Force
# Camera/door/travel assertions target the accepted three-map layout, not the
# historical pre-split factory catalog with overlapping comparison landmarks.
$acceptedMaps = Join-Path $root 'games/SinStarI/SourceAssets/Towns/Neris/NerisHorizonV1/Town/r009'
if (-not $SavedTown) { $SavedTown = Join-Path $acceptedMaps 'Neris-Town-r009.town' }
if (-not $LinkedTown) { $LinkedTown = Join-Path $acceptedMaps 'Neris-Spaceport-r002.town' }
if (-not $AirportTown) { $AirportTown = Join-Path $acceptedMaps 'Horizon-Airport-r009.town' }

function Invoke-Check([string]$Project, [string]$Executable, [string]$Expected) {
    & $compiler --project $Project --target windows-x64 -o $Executable *> "$Executable.compile.log"
    if ($LASTEXITCODE -ne 0) { Get-Content "$Executable.compile.log" -Tail 20; throw 'Town fixture compile failed.' }
    $process = Start-Process -FilePath $Executable -WorkingDirectory (Split-Path $Executable -Parent) `
        -WindowStyle Hidden -RedirectStandardOutput "$Executable.log" -RedirectStandardError "$Executable.errors.log" -PassThru
    if (-not $process.WaitForExit(45000)) { throw "Town fixture did not finish: $Executable (PID $($process.Id))" }
    $text = [string](Get-Content "$Executable.log" -Raw)
    Write-Host $text.Trim()
    if ($process.ExitCode -ne 0 -or $text -match 'FAIL' -or $text -notmatch $Expected) { throw "Town fixture failed: $Executable (exit $($process.ExitCode))" }
}

Invoke-Check (Join-Path $viewer 'TownEditorTests.smileproj') (Join-Path $output 'Foundations.exe') 'PASS Town Editor Foundations'

# Exercise the existing demonstrated route regressions against the editable document.
# This generated fixture shares the same assertions; it is not a second route-test owner.
[xml]$project = Get-Content (Join-Path $viewer 'NerisTownTests.smileproj') -Raw
$source = [IO.File]::ReadAllText((Join-Path $viewer 'NerisTownTests.smile'))
$source = $source.Replace('Dim Path As Trail.State', @'
Import Smile.Tools.TownDocument As Document
Import Smile.Tools.TownCatalogData As Catalog
Import Smile.Tools.TownInitialSurface As Initial
Import Smile.Tools.TownDocumentNavigation As Edited

Dim Town As Document.State
Dim Path As Trail.State
'@)
$marker = 'Call Trail.Reset(Path, P.Vector(0.0, 0.0, 0.0), 5, 22.0)'
$source = $source.Replace($marker, @'
Call Initial.Populate(Town.Surface)

Town.ItemCount = Catalog.INITIAL_ITEMS

For Index = 0 To Town.ItemCount - 1
    Town.Items[Index] = Catalog.InitialItem(Index)
End For

Call Edited.Rebuild(Town)
'@ + "`n`n" + $marker)
$generatedSource = Join-Path $output 'EditedRoutes.smile'
[IO.File]::WriteAllText($generatedSource, $source)
$project.SmileProject.PropertyGroup.StartupFile = $generatedSource
foreach ($node in @($project.SmileProject.ItemGroup.ChildNodes)) {
    if ($node.LocalName -eq 'SmileSource' -and $node.GetAttribute('StartupOnly') -eq 'true') {
        $node.SetAttribute('Include', $generatedSource)
    } elseif ($node.HasAttribute('Include')) {
        $node.SetAttribute('Include', [IO.Path]::GetFullPath((Join-Path $viewer $node.GetAttribute('Include'))))
    }
}
$initial = $project.CreateElement('SmileSource')
$initial.SetAttribute('Include', (Join-Path $viewer 'TownInitialSurface.smile'))
$null = $project.SmileProject.ItemGroup.AppendChild($initial)
$generatedProject = Join-Path $output 'EditedRoutes.smileproj'
$project.Save($generatedProject)
Invoke-Check $generatedProject (Join-Path $output 'EditedRoutes.exe') 'PASS Neris Town Routes'

if (-not $SkipRendering) {
    # Reproduce the real canal document that exhausted the bounded terrain scratch buffer.
    $renderHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData(
        [Text.Encoding]::UTF8.GetBytes('smile.tests.town-render'))).ToLowerInvariant()
    $canalHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData(
        [Text.Encoding]::UTF8.GetBytes('TownRender.Canals'))).ToLowerInvariant()
    $renderData = Join-Path $env:LOCALAPPDATA "SMILE 2.0\Games\$renderHash\Data"
    $null = New-Item -ItemType Directory -Path $renderData -Force
    Copy-Item -LiteralPath (Join-Path $viewer 'Fixtures/DenseCanals.town') `
        -Destination (Join-Path $renderData "$canalHash.bin") -Force
    Invoke-Check (Join-Path $viewer 'TownRenderTests.smileproj') (Join-Path $output 'TownRenderTests.exe') 'PASS Town Editor Rendering'

    [xml]$project = Get-Content (Join-Path $viewer 'Character3DViewer.smileproj') -Raw
    $project.SmileProject.PropertyGroup.StartupFile = 'TownSessionTests.smile'
    $project.SmileProject.PropertyGroup.ApplicationId = 'smile.tests.town-session.run-' + [Guid]::NewGuid().ToString('N')
    if ($SavedTown) {
        $applicationHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData(
            [Text.Encoding]::UTF8.GetBytes($project.SmileProject.PropertyGroup.ApplicationId))).ToLowerInvariant()
        $keyHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData(
            [Text.Encoding]::UTF8.GetBytes('TownEditor.PermanentNeris'))).ToLowerInvariant()
        $fixtureData = Join-Path $env:LOCALAPPDATA "SMILE 2.0\Games\$applicationHash\Data"
        $null = New-Item -ItemType Directory -Path $fixtureData -Force
        Copy-Item -LiteralPath $SavedTown -Destination (Join-Path $fixtureData "$keyHash.bin")
        if ($AirportTown) {
            $airportHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData(
                [Text.Encoding]::UTF8.GetBytes('TownEditor.Town.Horizon Airport'))).ToLowerInvariant()
            Copy-Item -LiteralPath $AirportTown -Destination (Join-Path $fixtureData "$airportHash.bin")
        }
        if ($LinkedTown) {
            $linkedHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData(
                [Text.Encoding]::UTF8.GetBytes('TownEditor.Town.Neris Spaceport'))).ToLowerInvariant()
            Copy-Item -LiteralPath $LinkedTown -Destination (Join-Path $fixtureData "$linkedHash.bin")
        }
    }
    $project.SmileProject.PropertyGroup.RememberWindowPlacement = 'false'
    $workerNode = $project.SmileProject.PropertyGroup.NativeWorkerScript
    $workerElement = $project.SmileProject.PropertyGroup.SelectSingleNode('NativeWorkerScript')
    if ($null -ne $workerElement) { $null = $workerElement.ParentNode.RemoveChild($workerElement) }
    $entry = $project.SmileProject.ItemGroup.SmileSource | Where-Object StartupOnly -eq 'true'
    $entry.SetAttribute('Include', 'TownSessionTests.smile')
    $inspection = $project.CreateElement('SmileSource')
    $inspection.SetAttribute('Include', 'NerisTownInspectionTests.smile')
    $null = $project.SmileProject.ItemGroup.AppendChild($inspection)
    foreach ($item in @($project.SmileProject.ItemGroup.ChildNodes)) {
        if ($item.Name -in @('Model3DAsset', 'Asset')) { $null = $item.ParentNode.RemoveChild($item) }
    }
    $sessionProject = Join-Path $viewer 'Character3DViewer.TownSessionTests.smileproj'
    $project.Save($sessionProject)
    # A fixture must never publish into the user's running Studio folder, even with a
    # different ApplicationId. Reuse immutable prepared assets in isolated test output.
    $sessionOutput = Join-Path $output 'session'
    $null = New-Item -ItemType Directory -Path $sessionOutput -Force
    $publication = Join-Path $viewer 'bin/Release'
    if ($PublicationDirectory) { $publication = [IO.Path]::GetFullPath($PublicationDirectory) }
    & (Join-Path $viewer 'Check-Publication.ps1') -Directory $publication
    [xml]$assetProject = Get-Content (Join-Path $viewer 'Character3DViewer.smileproj') -Raw
    $manifestPath = Join-Path $publication ($assetProject.SmileProject.PropertyGroup.ApplicationId + '.smile-assets.json')
    foreach ($asset in (Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json).assets) {
        $target = Join-Path $sessionOutput $asset
        $null = New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force
        Copy-Item -LiteralPath (Join-Path $publication $asset) -Destination $target -Force
    }
    $images = Join-Path $sessionOutput 'Assets/Neris'
    $null = New-Item -ItemType Directory -Path $images -Force
    foreach ($name in @('Town-Palette.png', 'Neris-Grass-Color.png')) {
        Copy-Item -LiteralPath (Join-Path $viewer ('Assets/Neris/' + $name)) -Destination $images -Force
    }
    Invoke-Check $sessionProject (Join-Path $sessionOutput 'TownSessionTests.exe') 'PASS Town Editor Session'
}
Write-Host 'PASS Native Town Editor'
