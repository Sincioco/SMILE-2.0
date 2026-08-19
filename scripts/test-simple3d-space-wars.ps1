param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$compiler = Join-Path $repositoryRoot "artifacts\compiler\smilec.exe"
$simple3DPackage = Join-Path $repositoryRoot "libraries\Smile.Simple3D\bin\$Configuration\Smile.Simple3D.smilelib"

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

Push-Location $repositoryRoot
try {
    Invoke-Checked {
        & $compiler --project "libraries\Smile.Simple3D\Smile.Simple3D.smilelibproj" --target library --configuration $Configuration
    } "Simple3D package build"

    Invoke-Checked {
        & $compiler --project "examples\Simple3DTests\Simple3DTests.smileproj" --target windows-x64 --configuration $Configuration --graphics GDI -o "artifacts\tests\Simple3DTests.exe"
    } "Simple3D native state tests"

    Invoke-Checked {
        & "scripts\run-bounded-test.cmd" 30 "artifacts\tests\Simple3DTests.exe"
    } "Simple3D native execution"

    Invoke-Checked {
        & $compiler --project "examples\Simple3DTests\Simple3DTests.smileproj" --target web --configuration $Configuration --output-dir "artifacts\web\Simple3DTests"
    } "Simple3D Web state tests"

    Invoke-Checked {
        node "scripts\run-web-test.js" "artifacts\web\Simple3DTests" --expected "examples\Simple3DTests\expected.txt" --frames 3 --timeout 10000
    } "Simple3D Web execution"

    Invoke-Checked {
        & $compiler --project "examples\Simple3DGallery\Simple3DGallery.smileproj" --target windows-x64 --configuration $Configuration --graphics GDI -o "artifacts\games\Simple3DGallery-GDI\Simple3DGallery.exe"
    } "Simple3D Gallery GDI build"

    Invoke-Checked {
        & $compiler --project "examples\Simple3DGallery\Simple3DGallery.smileproj" --target web --configuration $Configuration --output-dir "artifacts\web\Simple3DGallery"
    } "Simple3D Gallery Web build"

    Invoke-Checked {
        node "scripts\run-web-test.js" "artifacts\web\Simple3DGallery" --frames 8 --timeout 10000
    } "Simple3D Gallery Web execution"

    Invoke-Checked {
        & $compiler --project "games\SpaceWars\SpaceWars.smileproj" --target windows-x64 --configuration $Configuration --graphics GDI -o "artifacts\games\SpaceWars-GDI\SpaceWars.exe"
    } "Space Wars GDI build"

    Invoke-Checked {
        & $compiler --project "games\SpaceWars\SpaceWars.smileproj" --target web --configuration $Configuration --output-dir "artifacts\web\SpaceWars"
    } "Space Wars Web build"

    Invoke-Checked {
        node "scripts\run-web-test.js" "artifacts\web\SpaceWars" --frames 8 --timeout 10000
    } "Space Wars Web title execution"

    $noDemoSources = @(
        "--source", "games\SpaceWars\SpaceWarsTypes.smile",
        "--source", "games\SpaceWars\SpaceWarsModels.smile",
        "--source", "games\SpaceWars\SpaceWarsGameplay.smile",
        "--library", $simple3DPackage
    )

    Invoke-Checked {
        & $compiler "games\SpaceWars\Program-NoDemo.smile" @noDemoSources --target windows-x64 --graphics GDI -o "artifacts\games\SpaceWars-NoDemo-GDI\SpaceWars-NoDemo.exe"
    } "Space Wars no-demo native build"

    $noDemoNativeAssets = "artifacts\games\SpaceWars-NoDemo-GDI\Assets"
    New-Item -ItemType Directory -Force -Path $noDemoNativeAssets | Out-Null
    Copy-Item "games\SpaceWars\Assets\*.wav" $noDemoNativeAssets -Force

    Invoke-Checked {
        & $compiler "games\SpaceWars\Program-NoDemo.smile" @noDemoSources --target web --output-dir "artifacts\web\SpaceWars-NoDemo"
    } "Space Wars no-demo Web build"

    $noDemoWebAssets = "artifacts\web\SpaceWars-NoDemo\Assets"
    New-Item -ItemType Directory -Force -Path $noDemoWebAssets | Out-Null
    Copy-Item "games\SpaceWars\Assets\*.wav" $noDemoWebAssets -Force

    Invoke-Checked {
        node "scripts\run-web-test.js" "artifacts\web\SpaceWars-NoDemo" --frames 8 --timeout 10000
    } "Space Wars no-demo Web execution"

    Invoke-Checked {
        & $compiler --project "games\SpaceWars\SpaceWarsStateTests.smileproj" --target windows-x64 --configuration $Configuration --graphics GDI -o "artifacts\tests\SpaceWarsStateTests.exe"
    } "Space Wars native state tests"

    Invoke-Checked {
        & "scripts\run-bounded-test.cmd" 30 "artifacts\tests\SpaceWarsStateTests.exe"
    } "Space Wars native state execution"

    Invoke-Checked {
        & $compiler --project "games\SpaceWars\SpaceWarsStateTests.smileproj" --target web --configuration $Configuration --output-dir "artifacts\web\SpaceWarsStateTests"
    } "Space Wars Web state tests"

    Invoke-Checked {
        node "scripts\run-web-test.js" "artifacts\web\SpaceWarsStateTests" --expected "games\SpaceWars\SpaceWarsStateTests.expected.txt" --frames 3 --timeout 10000
    } "Space Wars Web state execution"

    Write-Host "Simple3D and Space Wars focused validation passed."
}
finally {
    Pop-Location
}
