param(
    [switch]$Check,
    [switch]$IncludeUntracked,
    [switch]$FormatLongIf,
    [int]$MaximumLineLength = 100,
    [string[]]$Files,
    [scriptblock]$BeforeCommitTestHook
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$RepositoryRoot = Split-Path -Parent $PSScriptRoot
$Utf8WithoutBom = [Text.UTF8Encoding]::new($false)
$ReleaseLanguageAssemblyPath = Join-Path $RepositoryRoot 'src\Smile.Language\bin\Release\netstandard2.0\Smile.Language.dll'
$DebugLanguageAssemblyPath = Join-Path $RepositoryRoot 'src\Smile.Language\bin\Debug\netstandard2.0\Smile.Language.dll'
$LanguageAssemblyPath = if (Test-Path -LiteralPath $ReleaseLanguageAssemblyPath) {
    $ReleaseLanguageAssemblyPath
}
else {
    $DebugLanguageAssemblyPath
}
$TextLiteralLineBreak = [char]0xE000
$ProjectOwnersBySource = @{}
$ProjectAnalyses = @{}
$ProjectAnalysisFailures = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$SymbolCacheRoot = Join-Path $env:TEMP ("smile-formatter-symbols-" + [Guid]::NewGuid().ToString('N'))

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

function Initialize-LanguageAssembly {
    $NeedsBuild = -not (Test-Path -LiteralPath $LanguageAssemblyPath)

    if (-not $NeedsBuild) {
        $AssemblyWriteTime = (Get-Item -LiteralPath $LanguageAssemblyPath).LastWriteTimeUtc
        $NeedsBuild = @(Get-ChildItem -LiteralPath (Join-Path $RepositoryRoot 'src\Smile.Language') -Recurse -File |
            Where-Object { $_.FullName -notmatch '[\\/](?:bin|obj)[\\/]' } |
            Where-Object { $_.Extension -eq '.cs' -or $_.Extension -eq '.csproj' } |
            Where-Object { $_.LastWriteTimeUtc -gt $AssemblyWriteTime }).Count -gt 0
    }

    if ($NeedsBuild) {
        if ($Check) {
            throw 'Smile.Language formatter assembly is missing or stale. Run scripts\build.cmd first; -Check never builds dependencies.'
        }

        & dotnet build (Join-Path $RepositoryRoot 'src\Smile.Language\Smile.Language.csproj') --nologo

        if ($LASTEXITCODE -ne 0) {
            throw 'Unable to build Smile.Language for syntax-aware formatting.'
        }
    }

    if (-not ('Smile.Language.SmileLanguage' -as [type])) {
        [Reflection.Assembly]::LoadFrom($LanguageAssemblyPath) | Out-Null
    }
}

function Get-StatementCategory {
    param([string]$Line)

    $Value = $Line.Trim()

    if ($Value -match '^(?:Public\s+|Private\s+)?Module\b') { return 'Module' }
    if ($Value -match '^Import\b') { return 'Import' }
    if ($Value -match '^(?:Public\s+|Private\s+)?Dim\b') { return 'Dim' }
    if ($Value -match '^Call\b') { return 'Call' }
    if ($Value -match '^Play\s+Sound\b') { return 'PlaySound' }
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
        $Value -match '^Do\b' -or
        $Value -match '^End\s+Sub$' -or
        $Value -match '^Loop\b' -or
        $Value -match '^Option\s+Explicit$'
}

function Get-SyntaxIfBlockLayout {
    param(
        $Layouts,
        [int]$CurrentSourceLine,
        [int]$NextSourceLine
    )

    foreach ($Layout in $Layouts) {
        if ($Layout.HeaderEndLines -contains $CurrentSourceLine -or
            $Layout.BoundaryLines -contains $NextSourceLine) {
            return $(if ($Layout.IsExpanded) { 'Expanded' } else { 'Compact' })
        }
    }

    return ''
}

function Test-CompactForBoundary {
    param(
        [string[]]$Lines,
        [int]$Index
    )

    $ForIndex = -1
    $EndForIndex = -1

    if ($Lines[$Index].Trim() -match '^For\b') {
        $ForIndex = $Index

        for ($CandidateIndex = $Index + 1; $CandidateIndex -lt $Lines.Count; $CandidateIndex++) {
            if ((Get-LeadingWhitespace $Lines[$CandidateIndex]) -cne (Get-LeadingWhitespace $Lines[$Index])) {
                continue
            }

            if ($Lines[$CandidateIndex].Trim() -match '^End\s+For$') {
                $EndForIndex = $CandidateIndex
            }

            break
        }
    }
    elseif ($Index + 1 -lt $Lines.Count -and $Lines[$Index + 1].Trim() -match '^End\s+For$') {
        $EndForIndex = $Index + 1

        for ($CandidateIndex = $Index; $CandidateIndex -ge 0; $CandidateIndex--) {
            if ((Get-LeadingWhitespace $Lines[$CandidateIndex]) -cne (Get-LeadingWhitespace $Lines[$EndForIndex])) {
                continue
            }

            if ($Lines[$CandidateIndex].Trim() -match '^For\b') {
                $ForIndex = $CandidateIndex
            }

            break
        }
    }

    if ($ForIndex -lt 0 -or $EndForIndex -le $ForIndex + 1) {
        return $false
    }

    $BodyStatements = @($Lines[($ForIndex + 1)..($EndForIndex - 1)] | Where-Object {
        $_.Trim().Length -gt 0 -and -not $_.Trim().StartsWith("'")
    })

    if ($BodyStatements.Count -lt 1 -or $BodyStatements.Count -gt 4) {
        return $false
    }

    $HasNestedControl = @($BodyStatements | Where-Object {
        $_.Trim() -match '^(?:If\b|Else\b|End\s+If$|For\b|End\s+For$|Do\b|Loop\b)'
    }).Count -gt 0

    return -not $HasNestedControl
}

function Test-RequiresBlankAfter {
    param([string]$Line)

    $Value = $Line.Trim()

    return $Value -match '^For\b' -or
        $Value -match '^End\s+If$' -or
        $Value -match '^Loop\b' -or
        $Value -match '^Option\s+Explicit$' -or
        $Value -match '^(?:Public\s+|Private\s+)?(?:Function|Sub|Procedure)\b' -or
        $Value -match '^End\s+(?:Function|Sub)$'
}

function Format-BlankLines {
    param([string[]]$Lines, $IfBlockLayouts)

    $Collapsed = [Collections.Generic.List[string]]::new()
    $CollapsedSourceLines = [Collections.Generic.List[int]]::new()

    for ($LineIndex = 0; $LineIndex -lt $Lines.Count; $LineIndex++) {
        $Line = $Lines[$LineIndex]

        if ($Line.Trim().Length -eq 0) {
            if ($Collapsed.Count -ne 0 -and $Collapsed[$Collapsed.Count - 1].Length -ne 0) {
                $Collapsed.Add('')
                $CollapsedSourceLines.Add($LineIndex + 1)
            }
        }
        else {
            $Collapsed.Add($Line.TrimEnd())
            $CollapsedSourceLines.Add($LineIndex + 1)
        }
    }

    while ($Collapsed.Count -gt 0 -and $Collapsed[$Collapsed.Count - 1].Length -eq 0) {
        $Collapsed.RemoveAt($Collapsed.Count - 1)
    }

    $NonBlank = [Collections.Generic.List[string]]::new()
    $NonBlankSourceLines = [Collections.Generic.List[int]]::new()
    $HadBlankAfter = [Collections.Generic.List[bool]]::new()

    for ($CollapsedIndex = 0; $CollapsedIndex -lt $Collapsed.Count; $CollapsedIndex++) {
        if ($Collapsed[$CollapsedIndex].Length -eq 0) {
            continue
        }

        $NonBlank.Add($Collapsed[$CollapsedIndex])
        $NonBlankSourceLines.Add($CollapsedSourceLines[$CollapsedIndex])
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
            $IfBlockLayout = Get-SyntaxIfBlockLayout $IfBlockLayouts $NonBlankSourceLines[$Index] $NonBlankSourceLines[$Index + 1]
            $SuppressBlank = ($IfBlockLayout -eq 'Compact') -or
                (Test-CompactForBoundary $NonBlank $Index)

            if ($IfBlockLayout -eq 'Expanded') {
                $NeedsBlank = $true
            }

            if ($NextCategory -eq 'Call' -and $CurrentCategory -ne 'Call') {
                $NeedsBlank = $true
            }

            if ($NextCategory -eq 'PlaySound' -and $CurrentCategory -ne 'PlaySound') {
                $NeedsBlank = $true
                $SuppressBlank = $false
            }

            if ($CurrentCategory -eq 'PlaySound' -and $NextCategory -ne 'PlaySound') {
                $NeedsBlank = $true
                $SuppressBlank = $false
            }

            if ($Current.Trim() -match '^Play\s+Sound\b' -and $Next.Trim() -match '^Call\b') {
                $SuppressBlank = $true
            }

            if ($Next.Trim() -match '^End\s+For$' -and -not (Test-RequiresBlankAfter $Current)) {
                $SuppressBlank = $true
            }

            if ($CurrentCategory.Length -gt 0 -and $CurrentCategory -ne $NextCategory) {
                $NeedsBlank = $true
            }

            if ($Next.Trim() -match '^End\s+Function$' -and $Current.Trim() -match '^Return\b') {
                $NeedsBlank = $true
            }

            if (-not $SuppressBlank -and ($NeedsBlank -or $HadBlankAfter[$Index])) {
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
        [bool]$SkipReturnVariables,
        [string]$FilePath,
        $SymbolAnalysis,
        $SymbolSyntaxTree
    )

    $NormalizedText = $Text.Replace("`r`n", "`n").Replace("`r", "`n")
    $StructuredText = if ($null -ne $SymbolAnalysis -and $null -ne $SymbolSyntaxTree) {
        [Smile.Language.SmileSourceFormatter]::Format(
            $NormalizedText,
            [bool]$FormatLongIf,
            $MaximumLineLength,
            -not $SkipReturnVariables,
            $true,
            $FilePath,
            $SymbolAnalysis,
            $SymbolSyntaxTree)
    }
    else {
        [Smile.Language.SmileSourceFormatter]::Format(
            $NormalizedText,
            [bool]$FormatLongIf,
            $MaximumLineLength,
            -not $SkipReturnVariables,
            $true,
            $FilePath)
    }
    $Lines = @($StructuredText.Split("`n"))

    if ($Lines.Count -gt 0 -and $Lines[$Lines.Count - 1].Length -eq 0) {
        $Lines = if ($Lines.Count -eq 1) { @() } else { @($Lines[0..($Lines.Count - 2)]) }
    }

    $Lines = @(Join-MultilineTextLiterals $Lines)
    $LayoutText = ($Lines -join "`n") + "`n"
    $IfBlockLayouts = [Smile.Language.SmileSourceFormatter]::GetIfBlockLayouts($LayoutText, $FilePath)
    $Lines = @(Format-BlankLines $Lines $IfBlockLayouts)
    $Lines = @(Expand-MultilineTextLiterals $Lines)
    return ($Lines -join "`n") + "`n"
}

function Initialize-ProjectOwners {
    param([string[]]$RelativeTargets)

    $TargetPaths = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)

    foreach ($RelativeTarget in $RelativeTargets) {
        [void]$TargetPaths.Add([IO.Path]::GetFullPath((Join-Path $RepositoryRoot $RelativeTarget)))
    }

    $ProjectPaths = @(& git -C $RepositoryRoot ls-files --cached --others --exclude-standard -- '*.smileproj' '*.smilelibproj')

    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to enumerate SMILE project files for formatter symbol resolution.'
    }

    foreach ($RelativeProjectPath in $ProjectPaths | Sort-Object) {
        $ProjectPath = [IO.Path]::GetFullPath((Join-Path $RepositoryRoot $RelativeProjectPath))

        try {
            $SourceSet = [Smile.Language.SmileProjectSourceSet]::Load($ProjectPath)
        }
        catch {
            continue
        }

        foreach ($Source in $SourceSet.Items) {
            $SourcePath = [IO.Path]::GetFullPath($Source.FullPath)

            if (-not $TargetPaths.Contains($SourcePath)) {
                continue
            }

            if (-not $ProjectOwnersBySource.ContainsKey($SourcePath)) {
                $ProjectOwnersBySource[$SourcePath] = [Collections.Generic.List[string]]::new()
            }

            $ProjectOwnersBySource[$SourcePath].Add($ProjectPath)
        }
    }
}

