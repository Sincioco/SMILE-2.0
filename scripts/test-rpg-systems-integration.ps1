[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$RepositoryRoot = Split-Path -Parent $PSScriptRoot
$Compiler = Join-Path $RepositoryRoot 'artifacts\compiler\smilec.exe'
$RpgSystemsRoot = Join-Path $RepositoryRoot 'games\RPGSystems'
$ArtifactsRoot = Join-Path $RepositoryRoot 'artifacts'
$TestsRoot = Join-Path $ArtifactsRoot 'tests'
$GamesRoot = Join-Path $ArtifactsRoot 'games'
$WebRoot = Join-Path $ArtifactsRoot 'web'
$TempRoot = Join-Path $ArtifactsRoot 'temp'

function Invoke-Checked {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & $FilePath @Arguments

    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $FilePath $($Arguments -join ' ')"
    }
}

function Invoke-NativeCapture {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Executable,

        [Parameter(Mandatory = $true)]
        [string]$OutputPath
    )

    $ErrorPath = "$OutputPath.err"

    Remove-Item -LiteralPath $OutputPath, $ErrorPath -Force -ErrorAction SilentlyContinue

    $Process = Start-Process -FilePath $Executable `
        -WorkingDirectory (Split-Path -Parent $Executable) `
        -NoNewWindow `
        -Wait `
        -PassThru `
        -RedirectStandardOutput $OutputPath `
        -RedirectStandardError $ErrorPath

    if ($Process.ExitCode -ne 0) {
        $ErrorText = Get-Content -LiteralPath $ErrorPath -Raw -ErrorAction SilentlyContinue
        throw "Native test failed with exit code $($Process.ExitCode): $Executable`n$ErrorText"
    }

    $ErrorText = Get-Content -LiteralPath $ErrorPath -Raw -ErrorAction SilentlyContinue

    if (-not [string]::IsNullOrWhiteSpace($ErrorText)) {
        throw "Native test wrote unexpected standard error: $Executable`n$ErrorText"
    }
}

function Assert-ExactLines {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ExpectedPath,

        [Parameter(Mandatory = $true)]
        [string]$ActualPath
    )

    $Expected = [IO.File]::ReadAllLines($ExpectedPath, [Text.Encoding]::UTF8)
    $Actual = [IO.File]::ReadAllLines($ActualPath, [Text.Encoding]::UTF8)

    if ($Expected.Count -ne $Actual.Count) {
        throw "Output line count differs. Expected $($Expected.Count), found $($Actual.Count): $ActualPath"
    }

    for ($Index = 0; $Index -lt $Expected.Count; $Index++) {
        if ($Expected[$Index] -cne $Actual[$Index]) {
            throw "Output differs at line $($Index + 1). Expected '$($Expected[$Index])', found '$($Actual[$Index])'."
        }
    }
}

function Test-OrdinalContains {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Text,

        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    return $Text.IndexOf($Value, [StringComparison]::Ordinal) -ge 0
}

