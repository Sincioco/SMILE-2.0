[CmdletBinding()]
param(
    [ValidateSet('Validate', 'Export', 'Import', 'Compare', 'Backup', 'Restore', 'Watch')]
    [string]$Mode = 'Export',
    [int]$ViewerProcessId = 0,
    [switch]$AllowMissing,
    [switch]$Force,
    [string]$SourcePath,
    [string]$DestinationPath,
    [string]$DataRoot,
    [switch]$MigrateLegacy,
    [switch]$FunctionsOnly,
    [ValidateSet('Arin', 'Orin')]
    [string]$Character = 'Arin'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$packageRoot = Join-Path $repositoryRoot `
    'games\SinStarI\SourceAssets\Characters\Paladin\ArinV57'
$snapshotPath = Join-Path $packageRoot `
    'Calibration\arin-v5.7-pose-calibration.json'
$applicationId = 'smile.tools.character3d-viewer'
$dataKey = 'CharacterViewerCalibrationKeyframes'
$clipNames = @(
    'BlockImpact',
    'Defend',
    'Hit',
    'Idle',
    'Run',
    'SwordAttack',
    'SwordAttack2',
    'Walk'
)
$channelCount = 20
$profileFile = 'Calibration\arin-v5.7-profile.json'
$modelFile = 'arin-v5.7-idle-equipment-checkpoint.glb'
$descriptorFile = 'ArinV57.sm3d.json'
$cookedRelativePath = 'ArinV57\ArinV57.sm3d'
if ($Character -eq 'Orin') {
    $packageRoot = Join-Path $repositoryRoot 'games\SinStarI\SourceAssets\Characters\Tank\OrinV13'
    $snapshotPath = Join-Path $packageRoot 'Calibration\orin-v1.3-pose-calibration.json'
    $profileFile = 'Calibration\orin-v1.3-profile.json'
    $modelFile = 'orin-v1.3-animation-checkpoint.glb'
    $descriptorFile = 'OrinV13.sm3d.json'
    $cookedRelativePath = 'OrinV13\OrinV13.sm3d'
    $dataKey = 'CharacterViewer.Orin.v1.3.CalibrationKeyframes'
}
$profile = Get-Content -LiteralPath (Join-Path $packageRoot $profileFile) -Raw |
    ConvertFrom-Json -AsHashtable
$profileClips = $profile.clips
$legacyClipNames = $clipNames.Clone()
$clipNames = @($profileClips.name)

function Get-TextSha256([string]$Text) {
    $bytes = [Text.Encoding]::UTF8.GetBytes($Text)
    $digest = [Security.Cryptography.SHA256]::HashData($bytes)

    return [Convert]::ToHexString($digest).ToLowerInvariant()
}

$localAppData = [Environment]::GetFolderPath(
    [Environment+SpecialFolder]::LocalApplicationData
)
$applicationHash = Get-TextSha256 $applicationId
$keyHash = Get-TextSha256 $dataKey
$livePath = Join-Path $localAppData `
    "SMILE 2.0\Games\$applicationHash\Data\$keyHash.bin"

function Assert-ConfinedPath([string]$Path) {
    $resolved = [IO.Path]::GetFullPath($Path)
    $allowedRoots = @($repositoryRoot, (Join-Path $localAppData 'SMILE 2.0'))
    $allowed = $false
    foreach ($root in $allowedRoots) {
        if ($resolved.StartsWith($root.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar,
            [StringComparison]::OrdinalIgnoreCase)) { $allowed = $true }
    }
    if (-not $allowed) { throw "Calibration path must stay under the repository or SMILE application data: $resolved" }
    # Resolve existing ancestors as well: a junction must not escape the allowed roots.
    $ancestor = $resolved
    while ($ancestor -and -not (Test-Path -LiteralPath $ancestor)) { $ancestor = [IO.Path]::GetDirectoryName($ancestor) }
    while ($ancestor) {
        $item = Get-Item -LiteralPath $ancestor -Force
        if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
            throw "Calibration paths cannot traverse a junction or symbolic link: $ancestor"
        }
        $ancestor = [IO.Path]::GetDirectoryName($ancestor)
    }
    return $resolved
}

if ($DataRoot) {
    $livePath = Join-Path (Assert-ConfinedPath $DataRoot) "$keyHash.bin"
}
if ($SourcePath) { $SourcePath = Assert-ConfinedPath $SourcePath }
if ($DestinationPath) { $DestinationPath = Assert-ConfinedPath $DestinationPath }

function Get-ProfileIdentity {
    return [ordered]@{
        version = 1
        modelSha256 = $profile.modelSha256
        descriptorSha256 = $profile.descriptorSha256
        sm3dSha256 = $profile.sm3dSha256
        clipNamesSha256 = Get-TextSha256 ($profileClips.name -join "`n")
        socketNamesSha256 = Get-TextSha256 ($profile.sockets -join "`n")
    }
}

