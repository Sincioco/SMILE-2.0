[CmdletBinding()]
param(
    [ValidateSet('Trial', 'Original')][string]$Mode = 'Trial',
    [ValidateSet('Native', 'Web')][string]$Target = 'Native',
    [switch]$Build,
    [switch]$NoLaunch
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..\..\..\..'))
$viewerRoot = Join-Path $repositoryRoot 'tools\Character3DViewer'
$privateRoot = Join-Path $PSScriptRoot 'Private'
$trialExecutable = Join-Path $privateRoot 'Viewer\Character3DViewer.exe'
$originalExecutable = Join-Path $privateRoot 'OriginalViewer\Character3DViewer.exe'
$baselineExecutable = Join-Path $repositoryRoot 'artifacts\deliverables\Character3DViewer-2026-09-10\Character3DViewer.exe'

if (-not (Test-Path -LiteralPath (Join-Path $privateRoot 'original-viewer-verified.json'))) {
    if (-not (Test-Path -LiteralPath $baselineExecutable)) {
        throw 'The verified September 10 baseline Viewer is required to preserve the Original option.'
    }
    if ((Get-FileHash -LiteralPath $baselineExecutable).Hash -ne
        '269348985DF3811B611F08E42196F2148533CD00C348A7BE8F63D84AF7BFC102') {
        throw 'The baseline Viewer checksum changed; do not replace the Original snapshot.'
    }
    $null = New-Item -ItemType Directory -Force -Path (Split-Path $originalExecutable)
    $snapshotFiles = @()
    foreach ($item in Get-ChildItem -LiteralPath (Split-Path $baselineExecutable) -File -Recurse) {
        $relative = [IO.Path]::GetRelativePath((Split-Path $baselineExecutable), $item.FullName)
        $destination = Join-Path (Split-Path $originalExecutable) $relative
        $checksum = (Get-FileHash -LiteralPath $item.FullName).Hash
        if (-not (Test-Path -LiteralPath $destination) -or
            (Get-FileHash -LiteralPath $destination).Hash -ne $checksum) {
            $null = New-Item -ItemType Directory -Force -Path (Split-Path $destination)
            Copy-Item -LiteralPath $item.FullName -Destination $destination -Force
        }
        if ((Get-FileHash -LiteralPath $destination).Hash -ne $checksum) { throw "Snapshot mismatch: $relative" }
        $snapshotFiles += [pscustomobject]@{ path = $relative; sha256 = $checksum }
    }
    $snapshotFiles | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $privateRoot 'original-viewer-verified.json')
}

if ($Mode -eq 'Original') {
    if ($Target -ne 'Native') { throw 'Original restores the saved native Viewer; the normal Web build remains unchanged.' }
    $executable = $originalExecutable
} else {
    $executable = $trialExecutable
    if ($Build -or -not (Test-Path -LiteralPath $trialExecutable) -or $Target -eq 'Web') {
        $model = Join-Path $privateRoot 'red-dragon-vrax-trial.glb'
        $validation = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'retarget-validation.json') -Raw | ConvertFrom-Json
        if ((Get-FileHash -LiteralPath $model).Hash -ine $validation.outputSha256) {
            throw 'Trial model does not match its validation checksum.'
        }
        & (Join-Path $viewerRoot 'Prepare-BuildAssets.ps1')
        $profileRoot = Join-Path $viewerRoot 'BuildAssets\DragonTrial'
        $null = New-Item -ItemType Directory -Force -Path $profileRoot
        Copy-Item -LiteralPath $model -Destination (Join-Path $profileRoot 'red-dragon-vrax-trial.glb')
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'RedDragonV12VraxTrial.sm3d.json') -Destination $profileRoot
        $profile = [IO.File]::ReadAllText((Join-Path $viewerRoot 'Profiles.smile'))
        $profile = $profile.Replace('Result.CandidateVersion = "v1.1"', 'Result.CandidateVersion = "Vrax Trial"')
        $profile = $profile.Replace('Result.DisplayName = "Dragon"', 'Result.DisplayName = "Dragon Trial"')
        $profile = $profile.Replace('red-dragon-v1.1-animated.glb', 'red-dragon-vrax-trial.glb')
        $profile = $profile.Replace('Result.ExpectedClipCount = 6', 'Result.ExpectedClipCount = 16')
        $profile = [regex]::Replace($profile,
            '(Else If ProfileIndex = PROFILE_DRAGON Then\s+Result = )6', '${1}16')
        $names = @('Idle','Walk','Run','Hit','Attack','Attack2','Attack3','Attack4','Attack5','Attack6')
        $extra = ''
        for ($index = 0; $index -lt $names.Count; $index++) {
            $extra += "`n        Else If ClipIndex = $($index + 6) Then`n            Result = `"Vrax_$($names[$index])`""
        }
        $profile = $profile.Replace('Result = "Roar"', 'Result = "Roar"' + $extra)
        [IO.File]::WriteAllText((Join-Path $profileRoot 'Profiles.smile'), $profile)
        [xml]$project = Get-Content -LiteralPath (Join-Path $viewerRoot 'Character3DViewer.smileproj') -Raw
        foreach ($node in $project.SmileProject.ItemGroup.ChildNodes) {
            if ($node.Name -eq 'SmileSource' -and $node.Include -eq 'Profiles.smile') {
                $node.SetAttribute('Include', 'BuildAssets\DragonTrial\Profiles.smile')
            }
            if ($node.Name -eq 'Model3DAsset' -and $node.LogicalPath -eq 'Assets\Generation2\RedDragon\RedDragon.sm3d') {
                $node.SetAttribute('Include', 'BuildAssets\DragonTrial\red-dragon-vrax-trial.glb')
                $node.SetAttribute('Descriptor', 'BuildAssets\DragonTrial\RedDragonV12VraxTrial.sm3d.json')
            }
        }
        $projectPath = Join-Path $viewerRoot 'Character3DViewer.DragonTrial.smileproj'
        $project.Save($projectPath)
        $compiler = Join-Path $repositoryRoot 'artifacts\compiler\smilec.exe'
        if ($Target -eq 'Native') {
            & $compiler --project $projectPath --target windows-x64 --configuration Release --graphics DirectX -o $trialExecutable
        } else {
            & $compiler --project $projectPath --target web --configuration Release --output-dir (Join-Path $privateRoot 'Web')
        }
        if ($LASTEXITCODE -ne 0) { throw 'Dragon trial compilation failed.' }
    }
}

if (-not $NoLaunch -and $Target -eq 'Native') {
    $selectedTrialExecutable = $executable
    . (Join-Path $viewerRoot 'Launch.ps1') -FunctionsOnly
    foreach ($known in @($trialExecutable, $originalExecutable, $baselineExecutable)) {
        Request-ViewerShutdown @(Get-ViewerProcessCandidates $known) 5
    }
    & (Join-Path $viewerRoot 'Launch.ps1') -Executable $selectedTrialExecutable -SkipWindowActivation
}