function Assert-SourceContract {
    $Project = Get-Content -LiteralPath (Join-Path $RpgSystemsRoot 'RPGSystems.smileproj') -Raw
    $Storage = Get-Content -LiteralPath (Join-Path $RpgSystemsRoot 'Storage.smile') -Raw

    if (-not (Test-OrdinalContains $Project '<ApplicationId>smile.gallery.rpg-systems</ApplicationId>') -or
        -not (Test-OrdinalContains $Project '<SmileSource Include="Storage.smile" />')) {
        throw 'RPGSystems project identity or Storage source contract is missing.'
    }

    foreach ($Required in @(
            'Public Enum SaveDomain',
            'Management = 1',
            'Dungeon = 2',
            'World = 3',
            'Case SaveDomain.Management',
            'Case SaveDomain.Dungeon',
            'Case SaveDomain.World')) {
        if (-not (Test-OrdinalContains $Storage $Required)) {
            throw "RPGSystems Storage contract is missing: $Required"
        }
    }

    foreach ($SystemName in @('BattleSystem', 'DungeonSystem', 'ManagementSystem', 'WorldSystem')) {
        $SourcePath = Join-Path $RpgSystemsRoot "$SystemName.smile"
        $Source = Get-Content -LiteralPath $SourcePath -Raw

        foreach ($Required in @(
                'Dim InitializationSucceeded As Boolean',
                'Call ResetRunState()',
                'If Not InitializationSucceeded Then',
                'Call Shutdown()')) {
            if (-not (Test-OrdinalContains $Source $Required)) {
                throw "$SystemName lifecycle contract is missing: $Required"
            }
        }

        if (Test-OrdinalContains $Source 'End Program') {
            throw "$SystemName must return to the launcher instead of using End Program."
        }
    }

    foreach ($SystemName in @('ManagementSystem', 'WorldSystem')) {
        $Source = Get-Content -LiteralPath (Join-Path $RpgSystemsRoot "$SystemName.smile") -Raw

        if ((Test-OrdinalContains $Source 'SaveGames.SaveGame') -or
            (Test-OrdinalContains $Source 'SaveGames.LoadGame') -or
            (Test-OrdinalContains $Source 'SaveGames.Exists')) {
            throw "$SystemName bypasses the application-local persistence domain mapping."
        }
    }

    $DungeonSource = Get-Content -LiteralPath (Join-Path $RpgSystemsRoot 'DungeonSystem.smile') -Raw

    if (-not (Test-OrdinalContains $DungeonSource 'Storage.PhysicalSlot(Storage.SaveDomain.Dungeon)')) {
        throw 'DungeonSystem does not use its centralized SaveDomain mapping.'
    }

    foreach ($SystemName in @('BattleSystem', 'DungeonSystem', 'WorldSystem')) {
        $Source = Get-Content -LiteralPath (Join-Path $RpgSystemsRoot "$SystemName.smile") -Raw

        if (-not (Test-OrdinalContains $Source 'Stop Sound') -or
            -not (Test-OrdinalContains $Source 'Stop Music')) {
            throw "$SystemName does not stop both system-owned audio channels during shutdown."
        }
    }
}

if (-not (Test-Path -LiteralPath $Compiler -PathType Leaf)) {
    throw "SMILE compiler not found. Run scripts\build.cmd first: $Compiler"
}

New-Item -ItemType Directory -Force -Path $TestsRoot, $GamesRoot, $WebRoot, $TempRoot | Out-Null

Assert-SourceContract

$PersistenceProject = Join-Path $RpgSystemsRoot 'RPGSystemsPersistenceTests.smileproj'
$PersistenceExpected = Join-Path $RpgSystemsRoot 'Tests\PersistenceIsolation.expected.txt'
$PersistenceExe = Join-Path $TestsRoot 'RPGSystemsPersistenceTests.exe'
$PersistenceOutput = Join-Path $TempRoot 'RPGSystemsPersistenceTests.out'
$PersistenceWeb = Join-Path $WebRoot 'RPGSystemsPersistenceTests'

Invoke-Checked $Compiler @('--project', $PersistenceProject, '--target', 'windows-x64',
    '--configuration', 'Release', '-o', $PersistenceExe)
Invoke-NativeCapture $PersistenceExe $PersistenceOutput
Assert-ExactLines $PersistenceExpected $PersistenceOutput
Invoke-Checked $Compiler @('--project', $PersistenceProject, '--target', 'web',
    '--configuration', 'Release', '--output-dir', $PersistenceWeb)
Invoke-Checked 'node' @('--check', (Join-Path $PersistenceWeb 'game.js'))
Invoke-Checked 'node' @((Join-Path $PSScriptRoot 'run-web-test.js'), $PersistenceWeb,
    '--native-output', $PersistenceExpected, '--timeout', '10000')

$InitializationProject = Join-Path $RpgSystemsRoot 'RPGSystemsInitializationTests.smileproj'
$InitializationExpected = Join-Path $RpgSystemsRoot 'Tests\InitializationFailures.expected.txt'
$InitializationExe = Join-Path $TestsRoot 'RPGSystemsInitializationTests.exe'
$InitializationOutput = Join-Path $TempRoot 'RPGSystemsInitializationTests.out'
$InitializationWeb = Join-Path $WebRoot 'RPGSystemsInitializationTests'
$PreviousClassDiagnostics = [Environment]::GetEnvironmentVariable('SMILE_CLASS_LIFETIME_DIAGNOSTICS', 'Process')
$PreviousTextDiagnostics = [Environment]::GetEnvironmentVariable('SMILE_TEXT_LIFETIME_DIAGNOSTICS', 'Process')
$PreviousImageDiagnostics = [Environment]::GetEnvironmentVariable('SMILE_IMAGE_LIFETIME_DIAGNOSTICS', 'Process')

