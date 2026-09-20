[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$gameRoot = Join-Path $root 'games\SinStarI'
$testRoot = Join-Path $root ('artifacts\tests\SinStarPresentation-' + [Guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $testRoot
$presentationSource = Get-Content -LiteralPath (Join-Path $gameRoot 'CharacterPresentation.smile') -Raw
if ($presentationSource -match 'Dim\s+Preview\s+As\s+New\s+Viewer\.Session') {
    throw 'Sin Star I must not move Viewer session construction into a module initializer.'
}
if ($presentationSource -notmatch '(?s)Public Sub Enter\(.+?Preview = New Viewer\.Session\(\).+?End Sub') {
    throw 'Sin Star I must construct the Viewer session explicitly inside Enter.'
}
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
$musicEntry = $project.SmileProject.ItemGroup.SmileSource | Where-Object { $_.Include.EndsWith('Music.smile') }
$musicText = Get-Content -LiteralPath (Join-Path $gameRoot 'Music.smile') -Raw
$musicText = $musicText.Replace('    CurrentTrack = Name', "    CurrentTrack = Name`n    Print `"MUSIC_SELECTED: `" + Name")
$musicText = $musicText.Replace('    Stop Music', "    Stop Music`n    Print `"MUSIC_STOPPED`"")
[IO.File]::WriteAllText((Join-Path $testRoot 'Music.smile'), $musicText)
$musicEntry.SetAttribute('Include', 'Music.smile')
$startup = Get-Content -LiteralPath (Join-Path $testRoot 'Program.smile') -Raw
$startup = $startup.Replace('Import SinStarI.TitleScreen As TitleScreen',
    "Import SinStarI.TitleScreen As TitleScreen`nImport SinStarI.BackgroundMusic As BackgroundMusic")
$startup = $startup.Replace('Passed = True', @'
Call BackgroundMusic.SelectTrack("Starforge March (Title Screen)")
Call BackgroundMusic.SelectTrack("Starforge March (Title Screen)")
Call BackgroundMusic.SelectTrack("")
Call BackgroundMusic.SelectTrack("Unassigned")
Passed = True
'@)
$startup = $startup.Replace('Print "Sin Star I presentations passed."',
    "Call BackgroundMusic.Shutdown()`nPrint `"Sin Star I presentations passed.`"")
[IO.File]::WriteAllText((Join-Path $testRoot 'Program.smile'), $startup)
$workflowEntry = $project.SmileProject.ItemGroup.SmileSource | Where-Object { $_.Include.EndsWith('ViewerWorkflow.smile') }
$workflowText = Get-Content -LiteralPath (Join-Path $root 'tools\Character3DViewer\ViewerWorkflow.smile') -Raw
$workflowText = $workflowText.Replace('Import Smile.Simple3D.Core As Core',
    "Import Smile.Simple3D.Core As Core`nImport Smile.Simple3D.PrecisionCamera3D As PrecisionCamera")
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
        If ViewerProfiles.IsPartyTab(Me.Session.SelectedCharacterTab) Then
            If (Me.Party.ParticipantCount <> 4 Or
                Not Me.Party.Fourth.Ready Or Not Me.Party.Mira.Ready Or
                Me.Party.Companion.Profile <> ViewerProfiles.PROFILE_ORIN Or
                Me.Party.Fourth.Profile <> ViewerProfiles.PROFILE_ZARA Or
                Me.Party.Mira.Profile <> ViewerProfiles.PROFILE_MIRA) Then
                Print "Party loaded an incorrect four-hero roster."
            End If
        End If
        If Me.Session.SelectedCharacterTab = ViewerProfiles.CHARACTER_TAB_PARTY_KAEL Then
            If (Me.DragonState.SelectedProfile <> ViewerProfiles.PROFILE_KAEL Or
                Me.DragonState.UseVrax Or
                ViewerParty.ActorPlaybackSpeed(Me.Party, 2, 100) <> 200) Then
                Print "Kael Party must load Kael at speed 200."
            End If
            Call Me.CheckKaelBendingFixture()
        End If
        If Me.Session.SelectedCharacterTab = ViewerProfiles.CHARACTER_TAB_KAEL Then
            If Me.Playback.PlaybackSpeed <> 200 Then
                Print "Kael's solo presentation must start at speed 200."
            End If
            Call Me.CheckKaelDemoFixture()
        End If
'@
$workflowText = $workflowText.Replace($loadBoundary, $diagnosticLoad)
$bendingFixture = @'
    Private Sub CheckKaelBendingFixture()

        Dim Cycle As Number
        Dim TurnIndex As Number
        Dim ClipIndex As Number
        Dim Duration As Number
        Dim Ok As Boolean
        Dim Name As Text
        Dim SoloFrame As PrecisionCamera.Framing
        Dim Home As Precision3D.Vector3
        Dim Position As Precision3D.Vector3

        SoloFrame = PrecisionCamera.AutoFit(
            ViewerProfiles.ProfileAt(ViewerProfiles.PROFILE_KAEL),
            Character3D.LocalBounds(Me.DragonState.Actor), 0, 0, 55.0, 10000)
        If Me.DragonState.Frame.ScalePercent <> SoloFrame.ScalePercent * 3 Then
            Print "Kael Party must be three times solo scale."
        End If

        Home = Precision3D.Vector(ToDouble(ViewerDragon.PositionX()), 0.0,
            ToDouble(ViewerDragon.PositionZ()))

        Me.Playback.AnimationUpdateElapsed = 60000

        Call Me.AdvancePartyDemo()

        Me.Playback.AnimationUpdateElapsed = 0

        Call Me.UpdateDragon()

        For Cycle = 0 To 8

            For TurnIndex = 0 To 5

                If Me.Party.Turn = 2 Then
                    Exit For
                End If

                Me.Playback.AnimationUpdateElapsed = 60000

                Call Me.AdvancePartyDemo()

            End For

            Me.Playback.AnimationUpdateElapsed = ViewerParty.VRAX_ATTACK_START_MILLISECONDS

            Call Me.AdvancePartyDemo()

            Name = ViewerProfiles.PartyAttackName(ViewerProfiles.PROFILE_KAEL, Cycle)
            If ((Cycle = 0 And Name <> "Attack") Or
                (Cycle = 1 And Name <> "EarthHurl") Or
                (Cycle = 2 And Name <> "WaterWhip") Or
                (Cycle = 3 And Name <> "Attack2") Or
                (Cycle = 4 And Name <> "EarthVolley") Or
                (Cycle = 5 And Name <> "WaterOrbit") Or
                (Cycle = 6 And Name <> "Attack") Or
                (Cycle = 7 And Name <> "EarthSlam") Or
                (Cycle = 8 And Name <> "WaterSurge")) Then
                Print "Kael must alternate normal, Earth and Water attacks."
            End If

            Ok = ViewerParty.UpdateDragon(Me.Party, Me.DragonState, Me.Effects,
                Me.Character, False, Name, False, 200, 0)

            If Me.Party.DragonCounter <> Cycle Or Me.DragonState.Clip <> Name Then
                Print "Kael automatic battle rotation skipped: "; Name
            End If

            If Cycle Mod 3 <> 0 Then

                Position = ViewerParty.VraxPosition(Me.Party)
                If Position.X <> Home.X Or Position.Z <> Home.Z Then
                    Print "Kael bending left his home position: "; Name
                End If

                For ClipIndex = 0 To Character3D.ClipCount(Me.DragonState.Actor) - 1

                    If Character3D.ClipName(Me.DragonState.Actor, ClipIndex) = Name Then
                        Duration = Character3D.ClipDuration(Me.DragonState.Actor, ClipIndex)
                    End If

                End For

                Ok = Character3D.SetAnimationTime(Me.DragonState.Actor, Duration * 60 / 100) And Ok
                Ok = ViewerParty.UpdateDragon(Me.Party, Me.DragonState, Me.Effects,
                    Me.Character, False, Name, False, 200, 0) And Ok

                If Cycle Mod 3 = 1 Then
                    Ok = Me.DragonState.Earth.Effect.Visible And Ok
                    Ok = Me.DragonState.Earth.Effect.ErrorCode = 0 And Ok
                Else
                    Ok = Me.DragonState.KaelWater.Effect.Visible And Ok
                    Ok = Me.DragonState.KaelWater.Frame.WrapStyle = Cycle / 3 + 1 And Ok
                    Ok = Me.DragonState.KaelWater.Frame.FlowScale > 1.9 And Ok
                    Ok = Me.DragonState.KaelWater.Frame.TargetHeight > 0.0 And Ok
                    Ok = Character3D.SetAnimationTime(Me.DragonState.Actor, Duration * 75 / 100) And Ok
                    Ok = ViewerParty.UpdateDragon(Me.Party, Me.DragonState, Me.Effects,
                        Me.Character, False, Name, False, 200, 0) And Ok
                    Ok = Me.DragonState.KaelWater.Effect.Visible And Ok
                    Ok = Me.DragonState.KaelWater.Effect.ImpactSprayCount > 0 And Ok
                End If

                If Not Ok Then
                    Print "Kael automatic bending cast failed: "; Name
                    Print Me.DragonState.KaelWater.Frame.WrapStyle
                    Print Me.DragonState.KaelWater.Frame.FlowScale
                    Print Me.DragonState.KaelWater.Frame.TargetHeight
                    Print Me.DragonState.KaelWater.Effect.ImpactSprayCount
                End If

            End If

            Me.Playback.AnimationUpdateElapsed = 60000

            Call Me.AdvancePartyDemo()

        End For

    End Sub

    Private Sub CheckKaelDemoFixture()

        Dim ClipIndex As Number
        Dim StepIndex As Number
        Dim Name As Text
        Dim SeenHurl As Boolean
        Dim SeenVolley As Boolean
        Dim SeenSlam As Boolean
        Dim SeenWhip As Boolean
        Dim SeenOrbit As Boolean
        Dim SeenSurge As Boolean
        Dim Ok As Boolean

        For ClipIndex = 0 To 15

            Name = ViewerPlayback.SelectedClipName(Me.Character, Me.Playback)
            SeenHurl = SeenHurl Or Name = "EarthHurl"
            SeenVolley = SeenVolley Or Name = "EarthVolley"
            SeenSlam = SeenSlam Or Name = "EarthSlam"
            SeenWhip = SeenWhip Or Name = "WaterWhip"
            SeenOrbit = SeenOrbit Or Name = "WaterOrbit"
            SeenSurge = SeenSurge Or Name = "WaterSurge"

            For StepIndex = 0 To 199

                Ok = Character3D.Update(Me.Character, 100)

                Call ViewerEffects.UpdateEquipmentFire(Me.Effects, Me.Character,
                    ViewerProfiles.PROFILE_KAEL, False, True, False, False, False,
                    Me.Playback.SelectedClip, Name, Me.ViewerCameraState.Live, 100, False, 200)

                Ok = Me.Effects.Earth.Effect.ErrorCode = 0 And Ok
                If Name = "WaterWhip" Or Name = "WaterOrbit" Or Name = "WaterSurge" Then
                    If Character3D.AnimationTime(Me.Character) = 2000 Then
                        Ok = Me.Effects.KaelWater.Effect.Visible And Ok
                        Ok = Abs(Me.Effects.KaelWater.Frame.FlowScale - 1.0) < 0.03 And Ok
                    End If
                End If

                Me.Timing.PresentationElapsed = 100

                Call Me.AdvanceAnimationSequence()

                If Not Ok Then
                    Print "Kael automatic demo effect failed: "; Name
                    Exit For
                End If

                If ViewerPlayback.SelectedClipName(Me.Character, Me.Playback) <> Name Then
                    Exit For
                End If

            End For

            If ViewerPlayback.SelectedClipName(Me.Character, Me.Playback) = Name Then
                Print "Kael automatic demo did not advance: "; Name
            End If

        End For

        If (Not SeenHurl Or
            Not SeenVolley Or
            Not SeenSlam Or Not SeenWhip Or Not SeenOrbit Or Not SeenSurge) Then
            Print "Kael automatic demo skipped an Earth or Water attack."
        End If

    End Sub

'@
$workflowText = $workflowText.Replace('End Class', $bendingFixture + 'End Class')
[IO.File]::WriteAllText((Join-Path $testRoot 'ViewerWorkflow.smile'), $workflowText)
$workflowEntry.SetAttribute('Include', 'ViewerWorkflow.smile')
$projectPath = Join-Path $testRoot 'PresentationTests.smileproj'
$project.Save($projectPath)
$exe = Join-Path $testRoot 'PresentationTests.exe'
& (Join-Path $root 'artifacts\compiler\smilec.exe') --project $projectPath --target windows-x64 --graphics DirectX -o $exe
if ($LASTEXITCODE -ne 0) { throw 'Presentation regression compilation failed.' }
$actual = & (Join-Path $PSScriptRoot 'run-bounded-test.cmd') 60 $exe
$expectedMusic = @('MUSIC_SELECTED: Starforge March (Title Screen)', 'MUSIC_SELECTED: Bloom (Arin)',
    'MUSIC_SELECTED: Sunrise Oath (Orin)', 'MUSIC_SELECTED: Golden Hour Ascend (Mira)',
    'MUSIC_SELECTED: Starforge Ascend (Kael)', 'MUSIC_STOPPED')
$musicEvents = @($actual | Where-Object { $_.StartsWith('MUSIC_') })
if (($musicEvents -join "`n") -cne ($expectedMusic -join "`n")) {
    throw "Music continuity or character selection failed: $musicEvents"
}
$actual = @($actual | Where-Object { -not $_.StartsWith('MUSIC_') })
foreach ($musicFile in Get-ChildItem -LiteralPath (Join-Path $gameRoot 'Assets\Music') -Filter *.mp3) {
    $published = Join-Path $testRoot "Assets\Music\$($musicFile.Name)"
    if ((Get-FileHash -LiteralPath $published).Hash -cne (Get-FileHash -LiteralPath $musicFile.FullName).Hash) {
        throw "Deployed music differs: $($musicFile.Name)"
    }
}
if ($LASTEXITCODE -ne 0 -or ($actual -join "`n").Trim() -ne 'Sin Star I presentations passed.') {
    throw "Presentation regression failed: $actual"
}
Write-Host 'PASS: Nine character entries and three battle simulations create, draw and release their actual assets.'
Write-Host 'PASS: Kael automatically cycles all sixteen demo clips and nine alternating normal/Earth/Water turns, including all six bending casts.'
Write-Host 'PASS: Five deployed MP3s match their sources; only explicit track changes restart playback, and scene shutdown preserves music.'
