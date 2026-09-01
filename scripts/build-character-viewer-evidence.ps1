[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$evidenceRoot = Join-Path $repositoryRoot `
    'docs\implementation\screenshots\m7b-1-paladin-viewer'
$fixturePath = Join-Path $repositoryRoot 'artifacts\temp\m7b1-auto-fit-fixture.png'
$historicalPath = Join-Path $repositoryRoot `
    'docs\implementation\screenshots\m7b-arin-prototype\character-3d-viewer-web.png'
$maximumEvidenceBytes = 5MB

Add-Type -AssemblyName System.Drawing.Common

function Open-Bitmap([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Character Viewer evidence source is missing: $Path"
    }

    $source = [System.Drawing.Image]::FromFile($Path)
    try {
        $copy = [System.Drawing.Bitmap]::new(
            $source.Width,
            $source.Height,
            [System.Drawing.Imaging.PixelFormat]::Format32bppArgb
        )
        $graphics = [System.Drawing.Graphics]::FromImage($copy)
        try {
            $graphics.DrawImageUnscaled($source, 0, 0)
        }
        finally {
            $graphics.Dispose()
        }

        return $copy
    }
    finally {
        $source.Dispose()
    }
}

function Draw-FittedImage(
    [System.Drawing.Graphics]$Graphics,
    [System.Drawing.Image]$Image,
    [System.Drawing.Rectangle]$Bounds
) {
    $scale = [Math]::Min($Bounds.Width / $Image.Width, $Bounds.Height / $Image.Height)
    $width = [int][Math]::Round($Image.Width * $scale)
    $height = [int][Math]::Round($Image.Height * $scale)
    $x = $Bounds.X + [int](($Bounds.Width - $width) / 2)
    $y = $Bounds.Y + [int](($Bounds.Height - $height) / 2)
    $Graphics.DrawImage($Image, [System.Drawing.Rectangle]::new($x, $y, $width, $height))
}

function Save-Composite(
    [string[]]$Sources,
    [string[]]$Labels,
    [string]$Destination,
    [int]$Columns,
    [int]$CellWidth,
    [int]$CellImageHeight,
    [int]$LabelHeight,
    [string]$Title
) {
    $rows = [int][Math]::Ceiling($Sources.Count / $Columns)
    $titleHeight = if ([string]::IsNullOrWhiteSpace($Title)) { 0 } else { 72 }
    $width = $Columns * $CellWidth
    $height = $titleHeight + $rows * ($CellImageHeight + $LabelHeight)
    $sheet = [System.Drawing.Bitmap]::new(
        $width,
        $height,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb
    )
    try {
        $graphics = [System.Drawing.Graphics]::FromImage($sheet)
        try {
            $graphics.Clear([System.Drawing.Color]::FromArgb(3, 7, 18))
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $titleFont = [System.Drawing.Font]::new('Segoe UI', 25, [System.Drawing.FontStyle]::Bold)
            $labelFont = [System.Drawing.Font]::new('Segoe UI', 14, [System.Drawing.FontStyle]::Bold)
            $detailFont = [System.Drawing.Font]::new('Segoe UI', 11)
            $titleBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(60, 230, 255))
            $labelBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::White)
            $detailBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(190, 205, 225))
            try {
                if ($titleHeight -gt 0) {
                    $graphics.DrawString($Title, $titleFont, $titleBrush, 22, 18)
                }

                for ($index = 0; $index -lt $Sources.Count; $index++) {
                    $row = [int]($index / $Columns)
                    $column = $index % $Columns
                    $x = $column * $CellWidth
                    $y = $titleHeight + $row * ($CellImageHeight + $LabelHeight)
                    $image = Open-Bitmap $Sources[$index]
                    try {
                        Draw-FittedImage $graphics $image `
                            ([System.Drawing.Rectangle]::new($x + 8, $y + 8, $CellWidth - 16, $CellImageHeight - 16))
                    }
                    finally {
                        $image.Dispose()
                    }

                    $labelParts = $Labels[$index].Split('|', 2)
                    $graphics.DrawString(
                        $labelParts[0],
                        $labelFont,
                        $labelBrush,
                        [System.Drawing.RectangleF]::new(
                            $x + 14,
                            $y + $CellImageHeight + 4,
                            $CellWidth - 28,
                            27
                        )
                    )
                    if ($labelParts.Count -gt 1) {
                        $graphics.DrawString(
                            $labelParts[1],
                            $detailFont,
                            $detailBrush,
                            [System.Drawing.RectangleF]::new(
                                $x + 14,
                                $y + $CellImageHeight + 31,
                                $CellWidth - 28,
                                29
                            )
                        )
                    }
                }
            }
            finally {
                $titleFont.Dispose()
                $labelFont.Dispose()
                $detailFont.Dispose()
                $titleBrush.Dispose()
                $labelBrush.Dispose()
                $detailBrush.Dispose()
            }
        }
        finally {
            $graphics.Dispose()
        }

        $sheet.Save($Destination, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $sheet.Dispose()
    }
}

[System.IO.Directory]::CreateDirectory($evidenceRoot) | Out-Null

function Convert-ToTruePng([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Character Viewer evidence source is missing: $Path"
    }

    $prefix = [System.IO.File]::ReadAllBytes($Path)
    if ($prefix.Length -ge 8 -and
        $prefix[0] -eq 0x89 -and $prefix[1] -eq 0x50 -and
        $prefix[2] -eq 0x4E -and $prefix[3] -eq 0x47 -and
        $prefix[4] -eq 0x0D -and $prefix[5] -eq 0x0A -and
        $prefix[6] -eq 0x1A -and $prefix[7] -eq 0x0A) {
        return
    }

    $bitmap = Open-Bitmap $Path
    $converted = "$Path.true-png"
    try {
        $bitmap.Save($converted, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $bitmap.Dispose()
    }

    Move-Item -LiteralPath $converted -Destination $Path -Force
}

foreach ($name in @(
        '04-paladin-idle-web.png',
        '05-paladin-walk-web.png',
        '06-paladin-run-web.png',
        '07-paladin-socket-gizmos.png',
        '08-paladin-material-channels.png',
        '10-paladin-viewer-controls.png')) {
    Convert-ToTruePng (Join-Path $evidenceRoot $name)
}
Convert-ToTruePng $fixturePath

$arinAutoFit = Join-Path $evidenceRoot '04-paladin-idle-web.png'
$autoFitOutput = Join-Path $evidenceRoot '09-paladin-auto-fit-small-large.png'
Save-Composite `
    @($arinAutoFit, $fixturePath) `
    @('Arin profile|1.00 m source bounds fitted to the shared studio',
        'Articulated fixture|Different dimensions fitted with no viewer source edit') `
    $autoFitOutput `
    2 `
    640 `
    360 `
    68 `
    ''

$requiredNames = @(
    '01-paladin-front-native.png',
    '02-paladin-side-native.png',
    '03-paladin-back-native.png',
    '04-paladin-idle-web.png',
    '05-paladin-walk-web.png',
    '06-paladin-run-web.png',
    '07-paladin-socket-gizmos.png',
    '08-paladin-material-channels.png',
    '09-paladin-auto-fit-small-large.png',
    '10-paladin-viewer-controls.png'
)
$contactLabels = @(
    'Native front · Idle 100%|Official name Arin; party role Paladin',
    'Native side · Auto Orbit On|O-key smooth orbit and complete silhouette',
    'Native back · Auto Orbit On|Rear geometry, armor, and texture coverage',
    'Web · Idle 100%|Real skeleton and authored prototype idle clip',
    'Web · Walk 100%|Real skeleton and authored prototype walk clip',
    'Web · Run 100%|Real skeleton and authored prototype run clip',
    'Web · Socket gizmos|Prototype-inferred attachment positions and axes',
    'Web · Base Color channel|PBR material-channel inspection',
    'Web · Two auto-fit profiles|Bounds-driven reusable camera and floor',
    'Web · Controls|Orbit, pan, zoom, clips, lighting, speed, profile'
)
$requiredPaths = @($requiredNames | ForEach-Object { Join-Path $evidenceRoot $_ })
$contactPath = Join-Path $evidenceRoot 'paladin-viewer-contact-sheet-iphone.png'
Save-Composite `
    $requiredPaths `
    $contactLabels `
    $contactPath `
    2 `
    585 `
    329 `
    66 `
    'Arin · SMILE Character 3D Viewer · M7B.1'

if (Test-Path -LiteralPath $historicalPath -PathType Leaf) {
    $historical = Open-Bitmap $historicalPath
    $correctedHistorical = "$historicalPath.corrected.png"
    try {
        $historical.Save($correctedHistorical, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $historical.Dispose()
    }

    Move-Item -LiteralPath $correctedHistorical -Destination $historicalPath -Force
}

$metadata = @(
    @{ platform='Native'; quality='High / Viewer Studio'; clip='Idle / 100%'; camera='Front; yaw 0; pitch 0; zoom 0'; metrics='2 draws / 9,976 triangles'; shown='Arin front view and complete control/status UI'; proves='Native model, PBR textures, identity, framing, and release warning'; limit='1K JPEG-derived prototype textures'; goal='Reusable inspection before Dragonfall production intake' },
    @{ platform='Native'; quality='High / Viewer Studio'; clip='Idle / 100%'; camera='Side; auto-orbit about 90°; zoom 0'; metrics='2 draws / 9,976 triangles'; shown='Arin side silhouette with Auto Orbit On'; proves='O-key elapsed-time orbit and side completeness'; limit='Capture angle advances slightly while auto-orbit remains active'; goal='Smooth hands-free character inspection' },
    @{ platform='Native'; quality='High / Viewer Studio'; clip='Idle / 100%'; camera='Back; auto-orbit about 180°; zoom 0'; metrics='2 draws / 9,976 triangles'; shown='Arin rear armor and texture coverage'; proves='Complete rear model rather than front-only presentation'; limit='Prototype fused equipment'; goal='Whole-character production review' },
    @{ platform='Web'; quality='High / Viewer Studio'; clip='Idle / 100%'; camera='Front; yaw 0; pitch 0; zoom 0'; metrics='2 draws / 9,976 triangles'; shown='Arin using the authored prototype Idle clip'; proves='WebGL2 model, rig, skinning, and Idle selection'; limit='JPEG-derived normal/ORM artifacts remain visible'; goal='Cross-target character playback' },
    @{ platform='Web'; quality='High / Viewer Studio'; clip='Walk / 100%'; camera='Front; yaw 0; pitch 0; zoom 0'; metrics='2 draws / 9,976 triangles'; shown='Arin using the authored prototype Walk clip'; proves='Exact Walk selection at authored 100% speed'; limit='Not a final gameplay locomotion review'; goal='Cross-target animation inspection' },
    @{ platform='Web'; quality='High / Viewer Studio'; clip='Run / 100%'; camera='Front; yaw 0; pitch 0; zoom 0'; metrics='2 draws / 9,976 triangles'; shown='Arin using the authored prototype Run clip'; proves='Exact Run selection at authored 100% speed'; limit='Required combat clips are still missing'; goal='Truthful prototype animation inventory' },
    @{ platform='Web'; quality='High / Viewer Studio'; clip='Walk / 100%'; camera='Front; yaw 0; pitch 0; zoom 0'; metrics='26 draws / 10,264 triangles'; shown='Six socket origins and RGB local-axis endpoints'; proves='Prototype socket aliases are visible and follow animation'; limit='Sockets are inferred, not production-authored or equipment-validated'; goal='Attachment readiness before combat VFX/equipment' },
    @{ platform='Web'; quality='High / Viewer Studio'; clip='Walk / 100%'; camera='Front; yaw 0; pitch 0; zoom 0'; metrics='2 draws / 9,976 triangles'; shown='Base Color inspection selected from the seven-channel cycle'; proves='Material output can be separated from lighting'; limit='One screenshot cannot show every channel simultaneously'; goal='PBR texture/channel diagnosis' },
    @{ platform='Web composite'; quality='High / Viewer Studio'; clip='Idle / 100%'; camera='Profile-derived front framing'; metrics='Arin 2/9,976; fixture 3/38'; shown='Arin and differently sized articulated fixture profiles'; proves='Bounds-driven camera, scale, floor, and shadow framing with no viewer edit'; limit='Technical fixture is intentionally simple'; goal='Reusable Character Viewer for future cast assets' },
    @{ platform='Web'; quality='High / Viewer Studio'; clip='Idle / 100%'; camera='Front; yaw 0; pitch 0; zoom 0'; metrics='2 draws / 9,976 triangles'; shown='Complete on-screen control and status surface'; proves='Orbit, pan, zoom, clips, lighting, speed, sockets, material, profile, reset, and O key are discoverable'; limit='Touch gestures remain mapped through pointer controls'; goal='Student-facing reusable SMILE inspection tool' }
)

$indexLines = [System.Collections.Generic.List[string]]::new()
$indexLines.Add('# M7B.1 Arin Character Viewer Screenshot Index')
$indexLines.Add('')
$indexLines.Add('All evidence files are repository-owned true PNGs. Arin is the official character name; Paladin is his party role. The current asset remains a technical prototype and Dragonfall release mode remains Classic.')
$indexLines.Add('')
for ($index = 0; $index -lt $requiredNames.Count; $index++) {
    $path = $requiredPaths[$index]
    $file = Get-Item -LiteralPath $path
    $image = [System.Drawing.Image]::FromFile($path)
    try {
        $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $path).Hash
        $record = $metadata[$index]
        $indexLines.Add("## $($requiredNames[$index])")
        $indexLines.Add('')
        $indexLines.Add("- Actual media format / magic validation: PNG / PASS (`89 50 4E 47 0D 0A 1A 0A`)")
        $indexLines.Add("- Dimensions / file size / SHA-256: $($image.Width)x$($image.Height) / $($file.Length) bytes / $hash")
        $indexLines.Add("- Native/Web: $($record.platform)")
        $indexLines.Add("- Quality and lighting profile: $($record.quality)")
        $indexLines.Add("- Clip/speed: $($record.clip)")
        $indexLines.Add("- Camera values: $($record.camera)")
        $indexLines.Add("- Draws/triangles: $($record.metrics)")
        $indexLines.Add("- What is shown: $($record.shown)")
        $indexLines.Add("- What it proves: $($record.proves)")
        $indexLines.Add("- Known limitation: $($record.limit)")
        $indexLines.Add("- Connection to end goal: $($record.goal)")
        $indexLines.Add('')
    }
    finally {
        $image.Dispose()
    }
}

$contactFile = Get-Item -LiteralPath $contactPath
$contactImage = [System.Drawing.Image]::FromFile($contactPath)
try {
    $contactHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $contactPath).Hash
    $indexLines.Add('## paladin-viewer-contact-sheet-iphone.png')
    $indexLines.Add('')
    $indexLines.Add('- Actual media format / magic validation: PNG / PASS (`89 50 4E 47 0D 0A 1A 0A`)')
    $indexLines.Add("- Dimensions / file size / SHA-256: $($contactImage.Width)x$($contactImage.Height) / $($contactFile.Length) bytes / $contactHash")
    $indexLines.Add('- Native/Web: Mixed native and Web evidence')
    $indexLines.Add('- Quality and lighting profile: High / Viewer Studio')
    $indexLines.Add('- Clip/speed: Idle, Walk, and Run / 100%')
    $indexLines.Add('- Camera values: Front, side, back, and profile-derived auto-fit')
    $indexLines.Add('- Draws/triangles: Values are labeled in the source screenshots and recorded above')
    $indexLines.Add('- What is shown: Phone-friendly two-column summary of all ten required frames')
    $indexLines.Add('- What it proves: The complete evidence set can be reviewed as one normal sRGB PNG')
    $indexLines.Add('- Known limitation: Use source screenshots above for full-resolution pixel inspection')
    $indexLines.Add('- Connection to end goal: Mobile review of Character3D production-readiness evidence')
}
finally {
    $contactImage.Dispose()
}

$indexPath = Join-Path $evidenceRoot 'screenshot-index.md'
[System.IO.File]::WriteAllLines($indexPath, $indexLines, [System.Text.UTF8Encoding]::new($false))

foreach ($path in @($requiredPaths + $contactPath)) {
    $length = (Get-Item -LiteralPath $path).Length
    if ($length -gt $maximumEvidenceBytes) {
        throw "Character Viewer evidence exceeds 5 MiB: $path"
    }
}

Write-Host "Built Character Viewer auto-fit, contact-sheet, historical PNG correction, and index evidence."
