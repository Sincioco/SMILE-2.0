param(
    [switch]$Check
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$RepositoryRoot = Split-Path -Parent $PSScriptRoot
$TutorialRoot = Join-Path $RepositoryRoot 'tutorials\Snake'
$ManifestPath = Join-Path $TutorialRoot 'tutorial-manifest.json'
$ProjectPath = Join-Path $RepositoryRoot 'games\Snake\Snake.smileproj'
$ProjectSnapshotPath = Join-Path $TutorialRoot 'assets\code\Snake.smileproj'
$Utf8WithoutBom = [Text.UTF8Encoding]::new($false)
$Pending = [Collections.Generic.List[string]]::new()

function Normalize-Text {
    param([string]$Text)

    return $Text.Replace("`r`n", "`n").Replace("`r", "`n")
}

function Split-Lines {
    param([string]$Text)

    $Normalized = (Normalize-Text $Text).TrimEnd("`n")
    return ,@($Normalized.Split("`n"))
}

function Encode-Code {
    param([string]$Text)

    return $Text.Replace('&', '&amp;').Replace('<', '&lt;').Replace('>', '&gt;')
}

function Get-GitBlobText {
    param(
        [string]$BlobHash,
        [string]$Fallback
    )

    if ([string]::IsNullOrWhiteSpace($BlobHash)) {
        return $Fallback
    }

    $PreviousErrorActionPreference = $ErrorActionPreference

    try {
        $ErrorActionPreference = 'Continue'
        $Text = (& git -C $RepositoryRoot cat-file -p $BlobHash 2>$null) -join "`n"
        $GitExitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $PreviousErrorActionPreference
    }

    if ($GitExitCode -ne 0) {
        return $Fallback
    }

    return $Text
}

function New-LineMap {
    param(
        [string[]]$PreviousLines,
        [string[]]$CurrentLines
    )

    $Map = @{}
    $CurrentIndexes = @{}

    for ($CurrentIndex = 0; $CurrentIndex -lt $CurrentLines.Count; $CurrentIndex++) {
        $Line = $CurrentLines[$CurrentIndex]

        if ([string]::IsNullOrWhiteSpace($Line)) {
            continue
        }

        if (-not $CurrentIndexes.ContainsKey($Line)) {
            $CurrentIndexes[$Line] = [Collections.Generic.List[int]]::new()
        }

        $CurrentIndexes[$Line].Add($CurrentIndex + 1)
    }

    for ($PreviousIndex = 0; $PreviousIndex -lt $PreviousLines.Count; $PreviousIndex++) {
        $Line = $PreviousLines[$PreviousIndex]

        if ([string]::IsNullOrWhiteSpace($Line) -or -not $CurrentIndexes.ContainsKey($Line)) {
            continue
        }

        $Candidates = $CurrentIndexes[$Line]

        if ($Candidates.Count -eq 1) {
            $Map[$PreviousIndex + 1] = $Candidates[0]
        }
    }

    for ($PreviousIndex = 0; $PreviousIndex -lt $PreviousLines.Count; $PreviousIndex++) {
        $LineNumber = $PreviousIndex + 1
        $Line = $PreviousLines[$PreviousIndex]

        if ($Map.ContainsKey($LineNumber) -or [string]::IsNullOrWhiteSpace($Line) -or
            -not $CurrentIndexes.ContainsKey($Line) -or $Map.Count -eq 0) {
            continue
        }

        $Anchor = $Map.Keys |
            Sort-Object { [Math]::Abs([int]$_ - $LineNumber) } |
            Select-Object -First 1
        $Expected = [int]$Map[$Anchor] + $LineNumber - [int]$Anchor
        $Mapped = $CurrentIndexes[$Line] |
            Sort-Object { [Math]::Abs([int]$_ - $Expected) } |
            Select-Object -First 1
        $Map[$LineNumber] = [int]$Mapped
    }

    return $Map
}

function Get-MappedLine {
    param(
        [hashtable]$Source,
        [int]$Line,
        [switch]$PreferFollowing
    )

    if ($Source.LineMap.ContainsKey($Line)) {
        return [int]$Source.LineMap[$Line]
    }

    for ($Distance = 1; $Distance -le $Source.PreviousLines.Count; $Distance++) {
        $First = if ($PreferFollowing) { $Line + $Distance } else { $Line - $Distance }
        $Second = if ($PreferFollowing) { $Line - $Distance } else { $Line + $Distance }

        if ($Source.LineMap.ContainsKey($First)) {
            return [int]$Source.LineMap[$First]
        }

        if ($Source.LineMap.ContainsKey($Second)) {
            return [int]$Source.LineMap[$Second]
        }
    }

    return [Math]::Max(1, [Math]::Min($Line, $Source.CurrentLines.Count))
}

function Find-ExactBlockStart {
    param(
        [string[]]$SourceLines,
        [string[]]$BlockLines,
        [int]$ExpectedStart
    )

    if ($BlockLines.Count -eq 0 -or $BlockLines.Count -gt $SourceLines.Count) {
        return 0
    }

    $Candidates = [Collections.Generic.List[int]]::new()

    for ($Start = 0; $Start -le $SourceLines.Count - $BlockLines.Count; $Start++) {
        $Matches = $true

        for ($Offset = 0; $Offset -lt $BlockLines.Count; $Offset++) {
            if ($SourceLines[$Start + $Offset] -cne $BlockLines[$Offset]) {
                $Matches = $false
                break
            }
        }

        if ($Matches) {
            $Candidates.Add($Start + 1)
        }
    }

    if ($Candidates.Count -eq 0) {
        return 0
    }

    return [int]($Candidates | Sort-Object { [Math]::Abs($_ - $ExpectedStart) } | Select-Object -First 1)
}

function Set-ExpectedText {
    param(
        [string]$Path,
        [string]$Text
    )

    $Expected = Normalize-Text $Text

    if (-not $Expected.EndsWith("`n", [StringComparison]::Ordinal)) {
        $Expected += "`n"
    }

    $Existing = if (Test-Path -LiteralPath $Path) {
        Normalize-Text ([IO.File]::ReadAllText($Path))
    }
    else {
        $null
    }

    if ($Existing -ceq $Expected) {
        return
    }

    if ($Check) {
        $Pending.Add($Path)
        return
    }

    [IO.File]::WriteAllText($Path, $Expected, $Utf8WithoutBom)
}

$ManifestText = [IO.File]::ReadAllText($ManifestPath)
$Manifest = $ManifestText | ConvertFrom-Json
$LegacyHashMatch = [regex]::Match($ManifestText, '"sourceBlobSha"\s*:\s*"(?<hash>[0-9a-f]+)"')
$PreviousHashes = @{}

if ($Manifest.PSObject.Properties.Name -contains 'sourceFiles') {
    foreach ($SourceFile in $Manifest.sourceFiles) {
        $PreviousHashes[[string]$SourceFile.id] = [string]$SourceFile.blobSha
    }
}
elseif ($LegacyHashMatch.Success) {
    $PreviousHashes['program'] = $LegacyHashMatch.Groups['hash'].Value
}

$SourceDefinitions = @(
    [ordered]@{
        Id = 'program'
        RelativePath = 'games/Snake/Program-NoDemo.smile'
        Snapshot = 'assets/code/Program-NoDemo.smile'
        AnchorPrefix = 'source-line'
    },
    [ordered]@{
        Id = 'model'
        RelativePath = 'games/Snake/SnakeModel.smile'
        Snapshot = 'assets/code/SnakeModel.smile'
        AnchorPrefix = 'model-line'
    }
)
$Sources = @{}

foreach ($Definition in $SourceDefinitions) {
    $Path = Join-Path $RepositoryRoot $Definition.RelativePath
    $CurrentText = Normalize-Text ([IO.File]::ReadAllText($Path))
    $CurrentHash = (& git -C $RepositoryRoot hash-object $Definition.RelativePath).Trim()
    $PreviousHash = if ($PreviousHashes.ContainsKey($Definition.Id)) {
        $PreviousHashes[$Definition.Id]
    }
    else {
        $CurrentHash
    }
    $SnapshotPath = Join-Path $TutorialRoot $Definition.Snapshot
    $FallbackText = if (Test-Path -LiteralPath $SnapshotPath) {
        Normalize-Text ([IO.File]::ReadAllText($SnapshotPath))
    }
    else {
        $CurrentText
    }
    $PreviousText = if ([string]::Equals($PreviousHash, $CurrentHash,
        [StringComparison]::OrdinalIgnoreCase)) {
        $FallbackText
    }
    else {
        Get-GitBlobText $PreviousHash $FallbackText
    }
    $PreviousLines = Split-Lines $PreviousText
    $CurrentLines = Split-Lines $CurrentText
    $Sources[$Definition.Id] = @{
        Definition = $Definition
        Path = $Path
        CurrentText = $CurrentText
        CurrentHash = $CurrentHash
        CurrentLines = $CurrentLines
        PreviousLines = $PreviousLines
        LineMap = New-LineMap $PreviousLines $CurrentLines
    }
}

$CodePattern = [regex]::new('<code(?<attributes>[^>]*class="language-smile"[^>]*)>(?<body>.*?)</code>',
    [Text.RegularExpressions.RegexOptions]::Singleline)
$LinkPattern = [regex]::new('<a(?<attributes>[^>]*(?:source-range-link|source-line-link|syntax-line-link)[^>]*)>(?<body>.*?)</a>',
    [Text.RegularExpressions.RegexOptions]::Singleline)

foreach ($HtmlFile in Get-ChildItem -LiteralPath $TutorialRoot -Filter '*.html' -File) {
    $Existing = [IO.File]::ReadAllText($HtmlFile.FullName)
    $Updated = $CodePattern.Replace($Existing, {
        param($Match)

        $Attributes = $Match.Groups['attributes'].Value
        $SourceMatch = [regex]::Match($Attributes, 'data-source-file="(?<id>[^"]+)"')
        $SourceId = if ($SourceMatch.Success) { $SourceMatch.Groups['id'].Value } else { 'program' }

        if (-not $Sources.ContainsKey($SourceId)) {
            throw "Unknown Snake tutorial source id '$SourceId' in $($HtmlFile.Name)."
        }

        $StartMatch = [regex]::Match($Attributes, 'data-source-start="(?<line>\d+)"')

        if (-not $StartMatch.Success) {
            return $Match.Value
        }

        $Source = $Sources[$SourceId]
        $OldStart = [int]$StartMatch.Groups['line'].Value
        $IsCurrent = $Attributes -match 'data-source-current="true"'

        if ($Attributes -match 'data-source-anchor="true"') {
            $NewStart = 1
            $ReplacementText = $Source.CurrentText.TrimEnd("`n")
        }
        else {
            $BlockLines = Split-Lines ([Net.WebUtility]::HtmlDecode($Match.Groups['body'].Value))
            $ExactStart = Find-ExactBlockStart $Source.CurrentLines $BlockLines $OldStart

            if ($IsCurrent) {
                $NewStart = $OldStart
            }
            elseif ($ExactStart -gt 0) {
                $NewStart = $ExactStart
            }
            else {
                $NewStart = Get-MappedLine $Source $OldStart -PreferFollowing
            }

            $NewEnd = $NewStart + $BlockLines.Count - 1

            if ($NewStart -lt 1 -or $NewEnd -gt $Source.CurrentLines.Count) {
                throw "Snake tutorial block $($HtmlFile.Name):$OldStart exceeds $SourceId source bounds."
            }

            $ReplacementText = $Source.CurrentLines[($NewStart - 1)..($NewEnd - 1)] -join "`n"
        }

        $NewAttributes = [regex]::Replace($Attributes, 'data-source-start="\d+"',
            "data-source-start=`"$NewStart`"")
        $NewAttributes = [regex]::Replace($NewAttributes, '\s+data-source-current="true"', '')

        if (-not $SourceMatch.Success) {
            $NewAttributes += " data-source-file=`"$SourceId`""
        }

        return '<code' + $NewAttributes + '>' + (Encode-Code $ReplacementText) + '</code>'
    })

    $Updated = $LinkPattern.Replace($Updated, {
        param($Match)

        $Attributes = $Match.Groups['attributes'].Value
        $Body = $Match.Groups['body'].Value
        $SourceMatch = [regex]::Match($Attributes, 'data-source-file="(?<id>[^"]+)"')
        $SourceId = if ($SourceMatch.Success) { $SourceMatch.Groups['id'].Value } else { 'program' }

        if (-not $Sources.ContainsKey($SourceId)) {
            throw "Unknown Snake tutorial link source id '$SourceId' in $($HtmlFile.Name)."
        }

        $Source = $Sources[$SourceId]
        $IsCurrent = $Attributes -match 'data-source-current="true"'
        $Prefix = $Source.Definition.AnchorPrefix

        $MapLine = {
            param([int]$Line, [bool]$Following)

            if ($IsCurrent) {
                return $Line
            }

            return Get-MappedLine $Source $Line -PreferFollowing:$Following
        }

        $NewAttributes = [regex]::Replace($Attributes, '(?:source-line|model-line)-(?<line>\d+)', {
            param($LineMatch)
            return $Prefix + '-' + (& $MapLine ([int]$LineMatch.Groups['line'].Value) $true)
        })
        $NewAttributes = [regex]::Replace($NewAttributes, 'data-line-start="(?<line>\d+)"', {
            param($LineMatch)
            return 'data-line-start="' + (& $MapLine ([int]$LineMatch.Groups['line'].Value) $true) + '"'
        })
        $NewAttributes = [regex]::Replace($NewAttributes, 'data-line-end="(?<line>\d+)"', {
            param($LineMatch)
            return 'data-line-end="' + (& $MapLine ([int]$LineMatch.Groups['line'].Value) $false) + '"'
        })
        $NewAttributes = [regex]::Replace($NewAttributes, '\s+data-source-current="true"', '')

        if (-not $SourceMatch.Success) {
            $NewAttributes += " data-source-file=`"$SourceId`""
        }

        $RangeStart = [regex]::Match($NewAttributes, 'data-line-start="(?<line>\d+)"')
        $RangeEnd = [regex]::Match($NewAttributes, 'data-line-end="(?<line>\d+)"')

        if ($RangeStart.Success -and $RangeEnd.Success) {
            $StartLine = [int]$RangeStart.Groups['line'].Value
            $EndLine = [int]$RangeEnd.Groups['line'].Value

            if ($StartLine -lt 1 -or $EndLine -lt $StartLine -or $EndLine -gt $Source.CurrentLines.Count) {
                throw "Snake tutorial link $($HtmlFile.Name) has invalid $SourceId range $StartLine-$EndLine."
            }
        }

        $Anchor = [regex]::Match($NewAttributes, '#(?:source-line|model-line)-(?<line>\d+)')

        if ($Anchor.Success) {
            $AnchorLine = [int]$Anchor.Groups['line'].Value

            if ($AnchorLine -lt 1 -or $AnchorLine -gt $Source.CurrentLines.Count) {
                throw "Snake tutorial link $($HtmlFile.Name) has invalid $SourceId anchor $AnchorLine."
            }
        }

        $NewBody = [regex]::Replace($Body, '\d+', {
            param($NumberMatch)
            return [string](& $MapLine ([int]$NumberMatch.Value) $true)
        })

        return '<a' + $NewAttributes + '>' + $NewBody + '</a>'
    })

    if ($HtmlFile.Name -eq '19-complete-source.html') {
        $Updated = [regex]::Replace($Updated, 'Program blob Sha <code>[0-9a-f]+</code>',
            "Program blob Sha <code>$($Sources.program.CurrentHash)</code>")
        $Updated = [regex]::Replace($Updated, 'SnakeModel blob Sha <code>[0-9a-f]+</code>',
            "SnakeModel blob Sha <code>$($Sources.model.CurrentHash)</code>")
    }

    Set-ExpectedText $HtmlFile.FullName $Updated
}

foreach ($SourceId in $Sources.Keys) {
    $Source = $Sources[$SourceId]
    $SnapshotPath = Join-Path $TutorialRoot $Source.Definition.Snapshot
    Set-ExpectedText $SnapshotPath $Source.CurrentText
}

Set-ExpectedText $ProjectSnapshotPath ([IO.File]::ReadAllText($ProjectPath))

$ManifestJson = @"
{
  "product": "SMILE 2.0 (SinBASIC)",
  "tutorial": "Snake Visual Tutorial",
  "tutorialPath": "<SMILE-2.0-root>\\tutorials\\Snake",
  "gamePath": "<SMILE-2.0-root>\\games\\Snake",
  "relativeGamePath": "../../games/Snake",
  "entryPoint": "index.html",
  "topicCount": 20,
  "sourceFiles": [
    {
      "id": "program",
      "path": "games/Snake/Program-NoDemo.smile",
      "snapshot": "assets/code/Program-NoDemo.smile",
      "blobSha": "$($Sources.program.CurrentHash)",
      "lineCount": $($Sources.program.CurrentLines.Count),
      "anchorPrefix": "source-line"
    },
    {
      "id": "model",
      "path": "games/Snake/SnakeModel.smile",
      "snapshot": "assets/code/SnakeModel.smile",
      "blobSha": "$($Sources.model.CurrentHash)",
      "lineCount": $($Sources.model.CurrentLines.Count),
      "anchorPrefix": "model-line"
    }
  ],
  "sourceMap": {
    "page": "19-complete-source.html",
    "programAnchor": "source-line-{line}",
    "modelAnchor": "model-line-{line}",
    "excerptAttributes": "data-source-file + data-source-start"
  },
  "language": "HTML, CSS, pure JavaScript",
  "copyrightYear": 2026,
  "author": "Louiery R. Sincioco (Sin)",
  "version": "2.0",
  "features": [
    "portable repository-relative paths",
    "two synchronized official SMILE sources",
    "per-source canonical line numbers and anchors",
    "persistent sidebar scroll position",
    "clickable full-source map",
    "syntax examples linked to real Snake source",
    "repetition and reinforcement prompts",
    "two-line muted hyperlink footer"
  ]
}
"@
Set-ExpectedText $ManifestPath $ManifestJson

if ($Check -and $Pending.Count -gt 0) {
    $RepositoryPrefix = $RepositoryRoot.TrimEnd('\') + '\'
    $Relative = $Pending | ForEach-Object {
        if ($_.StartsWith($RepositoryPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            $_.Substring($RepositoryPrefix.Length)
        }
        else {
            $_
        }
    }
    throw "Snake tutorial synchronization is stale: $($Relative -join ', ')"
}

$ProgramLines = $Sources.program.CurrentLines.Count
$ModelLines = $Sources.model.CurrentLines.Count
$Action = if ($Check) { 'Verified' } else { 'Synchronized' }
Write-Output "$Action the Snake tutorial ($ProgramLines program lines, $ModelLines model lines)."
