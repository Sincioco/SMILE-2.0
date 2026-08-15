param(
    [Parameter(Mandatory = $true)]
    [string]$LoaderPath
)

$ErrorActionPreference = 'Stop'

function Get-Sha256Hex([string]$Text) {
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [Text.Encoding]::UTF8.GetBytes($Text)
        return ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '').ToLowerInvariant()
    }
    finally { $sha.Dispose() }
}

$gamesRoot = Join-Path $env:LOCALAPPDATA 'SMILE 2.0\Games'
$appRoot = Join-Path $gamesRoot (Get-Sha256Hex 'Phase4HardeningData')
$dataPath = Join-Path (Join-Path $appRoot 'Data') ((Get-Sha256Hex 'Slot/A') + '.bin')
if (-not (Test-Path -LiteralPath $dataPath -PathType Leaf)) {
    throw "Phase 4.1 Data envelope was not created at the stable app/key identity path: $dataPath"
}

$bytes = [IO.File]::ReadAllBytes($dataPath)
if ($bytes.Length -ne 47 -or [Text.Encoding]::ASCII.GetString($bytes, 0, 4) -ne 'SMD4' -or
    [BitConverter]::ToUInt32($bytes, 4) -ne 1 -or [BitConverter]::ToUInt32($bytes, 8) -ne 3 -or
    $bytes[44] -ne 1 -or $bytes[45] -ne 2 -or $bytes[46] -ne 3) {
    throw 'Phase 4.1 native Data envelope magic, version, length, or payload was invalid.'
}

$sha = [Security.Cryptography.SHA256]::Create()
try { $digest = $sha.ComputeHash($bytes[44..46]) }
finally { $sha.Dispose() }
for ($index = 0; $index -lt 32; $index++) {
    if ($bytes[12 + $index] -ne $digest[$index]) { throw 'Phase 4.1 native Data envelope checksum was invalid.' }
}

$bytes[$bytes.Length - 1] = $bytes[$bytes.Length - 1] -bxor 1
[IO.File]::WriteAllBytes($dataPath, $bytes)
$stdout = Join-Path $env:TEMP 'SmileP4H-DataCorrupt.out'
$stderr = Join-Path $env:TEMP 'SmileP4H-DataCorrupt.err'
$process = Start-Process -FilePath $LoaderPath -Wait -PassThru -WindowStyle Hidden `
    -RedirectStandardOutput $stdout -RedirectStandardError $stderr
if ($process.ExitCode -ne 2) { throw "Corrupt native Data exited $($process.ExitCode), expected 2." }
if ((Get-Content -LiteralPath $stderr -Raw) -notmatch 'Load Data encountered') {
    throw 'Corrupt native Data did not emit a visible console diagnostic.'
}

$resolvedGamesRoot = [IO.Path]::GetFullPath($gamesRoot).TrimEnd('\') + '\'
$resolvedAppRoot = [IO.Path]::GetFullPath($appRoot)
if (-not $resolvedAppRoot.StartsWith($resolvedGamesRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to clean an unexpected Data test path: $resolvedAppRoot"
}
Remove-Item -LiteralPath $resolvedAppRoot -Recurse -Force
Remove-Item -LiteralPath $stdout, $stderr -Force -ErrorAction SilentlyContinue
Write-Output 'Phase 4.1 native Data identity, envelope, checksum failure, and cleanup passed.'
