[CmdletBinding()]
param(
    [switch]$RequireTracked
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$evidenceRoot = Join-Path $repositoryRoot `
    'docs\implementation\screenshots\m7b-1-paladin-viewer'
$indexPath = Join-Path $evidenceRoot 'screenshot-index.md'
$historicalPath = Join-Path $repositoryRoot `
    'docs\implementation\screenshots\m7b-arin-prototype\character-3d-viewer-web.png'
$maximumDimension = 4096
$maximumPixels = 16777216
$maximumBytes = 5MB
$pngSignature = [byte[]](0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A)

Add-Type -AssemblyName System.Drawing.Common

function Read-BigEndianUInt32([byte[]]$Bytes, [int]$Offset) {
    if ($Offset -lt 0 -or $Offset -gt $Bytes.Length - 4) {
        throw 'Media metadata ended before a bounded 32-bit value.'
    }

    $value = ([uint64]$Bytes[$Offset] -shl 24) -bor
        ([uint64]$Bytes[$Offset + 1] -shl 16) -bor
        ([uint64]$Bytes[$Offset + 2] -shl 8) -bor
        [uint64]$Bytes[$Offset + 3]

    return [uint32]$value
}

function Test-CommonMediaSafety([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Screenshot evidence is missing: $Path"
    }

    $file = Get-Item -LiteralPath $Path -Force
    if (($file.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "Screenshot evidence must not be a symlink or reparse point: $Path"
    }
    if ($file.Length -le 0 -or $file.Length -gt $maximumBytes) {
        throw "Screenshot evidence file size is outside the bounded profile: $Path"
    }

    $prefixLength = [int][Math]::Min(128, $file.Length)
    $prefix = [byte[]]::new($prefixLength)
    $stream = [System.IO.File]::OpenRead($Path)
    try {
        [void]$stream.Read($prefix, 0, $prefix.Length)
    }
    finally {
        $stream.Dispose()
    }

    $prefixText = [System.Text.Encoding]::ASCII.GetString($prefix)
    if ($prefixText.StartsWith('version https://git-lfs.github.com/spec/v1')) {
        throw "Screenshot evidence must not be a Git LFS pointer: $Path"
    }
}

function Test-Png([string]$Path) {
    Test-CommonMediaSafety $Path
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    if ($bytes.Length -lt 33) { throw "PNG evidence is truncated: $Path" }

    for ($index = 0; $index -lt $pngSignature.Length; $index++) {
        if ($bytes[$index] -ne $pngSignature[$index]) {
            throw "PNG evidence has the wrong magic signature: $Path"
        }
    }

    $ihdrLength = Read-BigEndianUInt32 $bytes 8
    $ihdrType = [System.Text.Encoding]::ASCII.GetString($bytes, 12, 4)
    if ($ihdrLength -ne 13 -or $ihdrType -cne 'IHDR') {
        throw "PNG evidence does not begin with a canonical IHDR chunk: $Path"
    }

    $width = Read-BigEndianUInt32 $bytes 16
    $height = Read-BigEndianUInt32 $bytes 20
    $colorType = $bytes[25]
    if ($width -eq 0 -or $height -eq 0 -or
        $width -gt $maximumDimension -or $height -gt $maximumDimension -or
        ([uint64]$width * [uint64]$height) -gt $maximumPixels) {
        throw "PNG evidence dimensions are outside the bounded profile: $Path"
    }
    if ($colorType -ne 2 -and $colorType -ne 6) {
        throw "PNG evidence must use RGB or RGBA-compatible color: $Path"
    }

    $offset = 8
    $foundEnd = $false
    while ($offset -lt $bytes.Length) {
        if ($offset -gt $bytes.Length - 12) {
            throw "PNG evidence ends inside a chunk header: $Path"
        }

        $length = [uint64](Read-BigEndianUInt32 $bytes $offset)
        $type = [System.Text.Encoding]::ASCII.GetString($bytes, $offset + 4, 4)
        $payload = [uint64]$offset + 8
        if ($length -gt [uint64]$bytes.Length - $payload - 4) {
            throw "PNG evidence chunk exceeds the file boundary: $Path"
        }

        $offset = [int]($payload + $length + 4)
        if ($type -ceq 'IEND') {
            if ($length -ne 0 -or $offset -ne $bytes.Length) {
                throw "PNG evidence has a malformed or non-terminal IEND chunk: $Path"
            }

            $foundEnd = $true
            break
        }
    }
    if (-not $foundEnd) { throw "PNG evidence has no terminal IEND chunk: $Path" }

    $image = [System.Drawing.Image]::FromFile($Path)
    try {
        if ($image.RawFormat.Guid -ne [System.Drawing.Imaging.ImageFormat]::Png.Guid -or
            $image.Width -ne $width -or $image.Height -ne $height) {
            throw "PNG evidence decoded format or dimensions disagree with IHDR: $Path"
        }
    }
    finally {
        $image.Dispose()
    }

    return [pscustomobject]@{
        Width = $width
        Height = $height
        Bytes = $bytes.Length
        Hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash
    }
}

function Test-Jpeg([string]$Path) {
    Test-CommonMediaSafety $Path
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    if ($bytes.Length -lt 4 -or $bytes[0] -ne 0xFF -or $bytes[1] -ne 0xD8 -or
        $bytes[$bytes.Length - 2] -ne 0xFF -or $bytes[$bytes.Length - 1] -ne 0xD9) {
        throw "JPEG evidence has the wrong or incomplete magic signature: $Path"
    }

    $image = [System.Drawing.Image]::FromFile($Path)
    try {
        if ($image.RawFormat.Guid -ne [System.Drawing.Imaging.ImageFormat]::Jpeg.Guid -or
            $image.Width -le 0 -or $image.Height -le 0 -or
            $image.Width -gt $maximumDimension -or $image.Height -gt $maximumDimension -or
            ([uint64]$image.Width * [uint64]$image.Height) -gt $maximumPixels) {
            throw "JPEG evidence decoded format or dimensions are invalid: $Path"
        }
    }
    finally {
        $image.Dispose()
    }
}

function Test-Media([string]$Path) {
    $extension = [System.IO.Path]::GetExtension($Path).ToLowerInvariant()
    if ($extension -ceq '.png') {
        return Test-Png $Path
    }
    if ($extension -ceq '.jpg' -or $extension -ceq '.jpeg') {
        Test-Jpeg $Path
        return $null
    }

    throw "Screenshot evidence uses an unsupported extension: $Path"
}

function Assert-Rejected([scriptblock]$Action, [string]$Label) {
    try {
        & $Action
    }
    catch {
        Write-Host "PASS: rejected $Label"
        return
    }

    throw "Screenshot validator accepted invalid fixture: $Label"
}

$fixtureRoot = Join-Path $repositoryRoot `
    ('artifacts\temp\screenshot-validator-' + [Guid]::NewGuid().ToString('N'))
[System.IO.Directory]::CreateDirectory($fixtureRoot) | Out-Null
try {
    $validPath = Join-Path $fixtureRoot 'valid.png'
    $valid = [System.Drawing.Bitmap]::new(16, 16)
    try {
        $valid.Save($validPath, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $valid.Dispose()
    }

    [void](Test-Media $validPath)
    Write-Host 'PASS: accepted valid PNG fixture'

    $jpegAsPng = Join-Path $fixtureRoot 'jpeg-as-png.png'
    $jpeg = [System.Drawing.Bitmap]::new(16, 16)
    try {
        $jpeg.Save($jpegAsPng, [System.Drawing.Imaging.ImageFormat]::Jpeg)
    }
    finally {
        $jpeg.Dispose()
    }
    Assert-Rejected { Test-Media $jpegAsPng } 'misnamed JPEG-as-PNG fixture'

    $validBytes = [System.IO.File]::ReadAllBytes($validPath)
    $truncatedPath = Join-Path $fixtureRoot 'truncated.png'
    [System.IO.File]::WriteAllBytes($truncatedPath, $validBytes[0..19])
    Assert-Rejected { Test-Media $truncatedPath } 'truncated PNG fixture'

    $hugePath = Join-Path $fixtureRoot 'huge.png'
    $hugeBytes = [byte[]]$validBytes.Clone()
    $hugeBytes[16] = 0
    $hugeBytes[17] = 0
    $hugeBytes[18] = 0x20
    $hugeBytes[19] = 0
    [System.IO.File]::WriteAllBytes($hugePath, $hugeBytes)
    Assert-Rejected { Test-Media $hugePath } 'oversized-dimension PNG fixture'

    $wrongExtension = Join-Path $fixtureRoot 'wrong-extension.jpg'
    [System.IO.File]::WriteAllBytes($wrongExtension, $validBytes)
    Assert-Rejected { Test-Media $wrongExtension } 'wrong-extension fixture'
}
finally {
    if (Test-Path -LiteralPath $fixtureRoot) {
        Remove-Item -LiteralPath $fixtureRoot -Recurse -Force
    }
}

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
    '10-paladin-viewer-controls.png',
    'paladin-viewer-contact-sheet-iphone.png'
)
$indexText = Get-Content -LiteralPath $indexPath -Raw
foreach ($name in $requiredNames) {
    $path = Join-Path $evidenceRoot $name
    $record = Test-Media $path
    if ($null -eq $record) { throw "Expected PNG metadata for evidence: $name" }
    if ($indexText.IndexOf($name, [System.StringComparison]::Ordinal) -lt 0 -or
        $indexText.IndexOf($record.Hash, [System.StringComparison]::Ordinal) -lt 0) {
        throw "Screenshot index is missing the current filename or SHA-256: $name"
    }

    if ($RequireTracked) {
        $relativePath = [System.IO.Path]::GetRelativePath($repositoryRoot, $path).Replace('\', '/')
        & git ls-files --error-unmatch -- $relativePath 2>$null | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "Screenshot evidence is not tracked: $relativePath" }
    }
}

$contact = Test-Png (Join-Path $evidenceRoot 'paladin-viewer-contact-sheet-iphone.png')
if ($contact.Width -ne 1170 -or $contact.Bytes -gt $maximumBytes) {
    throw 'The phone contact sheet is not 1170 pixels wide or exceeds 5 MiB.'
}
[void](Test-Png $historicalPath)

Write-Host ('Screenshot evidence gate passed: 10 required source PNGs, phone contact sheet, ' +
    'corrected historical PNG, index hashes, bounded decode, magic/extension agreement, ' +
    'and malformed-fixture rejection.')
