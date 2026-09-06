[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$syncScript = Join-Path $PSScriptRoot 'sync-arin-v5-7-calibration.ps1'
$launchScript = Join-Path $repositoryRoot 'tools\Character3DViewer\Launch.ps1'
$fixtureRoot = Join-Path $repositoryRoot `
    "artifacts\temp\CharacterViewerPreservation-$([Guid]::NewGuid().ToString('N'))"
[IO.Directory]::CreateDirectory($fixtureRoot) | Out-Null

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function Assert-Throws([scriptblock]$Action, [string]$Message) {
    try {
        & $Action
    } catch {
        return $_.Exception.Message
    }

    throw $Message
}

function New-LiveEnvelope([byte[]]$Payload) {
    $envelope = [byte[]]::new(44 + $Payload.Length)
    $envelope[0] = 83
    $envelope[1] = 77
    $envelope[2] = 68
    $envelope[3] = 52
    [BitConverter]::GetBytes([uint32]1).CopyTo($envelope, 4)
    [BitConverter]::GetBytes([uint32]$Payload.Length).CopyTo($envelope, 8)
    [Security.Cryptography.SHA256]::HashData($Payload).CopyTo($envelope, 12)
    $Payload.CopyTo($envelope, 44)

    return $envelope
}

function Set-LiveFiles([byte[]]$Primary, [byte[]]$Backup) {
    foreach ($path in @($livePath, "$livePath.bak")) {
        if (Test-Path -LiteralPath $path -PathType Leaf) {
            [IO.File]::Delete($path)
        } elseif (Test-Path -LiteralPath $path -PathType Container) {
            [IO.Directory]::Delete($path)
        }
    }

    if ($null -ne $Primary) { [IO.File]::WriteAllBytes($livePath, $Primary) }
    if ($null -ne $Backup) { [IO.File]::WriteAllBytes("$livePath.bak", $Backup) }
}

function Start-HiddenPowerShell([string]$Command, [string]$OutputBase) {
    $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($Command))
    $shell = (Get-Command pwsh.exe -ErrorAction Stop).Source

    return Start-Process -FilePath $shell -ArgumentList '-NoProfile', '-EncodedCommand', $encoded `
        -WindowStyle Hidden -RedirectStandardOutput "$OutputBase.out" `
        -RedirectStandardError "$OutputBase.err" -PassThru
}

$dataRoot = Join-Path $fixtureRoot 'Data'
[IO.Directory]::CreateDirectory($dataRoot) | Out-Null
. $syncScript -Character Arin -FunctionsOnly -DataRoot $dataRoot
Assert-CanonicalProfileAssets
$canonical = Read-Snapshot $snapshotPath
$canonicalPayload = Convert-SnapshotToPayload $canonical
$validEnvelope = New-LiveEnvelope $canonicalPayload
$corruptEnvelope = [byte[]]@(1, 2, 3, 4)
$orinEnvelopeBase64 = & {
    . $syncScript -Character Orin -FunctionsOnly -DataRoot (Join-Path $fixtureRoot 'OrinData')
    $orinSnapshot = Read-Snapshot $snapshotPath
    $orinPayload = Convert-SnapshotToPayload $orinSnapshot

    return [Convert]::ToBase64String((New-LiveEnvelope $orinPayload))
}
$orinEnvelope = [Convert]::FromBase64String($orinEnvelopeBase64)

Set-LiveFiles $validEnvelope $corruptEnvelope
$usable = Select-UsableLiveCalibration
Assert-True ($usable.Source -ceq 'Primary') 'A valid primary was not preferred over its backup.'

Set-LiveFiles $corruptEnvelope $validEnvelope
$usable = Select-UsableLiveCalibration
Assert-True ($usable.Source -ceq 'Backup') 'A corrupt primary did not select the valid previous-good backup.'

Set-LiveFiles $null $validEnvelope
$backupHash = Get-PathHash "$livePath.bak"
$usable = Select-UsableLiveCalibration
Assert-True ($usable.Source -ceq 'Backup') 'A missing primary did not select the valid previous-good backup.'
$restored = Restore-LiveCalibration $false
Assert-True (-not $restored -and -not (Test-Path -LiteralPath $livePath) -and
    (Get-PathHash "$livePath.bak") -ceq $backupHash) `
    'Normal restore replaced or changed a usable previous-good backup.'

$DestinationPath = Join-Path $fixtureRoot 'export-from-backup.json'
$SourcePath = $null
$null = Export-LiveCalibration $false
$exported = Read-Snapshot $DestinationPath
Assert-True (($exported | ConvertTo-Json -Depth 24 -Compress) -ceq
    ($canonical | ConvertTo-Json -Depth 24 -Compress)) `
    'Export from a previous-good backup changed the calibration payload.'

Set-LiveFiles $corruptEnvelope $validEnvelope
$rejectedHash = Get-PathHash $livePath
$backupHash = Get-PathHash "$livePath.bak"
$SourcePath = $snapshotPath
$DestinationPath = $livePath
$null = Restore-LiveCalibration $true
Assert-True ((Get-PathHash "$livePath.bak") -ceq $backupHash -and
    (Get-PathHash "$livePath.rejected.$($rejectedHash.ToLowerInvariant())") -ceq $rejectedHash) `
    'Forced recovery did not preserve the previous-good backup and rejected primary evidence.'
$SourcePath = $null
$DestinationPath = $null

Set-LiveFiles $corruptEnvelope $orinEnvelope
$message = Assert-Throws { Select-UsableLiveCalibration } `
    'A wrong-character backup was accepted.'
Assert-True ($message.Contains('No usable Character calibration exists.')) `
    'Wrong-character backup failure did not retain both candidate diagnostics.'

Set-LiveFiles $corruptEnvelope $corruptEnvelope
$null = Assert-Throws { Select-UsableLiveCalibration } 'Two invalid envelopes were accepted.'

Set-LiveFiles $validEnvelope $null
$locked = [IO.File]::Open($livePath, [IO.FileMode]::Open, [IO.FileAccess]::Read,
    [IO.FileShare]::None)
try {
    $null = Assert-Throws { Select-UsableLiveCalibration } `
        'A sharing denial was treated as an absent first-run save.'
} finally { $locked.Dispose() }

Set-LiveFiles $null $null
Assert-True ($null -eq (Select-UsableLiveCalibration)) `
    'A genuine first run did not remain distinct from invalid or denied data.'

$publicationRoot = Join-Path $fixtureRoot 'Publication'
$publicationAsset = Join-Path $publicationRoot 'Assets\Generation2\ArinV57\ArinV57.sm3d'
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($publicationAsset)) | Out-Null
[IO.File]::WriteAllText($publicationAsset, 'stale')
Assert-CanonicalProfileAssets
$null = Assert-Throws { Assert-PublishedProfileAsset $publicationRoot } `
    'A stale selected publication passed post-build validation.'
[IO.File]::Delete($publicationAsset)
$knownGoodCooked = Join-Path $repositoryRoot `
    'tools\Character3DViewer\bin\Release\Assets\Generation2\ArinV57\ArinV57.sm3d'
[IO.File]::Copy($knownGoodCooked, $publicationAsset)
Assert-PublishedProfileAsset $publicationRoot

. $launchScript -FunctionsOnly
$standardExecutable = [IO.Path]::GetFullPath(
    (Join-Path $repositoryRoot 'tools\Character3DViewer\bin\Release\Character3DViewer.exe')
)
Assert-True (Test-ViewerProcessOwned 'Character3DViewer' $standardExecutable $standardExecutable) `
    'The intended Viewer executable was not recognized.'
Assert-True (-not (Test-ViewerProcessOwned 'chrome' `
    (Join-Path $env:ProgramFiles 'Google\Chrome\Application\chrome.exe') $standardExecutable)) `
    'An unrelated browser path was accepted as the Viewer.'
Assert-True (-not (Test-ViewerProcessOwned 'Character3DViewer-copy' `
    (Get-Command pwsh.exe).Source $standardExecutable)) `
    'An unrelated same-name executable outside the tool root was accepted.'
$missingCustom = Join-Path $fixtureRoot 'missing-viewer.exe'
$null = Assert-Throws {
    Assert-ViewerLaunchPrerequisites $missingCustom $standardExecutable $false
} 'A missing custom executable passed preflight.'

$slowProcess = $null
try {
    $slowProcess = Start-HiddenPowerShell 'Start-Sleep -Seconds 30' `
        (Join-Path $fixtureRoot 'slow-window')
    $candidate = [pscustomobject]@{
        Id = $slowProcess.Id
        ExecutablePath = [IO.Path]::GetFullPath($slowProcess.Path)
        StartTimeUtcTicks = $slowProcess.StartTime.ToUniversalTime().Ticks
    }
    $null = Assert-Throws { Request-ViewerShutdown @($candidate) 1 } `
        'A slow/refusing disposable process did not abort graceful replacement.'
    $slowProcess.Refresh()
    Assert-True (-not $slowProcess.HasExited) 'Graceful replacement forcibly terminated a refusing process.'
} finally {
    if ($null -ne $slowProcess -and -not $slowProcess.HasExited) {
        Stop-Process -Id $slowProcess.Id -Force
        $slowProcess.WaitForExit()
    }
}

# Retry the same pending revision after a temporary destination failure, and
# ensure a newer revision observed during that retry is the one exported.
$watchDataRoot = Join-Path $fixtureRoot 'WatchData'
[IO.Directory]::CreateDirectory($watchDataRoot) | Out-Null
. $syncScript -Character Arin -FunctionsOnly -DataRoot $watchDataRoot
$watchLivePath = $livePath
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($watchLivePath)) | Out-Null
[IO.File]::WriteAllBytes($watchLivePath, $validEnvelope)
$newerSnapshot = Read-Snapshot $snapshotPath
$newerSnapshot.clips[0].keyframes[0].swordWrist.rotation[0]++
$newerPayload = Convert-SnapshotToPayload $newerSnapshot
$newerEnvelope = New-LiveEnvelope $newerPayload
$watchDestination = Join-Path $fixtureRoot 'watch-export.json'
[IO.Directory]::CreateDirectory($watchDestination) | Out-Null
$viewer = $null
$watcher = $null
try {
    $viewer = Start-HiddenPowerShell 'Start-Sleep -Seconds 4' (Join-Path $fixtureRoot 'watch-viewer')
    $watchCommand = "& '$syncScript' -Character Arin -Mode Watch -ViewerProcessId $($viewer.Id) " +
        "-DataRoot '$watchDataRoot' -DestinationPath '$watchDestination'"
    $watcher = Start-HiddenPowerShell $watchCommand (Join-Path $fixtureRoot 'watcher')
    Start-Sleep -Milliseconds 700
    [IO.File]::WriteAllBytes($watchLivePath, $newerEnvelope)
    Start-Sleep -Milliseconds 350
    [IO.Directory]::Delete($watchDestination)
    if (-not $watcher.WaitForExit(12000)) { throw 'Retry watcher did not exit after its Viewer process.' }
    Assert-True ($watcher.ExitCode -eq 0) 'Retry watcher reported failure after the destination recovered.'
    $watched = Read-Snapshot $watchDestination
    Assert-True ($watched.clips[0].keyframes[0].swordWrist.rotation[0] -eq
        $newerSnapshot.clips[0].keyframes[0].swordWrist.rotation[0]) `
        'Watcher synchronized an older revision after a newer pending save arrived.'
} finally {
    foreach ($process in @($watcher, $viewer)) {
        if ($null -ne $process -and -not $process.HasExited) {
            Stop-Process -Id $process.Id -Force
            $process.WaitForExit()
        }
    }
}

$failedDestination = Join-Path $fixtureRoot 'persistent-failure.json'
[IO.Directory]::CreateDirectory($failedDestination) | Out-Null
$viewer = $null
$watcher = $null
try {
    $viewer = Start-HiddenPowerShell 'Start-Sleep -Seconds 1' (Join-Path $fixtureRoot 'failure-viewer')
    $watchCommand = "& '$syncScript' -Character Arin -Mode Watch -ViewerProcessId $($viewer.Id) " +
        "-DataRoot '$watchDataRoot' -DestinationPath '$failedDestination'"
    $watcher = Start-HiddenPowerShell $watchCommand (Join-Path $fixtureRoot 'failure-watcher')
    if (-not $watcher.WaitForExit(12000)) { throw 'Persistent-failure watcher did not finish bounded shutdown retries.' }
    Assert-True ($watcher.ExitCode -ne 0) 'Persistent watcher failure was incorrectly reported as synchronized.'
} finally {
    foreach ($process in @($watcher, $viewer)) {
        if ($null -ne $process -and -not $process.HasExited) {
            Stop-Process -Id $process.Id -Force
            $process.WaitForExit()
        }
    }
}

Write-Host "Character Viewer preservation fixtures passed; isolated root: $fixtureRoot"
