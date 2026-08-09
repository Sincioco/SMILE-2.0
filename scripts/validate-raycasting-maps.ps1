[CmdletBinding()]
param(
    [string] $MapDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if ([string]::IsNullOrWhiteSpace($MapDirectory)) {
    $MapDirectory = Join-Path $PSScriptRoot '..\games\DungeonStarII\Maps'
}

$expectedFiles = @('default.map', 'custom.map')
$wallSymbols = '#123456'
$passageSymbols = '.DONESWUV'

function Test-Wall([char] $Symbol) {
    return $wallSymbols.Contains([string] $Symbol)
}

function Test-Passage([char] $Symbol) {
    return $passageSymbols.Contains([string] $Symbol)
}

function Assert-RaycasterMap([string] $Path) {
    $rows = [System.Collections.Generic.List[string]]::new()
    $seenFloorOne = $false

    foreach ($rawLine in [System.IO.File]::ReadAllLines($Path)) {
        $line = $rawLine.TrimEnd("`r")
        if ($line.Length -eq 0 -or $line.StartsWith(';', [System.StringComparison]::Ordinal)) {
            continue
        }

        if ($line -match '^\[FLOOR ([1-3])\]$') {
            $floor = [int] $Matches[1]
            if ($floor -eq 1 -and -not $seenFloorOne -and $rows.Count -eq 0) {
                $seenFloorOne = $true
                continue
            }
            if ($seenFloorOne -and $rows.Count -eq 31 -and $floor -gt 1) {
                break
            }
            throw "$(Split-Path $Path -Leaf): unexpected or repeated $line header."
        }

        if (-not $seenFloorOne) {
            throw "$(Split-Path $Path -Leaf): map data appears before [FLOOR 1]."
        }
        if ($rows.Count -ge 31) {
            throw "$(Split-Path $Path -Leaf): floor 1 has more than 31 rows."
        }
        if ($line.Length -ne 31) {
            throw "$(Split-Path $Path -Leaf): row $($rows.Count + 1) is $($line.Length) symbols instead of 31."
        }
        if ($line -notmatch '^[#1-6.DONESWUV]{31}$') {
            throw "$(Split-Path $Path -Leaf): row $($rows.Count + 1) contains an unknown symbol."
        }
        $rows.Add($line)
    }

    if (-not $seenFloorOne -or $rows.Count -ne 31) {
        throw "$(Split-Path $Path -Leaf): [FLOOR 1] and exactly 31 rows are required."
    }

    for ($coordinate = 0; $coordinate -lt 31; $coordinate++) {
        if (-not (Test-Wall $rows[0][$coordinate]) -or
            -not (Test-Wall $rows[30][$coordinate]) -or
            -not (Test-Wall $rows[$coordinate][0]) -or
            -not (Test-Wall $rows[$coordinate][30])) {
            throw "$(Split-Path $Path -Leaf): the outside border must be solid wall material."
        }
    }

    $starts = [System.Collections.Generic.List[object]]::new()
    for ($y = 1; $y -lt 30; $y++) {
        for ($x = 1; $x -lt 30; $x++) {
            $symbol = $rows[$y][$x]
            if ('NESW'.Contains([string] $symbol)) {
                $starts.Add([pscustomobject]@{ X = $x; Y = $y; Direction = $symbol })
            }
            if ($symbol -eq 'D') {
                $horizontal = (Test-Passage $rows[$y][$x - 1]) -and (Test-Passage $rows[$y][$x + 1]) -and
                    (Test-Wall $rows[$y - 1][$x]) -and (Test-Wall $rows[$y + 1][$x])
                $vertical = (Test-Passage $rows[$y - 1][$x]) -and (Test-Passage $rows[$y + 1][$x]) -and
                    (Test-Wall $rows[$y][$x - 1]) -and (Test-Wall $rows[$y][$x + 1])
                if ($horizontal -eq $vertical) {
                    throw "$(Split-Path $Path -Leaf): door at ($x,$y) must have exactly one travel orientation."
                }
            }
        }
    }

    if ($starts.Count -ne 1) {
        throw "$(Split-Path $Path -Leaf): expected exactly one start, found $($starts.Count)."
    }

    $start = $starts[0]
    $visited = [System.Collections.Generic.HashSet[string]]::new()
    $queue = [System.Collections.Generic.Queue[object]]::new()
    $queue.Enqueue([pscustomobject]@{ X = $start.X; Y = $start.Y })
    [void] $visited.Add("$($start.X),$($start.Y)")

    while ($queue.Count -gt 0) {
        $cell = $queue.Dequeue()
        foreach ($step in @(@(0, -1), @(1, 0), @(0, 1), @(-1, 0))) {
            $nextX = $cell.X + $step[0]
            $nextY = $cell.Y + $step[1]
            $key = "$nextX,$nextY"
            if ($nextX -gt 0 -and $nextX -lt 30 -and $nextY -gt 0 -and $nextY -lt 30 -and
                (Test-Passage $rows[$nextY][$nextX]) -and $visited.Add($key)) {
                $queue.Enqueue([pscustomobject]@{ X = $nextX; Y = $nextY })
            }
        }
    }

    $passageCount = 0
    $openSquareCount = 0
    for ($y = 1; $y -lt 30; $y++) {
        for ($x = 1; $x -lt 30; $x++) {
            if (Test-Passage $rows[$y][$x]) { $passageCount++ }
            if ($x -lt 29 -and $y -lt 29 -and
                (Test-Passage $rows[$y][$x]) -and (Test-Passage $rows[$y][$x + 1]) -and
                (Test-Passage $rows[$y + 1][$x]) -and (Test-Passage $rows[$y + 1][$x + 1])) {
                $openSquareCount++
            }
        }
    }
    if ($visited.Count -ne $passageCount) {
        throw "$(Split-Path $Path -Leaf): walkable cells and doors are not completely connected."
    }
    if ($openSquareCount -eq 0) {
        throw "$(Split-Path $Path -Leaf): expected at least one open 2-by-2 room region."
    }

    Write-Host "Validated Dungeon Star II map: $(Split-Path $Path -Leaf) ($passageCount reachable cells, $openSquareCount open room squares)"
}

foreach ($file in $expectedFiles) {
    $path = Join-Path $MapDirectory $file
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required Dungeon Star II map is missing: $path"
    }
    Assert-RaycasterMap $path
}

Write-Host 'All supplied Dungeon Star II maps passed raycasting-map validation.'
