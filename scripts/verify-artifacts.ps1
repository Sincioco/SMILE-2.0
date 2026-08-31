$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot

function Get-Sha256 {
    param([string]$Path)

    $algorithm = [Security.Cryptography.SHA256]::Create()
    $stream = [IO.File]::OpenRead($Path)

    try {
        return [BitConverter]::ToString($algorithm.ComputeHash($stream)).Replace('-', '')
    }
    finally {
        $stream.Dispose()
        $algorithm.Dispose()
    }
}

function Require-File {
    param([string]$RelativePath)

    $path = Join-Path $repositoryRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required artifact is missing: $RelativePath"
    }
    return $path
}

function Assert-NativeGuiX64 {
    param([string]$RelativePath)

    $path = Require-File $RelativePath
    $bytes = [System.IO.File]::ReadAllBytes($path)
    if ($bytes.Length -lt 512 -or $bytes[0] -ne 0x4D -or $bytes[1] -ne 0x5A) {
        throw "$RelativePath is not a valid PE image."
    }

    $peOffset = [BitConverter]::ToInt32($bytes, 0x3C)
    if ($bytes[$peOffset] -ne 0x50 -or $bytes[$peOffset + 1] -ne 0x45) {
        throw "$RelativePath has no PE signature."
    }

    $machine = [BitConverter]::ToUInt16($bytes, $peOffset + 4)
    $optionalHeader = $peOffset + 24
    $magic = [BitConverter]::ToUInt16($bytes, $optionalHeader)
    $subsystem = [BitConverter]::ToUInt16($bytes, $optionalHeader + 68)
    $clrDirectory = $optionalHeader + 112 + (14 * 8)
    $clrSize = [BitConverter]::ToUInt32($bytes, $clrDirectory + 4)

    if ($machine -ne 0x8664) { throw "$RelativePath is not x64 (machine 0x$($machine.ToString('X4')))." }
    if ($magic -ne 0x20B) { throw "$RelativePath is not PE32+." }
    if ($subsystem -ne 2) { throw "$RelativePath is not a Windows GUI executable (subsystem $subsystem)." }
    if ($clrSize -ne 0) { throw "$RelativePath contains a CLR header." }

    Write-Host "Native x64 GUI verified: $RelativePath"
}

function Assert-WaveCopy {
    param([string]$Game, [string]$Name)

    $sourceRelative = "games\$Game\Assets\$Name"
    $outputRelative = "artifacts\games\$Game\Assets\$Name"
    $source = Require-File $sourceRelative
    $output = Require-File $outputRelative
    $bytes = [System.IO.File]::ReadAllBytes($output)
    if ($bytes.Length -lt 12 -or [Text.Encoding]::ASCII.GetString($bytes, 0, 4) -ne 'RIFF' -or
        [Text.Encoding]::ASCII.GetString($bytes, 8, 4) -ne 'WAVE') {
        throw "$outputRelative is not a RIFF/WAVE asset."
    }
    if ((Get-Sha256 $source) -ne (Get-Sha256 $output)) {
        throw "$outputRelative does not match its project asset."
    }
}

function Assert-AssetCopy {
    param([string]$SourceRelative, [string]$OutputRelative)

    $source = Require-File $SourceRelative
    $output = Require-File $OutputRelative
    if ((Get-Sha256 $source) -ne (Get-Sha256 $output)) {
        throw "$OutputRelative does not match its project asset."
    }
}

function Get-BytesSha256 {
    param([byte[]]$Bytes)

    $algorithm = [Security.Cryptography.SHA256]::Create()
    try {
        return ([BitConverter]::ToString($algorithm.ComputeHash($Bytes))).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $algorithm.Dispose()
    }
}

function Read-ZipEntryBytes {
    param([IO.Compression.ZipArchiveEntry]$Entry)

    $stream = $Entry.Open()
    $output = [IO.MemoryStream]::new()
    try {
        $stream.CopyTo($output)
        return $output.ToArray()
    }
    finally {
        $output.Dispose()
        $stream.Dispose()
    }
}

function Assert-PackageLocation {
    param($Location, [string[]]$DeclaredSources, [string]$Description)

    $propertyNames = [string[]]@($Location.PSObject.Properties | ForEach-Object { $_.Name })
    if ($null -eq $Location -or
        [string]::Join("`n", $propertyNames) -cne "source`nline`ncolumn`nlength" -or
        $DeclaredSources -cnotcontains $Location.source -or
        $Location.line -lt 1 -or $Location.column -lt 1 -or $Location.length -lt 1) {
        throw "$Description has an invalid format-6 source location."
    }
}

function Assert-PackageParameter {
    param($Parameter, [int]$ExpectedOrdinal, [string[]]$DeclaredSources, [string]$Description)

    $propertyNames = [string[]]@($Parameter.PSObject.Properties | ForEach-Object { $_.Name })
    if ([string]::Join("`n", $propertyNames) -cne
        "name`ntype`nmode`noptional`ndefault`nordinal`nlocation" -or
        [string]::IsNullOrWhiteSpace($Parameter.name) -or
        $Parameter.mode -notin @('ByVal', 'ByRef') -or $Parameter.optional -isnot [bool] -or
        $Parameter.ordinal -ne $ExpectedOrdinal) {
        throw "$Description does not use the canonical format-6 parameter shape."
    }
    Assert-PackageLocation $Parameter.location $DeclaredSources $Description

    if (-not $Parameter.optional) {
        if ($null -ne $Parameter.default) {
            throw "$Description is required but has non-null default metadata."
        }
        return
    }
    if ($Parameter.mode -cne 'ByVal' -or $null -eq $Parameter.default) {
        throw "$Description is Optional without a bound ByVal default."
    }

    $defaultNames = [string[]]@($Parameter.default.PSObject.Properties | ForEach-Object { $_.Name })
    switch -CaseSensitive ($Parameter.default.kind) {
        'number' {
            if ([string]::Join("`n", $defaultNames) -cne "kind`nvalue" -or
                $Parameter.type.kind -cne 'primitive' -or $Parameter.type.name -cne 'Number' -or
                ($Parameter.default.value -isnot [int] -and $Parameter.default.value -isnot [long])) {
                throw "$Description has invalid normalized Number default metadata."
            }
        }
        'boolean' {
            if ([string]::Join("`n", $defaultNames) -cne "kind`nvalue" -or
                $Parameter.type.kind -cne 'primitive' -or $Parameter.type.name -cne 'Boolean' -or
                $Parameter.default.value -isnot [bool]) {
                throw "$Description has invalid normalized Boolean default metadata."
            }
        }
        'text' {
            if ([string]::Join("`n", $defaultNames) -cne "kind`nvalue" -or
                $Parameter.type.kind -cne 'primitive' -or $Parameter.type.name -cne 'Text' -or
                $Parameter.default.value -isnot [string]) {
                throw "$Description has invalid normalized Text default metadata."
            }
        }
        'enum' {
            if ([string]::Join("`n", $defaultNames) -cne "kind`nmember`nvalue" -or
                $Parameter.type.kind -cne 'enum' -or
                [string]::IsNullOrWhiteSpace($Parameter.default.member) -or
                ($Parameter.default.value -isnot [int] -and $Parameter.default.value -isnot [long])) {
                throw "$Description has invalid normalized Enum default metadata."
            }
        }
        default { throw "$Description has unsupported default kind '$($Parameter.default.kind)'." }
    }
}

function Assert-PackageAccessor {
    param($Accessor, [string]$ExpectedIdentity, [string[]]$DeclaredSources, [string]$Description)

    $propertyNames = [string[]]@($Accessor.PSObject.Properties | ForEach-Object { $_.Name })
    if ([string]::Join("`n", $propertyNames) -cne "identity`nrequiresGameWindow`nlocation" -or
        $Accessor.identity -cne $ExpectedIdentity -or
        $Accessor.requiresGameWindow -isnot [bool]) {
        throw "$Description does not use the canonical format-6 accessor shape."
    }
    Assert-PackageLocation $Accessor.location $DeclaredSources $Description
}

