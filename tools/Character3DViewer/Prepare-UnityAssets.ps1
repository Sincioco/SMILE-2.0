[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$definitions = @(
    @{ Name = 'Valor'; Folder = 'Characters\Knight\ValorV1' },
    @{ Name = 'Zara'; Folder = 'Characters\Warrior\ZaraV1' },
    @{ Name = 'Vrax'; Folder = 'Bosses\Vrax\VraxV1' }
)
$available = @($definitions | Where-Object {
    Test-Path -LiteralPath (Join-Path $repositoryRoot (
        'games\SinStarI\SourceAssets\' + $_.Folder + '\Private\' + $_.Name + '-v1-animation-set.glb'))
})
if ($available.Count -eq 0) { return }
if ($available.Count -ne $definitions.Count) {
    throw 'The local Unity roster requires the complete Valor, Zara and Vrax packages.'
}
foreach ($definition in $definitions) {
    $name = $definition.Name
    $package = Join-Path $repositoryRoot ('games\SinStarI\SourceAssets\' + $definition.Folder)
    $staging = Join-Path $PSScriptRoot "BuildAssets\${name}V1"
    $null = New-Item -ItemType Directory -Force -Path $staging
    $source = Join-Path $package "Private\$name-v1-animation-set.glb"
    $manifest = Get-Content -LiteralPath (Join-Path $package 'package-manifest.json') -Raw | ConvertFrom-Json
    if ((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ine $manifest.export.sha256) {
        throw "$name does not match its canonical package checksum."
    }
    Copy-Item -LiteralPath $source -Destination (Join-Path $staging "$name-v1-animation-set.glb") -Force
    Copy-Item -LiteralPath (Join-Path $package "${name}V1.sm3d.json") -Destination $staging -Force
    [pscustomobject]@{
        Name = $name
        Include = "BuildAssets\${name}V1\$name-v1-animation-set.glb"
        Descriptor = "BuildAssets\${name}V1\${name}V1.sm3d.json"
        LogicalPath = "Assets\Generation2\${name}V1\$name.sm3d"
        Textures = "Assets\Generation2\${name}V1\Textures"
    }
}
