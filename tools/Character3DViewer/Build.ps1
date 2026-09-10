[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [ValidateSet('Native', 'Web', 'All')]
    [string]$Target = 'All',
    [ValidateSet('Full', 'Low', 'Medium', 'High')]
    [string]$WebQuality = 'Full',
    [switch]$PublicRoster,
    [switch]$Studio,
    [switch]$PrepareOnly
)

$ErrorActionPreference = 'Stop'
if ($PrepareOnly -and $PublicRoster) {
    throw 'Use -PrepareOnly for the normal local IDE roster, not PublicRoster.'
}
if ($PublicRoster -and $Target -ne 'Web') {
    throw 'Public roster publication requires -Target Web.'
}
if ($WebQuality -ne 'Full' -and $Target -ne 'Web') {
    throw 'Optimized profiles require -Target Web; normal native/Web output is preserved.'
}
$toolRoot = $PSScriptRoot
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $toolRoot '..\..'))
$compiler = Join-Path $repositoryRoot 'artifacts\compiler\smilec.exe'
$project = Join-Path $toolRoot 'Character3DViewer.smileproj'
$outputRoot = Join-Path $toolRoot "bin\$Configuration"
$studioRoot = Join-Path $repositoryRoot 'tools\SmileStudio'
if ($Studio) {
    $outputRoot = Join-Path $repositoryRoot "tools\SmileStudio\bin\$Configuration"
}
$viewerSources = @(
    'Program.smile',
    'ViewerWorkflow.smile',
    'Profiles.smile',
    'OrinStorm.smile',
    '..\..\games\SinStarI\SourceAssets\Characters\Tank\OrinV13\OrinEquipmentContours.smile',
    'ArinShieldRim.smile',
    'BattleAudio.smile',
    'BattleCamera.smile',
    'BattleCameraShots.smile',
    'ViewerBeatSequence.smile',
    'ViewerBeatEditor.smile',
    'DragonPresence.smile',
    'CalibrationJson.smile',
    'ViewerTiming.smile',
    'ViewerSession.smile',
    'ViewerLifecycle.smile',
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
    $expectedModels += $unityLogicalPaths
    $modelNodes = @($ProjectXml.SmileProject.ItemGroup.Model3DAsset)

    if ($modelNodes.Count -ne $expectedModels.Count) {
        throw 'Web publication must contain exactly the selected character roster.'
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

function Set-StudioHost([xml]$ProjectXml) {
    $ProjectXml.SmileProject.PropertyGroup.OutputName = 'SmileStudio'
    # Preserve the authoritative calibration storage namespace, not a second
    # Studio copy of each character's saved data.
    $node = @($ProjectXml.SmileProject.ItemGroup.SmileSource | Where-Object {
        [string]$_.Include -eq 'Program.smile'
    })[0]
    foreach ($source in @($ProjectXml.SmileProject.ItemGroup.SmileSource)) {
        if ($source -eq $node) { continue }
        # Both hosts are siblings. Repository-root references keep their path;
        # Viewer-local modules stay linked, including the Web profile override.
        if (-not $source.Include.StartsWith('..\..\')) {
            $source.SetAttribute('Include', '..\Character3DViewer\' + $source.Include)
        }
    }
    $shell = $ProjectXml.CreateElement('SmileSource')
    $shell.SetAttribute('Include', 'StudioShell.smile')
    $null = $node.ParentNode.InsertAfter($shell, $node)
    $logo = $ProjectXml.CreateElement('Asset')
    $logo.SetAttribute('Include', 'Assets\Branding\smile-2.0-logo.png')
    $null = $node.ParentNode.AppendChild($logo)
}

function Prepare-StudioAssets([xml]$ProjectXml) {
    # The shared project model confines asset inputs to their project directory.
    # Mirror only declared inputs, including each model's adjacent texture files.
    # Source modules remain linked to their real owners; live saves are never copied.
    $inputs = @($ProjectXml.SmileProject.ItemGroup.Asset | Where-Object {
        $_.Include -ne 'Assets\Branding\smile-2.0-logo.png'
    } | ForEach-Object {
        Get-ChildItem -Path (Join-Path $toolRoot $_.Include) -File
    })
    $modelDirectories = @($ProjectXml.SmileProject.ItemGroup.Model3DAsset | ForEach-Object {
        Split-Path (Join-Path $toolRoot $_.Include) -Parent
        Split-Path (Join-Path $toolRoot $_.Descriptor) -Parent
    } | Sort-Object -Unique)
    foreach ($directory in $modelDirectories) {
        $inputs += Get-ChildItem -LiteralPath $directory -File -Recurse
    }
    foreach ($inputFile in $inputs) {
        $relative = $inputFile.FullName.Substring($toolRoot.Length + 1)
        $destination = Join-Path $studioRoot $relative
        $null = New-Item -ItemType Directory -Force -Path (Split-Path $destination -Parent)
        Copy-Item -LiteralPath $inputFile.FullName -Destination $destination -Force
    }
    $branding = Join-Path $studioRoot 'Assets\Branding'
    $null = New-Item -ItemType Directory -Force -Path $branding
    Copy-Item -LiteralPath (Join-Path $repositoryRoot 'assets\branding\smile-2.0-logo.png') `
        -Destination (Join-Path $branding 'smile-2.0-logo.png') -Force
}

if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) {
    throw "Build SMILE before compiling the Character Viewer/editor: $compiler"
}

[xml]$nativeProject = Get-Content -LiteralPath $project -Raw
Assert-ViewerSourceInventory $nativeProject 'Profiles.smile' 'Character Viewer project'

$unityAssets = @()
if (-not $PublicRoster) {
    $unityAssets = @(& (Join-Path $toolRoot 'Prepare-UnityAssets.ps1'))
}
& (Join-Path $toolRoot 'Prepare-BuildAssets.ps1') -SkipUnityRoster
$unityLogicalPaths = @($unityAssets | ForEach-Object { $_.LogicalPath })
if ($Studio) {
    [xml]$expectedStudio = $nativeProject.OuterXml
    Set-StudioHost $expectedStudio
    $project = Join-Path $studioRoot 'SmileStudio.smileproj'
    [xml]$studioProject = Get-Content -LiteralPath $project -Raw
    if ($studioProject.OuterXml -cne $expectedStudio.OuterXml) {
        throw 'SmileStudio.smileproj must retain the shared Viewer inventory plus its Program, StudioShell and official logo.'
    }
    if (-not $PublicRoster) { Prepare-StudioAssets $studioProject }
}
if ($PrepareOnly) {
    Write-Host "Prepared project inputs: $project"
    return
}
if ($Target -in @('Native', 'All')) {
    $output = Join-Path $outputRoot 'Character3DViewer.exe'
    if ($Studio) {
        $output = Join-Path $outputRoot 'SmileStudio.exe'
    }
    New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null
    [string[]]$debugArguments = if ($Configuration -eq 'Debug') { @('--debug') } else { @() }
    & $compiler --project $project --target windows-x64 `
        --configuration $Configuration --graphics DirectX -o $output @debugArguments
    if ($LASTEXITCODE -ne 0) {
        throw 'Character Viewer/editor native compilation failed.'
    }
    Assert-CharacterPublication $outputRoot
    Write-Host "Built Character Viewer/editor: $output"
}

if ($Target -in @('Web', 'All')) {
    $webFolder = if ($WebQuality -eq 'Full') { 'Web' } else { "Web - Optimized $WebQuality" }
    if ($PublicRoster) { $webFolder += ' - Public' }
    $webOutput = Join-Path $outputRoot $webFolder
    # Keep source ownership and native diagnostics intact. Generate only the
    # Web publication's profile policy and project asset list. Quality profiles
    # resize only unpublished staging copies, never accepted textures. The existing asset publisher transaction
    # removes obsolete managed files from this exact configuration's Web folder.
    [xml]$webProject = $nativeProject.OuterXml
    $profileText = Get-Content -LiteralPath (Join-Path $toolRoot 'Profiles.smile') -Raw
    $localRosterPolicy = 'Public Const INCLUDE_UNITY_CHARACTERS = True'
    if ([regex]::Matches($profileText, [regex]::Escape($localRosterPolicy)).Count -ne 1) {
        throw 'Expected exactly one permanent local-roster publication policy.'
    }
    if ($PublicRoster) {
        $profileText = $profileText.Replace($localRosterPolicy,
            'Public Const INCLUDE_UNITY_CHARACTERS = False')
    }
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
    $currentModels += $unityLogicalPaths
    foreach ($item in @($webProject.SmileProject.ItemGroup.ChildNodes)) {
        if ($PublicRoster -and $item.Name -eq 'Asset' -and
            $item.Include -in @('Assets\Audio\zara-*.wav', 'Assets\Audio\vrax-*.wav')) {
            $null = $item.ParentNode.RemoveChild($item)
            continue
        }
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
    if ($Studio) {
        Set-StudioHost $webProject
        if ($PublicRoster) { Prepare-StudioAssets $webProject }
        $webProjectPath = Join-Path $studioRoot 'SmileStudio.WebPublication.smileproj'
    }
    $webProject.Save($webProjectPath)
    & $compiler --project $webProjectPath --target web `
        --configuration $Configuration --output-dir $webOutput --web-quality $WebQuality
    if ($LASTEXITCODE -ne 0) {
        throw 'Character Viewer/editor Web compilation failed.'
    }
    Assert-CharacterPublication $webOutput
    Write-Host "Built Character Viewer/editor Web: $webOutput"
}