function Get-ProjectSymbolContext {
    param([string]$FilePath, [string]$SourceText)

    $NormalizedPath = [IO.Path]::GetFullPath($FilePath)

    if (-not $ProjectOwnersBySource.ContainsKey($NormalizedPath)) {
        return $null
    }

    foreach ($ProjectPath in $ProjectOwnersBySource[$NormalizedPath]) {
        if ($ProjectAnalysisFailures.Contains($ProjectPath)) {
            continue
        }

        if (-not $ProjectAnalyses.ContainsKey($ProjectPath)) {
            try {
                $Compilation = [Smile.Language.SmileProjectCompilation]::Load($ProjectPath, $SymbolCacheRoot)
                $ProjectAnalyses[$ProjectPath] = [Smile.Language.SmileLanguage]::Analyze(
                    $Compilation.Sources,
                    $Compilation.CompilationKind,
                    $Compilation.DependencyContext)
            }
            catch {
                [void]$ProjectAnalysisFailures.Add($ProjectPath)
                continue
            }
        }

        $Analysis = $ProjectAnalyses[$ProjectPath]
        $SyntaxTree = $Analysis.SyntaxTrees | Where-Object {
            [string]::Equals([IO.Path]::GetFullPath($_.Source.FilePath), $NormalizedPath,
                [StringComparison]::OrdinalIgnoreCase)
        } | Select-Object -First 1

        if ($null -ne $SyntaxTree -and
            $SyntaxTree.Source.Text.Replace("`r`n", "`n").Replace("`r", "`n") -ceq
                $SourceText.Replace("`r`n", "`n").Replace("`r", "`n")) {
            return [pscustomobject]@{ Analysis = $Analysis; SyntaxTree = $SyntaxTree }
        }
    }

    return $null
}

