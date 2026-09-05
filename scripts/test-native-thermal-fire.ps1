[CmdletBinding()]
param([switch]$SkipBuild)

$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location $taskRoot
try {
    if (-not $SkipBuild) {
        & scripts\build.cmd
        if ($LASTEXITCODE -ne 0) { throw 'SMILE build failed.' }
    }
    & scripts\generate-thermal-fire-assets.ps1 -Check
    $taskVs = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" `
        -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
    if (-not $taskVs) { throw 'Visual C++ tools unavailable.' }
    $taskSetup = Join-Path $taskVs 'VC\Auxiliary\Build\vcvars64.bat'
    & cmd.exe /d /c "call `"$taskSetup`" >nul && cl /nologo /EHsc /O2 tools\AdvancedFireVfxLab\ThermalDynamicsTests.cpp /Foartifacts\tests\ThermalDynamicsTests.obj /Feartifacts\tests\ThermalDynamicsTests.exe"
    if ($LASTEXITCODE -ne 0) { throw 'Thermal reference test compilation failed.' }
    & artifacts\tests\ThermalDynamicsTests.exe
    if ($LASTEXITCODE -ne 0) { throw 'Thermal reference tests failed.' }
    $taskLibraries = 'kernel32.lib user32.lib gdi32.lib gdiplus.lib dwmapi.lib d3d11.lib d3dcompiler.lib dxgi.lib d2d1.lib dwrite.lib windowscodecs.lib winmm.lib shell32.lib ole32.lib windowsapp.lib xaudio2.lib'
    & cmd.exe /d /c "call `"$taskSetup`" >nul && cl /nologo /MT /EHsc /O2 tools\AdvancedFireVfxLab\NativeFireGpuTests.cpp /Foartifacts\tests\NativeFireGpuTests.obj /Feartifacts\tests\NativeFireGpuTests.exe /link artifacts\runtime\Smile.NativeRuntime.lib $taskLibraries"
    if ($LASTEXITCODE -ne 0) { throw 'Native GPU recovery test compilation failed.' }
    & scripts\run-bounded-test.cmd 60 (Join-Path $taskRoot 'artifacts\tests\NativeFireGpuTests.exe')
    if ($LASTEXITCODE -ne 0) { throw 'Native GPU recovery test failed.' }
    & tools\AdvancedFireVfxLab\Build.ps1 -Configuration Debug -Target Native
    & artifacts\compiler\smilec.exe --project tools\AdvancedFireVfxLab\FireEmitterTests.smileproj `
        --target windows-x64 --configuration Debug --graphics DirectX -o artifacts\tests\FireEmitterTests.exe
    if ($LASTEXITCODE -ne 0) { throw 'Fire contract compilation failed.' }
    $taskResult = & scripts\run-bounded-test.cmd 60 (Join-Path $taskRoot 'artifacts\tests\FireEmitterTests.exe')
    if ($LASTEXITCODE -ne 0 -or ($taskResult -join "`n").Trim() -cne 'FireEmitter3D tests passed') {
        throw "Fire contract failed: $taskResult"
    }
    Write-Host $taskResult
    & artifacts\compiler\smilec.exe --project tools\AdvancedFireVfxLab\FireEmitterTests.smileproj `
        --target web --configuration Debug --output-dir artifacts\web\FireEmitterTests
    if ($LASTEXITCODE -ne 0) { throw 'Fire fallback Web compilation failed.' }
    & node scripts\run-web-test.js artifacts\web\FireEmitterTests `
        --expected tools\AdvancedFireVfxLab\expected.txt --renderer3d --frames 20 --timeout 60000
    if ($LASTEXITCODE -ne 0) { throw 'Fire fallback Web contract failed.' }
}
finally { Pop-Location }
