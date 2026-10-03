[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$viewer = Join-Path $root 'tools/Character3DViewer'
$output = Join-Path $root ('artifacts/tests/town-export-' + [Guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $output
$null = New-Item -ItemType Directory -Path (Join-Path $output 'Broken.png')
$source = Join-Path $output 'TownExportTests.smile'
[IO.File]::WriteAllText($source, [IO.File]::ReadAllText((Join-Path $viewer 'TownExportTests.smile')).Replace('@OUTPUT@', $output))
[xml]$project = Get-Content (Join-Path $viewer 'Character3DViewer.smileproj') -Raw
$project.SmileProject.PropertyGroup.StartupFile = $source
$project.SmileProject.PropertyGroup.ApplicationId = 'smile.tests.town-export.run-' + [Guid]::NewGuid().ToString('N')
$project.SmileProject.PropertyGroup.RememberWindowPlacement = 'false'
foreach ($node in @($project.SelectNodes('//NativeWorkerScript | //Model3DAsset | //Asset'))) {
    $null = $node.ParentNode.RemoveChild($node)
}
foreach ($node in $project.SmileProject.ItemGroup.ChildNodes) {
    if ($node.HasAttribute('Include')) {
        $path = [IO.Path]::GetFullPath((Join-Path $viewer $node.GetAttribute('Include')))
        if ($node.GetAttribute('StartupOnly') -eq 'true') { $path = $source }
        $node.SetAttribute('Include', $path)
    }
}
$projectPath = Join-Path $output 'Export.smileproj'
$executable = Join-Path $output 'Export.exe'
$project.Save($projectPath)
try {
    & (Join-Path $root 'artifacts/compiler/smilec.exe') --project $projectPath --target windows-x64 -o $executable *> "$executable.compile.log"
    if ($LASTEXITCODE -ne 0) {
        Get-Content "$executable.compile.log" -Tail 25
        throw 'Export fixture compilation failed.'
    }
    & (Join-Path $PSScriptRoot 'Invoke-TownNativeCheck.ps1') -Executable $executable `
        -Expected 'PASS Town Export Photographs' -LogPrefix $executable -TimeoutSeconds 60
    $text = [string](Get-Content "$executable.stdout.log" -Raw)
    $pair = [regex]::Match($text, '(?m)^PAIR (.+)\r?$').Groups[1].Value.Trim()
    $expected = (Get-FileHash (Join-Path $output 'Expected.png')).Hash
    if ((Get-FileHash $pair).Hash -ne $expected) { throw 'Queued PNG differs from the frozen original photograph.' }
    if ((Get-FileHash (Join-Path $output 'Newer.png')).Hash -eq $expected) { throw 'Photograph negative control did not change its pixels.' }
    foreach ($match in [regex]::Matches($text, '(?m)^PNG (.+)\r?$')) {
        if ((Get-FileHash $match.Groups[1].Value.Trim()).Hash -ne $expected) {
            throw 'Unchanged queued photograph does not match its native reference.'
        }
    }
    Write-Host "PASS Parsed town identities and native PNG byte provenance. Evidence: $output"
} finally {
    Remove-Item -LiteralPath $projectPath -ErrorAction SilentlyContinue
}
