[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\export-viewer-recovery.ps1" -FunctionsOnly
$testRoot = Join-Path $PSScriptRoot '..\artifacts\tests\ViewerRecoveryExport'
$null = New-Item -ItemType Directory -Path $testRoot -Force
$output = Join-Path $testRoot 'Reports'
$source = Join-Path $testRoot 'report.bin'
$payload = [Text.Encoding]::UTF8.GetBytes("SMILE Character Viewer Recovery v1`nStage=25`nBossClip=EarthHurl`nInput=Backtick, Beat 1, Right Reset`n")
$bytes = [byte[]]::new(44 + $payload.Length)
[Text.Encoding]::ASCII.GetBytes('SMD4').CopyTo($bytes, 0)
[BitConverter]::GetBytes([uint32]1).CopyTo($bytes, 4)
[BitConverter]::GetBytes([uint32]$payload.Length).CopyTo($bytes, 8)
[Security.Cryptography.SHA256]::HashData($payload).CopyTo($bytes, 12)
$payload.CopyTo($bytes, 44)
[IO.File]::WriteAllBytes($source, $bytes)
$first = Export-ViewerRecoveryFile $source $output 'smile.tests.viewer-recovery' $null
$again = Export-ViewerRecoveryFile $source $output 'smile.tests.viewer-recovery' $null
if ($first -ne $again -or $first -notlike '*Stage25-EarthHurl-*') {
    throw 'Meaningful filename or deduplication failed.'
}
$text = [IO.File]::ReadAllText($first)
if (-not $text.Contains('Input=Backtick, Beat 1, Right Reset') -or
    -not $text.Contains('SavedUtc=') -or -not $text.Contains('ApplicationId=smile.tests.viewer-recovery')) {
    throw 'Readable report or export metadata missing.'
}
Copy-Item -LiteralPath $source -Destination "$source.bak" -Force
$bytes[44] = $bytes[44] -bxor 1
[IO.File]::WriteAllBytes($source, $bytes)
$rejected = $false
try { $null = Read-ViewerRecovery $source } catch { $rejected = $true }
if (-not $rejected) { throw 'Corrupt report was accepted.' }
$backup = Export-ViewerRecoveryFile "$source.bak" $output 'smile.tests.viewer-recovery' $null
if ($backup -ne $first) { throw 'Valid backup was not retained or deduplicated.' }

# Exercise the actual watch loop and executable attribution with an isolated identity.
$watchIdentity = "smile.tests.viewer-recovery.$([Guid]::NewGuid().ToString('N'))"
$appHash = Get-RecoveryHash ([Text.Encoding]::UTF8.GetBytes($watchIdentity))
$keyHash = Get-RecoveryHash ([Text.Encoding]::UTF8.GetBytes('Viewer.Recovery.v1'))
$watchData = Join-Path $env:LOCALAPPDATA "SMILE 2.0\Games\$appHash\Data"
$null = New-Item -ItemType Directory -Path $watchData -Force
$watchedProcess = Start-Process (Get-Command pwsh.exe).Source `
    -ArgumentList '-NoProfile -Command "Start-Sleep -Seconds 3"' -WindowStyle Hidden -PassThru
Copy-Item -LiteralPath "$source.bak" -Destination (Join-Path $watchData "$keyHash.bin")
# Copy preserves the source time; set the synthetic event's save time after process start.
(Get-Item -LiteralPath (Join-Path $watchData "$keyHash.bin")).LastWriteTimeUtc = [DateTime]::UtcNow
$watchOutput = Join-Path $testRoot "Watch-$appHash"
& "$PSScriptRoot\export-viewer-recovery.ps1" -Mode Watch -ApplicationId $watchIdentity `
    -ViewerProcessId $watchedProcess.Id -OutputDirectory $watchOutput
$watchReport = Get-ChildItem -LiteralPath $watchOutput -Filter '*.txt' | Select-Object -First 1
$watchText = [IO.File]::ReadAllText($watchReport.FullName)
if (-not $watchText.Contains("ProcessId=$($watchedProcess.Id)") -or
    $watchText -notmatch 'ExecutableSha256=[a-f0-9]{64}') {
    throw 'Watcher process/executable attribution failed.'
}
Write-Host 'Viewer recovery exporter checks passed: readable reports, metadata, filenames, deduplication, checksum rejection, valid backup and watcher exit.'