function Get-ProfileFingerprint {
    $identity = Get-ProfileIdentity
    return Get-TextSha256 (($profile.assetId, $profile.characterVersion,
        $identity.modelSha256, $identity.descriptorSha256, $identity.sm3dSha256,
        $identity.clipNamesSha256, $identity.socketNamesSha256) -join "`n")
}

function Assert-ProfileAssets {
    foreach ($pair in @(
        @((Join-Path $packageRoot $modelFile), $profile.modelSha256),
        @((Join-Path $packageRoot $descriptorFile), $profile.descriptorSha256)
    )) {
        if ((Get-FileHash -LiteralPath $pair[0]).Hash -cne $pair[1]) {
            throw "Profile assets changed; an explicit calibration migration is required: $($pair[0])"
        }
    }
    # The cooked mirror is disposable; when present it must match the identity
    # recorded alongside the canonical model/descriptor, never an older build.
    foreach ($configuration in @('Release', 'Debug')) {
        $cookedPath = Join-Path $repositoryRoot "tools\Character3DViewer\bin\$configuration\Assets\Generation2\$cookedRelativePath"
        if (Test-Path -LiteralPath $cookedPath) {
            if ((Get-FileHash -LiteralPath $cookedPath).Hash -cne $profile.sm3dSha256) {
                throw "Cooked profile changed; explicit migration is required: $cookedPath"
            }
        }
    }
}

function Assert-Fields($Object, [string[]]$Required, [string[]]$Optional, [string]$Label) {
    if ($Object -isnot [Collections.IDictionary]) { throw "$Label must be an object." }
    foreach ($field in $Required) {
        if (-not $Object.Contains($field)) { throw "$Label is missing $field." }
    }
    foreach ($field in $Object.Keys) {
        if ($field -cnotin $Required -and $field -cnotin $Optional) { throw "$Label has an unknown field: $field" }
    }
}

function Assert-Integer($Value, [int]$Minimum, [int]$Maximum, [string]$Label) {
    if ($null -eq $Value -or $Value -is [bool] -or $Value -is [string] -or
        $Value -is [Collections.IEnumerable] -or
        [double]$Value -ne [Math]::Truncate([double]$Value) -or
        [double]$Value -lt $Minimum -or [double]$Value -gt $Maximum) {
        throw "$Label must be a whole number from $Minimum through $Maximum."
    }
    return [int]$Value
}

function Read-Unsigned16([byte[]]$Bytes, [int]$Offset) {
    return [int]$Bytes[$Offset] + ([int]$Bytes[$Offset + 1] * 256)
}

function Add-Unsigned16(
    [Collections.Generic.List[byte]]$Bytes,
    [int]$Value
) {
    $Bytes.Add([byte]($Value -band 255))
    $Bytes.Add([byte](($Value -shr 8) -band 255))
}

function Assert-Triplet($Value, [string]$Label, [int]$Limit = 180) {
    $items = @($Value)

    if ($Value -isnot [Collections.IList] -or $items.Count -ne 3) {
        throw "$Label must contain exactly three numeric values."
    }

    $result = [int[]]::new(3)

    for ($index = 0; $index -lt 3; $index++) {
        $result[$index] = Assert-Integer $items[$index] (-$Limit) $Limit $Label
    }

    return $result
}

function Read-SharedBytes([string]$Path) {
    # Watching a save must never hold a delete-denying handle across atomic replace.
    $stream = [IO.FileStream]::new($Path, [IO.FileMode]::Open, [IO.FileAccess]::Read,
        [IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete)
    try {
        if ($stream.Length -gt 8MB) { throw 'Calibration file exceeds bounded size.' }
        $bytes = [byte[]]::new($stream.Length)
        $stream.ReadExactly($bytes, 0, $bytes.Length)
        return ,$bytes
    } finally { $stream.Dispose() }
}

function Read-LivePayload([string]$Path = $livePath) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $null
    }

    $envelope = Read-SharedBytes $Path

    if ($envelope.Length -lt 44 -or
        $envelope[0] -ne 83 -or
        $envelope[1] -ne 77 -or
        $envelope[2] -ne 68 -or
        $envelope[3] -ne 52) {
        throw "Character live calibration has an invalid SMD4 envelope: $livePath"
    }

    $envelopeVersion = [BitConverter]::ToUInt32($envelope, 4)
    $payloadLength = [BitConverter]::ToUInt32($envelope, 8)

    if ($envelopeVersion -ne 1 -or $envelope.Length -ne 44 + $payloadLength) {
        throw "Character live calibration has an unsupported SMD4 envelope."
    }

    $payload = [byte[]]::new($payloadLength)
    [Array]::Copy($envelope, 44, $payload, 0, $payloadLength)
    $expectedDigest = [Convert]::ToHexString($envelope[12..43])
    $actualDigest = [Convert]::ToHexString(
        [Security.Cryptography.SHA256]::HashData($payload)
    )

    if ($actualDigest -cne $expectedDigest) {
        throw "Character live calibration checksum does not match its payload."
    }

    return $payload
}

