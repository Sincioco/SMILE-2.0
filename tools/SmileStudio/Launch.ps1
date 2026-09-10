#requires -Version 7.0
[CmdletBinding()]
param([switch]$Build, [ValidateSet('Debug','Release')][string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot '..\Character3DViewer\Launch.ps1') -Studio -Build:$Build -Configuration $Configuration