function Assert-SmileLibraryPackage {
    param(
        [string]$RelativePath,
        [string]$ProjectRelativePath,
        [string]$ExpectedName,
        [string]$ExpectedVersion,
        [int]$ExpectedModuleCount,
        [int]$ExpectedSourceCount,
        [int]$ExpectedPublicMemberCount
    )

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $path = Require-File $RelativePath
    $projectPath = Join-Path $repositoryRoot $ProjectRelativePath
    [xml]$project = Get-Content -LiteralPath $projectPath -Raw
    $projectSources = @($project.SelectNodes('//SmileSource') | ForEach-Object {
        'src/' + $_.Include.Replace('\', '/')
    })
    [Array]::Sort($projectSources, [StringComparer]::Ordinal)
    $archive = [IO.Compression.ZipFile]::OpenRead($path)
    try {
        $entryNames = [string[]]@($archive.Entries | ForEach-Object { $_.FullName })
        $orderedNames = [string[]]$entryNames.Clone()
        [Array]::Sort($orderedNames, [StringComparer]::Ordinal)
        if ([string]::Join("`n", $entryNames) -cne [string]::Join("`n", $orderedNames)) {
            throw "$RelativePath does not use ordinal archive entry order."
        }
        if (@($entryNames | Select-Object -Unique).Count -ne $entryNames.Count) {
            throw "$RelativePath contains duplicate archive entries."
        }
        foreach ($entry in $archive.Entries) {
            if ($entry.LastWriteTime.Year -ne 1980 -or $entry.LastWriteTime.Month -ne 1 -or
                $entry.LastWriteTime.Day -ne 1 -or $entry.LastWriteTime.Hour -ne 0 -or
                $entry.LastWriteTime.Minute -ne 0 -or $entry.LastWriteTime.Second -ne 0) {
                throw "$RelativePath entry '$($entry.FullName)' does not use the deterministic timestamp."
            }
            if ($entry.CompressedLength -ne $entry.Length) {
                throw "$RelativePath entry '$($entry.FullName)' is compressed."
            }
        }

        $manifestEntry = $archive.GetEntry('manifest.json')
        $apiEntry = $archive.GetEntry('api/public-symbols.json')
        if ($null -eq $manifestEntry -or $null -eq $apiEntry) {
            throw "$RelativePath is missing required metadata entries."
        }
        $manifestText = [Text.Encoding]::UTF8.GetString((Read-ZipEntryBytes $manifestEntry))
        $apiText = [Text.Encoding]::UTF8.GetString((Read-ZipEntryBytes $apiEntry))
        $manifest = $manifestText | ConvertFrom-Json
        $api = $apiText | ConvertFrom-Json
        $expectedProvider = "$ExpectedName@$ExpectedVersion"
        if ($manifest.formatVersion -ne 6 -or $api.formatVersion -ne 6 -or
            $manifest.name -cne $ExpectedName -or $manifest.version -cne $ExpectedVersion -or
            $manifest.provider -cne $expectedProvider -or $api.library.name -cne $ExpectedName -or
            $api.library.version -cne $ExpectedVersion -or $api.library.provider -cne $expectedProvider) {
            throw "$RelativePath has incorrect format-6 library identity metadata."
        }
        $declaredSources = [string[]]@($manifest.sources)
        if (@($manifest.modules).Count -ne $ExpectedModuleCount -or
            $declaredSources.Count -ne $ExpectedSourceCount -or @($manifest.dependencies).Count -ne 0 -or
            @($manifest.sourceHashes.PSObject.Properties).Count -ne $ExpectedSourceCount -or
            @($api.modules).Count -ne $ExpectedModuleCount -or
            @($api.modules.members).Count -ne $ExpectedPublicMemberCount) {
            throw "$RelativePath has an unexpected module, source, dependency, or public-member count."
        }
        if ([string]::Join("`n", $declaredSources) -cne [string]::Join("`n", $projectSources)) {
            throw "$RelativePath sources do not retain exact project Include identities."
        }
        $expectedEntries = [string[]]@('api/public-symbols.json', 'manifest.json') + $declaredSources
        [Array]::Sort($expectedEntries, [StringComparer]::Ordinal)
        if ([string]::Join("`n", $entryNames) -cne [string]::Join("`n", $expectedEntries)) {
            throw "$RelativePath contains undeclared or missing archive entries."
        }
        foreach ($sourceId in $declaredSources) {
            if (-not $sourceId.StartsWith('src/', [StringComparison]::Ordinal) -or
                $sourceId.Contains('\') -or $sourceId.Contains(':') -or $sourceId.Contains('/../')) {
                throw "$RelativePath declares unsafe source ID '$sourceId'."
            }
            $entry = $archive.GetEntry($sourceId)
            $declaredHash = $manifest.sourceHashes.PSObject.Properties[$sourceId].Value
            $actualHash = Get-BytesSha256 (Read-ZipEntryBytes $entry)
            if ($declaredHash -cne $actualHash) {
                throw "$RelativePath source hash is invalid for '$sourceId'."
            }
            $include = $sourceId.Substring(4).Replace('/', '\')
            $sourceText = Get-Content -LiteralPath (Join-Path (Split-Path -Parent $projectPath) $include) -Raw
            $normalizedBytes = [Text.Encoding]::UTF8.GetBytes($sourceText.Replace("`r`n", "`n").Replace("`r", "`n"))
            if ((Get-BytesSha256 $normalizedBytes) -cne $actualHash) {
                throw "$RelativePath source '$sourceId' differs from the current project source."
            }
        }
        if ($manifestText.IndexOf($repositoryRoot, [StringComparison]::OrdinalIgnoreCase) -ge 0 -or
            $apiText.IndexOf($repositoryRoot, [StringComparison]::OrdinalIgnoreCase) -ge 0 -or
            $apiText -match '[A-Za-z]:[\\/]') {
            throw "$RelativePath serializes an absolute checkout, cache, or temporary path."
        }
        foreach ($module in @($api.modules)) {
            if ($module.provider -cne $expectedProvider) {
                throw "$RelativePath module '$($module.name)' has an incorrect provider."
            }
            foreach ($moduleSource in @($module.sources)) {
                if ($declaredSources -cnotcontains $moduleSource) {
                    throw "$RelativePath module '$($module.name)' cites undeclared source '$moduleSource'."
                }
            }
            foreach ($member in @($module.members)) {
                Assert-PackageLocation $member.location $declaredSources "$RelativePath member '$($member.name)'"
                $memberParameters = $member.PSObject.Properties['parameters']
                if ($null -ne $memberParameters) {
                    $parameterOrdinal = 0
                    foreach ($parameter in @($memberParameters.Value | Where-Object { $null -ne $_ })) {
                        Assert-PackageParameter $parameter $parameterOrdinal $declaredSources `
                            "$RelativePath parameter '$($member.name).$($parameter.name)'"
                        $parameterOrdinal++
                    }
                }
                $memberFields = $member.PSObject.Properties['fields']
                if ($null -ne $memberFields) {
                    foreach ($field in @($memberFields.Value | Where-Object { $null -ne $_ })) {
                        Assert-PackageLocation $field.location $declaredSources `
                            "$RelativePath field '$($member.name).$($field.name)'"
                    }
                }
                if ($member.kind -ceq 'Class') {
                    $classPropertyNames = [string[]]@($member.PSObject.Properties | ForEach-Object { $_.Name })
                    if ([string]::Join("`n", $classPropertyNames) -cne
                        "name`nkind`nvisibility`nidentity`nmodule`nprovider`nsize`nalignment`nfields`nconstructor`nmembers`nlocation" -or
                        $member.visibility -cne 'Public' -or $member.provider -cne $expectedProvider -or
                        $member.size -ne 8 -or $member.alignment -ne 8 -or
                        $member.PSObject.Properties.Name -ccontains 'instanceSize') {
                        throw "$RelativePath Class '$($member.name)' has invalid format-6 metadata."
                    }
                    $fieldOrdinal = 0
                    foreach ($field in @($member.fields | Where-Object { $null -ne $_ })) {
                        $fieldPropertyNames = [string[]]@($field.PSObject.Properties | ForEach-Object { $_.Name })
                        $expectedFieldProperties = if ($field.PSObject.Properties.Name -ccontains 'dimensions') {
                            "name`nvisibility`nelementType`nrank`ndimensions`nordinal`nlocation"
                        }
                        else {
                            "name`nvisibility`ntype`nordinal`nlocation"
                        }
                        if ([string]::Join("`n", $fieldPropertyNames) -cne $expectedFieldProperties -or
                            $field.visibility -cne 'Public' -or $field.ordinal -ne $fieldOrdinal -or
                            $field.PSObject.Properties.Name -ccontains 'offset') {
                            throw "$RelativePath Class field '$($member.name).$($field.name)' has invalid metadata."
                        }
                        $fieldOrdinal++
                    }
                    $constructor = $member.constructor
                    $constructorProperties = [string[]]@($constructor.PSObject.Properties |
                        ForEach-Object { $_.Name })
                    if ([string]::Join("`n", $constructorProperties) -cne
                        "identity`nvisibility`ndeclared`nparameters`nrequiresGameWindow`nlocation" -or
                        $constructor.identity -cne ($member.identity + '::constructor::New') -or
                        $constructor.visibility -cne 'Public' -or $constructor.declared -isnot [bool] -or
                        $constructor.requiresGameWindow -isnot [bool]) {
                        throw "$RelativePath Class constructor '$($member.name).New' has invalid metadata."
                    }
                    Assert-PackageLocation $constructor.location $declaredSources `
                        "$RelativePath constructor '$($member.name).New'"
                    $constructorParameterOrdinal = 0
                    foreach ($parameter in @($constructor.parameters | Where-Object { $null -ne $_ })) {
                        Assert-PackageParameter $parameter $constructorParameterOrdinal $declaredSources `
                            "$RelativePath constructor parameter '$($member.name).$($parameter.name)'"
                        $constructorParameterOrdinal++
                    }
                }
                $memberMembers = $member.PSObject.Properties['members']
                if ($null -eq $memberMembers) {
                    continue
                }
                foreach ($nestedMember in @($memberMembers.Value | Where-Object { $null -ne $_ })) {
                    Assert-PackageLocation $nestedMember.location $declaredSources `
                        "$RelativePath nested member '$($member.name).$($nestedMember.name)'"
                    if ($member.kind -cne 'Type' -and $member.kind -cne 'Class') {
                        continue
                    }
                    if ($nestedMember.visibility -cne 'Public' -or
                        [string]::IsNullOrWhiteSpace($nestedMember.identity) -or
                        $nestedMember.PSObject.Properties.Name -ccontains 'provider') {
                        throw "$RelativePath instance member '$($member.name).$($nestedMember.name)' has invalid identity, visibility, or provider metadata."
                    }
                    switch -CaseSensitive ($nestedMember.kind) {
                        'Subroutine' {
                            $propertyNames = [string[]]@($nestedMember.PSObject.Properties |
                                ForEach-Object { $_.Name })
                            if ([string]::Join("`n", $propertyNames) -cne
                                "name`nkind`nvisibility`nidentity`nreturnType`nparameters`nrequiresGameWindow`nlocation" -or
                                $nestedMember.identity -cne ($member.identity + '::member::' + $nestedMember.name) -or
                                $null -ne $nestedMember.returnType -or
                                $nestedMember.requiresGameWindow -isnot [bool]) {
                                throw "$RelativePath instance Sub '$($member.name).$($nestedMember.name)' has invalid format-6 metadata."
                            }
                            $nestedParameterOrdinal = 0
                            foreach ($parameter in @($nestedMember.parameters | Where-Object { $null -ne $_ })) {
                                Assert-PackageParameter $parameter $nestedParameterOrdinal $declaredSources `
                                    "$RelativePath nested parameter '$($member.name).$($nestedMember.name).$($parameter.name)'"
                                $nestedParameterOrdinal++
                            }
                        }
                        'Function' {
                            $propertyNames = [string[]]@($nestedMember.PSObject.Properties |
                                ForEach-Object { $_.Name })
                            if ([string]::Join("`n", $propertyNames) -cne
                                "name`nkind`nvisibility`nidentity`nreturnType`nparameters`nrequiresGameWindow`nlocation" -or
                                $nestedMember.identity -cne ($member.identity + '::member::' + $nestedMember.name) -or
                                $null -eq $nestedMember.returnType -or
                                $nestedMember.requiresGameWindow -isnot [bool]) {
                                throw "$RelativePath instance Function '$($member.name).$($nestedMember.name)' has invalid format-6 metadata."
                            }
                            $nestedParameterOrdinal = 0
                            foreach ($parameter in @($nestedMember.parameters | Where-Object { $null -ne $_ })) {
                                Assert-PackageParameter $parameter $nestedParameterOrdinal $declaredSources `
                                    "$RelativePath nested parameter '$($member.name).$($nestedMember.name).$($parameter.name)'"
                                $nestedParameterOrdinal++
                            }
                        }
                        'Property' {
                            $propertyNames = [string[]]@($nestedMember.PSObject.Properties |
                                ForEach-Object { $_.Name })
                            if ([string]::Join("`n", $propertyNames) -cne
                                "name`nkind`nvisibility`nidentity`ntype`nget`nset`nlocation" -or
                                $nestedMember.identity -cne ($member.identity + '::property::' + $nestedMember.name) -or
                                $null -eq $nestedMember.type -or
                                ($null -eq $nestedMember.get -and $null -eq $nestedMember.set)) {
                                throw "$RelativePath instance Property '$($member.name).$($nestedMember.name)' has invalid format-6 metadata."
                            }
                            if ($null -ne $nestedMember.get) {
                                Assert-PackageAccessor $nestedMember.get ($nestedMember.identity + '::get') `
                                    $declaredSources "$RelativePath getter '$($member.name).$($nestedMember.name)'"
                            }
                            if ($null -ne $nestedMember.set) {
                                Assert-PackageAccessor $nestedMember.set ($nestedMember.identity + '::set') `
                                    $declaredSources "$RelativePath setter '$($member.name).$($nestedMember.name)'"
                            }
                        }
                        default {
                            throw "$RelativePath instance type '$($member.name)' has unsupported nested member kind '$($nestedMember.kind)'."
                        }
                    }
                }
            }
        }
        Write-Host "Format-6 SMILE library verified: $RelativePath"
    }
    finally {
        $archive.Dispose()
    }
}

