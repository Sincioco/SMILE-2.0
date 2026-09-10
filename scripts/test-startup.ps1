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
#ifdef SMILE_STARTUP_TESTS
extern "C" { int smile_startup_test_fault; void smile_startup_test_resume() {} }
static BOOL CALLBACK cancel_owned_splash(HWND candidate, LPARAM owner) {
    WCHAR name[64] = {};
    GetClassNameW(candidate, name, 64);
    if (GetWindow(candidate, GW_OWNER) == (HWND)owner && lstrcmpW(name, L"SMILE20StartupWindow") == 0)
        PostMessageW(candidate, WM_CLOSE, 0, 0);
    return TRUE;
}
static void CALLBACK restore_owner(HWND owner, UINT, UINT_PTR timer, DWORD) {
    KillTimer(owner, timer);
    ShowWindow(owner, SW_SHOWNOACTIVATE);
    ShowWindow(owner, SW_RESTORE);
}
static void CALLBACK cancel_minimized_reload(HWND owner, UINT, UINT_PTR timer, DWORD) {
    KillTimer(owner, timer);
    EnumWindows(cancel_owned_splash, (LPARAM)owner);
}
#endif
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
#ifdef SMILE_STARTUP_TESTS
    HWND owner = CreateWindowW(L"STATIC", L"Retained editing session", WS_OVERLAPPEDWINDOW,
        0, 0, 640, 480, 0, 0, GetModuleHandleW(0), 0);
    if (!owner) return 4;
    SetPropW(owner, L"UnsavedDocument", (HANDLE)123);
    // OutputDebugString lazily initializes Windows debug-output synchronization.
    OutputDebugStringW(L"SMILE startup fault fixture warmup.\n");
    DWORD baseline = 0, after = 0;
    GetProcessHandleCount(GetCurrentProcess(), &baseline);
    for (int fault = 1; fault <= 6; ++fault) {
        smile_startup_test_fault = fault;
        ULONGLONG fault_start = GetTickCount64();
        if (smile_startup_resume(owner)) return 10 + fault;
        smile_startup_ready(); smile_startup_ready();
        if (GetTickCount64() - fault_start > 5000 || !IsWindow(owner) || GetPropW(owner, L"UnsavedDocument") != (HANDLE)123) return 20 + fault;
        GetProcessHandleCount(GetCurrentProcess(), &after);
        if (after != baseline) { printf("handle fault=%d before=%lu after=%lu\n", fault, baseline, after); return 30 + fault; }
    }
    smile_startup_test_fault = 0;
    if (!smile_startup_resume(owner)) return 40;
    smile_startup_ready();
    // The fault-enabled owner uses a 500-ms deadline; retain actual Windows
    // minimized eligibility for longer than it, then restore through the pumped queue.
    ShowWindow(owner, SW_MINIMIZE);
    if (!IsIconic(owner) || !SetTimer(owner, 17, 900, restore_owner)) return 41;
    ULONGLONG hidden_start = GetTickCount64();
    if (!smile_startup_resume(owner)) { printf("FAIL: healthy minimized reload timed out before restore.\n"); return 42; }
    ULONGLONG restored = GetTickCount64();
    smile_startup_ready();
    if (restored - hidden_start < 850 || GetTickCount64() - restored < 1000) return 43;
    ShowWindow(owner, SW_MINIMIZE);
    if (!SetTimer(owner, 18, 150, cancel_minimized_reload)) return 44;
    if (smile_startup_resume(owner) || !IsWindow(owner) || GetPropW(owner, L"UnsavedDocument") != (HANDLE)123) return 45;
    ShowWindow(owner, SW_RESTORE);
    if (!smile_startup_resume(owner)) return 46;
    smile_startup_ready();
    printf("PASS: native minimized pre-admission wait, restore, visible minimum, cancel and retry.\n");
    DestroyWindow(owner);
    printf("PASS: native decode/event/thread/window/timer/cancel faults retain owner and balance handles; retry succeeds.\n");
#endif
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

