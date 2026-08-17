param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$compiler = Join-Path $root 'artifacts\compiler\smilec.exe'
$fixtureRoot = Join-Path $root 'examples\NonLocalResourceUnwind'
$nativeRoot = Join-Path $root 'artifacts\games\NonLocalResourceUnwind'
$webRoot = Join-Path $root 'artifacts\web\NonLocalResourceUnwind'
$temporaryRoot = Join-Path $root 'artifacts\temp\NonLocalResourceUnwind'
$cursor = Join-Path $root 'examples\MenuGallery\Assets\Cursor.png'

if (-not (Test-Path -LiteralPath $compiler)) {
    throw "SMILE compiler not found at '$compiler'. Run scripts\build.cmd first."
}

New-Item -ItemType Directory -Force -Path $nativeRoot, $webRoot, $temporaryRoot | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $nativeRoot 'Assets') | Out-Null
Copy-Item -LiteralPath $cursor -Destination (Join-Path $nativeRoot 'Assets\Cursor.png') -Force

function Invoke-Compiler {
    param([string[]]$Arguments)

    & $compiler @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "smilec failed with exit code ${LASTEXITCODE}: $($Arguments -join ' ')"
    }
}

function Read-NormalizedLines {
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
        [string]$Source,
        [string[]]$Expected,
        [int]$ExpectedExit,
        [string]$AllocationFailAfter = '',
        [switch]$ImageDiagnostics
    )

    $executable = Join-Path $nativeRoot "$Name.exe"
    $stdout = Join-Path $temporaryRoot "$Name.stdout.txt"
    $stderr = Join-Path $temporaryRoot "$Name.stderr.txt"
    Invoke-Compiler @($Source, '--target', 'windows-x64', '--configuration', $Configuration,
        '--graphics', 'GDI', '-o', $executable)

    $env:SMILE_CLASS_LIFETIME_DIAGNOSTICS = '1'
    $env:SMILE_TEXT_LIFETIME_DIAGNOSTICS = '1'
    if ($ImageDiagnostics) {
        $env:SMILE_IMAGE_LIFETIME_DIAGNOSTICS = '1'
    }
    if ($AllocationFailAfter.Length -ne 0) {
        $env:SMILE_CLASS_ALLOCATION_FAIL_AFTER = $AllocationFailAfter
    }
    try {
        $process = Start-Process -FilePath $executable -Wait -PassThru -NoNewWindow `
            -RedirectStandardOutput $stdout -RedirectStandardError $stderr
    }
    finally {
        Remove-Item Env:SMILE_CLASS_LIFETIME_DIAGNOSTICS -ErrorAction SilentlyContinue
        Remove-Item Env:SMILE_TEXT_LIFETIME_DIAGNOSTICS -ErrorAction SilentlyContinue
        Remove-Item Env:SMILE_IMAGE_LIFETIME_DIAGNOSTICS -ErrorAction SilentlyContinue
        Remove-Item Env:SMILE_CLASS_ALLOCATION_FAIL_AFTER -ErrorAction SilentlyContinue
    }

    if ($process.ExitCode -ne $ExpectedExit) {
        throw "$Name exited with $($process.ExitCode); expected $ExpectedExit."
    }
    $errorLines = Read-NormalizedLines $stderr
    if ($errorLines.Count -ne 0) {
        throw "$Name wrote unexpected stderr: $($errorLines -join ' | ')"
    }
    $diagnostics = @('SMILE_CLASS_LIVE=0')
    if ($ImageDiagnostics) {
        $diagnostics += 'SMILE_IMAGE_LIVE=0'
    }
    $diagnostics += 'SMILE_TEXT_LIVE=0'
    Assert-Lines $Name ($Expected + $diagnostics) (Read-NormalizedLines $stdout)
}

function Invoke-Web {
    param(
        [string]$Name,
        [string]$Source,
        [string]$ExpectedPath = '',
        [string]$ExpectedRuntimeError = '',
        [switch]$CopyCursor
    )

    $directory = Join-Path $webRoot $Name
    Invoke-Compiler @($Source, '--target', 'web', '--configuration', $Configuration,
        '--output-dir', $directory)
    if ($CopyCursor) {
        New-Item -ItemType Directory -Force -Path (Join-Path $directory 'Assets') | Out-Null
        Copy-Item -LiteralPath $cursor -Destination (Join-Path $directory 'Assets\Cursor.png') -Force
    }
    & node --check (Join-Path $directory 'game.js')
    if ($LASTEXITCODE -ne 0) {
        throw "$Name generated invalid JavaScript."
    }
    $arguments = @((Join-Path $root 'scripts\run-web-test.js'), $directory, '--timeout', '10000')
    if ($ExpectedPath.Length -ne 0) {
        $arguments += @('--expected', $ExpectedPath)
    }
    if ($ExpectedRuntimeError.Length -ne 0) {
        $arguments += @('--expected-runtime-error', $ExpectedRuntimeError)
    }
    & node @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Name Web execution failed."
    }
}

$nestedEndSource = Join-Path $fixtureRoot 'NestedEndProgram.smile'
$nestedNothingSource = Join-Path $fixtureRoot 'NestedNothing.smile'
$allocationSource = Join-Path $fixtureRoot 'ClassAllocationFailure.smile'
$nestedEndExpected = Read-NormalizedLines (Join-Path $fixtureRoot 'NestedEndProgram.expected.txt')
$nestedNothingExpected = Read-NormalizedLines (Join-Path $fixtureRoot 'NestedNothing.expected.txt')
$allocationExpected = Read-NormalizedLines (Join-Path $fixtureRoot 'ClassAllocationFailure.expected.txt')

Invoke-Native 'NestedEndProgram' $nestedEndSource $nestedEndExpected 0 -ImageDiagnostics
Invoke-Native 'NestedNothing' $nestedNothingSource $nestedNothingExpected 2 -ImageDiagnostics
Invoke-Native 'ClassAllocationFailure' $allocationSource $allocationExpected 3 '2' -ImageDiagnostics
Invoke-Native 'ClassAllocationDefault' $allocationSource @(
    'constructed first',
    'constructed second',
    'constructed third',
    'unreachable',
    'unreachable'
) 0 -ImageDiagnostics

Invoke-Web 'NestedEndProgram' $nestedEndSource `
    (Join-Path $fixtureRoot 'NestedEndProgram.expected.txt') -CopyCursor
Invoke-Web 'NestedNothing' $nestedNothingSource -ExpectedRuntimeError 'Object reference is Nothing.' -CopyCursor

$optionalSource = Join-Path $root 'examples\OptionalNamedEndProgramCleanup.smile'
$optionalExpectedPath = Join-Path $root 'examples\OptionalNamedEndProgramCleanup.expected.txt'
$optionalExpected = Read-NormalizedLines $optionalExpectedPath
Invoke-Native 'OptionalNamedEndProgramCleanup' $optionalSource $optionalExpected 0
Invoke-Web 'OptionalNamedEndProgramCleanup' $optionalSource $optionalExpectedPath

$propertySource = Join-Path $root 'examples\TypeMemberRuntime\TypeMemberEndProgramCleanup.smile'
$propertyExpectedPath = Join-Path $root 'examples\TypeMemberRuntime\TypeMemberEndProgramCleanup.expected.txt'
$propertyExpected = Read-NormalizedLines $propertyExpectedPath
Invoke-Native 'TypeMemberEndProgramCleanup' $propertySource $propertyExpected 0
Invoke-Web 'TypeMemberEndProgramCleanup' $propertySource $propertyExpectedPath

Write-Output 'Lightweight OOP non-local unwind, finalizer, and allocation-failure tests passed.'