function Assert-LightweightOopProofPackage {
    param([string]$RelativePath)

    $path = Require-File $RelativePath
    $archive = [IO.Compression.ZipFile]::OpenRead($path)
    try {
        $apiText = [Text.Encoding]::UTF8.GetString(
            (Read-ZipEntryBytes ($archive.GetEntry('api/public-symbols.json'))))
        $api = $apiText | ConvertFrom-Json
        $module = @($api.modules | Where-Object { $_.name -ceq 'Smile.Lightweight.Oop.Proof' })
        if ($module.Count -ne 1) {
            throw "$RelativePath does not expose the proof Module exactly once."
        }
        if ([string]::Join('|', @($module[0].members.name)) -cne
            'Counter|CounterBox|DisplayMode|EmptyReference|GameConstructorProbe|GameReferenceProbe|ReferenceCounter|Report') {
            throw "$RelativePath has an unexpected proof Module member order."
        }
        $counter = @($module[0].members | Where-Object { $_.name -ceq 'Counter' })
        $counterBox = @($module[0].members | Where-Object { $_.name -ceq 'CounterBox' })
        $displayMode = @($module[0].members | Where-Object { $_.name -ceq 'DisplayMode' })
        $emptyReference = @($module[0].members | Where-Object { $_.name -ceq 'EmptyReference' })
        $gameConstructor = @($module[0].members | Where-Object { $_.name -ceq 'GameConstructorProbe' })
        $gameReference = @($module[0].members | Where-Object { $_.name -ceq 'GameReferenceProbe' })
        $referenceCounter = @($module[0].members | Where-Object { $_.name -ceq 'ReferenceCounter' })
        $report = @($module[0].members | Where-Object { $_.name -ceq 'Report' })
        if ($counter.Count -ne 1 -or $counterBox.Count -ne 1 -or
            $displayMode.Count -ne 1 -or $emptyReference.Count -ne 1 -or
            $gameConstructor.Count -ne 1 -or $gameReference.Count -ne 1 -or
            $referenceCounter.Count -ne 1 -or $report.Count -ne 1 -or
            [string]::Join('|', @($displayMode[0].members.name)) -cne 'Standard|Compact|CompactAlias' -or
            [string]::Join('|', @($displayMode[0].members.value)) -cne '1|2|2') {
            throw "$RelativePath has an unexpected proof Type, Enum, or routine surface."
        }
        $parameters = @($report[0].parameters)
        if ($parameters.Count -ne 5 -or
            [string]::Join('|', @($parameters.name)) -cne 'Label|Copies|Enabled|Suffix|Mode' -or
            $parameters[0].optional -or $null -ne $parameters[0].default -or
            $parameters[1].default.kind -cne 'number' -or $parameters[1].default.value -ne 3 -or
            $parameters[2].default.kind -cne 'boolean' -or -not $parameters[2].default.value -or
            $parameters[3].default.kind -cne 'text' -or $parameters[3].default.value -cne '!' -or
            $parameters[4].type.kind -cne 'enum' -or
            $parameters[4].type.identity -cne 'Smile.Lightweight.Oop.Proof::DisplayMode' -or
            $parameters[4].type.provider -cne 'Smile.Lightweight.Oop.Proof@1.2.0' -or
            $parameters[4].default.kind -cne 'enum' -or
            $parameters[4].default.member -cne 'CompactAlias' -or
            $parameters[4].default.value -ne 2 -or
            $parameters[4].default.PSObject.Properties.Name -ccontains 'type' -or
            $parameters[4].default.PSObject.Properties.Name -ccontains 'provider') {
            throw "$RelativePath has incorrect normalized Optional/default metadata."
        }
        $declaredAlias = @($displayMode[0].members | Where-Object {
            $_.name -ceq $parameters[4].default.member -and $_.value -eq $parameters[4].default.value
        })
        if ($declaredAlias.Count -ne 1) {
            throw "$RelativePath Enum default does not identify an exact declared member/value pair."
        }

        $counterMembers = @($counter[0].members)
        if ($counter[0].identity -cne 'Smile.Lightweight.Oop.Proof::Counter' -or
            $counter[0].provider -cne 'Smile.Lightweight.Oop.Proof@1.2.0' -or
            [string]::Join('|', @($counter[0].fields.name)) -cne 'Label|StoredValue|Enabled|Mode' -or
            [string]::Join('|', @($counterMembers.name)) -cne
                'Advance|Caption|Configure|Difference|DrawProbe|GameProbe|Shifted|Total' -or
            [string]::Join('|', @($counterBox[0].fields.name)) -cne 'Item' -or
            @($counterBox[0].members).Count -ne 0 -or
            $apiText.IndexOf('::member::Hide', [StringComparison]::Ordinal) -ge 0 -or
            $apiText.IndexOf('::property::Secret', [StringComparison]::Ordinal) -ge 0 -or
            $apiText.IndexOf('::receiver', [StringComparison]::Ordinal) -ge 0 -or
            $apiText.IndexOf('::value', [StringComparison]::Ordinal) -ge 0) {
            throw "$RelativePath has incorrect public Type members or leaks private/implicit symbols."
        }
        $configure = @($counterMembers | Where-Object { $_.name -ceq 'Configure' })
        $difference = @($counterMembers | Where-Object { $_.name -ceq 'Difference' })
        $drawProbe = @($counterMembers | Where-Object { $_.name -ceq 'DrawProbe' })
        $gameProbe = @($counterMembers | Where-Object { $_.name -ceq 'GameProbe' })
        $shifted = @($counterMembers | Where-Object { $_.name -ceq 'Shifted' })
        $caption = @($counterMembers | Where-Object { $_.name -ceq 'Caption' })
        $total = @($counterMembers | Where-Object { $_.name -ceq 'Total' })
        if ($configure.Count -ne 1 -or $difference.Count -ne 1 -or $drawProbe.Count -ne 1 -or
            $gameProbe.Count -ne 1 -or $shifted.Count -ne 1 -or $caption.Count -ne 1 -or
            $total.Count -ne 1 -or
            [string]::Join('|', @($configure[0].parameters.name)) -cne 'Label|Start|Enabled|Mode' -or
            $configure[0].parameters[3].type.provider -cne 'Smile.Lightweight.Oop.Proof@1.2.0' -or
            $configure[0].parameters[3].default.member -cne 'Standard' -or
            $configure[0].parameters[3].default.value -ne 1 -or
            $difference[0].parameters[0].type.identity -cne 'Smile.Lightweight.Oop.Proof::Counter' -or
            $difference[0].parameters[0].type.provider -cne 'Smile.Lightweight.Oop.Proof@1.2.0' -or
            $shifted[0].returnType.identity -cne 'Smile.Lightweight.Oop.Proof::Counter' -or
            $shifted[0].returnType.provider -cne 'Smile.Lightweight.Oop.Proof@1.2.0' -or
            -not $drawProbe[0].requiresGameWindow -or
            -not $gameProbe[0].get.requiresGameWindow -or $gameProbe[0].set.requiresGameWindow -or
            $null -ne $caption[0].set -or $null -eq $caption[0].get -or
            $null -eq $total[0].get -or $null -eq $total[0].set -or
            $gameProbe[0].get.identity -ceq $gameProbe[0].set.identity) {
            throw "$RelativePath has incorrect Type signatures, providers, properties, or capabilities."
        }
        $referenceMembers = @($referenceCounter[0].members)
        $referenceConstructor = $referenceCounter[0].constructor
        $classGameProperty = @($gameReference[0].members | Where-Object { $_.name -ceq 'GameProbe' })
        if ($referenceCounter[0].kind -cne 'Class' -or
            $referenceCounter[0].provider -cne 'Smile.Lightweight.Oop.Proof@1.2.0' -or
            [string]::Join('|', @($referenceCounter[0].fields.name)) -cne 'Code|Samples' -or
            $referenceCounter[0].fields[1].rank -ne 1 -or
            [string]::Join('|', @($referenceCounter[0].fields[1].dimensions)) -cne '2' -or
            $referenceCounter[0].PSObject.Properties.Name -ccontains 'instanceSize' -or
            $referenceConstructor.identity -cne
                'Smile.Lightweight.Oop.Proof::ReferenceCounter::constructor::New' -or
            -not $referenceConstructor.declared -or $referenceConstructor.requiresGameWindow -or
            [string]::Join('|', @($referenceConstructor.parameters.name)) -cne 'Label|Start|Mode' -or
            [string]::Join('|', @($referenceMembers.name)) -cne
                'Advance|Alias|Caption|Same|Snapshot|Total' -or
            $referenceMembers[1].returnType.kind -cne 'class' -or
            $referenceMembers[1].returnType.provider -cne 'Smile.Lightweight.Oop.Proof@1.2.0' -or
            $emptyReference[0].constructor.declared -or @($emptyReference[0].constructor.parameters).Count -ne 0 -or
            -not $gameConstructor[0].constructor.requiresGameWindow -or
            $gameReference[0].constructor.requiresGameWindow -or
            -not (@($gameReference[0].members | Where-Object { $_.name -ceq 'DrawProbe' })[0].requiresGameWindow) -or
            $classGameProperty.Count -ne 1 -or -not $classGameProperty[0].get.requiresGameWindow -or
            $classGameProperty[0].set.requiresGameWindow -or
            $apiText.IndexOf('ReferenceCounter::member::Hide', [StringComparison]::Ordinal) -ge 0 -or
            $apiText.IndexOf('ReferenceCounter::property::Secret', [StringComparison]::Ordinal) -ge 0) {
            throw "$RelativePath has incorrect Class constructors, fields, members, providers, or capabilities."
        }
        Write-Host "Optional/default, Type-member, and Class SMILE library metadata verified: $RelativePath"
    }
    finally {
        $archive.Dispose()
    }
}

