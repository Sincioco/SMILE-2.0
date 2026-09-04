[CmdletBinding()]
param(
    [ValidateSet('Export', 'Restore', 'Watch')]
    [string]$Mode = 'Export',
    [int]$ViewerProcessId = 0,
    [switch]$AllowMissing,
    [switch]$Force
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
$channelCount = 18

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

function Assert-Triplet($Value, [string]$Label) {
    $items = @($Value)

    if ($items.Count -ne 3) {
        throw "$Label must contain exactly three numeric values."
    }

    $result = [int[]]::new(3)

    for ($index = 0; $index -lt 3; $index++) {
        $number = [double]$items[$index]

        if ($number -ne [Math]::Truncate($number) -or $number -lt -180 -or $number -gt 180) {
            throw "$Label values must be whole numbers from -180 through 180."
        }

        $result[$index] = [int]$number
    }

    return $result
}

function Read-LivePayload() {
    if (-not (Test-Path -LiteralPath $livePath -PathType Leaf)) {
        return $null
    }

    $envelope = [IO.File]::ReadAllBytes($livePath)

    if ($envelope.Length -lt 44 -or
        $envelope[0] -ne 83 -or
        $envelope[1] -ne 77 -or
        $envelope[2] -ne 68 -or
        $envelope[3] -ne 52) {
        throw "Arin v5.7 live calibration has an invalid SMD4 envelope: $livePath"
    }

    $envelopeVersion = [BitConverter]::ToUInt32($envelope, 4)
    $payloadLength = [BitConverter]::ToUInt32($envelope, 8)

    if ($envelopeVersion -ne 1 -or $envelope.Length -ne 44 + $payloadLength) {
        throw "Arin v5.7 live calibration has an unsupported SMD4 envelope."
    }

    $payload = [byte[]]::new($payloadLength)
    [Array]::Copy($envelope, 44, $payload, 0, $payloadLength)
    $expectedDigest = [Convert]::ToHexString($envelope[12..43])
    $actualDigest = [Convert]::ToHexString(
        [Security.Cryptography.SHA256]::HashData($payload)
    )

    if ($actualDigest -cne $expectedDigest) {
        throw "Arin v5.7 live calibration checksum does not match its payload."
    }

    return $payload
}

function Convert-PayloadToSnapshot([byte[]]$Payload) {
    if ($Payload.Length -lt 10 -or
        $Payload[0] -ne 83 -or
        $Payload[1] -ne 77 -or
        $Payload[2] -ne 75 -or
        $Payload[3] -ne 70 -or
        $Payload[4] -ne 1) {
        throw 'Arin v5.7 calibration payload has an invalid SMKF header.'
    }

    $clipCount = [int]$Payload[5]

    if ($clipCount -ne $clipNames.Count) {
        throw "Arin v5.7 calibration must contain exactly $($clipNames.Count) clips."
    }

    $saved = $Payload[6] -ne 0
    $savedClip = [int]$Payload[7]
    $savedFrame = Read-Unsigned16 $Payload 8
    $offset = 10
    $totalKeyframes = 0
    $clips = [Collections.Generic.List[object]]::new()

    for ($clipIndex = 0; $clipIndex -lt $clipCount; $clipIndex++) {
        if ($offset + 2 -gt $Payload.Length) {
            throw 'Arin v5.7 calibration ended before a clip keyframe count.'
        }

        $keyframeCount = Read-Unsigned16 $Payload $offset
        $offset += 2
        $keyframes = [Collections.Generic.List[object]]::new()
        $previousFrame = -1

        if ($keyframeCount -gt 256) {
            throw 'Arin v5.7 calibration exceeds 256 keyframes in one clip.'
        }

        for ($keyframeIndex = 0; $keyframeIndex -lt $keyframeCount; $keyframeIndex++) {
            $required = 2 + $channelCount * 2

            if ($offset + $required -gt $Payload.Length) {
                throw 'Arin v5.7 calibration ended inside a keyframe record.'
            }

            $frame = Read-Unsigned16 $Payload $offset
            $offset += 2

            if ($frame -le $previousFrame) {
                throw 'Arin v5.7 calibration keyframes must be in ascending frame order.'
            }

            $previousFrame = $frame
            $values = [int[]]::new($channelCount)

            for ($channel = 0; $channel -lt $channelCount; $channel++) {
                $encoded = Read-Unsigned16 $Payload $offset
                $offset += 2

                if ($encoded -gt 360) {
                    throw 'Arin v5.7 calibration contains a channel outside -180 through 180.'
                }

                $values[$channel] = $encoded - 180
            }

            $keyframes.Add([ordered]@{
                frame = $frame
                swordWrist = [ordered]@{ rotation = @($values[0], $values[1], $values[2]) }
                shieldWrist = [ordered]@{ rotation = @($values[3], $values[4], $values[5]) }
                sword = [ordered]@{
                    rotation = @($values[6], $values[7], $values[8])
                    position = @($values[12], $values[13], $values[14])
                }
                shield = [ordered]@{
                    rotation = @($values[9], $values[10], $values[11])
                    position = @($values[15], $values[16], $values[17])
                }
            })
        }

        $totalKeyframes += $keyframeCount
        $clips.Add([ordered]@{
            index = $clipIndex
            name = $clipNames[$clipIndex]
            keyframes = $keyframes.ToArray()
        })
    }

    if ($offset -ne $Payload.Length) {
        throw 'Arin v5.7 calibration contains unexpected trailing data.'
    }

    $savedKeyframe = $null

    if ($saved) {
        if ($savedClip -lt 0 -or $savedClip -ge $clipCount) {
            throw 'Arin v5.7 calibration refers to an invalid saved clip.'
        }

        $matchingFrame = @($clips[$savedClip].keyframes | Where-Object frame -eq $savedFrame)

        if ($matchingFrame.Count -ne 1) {
            throw 'Arin v5.7 calibration refers to a saved frame that is not keyed.'
        }

        $savedKeyframe = [ordered]@{
            clipIndex = $savedClip
            clipName = $clipNames[$savedClip]
            frame = $savedFrame
        }
    }

    return [ordered]@{
        schemaVersion = 1
        assetId = 'sin-star-i.character-1.paladin'
        characterVersion = 'v5.7'
        applicationId = $applicationId
        dataKey = $dataKey
        storageVersion = 1
        savedKeyframe = $savedKeyframe
        totalKeyframes = $totalKeyframes
        clips = $clips.ToArray()
    }
}

function Convert-SnapshotToPayload($Snapshot) {
    if ($Snapshot.schemaVersion -ne 1 -or
        $Snapshot.assetId -cne 'sin-star-i.character-1.paladin' -or
        $Snapshot.characterVersion -cne 'v5.7' -or
        $Snapshot.applicationId -cne $applicationId -or
        $Snapshot.dataKey -cne $dataKey -or
        $Snapshot.storageVersion -ne 1) {
        throw 'Arin v5.7 calibration JSON identity or schema is invalid.'
    }

    $clips = @($Snapshot.clips)

    if ($clips.Count -ne $clipNames.Count) {
        throw "Arin v5.7 calibration JSON must contain exactly $($clipNames.Count) clips."
    }

    $bytes = [Collections.Generic.List[byte]]::new()
    $bytes.AddRange([byte[]]@(83, 77, 75, 70, 1, $clipNames.Count))
    $saved = $null -ne $Snapshot.savedKeyframe
    $bytes.Add([byte]$(if ($saved) { 1 } else { 0 }))
    $savedClip = 0
    $savedFrame = 0

    if ($saved) {
        $savedClip = [int]$Snapshot.savedKeyframe.clipIndex
        $savedFrame = [int]$Snapshot.savedKeyframe.frame

        if ($savedClip -lt 0 -or $savedClip -ge $clipNames.Count -or
            $Snapshot.savedKeyframe.clipName -cne $clipNames[$savedClip] -or
            $savedFrame -lt 0 -or $savedFrame -gt 65535) {
            throw 'Arin v5.7 savedKeyframe is invalid.'
        }
    }

    $bytes.Add([byte]$savedClip)
    Add-Unsigned16 $bytes $savedFrame
    $totalKeyframes = 0
    $savedFrameFound = -not $saved

    for ($clipIndex = 0; $clipIndex -lt $clips.Count; $clipIndex++) {
        $clip = $clips[$clipIndex]

        if ($clip.index -ne $clipIndex -or $clip.name -cne $clipNames[$clipIndex]) {
            throw "Arin v5.7 clip $clipIndex identity is invalid."
        }

        $keyframes = @($clip.keyframes)

        if ($keyframes.Count -gt 256) {
            throw "Arin v5.7 clip $($clip.name) exceeds 256 keyframes."
        }

        Add-Unsigned16 $bytes $keyframes.Count
        $previousFrame = -1

        foreach ($keyframe in $keyframes) {
            $frame = [double]$keyframe.frame

            if ($frame -ne [Math]::Truncate($frame) -or
                $frame -le $previousFrame -or
                $frame -gt 65535) {
                throw "Arin v5.7 clip $($clip.name) keyframes must use ascending whole frames."
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

            if ($saved -and $savedClip -eq $clipIndex -and $savedFrame -eq $frame) {
                $savedFrameFound = $true
            }

            $totalKeyframes++
        }
    }

    if (-not $savedFrameFound) {
        throw 'Arin v5.7 savedKeyframe does not identify a keyframe in the JSON.'
    }

    if ($null -ne $Snapshot.totalKeyframes -and
        [int]$Snapshot.totalKeyframes -ne $totalKeyframes) {
        throw 'Arin v5.7 totalKeyframes does not match the clip contents.'
    }

    return $bytes.ToArray()
}

function Write-AtomicText([string]$Path, [string]$Text) {
    $directory = [IO.Path]::GetDirectoryName($Path)
    [IO.Directory]::CreateDirectory($directory) | Out-Null
    $temporary = "$Path.tmp.$PID"
    [IO.File]::WriteAllText($temporary, $Text, [Text.UTF8Encoding]::new($false))
    Move-Item -LiteralPath $temporary -Destination $Path -Force
}

function Export-LiveCalibration([bool]$MissingIsAllowed) {
    $payload = Read-LivePayload

    if ($null -eq $payload) {
        if ($MissingIsAllowed) {
            Write-Host 'No live Arin v5.7 keyframe file exists yet; repository JSON remains unchanged.'

            return $false
        }

        throw 'No live Arin v5.7 keyframe file exists. Save a frame in the editor first.'
    }

    $snapshot = Convert-PayloadToSnapshot $payload
    $json = $snapshot | ConvertTo-Json -Depth 12
    Write-AtomicText $snapshotPath ($json + [Environment]::NewLine)
    Write-Host "Exported $($snapshot.totalKeyframes) Arin v5.7 keyframes to $snapshotPath"

    return $true
}

function Restore-LiveCalibration([bool]$Overwrite) {
    if (-not (Test-Path -LiteralPath $snapshotPath -PathType Leaf)) {
        if ($AllowMissing) {
            Write-Host 'No repository Arin v5.7 calibration JSON exists yet.'

            return $false
        }

        throw "Arin v5.7 calibration JSON is missing: $snapshotPath"
    }

    if ((Test-Path -LiteralPath $livePath -PathType Leaf) -and -not $Overwrite) {
        Write-Host 'Live Arin v5.7 calibration already exists; repository JSON was not restored over it.'

        return $false
    }

    $snapshot = Get-Content -LiteralPath $snapshotPath -Raw | ConvertFrom-Json -Depth 20
    $payload = Convert-SnapshotToPayload $snapshot
    $envelope = [byte[]]::new(44 + $payload.Length)
    $envelope[0] = 83
    $envelope[1] = 77
    $envelope[2] = 68
    $envelope[3] = 52
    [BitConverter]::GetBytes([uint32]1).CopyTo($envelope, 4)
    [BitConverter]::GetBytes([uint32]$payload.Length).CopyTo($envelope, 8)
    [Security.Cryptography.SHA256]::HashData($payload).CopyTo($envelope, 12)
    $payload.CopyTo($envelope, 44)
    $liveDirectory = [IO.Path]::GetDirectoryName($livePath)
    [IO.Directory]::CreateDirectory($liveDirectory) | Out-Null
    $temporary = "$livePath.tmp.$PID"
    [IO.File]::WriteAllBytes($temporary, $envelope)
    Move-Item -LiteralPath $temporary -Destination $livePath -Force
    Write-Host "Restored $($snapshot.totalKeyframes) Arin v5.7 keyframes from repository JSON."

    return $true
}

function Get-LiveSignature() {
    if (-not (Test-Path -LiteralPath $livePath -PathType Leaf)) {
        return ''
    }

    $file = Get-Item -LiteralPath $livePath

    return "$($file.Length):$($file.LastWriteTimeUtc.Ticks)"
}

if ($Mode -eq 'Export') {
    Export-LiveCalibration $AllowMissing.IsPresent | Out-Null
} elseif ($Mode -eq 'Restore') {
    Restore-LiveCalibration $Force.IsPresent | Out-Null
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
            $lastSignature = $signature
            Export-LiveCalibration $true | Out-Null
        }
    }

    if ((Get-LiveSignature) -cne $lastSignature) {
        Export-LiveCalibration $true | Out-Null
    }
}
