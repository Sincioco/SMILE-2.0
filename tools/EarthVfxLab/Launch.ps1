[CmdletBinding()]
param([switch]$Build)
$ErrorActionPreference = 'Stop'
$earthExecutable = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'bin\Release\EarthVfxLab.exe'))
foreach ($earthProcess in @(Get-Process -Name 'EarthVfxLab*' -ErrorAction SilentlyContinue)) {
    if ($earthProcess.Path -eq $earthExecutable -or
        $earthProcess.Path.StartsWith($earthExecutable + '~RF', [StringComparison]::OrdinalIgnoreCase)) {
        [void]$earthProcess.CloseMainWindow()
        if (-not $earthProcess.WaitForExit(10000)) { throw 'The previous Earth Lab did not close gracefully.' }
    }
}
if ($Build -or -not (Test-Path -LiteralPath $earthExecutable)) { & (Join-Path $PSScriptRoot 'Build.ps1') }
Start-Process -FilePath $earthExecutable -WorkingDirectory (Split-Path -Parent $earthExecutable)