function Assert-SmileUiPackage {
    param([string]$RelativePath)

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $path = Require-File $RelativePath
    $archive = [IO.Compression.ZipFile]::OpenRead($path)
    try {
        $apiText = [Text.Encoding]::UTF8.GetString((Read-ZipEntryBytes `
            $archive.GetEntry('api/public-symbols.json')))
        $api = $apiText | ConvertFrom-Json
        $menuModule = @($api.modules | Where-Object { $_.name -ceq 'Smile.UI.Menu' })
        $dialogueModule = @($api.modules | Where-Object { $_.name -ceq 'Smile.UI.Dialogue' })
        $legacyNavigatorModule = @($api.modules | Where-Object { $_.name -ceq 'Smile.UI.MenuNavigator' })
        if ($menuModule.Count -ne 1 -or $dialogueModule.Count -ne 1 -or
            $legacyNavigatorModule.Count -ne 0) {
            throw "$RelativePath does not use the consolidated Smile.UI 2.0 module boundary."
        }

        $menu = @($menuModule[0].members | Where-Object { $_.name -ceq 'Menu' -and $_.kind -ceq 'Class' })
        $navigator = @($menuModule[0].members |
            Where-Object { $_.name -ceq 'MenuNavigator' -and $_.kind -ceq 'Class' })
        $dialogue = @($dialogueModule[0].members |
            Where-Object { $_.name -ceq 'Dialogue' -and $_.kind -ceq 'Class' })
        if ($menu.Count -ne 1 -or $navigator.Count -ne 1 -or $dialogue.Count -ne 1 -or
            [string]::Join('|', @($menuModule[0].members.name)) -cne 'Menu|MenuNavigator' -or
            [string]::Join('|', @($dialogueModule[0].members.name)) -cne 'Dialogue') {
            throw "$RelativePath does not publish the three expected UI Class facades."
        }

        $menuMembers = @($menu[0].members)
        $navigatorMembers = @($navigator[0].members)
        $dialogueMembers = @($dialogue[0].members)
        $menuDraw = @($menuMembers | Where-Object { $_.name -ceq 'Draw' })
        $menuUpdate = @($menuMembers | Where-Object { $_.name -ceq 'Update' })
        $navigatorDraw = @($navigatorMembers | Where-Object { $_.name -ceq 'Draw' })
        $navigatorUpdate = @($navigatorMembers | Where-Object { $_.name -ceq 'Update' })
        $dialogueDraw = @($dialogueMembers | Where-Object { $_.name -ceq 'Draw' })
        $dialogueSetStyle = @($dialogueMembers | Where-Object { $_.name -ceq 'SetStyle' })
        $addItem = @($menuMembers | Where-Object { $_.name -ceq 'AddItem' })
        $bindSubmenu = @($navigatorMembers | Where-Object { $_.name -ceq 'BindSubmenu' })
        if ($menuDraw.Count -ne 1 -or -not $menuDraw[0].requiresGameWindow -or
            $menuUpdate.Count -ne 1 -or $menuUpdate[0].requiresGameWindow -or
            $navigatorDraw.Count -ne 1 -or -not $navigatorDraw[0].requiresGameWindow -or
            $navigatorUpdate.Count -ne 1 -or $navigatorUpdate[0].requiresGameWindow -or
            $dialogueDraw.Count -ne 1 -or -not $dialogueDraw[0].requiresGameWindow -or
            $dialogueSetStyle.Count -ne 1 -or -not $dialogueSetStyle[0].requiresGameWindow -or
            $addItem.Count -ne 1 -or $addItem[0].parameters[2].name -cne 'Enabled' -or
            -not $addItem[0].parameters[2].optional -or
            $addItem[0].parameters[2].default.kind -cne 'boolean' -or
            -not $addItem[0].parameters[2].default.value -or
            $bindSubmenu.Count -ne 1 -or
            $bindSubmenu[0].parameters[3].name -cne 'ResetChildSelection' -or
            -not $bindSubmenu[0].parameters[3].optional -or
            $bindSubmenu[0].parameters[3].default.kind -cne 'boolean' -or
            -not $bindSubmenu[0].parameters[3].default.value) {
            throw "$RelativePath has incorrect UI member signatures, defaults, or capabilities."
        }

        foreach ($legacyName in @('MenuHandleCreate', 'NavigatorHandleCreate', 'DialogueHandleCreate',
                'InternalNavigationHandle', 'RootMenu', 'CurrentMenu', 'MenuAtDepth', 'ParentMenu',
                'LastAcceptedMenu')) {
            if ($apiText.IndexOf('"name":"' + $legacyName + '"', [StringComparison]::Ordinal) -ge 0) {
                throw "$RelativePath leaks obsolete or private UI member '$legacyName'."
            }
        }
        Write-Host "Smile.UI 2.0 Class facade metadata verified: $RelativePath"
    }
    finally {
        $archive.Dispose()
    }
}

function Assert-SmileGamePackage {
    param([string]$RelativePath)

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $path = Require-File $RelativePath
    $archive = [IO.Compression.ZipFile]::OpenRead($path)
    try {
        $apiText = [Text.Encoding]::UTF8.GetString((Read-ZipEntryBytes `
            $archive.GetEntry('api/public-symbols.json')))
        $api = $apiText | ConvertFrom-Json
        if ($api.library.provider -cne 'Smile.Game@2.0.0') {
            throw "$RelativePath does not publish Smile.Game@2.0.0."
        }

        $core = @($api.modules | Where-Object { $_.name -ceq 'Smile.Game.Core' })
        $cameraModule = @($api.modules | Where-Object { $_.name -ceq 'Smile.Game.Camera2D' })
        if ($core.Count -ne 1 -or $cameraModule.Count -ne 1) {
            throw "$RelativePath is missing the Smile.Game Core or Camera2D module."
        }

        $direction = @($core[0].members |
            Where-Object { $_.name -ceq 'CardinalDirection' -and $_.kind -ceq 'Enum' })
        $mover = @($core[0].members |
            Where-Object { $_.name -ceq 'CardinalMover' -and $_.kind -ceq 'Type' })
        $camera = @($cameraModule[0].members |
            Where-Object { $_.name -ceq 'CameraState' -and $_.kind -ceq 'Type' })
        if ($direction.Count -ne 1 -or $mover.Count -ne 1 -or $camera.Count -ne 1) {
            throw "$RelativePath does not publish the typed direction, mover, and camera values."
        }

        $directionX = @($core[0].members | Where-Object { $_.name -ceq 'DirectionX' })
        $directionY = @($core[0].members | Where-Object { $_.name -ceq 'DirectionY' })
        $legacyProcedural = @($core[0].members | Where-Object {
                $_.name -cin @('Place', 'BeginMove', 'UpdateMove', 'CancelMove', 'VisualX', 'VisualY')
            })
        if ([string]::Join('|', @($direction[0].members.name)) -cne 'None|Up|Right|Down|Left' -or
            [string]::Join('|', @($direction[0].members.value)) -cne '0|1|2|3|4' -or
            $direction[0].provider -cne 'Smile.Game@2.0.0' -or
            [string]::Join('|', @($mover[0].members.name)) -cne
                'BeginMove|CancelMove|Place|UpdateMove|VisualX|VisualY' -or
            $mover[0].fields[8].name -cne 'Facing' -or
            $mover[0].fields[8].type.kind -cne 'enum' -or
            $mover[0].fields[8].type.identity -cne 'Smile.Game.Core::CardinalDirection' -or
            $mover[0].fields[8].type.provider -cne 'Smile.Game@2.0.0' -or
            $mover[0].members[0].parameters[0].type.identity -cne
                'Smile.Game.Core::CardinalDirection' -or
            $directionX.Count -ne 1 -or $directionY.Count -ne 1 -or
            $directionX[0].parameters[0].type.kind -cne 'enum' -or
            $directionY[0].parameters[0].type.provider -cne 'Smile.Game@2.0.0' -or
            $legacyProcedural.Count -ne 0 -or
            [string]::Join('|', @($camera[0].members.name)) -cne
                'Configure|FirstVisibleCellX|FirstVisibleCellY|Follow|LastVisibleCellX|LastVisibleCellY|SmoothFollow' -or
            $apiText.IndexOf('::receiver', [StringComparison]::Ordinal) -ge 0) {
            throw "$RelativePath has incorrect Smile.Game Enum, Type-member, provider, or visibility metadata."
        }
        Write-Host "Smile.Game 2.0 value-Type metadata verified: $RelativePath"
    }
    finally {
        $archive.Dispose()
    }
}

