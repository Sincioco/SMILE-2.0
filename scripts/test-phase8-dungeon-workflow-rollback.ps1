param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$RepositoryRoot = Split-Path -Parent $PSScriptRoot
$Compiler = Join-Path $RepositoryRoot 'artifacts\compiler\smilec.exe'
$WorkflowSource = Join-Path $RepositoryRoot 'games\RPGSystems\DungeonWorkflow.smile'
$ProgramSource = Join-Path $RepositoryRoot 'examples\Phase8DungeonStateTests\Program.smile'
$TestRoot = Join-Path $RepositoryRoot ('artifacts\temp\phase8.1-dungeon-workflow-' + [Guid]::NewGuid().ToString('N'))
$Utf8WithoutBom = [Text.UTF8Encoding]::new($false)

if (-not (Test-Path -LiteralPath $Compiler -PathType Leaf)) {
    throw 'The SMILE compiler must be built before the Phase 8.1 rollback test runs.'
}

function Replace-ExactlyOnce {
    param(
        [Parameter(Mandatory)] [string] $Text,
        [Parameter(Mandatory)] [string] $Needle,
        [Parameter(Mandatory)] [string] $Replacement
    )

    $First = $Text.IndexOf($Needle, [StringComparison]::Ordinal)
    $Last = $Text.LastIndexOf($Needle, [StringComparison]::Ordinal)
    if ($First -lt 0 -or $First -ne $Last) {
        throw "The private fault-injection point was not found exactly once: $Needle"
    }

    return $Text.Remove($First, $Needle.Length).Insert($First, $Replacement)
}

