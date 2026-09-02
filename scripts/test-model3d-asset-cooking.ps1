[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$compiler = Join-Path $repositoryRoot 'artifacts\compiler\smilec.exe'
$runtime = Join-Path $repositoryRoot 'artifacts\compiler\Smile.NativeRuntime.lib'
$sourceModel = Join-Path $repositoryRoot `
    'games\Dragonfall\SourceAssets\Arin\sin-star-i-character-1-paladin-tripo-v01.original.glb'
$sourceDescriptor = Join-Path $repositoryRoot `
    'games\Dragonfall\SourceAssets\Arin\ArinPrototype.sm3d.json'
$temporaryRoot = Join-Path $repositoryRoot 'artifacts\temp\model3d-asset-cooking-tests'
$resolvedTemporaryRoot = [System.IO.Path]::GetFullPath($temporaryRoot)
$resolvedArtifactsRoot = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot 'artifacts\temp')).
    TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar

if (-not $resolvedTemporaryRoot.StartsWith(
        $resolvedArtifactsRoot,
        [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'Model3DAsset test directory escaped artifacts\temp.'
}
if (-not (Test-Path -LiteralPath $compiler -PathType Leaf) -or
    -not (Test-Path -LiteralPath $runtime -PathType Leaf)) {
    throw 'Built compiler/runtime artifacts are missing. Run scripts\build.cmd first.'
}

function Invoke-Compiler([string[]]$Arguments, [string]$LogPath, [int]$ExpectedExit = 0) {
    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = (& $compiler @Arguments 2>&1) -join "`n"
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }
    [System.IO.File]::WriteAllText($LogPath, $output + "`n", [System.Text.UTF8Encoding]::new($false))
    if ($exitCode -ne $ExpectedExit) {
        throw "Compiler exit $exitCode did not match expected $ExpectedExit.`n$output"
    }
    return $output
}

function Get-Sha256([string]$Path) {
    $algorithm = [System.Security.Cryptography.SHA256]::Create()
    $stream = [System.IO.File]::OpenRead($Path)
    try {
        return [BitConverter]::ToString($algorithm.ComputeHash($stream)).Replace('-', '')
    }
    finally {
        $stream.Dispose()
        $algorithm.Dispose()
    }
}

function Get-AssetHashes([string]$Root) {
    $assets = Get-ChildItem -LiteralPath (Join-Path $Root 'Assets') -File -Recurse |
        Sort-Object { $_.FullName.Substring($Root.Length) }
    return @($assets | ForEach-Object {
        $logical = $_.FullName.Substring($Root.Length).TrimStart('\').Replace('\', '/')
        $hash = Get-Sha256 $_.FullName
        "$logical=$hash"
    })
}

if (Test-Path -LiteralPath $resolvedTemporaryRoot) {
    Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force
}

try {
    [System.IO.Directory]::CreateDirectory((Join-Path $resolvedTemporaryRoot 'Source')) | Out-Null
    Copy-Item -LiteralPath $sourceModel -Destination (Join-Path $resolvedTemporaryRoot 'Source\Arin.glb')
    Copy-Item -LiteralPath $sourceDescriptor -Destination `
        (Join-Path $resolvedTemporaryRoot 'Source\Arin.sm3d.json')
    [System.IO.File]::WriteAllText(
        (Join-Path $resolvedTemporaryRoot 'Program.smile'),
        "Print `"Model3DAsset cooking test`"`n",
        [System.Text.UTF8Encoding]::new($false)
    )

    $projectPath = Join-Path $resolvedTemporaryRoot 'Cook.smileproj'
    $projectXml = @'
<SmileProject Version="1.0">
  <PropertyGroup>
    <ProjectKind>Console</ProjectKind>
    <StartupFile>Program.smile</StartupFile>
    <OutputName>Model3DCookTest</OutputName>
  </PropertyGroup>
  <ItemGroup>
    <SmileSource Include="Program.smile" StartupOnly="true" />
    <Model3DAsset Include="Source\Arin.glb"
                  Descriptor="Source\Arin.sm3d.json"
                  LogicalPath="Assets\Cooked\Arin.sm3d"
                  TextureOutputDirectory="Assets\Cooked\Textures"
                  Profile="Character"
                  Identity="sin-star-i.character-1.paladin"
                  ProductionState="Prototype" />
  </ItemGroup>
</SmileProject>
'@
    [System.IO.File]::WriteAllText($projectPath, $projectXml, [System.Text.UTF8Encoding]::new($false))

    $nativeRoot = Join-Path $resolvedTemporaryRoot 'native'
    $nativeOutput = Join-Path $nativeRoot 'Model3DCookTest.exe'
    $coldTimer = [System.Diagnostics.Stopwatch]::StartNew()
    $cold = Invoke-Compiler @(
        '--project', $projectPath,
        '--target', 'windows-x64',
        '--configuration', 'Release',
        '-o', $nativeOutput
    ) (Join-Path $resolvedTemporaryRoot 'cold-build.log')
    $coldTimer.Stop()
    if ($cold -notmatch '(?m)^COOK Model3DAsset ') {
        throw 'Cold build did not report COOK.'
    }

    $webRoot = Join-Path $resolvedTemporaryRoot 'web'
    $cacheHitTimer = [System.Diagnostics.Stopwatch]::StartNew()
    $hit = Invoke-Compiler @(
        '--project', $projectPath,
        '--target', 'web',
        '--configuration', 'Release',
        '--output-dir', $webRoot
    ) (Join-Path $resolvedTemporaryRoot 'cache-hit-build.log')
    $cacheHitTimer.Stop()
    if ($hit -notmatch '(?m)^CACHE-HIT Model3DAsset ') {
        throw 'Second target build did not report CACHE-HIT.'
    }

    $nativeHashes = Get-AssetHashes $nativeRoot
    $webHashes = Get-AssetHashes $webRoot
    if (($nativeHashes -join "`n") -cne ($webHashes -join "`n")) {
        throw 'Native and Web cooked asset paths/hashes differ.'
    }
    if ($nativeHashes.Count -ne 4 -or
        (Get-ChildItem -LiteralPath $nativeRoot -File -Recurse -Filter '*.glb').Count -ne 0 -or
        (Get-ChildItem -LiteralPath $webRoot -File -Recurse -Filter '*.glb').Count -ne 0) {
        throw 'Cook publication did not contain exactly one SM3D plus three textures, or leaked GLB source.'
    }

    $cacheManifest = Get-ChildItem -LiteralPath (Join-Path $resolvedTemporaryRoot 'obj\Smile\Model3DCache') `
        -File -Recurse -Filter 'cook-manifest.json' | Select-Object -First 1
    if ($null -eq $cacheManifest) {
        throw 'Cook cache manifest was not created.'
    }
    $cache = Get-Content -LiteralPath $cacheManifest.FullName -Raw | ConvertFrom-Json
    $cachedModel = Join-Path $cacheManifest.Directory.FullName `
        ($cache.outputs[0].relativePath.Replace('/', '\'))
    [System.IO.File]::WriteAllBytes($cachedModel, [byte[]](1, 2, 3, 4))
    $recoveryRoot = Join-Path $resolvedTemporaryRoot 'recovery-web'
    $recovered = Invoke-Compiler @(
        '--project', $projectPath,
        '--target', 'web',
        '--configuration', 'Release',
        '--output-dir', $recoveryRoot
    ) (Join-Path $resolvedTemporaryRoot 'cache-recovery-build.log')
    if ($recovered -notmatch '(?m)^CACHE-RECOVER Model3DAsset ') {
        throw 'Corrupt cache rebuild did not report CACHE-RECOVER.'
    }
    $recoveryHashes = Get-AssetHashes $recoveryRoot
    if (($recoveryHashes -join "`n") -cne ($webHashes -join "`n")) {
        throw "Cache recovery changed cooked asset paths or hashes.`nBefore:`n$($webHashes -join "`n")`nAfter:`n$($recoveryHashes -join "`n")"
    }

    $cacheRoot = [System.IO.Path]::GetFullPath((Join-Path $resolvedTemporaryRoot 'obj\Smile\Model3DCache'))
    $temporaryPrefix = $resolvedTemporaryRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar) +
        [System.IO.Path]::DirectorySeparatorChar
    if (-not $cacheRoot.StartsWith($temporaryPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'Concurrent-test cache root escaped the verified temporary project.'
    }
    Remove-Item -LiteralPath $cacheRoot -Recurse -Force
    $concurrentNative = Join-Path $resolvedTemporaryRoot 'concurrent-native\Model3DCookTest.exe'
    $concurrentWeb = Join-Path $resolvedTemporaryRoot 'concurrent-web'
    $nativeOut = Join-Path $resolvedTemporaryRoot 'concurrent-native.stdout.log'
    $nativeError = Join-Path $resolvedTemporaryRoot 'concurrent-native.stderr.log'
    $webOut = Join-Path $resolvedTemporaryRoot 'concurrent-web.stdout.log'
    $webError = Join-Path $resolvedTemporaryRoot 'concurrent-web.stderr.log'
    $nativeProcess = Start-Process -FilePath $compiler -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput $nativeOut -RedirectStandardError $nativeError -ArgumentList @(
            '--project', "`"$projectPath`"", '--target', 'windows-x64', '--configuration', 'Release',
            '-o', "`"$concurrentNative`"")
    $webProcess = Start-Process -FilePath $compiler -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput $webOut -RedirectStandardError $webError -ArgumentList @(
            '--project', "`"$projectPath`"", '--target', 'web', '--configuration', 'Release',
            '--output-dir', "`"$concurrentWeb`"")
    $nativeProcess.WaitForExit()
    $webProcess.WaitForExit()
    $nativeProcess.Refresh()
    $webProcess.Refresh()
    $nativeConcurrentLog = (Get-Content -LiteralPath $nativeOut -Raw) +
        (Get-Content -LiteralPath $nativeError -Raw)
    $webConcurrentLog = (Get-Content -LiteralPath $webOut -Raw) +
        (Get-Content -LiteralPath $webError -Raw)
    if (-not (Test-Path -LiteralPath $concurrentNative -PathType Leaf) -or
        -not (Test-Path -LiteralPath (Join-Path $concurrentWeb 'game.js') -PathType Leaf) -or
        ($nativeConcurrentLog + $webConcurrentLog) -match '(?m)\berror SML' -or
        ($nativeConcurrentLog + $webConcurrentLog) -notmatch 'COOK Model3DAsset' -or
        ($nativeConcurrentLog + $webConcurrentLog) -notmatch 'CACHE-HIT Model3DAsset') {
        throw "Concurrent native/Web cooking failed.`n$nativeConcurrentLog`n$webConcurrentLog"
    }
    $concurrentNativeHashes = Get-AssetHashes (Split-Path -Parent $concurrentNative)
    $concurrentWebHashes = Get-AssetHashes $concurrentWeb
    if (($concurrentNativeHashes -join "`n") -cne ($concurrentWebHashes -join "`n")) {
        throw "Concurrent native/Web builds published different cooked asset paths or hashes.`nNative:`n$($concurrentNativeHashes -join "`n")`nWeb:`n$($concurrentWebHashes -join "`n")"
    }

    $beforeFailure = Get-AssetHashes $webRoot
    $invalidProject = $projectXml.Replace('Profile="Character"', 'Profile="Static"')
    [System.IO.File]::WriteAllText($projectPath, $invalidProject, [System.Text.UTF8Encoding]::new($false))
    $failure = Invoke-Compiler @(
        '--project', $projectPath,
        '--target', 'web',
        '--configuration', 'Release',
        '--output-dir', $webRoot
    ) (Join-Path $resolvedTemporaryRoot 'failed-build.log') 1
    $afterFailure = Get-AssetHashes $webRoot
    if ($failure -notmatch 'SML3712' -or
        ($afterFailure -join "`n") -cne ($beforeFailure -join "`n")) {
        throw "Failed cook did not preserve the last successful output tree.`nDiagnostic:`n$failure`nBefore:`n$($beforeFailure -join "`n")`nAfter:`n$($afterFailure -join "`n")"
    }

    [System.IO.Directory]::CreateDirectory((Join-Path $resolvedTemporaryRoot 'Assets\Cooked')) | Out-Null
    [System.IO.File]::WriteAllText(
        (Join-Path $resolvedTemporaryRoot 'Assets\Cooked\Arin.sm3d'),
        'ordinary collision',
        [System.Text.UTF8Encoding]::new($false)
    )
    $collisionProject = $projectXml.Replace(
        '<SmileSource Include="Program.smile" StartupOnly="true" />',
        '<SmileSource Include="Program.smile" StartupOnly="true" />' + "`n" +
        '    <Asset Include="Assets\Cooked\Arin.sm3d" />'
    )
    [System.IO.File]::WriteAllText($projectPath, $collisionProject, [System.Text.UTF8Encoding]::new($false))
    $collision = Invoke-Compiler @(
        '--project', $projectPath,
        '--target', 'web',
        '--configuration', 'Release',
        '--output-dir', $webRoot
    ) (Join-Path $resolvedTemporaryRoot 'collision-build.log') 1
    if ($collision -notmatch 'SML3713') {
        throw "Generated/copy collision did not report SML3713.`n$collision"
    }

    $renamedProject = $projectXml.Replace(
        'LogicalPath="Assets\Cooked\Arin.sm3d"',
        'LogicalPath="Assets\Cooked\Renamed.sm3d"'
    )
    [System.IO.File]::WriteAllText($projectPath, $renamedProject, [System.Text.UTF8Encoding]::new($false))
    $renamed = Invoke-Compiler @(
        '--project', $projectPath,
        '--target', 'web',
        '--configuration', 'Release',
        '--output-dir', $webRoot
    ) (Join-Path $resolvedTemporaryRoot 'renamed-build.log')
    if ($renamed -notmatch '(?m)^COOK Model3DAsset ' -or
        (Test-Path -LiteralPath (Join-Path $webRoot 'Assets\Cooked\Arin.sm3d')) -or
        -not (Test-Path -LiteralPath (Join-Path $webRoot 'Assets\Cooked\Renamed.sm3d'))) {
        throw 'Logical-path invalidation did not cook and atomically remove the stale generated model.'
    }

    Write-Host 'Model3DAsset cooking tests passed.'
    Write-Host "Cold build: COOK in $($coldTimer.ElapsedMilliseconds) ms"
    Write-Host "Second target: CACHE-HIT in $($cacheHitTimer.ElapsedMilliseconds) ms"
    Write-Host 'Corrupt entry: CACHE-RECOVER'
    Write-Host 'Concurrent native/Web: COOK plus CACHE-HIT with identical outputs'
    Write-Host 'Collision: SML3713; failed cook preserved output; renamed cook removed stale outputs'
    Write-Host "Published parity assets: $($webHashes.Count)"
    Write-Output $webHashes
}
finally {
    if (Test-Path -LiteralPath $resolvedTemporaryRoot) {
        Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force
    }
}
