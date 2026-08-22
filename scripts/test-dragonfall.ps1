[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$RepositoryRoot = Split-Path -Parent $PSScriptRoot
$Compiler = Join-Path $RepositoryRoot 'artifacts\compiler\smilec.exe'
$Artifacts = Join-Path $RepositoryRoot 'artifacts'
$GameDirectory = Join-Path $RepositoryRoot 'games\Dragonfall'
$Runner = Join-Path $RepositoryRoot 'scripts\run-web-test.js'

function Assert-ExactOutput {
    param(
        [Parameter(Mandatory)]
        [string]$ActualPath,

        [Parameter(Mandatory)]
        [string]$ExpectedPath
    )

    $actual = [IO.File]::ReadAllText($ActualPath).Replace("`r`n", "`n").TrimEnd("`n")
    $expected = [IO.File]::ReadAllText($ExpectedPath).Replace("`r`n", "`n").TrimEnd("`n")

    if ($actual -cne $expected) {
        throw "Output '$ActualPath' differs from '$ExpectedPath'."
    }
}

function Compile-Project {
    param(
        [Parameter(Mandatory)]
        [string]$Project,

        [Parameter(Mandatory)]
        [string]$Output,

        [switch]$Web
    )

    if ($Web) {
        & $Compiler --project $Project --target web --configuration Release --output-dir $Output
    }
    else {
        & $Compiler --project $Project --configuration Release -o $Output
    }

    if ($LASTEXITCODE -ne 0) {
        throw "Compilation failed for '$Project'."
    }
}

$mechanicsProject = Join-Path $GameDirectory 'DragonfallTests.smileproj'
$mechanicsExpected = Join-Path $GameDirectory 'DragonfallTests.expected.txt'
$mechanicsNative = Join-Path $Artifacts 'tests\DragonfallTests.exe'
$mechanicsOutput = Join-Path $Artifacts 'temp\DragonfallTests.out'
$mechanicsWeb = Join-Path $Artifacts 'web\DragonfallTests'

Compile-Project $mechanicsProject $mechanicsNative
& $mechanicsNative | Set-Content -LiteralPath $mechanicsOutput -Encoding utf8

if ($LASTEXITCODE -ne 0) {
    throw 'Dragonfall native mechanics execution failed.'
}

Assert-ExactOutput $mechanicsOutput $mechanicsExpected
Compile-Project $mechanicsProject $mechanicsWeb -Web
& node $Runner $mechanicsWeb --expected $mechanicsExpected --timeout 60000

if ($LASTEXITCODE -ne 0) {
    throw 'Dragonfall Web mechanics execution failed.'
}

$balanceProject = Join-Path $GameDirectory 'DragonfallBalanceTests.smileproj'
$balanceExpected = Join-Path $GameDirectory 'DragonfallBalanceTests.expected.txt'
$balanceNative = Join-Path $Artifacts 'tests\DragonfallBalanceTests.exe'
$balanceOutput = Join-Path $Artifacts 'temp\DragonfallBalanceTests.out'

Compile-Project $balanceProject $balanceNative
& $balanceNative | Set-Content -LiteralPath $balanceOutput -Encoding utf8

if ($LASTEXITCODE -ne 0) {
    throw 'Dragonfall native balance simulation failed.'
}

Assert-ExactOutput $balanceOutput $balanceExpected

$lifecycleProject = Join-Path $GameDirectory 'DragonfallLifecycleTests.smileproj'
$lifecycleExpected = Join-Path $GameDirectory 'DragonfallLifecycleTests.expected.txt'
$lifecycleNative = Join-Path $Artifacts 'tests\DragonfallLifecycleTests.exe'
$lifecycleOutput = Join-Path $Artifacts 'temp\DragonfallLifecycleTests.out'
$lifecycleError = Join-Path $Artifacts 'temp\DragonfallLifecycleTests.err'
$lifecycleWeb = Join-Path $Artifacts 'web\DragonfallLifecycleTests'

Compile-Project $lifecycleProject $lifecycleNative
$process = Start-Process -FilePath $lifecycleNative -WorkingDirectory (Split-Path -Parent $lifecycleNative) -Wait -PassThru -WindowStyle Hidden -RedirectStandardOutput $lifecycleOutput -RedirectStandardError $lifecycleError

if ($process.ExitCode -ne 0) {
    throw "Dragonfall native lifecycle execution failed with exit code $($process.ExitCode)."
}

Assert-ExactOutput $lifecycleOutput $lifecycleExpected
Compile-Project $lifecycleProject $lifecycleWeb -Web
& node $Runner $lifecycleWeb --renderer3d --expected $lifecycleExpected --frames 4 --timeout 60000

if ($LASTEXITCODE -ne 0) {
    throw 'Dragonfall Web lifecycle execution failed.'
}

Compile-Project (Join-Path $GameDirectory 'Dragonfall.smileproj') (Join-Path $Artifacts 'games\Dragonfall.exe')
Compile-Project (Join-Path $GameDirectory 'Dragonfall-NoDemo.smileproj') (Join-Path $Artifacts 'games\Dragonfall-NoDemo.exe')
Compile-Project (Join-Path $GameDirectory 'Dragonfall.smileproj') (Join-Path $Artifacts 'web\Dragonfall') -Web
Compile-Project (Join-Path $GameDirectory 'Dragonfall-NoDemo.smileproj') (Join-Path $Artifacts 'web\Dragonfall-NoDemo') -Web

Write-Host 'Dragonfall native/Web mechanics, lifecycle, demo, and no-demo validation passed.'
