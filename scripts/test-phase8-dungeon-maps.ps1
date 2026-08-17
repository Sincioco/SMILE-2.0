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
    $Path = Join-Path $RepositoryRoot "games\RPGSystems\Maps\Dungeon\Archive$MapIndex.smilemap"
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

function Add-ExplorationState {
    param(
        [Parameter(Mandatory)] [AllowEmptyCollection()] [Collections.Generic.Queue[hashtable]] $Queue,
        [Parameter(Mandatory)] [AllowEmptyCollection()] [Collections.Generic.HashSet[string]] $Visited,
        [Parameter(Mandatory)] [int] $Floor,
        [Parameter(Mandatory)] [int] $X,
        [Parameter(Mandatory)] [int] $Y,
        [Parameter(Mandatory)] [bool] $HasKey,
        [Parameter(Mandatory)] [int] $OpenBits
    )

    $StateKey = "$Floor,$X,$Y,$HasKey,$OpenBits"
    if ($Visited.Add($StateKey)) {
        $Queue.Enqueue(@{ Floor = $Floor; X = $X; Y = $Y; HasKey = $HasKey; OpenBits = $OpenBits })
    }
}

function Test-ActorBlocksCell {
    param(
        [Parameter(Mandatory)] [array] $Actors,
        [Parameter(Mandatory)] [int] $Floor,
        [Parameter(Mandatory)] [int] $X,
        [Parameter(Mandatory)] [int] $Y,
        [Parameter(Mandatory)] [int] $OpenBits
    )

    foreach ($Actor in $Actors) {
        if ($Actor.Floor -eq $Floor -and $Actor.X -eq $X -and $Actor.Y -eq $Y -and
            ($Actor.OpenBit -eq 0 -or ($OpenBits -band $Actor.OpenBit) -eq 0)) {
            return $true
        }
    }

    return $false
}

$TopActors = @(
    @{ Name = 'B1 ordinary door'; Floor = 1; X = 6; Y = 8; OpenBit = 1; RequiresKey = $false; GrantsKey = $false },
    @{ Name = 'B2 locked door'; Floor = 2; X = 6; Y = 3; OpenBit = 2; RequiresKey = $true; GrantsKey = $false },
    @{ Name = 'B3 item chest'; Floor = 3; X = 10; Y = 2; OpenBit = 4; RequiresKey = $false; GrantsKey = $true },
    @{ Name = 'B3 hidden passage'; Floor = 3; X = 6; Y = 4; OpenBit = 8; RequiresKey = $false; GrantsKey = $false },
    @{ Name = 'B4 Gold chest'; Floor = 4; X = 3; Y = 3; OpenBit = 16; RequiresKey = $false; GrantsKey = $false },
    @{ Name = 'B4 NPC'; Floor = 4; X = 8; Y = 5; OpenBit = 0; RequiresKey = $false; GrantsKey = $false }
)
$TopTransitions = @(
    @{ Id = 1; Floor = 1; X = 10; Y = 2; ToFloor = 2; ToX = 10; ToY = 8 },
    @{ Id = 2; Floor = 2; X = 10; Y = 8; ToFloor = 1; ToX = 10; ToY = 2 },
    @{ Id = 3; Floor = 2; X = 2; Y = 2; ToFloor = 3; ToX = 2; ToY = 2 },
    @{ Id = 4; Floor = 3; X = 2; Y = 2; ToFloor = 2; ToX = 2; ToY = 2 },
    @{ Id = 5; Floor = 3; X = 10; Y = 8; ToFloor = 4; ToX = 10; ToY = 8 },
    @{ Id = 6; Floor = 4; X = 10; Y = 8; ToFloor = 3; ToX = 10; ToY = 8 },
    @{ Id = 7; Floor = 2; X = 10; Y = 2; ToFloor = 4; ToX = 10; ToY = 8 },
    @{ Id = 8; Floor = 3; X = 6; Y = 7; ToFloor = 1; ToX = 6; ToY = 7 },
    @{ Id = 9; Floor = 1; X = 2; Y = 2; ToFloor = 2; ToX = 6; ToY = 1 },
    @{ Id = 10; Floor = 2; X = 6; Y = 1; ToFloor = 1; ToX = 2; ToY = 2 }
)
$TopQueue = [Collections.Generic.Queue[hashtable]]::new()
$TopVisited = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$TopFloors = [Collections.Generic.HashSet[int]]::new()
$TopInteractions = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$TopTransitionIds = [Collections.Generic.HashSet[int]]::new()
$TopCompleteExit = $false
Add-ExplorationState $TopQueue $TopVisited 1 2 8 $false 0

