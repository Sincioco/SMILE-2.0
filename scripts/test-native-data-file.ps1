[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$output = Join-Path $root ('artifacts\tests\direct-town-save\' + [Guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $output -Force
$chosen = Join-Path $output 'Town Test 世界.town'
$locked = Join-Path $output 'Locked.town'
$bad = Join-Path $output 'Corrupt.town'
[IO.File]::WriteAllText($bad, 'damaged data')
[IO.File]::WriteAllText($locked, 'previous file stays untouched')
$lock = [IO.File]::Open($locked, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
$source = @'
Game Window "Direct Town Save Regression" Size 480 By 240
Dim Bytes[4] As Number
Dim ByteCount As Number
Dim Job As Number
Dim Status As Number
Dim Other As Number
Dim Message As Text

Bytes[0] = 17
Bytes[1] = 99
Save Data Bytes Count 2 To "Snapshot" Status Status
Job = Data_FileStart(True, "Snapshot", "@CHOSEN@")
Call AwaitJob(Job, 1)
Other = Data_FileProgress(Job)
Call Check(Other = 100)
Call Check(Text_Length(Local_Timestamp()) = 15)
Job = Data_FileStart(False, "Opened", "@CHOSEN@")
Call AwaitJob(Job, 1)
Load Data "Opened" Into Bytes Count ByteCount Status Status
Call Check(Status = DATA_STATUS_OK And ByteCount = 2 And Bytes[0] = 17 And Bytes[1] = 99)
Bytes[0] = 42
Save Data Bytes Count 2 To "Snapshot" Status Status
Job = Data_FileStart(True, "Snapshot", "@CHOSEN@")
Call AwaitJob(Job, 1)
Job = Data_FileStart(False, "Old", "@CHOSEN@.bak")
Call AwaitJob(Job, 1)
Load Data "Old" Into Bytes Count ByteCount Status Status
Call Check(Bytes[0] = 17)
Job = Data_FileStart(True, "Snapshot", "@LOCKED@")
Call AwaitJob(Job, 2)
Other = Data_FileProgress(Job)
Call Check(Other < 100)
Message = Data_FileMessage(Job)
Call Check(Text_Length(Message) > 20)
Job = Data_FileStart(False, "Opened", "@BAD@")
Call AwaitJob(Job, 2)
Load Data "Opened" Into Bytes Count ByteCount Status Status
Call Check(Bytes[0] = 17)
Job = Data_FileStart(True, "Snapshot", "relative.town")
Call AwaitJob(Job, 2)
Print "PASS In-Process Save/Open Without Worker; Unicode; Verified 100%; Atomic Backup; Failed Write; Corrupt Open"

Sub AwaitJob(Job As Number, Expected As Number)

    Dim Deadline As Number
    Dim Status As Number

    Call Check(Job > 0)
    Deadline = Timer() + 10000

    Do
        Status = Data_FileStatus(Job)
        Show Screen
        Wait 10 Milliseconds
    Loop Until Status <> 0 Or Timer() > Deadline

    Call Check(Status = Expected)

End Sub
Sub Check(Ok As Boolean)

    If Not Ok Then
        Print "FAIL Direct File Assertion"
    End If

End Sub

'@
$source = $source.Replace('@CHOSEN@', $chosen).Replace('@LOCKED@', $locked).Replace('@BAD@', $bad)
[IO.File]::WriteAllText((Join-Path $output 'Test.smile'), $source)
$application = 'smile.tests.direct-file.run-' + [Guid]::NewGuid().ToString('N')
[IO.File]::WriteAllText((Join-Path $output 'Test.smileproj'), @"
<SmileProject><PropertyGroup><ProjectKind>Game</ProjectKind><ApplicationId>$application</ApplicationId>
<StartupFile>Test.smile</StartupFile></PropertyGroup><ItemGroup>
<SmileSource Include="Test.smile" StartupOnly="true" /></ItemGroup></SmileProject>
"@)
try {
    $exe = Join-Path $output 'Test.exe'
    & (Join-Path $root 'artifacts\compiler\smilec.exe') --project (Join-Path $output 'Test.smileproj') --target windows-x64 -o $exe *> (Join-Path $output 'compile.log')
    if ($LASTEXITCODE -ne 0) { Get-Content (Join-Path $output 'compile.log') -Tail 15; throw 'Direct file fixture compilation failed.' }
    $run = Start-Process $exe -WorkingDirectory $output -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $output 'run.log') -RedirectStandardError (Join-Path $output 'errors.log')
    if (-not $run.WaitForExit(20000)) { throw "Save test did not finish: $($run.Id)" }
    $log = Get-Content (Join-Path $output 'run.log') -Raw
    Write-Host $log
    if ($run.ExitCode -ne 0 -or $log -notmatch 'PASS In-Process' -or $log -match '(?m)^FAIL ') { throw 'Direct file regression failed.' }
} finally { $lock.Dispose() }
if ([IO.File]::ReadAllText($locked) -ne 'previous file stays untouched') { throw 'Failed save changed the original file.' }
if (@(Get-ChildItem -LiteralPath $output -Filter '*.pending.*').Count) { throw 'Failed save left temporary files.' }
Write-Host "PASS Original Retained And Temporary Files Cleaned; Evidence: $output"
