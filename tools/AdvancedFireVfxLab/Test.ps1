[CmdletBinding()]
param([ValidateSet('None', 'Begin', 'Draw', 'End')][string]$Fault = 'None')
$ErrorActionPreference = 'Stop'
$fireRepository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$projectPath = Join-Path $PSScriptRoot 'FireLabTests.generated.smileproj'
$fixturePath = Join-Path $PSScriptRoot 'FireFault.generated.smile'
$projectText = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'AdvancedFireVfxLab.smileproj') -Raw
$projectText = $projectText.Replace('Program.smile', 'FireLabTests.smile').Replace(
    '<ApplicationId>smile.examples.advanced-fire-vfx-lab</ApplicationId>',
    '<ApplicationId>smile.tests.kael-fire-lab</ApplicationId>')
try {
    if ($Fault -ne 'None') {
        $fixture = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'FireLabTests.smile') -Raw
        $operation = switch ($Fault) {
            Begin { '    Ok = G.Begin3D(Camera, 0, 0, 0)' }
            Draw { '        Ok = Scene.Draw(Value)' }
            End { '    Ok = G.End3DChecked() And Ok' }
        }
        if (-not $fixture.Contains($operation)) { throw "Missing $Fault observation boundary." }
        # Preserve real rendering and cleanup; inject only the first observed return value.
        $fixture = $fixture.Replace($operation, $operation + "`n`n    If Index = 0 Then`n        Ok = False`n    End If`n")
        [IO.File]::WriteAllText($fixturePath, $fixture)
        $projectText = $projectText.Replace('FireLabTests.smile', 'FireFault.generated.smile')
    }
    [IO.File]::WriteAllText($projectPath, $projectText)
    $output = Join-Path $PSScriptRoot 'bin\Tests\FireLabTests.exe'
    & (Join-Path $fireRepository 'artifacts\compiler\smilec.exe') --project $projectPath `
        --target windows-x64 --configuration Release --graphics DirectX -o $output
    if ($LASTEXITCODE -ne 0) { throw 'Fire Lab native fixture compilation failed.' }
    $result = & (Join-Path $fireRepository 'scripts\run-bounded-test.cmd') 30 $output
    $runExit = $LASTEXITCODE
    $calibration = @($result | Where-Object { $_.StartsWith('FIRE_ARIN_JSON: ') })
    if ($calibration.Count -ne 1) { throw 'Expected the actual Fire Lab Arin calibration export.' }
    & {
        . (Join-Path $fireRepository 'scripts\sync-arin-v5-7-calibration.ps1') -Character Arin -FunctionsOnly
        $actual = Normalize-Snapshot ($calibration[0].Substring('FIRE_ARIN_JSON: '.Length) | ConvertFrom-Json -AsHashtable)
        $expected = Read-Snapshot $snapshotPath
        if (($actual | ConvertTo-Json -Depth 24 -Compress) -cne ($expected | ConvertTo-Json -Depth 24 -Compress)) {
            throw 'Fire Lab Arin calibration differs from the complete canonical package.'
        }
    }
    $result = @($result | Where-Object { -not $_.StartsWith('FIRE_ARIN_JSON: ') })
    $poseRows = @($result | Where-Object { $_.StartsWith('FIRE_FRAME_ZERO: ') })
    $reference = Get-Content -LiteralPath (Join-Path $fireRepository 'games\SinStarI\SourceAssets\Characters\Paladin\ArinV57\Previews\Accepted-Pose-References\frame-zero-transforms.json') -Raw | ConvertFrom-Json
    if ($poseRows.Count -ne $reference.poses.Count) { throw 'Expected every canonical Arin frame-zero pose.' }
    for ($poseIndex = 0; $poseIndex -lt $poseRows.Count; $poseIndex++) {
        $actualPose = $poseRows[$poseIndex].Replace('FIRE_FRAME_ZERO: ', 'ARIN_FRAME_ZERO: ').Split('|')
        $matching = @($reference.poses | Where-Object { $_.Split('|')[0] -ceq $actualPose[0] })
        if ($matching.Count -ne 1) { throw "Missing Viewer pose: $($actualPose[0])" }
        $expectedPose = $matching[0].Split('|')
        if ($actualPose[0] -cne $expectedPose[0] -or $actualPose.Count -ne $expectedPose.Count) {
            throw 'Fire Lab pose reference layout differs.'
        }
        for ($component = 1; $component -lt $actualPose.Count; $component++) {
            if ([Math]::Abs([long]$actualPose[$component] - [long]$expectedPose[$component]) -gt 2) {
                throw "Fire Lab pose differs from Viewer: $($actualPose[0]), component $component ($($actualPose[$component]) versus $($expectedPose[$component]))."
            }
        }
    }
    $result = @($result | Where-Object { -not $_.StartsWith('FIRE_FRAME_ZERO: ') })
    if ($runExit -ne 0 -or (($result -join "`n").Trim() -ne 'Kael Fire Lab native failures: 0')) {
        throw "Fire Lab native checks failed: $result"
    }
    Write-Host ($result -join "`n")
} finally {
    if (Test-Path -LiteralPath $projectPath) { Remove-Item -LiteralPath $projectPath }
    if (Test-Path -LiteralPath $fixturePath) { Remove-Item -LiteralPath $fixturePath }
}