function Remove-SymbolCache {
    $TempPrefix = ([IO.Path]::GetFullPath($env:TEMP)).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    $ResolvedCacheRoot = [IO.Path]::GetFullPath($SymbolCacheRoot)

    if ($ResolvedCacheRoot.StartsWith($TempPrefix, [StringComparison]::OrdinalIgnoreCase) -and
        [IO.Directory]::Exists($ResolvedCacheRoot)) {
        Remove-Item -LiteralPath $ResolvedCacheRoot -Recurse -Force
    }
}

function Get-Sha256 {
    param([byte[]]$Bytes)

    $Algorithm = [Security.Cryptography.SHA256]::Create()

    try {
        return ([BitConverter]::ToString($Algorithm.ComputeHash($Bytes))).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $Algorithm.Dispose()
    }
}

function Get-ErrorCodeCounts {
    param([string]$Text, [string]$FilePath)

    $Counts = @{}
    $Analysis = [Smile.Language.SmileLanguage]::Analyze($Text, $FilePath)

    foreach ($Diagnostic in $Analysis.Diagnostics) {
        if ($Diagnostic.Severity.ToString() -ne 'Error') {
            continue
        }

        if (-not $Counts.ContainsKey($Diagnostic.Code)) {
            $Counts[$Diagnostic.Code] = 0
        }

        $Counts[$Diagnostic.Code]++
    }

    return $Counts
}

