[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [switch]$SkipExporter,
    [switch]$SkipEvidence
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$compiler = Join-Path $repositoryRoot 'artifacts\compiler\smilec.exe'
$assetTool = Join-Path $repositoryRoot 'artifacts\assettool\smileasset.exe'
$cameraProject = Join-Path $repositoryRoot `
    'examples\Renderer3DCameraHardeningTests\Renderer3DCameraHardeningTests.smileproj'
$cameraExpected = Join-Path $repositoryRoot `
    'examples\Renderer3DCameraHardeningTests\expected.txt'
$cameraNative = Join-Path $repositoryRoot 'artifacts\tests\Renderer3DCameraHardeningTests.exe'
$cameraWeb = Join-Path $repositoryRoot 'artifacts\web\Renderer3DCameraHardeningTests'
$viewerProject = Join-Path $repositoryRoot 'games\Dragonfall\Character3DViewerCooked.smileproj'
$precookedProject = Join-Path $repositoryRoot 'games\Dragonfall\Character3DViewer.smileproj'
$viewerSource = Join-Path $repositoryRoot 'tools\Character3DViewer\Program.smile'
$profileSource = Join-Path $repositoryRoot 'tools\Character3DViewer\Profiles.smile'
$nativeRenderer = Join-Path $repositoryRoot `
    'src\Smile.NativeRuntime\graphics\graphics3d_directx.cpp'
$webRenderer = Join-Path $repositoryRoot 'src\Smile.Compiler\WebOutputWriter.cs'
$nativeWindow = Join-Path $repositoryRoot 'src\Smile.NativeRuntime\runtime.c'
$exportManifest = Join-Path $repositoryRoot 'scripts\export-arin-v5-4-viewer.manifest.json'
$exporter = Join-Path $repositoryRoot 'scripts\export-arin-v5-4-viewer.ps1'
$sourceBlend = Join-Path $repositoryRoot `
    'games\SinStarI\SourceAssets\Characters\Paladin\arin-integrated-candidate-v5.4.blend'
$descriptor = Join-Path $repositoryRoot `
    'games\Dragonfall\SourceAssets\Arin\ArinV54.sm3d.json'
$evidenceRoot = Join-Path $repositoryRoot `
    'docs\implementation\screenshots\m7c-b1-paladin-v5-4-hardening'

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function Assert-Contains([string]$Text, [string]$Expected, [string]$Label) {
    if ($Text.IndexOf($Expected, [StringComparison]::Ordinal) -lt 0) {
        throw "$Label is missing required text: $Expected"
    }
}

function Invoke-Compiler([string[]]$Arguments, [string]$Failure) {
    & $compiler @Arguments
    if ($LASTEXITCODE -ne 0) { throw $Failure }
}

function Test-Png([string]$Path) {
    Assert-True (Test-Path -LiteralPath $Path -PathType Leaf) "Missing screenshot: $Path"
    $file = Get-Item -LiteralPath $Path
    Assert-True (-not ($file.Attributes -band [IO.FileAttributes]::ReparsePoint)) `
        "Screenshot must not be a symlink: $Path"
    Assert-True ($file.Length -ge 1024 -and $file.Length -le 5MB) `
        "Screenshot size is outside the accepted bounds: $Path"
    $bytes = [IO.File]::ReadAllBytes($Path)
    $signature = [byte[]](0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A)
    for ($index = 0; $index -lt $signature.Length; $index++) {
        Assert-True ($bytes[$index] -eq $signature[$index]) "Screenshot is not true PNG: $Path"
    }
    $image = [Drawing.Image]::FromFile($Path)
    try {
        Assert-True ($image.Width -ge 320 -and $image.Width -le 4096) `
            "Screenshot width is invalid: $Path"
        Assert-True ($image.Height -ge 240 -and $image.Height -le 4096) `
            "Screenshot height is invalid: $Path"
    }
    finally {
        $image.Dispose()
    }
}

if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) {
    throw 'Build SMILE before running the Paladin v5.4 hardening gate.'
}

