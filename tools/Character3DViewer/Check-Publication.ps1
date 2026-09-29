[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Directory,
    [string]$Project = (Join-Path $PSScriptRoot 'Character3DViewer.smileproj')
)

# Validate before closing a working editor or launching an incomplete publication.
$ErrorActionPreference = 'Stop'
[xml]$definition = Get-Content -LiteralPath $Project -Raw
$identity = [string]$definition.SmileProject.PropertyGroup.ApplicationId
$manifestPath = Join-Path $Directory ($identity + '.smile-assets.json')
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($manifest.applicationIdentity -cne $identity -or $manifest.target -cne 'windows-x64' -or
    @($manifest.assets).Count -eq 0) {
    throw "Studio publication has an empty or incompatible asset manifest: $manifestPath"
}
$published = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($relative in $manifest.assets) {
    $null = $published.Add($relative.Replace('\', '/'))
    $path = Join-Path $Directory $relative
    if (-not (Test-Path -LiteralPath $path -PathType Leaf) -or (Get-Item -LiteralPath $path).Length -eq 0) {
        throw "Studio runtime asset is missing or empty: $path"
    }
}
foreach ($node in $definition.SelectNodes('//Model3DAsset | //Asset')) {
    $relative = if ($node.Name -eq 'Model3DAsset') { $node.LogicalPath } else { $node.Include }
    if ($node.Name -eq 'Asset' -and $node.HasAttribute('LogicalPath')) { $relative = $node.LogicalPath }
    $relative = $relative.Replace('\', '/')
    if (-not [Management.Automation.WildcardPattern]::ContainsWildcardCharacters($relative) -and
        -not $published.Contains($relative)) {
        throw "Studio asset manifest omits a declared asset: $relative"
    }
}
Write-Host "Studio publication verified: $($published.Count) runtime assets."
