param(
    [Parameter(Mandatory = $true)]
    [string]$InstanceId,

    [switch]$RemoveOrphans,

    [string]$BuiltDllPath,

    [string]$ManifestPath,

    [string]$BuiltVsixPath
)

$ErrorActionPreference = 'Stop'

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
$extensionsRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $env:LOCALAPPDATA "Microsoft\VisualStudio\18.0_$InstanceId\Extensions"))

if (-not (Test-Path -LiteralPath $extensionsRoot)) {
    throw "Visual Studio extension directory was not found: $extensionsRoot"
}

if ($RemoveOrphans) {
    foreach ($directory in Get-ChildItem -LiteralPath $extensionsRoot -Directory) {
        $dllPath = Join-Path $directory.FullName 'Smile.VisualStudio.dll'
        $vsixManifestPath = Join-Path $directory.FullName 'extension.vsixmanifest'
        if (-not (Test-Path -LiteralPath $dllPath) -or (Test-Path -LiteralPath $vsixManifestPath)) {
            continue
        }

        $files = @(Get-ChildItem -LiteralPath $directory.FullName -Recurse -File)
        $allowedAssemblies = @{
            'Smile.VisualStudio.dll' = 'Smile.VisualStudio'
            'Smile.Language.dll' = 'Smile.Language'
        }
        $unexpected = @($files | Where-Object {
            -not $allowedAssemblies.ContainsKey($_.Name) -or
            [Reflection.AssemblyName]::GetAssemblyName($_.FullName).Name -ne $allowedAssemblies[$_.Name]
        })
        if ($files.Count -lt 1 -or $files.Count -gt $allowedAssemblies.Count -or $unexpected.Count -ne 0 -or
            [Reflection.AssemblyName]::GetAssemblyName($dllPath).Name -ne 'Smile.VisualStudio') {
            throw "Refusing to remove unexpected orphan extension contents: $($directory.FullName)"
        }

        $resolvedDirectory = [System.IO.Path]::GetFullPath($directory.FullName)
        if (-not $resolvedDirectory.StartsWith($extensionsRoot + [System.IO.Path]::DirectorySeparatorChar,
                [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to remove a directory outside the Visual Studio extension root: $resolvedDirectory"
        }

        Remove-Item -LiteralPath $resolvedDirectory -Recurse -Force
        Write-Output "Removed orphaned SMILE extension directory: $resolvedDirectory"
    }
}

if ([string]::IsNullOrWhiteSpace($BuiltDllPath) -and [string]::IsNullOrWhiteSpace($ManifestPath)) {
    exit 0
}
if ([string]::IsNullOrWhiteSpace($BuiltDllPath) -or [string]::IsNullOrWhiteSpace($ManifestPath)) {
    throw 'BuiltDllPath and ManifestPath must be supplied together.'
}

$builtDll = [System.IO.Path]::GetFullPath($BuiltDllPath)
$sourceManifest = [System.IO.Path]::GetFullPath($ManifestPath)
if (-not (Test-Path -LiteralPath $builtDll)) {
    throw "Built SMILE extension DLL was not found: $builtDll"
}
if (-not (Test-Path -LiteralPath $sourceManifest)) {
    throw "SMILE VSIX manifest was not found: $sourceManifest"
}

$expectedIdentity = ([xml](Get-Content -LiteralPath $sourceManifest -Raw)).PackageManifest.Metadata.Identity
$installedManifests = @(Get-ChildItem -LiteralPath $extensionsRoot -Recurse -Filter extension.vsixmanifest |
    Where-Object {
        ([xml](Get-Content -LiteralPath $_.FullName -Raw)).PackageManifest.Metadata.Identity.Id -eq $expectedIdentity.Id
    })
if ($installedManifests.Count -ne 1) {
    throw "Expected one installed $($expectedIdentity.Id) manifest, found $($installedManifests.Count)."
}

$installedManifest = $installedManifests[0]
$installedIdentity = ([xml](Get-Content -LiteralPath $installedManifest.FullName -Raw)).PackageManifest.Metadata.Identity
if ($installedIdentity.Version -ne $expectedIdentity.Version) {
    throw "Installed VSIX version $($installedIdentity.Version) does not match expected version $($expectedIdentity.Version)."
}

$installedDll = Join-Path $installedManifest.DirectoryName 'Smile.VisualStudio.dll'
if (-not (Test-Path -LiteralPath $installedDll)) {
    throw "Installed SMILE extension DLL was not found: $installedDll"
}

$builtHash = Get-Sha256 $builtDll
$installedHash = Get-Sha256 $installedDll
if ($installedHash -ne $builtHash) {
    throw 'Installed SMILE extension DLL hash does not match the newly built DLL.'
}

$expectedVersion = [Version]$expectedIdentity.Version
$expectedAssemblyVersion = [Version]::new(
    $expectedVersion.Major, $expectedVersion.Minor, $expectedVersion.Build, 0)
$installedAssemblyVersion = [Reflection.AssemblyName]::GetAssemblyName($installedDll).Version
if ($installedAssemblyVersion -ne $expectedAssemblyVersion) {
    throw "Installed assembly version $installedAssemblyVersion does not match expected version $expectedAssemblyVersion."
}

Write-Output "Verified SMILE VSIX $($installedIdentity.Version)."
Write-Output "Installed DLL: $installedDll"
Write-Output "Assembly version: $installedAssemblyVersion"
Write-Output "SHA256: $installedHash"

if ($BuiltVsixPath) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $installedRoot = [IO.Path]::GetFullPath($installedManifest.DirectoryName) + [IO.Path]::DirectorySeparatorChar
    $archive = [IO.Compression.ZipFile]::OpenRead([IO.Path]::GetFullPath($BuiltVsixPath))
    $verifiedPayloads = 0
    try {
        foreach ($entry in $archive.Entries) {
            $name = $entry.FullName.Replace('\', '/')
            if ($name.EndsWith('/') -or $name -notmatch '^(Compiler/|ProjectTemplates/|ItemTemplates/|Smile\.(Language|VisualStudio)\.dll$|smile-language-configuration\.json$|Smile\.LanguageConfiguration\.pkgdef$)') { continue }
            $installedPayload = [IO.Path]::GetFullPath((Join-Path $installedRoot $name))
            if (-not $installedPayload.StartsWith($installedRoot, [StringComparison]::OrdinalIgnoreCase)) {
                throw "VSIX payload escapes its installation directory: $name"
            }
            if (-not (Test-Path -LiteralPath $installedPayload -PathType Leaf)) {
                throw "Installed VSIX payload is missing: $name"
            }
            $stream = $entry.Open()
            $algorithm = [Security.Cryptography.SHA256]::Create()
            try { $entryHash = [BitConverter]::ToString($algorithm.ComputeHash($stream)).Replace('-', '') }
            finally { $algorithm.Dispose(); $stream.Dispose() }
            if ((Get-Sha256 $installedPayload) -cne $entryHash) {
                throw "Installed VSIX payload differs from the built archive: $name"
            }
            $verifiedPayloads++
        }
        if ($verifiedPayloads -lt 4) { throw 'VSIX did not contain the expected compiler, language and template payloads.' }
    }
    finally { $archive.Dispose() }
    Write-Output "Verified $verifiedPayloads installed compiler, language, library and template payload hashes against the built VSIX."
}
