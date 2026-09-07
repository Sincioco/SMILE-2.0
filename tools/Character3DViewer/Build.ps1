[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [ValidateSet('Native', 'Web', 'All')]
    [string]$Target = 'All',
    [ValidateSet('Full', 'Low', 'Medium', 'High')]
    [string]$WebQuality = 'Full'
)

$ErrorActionPreference = 'Stop'
if ($WebQuality -ne 'Full' -and $Target -ne 'Web') {
    throw 'Optimized profiles require -Target Web; normal native/Web output is preserved.'
}
$toolRoot = $PSScriptRoot
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $toolRoot '..\..'))
$compiler = Join-Path $repositoryRoot 'artifacts\compiler\smilec.exe'
$project = Join-Path $toolRoot 'Character3DViewer.smileproj'
$outputRoot = Join-Path $toolRoot "bin\$Configuration"
$viewerSources = @(
    'Program.smile',
    'Profiles.smile',
    'OrinStorm.smile',
    'ArinShieldRim.smile',
    'BattleAudio.smile',
    'BattleCamera.smile',
    'DragonPresence.smile',
    'CalibrationJson.smile',
    'ViewerTiming.smile',
    'ViewerSession.smile',
    'ViewerPlayback.smile',
    'ViewerActors.smile',
    'ViewerCamera.smile',
    'ViewerCalibration.smile',
    'ViewerCalibrationEditing.smile',
    'ViewerCalibrationControls.smile',
    'ViewerInput.smile',
    'ViewerInspectorCommands.smile',
    'ViewerInspectorPresentation.smile',
    'ViewerTimelineEditing.smile',
    'ViewerUi.smile',
    'ViewerGizmo.smile',
    'ViewerParty.smile',
    'ViewerEffects.smile',
    'ViewerDragon.smile',
    'ViewerRendering.smile'
)

function Assert-ViewerSourceInventory(
    [xml]$ProjectXml,
    [string]$ProfileSource,
    [string]$Label
) {
    $expectedSources = @($viewerSources | ForEach-Object {
        if ($_ -eq 'Profiles.smile') { $ProfileSource } else { $_ }
    })
    $sourceNodes = @($ProjectXml.SmileProject.ItemGroup.SmileSource)

    if ([string]$ProjectXml.SmileProject.PropertyGroup.StartupFile -cne 'Program.smile') {
        throw "$Label must keep Program.smile as StartupFile."
    }
    if ([string]$ProjectXml.SmileProject.PropertyGroup.ApplicationId -cne
        'smile.tools.character3d-viewer') {
        throw "$Label changed the stable Character Viewer ApplicationId."
    }
    if ($sourceNodes.Count -ne $expectedSources.Count) {
        throw "$Label must contain exactly $($expectedSources.Count) explicit SmileSource items."
    }
    foreach ($source in $expectedSources) {
        $matches = @($sourceNodes | Where-Object { [string]$_.Include -ceq $source })
        if ($matches.Count -ne 1) {
            throw "$Label must include $source exactly once."
        }
        if (-not (Test-Path -LiteralPath (Join-Path $toolRoot $source) -PathType Leaf)) {
            throw "$Label source does not exist: $source"
        }
    }
    $programNode = @($sourceNodes | Where-Object {
        [string]$_.Include -ceq 'Program.smile'
    })
    if ([string]$programNode[0].StartupOnly -cne 'true') {
        throw "$Label must keep Program.smile StartupOnly."
    }
}

function Assert-WebModelInventory([xml]$ProjectXml) {
    $expectedModels = @(
        'Assets\Generation2\ArinV57\ArinV57.sm3d',
        'Assets\Generation2\OrinV13\OrinV13.sm3d',
        'Assets\Generation2\RedDragon\RedDragon.sm3d'
    )
    $modelNodes = @($ProjectXml.SmileProject.ItemGroup.Model3DAsset)

    if ($modelNodes.Count -ne $expectedModels.Count) {
        throw 'Web publication must contain exactly the current Arin, Orin and Dragon models.'
    }
    foreach ($model in $expectedModels) {
        $matches = @($modelNodes | Where-Object { [string]$_.LogicalPath -ceq $model })
        if ($matches.Count -ne 1) {
            throw "Web publication must include $model exactly once."
        }
    }
}

