$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$viewer = Join-Path $root 'tools/Character3DViewer'
$output = Join-Path $root ('artifacts/tests/neris-policy-' + [Guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $output
$native = Join-Path $output 'Control.exe'
$vs = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" `
    -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
if (-not $vs) { throw 'Installed Visual C++ tools not found.' }
$setup = Join-Path $vs 'VC/Auxiliary/Build/vcvars64.bat'
$source = Join-Path $viewer 'NerisAcceptanceControl.c'
& cmd.exe /d /c "call `"$setup`" >nul && cl /nologo /O2 `"$source`" /Fo`"$output/Control.obj`" /Fe`"$native`"" *> "$output/compile.log"
if ($LASTEXITCODE -ne 0) { throw "Negative-control compilation failed: $output/compile.log" }
$check = Join-Path $PSScriptRoot 'Invoke-TownNativeCheck.ps1'
$positive = & $check -Executable $native -Expected 'PASS Neris Town Routes' -LogPrefix "$output/positive" -Arguments '0'
if (-not $positive.Contains('PASS')) { throw 'Positive control failed.' }

# Reproduce the old route predicate against a real nonzero native exit.
$oldText = & $native 1 | Out-String
$oldExit = $LASTEXITCODE
if ($oldExit -eq 0 -or $oldText -notmatch 'PASS Neris Town Routes' -or $oldText -match 'FAIL') {
    throw 'Old route false-positive counterexample did not reproduce.'
}
Write-Host "REPRODUCED old stdout-only predicate accepts native exit $oldExit."

foreach ($case in @(
    @{ Mode = '1'; Name = 'PASS plus nonzero exit'; Reason = 'exit 7' },
    @{ Mode = '2'; Name = 'zero exit missing PASS'; Reason = 'exit 0' },
    @{ Mode = '3'; Name = 'PASS plus FAIL'; Reason = 'exit 0' },
    @{ Mode = '4'; Name = 'route hang'; Reason = 'timed out' }
)) {
    $rejected = $false
    try {
        $null = & $check -Executable $native -Expected 'PASS Neris Town Routes' `
            -LogPrefix "$output/control-$($case.Mode)" -TimeoutSeconds 2 -Arguments $case.Mode
    } catch {
        if ($_.Exception.Message -notmatch [regex]::Escape($case.Reason)) { throw }
        $rejected = $true
    }
    if (-not $rejected) { throw "Accepted negative control: $($case.Name)" }
    Write-Host "PASS Rejects $($case.Name)"
}

$project = Join-Path $output 'SceneFailure.smileproj'
$scene = Join-Path $output 'SceneFailure.exe'
$sceneSource = Join-Path $viewer 'NerisSceneFailureTests.smile'
$library = Join-Path $root 'libraries/Smile.Simple3D/Smile.Simple3D.smilelibproj'
@"
<SmileProject Version="1.0"><PropertyGroup><ProjectKind>Game</ProjectKind>
<ApplicationId>smile.tests.neris-negative.run-$([Guid]::NewGuid().ToString('N'))</ApplicationId>
<StartupFile>$sceneSource</StartupFile></PropertyGroup><ItemGroup>
<SmileSource Include="$sceneSource" StartupOnly="true"/>
<SmileProjectReference Include="$library"/></ItemGroup></SmileProject>
"@ | Set-Content -LiteralPath $project
try {
    & (Join-Path $root 'artifacts/compiler/smilec.exe') --project $project --target windows-x64 -o $scene *> "$scene.compile.log"
    if ($LASTEXITCODE -ne 0) { Get-Content "$scene.compile.log" -Tail 12; throw 'Native scene negative control compilation failed.' }
    $rejected = $false
    try {
        $null = & $check -Executable $scene -Expected 'PASS Neris Town Scene' -LogPrefix "$output/scene-negative"
    } catch {
        $text = Get-Content "$output/scene-negative.stdout.log" -Raw
        if ($text -match 'INVALID' -or $text -notmatch 'FAIL Native Draw After Successful Begin And Draw') { throw }
        $rejected = $true
    }
    if (-not $rejected) { throw 'Accepted native render failure after earlier success.' }
    Write-Host "PASS Rejects actual native draw failure after success. Evidence: $output"
} finally {
    Remove-Item -LiteralPath $project -ErrorAction SilentlyContinue
}