function Convert-PayloadToSnapshot([byte[]]$Payload) {
    if ($Payload.Length -lt 10 -or
        $Payload[0] -ne 83 -or
        $Payload[1] -ne 77 -or
        $Payload[2] -ne 75 -or
        $Payload[3] -ne 70 -or
        $Payload[4] -notin @(1, 2, 3)) {
        throw 'Character calibration payload has an invalid SMKF header.'
    }

    if ($Character -eq 'Orin' -and $Payload[4] -ne 3) {
        throw 'Orin requires a fingerprinted version-3 calibration payload.'
    }
    $clipCount = [int]$Payload[5]
    $storedChannels = if ($Payload[4] -eq 1) { 18 } else { 20 }

    if ($clipCount -lt 1 -or $clipCount -gt 64) { throw 'Invalid clip count.' }
    if ($Payload[4] -eq 1 -and -not $MigrateLegacy) { throw 'Storage v1 requires -MigrateLegacy.' }
    if ($Payload[4] -lt 3 -and $clipCount -ne $legacyClipNames.Count) { throw 'Invalid legacy clip count.' }
    if ($Payload[6] -gt 1) { throw 'Invalid saved flag.' }

    $saved = $Payload[6] -ne 0
    $savedClip = [int]$Payload[7]
    $savedFrame = Read-Unsigned16 $Payload 8
    $offset = 10
    if ($Payload[4] -eq 3) {
        if ($Payload.Length -lt 74 -or
            [Text.Encoding]::ASCII.GetString($Payload, 10, 64) -cne (Get-ProfileFingerprint)) {
            throw 'Calibration runtime profile fingerprint mismatch; migrate by name before loading.'
        }
        $offset = 74
    }
    $totalKeyframes = 0
    $clips = [Collections.Generic.List[object]]::new()

    for ($clipIndex = 0; $clipIndex -lt $clipCount; $clipIndex++) {
        $clipName = $legacyClipNames[$clipIndex]
        if ($Payload[4] -eq 3) {
            if ($offset -ge $Payload.Length) { throw 'Missing clip name.' }
            $nameLength = [int]$Payload[$offset++]
            if ($nameLength -lt 1 -or $nameLength -gt 128 -or $offset + $nameLength -gt $Payload.Length) {
                throw 'Invalid clip name length.'
            }
            $clipName = [Text.UTF8Encoding]::new($false, $true).GetString($Payload, $offset, $nameLength)
            $offset += $nameLength
        }
        if ($offset + 2 -gt $Payload.Length) {
            throw 'Character calibration ended before a clip keyframe count.'
        }

        $keyframeCount = Read-Unsigned16 $Payload $offset
        $offset += 2
        $keyframes = [Collections.Generic.List[object]]::new()
        $previousFrame = -1

        if ($keyframeCount -gt 256) {
            throw 'Character calibration exceeds 256 keyframes in one clip.'
        }

        for ($keyframeIndex = 0; $keyframeIndex -lt $keyframeCount; $keyframeIndex++) {
            $required = 2 + $storedChannels * 2

            if ($offset + $required -gt $Payload.Length) {
                throw 'Character calibration ended inside a keyframe record.'
            }

            $frame = Read-Unsigned16 $Payload $offset
            $offset += 2

            if ($frame -le $previousFrame) {
                throw 'Character calibration keyframes must be in ascending frame order.'
            }

            $previousFrame = $frame
            $values = [int[]]::new($channelCount)

            for ($channel = 0; $channel -lt $storedChannels; $channel++) {
                $encoded = Read-Unsigned16 $Payload $offset
                $offset += 2

                if ($encoded -gt 360) {
                    throw 'Character calibration contains a channel outside -180 through 180.'
                }

                $values[$channel] = $encoded - 180
                if ($channel -ge 18 -and $values[$channel] -notin @(0, 1)) {
                    throw 'Invalid equipment decoupling flag.'
                }
            }

            $keyframes.Add([ordered]@{
                frame = $frame
                swordWrist = [ordered]@{ rotation = @($values[0], $values[1], $values[2]) }
                shieldWrist = [ordered]@{ rotation = @($values[3], $values[4], $values[5]) }
                sword = [ordered]@{
                    decoupled = $values[18] -ne 0
                    rotation = @($values[6], $values[7], $values[8])
                    position = @($values[12], $values[13], $values[14])
                }
                shield = [ordered]@{
                    decoupled = $values[19] -ne 0
                    rotation = @($values[9], $values[10], $values[11])
                    position = @($values[15], $values[16], $values[17])
                }
            })
        }

        $totalKeyframes += $keyframeCount
        $clips.Add([ordered]@{
            index = $clipIndex
            name = $clipName
            keyframes = $keyframes.ToArray()
        })
    }

    if ($offset -ne $Payload.Length) {
        throw 'Character calibration contains unexpected trailing data.'
    }

    $savedKeyframe = $null

    if ($saved) {
        if ($savedClip -lt 0 -or $savedClip -ge $clipCount) {
            throw 'Character calibration refers to an invalid saved clip.'
        }

        $matchingFrame = @($clips[$savedClip].keyframes | Where-Object frame -eq $savedFrame)

        if ($matchingFrame.Count -ne 1) {
            throw 'Character calibration refers to a saved frame that is not keyed.'
        }

        $savedKeyframe = [ordered]@{
            clipIndex = $savedClip
            clipName = $clips[$savedClip].name
            frame = $savedFrame
        }
    }

    $snapshot = [ordered]@{
        schemaVersion = 2
        assetId = $profile.assetId
        characterVersion = $profile.characterVersion
        applicationId = $applicationId
        dataKey = $dataKey
        storageVersion = 3
        profile = Get-ProfileIdentity
        savedKeyframe = $savedKeyframe
        totalKeyframes = $totalKeyframes
        clips = $clips.ToArray()
    }
    return Normalize-Snapshot $snapshot
}

