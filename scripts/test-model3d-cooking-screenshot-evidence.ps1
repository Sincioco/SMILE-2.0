[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$evidenceRoot = Join-Path $repositoryRoot `
    'docs\implementation\screenshots\m7c-model3d-cooking'
$indexPath = Join-Path $evidenceRoot 'screenshot-index.md'
$maximumBytes = 5MB
$pngSignature = [byte[]](0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A)
$requiredNames = @(
    '01-visual-studio-model3d-item.png',
    '02-cold-build-cook-output.png',
    '03-cache-hit-build-output.png',
    '04-native-viewer-auto-fit.png',
    '05-web-viewer-auto-fit.png',
    '06-viewer-clip-browser.png',
    '07-viewer-socket-material-debug.png',
    '08-iphone-contact-sheet.png'
)

Add-Type -AssemblyName System.Drawing

function Get-Sha256([string]$Path) {
    $algorithm = [System.Security.Cryptography.SHA256]::Create()
    $stream = [System.IO.File]::OpenRead($Path)
    try {
        return [BitConverter]::ToString($algorithm.ComputeHash($stream)).Replace('-', '')
    }
    finally {
        $stream.Dispose()
        $algorithm.Dispose()
    }
}

if (-not (Test-Path -LiteralPath $indexPath -PathType Leaf)) {
    throw "Model3D cooking screenshot index is missing: $indexPath"
}
$indexText = Get-Content -LiteralPath $indexPath -Raw

foreach ($name in $requiredNames) {
    $path = Join-Path $evidenceRoot $name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Model3D cooking screenshot is missing: $name"
    }

    $file = Get-Item -LiteralPath $path -Force
    if (($file.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0 -or
        $file.Length -le 0 -or
        $file.Length -gt $maximumBytes) {
        throw "Model3D cooking screenshot is not a bounded regular file: $name"
    }

    $bytes = [System.IO.File]::ReadAllBytes($path)

    for ($index = 0; $index -lt $pngSignature.Length; $index++) {
        if ($bytes[$index] -ne $pngSignature[$index]) {
            throw "Model3D cooking screenshot is not true PNG data: $name"
        }
    }

    $image = [System.Drawing.Image]::FromFile($path)
    try {
        if ($image.RawFormat.Guid -ne [System.Drawing.Imaging.ImageFormat]::Png.Guid -or
            $image.Width -le 0 -or
            $image.Height -le 0 -or
            $image.Width -gt 4096 -or
            $image.Height -gt 4096 -or
            ([uint64]$image.Width * [uint64]$image.Height) -gt 16777216) {
            throw "Model3D cooking screenshot dimensions or decoded format are invalid: $name"
        }
        if ($name -ceq '08-iphone-contact-sheet.png' -and $image.Width -ne 1170) {
            throw 'The M7C-A phone contact sheet must be 1170 pixels wide.'
        }
    }
    finally {
        $image.Dispose()
    }

    $hash = Get-Sha256 $path
    if ($indexText.IndexOf($name, [System.StringComparison]::Ordinal) -lt 0 -or
        $indexText.IndexOf($hash, [System.StringComparison]::Ordinal) -lt 0) {
        throw "Model3D cooking screenshot index is missing the current name or hash: $name"
    }
}

Write-Host ('Model3D cooking screenshot evidence gate passed: 8 true PNG files, ' +
    'bounded decode, indexed SHA-256 values, and 1170-pixel phone contact sheet.')
