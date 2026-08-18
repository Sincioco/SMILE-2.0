$ErrorActionPreference = "Stop"

$runner = Join-Path $PSScriptRoot "Invoke-BoundedProcess.ps1"
$powershell = (Get-Command powershell.exe -ErrorAction Stop).Source

$normalOutput = & $powershell -NoProfile -ExecutionPolicy Bypass -File $runner `
    -TimeoutSeconds 5 -ProgramPath "cmd.exe" -Arguments "/d /c echo bounded-ok" 2>&1
if ($LASTEXITCODE -ne 0 -or ($normalOutput -join "`n").Trim() -ne "bounded-ok") {
    throw "Bounded-process success-path test failed: $($normalOutput -join ' ')"
}

& $powershell -NoProfile -ExecutionPolicy Bypass -File $runner `
    -TimeoutSeconds 5 -ProgramPath "cmd.exe" -Arguments "/d /c exit 7"
if ($LASTEXITCODE -ne 7) {
    throw "Bounded-process exit-code test expected 7, found $LASTEXITCODE."
}

$started = [Diagnostics.Stopwatch]::StartNew()
$savedErrorActionPreference = $ErrorActionPreference
try {
    $ErrorActionPreference = "Continue"
    $timeoutOutput = & $powershell -NoProfile -ExecutionPolicy Bypass -File $runner `
        -TimeoutSeconds 1 -ProgramPath "ping.exe" -Arguments "-n 10 127.0.0.1" 2>&1
    $timeoutExitCode = $LASTEXITCODE
}
finally {
    $ErrorActionPreference = $savedErrorActionPreference
}
$started.Stop()
if ($timeoutExitCode -ne 124) {
    throw "Bounded-process timeout test expected exit 124, found $timeoutExitCode."
}
if ($started.Elapsed.TotalSeconds -gt 8) {
    throw "Bounded-process timeout test took $($started.Elapsed.TotalSeconds) seconds."
}
if (($timeoutOutput -join "`n") -notmatch "timed out after 1 second") {
    throw "Bounded-process timeout test did not report the command and deadline."
}

Write-Host "Bounded-process runner tests passed."
