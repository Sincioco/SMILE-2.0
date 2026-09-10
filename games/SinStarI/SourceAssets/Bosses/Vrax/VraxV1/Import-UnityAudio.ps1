[CmdletBinding()]
param(
    [string]$UnityProject = 'D:\Knights of Bits and Bytes',
    [string]$PackageRoot = $PSScriptRoot,
    [string]$ConverterPath = 'ffmpeg',
    [switch]$FunctionsOnly
)
$ErrorActionPreference = 'Stop'

function Get-AudioHash([string]$Path) {
    if (Test-Path -LiteralPath $Path -PathType Leaf) { return (Get-FileHash -LiteralPath $Path).Hash.ToLowerInvariant() }
    return ''
}

function Get-PackageAudioPath([string]$Relative) {
    $root = [IO.Path]::GetFullPath($PackageRoot).TrimEnd([char[]]'\/')
    $path = [IO.Path]::GetFullPath((Join-Path $root $Relative))
    if ($Relative.Contains(':') -or -not $path.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Audio package path escapes its root: $Relative"
    }
    for ($check = $path; $check; $check = [IO.Path]::GetDirectoryName($check)) {
        if ((Test-Path -LiteralPath $check) -and ((Get-Item -LiteralPath $check).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw "Audio publication cannot follow a redirected path: $check"
        }
    }
    return $path
}

function Get-UnityReference([string]$Block, [string]$Name) {
    $refs = [regex]::Matches($Block, "(?m)^  ${Name}: \{fileID: (\d+)\}")
    if ($refs.Count -ne 1 -or $refs[0].Groups[1].Value -eq '0') { throw "Missing or ambiguous Unity reference: $Name" }
    return $refs[0].Groups[1].Value
}

function Assert-PlaybackWave([string]$Path) {
    $reader = [IO.BinaryReader]::new([IO.File]::OpenRead($Path))
    try {
        $length = $reader.BaseStream.Length
        if ($length -lt 44 -or $length -gt 128MB -or [Text.Encoding]::ASCII.GetString($reader.ReadBytes(4)) -ne 'RIFF') { throw 'Invalid playback WAV header.' }
        if ($reader.ReadUInt32() + 8 -ne $length -or [Text.Encoding]::ASCII.GetString($reader.ReadBytes(4)) -ne 'WAVE') { throw 'Invalid playback WAV length.' }
        $format = $false; $data = $false
        while ($reader.BaseStream.Position + 8 -le $length) {
            $id = [Text.Encoding]::ASCII.GetString($reader.ReadBytes(4))
            $size = $reader.ReadUInt32(); $start = $reader.BaseStream.Position
            if ($start + $size -gt $length) { throw 'Truncated playback WAV chunk.' }
            if ($id -eq 'fmt ') {
                if ($size -lt 16 -or $reader.ReadUInt16() -ne 1 -or $reader.ReadUInt16() -ne 2 -or
                    $reader.ReadUInt32() -ne 44100 -or $reader.ReadUInt32() -ne 176400 -or
                    $reader.ReadUInt16() -ne 4 -or $reader.ReadUInt16() -ne 16) { throw 'Playback must be PCM16 stereo 44100 Hz.' }
                $format = $true
            }
            if ($id -eq 'data') { $data = $size -gt 0 -and $size % 4 -eq 0 }
            $reader.BaseStream.Position = $start + $size + ($size % 2)
        }
        if (-not $format -or -not $data) { throw 'Playback WAV has no valid format or samples.' }
    } finally { $reader.Dispose() }
}

function Convert-VraxAudio([string]$Source, [string]$Destination, [string]$Converter) {
    & $Converter -hide_banner -loglevel error -y -i $Source -map_metadata -1 -ac 2 -ar 44100 -c:a pcm_s16le $Destination
    if ($LASTEXITCODE -ne 0) { throw "Audio conversion failed: $Source" }
}

function Write-AudioMetadata([string]$Path, $Value) {
    [IO.File]::WriteAllText($Path, ($Value | ConvertTo-Json -Depth 30) + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
    $null = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
}

function Publish-AudioFile([string]$Staged, [string]$Destination, [string]$Backup) {
    if (Test-Path -LiteralPath $Destination) { [IO.File]::Replace($Staged, $Destination, $Backup) }
    else { [IO.File]::Move($Staged, $Destination) }
}

function Invoke-VraxAudioImport {
    $manifestPath = Get-PackageAudioPath 'package-manifest.json'
    $reportPath = Get-PackageAudioPath 'unity-audio-import.json'
    $manifestHash = Get-AudioHash $manifestPath; $reportHash = Get-AudioHash $reportPath
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    $previous = if ($reportHash) { Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json } else { $null }
    $converter = (Get-Command $ConverterPath -CommandType Application -ErrorAction Stop).Source
    $rg = (Get-Command rg -CommandType Application -ErrorAction Stop).Source
    $scenePath = 'Assets/Scenes/BattleSystem.unity'
    $scriptPath = 'Assets/Characters/Sci-Fi Beast01/SciFiBeast01.cs'
    $controllerPath = Join-Path $UnityProject $scriptPath
    $controller = Get-Content -LiteralPath $controllerPath -Raw
    foreach ($cue in @('gameManager.VFX.Sound14.Play()', 'gameManager.VFX.Sound13.PlayDelayed(.2f)', 'gameManager.VFX.Sound15.Play()')) {
        if (-not $controller.Contains($cue)) { throw 'Vrax controller cues changed; review the Unity sound mapping before importing.' }
    }
    $scriptGuid = [regex]::Match((Get-Content -LiteralPath "$controllerPath.meta" -Raw), '(?m)^guid: ([a-f0-9]{32})\r?$').Groups[1].Value
    if (-not $scriptGuid) { throw 'Missing Vrax controller GUID.' }
    $scene = Get-Content -LiteralPath (Join-Path $UnityProject $scenePath) -Raw
    $blocks = @{}
    foreach ($block in [regex]::Matches($scene, '(?ms)^--- !u!\d+ &(\d+)\r?\n.*?(?=^---|\z)')) {
        $id = $block.Groups[1].Value
        if ($blocks.ContainsKey($id)) { throw "Duplicate scene object: $id" }
        $blocks[$id] = $block.Value
    }
    $owners = @($blocks.Values | Where-Object { $_ -match "(?m)^  m_Script: \{fileID: \d+, guid: $scriptGuid," })
    if ($owners.Count -ne 1) { throw 'Expected exactly one Vrax controller in the battle scene.' }
    $manager = Get-UnityReference $owners[0] 'gameManager'
    $vfx = Get-UnityReference $blocks[$manager] 'VFX'
    $definitions = @(@{ Slot = 13; Name = 'swoosh'; Cue = 200 }, @{ Slot = 14; Name = 'growl'; Cue = 0 }, @{ Slot = 15; Name = 'death'; Cue = 0 })
    $inputs = @(); $destinations = @{}
    foreach ($definition in $definitions) {
        $reference = Get-UnityReference $blocks[$vfx] "Sound$($definition.Slot)"
        $sourceBlock = $blocks[$reference]
        if ($sourceBlock -notmatch '^--- !u!82 ') { throw "Not an AudioSource: $reference" }
        $clips = [regex]::Matches($sourceBlock, 'm_audioClip: \{fileID: \d+, guid: ([a-f0-9]{32}),')
        if ($clips.Count -ne 1) { throw "Missing or ambiguous AudioClip for sound slot $($definition.Slot)" }
        $guid = $clips[0].Groups[1].Value
        $metadata = @(& $rg -l -g '*.meta' -- ("^guid: " + $guid + '\r?$') (Join-Path $UnityProject 'Assets'))
        if ($LASTEXITCODE -gt 1 -or $metadata.Count -ne 1) { throw "Expected one source audio file for $guid" }
        $source = $metadata[0].Substring(0, $metadata[0].Length - 5)
        $hash = Get-AudioHash $source
        if (-not $hash) { throw "Missing audio source: $source" }
        $original = "Private/Audio/Originals/$guid-$($hash.Substring(0, 12))-$([IO.Path]::GetFileName($source))"
        foreach ($prior in @($previous.sources)) {
            if ($prior -and $prior.guid -eq $guid -and $prior.sha256 -eq $hash -and
                (Get-AudioHash (Get-PackageAudioPath $prior.packageOriginal)) -eq $hash) { $original = $prior.packageOriginal; break }
        }
        $playback = "Private/Audio/vrax-$($definition.Name).wav"
        foreach ($relative in @($original, $playback)) {
            $destination = Get-PackageAudioPath $relative
            if ($destinations.ContainsKey($destination)) { throw "Colliding audio destination: $relative" }
            $destinations[$destination] = Get-AudioHash $destination
        }
        $inputs += @{ Definition = $definition; Source = $source; Hash = $hash; Guid = $guid; Original = $original; Playback = $playback }
    }
    $audioRoot = Get-PackageAudioPath 'Private/Audio'
    if (Test-Path -LiteralPath $audioRoot) {
        foreach ($folder in Get-ChildItem -LiteralPath $audioRoot -Directory -Filter '.import-*') {
            if (Test-Path -LiteralPath (Join-Path $folder.FullName 'commit.json')) { throw "An interrupted import needs recovery before retry: $($folder.FullName)" }
        }
    }
    $stageRelative = 'Private/Audio/.import-' + [Guid]::NewGuid().ToString('N')
    $stage = Get-PackageAudioPath $stageRelative
    $null = New-Item -ItemType Directory -Path $stage
    $entries = @(); $attempted = @(); $keepRecovery = $false; $locked = $false; $mutex = $null
    try {
        $sources = @(); $outputs = @()
        foreach ($sourceInput in $inputs) {
            $slot = $sourceInput.Definition.Slot
            $copy = Join-Path $stage "original-$slot"; $wave = Join-Path $stage "playback-$slot.wav"
            Copy-Item -LiteralPath $sourceInput.Source -Destination $copy
            if ((Get-AudioHash $copy) -ne $sourceInput.Hash) { throw 'Source audio changed while taking its snapshot.' }
            Convert-VraxAudio $copy $wave $converter
            Assert-PlaybackWave $wave
            foreach ($pair in @(@($copy, $sourceInput.Original), @($wave, $sourceInput.Playback))) {
                $destination = Get-PackageAudioPath $pair[1]
                $entries += @{ Staged = $pair[0]; Destination = $destination; Before = $destinations[$destination]; After = Get-AudioHash $pair[0]; Backup = Join-Path $stage "backup-$($entries.Count)" }
            }
            $sources += [ordered]@{ slot = $slot; guid = $sourceInput.Guid; unityAsset = [IO.Path]::GetRelativePath($UnityProject, $sourceInput.Source).Replace('\', '/'); packageOriginal = $sourceInput.Original; sha256 = $sourceInput.Hash; cueMilliseconds = $sourceInput.Definition.Cue }
            $outputs += [ordered]@{ path = $sourceInput.Playback; viewerPath = "Assets/Audio/vrax-$($sourceInput.Definition.Name).wav"; sha256 = Get-AudioHash $wave; unitySoundSlot = $slot }
        }
        $report = [ordered]@{
            sourceProject = 'Knights of Bits and Bytes'; scene = $scenePath; script = $scriptPath; scriptSha256 = Get-AudioHash $controllerPath
            sources = $sources; outputs = $outputs
            conversion = 'Original bytes preserved; playback copies are PCM16 stereo 44100 Hz, with no gain, pitch or waveform remix.'
            runtime = 'Attack through Attack6: growl at clip start, swoosh at 200 ms; Death and Death2: death vocalization at clip start. Separate channels 4 and 5. Clip time follows playback speed; the Unity project is unchanged.'
        }
        $manifest | Add-Member -NotePropertyName audio -NotePropertyValue $outputs -Force
        Write-AudioMetadata (Join-Path $stage 'report.json') $report
        Write-AudioMetadata (Join-Path $stage 'manifest.json') $manifest
        $entries += @{ Staged = Join-Path $stage 'report.json'; Destination = $reportPath; Before = $reportHash; After = Get-AudioHash (Join-Path $stage 'report.json'); Backup = Join-Path $stage 'backup-report' }
        $entries += @{ Staged = Join-Path $stage 'manifest.json'; Destination = $manifestPath; Before = $manifestHash; After = Get-AudioHash (Join-Path $stage 'manifest.json'); Backup = Join-Path $stage 'backup-manifest' }
        $identity = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes([IO.Path]::GetFullPath($PackageRoot).ToLowerInvariant())))
        $mutex = [Threading.Mutex]::new($false, "Local\SmileVraxAudio-$identity")
        try { $locked = $mutex.WaitOne(0) } catch [Threading.AbandonedMutexException] { $locked = $true }
        if (-not $locked) { throw 'Another audio import is publishing this package. Retry after it completes.' }
        foreach ($entry in $entries) {
            if ((Get-AudioHash $entry.Destination) -ne $entry.Before) { throw "Package changed during import; refusing to overwrite: $($entry.Destination)" }
        }
        $changes = @($entries | Where-Object { $_.Before -ne $_.After })
        Write-AudioMetadata (Join-Path $stage 'commit.json') @{ entries = $changes; policy = 'Replace files in order; roll back in reverse. Preserve this folder if rollback fails.' }
        foreach ($entry in $changes) {
            $null = New-Item -ItemType Directory -Force -Path ([IO.Path]::GetDirectoryName($entry.Destination))
            $attempted += $entry
            Publish-AudioFile $entry.Staged $entry.Destination $entry.Backup
        }
        foreach ($entry in $entries) {
            if ((Get-AudioHash $entry.Destination) -ne $entry.After) { throw "Published audio verification failed: $($entry.Destination)" }
        }
        Write-Output "Vrax audio import complete; $($changes.Count) files changed."
    } catch {
        $failure = $_
        for ($index = $attempted.Count - 1; $index -ge 0; $index--) {
            $entry = $attempted[$index]
            try {
                $current = Get-AudioHash $entry.Destination
                if ($current -eq $entry.Before) { continue }
                if ($current -ne $entry.After) { throw 'A concurrent edit prevents safe rollback.' }
                if ($entry.Before) { [IO.File]::Replace($entry.Backup, $entry.Destination, $entry.Backup + '-rejected') }
                else { [IO.File]::Delete($entry.Destination) }
                if ((Get-AudioHash $entry.Destination) -ne $entry.Before) { throw 'Rollback hash mismatch.' }
            } catch { $keepRecovery = $true; Write-Warning "Rollback needs recovery: $($entry.Destination): $_" }
        }
        if ($keepRecovery) { throw "Import failed and recovery data was retained at $stage. Original failure: $failure" }
        throw $failure
    } finally {
        if ($locked) { $mutex.ReleaseMutex() }
        if ($mutex) { $mutex.Dispose() }
        if (-not $keepRecovery) { Remove-Item -LiteralPath (Get-PackageAudioPath $stageRelative) -Recurse -Force }
    }
}

if (-not $FunctionsOnly) { Invoke-VraxAudioImport }