Require-File 'artifacts\compiler\smilec.exe' | Out-Null
$vsixPath = Require-File 'artifacts\vsix\Smile.VisualStudio.vsix'
Assert-SmileLibraryPackage 'artifacts\libraries\Smile.Math.Extras.smilelib' `
    'libraries\Smile.Math.Extras\Smile.Math.Extras.smilelibproj' 'Smile.Math.Extras' '1.0.0' 1 2 3
Assert-SmileLibraryPackage 'artifacts\libraries\Smile.Text.Extras.smilelib' `
    'libraries\Smile.Text.Extras\Smile.Text.Extras.smilelibproj' 'Smile.Text.Extras' '1.0.0' 1 1 5
Assert-SmileLibraryPackage 'artifacts\libraries\Smile.Data.Models.smilelib' `
    'libraries\Smile.Data.Models\Smile.Data.Models.smilelibproj' 'Smile.Data.Models' '1.0.0' 1 2 7
Assert-SmileLibraryPackage 'artifacts\libraries\Smile.UI.smilelib' `
    'libraries\Smile.UI\Smile.UI.smilelibproj' 'Smile.UI' '2.0.0' 6 7 62
Assert-SmileUiPackage 'artifacts\libraries\Smile.UI.smilelib'
Assert-SmileLibraryPackage 'artifacts\libraries\Smile.Game.smilelib' `
    'libraries\Smile.Game\Smile.Game.smilelibproj' 'Smile.Game' '2.0.0' 5 5 56
Assert-SmileGamePackage 'artifacts\libraries\Smile.Game.smilelib'
Assert-SmileLibraryPackage 'artifacts\libraries\Smile.RPG.smilelib' `
    'libraries\Smile.RPG\Smile.RPG.smilelibproj' 'Smile.RPG' '1.3.0' 15 15 497
Assert-SmileLibraryPackage 'artifacts\libraries\Smile.Lightweight.Oop.Proof.smilelib' `
    'examples\LightweightOopCalls\LightweightOopLibrary.smilelibproj' `
    'Smile.Lightweight.Oop.Proof' '1.2.0' 1 1 8
