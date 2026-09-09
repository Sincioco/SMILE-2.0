[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$target = Join-Path $root 'artifacts\tests\startup-contract'
[void][IO.Directory]::CreateDirectory($target)
$utf8 = [Text.UTF8Encoding]::new($false)
$compiler = Join-Path $root 'artifacts\compiler\smilec.exe'
$source = Join-Path $target 'Program.smile'
[IO.File]::WriteAllText($source, "Game Window `"Startup Contract`"`n`nShow Screen`nEnd Program`n", $utf8)
& $compiler $source --target web --output-dir (Join-Path $target 'Web')
if ($LASTEXITCODE -ne 0) { throw 'Startup Web compilation failed.' }
& node (Join-Path $PSScriptRoot 'test-startup-web.js') (Join-Path $target 'Web')
if ($LASTEXITCODE -ne 0) { throw 'Startup presentation contract failed.' }
& node (Join-Path $PSScriptRoot 'run-web-test.js') (Join-Path $target 'Web') --startup-loading
if ($LASTEXITCODE -ne 0) { throw 'Startup streaming/cache contract failed.' }

# Exercise the same native startup owner used by generated custom-entry executables.
# Begin returns only after painting/flushing the embedded logo; Ready overlaps preparation.
$native = @'
#include <windows.h>
#include <stdio.h>
#include "startup/startup.h"
int main(int argc, char**) {
    ULONGLONG start = GetTickCount64();
    smile_startup_begin("SMILE Startup Timing", "", "Native startup timing fixture");
    ULONGLONG painted = GetTickCount64();
    if (argc > 1) Sleep(1500);
    ULONGLONG ready = GetTickCount64();
    smile_startup_ready();
    ULONGLONG closed = GetTickCount64();
    printf("paint-ready=%llu ms; preparation=%llu ms; remaining=%llu ms; total=%llu ms\n",
        painted - start, ready - painted, closed - ready, closed - start);
    if (closed - start < 1000) return 1;
    if (argc > 1 && closed - ready >= 1000) return 2;
    for (int cycle = 0; cycle < 2; ++cycle) {
        smile_startup_resume(0);
        ULONGLONG repainted = GetTickCount64();
        smile_startup_resume(0);
        smile_startup_ready();
        ULONGLONG reclosed = GetTickCount64();
        printf("reload=%d; visible=%llu ms\n", cycle, reclosed - repainted);
        if (reclosed - repainted < 1000) return 3;
    }
    return 0;
}
'@
[IO.File]::WriteAllText((Join-Path $target 'startup-timing.cpp'), $native, $utf8)
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$vs = & $vswhere -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
if ($LASTEXITCODE -ne 0 -or !$vs) { throw 'Visual Studio C++ tools are required.' }
$command = @"
@echo off
call "$vs\VC\Auxiliary\Build\vcvars64.bat" >nul
if errorlevel 1 exit /b 1
cl /nologo /EHsc /MT /I"$root\src\Smile.NativeRuntime" "$target\startup-timing.cpp" /Fo"$target\startup-timing.obj" /Fe"$target\startup-timing.exe" /link "$root\artifacts\runtime\Smile.NativeRuntime.lib" user32.lib gdi32.lib gdiplus.lib dwmapi.lib shell32.lib ole32.lib
exit /b %errorlevel%
"@
[IO.File]::WriteAllText((Join-Path $target 'build-timing.cmd'), $command, $utf8)
& (Join-Path $target 'build-timing.cmd')
if ($LASTEXITCODE -ne 0) { throw 'Native startup timing fixture build failed.' }
& (Join-Path $target 'startup-timing.exe')
if ($LASTEXITCODE -ne 0) { throw 'Native fast startup timing failed.' }
& (Join-Path $target 'startup-timing.exe') slow
if ($LASTEXITCODE -ne 0) { throw 'Native slow startup overlap failed.' }

[IO.File]::WriteAllText($source, "Print `"Startup console output preserved.`"`n", $utf8)
& $compiler $source -o (Join-Path $target 'Console.exe')
if ($LASTEXITCODE -ne 0) { throw 'Native startup console compilation failed.' }
$output = & (Join-Path $target 'Console.exe')
if ($LASTEXITCODE -ne 0 -or $output -ne 'Startup console output preserved.') { throw 'Startup changed console output.' }
Write-Host 'PASS: native visible minimum/overlap, embedded logo, custom-entry console, Web startup and download contracts.'

# The public operation is emitted normally on both targets; never patch emitted programs.
$reload = @'
Game Window "Reload Contract"

Dim Shown As Boolean
Dim CycleIndex As Number

Show Screen

For CycleIndex = 1 To 2
    Shown = Window_Loading()
    Show Screen
End For

Print "Reload contract complete."
End Program
'@
[IO.File]::WriteAllText($source, $reload, $utf8)
& $compiler $source -o (Join-Path $target 'Reload.exe')
if ($LASTEXITCODE -ne 0) { throw 'Native reload compilation failed.' }
$output = & (Join-Path $PSScriptRoot 'run-bounded-test.cmd') 20 (Join-Path $target 'Reload.exe')
if ($LASTEXITCODE -ne 0 -or $output -ne 'Reload contract complete.') { throw 'Native reload execution failed.' }
& $compiler $source --target web --output-dir (Join-Path $target 'ReloadWeb')
if ($LASTEXITCODE -ne 0) { throw 'Web reload compilation failed.' }
& node (Join-Path $PSScriptRoot 'run-web-test.js') (Join-Path $target 'ReloadWeb') --frames 8
if ($LASTEXITCODE -ne 0) { throw 'Web reload execution failed.' }
Write-Host 'PASS: repeated Window_Loading calls execute through generated native/Web programs.'
