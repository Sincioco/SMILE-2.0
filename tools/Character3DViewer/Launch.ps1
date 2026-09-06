[CmdletBinding()]
param(
    [string]$Executable,
    [switch]$Build,
    [switch]$SkipWindowActivation,
    [switch]$FunctionsOnly,
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [ValidateRange(1, 30)]
    [int]$GracefulCloseTimeoutSeconds = 5
)

$ErrorActionPreference = 'Stop'
$toolRoot = $PSScriptRoot
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $toolRoot '..\..'))
$syncScript = Join-Path $repositoryRoot 'scripts\sync-arin-v5-7-calibration.ps1'
$configurationExecutable = [IO.Path]::GetFullPath(
    (Join-Path $toolRoot "bin\$Configuration\Character3DViewer.exe")
)

function Assert-ViewerLaunchPrerequisites(
    [string]$ResolvedExecutable,
    [string]$StandardExecutable,
    [bool]$BuildRequested
) {
    $customExecutable = $ResolvedExecutable -ine $StandardExecutable

    if ($customExecutable -and
        ($BuildRequested -or -not (Test-Path -LiteralPath $ResolvedExecutable -PathType Leaf))) {
        throw 'A custom -Executable must already exist and cannot be combined with -Build. Use -Configuration to build and launch the standard output.'
    }

    if ($BuildRequested -or -not (Test-Path -LiteralPath $ResolvedExecutable -PathType Leaf)) {
        $compiler = Join-Path $repositoryRoot 'artifacts\compiler\smilec.exe'
        $project = Join-Path $toolRoot 'Character3DViewer.smileproj'
        $buildScript = Join-Path $toolRoot 'Build.ps1'
        $preparationScript = Join-Path $toolRoot 'Prepare-BuildAssets.ps1'

        foreach ($required in @($compiler, $project, $buildScript, $preparationScript)) {
            if (-not (Test-Path -LiteralPath $required -PathType Leaf)) {
                throw "Character Viewer build prerequisite is missing; the running Viewer was not closed: $required"
            }
        }

        & $preparationScript -ValidateOnly
    }
}

function Test-ViewerProcessOwned(
    [string]$ProcessName,
    [string]$ExecutablePath,
    [string]$ResolvedExecutable
) {
    if ([string]::IsNullOrWhiteSpace($ExecutablePath)) { return $false }

    $normalizedPath = [IO.Path]::GetFullPath($ExecutablePath)
    $toolPrefix = $toolRoot.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar

    return $normalizedPath -ieq $ResolvedExecutable -or
        ($ProcessName -like 'Character3DViewer*' -and
            $normalizedPath.StartsWith($toolPrefix, [StringComparison]::OrdinalIgnoreCase))
}

function Get-ViewerProcessCandidates([string]$ResolvedExecutable) {
    $candidates = [Collections.Generic.List[object]]::new()

    foreach ($process in Get-Process -ErrorAction SilentlyContinue) {
        try {
            $path = $process.Path

            if ([string]::IsNullOrWhiteSpace($path)) { continue }

            $normalizedPath = [IO.Path]::GetFullPath($path)
            $owned = Test-ViewerProcessOwned $process.ProcessName $normalizedPath $ResolvedExecutable

            if ($owned) {
                $candidates.Add([pscustomobject]@{
                    Id = $process.Id
                    ExecutablePath = $normalizedPath
                    StartTimeUtcTicks = $process.StartTime.ToUniversalTime().Ticks
                })
            }
        } catch {
            # Inaccessible process identity is never authority to close it.
        }
    }

    return $candidates.ToArray()
}

function Get-RevalidatedViewerProcess($Candidate) {
    try {
        $process = Get-Process -Id $Candidate.Id -ErrorAction Stop
        $process.Refresh()
        $path = [IO.Path]::GetFullPath($process.Path)
        $startTimeUtcTicks = $process.StartTime.ToUniversalTime().Ticks

        if ($path -ine $Candidate.ExecutablePath -or
            $startTimeUtcTicks -ne $Candidate.StartTimeUtcTicks) {
            return $null
        }

        return $process
    } catch {
        return $null
    }
}

function Request-ViewerShutdown($Candidates, [int]$TimeoutSeconds) {
    foreach ($candidate in @($Candidates)) {
        $process = Get-RevalidatedViewerProcess $candidate

        if ($null -ne $process -and -not $process.HasExited) {
            $null = $process.CloseMainWindow()
        }
    }

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)

    do {
        $remaining = @($Candidates | Where-Object {
            $process = Get-RevalidatedViewerProcess $_
            $null -ne $process -and -not $process.HasExited
        })

        if ($remaining.Count -eq 0) { return }

        Start-Sleep -Milliseconds 100
    } while ([DateTime]::UtcNow -lt $deadline)

    $identities = $remaining | ForEach-Object { "$($_.Id):$($_.ExecutablePath)" }
    throw "Character Viewer/editor did not close within $TimeoutSeconds seconds and was not forcibly stopped: $($identities -join ', ')"
}