function Normalize-Snapshot($Snapshot) {
    Assert-Fields $Snapshot @('schemaVersion','assetId','characterVersion','applicationId',
        'dataKey','storageVersion','savedKeyframe','totalKeyframes','clips') @('profile') 'Calibration'
    if ($Snapshot.schemaVersion -notin @(1, 2) -or
        $Snapshot.assetId -cne $profile.assetId -or
        $Snapshot.characterVersion -cne $profile.characterVersion -or
        $Snapshot.applicationId -cne $applicationId -or
        $Snapshot.dataKey -cne $dataKey -or
        $Snapshot.storageVersion -notin @(1, 2, 3)) {
        throw 'Character calibration JSON identity or schema is invalid.'
    }
    if ($Character -eq 'Orin' -and $Snapshot.schemaVersion -ne 2) {
        throw 'Orin requires schema-2 calibration JSON with its own asset identity.'
    }
    $null = Assert-Integer $Snapshot.schemaVersion 1 2 'Schema version'
    $null = Assert-Integer $Snapshot.storageVersion 1 3 'Storage version'
    if (($Snapshot.schemaVersion -eq 2 -and $Snapshot.storageVersion -ne 3) -or
        ($Snapshot.schemaVersion -eq 1 -and $Snapshot.storageVersion -eq 3)) { throw 'Schema/storage version mismatch.' }
    if ($Snapshot.storageVersion -eq 1 -and -not $MigrateLegacy) { throw 'Storage v1 requires -MigrateLegacy.' }
    $identity = Get-ProfileIdentity
    if ($Snapshot.schemaVersion -eq 2 -or $Snapshot.Contains('profile')) {
        Assert-Fields $Snapshot.profile @('version','modelSha256','descriptorSha256','sm3dSha256',
            'clipNamesSha256','socketNamesSha256') @() 'Profile'
        foreach ($field in $identity.Keys) {
            # Clip-name order is a migration hint, never the binding authority.
            if ($field -eq 'clipNamesSha256') {
                if ($Snapshot.profile[$field] -notmatch '^[a-fA-F0-9]{64}$') { throw 'Invalid clip-name hash.' }
            } elseif ($Snapshot.profile[$field] -cne $identity[$field]) {
                throw "Profile $field mismatch; explicit asset migration is required."
            }
        }
    }
    $byName = [Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
    $total = 0
    if ($Snapshot.clips -isnot [Collections.IList]) { throw 'clips must be an array.' }
    foreach ($clip in @($Snapshot.clips)) {
        Assert-Fields $clip @('index','name','keyframes') @() 'Clip'
        $null = Assert-Integer $clip.index -1 63 'Clip index hint'
        if ($clip.name -isnot [string] -or $clip.name -cnotmatch '^[A-Za-z][A-Za-z0-9_]{0,127}$' -or
            $byName.ContainsKey($clip.name)) { throw 'Invalid or duplicate exact clip name.' }
        $runtimeIndex = [Array]::IndexOf($clipNames, $clip.name)
        $frameLimit = if ($runtimeIndex -ge 0) { $profileClips[$runtimeIndex].sampleCount - 1 } else { 65535 }
        if ($clip.keyframes -isnot [Collections.IList] -or @($clip.keyframes).Count -gt 256) {
            throw 'keyframes must be an array with at most 256 entries.'
        }
        $frames = [Collections.Generic.HashSet[int]]::new()
        $keys = [Collections.Generic.List[object]]::new()
        foreach ($key in @($clip.keyframes)) {
            Assert-Fields $key @('frame','swordWrist','shieldWrist','sword','shield') @() 'Keyframe'
            $frame = Assert-Integer $key.frame 0 $frameLimit "$($clip.name) frame"
            if (-not $frames.Add($frame)) { throw 'Duplicate keyframe.' }
            $normalized = [ordered]@{ frame = $frame }
            foreach ($partName in @('swordWrist','shieldWrist','sword','shield')) {
                $part = $key[$partName]
                $equipment = $partName -in @('sword','shield')
                if ($equipment) {
                    $required = @('rotation','position')
                    if ($Snapshot.storageVersion -ne 1) { $required += 'decoupled' }
                    Assert-Fields $part $required @('decoupled') $partName
                } else { Assert-Fields $part @('rotation') @() $partName }
                $outputPart = [ordered]@{}
                if ($equipment) {
                    if ($part.Contains('decoupled') -and $part.decoupled -isnot [bool]) { throw 'Decoupled must be Boolean.' }
                    $outputPart.decoupled = [bool]$part.decoupled
                }
                $outputPart.rotation = @(Assert-Triplet $part.rotation "$partName rotation")
                if ($equipment) { $outputPart.position = @(Assert-Triplet $part.position "$partName position" 100) }
                $normalized[$partName] = $outputPart
            }
            $keys.Add($normalized)
        }
        $total += $keys.Count
        $byName.Add($clip.name, [ordered]@{ index = $runtimeIndex; name = $clip.name;
            keyframes = @($keys.ToArray() | Sort-Object frame) })
    }
    if ($byName.Count -gt 64 -or $byName.Count -eq 0) { throw 'Invalid clip count.' }
    if ((Assert-Integer $Snapshot.totalKeyframes 0 16384 'Total keys') -ne $total) { throw 'totalKeyframes mismatch.' }
    $saved = $null
    if ($null -ne $Snapshot.savedKeyframe) {
        $entry = $Snapshot.savedKeyframe
        Assert-Fields $entry @('clipIndex','clipName','frame') @() 'Saved keyframe'
        $null = Assert-Integer $entry.clipIndex -1 63 'Saved clip index hint'
        $frame = Assert-Integer $entry.frame 0 65535 'Saved frame'
        if ($entry.clipName -isnot [string] -or -not $byName.ContainsKey($entry.clipName) -or
            @($byName[$entry.clipName].keyframes | Where-Object frame -eq $frame).Count -ne 1) {
            throw 'savedKeyframe does not identify an exact clip name and keyed frame.'
        }
        $saved = [ordered]@{ clipIndex = [Array]::IndexOf($clipNames, $entry.clipName);
            clipName = $entry.clipName; frame = $frame }
    }
    $orderedClips = [Collections.Generic.List[object]]::new()
    foreach ($name in $clipNames) {
        if ($byName.ContainsKey($name)) { $orderedClips.Add($byName[$name]) }
        else { $orderedClips.Add([ordered]@{index = [Array]::IndexOf($clipNames,$name); name = $name; keyframes = @()}) }
    }
    foreach ($name in @($byName.Keys | Sort-Object -CaseSensitive)) {
        if ($name -cnotin $clipNames) { $orderedClips.Add($byName[$name]) }
    }
    return [ordered]@{ schemaVersion = 2; assetId = $profile.assetId; characterVersion = $profile.characterVersion;
        applicationId = $applicationId; dataKey = $dataKey; storageVersion = 3; profile = $identity;
        savedKeyframe = $saved; totalKeyframes = $total; clips = $orderedClips.ToArray() }
}

function Convert-SnapshotToPayload($Snapshot) {
    $Snapshot = Normalize-Snapshot $Snapshot

    # Unresolved names stay in the canonical JSON, never bind by a stale number.
    $clips = @($Snapshot.clips | Where-Object index -ge 0)

    if ($clips.Count -ne $clipNames.Count) {
        throw "Character calibration JSON must contain exactly $($clipNames.Count) clips."
    }

    $bytes = [Collections.Generic.List[byte]]::new()
    $bytes.AddRange([byte[]]@(83, 77, 75, 70, 3, $clipNames.Count))
    $saved = $null -ne $Snapshot.savedKeyframe -and $Snapshot.savedKeyframe.clipIndex -ge 0
    $bytes.Add([byte]$(if ($saved) { 1 } else { 0 }))
    $savedClip = 0
    $savedFrame = 0

    if ($saved) {
        $savedClip = [int]$Snapshot.savedKeyframe.clipIndex
        $savedFrame = [int]$Snapshot.savedKeyframe.frame

        if ($savedClip -lt 0 -or $savedClip -ge $clipNames.Count -or
            $Snapshot.savedKeyframe.clipName -cne $clipNames[$savedClip] -or
            $savedFrame -lt 0 -or $savedFrame -gt 65535) {
            throw 'Character savedKeyframe is invalid.'
        }
    }

    $bytes.Add([byte]$savedClip)
    Add-Unsigned16 $bytes $savedFrame
    $bytes.AddRange([Text.Encoding]::ASCII.GetBytes((Get-ProfileFingerprint)))
    $totalKeyframes = 0
    $savedFrameFound = -not $saved

    for ($clipIndex = 0; $clipIndex -lt $clips.Count; $clipIndex++) {
        $clip = $clips[$clipIndex]

        if ($clip.index -ne $clipIndex -or $clip.name -cne $clipNames[$clipIndex]) {
            throw "Character clip $clipIndex identity is invalid."
        }

        $keyframes = @($clip.keyframes)
        $nameBytes = [Text.Encoding]::UTF8.GetBytes($clip.name)
        $bytes.Add([byte]$nameBytes.Length)
        $bytes.AddRange($nameBytes)

        if ($keyframes.Count -gt 256) {
            throw "Character clip $($clip.name) exceeds 256 keyframes."
        }

        Add-Unsigned16 $bytes $keyframes.Count
        $previousFrame = -1

        foreach ($keyframe in $keyframes) {
            $frame = [double]$keyframe.frame

            if ($frame -ne [Math]::Truncate($frame) -or
                $frame -le $previousFrame -or
                $frame -gt 65535) {
                throw "Character clip $($clip.name) keyframes must use ascending whole frames."
            }

            $frame = [int]$frame
            $previousFrame = $frame
            Add-Unsigned16 $bytes $frame
            $values = @(
                Assert-Triplet $keyframe.swordWrist.rotation "$($clip.name) frame $frame sword wrist rotation"
                Assert-Triplet $keyframe.shieldWrist.rotation "$($clip.name) frame $frame shield wrist rotation"
                Assert-Triplet $keyframe.sword.rotation "$($clip.name) frame $frame sword rotation"
                Assert-Triplet $keyframe.shield.rotation "$($clip.name) frame $frame shield rotation"
                Assert-Triplet $keyframe.sword.position "$($clip.name) frame $frame sword position"
                Assert-Triplet $keyframe.shield.position "$($clip.name) frame $frame shield position"
            )

            foreach ($value in $values) {
                Add-Unsigned16 $bytes ([int]$value + 180)
            }

            foreach ($part in @($keyframe.sword, $keyframe.shield)) {
                if ($null -ne $part.decoupled -and $part.decoupled -isnot [bool]) {
                    throw 'Equipment decoupled must be a Boolean.'
                }
                Add-Unsigned16 $bytes (180 + [int][bool]$part.decoupled)
            }

            if ($saved -and $savedClip -eq $clipIndex -and $savedFrame -eq $frame) {
                $savedFrameFound = $true
            }

            $totalKeyframes++
        }
    }

    if (-not $savedFrameFound) {
        throw 'Character savedKeyframe does not identify a keyframe in the JSON.'
    }

    if (($clips.keyframes | Measure-Object).Count -ne $totalKeyframes) {
        throw 'Character totalKeyframes does not match the clip contents.'
    }

    return $bytes.ToArray()
}

function Get-PathHash([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return '' }
    return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData((Read-SharedBytes $Path)))
}

