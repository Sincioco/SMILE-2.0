[CmdletBinding()]
param([string]$UnityProject = 'D:\Knights of Bits and Bytes')

$ErrorActionPreference = 'Stop'
$scenePath = 'Assets/Scenes/BattleSystem.unity'
$scriptPath = 'Assets/Characters/Sci-Fi Beast01/SciFiBeast01.cs'
$scene = Get-Content -LiteralPath (Join-Path $UnityProject $scenePath) -Raw
$controller = Get-Content -LiteralPath (Join-Path $UnityProject $scriptPath) -Raw
if (-not $controller.Contains('Sound14.Play()') -or
    -not $controller.Contains('Sound13.PlayDelayed(.2f)') -or
    -not $controller.Contains('Sound15.Play()')) {
    throw 'Vrax controller cues changed; review the Unity sound mapping before importing.'
}
$definitions = @(
    @{ Slot = 13; Name = 'swoosh'; Cue = 200 },
    @{ Slot = 14; Name = 'growl'; Cue = 0 },
    @{ Slot = 15; Name = 'death'; Cue = 0 }
)
$originalRoot = Join-Path $PSScriptRoot 'Private/Audio/Originals'
$null = New-Item -ItemType Directory -Force -Path $originalRoot
$sources = @()
$outputs = @()
foreach ($definition in $definitions) {
    $slot = $definition.Slot
    $reference = [regex]::Match($scene, "(?m)^  Sound${slot}: \{fileID: (\d+)\}").Groups[1].Value
    if (-not $reference) { throw "Missing Unity sound slot $slot" }
    $sourceBlock = [regex]::Match($scene, "(?ms)^--- !u!82 &$reference\r?\n.*?(?=^---|\z)").Value
    $guid = [regex]::Match($sourceBlock, 'm_audioClip: \{fileID: \d+, guid: ([a-f0-9]+)').Groups[1].Value
    if (-not $guid) { throw "Missing AudioClip for sound slot $slot" }
    $metadata = @(rg -l --fixed-strings "guid: $guid" (Join-Path $UnityProject 'Assets') -g '*.meta')
    if ($metadata.Count -ne 1) { throw "Expected one source audio file for $guid" }
    $original = $metadata[0].Substring(0, $metadata[0].Length - 5)
    $originalCopy = Join-Path $originalRoot ([IO.Path]::GetFileName($original))
    Copy-Item -LiteralPath $original -Destination $originalCopy -Force
    $sourceHash = (Get-FileHash -LiteralPath $original).Hash.ToLowerInvariant()
    if ((Get-FileHash -LiteralPath $originalCopy).Hash -ine $sourceHash) { throw 'Original audio backup mismatch.' }
    $outputPath = "Private/Audio/vrax-$($definition.Name).wav"
    $output = Join-Path $PSScriptRoot $outputPath
    & ffmpeg -hide_banner -loglevel error -y -i $original -map_metadata -1 -ac 2 -ar 44100 -c:a pcm_s16le $output
    if ($LASTEXITCODE -ne 0) { throw "Audio conversion failed: $original" }
    $sources += [ordered]@{
        slot = $slot
        guid = $guid
        unityAsset = [IO.Path]::GetRelativePath($UnityProject, $original).Replace('\', '/')
        packageOriginal = "Private/Audio/Originals/$([IO.Path]::GetFileName($original))"
        sha256 = $sourceHash
        cueMilliseconds = $definition.Cue
    }
    $outputs += [ordered]@{
        path = $outputPath
        viewerPath = "Assets/Audio/vrax-$($definition.Name).wav"
        sha256 = (Get-FileHash -LiteralPath $output).Hash.ToLowerInvariant()
        unitySoundSlot = $slot
    }
}
$report = [ordered]@{
    sourceProject = 'Knights of Bits and Bytes'
    scene = $scenePath
    script = $scriptPath
    scriptSha256 = (Get-FileHash -LiteralPath (Join-Path $UnityProject $scriptPath)).Hash.ToLowerInvariant()
    sources = $sources
    outputs = $outputs
    conversion = 'Original bytes preserved; playback copies are PCM16 stereo 44100 Hz, with no gain, pitch or waveform remix.'
    runtime = 'Attack through Attack6: growl at clip start, swoosh at 200 ms; Death and Death2: death vocalization at clip start. Separate channels 4 and 5. Clip time follows playback speed; the Unity project is unchanged.'
}
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'unity-audio-import.json')
$manifestPath = Join-Path $PSScriptRoot 'package-manifest.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$manifest | Add-Member -NotePropertyName audio -NotePropertyValue $outputs -Force
$manifest | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $manifestPath
