[CmdletBinding()]
param([string]$PublicationDirectory, [ValidateRange(10, 120)][int]$ExportTimeoutSeconds = 120)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$viewer = Join-Path $root 'tools/Character3DViewer'
$publication = Join-Path $viewer 'bin/Release'
if ($PublicationDirectory) { $publication = [IO.Path]::GetFullPath($PublicationDirectory) }
& (Join-Path $viewer 'Check-Publication.ps1') -Directory $publication

$run = 'town-cold-export-' + [Guid]::NewGuid().ToString('N')
$output = Join-Path $root ('artifacts/tests/' + $run)
$null = New-Item -ItemType Directory -Path $output
$applicationId = 'smile.tests.' + $run
$sourcePath = Join-Path $output 'TownColdExportTests.smile'
$source = [IO.File]::ReadAllText((Join-Path $viewer 'TownColdExportTests.smile'))
[IO.File]::WriteAllText($sourcePath, $source.Replace('@OUTPUT@', $output).Replace('@EXPORT_TIMEOUT@', [string]($ExportTimeoutSeconds * 1000)))
[xml]$project = Get-Content (Join-Path $viewer 'Character3DViewer.smileproj') -Raw
$project.SmileProject.PropertyGroup.StartupFile = $sourcePath
$project.SmileProject.PropertyGroup.ApplicationId = $applicationId
$project.SmileProject.PropertyGroup.RememberWindowPlacement = 'false'
foreach ($node in @($project.SelectNodes('//NativeWorkerScript | //Model3DAsset | //Asset'))) {
    $null = $node.ParentNode.RemoveChild($node)
}
foreach ($node in $project.SmileProject.ItemGroup.ChildNodes) {
    if ($node.HasAttribute('Include')) {
        $path = [IO.Path]::GetFullPath((Join-Path $viewer $node.GetAttribute('Include')))
        if ($node.GetAttribute('StartupOnly') -eq 'true') { $path = $sourcePath }
        $node.SetAttribute('Include', $path)
    }
}
$projectPath = Join-Path $output 'ColdExport.smileproj'
$exe = Join-Path $output 'ColdExport.exe'
$project.Save($projectPath)
& (Join-Path $root 'artifacts/compiler/smilec.exe') --project $projectPath --target windows-x64 --graphics DirectX -o $exe *> "$exe.compile.log"
if ($LASTEXITCODE -ne 0) {
    Get-Content "$exe.compile.log" -Tail 30
    throw "Cold export fixture compile failed. Evidence: $output"
}

# Link only after compiling in a fresh disposable directory. The temporary project
# cannot prune assets in the verified Studio publication or another test output.
$assetSource = Join-Path $publication 'Assets'
$assetFiles = @(Get-ChildItem -LiteralPath $assetSource -File -Recurse)
if ($assetFiles.Count -eq 0) { throw 'Verified publication has no assets.' }
foreach ($file in $assetFiles) {
    $destination = Join-Path (Join-Path $output 'Assets') ([IO.Path]::GetRelativePath($assetSource, $file.FullName))
    $null = New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force
    $null = New-Item -ItemType HardLink -Path $destination -Target $file.FullName
}

function Get-Hash([string]$Value) {
    [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($Value))).ToLowerInvariant()
}
$storage = & (Join-Path $PSScriptRoot 'get-smile-data-root.ps1')
$data = Join-Path $storage "$(Get-Hash $applicationId)/Data"
$null = New-Item -ItemType Directory -Path $data -Force
$game = 'D:\SMILE 2.0 - Sin Star I'
if (-not (Test-Path -LiteralPath $game)) { $game = Join-Path $root 'games/SinStarI' }
$townRoot = Join-Path $game 'SourceAssets/Towns/Neris'
$fixtures = [ordered]@{
    'Cold.Metropolis' = 'NerisMetropolisV1/Town/Neris Metropolis.town'
    'Cold.Court' = 'StoryTownsV1/Towns/Neris Town.town'
    'Cold.Port' = 'StoryTownsV1/Towns/Neris Spaceport.town'
    'Cold.Horizon' = 'StoryTownsV1/Towns/Horizon Airport.town'
    'TownEditor.Permanent.Neris Metropolis' = 'NerisMetropolisV1/Town/Neris Metropolis.town'
}
$inputEvidence = foreach ($key in $fixtures.Keys) {
    $path = Join-Path $townRoot $fixtures[$key]
    $bytes = [IO.File]::ReadAllBytes($path)
    if ($bytes.Length -lt 44) { throw "Incomplete fixture envelope: $path" }
    $length = 44 + [BitConverter]::ToUInt32($bytes, 8)
    if ($length -gt $bytes.Length) { throw "Truncated fixture envelope: $path" }
    [IO.File]::WriteAllBytes((Join-Path $data "$(Get-Hash $key).bin"), $bytes[0..($length - 1)])
    [pscustomobject]@{ key = $key; path = $path; sha256 = (Get-FileHash -LiteralPath $path).Hash }
}
$inputEvidence | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'inputs.json')
[pscustomobject]@{
    applicationId = $applicationId
    publication = $publication
    assetCount = $assetFiles.Count
    sourceSha256 = (Get-FileHash -LiteralPath $sourcePath).Hash
    data = $data
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'run.json')

& (Join-Path $PSScriptRoot 'Invoke-TownNativeCheck.ps1') -Executable $exe `
    -Expected 'PASS Town Cold Export Acceptance' -LogPrefix $exe -TimeoutSeconds (7 * $ExportTimeoutSeconds + 60)
$text = [IO.File]::ReadAllText("$exe.stdout.log")
$matches = @([regex]::Matches($text, '(?m)^PNG (.+)\r?$'))
if ($matches.Count -ne 7) { throw "Expected exactly seven successful cold pairs, observed $($matches.Count)." }
Add-Type -AssemblyName System.Drawing
$images = foreach ($match in $matches) {
    $path = $match.Groups[1].Value.Trim()
    if (-not (Test-Path -LiteralPath ([IO.Path]::ChangeExtension($path, '.town')))) {
        throw "PNG has no same-name town: $path"
    }
    $bitmap = [Drawing.Bitmap]::new($path)
    try {
        if ($bitmap.Width -ne 384 -or $bitmap.Height -ne 240) { throw "Unexpected photograph dimensions: $path" }
        $colors = [Collections.Generic.HashSet[int]]::new()
        for ($y = 0; $y -lt $bitmap.Height; $y += 4) {
            for ($x = 0; $x -lt $bitmap.Width; $x += 4) { $null = $colors.Add($bitmap.GetPixel($x, $y).ToArgb()) }
        }
        if ($colors.Count -lt 8) { throw "Cold photograph lacks real scene color variation: $path" }
        [pscustomobject]@{ path = $path; width = $bitmap.Width; height = $bitmap.Height; colors = $colors.Count; sha256 = (Get-FileHash -LiteralPath $path).Hash }
    } finally { $bitmap.Dispose() }
}
$images | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'images.json')
$original = (Get-FileHash -LiteralPath (Join-Path $output 'Cold.png')).Hash
if ($original -eq (Get-FileHash -LiteralPath (Join-Path $output 'Newer.png')).Hash) {
    throw 'Negative control changed geometry/light but did not change photograph pixels.'
}
if ($original -ne (Get-FileHash -LiteralPath (Join-Path $output 'Copy.png')).Hash) {
    throw 'Cold Save As does not preserve deterministic source photograph pixels.'
}
Write-Host "PASS Seven real cold pairs, native reopen, PNG dimensions/content and negative control. Evidence: $output"
