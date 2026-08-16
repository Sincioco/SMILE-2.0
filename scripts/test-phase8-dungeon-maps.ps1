param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$RepositoryRoot = Split-Path -Parent $PSScriptRoot
$Checks = 0
$Maps = @{}

function Assert-Condition {
    param(
        [Parameter(Mandatory)] [bool] $Condition,
        [Parameter(Mandatory)] [string] $Message
    )

    if (-not $Condition) {
        throw $Message
    }

    $script:Checks++
}

foreach ($MapIndex in 1..4) {
    $Path = Join-Path $RepositoryRoot "examples\RpgDungeonGallery\Maps\Archive$MapIndex.smilemap"
    $Lines = @(Get-Content -LiteralPath $Path)
    Assert-Condition ($Lines[0] -eq 'SMILE-MAP 1') "Archive$MapIndex has the wrong format header."

    $Size = @($Lines[1].Split(' ', [StringSplitOptions]::RemoveEmptyEntries))
    Assert-Condition ($Size.Count -eq 3 -and $Size[0] -eq 'SIZE' -and [int]$Size[1] -eq 13 -and [int]$Size[2] -eq 11) "Archive$MapIndex must be 13 by 11."
    Assert-Condition ($Lines[2] -eq 'CELL 64 64') "Archive$MapIndex must use 64-pixel cells."

    $Sections = @{}
    foreach ($Name in @('GROUND', 'DETAIL', 'FOREGROUND', 'COLLISION', 'REGIONS')) {
        $Start = [Array]::IndexOf($Lines, $Name) + 1
        Assert-Condition ($Start -gt 0) "Archive$MapIndex is missing $Name."
        $Rows = @()

        foreach ($Y in 0..10) {
            $Row = @($Lines[$Start + $Y].Split(' ', [StringSplitOptions]::RemoveEmptyEntries) | ForEach-Object { [int]$_ })
            if ($Row.Count -ne 13) {
                throw "Archive$MapIndex $Name row $Y contains $($Row.Count) cells instead of 13."
            }

            $Rows += ,$Row
        }

        $Sections[$Name] = $Rows
    }

    $ForegroundCount = @($Sections['FOREGROUND'] | ForEach-Object { $_ } | Where-Object { $_ -ne 0 }).Count
    Assert-Condition ($ForegroundCount -gt 0) "Archive$MapIndex must demonstrate foreground occlusion."
    $Maps[$MapIndex] = $Sections
}

function Get-ReachableCells {
    param(
        [Parameter(Mandatory)] [int] $MapIndex,
        [Parameter(Mandatory)] [int] $StartX,
        [Parameter(Mandatory)] [int] $StartY,
        [Parameter(Mandatory)] [hashtable] $BlockedCells
    )

    $Collision = $Maps[$MapIndex]['COLLISION']
    $Queue = [Collections.Generic.Queue[string]]::new()
    $Visited = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $Queue.Enqueue("$StartX,$StartY")
    [void] $Visited.Add("$StartX,$StartY")

    while ($Queue.Count -ne 0) {
        $Cell = @($Queue.Dequeue().Split(','))
        $X = [int]$Cell[0]
        $Y = [int]$Cell[1]

        foreach ($Delta in @(@(1, 0), @(-1, 0), @(0, 1), @(0, -1))) {
            $NextX = $X + $Delta[0]
            $NextY = $Y + $Delta[1]
            $Key = "$NextX,$NextY"

            if ($NextX -ge 0 -and $NextX -lt 13 -and
                $NextY -ge 0 -and $NextY -lt 11 -and
                $Collision[$NextY][$NextX] -eq 0 -and
                -not $BlockedCells.ContainsKey($Key) -and
                $Visited.Add($Key)) {
                $Queue.Enqueue($Key)
            }
        }
    }

    return $Visited
}

$Endpoints = @(
    @(1, 2, 2, 107),
    @(1, 10, 2, 101),
    @(2, 6, 1, 108),
    @(2, 2, 2, 103),
    @(2, 10, 8, 102),
    @(2, 10, 2, 114),
    @(3, 2, 2, 104),
    @(3, 10, 8, 105),
    @(3, 6, 7, 115),
    @(4, 10, 8, 106)
)

foreach ($Endpoint in $Endpoints) {
    $MapIndex = $Endpoint[0]
    $X = $Endpoint[1]
    $Y = $Endpoint[2]
    $Region = $Endpoint[3]
    Assert-Condition ($Maps[$MapIndex]['REGIONS'][$Y][$X] -eq $Region) "Region $Region is not at its declared floor endpoint."
    Assert-Condition ($Maps[$MapIndex]['COLLISION'][$Y][$X] -eq 0) "Region $Region is not traversable."
}

$ClosedReach = Get-ReachableCells 2 10 8 @{ '6,3' = $true }
$OpenReach = Get-ReachableCells 2 10 8 @{}
Assert-Condition (-not $ClosedReach.Contains('2,2')) 'The closed locked door must partition Archive B2.'
Assert-Condition ($OpenReach.Contains('2,2')) 'Opening the locked door must reconnect Archive B2.'
Assert-Condition ($Maps[2]['COLLISION'][3][6] -eq 0) 'The locked door must cover a traversable base cell.'

$RewardNeighbors = 0
foreach ($Delta in @(@(1, 0), @(-1, 0), @(0, 1), @(0, -1))) {
    if ($Maps[3]['COLLISION'][2 + $Delta[1]][10 + $Delta[0]] -eq 0) {
        $RewardNeighbors++
    }
}

Assert-Condition ($Maps[3]['COLLISION'][2][10] -eq 0) 'The item chest must occupy a traversable base cell.'
Assert-Condition ($RewardNeighbors -eq 1) 'The Archive B3 item chest must terminate a dead-end branch.'
Assert-Condition ($Maps[1]['REGIONS'][8][1] -eq 109 -and $Maps[1]['COLLISION'][8][1] -eq 0) 'The archive entrance exit must remain traversable.'

Write-Host "$Checks Phase 8 dungeon map topology checks passed."
