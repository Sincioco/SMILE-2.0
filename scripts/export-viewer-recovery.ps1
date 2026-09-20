[CmdletBinding()]
param(
    [ValidateSet('Export', 'Watch')][string]$Mode = 'Export',
    [string]$ApplicationId = 'smile.tools.character3d-viewer',
    [int]$ViewerProcessId,
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\artifacts\reports\Character3DViewer'),
    [switch]$FunctionsOnly
)

$ErrorActionPreference = 'Stop'

function Get-RecoveryHash([byte[]]$Bytes) {
    return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($Bytes)).ToLowerInvariant()
}

function Read-ViewerRecovery([string]$Path) {
    # The runtime replaces this envelope atomically; permit that replacement while reading.
    $stream = [IO.File]::Open($Path, [IO.FileMode]::Open, [IO.FileAccess]::Read,
        [IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete)
    try {
        if ($stream.Length -lt 44 -or $stream.Length -gt 16428) {
            throw 'Invalid recovery envelope length.'
        }
        $bytes = [byte[]]::new($stream.Length)
        $stream.ReadExactly($bytes)
    } finally { $stream.Dispose() }
    if ([Text.Encoding]::ASCII.GetString($bytes, 0, 4) -ne 'SMD4' -or
        [BitConverter]::ToUInt32($bytes, 4) -ne 1 -or
        [BitConverter]::ToUInt32($bytes, 8) -ne $bytes.Length - 44) {
        throw 'Invalid recovery envelope header.'
    }
    $payload = $bytes[44..($bytes.Length - 1)]
    $hash = Get-RecoveryHash $payload
    if ($hash -cne [Convert]::ToHexString($bytes[12..43]).ToLowerInvariant()) {
        throw 'Recovery envelope checksum mismatch.'
    }
    $contents = [Text.Encoding]::UTF8.GetString($payload)
    if (-not $contents.StartsWith("SMILE Character Viewer Recovery v1`n")) {
        throw 'Unsupported recovery report format.'
    }
    return [pscustomobject]@{ Text = $contents; Hash = $hash }
}

function Export-ViewerRecoveryFile(
    [string]$Path, [string]$Destination, [string]$Identity, $ProcessContext
) {
    $Destination = [IO.Path]::GetFullPath($Destination)
    $report = Read-ViewerRecovery $Path
    $written = (Get-Item -LiteralPath $Path).LastWriteTimeUtc
    $stage = 'Unknown'
    $clip = 'Unknown'
    if ($report.Text -match '(?m)^Stage=(\d+)(?:\.0)?\r?$') { $stage = $Matches[1] }
    if ($report.Text -match '(?m)^BossClip=([^\r\n]+)$') { $clip = $Matches[1] }
    elseif ($report.Text -match '(?m)^PrimaryClip=([^\r\n]+)$') { $clip = $Matches[1] }
    $clip = [regex]::Replace($clip, '[^a-zA-Z0-9_-]', '_')
    $clip = $clip.Substring(0, [Math]::Min(48, $clip.Length))
    $shortHash = $report.Hash.Substring(0, 16)
    $identityHash = (Get-RecoveryHash ([Text.Encoding]::UTF8.GetBytes($Identity))).Substring(0, 8)
    $null = New-Item -ItemType Directory -Path $Destination -Force
    $existing = @(Get-ChildItem -LiteralPath $Destination -Filter "*-$identityHash-$shortHash.txt")
    if ($existing.Count -gt 0) { return $existing[0].FullName }
    $name = 'Recovery-{0}-Stage{1}-{2}-{3}-{4}.txt' -f `
        $written.ToString('yyyyMMdd-HHmmss-fffZ'), $stage, $clip, $identityHash, $shortHash
    $outputPath = Join-Path $Destination $name
    $header = "SavedUtc=$($written.ToString('o'))`nApplicationId=$Identity`nPayloadSha256=$($report.Hash)`n"
    if ($null -ne $ProcessContext -and $written -ge $ProcessContext.StartTimeUtc) {
        $header += "ProcessId=$($ProcessContext.Id)`nProcessStartUtc=$($ProcessContext.StartTimeUtc.ToString('o'))`n"
        $header += "Executable=$($ProcessContext.Path)`nExecutableSha256=$($ProcessContext.Hash)`n"
    } else {
        $header += "Executable=Not captured for this saved report`n"
    }
    $header += "OS=$([Environment]::OSVersion.VersionString)`n`n"
    # Never overwrite a different report. The hash and timestamp distinguish recurrences.
    [IO.File]::WriteAllText($outputPath, $header + $report.Text, [Text.UTF8Encoding]::new($false))
    return $outputPath
}

if ($FunctionsOnly) { return }

$appHash = Get-RecoveryHash ([Text.Encoding]::UTF8.GetBytes($ApplicationId))
$keyHash = Get-RecoveryHash ([Text.Encoding]::UTF8.GetBytes('Viewer.Recovery.v1'))
$dataPath = Join-Path $env:LOCALAPPDATA "SMILE 2.0\Games\$appHash\Data\$keyHash.bin"
$context = $null
$process = $null
if ($Mode -eq 'Watch') {
    if ($ViewerProcessId -le 0) { throw 'Watch requires a ViewerProcessId.' }
    $process = Get-Process -Id $ViewerProcessId -ErrorAction Stop
    $context = [pscustomobject]@{
        Id = $process.Id
        StartTimeUtc = $process.StartTime.ToUniversalTime()
        Path = $process.Path
        Hash = (Get-FileHash -LiteralPath $process.Path -Algorithm SHA256).Hash.ToLowerInvariant()
    }
}
$seen = @{}
$finished = $false
do {
    foreach ($path in @("$dataPath.bak", $dataPath)) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { continue }
        try {
            $item = Get-Item -LiteralPath $path
            $stamp = "$($item.LastWriteTimeUtc.Ticks):$($item.Length)"
            if ($seen[$path] -eq $stamp) { continue }
            $result = Export-ViewerRecoveryFile $path $OutputDirectory $ApplicationId $context
            $seen[$path] = $stamp
            Write-Host "Viewer recovery report: $result"
        } catch {
            # A corrupt latest file must not prevent exporting its valid backup.
            Write-Warning "Could not export $path : $($_.Exception.Message)"
        }
    }
    if ($Mode -ne 'Watch' -or $finished) { break }
    Start-Sleep -Milliseconds 1000
    $process.Refresh()
    $finished = $process.HasExited
} while ($true)