# Fault-enabled startup owner is compiled only into these isolated test executables.
$faultOwner = Join-Path $target 'startup-fault-owner.obj'
$faultCommand = @"
@echo off
call "$vs\VC\Auxiliary\Build\vcvars64.bat" >nul
if errorlevel 1 exit /b 1
cl /nologo /c /EHsc /MT /DUNICODE /D_UNICODE /DSMILE_STARTUP_TESTS /I"$root\src\Smile.NativeRuntime" /I"$root\artifacts\temp\runtime\Release" "$root\src\Smile.NativeRuntime\startup\startup.cpp" /Fo"$faultOwner"
if errorlevel 1 exit /b 1
cl /nologo /EHsc /MT /DSMILE_STARTUP_TESTS /I"$root\src\Smile.NativeRuntime" "$target\startup-timing.cpp" /Fo"$target\startup-fault-timing.obj" /Fe"$target\startup-fault-timing.exe" /link "$faultOwner" "$root\artifacts\runtime\Smile.NativeRuntime.lib" user32.lib gdi32.lib gdiplus.lib dwmapi.lib shell32.lib ole32.lib
exit /b %errorlevel%
"@
[IO.File]::WriteAllText((Join-Path $target 'build-faults.cmd'), $faultCommand, $utf8)
& (Join-Path $target 'build-faults.cmd')
if ($LASTEXITCODE -ne 0) { throw 'Native startup fault owner build failed.' }
& (Join-Path $PSScriptRoot 'run-bounded-test.cmd') 30 (Join-Path $target 'startup-fault-timing.exe')
if ($LASTEXITCODE -ne 0) { throw 'Native startup faults did not preserve the session/handles.' }

# Use the actual Viewer loading boundary, with a public synthetic actor and isolated storage.
$guard = @'
Import Smile.Tools.Character3DViewerLifecycle As ViewerLifecycle
Import Smile.Tools.Character3DViewerSession As ViewerSession
Import Smile.Simple3D.Character3D As Character3D

Dim Session As ViewerSession.State
Dim Actor As Character3D.Actor
Dim Baseline As Number
Dim Shown As Boolean
Dim Valid As Boolean

Game Window "Viewer Loading Guard"

Baseline = Character3D.LiveActorCount()
Actor = Character3D.LoadWithPolicy("Assets\Generation2\VraxV1\Vrax.sm3d",
    Character3D.CHARACTER_LOAD_REQUIRE_PBR)
Session.Ready = True
Session.SelectedCharacterTab = 5
Session.ViewerEpoch = 77
Session.FirstViewerError = 93

Show Screen

Shown = ViewerLifecycle.BeginCharacterSwitchLoading(Session)
Valid = Not Shown And Session.LoadingFailed And Session.Ready
Valid = Valid And Session.SelectedCharacterTab = 5 And Session.ViewerEpoch = 77
Valid = Valid And Session.FirstViewerError = 93 And Character3D.IsValid(Actor)
Valid = Valid And Character3D.LiveActorCount() = Baseline + 1
Shown = ViewerLifecycle.BeginCharacterSwitchLoading(Session)
Valid = Valid And Shown And Not Session.LoadingFailed And Session.Ready
Valid = Valid And Character3D.IsValid(Actor)

Show Screen

Call Character3D.Destroy(Actor)

Valid = Valid And Character3D.LiveActorCount() = Baseline

If Valid Then
    Print "Viewer loading guard passed."
Else
    Print "Viewer loading guard FAILED."
End If

