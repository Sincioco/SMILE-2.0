[CmdletBinding()]
param([Parameter(Mandatory)][string]$ProjectDirectory)

$ErrorActionPreference = 'Stop'
$arenaRepository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$arenaSource = Join-Path $arenaRepository 'tools\Character3DViewer\Assets\Backgrounds'
$arenaDestination = Join-Path ([IO.Path]::GetFullPath($ProjectDirectory)) 'Assets\Backgrounds'
New-Item -ItemType Directory -Path $arenaDestination -Force | Out-Null
foreach ($arenaImage in @('SinStarLandscape.png', 'SinStarTitleWithLogo.png')) {
    $arenaInput = Join-Path $arenaSource $arenaImage
    $arenaOutput = Join-Path $arenaDestination $arenaImage
    if ($arenaInput -eq $arenaOutput) { continue }
    if ((Test-Path -LiteralPath $arenaOutput) -and
        (Get-FileHash -LiteralPath $arenaInput).Hash -eq (Get-FileHash -LiteralPath $arenaOutput).Hash) {
        continue
    }
    Copy-Item -LiteralPath $arenaInput -Destination $arenaOutput -Force
}
