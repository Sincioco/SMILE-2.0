[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$RepositoryRoot,
    [string]$OutputDirectory,
    [string]$SourcePath
)

$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ($RepositoryRoot) { $root = [IO.Path]::GetFullPath($RepositoryRoot) }
$compiler = Join-Path $root 'artifacts/compiler/smilec.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'Build SMILE before the native PBR sharing check.' }
$run = 'renderer3d-pbr-sharing-' + [Guid]::NewGuid().ToString('N')
$output = Join-Path $root ('artifacts/tests/' + $run)
if ($OutputDirectory) { $output = Join-Path ([IO.Path]::GetFullPath($OutputDirectory)) $run }
$sourceRoot = Join-Path $output 'source'
$binaryRoot = Join-Path $output 'bin'
$assetRoot = Join-Path $sourceRoot 'Assets'
$textureRoot = Join-Path $assetRoot 'Textures'
$null = New-Item -ItemType Directory -Path $textureRoot, $binaryRoot

# Reuse the checked-in two-part fixture's exact geometry. Strip texture references
# and duplicate only PBR values, preserving distinct material names. The immutable
# source asset is never changed; all derived assets and publications are disposable.
$reference = Join-Path $root 'examples/Renderer3DPbrHardeningTests/Assets'
$original = [IO.File]::ReadAllBytes((Join-Path $reference 'PbrLab.sm3d'))
if ([BitConverter]::ToUInt16($original, 4) -ne 2 -or
    [BitConverter]::ToUInt32($original, 48) -ne 2 -or
    [BitConverter]::ToUInt32($original, 36) -ne 2) {
    throw 'Expected the checked-in two-material, two-part SM3D v2 fixture.'
}
$materialOffset = -1
$textureEntry = -1
$directory = [BitConverter]::ToUInt32($original, 24)
$entries = [BitConverter]::ToUInt32($original, 20)
for ($index = 0; $index -lt $entries; $index++) {
    $entry = $directory + $index * 32
    $name = [Text.Encoding]::ASCII.GetString($original, $entry, 4)
    if ($name -eq 'MATL') { $materialOffset = [BitConverter]::ToUInt32($original, $entry + 8) }
    if ($name -eq 'TEXR') { $textureEntry = $entry }
}
if ($materialOffset -lt 0 -or $textureEntry -lt 0) { throw 'Missing SM3D fixture chunks.' }

function Write-U32([byte[]]$Bytes, [int]$Offset, [uint32]$Value) {
    [Array]::Copy([BitConverter]::GetBytes($Value), 0, $Bytes, $Offset, 4)
}
function Write-F32([byte[]]$Bytes, [int]$Offset, [single]$Value) {
    [Array]::Copy([BitConverter]::GetBytes($Value), 0, $Bytes, $Offset, 4)
}
function Write-ChecksummedModel([byte[]]$Bytes, [string]$Path) {
    [uint64]$hash = 2166136261
    for ($position = 64; $position -lt $Bytes.Length; $position++) {
        $hash = (($hash -bxor $Bytes[$position]) * [uint64]16777619) -band [uint64]4294967295
    }
    Write-U32 $Bytes 16 ([uint32]$hash)
    [IO.File]::WriteAllBytes($Path, $Bytes)
}

$duplicate = [byte[]]$original.Clone()
Write-U32 $duplicate 52 0
Write-U32 $duplicate ($textureEntry + 12) 0
Write-U32 $duplicate ($textureEntry + 16) 0
for ($channel = 0; $channel -lt 4; $channel++) {
    Write-U32 $duplicate ($materialOffset + 4 + $channel * 4) ([uint32]::MaxValue)
}
Write-U32 $duplicate ($materialOffset + 20) 0
Write-U32 $duplicate ($materialOffset + 24) 1
[Array]::Copy($duplicate, $materialOffset + 4, $duplicate, $materialOffset + 84, 76)
Write-ChecksummedModel $duplicate (Join-Path $assetRoot 'Duplicate.sm3d')
$different = [byte[]]$duplicate.Clone()
Write-F32 $different ($materialOffset + 80 + 52) ([single]0.37)
Write-ChecksummedModel $different (Join-Path $assetRoot 'Different.sm3d')
[IO.File]::WriteAllBytes((Join-Path $assetRoot 'Textured.sm3d'), $original)
foreach ($name in @('Pbr-base-color.png', 'Pbr-normal.png', 'Pbr-orm.png', 'Pbr-emissive.png')) {
    Copy-Item -LiteralPath (Join-Path $reference ('Textures/' + $name)) -Destination (Join-Path $textureRoot $name)
}