while ($TopQueue.Count -gt 0) {
    $State = $TopQueue.Dequeue()
    [void] $TopFloors.Add($State.Floor)

    if ($State.Floor -eq 1 -and $State.X -eq 1 -and $State.Y -eq 8 -and
        $State.HasKey -and $State.OpenBits -eq 31) {
        $TopCompleteExit = $true
    }

    foreach ($Actor in $TopActors) {
        $Distance = [math]::Abs($State.X - $Actor.X) + [math]::Abs($State.Y - $Actor.Y)
        if ($Actor.Floor -eq $State.Floor -and $Distance -eq 1) {
            [void] $TopInteractions.Add($Actor.Name)

            if ($Actor.OpenBit -ne 0 -and ($State.OpenBits -band $Actor.OpenBit) -eq 0 -and
                (-not $Actor.RequiresKey -or $State.HasKey)) {
                Add-ExplorationState $TopQueue $TopVisited $State.Floor $State.X $State.Y `
                    ($State.HasKey -or $Actor.GrantsKey) ($State.OpenBits -bor $Actor.OpenBit)
            }
        }
    }

    foreach ($TransitionEdge in $TopTransitions) {
        if ($TransitionEdge.Floor -eq $State.Floor -and $TransitionEdge.X -eq $State.X -and
            $TransitionEdge.Y -eq $State.Y) {
            [void] $TopTransitionIds.Add($TransitionEdge.Id)
            Add-ExplorationState $TopQueue $TopVisited $TransitionEdge.ToFloor $TransitionEdge.ToX `
                $TransitionEdge.ToY $State.HasKey $State.OpenBits
        }
    }

    foreach ($Delta in @(@(1, 0), @(-1, 0), @(0, 1), @(0, -1))) {
        $NextX = $State.X + $Delta[0]
        $NextY = $State.Y + $Delta[1]
        if ($NextX -ge 0 -and $NextX -lt 13 -and $NextY -ge 0 -and $NextY -lt 11 -and
            $Maps[$State.Floor]['COLLISION'][$NextY][$NextX] -eq 0 -and
            -not (Test-ActorBlocksCell $TopActors $State.Floor $NextX $NextY $State.OpenBits)) {
            Add-ExplorationState $TopQueue $TopVisited $State.Floor $NextX $NextY $State.HasKey $State.OpenBits
        }
    }
}

Assert-Condition ($TopFloors.Count -eq 4) 'One legal top-down progression must reach all four archive floors.'
Assert-Condition ($TopTransitionIds.Count -eq 10) 'Every reciprocal, chute, warp, and alternate top-down transition source must be reachable.'
foreach ($Actor in $TopActors) {
    Assert-Condition ($TopInteractions.Contains($Actor.Name)) "$($Actor.Name) must be approachable from a legal progression state."
}
Assert-Condition $TopCompleteExit 'A single top-down route must collect the key, complete every route actor, and return to the exit.'

$FpCollision = @{}
foreach ($Floor in 1..3) {
    $Rows = @()
    foreach ($Y in 0..8) {
        $Row = @()
        foreach ($X in 0..8) {
            $Row += [int]($X -eq 0 -or $X -eq 8 -or $Y -eq 0 -or $Y -eq 8)
        }
        $Rows += ,$Row
    }
    $FpCollision[$Floor] = $Rows
}
foreach ($Y in 2..6) { $FpCollision[1][$Y][4] = 1 }
$FpCollision[1][4][4] = 0
foreach ($X in 2..6) { $FpCollision[2][4][$X] = 1 }
$FpCollision[2][4][2] = 0
$FpCollision[2][4][6] = 0
foreach ($Cell in @(@(2, 2), @(3, 2), @(5, 2), @(6, 2), @(2, 6), @(3, 6), @(5, 6), @(6, 6))) {
    $FpCollision[3][$Cell[1]][$Cell[0]] = 1
}

$FpActors = @(
    @{ Name = 'Prism B1 ordinary door'; Floor = 1; X = 2; Y = 4; OpenBit = 1; RequiresKey = $false; GrantsKey = $false },
    @{ Name = 'Prism B1 Gold chest'; Floor = 1; X = 6; Y = 2; OpenBit = 2; RequiresKey = $false; GrantsKey = $false },
    @{ Name = 'Prism B2 locked door'; Floor = 2; X = 6; Y = 4; OpenBit = 4; RequiresKey = $true; GrantsKey = $false },
    @{ Name = 'Prism B2 key chest'; Floor = 2; X = 2; Y = 2; OpenBit = 8; RequiresKey = $false; GrantsKey = $true },
    @{ Name = 'Prism B3 hidden passage'; Floor = 3; X = 4; Y = 2; OpenBit = 16; RequiresKey = $false; GrantsKey = $false },
    @{ Name = 'Prism B3 NPC'; Floor = 3; X = 6; Y = 3; OpenBit = 0; RequiresKey = $false; GrantsKey = $false }
)
$FpTransitions = @(
    @{ Id = 1; Floor = 1; X = 7; Y = 1; ToFloor = 2; ToX = 1; ToY = 7 },
    @{ Id = 2; Floor = 2; X = 1; Y = 7; ToFloor = 1; ToX = 7; ToY = 1 },
    @{ Id = 3; Floor = 2; X = 7; Y = 1; ToFloor = 3; ToX = 1; ToY = 7 },
    @{ Id = 4; Floor = 3; X = 1; Y = 7; ToFloor = 2; ToX = 7; ToY = 1 },
    @{ Id = 5; Floor = 2; X = 4; Y = 6; ToFloor = 3; ToX = 7; ToY = 6 },
    @{ Id = 6; Floor = 3; X = 7; Y = 7; ToFloor = 1; ToX = 1; ToY = 1 }
)
$FpQueue = [Collections.Generic.Queue[hashtable]]::new()
$FpVisited = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$FpFloors = [Collections.Generic.HashSet[int]]::new()
$FpInteractions = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$FpTransitionIds = [Collections.Generic.HashSet[int]]::new()
$FpCompleteExit = $false
Add-ExplorationState $FpQueue $FpVisited 1 1 7 $false 0

foreach ($TransitionEdge in $FpTransitions) {
    Assert-Condition ($FpCollision[$TransitionEdge.Floor][$TransitionEdge.Y][$TransitionEdge.X] -eq 0) `
        "First-person transition $($TransitionEdge.Id) must start on a traversable cell."
    Assert-Condition ($FpCollision[$TransitionEdge.ToFloor][$TransitionEdge.ToY][$TransitionEdge.ToX] -eq 0) `
        "First-person transition $($TransitionEdge.Id) must land on a traversable cell."
}

while ($FpQueue.Count -gt 0) {
    $State = $FpQueue.Dequeue()
    [void] $FpFloors.Add($State.Floor)

    if ($State.Floor -eq 1 -and $State.X -eq 1 -and $State.Y -eq 7 -and
        $State.HasKey -and $State.OpenBits -eq 31) {
        $FpCompleteExit = $true
    }

    foreach ($Actor in $FpActors) {
        $Distance = [math]::Abs($State.X - $Actor.X) + [math]::Abs($State.Y - $Actor.Y)
        if ($Actor.Floor -eq $State.Floor -and $Distance -eq 1) {
            [void] $FpInteractions.Add($Actor.Name)

            if ($Actor.OpenBit -ne 0 -and ($State.OpenBits -band $Actor.OpenBit) -eq 0 -and
                (-not $Actor.RequiresKey -or $State.HasKey)) {
                Add-ExplorationState $FpQueue $FpVisited $State.Floor $State.X $State.Y `
                    ($State.HasKey -or $Actor.GrantsKey) ($State.OpenBits -bor $Actor.OpenBit)
            }
        }
    }

    foreach ($TransitionEdge in $FpTransitions) {
        if ($TransitionEdge.Floor -eq $State.Floor -and $TransitionEdge.X -eq $State.X -and
            $TransitionEdge.Y -eq $State.Y) {
            [void] $FpTransitionIds.Add($TransitionEdge.Id)
            Add-ExplorationState $FpQueue $FpVisited $TransitionEdge.ToFloor $TransitionEdge.ToX `
                $TransitionEdge.ToY $State.HasKey $State.OpenBits
        }
    }

    foreach ($Delta in @(@(1, 0), @(-1, 0), @(0, 1), @(0, -1))) {
        $NextX = $State.X + $Delta[0]
        $NextY = $State.Y + $Delta[1]
        if ($NextX -ge 0 -and $NextX -lt 9 -and $NextY -ge 0 -and $NextY -lt 9 -and
            $FpCollision[$State.Floor][$NextY][$NextX] -eq 0 -and
            -not (Test-ActorBlocksCell $FpActors $State.Floor $NextX $NextY $State.OpenBits)) {
            Add-ExplorationState $FpQueue $FpVisited $State.Floor $NextX $NextY $State.HasKey $State.OpenBits
        }
    }
}

Assert-Condition ($FpFloors.Count -eq 3) 'One legal first-person progression must reach all three Prism Vault floors.'
Assert-Condition ($FpTransitionIds.Count -eq 6) 'Every first-person stair, chute, and warp source must be reachable.'
foreach ($Actor in $FpActors) {
    Assert-Condition ($FpInteractions.Contains($Actor.Name)) "$($Actor.Name) must be approachable from a legal progression state."
}
Assert-Condition $FpCompleteExit 'A single first-person route must complete every route actor and return to the B1 exit.'

Write-Host "$Checks Phase 8 dungeon map topology checks passed."
