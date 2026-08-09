[CmdletBinding()]
param(
    [string] $MapDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if ([string]::IsNullOrWhiteSpace($MapDirectory)) {
    $MapDirectory = Join-Path $PSScriptRoot '..\games\DungeonStarI\Maps'
}

$expectedFiles = @('default.map', 'sample-loops.map', 'sample-switchbacks.map')
$walkableSymbols = '.DONESWUV'

function Test-Walkable([char] $Symbol) {
    return $walkableSymbols.Contains([string] $Symbol)
}

function Assert-Map([string] $Path) {
    $floors = @(
        [System.Collections.Generic.List[string]]::new(),
        [System.Collections.Generic.List[string]]::new(),
        [System.Collections.Generic.List[string]]::new()
    )
    $expectedHeader = 1
    $currentFloor = -1

    foreach ($rawLine in [System.IO.File]::ReadAllLines($Path)) {
        $line = $rawLine.TrimEnd("`r")
        if ($line.Length -eq 0 -or $line.StartsWith(';', [System.StringComparison]::Ordinal)) {
            continue
        }

        if ($line -match '^\[FLOOR ([1-3])\]$') {
            $floorNumber = [int] $Matches[1]
            if ($floorNumber -ne $expectedHeader) {
                throw "$(Split-Path $Path -Leaf): expected [FLOOR $expectedHeader], found $line."
            }
            if ($currentFloor -ge 0 -and $floors[$currentFloor].Count -ne 31) {
                throw "$(Split-Path $Path -Leaf): floor $($currentFloor + 1) has $($floors[$currentFloor].Count) rows instead of 31."
            }
            $currentFloor = $floorNumber - 1
            $expectedHeader++
            continue
        }

        if ($currentFloor -lt 0) {
            throw "$(Split-Path $Path -Leaf): map data appears before [FLOOR 1]."
        }
        if ($line.Length -ne 31) {
            throw "$(Split-Path $Path -Leaf): floor $($currentFloor + 1) row $($floors[$currentFloor].Count + 1) is $($line.Length) symbols instead of 31."
        }
        if ($line -notmatch '^[#.DONESWUV]{31}$') {
            throw "$(Split-Path $Path -Leaf): floor $($currentFloor + 1) row $($floors[$currentFloor].Count + 1) contains an unknown symbol."
        }
        $floors[$currentFloor].Add($line)
    }

    if ($expectedHeader -ne 4) {
        throw "$(Split-Path $Path -Leaf): all three ordered floor headers are required."
    }
    for ($floor = 0; $floor -lt 3; $floor++) {
        if ($floors[$floor].Count -ne 31) {
            throw "$(Split-Path $Path -Leaf): floor $($floor + 1) has $($floors[$floor].Count) rows instead of 31."
        }
    }

    $starts = [System.Collections.Generic.List[object]]::new()
    $upCounts = @(0, 0, 0)
    $downCounts = @(0, 0, 0)

    for ($floor = 0; $floor -lt 3; $floor++) {
        for ($coordinate = 0; $coordinate -lt 31; $coordinate++) {
            if ($floors[$floor][0][$coordinate] -ne '#' -or $floors[$floor][30][$coordinate] -ne '#' -or
                $floors[$floor][$coordinate][0] -ne '#' -or $floors[$floor][$coordinate][30] -ne '#') {
                throw "$(Split-Path $Path -Leaf): floor $($floor + 1) has an open outside border."
            }
        }

        for ($y = 0; $y -lt 31; $y++) {
            for ($x = 0; $x -lt 31; $x++) {
                $symbol = $floors[$floor][$y][$x]
                if ('NESW'.Contains([string] $symbol)) {
                    $starts.Add([pscustomobject]@{ Floor = $floor; X = $x; Y = $y; Direction = $symbol })
                }
                if ($symbol -eq 'U') { $upCounts[$floor]++ }
                if ($symbol -eq 'V') { $downCounts[$floor]++ }

                if ($x -lt 30 -and $y -lt 30 -and
                    (Test-Walkable $floors[$floor][$y][$x]) -and
                    (Test-Walkable $floors[$floor][$y][$x + 1]) -and
                    (Test-Walkable $floors[$floor][$y + 1][$x]) -and
                    (Test-Walkable $floors[$floor][$y + 1][$x + 1])) {
                    throw "$(Split-Path $Path -Leaf): floor $($floor + 1) has a 2-by-2 walkable block at ($x,$y)."
                }
            }
        }

        $anchor = $null
        for ($y = 1; $y -lt 30 -and $null -eq $anchor; $y++) {
            for ($x = 1; $x -lt 30; $x++) {
                if (Test-Walkable $floors[$floor][$y][$x]) {
                    $anchor = [pscustomobject]@{ X = $x; Y = $y }
                    break
                }
            }
        }
        if ($null -eq $anchor) {
            throw "$(Split-Path $Path -Leaf): floor $($floor + 1) has no walkable cells."
        }

        $visited = [System.Collections.Generic.HashSet[string]]::new()
        $queue = [System.Collections.Generic.Queue[object]]::new()
        $queue.Enqueue($anchor)
        [void] $visited.Add("$($anchor.X),$($anchor.Y)")
        while ($queue.Count -gt 0) {
            $cell = $queue.Dequeue()
            foreach ($step in @(@(0, -1), @(1, 0), @(0, 1), @(-1, 0))) {
                $nextX = $cell.X + $step[0]
                $nextY = $cell.Y + $step[1]
                $key = "$nextX,$nextY"
                if ($nextX -gt 0 -and $nextX -lt 30 -and $nextY -gt 0 -and $nextY -lt 30 -and
                    (Test-Walkable $floors[$floor][$nextY][$nextX]) -and $visited.Add($key)) {
                    $queue.Enqueue([pscustomobject]@{ X = $nextX; Y = $nextY })
                }
            }
        }

        $walkableCount = 0
        for ($y = 1; $y -lt 30; $y++) {
            for ($x = 1; $x -lt 30; $x++) {
                if (Test-Walkable $floors[$floor][$y][$x]) { $walkableCount++ }

                $symbol = $floors[$floor][$y][$x]
                if ($symbol -notin @('D', 'O')) { continue }
                $horizontal = (Test-Walkable $floors[$floor][$y][$x - 1]) -and (Test-Walkable $floors[$floor][$y][$x + 1]) -and $floors[$floor][$y - 1][$x] -eq '#' -and $floors[$floor][$y + 1][$x] -eq '#'
                $vertical = (Test-Walkable $floors[$floor][$y - 1][$x]) -and (Test-Walkable $floors[$floor][$y + 1][$x]) -and $floors[$floor][$y][$x - 1] -eq '#' -and $floors[$floor][$y][$x + 1] -eq '#'
                if ($horizontal -eq $vertical) {
                    throw "$(Split-Path $Path -Leaf): floor $($floor + 1) door at ($x,$y) is not in one straight corridor."
                }
                for ($distance = 1; $distance -le 4; $distance++) {
                    if ($horizontal) {
                        $clear = $floors[$floor][$y][$x - $distance] -eq '.' -and $floors[$floor][$y][$x + $distance] -eq '.' -and
                            $floors[$floor][$y - 1][$x - $distance] -eq '#' -and $floors[$floor][$y + 1][$x - $distance] -eq '#' -and
                            $floors[$floor][$y - 1][$x + $distance] -eq '#' -and $floors[$floor][$y + 1][$x + $distance] -eq '#'
                    } else {
                        $clear = $floors[$floor][$y - $distance][$x] -eq '.' -and $floors[$floor][$y + $distance][$x] -eq '.' -and
                            $floors[$floor][$y - $distance][$x - 1] -eq '#' -and $floors[$floor][$y - $distance][$x + 1] -eq '#' -and
                            $floors[$floor][$y + $distance][$x - 1] -eq '#' -and $floors[$floor][$y + $distance][$x + 1] -eq '#'
                    }
                    if (-not $clear) {
                        throw "$(Split-Path $Path -Leaf): floor $($floor + 1) door at ($x,$y) is too close to a turn, junction, door, or feature."
                    }
                }
            }
        }
        if ($visited.Count -ne $walkableCount) {
            throw "$(Split-Path $Path -Leaf): floor $($floor + 1) is not completely connected."
        }
    }

    if ($starts.Count -ne 1) {
        throw "$(Split-Path $Path -Leaf): expected exactly one start, found $($starts.Count)."
    }
    if (($upCounts -join ',') -ne '0,1,1' -or ($downCounts -join ',') -ne '1,1,0') {
        throw "$(Split-Path $Path -Leaf): stair structure must be U=0,1,1 and V=1,1,0."
    }

    $start = $starts[0]
    $directionSteps = @{ N = @(0, -1); E = @(1, 0); S = @(0, 1); W = @(-1, 0) }
    $directionOrder = @('N', 'E', 'S', 'W')
    $directionIndex = [array]::IndexOf($directionOrder, [string] $start.Direction)
    $forward = $directionSteps[[string] $start.Direction]
    $left = $directionSteps[$directionOrder[($directionIndex + 3) % 4]]
    $right = $directionSteps[$directionOrder[($directionIndex + 1) % 4]]
    $grid = $floors[$start.Floor]
    if (-not (Test-Walkable $grid[$start.Y + $forward[1]][$start.X + $forward[0]]) -or
        -not (Test-Walkable $grid[$start.Y - $forward[1]][$start.X - $forward[0]]) -or
        $grid[$start.Y + $left[1]][$start.X + $left[0]] -ne '#' -or
        $grid[$start.Y + $right[1]][$start.X + $right[0]] -ne '#') {
        throw "$(Split-Path $Path -Leaf): start must face along a straight corridor with immediate side walls."
    }

    Write-Host "Validated Dungeon Star I map: $(Split-Path $Path -Leaf)"
}

foreach ($file in $expectedFiles) {
    $path = Join-Path $MapDirectory $file
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required Dungeon Star I map is missing: $path"
    }
    Assert-Map $path
}

Write-Host 'All supplied Dungeon Star I maps passed structural validation.'
