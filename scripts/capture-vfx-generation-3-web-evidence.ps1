[CmdletBinding()]
param(
    [string]$WebDirectory = 'artifacts\web\AetherBladeVfxLab',
    [string]$OutputDirectory = 'docs\implementation\screenshots\m7e-0-vfx3-preflight'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$webPath = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $WebDirectory))
$outputPath = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $OutputDirectory))
$temporaryRoot = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot 'artifacts\temp'))
$chrome = 'C:\Program Files\Google\Chrome\Application\chrome.exe'

if (-not (Test-Path -LiteralPath (Join-Path $webPath 'index.html') -PathType Leaf)) {
    throw "VFX Web lab is missing: $webPath"
}
if (-not (Test-Path -LiteralPath $chrome -PathType Leaf)) {
    throw "Chrome is missing: $chrome"
}

[System.IO.Directory]::CreateDirectory($outputPath) | Out-Null
[System.IO.Directory]::CreateDirectory($temporaryRoot) | Out-Null
$captureRoot = Join-Path $temporaryRoot ('m7e-0-web-evidence-' + [Guid]::NewGuid().ToString('N'))
[System.IO.Directory]::CreateDirectory($captureRoot) | Out-Null
$portrait = Join-Path $captureRoot 'iphone-portrait.png'
$landscape = Join-Path $captureRoot 'iphone-landscape.png'

$listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
$listener.Start()
$port = ([System.Net.IPEndPoint]$listener.LocalEndpoint).Port
$listener.Stop()
$url = "http://127.0.0.1:$port/index.html"
$server = Start-Process -FilePath 'py' `
    -ArgumentList '-m', 'http.server', $port, '--bind', '127.0.0.1' `
    -WorkingDirectory $webPath `
    -WindowStyle Hidden `
    -PassThru

function Capture-Chrome(
    [string]$Path,
    [int]$Width,
    [int]$Height,
    [int]$VirtualTimeMilliseconds
) {
    $profile = Join-Path $captureRoot ([Guid]::NewGuid().ToString('N'))
    $arguments = @(
        '--headless=new',
        '--hide-scrollbars',
        '--no-first-run',
        '--no-default-browser-check',
        '--disable-extensions',
        '--disable-background-networking',
        '--enable-webgl',
        '--ignore-gpu-blocklist',
        '--use-angle=swiftshader',
        '--run-all-compositor-stages-before-draw',
        "--window-size=$Width,$Height",
        '--force-device-scale-factor=1',
        "--virtual-time-budget=$VirtualTimeMilliseconds",
        "--user-data-dir=$profile",
        "--screenshot=$Path",
        $url
    )

    & $chrome $arguments | Out-Null
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Chrome failed to capture VFX evidence: $Path"
    }
}

try {
    $deadline = [DateTime]::UtcNow.AddSeconds(10)
    do {
        try {
            $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 1
        }
        catch {
            $response = $null
        }
        if ($response -and $response.StatusCode -eq 200) { break }
        Start-Sleep -Milliseconds 100
    } while ([DateTime]::UtcNow -lt $deadline)

    if (-not $response -or $response.StatusCode -ne 200) {
        throw 'The temporary VFX evidence server did not become ready.'
    }

    Capture-Chrome (Join-Path $outputPath '03-energy-blade-idle-web.png') 960 540 2500
    Capture-Chrome (Join-Path $outputPath '04-energy-blade-swing-web.png') 960 540 5000
    Capture-Chrome $portrait 390 844 2500
    Capture-Chrome $landscape 844 390 5000

    Add-Type -AssemblyName System.Drawing.Common
    $portraitImage = [System.Drawing.Image]::FromFile($portrait)
    $landscapeImage = [System.Drawing.Image]::FromFile($landscape)
    try {
        $contactSheet = [System.Drawing.Bitmap]::new(900, 900)
        try {
            $graphics = [System.Drawing.Graphics]::FromImage($contactSheet)
            try {
                $graphics.Clear([System.Drawing.Color]::FromArgb(7, 12, 24))
                $graphics.DrawImage($portraitImage, 20, 46, 390, 844)
                $graphics.DrawImage($landscapeImage, 430, 255, 450, 208)
                $font = [System.Drawing.Font]::new('Segoe UI', 15, [System.Drawing.FontStyle]::Bold)
                try {
                    $brush = [System.Drawing.Brushes]::White
                    $graphics.DrawString('iPhone Portrait - Idle', $font, $brush, 20, 12)
                    $graphics.DrawString('iPhone Landscape - Swing', $font, $brush, 430, 220)
                }
                finally {
                    $font.Dispose()
                }
            }
            finally {
                $graphics.Dispose()
            }

            $contactSheet.Save(
                (Join-Path $outputPath '07-iphone-contact-sheet.png'),
                [System.Drawing.Imaging.ImageFormat]::Png
            )
        }
        finally {
            $contactSheet.Dispose()
        }
    }
    finally {
        $portraitImage.Dispose()
        $landscapeImage.Dispose()
    }

    Write-Host "Captured Web and iPhone M7E-0 VFX evidence in $outputPath"
}
finally {
    if (-not $server.HasExited) {
        $server.Kill()
        $server.WaitForExit()
    }
    $server.Dispose()

    $resolvedCapture = [System.IO.Path]::GetFullPath($captureRoot)
    $temporaryPrefix = $temporaryRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar) +
        [System.IO.Path]::DirectorySeparatorChar
    if ($resolvedCapture.StartsWith($temporaryPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        Remove-Item -LiteralPath $resolvedCapture -Recurse -Force
    }
}