function Assert-CharacterPublication([string]$PublicationRoot) {
    $synchronizer = Join-Path $repositoryRoot 'scripts\sync-arin-v5-7-calibration.ps1'

    foreach ($characterName in @('Arin', 'Orin')) {
        & {
            param($CharacterName, $Root, $Synchronizer)
            . $Synchronizer -Character $CharacterName -FunctionsOnly
            Assert-PublishedProfileAsset $Root
        } $characterName $PublicationRoot $synchronizer
    }
}

if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) {
    throw "Build SMILE before compiling the Character Viewer/editor: $compiler"
}

[xml]$nativeProject = Get-Content -LiteralPath $project -Raw
Assert-ViewerSourceInventory $nativeProject 'Profiles.smile' 'Character Viewer project'

& (Join-Path $toolRoot 'Prepare-BuildAssets.ps1')
if ($Target -in @('Native', 'All')) {
    $output = Join-Path $outputRoot 'Character3DViewer.exe'
    New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null
    & $compiler --project $project --target windows-x64 `
        --configuration $Configuration --graphics DirectX -o $output
    if ($LASTEXITCODE -ne 0) {
        throw 'Character Viewer/editor native compilation failed.'
    }
    Assert-CharacterPublication $outputRoot
    Write-Host "Built Character Viewer/editor: $output"
}

if ($Target -in @('Web', 'All')) {
    $webFolder = if ($WebQuality -eq 'Full') { 'Web' } else { "Web - Optimized $WebQuality" }
    $webOutput = Join-Path $outputRoot $webFolder
    # Keep source ownership and native diagnostics intact. Generate only the
    # Web publication's profile policy and project asset list. Quality profiles
    # resize only unpublished staging copies, never accepted textures. The existing asset publisher transaction
    # removes obsolete managed files from this exact configuration's Web folder.
    [xml]$webProject = $nativeProject.OuterXml
    $profileText = Get-Content -LiteralPath (Join-Path $toolRoot 'Profiles.smile') -Raw
    $nativePolicy = 'Public Const INCLUDE_DIAGNOSTIC_PROFILES = True'
    if ([regex]::Matches($profileText, [regex]::Escape($nativePolicy)).Count -ne 1) {
        throw 'Expected exactly one native diagnostic-profile publication policy.'
    }
    $webProfileRoot = Join-Path $toolRoot 'BuildAssets\ViewerWeb'
    $null = New-Item -ItemType Directory -Force -Path $webProfileRoot
    [IO.File]::WriteAllText((Join-Path $webProfileRoot 'Profiles.smile'),
        $profileText.Replace($nativePolicy, 'Public Const INCLUDE_DIAGNOSTIC_PROFILES = False'),
        [Text.UTF8Encoding]::new($false))
    $currentModels = @('Assets\Generation2\ArinV57\ArinV57.sm3d',
        'Assets\Generation2\OrinV13\OrinV13.sm3d',
        'Assets\Generation2\RedDragon\RedDragon.sm3d')
    foreach ($item in @($webProject.SmileProject.ItemGroup.ChildNodes)) {
        if ($item.Name -eq 'SmileSource' -and $item.Include -eq 'Profiles.smile') {
            $item.SetAttribute('Include', 'BuildAssets\ViewerWeb\Profiles.smile')
        }
        if (($item.Name -eq 'Model3DAsset' -and $item.LogicalPath -notin $currentModels) -or
            ($item.Name -eq 'Asset' -and $item.Include -eq 'TechnicalAssets\Generation2\AnimationArticulated.sm3d')) {
            $null = $item.ParentNode.RemoveChild($item)
        }
    }
    Assert-ViewerSourceInventory $webProject 'BuildAssets\ViewerWeb\Profiles.smile' `
        'Character Viewer Web publication project'
    Assert-WebModelInventory $webProject
    $webProjectPath = Join-Path $toolRoot 'Character3DViewer.WebPublication.smileproj'
    $webProject.Save($webProjectPath)
    & $compiler --project $webProjectPath --target web `
        --configuration $Configuration --output-dir $webOutput --web-quality $WebQuality
    if ($LASTEXITCODE -ne 0) {
        throw 'Character Viewer/editor Web compilation failed.'
    }
    Assert-CharacterPublication $webOutput
    Write-Host "Built Character Viewer/editor Web: $webOutput"
}