function Assert-NoNewDiagnostics {
    param(
        [hashtable]$OriginalCounts,
        [hashtable]$FormattedCounts,
        [string]$RelativePath
    )

    foreach ($Code in $FormattedCounts.Keys) {
        $OriginalCount = if ($OriginalCounts.ContainsKey($Code)) { $OriginalCounts[$Code] } else { 0 }

        if ($FormattedCounts[$Code] -gt $OriginalCount) {
            throw "Formatting '$RelativePath' introduced $Code diagnostics."
        }
    }
}

function Resolve-RequestedTargets {
    param([string[]]$Requested)

    $RepositoryPrefix = $RepositoryRoot.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    $Seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $Resolved = [Collections.Generic.List[string]]::new()

    foreach ($RequestedPath in $Requested) {
        if ([string]::IsNullOrWhiteSpace($RequestedPath)) {
            throw 'An explicit formatter file path cannot be empty.'
        }

        $FullPath = if ([IO.Path]::IsPathRooted($RequestedPath)) {
            [IO.Path]::GetFullPath($RequestedPath)
        }
        else {
            [IO.Path]::GetFullPath((Join-Path $RepositoryRoot $RequestedPath))
        }

        if (-not $FullPath.StartsWith($RepositoryPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Explicit formatter target '$RequestedPath' is outside the repository."
        }

        if (-not [IO.File]::Exists($FullPath)) {
            throw "Explicit formatter target '$RequestedPath' does not exist."
        }

        if ([IO.Path]::GetExtension($FullPath) -ine '.smile') {
            throw "Explicit formatter target '$RequestedPath' is not a .smile file."
        }

        $RelativePath = $FullPath.Substring($RepositoryPrefix.Length).Replace('\', '/')

        if ($Seen.Add($RelativePath)) {
            $Resolved.Add($RelativePath)
        }
    }

    return $Resolved.ToArray()
}

Initialize-LanguageAssembly

if ($null -ne $Files -and $Files.Count -gt 0) {
    $TargetFiles = @(Resolve-RequestedTargets $Files)
}
else {
    $TargetFiles = @(& git -C $RepositoryRoot ls-files -- '*.smile')

    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to enumerate tracked SMILE source files.'
    }

    if ($IncludeUntracked) {
        $UntrackedFiles = @(& git -C $RepositoryRoot ls-files --others --exclude-standard -- '*.smile')

        if ($LASTEXITCODE -ne 0) {
            throw 'Unable to enumerate untracked SMILE source files.'
        }

        $Seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
        $Combined = [Collections.Generic.List[string]]::new()

        foreach ($RelativePath in @($TargetFiles) + @($UntrackedFiles)) {
            if ($Seen.Add($RelativePath)) {
                $Combined.Add($RelativePath)
            }
        }

        $TargetFiles = $Combined.ToArray()
    }
}

Initialize-ProjectOwners $TargetFiles

$States = [Collections.Generic.List[object]]::new()
$ChangedFiles = [Collections.Generic.List[string]]::new()

foreach ($RelativePath in $TargetFiles) {
    $FullPath = Join-Path $RepositoryRoot $RelativePath
    $OriginalBytes = [IO.File]::ReadAllBytes($FullPath)
    $Original = [IO.File]::ReadAllText($FullPath)
    $OriginalDiagnostics = Get-ErrorCodeCounts $Original $FullPath
    $SkipReturnVariables = $RelativePath -match '(^|/)Invalid[^/]*/|^examples/diagnostics/'
    $SymbolContext = Get-ProjectSymbolContext $FullPath $Original
    $SymbolAnalysis = if ($null -eq $SymbolContext) { $null } else { $SymbolContext.Analysis }
    $SymbolSyntaxTree = if ($null -eq $SymbolContext) { $null } else { $SymbolContext.SyntaxTree }
    $Formatted = Format-SmileText $Original $SkipReturnVariables $FullPath $SymbolAnalysis $SymbolSyntaxTree
    $FormattedDiagnostics = Get-ErrorCodeCounts $Formatted $FullPath

    Assert-NoNewDiagnostics $OriginalDiagnostics $FormattedDiagnostics $RelativePath

    $Changed = $Original.Replace("`r`n", "`n").Replace("`r", "`n") -cne $Formatted

    if ($Changed) {
        $ChangedFiles.Add($RelativePath)
    }

    $States.Add([pscustomobject]@{
        RelativePath = $RelativePath
        FullPath = $FullPath
        OriginalBytes = $OriginalBytes
        OriginalHash = Get-Sha256 $OriginalBytes
        OriginalWriteTimeUtc = [IO.File]::GetLastWriteTimeUtc($FullPath)
        Formatted = $Formatted
        Changed = $Changed
    })
}

if ($Check -and $ChangedFiles.Count -gt 0) {
    Remove-SymbolCache
    Write-Error ("SMILE style differs in {0} file(s):`n{1}" -f $ChangedFiles.Count, ($ChangedFiles -join "`n"))
    exit 1
}

if ($Check) {
    Remove-SymbolCache
    Write-Output ("SMILE style check passed for {0} file(s)." -f $TargetFiles.Count)
    exit 0
}

if ($null -ne $BeforeCommitTestHook) {
    & $BeforeCommitTestHook $States.ToArray()
}

foreach ($State in $States) {
    $CurrentHash = Get-Sha256 ([IO.File]::ReadAllBytes($State.FullPath))

    if ($CurrentHash -cne $State.OriginalHash) {
        throw "Formatter target '$($State.RelativePath)' changed after preflight; no formatter writes were committed."
    }
}

if ($ChangedFiles.Count -eq 0) {
    Remove-SymbolCache
    Write-Output ("Formatted 0 of {0} SMILE file(s)." -f $TargetFiles.Count)
    exit 0
}

$ArtifactsTemp = Join-Path $RepositoryRoot 'artifacts\temp'
[IO.Directory]::CreateDirectory($ArtifactsTemp) | Out-Null
$StageRoot = Join-Path $ArtifactsTemp ("smile-formatter-" + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($StageRoot) | Out-Null
$Committed = [Collections.Generic.List[object]]::new()

try {
    foreach ($State in $States | Where-Object Changed) {
        $StagePath = Join-Path $StageRoot ([Guid]::NewGuid().ToString('N') + '.new')
        $BackupPath = Join-Path $StageRoot ([Guid]::NewGuid().ToString('N') + '.backup')
        [IO.File]::WriteAllText($StagePath, $State.Formatted, $Utf8WithoutBom)
        Add-Member -InputObject $State -NotePropertyName StagePath -NotePropertyValue $StagePath
        Add-Member -InputObject $State -NotePropertyName BackupPath -NotePropertyValue $BackupPath
    }

    foreach ($State in $States | Where-Object Changed) {
        [IO.File]::Replace($State.StagePath, $State.FullPath, $State.BackupPath)
        $Committed.Add($State)
    }
}
catch {
    $CommitError = $_

    foreach ($State in $Committed) {
        $DiscardPath = Join-Path $StageRoot ([Guid]::NewGuid().ToString('N') + '.discard')
        [IO.File]::Replace($State.BackupPath, $State.FullPath, $DiscardPath)
        [IO.File]::SetLastWriteTimeUtc($State.FullPath, $State.OriginalWriteTimeUtc)
    }

    throw $CommitError
}
finally {
    $ExpectedStagePrefix = $ArtifactsTemp.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar

    if ($StageRoot.StartsWith($ExpectedStagePrefix, [StringComparison]::OrdinalIgnoreCase) -and
        [IO.Directory]::Exists($StageRoot)) {
        Remove-Item -LiteralPath $StageRoot -Recurse -Force
    }
}

Remove-SymbolCache
Write-Output ("Formatted {0} of {1} SMILE file(s)." -f $ChangedFiles.Count, $TargetFiles.Count)