try {
    [IO.Directory]::CreateDirectory($TestRoot) | Out-Null

    $Workflow = [IO.File]::ReadAllText($WorkflowSource)
    $BarrierFunction = $Workflow.IndexOf('Private Function CompleteActorFlag', [StringComparison]::Ordinal)
    $BarrierNeedle = '    If Not Story.SetFlag(StateHandle, FlagId, True) Then'
    $BarrierPoint = $Workflow.IndexOf($BarrierNeedle, $BarrierFunction, [StringComparison]::Ordinal)
    if ($BarrierPoint -lt 0) { throw 'The private barrier fault-injection point was not found.' }
    $Workflow = $Workflow.Remove($BarrierPoint, $BarrierNeedle.Length).Insert(
        $BarrierPoint, '    If Not InjectSetFlag(StateHandle, FlagId, True) Then')

    $GoldFunction = $Workflow.IndexOf('Public Function CompleteGoldChest', [StringComparison]::Ordinal)
    $GoldNeedle = '    If Not Story.SetFlag(StateHandle, FlagId, True) Then'
    $GoldPoint = $Workflow.IndexOf($GoldNeedle, $GoldFunction, [StringComparison]::Ordinal)
    if ($GoldPoint -lt 0) { throw 'The private Gold fault-injection point was not found.' }
    $Workflow = $Workflow.Remove($GoldPoint, $GoldNeedle.Length).Insert(
        $GoldPoint, '    If Not InjectSetFlag(StateHandle, FlagId, True) Then')

    $Workflow = Replace-ExactlyOnce $Workflow `
        '    If Not Inventory.AddItem(StateHandle, TonicItemId, 1) Then' `
        '    If Not InjectTonicItem(StateHandle, TonicItemId, AddKey) Then'

    $TrapFunction = $Workflow.IndexOf('Public Function ApplyOneShotTrap', [StringComparison]::Ordinal)
    $TrapNeedle = '    If Not Story.SetFlag(StateHandle, FlagId, True) Then'
    $TrapPoint = $Workflow.IndexOf($TrapNeedle, $TrapFunction, [StringComparison]::Ordinal)
    if ($TrapPoint -lt 0) { throw 'The private trap fault-injection point was not found.' }
    $Workflow = $Workflow.Remove($TrapPoint, $TrapNeedle.Length).Insert(
        $TrapPoint, '    If Not InjectSetFlag(StateHandle, FlagId, True) Then')

    $Workflow = Replace-ExactlyOnce $Workflow `
        '    If Not Encounters.SetPendingEncounter(StateHandle, EncounterId) Then' `
        '    If Not InjectPendingEncounter(StateHandle, EncounterId) Then'

    $InjectionHelpers = @'
Private Function InjectSetFlag(StateHandle As Number, FlagId As Number, Value As Boolean) As Boolean

    Dim ReturnValue As Boolean

    If FlagId = 8 Or FlagId = 11 Or FlagId = 13 Then
        Return False
    End If

    ReturnValue = Story.SetFlag(StateHandle, FlagId, Value)

    Return ReturnValue

End Function

Private Function InjectTonicItem(StateHandle As Number, ItemId As Number, FailAfterKey As Boolean) As Boolean

    Dim ReturnValue As Boolean

    If FailAfterKey Then
        Return False
    End If

    ReturnValue = Inventory.AddItem(StateHandle, ItemId, 1)

    Return ReturnValue

End Function

Private Function InjectPendingEncounter(StateHandle As Number, EncounterId As Number) As Boolean

    Dim ReturnValue As Boolean

    If EncounterId = 777 Then
        Return False
    End If

    ReturnValue = Encounters.SetPendingEncounter(StateHandle, EncounterId)

    Return ReturnValue

End Function

'@
    $Workflow = Replace-ExactlyOnce $Workflow 'End Module' ($InjectionHelpers + 'End Module')

    [IO.File]::WriteAllText((Join-Path $TestRoot 'DungeonWorkflow.smile'), $Workflow, $Utf8WithoutBom)

    $Program = [IO.File]::ReadAllText($ProgramSource)
    $FaultChecks = @'
Result = DungeonWorkflow.StartNewGame(State, 1, 8)
Call Check(Result = DungeonWorkflow.DUNGEON_RESULT_OK)

Result = DungeonWorkflow.CompleteBarrier(State, ACTOR_TOP_DOOR, FLAG_TOP_DOOR)
Call Check(Result = DungeonWorkflow.DUNGEON_RESULT_APPLY_FAILED)
Call Check(Not Story.Flag(State, FLAG_TOP_DOOR) And World.ActorIsVisible(State, ACTOR_TOP_DOOR))

Result = DungeonWorkflow.CompleteGoldChest(State, ACTOR_TOP_CHEST_GOLD, FLAG_TOP_CHEST_GOLD, 77)
Call Check(Result = DungeonWorkflow.DUNGEON_RESULT_APPLY_FAILED)
Call Check(Party.Gold(State) = 60)
Call Check(Not Story.Flag(State, FLAG_TOP_CHEST_GOLD) And World.ActorIsVisible(State, ACTOR_TOP_CHEST_GOLD))

Result = DungeonWorkflow.CompleteMultiItemChest(State, ACTOR_TOP_CHEST_ITEM, FLAG_TOP_CHEST_ITEM, ITEM_VAULT_KEY, ITEM_SUN_TONIC)
Call Check(Result = DungeonWorkflow.DUNGEON_RESULT_APPLY_FAILED)
Call Check(Inventory.Quantity(State, ITEM_VAULT_KEY) = 0 And Inventory.Quantity(State, ITEM_SUN_TONIC) = 0)
Call Check(Not Story.Flag(State, FLAG_TOP_CHEST_ITEM) And World.ActorIsVisible(State, ACTOR_TOP_CHEST_ITEM))

Result = DungeonWorkflow.ApplyOneShotTrap(State, CHARACTER_HERO, FLAG_TOP_TRAP, 8)
Call Check(Result = DungeonWorkflow.DUNGEON_RESULT_APPLY_FAILED)
Call Check(Characters.Health(State, CHARACTER_HERO) = 140 And Not Story.Flag(State, FLAG_TOP_TRAP))

PreviousScene = World.ReturnScene(State)
PreviousX = World.ReturnX(State)
PreviousY = World.ReturnY(State)
Result = DungeonWorkflow.BeginEncounter(State, ACTOR_HERO, 777)
Call Check(Result = DungeonWorkflow.DUNGEON_RESULT_APPLY_FAILED)
Call Check(Encounters.PendingEncounter(State) = 0)
Call Check(World.ReturnScene(State) = PreviousScene And World.ReturnX(State) = PreviousX And World.ReturnY(State) = PreviousY)

'__FAULT_CHECKS_COMPLETE__

'@
    $DestroyNeedle = 'Call RPG.Destroy(State)'
    $Program = Replace-ExactlyOnce $Program $DestroyNeedle ($FaultChecks + $DestroyNeedle)
    $Program = $Program.Replace('Print "Phase 8 dungeon state tests: PASS"',
        'Print "Phase 8.1 private dungeon rollback tests: PASS"')
    [IO.File]::WriteAllText((Join-Path $TestRoot 'Program.smile'), $Program, $Utf8WithoutBom)

    $Project = @'
<SmileProject Version="1.0">
  <PropertyGroup>
    <ProjectKind>Console</ProjectKind>
    <StartupFile>Program.smile</StartupFile>
    <OutputName>Phase8DungeonWorkflowFaultTests</OutputName>
    <ApplicationId>smile.tests.phase8-dungeon-workflow-fault</ApplicationId>
  </PropertyGroup>
  <ItemGroup>
    <SmileSource Include="Program.smile" StartupOnly="true" />
    <SmileSource Include="DungeonWorkflow.smile" />
    <SmileProjectReference Include="..\..\..\libraries\Smile.RPG\Smile.RPG.smilelibproj" />
  </ItemGroup>
</SmileProject>
'@
    $ProjectPath = Join-Path $TestRoot 'Phase8DungeonWorkflowFaultTests.smileproj'
    [IO.File]::WriteAllText($ProjectPath, $Project, $Utf8WithoutBom)

    $NativeOutput = Join-Path $TestRoot 'Phase8DungeonWorkflowFaultTests.exe'
    $NativeText = Join-Path $TestRoot 'native.out'
    $WebOutput = Join-Path $TestRoot 'web'

    & $Compiler --project $ProjectPath --target windows-x64 --configuration Release -o $NativeOutput
    if ($LASTEXITCODE -ne 0) { throw 'The native Phase 8.1 fault-injection fixture did not compile.' }

    $NativeLines = @(& $NativeOutput)
    if ($LASTEXITCODE -ne 0) { throw 'The native Phase 8.1 fault-injection fixture failed.' }
    [IO.File]::WriteAllLines($NativeText, $NativeLines, $Utf8WithoutBom)
    if ($NativeLines[0] -cne 'Phase 8.1 private dungeon rollback tests: PASS') {
        throw "The native Phase 8.1 fault-injection result was unexpected: $($NativeLines -join ' | ')"
    }

    & $Compiler --project $ProjectPath --target web --configuration Release --output-dir $WebOutput
    if ($LASTEXITCODE -ne 0) { throw 'The Web Phase 8.1 fault-injection fixture did not compile.' }
    & node (Join-Path $RepositoryRoot 'scripts\run-web-test.js') $WebOutput --native-output $NativeText --timeout 10000
    if ($LASTEXITCODE -ne 0) { throw 'The Web Phase 8.1 fault-injection fixture did not match native output.' }

    Write-Host 'Phase 8.1 private post-mutation fault injection and exact native/Web rollback passed.'
}
finally {
    $ArtifactsPrefix = ([IO.Path]::GetFullPath((Join-Path $RepositoryRoot 'artifacts'))).TrimEnd('\', '/') +
        [IO.Path]::DirectorySeparatorChar
    $ResolvedTestRoot = [IO.Path]::GetFullPath($TestRoot)

    if ($ResolvedTestRoot.StartsWith($ArtifactsPrefix, [StringComparison]::OrdinalIgnoreCase) -and
        [IO.Directory]::Exists($ResolvedTestRoot)) {
        Remove-Item -LiteralPath $ResolvedTestRoot -Recurse -Force
    }
}
