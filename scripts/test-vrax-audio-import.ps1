# Isolated importer faults: no licensed media or canonical package writes.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$fixture = Join-Path $repo ('artifacts/temp/vrax-audio-import-' + [Guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $fixture
. (Join-Path $repo 'games/SinStarI/SourceAssets/Bosses/Vrax/VraxV1/Import-UnityAudio.ps1') -FunctionsOnly
$realConvert = (Get-Item Function:Convert-VraxAudio).ScriptBlock
$realMetadata = (Get-Item Function:Write-AudioMetadata).ScriptBlock
$realPublish = (Get-Item Function:Publish-AudioFile).ScriptBlock

function Assert($Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
function Write-Wave([string]$Path, [int]$Sample) {
    $null = New-Item -ItemType Directory -Force -Path ([IO.Path]::GetDirectoryName($Path))
    $writer = [IO.BinaryWriter]::new([IO.File]::Create($Path))
    try {
        $writer.Write([Text.Encoding]::ASCII.GetBytes('RIFF')); $writer.Write([uint32]436)
        $writer.Write([Text.Encoding]::ASCII.GetBytes('WAVEfmt ')); $writer.Write([uint32]16)
        $writer.Write([uint16]1); $writer.Write([uint16]2); $writer.Write([uint32]44100)
        $writer.Write([uint32]176400); $writer.Write([uint16]4); $writer.Write([uint16]16)
        $writer.Write([Text.Encoding]::ASCII.GetBytes('data')); $writer.Write([uint32]400)
        for ($i = 0; $i -lt 200; $i++) { $writer.Write([int16]$Sample) }
    } finally { $writer.Dispose() }
}
function Snapshot([string]$Root) {
    $result = [ordered]@{}
    foreach ($file in Get-ChildItem -LiteralPath $Root -Recurse -File | Sort-Object FullName) {
        $result[[IO.Path]::GetRelativePath($Root, $file.FullName)] = Get-AudioHash $file.FullName
    }
    return ($result | ConvertTo-Json -Compress)
}
$seedUnity = Join-Path $fixture 'unity'
$scriptFile = Join-Path $seedUnity 'Assets/Characters/Sci-Fi Beast01/SciFiBeast01.cs'
$null = New-Item -ItemType Directory -Force -Path (Split-Path $scriptFile)
[IO.File]::WriteAllText($scriptFile, 'gameManager.VFX.Sound14.Play(); gameManager.VFX.Sound13.PlayDelayed(.2f); gameManager.VFX.Sound15.Play();')
[IO.File]::WriteAllText("$scriptFile.meta", 'guid: aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa')
$scene = @'
--- !u!114 &1
MonoBehaviour:
  m_Script: {fileID: 11500000, guid: aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa, type: 3}
  gameManager: {fileID: 2}
--- !u!114 &2
MonoBehaviour:
  VFX: {fileID: 3}
--- !u!114 &3
MonoBehaviour:
  Sound13: {fileID: 13}
  Sound14: {fileID: 14}
  Sound15: {fileID: 15}
--- !u!114 &4
MonoBehaviour:
  Sound13: {fileID: 999}
  Sound14: {fileID: 999}
  Sound15: {fileID: 999}
'@
foreach ($slot in 13..15) {
    $guid = $slot.ToString('x32')
    $source = Join-Path $seedUnity "Assets/Sounds/$slot/same-name.wav"
    Write-Wave $source $slot
    [IO.File]::WriteAllText("$source.meta", "guid: $guid")
    $scene += [Environment]::NewLine + "--- !u!82 &$slot" + [Environment]::NewLine +
        "AudioSource:" + [Environment]::NewLine + "  m_audioClip: {fileID: 8300000, guid: $guid, type: 3}" + [Environment]::NewLine
}
$sceneFile = Join-Path $seedUnity 'Assets/Scenes/BattleSystem.unity'
$null = New-Item -ItemType Directory -Force -Path (Split-Path $sceneFile)
[IO.File]::WriteAllText($sceneFile, $scene)
$seedPackage = Join-Path $fixture 'seed'
$null = New-Item -ItemType Directory -Path $seedPackage
[IO.File]::WriteAllText((Join-Path $seedPackage 'package-manifest.json'), '{"character":"Vrax","protected":"keep"}')
$UnityProject = $seedUnity; $PackageRoot = $seedPackage; $ConverterPath = (Get-Command ffmpeg).Source
Invoke-VraxAudioImport
$report = Get-Content -LiteralPath (Join-Path $seedPackage 'unity-audio-import.json') -Raw | ConvertFrom-Json
Assert (@($report.sources.packageOriginal | Select-Object -Unique).Count -eq 3) 'Same basenames collided.'
foreach ($source in $report.sources) {
    Assert ((Get-AudioHash (Join-Path $seedPackage $source.packageOriginal)) -eq $source.sha256) 'Original bytes changed.'
}
$before = Snapshot $seedPackage
$writeTimes = @(Get-ChildItem -LiteralPath $seedPackage -File -Recurse | ForEach-Object LastWriteTimeUtc)
$output = Invoke-VraxAudioImport
Assert ($output -match '0 files changed') 'Identical import is not idempotent.'
Assert ((Snapshot $seedPackage) -eq $before) 'Idempotent import changed bytes.'
Assert (@(Compare-Object $writeTimes @(Get-ChildItem -LiteralPath $seedPackage -File -Recurse | ForEach-Object LastWriteTimeUtc)).Count -eq 0) 'Idempotent import rewrote files.'

foreach ($fault in @('late-mapping', 'converter', 'invalid-wave', 'metadata-write', 'metadata-replace', 'replace-after', 'concurrent', 'first-import')) {
    $case = Join-Path $fixture $fault
    $null = New-Item -ItemType Directory -Path $case
    Copy-Item -LiteralPath $seedUnity -Destination (Join-Path $case 'unity') -Recurse
    $UnityProject = Join-Path $case 'unity'; $PackageRoot = Join-Path $case 'package'
    if ($fault -eq 'first-import') {
        $null = New-Item -ItemType Directory -Path $PackageRoot
        [IO.File]::WriteAllText((Join-Path $PackageRoot 'package-manifest.json'), '{"character":"Vrax"}')
    } else { Copy-Item -LiteralPath $seedPackage -Destination $PackageRoot -Recurse }
    foreach ($slot in 13..15) { Write-Wave (Join-Path $UnityProject "Assets/Sounds/$slot/same-name.wav") ($slot + 100) }
    if ($fault -eq 'late-mapping') {
        $badScene = $scene.Replace('  Sound15: {fileID: 15}', '  Sound15: {fileID: 0}')
        [IO.File]::WriteAllText((Join-Path $UnityProject 'Assets/Scenes/BattleSystem.unity'), $badScene)
    }
    $expected = Snapshot $PackageRoot
    function Convert-VraxAudio($Source, $Destination, $Converter) {
        if ($fault -in @('converter', 'first-import') -and $Destination.EndsWith('playback-15.wav')) { throw 'Injected converter failure after two staged clips.' }
        & $realConvert $Source $Destination $Converter
        if ($fault -eq 'invalid-wave') { [IO.File]::WriteAllText($Destination, 'invalid') }
    }
    function Write-AudioMetadata($Path, $Value) {
        if ($fault -eq 'metadata-write' -and $Path.EndsWith('manifest.json')) { throw 'Injected staged metadata failure.' }
        & $realMetadata $Path $Value
        if ($fault -eq 'concurrent' -and $Path.EndsWith('manifest.json')) {
            [IO.File]::WriteAllText((Join-Path $PackageRoot 'package-manifest.json'), '{"concurrent":"preserve me"}')
        }
    }
    function Publish-AudioFile($Staged, $Destination, $Backup) {
        if ($fault -eq 'metadata-replace' -and $Destination.EndsWith('package-manifest.json')) { throw 'Injected final metadata replace failure.' }
        & $realPublish $Staged $Destination $Backup
        if ($fault -eq 'replace-after' -and $Destination.EndsWith('unity-audio-import.json')) { throw 'Injected failure after replacement.' }
    }
    $failed = $false
    try { Invoke-VraxAudioImport } catch { $failed = $true; Write-Host "$fault correctly rejected: $_" }
    Assert $failed "$fault unexpectedly succeeded."
    if ($fault -eq 'concurrent') {
        $snapshot = (Snapshot $PackageRoot) | ConvertFrom-Json -AsHashtable
        $expectedSnapshot = $expected | ConvertFrom-Json -AsHashtable
        $expectedSnapshot['package-manifest.json'] = Get-AudioHash (Join-Path $PackageRoot 'package-manifest.json')
        Assert (($snapshot | ConvertTo-Json -Compress) -eq ($expectedSnapshot | ConvertTo-Json -Compress)) 'Concurrent edit caused other changes.'
        Assert ((Get-Content -LiteralPath (Join-Path $PackageRoot 'package-manifest.json') -Raw) -eq '{"concurrent":"preserve me"}') 'Concurrent edit lost.'
    } else { Assert ((Snapshot $PackageRoot) -eq $expected) "$fault damaged the prior package." }
}
Write-Host "Vrax audio importer isolated success, idempotence and fault checks passed. Evidence: $fixture"
