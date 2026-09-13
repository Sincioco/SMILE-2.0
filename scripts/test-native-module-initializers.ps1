param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$compiler = Join-Path $root 'artifacts\compiler\smilec.exe'
$fixtureRoot = Join-Path $root 'examples\NativeModuleInitializers'
$nativeRoot = Join-Path $root 'artifacts\games\NativeModuleInitializers'
$temporaryRoot = Join-Path $root 'artifacts\temp\NativeModuleInitializers'
$libraryPath = Join-Path $root 'artifacts\libraries\Smile.Tests.NativeModuleInitializers.smilelib'

if (-not (Test-Path -LiteralPath $compiler)) {
    throw "SMILE compiler not found at '$compiler'. Run scripts\build.cmd first."
}

New-Item -ItemType Directory -Force -Path $nativeRoot, $temporaryRoot,
    (Split-Path -Parent $libraryPath) | Out-Null

function Invoke-Compiler {
    param([string[]]$Arguments)

    & $compiler @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "smilec failed with exit code ${LASTEXITCODE}: $($Arguments -join ' ')"
    }
}

function Read-Lines {
    param([string]$Path)

    return [IO.File]::ReadAllLines($Path, [Text.Encoding]::UTF8)
}

function Assert-Lines {
    param(
        [string]$Name,
        [string[]]$Expected,
        [string[]]$Actual
    )

    $difference = Compare-Object -ReferenceObject $Expected -DifferenceObject $Actual -SyncWindow 0
    if ($difference) {
        throw "$Name output differed.`nEXPECTED:`n$($Expected -join "`n")`nACTUAL:`n$($Actual -join "`n")"
    }
}

function Invoke-Native {
    param(
        [string]$Name,
        [string]$Project,
        [string[]]$Expected,
        [int]$ExpectedExit
    )

    $executable = Join-Path $nativeRoot "$Name.exe"
    $stdout = Join-Path $temporaryRoot "$Name.stdout.txt"
    $stderr = Join-Path $temporaryRoot "$Name.stderr.txt"

    Invoke-Compiler @('--project', $Project, '--target', 'windows-x64',
        '--configuration', $Configuration, '-o', $executable)

    $env:SMILE_CLASS_LIFETIME_DIAGNOSTICS = '1'
    $env:SMILE_TEXT_LIFETIME_DIAGNOSTICS = '1'
    try {
        $process = Start-Process -FilePath $executable -Wait -PassThru -NoNewWindow `
            -RedirectStandardOutput $stdout -RedirectStandardError $stderr
    }
    finally {
        Remove-Item Env:SMILE_CLASS_LIFETIME_DIAGNOSTICS -ErrorAction SilentlyContinue
        Remove-Item Env:SMILE_TEXT_LIFETIME_DIAGNOSTICS -ErrorAction SilentlyContinue
    }

    if ($process.ExitCode -ne $ExpectedExit) {
        throw "$Name exited with $($process.ExitCode); expected $ExpectedExit."
    }
    $errorLines = Read-Lines $stderr
    if ($errorLines.Count -ne 0) {
        throw "$Name wrote unexpected stderr: $($errorLines -join ' | ')"
    }

    Assert-Lines $Name ($Expected + @('SMILE_CLASS_LIVE=0', 'SMILE_TEXT_LIVE=0')) (Read-Lines $stdout)
}

$expected = Read-Lines (Join-Path $fixtureRoot 'Program.expected.txt')
$failureExpected = Read-Lines (Join-Path $fixtureRoot 'Failure.expected.txt')

Invoke-Native 'LocalModules' (Join-Path $fixtureRoot 'NativeModuleInitializers.smileproj') $expected 0

Invoke-Compiler @('--project', (Join-Path $fixtureRoot 'NativeModuleInitializerLibrary.smilelibproj'),
    '--target', 'library', '--configuration', $Configuration, '-o', $libraryPath)
Invoke-Native 'PackageModules' (Join-Path $fixtureRoot 'NativeModuleInitializers.Package.smileproj') $expected 0
Invoke-Native 'InitializerFailure' (Join-Path $fixtureRoot 'NativeModuleInitializerFailure.smileproj') `
    $failureExpected 2

Write-Output 'Native module initialization order, package, once-only, and cleanup tests passed.'
