param(
    [string]$BaselineRevision
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
    $ManifestText = [IO.File]::ReadAllText($ManifestPath)
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
$CurrentIndex = 0

for ($PreviousIndex = 0; $PreviousIndex -lt $PreviousLines.Count; $PreviousIndex++) {
    if ([string]::IsNullOrWhiteSpace($PreviousLines[$PreviousIndex])) {
        continue
    }

    while ($CurrentIndex -lt $CurrentLines.Count -and
        $CurrentLines[$CurrentIndex] -ine $PreviousLines[$PreviousIndex]) {
        $CurrentIndex++
    }

    if ($CurrentIndex -ge $CurrentLines.Count) {
        throw "Unable to map baseline source line $($PreviousIndex + 1): $($PreviousLines[$PreviousIndex])"
    }

    $LineMap[$PreviousIndex + 1] = $CurrentIndex + 1
    $CurrentIndex++
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

foreach ($HtmlFile in $HtmlFiles) {
    $Html = [IO.File]::ReadAllText($HtmlFile.FullName)
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

    if ($Updated -cne $Html) {
        [IO.File]::WriteAllText($HtmlFile.FullName, $Updated, $Utf8WithoutBom)
    }
}

[IO.File]::WriteAllText($SnapshotPath, $CurrentText, $Utf8WithoutBom)

$Manifest = [IO.File]::ReadAllText($ManifestPath)
$Manifest = [regex]::Replace($Manifest, '"sourceBlobSha": "[0-9a-f]+"',
    '"sourceBlobSha": "' + $CurrentBlobHash + '"')
[IO.File]::WriteAllText($ManifestPath, $Manifest, $Utf8WithoutBom)

Write-Output "Synchronized the Snake tutorial to $SourceRelativePath ($($CurrentLines.Count) lines, blob $CurrentBlobHash)."
