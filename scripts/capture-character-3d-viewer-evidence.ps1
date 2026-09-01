[CmdletBinding()]
param(
    [string]$Executable = 'artifacts\games\Character3DViewer.exe',
    [string]$OutputDirectory = 'docs\implementation\screenshots\m7b-1-paladin-viewer'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$executablePath = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $Executable))
$outputPath = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $OutputDirectory))
$repositoryPrefix = $repositoryRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar) +
    [System.IO.Path]::DirectorySeparatorChar

if (-not $outputPath.StartsWith($repositoryPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'Character Viewer evidence output escaped the repository.'
}
if (-not (Test-Path -LiteralPath $executablePath -PathType Leaf)) {
    throw "Character Viewer executable is missing: $executablePath"
}

Add-Type -AssemblyName System.Drawing.Common
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

public static class SmileViewerCaptureNative
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
        if ($Process.HasExited) { throw 'Character Viewer exited before evidence capture.' }
        if ($Process.MainWindowHandle -ne [IntPtr]::Zero) { return $Process.MainWindowHandle }
        Start-Sleep -Milliseconds 100
    } while ([DateTime]::UtcNow -lt $deadline)

    throw 'Character Viewer did not publish a top-level window in time.'
}

function Send-Key([IntPtr]$Window, [int]$VirtualKey) {
    $wmKeyDown = 0x0100
    $wmKeyUp = 0x0101
    [void][SmileViewerCaptureNative]::PostMessage($Window, $wmKeyDown, [IntPtr]$VirtualKey, [IntPtr]::Zero)
    Start-Sleep -Milliseconds 40
    [void][SmileViewerCaptureNative]::PostMessage($Window, $wmKeyUp, [IntPtr]$VirtualKey, [IntPtr]::Zero)
}

function Capture-Client([IntPtr]$Window, [string]$Path) {
    $rectangle = [SmileViewerCaptureNative+Rect]::new()
    if (-not [SmileViewerCaptureNative]::GetClientRect($Window, [ref]$rectangle)) {
        throw 'Could not read the Character Viewer client rectangle.'
    }

    $origin = [SmileViewerCaptureNative+Point]::new()
    if (-not [SmileViewerCaptureNative]::ClientToScreen($Window, [ref]$origin)) {
        throw 'Could not resolve the Character Viewer screen position.'
    }

    $width = $rectangle.Right - $rectangle.Left
    $height = $rectangle.Bottom - $rectangle.Top
    if ($width -le 0 -or $height -le 0 -or $width -gt 4096 -or $height -gt 4096) {
        throw "Character Viewer capture dimensions are invalid: ${width}x${height}"
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
    [void][SmileViewerCaptureNative]::ShowWindow($window, 9)
    [void][SmileViewerCaptureNative]::SetForegroundWindow($window)
    Start-Sleep -Seconds 3

    Capture-Client $window (Join-Path $outputPath '01-paladin-front-native.png')

    Send-Key $window 0x4F
    Start-Sleep -Seconds 3
    Capture-Client $window (Join-Path $outputPath '02-paladin-side-native.png')

    Start-Sleep -Seconds 3
    Capture-Client $window (Join-Path $outputPath '03-paladin-back-native.png')

    Send-Key $window 0x4F
    Write-Host "Captured native Character Viewer front, side, and back evidence in $outputPath"
}
finally {
    if (-not $process.HasExited) {
        [void][SmileViewerCaptureNative]::PostMessage(
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