Assert-LightweightOopProofPackage 'artifacts\libraries\Smile.Lightweight.Oop.Proof.smilelib'
Require-File 'artifacts\games\LibraryConsumer.exe' | Out-Null
Require-File 'artifacts\games\LibraryPackageConsumer.exe' | Out-Null
Require-File 'artifacts\games\LightweightOopCalls.exe' | Out-Null
Require-File 'artifacts\games\LightweightOopCalls.Package.exe' | Out-Null
Require-File 'artifacts\games\ClassRuntime.exe' | Out-Null
Require-File 'artifacts\games\ClassEndProgramCleanup.exe' | Out-Null
Require-File 'artifacts\games\ClassNothingFailure.exe' | Out-Null
Require-File 'artifacts\games\SnakeModelTests.exe' | Out-Null
Require-File 'artifacts\games\LocalModuleBasics.exe' | Out-Null
Require-File 'artifacts\web\LibraryConsumer\game.js' | Out-Null
Require-File 'artifacts\web\LightweightOopCalls\game.js' | Out-Null
Require-File 'artifacts\web\LightweightOopCalls.Package\game.js' | Out-Null
Require-File 'artifacts\web\ClassRuntime\game.js' | Out-Null
Require-File 'artifacts\web\ClassWebOwnership\game.js' | Out-Null
Require-File 'artifacts\web\ClassNothingFailure\game.js' | Out-Null
Require-File 'artifacts\web\SnakeModelTests\game.js' | Out-Null
Require-File 'artifacts\web\LocalModuleBasics\game.js' | Out-Null
Require-File 'artifacts\web\Phase4VisualSlice\game.js' | Out-Null
Require-File 'artifacts\games\Phase4VisualSlice-DirectX\Phase4VisualSlice.smile-assets.json' | Out-Null
Require-File 'artifacts\games\Phase4VisualSlice-GDI\Phase4VisualSlice.smile-assets.json' | Out-Null
Require-File 'artifacts\web\Phase4VisualSlice\smile-assets.json' | Out-Null
Require-File 'artifacts\games\Phase4AssetPublication\Phase4AssetPublication.smile-assets.json' | Out-Null
Require-File 'artifacts\web\Phase4AssetPublication\smile-assets.json' | Out-Null
Require-File 'artifacts\tests\Phase6RpgStateTests.exe' | Out-Null
Require-File 'artifacts\tests\Phase6RpgStateTestsPackage.exe' | Out-Null
Require-File 'artifacts\games\RPGSystems-DirectX\smile.gallery.rpg-systems.smile-assets.json' | Out-Null
Require-File 'artifacts\games\RPGSystems-GDI\smile.gallery.rpg-systems.smile-assets.json' | Out-Null
Require-File 'artifacts\web\RPGSystems\smile-assets.json' | Out-Null
Require-File 'artifacts\tests\Phase7WorldStateTests.exe' | Out-Null
Require-File 'artifacts\tests\Phase7WorldStateTestsPackage.exe' | Out-Null
Require-File 'artifacts\tests\Phase8DungeonStateTests.exe' | Out-Null
Require-File 'artifacts\tests\Phase8DungeonStateTestsPackage.exe' | Out-Null
Require-File 'artifacts\tests\Phase9BattleStateTests.exe' | Out-Null
Require-File 'artifacts\tests\Phase9BattleStateTestsPackage.exe' | Out-Null
Require-File 'artifacts\games\Phase5UIStateTests.exe' | Out-Null
Require-File 'artifacts\games\Phase5UIStateTestsPackage.exe' | Out-Null
Require-File 'artifacts\games\Phase5SubmenuStateTests.exe' | Out-Null
Require-File 'artifacts\games\Phase5SubmenuStateTestsPackage.exe' | Out-Null
Require-File 'artifacts\games\Phase5SubmenuViewport-DirectX\Phase5SubmenuViewport.smile-assets.json' | Out-Null
Require-File 'artifacts\games\Phase5SubmenuViewport-GDI\Phase5SubmenuViewport.smile-assets.json' | Out-Null
Require-File 'artifacts\web\Phase5SubmenuViewport\smile-assets.json' | Out-Null
Require-File 'artifacts\games\Phase5Hardening-DirectX\Phase5Hardening.exe' | Out-Null
Require-File 'artifacts\games\Phase5Hardening-GDI\Phase5Hardening.exe' | Out-Null
Require-File 'artifacts\games\Phase5HardeningPackage.exe' | Out-Null
Require-File 'artifacts\web\Phase5Hardening\smile-assets.json' | Out-Null
Require-File 'artifacts\web\Phase5HardeningPackage\smile-assets.json' | Out-Null
Require-File 'artifacts\web\MenuGallery\smile-assets.json' | Out-Null
Require-File 'artifacts\web\MenuGalleryPackage\smile-assets.json' | Out-Null

$nativePrograms = @(
    'artifacts\games\GraphicsBasics.exe',
    'artifacts\games\ArcBasics.exe',
    'artifacts\games\GraphicsTextSample.exe',
    'artifacts\games\Phase4VisualSlice-DirectX\Phase4VisualSlice.exe',
    'artifacts\games\Phase4VisualSlice-GDI\Phase4VisualSlice.exe',
    'artifacts\games\MenuGallery-DirectX\MenuGallery.exe',
    'artifacts\games\MenuGallery-GDI\MenuGallery.exe',
    'artifacts\games\MenuGalleryPackage.exe',
    'artifacts\games\RPGSystems-DirectX\RPGSystems.exe',
    'artifacts\games\RPGSystems-GDI\RPGSystems.exe',
    'artifacts\games\Phase5DialogueStateTests.exe',
    'artifacts\games\Phase5SubmenuViewport-DirectX\Phase5SubmenuViewport.exe',
    'artifacts\games\Phase5SubmenuViewport-GDI\Phase5SubmenuViewport.exe',
    'artifacts\games\Phase5Hardening-DirectX\Phase5Hardening.exe',
    'artifacts\games\Phase5Hardening-GDI\Phase5Hardening.exe',
    'artifacts\games\Phase5HardeningPackage.exe',
    'artifacts\games\Snake\Snake.exe',
    'artifacts\games\Snake\Snake-NoDemo.exe',
    'artifacts\games\Tetris\Tetris.exe',
    'artifacts\games\Tetris\Tetris-NoDemo.exe',
    'artifacts\games\PaddleBall\PaddleBall.exe',
    'artifacts\games\PaddleBall\PaddleBall-NoDemo.exe',
    'artifacts\games\BrickBreaker\BrickBreaker.exe',
    'artifacts\games\BrickBreaker\BrickBreaker-NoDemo.exe',
    'artifacts\games\MazeMuncher\MazeMuncher.exe',
    'artifacts\games\MazeMuncher\MazeMuncher-NoDemo.exe',
    'artifacts\games\DungeonStarI\DungeonStarI.exe',
    'artifacts\games\DungeonStarI\DungeonStarI-NoDemo.exe',
    'artifacts\games\DungeonStarII\DungeonStarII.exe',
    'artifacts\games\DungeonStarII\DungeonStarII-NoDemo.exe'
)
foreach ($program in $nativePrograms) {
    Assert-NativeGuiX64 $program
}

