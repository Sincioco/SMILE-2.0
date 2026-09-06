[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$toolRoot = Join-Path $repositoryRoot 'tools\Character3DViewer'
$testRoot = Join-Path $repositoryRoot 'artifacts\tests\ViewerCalibrationIsolation'
$null = New-Item -ItemType Directory -Path $testRoot -Force
$viewerSource = Get-Content -LiteralPath (Join-Path $toolRoot 'Program.smile') -Raw
$testStartup = Get-Content -LiteralPath (Join-Path $toolRoot 'CalibrationTests.smile') -Raw
$profileConstants = foreach ($characterName in @('Arin', 'Orin')) {
    $fingerprint = & {
        . (Join-Path $PSScriptRoot 'sync-arin-v5-7-calibration.ps1') -Character $characterName -FunctionsOnly
        Get-ProfileFingerprint
    }
    'Const TEST_' + $characterName.ToUpperInvariant() + '_PROFILE_FINGERPRINT = "' + $fingerprint + '"'
}
$startupIndex = $viewerSource.IndexOf('Game Window "')
$helperIndex = $viewerSource.IndexOf('Sub LoadViewer()')
if ($startupIndex -lt 0 -or $helperIndex -le $startupIndex) { throw 'Viewer startup boundaries changed; update the isolation harness.' }
# Mechanical test-input assembly: retain the actual declarations and every actual
# Viewer procedure, replacing only its interactive startup with bounded checks.
$testPrefix = $viewerSource.Substring(0, $startupIndex)
$testPrefix = $testPrefix.Replace(
    "Import Smile.Simple3D.FireEmitter3D As Fire`n",
    "Import Smile.Simple3D.FireEmitter3D As Fire`n" +
        "Import Smile.Simple3D.LightningVfx3D As Lightning`n" +
        "Import Smile.Tools.ArinShieldRim As ArinShieldRim`n")
$testSource = $testPrefix + ($profileConstants -join "`n") + "`n`n" +
    $testStartup + "`n" + $viewerSource.Substring($helperIndex)
$encoding = [Text.UTF8Encoding]::new($false)
[IO.File]::WriteAllText((Join-Path $testRoot 'Program.smile'), $testSource, $encoding)
[xml]$project = Get-Content -LiteralPath (Join-Path $toolRoot 'Character3DViewer.smileproj') -Raw
# Disposable parser fixtures use canonical JSON, never the user's writable saves.
$fixtureRoot = Join-Path $testRoot 'ImportFixtures'
$null = New-Item -ItemType Directory -Path $fixtureRoot -Force
$arinJson = Get-Content -LiteralPath (Join-Path $repositoryRoot 'games/SinStarI/SourceAssets/Characters/Paladin/ArinV57/Calibration/arin-v5.7-pose-calibration.json') -Raw
$importCases = [Collections.Generic.List[string]]::new()
function Add-ImportCase([scriptblock]$Mutate) {
    $snapshot = $arinJson | ConvertFrom-Json -AsHashtable
    & $Mutate $snapshot
    $importCases.Add(($snapshot | ConvertTo-Json -Depth 24 -Compress))
}
foreach ($field in @('assetId','characterVersion','applicationId','dataKey')) {
    Add-ImportCase { param($snapshot) $snapshot[$field] = 'wrong' }
}
foreach ($field in @('modelSha256','descriptorSha256','sm3dSha256','clipNamesSha256','socketNamesSha256')) {
    Add-ImportCase { param($snapshot) $snapshot.profile[$field] = '0' * 64 }
}
Add-ImportCase { param($snapshot) $snapshot.schemaVersion = 1 }
Add-ImportCase { param($snapshot) $snapshot.storageVersion = 2 }
Add-ImportCase { param($snapshot) $snapshot.profile.version = 2 }
Add-ImportCase { param($snapshot) $snapshot.totalKeyframes++ }
Add-ImportCase { param($snapshot) $snapshot.extra = 1 }
Add-ImportCase { param($snapshot) $snapshot.profile.extra = 1 }
Add-ImportCase { param($snapshot) $snapshot.clips[0].name = 'UnknownClip' }
Add-ImportCase { param($snapshot) $snapshot.clips[1].name = $snapshot.clips[0].name }
Add-ImportCase { param($snapshot) $snapshot.savedKeyframe.clipName = 'UnknownClip' }
Add-ImportCase { param($snapshot) $snapshot.savedKeyframe.frame = 65535 }
Add-ImportCase { param($snapshot) $snapshot.Remove('profile') }
Add-ImportCase { param($snapshot) $snapshot.clips[0].keyframes[0].frame = -1 }
Add-ImportCase { param($snapshot) $snapshot.clips[0].keyframes[0].frame = 65535 }
Add-ImportCase { param($snapshot) $snapshot.clips[0].keyframes[0].sword.rotation[0] = 181 }
Add-ImportCase { param($snapshot) $snapshot.clips[0].keyframes[0].shield.position[0] = 101 }
Add-ImportCase { param($snapshot) $snapshot.clips[0].keyframes[0].sword.decoupled = 1 }
Add-ImportCase { param($snapshot) $snapshot.clips[0].keyframes[0].sword.rotation = @(1,2) }
Add-ImportCase { param($snapshot) $snapshot.clips[0].keyframes[0].sword.rotation = @(1,2,3,4) }
Add-ImportCase { param($snapshot) $snapshot.clips[0].keyframes[0].Remove('swordWrist') }
Add-ImportCase { param($snapshot) $snapshot.clips[0].keyframes[0].extra = 1 }
Add-ImportCase { param($snapshot) $snapshot.clips[0].keyframes += $snapshot.clips[0].keyframes[0]; $snapshot.totalKeyframes++ }
Add-ImportCase { param($snapshot) $snapshot.clips[0].keyframes = @(1..257 | ForEach-Object { $snapshot.clips[0].keyframes[0] }) }
$compact = ($arinJson | ConvertFrom-Json | ConvertTo-Json -Depth 24 -Compress)
$importCases.Add($compact.Replace('"schemaVersion":2', '"schemaVersion":2,"schemaVersion":2'))
$importCases.Add($compact.Replace('"schemaVersion":2', '"schemaVersion":02'))
$importCases.Add($compact.Replace('"schemaVersion":2', '"schemaVersion":2.0'))
$importCases.Add($compact.Replace('"schemaVersion":2', '"schemaVersion":2e0'))
$importCases.Add($compact.Replace('"schemaVersion":2', '"schemaVersion":999999999999999999999999'))
$importCases.Add($compact + '{}')
$importCases.Add($compact.Substring(0, $compact.Length - 1) + ',}')
$importCases.Add($compact.Substring(0, $compact.Length - 12))
$importCases.Add($compact.Replace('"assetId":', '"asset\qId":'))
$importCases.Add($compact.Replace('"assetId":', '"asset\u0000Id":'))
$importCases.Add($compact.Replace('sin-star-i.character-1.paladin', ('x' * 129)))
$fixtureTexts = [ordered]@{ 'canonical.json' = $arinJson }
$reordered = $arinJson | ConvertFrom-Json -AsHashtable
$reordered.clips = @($reordered.clips | Sort-Object name -Descending)
foreach ($clip in $reordered.clips) { $clip.index = 0 }
$reordered.savedKeyframe.clipIndex = 0
$rootReversed = [ordered]@{}
foreach ($key in @($reordered.Keys | Sort-Object -Descending)) { $rootReversed[$key] = $reordered[$key] }
$fixtureTexts['reordered.json'] = ($rootReversed | ConvertTo-Json -Depth 24 -Compress).Replace('"assetId"', '"\u0061ssetId"')
$fixtureTexts['orin.json'] = Get-Content -LiteralPath (Join-Path $repositoryRoot 'games/SinStarI/SourceAssets/Characters/Tank/OrinV13/Calibration/orin-v1.3-pose-calibration.json') -Raw
for ($caseIndex = 0; $caseIndex -lt $importCases.Count; $caseIndex++) { $fixtureTexts["reject-$caseIndex.json"] = $importCases[$caseIndex] }
foreach ($entry in $fixtureTexts.GetEnumerator()) {
    [IO.File]::WriteAllText((Join-Path $fixtureRoot $entry.Key), $entry.Value, $encoding)
    $asset = $project.CreateElement('Asset')
    $asset.SetAttribute('Include', "ImportFixtures\$($entry.Key)")
    $null = $project.SmileProject.ItemGroup.AppendChild($asset)
}
$testSource = $testSource.Replace('Const TEST_IMPORT_REJECTION_COUNT = 0', "Const TEST_IMPORT_REJECTION_COUNT = $($importCases.Count)")
[IO.File]::WriteAllText((Join-Path $testRoot 'Program.smile'), $testSource, $encoding)
$applicationId = "smile.tests.viewer-calibration.run-$([Guid]::NewGuid().ToString('N'))"
$project.SmileProject.PropertyGroup.ApplicationId = $applicationId
$project.SmileProject.PropertyGroup.RememberWindowPlacement = 'false'
# This generated project can also be compiled for Web. Keep optional branding
# relative to its new project directory rather than the original tool directory.
if ($project.SmileProject.PropertyGroup.WebLoadingLogo) {
    $logoSource = [IO.Path]::GetFullPath((Join-Path $toolRoot $project.SmileProject.PropertyGroup.WebLoadingLogo))
    $project.SmileProject.PropertyGroup.WebLoadingLogo = [IO.Path]::GetRelativePath($testRoot, $logoSource)
}
# Seed the isolated application with real canonical snapshots. Empty storage hid
# a stale Orin runtime fingerprint during the JumpAttack asset migration.
$nativeIdentityHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData(
    [Text.Encoding]::UTF8.GetBytes($applicationId))).ToLowerInvariant()
