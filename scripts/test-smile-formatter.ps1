param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$RepositoryRoot = Split-Path -Parent $PSScriptRoot
$FormatterSource = Join-Path $PSScriptRoot 'format-smile-style.ps1'
$ReleaseAssembly = Join-Path $RepositoryRoot 'src\Smile.Language\bin\Release\netstandard2.0\Smile.Language.dll'
$DebugAssembly = Join-Path $RepositoryRoot 'src\Smile.Language\bin\Debug\netstandard2.0\Smile.Language.dll'
$LanguageAssembly = if (Test-Path -LiteralPath $ReleaseAssembly) { $ReleaseAssembly } else { $DebugAssembly }
$Utf8WithoutBom = [Text.UTF8Encoding]::new($false)
$TestRoot = Join-Path $env:TEMP ("smile-formatter-tests-" + [Guid]::NewGuid().ToString('N'))
$FormatterPath = Join-Path $TestRoot 'scripts\format-smile-style.ps1'
$Passed = 0

function Assert-True {
    param([bool]$Condition, [string]$Message)

    if (-not $Condition) {
        throw $Message
    }
}

function Assert-Equal {
    param($Expected, $Actual, [string]$Message)

    if ($Expected -cne $Actual) {
        throw "$Message Expected '$Expected', found '$Actual'."
    }
}

function Write-TestSource {
    param([string]$RelativePath, [string]$Text)

    $Path = Join-Path $TestRoot $RelativePath
    [IO.Directory]::CreateDirectory((Split-Path -Parent $Path)) | Out-Null
    [IO.File]::WriteAllText($Path, $Text, $Utf8WithoutBom)
    return $Path
}

function Invoke-Formatter {
    param([string[]]$Arguments)

    $PreviousErrorAction = $ErrorActionPreference

    try {
        $ErrorActionPreference = 'Continue'
        $Output = @(& powershell -NoProfile -ExecutionPolicy Bypass -File $FormatterPath @Arguments 2>&1)
        $ExitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $PreviousErrorAction
    }

    return [pscustomobject]@{ ExitCode = $ExitCode; Output = ($Output -join "`n") }
}

function Invoke-FormatterCommand {
    param([string]$Command)

    $PreviousErrorAction = $ErrorActionPreference

    try {
        $ErrorActionPreference = 'Continue'
        $Output = @(& powershell -NoProfile -ExecutionPolicy Bypass -Command $Command 2>&1)
        $ExitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $PreviousErrorAction
    }

    return [pscustomobject]@{ ExitCode = $ExitCode; Output = ($Output -join "`n") }
}

function Pass {
    param([string]$Name)

    $script:Passed++
    Write-Host "PASS: $Name"
}

if (-not (Test-Path -LiteralPath $LanguageAssembly)) {
    throw 'Smile.Language must be built before the formatter integration tests run.'
}