End Program
'@
$guardRoot = Join-Path $target 'ViewerGuard'
$null = New-Item -ItemType Directory -Force -Path $guardRoot
[IO.File]::WriteAllText((Join-Path $guardRoot 'Program.smile'), $guard, $utf8)
$viewerRoot = Join-Path $root 'tools/Character3DViewer'
$preparedProject = & (Join-Path $PSScriptRoot 'prepare-viewer-hardening-fixture.ps1') -OutputDirectory $guardRoot
[xml]$project = Get-Content -LiteralPath $preparedProject -Raw
$project.SmileProject.PropertyGroup.StartupFile = 'Program.smile'
$project.SmileProject.PropertyGroup.OutputName = 'ViewerGuard'
$project.SmileProject.PropertyGroup.ApplicationId = 'smile.tests.viewer-loading-guard'
foreach ($item in $project.SelectNodes('//*[@Include]')) {
    if ($item.GetAttribute('StartupOnly') -eq 'true') {
        $item.SetAttribute('Include', 'Program.smile')
    }
}
$guardProject = Join-Path $guardRoot 'ViewerGuard.smileproj'
$project.Save($guardProject)
$guardExe = Join-Path $guardRoot 'ViewerGuard.exe'
$buildLines = & $compiler --project $guardProject --keep-temp --graphics DirectX -o $guardExe
$buildLines | Write-Host
if ($LASTEXITCODE -ne 0) { throw 'Viewer loading guard compilation failed.' }
$objectPath = ($buildLines | Where-Object { $_ -like 'Object: *' }).Substring(8)
if (-not (Test-Path -LiteralPath $objectPath)) { throw 'Missing retained generated object for isolated runtime link.' }
$control = @'
#include <windows.h>
extern "C" {
int smile_startup_test_fault;
void smile_startup_test_resume() {
    static int attempt;
    WCHAR value[16] = {};
    GetEnvironmentVariableW(L"SMILE_TEST_STARTUP_FAULT_ONCE", value, 16);
    smile_startup_test_fault = attempt++ == 0 ? (value[0] == L'6' ? 6 : 1) : 0;
}
}
'@
[IO.File]::WriteAllText((Join-Path $target 'startup-fault-control.cpp'), $control, $utf8)
$guardCommand = @"
@echo off
call "$vs\VC\Auxiliary\Build\vcvars64.bat" >nul
if errorlevel 1 exit /b 1
cl /nologo /c /EHsc /MT "$target\startup-fault-control.cpp" /Fo"$target\startup-fault-control.obj"
if errorlevel 1 exit /b 1
link /nologo /subsystem:windows /entry:main /machine:x64 /out:"$guardRoot\ViewerGuardFault.exe" "$objectPath" "$faultOwner" "$target\startup-fault-control.obj" "$root\artifacts\runtime\Smile.NativeRuntime.lib" kernel32.lib user32.lib gdi32.lib gdiplus.lib dwmapi.lib d3d11.lib d3dcompiler.lib dxgi.lib d2d1.lib dwrite.lib windowscodecs.lib winmm.lib shell32.lib ole32.lib windowsapp.lib xaudio2.lib ucrt.lib msvcrt.lib msvcprt.lib vcruntime.lib
exit /b %errorlevel%
"@
[IO.File]::WriteAllText((Join-Path $target 'build-guard-fault.cmd'), $guardCommand, $utf8)
& (Join-Path $target 'build-guard-fault.cmd')
if ($LASTEXITCODE -ne 0) { throw 'Isolated Viewer guard fault link failed.' }
try {
    foreach ($fault in @('1', '6')) {
        $env:SMILE_TEST_STARTUP_FAULT_ONCE = $fault
        $actual = & (Join-Path $PSScriptRoot 'run-bounded-test.cmd') 20 (Join-Path $guardRoot 'ViewerGuardFault.exe')
        if ($LASTEXITCODE -ne 0 -or $actual -ne 'Viewer loading guard passed.') { throw "Generated native Viewer guard failed for fault $fault : $actual" }
    }
} finally { Remove-Item Env:SMILE_TEST_STARTUP_FAULT_ONCE -ErrorAction SilentlyContinue }
& $compiler --project $guardProject --target web --output-dir (Join-Path $guardRoot 'Web')
if ($LASTEXITCODE -ne 0) { throw 'Web Viewer loading guard compilation failed.' }
Write-Host 'PASS: generated native Viewer loading guard preserves the existing actor/session on decode failure and cancellation, then retries.'
$guardExpected = Join-Path $guardRoot 'expected.txt'
[IO.File]::WriteAllText($guardExpected, "Viewer loading guard passed." + [Environment]::NewLine, $utf8)
foreach ($fault in @('decode', 'cancel')) {
    & node (Join-Path $PSScriptRoot 'run-web-test.js') (Join-Path $guardRoot 'Web') --renderer3d-state --startup-repeat-fault $fault --expected $guardExpected
    if ($LASTEXITCODE -ne 0) { throw "Generated Web Viewer guard failed: $fault" }
}

