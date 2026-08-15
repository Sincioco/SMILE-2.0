param(
    [switch]$Check,
    [switch]$IncludeRequirements,
    [switch]$FormatLongIf,
    [int]$MaximumLineLength = 100,
    [string[]]$Files
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$RepositoryRoot = Split-Path -Parent $PSScriptRoot
$Utf8WithoutBom = [Text.UTF8Encoding]::new($false)
$LanguageAssemblyPath = Join-Path $RepositoryRoot 'src\Smile.Language\bin\Debug\netstandard2.0\Smile.Language.dll'
$TextLiteralLineBreak = [char]0xE000

function Join-MultilineTextLiterals {
    param([string[]]$Lines)

    $Joined = [Collections.Generic.List[string]]::new()
    $Current = [Text.StringBuilder]::new()
    $InTextLiteral = $false

    foreach ($Line in $Lines) {
        if ($Current.Length -gt 0) {
            [void]$Current.Append($TextLiteralLineBreak)
        }

        [void]$Current.Append($Line)

        for ($Index = 0; $Index -lt $Line.Length; $Index++) {
            $Character = $Line[$Index]

            if (-not $InTextLiteral -and $Character -eq "'") {
                break
            }

            if ($Character -ne '"') {
                continue
            }

            if ($InTextLiteral -and $Index + 1 -lt $Line.Length -and $Line[$Index + 1] -eq '"') {
                $Index++
                continue
            }

            $InTextLiteral = -not $InTextLiteral
        }

        if (-not $InTextLiteral) {
            $Joined.Add($Current.ToString())
            [void]$Current.Clear()
        }
    }

    if ($Current.Length -gt 0) {
        $Joined.Add($Current.ToString())
    }

    return $Joined.ToArray()
}

function Expand-MultilineTextLiterals {
    param([string[]]$Lines)

    $Expanded = [Collections.Generic.List[string]]::new()

    foreach ($Line in $Lines) {
        foreach ($Part in $Line.Split([char[]]@($TextLiteralLineBreak), [StringSplitOptions]::None)) {
            $Expanded.Add($Part)
        }
    }

    return $Expanded.ToArray()
}

function Get-LeadingWhitespace {
    param([string]$Line)

    $Match = [regex]::Match($Line, '^\s*')
    return $Match.Value
}

function Test-SimpleVariable {
    param([string]$Expression)

    if ($Expression -notmatch '^[A-Za-z_][A-Za-z0-9_]*$') {
        return $false
    }

    return $Expression.Length -eq 1 -or $Expression -cnotmatch '^[A-Z][A-Z0-9_]*$'
}

function Get-ReturnType {
    param(
        [string]$Declaration,
        [string]$InferredType
    )

    $ClosingParenthesis = $Declaration.LastIndexOf(')')

    if ($ClosingParenthesis -ge 0) {
        $Suffix = $Declaration.Substring($ClosingParenthesis + 1).Trim()
        $TypeMatch = [regex]::Match($Suffix, '^As\s+(.+)$', [Text.RegularExpressions.RegexOptions]::IgnoreCase)

        if ($TypeMatch.Success) {
            return $TypeMatch.Groups[1].Value.Trim()
        }
    }

    if ([string]::IsNullOrWhiteSpace($InferredType) -or $InferredType -eq 'ERROR') {
        throw "Unable to infer the return type for '$($Declaration.Trim())'."
    }

    return $InferredType
}

function Initialize-LanguageAssembly {
    if (-not (Test-Path -LiteralPath $LanguageAssemblyPath)) {
        & dotnet build (Join-Path $RepositoryRoot 'src\Smile.Language\Smile.Language.csproj') --nologo

        if ($LASTEXITCODE -ne 0) {
            throw 'Unable to build Smile.Language for return-type inference.'
        }
    }

    if (-not ('Smile.Language.SmileLanguage' -as [type])) {
        [Reflection.Assembly]::LoadFrom($LanguageAssemblyPath) | Out-Null
    }
}

function Get-LogicalLineNumber {
    param(
        [string]$Text,
        [int]$Position
    )

    $LogicalLine = 1
    $InTextLiteral = $false
    $InComment = $false

    for ($Index = 0; $Index -lt $Position; $Index++) {
        $Character = $Text[$Index]

        if ($Character -eq "`n") {
            if (-not $InTextLiteral) {
                $LogicalLine++
            }

            $InComment = $false
            continue
        }

        if ($InComment) {
            continue
        }

        if (-not $InTextLiteral -and $Character -eq "'") {
            $InComment = $true
            continue
        }

        if ($Character -ne '"') {
            continue
        }

        if ($InTextLiteral -and $Index + 1 -lt $Position -and $Text[$Index + 1] -eq '"') {
            $Index++
            continue
        }

        $InTextLiteral = -not $InTextLiteral
    }

    return $LogicalLine
}

function Get-FunctionReturnTypes {
    param([string]$Text)

    Initialize-LanguageAssembly
    $Analysis = [Smile.Language.SmileLanguage]::Analyze($Text)
    $ReturnTypes = @{}

    foreach ($Routine in $Analysis.SemanticModel.Routines.Values) {
        if (-not $Routine.IsFunction) {
            continue
        }

        $LogicalLine = Get-LogicalLineNumber $Text $Routine.Declaration.Identifier.Position
        $ReturnTypes[$LogicalLine] = $Routine.ReturnType.Name
    }

    return $ReturnTypes
}

function Remove-OuterParentheses {
    param([string]$Condition)

    $Value = $Condition.Trim()

    if (-not ($Value.StartsWith('(') -and $Value.EndsWith(')'))) {
        return $Value
    }

    $Depth = 0
    $InString = $false

    for ($Index = 0; $Index -lt $Value.Length; $Index++) {
        $Character = $Value[$Index]

        if ($Character -eq '"') {
            if ($InString -and $Index + 1 -lt $Value.Length -and $Value[$Index + 1] -eq '"') {
                $Index++
                continue
            }

            $InString = -not $InString
            continue
        }

        if ($InString) {
            continue
        }

        if ($Character -eq '(') {
            $Depth++
        }
        elseif ($Character -eq ')') {
            $Depth--

            if ($Depth -eq 0 -and $Index -ne $Value.Length - 1) {
                return $Value
            }
        }
    }

    if ($Depth -eq 0) {
        return $Value.Substring(1, $Value.Length - 2).Trim()
    }

    return $Value
}

function Split-LogicalCondition {
    param([string]$Condition)

    $Parts = [Collections.Generic.List[string]]::new()
    $Start = 0
    $Depth = 0
    $InString = $false

    for ($Index = 0; $Index -lt $Condition.Length; $Index++) {
        $Character = $Condition[$Index]

        if ($Character -eq '"') {
            if ($InString -and $Index + 1 -lt $Condition.Length -and $Condition[$Index + 1] -eq '"') {
                $Index++
                continue
            }

            $InString = -not $InString
            continue
        }

        if ($InString) {
            continue
        }

        if ($Character -eq '(') {
            $Depth++
            continue
        }

        if ($Character -eq ')') {
            $Depth--
            continue
        }

        if ($Depth -ne 0) {
            continue
        }

        foreach ($Operator in @(' And ', ' Or ')) {
            if ($Index + $Operator.Length -le $Condition.Length -and
                $Condition.Substring($Index, $Operator.Length) -ieq $Operator) {
                $Segment = $Condition.Substring($Start, $Index - $Start).Trim()
                $LogicalWord = $Operator.Trim()
                $Parts.Add("$Segment $LogicalWord")
                $Index += $Operator.Length - 1
                $Start = $Index + 1
                break
            }
        }
    }

    $FinalSegment = $Condition.Substring($Start).Trim()

    if ($FinalSegment.Length -gt 0) {
        $Parts.Add($FinalSegment)
    }

    return $Parts.ToArray()
}

function Format-LongIfStatements {
    param(
        [string[]]$Lines,
        [int]$LineLimit
    )

    $Formatted = [Collections.Generic.List[string]]::new()

    foreach ($Line in $Lines) {
        $Match = [regex]::Match($Line, '^(\s*)(If|Else\s+If)\s+(.+)\s+Then\s*$', [Text.RegularExpressions.RegexOptions]::IgnoreCase)

        if (-not $Match.Success) {
            $Formatted.Add($Line)
            continue
        }

        $Indent = $Match.Groups[1].Value
        $Keyword = if ($Match.Groups[2].Value -match '^Else') { 'Else If' } else { 'If' }
        $Condition = Remove-OuterParentheses $Match.Groups[3].Value
        $Parts = @(Split-LogicalCondition $Condition)

        if ($Parts.Count -lt 2 -or ($Line.Length -le $LineLimit -and $Parts.Count -lt 3)) {
            $Formatted.Add($Line)
            continue
        }

        $ContinuationIndent = $Indent + '    '
        $Formatted.Add("$Indent$Keyword ($($Parts[0])")

        for ($PartIndex = 1; $PartIndex -lt $Parts.Count; $PartIndex++) {
            if ($PartIndex -eq $Parts.Count - 1) {
                $Formatted.Add("$ContinuationIndent$($Parts[$PartIndex])) Then")
            }
            else {
                $Formatted.Add("$ContinuationIndent$($Parts[$PartIndex])")
            }
        }
    }

    return $Formatted.ToArray()
}

function Add-ReturnVariables {
    param(
        [string[]]$Lines,
        [hashtable]$ReturnTypes
    )

    $Formatted = [Collections.Generic.List[string]]::new()
    $LineIndex = 0

    while ($LineIndex -lt $Lines.Count) {
        $Declaration = $Lines[$LineIndex]
        $FunctionMatch = [regex]::Match($Declaration, '^\s*(?:Public\s+|Private\s+)?Function\b', [Text.RegularExpressions.RegexOptions]::IgnoreCase)

        if (-not $FunctionMatch.Success) {
            $Formatted.Add($Declaration)
            $LineIndex++
            continue
        }

        $EndIndex = $LineIndex + 1

        while ($EndIndex -lt $Lines.Count -and $Lines[$EndIndex].Trim() -notmatch '^End\s+Function$') {
            $EndIndex++
        }

        if ($EndIndex -ge $Lines.Count) {
            $Formatted.Add($Declaration)
            $LineIndex++
            continue
        }

        $NeedsReturnVariable = $false

        for ($BodyIndex = $LineIndex + 1; $BodyIndex -lt $EndIndex; $BodyIndex++) {
            $ReturnMatch = [regex]::Match($Lines[$BodyIndex], '^\s*Return\s+(.+?)\s*$', [Text.RegularExpressions.RegexOptions]::IgnoreCase)

            if ($ReturnMatch.Success -and -not (Test-SimpleVariable $ReturnMatch.Groups[1].Value.Trim())) {
                $NeedsReturnVariable = $true
                break
            }
        }

        if (-not $NeedsReturnVariable) {
            for ($CopyIndex = $LineIndex; $CopyIndex -le $EndIndex; $CopyIndex++) {
                $Formatted.Add($Lines[$CopyIndex])
            }

            $LineIndex = $EndIndex + 1
            continue
        }

        $FunctionText = ($Lines[$LineIndex..$EndIndex] -join "`n")
        $ReturnVariable = 'ReturnValue'
        $Suffix = 2

        while ($FunctionText -match "\b$([regex]::Escape($ReturnVariable))\b") {
            $ReturnVariable = "ReturnValue$Suffix"
            $Suffix++
        }

        $DeclarationIndent = Get-LeadingWhitespace $Declaration
        $BodyIndent = $DeclarationIndent + '    '
        $InferredType = if ($ReturnTypes.ContainsKey($LineIndex + 1)) { $ReturnTypes[$LineIndex + 1] } else { '' }
        $ReturnType = Get-ReturnType $Declaration $InferredType
        $Formatted.Add($Declaration)
        $Formatted.Add("${BodyIndent}Dim $ReturnVariable As $ReturnType")

        for ($BodyIndex = $LineIndex + 1; $BodyIndex -lt $EndIndex; $BodyIndex++) {
            $ReturnMatch = [regex]::Match($Lines[$BodyIndex], '^(\s*)Return\s+(.+?)\s*$', [Text.RegularExpressions.RegexOptions]::IgnoreCase)

            if ($ReturnMatch.Success -and -not (Test-SimpleVariable $ReturnMatch.Groups[2].Value.Trim())) {
                $ReturnIndent = $ReturnMatch.Groups[1].Value
                $Expression = $ReturnMatch.Groups[2].Value.Trim()
                $Formatted.Add("$ReturnIndent$ReturnVariable = $Expression")
                $Formatted.Add("${ReturnIndent}Return $ReturnVariable")
            }
            else {
                $Formatted.Add($Lines[$BodyIndex])
            }
        }

        $Formatted.Add($Lines[$EndIndex])
        $LineIndex = $EndIndex + 1
    }

    return $Formatted.ToArray()
}

function Get-StatementCategory {
    param([string]$Line)

    $Value = $Line.Trim()

    if ($Value -match '^(?:Public\s+|Private\s+)?Module\b') { return 'Module' }
    if ($Value -match '^Import\b') { return 'Import' }
    if ($Value -match '^(?:Public\s+|Private\s+)?Dim\b') { return 'Dim' }
    if ($Value -match '^Call\b') { return 'Call' }
    if ($Value -match '^Unload\b') { return 'Unload' }
    return ''
}

function Get-ParenthesisDelta {
    param([string]$Line)

    $Delta = 0
    $InString = $false

    for ($Index = 0; $Index -lt $Line.Length; $Index++) {
        $Character = $Line[$Index]

        if ($Character -eq "'" -and -not $InString) {
            break
        }

        if ($Character -eq '"') {
            if ($InString -and $Index + 1 -lt $Line.Length -and $Line[$Index + 1] -eq '"') {
                $Index++
                continue
            }

            $InString = -not $InString
            continue
        }

        if ($InString) {
            continue
        }

        if ($Character -eq '(') {
            $Delta++
        }
        elseif ($Character -eq ')') {
            $Delta--
        }
    }

    return $Delta
}

function Test-SingleStatementIfEnd {
    param(
        [string[]]$Lines,
        [int]$EndIndex
    )

    $Depth = 0

    for ($Index = $EndIndex - 1; $Index -ge 0; $Index--) {
        $Value = $Lines[$Index].Trim()

        if ($Value -match '^End\s+If$') {
            $Depth++
            continue
        }

        if ($Value -match '^If\b') {
            if ($Depth -gt 0) {
                $Depth--
                continue
            }

            $Statements = @($Lines[($Index + 1)..($EndIndex - 1)] | Where-Object {
                $Candidate = $_.Trim()
                $Candidate.Length -gt 0 -and -not $Candidate.StartsWith("'")
            })
            return $Statements.Count -eq 1
        }
    }

    return $false
}

function Test-RequiresBlankBefore {
    param([string]$Line)

    $Value = $Line.Trim()

    return $Value -match '^If\b' -or
        $Value -match '^For\b' -or
        $Value -match '^End\s+For$' -or
        $Value -match '^Do\b' -or
        $Value -match '^End\s+Sub$' -or
        $Value -match '^Loop\b' -or
        $Value -match '^Option\s+Explicit$'
}

function Test-RequiresBlankAfter {
    param([string]$Line)

    $Value = $Line.Trim()

    return $Value -match '^End\s+If$' -or
        $Value -match '^Loop\b' -or
        $Value -match '^Option\s+Explicit$' -or
        $Value -match '^(?:Public\s+|Private\s+)?(?:Function|Sub|Procedure)\b' -or
        $Value -match '^End\s+(?:Function|Sub)$'
}

function Format-BlankLines {
    param([string[]]$Lines)

    $Collapsed = [Collections.Generic.List[string]]::new()

    foreach ($Line in $Lines) {
        if ($Line.Trim().Length -eq 0) {
            if ($Collapsed.Count -ne 0 -and $Collapsed[$Collapsed.Count - 1].Length -ne 0) {
                $Collapsed.Add('')
            }
        }
        else {
            $Collapsed.Add($Line.TrimEnd())
        }
    }

    while ($Collapsed.Count -gt 0 -and $Collapsed[$Collapsed.Count - 1].Length -eq 0) {
        $Collapsed.RemoveAt($Collapsed.Count - 1)
    }

    $NonBlank = [Collections.Generic.List[string]]::new()
    $HadBlankAfter = [Collections.Generic.List[bool]]::new()

    for ($CollapsedIndex = 0; $CollapsedIndex -lt $Collapsed.Count; $CollapsedIndex++) {
        if ($Collapsed[$CollapsedIndex].Length -eq 0) {
            continue
        }

        $NonBlank.Add($Collapsed[$CollapsedIndex])
        $HadBlankAfter.Add($CollapsedIndex + 1 -lt $Collapsed.Count -and
            $Collapsed[$CollapsedIndex + 1].Length -eq 0)
    }

    $Formatted = [Collections.Generic.List[string]]::new()
    $CategoriesStartingAt = [Collections.Generic.List[string]]::new()
    $CategoriesEndingAt = [Collections.Generic.List[string]]::new()
    $CallDepth = 0

    for ($Index = 0; $Index -lt $NonBlank.Count; $Index++) {
        $Category = Get-StatementCategory $NonBlank[$Index]

        if ($CallDepth -gt 0) {
            $CategoriesStartingAt.Add('')
            $CallDepth += Get-ParenthesisDelta $NonBlank[$Index]
            $CategoriesEndingAt.Add($(if ($CallDepth -le 0) { 'Call' } else { '' }))
            continue
        }

        $CategoriesStartingAt.Add($Category)

        if ($Category -eq 'Call') {
            $CallDepth = Get-ParenthesisDelta $NonBlank[$Index]
            $CategoriesEndingAt.Add($(if ($CallDepth -gt 0) { '' } else { 'Call' }))
        }
        else {
            $CategoriesEndingAt.Add($Category)
        }
    }

    for ($Index = 0; $Index -lt $NonBlank.Count; $Index++) {
        $Current = $NonBlank[$Index]
        $Next = if ($Index + 1 -lt $NonBlank.Count) { $NonBlank[$Index + 1] } else { $null }
        $Formatted.Add($Current)

        if ($null -ne $Next) {
            $CurrentCategory = $CategoriesEndingAt[$Index]
            $NextCategory = $CategoriesStartingAt[$Index + 1]
            $NeedsBlank = (Test-RequiresBlankAfter $Current) -or (Test-RequiresBlankBefore $Next)

            if ($CurrentCategory.Length -gt 0 -and $CurrentCategory -ne $NextCategory) {
                $NeedsBlank = $true
            }

            if ($Next.Trim() -match '^End\s+Function$' -and $Current.Trim() -match '^Return\b') {
                $NeedsBlank = $true
            }

            if ($NeedsBlank -or $HadBlankAfter[$Index]) {
                $Formatted.Add('')
            }
        }
    }

    $Normalized = [Collections.Generic.List[string]]::new()

    foreach ($Line in $Formatted) {
        if ($Line.Length -eq 0 -and $Normalized.Count -gt 0 -and $Normalized[$Normalized.Count - 1].Length -eq 0) {
            continue
        }

        $Normalized.Add($Line)
    }

    while ($Normalized.Count -gt 0 -and $Normalized[$Normalized.Count - 1].Length -eq 0) {
        $Normalized.RemoveAt($Normalized.Count - 1)
    }

    return $Normalized.ToArray()
}

function Format-SmileText {
    param(
        [string]$Text,
        [bool]$SkipReturnVariables
    )

    $NormalizedText = $Text.Replace("`r`n", "`n").Replace("`r", "`n")
    $Lines = @($NormalizedText.Split("`n"))

    if ($Lines.Count -gt 0 -and $Lines[$Lines.Count - 1].Length -eq 0) {
        $Lines = if ($Lines.Count -eq 1) { @() } else { @($Lines[0..($Lines.Count - 2)]) }
    }

    $Lines = @(Join-MultilineTextLiterals $Lines)

    if (-not $SkipReturnVariables) {
        $ReturnTypes = Get-FunctionReturnTypes $NormalizedText
        $Lines = @(Add-ReturnVariables $Lines $ReturnTypes)
    }
    if ($FormatLongIf) {
        $Lines = @(Format-LongIfStatements $Lines $MaximumLineLength)
    }
    $Lines = @(Format-BlankLines $Lines)
    $Lines = @(Expand-MultilineTextLiterals $Lines)
    return ($Lines -join "`n") + "`n"
}

$TrackedFiles = @(& git -C $RepositoryRoot ls-files --cached --others --exclude-standard -- '*.smile')

if ($LASTEXITCODE -ne 0) {
    throw 'Unable to enumerate tracked SMILE source files.'
}

if (-not $IncludeRequirements) {
    $TrackedFiles = @($TrackedFiles | Where-Object { $_ -notmatch '^requirements/' })
}

if ($null -ne $Files -and $Files.Count -gt 0) {
    $RequestedFiles = @($Files | ForEach-Object { $_.Replace('\', '/') })
    $TrackedFiles = @($TrackedFiles | Where-Object { $RequestedFiles -contains $_ })
}

$ChangedFiles = [Collections.Generic.List[string]]::new()

foreach ($RelativePath in $TrackedFiles) {
    $FullPath = Join-Path $RepositoryRoot $RelativePath
    $Original = [IO.File]::ReadAllText($FullPath)
    $SkipReturnVariables = $RelativePath -match '(^|/)Invalid[^/]*/|^examples/diagnostics/'
    $Formatted = Format-SmileText $Original $SkipReturnVariables

    if ($Original.Replace("`r`n", "`n").Replace("`r", "`n") -cne $Formatted) {
        $ChangedFiles.Add($RelativePath)

        if (-not $Check) {
            [IO.File]::WriteAllText($FullPath, $Formatted, $Utf8WithoutBom)
        }
    }
}

if ($Check -and $ChangedFiles.Count -gt 0) {
    Write-Error ("SMILE style differs in {0} file(s):`n{1}" -f $ChangedFiles.Count, ($ChangedFiles -join "`n"))
    exit 1
}

if ($Check) {
    Write-Output ("SMILE style check passed for {0} file(s)." -f $TrackedFiles.Count)
}
else {
    Write-Output ("Formatted {0} of {1} tracked SMILE file(s)." -f $ChangedFiles.Count, $TrackedFiles.Count)
}
