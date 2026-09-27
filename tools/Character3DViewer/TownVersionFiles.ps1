# User-named Viewer versions. The index owns only files successfully saved here.
function Read-TownVersionIndex([string]$Folder) {
    $path = Join-Path $Folder 'town-viewer-versions.json'
    if (-not [IO.File]::Exists($path)) { return @() }
    return @(Get-Content -LiteralPath $path -Raw | ConvertFrom-Json)
}
function Save-TownVersionIndex([string]$Folder, $Entries) {
    $path = Join-Path $Folder 'town-viewer-versions.json'
    $json = ConvertTo-Json -InputObject @($Entries) -Depth 4
    [IO.File]::WriteAllText("$path.pending", $json, [Text.UTF8Encoding]::new($false))
    [IO.File]::Move("$path.pending", $path, $true)
}
# Repeated saves within one minute keep the earlier revision as a numbered sibling.
function Preserve-TownVersion([string]$Folder, [string]$Path) {
    $entries = @(Read-TownVersionIndex $Folder)
    $existing = $entries | Where-Object Path -IEQ $Path | Select-Object -First 1
    if (-not $existing -or -not [IO.File]::Exists($Path)) { return }
    if ((Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash -ne $existing.Hash) { return }
    $number = 1
    do {
        $archive = Join-Path ([IO.Path]::GetDirectoryName($Path)) (
            [IO.Path]::GetFileNameWithoutExtension($Path) + " ($number).town")
        $number++
    } while ([IO.File]::Exists($archive))
    [IO.File]::Copy($Path, $archive, $false)
    $existing.Path = $archive
    Save-TownVersionIndex $Folder $entries
}
function Register-TownVersion([string]$Folder, [string]$TownName, [string]$Path) {
    $path = [IO.Path]::GetFullPath($Path)
    $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    $entries = @(Read-TownVersionIndex $Folder | Where-Object { $_.Path -ine $path })
    $entries += [pscustomobject]@{ Town = $TownName; Path = $path; SavedUtc = [DateTime]::UtcNow; Hash = $hash }
    $versions = @($entries | Where-Object Town -ceq $TownName | Sort-Object SavedUtc -Descending)
    $expired = @($versions | Select-Object -Skip 10)
    foreach ($entry in $expired) {
        # Never delete an unrelated or externally replaced file on a reused path.
        if ([IO.File]::Exists($entry.Path) -and [IO.Path]::GetExtension($entry.Path) -ieq '.town' -and
            (Get-FileHash -LiteralPath $entry.Path -Algorithm SHA256).Hash -eq $entry.Hash) {
            [IO.File]::Delete($entry.Path)
        }
    }
    $keep = @($entries | Where-Object { $_.Path -notin $expired.Path })
    Save-TownVersionIndex $Folder $keep
}
function Publish-TownVersions([string]$Folder, [string]$TownName, [int]$Id) {
    $versions = @(Read-TownVersionIndex $Folder | Where-Object {
        $_.Town -ceq $TownName -and [IO.File]::Exists($_.Path)
    } | Sort-Object SavedUtc -Descending | Select-Object -First 10)
    $bytes = [Collections.Generic.List[byte]]::new()
    $bytes.AddRange([BitConverter]::GetBytes([uint32]21780308)) # TWL1
    $bytes.AddRange([BitConverter]::GetBytes([uint32]$Id))
    $bytes.AddRange([BitConverter]::GetBytes([uint32]$versions.Count))
    foreach ($entry in $versions) {
        $utf32 = [Text.Encoding]::UTF32.GetBytes($entry.Path)
        $bytes.AddRange([BitConverter]::GetBytes([uint32]($utf32.Length / 4)))
        $bytes.AddRange($utf32)
    }
    Write-TownPayload (Join-Path $Folder ((HashText 'TownEditor.File.Recent') + '.bin')) $bytes.ToArray()
}