# Local browser fault harness; generated runtime, loader script and game stay unchanged.
# Decode interception is confined to this extra HTML document, never a published program.
$guardWeb = Join-Path $guardRoot 'Web'
$html = [IO.File]::ReadAllText((Join-Path $guardWeb 'index.html'))
$injection = @'
<script>
(() => {
    const original = HTMLImageElement.prototype.decode;
    let attempt = 0;
    const mode = new URLSearchParams(location.search).get("fault") || "decode";
    window.__loadingEvidence = { mode, results: [], errors: [] };
    addEventListener("unhandledrejection", event => __loadingEvidence.errors.push(String(event.reason)));
    HTMLImageElement.prototype.decode = function() {
        if (this.id === "smile-loading-logo" && ++attempt === 2) {
            if (mode === "cancel") return new Promise(() => {});
            return Promise.reject(new Error("Isolated repeat decode fault"));
        }
        return original.call(this);
    };
})();
</script>
'@
$observation = @'
<script>
(() => {
    const begin = smileStartup.begin, finish = smileStartup.finish;
    let shownAt = 0;
    smileStartup.painted.then(shown => { if (shown) shownAt = performance.now(); });
    smileStartup.begin = () => {
        const result = begin();
        result.then(shown => {
            __loadingEvidence.results.push({ operation: "begin", shown });
            if (shown) shownAt = performance.now();
        });
        return result;
    };
    smileStartup.finish = () => {
        const result = finish();
        result.then(shown => {
            __loadingEvidence.results.push({ operation: "finish", shown, visibleMs: performance.now() - shownAt });
            let report = document.getElementById("test-loading-evidence");
            if (!report) {
                report = document.createElement("pre"); report.id = "test-loading-evidence";
                document.body.append(report);
            }
            report.textContent = JSON.stringify(__loadingEvidence, null, 2);
        });
        return result;
    };
})();
</script>
'@
$html = $html.Insert($html.IndexOf('<script>'), $injection)
$loaderEnd = $html.IndexOf('</script>', $html.IndexOf('window.smileStartup =')) + '</script>'.Length
$html = $html.Insert($loaderEnd, $observation)
[IO.File]::WriteAllText((Join-Path $guardWeb 'fault-test.html'), $html, $utf8)
Write-Host 'Chrome harness: ViewerGuard/Web/fault-test.html?fault=decode (or cancel).'

