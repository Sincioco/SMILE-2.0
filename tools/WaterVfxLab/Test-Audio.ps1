[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$scenePath = Join-Path $PSScriptRoot 'AudioScene.generated.smile'
$fixturePath = Join-Path $PSScriptRoot 'AudioTests.generated.smile'
$projectPath = Join-Path $PSScriptRoot 'AudioTests.generated.smileproj'
# Observe actual scene statements without adding a production diagnostics API or replacing playback.
$scene = Get-Content (Join-Path $PSScriptRoot 'WaterLabScene.smile') -Raw
$scene = [regex]::Replace($scene, '(?m)^(\s*)Play Sound "([^"]+)" On Channel (\d+)\s*$', {
    param($match)
    $match.Value.TrimEnd() + "`n" + $match.Groups[1].Value.Trim("`r", "`n") +
        'Print "Play ' + $match.Groups[3].Value + ' ' + $match.Groups[2].Value + '"' + "`n"
})
$scene = [regex]::Replace($scene, '(?m)^(\s*)Stop Sound On Channel (\d+)\s*$', {
    param($match)
    $match.Value.TrimEnd() + "`n" + $match.Groups[1].Value.Trim("`r", "`n") +
        'Print "Stop ' + $match.Groups[2].Value + '"' + "`n"
})
$fixture = @'
Option Explicit

Import Smile.Tools.WaterLabScene As Scene
Import Smile.Tools.WaterLabUi As UI

Dim Lab As Scene.State
Dim Index As Number

Game Window "Water Lab Audio Checks" Size 400 By 300

Call Scene.Start(Lab)

If Not Lab.Ready Then
    Print "Audio Scene Failed"
    End Program
End If

Lab.Orbit = False
Lab.SpeedPercent = 100

Do
    Call Prepare(3, False)
    Call Scene.Seek(Lab, 1400)

    Print "Begin Paused Cast"

    Call UI.Update(Lab, KEY_RIGHT)
    Call Scene.Update(Lab, 0)

    Print "End"

    Call Scene.Seek(Lab, 3500)

    Print "Begin Paused Impact"

    Call UI.Update(Lab, KEY_RIGHT)
    Call Scene.Update(Lab, 0)

    Print "End"

    Call Prepare(3, False)
    Call Scene.SetAudioEnabled(Lab, False)
    Call Scene.Seek(Lab, 4000)

    Print "Begin Silent Seek Unmute"

    Call Scene.SetAudioEnabled(Lab, True)
    Call Scene.Update(Lab, 0)
    Call Scene.SetPaused(Lab, False)
    Call Scene.Update(Lab, 100)

    Print "End"

    For Index = 1 To 8

        Call Prepare(Index, False)
        Call Scene.Seek(Lab, 1400)
        Call Scene.SetPaused(Lab, False)

        Print "Begin Mode "; Index

        Call Scene.Update(Lab, 100)
        Call Scene.Update(Lab, 100)
        Call Scene.Seek(Lab, 2400)
        Call Scene.Update(Lab, 100)
        Call Scene.Seek(Lab, 3500)
        Call Scene.Update(Lab, 100)
        Call Scene.Update(Lab, 100)

        Print "End"
    End For

    For Index = 1 To 3

        Call Prepare(3, True)

        Lab.KaelAttack = Index

        Call Scene.Restart(Lab)
        Call Scene.Seek(Lab, 1400)
        Call Scene.SetPaused(Lab, False)

        Print "Begin Kael "; Index

        Call Scene.Update(Lab, 100)
        Call Scene.Seek(Lab, 3500)
        Call Scene.Update(Lab, 100)

        Print "End"
    End For

    Call Prepare(7, False)
    Call Scene.Seek(Lab, 1400)
    Call Scene.SetPaused(Lab, False)
    Call Scene.Update(Lab, 100)

    Play Sound "Assets/Lightning/thunder.wav" On Channel 9

    Print "Begin Sound Off"

    Call Scene.SetAudioEnabled(Lab, False)
    Call Scene.Update(Lab, 100)

    Print "End"

    Call Scene.SetAudioEnabled(Lab, True)
    Call Scene.Restart(Lab)
    Call Scene.Seek(Lab, 1400)
    Call Scene.Update(Lab, 100)

    Print "Begin Pause Resume"

    Call UI.Update(Lab, KEY_SPACE)
    Call Scene.Update(Lab, 100)
    Call UI.Update(Lab, KEY_SPACE)
    Call Scene.Update(Lab, 100)

    Print "End"
    Print "Begin Restart"

    Call Scene.Restart(Lab)

    Print "End"

    Call Scene.Seek(Lab, 1400)
    Call Scene.Update(Lab, 100)

    Print "Begin Character Switch"

    Call UI.Update(Lab, KEY_X)

    Print "End"
    Print "Begin Destroy"

    If Lab.ErrorCode <> 0 Then
        Print "Audio Scene Failed"
    End If

    Call Scene.Destroy(Lab)

    Print "End"

Loop Until True

Stop Sound On Channel 9

Sub Prepare(Mode As Number, Kael As Boolean)

    Call Scene.SelectCharacter(Lab, Kael)

    Lab.Mode = Mode

    Call Scene.Restart(Lab)
    Call Scene.SetAudioEnabled(Lab, True)
    Call Scene.SetPaused(Lab, True)

End Sub
'@
$project = (Get-Content (Join-Path $PSScriptRoot 'WaterVfxLab.smileproj') -Raw).
    Replace('Program.smile', 'AudioTests.generated.smile').
    Replace('WaterLabScene.smile', 'AudioScene.generated.smile').
    Replace('smile.tools.water-vfx-lab', 'smile.tests.water-audio')
$cast = 'Play 6 Assets/Audio/mira-water-attack.wav'
$splash = 'Play 8 Assets/Water/water-splash.wav'
$stops = @('Stop 6', 'Stop 7', 'Stop 8')
$expected = @{
    'Paused Cast' = $stops; 'Paused Impact' = $stops; 'Silent Seek Unmute' = @()
    'Sound Off' = $stops; 'Pause Resume' = $stops; 'Restart' = $stops
    'Character Switch' = $stops; 'Destroy' = $stops
}
foreach ($index in 1..8) {
    $plays = @($cast)
    if ($index -eq 1) { $plays = @('Play 6 Assets/Audio/mira-heal-party.wav') }
    if ($index -in 6,7) { $plays = @('Play 6 Assets/Water/water-tsunami.wav') }
    if ($index -eq 7) { $plays += 'Play 7 Assets/Lightning/thunder.wav' }
    $plays += $stops
    if ($index -eq 2) { $plays += 'Play 8 Assets/Water/water-barrier-hit.wav' }
    $plays += $stops
    if ($index -in 3,5,6,7,8) { $plays += $splash }
    $expected["Mode $index"] = $plays
}
foreach ($index in 1..3) { $expected["Kael $index"] = @($cast) + $stops + @($splash) }
try {
    [IO.File]::WriteAllText($scenePath, $scene)
    [IO.File]::WriteAllText($fixturePath, $fixture)
    [IO.File]::WriteAllText($projectPath, $project)
    $output = Join-Path $PSScriptRoot 'bin\AudioTests\WaterAudioTests.exe'
    & "$repository\artifacts\compiler\smilec.exe" --project $projectPath --target windows-x64 `
        --configuration Release --graphics DirectX -o $output
    if ($LASTEXITCODE -ne 0) { throw 'Water audio fixture compilation failed.' }
    $result = & "$repository\scripts\run-bounded-test.cmd" 30 $output
    if ($LASTEXITCODE -ne 0) { throw "Water audio fixture execution failed: $result" }
    $failures = 0
    $seen = 0
    $label = $null
    foreach ($line in $result) {
        if ($line -match '^Begin (.+)$') { $label = $Matches[1]; $observed = @() }
        elseif ($line -eq 'End' -and $label) {
            $seen++
            if (($observed -join '|') -ne ($expected[$label] -join '|')) {
                $failures++
                Write-Host "FAIL $label`: expected [$($expected[$label] -join ', ')]; observed [$($observed -join ', ')]"
            }
            $label = $null
        }
        elseif ($label) { $observed += $line }
    }
    if ($seen -ne $expected.Count -or $failures -ne 0) {
        throw "Water native audio transitions: $failures failed; $seen/$($expected.Count) observed."
    }
    Write-Host "Water native audio transitions: $seen passed; actual Play/Stop statements retained."
} finally {
    foreach ($path in @($scenePath, $fixturePath, $projectPath)) {
        if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path }
    }
}