if ($FunctionsOnly) { return }

if ([string]::IsNullOrWhiteSpace($Executable)) {
    $Executable = $configurationExecutable
}

$resolvedExecutable = [IO.Path]::GetFullPath($Executable)
Assert-ViewerLaunchPrerequisites $resolvedExecutable $configurationExecutable $Build.IsPresent

$orinProfile = Join-Path $repositoryRoot 'games\SinStarI\SourceAssets\Characters\Tank\OrinV13\Calibration\orin-v1.3-profile.json'
$characters = @('Arin')

if (Test-Path -LiteralPath $orinProfile -PathType Leaf) {
    $characters += 'Orin'
}

foreach ($character in $characters) {
    & $syncScript -Character $character -Mode Export -AllowMissing
}

$viewers = @(Get-ViewerProcessCandidates $resolvedExecutable)
Request-ViewerShutdown $viewers $GracefulCloseTimeoutSeconds

# Capture a final checked revision after the Viewer has closed and its watcher
# has had a chance to flush. A refused close aborts before build or replacement.
foreach ($character in $characters) {
    & $syncScript -Character $character -Mode Export -AllowMissing
}

if ($Build -or -not (Test-Path -LiteralPath $resolvedExecutable -PathType Leaf)) {
    & (Join-Path $toolRoot 'Build.ps1') -Configuration $Configuration -Target Native
}

if (-not (Test-Path -LiteralPath $resolvedExecutable -PathType Leaf)) {
    throw "Character Viewer/editor executable is missing: $resolvedExecutable"
}

foreach ($character in $characters) {
    & $syncScript -Character $character -Mode Restore
}
$viewerProcess = Start-Process -FilePath $resolvedExecutable `
    -WorkingDirectory ([IO.Path]::GetDirectoryName($resolvedExecutable)) -PassThru
$shellCommand = Get-Command pwsh.exe -ErrorAction SilentlyContinue

if ($null -eq $shellCommand) {
    $shellCommand = Get-Command powershell.exe -ErrorAction Stop
}

foreach ($character in $characters) {
    $watchArguments = '-NoProfile -ExecutionPolicy Bypass -File "{0}" -Character {1} -Mode Watch -ViewerProcessId {2}' -f `
        $syncScript, $character, $viewerProcess.Id
    Start-Process -FilePath $shellCommand.Source -ArgumentList $watchArguments `
        -WindowStyle Hidden | Out-Null
}

# Automation can use the supported Windows app-control surface for foreground
# activation while retaining this launcher's working directory and save watchers.
if ($SkipWindowActivation) {
    Write-Host "Launched Character Viewer/editor process $($viewerProcess.Id): $resolvedExecutable"
    return
}

Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

public static class SmileViewerWindowActivation
{
    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(
        IntPtr windowHandle,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    [DllImport("user32.dll")]
    public static extern bool ShowWindowAsync(IntPtr windowHandle, int showCommand);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr windowHandle);
}
'@

try {
    $null = $viewerProcess.WaitForInputIdle(10000)
    $activationDeadline = [DateTime]::UtcNow.AddSeconds(5)

    do {
        $viewerProcess.Refresh()
        $windowHandle = $viewerProcess.MainWindowHandle

        if ($windowHandle -eq [IntPtr]::Zero) {
            Start-Sleep -Milliseconds 50
        }
    } while ($windowHandle -eq [IntPtr]::Zero -and [DateTime]::UtcNow -lt $activationDeadline)

    if ($windowHandle -ne [IntPtr]::Zero) {
        $null = [SmileViewerWindowActivation]::ShowWindowAsync($windowHandle, 9)
        $positionFlags = [uint32]0x0043
        $null = [SmileViewerWindowActivation]::SetWindowPos(
            $windowHandle, [IntPtr](-1), 0, 0, 0, 0, $positionFlags)
        $null = [SmileViewerWindowActivation]::SetWindowPos(
            $windowHandle, [IntPtr](-2), 0, 0, 0, 0, $positionFlags)
        $null = [SmileViewerWindowActivation]::SetForegroundWindow($windowHandle)
        $windowShell = New-Object -ComObject WScript.Shell
        $null = $windowShell.AppActivate($viewerProcess.Id)
    }
} catch {
    Write-Warning "The Character Viewer/editor launched, but its window could not be activated: $($_.Exception.Message)"
}

Write-Host "Launched Character Viewer/editor process $($viewerProcess.Id): $resolvedExecutable"
