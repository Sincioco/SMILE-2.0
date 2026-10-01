$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$identity = 'smile.tests.storage-location.run-' + [guid]::NewGuid().ToString('N')
function Hash([string]$Text) {
    [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($Text))).ToLowerInvariant()
}
function Envelope([byte[]]$Bytes) {
    $header = [Text.Encoding]::ASCII.GetBytes('SMD4') + [BitConverter]::GetBytes([uint32]1) +
        [BitConverter]::GetBytes([uint32]$Bytes.Length) + [Security.Cryptography.SHA256]::HashData($Bytes)
    return [byte[]]($header + $Bytes)
}
$app = Hash $identity
$name = (Hash 'Recovery Probe') + '.bin'
$legacy = Join-Path $env:LOCALAPPDATA "SMILE 2.0\Games\$app\Data\$name"
$current = Join-Path (& (Join-Path $PSScriptRoot 'get-smile-data-root.ps1')) "$app\Data\$name"
$output = Join-Path $root "artifacts\tests\StorageLocation\$app"
New-Item -ItemType Directory -Path $output, (Split-Path $legacy) -Force | Out-Null
[IO.File]::WriteAllBytes($legacy, (Envelope ([byte[]](17,23))))
[IO.File]::WriteAllBytes($legacy + '.bak', (Envelope ([byte[]](41,43))))
$before = (Get-FileHash -LiteralPath $legacy).Hash
$exe = Join-Path $output 'Read.exe'
& (Join-Path $root 'artifacts\compiler\smilec.exe') (Join-Path $root 'examples\Phase4Hardening\DataStatusRead.smile') -o $exe --application-id $identity
if ($LASTEXITCODE -ne 0) { throw 'Storage fixture compile failed' }
function Check([string]$Expected) {
    $actual = (& $exe | Out-String).Trim()
    if ($LASTEXITCODE -ne 0 -or $actual -ne $Expected) { throw "Expected $Expected; got $actual" }
}
Check '0,2,17,23'
if ((Get-FileHash -LiteralPath $current).Hash -ne $before -or
    (Get-FileHash -LiteralPath $legacy).Hash -ne $before) { throw 'Legacy import changed save contents' }
if ((Get-FileHash -LiteralPath ($current + '.bak')).Hash -ne
    (Get-FileHash -LiteralPath ($legacy + '.bak')).Hash) { throw 'Legacy backup not preserved' }
[IO.File]::WriteAllBytes($legacy, (Envelope ([byte[]](91,92))))
Check '0,2,17,23'
# Only this isolated fixture's primary is removed to prove its backup remains authoritative.
Remove-Item -LiteralPath $current
Check '2,2,41,43'
Write-Host 'PASS: Saved Games path, legacy primary/backup import, no overwrite, canonical backup authority.'
Write-Host "Evidence: $output"