try {
    [IO.Directory]::CreateDirectory((Join-Path $TestRoot 'scripts')) | Out-Null
    Copy-Item -LiteralPath $FormatterSource -Destination $FormatterPath

    & git -C $TestRoot init --quiet
    if ($LASTEXITCODE -ne 0) { throw 'Unable to initialize the temporary formatter Git fixture.' }

    Write-TestSource 'Tracked.smile' "Option Explicit`n" | Out-Null
    & git -C $TestRoot add -- Tracked.smile
    if ($LASTEXITCODE -ne 0) { throw 'Unable to track the formatter fixture.' }

    $TrackedPath = Join-Path $TestRoot 'Tracked.smile'
    $TrackedBytes = [IO.File]::ReadAllBytes($TrackedPath)
    $TrackedTimestamp = [IO.File]::GetLastWriteTimeUtc($TrackedPath)
    $MissingCheck = Invoke-Formatter @('-Check', '-FormatLongIf')
    Assert-True ($MissingCheck.ExitCode -ne 0 -and $MissingCheck.Output.Contains('Run scripts\build.cmd first')) 'Missing assembly Check did not fail with the build-first instruction.'
    Assert-Equal ([Convert]::ToBase64String($TrackedBytes)) ([Convert]::ToBase64String([IO.File]::ReadAllBytes($TrackedPath))) 'Missing assembly Check changed source bytes.'
    Assert-Equal $TrackedTimestamp ([IO.File]::GetLastWriteTimeUtc($TrackedPath)) 'Missing assembly Check changed the source timestamp.'
    Assert-Equal 0 @(Get-ChildItem -LiteralPath $TestRoot -Recurse -Directory | Where-Object { $_.Name -in @('bin', 'obj') }).Count 'Missing assembly Check created bin or obj.'

    $AssemblyDirectory = Join-Path $TestRoot 'src\Smile.Language\bin\Debug\netstandard2.0'
    [IO.Directory]::CreateDirectory($AssemblyDirectory) | Out-Null
    $TestAssembly = Join-Path $AssemblyDirectory 'Smile.Language.dll'
    Copy-Item -LiteralPath $LanguageAssembly -Destination $TestAssembly
    $StaleMarker = Write-TestSource 'src\Smile.Language\FormatterMarker.cs' '// stale dependency marker'
    [IO.File]::SetLastWriteTimeUtc($StaleMarker, [IO.File]::GetLastWriteTimeUtc($TestAssembly).AddSeconds(2))
    $StaleFilesBefore = @(Get-ChildItem -LiteralPath $TestRoot -Recurse -File | Select-Object -ExpandProperty FullName)
    $StaleCheck = Invoke-Formatter @('-Check', '-FormatLongIf')
    Assert-True ($StaleCheck.ExitCode -ne 0 -and $StaleCheck.Output.Contains('Run scripts\build.cmd first')) 'Stale assembly Check did not fail with the build-first instruction.'
    $StaleFilesAfter = @(Get-ChildItem -LiteralPath $TestRoot -Recurse -File | Select-Object -ExpandProperty FullName)
    Assert-Equal ($StaleFilesBefore -join '|') ($StaleFilesAfter -join '|') 'Stale assembly Check created a file.'
    Assert-Equal 0 @(Get-ChildItem -LiteralPath $TestRoot -Recurse -Directory | Where-Object { $_.Name -eq 'obj' }).Count 'Stale assembly Check created obj.'
    Assert-Equal ([Convert]::ToBase64String($TrackedBytes)) ([Convert]::ToBase64String([IO.File]::ReadAllBytes($TrackedPath))) 'Stale assembly Check changed source bytes.'
    Assert-Equal $TrackedTimestamp ([IO.File]::GetLastWriteTimeUtc($TrackedPath)) 'Stale assembly Check changed the source timestamp.'
    Remove-Item -LiteralPath $StaleMarker -Force
    Pass 'missing and stale Check fail build-first without source or build side effects'

    $Untracked = Write-TestSource 'Untracked.smile' "Option Explicit`n`nFunction Add() As Number`n`n    Return 1 + 2`n`nEnd Function`n"
    $DefaultCheck = Invoke-Formatter @('-Check', '-FormatLongIf')
    Assert-Equal 0 $DefaultCheck.ExitCode 'Default scope did not ignore an untracked .smile file.'
    $IncludeCheck = Invoke-Formatter @('-Check', '-FormatLongIf', '-IncludeUntracked')
    Assert-True ($IncludeCheck.ExitCode -ne 0) '-IncludeUntracked did not include formatter drift.'
    $ExplicitCheck = Invoke-Formatter @('-Check', '-FormatLongIf', '-Files', 'Untracked.smile')
    Assert-True ($ExplicitCheck.ExitCode -ne 0) '-Files did not explicitly target an untracked file.'
    $ExplicitFormat = Invoke-Formatter @('-FormatLongIf', '-Files', 'Untracked.smile')
    Assert-Equal 0 $ExplicitFormat.ExitCode ("Explicit untracked formatting failed: " + $ExplicitFormat.Output)
    $DuplicateFormat = Invoke-FormatterCommand ("& '" + $FormatterPath + "' -FormatLongIf -Files @('Untracked.smile','Untracked.smile')")
    Assert-Equal 0 $DuplicateFormat.ExitCode ("Duplicate explicit formatting failed: " + $DuplicateFormat.Output)
    Assert-True ($DuplicateFormat.Output.Contains('of 1 SMILE file(s).')) 'Duplicate explicit targets were not deduplicated.'
    Assert-Equal 0 (Invoke-Formatter @('-Check', '-FormatLongIf', '-Files', 'Untracked.smile')).ExitCode 'Explicitly formatted source did not pass Check.'
    Assert-True ((Invoke-Formatter @('-Check', '-Files', 'Missing.smile')).ExitCode -ne 0) 'A missing explicit file did not fail clearly.'
    Pass 'tracked, untracked, explicit, missing, and deduplicated scope behavior'

    $Drift = Write-TestSource 'Drift.smile' "Option Explicit`n`nFunction Multiply() As Number`n`n    Return 2 * 3`n`nEnd Function`n"
    & git -C $TestRoot add -- Drift.smile
    $BeforeBytes = [IO.File]::ReadAllBytes($Drift)
    $BeforeTimestamp = [IO.File]::GetLastWriteTimeUtc($Drift)
    $BeforeFiles = @(Get-ChildItem -LiteralPath $TestRoot -Recurse -File | Select-Object -ExpandProperty FullName)
    $DriftCheck = Invoke-Formatter @('-Check', '-FormatLongIf')
    Assert-True ($DriftCheck.ExitCode -ne 0) 'Check did not report tracked formatter drift.'
    Assert-Equal ([Convert]::ToBase64String($BeforeBytes)) ([Convert]::ToBase64String([IO.File]::ReadAllBytes($Drift))) 'Check changed source bytes.'
    Assert-Equal $BeforeTimestamp ([IO.File]::GetLastWriteTimeUtc($Drift)) 'Check changed the source timestamp.'
    $AfterFiles = @(Get-ChildItem -LiteralPath $TestRoot -Recurse -File | Select-Object -ExpandProperty FullName)
    Assert-Equal ($BeforeFiles -join '|') ($AfterFiles -join '|') 'Check generated a file.'
    Assert-Equal 0 (Invoke-Formatter @('-FormatLongIf', '-Files', 'Drift.smile')).ExitCode 'Formatting tracked drift failed.'
    Assert-Equal 0 (Invoke-Formatter @('-Check', '-FormatLongIf')).ExitCode 'Repository style gate did not pass after formatting drift.'
    Pass 'read-only Check failure, restoration, bytes, timestamps, and generated-file safety'

    $MultilineRoutineSource = "Option Explicit`n" +
        "Function Add(`n" +
        "    FirstValue As Number,`n" +
        "    SecondValue As Number`n" +
        ")`n" +
        "    Return FirstValue + SecondValue`n" +
        "End Function`n" +
        "Sub Present(`n" +
        "    Value As Number`n" +
        ")`n" +
        "    Print Value`n" +
        "End Sub`n"
    $MultilineRoutinePath = Write-TestSource 'MultilineRoutine.smile' $MultilineRoutineSource
    Assert-Equal 0 (Invoke-Formatter @('-FormatLongIf', '-Files', 'MultilineRoutine.smile')).ExitCode 'Multiline routine formatting failed.'
    $MultilineRoutineFormatted = [IO.File]::ReadAllText($MultilineRoutinePath)
    Assert-True (-not $MultilineRoutineFormatted.Contains("Function Add(`n`n")) 'Function opening line received a premature blank line.'
    Assert-True ($MultilineRoutineFormatted.Contains(
        "Function Add(`n    FirstValue As Number,`n    SecondValue As Number`n)`n`n    Dim ReturnValue As Number`n`n")) 'Computed Return local was not placed after the complete multiline Function header.'
    Assert-True ($MultilineRoutineFormatted.Contains(
        "Sub Present(`n    Value As Number`n)`n`n    Print Value")) 'Sub header did not receive exactly one trailing blank line.'
    $MultilineRoutineFirstPass = [IO.File]::ReadAllBytes($MultilineRoutinePath)
    Assert-Equal 0 (Invoke-Formatter @('-FormatLongIf', '-Files', 'MultilineRoutine.smile')).ExitCode 'Multiline routine second formatting pass failed.'
    $MultilineRoutineSecondPass = [IO.File]::ReadAllBytes($MultilineRoutinePath)
    Assert-Equal ([Convert]::ToBase64String($MultilineRoutineFirstPass)) ([Convert]::ToBase64String($MultilineRoutineSecondPass)) 'Multiline routine formatting was not idempotent.'
    Pass 'multiline routine headers, computed Returns, blank lines, and idempotence'

    $OptionalNamedSource = "Option Explicit`n" +
        "Sub Present(`n" +
        "    Value As Number,`n" +
        "    Optional left As Number = 2,`n" +
        "    Optional Caption As Text = `"ready`"`n" +
        ")`n" +
        "    Print Value, left, Caption`n" +
        "End Sub`n" +
        "Call Present(Caption := `"named`", Value := 1)`n"
    $OptionalNamedPath = Write-TestSource 'OptionalNamedFormatting.smile' $OptionalNamedSource
    Assert-Equal 0 (Invoke-Formatter @('-FormatLongIf', '-Files', 'OptionalNamedFormatting.smile')).ExitCode 'Optional/named formatting failed.'
    $OptionalNamedFormatted = [IO.File]::ReadAllText($OptionalNamedPath)
    Assert-True ($OptionalNamedFormatted.Contains('Optional Left As Number = 2')) 'Contextual Optional parameter casing was not canonicalized.'
    Assert-True ($OptionalNamedFormatted.Contains('Call Present(Caption:="named", Value:=1)')) 'Named argument operators were not formatted as Name:=Value.'
    $OptionalNamedFirstPass = [IO.File]::ReadAllBytes($OptionalNamedPath)
    Assert-Equal 0 (Invoke-Formatter @('-FormatLongIf', '-Files', 'OptionalNamedFormatting.smile')).ExitCode 'Optional/named second formatting pass failed.'
    $OptionalNamedSecondPass = [IO.File]::ReadAllBytes($OptionalNamedPath)
    Assert-Equal ([Convert]::ToBase64String($OptionalNamedFirstPass)) ([Convert]::ToBase64String($OptionalNamedSecondPass)) 'Optional/named formatting was not idempotent.'
    Pass 'Optional declaration traversal, named argument spacing, contextual casing, and idempotence'

    $TypeMemberSource = "Option Explicit`n" +
        "Type Counter`n" +
        "    StoredValue As Number`n" +
        "    Public Function Difference(Optional left As Number = 1) As Number`n" +
        "        Return me.StoredValue + left`n" +
        "    End Function`n" +
        "    Public Property Total As Number`n" +
        "        Get`n" +
        "            Return me.StoredValue + 1`n" +
        "        End Get`n" +
        "        Set`n" +
        "            me.StoredValue = value`n" +
        "        End Set`n" +
        "    End Property`n" +
        "End Type`n"
    $TypeMemberPath = Write-TestSource 'TypeMemberFormatting.smile' $TypeMemberSource
    Assert-Equal 0 (Invoke-Formatter @('-FormatLongIf', '-Files', 'TypeMemberFormatting.smile')).ExitCode 'Type-member formatting failed.'
    $TypeMemberFormatted = [IO.File]::ReadAllText($TypeMemberPath)
    Assert-True ($TypeMemberFormatted.Contains('Optional Left As Number = 1')) 'Type-method parameter casing was not canonicalized.'
    Assert-True ($TypeMemberFormatted.Contains('ReturnValue = Me.StoredValue + Left')) 'Type-method computed Return and Me/parameter casing were not formatted.'
    Assert-True ($TypeMemberFormatted.Contains("Get`n`n            Dim ReturnValue As Number")) 'Property getter computed Return local was not placed after Get.'
    Assert-True ($TypeMemberFormatted.Contains('ReturnValue = Me.StoredValue + 1')) 'Property getter computed Return was not rewritten.'
    Assert-True ($TypeMemberFormatted.Contains('Me.StoredValue = Value')) 'Property setter Me/Value casing was not canonicalized.'
    $TypeMemberFirstPass = [IO.File]::ReadAllBytes($TypeMemberPath)
    Assert-Equal 0 (Invoke-Formatter @('-FormatLongIf', '-Files', 'TypeMemberFormatting.smile')).ExitCode 'Type-member second formatting pass failed.'
    $TypeMemberSecondPass = [IO.File]::ReadAllBytes($TypeMemberPath)
    Assert-Equal ([Convert]::ToBase64String($TypeMemberFirstPass)) ([Convert]::ToBase64String($TypeMemberSecondPass)) 'Type-member formatting was not idempotent.'
    Pass 'Type methods, property accessors, Me/Value casing, computed Returns, and idempotence'

    $ClassMemberSource = "Option Explicit`n" +
        "Class Counter`n" +
        "    Private StoredValue As Number`n" +
        "    Public Sub New(Optional left As Number = 1)`n" +
        "        me.StoredValue = left`n" +
        "    End Sub`n" +
        "    Public Function Difference(Optional left As Number = 1) As Number`n" +
        "        Return me.StoredValue + left`n" +
        "    End Function`n" +
        "    Public Property Total As Number`n" +
        "        Get`n" +
        "            Return me.StoredValue + 1`n" +
        "        End Get`n" +
        "        Set`n" +
        "            me.StoredValue = value`n" +
        "        End Set`n" +
        "    End Property`n" +
        "End Class`n" +
        "Dim Current As New Counter(left := 2)`n" +
        "Print Current Is Not Nothing`n"
    $ClassMemberPath = Write-TestSource 'ClassMemberFormatting.smile' $ClassMemberSource
    Assert-Equal 0 (Invoke-Formatter @('-FormatLongIf', '-Files', 'ClassMemberFormatting.smile')).ExitCode 'Class-member formatting failed.'
    $ClassMemberFormatted = [IO.File]::ReadAllText($ClassMemberPath)
    Assert-True ($ClassMemberFormatted.Contains('Optional Left As Number = 1')) 'Constructor parameter casing was not canonicalized.'
    Assert-True ($ClassMemberFormatted.Contains('Me.StoredValue = Left')) 'Constructor Me/parameter casing was not canonicalized.'
    Assert-True ($ClassMemberFormatted.Contains('ReturnValue = Me.StoredValue + Left')) 'Class Function computed Return was not rewritten.'
    Assert-True ($ClassMemberFormatted.Contains("Get`n`n            Dim ReturnValue As Number")) 'Class Property getter computed Return local was not placed after Get.'
    Assert-True ($ClassMemberFormatted.Contains('Me.StoredValue = Value')) 'Class Property setter Me/Value casing was not canonicalized.'
    Assert-True ($ClassMemberFormatted.Contains('New Counter(Left:=2)')) 'Class constructor named argument was not canonicalized.'
    Assert-True ($ClassMemberFormatted.Contains('Is Not Nothing')) 'Class identity syntax did not survive formatting.'
    $ClassMemberFirstPass = [IO.File]::ReadAllBytes($ClassMemberPath)
    Assert-Equal 0 (Invoke-Formatter @('-FormatLongIf', '-Files', 'ClassMemberFormatting.smile')).ExitCode 'Class-member second formatting pass failed.'
    $ClassMemberSecondPass = [IO.File]::ReadAllBytes($ClassMemberPath)
    Assert-Equal ([Convert]::ToBase64String($ClassMemberFirstPass)) ([Convert]::ToBase64String($ClassMemberSecondPass)) 'Class-member formatting was not idempotent.'
    Pass 'Class constructors, methods, properties, New/Is Not, contextual casing, and idempotence'

    $EnumSource = "Option Explicit`n`n" +
        "Enum Direction`n    none`n    up = 10`n    down`n    left = -5`n    right = -5`nEnd Enum`n`n" +
        "Function DefaultDirection() As Direction`n`n    Return Direction.left`n`nEnd Function`n"
    $EnumPath = Write-TestSource 'EnumFormatting.smile' $EnumSource
    Assert-Equal 0 (Invoke-Formatter @('-FormatLongIf', '-Files', 'EnumFormatting.smile')).ExitCode 'Enum formatting failed.'
    $EnumFormatted = [IO.File]::ReadAllText($EnumPath)
    Assert-True ($EnumFormatted.Contains("Enum Direction`n    None`n    Up = 10`n    Down`n    Left = -5`n    Right = -5`nEnd Enum")) 'Enum contextual member names were not canonicalized through the declaration.'
    Assert-True ($EnumFormatted.Contains('Return Direction.Left')) 'Enum member use was not canonicalized.'
    Assert-True (-not $EnumFormatted.Contains('ReturnValue = Direction.Left')) 'A direct Enum member Return received an unnecessary temporary.'
    $EnumFirstPass = [IO.File]::ReadAllBytes($EnumPath)
    Assert-Equal 0 (Invoke-Formatter @('-FormatLongIf', '-Files', 'EnumFormatting.smile')).ExitCode 'Enum second formatting pass failed.'
    $EnumSecondPass = [IO.File]::ReadAllBytes($EnumPath)
    Assert-Equal ([Convert]::ToBase64String($EnumFirstPass)) ([Convert]::ToBase64String($EnumSecondPass)) 'Enum formatting was not idempotent.'
    Pass 'Enum declaration traversal, contextual members, direct Returns, and idempotence'

    $ClipSource = "Option Explicit`n`nGame Window `"Clip Formatter`"`n`n" +
        "Function Calculate(Value As Number) As Number`n`n" +
        "    Clip Rectangle 0, 0, 100, 100`n" +
        "        If (Value < 0`n            Or Value > 100`n            Or Value = 50) Then`n" +
        "            Return Value + 1`n        End If`n    End Clip`n`n" +
        "    Return 0`n`nEnd Function`n"
    $ClipPath = Write-TestSource 'ClipTraversal.smile' $ClipSource
    Assert-Equal 0 (Invoke-Formatter @('-FormatLongIf', '-Files', 'ClipTraversal.smile')).ExitCode 'Nested Clip formatting failed.'
    $ClipFormatted = [IO.File]::ReadAllText($ClipPath)
    Assert-True ($ClipFormatted.Contains("If (Value < 0 Or`n            Value > 100 Or`n            Value = 50) Then")) 'Long If inside Clip was not syntax-formatted.'
    Assert-True ($ClipFormatted.Contains('ReturnValue = Value + 1')) 'Computed Return inside Clip was not rewritten.'

    $WithSource = "Option Explicit`n`n" +
        "Type Holder`n    Value As Number`nEnd Type`n`nDim Current As Holder`n`n" +
        "Function Calculate(Value As Holder) As Number`n`n" +
        "    With Value`n" +
        "        If (.Value < 0`n            Or .Value > 100`n            Or .Value = 50) Then`n" +
        "            Return .Value + 1`n        End If`n    End With`n`n" +
        "    Return 0`n`nEnd Function`n`n" +
        "If True Then`n    With Current`n        .Value = 1`n    End With`nEnd If`n"
    $WithPath = Write-TestSource 'WithTraversal.smile' $WithSource
    Assert-Equal 0 (Invoke-Formatter @('-FormatLongIf', '-Files', 'WithTraversal.smile')).ExitCode 'Nested With formatting failed.'
    $WithFormatted = [IO.File]::ReadAllText($WithPath)
    Assert-True ($WithFormatted.Contains("If (.Value < 0 Or`n            .Value > 100 Or`n            .Value = 50) Then")) 'Long If inside With was not syntax-formatted.'
    Assert-True ($WithFormatted.Contains('ReturnValue = .Value + 1')) 'Computed Return inside With was not rewritten.'
    Assert-True ($WithFormatted.Contains("If True Then`n`n    With Current")) 'An If containing With was not expanded as nested control flow.'
    Assert-True ($WithFormatted.Contains("    End With`n`nEnd If")) 'The expanded If did not preserve its End With boundary.'
    $WithFirstPass = [IO.File]::ReadAllBytes($WithPath)
    Assert-Equal 0 (Invoke-Formatter @('-FormatLongIf', '-Files', 'WithTraversal.smile')).ExitCode 'Nested With second formatting pass failed.'
    $WithSecondPass = [IO.File]::ReadAllBytes($WithPath)
    Assert-Equal ([Convert]::ToBase64String($WithFirstPass)) ([Convert]::ToBase64String($WithSecondPass)) 'Nested With formatting was not idempotent.'

    $CompactSource = "Option Explicit`r`n`r`nDim FirstCondition As Boolean`r`nDim SecondCondition As Boolean`r`n" +
        "Dim ThirdCondition As Boolean`r`n`r`nIf (FirstCondition`r`n    Or SecondCondition`r`n    Or ThirdCondition) Then`r`n`r`n" +
        "    Print `"Matched`"`r`n`r`nEnd If`r`n"
    $CompactPath = Write-TestSource 'MultilineCompact.smile' $CompactSource
    Assert-Equal 0 (Invoke-Formatter @('-FormatLongIf', '-Files', 'MultilineCompact.smile')).ExitCode 'Compact multiline If formatting failed.'
    $CompactExpected = "Option Explicit`n`nDim FirstCondition As Boolean`nDim SecondCondition As Boolean`n" +
        "Dim ThirdCondition As Boolean`n`nIf (FirstCondition Or`n    SecondCondition Or`n    ThirdCondition) Then`n" +
        "    Print `"Matched`"`nEnd If`n"
    Assert-Equal $CompactExpected ([IO.File]::ReadAllText($CompactPath)) 'Compact multiline If layout differed.'

    $ExpandedSource = "Option Explicit`n`nDim FirstCondition As Boolean`nDim SecondCondition As Boolean`n" +
        "Dim ThirdCondition As Boolean`nDim FourthCondition As Boolean`nDim FifthCondition As Boolean`n" +
        "Dim SixthCondition As Boolean`nDim Value As Number`nDim Total As Number`nDim Ready As Boolean`n`n" +
        "If (FirstCondition`n    Or SecondCondition`n    Or ThirdCondition) Then`n" +
        "    Value = 1`n    Total = Total + 1`n    Ready = True`n" +
        "Else If (FourthCondition`n    Or FifthCondition`n    Or SixthCondition) Then`n" +
        "    Value = 2`n    Total = Total + 1`n    Ready = False`nEnd If`n"
    $ExpandedPath = Write-TestSource 'MultilineExpanded.smile' $ExpandedSource
    Assert-Equal 0 (Invoke-Formatter @('-FormatLongIf', '-Files', 'MultilineExpanded.smile')).ExitCode 'Expanded multiline If formatting failed.'
    $ExpandedFormatted = [IO.File]::ReadAllText($ExpandedPath)
    Assert-True ($ExpandedFormatted.Contains("ThirdCondition) Then`n`n    Value = 1")) 'Expanded If header did not receive a blank line.'
    Assert-True ($ExpandedFormatted.Contains("Ready = True`n`nElse If (FourthCondition Or")) 'Expanded Else If boundary did not receive a blank line.'
    Assert-True ($ExpandedFormatted.Contains("SixthCondition) Then`n`n    Value = 2")) 'Expanded Else If header did not receive a blank line.'
    Assert-True ($ExpandedFormatted.Contains("Ready = False`n`nEnd If")) 'Expanded End If boundary did not receive a blank line.'

    Write-TestSource 'Provider\Values.smile' "Module Example.Values`n`nOption Explicit`n`nPublic Const UI_EVENT_NONE = 0`nPublic Dim DefaultValue As Number`nPublic Dim Items[2] As Number`n`nPublic Type Holder`n    Value As Number`nEnd Type`n`nPublic Dim Current As Holder`n`nPublic Function CreateValue() As Number`n`n    Return 1`n`nEnd Function`n`nEnd Module`n" | Out-Null
    Write-TestSource 'Provider\Provider.smilelibproj' '<SmileProject><PropertyGroup><ProjectKind>Library</ProjectKind><LibraryName>Example.Provider</LibraryName><Version>1.0.0</Version></PropertyGroup><ItemGroup><SmileSource Include="Values.smile" /></ItemGroup></SmileProject>' | Out-Null
    $QualifiedPath = Write-TestSource 'Consumer\Consumer.smile' "Module Example.Consumer`n`nOption Explicit`n`nImport Example.Values As Values`n`nPublic Function ConstantValue() As Number`n`n    Return Values.UI_EVENT_NONE`n`nEnd Function`n`nPublic Function ModuleValue() As Number`n`n    Return Values.DefaultValue`n`nEnd Function`n`nPublic Function FieldValue() As Number`n`n    Return Values.Current.Value`n`nEnd Function`n`nPublic Function ArrayValue() As Number`n`n    Return Values.Items[0]`n`nEnd Function`n`nPublic Function CallValue() As Number`n`n    Return Values.CreateValue()`n`nEnd Function`n`nEnd Module`n"
    Write-TestSource 'Consumer\Consumer.smilelibproj' '<SmileProject><PropertyGroup><ProjectKind>Library</ProjectKind><LibraryName>Example.Consumer</LibraryName><Version>1.0.0</Version></PropertyGroup><ItemGroup><SmileSource Include="Consumer.smile" /><SmileProjectReference Include="..\Provider\Provider.smilelibproj" /></ItemGroup></SmileProject>' | Out-Null
    & git -C $TestRoot add -- Provider Consumer
    if ($LASTEXITCODE -ne 0) { throw 'Unable to track the qualified Return project fixture.' }
    Assert-Equal 0 (Invoke-Formatter @('-FormatLongIf', '-Files', 'Consumer\Consumer.smile')).ExitCode 'Qualified Return project formatting failed.'
    $QualifiedFormatted = [IO.File]::ReadAllText($QualifiedPath)
    Assert-True ($QualifiedFormatted.Contains('Return Values.UI_EVENT_NONE')) 'Imported public constant did not remain a direct Return.'
    Assert-True ($QualifiedFormatted.Contains('Return Values.DefaultValue')) 'Imported public module variable did not remain a direct Return.'
    foreach ($Expression in @('Values.Current.Value', 'Values.Items[0]', 'Values.CreateValue()')) {
        Assert-True ($QualifiedFormatted.Contains('ReturnValue = ' + $Expression)) "Evaluated Return '$Expression' did not receive an intermediate variable."
    }
    $QualifiedFirstPass = [IO.File]::ReadAllBytes($QualifiedPath)
    Assert-Equal 0 (Invoke-Formatter @('-FormatLongIf', '-Files', 'Consumer\Consumer.smile')).ExitCode 'Qualified Return second pass failed.'
    $QualifiedSecondPass = [IO.File]::ReadAllBytes($QualifiedPath)
    Assert-Equal ([Convert]::ToBase64String($QualifiedFirstPass)) ([Convert]::ToBase64String($QualifiedSecondPass)) 'Qualified Return formatting was not idempotent.'
    Pass 'Clip and With traversal, multiline If layout, and symbol-aware qualified Returns'

    Write-TestSource 'Context\TrackedProvider\Values.smile' "Module Context.Values`n`nOption Explicit`n`nPublic Dim Current As Number`n`nEnd Module`n" | Out-Null
    Write-TestSource 'Context\TrackedProvider\Provider.smilelibproj' '<SmileProject><PropertyGroup><ProjectKind>Library</ProjectKind><LibraryName>Context.TrackedProvider</LibraryName><Version>1.0.0</Version></PropertyGroup><ItemGroup><SmileSource Include="Values.smile" /></ItemGroup></SmileProject>' | Out-Null
    $ContextShared = Write-TestSource 'Context\Shared.smile' "Module Context.Consumer`n`nOption Explicit`n`nImport Context.Values As Values`n`nPublic Function ReadValue() As Number`n`n    Return Values.Current`n`nEnd Function`n`nEnd Module`n"
    Write-TestSource 'Context\ZTracked.smilelibproj' '<SmileProject><PropertyGroup><ProjectKind>Library</ProjectKind><LibraryName>Context.Tracked</LibraryName><Version>1.0.0</Version></PropertyGroup><ItemGroup><SmileSource Include="Shared.smile" /><SmileProjectReference Include="TrackedProvider\Provider.smilelibproj" /></ItemGroup></SmileProject>' | Out-Null
    & git -C $TestRoot add -- Context/Shared.smile Context/ZTracked.smilelibproj Context/TrackedProvider
    if ($LASTEXITCODE -ne 0) { throw 'Unable to track the formatter project-context fixture.' }

    Write-TestSource 'Context\UntrackedProvider\Values.smile' "Module Context.Values`n`nOption Explicit`n`nPublic Function Current() As Number`n`n    Return 2`n`nEnd Function`n`nEnd Module`n" | Out-Null
    Write-TestSource 'Context\UntrackedProvider\Provider.smilelibproj' '<SmileProject><PropertyGroup><ProjectKind>Library</ProjectKind><LibraryName>Context.UntrackedProvider</LibraryName><Version>1.0.0</Version></PropertyGroup><ItemGroup><SmileSource Include="Values.smile" /></ItemGroup></SmileProject>' | Out-Null
    Write-TestSource 'Context\AUntracked.smilelibproj' '<SmileProject><PropertyGroup><ProjectKind>Library</ProjectKind><LibraryName>Context.Untracked</LibraryName><Version>1.0.0</Version></PropertyGroup><ItemGroup><SmileSource Include="Shared.smile" /><SmileProjectReference Include="UntrackedProvider\Provider.smilelibproj" /></ItemGroup></SmileProject>' | Out-Null

    Assert-Equal 0 (Invoke-Formatter @('-FormatLongIf', '-Files', 'Context\Shared.smile')).ExitCode 'Explicit tracked project-context formatting failed.'
    $TrackedContextText = [IO.File]::ReadAllText($ContextShared)
    Assert-True ($TrackedContextText.Contains('Return Values.Current') -and
        -not $TrackedContextText.Contains('ReturnValue = Values.Current')) "An untracked project influenced explicit tracked source formatting: $TrackedContextText"
    $TrackedContextBytes = [IO.File]::ReadAllBytes($ContextShared)
    Assert-Equal 0 (Invoke-Formatter @('-FormatLongIf', '-Files', 'Context\Shared.smile', '-IncludeUntracked')).ExitCode 'IncludeUntracked project-context formatting failed.'
    Assert-Equal ([Convert]::ToBase64String($TrackedContextBytes)) ([Convert]::ToBase64String([IO.File]::ReadAllBytes($ContextShared))) 'Tracked owner precedence changed when untracked contexts were enabled.'

    $OnlyUntracked = Write-TestSource 'Context\OnlyUntracked.smile' "Module Context.NewConsumer`n`nOption Explicit`n`nImport Context.Values As Values`n`nPublic Function ReadValue() As Number`n`n    Return Values.Current`n`nEnd Function`n`nEnd Module`n"
    Write-TestSource 'Context\OnlyUntracked.smilelibproj' '<SmileProject><PropertyGroup><ProjectKind>Library</ProjectKind><LibraryName>Context.OnlyUntracked</LibraryName><Version>1.0.0</Version></PropertyGroup><ItemGroup><SmileSource Include="OnlyUntracked.smile" /><SmileProjectReference Include="UntrackedProvider\Provider.smilelibproj" /></ItemGroup></SmileProject>' | Out-Null
    Assert-Equal 0 (Invoke-Formatter @('-FormatLongIf', '-Files', 'Context\OnlyUntracked.smile')).ExitCode 'Explicit untracked owning context formatting failed.'
    Assert-True (([IO.File]::ReadAllText($OnlyUntracked)).Contains('ReturnValue = Values.Current')) 'An explicit untracked source did not use its owning untracked project context.'

    Write-TestSource 'Context\FirstProvider\Values.smile' "Module Context.Values`n`nOption Explicit`n`nPublic Function Current() As Number`n`n    Return 3`n`nEnd Function`n`nEnd Module`n" | Out-Null
    Write-TestSource 'Context\FirstProvider\Provider.smilelibproj' '<SmileProject><PropertyGroup><ProjectKind>Library</ProjectKind><LibraryName>Context.FirstProvider</LibraryName><Version>1.0.0</Version></PropertyGroup><ItemGroup><SmileSource Include="Values.smile" /></ItemGroup></SmileProject>' | Out-Null
    Write-TestSource 'Context\AFirstOwner.smilelibproj' '<SmileProject><PropertyGroup><ProjectKind>Library</ProjectKind><LibraryName>Context.FirstOwner</LibraryName><Version>1.0.0</Version></PropertyGroup><ItemGroup><SmileSource Include="Shared.smile" /><SmileProjectReference Include="FirstProvider\Provider.smilelibproj" /></ItemGroup></SmileProject>' | Out-Null
    & git -C $TestRoot add -- Context/AFirstOwner.smilelibproj Context/FirstProvider
    if ($LASTEXITCODE -ne 0) { throw 'Unable to track the multiple-owner formatter fixture.' }
    Assert-Equal 0 (Invoke-Formatter @('-FormatLongIf', '-Files', 'Context\Shared.smile')).ExitCode 'Multiple tracked-owner formatting failed.'
    Assert-True (([IO.File]::ReadAllText($ContextShared)).Contains('ReturnValue = Values.Current')) 'Tracked project owners were not selected in ordinal path order.'
    $FirstOwnerBytes = [IO.File]::ReadAllBytes($ContextShared)
    Assert-Equal 0 (Invoke-Formatter @('-FormatLongIf', '-Files', 'Context\Shared.smile')).ExitCode 'Multiple tracked-owner idempotence pass failed.'
    Assert-Equal ([Convert]::ToBase64String($FirstOwnerBytes)) ([Convert]::ToBase64String([IO.File]::ReadAllBytes($ContextShared))) 'Multiple tracked-owner formatting was not idempotent.'
    Pass 'tracked and untracked project contexts remain deliberate, deterministic, and idempotent'

    $First = Write-TestSource 'AFirst.smile' "Option Explicit`n`nFunction First() As Number`n`n    Return 4 + 5`n`nEnd Function`n"
    $Bad = Write-TestSource 'ZBad.smile' "Option Explicit`n`nFunction Broken()`n`n    Return Missing()`n`nEnd Function`n"
    $FirstBefore = [IO.File]::ReadAllBytes($First)
    $BadBefore = [IO.File]::ReadAllBytes($Bad)
    $Transaction = Invoke-FormatterCommand ("& '" + $FormatterPath + "' -FormatLongIf -Files @('AFirst.smile','ZBad.smile')")
    Assert-True ($Transaction.ExitCode -ne 0) 'Unsafe transformation did not fail preflight.'
    Assert-Equal ([Convert]::ToBase64String($FirstBefore)) ([Convert]::ToBase64String([IO.File]::ReadAllBytes($First))) 'A preflight failure partially wrote the first target.'
    Assert-Equal ([Convert]::ToBase64String($BadBefore)) ([Convert]::ToBase64String([IO.File]::ReadAllBytes($Bad))) 'A preflight failure changed the failing target.'
    Assert-Equal 0 @(Get-ChildItem -LiteralPath (Join-Path $TestRoot 'artifacts') -Recurse -File -ErrorAction SilentlyContinue).Count 'A preflight failure left staging files.'
    Pass 'all-target preflight prevents partial writes'

    $Concurrent = Write-TestSource 'Concurrent.smile' "Option Explicit`n`nFunction Concurrent() As Number`n`n    Return 6 + 7`n`nEnd Function`n"
    $ConcurrentBefore = [IO.File]::ReadAllText($Concurrent)
    $ConcurrentCommand = "& '" + $FormatterPath +
        "' -FormatLongIf -Files 'Concurrent.smile' -BeforeCommitTestHook { param(`$States) " +
        "[IO.File]::AppendAllText(`$States[0].FullPath, 'External change' + [Environment]::NewLine) }"
    $ConcurrentResult = Invoke-FormatterCommand $ConcurrentCommand
    Assert-True ($ConcurrentResult.ExitCode -ne 0 -and $ConcurrentResult.Output.Contains('changed after preflight')) 'A concurrent target change did not fail the commit hash check.'
    Assert-Equal ($ConcurrentBefore + 'External change' + [Environment]::NewLine) ([IO.File]::ReadAllText($Concurrent)) 'Formatter overwrote a concurrent external change.'
    Pass 'concurrent-change hash mismatch blocks formatter writes'

    $Emoji = [char]::ConvertFromUtf32(0x1F600)
    $World = [string][char]0x4E16 + [string][char]0x754C
    $Quote = [char]34
    $EncodingSource = "Option Explicit`r`n`r`n' Unicode $Emoji stays`r`n" +
        "Dim Message As Text`r`n`r`nMessage = $Quote" + "Hello`r`n$World $Emoji`r`n$Quote" + "`r`n`r`n" +
        "Function Total() As Number`r`n`r`n    Return (`r`n        1 +`r`n`r`n        2`r`n    )`r`n`r`nEnd Function`r`n"
    $Encoding = Write-TestSource 'Encoding.smile' $EncodingSource
    Assert-Equal 0 (Invoke-Formatter @('-FormatLongIf', '-Files', 'Encoding.smile')).ExitCode 'First encoding/idempotence pass failed.'
    $FirstPass = [IO.File]::ReadAllBytes($Encoding)
    Assert-Equal 0 (Invoke-Formatter @('-FormatLongIf', '-Files', 'Encoding.smile')).ExitCode 'Second encoding/idempotence pass failed.'
    $SecondPass = [IO.File]::ReadAllBytes($Encoding)
    Assert-Equal 0 (Invoke-Formatter @('-FormatLongIf', '-Files', 'Encoding.smile')).ExitCode 'Third encoding/idempotence pass failed.'
    $ThirdPass = [IO.File]::ReadAllBytes($Encoding)
    Assert-True (([Convert]::ToBase64String($FirstPass) -ceq [Convert]::ToBase64String($SecondPass)) -and
        ([Convert]::ToBase64String($SecondPass) -ceq [Convert]::ToBase64String($ThirdPass))) 'Three formatter passes were not byte-idempotent.'
    Assert-True (-not ($FirstPass.Length -ge 3 -and $FirstPass[0] -eq 0xEF -and $FirstPass[1] -eq 0xBB -and $FirstPass[2] -eq 0xBF)) 'Formatter wrote a UTF-8 BOM.'
    $EncodingText = [Text.Encoding]::UTF8.GetString($FirstPass)
    Assert-True (-not $EncodingText.Contains("`r")) 'Formatter did not normalize to LF.'
    Assert-True ($EncodingText.EndsWith("`n") -and -not $EncodingText.EndsWith("`n`n")) 'Formatter did not write exactly one final newline.'
    Assert-True ($EncodingText.Contains("Hello`n$World $Emoji`n$Quote")) 'Formatter changed a multiline Unicode string value.'
    Assert-Equal 1 ([regex]::Matches($EncodingText, [regex]::Escape("Unicode $Emoji stays")).Count) 'Formatter changed a comment.'
    Pass 'three-pass idempotence, UTF-8 without BOM, LF, final newline, strings, Unicode, and comments'

    Write-Host "$Passed focused SMILE formatter integration tests passed."
}
finally {
    $TempPrefix = ([IO.Path]::GetFullPath($env:TEMP)).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    $ResolvedTestRoot = [IO.Path]::GetFullPath($TestRoot)

    if ($ResolvedTestRoot.StartsWith($TempPrefix, [StringComparison]::OrdinalIgnoreCase) -and
        [IO.Directory]::Exists($ResolvedTestRoot)) {
        Remove-Item -LiteralPath $ResolvedTestRoot -Recurse -Force
    }
}
