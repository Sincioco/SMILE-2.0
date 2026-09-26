[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$work = Join-Path $root ('artifacts\tests\shared-textures-' + [Guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $work -Force
Add-Type -AssemblyName System.Drawing
$bitmap = [Drawing.Bitmap]::new(4, 4)
try {
    for ($y = 0; $y -lt 4; $y++) {
        for ($x = 0; $x -lt 4; $x++) {
            $bitmap.SetPixel($x, $y, [Drawing.Color]::FromArgb(255, 51, 102, 153))
        }
    }
    $bitmap.Save((Join-Path $work 'Atlas.png'), [Drawing.Imaging.ImageFormat]::Png)
} finally { $bitmap.Dispose() }
$fixture = Join-Path $root 'examples\Renderer3DModelTests\Source\PbrTriangle.gltf'
foreach ($name in @('First', 'Second', 'OcclusionOnly')) {
    $model = Get-Content -LiteralPath $fixture -Raw | ConvertFrom-Json -AsHashtable
    $material = @{ name = 'Shared'; pbrMetallicRoughness = @{}; occlusionTexture = @{ index = 0 } }
    if ($name -ne 'OcclusionOnly') {
        $material.pbrMetallicRoughness.metallicRoughnessTexture = @{ index = 0 }
    }
    $model.materials = @($material)
    $model.textures = @(@{ source = 0 })
    $model.images = @(@{ uri = 'Atlas.png' })
    $model | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath (Join-Path $work "$name.gltf")
}
'Print "Shared Texture Fixture"' | Set-Content -LiteralPath (Join-Path $work 'Program.smile')
@'
<SmileProject Version="1.0">
  <PropertyGroup><ProjectKind>Console</ProjectKind><StartupFile>Program.smile</StartupFile></PropertyGroup>
  <ItemGroup>
    <SmileSource Include="Program.smile" StartupOnly="true" />
    <Model3DAsset Include="First.gltf" LogicalPath="Assets/First.sm3d" TextureOutputDirectory="Assets/Textures" Profile="Static" />
    <Model3DAsset Include="Second.gltf" LogicalPath="Assets/Second.sm3d" TextureOutputDirectory="Assets/Textures" Profile="Static" />
    <Model3DAsset Include="OcclusionOnly.gltf" LogicalPath="Assets/Third.sm3d" TextureOutputDirectory="Assets/Textures" Profile="Static" />
  </ItemGroup>
</SmileProject>
'@ | Set-Content -LiteralPath (Join-Path $work 'Test.smileproj')
& (Join-Path $root 'artifacts\compiler\smilec.exe') --project (Join-Path $work 'Test.smileproj') `
    --target windows-x64 -o (Join-Path $work 'out\Test.exe')
if ($LASTEXITCODE -ne 0) { throw 'Shared texture compilation failed.' }
$textures = @(Get-ChildItem -LiteralPath (Join-Path $work 'out\Assets\Textures') -File)
if ($textures.Count -ne 2) { throw "Expected two distinct cooked ORM maps, found $($textures.Count)." }
$pixels = foreach ($texture in $textures) {
    $image = [Drawing.Bitmap]::new($texture.FullName)
    try { $image.GetPixel(0, 0).ToArgb() } finally { $image.Dispose() }
}
if ($pixels[0] -eq $pixels[1]) { throw 'Different ORM channel conversions were conflated.' }
$manifests = Get-ChildItem -LiteralPath (Join-Path $work 'obj') -Recurse -Filter cook-manifest.json
$paths = foreach ($manifest in $manifests) {
    $data = Get-Content -LiteralPath $manifest.FullName -Raw | ConvertFrom-Json
    ($data.outputs | Where-Object isTexture).logicalPath
}
if (@($paths).Count -ne 3 -or @($paths | Sort-Object -Unique).Count -ne 2) {
    throw 'Identical static partitions did not share their cooked texture path.'
}
Write-Host 'PASS static partitions share identical textures; different cooked ORM pixels stay distinct.'
