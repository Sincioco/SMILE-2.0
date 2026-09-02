[CmdletBinding()]
param(
    [string]$Executable = 'artifacts\games\Character3DViewer.exe',
    [string]$OutputDirectory = `
        'docs\implementation\screenshots\m7c-b1-paladin-v5-4-hardening'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$executablePath = [IO.Path]::GetFullPath((Join-Path $repositoryRoot $Executable))
$outputPath = [IO.Path]::GetFullPath((Join-Path $repositoryRoot $OutputDirectory))
$repositoryPrefix = $repositoryRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) + `
    [IO.Path]::DirectorySeparatorChar
if (-not $outputPath.StartsWith($repositoryPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Paladin evidence output escaped the repository.'
}
if (-not (Test-Path -LiteralPath $executablePath -PathType Leaf)) {
    throw "Character Viewer executable is missing: $executablePath"
}

Add-Type -AssemblyName System.Drawing.Common
if (-not ('SmilePaladinCaptureNative' -as [Type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

public static class SmilePaladinCaptureNative
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
    public static extern bool GetWindowRect(IntPtr window, out Rect rectangle);

    [DllImport("user32.dll")]
    public static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr window);

    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y,
        int width, int height, uint flags);
}
'@
}

function Get-WindowHandle([Diagnostics.Process]$Process) {
    $deadline = [DateTime]::UtcNow.AddSeconds(15)
    do {
        $Process.Refresh()
        if ($Process.HasExited) { throw 'Character Viewer exited before evidence capture.' }
        if ($Process.MainWindowHandle -ne [IntPtr]::Zero) { return $Process.MainWindowHandle }
        Start-Sleep -Milliseconds 100
    } while ([DateTime]::UtcNow -lt $deadline)
    throw 'Character Viewer did not publish a window in time.'
}

function Send-Key([IntPtr]$Window, [int]$VirtualKey) {
    [void][SmilePaladinCaptureNative]::PostMessage(
        $Window, 0x0100, [IntPtr]$VirtualKey, [IntPtr]::Zero)
    Start-Sleep -Milliseconds 40
    [void][SmilePaladinCaptureNative]::PostMessage(
        $Window, 0x0101, [IntPtr]$VirtualKey, [IntPtr]::Zero)
}

function Send-Click([IntPtr]$Window, [int]$X, [int]$Y) {
    $packed = [IntPtr](($Y -shl 16) -bor ($X -band 0xFFFF))
    [void][SmilePaladinCaptureNative]::PostMessage($Window, 0x0201, [IntPtr]1, $packed)
    Start-Sleep -Milliseconds 40
    [void][SmilePaladinCaptureNative]::PostMessage($Window, 0x0202, [IntPtr]::Zero, $packed)
}

function Set-ClientSize([IntPtr]$Window, [int]$Width, [int]$Height) {
    $client = [SmilePaladinCaptureNative+Rect]::new()
    $outer = [SmilePaladinCaptureNative+Rect]::new()
    if (-not [SmilePaladinCaptureNative]::GetClientRect($Window, [ref]$client) -or
        -not [SmilePaladinCaptureNative]::GetWindowRect($Window, [ref]$outer)) {
        throw 'Could not measure the Character Viewer window.'
    }
    $borderWidth = ($outer.Right - $outer.Left) - ($client.Right - $client.Left)
    $borderHeight = ($outer.Bottom - $outer.Top) - ($client.Bottom - $client.Top)
    if (-not [SmilePaladinCaptureNative]::SetWindowPos(
            $Window, [IntPtr]::Zero, 60, 60, $Width + $borderWidth,
            $Height + $borderHeight, 0x0040)) {
        throw 'Could not size the Character Viewer window.'
    }
    Start-Sleep -Milliseconds 500
}

function Get-ClientDimensions([IntPtr]$Window) {
    $rectangle = [SmilePaladinCaptureNative+Rect]::new()
    if (-not [SmilePaladinCaptureNative]::GetClientRect($Window, [ref]$rectangle)) {
        throw 'Could not read the Character Viewer client rectangle.'
    }
    return @(($rectangle.Right - $rectangle.Left), ($rectangle.Bottom - $rectangle.Top))
}

function Capture-Client([IntPtr]$Window, [string]$Path) {
    $rectangle = [SmilePaladinCaptureNative+Rect]::new()
    $origin = [SmilePaladinCaptureNative+Point]::new()
    if (-not [SmilePaladinCaptureNative]::GetClientRect($Window, [ref]$rectangle) -or
        -not [SmilePaladinCaptureNative]::ClientToScreen($Window, [ref]$origin)) {
        throw 'Could not resolve the Character Viewer capture rectangle.'
    }
    $width = $rectangle.Right - $rectangle.Left
    $height = $rectangle.Bottom - $rectangle.Top
    if ($width -lt 320 -or $height -lt 240 -or $width -gt 4096 -or $height -gt 4096) {
        throw "Character Viewer capture dimensions are invalid: ${width}x${height}"
    }
    $bitmap = [Drawing.Bitmap]::new($width, $height)
    try {
        $graphics = [Drawing.Graphics]::FromImage($bitmap)
        try {
            $graphics.CopyFromScreen($origin.X, $origin.Y, 0, 0,
                [Drawing.Size]::new($width, $height),
                [Drawing.CopyPixelOperation]::SourceCopy)
        }
        finally {
            $graphics.Dispose()
        }
        $bitmap.Save($Path, [Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $bitmap.Dispose()
    }
}

function Reset-Front([IntPtr]$Window) {
    Send-Key $Window 0x0D
    Start-Sleep -Milliseconds 100
    Send-Key $Window 0x4F
    Start-Sleep -Milliseconds 500
}

function Select-Clip([IntPtr]$Window, [int]$Index) {
    Reset-Front $Window
    for ($value = 0; $value -lt $Index; $value++) {
        Send-Key $Window 0x09
    }
}

function Save-ResponsiveComposite([string]$Left, [string]$Right, [string]$Destination) {
    $leftImage = [Drawing.Image]::FromFile($Left)
    $rightImage = [Drawing.Image]::FromFile($Right)
    try {
        $cellWidth = 720
        $cellHeight = 405
        $labelHeight = 52
        $sheet = [Drawing.Bitmap]::new($cellWidth * 2, $cellHeight + $labelHeight)
        try {
            $graphics = [Drawing.Graphics]::FromImage($sheet)
            try {
                $graphics.Clear([Drawing.Color]::FromArgb(3, 7, 18))
                $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $font = [Drawing.Font]::new('Segoe UI', 17, [Drawing.FontStyle]::Bold)
                $brush = [Drawing.SolidBrush]::new([Drawing.Color]::White)
                try {
                    $graphics.DrawImage($leftImage, 0, 0, $cellWidth, $cellHeight)
                    $graphics.DrawImage($rightImage, $cellWidth, 0, $cellWidth, $cellHeight)
                    $graphics.DrawString('Minimum supported client · 800 × 540', $font, $brush, 16, 414)
                    $graphics.DrawString('Ultrawide client · 1440 × 700', $font, $brush, 736, 414)
                }
                finally {
                    $font.Dispose()
                    $brush.Dispose()
                }
            }
            finally {
                $graphics.Dispose()
            }
            $sheet.Save($Destination, [Drawing.Imaging.ImageFormat]::Png)
        }
        finally {
            $sheet.Dispose()
        }
    }
    finally {
        $leftImage.Dispose()
        $rightImage.Dispose()
    }
}

[IO.Directory]::CreateDirectory($outputPath) | Out-Null
$process = Start-Process -FilePath $executablePath `
    -WorkingDirectory ([IO.Path]::GetDirectoryName($executablePath)) -PassThru
try {
    [void]$process.WaitForInputIdle(10000)
    $window = Get-WindowHandle $process
    [void][SmilePaladinCaptureNative]::SetForegroundWindow($window)
    Set-ClientSize $window 1280 720
    Start-Sleep -Seconds 2

    Reset-Front $window
    Capture-Client $window (Join-Path $outputPath '01-native-idle-front.png')

    Select-Clip $window 4
    Start-Sleep -Milliseconds 450
    Capture-Client $window (Join-Path $outputPath '02-native-sword-attack.png')

    Select-Clip $window 5
    Start-Sleep -Milliseconds 450
    Capture-Client $window (Join-Path $outputPath '03-native-shield-bash-candidate.png')

    Select-Clip $window 9
    Start-Sleep -Milliseconds 2400
    Capture-Client $window (Join-Path $outputPath '04-native-ko-grounding.png')

    Reset-Front $window
    $dimensions = Get-ClientDimensions $window
    $panelLeft = [Math]::Max(492, $dimensions[0] - 268)
    Send-Click $window ($panelLeft + 161) 35
    Start-Sleep -Milliseconds 500
    Capture-Client $window (Join-Path $outputPath '05-native-socket-gizmos.png')

    Send-Click $window ($panelLeft + 94) 35
    Start-Sleep -Milliseconds 500
    Capture-Client $window (Join-Path $outputPath '06-native-material-channels.png')

    Send-Click $window ($panelLeft + 161) 35
    Start-Sleep -Milliseconds 500
    Capture-Client $window (Join-Path $outputPath '11-grid-gizmo-resource-counts.png')

    $minimumPath = Join-Path $repositoryRoot 'artifacts\temp\m7c-viewer-minimum.png'
    $ultrawidePath = Join-Path $repositoryRoot 'artifacts\temp\m7c-viewer-ultrawide.png'
    Set-ClientSize $window 800 540
    Reset-Front $window
    Capture-Client $window $minimumPath
    Set-ClientSize $window 1440 700
    Reset-Front $window
    Capture-Client $window $ultrawidePath
    Save-ResponsiveComposite $minimumPath $ultrawidePath `
        (Join-Path $outputPath '10-responsive-layouts.png')

    Write-Host "Captured native Paladin v5.4 evidence in $outputPath"
}
finally {
    if (-not $process.HasExited) {
        [void][SmilePaladinCaptureNative]::PostMessage(
            $process.MainWindowHandle, 0x0010, [IntPtr]::Zero, [IntPtr]::Zero)
        if (-not $process.WaitForExit(5000)) {
            $process.Kill()
            $process.WaitForExit()
        }
    }
    $process.Dispose()
}
