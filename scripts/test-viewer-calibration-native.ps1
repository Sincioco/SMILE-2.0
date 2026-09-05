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
$testSource = $viewerSource.Substring(0, $startupIndex) + ($profileConstants -join "`n") + "`n`n" +
    $testStartup + "`n" + $viewerSource.Substring($helperIndex)
$encoding = [Text.UTF8Encoding]::new($false)
[IO.File]::WriteAllText((Join-Path $testRoot 'Program.smile'), $testSource, $encoding)
[xml]$project = Get-Content -LiteralPath (Join-Path $toolRoot 'Character3DViewer.smileproj') -Raw
$applicationId = "smile.tests.viewer-calibration.run-$([Guid]::NewGuid().ToString('N'))"
$project.SmileProject.PropertyGroup.ApplicationId = $applicationId
$project.SmileProject.PropertyGroup.RememberWindowPlacement = 'false'
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
if ($LASTEXITCODE -ne 0) { throw 'Isolated Viewer execution failed.' }
$result = $result -join "`n"
[IO.File]::WriteAllText($output, $result + "`n", $encoding)
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
if ($assertionOutput.Trim() -cne 'Viewer calibration isolation passed') { throw "Native Viewer checks failed: $result" }
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
