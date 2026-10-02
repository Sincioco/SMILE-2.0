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
    if ($text) { Write-Host $text.Trim() }
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
    $renderData = Join-Path (& (Join-Path $PSScriptRoot 'get-smile-data-root.ps1')) "$renderHash\Data"
    $null = New-Item -ItemType Directory -Path $renderData -Force
    Copy-Item -LiteralPath (Join-Path $viewer 'Fixtures/DenseCanals.town') `
        -Destination (Join-Path $renderData "$canalHash.bin") -Force
    # Keep only the checked authored envelope, forcing a fresh native terrain build.
    $silverfall = [IO.File]::ReadAllBytes((Join-Path $root 'games/SinStarI/SourceAssets/Towns/Neris/StoryTownsV1/Towns/Silverfall Basin.town'))
    $silverfallLength = 44 + [BitConverter]::ToUInt32($silverfall, 8)
    if ($silverfallLength -gt $silverfall.Length) { throw 'Incomplete Silverfall fixture.' }
    $silverfallHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData(
        [Text.Encoding]::UTF8.GetBytes('TownRender.Silverfall'))).ToLowerInvariant()
    [IO.File]::WriteAllBytes((Join-Path $renderData "$silverfallHash.bin"), $silverfall[0..($silverfallLength - 1)])
    Invoke-Check (Join-Path $viewer 'TownRenderTests.smileproj') (Join-Path $output 'TownRenderTests.exe') 'PASS Town Editor Rendering'

    [xml]$project = Get-Content (Join-Path $viewer 'Character3DViewer.smileproj') -Raw
    $project.SmileProject.PropertyGroup.StartupFile = 'TownSessionTests.smile'
    $project.SmileProject.PropertyGroup.ApplicationId = 'smile.tests.town-session.run-' + [Guid]::NewGuid().ToString('N')
    # Use the native portable-file importer: modern towns contain appended prepared
    # records, which cannot be copied directly into a plain Save Data .bin envelope.
    $sessionSource = [IO.File]::ReadAllText((Join-Path $viewer 'TownSessionTests.smile'))
    $imports = [Collections.Generic.List[string]]::new()
    foreach ($fixture in @(
        @{ Path = $SavedTown; Key = 'TownEditor.PermanentNeris' },
        @{ Path = $AirportTown; Key = 'TownEditor.Town.Horizon Airport' },
        @{ Path = $LinkedTown; Key = 'TownEditor.Town.Neris Spaceport' }
    )) {
        if ($fixture.Path) {
            $path = (Resolve-Path -LiteralPath $fixture.Path).Path
            $imports.Add('Call ImportFixture("' + $fixture.Key + '", "' + $path.Replace('"', '""') + '")')
        }
    }
    $sessionSource = $sessionSource.Replace('Call Commands.Start(Controls)',
        ($imports -join "`n") + "`n`nCall Commands.Start(Controls)")
    $sessionSource += @'

Sub ImportFixture(Key As Text, Path As Text)

    Dim Job As Number
    Dim Status As Number
    Dim Deadline As Number

    Job = Data_BundleStart(False, Key, Path, "")
    Deadline = Timer() + 10000

    Do
        Status = Data_FileStatus(Job)
        Show Screen
    Loop Until Status <> 0 Or Timer() >= Deadline

    Call Check(Job > 0 And Status = 1, "Native Portable Fixture Import")

End Sub
'@
    $generatedSession = Join-Path $output 'TownSessionTests.smile'
    [IO.File]::WriteAllText($generatedSession, $sessionSource)
    $project.SmileProject.PropertyGroup.StartupFile = $generatedSession
    $project.SmileProject.PropertyGroup.RememberWindowPlacement = 'false'
    $workerNode = $project.SmileProject.PropertyGroup.NativeWorkerScript
    $workerElement = $project.SmileProject.PropertyGroup.SelectSingleNode('NativeWorkerScript')
    if ($null -ne $workerElement) { $null = $workerElement.ParentNode.RemoveChild($workerElement) }
    $entry = $project.SmileProject.ItemGroup.SmileSource | Where-Object StartupOnly -eq 'true'
    $entry.SetAttribute('Include', $generatedSession)
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
