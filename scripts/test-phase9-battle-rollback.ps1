[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$artifactTempRoot = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot 'artifacts\temp'))
$testRoot = [System.IO.Path]::GetFullPath((Join-Path $artifactTempRoot 'Phase9BattleRollback'))
$compiler = Join-Path $repositoryRoot 'artifacts\compiler\smilec.exe'
$webRunner = Join-Path $repositoryRoot 'scripts\run-web-test.js'
$fixture = Join-Path $repositoryRoot 'scripts\fixtures\Phase9BattleRollbackProgram.smile'
$sourceLibrary = Join-Path $repositoryRoot 'libraries\Smile.RPG'
$checkpointText = 'ReturnValue = Checkpoint >= CHECKPOINT_MAGIC_POINT_PAYMENT And Checkpoint <= CHECKPOINT_GOLD_REWARD'

if (-not $testRoot.StartsWith($artifactTempRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'Phase 9 rollback test root escaped artifacts\temp.'
}

if (Test-Path -LiteralPath $testRoot) {
    Remove-Item -LiteralPath $testRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $testRoot -Force | Out-Null

for ($faultPoint = 1; $faultPoint -le 6; $faultPoint++) {
    $caseRoot = Join-Path $testRoot "Fault$faultPoint"
    $libraryRoot = Join-Path $caseRoot 'Smile.RPG'
    $programPath = Join-Path $caseRoot 'Program.smile'
    $projectPath = Join-Path $caseRoot 'Phase9BattleRollback.smileproj'
    $nativePath = Join-Path $caseRoot 'Phase9BattleRollback.exe'
    $nativeOutputPath = Join-Path $caseRoot 'native.out'
    $expectedPath = Join-Path $caseRoot 'expected.txt'
    $webOutputPath = Join-Path $caseRoot 'web'

    New-Item -ItemType Directory -Path $caseRoot -Force | Out-Null
    Copy-Item -LiteralPath $sourceLibrary -Destination $libraryRoot -Recurse

    $battleCorePath = Join-Path $libraryRoot 'BattleCore.smile'
    $battleCoreText = [System.IO.File]::ReadAllText($battleCorePath)
    $replacementText = "ReturnValue = Checkpoint <> $faultPoint"

    if (-not $battleCoreText.Contains($checkpointText)) {
        throw "The private Phase 9 checkpoint seam changed before fault $faultPoint."
    }

    [System.IO.File]::WriteAllText($battleCorePath, $battleCoreText.Replace($checkpointText, $replacementText))

    $programText = [System.IO.File]::ReadAllText($fixture).Replace('Const FAULT_POINT = 0', "Const FAULT_POINT = $faultPoint")
    [System.IO.File]::WriteAllText($programPath, $programText)

    $projectText = @"
<SmileProject Version="1.0">
  <PropertyGroup>
    <ProjectKind>Console</ProjectKind>
    <StartupFile>Program.smile</StartupFile>
    <OutputName>Phase9BattleRollback</OutputName>
    <ApplicationId>smile.tests.phase9-battle-rollback-$faultPoint</ApplicationId>
  </PropertyGroup>
  <ItemGroup>
    <SmileSource Include="Program.smile" StartupOnly="true" />
    <SmileProjectReference Include="Smile.RPG\Smile.RPG.smilelibproj" />
  </ItemGroup>
</SmileProject>
"@
    [System.IO.File]::WriteAllText($projectPath, $projectText)

    & $compiler --project $projectPath --target windows-x64 --configuration Release -o $nativePath

    if ($LASTEXITCODE -ne 0) {
        throw "Native compilation failed for Phase 9 fault $faultPoint."
    }

    $nativeLines = @(& $nativePath)

    if ($LASTEXITCODE -ne 0) {
        throw "Native execution failed for Phase 9 fault $faultPoint."
    }

    [System.IO.File]::WriteAllLines(
        $nativeOutputPath,
        $nativeLines,
        (New-Object System.Text.UTF8Encoding($false)))

    if ($nativeLines.Count -ne 3 -or $nativeLines[0] -ne 'Phase 9 battle rollback fault: PASS' -or $nativeLines[1] -ne "$faultPoint") {
        throw "Native rollback assertions failed for Phase 9 fault $faultPoint.`n$($nativeLines -join [Environment]::NewLine)"
    }

    [System.IO.File]::WriteAllLines($expectedPath, $nativeLines)

    & $compiler --project $projectPath --target web --configuration Release --output-dir $webOutputPath

    if ($LASTEXITCODE -ne 0) {
        throw "Web compilation failed for Phase 9 fault $faultPoint."
    }

    & node --check (Join-Path $webOutputPath 'game.js')

    if ($LASTEXITCODE -ne 0) {
        throw "Web JavaScript validation failed for Phase 9 fault $faultPoint."
    }

    & node $webRunner $webOutputPath --expected $expectedPath --timeout 10000

    if ($LASTEXITCODE -ne 0) {
        throw "Web rollback assertions failed for Phase 9 fault $faultPoint."
    }
}

Write-Host 'Phase 9 battle rollback fault injection passed: 6 native/Web checkpoints.'