function Assert-UniqueJsonFields($Element) {
    if ($Element.ValueKind -eq [Text.Json.JsonValueKind]::Object) {
        $names = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach ($property in $Element.EnumerateObject()) {
            if (-not $names.Add($property.Name)) { throw "Duplicate JSON field: $($property.Name)" }
            Assert-UniqueJsonFields $property.Value
        }
    } elseif ($Element.ValueKind -eq [Text.Json.JsonValueKind]::Array) {
        foreach ($child in $Element.EnumerateArray()) { Assert-UniqueJsonFields $child }
    }
}

function Read-Snapshot([string]$Path) {
    $text = [Text.UTF8Encoding]::new($false, $true).GetString((Read-SharedBytes $Path))
    if ($text.Length -gt 8MB) { throw 'Calibration JSON exceeds the bounded input size.' }
    $document = [Text.Json.JsonDocument]::Parse($text)
    try { Assert-UniqueJsonFields $document.RootElement } finally { $document.Dispose() }
    return Normalize-Snapshot ($text | ConvertFrom-Json -AsHashtable -Depth 24)
}

function Write-AtomicBytes([string]$Path, [byte[]]$Bytes, [string]$ExpectedHash) {
    $Path = Assert-ConfinedPath $Path
    if ((Get-PathHash $Path) -cne $ExpectedHash) { throw "Concurrent calibration change; no overwrite: $Path" }
    if ($ExpectedHash -ceq [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($Bytes))) { return }
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($Path)) | Out-Null
    $temporary = "$Path.tmp.$([Guid]::NewGuid().ToString('N'))"
    $stream = $null
    try {
        $stream = [IO.FileStream]::new($temporary, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
        $stream.Write($Bytes, 0, $Bytes.Length)
        $stream.Flush($true)
        $stream.Dispose()
        $stream = $null
        if ((Get-PathHash $Path) -cne $ExpectedHash) { throw "Concurrent calibration change; no overwrite: $Path" }
        if ($ExpectedHash) {
            # Same-volume atomic replacement retains the exact previous bytes.
            [IO.File]::Replace($temporary, $Path, "$Path.bak", $false)
        } else { [IO.File]::Move($temporary, $Path) }
    } finally {
        if ($stream) { $stream.Dispose() }
        if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary }
    }
}

