[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$output = Join-Path $root 'artifacts\tests\native-town-worker'
$null = New-Item -ItemType Directory -Path $output -Force
$application = 'smile.tests.native-worker-' + [Guid]::NewGuid().ToString('N')
$worker = Join-Path $root 'tools\Character3DViewer\Watch-TownSaves.ps1'
. (Join-Path $root 'tools\Character3DViewer\TownFileWorker.ps1')
function HashText([string]$Value) {
    [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($Value))).ToLowerInvariant()
}
function Wait-Until([scriptblock]$Condition, [string]$Label) {
    $until = [DateTime]::UtcNow.AddSeconds(15)
    do {
        if (& $Condition) { Write-Host "PASS $Label"; return }
        Start-Sleep -Milliseconds 200
    } while ([DateTime]::UtcNow -lt $until)
    throw "FAIL $Label"
}
function Get-Workers {
    @(Get-CimInstance Win32_Process -Filter "Name='pwsh.exe'" | Where-Object {
        $_.CommandLine -match [regex]::Escape($worker) -and
        $_.CommandLine -match "-(?:Parent|Viewer)ProcessId $($fixture.Id)(?:\s|$)"
    })
}
$source = @'
Game Window "Native Worker Regression" Size 320 By 200
Dim Deadline As Number

Deadline = Timer() + 45000

Do
    Show Screen
    Wait 16 Milliseconds

Loop Until Game_Closed() Or Timer() >= Deadline
'@
[IO.File]::WriteAllText((Join-Path $output 'Program.smile'), $source)
$relative = [IO.Path]::GetRelativePath($output, $worker)
$project = @"
<SmileProject><PropertyGroup><ProjectKind>Game</ProjectKind>
<StartupFile>Program.smile</StartupFile><ApplicationId>$application</ApplicationId>
<NativeWorkerScript>$relative</NativeWorkerScript></PropertyGroup>
<ItemGroup><SmileSource Include="Program.smile" StartupOnly="true" /></ItemGroup></SmileProject>
"@
[IO.File]::WriteAllText((Join-Path $output 'Worker.smileproj'), $project)
$executable = Join-Path $output 'Worker.exe'
& (Join-Path $root 'artifacts\compiler\smilec.exe') --project (Join-Path $output 'Worker.smileproj') --target windows-x64 -o $executable *> (Join-Path $output 'compile.log')
if ($LASTEXITCODE -ne 0) { throw 'Worker fixture compilation failed.' }
$fixture = Start-Process $executable -WorkingDirectory $output -WindowStyle Hidden -PassThru
try {
    Wait-Until { @(Get-Workers).Count -eq 1 } 'Direct Executable Starts Its Worker'
    $first = @(Get-Workers)[0]
    # This is the deliberately disposable regression worker, never a user session.
    Stop-Process -Id $first.ProcessId
    Wait-Until { $current = @(Get-Workers); $current.Count -eq 1 -and $current[0].ProcessId -ne $first.ProcessId } 'Exited Worker Restarts Automatically'
    $folder = Join-Path (& (Join-Path $PSScriptRoot 'get-smile-data-root.ps1')) ((HashText $application) + '\Data')
    $physical = $folder
    Wait-Until { Test-Path (Join-Path $physical ((HashText 'TownEditor.File.Clock') + '.bin')) } 'Worker Uses The Native Applications Actual Save Folder'
    $duplicate = Start-Process pwsh -ArgumentList ('-NoProfile -File "{0}" -ViewerProcessId {1} -DataFolder "{2}"' -f $worker, $fixture.Id, $physical) -WindowStyle Hidden -PassThru
    if (-not $duplicate.WaitForExit(5000) -or $duplicate.ExitCode -ne 0 -or @(Get-Workers).Count -ne 1) { throw 'FAIL Duplicate Worker Protection' }
    Write-Host 'PASS Duplicate Worker Protection'
    $snapshot = Join-Path $root 'games\SinStarI\SourceAssets\Towns\Neris\NerisHorizonV1\Town\Neris-Town-Two-Spaceports-r001.town'
    $payload = Read-TownPayload $snapshot
    Write-TownPayload (Join-Path $physical ((HashText 'TownEditor.File.Snapshot.1') + '.bin')) $payload
    $destination = Join-Path $output ($application + '.town')
    $pathBytes = [Text.Encoding]::UTF32.GetBytes($destination)
    $nameBytes = [Text.Encoding]::UTF32.GetBytes('Worker Regression')
    $request = [BitConverter]::GetBytes([uint32]21387092) + [BitConverter]::GetBytes([uint32]1) +
        [BitConverter]::GetBytes([uint32]1) + [BitConverter]::GetBytes([uint32]$destination.Length) + $pathBytes +
        [BitConverter]::GetBytes([uint32]17) + $nameBytes
    Write-TownPayload (Join-Path $physical ((HashText 'TownEditor.File.Request') + '.bin')) $request
    Wait-Until { (Test-Path $destination) -and (Get-FileHash $destination).Hash -eq (Get-FileHash $snapshot).Hash } 'Real Town Save Finishes With Identical Bytes'
} finally {
    # Only this isolated fixture is stopped; its worker exits when its parent disappears.
    if (-not $fixture.HasExited) { $fixture.Kill(); $fixture.WaitForExit() }
}
Wait-Until { @(Get-Workers).Count -eq 0 } 'Worker Ends With Its Application'
