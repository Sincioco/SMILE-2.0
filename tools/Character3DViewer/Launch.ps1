[CmdletBinding()]
param(
    [string]$Executable = (Join-Path $PSScriptRoot 'bin\Character3DViewer.exe'),
    [switch]$Build
)

$ErrorActionPreference = 'Stop'
$toolRoot = $PSScriptRoot
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $toolRoot '..\..'))
$syncScript = Join-Path $repositoryRoot 'scripts\sync-arin-v5-7-calibration.ps1'
$resolvedExecutable = [IO.Path]::GetFullPath($Executable)

& $syncScript -Mode Export -AllowMissing

$viewers = Get-Process -ErrorAction SilentlyContinue | Where-Object {
    $_.ProcessName -like 'Character3DViewer*' -or
    $_.MainWindowTitle -like 'SMILE 2.0 - 3D Viewer, Animation Editor*' -or
    $_.MainWindowTitle -like 'SMILE 2.0 - Character 3D Viewer*'
}

foreach ($viewer in $viewers) {
    if (-not $viewer.HasExited) {
        $null = $viewer.CloseMainWindow()
    }
}

if ($viewers.Count -gt 0) {
    Start-Sleep -Milliseconds 500
}

foreach ($viewer in $viewers) {
    if (-not $viewer.HasExited) {
        Stop-Process -Id $viewer.Id
    }
}

if ($Build -or -not (Test-Path -LiteralPath $resolvedExecutable -PathType Leaf)) {
    & (Join-Path $toolRoot 'Build.ps1')
}

if (-not (Test-Path -LiteralPath $resolvedExecutable -PathType Leaf)) {
    throw "Character Viewer/editor executable is missing: $resolvedExecutable"
}

& $syncScript -Mode Restore
$viewerProcess = Start-Process -FilePath $resolvedExecutable `
    -WorkingDirectory ([IO.Path]::GetDirectoryName($resolvedExecutable)) -PassThru
$shellCommand = Get-Command pwsh.exe -ErrorAction SilentlyContinue

if ($null -eq $shellCommand) {
    $shellCommand = Get-Command powershell.exe -ErrorAction Stop
}

$watchArguments = '-NoProfile -ExecutionPolicy Bypass -File "{0}" -Mode Watch -ViewerProcessId {1}' -f `
    $syncScript, $viewerProcess.Id
Start-Process -FilePath $shellCommand.Source -ArgumentList $watchArguments `
    -WindowStyle Hidden | Out-Null

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
