[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$testRoot = Join-Path ([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))) `
    "artifacts\tests\calibration-$([Guid]::NewGuid().ToString('N'))"
$null = New-Item -ItemType Directory -Path $testRoot
. (Join-Path $PSScriptRoot 'sync-arin-v5-7-calibration.ps1') -FunctionsOnly -DataRoot $testRoot
$testCount = 0

function Check([bool]$Condition, [string]$Label) {
    if (-not $Condition) { throw "FAIL: $Label" }
    $script:testCount++
}

function Reject([scriptblock]$Action, [string]$Label) {
    $rejected = $false
    try { & $Action | Out-Null } catch { $rejected = $true }
    Check $rejected $Label
}

function Clone($Value) {
    return $Value | ConvertTo-Json -Depth 24 | ConvertFrom-Json -AsHashtable -Depth 24
}

Assert-ProfileAssets
$fixture = Read-Snapshot $snapshotPath
$before = Get-PathHash $snapshotPath
$payload = Convert-SnapshotToPayload $fixture
$roundTrip = Convert-PayloadToSnapshot $payload
Check (($fixture | ConvertTo-Json -Depth 24) -ceq ($roundTrip | ConvertTo-Json -Depth 24)) 'All current channels survive runtime round-trip'
Check ((Get-PathHash $snapshotPath) -ceq $before) 'Read-only validation did not modify the live canonical file'

$historicalText = (& git -C $repositoryRoot show 'de0fb926ed000daebb68f4efe2abe0706fbf4ac5:games/SinStarI/SourceAssets/Characters/Paladin/ArinV57/Calibration/arin-v5.7-pose-calibration.json') -join "`n"
if ($LASTEXITCODE -ne 0) { throw 'Cannot read historical nine-key fixture.' }
$historical = Normalize-Snapshot ($historicalText | ConvertFrom-Json -AsHashtable)
Check ($historical.totalKeyframes -eq 9) 'Historical nine-key fixture remains recoverable'
Check (($historical | ConvertTo-Json -Depth 24) -ceq ((Convert-PayloadToSnapshot (Convert-SnapshotToPayload $historical)) | ConvertTo-Json -Depth 24)) 'Historical values survive migration'

$modified = Clone $fixture
[Array]::Reverse($modified.clips)
foreach ($clip in $modified.clips) { $clip.index = 0 }
$modified.savedKeyframe.clipIndex = 0
Check (((Normalize-Snapshot $modified) | ConvertTo-Json -Depth 24) -ceq ($fixture | ConvertTo-Json -Depth 24)) 'Exact names override stale index hints and array order'

$modified = Clone $fixture
$modified.clips[0].name = 'RetiredClip'
if ($modified.savedKeyframe.clipName -eq 'BlockImpact') { $modified.savedKeyframe.clipName = 'RetiredClip' }
$unresolved = Normalize-Snapshot $modified
Check (@($unresolved.clips | Where-Object index -lt 0).Count -eq 1) 'Missing clip is retained unresolved'
$runtimeOnly = Convert-PayloadToSnapshot (Convert-SnapshotToPayload $unresolved)
Check (@($runtimeOnly.clips[0].keyframes).Count -eq 0) 'Missing clip does not bind to its old index'

foreach ($size in @(2,4)) {
    $modified = Clone $fixture
    $modified.clips[0].keyframes[0].sword.rotation = @(1) * $size
    Reject { Normalize-Snapshot $modified } "Reject $size-value rotation"
}
foreach ($value in @($null, '10', $true, 0.5, 181)) {
    $modified = Clone $fixture
    $modified.clips[0].keyframes[0].sword.rotation[0] = $value
    Reject { Normalize-Snapshot $modified } 'Reject non-integer/out-of-range rotation'
}
$modified = Clone $fixture
$modified.clips[0].keyframes[0].sword.position[0] = 101
Reject { Normalize-Snapshot $modified } 'Enforce actual position limit'
$modified = Clone $fixture
$modified.clips[0].keyframes[0].frame = 25
Reject { Normalize-Snapshot $modified } 'Enforce actual clip sample bound'
$modified = Clone $fixture
$modified.clips[0].keyframes += Clone $modified.clips[0].keyframes[0]
$modified.totalKeyframes++
Reject { Normalize-Snapshot $modified } 'Reject duplicate frames'
$modified = Clone $fixture
$modified.clips += Clone $modified.clips[0]
Reject { Normalize-Snapshot $modified } 'Reject duplicate exact names'
$modified = Clone $fixture
$modified.totalKeyframes++
Reject { Normalize-Snapshot $modified } 'Reject inconsistent count'
$modified = Clone $fixture
$modified.assetId = 'another-character'
Reject { Normalize-Snapshot $modified } 'Reject wrong asset'
$modified = Clone $fixture
$modified.profile.modelSha256 = '0' * 64
Reject { Normalize-Snapshot $modified } 'Reject wrong model hash'
$modified = Clone $fixture
$modified.clips[0].keyframes[0].sword.Remove('decoupled')
Reject { Normalize-Snapshot $modified } 'Do not silently default a missing current Boolean'
$modified = Clone $fixture
$modified.clips[0].keyframes[0].shield.decoupled = 1
Reject { Normalize-Snapshot $modified } 'Reject numeric Boolean'
$modified = Clone $fixture
$modified.deleteAll = $true
Reject { Normalize-Snapshot $modified } 'Reject unknown destructive field'
$modified = Clone $fixture
$modified.savedKeyframe.frame = 65535
Reject { Normalize-Snapshot $modified } 'Reject invalid saved key metadata'

