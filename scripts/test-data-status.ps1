[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$testRoot = Join-Path $repositoryRoot 'artifacts\tests\DataStatus'
$null = New-Item -ItemType Directory -Path $testRoot -Force
$applicationId = 'smile.tests.data-status.run-' + [Guid]::NewGuid().ToString('N')
$compiler = Join-Path $repositoryRoot 'artifacts\compiler\smilec.exe'
foreach ($name in @('Read', 'Write')) {
    & $compiler (Join-Path $repositoryRoot "examples\Phase4Hardening\DataStatus$name.smile") `
        -o (Join-Path $testRoot "$name.exe") --application-id $applicationId
    if ($LASTEXITCODE -ne 0) { throw "Data Status $name compilation failed." }
}
function Hash-Text([string]$Value) {
    return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($Value))).ToLowerInvariant()
}
$dataRoot = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) `
    ('SMILE 2.0\Games\' + (Hash-Text $applicationId) + '\Data')
$null = New-Item -ItemType Directory -Path $dataRoot -Force
$primary = Join-Path $dataRoot ((Hash-Text 'Recovery Probe') + '.bin')
$backup = $primary + '.bak'
function Envelope([byte[]]$Payload) {
    $bytes = [byte[]]::new(44 + $Payload.Length)
    [Text.Encoding]::ASCII.GetBytes('SMD4').CopyTo($bytes, 0)
    [BitConverter]::GetBytes([uint32]1).CopyTo($bytes, 4)
    [BitConverter]::GetBytes([uint32]$Payload.Length).CopyTo($bytes, 8)
    [Security.Cryptography.SHA256]::HashData($Payload).CopyTo($bytes, 12)
    $Payload.CopyTo($bytes, 44)
    return ,$bytes
}
function Check-Run([string]$Name, [string]$Expected) {
    $actual = (& (Join-Path $testRoot "$Name.exe") | Out-String).Trim()
    if ($LASTEXITCODE -ne 0 -or $actual -cne $Expected) {
        throw "$Name expected '$Expected', got '$actual' (exit $LASTEXITCODE)."
    }
    Write-Host "$Name : $actual"
}
Check-Run Read '1,0,99,98'
Check-Run Write '0'
Check-Run Read '0,2,17,23'
$first = (Get-FileHash -LiteralPath $primary).Hash
Check-Run Write '0'
if ((Get-FileHash -LiteralPath $backup).Hash -ne $first) { throw 'Backup differs from previous primary.' }
# A held file handle simulates a real sharing denial without changing ACLs or user storage.
$lock = [IO.File]::Open($primary, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::None)
try {
    Check-Run Read '4,0,99,98'
    Check-Run Write '4'
} finally { $lock.Dispose() }
# A held backup prevents replacement; both primary and backup remain byte-identical.
$lock = [IO.File]::Open($backup, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::None)
try { Check-Run Write '4' } finally { $lock.Dispose() }
foreach ($path in @($primary, $backup)) {
    if ((Get-FileHash -LiteralPath $path).Hash -ne $first) { throw 'Denied write changed persistent data.' }
}
[IO.File]::WriteAllBytes($primary, (Envelope ([byte[]](1,2,3,4,5,6,7,8,9))))
Check-Run Read '6,0,99,98'
[IO.File]::WriteAllBytes($primary, [byte[]](1,2,3))
Check-Run Read '2,2,17,23'
if ((Get-Item -LiteralPath $primary).Length -ne 3) { throw 'Recovery unexpectedly rewrote the corrupt primary.' }
Check-Run Write '0'
if ((Get-FileHash -LiteralPath $backup).Hash -ne $first) { throw 'Corrupt primary replaced the last-good backup.' }
[IO.File]::WriteAllBytes($primary, [byte[]](1,2,3))
[IO.File]::WriteAllBytes($backup, (Envelope ([byte[]](1,2,3,4,5,6,7,8,9))))
Check-Run Read '6,0,99,98'
[IO.File]::WriteAllBytes($backup, [byte[]](4,5,6))
Check-Run Read '5,0,99,98'
Check-Run Write '5'
[IO.File]::WriteAllBytes($backup, (Envelope ([byte[]](41,43))))
# Only this exact disposable test file is deleted; the evidence/app directory is retained.
Remove-Item -LiteralPath $primary
Check-Run Read '2,2,41,43'
if (@(Get-ChildItem -LiteralPath $dataRoot -Filter '*.tmp.*').Count -ne 0) { throw 'Temporary save files leaked.' }
Write-Host "Native checked Data atomicity/recovery passed. Disposable ApplicationId: $applicationId"
Write-Host "Evidence retained: $testRoot and $dataRoot"