try {
    $env:SMILE_CLASS_LIFETIME_DIAGNOSTICS = '1'
    $env:SMILE_TEXT_LIFETIME_DIAGNOSTICS = '1'
    $env:SMILE_IMAGE_LIFETIME_DIAGNOSTICS = '1'

    Invoke-Checked $Compiler @('--project', $InitializationProject, '--target', 'windows-x64',
        '--configuration', 'Release', '--graphics', 'GDI', '-o', $InitializationExe, '--debug')
    Invoke-NativeCapture $InitializationExe $InitializationOutput
}
finally {
    [Environment]::SetEnvironmentVariable('SMILE_CLASS_LIFETIME_DIAGNOSTICS', $PreviousClassDiagnostics, 'Process')
    [Environment]::SetEnvironmentVariable('SMILE_TEXT_LIFETIME_DIAGNOSTICS', $PreviousTextDiagnostics, 'Process')
    [Environment]::SetEnvironmentVariable('SMILE_IMAGE_LIFETIME_DIAGNOSTICS', $PreviousImageDiagnostics, 'Process')
}

$ExpectedInitialization = [IO.File]::ReadAllLines($InitializationExpected, [Text.Encoding]::UTF8)
$ActualInitialization = [IO.File]::ReadAllLines($InitializationOutput, [Text.Encoding]::UTF8)
$ExpectedDiagnostics = @('SMILE_CLASS_LIVE=0', 'SMILE_IMAGE_LIVE=0', 'SMILE_TEXT_LIVE=0')

if ($ActualInitialization.Count -ne $ExpectedInitialization.Count + $ExpectedDiagnostics.Count) {
    throw 'Initialization fixture did not emit the expected result and lifetime line counts.'
}

for ($Index = 0; $Index -lt $ExpectedInitialization.Count; $Index++) {
    if ($ExpectedInitialization[$Index] -cne $ActualInitialization[$Index]) {
        throw "Initialization fixture differs at line $($Index + 1)."
    }
}

foreach ($ExpectedDiagnostic in $ExpectedDiagnostics) {
    if (@($ActualInitialization | Where-Object { $_ -ceq $ExpectedDiagnostic }).Count -ne 1) {
        throw "Initialization fixture is missing exact lifetime output: $ExpectedDiagnostic"
    }
}

Invoke-Checked $Compiler @('--project', $InitializationProject, '--target', 'web',
    '--configuration', 'Release', '--output-dir', $InitializationWeb)
Invoke-Checked 'node' @('--check', (Join-Path $InitializationWeb 'game.js'))
Invoke-Checked 'node' @((Join-Path $PSScriptRoot 'run-web-test.js'), $InitializationWeb,
    '--native-output', $InitializationExpected, '--timeout', '20000')

$RpgSystemsProject = Join-Path $RpgSystemsRoot 'RPGSystems.smileproj'
$DirectXDirectory = Join-Path $GamesRoot 'RPGSystems-DirectX'
$GdiDirectory = Join-Path $GamesRoot 'RPGSystems-GDI'
$RpgSystemsWeb = Join-Path $WebRoot 'RPGSystems'

New-Item -ItemType Directory -Force -Path $DirectXDirectory, $GdiDirectory | Out-Null

Invoke-Checked $Compiler @('--project', $RpgSystemsProject, '--target', 'windows-x64',
    '--configuration', 'Release', '--graphics', 'DirectX', '-o',
    (Join-Path $DirectXDirectory 'RPGSystems.exe'), '--debug')
Invoke-Checked $Compiler @('--project', $RpgSystemsProject, '--target', 'windows-x64',
    '--configuration', 'Release', '--graphics', 'GDI', '-o',
    (Join-Path $GdiDirectory 'RPGSystems.exe'))
Invoke-Checked $Compiler @('--project', $RpgSystemsProject, '--target', 'web',
    '--configuration', 'Release', '--output-dir', $RpgSystemsWeb)
Invoke-Checked 'node' @('--check', (Join-Path $RpgSystemsWeb 'game.js'))
Invoke-Checked 'node' @((Join-Path $PSScriptRoot 'run-web-test.js'), $RpgSystemsWeb,
    '--frames', '40', '--timeout', '10000')

Write-Host 'RPGSystems persistence, initialization, lifetime, native, GDI, and Web integration tests passed.'
