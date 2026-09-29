$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$check = Join-Path $root 'tools/Character3DViewer/Check-Publication.ps1'
$fixture = Join-Path $root ('artifacts/temp/publication-' + [Guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $fixture
$project = Join-Path $fixture 'fixture.smileproj'
Set-Content -LiteralPath $project -Value '<SmileProject><PropertyGroup><ApplicationId>fixture</ApplicationId></PropertyGroup><ItemGroup><Asset Include="background.png"/><Model3DAsset LogicalPath="town.sm3d"/></ItemGroup></SmileProject>'
$manifest = Join-Path $fixture 'fixture.smile-assets.json'
function Expect-Rejected([string]$Label) {
    $rejected = $false
    try { & $check -Directory $fixture -Project $project } catch { $rejected = $true }
    if (-not $rejected) { throw "Accepted broken publication: $Label" }
    Write-Host "PASS: $Label"
}
Set-Content -LiteralPath $manifest -Value '{"applicationIdentity":"fixture","target":"windows-x64","assets":[]}'
Expect-Rejected 'stripped project cannot launch with an empty manifest'
Set-Content -LiteralPath $manifest -Value '{"applicationIdentity":"fixture","target":"windows-x64","assets":["background.png","town.sm3d"]}'
Expect-Rejected 'missing runtime files rejected'
Set-Content -LiteralPath (Join-Path $fixture 'background.png') -Value 'fixture image'
Set-Content -LiteralPath (Join-Path $fixture 'town.sm3d') -Value 'fixture model'
& $check -Directory $fixture -Project $project
Set-Content -LiteralPath $manifest -Value '{"applicationIdentity":"fixture","target":"windows-x64","assets":["background.png"]}'
Expect-Rejected 'manifest cannot silently omit a declared map model'
& $check -Directory (Join-Path $root 'tools/Character3DViewer/bin/Release')
Write-Host 'PASS: restored live Studio publication'
