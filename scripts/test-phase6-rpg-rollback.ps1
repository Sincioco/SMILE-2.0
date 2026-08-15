param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$RepositoryRoot = Split-Path -Parent $PSScriptRoot
$Compiler = Join-Path $RepositoryRoot 'artifacts\compiler\smilec.exe'
$SourceRoot = Join-Path $RepositoryRoot 'libraries\Smile.RPG'
$ProgramSource = Join-Path $RepositoryRoot 'examples\Phase6RpgRollbackFaultTests\Program.smile'
$TestRoot = Join-Path $RepositoryRoot ('artifacts\temp\phase6.1-rpg-rollback-' + [Guid]::NewGuid().ToString('N'))
$LibraryRoot = Join-Path $TestRoot 'Library'
$ApplicationRoot = Join-Path $TestRoot 'Application'
$Utf8WithoutBom = [Text.UTF8Encoding]::new($false)

if (-not (Test-Path -LiteralPath $Compiler)) {
    throw 'The SMILE compiler must be built before the Phase 6.1 rollback test runs.'
}

try {
    [IO.Directory]::CreateDirectory($LibraryRoot) | Out-Null
    [IO.Directory]::CreateDirectory($ApplicationRoot) | Out-Null

    foreach ($Name in @('Core.smile', 'Characters.smile', 'Party.smile', 'Inventory.smile',
            'Equipment.smile', 'Abilities.smile', 'Shops.smile', 'SaveGames.smile')) {
        Copy-Item -LiteralPath (Join-Path $SourceRoot $Name) -Destination (Join-Path $LibraryRoot $Name)
    }

    $SaveGamesPath = Join-Path $LibraryRoot 'SaveGames.smile'
    $SaveGames = [IO.File]::ReadAllText($SaveGamesPath)
    $Needle = "    For Index = 0 To ScratchInventoryCount - 1"
    $Injection = "    If ScratchGold = 777777777 Then`n        Return False`n    End If`n`n$Needle"
    $InjectionIndex = $SaveGames.LastIndexOf($Needle, [StringComparison]::Ordinal)
    $ApplyScratchIndex = $SaveGames.IndexOf('Private Function ApplyScratch', [StringComparison]::Ordinal)

    if ($InjectionIndex -lt 0 -or $InjectionIndex -lt $ApplyScratchIndex) {
        throw 'The private ApplyScratch fault-injection point was not found exactly once.'
    }

    $InstrumentedSaveGames = $SaveGames.Remove($InjectionIndex, $Needle.Length).Insert($InjectionIndex, $Injection)
    [IO.File]::WriteAllText($SaveGamesPath, $InstrumentedSaveGames, $Utf8WithoutBom)

    $LibraryProject = @'
<SmileProject Version="1.0">
  <PropertyGroup>
    <ProjectKind>Library</ProjectKind>
    <LibraryName>Smile.RPG</LibraryName>
    <Version>1.0.1</Version>
    <OutputName>Smile.RPG</OutputName>
  </PropertyGroup>
  <ItemGroup>
    <SmileSource Include="Core.smile" />
    <SmileSource Include="Characters.smile" />
    <SmileSource Include="Party.smile" />
    <SmileSource Include="Inventory.smile" />
    <SmileSource Include="Equipment.smile" />
    <SmileSource Include="Abilities.smile" />
    <SmileSource Include="Shops.smile" />
    <SmileSource Include="SaveGames.smile" />
  </ItemGroup>
</SmileProject>
'@
    [IO.File]::WriteAllText((Join-Path $LibraryRoot 'Smile.RPG.smilelibproj'), $LibraryProject, $Utf8WithoutBom)
    Copy-Item -LiteralPath $ProgramSource -Destination (Join-Path $ApplicationRoot 'Program.smile')

    $ApplicationProject = @'
<SmileProject Version="1.0">
  <PropertyGroup>
    <ProjectKind>Console</ProjectKind>
    <StartupFile>Program.smile</StartupFile>
    <OutputName>Phase6RpgRollbackFaultTests</OutputName>
    <ApplicationId>smile.tests.phase6-rpg-rollback-fault</ApplicationId>
  </PropertyGroup>
  <ItemGroup>
    <SmileSource Include="Program.smile" StartupOnly="true" />
    <SmileProjectReference Include="..\Library\Smile.RPG.smilelibproj" />
  </ItemGroup>
</SmileProject>
'@
    $ApplicationProjectPath = Join-Path $ApplicationRoot 'Phase6RpgRollbackFaultTests.smileproj'
    [IO.File]::WriteAllText($ApplicationProjectPath, $ApplicationProject, $Utf8WithoutBom)

    $NativeOutput = Join-Path $TestRoot 'Phase6RpgRollbackFaultTests.exe'
    $NativeText = Join-Path $TestRoot 'native.out'
    $WebOutput = Join-Path $TestRoot 'web'

    & $Compiler --project $ApplicationProjectPath --target windows-x64 --configuration Release -o $NativeOutput
    if ($LASTEXITCODE -ne 0) { throw 'The native rollback fault-injection fixture did not compile.' }

    $NativeLines = @(& $NativeOutput)
    if ($LASTEXITCODE -ne 0) { throw 'The native rollback fault-injection fixture failed.' }
    [IO.File]::WriteAllLines($NativeText, $NativeLines, $Utf8WithoutBom)
    if ($NativeLines.Count -ne 1 -or $NativeLines[0] -cne 'Phase 6.1 RPG rollback fault test: PASS') {
        throw "The native rollback fault-injection result was unexpected: $($NativeLines -join ' | ')"
    }

    & $Compiler --project $ApplicationProjectPath --target web --configuration Release --output-dir $WebOutput
    if ($LASTEXITCODE -ne 0) { throw 'The Web rollback fault-injection fixture did not compile.' }
    & node (Join-Path $RepositoryRoot 'scripts\run-web-test.js') $WebOutput --native-output $NativeText --timeout 10000
    if ($LASTEXITCODE -ne 0) { throw 'The Web rollback fault-injection fixture did not match native output.' }

    Write-Host 'Phase 6.1 private ApplyScratch fault injection and exact native/Web rollback passed.'
}
finally {
    $ArtifactsPrefix = ([IO.Path]::GetFullPath((Join-Path $RepositoryRoot 'artifacts'))).TrimEnd('\', '/') +
        [IO.Path]::DirectorySeparatorChar
    $ResolvedTestRoot = [IO.Path]::GetFullPath($TestRoot)

    if ($ResolvedTestRoot.StartsWith($ArtifactsPrefix, [StringComparison]::OrdinalIgnoreCase) -and
        [IO.Directory]::Exists($ResolvedTestRoot)) {
        Remove-Item -LiteralPath $ResolvedTestRoot -Recurse -Force
    }
}
