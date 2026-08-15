param(
    [string]$BaselineRevision,
    [string]$BaselineHtmlRevision
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$RepositoryRoot = Split-Path -Parent $PSScriptRoot
$TutorialRoot = Join-Path $RepositoryRoot 'tutorials\Snake'
$SourceRelativePath = 'games/Snake/Program-NoDemo.smile'
$SourcePath = Join-Path $RepositoryRoot $SourceRelativePath
$SnapshotPath = Join-Path $TutorialRoot 'assets\code\Program-NoDemo.smile'
$ManifestPath = Join-Path $TutorialRoot 'tutorial-manifest.json'
$Utf8WithoutBom = [Text.UTF8Encoding]::new($false)

function Split-Lines {
    param([string]$Text)

    $Normalized = $Text.Replace("`r`n", "`n").Replace("`r", "`n")

    if ($Normalized.EndsWith("`n", [StringComparison]::Ordinal)) {
        $Normalized = $Normalized.Substring(0, $Normalized.Length - 1)
    }

    $Lines = @($Normalized.Split("`n"))
    return ,$Lines
}

function Encode-Code {
    param([string]$Text)

    return $Text.Replace('&', '&amp;').Replace('<', '&lt;').Replace('>', '&gt;')
}

$CurrentText = [IO.File]::ReadAllText($SourcePath).Replace("`r`n", "`n").Replace("`r", "`n")
$CurrentBlobHash = (& git -C $RepositoryRoot hash-object $SourceRelativePath).Trim()

if ([string]::IsNullOrWhiteSpace($BaselineRevision)) {
    if ([string]::IsNullOrWhiteSpace($BaselineHtmlRevision)) {
        $ManifestText = [IO.File]::ReadAllText($ManifestPath)
    }
    else {
        $ManifestRelativePath = 'tutorials/Snake/tutorial-manifest.json'
        $ManifestText = (& git -C $RepositoryRoot show "${BaselineHtmlRevision}:$ManifestRelativePath") -join "`n"

        if ($LASTEXITCODE -ne 0) {
            throw "Unable to read $ManifestRelativePath from $BaselineHtmlRevision."
        }
    }

    $ManifestHashMatch = [regex]::Match($ManifestText, '"sourceBlobSha": "(?<hash>[0-9a-f]+)"')

    if (-not $ManifestHashMatch.Success) {
        throw 'The Snake tutorial manifest does not contain sourceBlobSha.'
    }

    $BaselineHash = $ManifestHashMatch.Groups['hash'].Value
    $BaselineDescription = $BaselineHash

    if ($BaselineHash -eq $CurrentBlobHash) {
        $PreviousText = $CurrentText
    }
    else {
        $PreviousText = (& git -C $RepositoryRoot cat-file -p $BaselineHash) -join "`n"
    }
}
else {
    $BaselineDescription = $BaselineRevision
    $PreviousText = (& git -C $RepositoryRoot show "${BaselineRevision}:$SourceRelativePath") -join "`n"
}

if ($LASTEXITCODE -ne 0) {
    throw "Unable to read $SourceRelativePath from $BaselineDescription."
}

$PreviousLines = Split-Lines $PreviousText
$CurrentLines = Split-Lines $CurrentText
$LineMap = @{}
$CurrentLineIndexes = @{}

for ($CurrentIndex = 0; $CurrentIndex -lt $CurrentLines.Count; $CurrentIndex++) {
    $CurrentLine = $CurrentLines[$CurrentIndex]

    if ([string]::IsNullOrWhiteSpace($CurrentLine)) {
        continue
    }

    if (-not $CurrentLineIndexes.ContainsKey($CurrentLine)) {
        $CurrentLineIndexes[$CurrentLine] = [Collections.Generic.List[int]]::new()
    }

    $CurrentLineIndexes[$CurrentLine].Add($CurrentIndex + 1)
}

for ($PreviousIndex = 0; $PreviousIndex -lt $PreviousLines.Count; $PreviousIndex++) {
    $PreviousLine = $PreviousLines[$PreviousIndex]

    if ([string]::IsNullOrWhiteSpace($PreviousLine) -or
        -not $CurrentLineIndexes.ContainsKey($PreviousLine)) {
        continue
    }

    $Candidates = $CurrentLineIndexes[$PreviousLine]

    if ($Candidates.Count -eq 1) {
        $LineMap[$PreviousIndex + 1] = $Candidates[0]
    }
}

for ($PreviousIndex = 0; $PreviousIndex -lt $PreviousLines.Count; $PreviousIndex++) {
    $PreviousLineNumber = $PreviousIndex + 1
    $PreviousLine = $PreviousLines[$PreviousIndex]

    if ($LineMap.ContainsKey($PreviousLineNumber) -or
        [string]::IsNullOrWhiteSpace($PreviousLine) -or
        -not $CurrentLineIndexes.ContainsKey($PreviousLine)) {
        continue
    }

    $AnchorLine = $LineMap.Keys |
        Sort-Object { [Math]::Abs([int]$_ - $PreviousLineNumber) } |
        Select-Object -First 1

    if ($null -eq $AnchorLine) {
        continue
    }

    $ExpectedLine = [int]$LineMap[$AnchorLine] + $PreviousLineNumber - [int]$AnchorLine
    $MappedLine = $CurrentLineIndexes[$PreviousLine] |
        Sort-Object { [Math]::Abs([int]$_ - $ExpectedLine) } |
        Select-Object -First 1
    $LineMap[$PreviousLineNumber] = [int]$MappedLine
}

function Get-MappedLine {
    param(
        [int]$Line,
        [switch]$PreferFollowing
    )

    if ($LineMap.ContainsKey($Line)) {
        return [int]$LineMap[$Line]
    }

    for ($Distance = 1; $Distance -le $PreviousLines.Count; $Distance++) {
        $First = if ($PreferFollowing) { $Line + $Distance } else { $Line - $Distance }
        $Second = if ($PreferFollowing) { $Line - $Distance } else { $Line + $Distance }

        if ($LineMap.ContainsKey($First)) {
            return [int]$LineMap[$First]
        }

        if ($LineMap.ContainsKey($Second)) {
            return [int]$LineMap[$Second]
        }
    }

    throw "Unable to map baseline source line $Line."
}

$CodePattern = [regex]::new('<code(?<attributes>[^>]*class="language-smile"[^>]*)>(?<body>.*?)</code>',
    [Text.RegularExpressions.RegexOptions]::Singleline)
$AnchorPattern = [regex]::new('<a(?<attributes>[^>]*(?:source-line-|source-range-link)[^>]*)>(?<body>.*?)</a>',
    [Text.RegularExpressions.RegexOptions]::Singleline)
$HtmlFiles = Get-ChildItem -LiteralPath $TutorialRoot -Filter '*.html' -File
$HtmlRevision = $BaselineHtmlRevision
$PendingHtml = @{}

if ([string]::IsNullOrWhiteSpace($HtmlRevision)) {
    $HtmlRevision = $BaselineRevision
}

foreach ($HtmlFile in $HtmlFiles) {
    $ExistingHtml = [IO.File]::ReadAllText($HtmlFile.FullName)

    if ([string]::IsNullOrWhiteSpace($HtmlRevision)) {
        $Html = $ExistingHtml
    }
    else {
        $RepositoryPrefix = $RepositoryRoot.TrimEnd('\') + '\'

        if (-not $HtmlFile.FullName.StartsWith($RepositoryPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Tutorial file is outside the repository: $($HtmlFile.FullName)"
        }

        $HtmlRelativePath = $HtmlFile.FullName.Substring($RepositoryPrefix.Length).Replace('\', '/')
        $Html = (& git -C $RepositoryRoot show "${HtmlRevision}:$HtmlRelativePath") -join "`n"

        if ($LASTEXITCODE -ne 0) {
            throw "Unable to read $HtmlRelativePath from $HtmlRevision."
        }
    }

    $Updated = $CodePattern.Replace($Html, {
        param($Match)

        $Attributes = $Match.Groups['attributes'].Value
        $StartMatch = [regex]::Match($Attributes, 'data-source-start="(?<line>\d+)"')

        if (-not $StartMatch.Success) {
            return $Match.Value
        }

        $OldStart = [int]$StartMatch.Groups['line'].Value
        $NewStart = Get-MappedLine $OldStart -PreferFollowing

        if ($Attributes -match 'data-source-anchor="true"') {
            $ReplacementText = $CurrentText.TrimEnd("`n")
        }
        else {
            $Decoded = [Net.WebUtility]::HtmlDecode($Match.Groups['body'].Value)
            $OldBlockLines = Split-Lines $Decoded
            $OldEnd = $OldStart + $OldBlockLines.Count - 1
            $NewEnd = Get-MappedLine $OldEnd

            if ($NewEnd -lt $NewStart) {
                throw "Mapped $($HtmlFile.Name) source block $OldStart-$OldEnd to invalid range $NewStart-$NewEnd."
            }

            $ReplacementText = $CurrentLines[($NewStart - 1)..($NewEnd - 1)] -join "`n"
        }

        $NewAttributes = [regex]::Replace($Attributes, 'data-source-start="\d+"',
            "data-source-start=`"$NewStart`"")
        return '<code' + $NewAttributes + '>' + (Encode-Code $ReplacementText) + '</code>'
    })

    $Updated = $AnchorPattern.Replace($Updated, {
        param($Match)

        $Attributes = $Match.Groups['attributes'].Value
        $Body = $Match.Groups['body'].Value

        $NewAttributes = [regex]::Replace($Attributes, 'source-line-(?<line>\d+)', {
            param($LineMatch)
            return 'source-line-' + (Get-MappedLine ([int]$LineMatch.Groups['line'].Value) -PreferFollowing)
        })
        $NewAttributes = [regex]::Replace($NewAttributes, 'data-line-start="(?<line>\d+)"', {
            param($LineMatch)
            return 'data-line-start="' +
                (Get-MappedLine ([int]$LineMatch.Groups['line'].Value) -PreferFollowing) + '"'
        })
        $NewAttributes = [regex]::Replace($NewAttributes, 'data-line-end="(?<line>\d+)"', {
            param($LineMatch)
            return 'data-line-end="' +
                (Get-MappedLine ([int]$LineMatch.Groups['line'].Value)) + '"'
        })

        $RangeStartMatch = [regex]::Match($NewAttributes, 'data-line-start="(?<line>\d+)"')
        $RangeEndMatch = [regex]::Match($NewAttributes, 'data-line-end="(?<line>\d+)"')

        if ($RangeStartMatch.Success -and $RangeEndMatch.Success -and
            [int]$RangeEndMatch.Groups['line'].Value -lt [int]$RangeStartMatch.Groups['line'].Value) {
            throw "Mapped $($HtmlFile.Name) source link to an invalid descending range."
        }

        $NewBody = [regex]::Replace($Body, '\d+', {
            param($NumberMatch)
            return [string](Get-MappedLine ([int]$NumberMatch.Value) -PreferFollowing)
        })

        return '<a' + $NewAttributes + '>' + $NewBody + '</a>'
    })

    if ($HtmlFile.Name -eq '19-complete-source.html') {
        $BlobHash = (& git -C $RepositoryRoot hash-object $SourceRelativePath).Trim()
        $Updated = [regex]::Replace($Updated, 'blob Sha <code>[0-9a-f]+</code>',
            "blob Sha <code>$BlobHash</code>")
    }

    if ($Updated -cne $ExistingHtml) {
        $PendingHtml[$HtmlFile.FullName] = $Updated
    }
}

foreach ($PendingPath in $PendingHtml.Keys) {
    [IO.File]::WriteAllText($PendingPath, $PendingHtml[$PendingPath], $Utf8WithoutBom)
}

[IO.File]::WriteAllText($SnapshotPath, $CurrentText, $Utf8WithoutBom)

$Manifest = [IO.File]::ReadAllText($ManifestPath)
$Manifest = [regex]::Replace($Manifest, '"sourceBlobSha": "[0-9a-f]+"',
    '"sourceBlobSha": "' + $CurrentBlobHash + '"')
[IO.File]::WriteAllText($ManifestPath, $Manifest, $Utf8WithoutBom)

Write-Output "Synchronized the Snake tutorial to $SourceRelativePath ($($CurrentLines.Count) lines, blob $CurrentBlobHash)."
