[CmdletBinding()]
param([Parameter(Mandatory)][Alias("ParentProcessId")][int]$ViewerProcessId, [Parameter(Mandatory)][string]$DataFolder)

$ErrorActionPreference = 'Stop'
# The executable supervisor and the launcher can race; only one worker owns a Viewer.
$created = $false
$workerMutex = [Threading.Mutex]::new($true, "Local\SmileNativeWorker-$ViewerProcessId", [ref]$created)
if (-not $created) { $workerMutex.Dispose(); exit 0 }
. (Join-Path $PSScriptRoot 'TownFileWorker.ps1')
function HashText([string]$Value) {
    [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($Value))).ToLowerInvariant()
}
function ReportFailure([string]$RequestPath, [string]$Message) {
    # The last request field is a positive base-128/zigzag request identity.
    $data = [IO.File]::ReadAllBytes($RequestPath)
    $first = $data.Length - 1
    while ($first -gt 44 -and $data[$first - 1] -ge 128) { $first-- }
    $payload = [Collections.Generic.List[byte]]::new()
    $payload.AddRange([byte[]](84, 87, 82, 1))
    $payload.AddRange([byte[]]$data[$first..($data.Length - 1)])
    $payload.AddRange([byte[]](4, 0)) # Failed, zero progress.
    $messageBytes = [Text.Encoding]::ASCII.GetBytes($Message.Substring(0, [Math]::Min(60, $Message.Length)))
    $payload.Add([byte]($messageBytes.Length * 2))
    foreach ($letter in $messageBytes) {
        $value = [int]$letter * 2
        if ($value -ge 128) { $payload.Add([byte](($value % 128) + 128)); $value = $value -shr 7 }
        $payload.Add([byte]$value)
    }
    $bytes = $payload.ToArray()
    $header = [byte[]](83, 77, 68, 52, 1, 0, 0, 0) + [BitConverter]::GetBytes([uint32]$bytes.Length)
    $result = $header + [Security.Cryptography.SHA256]::HashData($bytes) + $bytes
    $response = Join-Path $folder ((HashText 'TownEditor.Blender.Response') + '.bin')
    [IO.File]::WriteAllBytes("$response.pending", $result)
    [IO.File]::Move("$response.pending", $response, $true)
}
$viewer = Get-Process -Id $ViewerProcessId -ErrorAction Stop
$started = $viewer.StartTime.ToUniversalTime().Ticks
$folder = $DataFolder
$null = New-Item -ItemType Directory -Path $folder -Force
$request = Join-Path $folder ((HashText 'TownEditor.Blender.Request') + '.bin')
$stamp = Join-Path $folder 'town-blender-worker.completed'
$log = Join-Path $folder 'town-blender-worker.log'
$blender = Get-ChildItem -LiteralPath 'C:\Program Files\Blender Foundation' -Filter blender.exe -Recurse -ErrorAction SilentlyContinue |
    Sort-Object FullName -Descending | Select-Object -First 1
$script = Join-Path $PSScriptRoot 'town_blender_save.py'
$last = if (Test-Path -LiteralPath $stamp) { [IO.File]::ReadAllText($stamp) } else { '' }
$fileRequest = Join-Path $folder ((HashText 'TownEditor.File.Request') + '.bin')
$fileStamp = Join-Path $folder 'town-file-worker.completed'
$fileLast = if (Test-Path -LiteralPath $fileStamp) { [IO.File]::ReadAllText($fileStamp) } else { '' }
while ($true) {
    $current = Get-Process -Id $ViewerProcessId -ErrorAction SilentlyContinue
    if ($null -eq $current -or $current.StartTime.ToUniversalTime().Ticks -ne $started) { break }
    if (Test-Path -LiteralPath $request) {
        $hash = (Get-FileHash -LiteralPath $request -Algorithm SHA256).Hash
        if ($hash -ne $last) {
            # Snapshot an atomic request. A subsequent save cannot alter this job.
            $snapshot = Join-Path $folder 'town-blender-worker.request.bin'
            Copy-Item -LiteralPath $request -Destination $snapshot -Force
            if ($null -eq $blender) {
                ReportFailure $snapshot 'Blender is not installed; Viewer edits are retained.'
            } else {
                & $blender.FullName --background --python-exit-code 1 --python $script -- $snapshot $folder *> $log
                if ($LASTEXITCODE -ne 0) {
                    ReportFailure $snapshot 'Blender save failed; see town-blender-worker.log.'
                }
            }
            $last = $hash
            [IO.File]::WriteAllText($stamp, $last)
        }
    }
    $newStamp = Get-Date -Format 'yyyy-MM-dd HHmm'
    # Publish local date/time for the native Save As suggestion without another dialog.
    $status = [Collections.Generic.List[byte]]::new()
    $status.AddRange([byte[]](84,87,82,1))
    Add-TownInteger $status $ViewerProcessId
    Add-TownInteger $status 0
    Add-TownInteger $status ([Environment]::TickCount64 % 1000000000)
    Add-TownInteger $status $newStamp.Length
    foreach ($letter in $newStamp.ToCharArray()) { Add-TownInteger $status ([int]$letter) }
    Write-TownPayload (Join-Path $folder ((HashText 'TownEditor.File.Clock') + '.bin')) $status.ToArray()
    if (Test-Path -LiteralPath $fileRequest) {
        $fileHash = (Get-FileHash -LiteralPath $fileRequest -Algorithm SHA256).Hash
        if ($fileHash -ne $fileLast) {
            $fileSnapshot = Join-Path $folder 'town-file-worker.request.bin'
            Copy-Item -LiteralPath $fileRequest -Destination $fileSnapshot -Force
            Invoke-TownFileJob $fileSnapshot $folder $blender.FullName $log
            $fileLast = $fileHash
            [IO.File]::WriteAllText($fileStamp, $fileLast)
        }
    }
    Start-Sleep -Milliseconds 500
}
