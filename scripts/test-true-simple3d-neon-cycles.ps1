param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$compiler = Join-Path $repositoryRoot "artifacts\compiler\smilec.exe"

function Invoke-Checked {
    param(
        [Parameter(Mandatory = $true)]
        [scriptblock]$Command,
        [Parameter(Mandatory = $true)]
        [string]$Description
    )

    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "$Description failed with exit code $LASTEXITCODE."
    }
}

function Test-NativeLaunch {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Description
    )

    $process = Start-Process -FilePath $Path -PassThru
    try {
        if ($process.WaitForExit(1200)) {
            throw "$Description exited unexpectedly with code $($process.ExitCode)."
        }
    }
    finally {
        if (-not $process.HasExited) {
            Stop-Process -Id $process.Id
            $process.WaitForExit()
        }
    }
}

Push-Location $repositoryRoot
try {
    Invoke-Checked {
        & $compiler --project "libraries\Smile.Simple3D\Smile.Simple3D.smilelibproj" --target library --configuration $Configuration
    } "Simple3D package build"

    Invoke-Checked {
        & $compiler --project "examples\Simple3DTests\Simple3DTests.smileproj" --target windows-x64 --configuration $Configuration --graphics GDI -o "artifacts\tests\Simple3DTests.exe"
    } "Simple3D native math tests"

    Invoke-Checked {
        & "scripts\run-bounded-test.cmd" 30 "artifacts\tests\Simple3DTests.exe"
    } "Simple3D native math execution"

    Invoke-Checked {
        & $compiler --project "examples\Simple3DTests\Simple3DTests.smileproj" --target web --configuration $Configuration --output-dir "artifacts\web\Simple3DTests"
    } "Simple3D Web math tests"

    Invoke-Checked {
        node "scripts\run-web-test.js" "artifacts\web\Simple3DTests" --expected "examples\Simple3DTests\expected.txt" --frames 3 --timeout 10000
    } "Simple3D Web math execution"

    Invoke-Checked {
        & $compiler --project "games\NeonCycles\NeonCyclesTests.smileproj" --target windows-x64 --configuration $Configuration --graphics GDI -o "artifacts\tests\NeonCyclesTests.exe"
    } "Neon Cycles native simulation tests"

    Invoke-Checked {
        & "scripts\run-bounded-test.cmd" 30 "artifacts\tests\NeonCyclesTests.exe"
    } "Neon Cycles native simulation execution"

    Invoke-Checked {
        & $compiler --project "games\NeonCycles\NeonCyclesTests.smileproj" --target web --configuration $Configuration --output-dir "artifacts\web\NeonCyclesTests"
    } "Neon Cycles Web simulation tests"

    Invoke-Checked {
        node "scripts\run-web-test.js" "artifacts\web\NeonCyclesTests" --expected "games\NeonCycles\NeonCyclesTests.expected.txt" --frames 3 --timeout 10000
    } "Neon Cycles Web simulation execution"

    Invoke-Checked {
        & $compiler --project "examples\Simple3DConformance\Simple3DConformance.smileproj" --target windows-x64 --configuration $Configuration --graphics DirectX -o "artifacts\examples\Simple3DConformance\Simple3DConformance.exe"
    } "Simple3D conformance DirectX build"

    Invoke-Checked {
        & $compiler --project "examples\Simple3DConformance\Simple3DConformance.smileproj" --target web --configuration $Configuration --output-dir "artifacts\web\Simple3DConformance"
    } "Simple3D conformance Web build"

    Invoke-Checked {
        node "scripts\run-web-test.js" "artifacts\web\Simple3DConformance" --frames 8 --timeout 10000 --renderer3d
    } "Simple3D conformance WebGL2 execution"

    Invoke-Checked {
        & $compiler --project "games\NeonCycles\NeonCycles.smileproj" --target windows-x64 --configuration $Configuration --graphics DirectX -o "artifacts\games\NeonCycles\NeonCycles.exe"
    } "Neon Cycles DirectX build"

    Invoke-Checked {
        & $compiler --project "games\NeonCycles\NeonCycles.smileproj" --target web --configuration $Configuration --output-dir "artifacts\web\NeonCycles"
    } "Neon Cycles Web build"

    Invoke-Checked {
        node "scripts\run-web-test.js" "artifacts\web\NeonCycles" --frames 8 --timeout 10000 --renderer3d
    } "Neon Cycles one-player WebGL2 execution"

    Invoke-Checked {
        node "scripts\run-web-test.js" "artifacts\web\NeonCycles" --frames 8 --timeout 10000 --renderer3d --neon-cycles-input
    } "Neon Cycles two-player input execution"

    Test-NativeLaunch "artifacts\examples\Simple3DConformance\Simple3DConformance.exe" "Simple3D conformance native smoke"
    Test-NativeLaunch "artifacts\games\NeonCycles\NeonCycles.exe" "Neon Cycles native smoke"

    Write-Host "True Simple3D and Neon Cycles focused validation passed."
}
finally {
    Pop-Location
}
