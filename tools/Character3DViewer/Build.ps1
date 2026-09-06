[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [ValidateSet('Native', 'Web', 'All')]
    [string]$Target = 'All'
)

$ErrorActionPreference = 'Stop'
$toolRoot = $PSScriptRoot
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $toolRoot '..\..'))
$compiler = Join-Path $repositoryRoot 'artifacts\compiler\smilec.exe'
$project = Join-Path $toolRoot 'Character3DViewer.smileproj'
$outputRoot = Join-Path $toolRoot "bin\$Configuration"

if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) {
    throw "Build SMILE before compiling the Character Viewer/editor: $compiler"
}

& (Join-Path $toolRoot 'Prepare-BuildAssets.ps1')
if ($Target -in @('Native', 'All')) {
    $output = Join-Path $outputRoot 'Character3DViewer.exe'
    New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null
    & $compiler --project $project --target windows-x64 `
        --configuration $Configuration --graphics DirectX -o $output
    if ($LASTEXITCODE -ne 0) {
        throw 'Character Viewer/editor native compilation failed.'
    }
    Write-Host "Built Character Viewer/editor: $output"
}

if ($Target -in @('Web', 'All')) {
    $webOutput = Join-Path $outputRoot 'Web'
    # Keep source ownership and native diagnostics intact. Generate only the
    # Web publication's profile policy and project asset list; never transcode
    # or resize accepted textures. The existing asset publisher transaction
    # removes obsolete managed files from this exact configuration's Web folder.
    [xml]$webProject = Get-Content -LiteralPath $project -Raw
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
    $webProjectPath = Join-Path $toolRoot 'Character3DViewer.WebPublication.smileproj'
    $webProject.Save($webProjectPath)
    & $compiler --project $webProjectPath --target web `
        --configuration $Configuration --output-dir $webOutput
    if ($LASTEXITCODE -ne 0) {
        throw 'Character Viewer/editor Web compilation failed.'
    }
    Write-Host "Built Character Viewer/editor Web: $webOutput"
}
