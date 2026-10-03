[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Executable,
    [Parameter(Mandatory)][string]$Expected,
    [Parameter(Mandatory)][string]$LogPrefix,
    [int]$TimeoutSeconds = 60,
    [string[]]$Arguments = @()
)

$ErrorActionPreference = 'Stop'
$start = @{
    FilePath = $Executable
    WorkingDirectory = Split-Path $Executable -Parent
    WindowStyle = 'Hidden'
    RedirectStandardOutput = "$LogPrefix.stdout.log"
    RedirectStandardError = "$LogPrefix.stderr.log"
    PassThru = $true
}
if ($Arguments.Count) { $start.ArgumentList = $Arguments }
$child = Start-Process @start
try {
    if (-not $child.WaitForExit($TimeoutSeconds * 1000)) {
        # Only the disposable native child started here is ours to terminate.
        $child.Kill()
        $child.WaitForExit()
        throw "Native acceptance timed out after $TimeoutSeconds seconds: $Executable. Logs: $LogPrefix"
    }
    $exitCode = $child.ExitCode
    $stdout = [string](Get-Content -LiteralPath "$LogPrefix.stdout.log" -Raw)
    $stderr = [string](Get-Content -LiteralPath "$LogPrefix.stderr.log" -Raw)
    if ($exitCode -ne 0 -or ($stdout + $stderr) -match '\bFAIL\b' -or
        $stdout -notmatch ('(?m)^' + [regex]::Escape($Expected) + '\r?$')) {
        throw "Native acceptance failed (exit $exitCode): $Executable. Logs: $LogPrefix`n$stdout`n$stderr"
    }
    return $stdout
} finally {
    $child.Dispose()
}