Push-Location $repositoryRoot
try {
    $nativeText = Get-Content -LiteralPath $nativeRenderer -Raw
    $webText = Get-Content -LiteralPath $webRenderer -Raw
    $windowText = Get-Content -LiteralPath $nativeWindow -Raw
    $viewerText = Get-Content -LiteralPath $viewerSource -Raw
    $profileText = Get-Content -LiteralPath $profileSource -Raw
    $manifest = Get-Content -LiteralPath $exportManifest -Raw | ConvertFrom-Json

    Assert-Contains $nativeText 'SMILE_3D_SET_CAMERA_UP' 'Native command ABI'
    Assert-Contains $nativeText 'SMILE_3D_CAMERA_ERROR_FRAME_ACTIVE 64' 'Native camera state contract'
    Assert-Contains $nativeText 'smile_pending_camera_has_projection3d' 'Native pending camera state'
    Assert-Contains $webText 'renderer3DPendingCamera' 'Web pending camera state'
    Assert-Contains $webText 'renderer3DCameraErrorFrameActive = 64' 'Web camera state contract'
    Assert-Contains $webText 'responsiveWindowEnabled = true' 'Web responsive-window mode'
    Assert-Contains $webText 'gl.UNPACK_FLIP_Y_WEBGL,false' `
        'Web cooked-texture orientation parity'
    Assert-Contains $windowText '__smile_internal_window_placement_v2' 'Window placement schema'
    Assert-Contains $windowText 'smile_data_put_u32(record + 4, 2)' 'Window placement version'
    Assert-Contains $windowText 'WM_DPICHANGED' 'Native DPI transition'
    Assert-Contains $windowText 'smile_window_placement_checksum' 'Window placement checksum'
    Assert-Contains $viewerText 'SocketGizmoCount = 4' 'Bounded socket gizmos'
    Assert-Contains $viewerText 'CreateMesh3D(GridLineCount * 4' 'Single grid mesh'
    Assert-Contains $viewerText 'Call DestroySocketGizmos()' 'Socket allocation rollback'
    Assert-Contains $viewerText 'AdvanceClock(' 'Elapsed-time clock'
    Assert-Contains $viewerText 'MINIMUM_VIEWER_WIDTH = 800' 'Responsive minimum width'
    Assert-Contains $profileText 'sin-star-i.character-1.paladin' 'Stable Paladin identity'
    Assert-Contains $profileText 'Result.CandidateVersion = "v5.5"' 'Current Viewer candidate version'
    Assert-True ($manifest.version -eq 1) 'Exporter manifest version changed.'
    Assert-True ($manifest.assetId -ceq 'sin-star-i.character-1.paladin') `
        'Exporter stable asset identity changed.'
    Assert-True ($manifest.candidateVersion -ceq 'v5.4') 'Exporter candidate version changed.'
    Assert-True ($manifest.actions.Count -eq 11) 'Exporter action allowlist must contain eleven clips.'
    Assert-True ($manifest.referenceAction -ceq 'Idle' -and $manifest.referenceFrame -eq 1) `
        'Exporter reference action/frame changed.'
    Assert-True ($manifest.sampleRate -eq 30) 'Exporter output sample rate changed.'

    Invoke-Compiler @('--project', $cameraProject, '--target', 'windows-x64',
        '--configuration', $Configuration, '--graphics', 'DirectX', '-o', $cameraNative) `
        'Renderer3D camera native compilation failed.'
    & 'scripts\run-bounded-test.cmd' 60 $cameraNative
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D camera native execution failed.' }
    Invoke-Compiler @('--project', $cameraProject, '--target', 'web',
        '--configuration', $Configuration, '--output-dir', $cameraWeb) `
        'Renderer3D camera Web compilation failed.'
    & node 'scripts\run-web-test.js' $cameraWeb --expected $cameraExpected --timeout 60000
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D camera Web assertions failed.' }

    & 'scripts\test-character-3d-viewer-hardening.ps1' -Configuration $Configuration

    Invoke-Compiler @('--project', $viewerProject, '--target', 'windows-x64',
        '--configuration', $Configuration, '--graphics', 'DirectX', '-o',
        'artifacts\games\Character3DViewer.exe') 'Cooked Viewer native compilation failed.'
    Invoke-Compiler @('--project', $viewerProject, '--target', 'web',
        '--configuration', $Configuration, '--output-dir',
        'artifacts\web\Character3DViewer') 'Cooked Viewer Web compilation failed.'
    Invoke-Compiler @('--project', $precookedProject, '--target', 'windows-x64',
        '--configuration', $Configuration, '--graphics', 'DirectX', '-o',
        'artifacts\games\Character3DViewerPrecooked.exe') `
        'Pre-cooked Viewer native compilation failed.'
    Invoke-Compiler @('--project', $precookedProject, '--target', 'web',
        '--configuration', $Configuration, '--output-dir',
        'artifacts\web\Character3DViewerPrecooked') 'Pre-cooked Viewer Web compilation failed.'

    if (-not $SkipExporter) {
        $sourceHashBefore = (Get-FileHash -LiteralPath $sourceBlend -Algorithm SHA256).Hash
        $temporaryRoot = Join-Path $repositoryRoot `
            ('artifacts\temp\paladin-v5-4-hardening-' + [Guid]::NewGuid().ToString('N'))
        $temporaryPrefix = [IO.Path]::GetFullPath((Join-Path $repositoryRoot 'artifacts\temp')) + `
            [IO.Path]::DirectorySeparatorChar
        $resolvedTemporary = [IO.Path]::GetFullPath($temporaryRoot)
        Assert-True ($resolvedTemporary.StartsWith($temporaryPrefix,
            [StringComparison]::OrdinalIgnoreCase)) 'Exporter test path escaped artifacts/temp.'
        $firstRoot = Join-Path $temporaryRoot 'first'
        $secondRoot = Join-Path $temporaryRoot 'second'
        New-Item -ItemType Directory -Force -Path $firstRoot, $secondRoot | Out-Null
        try {
            $fileName = 'arin-integrated-candidate-v5.4.glb'
            $firstGlb = Join-Path $firstRoot $fileName
            $secondGlb = Join-Path $secondRoot $fileName
            & $exporter -Publish -OutputGlb $firstGlb
            & $exporter -Publish -OutputGlb $secondGlb
            Assert-True ((Get-FileHash $firstGlb -Algorithm SHA256).Hash -ceq
                (Get-FileHash $secondGlb -Algorithm SHA256).Hash) `
                'Two clean Blender exports were not byte-identical.'
            $firstTextures = @(Get-ChildItem $firstRoot -Filter '*.texture-*' | Sort-Object Name)
            $secondTextures = @(Get-ChildItem $secondRoot -Filter '*.texture-*' | Sort-Object Name)
            Assert-True ($firstTextures.Count -eq 9 -and $secondTextures.Count -eq 9) `
                'The deterministic export must externalize exactly nine source textures.'
            for ($index = 0; $index -lt 9; $index++) {
                Assert-True ($firstTextures[$index].Name -ceq $secondTextures[$index].Name) `
                    'Exported texture names differ between runs.'
                Assert-True ((Get-FileHash $firstTextures[$index].FullName -Algorithm SHA256).Hash -ceq
                    (Get-FileHash $secondTextures[$index].FullName -Algorithm SHA256).Hash) `
                    "Exported texture differs between runs: $($firstTextures[$index].Name)"
            }
            $firstSm3d = Join-Path $firstRoot 'ArinV54.sm3d'
            $secondSm3d = Join-Path $secondRoot 'ArinV54.sm3d'
            & $assetTool model $firstGlb --format-version 2 --descriptor $descriptor -o $firstSm3d
            if ($LASTEXITCODE -ne 0) { throw 'First deterministic cook failed.' }
            & $assetTool model $secondGlb --format-version 2 --descriptor $descriptor -o $secondSm3d
            if ($LASTEXITCODE -ne 0) { throw 'Second deterministic cook failed.' }
            Assert-True ((Get-FileHash $firstSm3d -Algorithm SHA256).Hash -ceq
                (Get-FileHash $secondSm3d -Algorithm SHA256).Hash) `
                'Two clean SM3D cooks were not byte-identical.'
        }
        finally {
            if (Test-Path -LiteralPath $temporaryRoot) {
                Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
            }
        }
        $sourceHashAfter = (Get-FileHash -LiteralPath $sourceBlend -Algorithm SHA256).Hash
        Assert-True ($sourceHashAfter -ceq $sourceHashBefore) `
            'The canonical Blender source changed during exporter validation.'
    }

    if (-not $SkipEvidence) {
        Add-Type -AssemblyName System.Drawing.Common
        $requiredScreenshots = @(
            '01-native-idle-front.png',
            '02-native-sword-attack.png',
            '03-native-shield-bash-candidate.png',
            '04-native-ko-grounding.png',
            '05-native-socket-gizmos.png',
            '06-native-material-channels.png',
            '07-web-idle-front.png',
            '08-web-sword-attack.png',
            '09-web-360-orbit.png',
            '10-responsive-layouts.png',
            '11-grid-gizmo-resource-counts.png',
            '12-iphone-contact-sheet.png'
        )
        foreach ($name in $requiredScreenshots) {
            Test-Png (Join-Path $evidenceRoot $name)
        }
        Assert-True (Test-Path -LiteralPath (Join-Path $evidenceRoot 'screenshot-index.md')) `
            'M7C-B.1 screenshot index is missing.'
    }

    Write-Host ('Paladin v5.4 camera, timing, responsive-window, resource, profile, ' +
        'export determinism, cooked/pre-cooked parity, and evidence hardening gate passed.')
}
finally {
    Pop-Location
}