if (-not $SourcePath) {
    $SourcePath = Join-Path $root 'examples/Renderer3DPbrSharingTests/Program.smile'
    if (-not (Test-Path -LiteralPath $SourcePath)) { $SourcePath = Join-Path $PSScriptRoot 'Program.smile' }
}
$source = [IO.File]::ReadAllText($SourcePath)
[IO.File]::WriteAllText((Join-Path $sourceRoot 'Program.smile'), $source.Replace('@OUTPUT@', $binaryRoot))
# Include current library sources directly so this isolated check cannot rewrite
# the shared library package or interfere with another compiler invocation.
$libraryRoot = Join-Path $root 'libraries/Smile.Simple3D'
[xml]$libraryProject = Get-Content -LiteralPath (Join-Path $libraryRoot 'Smile.Simple3D.smilelibproj') -Raw
$librarySources = ($libraryProject.SmileProject.ItemGroup.SmileSource | ForEach-Object {
    $include = [Security.SecurityElement]::Escape((Join-Path $libraryRoot $_.Include))
    '    <SmileSource Include="' + $include + '" />'
}) -join [Environment]::NewLine
$project = @"
<SmileProject Version="1.0">
  <PropertyGroup>
    <ProjectKind>Game</ProjectKind>
    <StartupFile>Program.smile</StartupFile>
    <OutputName>PbrSharing</OutputName>
    <ApplicationId>smile.tests.$run</ApplicationId>
    <RememberWindowPlacement>false</RememberWindowPlacement>
  </PropertyGroup>
  <ItemGroup>
    <SmileSource Include="Program.smile" StartupOnly="true" />
    <Asset Include="Assets\*.sm3d" />
    <Asset Include="Assets\Textures\*.png" />
$librarySources
  </ItemGroup>
</SmileProject>
"@
$projectPath = Join-Path $sourceRoot 'PbrSharing.smileproj'
[IO.File]::WriteAllText($projectPath, $project)
$exe = Join-Path $binaryRoot 'PbrSharing.exe'
& $compiler --project $projectPath --target windows-x64 --configuration $Configuration --graphics DirectX -o $exe *> "$exe.compile.log"
if ($LASTEXITCODE -ne 0) {
    Get-Content -LiteralPath "$exe.compile.log" -Tail 30
    throw "Native PBR sharing compilation failed. Evidence: $output"
}
& (Join-Path $root 'scripts/Invoke-TownNativeCheck.ps1') -Executable $exe `
    -Expected 'PASS Renderer3D Imported PBR Sharing' -LogPrefix $exe -TimeoutSeconds 60

Add-Type -AssemblyName System.Drawing
$imagePath = Join-Path $binaryRoot 'Shared.png'
$bitmap = [Drawing.Bitmap]::new($imagePath)
try {
    if ($bitmap.Width -ne 320 -or $bitmap.Height -ne 180) { throw 'Shared PBR capture has unexpected dimensions.' }
    $colors = [Collections.Generic.HashSet[int]]::new()
    for ($y = 0; $y -lt $bitmap.Height; $y += 3) {
        for ($x = 0; $x -lt $bitmap.Width; $x += 3) { $null = $colors.Add($bitmap.GetPixel($x, $y).ToArgb()) }
    }
    if ($colors.Count -lt 2) { throw 'Shared PBR capture contains only the background.' }
    [pscustomobject]@{
        width = $bitmap.Width
        height = $bitmap.Height
        sampledColors = $colors.Count
        imageSha256 = (Get-FileHash -LiteralPath $imagePath).Hash
        sourceFixtureSha256 = (Get-FileHash -LiteralPath (Join-Path $reference 'PbrLab.sm3d')).Hash
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'evidence.json')
} finally {
    $bitmap.Dispose()
}
Write-Host "PASS Native imported PBR sharing and 320x180 rendered image. Evidence: $output"
