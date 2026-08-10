$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$mapRoot = Join-Path $repositoryRoot 'games\PlatformQuest\Maps'
$mapNames = @('default.map', 'custom.map')
$width = 120
$height = 15
$legalSymbols = '.#B?=CE^SG'
$supportSymbols = '#B?='

function Test-Support {
    param([char]$Symbol)
    return $supportSymbols.IndexOf($Symbol) -ge 0
}

foreach ($mapName in $mapNames) {
    $path = Join-Path $mapRoot $mapName
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Platform Quest map is missing: $mapName"
    }

    $contentLines = @(Get-Content -LiteralPath $path | Where-Object {
        $trimmed = $_.Trim()
        $trimmed.Length -gt 0 -and -not $trimmed.StartsWith(';')
    })
    if ($contentLines.Count -ne $height + 1 -or $contentLines[0] -cne '[LEVEL 1]') {
        throw "$mapName must contain [LEVEL 1] followed by exactly $height map rows."
    }

    $rows = @($contentLines | Select-Object -Skip 1)
    $startCount = 0
    $goalCount = 0
    for ($y = 0; $y -lt $height; $y++) {
        if ($rows[$y].Length -ne $width) {
            throw "$mapName row $($y + 1) is $($rows[$y].Length) symbols; expected $width."
        }
        for ($x = 0; $x -lt $width; $x++) {
            $symbol = $rows[$y][$x]
            if ($legalSymbols.IndexOf($symbol) -lt 0) {
                throw "$mapName contains illegal symbol '$symbol' at row $($y + 1), column $($x + 1)."
            }
            if ($symbol -ceq 'S') { $startCount++ }
            if ($symbol -ceq 'G') { $goalCount++ }
            if ($symbol -in @('S', 'G', 'E', '^')) {
                if ($y + 1 -ge $height -or -not (Test-Support $rows[$y + 1][$x])) {
                    throw "$mapName has unsupported '$symbol' at row $($y + 1), column $($x + 1)."
                }
            }
        }
    }
    if ($startCount -ne 1 -or $goalCount -ne 1) {
        throw "$mapName must contain exactly one S and one G."
    }

    $bottomGap = 0
    for ($x = 0; $x -lt $width; $x++) {
        if (Test-Support $rows[$height - 1][$x]) {
            $bottomGap = 0
        }
        else {
            $bottomGap++
            if ($bottomGap -gt 3) {
                throw "$mapName has a bottom-row gap wider than three cells near column $($x + 1)."
            }
        }
    }
}

Write-Host "Platform Quest map validation passed: $($mapNames.Count) files, ${width}x${height}, legal symbols, supported actors, and gaps of at most three cells."
