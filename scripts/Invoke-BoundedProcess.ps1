[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProgramPath,

    [string]$Arguments = "",

    [ValidateRange(1, 86400)]
    [int]$TimeoutSeconds = 60
)

$ErrorActionPreference = "Stop"

$startInfo = New-Object System.Diagnostics.ProcessStartInfo
$startInfo.FileName = $ProgramPath
$startInfo.Arguments = $Arguments
$startInfo.UseShellExecute = $false
$startInfo.RedirectStandardOutput = $true
$startInfo.RedirectStandardError = $true
$startInfo.CreateNoWindow = $true

$process = New-Object System.Diagnostics.Process
$process.StartInfo = $startInfo

try {
    try {
        if (-not $process.Start()) {
            [Console]::Error.WriteLine("Bounded process could not start: $ProgramPath $Arguments")
            exit 126
        }
    }
    catch {
        [Console]::Error.WriteLine("Bounded process could not start '$ProgramPath': $($_.Exception.Message)")
        exit 126
    }

    $standardOutput = $process.StandardOutput.ReadToEndAsync()
    $standardError = $process.StandardError.ReadToEndAsync()
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
        try {
            & taskkill.exe /PID $process.Id /T /F 2>$null | Out-Null
        }
        catch {
            # Fall through to the direct-process fallback below.
        }
        if (-not $process.HasExited) {
            try { $process.Kill() } catch { }
        }
        $process.WaitForExit()
        [Console]::Out.Write($standardOutput.Result)
        [Console]::Error.Write($standardError.Result)
        [Console]::Error.WriteLine(
            "Bounded process timed out after $TimeoutSeconds second(s): $ProgramPath $Arguments")
        exit 124
    }

    $process.WaitForExit()
    [Console]::Out.Write($standardOutput.Result)
    [Console]::Error.Write($standardError.Result)
    exit $process.ExitCode
}
finally {
    $process.Dispose()
}
