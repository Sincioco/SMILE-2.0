[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$viewerRoot = Join-Path $root 'tools\Character3DViewer'
$testRoot = Join-Path $root ('artifacts\tests\StudioSession-' + [Guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $testRoot
$compiler = Join-Path $root 'artifacts\compiler\smilec.exe'
[xml]$project = Get-Content (Join-Path $viewerRoot 'Character3DViewer.smileproj') -Raw
$project.SmileProject.PropertyGroup.RememberWindowPlacement = 'false'
$shellSource = $project.CreateElement('SmileSource')
$shellSource.SetAttribute('Include', '..\SmileStudio\StudioShell.smile')
$null = $project.SmileProject.ItemGroup.AppendChild($shellSource)
$project.SmileProject.PropertyGroup.WebLoadingLogo = [IO.Path]::GetRelativePath($testRoot,
    (Join-Path $root 'assets\branding\smile-2.0-logo-web.png'))
Copy-Item -LiteralPath (Join-Path $viewerRoot 'BuildAssets') -Destination $testRoot -Recurse
foreach ($entry in $project.SmileProject.ItemGroup.ChildNodes) {
    if ($entry.Name -eq 'SmileSource' -and $entry.Include -eq 'Program.smile') { continue }
    if ($entry.Name -eq 'Model3DAsset') { continue }
    if ($entry.Name -eq 'Asset') {
        foreach ($source in Get-ChildItem -Path (Join-Path $viewerRoot $entry.Include) -File) {
            $relative = [IO.Path]::GetRelativePath($viewerRoot, $source.FullName)
            $destination = Join-Path $testRoot $relative
            $null = New-Item -ItemType Directory -Force -Path (Split-Path $destination -Parent)
            Copy-Item -LiteralPath $source.FullName -Destination $destination
        }
    } else {
        foreach ($attribute in @('Include','Descriptor')) {
            if ($entry.HasAttribute($attribute)) {
                $sourcePath = [IO.Path]::GetFullPath((Join-Path $viewerRoot $entry.GetAttribute($attribute)))
                $entry.SetAttribute($attribute, [IO.Path]::GetRelativePath($testRoot, $sourcePath))
            }
        }
    }
}
$startup = Get-Content (Join-Path $root 'tools\SmileStudio\SessionTests.smile') -Raw
$expected = Join-Path $testRoot 'expected.txt'
[IO.File]::WriteAllText($expected, "Studio session isolation passed.`n")
foreach ($fault in @($false, $true)) {
    $case = if ($fault) { 'WriteFailure' } else { 'Success' }
    $identity = 'smile.tests.studio.run-' + [Guid]::NewGuid().ToString('N')
    $project.SmileProject.PropertyGroup.ApplicationId = $identity
    $source = if ($fault) { $startup.Replace('Const EXPECT_SAVE_SUCCESS = True', 'Const EXPECT_SAVE_SUCCESS = False') } else { $startup }
    [IO.File]::WriteAllText((Join-Path $testRoot 'Program.smile'), $source)
    $projectPath = Join-Path $testRoot 'SessionTests.smileproj'
    $project.Save($projectPath)
    $identityHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($identity))).ToLowerInvariant()
    $dataRoot = Join-Path $env:LOCALAPPDATA "SMILE 2.0\Games\$identityHash\Data"
    foreach ($character in @('Arin', 'Orin')) {
        & (Join-Path $PSScriptRoot 'sync-arin-v5-7-calibration.ps1') -Character $character -Mode Restore -DataRoot $dataRoot
    }
    $keyHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes('CharacterViewerCalibrationKeyframes'))).ToLowerInvariant()
    $primary = Join-Path $dataRoot "$keyHash.bin"
    $before = (Get-FileHash -LiteralPath $primary).Hash
    $exe = Join-Path $testRoot "Session$case.exe"
    & $compiler --project $projectPath --target windows-x64 -o $exe
    if ($LASTEXITCODE -ne 0) { throw "$case native compilation failed." }
    $lock = $null
    try {
        if ($fault) {
            # Preserve the last valid isolated file while denying atomic replacement.
            $lock = [IO.File]::Open($primary, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::ReadWrite)
        }
        $actual = & (Join-Path $PSScriptRoot 'run-bounded-test.cmd') 40 $exe
        if ($LASTEXITCODE -ne 0 -or ($actual -join "`n").Trim() -ne 'Studio session isolation passed.') {
            throw "$case native execution failed: $actual"
        }
    } finally { if ($lock) { $lock.Dispose() } }
    if ($fault -and (Get-FileHash -LiteralPath $primary).Hash -ne $before) {
        throw 'Failed save changed the last valid isolated Arin snapshot.'
    }
    $web = Join-Path $testRoot "Web$case"
    & $compiler --project $projectPath --target web --output-dir $web
    if ($LASTEXITCODE -ne 0) { throw "$case Web compilation failed." }
    $arguments = @((Join-Path $PSScriptRoot 'run-web-test.js'), $web, '--renderer3d-state', '--expected', $expected)
    if ($fault) { $arguments += @('--deny-data-key','CharacterViewerCalibrationKeyframes') }
    & node @arguments
    if ($LASTEXITCODE -ne 0) { throw "$case generated Web logic failed." }
    if ($fault) {
        $html = [IO.File]::ReadAllText((Join-Path $web 'index.html'))
        $injection = @'
<script>
const originalStorageWrite = Storage.prototype.setItem;
Storage.prototype.setItem = function(key, value) {
    if (key.endsWith(":data:ARIN_HASH")) throw new DOMException("Isolated quota failure", "QuotaExceededError");
    return originalStorageWrite.call(this, key, value);
};
</script>
'@
        $injection = $injection.Replace('ARIN_HASH', $keyHash)
        $html = $html.Insert($html.IndexOf('<script>'), $injection)
        [IO.File]::WriteAllText((Join-Path $web 'index.html'), $html)
    }
    Write-Host "PASS $case : native session and generated Web logic; isolated storage $identity."
}
Write-Host "Chrome execution fixtures: $testRoot\WebSuccess and WebWriteFailure."