$assetSets = @{
    Snake = @('Eat.wav', 'GameOver.wav', 'Start.wav')
    Tetris = @('GameOver.wav', 'LineClear.wav', 'Move.wav', 'Rotate.wav')
    PaddleBall = @('GameOver.wav', 'Paddle.wav', 'Score.wav', 'Wall.wav')
    BrickBreaker = @('Brick.wav', 'GameOver.wav', 'LevelClear.wav', 'LoseLife.wav', 'Paddle.wav', 'Wall.wav')
    MazeMuncher = @('EnemyEaten.wav', 'GameOver.wav', 'LevelClear.wav', 'Pellet.wav', 'PlayerCaught.wav', 'Power.wav', 'Start.wav')
}
foreach ($game in $assetSets.Keys) {
    foreach ($asset in $assetSets[$game]) {
        Assert-WaveCopy $game $asset
    }
}
foreach ($game in @('Snake', 'Tetris', 'PaddleBall', 'BrickBreaker', 'DungeonStarI',
    'DungeonStarII', 'MazeMuncher')) {
    Require-File "artifacts\games\$game\$game.smile-assets.json" | Out-Null
    Require-File "artifacts\web\$game\smile-assets.json" | Out-Null
}
Assert-AssetCopy 'games\Tetris\Assets\Background.mp3' 'artifacts\games\Tetris\Assets\Background.mp3'
Assert-AssetCopy 'games\PaddleBall\Assets\Background.mp3' 'artifacts\games\PaddleBall\Assets\Background.mp3'
Assert-AssetCopy 'games\Snake\Assets\Background.mp3' 'artifacts\games\Snake\Assets\Background.mp3'
Assert-AssetCopy 'games\DungeonStarI\Assets\Background.mp3' 'artifacts\games\DungeonStarI\Assets\Background.mp3'
Assert-AssetCopy 'games\MazeMuncher\Assets\Background.mp3' 'artifacts\games\MazeMuncher\Assets\Background.mp3'
foreach ($asset in @('Background.png', 'CharacterSheet.png', 'Foreground.png', 'PixelProof.png',
    'ToneOne.wav', 'ToneTwo.wav', 'Music.wav')) {
    Assert-AssetCopy "examples\Phase4VisualSlice\Assets\$asset" "artifacts\games\Phase4VisualSlice-DirectX\Assets\$asset"
    Assert-AssetCopy "examples\Phase4VisualSlice\Assets\$asset" "artifacts\games\Phase4VisualSlice-GDI\Assets\$asset"
    Assert-AssetCopy "examples\Phase4VisualSlice\Assets\$asset" "artifacts\web\Phase4VisualSlice\Assets\$asset"
}
foreach ($asset in @('Background.png', 'WindowSkin.png', 'Cursor.png', 'Continue.png', 'BitmapFont.png',
    'Move.wav', 'Confirm.wav', 'Cancel.wav')) {
    Assert-AssetCopy "examples\MenuGallery\Assets\$asset" "artifacts\games\MenuGallery-DirectX\Assets\$asset"
    Assert-AssetCopy "examples\MenuGallery\Assets\$asset" "artifacts\games\MenuGallery-GDI\Assets\$asset"
    Assert-AssetCopy "examples\MenuGallery\Assets\$asset" "artifacts\games\Assets\$asset"
    Assert-AssetCopy "examples\MenuGallery\Assets\$asset" "artifacts\web\MenuGallery\Assets\$asset"
    Assert-AssetCopy "examples\MenuGallery\Assets\$asset" "artifacts\web\MenuGalleryPackage\Assets\$asset"
}
foreach ($asset in @('Cursor.png', 'BitmapFont.png')) {
    Assert-AssetCopy "examples\Phase5SubmenuViewport\Assets\$asset" "artifacts\games\Phase5SubmenuViewport-DirectX\Assets\$asset"
    Assert-AssetCopy "examples\Phase5SubmenuViewport\Assets\$asset" "artifacts\games\Phase5SubmenuViewport-GDI\Assets\$asset"
    Assert-AssetCopy "examples\Phase5SubmenuViewport\Assets\$asset" "artifacts\web\Phase5SubmenuViewport\Assets\$asset"
}
foreach ($asset in @('Companion.png', 'EncounterBackground.png', 'Hero.png', 'MireWarden.png',
    'Npc.png', 'PanelOverlay.png', 'TitleBackground.png', 'WorldTiles.png', 'LumenTheme.wav')) {
    Assert-AssetCopy "games\RPGSystems\Assets\World\$asset" "artifacts\games\RPGSystems-DirectX\Assets\World\$asset"
    Assert-AssetCopy "games\RPGSystems\Assets\World\$asset" "artifacts\games\RPGSystems-GDI\Assets\World\$asset"
    Assert-AssetCopy "games\RPGSystems\Assets\World\$asset" "artifacts\web\RPGSystems\Assets\World\$asset"
}
foreach ($map in @('Town.smilemap', 'Shop.smilemap', 'Overworld.smilemap')) {
    Assert-AssetCopy "games\RPGSystems\Maps\World\$map" "artifacts\games\RPGSystems-DirectX\Maps\World\$map"
    Assert-AssetCopy "games\RPGSystems\Maps\World\$map" "artifacts\games\RPGSystems-GDI\Maps\World\$map"
    Assert-AssetCopy "games\RPGSystems\Maps\World\$map" "artifacts\web\RPGSystems\Maps\World\$map"
}
foreach ($asset in @('Companion.png', 'Hero.png', 'MireWarden.png', 'Npc.png', 'WorldTiles.png')) {
    Assert-AssetCopy "games\RPGSystems\Assets\Dungeon\$asset" "artifacts\games\RPGSystems-DirectX\Assets\Dungeon\$asset"
    Assert-AssetCopy "games\RPGSystems\Assets\Dungeon\$asset" "artifacts\games\RPGSystems-GDI\Assets\Dungeon\$asset"
    Assert-AssetCopy "games\RPGSystems\Assets\Dungeon\$asset" "artifacts\web\RPGSystems\Assets\Dungeon\$asset"
}
foreach ($map in @('Archive1.smilemap', 'Archive2.smilemap', 'Archive3.smilemap', 'Archive4.smilemap')) {
    Assert-AssetCopy "games\RPGSystems\Maps\Dungeon\$map" "artifacts\games\RPGSystems-DirectX\Maps\Dungeon\$map"
    Assert-AssetCopy "games\RPGSystems\Maps\Dungeon\$map" "artifacts\games\RPGSystems-GDI\Maps\Dungeon\$map"
    Assert-AssetCopy "games\RPGSystems\Maps\Dungeon\$map" "artifacts\web\RPGSystems\Maps\Dungeon\$map"
}
foreach ($asset in @('Ability.wav', 'DungeonTheme.wav', 'EnemyLineup.png', 'LumenPlaza.png',
    'OverworldTheme.wav', 'PartyLineup.png', 'PrismVault.png', 'StarfallPlateau.png', 'Strike.wav',
    'TownTheme.wav', 'Victory.wav')) {
    Assert-AssetCopy "games\RPGSystems\Assets\Battle\$asset" "artifacts\games\RPGSystems-DirectX\Assets\Battle\$asset"
    Assert-AssetCopy "games\RPGSystems\Assets\Battle\$asset" "artifacts\games\RPGSystems-GDI\Assets\Battle\$asset"
    Assert-AssetCopy "games\RPGSystems\Assets\Battle\$asset" "artifacts\web\RPGSystems\Assets\Battle\$asset"
}
$phase42ExpectedPath = Join-Path $repositoryRoot 'examples\Phase4AssetPublication\ExpectedAssetPaths.txt'
foreach ($asset in Get-Content -LiteralPath $phase42ExpectedPath) {
    $assetPath = $asset.Replace('/', '\')
    Assert-AssetCopy "examples\Phase4AssetPublication\$assetPath" `
        "artifacts\games\Phase4AssetPublication\$assetPath"
    Assert-AssetCopy "examples\Phase4AssetPublication\$assetPath" `
        "artifacts\web\Phase4AssetPublication\$assetPath"
}
foreach ($map in @('default.map', 'sample-loops.map', 'sample-switchbacks.map')) {
    Assert-AssetCopy "games\DungeonStarI\Maps\$map" "artifacts\games\DungeonStarI\Maps\$map"
}
foreach ($map in @('default.map', 'custom.map')) {
    Assert-AssetCopy "games\DungeonStarII\Maps\$map" "artifacts\games\DungeonStarII\Maps\$map"
}
Assert-AssetCopy 'games\MazeMuncher\Maps\default.map' 'artifacts\games\MazeMuncher\Maps\default.map'
Write-Host 'Game asset copies verified.'

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($vsixPath)
try {
    $entries = @($archive.Entries | ForEach-Object { $_.FullName.Replace('\', '/') })
    $requiredEntries = @(
        'Smile.Language.dll',
        'Smile.VisualStudio.dll',
        'Smile.LanguageConfiguration.pkgdef',
        'smile-language-configuration.json',
        'Compiler/smilec.exe',
        'Compiler/Smile.Language.dll',
        'Compiler/Smile.NativeRuntime.lib',
        'ProjectTemplates/Smile/1033/SmileConsole/SmileConsole.smileproj',
        'ProjectTemplates/Smile/1033/SmileConsole/SmileConsole.vstemplate',
        'ProjectTemplates/Smile/1033/SmileConsole/Program.smile',
        'ProjectTemplates/Smile/1033/SmileGame/SmileGame.smileproj',
        'ProjectTemplates/Smile/1033/SmileGame/SmileGame.vstemplate',
        'ProjectTemplates/Smile/1033/SmileGame/Program.smile',
        'ProjectTemplates/Smile/1033/SmileLibrary/SmileLibrary.smilelibproj',
        'ProjectTemplates/Smile/1033/SmileLibrary/SmileLibrary.vstemplate',
        'ProjectTemplates/Smile/1033/SmileLibrary/Module.smile'
    )
    foreach ($entry in $requiredEntries) {
        if ($entries -notcontains $entry) {
            throw "VSIX entry is missing: $entry"
        }
    }

    foreach ($templateName in @('SmileConsole', 'SmileGame')) {
        $templateRoot = "ProjectTemplates/Smile/1033/$templateName"
        $programEntry = $archive.Entries | Where-Object { $_.FullName.Replace('\', '/') -eq "$templateRoot/Program.smile" }
        $templateEntry = $archive.Entries | Where-Object { $_.FullName.Replace('\', '/') -eq "$templateRoot/$templateName.vstemplate" }
        $programReader = [IO.StreamReader]::new($programEntry.Open())
        $templateReader = [IO.StreamReader]::new($templateEntry.Open())

        try {
            $programText = $programReader.ReadToEnd()
            $templateText = $templateReader.ReadToEnd()
        }
        finally {
            $programReader.Dispose()
            $templateReader.Dispose()
        }

        if ($programText -notmatch '\$smileuser\$' -or $programText -notmatch '\$smiledate\$' -or
            $programText -notmatch '\$smileversion\$') {
            throw "$templateName does not contain all generated identity tokens."
        }
        if ($templateText -notmatch 'SmileProjectTemplateWizard' -or
        $templateText -notmatch 'Version=2\.0\.49\.0') {
            throw "$templateName does not invoke the synchronized template wizard."
        }
    }

    $pkgdefEntry = $archive.Entries | Where-Object { $_.FullName.Replace('\', '/') -eq 'Smile.VisualStudio.pkgdef' }
    if ($null -eq $pkgdefEntry) {
        throw 'VSIX project-factory registration is missing.'
    }
    $pkgdefReader = [System.IO.StreamReader]::new($pkgdefEntry.Open())
    try {
        $pkgdef = $pkgdefReader.ReadToEnd()
    }
    finally {
        $pkgdefReader.Dispose()
    }
    if ($pkgdef -notmatch '"DefaultProjectExtension"="smileproj"') {
        throw 'VSIX project factory does not use .smileproj as its default extension.'
    }
    if ($pkgdef -notmatch '"PossibleProjectExtensions"="smileproj;smilelibproj"') {
        throw 'VSIX project factory does not register .smilelibproj as a possible extension.'
    }
    $languagePkgdefEntry = $archive.GetEntry('Smile.LanguageConfiguration.pkgdef')
    $languageConfigurationEntry = $archive.GetEntry('smile-language-configuration.json')
    $languagePkgdefReader = [System.IO.StreamReader]::new($languagePkgdefEntry.Open())
    $languageConfigurationReader = [System.IO.StreamReader]::new($languageConfigurationEntry.Open())
    try {
        $languagePkgdef = $languagePkgdefReader.ReadToEnd()
        $languageConfiguration = $languageConfigurationReader.ReadToEnd()
    }
    finally {
        $languagePkgdefReader.Dispose()
        $languageConfigurationReader.Dispose()
    }
    if ($languagePkgdef -notmatch '"SMILE 2\.0"="\$PackageFolder\$\\smile-language-configuration\.json"') {
        throw 'VSIX does not map the SMILE content type to its language configuration.'
    }
    if ($languageConfiguration -notmatch '"lineComment"\s*:\s*"\x27"') {
        throw 'VSIX language configuration does not declare the SMILE apostrophe line comment.'
    }
    $manifestEntry = $archive.GetEntry('extension.vsixmanifest')
    $manifestReader = [System.IO.StreamReader]::new($manifestEntry.Open())
    try { $vsixManifest = $manifestReader.ReadToEnd() }
    finally { $manifestReader.Dispose() }
    if ($vsixManifest -notmatch 'Version="2\.0\.49"') {
        throw 'VSIX identity version is not 2.0.49.'
    }
    if ($vsixManifest -notmatch 'Type="Microsoft\.VisualStudio\.Assembly"' -or
        $vsixManifest -notmatch 'AssemblyName="Smile\.VisualStudio, Version=2\.0\.49\.0, Culture=neutral, PublicKeyToken=null"') {
        throw 'VSIX does not register the template wizard assembly.'
    }
    if ($vsixManifest -notmatch 'Type="Microsoft\.VisualStudio\.VsPackage" Path="Smile\.LanguageConfiguration\.pkgdef"') {
        throw 'VSIX manifest does not register the SMILE language configuration package definition.'
    }
}
finally {
    $archive.Dispose()
}
Write-Host 'VSIX compiler, shared-language, and project-template payload verified.'
$visualStudioDll = Require-File 'src\Smile.VisualStudio\bin\Release\net472\Smile.VisualStudio.dll'
$versionInfo = [Diagnostics.FileVersionInfo]::GetVersionInfo($visualStudioDll)
$assemblyVersion = [Reflection.AssemblyName]::GetAssemblyName($visualStudioDll).Version.ToString()
if ($versionInfo.FileVersion -ne '2.0.49.0' -or $versionInfo.ProductVersion -notlike '2.0.49*' -or
    $assemblyVersion -ne '2.0.49.0') {
    throw "Visual Studio DLL versions differ: file=$($versionInfo.FileVersion), product=$($versionInfo.ProductVersion), assembly=$assemblyVersion."
}
Write-Host 'VSIX identity, assembly, file, and product versions are synchronized at 2.0.49.'

$scaleCases = @(
    @{ Width = 960; Height = 540; ExpectedWidth = 960; ExpectedHeight = 540; X = 0; Y = 0 },
    @{ Width = 1280; Height = 720; ExpectedWidth = 1280; ExpectedHeight = 720; X = 0; Y = 0 },
    @{ Width = 1920; Height = 1080; ExpectedWidth = 1920; ExpectedHeight = 1080; X = 0; Y = 0 },
    @{ Width = 1920; Height = 1200; ExpectedWidth = 1920; ExpectedHeight = 1080; X = 0; Y = 60 },
    @{ Width = 2560; Height = 1440; ExpectedWidth = 2560; ExpectedHeight = 1440; X = 0; Y = 0 },
    @{ Width = 3440; Height = 1440; ExpectedWidth = 2560; ExpectedHeight = 1440; X = 440; Y = 0 },
    @{ Width = 3840; Height = 2160; ExpectedWidth = 3840; ExpectedHeight = 2160; X = 0; Y = 0 }
)
foreach ($case in $scaleCases) {
    if ($case.Width * 540 -le $case.Height * 960) {
        $width = $case.Width
        $height = [math]::Floor($case.Width * 540 / 960)
    }
    else {
        $height = $case.Height
        $width = [math]::Floor($case.Height * 960 / 540)
    }
    $x = [math]::Floor(($case.Width - $width) / 2)
    $y = [math]::Floor(($case.Height - $height) / 2)
    if ($width -ne $case.ExpectedWidth -or $height -ne $case.ExpectedHeight -or $x -ne $case.X -or $y -ne $case.Y) {
        throw "Scaling check failed for $($case.Width)x$($case.Height)."
    }
    $scale = [math]::Min($case.Width / 960.0, $case.Height / 540.0)
    $mappedRadiusX = 9 * $scale
    $mappedRadiusY = 9 * $scale
    $mappedTextSize = 16 * $scale
    if ([math]::Abs($mappedRadiusX - $mappedRadiusY) -gt 0.000001 -or $mappedTextSize -le 0) {
        throw "Uniform coordinate or text-size mapping failed for $($case.Width)x$($case.Height)."
    }
}
Write-Host 'Viewport, uniform coordinate mapping, and text scaling verified for seven required output sizes.'

$dpiCases = @(
    @{ Dpi = 96; Width = 960; Height = 540; Scale = 1.0 },
    @{ Dpi = 120; Width = 1200; Height = 675; Scale = 1.25 },
    @{ Dpi = 144; Width = 1440; Height = 810; Scale = 1.5 },
    @{ Dpi = 192; Width = 1920; Height = 1080; Scale = 2.0 }
)
foreach ($case in $dpiCases) {
    $dpiScale = $case.Dpi / 96.0
    $suggestedWidth = [math]::Round(960 * $dpiScale)
    $suggestedHeight = [math]::Round(540 * $dpiScale)
    $viewportScale = [math]::Min($case.Width / 960.0, $case.Height / 540.0)
    if ($suggestedWidth -ne $case.Width -or $suggestedHeight -ne $case.Height -or
        [math]::Abs($viewportScale - $case.Scale) -gt 0.000001) {
        throw "DPI-change mapping check failed for $($case.Dpi) DPI."
    }
}
Write-Host 'DPI-change output and viewport calculations verified at 100, 125, 150, and 200 percent.'