function Write-Snapshot([string]$Path, $Snapshot, [string]$ExpectedHash) {
    $normalized = Normalize-Snapshot $Snapshot
    $json = ($normalized | ConvertTo-Json -Depth 16).Replace("`r`n", "`n") + "`n"
    # Validate the actual serialized representation, not only the input object.
    $null = Normalize-Snapshot ($json | ConvertFrom-Json -AsHashtable -Depth 24)
    Write-AtomicBytes $Path ([Text.UTF8Encoding]::new($false).GetBytes($json)) $ExpectedHash
}

function Show-SnapshotSummary([string]$Label, $Snapshot) {
    Write-Host "$Label keys=$($Snapshot.totalKeyframes), schema=$($Snapshot.schemaVersion), storage=$($Snapshot.storageVersion)"
    foreach ($clip in $Snapshot.clips) {
        $state = if ($clip.index -lt 0) { 'UNRESOLVED - retained, not applied' } else { "index $($clip.index)" }
        Write-Host "  $($clip.name): [$($clip.keyframes.frame -join ', ')] ($state)"
    }
}

function Export-LiveCalibration([bool]$MissingIsAllowed) {
    $source = if ($SourcePath) { $SourcePath } else { $livePath }
    $destination = if ($DestinationPath) { $DestinationPath } else { $snapshotPath }
    $previousHash = Get-PathHash $destination
    $payload = Read-LivePayload $source

    if ($null -eq $payload) {
        if ($MissingIsAllowed) {
            Write-Host 'No live Character keyframe file exists yet; repository JSON remains unchanged.'

            return $false
        }

        throw 'No live Character keyframe file exists. Save a frame in the editor first.'
    }

    $snapshot = Convert-PayloadToSnapshot $payload
    if ($previousHash) {
        $previous = Read-Snapshot $destination
        # The runtime cannot apply missing clips, but exports must not erase them.
        $unresolved = @($previous.clips | Where-Object { $_.name -cnotin $clipNames })
        $snapshot.clips = @($snapshot.clips) + $unresolved
        foreach ($entry in $unresolved) { $snapshot.totalKeyframes += @($entry.keyframes).Count }
    }
    Write-Snapshot $destination $snapshot $previousHash
    Write-Host "Exported $($snapshot.totalKeyframes) keys: $destination (SHA-256 $(Get-PathHash $destination))"

    return $true
}

