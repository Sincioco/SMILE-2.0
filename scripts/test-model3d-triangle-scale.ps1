[CmdletBinding()]
param([switch]$CookOnly)

$ErrorActionPreference = 'Stop'
$repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$testRoot = Join-Path $repository 'artifacts\temp\model3d-triangle-scale'
$assetTool = Join-Path $repository 'artifacts\assettool\smileasset.exe'
$compiler = Join-Path $repository 'artifacts\compiler\smilec.exe'
$null = New-Item -ItemType Directory -Force -Path (Join-Path $testRoot 'Assets')
$template = Get-Content -LiteralPath (Join-Path $repository 'examples\Renderer3DModelTests\Source\PbrTriangle.gltf') -Raw

foreach ($name in @('Small', 'Normal', 'Large', 'Collinear', 'Duplicate')) {
    $model = $template | ConvertFrom-Json -AsHashtable
    $scale = switch ($name) { 'Small' { 0.0001 } 'Large' { 10000 } default { 1 } }
    $buffer = [Convert]::FromBase64String($model.buffers[0].uri.Split(',')[1])
    for ($index = 0; $index -lt 9; $index++) {
        $value = [BitConverter]::ToSingle($buffer, $index * 4) * $scale
        if ($name -eq 'Collinear' -and $index -eq 7) { $value = 0 }
        if ($name -eq 'Duplicate' -and $index -ge 6) {
            $value = [BitConverter]::ToSingle($buffer, ($index - 6) * 4)
        }
        [BitConverter]::GetBytes([single]$value).CopyTo($buffer, $index * 4)
    }
    $model.buffers[0].uri = 'data:application/octet-stream;base64,' + [Convert]::ToBase64String($buffer)
    $model.accessors[0].Remove('min')
    $model.accessors[0].Remove('max')
    $model.materials = @(@{ name = 'Scale Test'; pbrMetallicRoughness = @{ roughnessFactor = 1 } })
    $model.Remove('images')
    $model.Remove('textures')
    $source = Join-Path $testRoot "$name.gltf"
    $model | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $source
    $output = Join-Path $testRoot "Assets\$name.sm3d"
    $log = (& $assetTool model $source --format-version 2 -o $output 2>&1) -join "`n"
    $exitCode = $LASTEXITCODE
    Set-Content -LiteralPath (Join-Path $testRoot "$name.log") -Value $log
    if ($name -in @('Collinear', 'Duplicate')) {
        if ($exitCode -eq 0 -or $log -notmatch 'SMA1170') { throw "Expected degenerate rejection: $name. $log" }
    } elseif ($exitCode -ne 0) {
        throw "Valid $name triangle rejected. $log"
    } else {
        & $assetTool inspect $output | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "Cooked $name triangle failed inspection." }
    }
}
Write-Host 'Triangle scale cooking: small, normal and large accepted; collinear and duplicate rejected.'
if ($CookOnly) { return }

# A valid checksum and bounds isolate the native triangle check from other gates.
$collapsed = [IO.File]::ReadAllBytes((Join-Path $testRoot 'Assets\Normal.sm3d'))
$chunkCount = [BitConverter]::ToUInt32($collapsed, 20)
for ($index = 0; $index -lt $chunkCount; $index++) {
    $entry = 64 + $index * 32
    if ([Text.Encoding]::ASCII.GetString($collapsed, $entry, 4) -ceq 'INDX') {
        $offset = [BitConverter]::ToUInt32($collapsed, $entry + 8)
        [Array]::Copy($collapsed, $offset, $collapsed, $offset + 4, 4)
    }
}
[uint32]$checksum = 2166136261
for ($index = 64; $index -lt $collapsed.Length; $index++) {
    $checksum = [uint32](([uint64]($checksum -bxor $collapsed[$index]) * 16777619) -band [uint64]4294967295)
}
[BitConverter]::GetBytes($checksum).CopyTo($collapsed, 16)
[IO.File]::WriteAllBytes((Join-Path $testRoot 'Assets\Collapsed.sm3d'), $collapsed)

$program = @'
Option Explicit

Import Smile.Simple3D.Core As Core
Import Smile.Simple3D.Graphics3D As Graphics

Dim Model As Core.Model3D
Dim Failures As Number

Game Window "SMILE Triangle Scale Regression" Size 320 By 180

Do

    Model = Graphics.LoadModel3D("Assets\Small.sm3d")

    If Model.Handle = 0 Then
        Failures = Failures + 1
    End If

    Call Graphics.DestroyModel3D(Model)

    Model = Graphics.LoadModel3D("Assets\Normal.sm3d")

    If Model.Handle = 0 Then
        Failures = Failures + 1
    End If

    Call Graphics.DestroyModel3D(Model)

    Model = Graphics.LoadModel3D("Assets\Large.sm3d")

    If Model.Handle = 0 Then
        Failures = Failures + 1
    End If

    Call Graphics.DestroyModel3D(Model)

    Model = Graphics.LoadModel3D("Assets\Collapsed.sm3d")

    If Model.Handle <> 0 Or Graphics.LiveModelCount3D() <> 0 Then
        Failures = Failures + 1
    End If

Loop Until True

Print "Triangle scale native failures: "; Failures
'@
Set-Content -LiteralPath (Join-Path $testRoot 'Program.smile') -Value $program
$project = @"
<SmileProject Version="1.0">
  <PropertyGroup><ProjectKind>Game</ProjectKind><StartupFile>Program.smile</StartupFile><ApplicationId>smile.tests.triangle-scale</ApplicationId></PropertyGroup>
  <ItemGroup>
    <SmileSource Include="Program.smile" StartupOnly="true" />
    <Asset Include="Assets\*.sm3d" />
    <SmileProjectReference Include="$repository\libraries\Smile.Simple3D\Smile.Simple3D.smilelibproj" />
  </ItemGroup>
</SmileProject>
"@
$projectPath = Join-Path $testRoot 'TriangleScale.smileproj'
Set-Content -LiteralPath $projectPath -Value $project
$executable = Join-Path $testRoot 'bin\TriangleScale.exe'
& $compiler --project $projectPath --target windows-x64 --configuration Release --graphics DirectX -o $executable
if ($LASTEXITCODE -ne 0) { throw 'Triangle scale native compilation failed.' }
$result = (& (Join-Path $PSScriptRoot 'run-bounded-test.cmd') 30 $executable) -join "`n"
if ($LASTEXITCODE -ne 0 -or $result.Trim() -cne 'Triangle scale native failures: 0') {
    throw "Triangle scale native validation failed: $result"
}
Write-Host $result.Trim()
