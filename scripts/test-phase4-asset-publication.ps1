$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$compiler = Join-Path $repositoryRoot 'artifacts\compiler\smilec.exe'
$fixtureRoot = Join-Path $repositoryRoot 'examples\Phase4AssetPublication'
$expectedPaths = @(Get-Content -LiteralPath (Join-Path $fixtureRoot 'ExpectedAssetPaths.txt') |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
$nativeRoot = Join-Path $repositoryRoot 'artifacts\games\Phase4AssetPublication'
$webRoot = Join-Path $repositoryRoot 'artifacts\web\Phase4AssetPublication'
$temporaryRoot = Join-Path $repositoryRoot 'artifacts\temp\Phase4AssetPublicationStale'

function Reset-TestDirectory {
    param([string]$Path)

    $resolvedArtifacts = [IO.Path]::GetFullPath((Join-Path $repositoryRoot 'artifacts')).TrimEnd('\') + '\'
    $resolvedPath = [IO.Path]::GetFullPath($Path)
    if (-not $resolvedPath.StartsWith($resolvedArtifacts, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to reset a Phase 4.2 test directory outside artifacts: $resolvedPath"
    }
    if (Test-Path -LiteralPath $resolvedPath) {
        Remove-Item -LiteralPath $resolvedPath -Recurse -Force
    }
    New-Item -ItemType Directory -Path $resolvedPath -Force | Out-Null
}

function Invoke-SmileCompiler {
    param(
        [string[]]$Arguments,
        [int]$ExpectedExitCode = 0,
        [string]$LogPath = ''
    )

    $savedPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = @(& $compiler @Arguments 2>&1 | ForEach-Object { $_.ToString() })
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $savedPreference
    }
    if ($LogPath) {
        [IO.File]::WriteAllLines($LogPath, $output)
    }
    if ($exitCode -ne $ExpectedExitCode) {
        throw "smilec exited $exitCode instead of $ExpectedExitCode.`n$($output -join [Environment]::NewLine)"
    }
    return $output
}

function Assert-ExactPublication {
    param(
        [string]$OutputRoot,
        [string]$ManifestName,
        [string]$Target
    )

    $assetsRoot = Join-Path $OutputRoot 'Assets'
    $actualPaths = @()
    if (Test-Path -LiteralPath $assetsRoot) {
        $actualPaths = @(Get-ChildItem -LiteralPath $assetsRoot -Recurse -File |
            ForEach-Object { $_.FullName.Substring($OutputRoot.Length + 1).Replace('\', '/') } |
            Sort-Object)
    }
    $difference = @(Compare-Object -ReferenceObject $expectedPaths -DifferenceObject $actualPaths -CaseSensitive)
    if ($difference.Count -ne 0) {
        throw "Published asset set differs beneath $OutputRoot`n$($difference | Out-String)"
    }

    foreach ($relativePath in $expectedPaths) {
        $source = Join-Path $fixtureRoot $relativePath.Replace('/', '\')
        $output = Join-Path $OutputRoot $relativePath.Replace('/', '\')
        if ((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ne
            (Get-FileHash -LiteralPath $output -Algorithm SHA256).Hash) {
            throw "Published asset bytes differ: $relativePath"
        }
    }

    $manifestPath = Join-Path $OutputRoot $ManifestName
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    if ($manifest.formatVersion -ne 1 -or $manifest.applicationIdentity -ne 'Phase4AssetPublication' -or
        $manifest.target -ne $Target) {
        throw "Publication manifest metadata is invalid: $manifestPath"
    }
    $manifestPaths = @($manifest.assets | ForEach-Object { [string]$_ })
    $manifestDifference = @(Compare-Object -ReferenceObject $expectedPaths -DifferenceObject $manifestPaths -CaseSensitive)
    if ($manifestDifference.Count -ne 0 -or $manifestPaths.Count -ne $expectedPaths.Count) {
        throw "Publication manifest asset set differs: $manifestPath"
    }
}

Reset-TestDirectory $nativeRoot
Reset-TestDirectory $webRoot

$fixtureProject = Join-Path $fixtureRoot 'Phase4AssetPublication.smileproj'
Invoke-SmileCompiler -Arguments @('--project', $fixtureProject, '--target', 'windows-x64', '--configuration',
    'Release', '-o', (Join-Path $nativeRoot 'Phase4AssetPublication.exe')) | Out-Null
Invoke-SmileCompiler -Arguments @('--project', $fixtureProject, '--target', 'web', '--configuration',
    'Release', '--output-dir', $webRoot) | Out-Null
Assert-ExactPublication $nativeRoot 'Phase4AssetPublication.smile-assets.json' 'windows-x64'
Assert-ExactPublication $webRoot 'smile-assets.json' 'web'
$webProgram = [IO.File]::ReadAllText((Join-Path $webRoot 'game.js'))
$nativeProgram = [Text.Encoding]::ASCII.GetString(
    [IO.File]::ReadAllBytes((Join-Path $nativeRoot 'Phase4AssetPublication.exe')))
foreach ($relativePath in $expectedPaths) {
    if (-not $webProgram.Contains($relativePath) -or -not $nativeProgram.Contains($relativePath)) {
        throw "Embedded runtime asset list omitted $relativePath."
    }
}
foreach ($excluded in @('Assets/UI/Click.wav', 'Assets/UI/Sub/Nested.png', 'Assets/Audio/Sub/Notes.txt',
    'Assets/Unlisted/Secret.txt')) {
    if ($webProgram.Contains($excluded) -or $nativeProgram.Contains($excluded)) {
        throw "Embedded runtime asset list contained excluded path $excluded."
    }
}

$missingLog = Join-Path $repositoryRoot 'artifacts\temp\Phase4AssetMissing.log'
$missingProject = Join-Path $repositoryRoot 'examples\InvalidPhase4Assets\MissingExplicit\MissingExplicit.smileproj'
$missingOutput = Invoke-SmileCompiler -Arguments @('--project', $missingProject) `
    -ExpectedExitCode 1 -LogPath $missingLog
if (($missingOutput -join "`n") -notmatch 'SML3601' -or ($missingOutput -join "`n") -notmatch '\.smileproj\(9,12\)') {
    throw 'Missing explicit asset did not report SML3601 at the project XML Include location.'
}

$libraryLog = Join-Path $repositoryRoot 'artifacts\temp\Phase4AssetLibrary.log'
$libraryProject = Join-Path $repositoryRoot 'examples\InvalidPhase4Assets\LibraryAsset\LibraryAsset.smilelibproj'
$libraryOutput = Invoke-SmileCompiler -Arguments @('--project', $libraryProject, '--target', 'library', '-o',
    (Join-Path $temporaryRoot 'invalid.smilelib')) -ExpectedExitCode 1 -LogPath $libraryLog
if (($libraryOutput -join "`n") -notmatch 'SML3606') {
    throw 'Library asset declaration did not report SML3606.'
}

Reset-TestDirectory $temporaryRoot
$staleProject = Join-Path $temporaryRoot 'Stale.smileproj'
$staleOutput = Join-Path $temporaryRoot 'Web'
New-Item -ItemType Directory -Path (Join-Path $temporaryRoot 'Assets') -Force | Out-Null
[IO.File]::WriteAllText((Join-Path $temporaryRoot 'Program.smile'), "Print 1`n")
[IO.File]::WriteAllText((Join-Path $temporaryRoot 'Assets\Old.txt'), 'old')
[IO.File]::WriteAllText((Join-Path $temporaryRoot 'Assets\New.txt'), 'new')

function Write-StaleProject {
    param([string]$AssetName)

    [IO.File]::WriteAllText($staleProject, @"
<SmileProject Version="1.0">
  <PropertyGroup><ProjectKind>Console</ProjectKind><StartupFile>Program.smile</StartupFile><OutputName>Stale</OutputName></PropertyGroup>
  <ItemGroup><SmileSource Include="Program.smile" StartupOnly="true" /><Asset Include="Assets\$AssetName" /></ItemGroup>
</SmileProject>
"@)
}

Write-StaleProject 'Old.txt'
Invoke-SmileCompiler -Arguments @('--project', $staleProject, '--target', 'web', '--output-dir', $staleOutput) | Out-Null
[IO.File]::WriteAllText((Join-Path $staleOutput 'sentinel.txt'), 'unrelated')
Write-StaleProject 'New.txt'
Invoke-SmileCompiler -Arguments @('--project', $staleProject, '--target', 'web', '--output-dir', $staleOutput) | Out-Null
if (Test-Path -LiteralPath (Join-Path $staleOutput 'Assets\Old.txt')) { throw 'Stale managed asset remained.' }
foreach ($path in @('Assets\New.txt', 'game.js', 'index.html', 'smile-runtime.js', 'smile.css', 'sentinel.txt')) {
    if (-not (Test-Path -LiteralPath (Join-Path $staleOutput $path) -PathType Leaf)) {
        throw "Stale cleanup removed or omitted required output: $path"
    }
}

$nativeStaleRoot = Join-Path $temporaryRoot 'Native'
New-Item -ItemType Directory -Path $nativeStaleRoot -Force | Out-Null
Write-StaleProject 'Old.txt'
Invoke-SmileCompiler -Arguments @('--project', $staleProject, '--target', 'windows-x64', '-o',
    (Join-Path $nativeStaleRoot 'Stale.exe')) | Out-Null
[IO.File]::WriteAllText((Join-Path $nativeStaleRoot 'sentinel.txt'), 'unrelated')
Write-StaleProject 'New.txt'
Invoke-SmileCompiler -Arguments @('--project', $staleProject, '--target', 'windows-x64', '-o',
    (Join-Path $nativeStaleRoot 'Stale.exe')) | Out-Null
if (Test-Path -LiteralPath (Join-Path $nativeStaleRoot 'Assets\Old.txt')) {
    throw 'Native stale managed asset remained.'
}
foreach ($path in @('Assets\New.txt', 'Stale.exe', 'Stale.smile-assets.json', 'sentinel.txt')) {
    if (-not (Test-Path -LiteralPath (Join-Path $nativeStaleRoot $path) -PathType Leaf)) {
        throw "Native stale cleanup removed or omitted required output: $path"
    }
}

$identityRoot = Join-Path $temporaryRoot 'Identity'
$identityOutput = Join-Path $identityRoot 'Output'
$identityProject = Join-Path $identityRoot 'Identity.smileproj'
New-Item -ItemType Directory -Path (Join-Path $identityRoot 'Assets') -Force | Out-Null
New-Item -ItemType Directory -Path $identityOutput -Force | Out-Null
[IO.File]::WriteAllText((Join-Path $identityRoot 'Program.smile'), "Print 1`n")
[IO.File]::WriteAllText((Join-Path $identityRoot 'Assets\A.txt'), 'A')
[IO.File]::WriteAllText((Join-Path $identityRoot 'Assets\B.txt'), 'B')

function Write-IdentityProject {
    param([string]$OutputName, [string[]]$AssetNames)

    $assetItems = $AssetNames | ForEach-Object { "<Asset Include=`"Assets\$_`" />" }
    [IO.File]::WriteAllText($identityProject, @"
<SmileProject Version="1.0">
  <PropertyGroup><ProjectKind>Console</ProjectKind><StartupFile>Program.smile</StartupFile><OutputName>$OutputName</OutputName><ApplicationId>smile.tests.asset-identity</ApplicationId></PropertyGroup>
  <ItemGroup><SmileSource Include="Program.smile" StartupOnly="true" />$($assetItems -join '')</ItemGroup>
</SmileProject>
"@)
}

Write-IdentityProject 'OldName' @('A.txt', 'B.txt')
Invoke-SmileCompiler -Arguments @('--project', $identityProject, '--target', 'windows-x64', '-o',
    (Join-Path $identityOutput 'OldName.exe')) | Out-Null
$stableIdentityManifest = Join-Path $identityOutput 'smile.tests.asset-identity.smile-assets.json'
$legacyIdentityManifest = Join-Path $identityOutput 'OldName.smile-assets.json'
if (-not (Test-Path -LiteralPath $stableIdentityManifest -PathType Leaf)) {
    throw 'Explicit ApplicationId did not select a stable native manifest filename.'
}
Move-Item -LiteralPath $stableIdentityManifest -Destination $legacyIdentityManifest
[IO.File]::WriteAllText((Join-Path $identityOutput 'sentinel.txt'), 'unrelated')
$mismatchedManifest = Join-Path $identityOutput 'Mismatched.smile-assets.json'
$malformedManifest = Join-Path $identityOutput 'Malformed.smile-assets.json'
[IO.File]::WriteAllText($mismatchedManifest,
    '{"formatVersion":1,"applicationIdentity":"smile.tests.other","target":"windows-x64","assets":["sentinel.txt"]}')
[IO.File]::WriteAllText($malformedManifest, '{not-json')

Write-IdentityProject 'NewName' @('A.txt')
$identityMigrationOutput = Invoke-SmileCompiler -Arguments @('--project', $identityProject,
    '--target', 'windows-x64', '-o', (Join-Path $identityOutput 'NewName.exe'))
if (($identityMigrationOutput -join "`n") -notmatch 'SML3605') {
    throw 'Malformed legacy native manifest did not report SML3605.'
}
if (-not (Test-Path -LiteralPath $stableIdentityManifest -PathType Leaf)) {
    throw 'Stable ApplicationId manifest was not written after OutputName migration.'
}
if (Test-Path -LiteralPath $legacyIdentityManifest) {
    throw 'Validated matching legacy native manifest was not removed after migration.'
}
if (Test-Path -LiteralPath (Join-Path $identityOutput 'Assets\B.txt')) {
    throw 'OutputName rename left a stale managed asset from the validated legacy manifest.'
}
foreach ($path in @('Assets\A.txt', 'NewName.exe', 'sentinel.txt',
        'Mismatched.smile-assets.json', 'Malformed.smile-assets.json')) {
    if (-not (Test-Path -LiteralPath (Join-Path $identityOutput $path) -PathType Leaf)) {
        throw "ApplicationId migration removed or omitted required output: $path"
    }
}

$outside = Join-Path $temporaryRoot 'outside.txt'
[IO.File]::WriteAllText($outside, 'untouched')
[IO.File]::WriteAllText((Join-Path $staleOutput 'smile-assets.json'),
    '{"formatVersion":1,"applicationIdentity":"Stale","target":"web","assets":["../outside.txt"]}')
$corruptOutput = Invoke-SmileCompiler -Arguments @('--project', $staleProject, '--target', 'web',
    '--output-dir', $staleOutput)
if (($corruptOutput -join "`n") -notmatch 'SML3605') { throw 'Unsafe prior manifest did not report SML3605.' }
if ([IO.File]::ReadAllText($outside) -ne 'untouched') { throw 'Unsafe prior manifest modified an out-of-root file.' }
$safeManifest = Get-Content -LiteralPath (Join-Path $staleOutput 'smile-assets.json') -Raw | ConvertFrom-Json
if (@($safeManifest.assets).Count -ne 1 -or $safeManifest.assets[0] -ne 'Assets/New.txt') {
    throw 'Unsafe prior manifest was not replaced with a safe current manifest.'
}

Write-Host 'Phase 4.2 exact native/Web publication, diagnostics, stale cleanup, and corrupt-manifest safety passed.'
