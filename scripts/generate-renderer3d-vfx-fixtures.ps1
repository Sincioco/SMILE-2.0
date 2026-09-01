[CmdletBinding()]
param(
    [string]$OutputPath = (Join-Path $PSScriptRoot '..\examples\Renderer3DVfxLab\Assets\VfxAtlas.png'),
    [switch]$Check
)

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

$declaredOutput = [System.IO.Path]::GetFullPath($OutputPath)
$resolvedOutput = if ($Check) {
    Join-Path ([System.IO.Path]::GetTempPath()) ("smile-vfx-atlas-{0}.png" -f [Guid]::NewGuid().ToString('N'))
}
else {
    $declaredOutput
}
$outputDirectory = [System.IO.Path]::GetDirectoryName($resolvedOutput)
[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null

$bitmap = [System.Drawing.Bitmap]::new(256, 256, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.Clear([System.Drawing.Color]::Transparent)

try {
    for ($frame = 0; $frame -lt 16; $frame++) {
        $column = $frame % 4
        $row = [Math]::Floor($frame / 4)
        $centerX = $column * 64 + 32
        $centerY = $row * 64 + 32
        $radius = 27 - ($frame % 4) * 2
        $hue = $row

        switch ($hue) {
            0 { $red = 255; $green = 245; $blue = 160 }
            1 { $red = 112; $green = 214; $blue = 255 }
            2 { $red = 255; $green = 124; $blue = 34 }
            default { $red = 172; $green = 255; $blue = 226 }
        }

        for ($ring = 8; $ring -ge 1; $ring--) {
            $ringRadius = [Math]::Max(2, [Math]::Floor($radius * $ring / 8))
            $alpha = [Math]::Min(255, 18 + (8 - $ring) * 25)
            $color = [System.Drawing.Color]::FromArgb($alpha, $red, $green, $blue)
            $brush = [System.Drawing.SolidBrush]::new($color)
            try {
                $graphics.FillEllipse(
                    $brush,
                    $centerX - $ringRadius,
                    $centerY - $ringRadius,
                    $ringRadius * 2,
                    $ringRadius * 2)
            }
            finally {
                $brush.Dispose()
            }
        }

        $core = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(245, 255, 255, 255))
        try {
            $coreRadius = 4 + ($frame % 3)
            $graphics.FillEllipse(
                $core,
                $centerX - $coreRadius,
                $centerY - $coreRadius,
                $coreRadius * 2,
                $coreRadius * 2)
        }
        finally {
            $core.Dispose()
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
        if (-not (Test-Path -LiteralPath $declaredOutput)) {
            throw "VFX atlas fixture is missing: $declaredOutput"
        }

        $declaredHash = (Get-FileHash -LiteralPath $declaredOutput -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($declaredHash -cne $hash) {
            throw "VFX atlas fixture differs from deterministic output: $declaredOutput"
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
