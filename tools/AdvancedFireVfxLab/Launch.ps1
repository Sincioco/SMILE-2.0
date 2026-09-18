[CmdletBinding()]
param([switch]$Build)
$ErrorActionPreference = 'Stop'
$fireExecutable = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'bin\Release\AdvancedFireVfxLab.exe'))
foreach ($fireProcess in @(Get-Process -Name 'AdvancedFireVfxLab*' -ErrorAction SilentlyContinue)) {
    if ($fireProcess.Path -eq $fireExecutable -or
        $fireProcess.Path.StartsWith($fireExecutable + '~RF', [StringComparison]::OrdinalIgnoreCase)) {
        [void]$fireProcess.CloseMainWindow()
        if (-not $fireProcess.WaitForExit(10000)) { throw 'The previous Fire Lab did not close gracefully.' }
    }
}
if ($Build -or -not (Test-Path -LiteralPath $fireExecutable)) {
    & (Join-Path $PSScriptRoot 'Build.ps1') -Target Native
}
Start-Process -FilePath $fireExecutable -WorkingDirectory (Split-Path -Parent $fireExecutable)
