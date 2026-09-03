[CmdletBinding()]
param(
    [string]$OutputPath = (Join-Path $PSScriptRoot `
        '..\games\Dragonfall\TechnicalAssets\Generation2\CharacterViewerFlameAtlas.png'),
    [switch]$Check
)

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

function New-FlamePath(
    [float]$Left,
    [float]$Top,
    [float]$Width,
    [float]$Height,
    [float]$TipOffset
) {
    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $bottomX = $Left + $Width / 2
    $bottomY = $Top + $Height
    $tipX = $bottomX + $TipOffset
    $path.StartFigure()
    $path.AddBezier(
        $bottomX,
        $bottomY,
        $Left - $Width * 0.08,
        $Top + $Height * 0.72,
        $Left + $Width * 0.15,
        $Top + $Height * 0.28,
        $tipX,
        $Top)
    $path.AddBezier(
        $tipX,
        $Top,
        $Left + $Width * 0.92,
        $Top + $Height * 0.24,
        $Left + $Width * 1.08,
        $Top + $Height * 0.72,
        $bottomX,
        $bottomY)
    $path.CloseFigure()
    return $path
}

$declaredOutput = [System.IO.Path]::GetFullPath($OutputPath)
$resolvedOutput = if ($Check) {
    Join-Path ([System.IO.Path]::GetTempPath()) `
        ("smile-character-viewer-flame-{0}.png" -f [Guid]::NewGuid().ToString('N'))
}
else {
    $declaredOutput
}
$outputDirectory = [System.IO.Path]::GetDirectoryName($resolvedOutput)
[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null

$bitmap = [System.Drawing.Bitmap]::new(
    256,
    64,
    [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$graphics.Clear([System.Drawing.Color]::Transparent)
$graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceOver
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

try {
    $tipOffsets = @(-5, 4, -2, 6)

    for ($frame = 0; $frame -lt 4; $frame++) {
        $frameLeft = $frame * 64
        $outerPath = New-FlamePath ($frameLeft + 13) 3 38 56 $tipOffsets[$frame]
        $middlePath = New-FlamePath ($frameLeft + 19) 15 26 43 (-$tipOffsets[$frame] / 2)
        $corePath = New-FlamePath ($frameLeft + 25) 29 14 27 ($tipOffsets[$frame] / 3)
        $outerBrush = [System.Drawing.SolidBrush]::new(
            [System.Drawing.Color]::FromArgb(110, 255, 54, 8))
        $middleBrush = [System.Drawing.SolidBrush]::new(
            [System.Drawing.Color]::FromArgb(185, 255, 142, 18))
        $coreBrush = [System.Drawing.SolidBrush]::new(
            [System.Drawing.Color]::FromArgb(235, 255, 244, 170))

        try {
            $graphics.FillPath($outerBrush, $outerPath)
            $graphics.FillPath($middleBrush, $middlePath)
            $graphics.FillPath($coreBrush, $corePath)
        }
        finally {
            $outerBrush.Dispose()
            $middleBrush.Dispose()
            $coreBrush.Dispose()
            $outerPath.Dispose()
            $middlePath.Dispose()
            $corePath.Dispose()
        }
    }

    $bitmap.Save($resolvedOutput, [System.Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $graphics.Dispose()
    $bitmap.Dispose()
}

$hash = (Get-FileHash -LiteralPath $resolvedOutput -Algorithm SHA256).Hash.ToLowerInvariant()

if ($Check) {
    try {
        if (-not (Test-Path -LiteralPath $declaredOutput -PathType Leaf)) {
            throw "Character Viewer flame atlas is missing: $declaredOutput"
        }

        $declaredHash = (Get-FileHash -LiteralPath $declaredOutput -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($declaredHash -cne $hash) {
            throw "Character Viewer flame atlas differs from deterministic output: $declaredOutput"
        }

        Write-Host "Verified $declaredOutput"
        Write-Host "SHA256 $hash"
    }
    finally {
        Remove-Item -LiteralPath $resolvedOutput -Force -ErrorAction SilentlyContinue
    }
}
else {
    Write-Host "Generated $resolvedOutput"
    Write-Host "SHA256 $hash"
}
