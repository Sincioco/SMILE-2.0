$ErrorActionPreference = "Stop"

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$failures = New-Object System.Collections.Generic.List[string]

function Pass([string]$Message) {
    Write-Host "[OK] $Message" -ForegroundColor Green
}

function Fail([string]$Message, [string]$Remediation) {
    Write-Host "[ERROR] $Message" -ForegroundColor Red
    Write-Host "        Remediation: $Remediation" -ForegroundColor Yellow
    $failures.Add($Message)
}

if ([Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT -and [Environment]::Is64BitOperatingSystem) {
    Pass "Windows x64 host"
}
else {
    Fail "SMILE native builds require Windows x64." "Run the build on a 64-bit Windows development machine."
}

if ($PSVersionTable.PSVersion -ge [Version]"5.1") {
    Pass "Windows PowerShell $($PSVersionTable.PSVersion)"
}
else {
    Fail "Windows PowerShell 5.1 or newer is required." "Enable Windows PowerShell 5.1."
}

$dotnet = Get-Command dotnet.exe -ErrorAction SilentlyContinue
if ($null -eq $dotnet) {
    Fail ".NET SDK was not found." "Install the .NET 10 SDK version selected by global.json."
}
else {
    $sdkOutput = @(& $dotnet.Source --version 2>$null)
    $sdkExitCode = $LASTEXITCODE
    $sdkVersion = ($sdkOutput | Select-Object -First 1).Trim()
    if ($sdkExitCode -eq 0 -and $sdkVersion -eq "10.0.400") {
        Pass ".NET SDK $sdkVersion"
    }
    else {
        Fail ".NET SDK 10.0.400 is required; resolved '$sdkVersion'." `
            "Install SDK 10.0.400 and rerun from this repository so global.json can select it."
    }

    $targetingPacks = Join-Path (Split-Path $dotnet.Source -Parent) "packs\Microsoft.NETCore.App.Ref"
    if (Test-Path (Join-Path $targetingPacks "10.0.*")) {
        Pass ".NET 10 targeting pack"
    }
    else {
        Fail ".NET 10 targeting pack was not found." "Repair or reinstall the .NET 10 SDK."
    }
}

$programFilesX86 = [Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFilesX86)
$vswhere = Join-Path $programFilesX86 "Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path $vswhere)) {
    Fail "vswhere.exe was not found." "Install Visual Studio 2026 or repair the Visual Studio Installer."
}
else {
    Pass "vswhere.exe"
    $installationPath = (& $vswhere -latest -products * `
        -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 `
        -property installationPath | Select-Object -First 1).Trim()
    if ([string]::IsNullOrWhiteSpace($installationPath)) {
        Fail "Visual Studio C++ x64 workload was not found." `
            "Install Desktop development with C++ in Visual Studio Installer."
    }
    else {
        Pass "Visual Studio C++ x64 toolchain at $installationPath"
        $vcvars = Join-Path $installationPath "VC\Auxiliary\Build\vcvars64.bat"
        if (Test-Path $vcvars) { Pass "vcvars64.bat" }
        else { Fail "vcvars64.bat was not found." "Repair the Visual Studio C++ workload." }

        $msbuild = Join-Path $installationPath "MSBuild\Current\Bin\MSBuild.exe"
        if (Test-Path $msbuild) { Pass "MSBuild.exe" }
        else { Fail "MSBuild.exe was not found." "Repair Visual Studio." }

        $toolsVersionFile = Join-Path $installationPath `
            "VC\Auxiliary\Build\Microsoft.VCToolsVersion.default.txt"
        $toolsVersion = if (Test-Path $toolsVersionFile) {
            (Get-Content -LiteralPath $toolsVersionFile -Raw).Trim()
        } else { "" }
        foreach ($tool in @("link.exe", "ml64.exe")) {
            $toolPath = Join-Path $installationPath `
                "VC\Tools\MSVC\$toolsVersion\bin\Hostx64\x64\$tool"
            if (-not [string]::IsNullOrWhiteSpace($toolsVersion) -and (Test-Path $toolPath)) {
                Pass "$tool"
            }
            else {
                Fail "$tool was not found." "Repair the Visual Studio C++ x64 workload."
            }
        }
    }

    $vsixInstaller = if ([string]::IsNullOrWhiteSpace($installationPath)) { "" } else {
        Join-Path $installationPath "Common7\IDE\VSIXInstaller.exe"
    }
    $shellInterop = if ([string]::IsNullOrWhiteSpace($installationPath)) { "" } else {
        Join-Path $installationPath "Common7\IDE\PublicAssemblies\Microsoft.VisualStudio.Shell.Interop.dll"
    }
    if (-not (Test-Path $vsixInstaller) -or -not (Test-Path $shellInterop)) {
        Fail "Visual Studio extension development workload was not found." `
            "Install Visual Studio extension development in Visual Studio Installer."
    }
    else {
        Pass "Visual Studio extension development workload"
    }
}

$node = Get-Command node.exe -ErrorAction SilentlyContinue
if ($null -eq $node) {
    Fail "Node.js was not found." "Install Node.js 20 or newer for Web smoke tests."
}
else {
    $nodeText = (& $node.Source --version).Trim().TrimStart("v")
    $nodeVersion = $null
    if ([Version]::TryParse($nodeText, [ref]$nodeVersion) -and $nodeVersion.Major -ge 20) {
        Pass "Node.js $nodeVersion"
    }
    else {
        Fail "Node.js 20 or newer is required; resolved '$nodeText'." "Install a current Node.js LTS release."
    }
}

foreach ($relativeDirectory in @("artifacts\temp", "src\Smile.Compiler\obj")) {
    $directory = Join-Path $repositoryRoot $relativeDirectory
    try {
        [IO.Directory]::CreateDirectory($directory) | Out-Null
        $probe = Join-Path $directory (".smile-doctor-" + [Guid]::NewGuid().ToString("N") + ".tmp")
        [IO.File]::WriteAllText($probe, "probe")
        Remove-Item -LiteralPath $probe -Force
        Pass "Writable $relativeDirectory"
    }
    catch {
        Fail "$relativeDirectory is not writable." "Grant the current user write access to the repository."
    }
}

if ($failures.Count -ne 0) {
    Write-Host "SMILE developer environment has $($failures.Count) blocking issue(s)." -ForegroundColor Red
    exit 1
}

Write-Host "SMILE developer environment is ready." -ForegroundColor Green
