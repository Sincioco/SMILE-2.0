[CmdletBinding()]
param([Parameter(Mandatory)][string]$PublicationDirectory, [int]$ExportTimeoutSeconds = 60)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$viewer = Join-Path $root 'tools/Character3DViewer'
$run = 'metropolis-' + [guid]::NewGuid().ToString('N')
$output = Join-Path $root "artifacts/tests/$run"
$null = New-Item -ItemType Directory -Path $output
$applicationId = 'smile.tests.' + $run
$sourcePath = Join-Path $output 'Metropolis.smile'
$source = [IO.File]::ReadAllText((Join-Path $viewer 'TownMetropolisTests.smile'))
[IO.File]::WriteAllText($sourcePath, $source.Replace('@EVIDENCE@', $output).Replace('@EXPORT_TIMEOUT@', [string]($ExportTimeoutSeconds * 1000)))
[xml]$project = Get-Content (Join-Path $viewer 'Character3DViewer.smileproj') -Raw
$project.SmileProject.PropertyGroup.StartupFile = $sourcePath
$project.SmileProject.PropertyGroup.ApplicationId = $applicationId
$worker = $project.SmileProject.PropertyGroup.SelectSingleNode('NativeWorkerScript')
if ($worker) { $null = $worker.ParentNode.RemoveChild($worker) }
foreach ($node in @($project.SmileProject.ItemGroup.ChildNodes)) {
    if ($node.LocalName -notin @('SmileSource', 'SmileProjectReference')) {
        $null = $node.ParentNode.RemoveChild($node)
    } elseif ($node.GetAttribute('StartupOnly') -eq 'true') {
        $node.SetAttribute('Include', $sourcePath)
    } else {
        $node.SetAttribute('Include', [IO.Path]::GetFullPath((Join-Path $viewer $node.GetAttribute('Include'))))
    }
}
$projectPath = Join-Path $output 'Metropolis.smileproj'
$project.Save($projectPath)
$exe = Join-Path $output 'Metropolis.exe'
& (Join-Path $root 'artifacts/compiler/smilec.exe') --project $projectPath --target windows-x64 -o $exe *> "$exe.compile.log"
if ($LASTEXITCODE -ne 0) { Get-Content "$exe.compile.log" -Tail 20; throw 'Metropolis fixture compile failed.' }

# Only link assets after compilation, in a fresh test directory. Compilation must
# never prune the asset publication of a running Studio or another test.
$assetSource = Join-Path ([IO.Path]::GetFullPath($PublicationDirectory)) 'Assets'
foreach ($file in Get-ChildItem -LiteralPath $assetSource -File -Recurse) {
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
$town = Join-Path $root 'games/SinStarI/SourceAssets/Towns/Neris/NerisMetropolisV1/Town/Neris Metropolis.town'
# LoadDocument reads a Save Data envelope, whereas a portable .town may append
# prepared renderer records. Seed only its checked document envelope here;
# the fixture separately tests the production prepared-file save/open path.
$townBytes = [IO.File]::ReadAllBytes($town)
$documentLength = 44 + [BitConverter]::ToUInt32($townBytes, 8)
if ($documentLength -gt $townBytes.Length) { throw 'Incomplete Metropolis fixture envelope.' }
[IO.File]::WriteAllBytes((Join-Path $data "$(Get-Hash 'Metropolis.Fixture').bin"),
    $townBytes[0..($documentLength - 1)])
$neris = Join-Path $storage "$(Get-Hash 'smile.tools.character3d-viewer')/Data/$(Get-Hash 'TownEditor.PermanentNeris').bin"
Copy-Item -LiteralPath $neris -Destination (Join-Path $data "$(Get-Hash 'Neris.Fixture').bin")
# Preserve the user's approved right-of-Codex placement in the isolated test app.
$placement = "$(Get-Hash '__smile_internal_window_placement_v2').bin"
$live = Join-Path $storage "$(Get-Hash 'smile.tools.character3d-viewer')/Data/$placement"
if (Test-Path -LiteralPath $live) { Copy-Item -LiteralPath $live -Destination (Join-Path $data $placement) }
& (Join-Path $PSScriptRoot 'Invoke-TownNativeCheck.ps1') -Executable $exe `
    -Expected 'PASS Metropolis Native Regression' -LogPrefix $exe -TimeoutSeconds ($ExportTimeoutSeconds + 45)
Write-Host "Evidence: $output"
