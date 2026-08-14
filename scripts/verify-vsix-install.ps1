param(
    [Parameter(Mandatory = $true)]
    [string]$InstanceId,

    [switch]$RemoveOrphans,

    [string]$BuiltDllPath,

    [string]$ManifestPath
)

$ErrorActionPreference = 'Stop'
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

$builtHash = (Get-FileHash -LiteralPath $builtDll -Algorithm SHA256).Hash
$installedHash = (Get-FileHash -LiteralPath $installedDll -Algorithm SHA256).Hash
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