$testDataRoot = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) `
    "SMILE 2.0\Games\$nativeIdentityHash\Data"
# A directory at one disposable probe filename forces a real filesystem write failure.
$deniedKeyHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData(
    [Text.Encoding]::UTF8.GetBytes('Viewer Denied Storage Probe'))).ToLowerInvariant()
$null = New-Item -ItemType Directory -Path (Join-Path $testDataRoot "$deniedKeyHash.bin") -Force
foreach ($characterName in @('Arin', 'Orin')) {
    & (Join-Path $PSScriptRoot 'sync-arin-v5-7-calibration.ps1') `
        -Character $characterName -Mode Restore -DataRoot $testDataRoot
}
# Include external model textures alongside their glTF/GLB cooking inputs.
Copy-Item -LiteralPath (Join-Path $toolRoot 'BuildAssets') -Destination $testRoot -Recurse -Force
foreach ($entry in $project.SmileProject.ItemGroup.ChildNodes) {
    if ($entry.Name -eq 'SmileSource' -and $entry.Include -eq 'Program.smile') { continue }
    if ($entry.Name -in @('Asset','Model3DAsset')) {
        if ($entry.Include.StartsWith('ImportFixtures\', [StringComparison]::Ordinal)) { continue }
        foreach ($attribute in @('Include','Descriptor')) {
            if (-not $entry.HasAttribute($attribute)) { continue }
            $relative = $entry.GetAttribute($attribute)
            $destination = Join-Path $testRoot $relative
            $null = New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($destination)) -Force
            Copy-Item -LiteralPath (Join-Path $toolRoot $relative) -Destination $destination -Force
        }
        continue
    }
    if ($entry.HasAttribute('Include')) { $entry.SetAttribute('Include', [IO.Path]::GetFullPath((Join-Path $toolRoot $entry.Include))) }
    if ($entry.HasAttribute('Descriptor')) { $entry.SetAttribute('Descriptor', [IO.Path]::GetFullPath((Join-Path $toolRoot $entry.Descriptor))) }
}
$projectPath = Join-Path $testRoot 'CalibrationTests.smileproj'
$project.Save($projectPath)
$compiler = Join-Path $repositoryRoot 'artifacts\compiler\smilec.exe'
$executable = Join-Path $testRoot 'CalibrationTests.exe'
& $compiler --project $projectPath --target windows-x64 --configuration Release -o $executable
if ($LASTEXITCODE -ne 0) { throw 'Isolated Viewer compile failed.' }
$output = Join-Path $testRoot 'native.out'
$result = & (Join-Path $PSScriptRoot 'run-bounded-test.cmd') 60 $executable
$runExitCode = $LASTEXITCODE
$result = $result -join "`n"
[IO.File]::WriteAllText($output, $result + "`n", $encoding)
if ($runExitCode -ne 0) { throw "Isolated Viewer execution failed ($runExitCode); see $output." }
$jsonLines = @($result -split "`n" | Where-Object { $_.StartsWith('CALIBRATION_JSON: ') })
if ($jsonLines.Count -ne 2) { throw 'Expected both current-character JSON exports.' }
foreach ($jsonLine in $jsonLines) {
    & {
        param($Text)
        if ($Text.Length -le 'CALIBRATION_JSON: '.Length) { throw 'Viewer returned an empty JSON export.' }
        $snapshot = $Text.Substring('CALIBRATION_JSON: '.Length) | ConvertFrom-Json -AsHashtable
        $characterName = if ($snapshot.assetId -ceq 'sin-star-i.character-1.paladin') { 'Arin' } else { 'Orin' }
        . (Join-Path $PSScriptRoot 'sync-arin-v5-7-calibration.ps1') -Character $characterName -FunctionsOnly
        $normalized = Normalize-Snapshot $snapshot
        $canonical = Read-Snapshot $snapshotPath
        if (($normalized | ConvertTo-Json -Depth 24 -Compress) -cne
            ($canonical | ConvertTo-Json -Depth 24 -Compress)) {
            throw "Shared Viewer JSON export differs from canonical $characterName."
        }
        $payload = Convert-SnapshotToPayload $normalized
        $roundTrip = Convert-PayloadToSnapshot $payload
        if (($roundTrip | ConvertTo-Json -Depth 24 -Compress) -cne
            ($canonical | ConvertTo-Json -Depth 24 -Compress)) {
            throw 'Downloaded JSON cannot round-trip through the native serializer.'
        }
    } $jsonLine
}
$assertionOutput = ($result -split "`n" | Where-Object { -not $_.StartsWith('CALIBRATION_JSON: ') }) -join "`n"
if ($assertionOutput.Trim() -cne 'Viewer calibration isolation passed') { throw "Native Viewer checks failed; see $output." }
Write-Host $assertionOutput.Trim()
Write-Host 'Both shared JSON exports exactly match canonical snapshots and native round-trips.'
Write-Host "Isolated ApplicationId: $applicationId; canonical snapshot copies loaded; live user storage untouched."
$nativeIdentity = $applicationId
$nativeIdentityHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData(
    [Text.Encoding]::UTF8.GetBytes($nativeIdentity))).ToLowerInvariant()
$nativeKeyHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData(
    [Text.Encoding]::UTF8.GetBytes('CharacterViewerCalibrationKeyframes'))).ToLowerInvariant()
$nativeDataPath = Join-Path $env:LOCALAPPDATA "SMILE 2.0\Games\$nativeIdentityHash\Data\$nativeKeyHash.bin"
. (Join-Path $PSScriptRoot 'sync-arin-v5-7-calibration.ps1') -FunctionsOnly -DataRoot $testRoot
foreach ($savedPath in @($nativeDataPath, "$nativeDataPath.bak")) {
    $snapshot = Convert-PayloadToSnapshot (Read-LivePayload $savedPath)
    if ($snapshot.totalKeyframes -ne 1 -or $snapshot.storageVersion -ne 3) {
        throw "Native Save Data/backup did not preserve the named full-channel snapshot: $savedPath"
    }
}
Write-Host 'Native Save Data and its previous-good backup both passed checksum/profile validation.'
