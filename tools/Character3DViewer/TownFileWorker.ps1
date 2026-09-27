. (Join-Path $PSScriptRoot "TownVersionFiles.ps1")
# File transfers use the same checksummed Save Data envelope as native recovery.
# Dot-sourced by the launcher-owned watcher; no dialogs or UI calls live here.
function Read-TownPayload([string]$Path) {
    if ((Get-Item -LiteralPath $Path).Length -gt 524332) { throw 'Town file is too large.' }
    $raw = [IO.File]::ReadAllBytes($Path)
    if ($raw.Length -lt 44 -or $raw.Length -gt 524332 -or
        [Text.Encoding]::ASCII.GetString($raw, 0, 4) -ne 'SMD4' -or
        [BitConverter]::ToUInt32($raw, 4) -ne 1 -or
        [BitConverter]::ToUInt32($raw, 8) -ne $raw.Length - 44) { throw 'Invalid town file envelope.' }
    $payload = [byte[]]$raw[44..($raw.Length - 1)]
    $hash = [Security.Cryptography.SHA256]::HashData($payload)
    if ([Convert]::ToHexString($hash) -ne [Convert]::ToHexString($raw[12..43])) { throw 'Town file checksum differs.' }
    return ,$payload
}
function Write-TownPayload([string]$Path, [byte[]]$Payload) {
    $header = [byte[]](83,77,68,52,1,0,0,0) + [BitConverter]::GetBytes([uint32]$Payload.Length)
    [byte[]]$raw = $header + [Security.Cryptography.SHA256]::HashData($Payload) + $Payload
    $pending = $Path + '.' + [Guid]::NewGuid().ToString('N') + '.pending'
    try {
        $stream = [IO.File]::Open($pending, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
        try { $stream.Write($raw); $stream.Flush($true) } finally { $stream.Dispose() }
        [IO.File]::Move($pending, $Path, $true)
    } finally { if ([IO.File]::Exists($pending)) { [IO.File]::Delete($pending) } }
}
function Add-TownInteger($Bytes, [long]$Value) {
    $value = $Value * 2
    while ($value -ge 128) { $Bytes.Add([byte](($value % 128) + 128)); $value = $value -shr 7 }
    $Bytes.Add([byte]$value)
}
function Read-TownInteger([byte[]]$Bytes, [ref]$Cursor) {
    [long]$result = 0
    $shift = 0
    do {
        if ($Cursor.Value -ge $Bytes.Length -or $shift -gt 63) { throw 'Invalid integer.' }
        $part = $Bytes[$Cursor.Value]; $Cursor.Value++
        $result = $result -bor (([long]$part -band 127) -shl $shift)
        $shift += 7
    } while ($part -ge 128)
    return $result -shr 1
}
function Send-TownFileStatus([string]$Folder, [int]$Id, [int]$Status, [int]$Progress, [string]$Message) {
    $bytes = [Collections.Generic.List[byte]]::new()
    $bytes.AddRange([byte[]](84,87,82,1))
    Add-TownInteger $bytes $Id
    Add-TownInteger $bytes $Status
    Add-TownInteger $bytes $Progress
    $message = $Message.Substring(0, [Math]::Min(75, $Message.Length))
    $letters = @($message.EnumerateRunes())
    Add-TownInteger $bytes $letters.Count
    foreach ($letter in $letters) { Add-TownInteger $bytes $letter.Value }
    Write-TownPayload (Join-Path $Folder ((HashText 'TownEditor.File.Response') + '.bin')) $bytes.ToArray()
}
function Invoke-TownFileJob([string]$RequestPath, [string]$Folder, [string]$Blender, [string]$Log) {
    $id = 0
    try {
        $job = Read-TownPayload $RequestPath
        if ($job.Length -lt 20 -or [BitConverter]::ToUInt32($job, 0) -ne 21387092) { throw 'Invalid file request.' }
        $mode = [BitConverter]::ToUInt32($job, 4)
        $id = [BitConverter]::ToUInt32($job, 8)
        $length = [BitConverter]::ToUInt32($job, 12)
        if ($mode -lt 1 -or $mode -gt 5 -or $id -lt 1 -or $length -gt 4095 -or $job.Length -lt 20 + 4 * $length) {
            throw 'Invalid file request fields.'
        }
        $encoding = [Text.UTF32Encoding]::new($false, $false, $true)
        $path = $encoding.GetString($job, 16, 4 * $length)
        $nameOffset = 16 + 4 * $length
        $nameLength = [BitConverter]::ToUInt32($job, $nameOffset)
        if ($nameLength -lt 1 -or $nameLength -gt 80 -or $job.Length -ne $nameOffset + 4 + 4 * $nameLength) {
            throw 'Invalid town name in file request.'
        }
        $townName = $encoding.GetString($job, $nameOffset + 4, 4 * $nameLength)
        if ($mode -eq 5) {
            Publish-TownVersions $Folder $townName $id
            Send-TownFileStatus $Folder $id 1 100 'Choose a version, newest first.'
            return
        }
        $extension = if ($mode -le 2) { '.town' } else { '.blend' }
        if (-not [IO.Path]::IsPathFullyQualified($path) -or [IO.Path]::GetExtension($path) -ine $extension) {
            throw "Choose a $extension file."
        }
        $snapshot = Join-Path $Folder ((HashText "TownEditor.File.Snapshot.$id") + '.bin')
        $opened = Join-Path $Folder ((HashText "TownEditor.File.Opened.$id") + '.bin')
        Send-TownFileStatus $Folder $id 0 10 'Reading selected file...'
        if ($mode -eq 1) {
            $payload = Read-TownPayload $snapshot
            Preserve-TownVersion $Folder $path
            Write-TownPayload $path $payload
            Register-TownVersion $Folder $townName $path
        } elseif ($mode -eq 2) {
            $payload = Read-TownPayload $path
            Write-TownPayload $opened $payload
        } else {
            if (-not $Blender) { throw 'Blender is not installed.' }
            # Argument-array invocation preserves spaces and Unicode without shell interpolation.
            & $Blender --background --disable-autoexec --python-exit-code 1 --python `
                (Join-Path $PSScriptRoot 'town_blender_files.py') -- $mode $id $path $Folder *> $Log
            if ($LASTEXITCODE -ne 0) {
                # The Blender worker reports its specific validation failure first.
                $response = Join-Path $Folder ((HashText 'TownEditor.File.Response') + '.bin')
                $failed = $false
                if (Test-Path -LiteralPath $response) {
                    $result = Read-TownPayload $response
                    $cursor = 4
                    $responseId = Read-TownInteger $result ([ref]$cursor)
                    $responseStatus = Read-TownInteger $result ([ref]$cursor)
                    $failed = $responseId -eq $id -and $responseStatus -eq 2
                }
                if (-not $failed) { throw 'Blender conversion failed; see worker log.' }
            }
            return
        }
        Send-TownFileStatus $Folder $id 1 100 'File transfer complete.'
    } catch {
        $_ | Out-String | Add-Content -LiteralPath $Log
        Send-TownFileStatus $Folder $id 2 0 $_.Exception.Message
    }
}
