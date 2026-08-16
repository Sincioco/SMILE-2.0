[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $PrivateRoot,

    [string] $PublicRoot = ''
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if ([string]::IsNullOrWhiteSpace($PublicRoot)) {
    $PublicRoot = Split-Path -Parent $PSScriptRoot
}

$PrivateRoot = [IO.Path]::GetFullPath($PrivateRoot)
$PublicRoot = [IO.Path]::GetFullPath($PublicRoot)

if (-not (Test-Path -LiteralPath $PrivateRoot -PathType Container)) {
    throw "Private reference root does not exist: $PrivateRoot"
}
if (-not (Test-Path -LiteralPath $PublicRoot -PathType Container)) {
    throw "Public repository root does not exist: $PublicRoot"
}
if ($PrivateRoot.Equals($PublicRoot, [StringComparison]::OrdinalIgnoreCase) -or
    $PrivateRoot.StartsWith($PublicRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or
    $PublicRoot.StartsWith($PrivateRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Private and public roots must be separate directory trees.'
}

function Get-HashHex {
    param([Parameter(Mandatory)] [AllowEmptyCollection()] [byte[]] $Bytes)

    $Algorithm = [Security.Cryptography.SHA256]::Create()

    try {
        return ([BitConverter]::ToString($Algorithm.ComputeHash($Bytes))).Replace('-', '')
    }
    finally {
        $Algorithm.Dispose()
    }
}

function Get-FileHashHex {
    param([Parameter(Mandatory)] [string] $Path)

    return Get-HashHex ([IO.File]::ReadAllBytes($Path))
}

function Test-ExcludedPrivatePath {
    param([Parameter(Mandatory)] [string] $Path)

    $Relative = $Path.Substring($PrivateRoot.Length).TrimStart('\', '/')
    return $Relative -match '^(?:\.vs|bin|obj)(?:[\/]|$)'
}

function Test-ExcludedPublicPath {
    param([Parameter(Mandatory)] [string] $Path)

    $Relative = $Path.Substring($PublicRoot.Length).TrimStart('\', '/')
    return $Relative -match '^(?:\.git|\.vs)(?:[\/]|$)' -or
        $Relative -match '(?:^|[\/])(?:bin|obj)(?:[\/]|$)'
}

$PrivateFiles = @(Get-ChildItem -LiteralPath $PrivateRoot -Recurse -File |
    Where-Object { -not (Test-ExcludedPrivatePath $_.FullName) })
$PrivateHashes = @{}

foreach ($File in $PrivateFiles) {
    $Hash = Get-FileHashHex $File.FullName

    if (-not $PrivateHashes.ContainsKey($Hash)) {
        $PrivateHashes[$Hash] = [Collections.Generic.List[string]]::new()
    }

    $PrivateHashes[$Hash].Add($File.FullName)
}

$PublicFiles = @(Get-ChildItem -LiteralPath $PublicRoot -Recurse -File |
    Where-Object { -not (Test-ExcludedPublicPath $_.FullName) })
$RawMatches = [Collections.Generic.List[string]]::new()

foreach ($File in $PublicFiles) {
    $Hash = Get-FileHashHex $File.FullName

    if ($PrivateHashes.ContainsKey($Hash)) {
        $RawMatches.Add("$($File.FullName) <= $($PrivateHashes[$Hash] -join ', ')")
    }
}

Add-Type -AssemblyName System.IO.Compression
$ArchiveExtensions = [Collections.Generic.HashSet[string]]::new(
    [string[]] @('.zip', '.vsix', '.nupkg', '.smilelib'),
    [StringComparer]::OrdinalIgnoreCase)
$ArchiveFiles = @($PublicFiles | Where-Object { $ArchiveExtensions.Contains($_.Extension) })
$ArchiveEntryCount = 0
$ArchiveMatches = [Collections.Generic.List[string]]::new()

foreach ($ArchiveFile in $ArchiveFiles) {
    $Stream = [IO.File]::OpenRead($ArchiveFile.FullName)
    $Archive = $null

    try {
        $Archive = [IO.Compression.ZipArchive]::new($Stream, [IO.Compression.ZipArchiveMode]::Read, $false)

        foreach ($Entry in $Archive.Entries) {
            if ([string]::IsNullOrEmpty($Entry.Name)) {
                continue
            }

            $ArchiveEntryCount++
            $EntryStream = $Entry.Open()
            $Memory = [IO.MemoryStream]::new()

            try {
                $EntryStream.CopyTo($Memory)
                $Hash = Get-HashHex $Memory.ToArray()
            }
            finally {
                $Memory.Dispose()
                $EntryStream.Dispose()
            }

            if ($PrivateHashes.ContainsKey($Hash)) {
                $ArchiveMatches.Add("$($ArchiveFile.FullName)!$($Entry.FullName) <= $($PrivateHashes[$Hash] -join ', ')")
            }
        }
    }
    catch [IO.InvalidDataException] {
        throw "Public archive could not be audited: $($ArchiveFile.FullName)"
    }
    finally {
        if ($null -ne $Archive) {
            $Archive.Dispose()
        }

        $Stream.Dispose()
    }
}

$PrivateWebDirectories = @(Get-ChildItem -LiteralPath $PrivateRoot -Recurse -Directory |
    Where-Object { $_.Name -in @('web', 'wwwroot') })

if ($RawMatches.Count -ne 0 -or $ArchiveMatches.Count -ne 0 -or $PrivateWebDirectories.Count -ne 0) {
    if ($RawMatches.Count -ne 0) {
        Write-Error ("Private/public raw SHA-256 matches:`n" + ($RawMatches -join [Environment]::NewLine))
    }
    if ($ArchiveMatches.Count -ne 0) {
        Write-Error ("Private/public archive-entry SHA-256 matches:`n" + ($ArchiveMatches -join [Environment]::NewLine))
    }
    if ($PrivateWebDirectories.Count -ne 0) {
        Write-Error ("Forbidden private Web directories:`n" + ($PrivateWebDirectories.FullName -join [Environment]::NewLine))
    }

    throw 'Private/public copyright boundary audit failed.'
}

$SuccessMessage = "Private/public SHA-256 audit passed: {0} private files / {1} unique hashes, {2} public files, " +
    "{3} public archives / {4} entries, 0 raw matches, 0 archive-entry matches, 0 private Web directories."
Write-Host ($SuccessMessage -f $PrivateFiles.Count, $PrivateHashes.Count, $PublicFiles.Count,
    $ArchiveFiles.Count, $ArchiveEntryCount)
