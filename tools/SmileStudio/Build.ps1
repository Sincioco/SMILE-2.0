#requires -Version 7.0
[CmdletBinding()]
param(
    [ValidateSet('Native', 'Web', 'All')][string]$Target = 'All',
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release',
    [ValidateSet('Full', 'Low', 'Medium', 'High')][string]$WebQuality = 'Full',
    [switch]$PublicRoster,
    [switch]$PrepareOnly
)
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot '..\Character3DViewer\Build.ps1') `
    -Studio -Target $Target -Configuration $Configuration -WebQuality $WebQuality `
    -PublicRoster:$PublicRoster -PrepareOnly:$PrepareOnly
