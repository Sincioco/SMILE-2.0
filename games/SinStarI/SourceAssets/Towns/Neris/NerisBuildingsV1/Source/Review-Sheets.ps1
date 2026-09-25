# Layout only: contact sheets assembled from actual Blender orthographic renders.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$previewFolder = Join-Path (Split-Path $PSScriptRoot -Parent) 'Previews'
$views = @('front', 'back', 'left', 'right')
$font = [System.Drawing.Font]::new('Segoe UI', 19)
$titleFont = [System.Drawing.Font]::new('Segoe UI', 24, [System.Drawing.FontStyle]::Bold)
$brush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(30, 43, 54))
$format = [System.Drawing.StringFormat]::new()
$format.Alignment = [System.Drawing.StringAlignment]::Center
$format.LineAlignment = [System.Drawing.StringAlignment]::Center
try {
    foreach ($beauty in Get-ChildItem -LiteralPath $previewFolder -Filter '*-beauty.png') {
        $slug = $beauty.BaseName -replace '-beauty$', ''
        $sheet = [System.Drawing.Bitmap]::new(1280, 1408)
        $graphics = [System.Drawing.Graphics]::FromImage($sheet)
        $graphics.Clear([System.Drawing.Color]::WhiteSmoke)
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.DrawString(($slug -replace '-', ' '), $titleFont, $brush,
            [System.Drawing.RectangleF]::new(0, 0, 1280, 64), $format)
        for ($i = 0; $i -lt 4; $i++) {
            $image = [System.Drawing.Image]::FromFile((Join-Path $previewFolder "$slug-$($views[$i]).png"))
            try {
                $x = ($i % 2) * 640
                $y = 64 + [Math]::Floor($i / 2) * 672
                $graphics.DrawImage($image, [System.Drawing.Rectangle]::new($x, $y, 640, 640))
                $graphics.DrawString($views[$i].ToUpperInvariant(), $font, $brush,
                    [System.Drawing.RectangleF]::new($x, $y + 640, 640, 32), $format)
            } finally { $image.Dispose() }
        }
        $sheet.Save((Join-Path $previewFolder "$slug-contact-sheet.png"), [System.Drawing.Imaging.ImageFormat]::Png)
        $graphics.Dispose()
        $sheet.Dispose()
        Write-Output "Contact sheet: $slug"
    }
} finally {
    $font.Dispose()
    $titleFont.Dispose()
    $brush.Dispose()
    $format.Dispose()
}