# Real-browser eligibility fixture: hide this tab when prompted, wait over 30 seconds,
# then return. Only this extra test page controls decode; emitted programs stay intact.
$visibilityInjection = @'
<script>
(() => {
    const original = HTMLImageElement.prototype.decode;
    const mode = new URLSearchParams(location.search).get("mode") || "initial";
    let attempt = 0, target = mode === "repeat" ? 2 : 1;
    const evidence = window.__visibilityEvidence = { mode, phase: "Starting", events: [], completions: [], errors: [] };
    function report() {
        let output = document.getElementById("test-visibility-evidence");
        if (!output) {
            output = document.createElement("pre"); output.id = "test-visibility-evidence";
            output.style.cssText = "position:fixed;left:8px;top:8px;z-index:99999;background:#08101e;color:white;padding:12px;max-height:45vh;overflow:auto;font:14px monospace";
            document.body.append(output);
            const hide = document.createElement("a"); hide.id = "test-background-link";
            hide.href = "visibility-wait.html"; hide.target = "_blank";
            hide.textContent = "Background This Test For 31 Seconds";
            hide.style.cssText = "position:fixed;right:20px;top:20px;z-index:99999;background:#16344d;color:white;padding:18px;font:16px sans-serif";
            document.body.append(hide);
        }
        output.textContent = JSON.stringify(evidence, null, 2);
    }
    evidence.report = report;
    addEventListener("unhandledrejection", event => { evidence.errors.push(String(event.reason)); report(); });
    document.addEventListener("visibilitychange", () => {
        evidence.events.push({ hidden: document.hidden, at: performance.now() }); report();
    });
    HTMLImageElement.prototype.decode = function() {
        const decoded = original.call(this);
        if (this.id !== "smile-loading-logo" || ++attempt !== target) return decoded;
        return decoded.then(() => new Promise(resolve => {
            evidence.phase = "Switch to another tab for at least 31 seconds, then return";
            report();
            function waitForHidden() {
                if (!document.hidden) return;
                document.removeEventListener("visibilitychange", waitForHidden);
                evidence.decodedHiddenAt = performance.now();
                evidence.phase = "Decoded successfully; awaiting visible presentation";
                report(); resolve();
            }
            document.addEventListener("visibilitychange", waitForHidden);
            waitForHidden();
        }));
    };
})();
</script>
'@
$visibilityObservation = @'
<script>
(() => {
    const loader = smileStartup, evidence = __visibilityEvidence;
    const begin = loader.begin, finish = loader.finish;
    let paintedAt = 0, visibleSince = null, visibleMs = 0;
    function admitted(shown) {
        if (shown) { paintedAt = performance.now(); visibleSince = paintedAt; visibleMs = 0; }
        evidence.completions.push({ operation: "admission", shown, at: performance.now() }); evidence.report();
    }
    document.addEventListener("visibilitychange", () => {
        const now = performance.now();
        if (visibleSince !== null) visibleMs += now - visibleSince;
        visibleSince = !document.hidden && paintedAt ? now : null;
    });
    loader.painted.then(admitted);
    loader.begin = () => { const result = begin(); result.then(admitted); return result; };
    loader.finish = () => {
        const result = finish();
        result.then(shown => {
            const elapsed = visibleMs + (visibleSince === null ? 0 : performance.now() - visibleSince);
            evidence.completions.push({ operation: "finish", shown, visibleMs: elapsed });
            if (evidence.decodedHiddenAt) evidence.phase = "Presentation resumed; inspect successful admissions and visible durations";
            evidence.report();
        });
        return result;
    };
})();
</script>
'@
$visibilityWeb = Join-Path $target 'ReloadWeb'
$visibilityHtml = [IO.File]::ReadAllText((Join-Path $visibilityWeb 'index.html'))
$visibilityHtml = $visibilityHtml.Insert($visibilityHtml.IndexOf('<script>'), $visibilityInjection)
$loaderEnd = $visibilityHtml.IndexOf('</script>', $visibilityHtml.IndexOf('window.smileStartup =')) + '</script>'.Length
$visibilityHtml = $visibilityHtml.Insert($loaderEnd, $visibilityObservation)
[IO.File]::WriteAllText((Join-Path $visibilityWeb 'visibility-test.html'), $visibilityHtml, $utf8)
$waitHtml = @'
<!doctype html><html lang="en"><meta charset="utf-8"><title>SMILE Startup Visibility Wait</title>
<body style="background:#08101e;color:white;font:20px sans-serif;padding:48px">
<h1>SMILE Startup Visibility Wait</h1><p id="elapsed">Keep this tab active for 31 seconds.</p>
<button id="return" disabled onclick="window.close()">Return To Startup Test</button>
<script>
const started = performance.now();
const tick = setInterval(() => {
    const elapsed = performance.now() - started;
    document.getElementById("elapsed").textContent = "Time in this tab: " + Math.floor(elapsed / 1000) + " seconds";
    if (elapsed >= 31000) { document.getElementById("return").disabled = false; clearInterval(tick); }
}, 250);
</script></body></html>
'@
[IO.File]::WriteAllText((Join-Path $visibilityWeb 'visibility-wait.html'), $waitHtml, $utf8)
Write-Host 'Chrome visibility harness: ReloadWeb/visibility-test.html?mode=initial (or repeat).'
