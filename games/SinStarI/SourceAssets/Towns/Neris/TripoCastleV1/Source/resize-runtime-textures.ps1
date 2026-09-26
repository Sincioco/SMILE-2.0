[CmdletBinding()]
param([ValidateSet(1024, 2048, 4096)][int]$Size = 2048)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$package = Split-Path $PSScriptRoot -Parent
$source = Join-Path $package 'Neris-Castle-Cleaned.glb'
$bytes = [IO.File]::ReadAllBytes($source)
$jsonLength = [BitConverter]::ToInt32($bytes, 12)
$document = [Text.Encoding]::UTF8.GetString($bytes, 20, $jsonLength) | ConvertFrom-Json
$binaryStart = 28 + $jsonLength
$textures = @()
for ($i = 0; $i -lt $document.images.Count; $i++) {
    $view = $document.bufferViews[$document.images[$i].bufferView]
    $stream = [IO.MemoryStream]::new($bytes, $binaryStart + $view.byteOffset, $view.byteLength)
    $inputImage = [Drawing.Image]::FromStream($stream)
    $ratio = [Math]::Min(1.0, $Size / [double][Math]::Max($inputImage.Width, $inputImage.Height))
    $bitmap = [Drawing.Bitmap]::new([int]($inputImage.Width * $ratio), [int]($inputImage.Height * $ratio))
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.CompositingMode = [Drawing.Drawing2D.CompositingMode]::SourceCopy
        $graphics.DrawImage($inputImage, [Drawing.Rectangle]::new(0, 0, $bitmap.Width, $bitmap.Height))
        $extension = if ($document.images[$i].mimeType -eq 'image/png') { '.png' } else { '.jpg' }
        $name = "Castle-PBR-$i$extension"
        $output = Join-Path $package "Runtime\$name"
        $format = if ($extension -eq '.png') { [Drawing.Imaging.ImageFormat]::Png } else { [Drawing.Imaging.ImageFormat]::Jpeg }
        $bitmap.Save($output, $format)
        $textures += @{ file = $name; width = $bitmap.Width; height = $bitmap.Height; sha256 = (Get-FileHash -LiteralPath $output).Hash.ToLowerInvariant() }
        Write-Host "$name -> $($bitmap.Width)x$($bitmap.Height)"
    }
    finally { $graphics.Dispose(); $bitmap.Dispose(); $inputImage.Dispose(); $stream.Dispose() }
}
$manifestPath = Join-Path $package 'Runtime\manifest.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json -AsHashtable
$manifest['runtimeTextures'] = $textures
$manifest['texturePolicy'] = '2K runtime derivative; original 4K textures preserved in the cleaned source GLB and Blender file.'
$manifest['placement']['x'] = -258
$manifest | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $manifestPath -Encoding utf8
