[CmdletBinding()]
param(
    [string]$Executable = 'artifacts\examples\AetherBladeVfxLab.exe',
    [string]$OutputDirectory = 'docs\implementation\screenshots\m7e-0-vfx3-preflight'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$executablePath = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $Executable))
$outputPath = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $OutputDirectory))
$repositoryPrefix = $repositoryRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar) +
    [System.IO.Path]::DirectorySeparatorChar

if (-not $outputPath.StartsWith($repositoryPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'VFX evidence output escaped the repository.'
}
if (-not (Test-Path -LiteralPath $executablePath -PathType Leaf)) {
    throw "VFX lab executable is missing: $executablePath"
}

Add-Type -AssemblyName System.Drawing.Common
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

public static class SmileVfxCaptureNative
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Point { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    public struct Rect { public int Left; public int Top; public int Right; public int Bottom; }

    [DllImport("user32.dll")]
    public static extern bool ClientToScreen(IntPtr window, ref Point point);

    [DllImport("user32.dll")]
    public static extern bool GetClientRect(IntPtr window, out Rect rectangle);

    [DllImport("user32.dll")]
    public static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr window);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr window, int command);
}
'@

function Get-WindowHandle([System.Diagnostics.Process]$Process) {
    $deadline = [DateTime]::UtcNow.AddSeconds(15)
    do {
        $Process.Refresh()
        if ($Process.HasExited) { throw 'VFX lab exited before evidence capture.' }
        if ($Process.MainWindowHandle -ne [IntPtr]::Zero) { return $Process.MainWindowHandle }
        Start-Sleep -Milliseconds 100
    } while ([DateTime]::UtcNow -lt $deadline)

    throw 'VFX lab did not publish a top-level window in time.'
}

function Send-Key([IntPtr]$Window, [int]$VirtualKey) {
    [void][SmileVfxCaptureNative]::PostMessage($Window, 0x0100, [IntPtr]$VirtualKey, [IntPtr]::Zero)
    Start-Sleep -Milliseconds 40
    [void][SmileVfxCaptureNative]::PostMessage($Window, 0x0101, [IntPtr]$VirtualKey, [IntPtr]::Zero)
}

function Capture-Client([IntPtr]$Window, [string]$Path) {
    $rectangle = [SmileVfxCaptureNative+Rect]::new()
    if (-not [SmileVfxCaptureNative]::GetClientRect($Window, [ref]$rectangle)) {
        throw 'Could not read the VFX lab client rectangle.'
    }

    $origin = [SmileVfxCaptureNative+Point]::new()
    if (-not [SmileVfxCaptureNative]::ClientToScreen($Window, [ref]$origin)) {
        throw 'Could not resolve the VFX lab screen position.'
    }

    $width = $rectangle.Right - $rectangle.Left
    $height = $rectangle.Bottom - $rectangle.Top
    if ($width -le 0 -or $height -le 0 -or $width -gt 4096 -or $height -gt 4096) {
        throw "VFX evidence dimensions are invalid: ${width}x${height}"
    }

    $bitmap = [System.Drawing.Bitmap]::new($width, $height)
    try {
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        try {
            $graphics.CopyFromScreen(
                $origin.X,
                $origin.Y,
                0,
                0,
                [System.Drawing.Size]::new($width, $height),
                [System.Drawing.CopyPixelOperation]::SourceCopy
            )
        }
        finally {
            $graphics.Dispose()
        }

        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $bitmap.Dispose()
    }
}

[System.IO.Directory]::CreateDirectory($outputPath) | Out-Null
$process = Start-Process -FilePath $executablePath `
    -WorkingDirectory ([System.IO.Path]::GetDirectoryName($executablePath)) `
    -PassThru
try {
    [void]$process.WaitForInputIdle(10000)
    $window = Get-WindowHandle $process
    [void][SmileVfxCaptureNative]::ShowWindow($window, 9)
    [void][SmileVfxCaptureNative]::SetForegroundWindow($window)
    Start-Sleep -Seconds 2

    Capture-Client $window (Join-Path $outputPath '01-energy-blade-idle-native.png')

    Send-Key $window 0x32
    Start-Sleep -Milliseconds 420
    Capture-Client $window (Join-Path $outputPath '02-energy-blade-swing-native.png')

    Start-Sleep -Milliseconds 780
    Capture-Client $window (Join-Path $outputPath '05-cpu-fallback-vfx-lab.png')

    Send-Key $window 0x31
    Start-Sleep -Milliseconds 350
    Capture-Client $window (Join-Path $outputPath '06-capability-diagnostics.png')

    Write-Host "Captured native M7E-0 VFX evidence in $outputPath"
}
finally {
    if (-not $process.HasExited) {
        [void][SmileVfxCaptureNative]::PostMessage(
            $process.MainWindowHandle,
            0x0010,
            [IntPtr]::Zero,
            [IntPtr]::Zero
        )
        if (-not $process.WaitForExit(5000)) {
            $process.Kill()
            $process.WaitForExit()
        }
    }
    $process.Dispose()
}