$legacy = Clone $historical
$legacy.schemaVersion = 1
$legacy.storageVersion = 1
$legacy.Remove('profile')
foreach ($clip in $legacy.clips) {
    foreach ($key in $clip.keyframes) { $key.sword.Remove('decoupled'); $key.shield.Remove('decoupled') }
}
Reject { Normalize-Snapshot $legacy } 'v1 migration is explicit'
$MigrateLegacy = $true
$migrated = Normalize-Snapshot $legacy
Check ($migrated.storageVersion -eq 3 -and -not $migrated.clips[0].keyframes[0].sword.decoupled) 'v1 default is coupled and all vectors retained'
$MigrateLegacy = $false

$source = Join-Path $testRoot 'canonical.json'
Write-Snapshot $source $fixture ''
$firstHash = Get-PathHash $source
Write-Snapshot $source $fixture $firstHash
Check ((Get-PathHash $source) -ceq $firstHash) 'Canonical serialization is deterministic'
$modified = Clone $fixture
$modified.clips[0].keyframes[0].sword.rotation[0] = 179
Write-Snapshot $source $modified $firstHash
Check ((Get-PathHash "$source.bak") -ceq $firstHash) 'Atomic replace preserves exact previous bytes'
$secondHash = Get-PathHash $source
Reject { Write-Snapshot $source $fixture $firstHash } 'Concurrent change rejected'
Check ((Get-PathHash $source) -ceq $secondHash) 'Concurrent failure preserves current file'
$locked = [IO.File]::Open($source, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
try { Reject { Write-Snapshot $source $fixture $secondHash } 'Locked destination rejects atomic replace' }
finally { $locked.Dispose() }
Check ((Get-PathHash $source) -ceq $secondHash) 'Failed replace preserves current file'
Check (@(Get-ChildItem -LiteralPath $testRoot -Filter '*.tmp.*').Count -eq 0) 'Failure cleans only its own temporary file'
$backup = Read-Snapshot "$source.bak"
Write-Snapshot $source $backup $secondHash
Check ((Get-PathHash $source) -ceq $firstHash) 'Backup restores all properties exactly'
$reader = [IO.FileStream]::new($source, [IO.FileMode]::Open, [IO.FileAccess]::Read,
    [IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete)
try {
    Write-Snapshot $source $modified $firstHash
    Check ((Get-PathHash $source) -ceq $secondHash) 'A shared watcher read does not block replacement'
} finally { $reader.Dispose() }
Write-Snapshot $source $fixture $secondHash

$SourcePath = $source
$DestinationPath = $livePath
$null = Restore-LiveCalibration $false
$envelopeHash = Get-PathHash $livePath
Check ($envelopeHash.Length -eq 64) 'Isolated binary save exists'
$DestinationPath = Join-Path $testRoot 'export.json'
$SourcePath = $livePath
$null = Export-LiveCalibration $false
Check ((Get-PathHash $DestinationPath) -ceq $firstHash) 'Binary import/export canonical parity'
$badEnvelope = [IO.File]::ReadAllBytes($livePath)
$badEnvelope[45] = $badEnvelope[45] -bxor 1
$badPath = Join-Path $testRoot 'bad.bin'
Write-AtomicBytes $badPath $badEnvelope ''
Reject { Read-LivePayload $badPath } 'Reject checksum mismatch'
$AllowMissing = $true
$SourcePath = $badPath
Reject { Export-LiveCalibration $true } 'AllowMissing never hides malformed data'
Reject { Assert-ConfinedPath 'C:\outside-smile\calibration.json' } 'Reject unsafe destination'
Check ((Get-PathHash $snapshotPath) -ceq $before) 'Tests leave user poses untouched'
Write-Host "Arin calibration: $testCount checks passed. Isolated evidence: $testRoot"
