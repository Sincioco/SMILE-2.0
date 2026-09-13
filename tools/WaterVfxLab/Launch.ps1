[CmdletBinding()]
param([switch]$Build)
$ErrorActionPreference = 'Stop'
$waterExecutable = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'bin\Release\WaterVfxLab.exe'))
foreach ($waterProcess in @(Get-Process -Name WaterVfxLab -ErrorAction SilentlyContinue)) {
    if ($waterProcess.Path -eq $waterExecutable) {
        [void]$waterProcess.CloseMainWindow()
        if (-not $waterProcess.WaitForExit(10000)) { throw 'The previous Water Lab did not close gracefully.' }
    }
}
if ($Build -or -not (Test-Path -LiteralPath $waterExecutable)) { & (Join-Path $PSScriptRoot 'Build.ps1') }
Start-Process -FilePath $waterExecutable -WorkingDirectory (Split-Path -Parent $waterExecutable)