function Restore-LiveCalibration([bool]$Overwrite) {
    $source = if ($SourcePath) { $SourcePath } else { $snapshotPath }
    $destination = if ($DestinationPath) { $DestinationPath } else { $livePath }
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
        if ($AllowMissing) {
            Write-Host 'No repository Character calibration JSON exists yet.'

            return $false
        }

        throw "Character calibration JSON is missing: $snapshotPath"
    }

    # A normal restore validates both files. An explicit forced migration may
    # replace a runtime payload bound to the preceding asset fingerprint.
    $snapshot = Read-Snapshot $source
    $previousHash = Get-PathHash $destination
    if ($previousHash -and -not $Overwrite) {
        $null = Convert-PayloadToSnapshot (Read-LivePayload $destination)
    }
    if ($previousHash -and -not $Overwrite) {
        Write-Host 'Live Character calibration already exists; repository JSON was not restored over it.'

        return $false
    }

    $payload = Convert-SnapshotToPayload $snapshot
    $null = Convert-PayloadToSnapshot $payload
    $envelope = [byte[]]::new(44 + $payload.Length)
    $envelope[0] = 83
    $envelope[1] = 77
    $envelope[2] = 68
    $envelope[3] = 52
    [BitConverter]::GetBytes([uint32]1).CopyTo($envelope, 4)
    [BitConverter]::GetBytes([uint32]$payload.Length).CopyTo($envelope, 8)
    [Security.Cryptography.SHA256]::HashData($payload).CopyTo($envelope, 12)
    $payload.CopyTo($envelope, 44)
    Write-AtomicBytes $destination $envelope $previousHash
    Write-Host "Restored $($snapshot.totalKeyframes) Character keyframes from repository JSON."

    return $true
}

