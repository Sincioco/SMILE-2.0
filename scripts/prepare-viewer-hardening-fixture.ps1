[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$viewer = Join-Path $root 'tools\Character3DViewer'
$output = [IO.Path]::GetFullPath($OutputDirectory)
$publicSource = Join-Path $root 'examples\Renderer3DAnimationV2Tests\Source'
[void][IO.Directory]::CreateDirectory($output)

# This public GLB embeds its buffers/images. Both descriptors are independent inputs:
# Vrax deliberately lacks aim sockets; Dragon supplies the valid Head/VraxMouth pair.
$inputs = @{
    'AnimationArticulated.glb' = Join-Path $publicSource 'AnimationArticulated.glb'
    'AnimationArticulated.sm3d.json' = Join-Path $publicSource 'AnimationArticulated.sm3d.json'
    'HardeningAim.sm3d.json' = Join-Path $viewer 'HardeningAim.sm3d.json'
    'HardeningTests.smile' = Join-Path $viewer 'HardeningTests.smile'
}
foreach ($name in $inputs.Keys) {
    Copy-Item -LiteralPath $inputs[$name] -Destination (Join-Path $output $name) -Force
}
[xml]$project = Get-Content -LiteralPath (Join-Path $viewer 'HardeningTests.smileproj') -Raw
foreach ($item in $project.SelectNodes('//*[@Include]')) {
    if ($item.Name -eq 'Model3DAsset') {
        foreach ($attribute in @('Include', 'Descriptor')) {
            $name = [IO.Path]::GetFileName($item.GetAttribute($attribute))
            if (-not $inputs.ContainsKey($name)) { throw "Undeclared public fixture input: $name" }
            $item.SetAttribute($attribute, $name)
        }
    } elseif ($item.GetAttribute('StartupOnly') -ne 'true') {
        $item.SetAttribute('Include', [IO.Path]::GetFullPath((Join-Path $viewer $item.GetAttribute('Include'))))
    }
}
$projectPath = Join-Path $output 'HardeningTests.smileproj'
$project.Save($projectPath)
Write-Output $projectPath