function Get-LiveSignature() {
    if (-not (Test-Path -LiteralPath $livePath -PathType Leaf)) {
        return ''
    }

    $file = Get-Item -LiteralPath $livePath

    return "$($file.Length):$($file.LastWriteTimeUtc.Ticks)"
}

if ($FunctionsOnly) { return }

Assert-ProfileAssets
if ($Mode -eq 'Validate') {
    $source = if ($SourcePath) { $SourcePath } else { $snapshotPath }
    $snapshot = Read-Snapshot $source
    Show-SnapshotSummary $source $snapshot
    Write-Host "SHA-256 $(Get-PathHash $source); profile $(Get-ProfileFingerprint)"
} elseif ($Mode -eq 'Compare') {
    $source = if ($SourcePath) { $SourcePath } else { $snapshotPath }
    $destination = if ($DestinationPath) { $DestinationPath } else { $livePath }
    $snapshot = Read-Snapshot $source
    $live = Convert-PayloadToSnapshot (Read-LivePayload $destination)
    Show-SnapshotSummary 'Canonical' $snapshot
    Show-SnapshotSummary 'Live' $live
    Write-Host "Source SHA-256 $(Get-PathHash $source); destination SHA-256 $(Get-PathHash $destination)"
    $canonicalJson = $snapshot | ConvertTo-Json -Depth 16
    $liveJson = $live | ConvertTo-Json -Depth 16
    if ($canonicalJson -cne $liveJson) {
        Compare-Object ($canonicalJson -split "`n") ($liveJson -split "`n") | Format-Table -AutoSize
        exit 2
    }
    Write-Host 'No calibration differences.'
} elseif ($Mode -eq 'Backup') {
    $source = if ($SourcePath) { $SourcePath } else { $snapshotPath }
    $null = Read-Snapshot $source
    $destination = if ($DestinationPath) { $DestinationPath } else { "$source.$(Get-PathHash $source).bak" }
    if (Test-Path -LiteralPath $destination) { throw 'Backup already exists; no overwrite.' }
    Write-AtomicBytes $destination ([IO.File]::ReadAllBytes($source)) ''
    Write-Host "Exact backup: $destination; SHA-256 $(Get-PathHash $destination)"
} elseif ($Mode -eq 'Export') {
    Export-LiveCalibration $AllowMissing.IsPresent | Out-Null
} elseif ($Mode -in @('Import','Restore')) {
    # Restore without explicit paths retains the existing launcher contract.
    # Explicit JSON destination restores a chosen backup to the canonical source.
    if ($DestinationPath -and [IO.Path]::GetExtension($DestinationPath) -eq '.json') {
        if (-not $SourcePath) { throw 'JSON Restore requires -SourcePath.' }
        $snapshot = Read-Snapshot $SourcePath
        $previousHash = Get-PathHash $DestinationPath
        if ($previousHash -and -not $Force) { throw 'Use -Force to replace an existing JSON after Compare/Backup.' }
        if ($previousHash) { $null = Read-Snapshot $DestinationPath }
        Write-Snapshot $DestinationPath $snapshot $previousHash
    } else {
    Restore-LiveCalibration $Force.IsPresent | Out-Null
    }
} else {
    if ($ViewerProcessId -le 0) {
        throw 'Watch mode requires -ViewerProcessId.'
    }

    $lastSignature = Get-LiveSignature
    Export-LiveCalibration $true | Out-Null

    while ($null -ne (Get-Process -Id $ViewerProcessId -ErrorAction SilentlyContinue)) {
        Start-Sleep -Milliseconds 250
        $signature = Get-LiveSignature

        if ($signature -cne $lastSignature) {
            try {
                Export-LiveCalibration $true | Out-Null
                $lastSignature = $signature
            } catch {
                # Preserve the first failed save and retry only on a new live revision.
                Write-Warning $_.Exception.Message
                $lastSignature = $signature
            }
        }
    }

    if ((Get-LiveSignature) -cne $lastSignature) {
        Export-LiveCalibration $true | Out-Null
    }
}
